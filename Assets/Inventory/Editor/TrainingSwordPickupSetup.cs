using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class TrainingSwordPickupSetup
{
    private const string MenuPath = "Tools/Android2D/Inventory/Setup Training Sword Pickup";
    private const string PickupName = "TrainingSwordPickup";

    [MenuItem(MenuPath)]
    private static void SetupTrainingSwordPickup()
    {
        Scene scene = SceneManager.GetActiveScene();
        if (!scene.IsValid() || !scene.isLoaded)
        {
            Debug.LogError("Training Sword Pickup setup failed: there is no valid open Scene.");
            return;
        }

        PlayerController player = FindPlayerInScene(scene);
        if (player == null)
        {
            return;
        }

        WeaponData trainingSword = FindTrainingSwordAsset();
        if (trainingSword == null)
        {
            return;
        }

        GameObject pickupObject = FindUniquePickupObject(scene);
        if (pickupObject == null && HasDuplicatePickupObjects(scene))
        {
            return;
        }

        const string undoName = "Setup Training Sword Pickup";
        Undo.IncrementCurrentGroup();
        int undoGroup = Undo.GetCurrentGroup();
        Undo.SetCurrentGroupName(undoName);

        Inventory inventory = player.GetComponent<Inventory>();
        if (inventory == null)
        {
            inventory = Undo.AddComponent<Inventory>(player.gameObject);
        }

        if (pickupObject == null)
        {
            pickupObject = new GameObject(PickupName);
            Undo.RegisterCreatedObjectUndo(pickupObject, undoName);
        }

        CircleCollider2D trigger = GetOrAddComponent<CircleCollider2D>(pickupObject);
        ItemPickup itemPickup = GetOrAddComponent<ItemPickup>(pickupObject);
        SpriteRenderer spriteRenderer = GetOrAddComponent<SpriteRenderer>(pickupObject);

        Undo.RecordObject(pickupObject.transform, undoName);
        pickupObject.transform.position = player.transform.position + new Vector3(1.5f, 0.25f, 0f);

        Undo.RecordObject(trigger, undoName);
        trigger.isTrigger = true;

        Undo.RecordObject(itemPickup, undoName);
        SerializedObject pickupSerializedObject = new SerializedObject(itemPickup);
        pickupSerializedObject.Update();

        SerializedProperty itemProperty = pickupSerializedObject.FindProperty("item");
        SerializedProperty quantityProperty = pickupSerializedObject.FindProperty("quantity");
        if (itemProperty == null || quantityProperty == null)
        {
            Debug.LogError("Training Sword Pickup setup failed: ItemPickup serialized fields were not found.", itemPickup);
            Undo.CollapseUndoOperations(undoGroup);
            return;
        }

        itemProperty.objectReferenceValue = trainingSword;
        quantityProperty.intValue = 1;
        pickupSerializedObject.ApplyModifiedProperties();

        Sprite pickupSprite = spriteRenderer.sprite;
        if (pickupSprite == null)
        {
            Debug.LogError(
                "Training Sword Pickup setup could not assign the TrainingSword icon because " +
                "TrainingSwordPickup's SpriteRenderer has no sprite. No replacement sprite was selected.",
                spriteRenderer);
        }
        else if (trainingSword.Icon == null)
        {
            Undo.RecordObject(trainingSword, undoName);

            SerializedObject trainingSwordSerializedObject = new SerializedObject(trainingSword);
            trainingSwordSerializedObject.Update();
            SerializedProperty iconProperty = trainingSwordSerializedObject.FindProperty("icon");

            if (iconProperty == null)
            {
                Debug.LogError(
                    "Training Sword Pickup setup failed to find the serialized 'icon' field on TrainingSword.asset.",
                    trainingSword);
            }
            else
            {
                iconProperty.objectReferenceValue = pickupSprite;
                trainingSwordSerializedObject.ApplyModifiedProperties();
                EditorUtility.SetDirty(trainingSword);

                Debug.Log(
                    $"Assigned sprite '{pickupSprite.name}' from TrainingSwordPickup to TrainingSword.asset Icon. " +
                    "The asset was marked dirty but was not saved automatically.",
                    trainingSword);
            }
        }
        else if (trainingSword.Icon != pickupSprite)
        {
            Debug.LogWarning(
                $"TrainingSword.asset already has icon '{trainingSword.Icon.name}', which differs from " +
                $"TrainingSwordPickup's sprite '{pickupSprite.name}'. The existing icon was left unchanged.",
                trainingSword);
        }

        EditorUtility.SetDirty(player.gameObject);
        EditorUtility.SetDirty(inventory);
        EditorUtility.SetDirty(pickupObject);
        EditorUtility.SetDirty(pickupObject.transform);
        EditorUtility.SetDirty(trigger);
        EditorUtility.SetDirty(itemPickup);
        EditorUtility.SetDirty(spriteRenderer);
        EditorSceneManager.MarkSceneDirty(scene);

        Selection.activeGameObject = pickupObject;
        EditorGUIUtility.PingObject(pickupObject);
        Undo.CollapseUndoOperations(undoGroup);

        Debug.Log(
            $"Training Sword Pickup setup complete in Scene '{scene.name}'. " +
            $"Player '{player.name}' has Inventory, and '{PickupName}' has ItemPickup, SpriteRenderer, " +
            "and a trigger CircleCollider2D. Review the Scene, then press Ctrl+S to save it.",
            pickupObject);
    }

    private static PlayerController FindPlayerInScene(Scene scene)
    {
        PlayerController selectedPlayer = Selection.activeGameObject != null
            ? Selection.activeGameObject.GetComponentInParent<PlayerController>()
            : null;

        if (selectedPlayer != null && selectedPlayer.gameObject.scene == scene)
        {
            return selectedPlayer;
        }

        PlayerController[] players = UnityEngine.Object
            .FindObjectsByType<PlayerController>(FindObjectsInactive.Include)
            .Where(candidate => candidate.gameObject.scene == scene)
            .ToArray();

        if (players.Length == 1)
        {
            return players[0];
        }

        if (players.Length == 0)
        {
            Debug.LogError("Training Sword Pickup setup failed: no PlayerController was found in the active Scene.");
        }
        else
        {
            Debug.LogError(
                "Training Sword Pickup setup failed: multiple PlayerController components were found. " +
                "Select the intended Player in the Hierarchy and run the menu again.");
        }

        return null;
    }

    private static WeaponData FindTrainingSwordAsset()
    {
        List<WeaponData> matches = new List<WeaponData>();
        string[] guids = AssetDatabase.FindAssets("TrainingSword t:WeaponData");

        foreach (string guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            if (!string.Equals(
                    Path.GetFileName(path),
                    "TrainingSword.asset",
                    StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            WeaponData asset = AssetDatabase.LoadAssetAtPath<WeaponData>(path);
            if (asset != null)
            {
                matches.Add(asset);
            }
        }

        if (matches.Count == 1)
        {
            return matches[0];
        }

        if (matches.Count == 0)
        {
            Debug.LogError(
                "Training Sword Pickup setup failed: AssetDatabase could not find TrainingSword.asset as WeaponData.");
        }
        else
        {
            Debug.LogError(
                "Training Sword Pickup setup failed: multiple TrainingSword.asset WeaponData assets were found. " +
                "Keep one uniquely named asset and run the menu again.");
        }

        return null;
    }

    private static GameObject FindUniquePickupObject(Scene scene)
    {
        GameObject[] matches = FindPickupObjects(scene);
        return matches.Length == 1 ? matches[0] : null;
    }

    private static bool HasDuplicatePickupObjects(Scene scene)
    {
        GameObject[] matches = FindPickupObjects(scene);
        if (matches.Length <= 1)
        {
            return false;
        }

        Debug.LogError(
            $"Training Sword Pickup setup failed: {matches.Length} objects named '{PickupName}' exist in the active Scene. " +
            "Rename or remove duplicates manually, then run the menu again.");
        return true;
    }

    private static GameObject[] FindPickupObjects(Scene scene)
    {
        return UnityEngine.Object
            .FindObjectsByType<Transform>(FindObjectsInactive.Include)
            .Where(candidate => candidate.gameObject.scene == scene && candidate.name == PickupName)
            .Select(candidate => candidate.gameObject)
            .ToArray();
    }

    private static T GetOrAddComponent<T>(GameObject target) where T : Component
    {
        T component = target.GetComponent<T>();
        return component != null ? component : Undo.AddComponent<T>(target);
    }
}
