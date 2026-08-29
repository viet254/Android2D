using UnityEngine;

/// <summary>
/// Xử lý tấn công của Player — hoạt động với MỌI Enemy có component Health.cs.
///
/// Gắn script này lên: Player GameObject.
///
/// Cách dùng:
///   1. Gắn PlayerAttack.cs vào Player GameObject.
///   2. Set Enemy Layer vào enemyLayer trong Inspector.
///   3. Thêm Animation Event vào clip attack:
///      - Frame HIT: gọi DealPlayerDamage()
///      - Frame CUỐI: gọi ResetAttackFlag()
///
/// Mở rộng:
///   Bất kỳ Enemy nào (Orc, Skeleton, Boss...) chỉ cần có Health.cs là bị hit
///   mà không cần sửa PlayerAttack.cs.
/// </summary>
public class PlayerAttack : MonoBehaviour
{
    // ─────────────────────────────────────────────────────────────
    //  INSPECTOR
    // ─────────────────────────────────────────────────────────────

    [Header("Attack Settings")]
    [Tooltip("Lượng damage gây cho Enemy mỗi đòn đánh.")]
    [SerializeField] private int attackDamage = 15;

    [Tooltip("Bán kính vùng tấn công (hitbox hình tròn).")]
    [SerializeField] private float attackRadius = 1f;

    [Tooltip("Offset của hitbox so với tâm Player.\n" +
             "X = 0.6 tức là hitbox nằm phía trước Player 0.6 đơn vị.")]
    [SerializeField] private Vector2 attackOffset = new Vector2(0.6f, 0f);

    [Tooltip("Layer của Enemy. Chỉ tấn công các collider thuộc layer này.\n" +
             "Thiết lập trong: Edit → Project Settings → Tags and Layers.")]
    [SerializeField] private LayerMask enemyLayer;

    // ─────────────────────────────────────────────────────────────
    //  PRIVATE
    // ─────────────────────────────────────────────────────────────

    // Guard chống multi-hit: mỗi lần swing chỉ gây damage đúng 1 lần
    private bool hasDealtDamage = false;

    // ─────────────────────────────────────────────────────────────
    //  ANIMATION EVENTS — gọi từ clip attack của Player
    // ─────────────────────────────────────────────────────────────

    /// <summary>
    /// [Animation Event] Gọi tại frame Player vung kiếm / đấm.
    /// Phát hiện tất cả Enemy trong vùng tấn công và gây damage.
    /// Hoạt động với mọi Enemy có Health.cs — không cần biết là Orc hay Skeleton.
    /// </summary>
    public void DealPlayerDamage()
    {
        // Chỉ gây damage 1 lần mỗi animation swing
        if (hasDealtDamage) return;

        // Tính vị trí hitbox dựa theo hướng Player đang nhìn
        // localScale.x > 0 = nhìn phải, < 0 = nhìn trái (theo cách Flip của PlayerController)
        float facingDir  = transform.localScale.x > 0 ? 1f : -1f;
        Vector2 hitPoint = (Vector2)transform.position
                           + new Vector2(attackOffset.x * facingDir, attackOffset.y);

        // Tìm TẤT CẢ Collider trong vùng tấn công (có thể hit nhiều enemy cùng lúc)
        Collider2D[] hits = Physics2D.OverlapCircleAll(hitPoint, attackRadius, enemyLayer);

        foreach (Collider2D hit in hits)
        {
            // Lấy component Health — hoạt động với MỌI Enemy có Health.cs
            Health enemyHealth = hit.GetComponent<Health>();
            if (enemyHealth != null && !enemyHealth.IsDead)
            {
                enemyHealth.TakeDamage(attackDamage);
                Debug.Log($"[PlayerAttack] Đánh trúng {hit.gameObject.name} | -{attackDamage} HP");
            }
        }

        // Đánh dấu đã gây damage để không hit lại trong cùng animation
        hasDealtDamage = true;
    }

    /// <summary>
    /// [Animation Event] Gọi ở CUỐI clip attack.
    /// Reset flag để lần attack tiếp theo hoạt động bình thường.
    /// </summary>
    public void ResetAttackFlag()
    {
        hasDealtDamage = false;
    }

    // ─────────────────────────────────────────────────────────────
    //  GIZMOS — hiện hitbox trong Scene View để dễ điều chỉnh
    // ─────────────────────────────────────────────────────────────

    void OnDrawGizmosSelected()
    {
        float   facingDir = transform.localScale.x > 0 ? 1f : -1f;
        Vector2 hitPoint  = (Vector2)transform.position
                            + new Vector2(attackOffset.x * facingDir, attackOffset.y);

        // Vòng tròn xanh lá = vùng tấn công của Player
        Gizmos.color = new Color(0f, 1f, 0f, 0.35f);
        Gizmos.DrawSphere(hitPoint, attackRadius);
        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(hitPoint, attackRadius);
    }
}
