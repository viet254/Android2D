using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(Animator))]
public class PlayerController : MonoBehaviour
{
    [Header("Movement")]
    public float moveSpeed = 5f;
    public float jumpForce = 12f;

    [Header("Ground Check")]
    public Transform groundCheck;
    public float groundCheckRadius = 0.15f;
    public LayerMask groundLayer;

    [Header("Combat")]
    private bool isAttacking = false;

    private Rigidbody2D rb;
    private Animator anim;
    private bool isGrounded;
    private float moveInput;
    private bool facingRight = true;

    // Tham chiếu PlayerStats (cùng GameObject)
    private PlayerStats stats;

    // Khoá mọi hành động khi chết
    private bool isDead = false;

    // ---- Khởi tạo ----

    void Start()
    {
        rb    = GetComponent<Rigidbody2D>();
        anim  = GetComponent<Animator>();
        stats = GetComponent<PlayerStats>();
    }

    void Update()
    {
        if (isDead) return;

        CheckGround();

#if UNITY_EDITOR || UNITY_STANDALONE
        ReadKeyboardForTesting();
#endif

        ApplyMovement();
        UpdateAnimatorParams();
    }

    void CheckGround()
    {
        isGrounded = groundCheck != null &&
            Physics2D.OverlapCircle(groundCheck.position, groundCheckRadius, groundLayer);
    }

#if UNITY_EDITOR || UNITY_STANDALONE
    void ReadKeyboardForTesting()
    {
        if (Keyboard.current == null) return;

        float input = 0f;
        if (Keyboard.current.aKey.isPressed || Keyboard.current.leftArrowKey.isPressed)  input -= 1f;
        if (Keyboard.current.dKey.isPressed || Keyboard.current.rightArrowKey.isPressed) input += 1f;
        Move(input);

        if (Keyboard.current.spaceKey.wasPressedThisFrame) Jump();

        if (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame) Attack();
    }
#endif

    // ---- Các hàm public gọi từ UI button / keyboard ----

    /// <summary>
    /// Gọi khi BẮT ĐẦU nhấn nút trái/phải (direction = -1 hoặc +1).
    /// Gọi với direction = 0 khi THẢ nút (dừng lại).
    /// </summary>
    public void Move(float direction)
    {
        if (isDead) return;

        if (isAttacking && isGrounded)
            moveInput = 0f;
        else
            moveInput = Mathf.Clamp(direction, -1f, 1f);
    }

    public void Jump()
    {
        if (isDead) return;
        if (!isGrounded || isAttacking) return;

        // Tiêu ST – không nhảy nếu không đủ
        if (stats != null && !stats.TrySpendSTForJump()) return;

        rb.linearVelocity = new Vector2(rb.linearVelocity.x, jumpForce);
    }

    public void Attack()
    {
        if (isDead) return;
        if (isAttacking) return;

        // Tiêu ST – không đánh nếu không đủ
        if (stats != null && !stats.TrySpendSTForAttack()) return;

        isAttacking = true;
        anim.SetInteger("isAttack", 1);
    }

    /// <summary>
    /// Gọi bằng Animation Event gần cuối clip PlayerCombo1.
    /// </summary>
    public void OnAttackAnimationEnd()
    {
        isAttacking = false;
        anim.SetInteger("isAttack", 0);
    }

    /// <summary>
    /// Gây sát thương cho nhân vật. Gọi từ Enemy / bẫy.
    /// Nếu có PlayerStats thì uỷ quyền xử lý HP cho nó.
    /// </summary>
    public void TakeDamage(float amount = 10f)
    {
        if (isDead) return;

        OnDamaged();

        if (stats != null)
            stats.TakeDamage(amount);
    }

    public void OnDamaged()
    {
        if (isDead) return;
        if (isAttacking)
        {
            isAttacking = false;
            anim.SetInteger("isAttack", 0);
        }
        anim.SetTrigger("isHurt");
    }

    // ---- Gọi bởi PlayerStats ----

    /// <summary>Khoá nhân vật, phát animation chết.</summary>
    public void OnDie()
    {
        isDead      = true;
        isAttacking = false;
        moveInput   = 0f;

        // Dừng hoàn toàn
        rb.linearVelocity = Vector2.zero;
        rb.bodyType       = RigidbodyType2D.Kinematic;

        // Reset các param animation rồi bật Hurt (dùng làm anim chết)
        anim.SetBool("isRunning", false);
        anim.SetBool("isJumping", false);
        anim.SetBool("isFall",    false);
        anim.SetInteger("isAttack", 0);
        anim.SetTrigger("isHurt");   // animation PlayerHurt
    }

    /// <summary>Mở khoá nhân vật sau khi hồi sinh.</summary>
    public void OnRespawn()
    {
        isDead    = false;
        rb.bodyType = RigidbodyType2D.Dynamic;
        anim.ResetTrigger("isHurt");
    }

    // ---- Nội bộ ----

    void ApplyMovement()
    {
        rb.linearVelocity = new Vector2(moveInput * moveSpeed, rb.linearVelocity.y);

        if (moveInput > 0f && !facingRight) Flip();
        else if (moveInput < 0f && facingRight) Flip();
    }

    void Flip()
    {
        facingRight = !facingRight;
        Vector3 scale = transform.localScale;
        scale.x *= -1f;
        transform.localScale = scale;
    }

    void UpdateAnimatorParams()
    {
        anim.SetBool("isRunning", isGrounded && Mathf.Abs(moveInput) > 0.01f);
        anim.SetBool("isJumping", !isGrounded && rb.linearVelocity.y >  0.01f);
        anim.SetBool("isFall",    !isGrounded && rb.linearVelocity.y < -0.01f);
    }

    void OnDrawGizmosSelected()
    {
        if (groundCheck != null)
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(groundCheck.position, groundCheckRadius);
        }
    }
}