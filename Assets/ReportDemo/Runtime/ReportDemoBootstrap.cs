using UnityEngine;
using UnityEngine.SceneManagement;

namespace Android2D.ReportDemo
{
    /// <summary>
    /// Automatically installs the report demo when the empty Menu scene is played.
    /// No manual prefab wiring is required.
    /// </summary>
    public static class ReportDemoBootstrap
    {
        private const string DemoSceneName = "Menu";

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Install()
        {
            if (SceneManager.GetActiveScene().name != DemoSceneName ||
                Object.FindAnyObjectByType<ReportDemoController>() != null)
            {
                return;
            }

            Application.targetFrameRate = 60;
            Screen.sleepTimeout = SleepTimeout.NeverSleep;

            GameObject root = new GameObject("Report Demo - Runtime Generated");
            root.AddComponent<ReportDemoController>();
        }
    }
}
