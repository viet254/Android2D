using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class SaveSystemSetup
{
    private const string RegistryFolder = "Assets/Data/Save";
    private const string RegistryPath = RegistryFolder + "/ItemRegistry.asset";

    [MenuItem("Tools/Android2D/Save/Setup Save System")]
    private static void SetupSaveSystem()
    {
        Scene scene = SceneManager.GetActiveScene();
        if (!scene.IsValid() || !scene.isLoaded)
        {
            Debug.LogError("Save setup failed: no valid loaded Scene is active.");
            return;
        }

        if (!EnsureFolder("Assets/Data", "Save"))
            return;

        List<ItemData> items = FindAllItems();
        if (!ValidateItems(items, out string validationError))
        {
            Debug.LogError($"Save setup failed: {validationError}");
            return;
        }

        ItemPickup runtimePickupPrefab = FindRuntimePickupPrefab();
        if (runtimePickupPrefab == null)
            return;

        const string undoName = "Setup Save System";
        ItemRegistry registry = AssetDatabase.LoadAssetAtPath<ItemRegistry>(RegistryPath);
        if (registry == null)
        {
            registry = ScriptableObject.CreateInstance<ItemRegistry>();
            AssetDatabase.CreateAsset(registry, RegistryPath);
            Undo.RegisterCreatedObjectUndo(registry, undoName);
        }

        Undo.RecordObject(registry, undoName);
        SerializedObject registrySerialized = new SerializedObject(registry);
        SerializedProperty registryItems = registrySerialized.FindProperty("items");
        registryItems.arraySize = items.Count;
        for (int i = 0; i < items.Count; i++)
            registryItems.GetArrayElementAtIndex(i).objectReferenceValue = items[i];
        registrySerialized.ApplyModifiedProperties();
        EditorUtility.SetDirty(registry);

        if (!SetupEnemyPersistence(scene, undoName, out EnemyPersistenceRegistry enemyRegistry))
            return;

        PlayerController player = FindActiveScenePlayer(scene);
        if (player == null)
            return;

        SaveManager saveManager = player.GetComponent<SaveManager>();
        if (saveManager == null)
            saveManager = Undo.AddComponent<SaveManager>(player.gameObject);

        if (saveManager == null)
        {
            Debug.LogError("Save setup failed: SaveManager could not be added to Player.", player);
            return;
        }

        Undo.RecordObject(saveManager, undoName);
        SerializedObject managerSerialized = new SerializedObject(saveManager);
        SerializedProperty registryProperty = managerSerialized.FindProperty("itemRegistry");
        registryProperty.objectReferenceValue = registry;
        SerializedProperty enemyRegistryProperty = managerSerialized.FindProperty("enemyRegistry");
        enemyRegistryProperty.objectReferenceValue = enemyRegistry;
        SerializedProperty pickupPrefabProperty = managerSerialized.FindProperty("runtimePickupPrefab");
        pickupPrefabProperty.objectReferenceValue = runtimePickupPrefab;
        managerSerialized.ApplyModifiedProperties();
        EditorUtility.SetDirty(saveManager);
        EditorSceneManager.MarkSceneDirty(scene);
        Selection.activeGameObject = player.gameObject;

        Debug.Log(
            $"Save system setup complete. Registered {items.Count} item(s) and configured Player '{player.name}'. " +
            "Review the ItemRegistry and Player, then press Ctrl+S to save the Scene.",
            saveManager);
    }

    [MenuItem("Tools/Android2D/Save/Setup Enemy Persistent IDs")]
    private static void SetupEnemyPersistentIds()
    {
        Scene scene = SceneManager.GetActiveScene();
        if (!scene.IsValid() || !scene.isLoaded)
        {
            Debug.LogError("Enemy persistence setup failed: no valid loaded Scene is active.");
            return;
        }

        const string undoName = "Setup Enemy Persistent IDs";
        if (!SetupEnemyPersistence(scene, undoName, out EnemyPersistenceRegistry registry))
            return;

        SaveManager manager = UnityEngine.Object.FindAnyObjectByType<SaveManager>();
        if (manager != null && manager.gameObject.scene == scene)
        {
            Undo.RecordObject(manager, undoName);
            SerializedObject serialized = new SerializedObject(manager);
            serialized.FindProperty("enemyRegistry").objectReferenceValue = registry;
            serialized.ApplyModifiedProperties();
            EditorUtility.SetDirty(manager);
        }

        EditorSceneManager.MarkSceneDirty(scene);
        Debug.Log(
            "Enemy persistent IDs are configured. Review IDs and prefab references, then press Ctrl+S.",
            registry);
    }

    [MenuItem("Tools/Android2D/Save/Save Game")]
    private static void SaveGame()
    {
        SaveManager manager = FindRuntimeManager();
        if (manager != null)
            manager.SaveGame();
    }

    [MenuItem("Tools/Android2D/Save/Load Game")]
    private static void LoadGame()
    {
        SaveManager manager = FindRuntimeManager();
        if (manager != null)
            manager.LoadGame();
    }

    [MenuItem("Tools/Android2D/Save/Delete Save")]
    private static void DeleteSave()
    {
        SaveManager manager = FindRuntimeManager();
        if (manager == null)
            return;

        if (EditorUtility.DisplayDialog(
                "Delete Android2D Save",
                $"Delete the save file at:\n{manager.SavePath}?",
                "Delete",
                "Cancel"))
        {
            manager.DeleteSave();
        }
    }

    private static bool SetupEnemyPersistence(
        Scene scene,
        string undoName,
        out EnemyPersistenceRegistry registry)
    {
        registry = FindOrCreateEnemyRegistry(scene, undoName);
        if (registry == null)
            return false;

        Enemy[] initialEnemies = UnityEngine.Object.FindObjectsByType<Enemy>(FindObjectsInactive.Include);
        HashSet<string> prefabPaths = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        for (int i = 0; i < initialEnemies.Length; i++)
        {
            Enemy enemy = initialEnemies[i];
            if (enemy.gameObject.scene != scene)
                continue;

            string prefabPath = PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(enemy.gameObject);
            if (!string.IsNullOrEmpty(prefabPath))
                prefabPaths.Add(prefabPath);
        }

        foreach (string prefabPath in prefabPaths)
        {
            if (!EnsureEnemyPersistentMarkerOnPrefab(prefabPath))
                return false;
        }

        Enemy[] enemies = UnityEngine.Object.FindObjectsByType<Enemy>(FindObjectsInactive.Include);
        HashSet<string> ids = new HashSet<string>(StringComparer.Ordinal);
        int configuredCount = 0;
        for (int i = 0; i < enemies.Length; i++)
        {
            Enemy enemy = enemies[i];
            if (enemy.gameObject.scene != scene)
                continue;

            GameObject prefabSource = ResolveEnemyPrefabAsset(enemy.gameObject);
            if (prefabSource == null)
            {
                Debug.LogError(
                    $"Enemy '{enemy.name}' is not linked to a prefab and cannot be respawned reliably.",
                    enemy);
                return false;
            }

            EnemyPersistentEntity persistent = enemy.GetComponent<EnemyPersistentEntity>();
            if (persistent == null)
                persistent = Undo.AddComponent<EnemyPersistentEntity>(enemy.gameObject);
            if (persistent == null)
                return false;

            Undo.RecordObject(persistent, undoName);
            SerializedObject serialized = new SerializedObject(persistent);
            SerializedProperty idProperty = serialized.FindProperty("persistentId");
            SerializedProperty prefabProperty = serialized.FindProperty("prefabSource");
            string persistentId = idProperty.stringValue;
            if (string.IsNullOrWhiteSpace(persistentId))
            {
                persistentId = "enemy_" + Guid.NewGuid().ToString("N");
                idProperty.stringValue = persistentId;
            }

            if (!ids.Add(persistentId))
            {
                Debug.LogError(
                    $"Duplicate enemy persistent ID '{persistentId}' found on '{enemy.name}'.",
                    enemy);
                return false;
            }

            prefabProperty.objectReferenceValue = prefabSource;
            serialized.ApplyModifiedProperties();
            EditorUtility.SetDirty(persistent);
            configuredCount++;
        }

        EditorUtility.SetDirty(registry);
        EditorSceneManager.MarkSceneDirty(scene);
        Debug.Log(
            $"Configured {configuredCount} persistent Enemy instance(s) with stable unique IDs.",
            registry);
        return true;
    }

    private static EnemyPersistenceRegistry FindOrCreateEnemyRegistry(Scene scene, string undoName)
    {
        EnemyPersistenceRegistry[] registries =
            UnityEngine.Object.FindObjectsByType<EnemyPersistenceRegistry>(FindObjectsInactive.Include);
        EnemyPersistenceRegistry result = null;
        for (int i = 0; i < registries.Length; i++)
        {
            if (registries[i].gameObject.scene != scene)
                continue;

            if (result != null)
            {
                Debug.LogError("Multiple EnemyPersistenceRegistry components exist in the active Scene.");
                return null;
            }

            result = registries[i];
        }

        if (result != null)
            return result;

        GameObject registryObject = new GameObject("Enemy Persistence Registry");
        SceneManager.MoveGameObjectToScene(registryObject, scene);
        Undo.RegisterCreatedObjectUndo(registryObject, undoName);
        return Undo.AddComponent<EnemyPersistenceRegistry>(registryObject);
    }

    private static GameObject ResolveEnemyPrefabAsset(GameObject sceneEnemy)
    {
        string prefabPath = PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(sceneEnemy);
        if (string.IsNullOrEmpty(prefabPath))
            return null;

        return AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
    }

    private static ItemPickup FindRuntimePickupPrefab()
    {
        string[] prefabGuids = AssetDatabase.FindAssets("t:Prefab", new[] { "Assets" });
        ItemPickup result = null;
        for (int i = 0; i < prefabGuids.Length; i++)
        {
            string prefabPath = AssetDatabase.GUIDToAssetPath(prefabGuids[i]);
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
            ItemPickup pickup = prefab != null ? prefab.GetComponent<ItemPickup>() : null;
            if (pickup == null)
                continue;

            if (result != null)
            {
                Debug.LogError(
                    $"Save setup found multiple ItemPickup prefabs: '{AssetDatabase.GetAssetPath(result)}' and '{prefabPath}'.");
                return null;
            }

            result = pickup;
        }

        if (result == null)
            Debug.LogError("Save setup could not find a prefab with ItemPickup under Assets.");

        return result;
    }

    private static bool EnsureEnemyPersistentMarkerOnPrefab(string prefabPath)
    {
        GameObject prefabAsset = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
        if (prefabAsset == null)
            return false;

        GameObject contents = PrefabUtility.LoadPrefabContents(prefabPath);
        try
        {
            Enemy enemy = contents.GetComponent<Enemy>();
            if (enemy == null)
            {
                Debug.LogError($"Enemy prefab '{prefabPath}' has no Enemy component.");
                return false;
            }

            EnemyPersistentEntity persistent = contents.GetComponent<EnemyPersistentEntity>();
            if (persistent == null)
                persistent = contents.AddComponent<EnemyPersistentEntity>();

            SerializedObject serialized = new SerializedObject(persistent);
            serialized.FindProperty("persistentId").stringValue = string.Empty;
            // Keep the prefab template empty. A self-reference stored inside the prefab is
            // remapped by Unity to the spawned Scene instance, which becomes null on death.
            // Scene instances receive an explicit prefab-asset override below instead.
            serialized.FindProperty("prefabSource").objectReferenceValue = null;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(persistent);

            if (PrefabUtility.SaveAsPrefabAsset(contents, prefabPath) == null)
            {
                Debug.LogError($"Failed to save Enemy persistence marker to '{prefabPath}'.");
                return false;
            }
        }
        finally
        {
            PrefabUtility.UnloadPrefabContents(contents);
        }

        AssetDatabase.ImportAsset(prefabPath, ImportAssetOptions.ForceUpdate);
        return true;
    }

    private static SaveManager FindRuntimeManager()
    {
        if (!Application.isPlaying)
        {
            Debug.LogWarning("Save/Load debug commands are available only in Play Mode.");
            return null;
        }

        SaveManager manager = UnityEngine.Object.FindAnyObjectByType<SaveManager>();
        if (manager == null)
            Debug.LogError("No active SaveManager was found. Run Setup Save System first.");
        return manager;
    }

    private static PlayerController FindActiveScenePlayer(Scene scene)
    {
        PlayerController[] players = UnityEngine.Object.FindObjectsByType<PlayerController>(
            FindObjectsInactive.Include);
        PlayerController result = null;
        int count = 0;
        for (int i = 0; i < players.Length; i++)
        {
            if (players[i].gameObject.scene != scene)
                continue;
            result = players[i];
            count++;
        }

        if (count == 1)
            return result;

        Debug.LogError($"Save setup requires exactly one PlayerController in the active Scene; found {count}.");
        return null;
    }

    private static List<ItemData> FindAllItems()
    {
        string[] guids = AssetDatabase.FindAssets("t:ScriptableObject", new[] { "Assets/Data" });
        List<ItemData> items = new List<ItemData>();
        for (int i = 0; i < guids.Length; i++)
        {
            string path = AssetDatabase.GUIDToAssetPath(guids[i]);
            ItemData item = AssetDatabase.LoadAssetAtPath<ItemData>(path);
            if (item != null)
                items.Add(item);
        }

        items.Sort((left, right) => string.Compare(left.ID, right.ID, StringComparison.Ordinal));
        return items;
    }

    private static bool ValidateItems(List<ItemData> items, out string error)
    {
        error = null;
        if (items == null || items.Count == 0)
        {
            error = "No ItemData assets were found under Assets/Data.";
            return false;
        }

        HashSet<string> ids = new HashSet<string>(StringComparer.Ordinal);
        for (int i = 0; i < items.Count; i++)
        {
            ItemData item = items[i];
            if (string.IsNullOrWhiteSpace(item.ID))
            {
                error = $"Item asset '{AssetDatabase.GetAssetPath(item)}' has an empty ID.";
                return false;
            }

            if (!ids.Add(item.ID))
            {
                error = $"Duplicate item ID '{item.ID}' was found.";
                return false;
            }
        }

        return true;
    }

    private static bool EnsureFolder(string parent, string childName)
    {
        string path = parent + "/" + childName;
        if (AssetDatabase.IsValidFolder(path))
            return true;

        if (!AssetDatabase.IsValidFolder(parent))
        {
            Debug.LogError($"Save setup failed: parent folder '{parent}' does not exist.");
            return false;
        }

        string guid = AssetDatabase.CreateFolder(parent, childName);
        if (!string.IsNullOrEmpty(guid))
            return true;

        Debug.LogError($"Save setup failed to create folder '{path}'.");
        return false;
    }
}
