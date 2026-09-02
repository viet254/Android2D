using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// One-click setup for exactly two report requirements: the entry menu and
/// the two-panel scrolling background in SampleScene.
/// </summary>
public static class MenuAndBackgroundSetup
{
    private const string MenuScenePath = "Assets/Scenes/Menu.unity";
    private const string GameplayScenePath = "Assets/Scenes/SampleScene.unity";
    private const string BackgroundPath = "Assets/World/Background/Sprites/forest_mountains_background.png";
    private const string GeneratedBackgroundName = "Moving Background (Slide 20)";

    [InitializeOnLoadMethod]
    private static void ScheduleAutomaticSetup()
    {
        EditorApplication.delayCall += () =>
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode || IsSetupComplete())
            {
                return;
            }

            if (EditorSceneManager.GetActiveScene().isDirty)
            {
                Debug.LogWarning("[Menu + Background] Scene hiện tại có thay đổi chưa lưu. " +
                                 "Hãy lưu scene rồi chọn Tools > Báo cáo > Thiết lập Menu + Background động.");
                return;
            }

            RunSetup(false);
        };
    }

    [MenuItem("Tools/Báo cáo/Thiết lập Menu + Background động _F8", priority = 20)]
    public static void Setup()
    {
        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
        {
            return;
        }

        RunSetup(true);
    }

    private static void RunSetup(bool showCompletionDialog)
    {

        ConfigureBackgroundImport();
        Sprite background = AssetDatabase.LoadAssetAtPath<Sprite>(BackgroundPath);
        if (background == null)
        {
            EditorUtility.DisplayDialog("Thiết lập thất bại",
                "Không đọc được ảnh nền tại:\n" + BackgroundPath, "OK");
            return;
        }

        CreateMenuScene(background);
        AddMovingBackground(background);
        EnsureBuildOrder();
        EditorSceneManager.OpenScene(MenuScenePath, OpenSceneMode.Single);

        if (showCompletionDialog)
        {
            EditorUtility.DisplayDialog("Hoàn tất",
                "Đã tạo màn hình Menu và thêm background động vào SampleScene.\n\nNhấn Play để kiểm tra.", "OK");
        }
        else
        {
            Debug.Log("[Menu + Background] Đã tự động tạo Menu và background động. Scene Menu đang sẵn sàng để chạy.");
        }
    }

    private static bool IsSetupComplete()
    {
        string projectRoot = Path.GetDirectoryName(Application.dataPath);
        string menuFile = Path.Combine(projectRoot, MenuScenePath);
        string gameplayFile = Path.Combine(projectRoot, GameplayScenePath);
        return File.Exists(menuFile) && File.Exists(gameplayFile) &&
               File.ReadAllText(menuFile).Contains("m_Name: Menu Canvas") &&
               File.ReadAllText(gameplayFile).Contains("m_Name: " + GeneratedBackgroundName);
    }

    private static void ConfigureBackgroundImport()
    {
        TextureImporter importer = AssetImporter.GetAtPath(BackgroundPath) as TextureImporter;
        if (importer == null)
        {
            AssetDatabase.ImportAsset(BackgroundPath, ImportAssetOptions.ForceSynchronousImport);
            importer = AssetImporter.GetAtPath(BackgroundPath) as TextureImporter;
        }

        if (importer == null)
        {
            return;
        }

        importer.textureType = TextureImporterType.Sprite;
        importer.spriteImportMode = SpriteImportMode.Single;
        importer.spritePixelsPerUnit = 100f;
        importer.filterMode = FilterMode.Bilinear;
        importer.wrapMode = TextureWrapMode.Clamp;
        importer.textureCompression = TextureImporterCompression.Uncompressed;
        importer.maxTextureSize = 2048;
        importer.SaveAndReimport();
    }

    private static void CreateMenuScene(Sprite background)
    {
        Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

        GameObject cameraObject = new GameObject("Main Camera", typeof(Camera), typeof(AudioListener));
        cameraObject.tag = "MainCamera";
        Camera camera = cameraObject.GetComponent<Camera>();
        camera.orthographic = true;
        camera.clearFlags = CameraClearFlags.SolidColor;
        camera.backgroundColor = new Color(0.02f, 0.03f, 0.08f);
        cameraObject.transform.position = new Vector3(0f, 0f, -10f);

        GameObject canvasObject = new GameObject("Menu Canvas", typeof(RectTransform), typeof(Canvas),
            typeof(CanvasScaler), typeof(GraphicRaycaster), typeof(MainMenuController));
        Canvas canvas = canvasObject.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        CanvasScaler scaler = canvasObject.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        scaler.matchWidthOrHeight = 0.5f;

        Image backdrop = CreateImage("Background", canvasObject.transform, Color.white, Vector2.zero, Vector2.one,
            Vector2.zero, Vector2.zero);
        backdrop.sprite = background;
        backdrop.preserveAspect = true;
        AspectRatioFitter fitter = backdrop.gameObject.AddComponent<AspectRatioFitter>();
        fitter.aspectMode = AspectRatioFitter.AspectMode.EnvelopeParent;
        fitter.aspectRatio = background.rect.width / background.rect.height;

        CreateImage("Dark Overlay", canvasObject.transform, new Color(0.01f, 0.02f, 0.07f, 0.48f), Vector2.zero,
            Vector2.one, Vector2.zero, Vector2.zero);

        RectTransform panel = CreateImage("Menu Panel", canvasObject.transform, new Color(0.025f, 0.05f, 0.12f, 0.9f),
            new Vector2(0.29f, 0.14f), new Vector2(0.71f, 0.86f), Vector2.zero, Vector2.zero).rectTransform;

        CreateText("Small Title", panel, "2D ANDROID GAME", 24, new Color(0.42f, 0.82f, 1f), TextAnchor.MiddleCenter,
            new Vector2(0.08f, 0.76f), new Vector2(0.92f, 0.88f), FontStyle.Bold);
        CreateText("Game Title", panel, "DARK FOREST", 64, Color.white, TextAnchor.MiddleCenter,
            new Vector2(0.06f, 0.55f), new Vector2(0.94f, 0.77f), FontStyle.Bold);
        CreateText("Subtitle", panel, "A 2D ADVENTURE", 22, new Color(0.72f, 0.79f, 0.9f), TextAnchor.MiddleCenter,
            new Vector2(0.08f, 0.47f), new Vector2(0.92f, 0.56f), FontStyle.Normal);

        Button playButton = CreateButton("Play Button", panel, "CHƠI", new Color(0.08f, 0.55f, 0.78f),
            new Vector2(0.18f, 0.26f), new Vector2(0.82f, 0.39f));
        Button exitButton = CreateButton("Exit Button", panel, "THOÁT", new Color(0.18f, 0.22f, 0.32f),
            new Vector2(0.18f, 0.09f), new Vector2(0.82f, 0.22f));

        MainMenuController controller = canvasObject.GetComponent<MainMenuController>();
        UnityEventTools.AddPersistentListener(playButton.onClick, controller.PlayGame);
        UnityEventTools.AddPersistentListener(exitButton.onClick, controller.QuitGame);

        GameObject eventSystem = new GameObject("EventSystem", typeof(EventSystem), typeof(InputSystemUIInputModule));
        eventSystem.transform.SetAsLastSibling();

        EditorSceneManager.SaveScene(scene, MenuScenePath);
    }

    private static void AddMovingBackground(Sprite background)
    {
        Scene scene = EditorSceneManager.OpenScene(GameplayScenePath, OpenSceneMode.Single);
        Camera camera = Camera.main != null ? Camera.main : Object.FindAnyObjectByType<Camera>();
        if (camera == null)
        {
            throw new MissingReferenceException("SampleScene không có Camera để gắn background.");
        }

        GameObject previous = GameObject.Find(GeneratedBackgroundName);
        if (previous != null)
        {
            Object.DestroyImmediate(previous);
        }

        GameObject root = new GameObject(GeneratedBackgroundName, typeof(ScrollingBackground));
        root.transform.SetParent(camera.transform, false);
        root.transform.localPosition = new Vector3(0f, 0f, 10f);

        SpriteRenderer first = CreateBackgroundPanel("Background A", root.transform, background);
        SpriteRenderer second = CreateBackgroundPanel("Background B", root.transform, background);
        root.GetComponent<ScrollingBackground>().Configure(first, second, camera, 1.2f);

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
    }

    private static SpriteRenderer CreateBackgroundPanel(string name, Transform parent, Sprite sprite)
    {
        GameObject panel = new GameObject(name, typeof(SpriteRenderer));
        panel.transform.SetParent(parent, false);
        SpriteRenderer renderer = panel.GetComponent<SpriteRenderer>();
        renderer.sprite = sprite;
        renderer.sortingOrder = -1000;
        return renderer;
    }

    private static void EnsureBuildOrder()
    {
        List<EditorBuildSettingsScene> scenes = EditorBuildSettings.scenes.ToList();
        scenes.RemoveAll(scene => scene.path == MenuScenePath);
        scenes.Insert(0, new EditorBuildSettingsScene(MenuScenePath, true));

        EditorBuildSettingsScene gameplay = scenes.FirstOrDefault(scene => scene.path == GameplayScenePath);
        if (gameplay == null)
        {
            scenes.Add(new EditorBuildSettingsScene(GameplayScenePath, true));
        }
        else
        {
            gameplay.enabled = true;
        }

        EditorBuildSettings.scenes = scenes.ToArray();
    }

    private static Image CreateImage(string name, Transform parent, Color color, Vector2 anchorMin,
        Vector2 anchorMax, Vector2 offsetMin, Vector2 offsetMax)
    {
        GameObject gameObject = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        gameObject.transform.SetParent(parent, false);
        RectTransform rect = gameObject.GetComponent<RectTransform>();
        rect.anchorMin = anchorMin;
        rect.anchorMax = anchorMax;
        rect.offsetMin = offsetMin;
        rect.offsetMax = offsetMax;
        Image image = gameObject.GetComponent<Image>();
        image.color = color;
        image.raycastTarget = false;
        return image;
    }

    private static Text CreateText(string name, Transform parent, string value, int fontSize, Color color,
        TextAnchor alignment, Vector2 anchorMin, Vector2 anchorMax, FontStyle style)
    {
        GameObject gameObject = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Text));
        gameObject.transform.SetParent(parent, false);
        RectTransform rect = gameObject.GetComponent<RectTransform>();
        rect.anchorMin = anchorMin;
        rect.anchorMax = anchorMax;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
        Text text = gameObject.GetComponent<Text>();
        text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        text.text = value;
        text.fontSize = fontSize;
        text.fontStyle = style;
        text.color = color;
        text.alignment = alignment;
        text.raycastTarget = false;
        return text;
    }

    private static Button CreateButton(string name, Transform parent, string label, Color normal,
        Vector2 anchorMin, Vector2 anchorMax)
    {
        Image image = CreateImage(name, parent, normal, anchorMin, anchorMax, Vector2.zero, Vector2.zero);
        image.raycastTarget = true;
        Button button = image.gameObject.AddComponent<Button>();
        button.targetGraphic = image;
        ColorBlock colors = button.colors;
        colors.normalColor = normal;
        colors.highlightedColor = Color.Lerp(normal, Color.white, 0.16f);
        colors.pressedColor = Color.Lerp(normal, Color.black, 0.25f);
        colors.selectedColor = colors.highlightedColor;
        button.colors = colors;

        CreateText("Label", image.transform, label, 30, Color.white, TextAnchor.MiddleCenter,
            Vector2.zero, Vector2.one, FontStyle.Bold);
        return button;
    }
}
