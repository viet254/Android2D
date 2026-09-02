using UnityEngine;

namespace Android2D.ReportDemo
{
    /// <summary>
    /// Applies the presentation settings before the Menu scene is shown.
    /// </summary>
    public static class ReportDemoPlatformSetup
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Configure()
        {
            Application.targetFrameRate = 60;
            Screen.sleepTimeout = SleepTimeout.NeverSleep;
            Screen.orientation = ScreenOrientation.LandscapeLeft;
        }
    }
}
