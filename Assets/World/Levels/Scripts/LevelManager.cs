using System;
using UnityEngine;
using UnityEngine.SceneManagement;

[DefaultExecutionOrder(-900)]
[DisallowMultipleComponent]
public sealed class LevelManager : MonoBehaviour
{
    [SerializeField] private LevelData currentLevel;
    [SerializeField] private LevelData startingLevel;
    [SerializeField] private SceneLoader sceneLoader;

    public LevelData CurrentLevel => currentLevel;
    public bool IsTransitioning => SceneLoader.IsTransitioning;

    private void Awake()
    {
        ResolveSceneLoader();

        if (currentLevel != null
            && !string.Equals(currentLevel.SceneName, SceneManager.GetActiveScene().name, StringComparison.Ordinal))
        {
            Debug.LogError(
                $"[LevelManager] LevelData '{currentLevel.name}' targets Scene '{currentLevel.SceneName}', " +
                $"but active Scene is '{SceneManager.GetActiveScene().name}'.",
                this);
        }
    }

    public bool LoadStartingLevel()
    {
        if (startingLevel == null)
        {
            Debug.LogError("[LevelManager] Starting Level is not assigned.", this);
            return false;
        }

        return LoadLevel(startingLevel);
    }

    public bool LoadNextLevel()
    {
        if (currentLevel == null || currentLevel.NextLevel == null)
        {
            Debug.LogWarning("[LevelManager] Current Level has no next Level configured.", this);
            return false;
        }

        return LoadLevel(currentLevel.NextLevel);
    }

    public bool LoadLevel(LevelData level)
    {
        if (level == null || !level.IsValid)
        {
            Debug.LogError("[LevelManager] Cannot load an invalid LevelData asset.", this);
            return false;
        }

        ResolveSceneLoader();
        if (sceneLoader == null)
        {
            Debug.LogError("[LevelManager] No SceneLoader is available. Run Level Progression setup.", this);
            return false;
        }

        if (!sceneLoader.CanLoadScene(level.SceneName))
        {
            Debug.LogError(
                $"[LevelManager] Scene '{level.SceneName}' for Level '{level.LevelId}' is not enabled in Build Settings.",
                level);
            return false;
        }

        SaveManager saveManager = FindAnyObjectByType<SaveManager>();
        PlayerTransitionBuffer.Clear();
        if (saveManager != null)
        {
            if (!saveManager.TryCapturePlayerTransition(out PlayerTransitionState transitionState))
                return false;

            PlayerTransitionBuffer.Store(transitionState);
        }

        if (sceneLoader.LoadScene(level.SceneName, SceneLoadReason.Normal))
            return true;

        PlayerTransitionBuffer.Clear();
        return false;
    }

    private void ResolveSceneLoader()
    {
        if (sceneLoader == null)
            sceneLoader = SceneLoader.Instance != null
                ? SceneLoader.Instance
                : FindAnyObjectByType<SceneLoader>();
    }
}
