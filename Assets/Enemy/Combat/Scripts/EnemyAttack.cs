using UnityEngine;

/// <summary>
/// Xử lý tấn công của Enemy — Orc gây damage cho Player.
///
/// Gắn script này lên: Enemy GameObject (cùng với EnemyAI.cs, Health.cs).
///
/// Cách dùng:
///   1. Gắn script vào Enemy GameObject.
///   2. Set Player Layer vào playerLayer trong Inspector.
///   3. Thêm Animation Event vào clip OrcAttack1 VÀ OrcAttack2:
///      - Frame GÕ TRÚNG: gọi DealDamage()
///      - Frame CUỐI: gọi ResetDamageFlag()
///
/// Mở rộng:
///   Script này có thể dùng lại cho Skeleton, Dragon... chỉ cần gắn và chỉnh attackDamage.
/// </summary>
public class EnemyAttack : MonoBehaviour
{
    // ─────────────────────────────────────────────────────────────
    //  INSPECTOR
    // ─────────────────────────────────────────────────────────────

    [Header("Attack Settings")]
    [Tooltip("Lượng damage gây cho Player mỗi đòn tấn công.")]
    [SerializeField] private int attackDamage = 10;

    [Tooltip("Bán kính hitbox tấn công.")]
    [SerializeField] private float attackRadius = 0.8f;

    [Tooltip("Offset hitbox so với tâm enemy.\n" +
             "Tăng X để hitbox nằm xa hơn về phía trước.")]
    [SerializeField] private Vector2 attackOffset = new Vector2(0.5f, 0f);

    [Tooltip("Layer của Player. Chỉ gây damage cho collider thuộc layer này.")]
    [SerializeField] private LayerMask playerLayer;

    // ─────────────────────────────────────────────────────────────
    //  PRIVATE
    // ─────────────────────────────────────────────────────────────

    // Guard chống multi-hit trong cùng 1 animation
    private bool hasDealtDamage = false;

    public void Configure(EnemyData data)
    {
        if (data != null) attackDamage = data.Damage;
    }

    // ─────────────────────────────────────────────────────────────
    //  ANIMATION EVENTS — gọi từ clip OrcAttack1 / OrcAttack2
    // ─────────────────────────────────────────────────────────────

    /// <summary>
    /// [Animation Event] Gọi tại frame Orc vung vũ khí chạm mục tiêu.
    /// Gây damage cho Player đúng 1 lần mỗi animation.
    /// </summary>
    public void DealDamage()
    {
        // Không gây damage nếu script đã bị tắt (lúc enemy chết)
        if (!enabled) return;

        // Chỉ gây damage 1 lần mỗi animation swing
        if (hasDealtDamage) return;

        // Tính vị trí hitbox dựa theo hướng enemy đang nhìn
        // localScale.x > 0 = nhìn phải, < 0 = nhìn trái
        float   facingDir = transform.localScale.x > 0 ? 1f : -1f;
        Vector2 hitPoint  = (Vector2)transform.position
                            + new Vector2(attackOffset.x * facingDir, attackOffset.y);

        // Tìm Player trong vùng hitbox
        Collider2D hit = Physics2D.OverlapCircle(hitPoint, attackRadius, playerLayer);

        if (hit != null)
        {
            IDamageable damageable = hit.GetComponentInParent(typeof(IDamageable)) as IDamageable;
            if (damageable != null)
            {
                damageable.TakeDamage(new DamageInfo(attackDamage, DamageType.Physical, gameObject));
                hasDealtDamage = true;
                Debug.Log($"[EnemyAttack] {gameObject.name} gây {attackDamage} damage cho Player.");
            }

        }
    }

    /// <summary>
    /// [Animation Event] Gọi ở CUỐI clip OrcAttack1 / OrcAttack2.
    /// Reset flag để lần attack tiếp theo có thể gây damage lại.
    /// </summary>
    public void ResetDamageFlag()
    {
        hasDealtDamage = false;
    }

    // ─────────────────────────────────────────────────────────────
    //  GIZMOS — hiện hitbox trong Scene View
    // ─────────────────────────────────────────────────────────────

    void OnDrawGizmosSelected()
    {
        float   facingDir = transform.localScale.x > 0 ? 1f : -1f;
        Vector2 hitPoint  = (Vector2)transform.position
                            + new Vector2(attackOffset.x * facingDir, attackOffset.y);

        // Vòng tròn đỏ trong suốt = hitbox tấn công của enemy
        Gizmos.color = new Color(1f, 0f, 0f, 0.35f);
        Gizmos.DrawSphere(hitPoint, attackRadius);
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(hitPoint, attackRadius);
    }
}
