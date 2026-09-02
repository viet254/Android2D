using System;
using UnityEngine;
using UnityEngine.SceneManagement;

public partial class SaveManager
{
    public bool SaveToSlot(int slotId)
    {
        ResolveSystems();
        if (!ValidateDependencies())
            return false;

        try
        {
            GameSaveData data = BuildSaveData();
            if (!SaveSlotStorage.WriteSlot(slotId, data, out string error))
            {
                Debug.LogError($"[SaveManager] Failed to save Slot {slotId}: {error}", this);
                return false;
            }

            Debug.Log(
                $"[SaveManager] Slot {slotId} saved to '{SaveSlotStorage.GetSavePath(slotId)}'.",
                this);
            return true;
        }
        catch (Exception exception)
        {
            Debug.LogError($"[SaveManager] Slot {slotId} save failed: {exception.Message}", this);
            return false;
        }
    }

    public static bool RequestLoadSlot(int slotId, out string error)
    {
        error = null;
        if (!SaveSlotStorage.TryReadSnapshot(
                slotId,
                out GameSaveData data,
                out SaveSlotStatus status,
                out string readError))
        {
            error = readError;
            Debug.LogError($"[SaveManager] Cannot load Slot {slotId} ({status}): {readError}");
            return false;
        }

        string path = SaveSlotStorage.GetSavePath(slotId);
        string activeSceneName = SceneManager.GetActiveScene().name;
        if (string.Equals(data.sceneName, activeSceneName, StringComparison.Ordinal))
        {
            SaveManager activeManager = UnityEngine.Object.FindAnyObjectByType<SaveManager>();
            if (activeManager == null)
            {
                error = "No active SaveManager exists in the saved Scene.";
                Debug.LogError($"[SaveManager] {error}");
                return false;
            }

            bool applied = activeManager.ApplySaveData(data, path);
            if (!applied)
                error = "Snapshot validation or runtime restoration failed. See Console for details.";
            return applied;
        }

        SceneLoader loader = ResolveSceneLoader();
        if (loader == null)
        {
            error = "Cross-Scene Load requires an active SceneLoader.";
            Debug.LogError($"[SaveManager] {error}");
            return false;
        }

        if (!loader.CanLoadScene(data.sceneName))
        {
            error = $"Saved Scene '{data.sceneName}' is not enabled in Build Settings.";
            Debug.LogError($"[SaveManager] {error}");
            return false;
        }

        pendingCrossSceneSave = data;
        PlayerTransitionBuffer.Clear();
        Debug.Log($"[SaveManager] Loading Slot {slotId} Scene '{data.sceneName}'.");
        if (loader.LoadScene(data.sceneName, SceneLoadReason.SaveRestore))
            return true;

        pendingCrossSceneSave = null;
        error = $"SceneLoader rejected the request for '{data.sceneName}'.";
        return false;
    }

    public static bool DeleteSlot(int slotId, out string error)
    {
        bool deleted = SaveSlotStorage.DeleteSlot(slotId, out error);
        if (deleted)
            Debug.Log($"[SaveManager] Slot {slotId} deleted.");
        else
            Debug.LogError($"[SaveManager] Could not delete Slot {slotId}: {error}");
        return deleted;
    }
}
