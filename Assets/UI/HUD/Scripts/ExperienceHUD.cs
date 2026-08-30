using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class ExperienceHUD : MonoBehaviour
{
    [Header("Data Source")]
    [SerializeField] private PlayerExperience playerExperience;

    [Header("UI References")]
    [SerializeField] private Slider experienceSlider;
    [SerializeField] private Text experienceText;
    [SerializeField] private Text levelText;

    private PlayerExperience subscribedExperience;

    private void OnEnable()
    {
        SceneManager.sceneLoaded += HandleSceneLoaded;
        BindExperience(ResolvePlayerExperience());
        ValidateReferences();
    }

    private void Start()
    {
        if (subscribedExperience == null)
            BindExperience(ResolvePlayerExperience());
        else
            Refresh();
    }

    private void OnDisable()
    {
        SceneManager.sceneLoaded -= HandleSceneLoaded;
        Unsubscribe();
    }

    private void HandleSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        BindExperience(ResolvePlayerExperience());
    }

    private PlayerExperience ResolvePlayerExperience()
    {
        if (playerExperience != null)
            return playerExperience;

        return FindFirstObjectByType<PlayerExperience>();
    }

    private void BindExperience(PlayerExperience source)
    {
        if (subscribedExperience == source)
        {
            Refresh();
            return;
        }

        Unsubscribe();
        playerExperience = source;
        subscribedExperience = source;

        if (subscribedExperience == null)
        {
            ShowUnavailableState();
            return;
        }

        subscribedExperience.OnExperienceChanged += HandleExperienceChanged;
        subscribedExperience.OnLevelUp += HandleLevelUp;
        Refresh();
    }

    private void Unsubscribe()
    {
        if (subscribedExperience == null)
            return;

        subscribedExperience.OnExperienceChanged -= HandleExperienceChanged;
        subscribedExperience.OnLevelUp -= HandleLevelUp;
        subscribedExperience = null;
    }

    private void HandleExperienceChanged(int currentExperience, int experienceToNextLevel)
    {
        Refresh();
    }

    private void HandleLevelUp(int newLevel)
    {
        Refresh();
    }

    private void Refresh()
    {
        if (subscribedExperience == null)
        {
            ShowUnavailableState();
            return;
        }

        int current = Mathf.Max(0, subscribedExperience.CurrentExperience);
        int required = Mathf.Max(1, subscribedExperience.ExperienceToNextLevel);

        if (experienceSlider != null)
        {
            experienceSlider.minValue = 0f;
            experienceSlider.maxValue = required;
            experienceSlider.wholeNumbers = true;
            experienceSlider.value = Mathf.Clamp(current, 0, required);
        }

        if (experienceText != null)
            experienceText.text = $"{current} / {required}";

        if (levelText != null)
            levelText.text = $"Level {subscribedExperience.CurrentLevel}";
    }

    private void ShowUnavailableState()
    {
        if (experienceSlider != null)
        {
            experienceSlider.minValue = 0f;
            experienceSlider.maxValue = 1f;
            experienceSlider.value = 0f;
        }

        if (experienceText != null)
            experienceText.text = "0 / 0";

        if (levelText != null)
            levelText.text = "Level -";
    }

    private void ValidateReferences()
    {
        if (experienceSlider == null || experienceText == null || levelText == null)
            Debug.LogWarning("[ExperienceHUD] Assign Slider, Experience Text and Level Text in the Inspector.", this);

        if (subscribedExperience == null)
            Debug.LogWarning("[ExperienceHUD] No active PlayerExperience was found in the scene.", this);
    }
}
