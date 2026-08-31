using System;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class HealthPotionSetup
{
    private const string MenuPath = "Tools/Android2D/Items/Setup Health Potion";
    private const string AssetFolder = "Assets/Data/Consumables";
    private const string AssetPath = AssetFolder + "/HealthPotion.asset";
    private const string PickupName = "HealthPotionPickup";

    [MenuItem(MenuPath)]
    private static void Setup()
    {
        Scene scene = SceneManager.GetActiveScene();
        if (!scene.IsValid() || !scene.isLoaded)
        {
            Debug.LogError("Health Potion setup failed: no valid loaded Scene is active.");
            return;
        }

        PlayerController[] allPlayers = UnityEngine.Object.FindObjectsByType<PlayerController>(
            FindObjectsInactive.Include);
        PlayerController player = null;
        int playerCount = 0;
        for (int i = 0; i < allPlayers.Length; i++)
        {
            if (allPlayers[i].gameObject.scene != scene)
                continue;

            player = allPlayers[i];
            playerCount++;
        }

        if (playerCount != 1)
        {
            Debug.LogError(
                $"Health Potion setup requires exactly one PlayerController in the active Scene; found {playerCount}.");
            return;
        }

        const string undoName = "Setup Health Potion";
        int undoGroup = Undo.GetCurrentGroup();
        Undo.SetCurrentGroupName(undoName);

        ConsumableData potion = FindOrCreatePotion(undoName);
        if (potion == null)
        {
            Undo.CollapseUndoOperations(undoGroup);
            return;
        }

        GameObject pickupObject = FindSceneObject(scene, PickupName);
        bool createdPickup = pickupObject == null;
        if (createdPickup)
        {
            pickupObject = new GameObject(PickupName);
            SceneManager.MoveGameObjectToScene(pickupObject, scene);
            Undo.RegisterCreatedObjectUndo(pickupObject, undoName);
            pickupObject.transform.position = player.transform.position + Vector3.right * 2f;
        }

        CircleCollider2D collider = GetOrAddComponent<CircleCollider2D>(pickupObject);
        SpriteRenderer renderer = GetOrAddComponent<SpriteRenderer>(pickupObject);
        ItemPickup pickup = GetOrAddComponent<ItemPickup>(pickupObject);

        if (collider == null || renderer == null || pickup == null)
        {
            Debug.LogError(
                "Health Potion setup failed: required pickup components could not be created.",
                pickupObject);
            Undo.CollapseUndoOperations(undoGroup);
            return;
        }

        Undo.RecordObject(pickup, undoName);
        SerializedObject pickupSerialized = new SerializedObject(pickup);
        pickupSerialized.FindProperty("item").objectReferenceValue = potion;
        pickupSerialized.FindProperty("quantity").intValue = 3;
        pickupSerialized.ApplyModifiedProperties();

        Undo.RecordObject(collider, undoName);
        collider.isTrigger = true;
        EditorUtility.SetDirty(collider);

        if (potion.Icon != null)
        {
            Undo.RecordObject(renderer, undoName);
            renderer.sprite = potion.Icon;
            EditorUtility.SetDirty(renderer);
        }
        else
        {
            Debug.LogWarning(
                "HealthPotion.asset has no Icon. Assign a potion sprite manually; the setup tool did not guess one.",
                potion);
        }

        EditorUtility.SetDirty(pickup);
        EditorUtility.SetDirty(pickupObject);
        EditorSceneManager.MarkSceneDirty(scene);
        Undo.CollapseUndoOperations(undoGroup);
        Selection.activeGameObject = pickupObject;

        Debug.Log(
            $"Health Potion setup complete. '{PickupName}' uses HealthPotion.asset with quantity 3. " +
            "Review the pickup and asset, then press Ctrl+S to save the Scene.",
            pickupObject);
    }

    private static ConsumableData FindOrCreatePotion(string undoName)
    {
        if (!EnsureAssetFolder())
            return null;

        ConsumableData potion = null;
        string potionPath = null;
        string[] guids = AssetDatabase.FindAssets("HealthPotion t:ConsumableData");
        for (int i = 0; i < guids.Length; i++)
        {
            string path = AssetDatabase.GUIDToAssetPath(guids[i]);
            if (!string.Equals(Path.GetFileName(path), "HealthPotion.asset", StringComparison.OrdinalIgnoreCase))
                continue;

            if (potion != null)
            {
                Debug.LogError("Health Potion setup failed: multiple HealthPotion.asset files were found.");
                return null;
            }

            potion = AssetDatabase.LoadAssetAtPath<ConsumableData>(path);
            potionPath = path;
        }

        if (potion == null)
        {
            potion = ScriptableObject.CreateInstance<ConsumableData>();
            AssetDatabase.CreateAsset(potion, AssetPath);
            Undo.RegisterCreatedObjectUndo(potion, undoName);
        }
        else if (!string.Equals(potionPath, AssetPath, StringComparison.OrdinalIgnoreCase))
        {
            string moveError = AssetDatabase.MoveAsset(potionPath, AssetPath);
            if (!string.IsNullOrEmpty(moveError))
            {
                Debug.LogError(
                    $"Health Potion setup failed to move the asset to '{AssetPath}': {moveError}",
                    potion);
                return null;
            }

            Debug.Log(
                $"Moved HealthPotion.asset from '{potionPath}' to '{AssetPath}' while preserving its GUID.",
                potion);
        }

        Undo.RecordObject(potion, undoName);
        SerializedObject serialized = new SerializedObject(potion);
        serialized.FindProperty("id").stringValue = "health_potion";
        serialized.FindProperty("displayName").stringValue = "Health Potion";
        serialized.FindProperty("description").stringValue = "Restores health when used.";
        serialized.FindProperty("itemType").enumValueIndex = (int)ItemType.Consumable;
        serialized.FindProperty("maxStack").intValue = 10;
        serialized.FindProperty("healAmount").intValue = 25;
        serialized.ApplyModifiedProperties();
        EditorUtility.SetDirty(potion);
        return potion;
    }

    private static bool EnsureAssetFolder()
    {
        if (AssetDatabase.IsValidFolder(AssetFolder))
            return true;

        if (!AssetDatabase.IsValidFolder("Assets/Data"))
        {
            Debug.LogError("Health Potion setup failed: the required 'Assets/Data' folder does not exist.");
            return false;
        }

        string folderGuid = AssetDatabase.CreateFolder("Assets/Data", "Consumables");
        if (string.IsNullOrEmpty(folderGuid))
        {
            Debug.LogError($"Health Potion setup failed: could not create '{AssetFolder}'.");
            return false;
        }

        return true;
    }

    private static T GetOrAddComponent<T>(GameObject target) where T : Component
    {
        T component = target.GetComponent<T>();
        return component != null ? component : Undo.AddComponent<T>(target);
    }

    private static GameObject FindSceneObject(Scene scene, string objectName)
    {
        GameObject[] roots = scene.GetRootGameObjects();
        for (int i = 0; i < roots.Length; i++)
        {
            Transform[] transforms = roots[i].GetComponentsInChildren<Transform>(true);
            for (int j = 0; j < transforms.Length; j++)
            {
                if (transforms[j].name == objectName)
                    return transforms[j].gameObject;
            }
        }

        return null;
    }
}
