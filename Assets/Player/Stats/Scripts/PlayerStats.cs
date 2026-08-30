using System.Collections;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Quản lý HP, MP, ST của nhân vật.
/// Gán script này lên cùng GameObject với PlayerController.
/// Kéo các Image (Filled) tương ứng vào Inspector.
/// </summary>
public class PlayerStats : MonoBehaviour
{
    // ────────────────────────────────────────────────────────────────
    //  Chỉ số cơ bản
    // ────────────────────────────────────────────────────────────────
    [Header("HP (máu)")]
    public float maxHP = 100f;
    public float currentHP;

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
    private PlayerController _controller;
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
        _spawnPoint = transform.position;

        currentHP  = maxHP;
        currentMP  = maxMP;   // bug fix: was maxHP
        currentST  = maxST;
        _displayHP = maxHP;

        if (redBar != null)
            _redBarOriginalScale = redBar.transform.localScale;
    }

    // ────────────────────────────────────────────────────────────────
    //  Update
    // ────────────────────────────────────────────────────────────────
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

        currentHP = Mathf.Clamp(currentHP - amount, 0f, maxHP);

        // Pulse animation trên orb khi mất máu
        if (redBar != null)
            StartCoroutine(PulseOrb());

        if (currentHP <= 0f)
            Die();
    }

    public void HealHP(float amount)
    {
        if (IsDead) return;
        currentHP = Mathf.Min(currentHP + amount, maxHP);
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
        if (redBar    != null) redBar.fillAmount    = _displayHP / maxHP;
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

        currentHP  = maxHP;
        currentMP  = maxMP;
        currentST  = maxST;
        _displayHP = maxHP;

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

