using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEngine;
using UnityEngine.SceneManagement;

[DisallowMultipleComponent]
[RequireComponent(typeof(PlayerExperience), typeof(Inventory), typeof(Equipment))]
[RequireComponent(typeof(PlayerSkillSystem))]
public partial class SaveManager : MonoBehaviour
{
    public const int CurrentVersion = 5;

    private static GameSaveData pendingCrossSceneSave;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStaticState()
    {
        pendingCrossSceneSave = null;
    }

    [SerializeField] private ItemRegistry itemRegistry;
    [SerializeField] private EnemyPersistenceRegistry enemyRegistry;
    [SerializeField] private ItemPickup runtimePickupPrefab;
    [SerializeField] private string fileName = "save.json";

    private PlayerExperience progression;
    private Inventory inventory;
    private Equipment equipment;
    private PlayerStats playerStats;
    private Rigidbody2D playerBody;

    private sealed class WorldPickupRestoreEntry
    {
        public ItemData Item;
        public int Quantity;
        public Vector3 Position;
    }

    public string SavePath => Path.Combine(
        Application.persistentDataPath,
        string.IsNullOrWhiteSpace(fileName) ? "save.json" : fileName);

    private void Awake()
    {
        ResolveSystems();
    }

    private void OnEnable()
    {
        SceneLoader.SceneLoadCompleted += HandleSceneLoadCompleted;
    }

    private void OnDisable()
    {
        SceneLoader.SceneLoadCompleted -= HandleSceneLoadCompleted;
    }

    public bool SaveGame()
    {
        ResolveSystems();
        if (!ValidateDependencies())
            return false;

        try
        {
            GameSaveData data = BuildSaveData();
            string json = JsonUtility.ToJson(data, true);
            WriteAtomically(SavePath, json);
            Debug.Log($"[SaveManager] Game saved to '{SavePath}'.", this);
            return true;
        }
        catch (Exception exception)
        {
            Debug.LogError($"[SaveManager] Save failed: {exception.Message}", this);
            return false;
        }
    }

    public bool LoadGame()
    {
        string path = SavePath;
        if (!File.Exists(path))
        {
            Debug.LogWarning($"[SaveManager] No save file exists at '{path}'. Current state was unchanged.", this);
            return false;
        }

        try
        {
            string json = File.ReadAllText(path, Encoding.UTF8);
            GameSaveData data = JsonUtility.FromJson<GameSaveData>(json);
            if (!ValidateSaveDataStructure(data))
                return false;

            string activeSceneName = SceneManager.GetActiveScene().name;
            if (string.Equals(data.sceneName, activeSceneName, StringComparison.Ordinal))
                return ApplySaveData(data, path);

            SceneLoader loader = ResolveSceneLoader();
            if (loader == null)
            {
                Debug.LogError("[SaveManager] Cross-Scene Load requires an active SceneLoader.", this);
                return false;
            }

            if (!loader.CanLoadScene(data.sceneName))
            {
                Debug.LogError(
                    $"[SaveManager] Saved Scene '{data.sceneName}' is not enabled in Build Settings. Current state was unchanged.",
                    this);
                return false;
            }

            pendingCrossSceneSave = data;
            PlayerTransitionBuffer.Clear();
            Debug.Log($"[SaveManager] Loading saved Scene '{data.sceneName}' before applying the snapshot.", this);
            if (!loader.LoadScene(data.sceneName, SceneLoadReason.SaveRestore))
            {
                pendingCrossSceneSave = null;
                return false;
            }

            return true;
        }
        catch (Exception exception)
        {
            pendingCrossSceneSave = null;
            Debug.LogError($"[SaveManager] Load failed: {exception.Message}", this);
            return false;
        }
    }

    public bool TryCapturePlayerTransition(out PlayerTransitionState state)
    {
        state = null;
        ResolveSystems();
        if (!ValidatePlayerDependencies())
            return false;

        PlayerTransitionState captured = new PlayerTransitionState
        {
            CurrentHealth = Mathf.RoundToInt(playerStats.CurrentHP),
            Level = progression.CurrentLevel,
            CurrentExperience = progression.CurrentExperience,
            EquippedWeapon = equipment.EquippedWeapon
        };

        CaptureTransitionSkills(captured);

        IReadOnlyList<InventorySlot> slots = inventory.Slots;
        for (int i = 0; i < slots.Count; i++)
        {
            InventorySlot slot = slots[i];
            if (slot == null || slot.IsEmpty)
                continue;

            captured.Inventory.Add(new InventoryRestoreEntry(i, slot.Item, slot.Quantity));
        }

        state = captured;
        return true;
    }

    private void HandleSceneLoadCompleted(Scene scene, SceneLoadReason reason)
    {
        if (gameObject.scene != scene)
            return;

        if (reason == SceneLoadReason.SaveRestore)
        {
            GameSaveData data = pendingCrossSceneSave;
            pendingCrossSceneSave = null;
            if (data == null)
            {
                Debug.LogError("[SaveManager] Saved Scene loaded, but no pending snapshot exists.", this);
                return;
            }

            ApplySaveData(data, SavePath);
            return;
        }

        if (PlayerTransitionBuffer.TryConsume(out PlayerTransitionState transitionState))
            ApplyPlayerTransition(transitionState);
    }

    private bool ApplySaveData(GameSaveData data, string path)
    {
        ResolveSystems();
        if (!ValidateDependencies())
            return false;

        string activeSceneName = SceneManager.GetActiveScene().name;
        if (!string.Equals(data.sceneName, activeSceneName, StringComparison.Ordinal))
        {
            Debug.LogError(
                $"[SaveManager] Refused to apply Scene '{data.sceneName}' snapshot in active Scene '{activeSceneName}'.",
                this);
            return false;
        }

        try
        {
            if (!TryResolveInventory(data.inventory, out List<InventoryRestoreEntry> restoredInventory))
                return false;

            WeaponData restoredWeapon = ResolveWeapon(data.equipment);
            if (!TryResolveEnemyStates(data.enemies, out List<EnemyPersistenceState> restoredEnemies))
                return false;

            if (!TryResolveWorldPickups(
                    data.worldPickups,
                    out List<WorldPickupRestoreEntry> restoredWorldPickups))
            {
                return false;
            }

            if (!progression.RestoreProgress(
                    data.progression.level,
                    data.progression.currentExperience))
            {
                Debug.LogError("[SaveManager] Progression data is invalid. Current state was unchanged.", this);
                return false;
            }

            if (!RestoreSkillData(data.skills))
                return false;

            RestorePlayerState(data.player);
            inventory.RestoreSlots(restoredInventory);
            equipment.RestoreWeapon(restoredWeapon);
            enemyRegistry.RestoreStates(restoredEnemies);
            ClearRuntimeWorldDrops();
            RestoreRuntimeWorldDrops(restoredWorldPickups);

            Debug.Log($"[SaveManager] Game loaded from '{path}'.", this);
            return true;
        }
        catch (Exception exception)
        {
            Debug.LogError($"[SaveManager] Snapshot apply failed: {exception.Message}", this);
            return false;
        }
    }

    private bool ApplyPlayerTransition(PlayerTransitionState state)
    {
        ResolveSystems();
        if (state == null || !ValidatePlayerDependencies())
            return false;

        if (!progression.RestoreProgress(state.Level, state.CurrentExperience))
        {
            Debug.LogError("[SaveManager] Runtime Player transition state is invalid.", this);
            return false;
        }

        RestoreTransitionSkills(state);
        if (!playerStats.RestoreHealthState(state.CurrentHealth))
        {
            Debug.LogError("[SaveManager] Runtime Player health state is invalid.", this);
            return false;
        }

        inventory.RestoreSlots(state.Inventory);
        equipment.RestoreWeapon(state.EquippedWeapon);
        Debug.Log("[SaveManager] Runtime Player state restored after normal Level transition.", this);
        return true;
    }

    public bool DeleteSave()
    {
        try
        {
            string path = SavePath;
            if (!File.Exists(path))
            {
                Debug.LogWarning($"[SaveManager] No save file exists at '{path}'.", this);
                return false;
            }

            File.Delete(path);
            string temporaryPath = path + ".tmp";
            if (File.Exists(temporaryPath))
                File.Delete(temporaryPath);

            Debug.Log($"[SaveManager] Deleted save file '{path}'.", this);
            return true;
        }
        catch (Exception exception)
        {
            Debug.LogError($"[SaveManager] Delete failed: {exception.Message}", this);
            return false;
        }
    }

    private GameSaveData BuildSaveData()
    {
        GameSaveData data = new GameSaveData
        {
            version = CurrentVersion,
            sceneName = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name,
            player = new PlayerSaveData
            {
                position = ToSaveVector(transform.position),
                currentHealth = Mathf.RoundToInt(playerStats.CurrentHP)
            },
            progression = new ProgressionSaveData
            {
                level = progression.CurrentLevel,
                currentExperience = progression.CurrentExperience
            },
            equipment = new EquipmentSaveData()
        };

        CaptureSkillData(data);

        IReadOnlyList<InventorySlot> slots = inventory.Slots;
        for (int i = 0; i < slots.Count; i++)
        {
            InventorySlot slot = slots[i];
            if (slot == null || slot.IsEmpty)
                continue;

            data.inventory.Add(new InventorySlotSaveData
            {
                slotIndex = i,
                itemId = slot.Item.ID,
                quantity = slot.Quantity
            });
        }

        WeaponData weapon = equipment.EquippedWeapon;
        data.equipment.weaponItemId = weapon != null ? weapon.ID : string.Empty;

        List<EnemyPersistenceState> enemyStates = enemyRegistry.CaptureStates();
        for (int i = 0; i < enemyStates.Count; i++)
        {
            EnemyPersistenceState state = enemyStates[i];
            data.enemies.Add(new EnemySaveData
            {
                persistentId = state.PersistentId,
                alive = state.Alive,
                position = ToSaveVector(state.Position),
                currentHealth = state.CurrentHealth
            });
        }

        ItemPickup[] pickups = UnityEngine.Object.FindObjectsByType<ItemPickup>(FindObjectsInactive.Exclude);
        for (int i = 0; i < pickups.Length; i++)
        {
            ItemPickup pickup = pickups[i];
            if (!pickup.IsRuntimeDrop || pickup.Item == null || pickup.Quantity <= 0)
                continue;

            data.worldPickups.Add(new WorldPickupSaveData
            {
                itemId = pickup.Item.ID,
                quantity = pickup.Quantity,
                position = ToSaveVector(pickup.transform.position)
            });
        }

        return data;
    }

    private bool TryResolveWorldPickups(
        List<WorldPickupSaveData> savedPickups,
        out List<WorldPickupRestoreEntry> restoredPickups)
    {
        restoredPickups = new List<WorldPickupRestoreEntry>();
        if (savedPickups == null)
            return true;

        for (int i = 0; i < savedPickups.Count; i++)
        {
            WorldPickupSaveData saved = savedPickups[i];
            if (saved == null || saved.position == null || saved.quantity <= 0
                || !itemRegistry.TryResolve(saved.itemId, out ItemData item))
            {
                Debug.LogError($"[SaveManager] Invalid world pickup entry at index {i}.", this);
                return false;
            }

            restoredPickups.Add(new WorldPickupRestoreEntry
            {
                Item = item,
                Quantity = Mathf.Clamp(saved.quantity, 1, item.MaxStack),
                Position = FromSaveVector(saved.position)
            });
        }

        return true;
    }

    private bool TryResolveInventory(
        List<InventorySlotSaveData> savedSlots,
        out List<InventoryRestoreEntry> restoredEntries)
    {
        restoredEntries = new List<InventoryRestoreEntry>();
        if (savedSlots == null)
            return true;

        HashSet<int> usedIndices = new HashSet<int>();
        for (int i = 0; i < savedSlots.Count; i++)
        {
            InventorySlotSaveData savedSlot = savedSlots[i];
            if (savedSlot == null
                || savedSlot.slotIndex < 0
                || savedSlot.slotIndex >= inventory.SlotCount
                || savedSlot.quantity <= 0
                || !usedIndices.Add(savedSlot.slotIndex))
            {
                Debug.LogWarning($"[SaveManager] Skipped invalid inventory entry at save index {i}.", this);
                continue;
            }

            if (!itemRegistry.TryResolve(savedSlot.itemId, out ItemData item))
            {
                Debug.LogWarning(
                    $"[SaveManager] Unknown item ID '{savedSlot.itemId}' was skipped.",
                    this);
                continue;
            }

            int quantity = Mathf.Clamp(savedSlot.quantity, 1, item.MaxStack);
            if (quantity != savedSlot.quantity)
            {
                Debug.LogWarning(
                    $"[SaveManager] Quantity for '{savedSlot.itemId}' was clamped to MaxStack {item.MaxStack}.",
                    this);
            }

            restoredEntries.Add(new InventoryRestoreEntry(savedSlot.slotIndex, item, quantity));
        }

        return true;
    }

    private bool TryResolveEnemyStates(
        List<EnemySaveData> savedEnemies,
        out List<EnemyPersistenceState> restoredEnemies)
    {
        restoredEnemies = new List<EnemyPersistenceState>();
        if (savedEnemies == null)
            return true;

        HashSet<string> ids = new HashSet<string>(StringComparer.Ordinal);
        for (int i = 0; i < savedEnemies.Count; i++)
        {
            EnemySaveData savedEnemy = savedEnemies[i];
            if (savedEnemy == null
                || string.IsNullOrWhiteSpace(savedEnemy.persistentId)
                || !ids.Add(savedEnemy.persistentId)
                || savedEnemy.position == null
                || (savedEnemy.alive && savedEnemy.currentHealth <= 0))
            {
                Debug.LogError($"[SaveManager] Invalid or duplicate enemy entry at index {i}.", this);
                return false;
            }

            restoredEnemies.Add(new EnemyPersistenceState(
                savedEnemy.persistentId,
                savedEnemy.alive,
                FromSaveVector(savedEnemy.position),
                savedEnemy.alive ? savedEnemy.currentHealth : 0));
        }

        return true;
    }

    private void RestorePlayerState(PlayerSaveData savedPlayer)
    {
        Vector3 position = FromSaveVector(savedPlayer.position);
        transform.position = position;
        if (playerBody != null)
        {
            playerBody.position = position;
            playerBody.linearVelocity = Vector2.zero;
            playerBody.angularVelocity = 0f;
        }

        if (!playerStats.RestoreHealthState(savedPlayer.currentHealth))
            throw new InvalidDataException("Player health could not be restored.");

        Physics2D.SyncTransforms();
    }

    private static Vector3SaveData ToSaveVector(Vector3 value)
    {
        return new Vector3SaveData { x = value.x, y = value.y, z = value.z };
    }

    private static Vector3 FromSaveVector(Vector3SaveData value)
    {
        return new Vector3(value.x, value.y, value.z);
    }

    private void RestoreRuntimeWorldDrops(IReadOnlyList<WorldPickupRestoreEntry> pickups)
    {
        for (int i = 0; i < pickups.Count; i++)
        {
            WorldPickupRestoreEntry entry = pickups[i];
            ItemPickup pickup = Instantiate(runtimePickupPrefab, entry.Position, Quaternion.identity);
            if (!pickup.RestoreRuntimeDrop(entry.Item, entry.Quantity, entry.Position))
            {
                Destroy(pickup.gameObject);
                throw new InvalidDataException($"World pickup '{entry.Item.ID}' could not be restored.");
            }
        }
    }

    private static void ClearRuntimeWorldDrops()
    {
        ItemPickup[] pickups = UnityEngine.Object.FindObjectsByType<ItemPickup>(
            FindObjectsInactive.Include);
        for (int i = 0; i < pickups.Length; i++)
        {
            if (!pickups[i].IsRuntimeDrop)
                continue;

            pickups[i].gameObject.SetActive(false);
            UnityEngine.Object.Destroy(pickups[i].gameObject);
        }
    }

    private WeaponData ResolveWeapon(EquipmentSaveData savedEquipment)
    {
        string weaponId = savedEquipment != null ? savedEquipment.weaponItemId : null;
        if (string.IsNullOrWhiteSpace(weaponId))
            return null;

        if (!itemRegistry.TryResolve(weaponId, out ItemData item))
        {
            Debug.LogWarning($"[SaveManager] Unknown equipped weapon ID '{weaponId}' was skipped.", this);
            return null;
        }

        WeaponData weapon = item as WeaponData;
        if (weapon == null || weapon.ItemType != ItemType.Weapon)
        {
            Debug.LogWarning($"[SaveManager] Item ID '{weaponId}' is not a valid weapon.", this);
            return null;
        }

        return weapon;
    }

    private bool ValidateDependencies()
    {
        if (!ValidatePlayerDependencies() || enemyRegistry == null)
        {
            Debug.LogError("[SaveManager] Required Player or Enemy persistence systems were not found.", this);
            return false;
        }

        if (runtimePickupPrefab == null)
        {
            Debug.LogError("[SaveManager] Runtime ItemPickup prefab is not assigned. Run Setup Save System.", this);
            return false;
        }

        if (itemRegistry == null)
        {
            Debug.LogError("[SaveManager] ItemRegistry is not assigned.", this);
            return false;
        }

        if (!itemRegistry.ValidateRegistry(out string error))
        {
            Debug.LogError($"[SaveManager] Invalid ItemRegistry: {error}", itemRegistry);
            return false;
        }

        if (!enemyRegistry.ValidateSceneSetup(out string enemyError))
        {
            Debug.LogError($"[SaveManager] Invalid Enemy persistence setup: {enemyError}", enemyRegistry);
            return false;
        }

        return true;
    }

    private bool ValidatePlayerDependencies()
    {
        if (progression != null
            && inventory != null
            && equipment != null
            && playerStats != null
            && skillSystem != null
            && skillSystem.Database != null)
            return true;

        Debug.LogError("[SaveManager] Thiếu hệ thống bắt buộc: tiến trình Player, chỉ số, Cơ sở dữ liệu Kỹ năng, Inventory hoặc Equipment.", this);
        return false;
    }

    private bool ValidateSaveDataStructure(GameSaveData data)
    {
        if (data == null)
        {
            Debug.LogError("[SaveManager] Save JSON did not contain valid data.", this);
            return false;
        }

        if (!TryPrepareSaveData(data, out string versionError))
        {
            Debug.LogError($"[SaveManager] {versionError}", this);
            return false;
        }

        if (string.IsNullOrWhiteSpace(data.sceneName)
            || data.player == null
            || data.player.position == null
            || data.player.currentHealth <= 0
            || data.progression == null
            || data.progression.level < 1
            || data.progression.currentExperience < 0)
        {
            Debug.LogError("[SaveManager] Save snapshot structure is invalid.", this);
            return false;
        }

        return true;
    }

    private static SceneLoader ResolveSceneLoader()
    {
        return SceneLoader.Instance != null
            ? SceneLoader.Instance
            : UnityEngine.Object.FindAnyObjectByType<SceneLoader>();
    }

    private void ResolveSystems()
    {
        if (progression == null)
            progression = GetComponent<PlayerExperience>();
        if (inventory == null)
            inventory = GetComponent<Inventory>();
        if (equipment == null)
            equipment = GetComponent<Equipment>();
        if (playerStats == null)
            playerStats = GetComponent<PlayerStats>();
        if (skillSystem == null)
            skillSystem = GetComponent<PlayerSkillSystem>();
        if (playerBody == null)
            playerBody = GetComponent<Rigidbody2D>();
        if (enemyRegistry == null)
            enemyRegistry = FindAnyObjectByType<EnemyPersistenceRegistry>();
    }

    private static void WriteAtomically(string path, string contents)
    {
        string directory = Path.GetDirectoryName(path);
        if (!string.IsNullOrEmpty(directory))
            Directory.CreateDirectory(directory);

        string temporaryPath = path + ".tmp";
        File.WriteAllText(temporaryPath, contents, new UTF8Encoding(false));

        if (!File.Exists(path))
        {
            File.Move(temporaryPath, path);
            return;
        }

        try
        {
            File.Replace(temporaryPath, path, null);
        }
        catch (Exception exception) when (
            exception is PlatformNotSupportedException || exception is IOException)
        {
            File.Copy(temporaryPath, path, true);
            File.Delete(temporaryPath);
        }
    }
}
