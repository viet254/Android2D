using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using UnityEngine;

public enum SaveSlotStatus
{
    Empty,
    Valid,
    Corrupted,
    Incompatible
}

[Serializable]
public sealed class SaveSlotMetadata
{
    public int slotId;
    public int snapshotVersion;
    public string sceneName;
    public int playerLevel;
    public int playerHealth;
    public string savedAtUtc;
    public long snapshotLength;
    public string snapshotSha256;
}

public sealed class SaveSlotInfo
{
    public int SlotId { get; }
    public SaveSlotStatus Status { get; }
    public SaveSlotMetadata Metadata { get; }
    public string Error { get; }
    public bool HasFile => Status != SaveSlotStatus.Empty;
    public bool CanLoad => Status == SaveSlotStatus.Valid;

    public SaveSlotInfo(
        int slotId,
        SaveSlotStatus status,
        SaveSlotMetadata metadata = null,
        string error = null)
    {
        SlotId = slotId;
        Status = status;
        Metadata = metadata;
        Error = error;
    }
}

public static class SaveSlotStorage
{
    public const int SlotCount = 3;
    public const string LegacyFileName = "save.json";

    public static string LegacySavePath =>
        Path.Combine(Application.persistentDataPath, LegacyFileName);

    public static string GetSavePath(int slotId)
    {
        ValidateSlotId(slotId);
        return Path.Combine(Application.persistentDataPath, $"save_slot_{slotId}.json");
    }

    public static string GetMetadataPath(int slotId)
    {
        ValidateSlotId(slotId);
        return Path.Combine(Application.persistentDataPath, $"save_slot_{slotId}.meta.json");
    }

    public static SaveSlotInfo GetSlotInfo(int slotId)
    {
        if (!IsValidSlotId(slotId))
            return new SaveSlotInfo(slotId, SaveSlotStatus.Corrupted, null, "Invalid slot ID.");

        string savePath = GetSavePath(slotId);
        if (!File.Exists(savePath))
            return new SaveSlotInfo(slotId, SaveSlotStatus.Empty);

        try
        {
            SaveSlotMetadata metadata = ReadMetadata(GetMetadataPath(slotId));
            if (IsMetadataCurrent(slotId, savePath, metadata))
            {
                SaveSlotStatus status = SaveManager.IsSupportedSnapshotVersion(metadata.snapshotVersion)
                    ? SaveSlotStatus.Valid
                    : SaveSlotStatus.Incompatible;
                string metadataError = status == SaveSlotStatus.Incompatible
                    ? $"Save version {metadata.snapshotVersion} is unsupported."
                    : null;
                return new SaveSlotInfo(slotId, status, metadata, metadataError);
            }

            if (!TryReadSnapshot(slotId, out GameSaveData data, out SaveSlotStatus readStatus, out string error))
                return new SaveSlotInfo(slotId, readStatus, null, error);

            SaveSlotMetadata rebuilt = BuildMetadata(
                slotId,
                data,
                File.GetLastWriteTimeUtc(savePath),
                savePath);
            return new SaveSlotInfo(slotId, SaveSlotStatus.Valid, rebuilt);
        }
        catch (Exception exception)
        {
            return new SaveSlotInfo(slotId, SaveSlotStatus.Corrupted, null, exception.Message);
        }
    }

    public static bool TryReadSnapshot(
        int slotId,
        out GameSaveData data,
        out SaveSlotStatus status,
        out string error)
    {
        data = null;
        status = SaveSlotStatus.Corrupted;
        error = null;

        if (!IsValidSlotId(slotId))
        {
            error = $"Slot ID {slotId} is outside the supported range 1-{SlotCount}.";
            return false;
        }

        string path = GetSavePath(slotId);
        if (!File.Exists(path))
        {
            status = SaveSlotStatus.Empty;
            error = $"Slot {slotId} is empty.";
            return false;
        }

        try
        {
            string json = File.ReadAllText(path, Encoding.UTF8);
            data = JsonUtility.FromJson<GameSaveData>(json);
            if (data == null)
            {
                error = "Save JSON did not contain a snapshot.";
                return false;
            }

            if (!SaveManager.TryPrepareSaveData(data, out error))
            {
                status = SaveSlotStatus.Incompatible;
                data = null;
                return false;
            }

            if (!ValidateSnapshotStructure(data, out error))
            {
                data = null;
                return false;
            }

            status = SaveSlotStatus.Valid;
            return true;
        }
        catch (Exception exception)
        {
            error = exception.Message;
            data = null;
            return false;
        }
    }

    public static bool WriteSlot(int slotId, GameSaveData data, out string error)
    {
        error = null;
        if (!IsValidSlotId(slotId))
        {
            error = $"Slot ID {slotId} is outside the supported range 1-{SlotCount}.";
            return false;
        }

        if (data == null || !ValidateSnapshotStructure(data, out error))
            return false;

        string savePath = GetSavePath(slotId);
        string metadataPath = GetMetadataPath(slotId);
        string saveTempPath = savePath + ".tmp";
        string metadataTempPath = metadataPath + ".tmp";
        string saveBackupPath = savePath + ".bak";
        string metadataBackupPath = metadataPath + ".bak";

        try
        {
            Directory.CreateDirectory(Application.persistentDataPath);
            string json = JsonUtility.ToJson(data, true);
            File.WriteAllText(saveTempPath, json, new UTF8Encoding(false));

            string verificationJson = File.ReadAllText(saveTempPath, Encoding.UTF8);
            GameSaveData verification = JsonUtility.FromJson<GameSaveData>(verificationJson);
            string verificationError = null;
            if (verification == null)
                verificationError = "Temporary JSON did not contain a snapshot.";
            else if (verification.version != SaveManager.CurrentVersion)
                verificationError = $"Unexpected snapshot version {verification.version}.";
            else if (!ValidateSnapshotStructure(verification, out verificationError))
            {
            }

            if (!string.IsNullOrEmpty(verificationError))
            {
                error = $"Temporary save validation failed: {verificationError}";
                return false;
            }

            SaveSlotMetadata metadata = BuildMetadata(slotId, data, DateTime.UtcNow, saveTempPath);
            string metadataJson = JsonUtility.ToJson(metadata, true);
            File.WriteAllText(metadataTempPath, metadataJson, new UTF8Encoding(false));
            SaveSlotMetadata metadataVerification = ReadMetadata(metadataTempPath);
            if (metadataVerification == null || metadataVerification.slotId != slotId)
            {
                error = "Temporary metadata validation failed.";
                return false;
            }

            BackupIfPresent(savePath, saveBackupPath);
            BackupIfPresent(metadataPath, metadataBackupPath);

            try
            {
                ReplaceFile(saveTempPath, savePath);
                ReplaceFile(metadataTempPath, metadataPath);
            }
            catch
            {
                RestoreBackup(savePath, saveBackupPath);
                RestoreBackup(metadataPath, metadataBackupPath);
                throw;
            }

            return true;
        }
        catch (Exception exception)
        {
            error = exception.Message;
            return false;
        }
        finally
        {
            DeleteIfPresent(saveTempPath);
            DeleteIfPresent(metadataTempPath);
            DeleteIfPresent(saveBackupPath);
            DeleteIfPresent(metadataBackupPath);
        }
    }

    public static bool DeleteSlot(int slotId, out string error)
    {
        error = null;
        if (!IsValidSlotId(slotId))
        {
            error = $"Slot ID {slotId} is outside the supported range 1-{SlotCount}.";
            return false;
        }

        try
        {
            string savePath = GetSavePath(slotId);
            string metadataPath = GetMetadataPath(slotId);
            DeleteIfPresent(savePath);
            DeleteIfPresent(metadataPath);
            DeleteIfPresent(savePath + ".tmp");
            DeleteIfPresent(metadataPath + ".tmp");
            DeleteIfPresent(savePath + ".bak");
            DeleteIfPresent(metadataPath + ".bak");
            return true;
        }
        catch (Exception exception)
        {
            error = exception.Message;
            return false;
        }
    }

    private static bool ValidateSnapshotStructure(GameSaveData data, out string error)
    {
        if (data == null
            || string.IsNullOrWhiteSpace(data.sceneName)
            || data.player == null
            || data.player.position == null
            || data.player.currentHealth <= 0
            || data.progression == null
            || data.progression.level < 1
            || data.progression.currentExperience < 0)
        {
            error = "Save snapshot structure is invalid.";
            return false;
        }

        error = null;
        return true;
    }

    private static SaveSlotMetadata BuildMetadata(
        int slotId,
        GameSaveData data,
        DateTime savedAtUtc,
        string snapshotPath)
    {
        FileInfo file = new FileInfo(snapshotPath);
        return new SaveSlotMetadata
        {
            slotId = slotId,
            snapshotVersion = data.version,
            sceneName = data.sceneName,
            playerLevel = data.progression.level,
            playerHealth = data.player.currentHealth,
            savedAtUtc = savedAtUtc.ToUniversalTime().ToString("O"),
            snapshotLength = file.Length,
            snapshotSha256 = ComputeSha256(snapshotPath)
        };
    }

    private static SaveSlotMetadata ReadMetadata(string path)
    {
        if (!File.Exists(path))
            return null;

        string json = File.ReadAllText(path, Encoding.UTF8);
        return JsonUtility.FromJson<SaveSlotMetadata>(json);
    }

    private static bool IsMetadataCurrent(int slotId, string savePath, SaveSlotMetadata metadata)
    {
        if (metadata == null
            || metadata.slotId != slotId
            || string.IsNullOrWhiteSpace(metadata.sceneName)
            || string.IsNullOrWhiteSpace(metadata.savedAtUtc)
            || string.IsNullOrWhiteSpace(metadata.snapshotSha256))
        {
            return false;
        }

        FileInfo file = new FileInfo(savePath);
        return file.Length == metadata.snapshotLength
            && string.Equals(
                ComputeSha256(savePath),
                metadata.snapshotSha256,
                StringComparison.OrdinalIgnoreCase);
    }

    private static string ComputeSha256(string path)
    {
        using FileStream stream = File.OpenRead(path);
        using SHA256 algorithm = SHA256.Create();
        byte[] hash = algorithm.ComputeHash(stream);
        StringBuilder result = new StringBuilder(hash.Length * 2);
        for (int i = 0; i < hash.Length; i++)
            result.Append(hash[i].ToString("x2"));
        return result.ToString();
    }

    private static void BackupIfPresent(string source, string backup)
    {
        DeleteIfPresent(backup);
        if (File.Exists(source))
            File.Copy(source, backup, true);
    }

    private static void RestoreBackup(string target, string backup)
    {
        if (File.Exists(backup))
        {
            File.Copy(backup, target, true);
            return;
        }

        DeleteIfPresent(target);
    }

    private static void ReplaceFile(string temporaryPath, string targetPath)
    {
        if (!File.Exists(targetPath))
        {
            File.Move(temporaryPath, targetPath);
            return;
        }

        try
        {
            File.Replace(temporaryPath, targetPath, null);
        }
        catch (Exception exception) when (
            exception is PlatformNotSupportedException || exception is IOException)
        {
            File.Copy(temporaryPath, targetPath, true);
            File.Delete(temporaryPath);
        }
    }

    private static void DeleteIfPresent(string path)
    {
        if (File.Exists(path))
            File.Delete(path);
    }

    private static bool IsValidSlotId(int slotId)
    {
        return slotId >= 1 && slotId <= SlotCount;
    }

    private static void ValidateSlotId(int slotId)
    {
        if (!IsValidSlotId(slotId))
        {
            throw new ArgumentOutOfRangeException(
                nameof(slotId),
                slotId,
                $"Slot ID must be between 1 and {SlotCount}.");
        }
    }
}
