using System;
using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

[DefaultExecutionOrder(-1000)]
[DisallowMultipleComponent]
public sealed class SceneLoader : MonoBehaviour
{
    public static SceneLoader Instance { get; private set; }
    public static SceneLoadReason CurrentLoadReason { get; private set; } = SceneLoadReason.Normal;
    public static bool IsTransitioning => Instance != null && Instance.isTransitioning;

    public static event Action<Scene, SceneLoadReason> SceneLoadCompleted;

    private bool isTransitioning;
    private SceneLoadReason pendingReason;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatics()
    {
        Instance = null;
        CurrentLoadReason = SceneLoadReason.Normal;
        SceneLoadCompleted = null;
    }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    private void OnDestroy()
    {
        if (Instance != this)
            return;

        SceneManager.sceneLoaded -= HandleSceneLoaded;
        Instance = null;
        isTransitioning = false;
        CurrentLoadReason = SceneLoadReason.Normal;
    }

    public bool CanLoadScene(string sceneName)
    {
        return !string.IsNullOrWhiteSpace(sceneName)
            && Application.CanStreamedLevelBeLoaded(sceneName);
    }

    public bool LoadScene(string sceneName, SceneLoadReason reason = SceneLoadReason.Normal)
    {
        if (isTransitioning)
        {
            Debug.LogWarning($"[SceneLoader] Ignored request for '{sceneName}': a Scene load is already running.", this);
            return false;
        }

        if (!CanLoadScene(sceneName))
        {
            Debug.LogError($"[SceneLoader] Scene '{sceneName}' is empty or is not enabled in Build Settings.", this);
            return false;
        }

        if (string.Equals(SceneManager.GetActiveScene().name, sceneName, StringComparison.Ordinal))
        {
            Debug.LogWarning($"[SceneLoader] Scene '{sceneName}' is already active.", this);
            return false;
        }

        isTransitioning = true;
        pendingReason = reason;
        CurrentLoadReason = reason;
        SceneManager.sceneLoaded += HandleSceneLoaded;

        try
        {
            SceneManager.LoadScene(sceneName, LoadSceneMode.Single);
            return true;
        }
        catch (Exception exception)
        {
            SceneManager.sceneLoaded -= HandleSceneLoaded;
            isTransitioning = false;
            CurrentLoadReason = SceneLoadReason.Normal;
            Debug.LogError($"[SceneLoader] Failed to load '{sceneName}': {exception.Message}", this);
            return false;
        }
    }

    private void HandleSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        SceneManager.sceneLoaded -= HandleSceneLoaded;
        StartCoroutine(CompleteLoadNextFrame(scene));
    }

    private IEnumerator CompleteLoadNextFrame(Scene scene)
    {
        yield return null;

        SceneLoadReason completedReason = pendingReason;
        isTransitioning = false;
        SceneLoadCompleted?.Invoke(scene, completedReason);
        CurrentLoadReason = SceneLoadReason.Normal;
    }
}
