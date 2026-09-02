using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace Android2D.ReportDemo
{
    /// <summary>
    /// Owns the Splash -> Home -> Gameplay flow required by the group report.
    /// </summary>
    public sealed class ReportDemoController : MonoBehaviour
    {
        private enum ScreenState
        {
            Splash,
            Home,
            Game
        }

        private readonly Color navy = new Color(0.025f, 0.045f, 0.11f, 1f);
        private readonly Color cyan = new Color(0.16f, 0.77f, 0.95f, 1f);
        private readonly Color cyanPressed = new Color(0.08f, 0.52f, 0.72f, 1f);
        private readonly Color magenta = new Color(0.88f, 0.24f, 0.47f, 1f);
        private readonly Color panel = new Color(0.04f, 0.075f, 0.15f, 0.94f);

        private GameObject splashPanel;
        private GameObject homePanel;
        private GameObject gamePanel;
        private GameObject tutorialPanel;
        private GameObject pausePanel;
        private RectTransform loadingFill;
        private Text loadingLabel;
        private InfiniteScrollingBackground homeBackground;
        private InfiniteScrollingBackground gameBackground;
        private DemoGameController gameController;
        private ReportButton retryButton;
        private ScreenState state;
        private bool paused;

        private void Awake()
        {
            BuildInterface();
            ShowOnly(splashPanel);
            state = ScreenState.Splash;
            StartCoroutine(PlaySplash());
        }

        private void Update()
        {
            if (state == ScreenState.Home && ReportInput.ConfirmPressed() && !tutorialPanel.activeSelf)
            {
                StartGame();
            }

            if (!ReportInput.BackPressed())
            {
                return;
            }

            if (tutorialPanel.activeSelf)
            {
                tutorialPanel.SetActive(false);
            }
            else if (state == ScreenState.Game)
            {
                ShowHome();
            }
            else if (state == ScreenState.Home)
            {
                QuitGame();
            }
        }

        private void BuildInterface()
        {
            GameObject canvasObject = new GameObject("Report Demo Canvas", typeof(RectTransform), typeof(Canvas),
                typeof(CanvasScaler), typeof(GraphicRaycaster));
            canvasObject.transform.SetParent(transform, false);
            Canvas canvas = canvasObject.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 500;
            CanvasScaler scaler = canvasObject.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 0.5f;

            RectTransform canvasRect = canvasObject.GetComponent<RectTransform>();
            splashPanel = BuildSplash(canvasRect);
            homePanel = BuildHome(canvasRect);
            gamePanel = BuildGame(canvasRect);
            Canvas.ForceUpdateCanvases();
        }

        private GameObject BuildSplash(RectTransform parent)
        {
            Image root = RuntimeUiFactory.Image("Splash Screen", parent, navy, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            Image glow = RuntimeUiFactory.Image("Logo Glow", root.transform, new Color(cyan.r, cyan.g, cyan.b, 0.12f),
                new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(-190f, -155f), new Vector2(190f, 225f));
            glow.transform.localRotation = Quaternion.Euler(0f, 0f, 45f);

            Image logo = RuntimeUiFactory.Image("Game Logo", root.transform, Color.white, new Vector2(0.5f, 0.5f),
                new Vector2(0.5f, 0.5f), new Vector2(-115f, -60f), new Vector2(115f, 170f));
            logo.sprite = RuntimeUiFactory.CreateLogoSprite();
            logo.preserveAspect = true;

            RuntimeUiFactory.Text("Title", root.transform, "ECHO RUNNER", 56, Color.white, TextAnchor.MiddleCenter,
                new Vector2(0.2f, 0.5f), new Vector2(0.8f, 0.5f), new Vector2(0f, -168f), new Vector2(0f, -92f), FontStyle.Bold);
            RuntimeUiFactory.Text("Subtitle", root.transform, "2D ANDROID ADVENTURE", 22, new Color(0.56f, 0.72f, 0.84f),
                TextAnchor.MiddleCenter, new Vector2(0.2f, 0.5f), new Vector2(0.8f, 0.5f), new Vector2(0f, -210f), new Vector2(0f, -160f));

            Image loadingTrack = RuntimeUiFactory.Image("Loading Track", root.transform, new Color(1f, 1f, 1f, 0.13f),
                new Vector2(0.3f, 0.16f), new Vector2(0.7f, 0.16f), new Vector2(0f, -10f), new Vector2(0f, 10f));
            Image fill = RuntimeUiFactory.Image("Loading Fill", loadingTrack.transform, cyan, Vector2.zero, new Vector2(0f, 1f),
                new Vector2(3f, 3f), new Vector2(-3f, -3f));
            loadingFill = fill.rectTransform;
            loadingFill.pivot = new Vector2(0f, 0.5f);
            loadingLabel = RuntimeUiFactory.Text("Loading Label", root.transform, "ĐANG KHỞI TẠO  0%", 18,
                new Color(0.7f, 0.82f, 0.9f), TextAnchor.MiddleCenter, new Vector2(0.2f, 0.08f), new Vector2(0.8f, 0.14f),
                Vector2.zero, Vector2.zero);
            RuntimeUiFactory.Text("Course", root.transform, "BÁO CÁO LẬP TRÌNH GAME 2D • UNITY ANDROID", 16,
                new Color(0.5f, 0.62f, 0.72f), TextAnchor.MiddleCenter, new Vector2(0.1f, 0f), new Vector2(0.9f, 0.07f),
                Vector2.zero, Vector2.zero);
            return root.gameObject;
        }

        private GameObject BuildHome(RectTransform parent)
        {
            RectTransform root = RuntimeUiFactory.Rect("Home Screen", parent, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            homeBackground = root.gameObject.AddComponent<InfiniteScrollingBackground>();
            homeBackground.Initialize(RuntimeUiFactory.CreateBackgroundTexture(), 24f);
            RuntimeUiFactory.Image("Dark Overlay", root, new Color(0.01f, 0.02f, 0.06f, 0.58f), Vector2.zero, Vector2.one,
                Vector2.zero, Vector2.zero);

            RectTransform card = RuntimeUiFactory.Image("Home Card", root, panel, new Vector2(0.08f, 0.12f), new Vector2(0.48f, 0.88f),
                Vector2.zero, Vector2.zero).rectTransform;
            Image logo = RuntimeUiFactory.Image("Small Logo", card, Color.white, new Vector2(0.08f, 0.62f), new Vector2(0.34f, 0.94f),
                Vector2.zero, Vector2.zero);
            logo.sprite = RuntimeUiFactory.CreateLogoSprite();
            logo.preserveAspect = true;
            RuntimeUiFactory.Text("Game Title", card, "ECHO\nRUNNER", 55, Color.white, TextAnchor.MiddleLeft,
                new Vector2(0.34f, 0.64f), new Vector2(0.93f, 0.93f), Vector2.zero, Vector2.zero, FontStyle.Bold);
            RuntimeUiFactory.Text("Intro", card, "Khám phá thung lũng Ánh Vọng, thu thập năng lượng và né tránh những bóng đêm đang tuần tra.",
                23, new Color(0.72f, 0.82f, 0.9f), TextAnchor.UpperLeft, new Vector2(0.09f, 0.42f), new Vector2(0.91f, 0.62f),
                Vector2.zero, Vector2.zero);

            RuntimeUiFactory.Button("Play Button", card, "CHƠI NGAY", cyan, cyanPressed, StartGame,
                new Vector2(0.09f, 0.25f), new Vector2(0.91f, 0.37f), Vector2.zero, Vector2.zero);
            RuntimeUiFactory.Button("Tutorial Button", card, "HƯỚNG DẪN", new Color(0.17f, 0.21f, 0.32f),
                new Color(0.24f, 0.3f, 0.43f), () => tutorialPanel.SetActive(true),
                new Vector2(0.09f, 0.11f), new Vector2(0.58f, 0.21f), Vector2.zero, Vector2.zero);
            RuntimeUiFactory.Button("Exit Button", card, "THOÁT", new Color(0.32f, 0.12f, 0.2f),
                new Color(0.5f, 0.15f, 0.25f), QuitGame, new Vector2(0.62f, 0.11f), new Vector2(0.91f, 0.21f),
                Vector2.zero, Vector2.zero);

            RectTransform preview = RuntimeUiFactory.Image("Intro Image Frame", root, new Color(0.05f, 0.1f, 0.2f, 0.72f),
                new Vector2(0.55f, 0.15f), new Vector2(0.92f, 0.84f), Vector2.zero, Vector2.zero).rectTransform;
            RuntimeUiFactory.Text("Preview Badge", preview, "INTRODUCTORY IMAGE", 18, cyan, TextAnchor.MiddleCenter,
                new Vector2(0.18f, 0.82f), new Vector2(0.82f, 0.91f), Vector2.zero, Vector2.zero, FontStyle.Bold);
            Image hero = RuntimeUiFactory.Image("Hero Preview", preview, Color.white, new Vector2(0.22f, 0.12f), new Vector2(0.78f, 0.78f),
                Vector2.zero, Vector2.zero);
            hero.sprite = RuntimeUiFactory.CreatePlayerSprite();
            hero.preserveAspect = true;
            RuntimeUiFactory.Text("Preview Text", preview, "THE LIGHT SEEKER", 25, Color.white, TextAnchor.MiddleCenter,
                new Vector2(0.08f, 0.04f), new Vector2(0.92f, 0.14f), Vector2.zero, Vector2.zero, FontStyle.Bold);

            tutorialPanel = BuildTutorial(root);
            tutorialPanel.SetActive(false);
            return root.gameObject;
        }

        private GameObject BuildTutorial(RectTransform parent)
        {
            Image overlay = RuntimeUiFactory.Image("Tutorial Overlay", parent, new Color(0f, 0f, 0f, 0.82f), Vector2.zero, Vector2.one,
                Vector2.zero, Vector2.zero);
            RectTransform card = RuntimeUiFactory.Image("Tutorial Card", overlay.transform, panel, new Vector2(0.25f, 0.18f),
                new Vector2(0.75f, 0.82f), Vector2.zero, Vector2.zero).rectTransform;
            RuntimeUiFactory.Text("Tutorial Title", card, "CÁCH CHƠI", 44, Color.white, TextAnchor.MiddleCenter,
                new Vector2(0.1f, 0.76f), new Vector2(0.9f, 0.94f), Vector2.zero, Vector2.zero, FontStyle.Bold);
            RuntimeUiFactory.Text("Tutorial Body", card,
                "◀  ▶   Giữ nút để di chuyển\n\n↑   Chạm để nhảy\n\n◆   Thu thập tinh thể vàng\n\n●   Tránh kẻ địch màu đỏ\n\nBàn phím: A/D hoặc ←/→, Space để nhảy",
                26, new Color(0.75f, 0.86f, 0.93f), TextAnchor.MiddleLeft, new Vector2(0.12f, 0.25f),
                new Vector2(0.88f, 0.75f), Vector2.zero, Vector2.zero);
            RuntimeUiFactory.Button("Close Tutorial", card, "ĐÃ HIỂU", cyan, cyanPressed, () => tutorialPanel.SetActive(false),
                new Vector2(0.22f, 0.08f), new Vector2(0.78f, 0.2f), Vector2.zero, Vector2.zero);
            return overlay.gameObject;
        }

        private GameObject BuildGame(RectTransform parent)
        {
            RectTransform root = RuntimeUiFactory.Rect("Gameplay Screen", parent, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            gameBackground = root.gameObject.AddComponent<InfiniteScrollingBackground>();
            gameBackground.Initialize(RuntimeUiFactory.CreateBackgroundTexture(), 115f);

            RuntimeUiFactory.Image("Ground", root, new Color(0.025f, 0.055f, 0.075f, 1f), new Vector2(0f, 0f),
                new Vector2(1f, 0.18f), Vector2.zero, Vector2.zero);
            RuntimeUiFactory.Image("Ground Line", root, cyan, new Vector2(0f, 0.18f), new Vector2(1f, 0.185f),
                Vector2.zero, Vector2.zero);

            Image playerImage = RuntimeUiFactory.Image("Player", root, Color.white, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f),
                new Vector2(-55f, 108f), new Vector2(55f, 255f));
            playerImage.sprite = RuntimeUiFactory.CreatePlayerSprite();
            playerImage.preserveAspect = true;
            playerImage.rectTransform.pivot = new Vector2(0.5f, 0f);

            Image enemyImage = RuntimeUiFactory.Image("Enemy", root, Color.white, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f),
                new Vector2(-55f, 108f), new Vector2(55f, 232f));
            enemyImage.sprite = RuntimeUiFactory.CreateEnemySprite();
            enemyImage.preserveAspect = true;
            enemyImage.rectTransform.pivot = new Vector2(0.5f, 0f);

            Image crystalImage = RuntimeUiFactory.Image("Crystal", root, Color.white, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f),
                new Vector2(-34f, 110f), new Vector2(34f, 198f));
            crystalImage.sprite = RuntimeUiFactory.CreateCrystalSprite();
            crystalImage.preserveAspect = true;
            crystalImage.rectTransform.pivot = new Vector2(0.5f, 0f);

            Text score = RuntimeUiFactory.Text("Score", root, "NĂNG LƯỢNG  000", 25, Color.white, TextAnchor.MiddleLeft,
                new Vector2(0.03f, 0.9f), new Vector2(0.35f, 0.98f), Vector2.zero, Vector2.zero, FontStyle.Bold);
            Text lives = RuntimeUiFactory.Text("Lives", root, "MẠNG  ♥♥♥", 25, new Color(1f, 0.45f, 0.58f), TextAnchor.MiddleLeft,
                new Vector2(0.03f, 0.82f), new Vector2(0.35f, 0.9f), Vector2.zero, Vector2.zero, FontStyle.Bold);
            Text message = RuntimeUiFactory.Text("Mission", root, "THU THẬP TINH THỂ • TRÁNH BÓNG ĐÊM", 20,
                new Color(0.8f, 0.9f, 0.96f), TextAnchor.MiddleCenter, new Vector2(0.28f, 0.9f), new Vector2(0.72f, 0.98f),
                Vector2.zero, Vector2.zero, FontStyle.Bold);

            RuntimeUiFactory.Button("Home Button", root, "⌂", new Color(0.08f, 0.12f, 0.2f, 0.88f),
                new Color(0.18f, 0.25f, 0.38f, 1f), ShowHome, new Vector2(0.86f, 0.9f), new Vector2(0.91f, 0.98f),
                Vector2.zero, Vector2.zero);
            RuntimeUiFactory.Button("Pause Button", root, "Ⅱ", new Color(0.08f, 0.12f, 0.2f, 0.88f),
                new Color(0.18f, 0.25f, 0.38f, 1f), TogglePause, new Vector2(0.92f, 0.9f), new Vector2(0.97f, 0.98f),
                Vector2.zero, Vector2.zero);

            ReportButton left = RuntimeUiFactory.Button("Move Left", root, "◀", new Color(0.1f, 0.18f, 0.28f, 0.7f),
                new Color(cyan.r, cyan.g, cyan.b, 0.85f), null, new Vector2(0.035f, 0.035f), new Vector2(0.125f, 0.16f),
                Vector2.zero, Vector2.zero);
            ReportButton right = RuntimeUiFactory.Button("Move Right", root, "▶", new Color(0.1f, 0.18f, 0.28f, 0.7f),
                new Color(cyan.r, cyan.g, cyan.b, 0.85f), null, new Vector2(0.14f, 0.035f), new Vector2(0.23f, 0.16f),
                Vector2.zero, Vector2.zero);
            ReportButton jump = RuntimeUiFactory.Button("Jump", root, "↑", new Color(0.32f, 0.12f, 0.22f, 0.78f),
                magenta, null, new Vector2(0.865f, 0.035f), new Vector2(0.965f, 0.17f), Vector2.zero, Vector2.zero);

            gameController = root.gameObject.AddComponent<DemoGameController>();
            gameController.Initialize(root, playerImage.rectTransform, enemyImage.rectTransform, crystalImage.rectTransform,
                left.RectTransform, right.RectTransform, jump.RectTransform, score, lives, message, ShowRetry);

            pausePanel = BuildPause(root);
            pausePanel.SetActive(false);
            retryButton = RuntimeUiFactory.Button("Retry Button", root, "CHƠI LẠI", magenta, new Color(0.65f, 0.12f, 0.3f),
                RestartGame, new Vector2(0.4f, 0.43f), new Vector2(0.6f, 0.53f), Vector2.zero, Vector2.zero);
            retryButton.gameObject.SetActive(false);
            return root.gameObject;
        }

        private GameObject BuildPause(RectTransform parent)
        {
            Image overlay = RuntimeUiFactory.Image("Pause Overlay", parent, new Color(0f, 0f, 0f, 0.7f), Vector2.zero, Vector2.one,
                Vector2.zero, Vector2.zero);
            RuntimeUiFactory.Text("Paused", overlay.transform, "TẠM DỪNG", 54, Color.white, TextAnchor.MiddleCenter,
                new Vector2(0.2f, 0.55f), new Vector2(0.8f, 0.7f), Vector2.zero, Vector2.zero, FontStyle.Bold);
            RuntimeUiFactory.Button("Continue", overlay.transform, "TIẾP TỤC", cyan, cyanPressed, TogglePause,
                new Vector2(0.38f, 0.41f), new Vector2(0.62f, 0.5f), Vector2.zero, Vector2.zero);
            RuntimeUiFactory.Button("Pause Home", overlay.transform, "VỀ TRANG CHỦ", new Color(0.18f, 0.22f, 0.32f),
                new Color(0.28f, 0.34f, 0.47f), ShowHome, new Vector2(0.38f, 0.29f), new Vector2(0.62f, 0.38f),
                Vector2.zero, Vector2.zero);
            return overlay.gameObject;
        }

        private IEnumerator PlaySplash()
        {
            const float duration = 2.35f;
            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                float normalized = Mathf.Clamp01(elapsed / duration);
                float eased = 1f - Mathf.Pow(1f - normalized, 3f);
                loadingFill.anchorMax = new Vector2(eased, 1f);
                loadingLabel.text = $"ĐANG KHỞI TẠO  {Mathf.RoundToInt(eased * 100f)}%";
                yield return null;
            }
            ShowHome();
        }

        private void StartGame()
        {
            StopAllCoroutines();
            tutorialPanel.SetActive(false);
            ShowOnly(gamePanel);
            state = ScreenState.Game;
            paused = false;
            pausePanel.SetActive(false);
            retryButton.gameObject.SetActive(false);
            gameController.ResetGame();
            gameController.SetRunning(true);
            gameBackground.SetRunning(true);
        }

        private void RestartGame()
        {
            retryButton.gameObject.SetActive(false);
            gameController.ResetGame();
            gameController.SetRunning(true);
            gameBackground.SetRunning(true);
        }

        private void ShowRetry()
        {
            retryButton.gameObject.SetActive(true);
            gameBackground.SetRunning(false);
        }

        private void ShowHome()
        {
            ShowOnly(homePanel);
            state = ScreenState.Home;
            paused = false;
            tutorialPanel.SetActive(false);
            pausePanel.SetActive(false);
            homeBackground.SetRunning(true);
            gameBackground.SetRunning(false);
            gameController.SetRunning(false);
        }

        private void TogglePause()
        {
            if (state != ScreenState.Game)
            {
                return;
            }

            paused = !paused;
            pausePanel.SetActive(paused);
            gameController.SetRunning(!paused);
            gameBackground.SetRunning(!paused);
        }

        private void ShowOnly(GameObject visible)
        {
            splashPanel.SetActive(visible == splashPanel);
            homePanel.SetActive(visible == homePanel);
            gamePanel.SetActive(visible == gamePanel);
        }

        private static void QuitGame()
        {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }
    }
}
