using System;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class OrcLootSetup
{
    private const string MenuPath = "Tools/Android2D/Loot/Setup Orc Loot";
    private const string LootFolder = "Assets/Data/Loot";
    private const string LootTablePath = LootFolder + "/OrcLootTable.asset";
    private const string PickupFolder = "Assets/Items/Prefabs";
    private const string PickupPrefabPath = PickupFolder + "/ItemPickup.prefab";

    [MenuItem(MenuPath)]
    private static void Setup()
    {
        Scene scene = SceneManager.GetActiveScene();
        if (!scene.IsValid() || !scene.isLoaded)
        {
            Debug.LogError("Orc Loot setup failed: no valid loaded Scene is active.");
            return;
        }

        EnemyData orcData = FindExactAsset<EnemyData>("OrcData.asset");
        ConsumableData healthPotion = FindExactAsset<ConsumableData>("HealthPotion.asset");
        GameObject orcPrefab = FindExactAsset<GameObject>("Orc.prefab");
        if (orcData == null || healthPotion == null || orcPrefab == null)
        {
            Debug.LogError(
                "Orc Loot setup requires exactly one OrcData.asset, HealthPotion.asset, and Orc.prefab.");
            return;
        }

        if (!EnsureFolder("Assets/Data", "Loot") || !EnsureFolder("Assets/Items", "Prefabs"))
            return;

        const string undoName = "Setup Orc Loot";
        int undoGroup = Undo.GetCurrentGroup();
        Undo.SetCurrentGroupName(undoName);

        LootTable lootTable = FindOrCreateLootTable(healthPotion, undoName);
        ItemPickup pickupPrefab = FindOrCreatePickupPrefab();
        if (lootTable == null || pickupPrefab == null)
        {
            Undo.CollapseUndoOperations(undoGroup);
            return;
        }

        LootDropper sceneTemplate = FindSceneDropperTemplate(scene, orcData);
        if (!SynchronizeOrcPrefab(orcPrefab, pickupPrefab, sceneTemplate))
        {
            Undo.CollapseUndoOperations(undoGroup);
            return;
        }

        Undo.RecordObject(orcData, undoName);
        SerializedObject enemyDataSerialized = new SerializedObject(orcData);
        SerializedProperty lootTableProperty = enemyDataSerialized.FindProperty("lootTable");
        if (lootTableProperty == null)
        {
            Debug.LogError("Orc Loot setup failed: EnemyData.lootTable was not found.", orcData);
            Undo.CollapseUndoOperations(undoGroup);
            return;
        }

        lootTableProperty.objectReferenceValue = lootTable;
        enemyDataSerialized.ApplyModifiedProperties();
        EditorUtility.SetDirty(orcData);

        Enemy[] enemies = UnityEngine.Object.FindObjectsByType<Enemy>(FindObjectsInactive.Include);
        int configuredCount = 0;
        int removedOverrideCount = 0;
        for (int i = 0; i < enemies.Length; i++)
        {
            Enemy enemy = enemies[i];
            if (enemy.gameObject.scene != scene || enemy.Data != orcData)
                continue;

            LootDropper[] droppers = enemy.GetComponents<LootDropper>();
            LootDropper inheritedDropper = null;
            for (int j = 0; j < droppers.Length; j++)
            {
                if (PrefabUtility.GetCorrespondingObjectFromSource(droppers[j]) != null
                    && !PrefabUtility.IsAddedComponentOverride(droppers[j]))
                {
                    inheritedDropper = droppers[j];
                    break;
                }
            }

            if (inheritedDropper != null)
            {
                for (int j = droppers.Length - 1; j >= 0; j--)
                {
                    if (droppers[j] != inheritedDropper
                        && PrefabUtility.IsAddedComponentOverride(droppers[j]))
                    {
                        Undo.DestroyObjectImmediate(droppers[j]);
                        removedOverrideCount++;
                    }
                }
            }
            else
            {
                LootDropper dropper = droppers.Length > 0
                    ? droppers[0]
                    : Undo.AddComponent<LootDropper>(enemy.gameObject);
                if (dropper == null)
                {
                    Debug.LogError($"Could not add LootDropper to '{enemy.name}'.", enemy);
                    continue;
                }

                Undo.RecordObject(dropper, undoName);
                SerializedObject dropperSerialized = new SerializedObject(dropper);
                dropperSerialized.FindProperty("itemPickupPrefab").objectReferenceValue = pickupPrefab;
                dropperSerialized.ApplyModifiedProperties();
                EditorUtility.SetDirty(dropper);
            }

            configuredCount++;
        }

        if (configuredCount > 0)
            EditorSceneManager.MarkSceneDirty(scene);
        else
            Debug.LogWarning("Orc Loot setup found no active-Scene Enemy using OrcData.asset.", orcData);

        Undo.CollapseUndoOperations(undoGroup);
        Selection.activeObject = lootTable;
        Debug.Log(
            $"Orc Loot setup complete. Orc.prefab now owns LootDropper; checked {configuredCount} Scene Orc object(s) " +
            $"and removed {removedOverrideCount} obsolete LootDropper override(s). " +
            "The test table drops Health Potion x1 at 100% until you rebalance it. " +
            "Review changes, then press Ctrl+S to save the Scene.",
            lootTable);
    }

    private static LootDropper FindSceneDropperTemplate(Scene scene, EnemyData orcData)
    {
        Enemy[] enemies = UnityEngine.Object.FindObjectsByType<Enemy>(FindObjectsInactive.Include);
        for (int i = 0; i < enemies.Length; i++)
        {
            if (enemies[i].gameObject.scene == scene && enemies[i].Data == orcData)
            {
                LootDropper dropper = enemies[i].GetComponent<LootDropper>();
                if (dropper != null)
                    return dropper;
            }
        }

        return null;
    }

    private static bool SynchronizeOrcPrefab(
        GameObject orcPrefab,
        ItemPickup pickupPrefab,
        LootDropper sceneTemplate)
    {
        string prefabPath = AssetDatabase.GetAssetPath(orcPrefab);
        GameObject contents = PrefabUtility.LoadPrefabContents(prefabPath);
        try
        {
            LootDropper[] existing = contents.GetComponents<LootDropper>();
            LootDropper prefabDropper = existing.Length > 0
                ? existing[0]
                : contents.AddComponent<LootDropper>();

            for (int i = existing.Length - 1; i >= 1; i--)
                UnityEngine.Object.DestroyImmediate(existing[i]);

            if (sceneTemplate != null)
                EditorUtility.CopySerialized(sceneTemplate, prefabDropper);

            SerializedObject serialized = new SerializedObject(prefabDropper);
            SerializedProperty pickupProperty = serialized.FindProperty("itemPickupPrefab");
            if (pickupProperty == null)
            {
                Debug.LogError("Orc Loot setup failed: LootDropper.itemPickupPrefab was not found.");
                return false;
            }

            pickupProperty.objectReferenceValue = pickupPrefab;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(prefabDropper);

            GameObject saved = PrefabUtility.SaveAsPrefabAsset(contents, prefabPath);
            if (saved == null)
            {
                Debug.LogError($"Orc Loot setup failed to save prefab '{prefabPath}'.");
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

    private static LootTable FindOrCreateLootTable(ConsumableData healthPotion, string undoName)
    {
        LootTable table = AssetDatabase.LoadAssetAtPath<LootTable>(LootTablePath);
        if (table == null)
        {
            table = ScriptableObject.CreateInstance<LootTable>();
            AssetDatabase.CreateAsset(table, LootTablePath);
            Undo.RegisterCreatedObjectUndo(table, undoName);
        }

        if (!table.HasValidEntries())
        {
            Undo.RecordObject(table, undoName);
            SerializedObject serialized = new SerializedObject(table);
            SerializedProperty entries = serialized.FindProperty("entries");
            entries.arraySize = 1;
            SerializedProperty entry = entries.GetArrayElementAtIndex(0);
            entry.FindPropertyRelative("item").objectReferenceValue = healthPotion;
            entry.FindPropertyRelative("dropChance").floatValue = 1f;
            entry.FindPropertyRelative("minQuantity").intValue = 1;
            entry.FindPropertyRelative("maxQuantity").intValue = 1;
            serialized.ApplyModifiedProperties();
            EditorUtility.SetDirty(table);
        }

        return table;
    }

    private static ItemPickup FindOrCreatePickupPrefab()
    {
        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PickupPrefabPath);
        if (prefab == null)
        {
            GameObject temporary = new GameObject("ItemPickup");
            try
            {
                CircleCollider2D collider = temporary.AddComponent<CircleCollider2D>();
                collider.isTrigger = true;
                temporary.AddComponent<SpriteRenderer>();
                temporary.AddComponent<ItemPickup>();
                prefab = PrefabUtility.SaveAsPrefabAsset(temporary, PickupPrefabPath);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(temporary);
            }
        }
        else
        {
            GameObject contents = PrefabUtility.LoadPrefabContents(PickupPrefabPath);
            bool changed = false;
            try
            {
                CircleCollider2D collider = contents.GetComponent<CircleCollider2D>();
                if (collider == null)
                {
                    collider = contents.AddComponent<CircleCollider2D>();
                    changed = true;
                }

                if (!collider.isTrigger)
                {
                    collider.isTrigger = true;
                    changed = true;
                }

                if (contents.GetComponent<SpriteRenderer>() == null)
                {
                    contents.AddComponent<SpriteRenderer>();
                    changed = true;
                }

                if (contents.GetComponent<ItemPickup>() == null)
                {
                    contents.AddComponent<ItemPickup>();
                    changed = true;
                }

                if (changed)
                    PrefabUtility.SaveAsPrefabAsset(contents, PickupPrefabPath);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(contents);
            }

            prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PickupPrefabPath);
        }

        ItemPickup pickup = prefab != null ? prefab.GetComponent<ItemPickup>() : null;
        if (pickup == null)
            Debug.LogError("Orc Loot setup failed to create a valid generic ItemPickup prefab.");

        return pickup;
    }

    private static T FindExactAsset<T>(string fileName) where T : UnityEngine.Object
    {
        string[] guids = AssetDatabase.FindAssets($"t:{typeof(T).Name}");
        T result = null;
        for (int i = 0; i < guids.Length; i++)
        {
            string path = AssetDatabase.GUIDToAssetPath(guids[i]);
            if (!string.Equals(Path.GetFileName(path), fileName, StringComparison.OrdinalIgnoreCase))
                continue;

            if (result != null)
            {
                Debug.LogError($"Multiple assets named '{fileName}' were found.");
                return null;
            }

            result = AssetDatabase.LoadAssetAtPath<T>(path);
        }

        return result;
    }

    private static bool EnsureFolder(string parent, string childName)
    {
        string path = parent + "/" + childName;
        if (AssetDatabase.IsValidFolder(path))
            return true;

        if (!AssetDatabase.IsValidFolder(parent))
        {
            Debug.LogError($"Orc Loot setup failed: parent folder '{parent}' does not exist.");
            return false;
        }

        string guid = AssetDatabase.CreateFolder(parent, childName);
        if (!string.IsNullOrEmpty(guid))
            return true;

        Debug.LogError($"Orc Loot setup failed to create folder '{path}'.");
        return false;
    }
}
