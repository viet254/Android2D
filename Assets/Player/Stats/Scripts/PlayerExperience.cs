using System;
using UnityEngine;

/// <summary>
/// Hệ thống XP và Level Up cho Player.
///
/// Gắn script này lên: Player GameObject (cùng với PlayerController, PlayerStats).
///
/// Cách dùng:
///   - Khi Orc chết → ExperienceReward gọi playerExp.AddExperience(amount).
///   - Subscribe vào OnXPGained / OnLevelUp để cập nhật UI sau này.
///
/// Công thức XP:
///   xpToNextLevel = baseXP * xpMultiplier ^ (currentLevel - 1)
///   Level 1→2: 100 XP  |  Level 2→3: 150 XP  |  Level 3→4: 225 XP
/// </summary>
public class PlayerExperience : MonoBehaviour
{
    // ─────────────────────────────────────────────────────────────
    //  INSPECTOR
    // ─────────────────────────────────────────────────────────────

    [Header("XP Formula")]
    [Tooltip("XP cần để lên từ Level 1 lên Level 2.")]
    [SerializeField] private int baseXP = 100;

    [Tooltip("Hệ số nhân XP mỗi level. 1.5 = cần thêm 50% XP mỗi cấp.")]
    [SerializeField] private float xpMultiplier = 1.5f;

    // ─────────────────────────────────────────────────────────────
    //  RUNTIME STATE (có thể xem trong Inspector khi Play Mode)
    // ─────────────────────────────────────────────────────────────

    [Header("── Runtime ─────────────────────────────")]
    [SerializeField] private int currentXP    = 0;
    [SerializeField] private int currentLevel = 1;
    [SerializeField] private int xpToNextLevel;

    // ─────────────────────────────────────────────────────────────
    //  PROPERTIES
    // ─────────────────────────────────────────────────────────────

    public int CurrentXP     => currentXP;
    public int CurrentLevel  => currentLevel;
    public int XPToNextLevel => xpToNextLevel;

    // ─────────────────────────────────────────────────────────────
    //  EVENTS
    // ─────────────────────────────────────────────────────────────

    /// <summary>
    /// Gọi khi nhận XP.
    /// Tham số: (lượng XP nhận, XP hiện tại, XP cần để lên level).
    /// Dùng để cập nhật thanh XP trên UI.
    /// </summary>
    public event Action<int, int, int> OnXPGained;

    /// <summary>
    /// Gọi khi Level Up.
    /// Tham số: level mới.
    /// Dùng để hiện hiệu ứng level up, tăng stat,...
    /// </summary>
    public event Action<int> OnLevelUp;

    // ─────────────────────────────────────────────────────────────
    //  LIFECYCLE
    // ─────────────────────────────────────────────────────────────

    void Awake()
    {
        // Tính XP cần cho level đầu tiên
        xpToNextLevel = CalculateXPRequired(currentLevel);
    }

    // ─────────────────────────────────────────────────────────────
    //  PUBLIC API
    // ─────────────────────────────────────────────────────────────

    /// <summary>
    /// Cộng XP cho Player. Gọi từ ExperienceReward khi enemy chết.
    /// Tự động xử lý Level Up nếu đủ XP (kể cả lên nhiều level liên tiếp).
    /// </summary>
    /// <param name="amount">Lượng XP thêm vào. Phải > 0.</param>
    public void AddExperience(int amount)
    {
        if (amount <= 0) return;

        currentXP += amount;

        // Thông báo UI cập nhật thanh XP
        OnXPGained?.Invoke(amount, currentXP, xpToNextLevel);

        Debug.Log($"[XP] +{amount} XP | Level {currentLevel} | {currentXP} / {xpToNextLevel} XP");

        // Vòng lặp để xử lý lên nhiều level liên tiếp trong 1 lần nhận XP
        while (currentXP >= xpToNextLevel)
        {
            currentXP    -= xpToNextLevel;   // Giữ XP thừa cho level tiếp
            currentLevel += 1;
            xpToNextLevel = CalculateXPRequired(currentLevel);

            Debug.Log($"[XP] ★ LEVEL UP! → Level {currentLevel} | XP cần tiếp: {xpToNextLevel}");

            // Thông báo: mở rộng sau này tăng stat, hiệu ứng,...
            OnLevelUp?.Invoke(currentLevel);
        }
    }

    // ─────────────────────────────────────────────────────────────
    //  PRIVATE
    // ─────────────────────────────────────────────────────────────

    /// <summary>
    /// Tính XP cần để lên từ level này lên level tiếp theo.
    /// Công thức: baseXP * xpMultiplier ^ (level - 1)
    /// </summary>
    private int CalculateXPRequired(int level)
    {
        return Mathf.RoundToInt(baseXP * Mathf.Pow(xpMultiplier, level - 1));
    }
}
