using System;
using UnityEngine;

/// <summary>
/// Quản lý HP cho Enemy. Dùng chung cho mọi loại Enemy (Orc, Skeleton, Boss...).
/// KHÔNG dùng cho Player — Player đã có PlayerStats.cs riêng.
///
/// Cách dùng:
///   1. Gắn Health.cs lên Enemy GameObject.
///   2. Các script khác subscribe vào OnDamaged / OnDied để phản ứng.
///   3. Gây damage bằng cách gọi health.TakeDamage(amount).
/// </summary>
public class Health : MonoBehaviour
{
    // ─────────────────────────────────────────────────────────────
    //  INSPECTOR
    // ─────────────────────────────────────────────────────────────

    [Header("HP Settings")]
    [Tooltip("HP tối đa của enemy này.")]
    [SerializeField] private int maxHealth = 50;

    // ─────────────────────────────────────────────────────────────
    //  PROPERTIES (đọc từ bên ngoài, không ghi trực tiếp)
    // ─────────────────────────────────────────────────────────────

    /// <summary>HP hiện tại.</summary>
    public int CurrentHealth { get; private set; }

    /// <summary>HP tối đa.</summary>
    public int MaxHealth => maxHealth;

    /// <summary>True nếu enemy đã chết (HP <= 0).</summary>
    public bool IsDead { get; private set; }

    // ─────────────────────────────────────────────────────────────
    //  EVENTS
    //  Các script khác đăng ký nhận thông báo qua event thay vì
    //  dùng GetComponent liên tục → hiệu năng tốt hơn.
    // ─────────────────────────────────────────────────────────────

    /// <summary>
    /// Gọi khi nhận damage thành công.
    /// Tham số: lượng damage đã nhận (int).
    /// </summary>
    public event Action<int> OnDamaged;

    /// <summary>
    /// Gọi đúng 1 lần khi HP xuống 0.
    /// EnemyAI và ExperienceReward đều subscribe vào event này.
    /// </summary>
    public event Action OnDied;

    // ─────────────────────────────────────────────────────────────
    //  LIFECYCLE
    // ─────────────────────────────────────────────────────────────

    void Awake()
    {
        // Khởi tạo HP bằng maxHealth lúc bắt đầu
        CurrentHealth = maxHealth;
        IsDead        = false;
    }

    // ─────────────────────────────────────────────────────────────
    //  PUBLIC API
    // ─────────────────────────────────────────────────────────────

    /// <summary>
    /// Gây damage cho enemy.
    /// Gọi từ PlayerAttack.cs hoặc bất kỳ nguồn nào (bẫy, skill...).
    /// </summary>
    /// <param name="damage">Lượng damage. Phải > 0 mới có tác dụng.</param>
    public void TakeDamage(int damage)
    {
        // Không nhận damage nếu đã chết
        if (IsDead) return;
        // Bỏ qua damage <= 0
        if (damage <= 0) return;

        // Trừ HP, không xuống dưới 0
        CurrentHealth = Mathf.Max(CurrentHealth - damage, 0);

        // Thông báo cho các subscriber (EnemyAI sẽ bật animation Hurt)
        OnDamaged?.Invoke(damage);

        // Kiểm tra chết
        if (CurrentHealth <= 0)
            Die();
    }

    // ─────────────────────────────────────────────────────────────
    //  PRIVATE
    // ─────────────────────────────────────────────────────────────

    private void Die()
    {
        // Guard: chỉ gọi 1 lần dù TakeDamage bị gọi nhiều lần cùng frame
        if (IsDead) return;

        IsDead = true;

        // Thông báo: EnemyAI bật animation Death, ExperienceReward trao XP
        OnDied?.Invoke();
    }
}
