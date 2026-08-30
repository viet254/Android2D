using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class AnimatedExperienceHUD : MonoBehaviour
{
    [Header("Data")]
    [SerializeField] private PlayerExperience playerExperience;

    [Header("Required UI")]
    [SerializeField] private Slider experienceSlider;
    [SerializeField] private Text experienceText;
    [SerializeField] private Text levelText;

    [Header("Optional Style References")]
    [SerializeField] private Image backgroundImage;
    [SerializeField] private Image fillImage;
    [SerializeField] private Image frameImage;
    [SerializeField] private RectTransform barRoot;
    [SerializeField] private Text gainText;
    [SerializeField] private CanvasGroup gainTextGroup;

    [Header("Dark Fantasy Colors")]
    [SerializeField] private Color backgroundColor = new Color(0.02f, 0.03f, 0.055f, 0.96f);
    [SerializeField] private Color fillColor = new Color(0.1f, 0.62f, 0.95f, 1f);
    [SerializeField] private Color frameColor = new Color(0.65f, 0.78f, 0.9f, 1f);
    [SerializeField] private Color accentColor = new Color(1f, 0.75f, 0.2f, 1f);

    [Header("Animation")]
    [SerializeField, Min(0.05f)] private float fillDuration = 0.45f;
    [SerializeField, Min(0.05f)] private float pulseDuration = 0.22f;
    [SerializeField, Min(1f)] private float pulseScale = 1.08f;
    [SerializeField, Min(0.1f)] private float gainTextDuration = 0.9f;

    private Coroutine progressRoutine;
    private Coroutine gainRoutine;
    private int displayedLevel;
    private Vector3 baseScale = Vector3.one;

    private void Awake()
    {
        if (barRoot == null && experienceSlider != null)
            barRoot = experienceSlider.transform as RectTransform;

        if (barRoot != null)
            baseScale = barRoot.localScale;

        ApplyStyle();
    }

    private void OnEnable()
    {
        if (playerExperience == null)
            playerExperience = FindFirstObjectByType<PlayerExperience>();

        Subscribe();
        RefreshImmediate();
    }

    private void OnDisable()
    {
        Unsubscribe();

        if (progressRoutine != null)
            StopCoroutine(progressRoutine);
        if (gainRoutine != null)
            StopCoroutine(gainRoutine);

        progressRoutine = null;
        gainRoutine = null;

        if (barRoot != null)
            barRoot.localScale = baseScale;
        if (gainTextGroup != null)
            gainTextGroup.alpha = 0f;
    }

    private void Subscribe()
    {
        if (playerExperience == null)
            return;

        playerExperience.OnXPGained += HandleExperienceGained;
        playerExperience.OnExperienceChanged += HandleExperienceChanged;
        playerExperience.OnLevelUp += HandleLevelUp;
    }

    private void Unsubscribe()
    {
        if (playerExperience == null)
            return;

        playerExperience.OnXPGained -= HandleExperienceGained;
        playerExperience.OnExperienceChanged -= HandleExperienceChanged;
        playerExperience.OnLevelUp -= HandleLevelUp;
    }

    private void HandleExperienceGained(int gained, int current, int required)
    {
        if (gainRoutine != null)
            StopCoroutine(gainRoutine);
        gainRoutine = StartCoroutine(ShowGainRoutine(gained));
    }

    private void HandleExperienceChanged(int current, int required)
    {
        if (progressRoutine != null)
            StopCoroutine(progressRoutine);
        progressRoutine = StartCoroutine(AnimateProgressRoutine(current, required));
    }

    private void HandleLevelUp(int newLevel)
    {
        if (levelText != null)
            levelText.text = $"LEVEL {newLevel}";
    }

    private IEnumerator AnimateProgressRoutine(int targetCurrent, int targetRequired)
    {
        if (experienceSlider == null || playerExperience == null)
            yield break;

        int targetLevel = playerExperience.CurrentLevel;
        if (targetLevel > displayedLevel)
        {
            yield return AnimateSlider(experienceSlider.maxValue);
            yield return PulseBar();

            experienceSlider.maxValue = Mathf.Max(1, targetRequired);
            experienceSlider.value = 0f;
            UpdateExperienceText(0, targetRequired);
        }
        else
        {
            experienceSlider.maxValue = Mathf.Max(1, targetRequired);
        }

        displayedLevel = targetLevel;
        if (levelText != null)
            levelText.text = $"LEVEL {displayedLevel}";

        yield return AnimateSlider(Mathf.Clamp(targetCurrent, 0, Mathf.Max(1, targetRequired)));
        progressRoutine = null;
    }

    private IEnumerator AnimateSlider(float target)
    {
        float start = experienceSlider.value;
        float elapsed = 0f;

        while (elapsed < fillDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(elapsed / fillDuration);
            t = 1f - Mathf.Pow(1f - t, 3f);
            experienceSlider.value = Mathf.Lerp(start, target, t);
            UpdateExperienceText(
                Mathf.RoundToInt(experienceSlider.value),
                Mathf.RoundToInt(experienceSlider.maxValue));
            yield return null;
        }

        experienceSlider.value = target;
        UpdateExperienceText(
            Mathf.RoundToInt(experienceSlider.value),
            Mathf.RoundToInt(experienceSlider.maxValue));
    }

    private IEnumerator PulseBar()
    {
        if (barRoot == null)
            yield break;

        float elapsed = 0f;
        while (elapsed < pulseDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            float wave = Mathf.Sin(Mathf.Clamp01(elapsed / pulseDuration) * Mathf.PI);
            barRoot.localScale = baseScale * Mathf.Lerp(1f, pulseScale, wave);
            yield return null;
        }

        barRoot.localScale = baseScale;
    }

    private IEnumerator ShowGainRoutine(int gained)
    {
        if (gainText == null || gainTextGroup == null)
            yield break;

        gainText.text = $"+{gained} EXP";
        float elapsed = 0f;

        while (elapsed < gainTextDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(elapsed / gainTextDuration);
            gainTextGroup.alpha = Mathf.Sin(t * Mathf.PI);
            yield return null;
        }

        gainTextGroup.alpha = 0f;
        gainRoutine = null;
    }

    private void RefreshImmediate()
    {
        if (playerExperience == null)
        {
            Debug.LogWarning("[AnimatedExperienceHUD] PlayerExperience was not found.", this);
            return;
        }

        int current = Mathf.Max(0, playerExperience.CurrentExperience);
        int required = Mathf.Max(1, playerExperience.ExperienceToNextLevel);
        displayedLevel = playerExperience.CurrentLevel;

        if (experienceSlider != null)
        {
            experienceSlider.minValue = 0f;
            experienceSlider.maxValue = required;
            experienceSlider.value = Mathf.Clamp(current, 0, required);
            experienceSlider.wholeNumbers = false;
            experienceSlider.interactable = false;
        }

        UpdateExperienceText(current, required);
        if (levelText != null)
            levelText.text = $"LEVEL {displayedLevel}";
    }

    private void UpdateExperienceText(int current, int required)
    {
        if (experienceText != null)
            experienceText.text = $"{current} / {required} EXP";
    }

    private void ApplyStyle()
    {
        if (backgroundImage != null)
            backgroundImage.color = backgroundColor;
        if (fillImage != null)
            fillImage.color = fillColor;
        if (frameImage != null)
            frameImage.color = frameColor;
        if (levelText != null)
            levelText.color = accentColor;
        if (experienceText != null)
            experienceText.color = Color.white;
        if (gainText != null)
            gainText.color = accentColor;
        if (gainTextGroup != null)
            gainTextGroup.alpha = 0f;
    }
}
