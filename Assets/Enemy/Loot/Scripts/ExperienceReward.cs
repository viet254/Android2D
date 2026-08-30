using UnityEngine;

/// <summary>
/// Trao XP cho Player khi Enemy chết.
/// Gắn script này lên Enemy GameObject (cùng với Health.cs).
///
/// Cách hoạt động:
///   - Tự động subscribe vào Health.OnDied.
///   - Khi HP về 0 → tìm Player có tag "Player" → gọi PlayerExperience.AddExperience().
///   - Có guard chỉ trao XP đúng 1 lần dù OnDied bị gọi nhiều lần.
///
/// Mở rộng sau này:
///   Mỗi loại Enemy (Orc, Skeleton, Boss) chỉ cần đổi giá trị experienceReward
///   trong Inspector — không cần sửa code.
/// </summary>
[RequireComponent(typeof(Health))]
public class ExperienceReward : MonoBehaviour
{
    // ─────────────────────────────────────────────────────────────
    //  INSPECTOR
    // ─────────────────────────────────────────────────────────────

    [Header("XP Reward")]
    [Tooltip("Lượng XP trao cho Player khi enemy này chết.\nOrc thường = 20, Elite = 50, Boss = 200,...")]
    [SerializeField] private int experienceReward = 20;

    // ─────────────────────────────────────────────────────────────
    //  PRIVATE
    // ─────────────────────────────────────────────────────────────

    private Health health;

    // Guard: chỉ trao XP đúng 1 lần
    private bool hasRewardedXP = false;

    // ─────────────────────────────────────────────────────────────
    //  LIFECYCLE
    // ─────────────────────────────────────────────────────────────

    void Awake()
    {
        health = GetComponent<Health>();
    }

    void OnEnable()
    {
        // Đăng ký nhận thông báo khi enemy chết
        health.OnDied += GiveExperienceToPlayer;
    }

    void OnDisable()
    {
        // Hủy đăng ký để tránh memory leak
        health.OnDied -= GiveExperienceToPlayer;
    }

    // ─────────────────────────────────────────────────────────────
    //  PRIVATE METHODS
    // ─────────────────────────────────────────────────────────────

    private void GiveExperienceToPlayer()
    {
        // Chỉ trao XP 1 lần
        if (hasRewardedXP) return;
        hasRewardedXP = true;

        // Tìm Player trong Scene bằng Tag
        // Yêu cầu: Player GameObject phải có Tag = "Player"
        GameObject playerObj = GameObject.FindWithTag("Player");
        if (playerObj == null)
        {
            Debug.LogWarning($"[ExperienceReward] Không tìm thấy Player! " +
                             $"Kiểm tra Tag của Player GameObject có phải 'Player' không.");
            return;
        }

        // Lấy component PlayerExperience
        PlayerExperience playerExp = playerObj.GetComponent<PlayerExperience>();
        if (playerExp == null)
        {
            Debug.LogWarning($"[ExperienceReward] Player không có component PlayerExperience! " +
                             $"Hãy gắn PlayerExperience.cs vào Player GameObject.");
            return;
        }

        // Trao XP!
        playerExp.AddExperience(experienceReward);
        Debug.Log($"[ExperienceReward] {gameObject.name} trao {experienceReward} XP cho Player.");
    }
}
