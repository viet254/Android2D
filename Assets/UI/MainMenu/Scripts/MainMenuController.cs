using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Handles only the two actions required by the entry menu.
/// </summary>
public sealed class MainMenuController : MonoBehaviour
{
    [SerializeField] private string firstGameplayScene = "SampleScene";

    public void PlayGame()
    {
        SceneManager.LoadScene(firstGameplayScene);
    }

    public void QuitGame()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }
}
