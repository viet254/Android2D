using System.Collections;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Quản lý HP, MP, ST của nhân vật.
/// Gán script này lên cùng GameObject với PlayerController.
/// Kéo các Image (Filled) tương ứng vào Inspector.
/// </summary>
[RequireComponent(typeof(Health))]
public class PlayerStats : MonoBehaviour, IDamageable
{
    // ────────────────────────────────────────────────────────────────
    //  Chỉ số cơ bản
    // ────────────────────────────────────────────────────────────────
    [Header("HP (máu)")]
    public float maxHP = 100f;
    public float currentHP => _health != null ? _health.CurrentHealth : maxHP;

    [Header("MP (mana)")]
    public float maxMP = 100f;
    public float currentMP;

    [Header("ST (stamina)")]
    public float maxST = 100f;
    public float currentST;

    [Header("ST tiêu hao")]
    public float jumpSTCost   = 15f;   // ST mỗi lần nhảy
    public float attackSTCost = 10f;   // ST mỗi lần đánh

    [Header("ST hồi phục")]
    public float stRegenRate = 8f;     // ST/giây khi không hành động

    // ────────────────────────────────────────────────────────────────
    //  HUD Bars
    // ────────────────────────────────────────────────────────────────
    [Header("HUD Bars")]
    public Image redBar;     // HP  – Filled, Radial360
    public Image blueBar;    // MP  – Filled, Horizontal
    public Image yellowBar;  // ST  – Filled, Horizontal

    [Header("HP Orb Animation")]
    [Tooltip("Tốc độ orb HP mượt chạy theo (fill lerp/giây)")]
    public float hpDrainSpeed = 3f;
    [Tooltip("Scale phình ra khi bị đánh")]
    public float pulsePeak    = 1.25f;
    [Tooltip("Thời gian 1 pulse (giây)")]
    public float pulseDuration = 0.35f;

    // ────────────────────────────────────────────────────────────────
    //  Hồi sinh
    // ────────────────────────────────────────────────────────────────
    [Header("Hồi sinh")]
    public float respawnDelay = 5f;    // giây chờ sau khi chết
    private Vector3 _spawnPoint;

    // ────────────────────────────────────────────────────────────────
    //  Nội bộ
    // ────────────────────────────────────────────────────────────────
    [Header("Combat Stats")]
    [SerializeField] private int attack = 15;
    [SerializeField] private int defense = 0;
    [SerializeField] private float moveSpeed = 5f;

    private PlayerController _controller;
    private Health _health;
    private Equipment _equipment;

    public float CurrentHP => currentHP;
    public float MaxHP => _health != null ? _health.MaxHealth : maxHP;
    public int BaseAttack => attack;
    public int Attack => GetAttackDamage();

    public int GetAttackDamage()
    {
        if (_equipment == null)
            _equipment = GetComponent<Equipment>();

        int weaponDamage = _equipment != null && _equipment.EquippedWeapon != null
            ? _equipment.EquippedWeapon.Damage
            : 0;

        return Mathf.Max(0, attack + weaponDamage);
    }

    public DamageType GetAttackDamageType()
    {
        if (_equipment == null)
            _equipment = GetComponent<Equipment>();

        return _equipment != null && _equipment.EquippedWeapon != null
            ? _equipment.EquippedWeapon.DamageType
            : DamageType.Physical;
    }
    public int Defense => defense;
    public float MoveSpeed => moveSpeed;
    public bool IsDead { get; private set; }

    // HP hiển thị trơn tru (lerp về currentHP)
    private float _displayHP;
    // Scale gốc của red bar để reset sau pulse
    private Vector3 _redBarOriginalScale;

    // ────────────────────────────────────────────────────────────────
    //  Khởi tạo
    // ────────────────────────────────────────────────────────────────
    void Awake()
    {
        _controller = GetComponent<PlayerController>();
        _health = GetComponent<Health>();
        _equipment = GetComponent<Equipment>();
        if (_health == null) _health = gameObject.AddComponent<Health>();
        _health.ConfigureMaxHealth(Mathf.RoundToInt(maxHP));
        _health.OnHealthChanged += HandleHealthChanged;
        _health.OnDied += Die;
        _spawnPoint = transform.position;
        currentMP  = maxMP;   // bug fix: was maxHP
        currentST  = maxST;
        _displayHP = _health.CurrentHealth;

        if (redBar != null)
            _redBarOriginalScale = redBar.transform.localScale;
    }

    // ────────────────────────────────────────────────────────────────
    //  Update
    // ────────────────────────────────────────────────────────────────
    private void HandleHealthChanged(int current, int maximum)
    {
        if (redBar != null && current < maximum) StartCoroutine(PulseOrb());
    }

    void Update()
    {
        if (IsDead) return;

        RegenerateST();
        SmoothHPDisplay();
        UpdateBars();
    }

    public bool TrySpendSTForJump()
    {
        if (IsDead || currentST < jumpSTCost) return false;
        currentST = Mathf.Max(currentST - jumpSTCost, 0f);
        return true;
    }

    public bool TrySpendSTForAttack()
    {
        if (IsDead || currentST < attackSTCost) return false;
        currentST = Mathf.Max(currentST - attackSTCost, 0f);
        return true;
    }

    /// <summary>Gây sát thương HP + kích hoạt hiệu ứng gợn sóng trên orb.</summary>
    public void TakeDamage(float amount)
    {
        if (IsDead) return;

        int reducedDamage = Mathf.Max(0, Mathf.RoundToInt(amount) - defense);
        _health.TakeDamage(new DamageInfo(reducedDamage, DamageType.Physical, null));
    }

    public void TakeDamage(DamageInfo damageInfo)
    {
        int reducedDamage = damageInfo.DamageType == DamageType.True
            ? damageInfo.Amount
            : Mathf.Max(0, damageInfo.Amount - defense);
        if (reducedDamage <= 0 || IsDead) return;
        if (_controller != null) _controller.OnDamaged();
        _health.TakeDamage(new DamageInfo(reducedDamage, damageInfo.DamageType, damageInfo.Source));
    }

    public void HealHP(float amount)
    {
        if (IsDead) return;
        _health.Heal(Mathf.RoundToInt(amount));
    }

    public void ChangeMP(float delta)
    {
        currentMP = Mathf.Clamp(currentMP + delta, 0f, maxMP);
    }

    // ────────────────────────────────────────────────────────────────
    //  Nội bộ – logic
    // ────────────────────────────────────────────────────────────────

    void RegenerateST()
    {
        if (currentST < maxST)
            currentST = Mathf.Min(currentST + stRegenRate * Time.deltaTime, maxST);
    }

    /// <summary>_displayHP lerp mượt về currentHP → drain chậm trên orb.</summary>
    void SmoothHPDisplay()
    {
        _displayHP = Mathf.Lerp(_displayHP, currentHP, hpDrainSpeed * Time.deltaTime);
    }

    void UpdateBars()
    {
        // Orb HP dùng _displayHP (lerp mượt), không nhảy đột ngột
        if (redBar    != null) redBar.fillAmount    = _displayHP / Mathf.Max(1, _health.MaxHealth);
        if (blueBar   != null) blueBar.fillAmount   = currentMP  / maxMP;
        if (yellowBar != null) yellowBar.fillAmount = currentST  / maxST;
    }

    void Die()
    {
        if (IsDead) return;
        IsDead = true;
        _controller.OnDie();
        StartCoroutine(RespawnRoutine());
    }

    IEnumerator RespawnRoutine()
    {
        yield return new WaitForSeconds(respawnDelay);
        Respawn();
    }

    void Respawn()
    {
        IsDead = false;

        _health.ResetHealth();
        currentMP  = maxMP;
        currentST  = maxST;
        _displayHP = _health.CurrentHealth;

        transform.position = _spawnPoint;
        _controller.OnRespawn();
        UpdateBars();
    }

    // ────────────────────────────────────────────────────────────────
    //  Hiệu ứng Pulse / Ripple cho orb HP
    // ────────────────────────────────────────────────────────────────

    /// <summary>
    /// Phình to → thu về: mô phỏng cảm giác "gợn sóng" khi mất máu.
    /// Scale: 1 → pulsePeak → 1 trong pulseDuration giây.
    /// </summary>
    IEnumerator PulseOrb()
    {
        if (redBar == null) yield break;

        Transform t    = redBar.transform;
        float     half = pulseDuration * 0.5f;
        float     timer;

        // Phình ra
        timer = 0f;
        while (timer < half)
        {
            timer += Time.deltaTime;
            float ratio = Mathf.Clamp01(timer / half);
            t.localScale = Vector3.Lerp(_redBarOriginalScale,
                                        _redBarOriginalScale * pulsePeak, ratio);
            yield return null;
        }

        // Thu về
        timer = 0f;
        while (timer < half)
        {
            timer += Time.deltaTime;
            float ratio = Mathf.Clamp01(timer / half);
            t.localScale = Vector3.Lerp(_redBarOriginalScale * pulsePeak,
                                        _redBarOriginalScale, ratio);
            yield return null;
        }

        t.localScale = _redBarOriginalScale;
    }
}

