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
public class Health : MonoBehaviour, IDamageable
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
    public event Action<DamageInfo> OnDamaged;
    public event Action<int> OnDamagedAmount;

    /// <summary>
    /// Gọi đúng 1 lần khi HP xuống 0.
    /// EnemyAI và ExperienceReward đều subscribe vào event này.
    /// </summary>
    public event Action OnDied;

    public event Action<int, int> OnHealthChanged;
    public event Action OnDeath;

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
        TakeDamage(new DamageInfo(damage, DamageType.Physical, null));
    }

    // ─────────────────────────────────────────────────────────────
    //  PRIVATE
    // ─────────────────────────────────────────────────────────────

    public void TakeDamage(DamageInfo damageInfo)
    {
        if (IsDead || damageInfo.Amount <= 0) return;

        CurrentHealth = Mathf.Max(CurrentHealth - damageInfo.Amount, 0);
        OnDamaged?.Invoke(damageInfo);
        OnDamagedAmount?.Invoke(damageInfo.Amount);
        OnHealthChanged?.Invoke(CurrentHealth, maxHealth);

        if (CurrentHealth <= 0) Die();
    }

    public bool Heal(int amount)
    {
        if (IsDead || amount <= 0 || CurrentHealth >= maxHealth)
            return false;

        CurrentHealth = Mathf.Min(CurrentHealth + amount, maxHealth);
        OnHealthChanged?.Invoke(CurrentHealth, maxHealth);
        return true;
    }

    public bool RestoreState(int currentHealth, bool alive)
    {
        if (alive)
        {
            CurrentHealth = Mathf.Clamp(currentHealth, 1, maxHealth);
            IsDead = false;
        }
        else
        {
            CurrentHealth = 0;
            IsDead = true;
        }

        OnHealthChanged?.Invoke(CurrentHealth, maxHealth);
        return true;
    }

    public void ConfigureMaxHealth(int value, bool refill = true)
    {
        maxHealth = Mathf.Max(1, value);
        if (refill)
        {
            CurrentHealth = maxHealth;
            IsDead = false;
            OnHealthChanged?.Invoke(CurrentHealth, maxHealth);
        }
    }

    public void ResetHealth() => ConfigureMaxHealth(maxHealth);

    private void Die()
    {
        // Guard: chỉ gọi 1 lần dù TakeDamage bị gọi nhiều lần cùng frame
        if (IsDead) return;

        IsDead = true;

        // Thông báo: EnemyAI bật animation Death, ExperienceReward trao XP
        OnDied?.Invoke();
        OnDeath?.Invoke();
    }
}
