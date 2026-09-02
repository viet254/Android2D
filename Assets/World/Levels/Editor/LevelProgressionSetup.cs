using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public static class LevelProgressionSetup
{
    private const string DataFolder = "Assets/Data/Levels";
    private const string SampleScenePath = "Assets/Scenes/SampleScene.unity";
    private const string Level02ScenePath = "Assets/Scenes/Level02.unity";
    private const string SampleLevelPath = DataFolder + "/SampleScene.asset";
    private const string Level02DataPath = DataFolder + "/Level02.asset";

    [MenuItem("Tools/Android2D/Levels/Setup Level Progression")]
    private static void SetupLevelProgression()
    {
        if (Application.isPlaying)
        {
            Debug.LogError("Level Progression setup must run in Edit Mode.");
            return;
        }

        Scene scene = SceneManager.GetActiveScene();
        if (!scene.IsValid() || !scene.isLoaded || string.IsNullOrEmpty(scene.path))
        {
            Debug.LogError("Level Progression setup requires a saved active Scene.");
            return;
        }

        if (!EnsureFolder("Assets/Data", "Levels"))
            return;

        const string undoName = "Setup Level Progression";
        LevelData sampleLevel = LoadOrCreateLevel(
            SampleLevelPath,
            "sample_scene",
            "SampleScene",
            "Sample Level",
            undoName);
        LevelData level02 = LoadOrCreateLevel(
            Level02DataPath,
            "level_02",
            "Level02",
            "Level 02",
            undoName);
        if (sampleLevel == null || level02 == null)
            return;

        ConfigureNextLevel(sampleLevel, level02, undoName);
        ConfigureNextLevel(level02, null, undoName);

        SceneLoader loader = EnsureSingleSceneComponent<SceneLoader>(
            scene,
            "Scene Loader",
            undoName);
        LevelManager manager = EnsureSingleSceneComponent<LevelManager>(
            scene,
            "Level Manager",
            undoName);
        if (loader == null || manager == null)
            return;

        LevelData currentLevel = ResolveCurrentLevel(scene.name, sampleLevel, level02);
        ConfigureLevelManager(manager, loader, currentLevel, sampleLevel, undoName);

        if (currentLevel != null)
        {
            PlayerController player = FindSingleSceneComponent<PlayerController>(scene);
            if (player == null || !ValidatePlayer(player))
                return;

            PlayerSpawnPoint spawnPoint = EnsureSpawnPoint(scene, player, undoName);
            LevelExit levelExit = EnsureLevelExit(scene, player, manager, undoName);
            if (spawnPoint == null || levelExit == null)
                return;
        }

        bool createdLevel02 = false;
        if (AssetDatabase.LoadAssetAtPath<SceneAsset>(Level02ScenePath) == null)
        {
            if (!string.Equals(scene.path, SampleScenePath, StringComparison.OrdinalIgnoreCase))
            {
                Debug.LogWarning(
                    $"Level02 does not exist. Open '{SampleScenePath}' and run this setup to create its test copy.");
            }
            else
            {
                createdLevel02 = EditorSceneManager.SaveScene(scene, Level02ScenePath, true);
                if (!createdLevel02)
                {
                    Debug.LogError($"Failed to create test Scene '{Level02ScenePath}'.");
                    return;
                }

                AssetDatabase.ImportAsset(Level02ScenePath, ImportAssetOptions.ForceUpdate);
            }
        }

        EnsureBuildScene(SampleScenePath);
        if (AssetDatabase.LoadAssetAtPath<SceneAsset>(Level02ScenePath) != null)
            EnsureBuildScene(Level02ScenePath);

        EditorUtility.SetDirty(loader);
        EditorUtility.SetDirty(manager);
        EditorSceneManager.MarkSceneDirty(scene);
        AssetDatabase.SaveAssets();

        string copyMessage = createdLevel02
            ? $" Created '{Level02ScenePath}' as a test copy; open it and run this menu once to assign Level02 data."
            : string.Empty;
        Debug.Log(
            $"Level Progression setup complete for Scene '{scene.name}'. " +
            "Review Spawn Point and Level Exit positions, then press Ctrl+S." + copyMessage,
            manager);
    }

    private static LevelData ResolveCurrentLevel(
        string sceneName,
        LevelData sampleLevel,
        LevelData level02)
    {
        if (string.Equals(sceneName, sampleLevel.SceneName, StringComparison.Ordinal))
            return sampleLevel;
        if (string.Equals(sceneName, level02.SceneName, StringComparison.Ordinal))
            return level02;
        return null;
    }

    private static void ConfigureLevelManager(
        LevelManager manager,
        SceneLoader loader,
        LevelData currentLevel,
        LevelData startingLevel,
        string undoName)
    {
        Undo.RecordObject(manager, undoName);
        SerializedObject serialized = new SerializedObject(manager);
        serialized.FindProperty("currentLevel").objectReferenceValue = currentLevel;
        serialized.FindProperty("startingLevel").objectReferenceValue = startingLevel;
        serialized.FindProperty("sceneLoader").objectReferenceValue = loader;
        serialized.ApplyModifiedProperties();
        EditorUtility.SetDirty(manager);
    }

    private static PlayerSpawnPoint EnsureSpawnPoint(
        Scene scene,
        PlayerController player,
        string undoName)
    {
        PlayerSpawnPoint[] points = FindSceneComponents<PlayerSpawnPoint>(scene);
        PlayerSpawnPoint result = null;
        for (int i = 0; i < points.Length; i++)
        {
            if (!string.Equals(points[i].SpawnId, "default", StringComparison.Ordinal))
                continue;

            if (result != null)
            {
                Debug.LogError("Multiple default PlayerSpawnPoint components exist in the active Scene.");
                return null;
            }

            result = points[i];
        }

        if (result != null)
            return result;

        GameObject spawnObject = new GameObject("Default Spawn Point");
        SceneManager.MoveGameObjectToScene(spawnObject, scene);
        Undo.RegisterCreatedObjectUndo(spawnObject, undoName);
        spawnObject.transform.position = player.transform.position;
        result = Undo.AddComponent<PlayerSpawnPoint>(spawnObject);
        EditorUtility.SetDirty(result);
        return result;
    }

    private static LevelExit EnsureLevelExit(
        Scene scene,
        PlayerController player,
        LevelManager manager,
        string undoName)
    {
        LevelExit[] exits = FindSceneComponents<LevelExit>(scene);
        if (exits.Length > 1)
        {
            Debug.LogError("Multiple LevelExit components exist in the active Scene.");
            return null;
        }

        LevelExit result;
        if (exits.Length == 1)
        {
            result = exits[0];
        }
        else
        {
            GameObject exitObject = new GameObject("Level Exit");
            SceneManager.MoveGameObjectToScene(exitObject, scene);
            Undo.RegisterCreatedObjectUndo(exitObject, undoName);
            exitObject.transform.position = player.transform.position + Vector3.right * 4f;
            BoxCollider2D collider = Undo.AddComponent<BoxCollider2D>(exitObject);
            collider.isTrigger = true;
            collider.size = new Vector2(1f, 2f);
            result = Undo.AddComponent<LevelExit>(exitObject);
        }

        Collider2D trigger = result.GetComponent<Collider2D>();
        if (trigger == null)
            trigger = Undo.AddComponent<BoxCollider2D>(result.gameObject);
        Undo.RecordObject(trigger, undoName);
        trigger.isTrigger = true;

        GameObject interactionPrompt = EnsureInteractionButton(scene, result, undoName);
        if (interactionPrompt == null)
            return null;

        Undo.RecordObject(result, undoName);
        SerializedObject serialized = new SerializedObject(result);
        serialized.FindProperty("levelManager").objectReferenceValue = manager;
        serialized.FindProperty("interactionPrompt").objectReferenceValue = interactionPrompt;
        SerializedProperty animatorProperty = serialized.FindProperty("portalAnimator");
        if (animatorProperty.objectReferenceValue == null)
            animatorProperty.objectReferenceValue = result.GetComponent<Animator>();
        SerializedProperty stateProperty = serialized.FindProperty("activationStateName");
        if (string.IsNullOrWhiteSpace(stateProperty.stringValue))
            stateProperty.stringValue = "Teleport";
        serialized.ApplyModifiedProperties();
        EditorUtility.SetDirty(result);
        EditorUtility.SetDirty(trigger);
        return result;
    }

    private static GameObject EnsureInteractionButton(
        Scene scene,
        LevelExit levelExit,
        string undoName)
    {
        Canvas[] canvases = FindSceneComponents<Canvas>(scene);
        if (canvases.Length != 1)
        {
            Debug.LogError(
                $"Level Exit interaction UI requires exactly one Canvas in Scene '{scene.name}'; found {canvases.Length}.");
            return null;
        }

        Canvas canvas = canvases[0];
        GameObject buttonObject = null;
        Transform[] descendants = canvas.GetComponentsInChildren<Transform>(true);
        for (int i = 0; i < descendants.Length; i++)
        {
            if (!string.Equals(descendants[i].name, "Interact Button", StringComparison.Ordinal))
                continue;

            if (buttonObject != null)
            {
                Debug.LogError("Multiple 'Interact Button' objects exist under the active Canvas.");
                return null;
            }

            buttonObject = descendants[i].gameObject;
        }

        if (buttonObject == null)
        {
            buttonObject = new GameObject(
                "Interact Button",
                typeof(RectTransform),
                typeof(CanvasRenderer),
                typeof(Image),
                typeof(Button));
            Undo.RegisterCreatedObjectUndo(buttonObject, undoName);
            Undo.SetTransformParent(buttonObject.transform, canvas.transform, undoName);
        }

        RectTransform rect = buttonObject.GetComponent<RectTransform>();
        Image image = buttonObject.GetComponent<Image>();
        Button button = buttonObject.GetComponent<Button>();
        if (rect == null || image == null || button == null)
        {
            Debug.LogError("'Interact Button' must have RectTransform, Image and Button components.", buttonObject);
            return null;
        }

        Undo.RecordObject(rect, undoName);
        rect.anchorMin = new Vector2(1f, 0f);
        rect.anchorMax = new Vector2(1f, 0f);
        rect.pivot = new Vector2(1f, 0f);
        rect.anchoredPosition = new Vector2(-36f, 36f);
        rect.sizeDelta = new Vector2(150f, 64f);
        rect.localScale = Vector3.one;

        Undo.RecordObject(image, undoName);
        image.color = new Color(0.16f, 0.08f, 0.28f, 0.92f);
        image.raycastTarget = true;

        Undo.RecordObject(button, undoName);
        ColorBlock colors = button.colors;
        colors.normalColor = Color.white;
        colors.highlightedColor = new Color(0.82f, 0.72f, 1f, 1f);
        colors.pressedColor = new Color(0.6f, 0.42f, 0.9f, 1f);
        colors.selectedColor = colors.highlightedColor;
        button.colors = colors;

        Text label = buttonObject.GetComponentInChildren<Text>(true);
        if (label == null)
        {
            GameObject labelObject = new GameObject(
                "Label",
                typeof(RectTransform),
                typeof(CanvasRenderer),
                typeof(Text));
            Undo.RegisterCreatedObjectUndo(labelObject, undoName);
            Undo.SetTransformParent(labelObject.transform, buttonObject.transform, undoName);
            label = labelObject.GetComponent<Text>();
        }

        RectTransform labelRect = label.GetComponent<RectTransform>();
        Undo.RecordObject(labelRect, undoName);
        labelRect.anchorMin = Vector2.zero;
        labelRect.anchorMax = Vector2.one;
        labelRect.offsetMin = Vector2.zero;
        labelRect.offsetMax = Vector2.zero;
        labelRect.localScale = Vector3.one;

        Undo.RecordObject(label, undoName);
        label.text = "TƯƠNG TÁC\n[E]";
        label.alignment = TextAnchor.MiddleCenter;
        label.fontSize = 18;
        label.fontStyle = FontStyle.Bold;
        label.color = Color.white;
        label.raycastTarget = false;
        if (label.font == null)
            label.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

        bool listenerExists = false;
        for (int i = 0; i < button.onClick.GetPersistentEventCount(); i++)
        {
            if (button.onClick.GetPersistentTarget(i) == levelExit
                && string.Equals(
                    button.onClick.GetPersistentMethodName(i),
                    nameof(LevelExit.Interact),
                    StringComparison.Ordinal))
            {
                listenerExists = true;
                break;
            }
        }

        if (!listenerExists)
            UnityEventTools.AddPersistentListener(button.onClick, levelExit.Interact);

        Undo.RecordObject(buttonObject, undoName);
        buttonObject.SetActive(false);
        EditorUtility.SetDirty(buttonObject);
        EditorUtility.SetDirty(button);
        EditorUtility.SetDirty(label);
        return buttonObject;
    }

    private static bool ValidatePlayer(PlayerController player)
    {
        Type[] requiredTypes =
        {
            typeof(Rigidbody2D),
            typeof(Health),
            typeof(PlayerStats),
            typeof(PlayerExperience),
            typeof(Inventory),
            typeof(Equipment),
            typeof(SaveManager)
        };

        for (int i = 0; i < requiredTypes.Length; i++)
        {
            if (player.GetComponent(requiredTypes[i]) != null)
                continue;

            Debug.LogError(
                $"Player '{player.name}' is missing required component '{requiredTypes[i].Name}'. " +
                "Run the appropriate existing setup before Level Progression setup.",
                player);
            return false;
        }

        return true;
    }

    private static T EnsureSingleSceneComponent<T>(
        Scene scene,
        string objectName,
        string undoName) where T : Component
    {
        T existing = FindSingleSceneComponent<T>(scene, false);
        if (existing != null)
            return existing;

        T[] all = FindSceneComponents<T>(scene);
        if (all.Length > 1)
        {
            Debug.LogError($"Multiple {typeof(T).Name} components exist in Scene '{scene.name}'.");
            return null;
        }

        GameObject target = new GameObject(objectName);
        SceneManager.MoveGameObjectToScene(target, scene);
        Undo.RegisterCreatedObjectUndo(target, undoName);
        return Undo.AddComponent<T>(target);
    }

    private static T FindSingleSceneComponent<T>(Scene scene, bool logMissing = true)
        where T : Component
    {
        T[] components = FindSceneComponents<T>(scene);
        if (components.Length == 1)
            return components[0];

        if (components.Length > 1)
            Debug.LogError($"Expected one {typeof(T).Name} in Scene '{scene.name}', found {components.Length}.");
        else if (logMissing)
            Debug.LogError($"No {typeof(T).Name} exists in Scene '{scene.name}'.");
        return null;
    }

    private static T[] FindSceneComponents<T>(Scene scene) where T : Component
    {
        T[] all = UnityEngine.Object.FindObjectsByType<T>(FindObjectsInactive.Include);
        List<T> results = new List<T>();
        for (int i = 0; i < all.Length; i++)
        {
            if (all[i].gameObject.scene == scene)
                results.Add(all[i]);
        }

        return results.ToArray();
    }

    private static LevelData LoadOrCreateLevel(
        string assetPath,
        string levelId,
        string sceneName,
        string displayName,
        string undoName)
    {
        LevelData data = AssetDatabase.LoadAssetAtPath<LevelData>(assetPath);
        if (data == null)
        {
            data = ScriptableObject.CreateInstance<LevelData>();
            AssetDatabase.CreateAsset(data, assetPath);
            Undo.RegisterCreatedObjectUndo(data, undoName);
        }

        Undo.RecordObject(data, undoName);
        SerializedObject serialized = new SerializedObject(data);
        serialized.FindProperty("levelId").stringValue = levelId;
        serialized.FindProperty("sceneName").stringValue = sceneName;
        serialized.FindProperty("displayName").stringValue = displayName;
        serialized.ApplyModifiedProperties();
        EditorUtility.SetDirty(data);
        return data;
    }

    private static void ConfigureNextLevel(LevelData data, LevelData next, string undoName)
    {
        Undo.RecordObject(data, undoName);
        SerializedObject serialized = new SerializedObject(data);
        serialized.FindProperty("nextLevel").objectReferenceValue = next;
        serialized.ApplyModifiedProperties();
        EditorUtility.SetDirty(data);
    }

    private static void EnsureBuildScene(string scenePath)
    {
        List<EditorBuildSettingsScene> scenes =
            new List<EditorBuildSettingsScene>(EditorBuildSettings.scenes);
        for (int i = 0; i < scenes.Count; i++)
        {
            if (!string.Equals(scenes[i].path, scenePath, StringComparison.OrdinalIgnoreCase))
                continue;

            if (!scenes[i].enabled)
            {
                scenes[i] = new EditorBuildSettingsScene(scenePath, true);
                EditorBuildSettings.scenes = scenes.ToArray();
            }

            return;
        }

        scenes.Add(new EditorBuildSettingsScene(scenePath, true));
        EditorBuildSettings.scenes = scenes.ToArray();
    }

    private static bool EnsureFolder(string parent, string childName)
    {
        string path = parent + "/" + childName;
        if (AssetDatabase.IsValidFolder(path))
            return true;

        if (!AssetDatabase.IsValidFolder(parent))
        {
            Debug.LogError($"Cannot create '{path}': parent folder '{parent}' is missing.");
            return false;
        }

        return !string.IsNullOrEmpty(AssetDatabase.CreateFolder(parent, childName));
    }
}
