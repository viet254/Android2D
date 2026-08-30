using System.Collections;
using UnityEngine;

/// <summary>
/// AI State Machine hoàn chỉnh cho Orc Enemy.
///
/// Gắn script này lên: Enemy GameObject.
/// Yêu cầu cùng GameObject phải có: Rigidbody2D, Animator, Health, EnemyAttack.
///
/// ╔══════════════════════════════════════════════════╗
/// ║  STATE MACHINE                                    ║
/// ║                                                   ║
/// ║  [Idle] ──timer──► [Patrol]                      ║
/// ║     ▲                  │                          ║
/// ║     │              detect player                  ║
/// ║     │                  ▼                          ║
/// ║     └────────────► [Chase] ──close──► [Attack]   ║
/// ║                        │                  │       ║
/// ║                    lost player         attack     ║
/// ║                        │                  │       ║
/// ║              ◄──── [Patrol]          [Hurt/Death] ║
/// ╚══════════════════════════════════════════════════╝
///
/// Animator Parameters cần tạo:
///   IsWalking    (Bool)    — Idle=false, Walk=true
///   AttackIndex  (Int)     — 0=Attack1, 1=Attack2
///   AttackTrigger (Trigger) — bắt đầu attack
///   HurtTrigger  (Trigger) — bắt đầu hurt
///   DeathTrigger (Trigger) — bắt đầu death
/// </summary>
[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(Animator))]
[RequireComponent(typeof(Health))]
public class EnemyAI : MonoBehaviour
{
    // ════════════════════════════════════════════════════════════
    //  INSPECTOR SETTINGS
    // ════════════════════════════════════════════════════════════

    [Header("── Detection ──────────────────────────────")]
    [Tooltip("Bán kính phát hiện Player (vòng tròn vàng trong Scene View).")]
    [SerializeField] private float detectionRange = 5f;

    [Tooltip("Layer của Player. Chỉ detect collider thuộc layer này.")]
    [SerializeField] private LayerMask playerLayer;

    [Header("── Patrol ──────────────────────────────────")]
    [Tooltip("Tốc độ di chuyển khi tuần tra (chậm).")]
    [SerializeField] private float patrolSpeed    = 1.5f;

    [Tooltip("Khoảng cách tuần tra tính từ vị trí spawn ban đầu.")]
    [SerializeField] private float patrolDistance = 3f;

    [Tooltip("Thời gian đứng yên (Idle) trước khi bắt đầu đi tuần.")]
    [SerializeField] private float idleWaitTime   = 2f;

    [Header("── Chase ───────────────────────────────────")]
    [Tooltip("Tốc độ đuổi theo Player (nhanh hơn patrol).")]
    [SerializeField] private float chaseSpeed = 3f;

    [Header("── Attack ──────────────────────────────────")]
    [Tooltip("Khoảng cách tối đa để bắt đầu tấn công (vòng tròn đỏ trong Scene View).")]
    [SerializeField] private float attackRange    = 1.2f;

    [Tooltip("Thời gian chờ giữa 2 lần tấn công liên tiếp (giây).")]
    [SerializeField] private float attackCooldown = 1.5f;

    [Header("── Hurt ────────────────────────────────────")]
    [Tooltip("Thời gian bất động sau khi nhận damage (giây).")]
    [SerializeField] private float hurtDuration = 0.5f;

    [Header("── Death ───────────────────────────────────")]
    [Tooltip("Tick nếu Animator có state OrcDeath.\nBỏ tick nếu không có → Orc đứng yên rồi biến mất.")]
    [SerializeField] private bool  hasDeathAnimation = false;

    [Tooltip("Thời gian chờ trước khi Destroy.\n" +
             "Có animation: đặt bằng độ dài clip OrcDeath.\n" +
             "Không có animation: 0.5~1 giây là đủ.")]
    [SerializeField] private float deathDestroyDelay = 1f;

    // ════════════════════════════════════════════════════════════
    //  STATE ENUM
    // ════════════════════════════════════════════════════════════

    private enum EnemyState { Idle, Patrol, Chase, Attack, Hurt, Death }
    private EnemyState currentState = EnemyState.Idle;

    // ════════════════════════════════════════════════════════════
    //  COMPONENTS
    // ════════════════════════════════════════════════════════════

    private Rigidbody2D  rb;
    private Animator     anim;
    private Health       health;
    private EnemyAttack  enemyAttack;
    private Collider2D   col;

    // ════════════════════════════════════════════════════════════
    //  ANIMATOR PARAMETER HASHES
    //  Dùng hash (int) thay vì string để tránh lỗi typo và tăng performance.
    // ════════════════════════════════════════════════════════════

    private static readonly int Hash_IsWalking     = Animator.StringToHash("IsWalking");
    private static readonly int Hash_AttackIndex   = Animator.StringToHash("AttackIndex");
    private static readonly int Hash_AttackTrigger = Animator.StringToHash("AttackTrigger");
    private static readonly int Hash_HurtTrigger   = Animator.StringToHash("HurtTrigger");
    private static readonly int Hash_DeathTrigger  = Animator.StringToHash("DeathTrigger");

    // ════════════════════════════════════════════════════════════
    //  RUNTIME VARIABLES
    // ════════════════════════════════════════════════════════════

    private Transform  playerTransform;         // Transform của Player khi phát hiện
    private Vector2    patrolOrigin;            // Vị trí spawn = tâm khu vực patrol
    private bool       patrolGoingRight = true; // Hướng patrol hiện tại
    private float      idleTimer        = 0f;   // Đếm ngược trước khi patrol
    private float      attackTimer      = 0f;   // Đếm ngược cooldown tấn công
    private bool       isHurting        = false; // Đang trong trạng thái hurt
    private bool       isDead           = false; // Đã chết, không xử lý AI nữa

    // Lưu coroutine để có thể dừng nếu cần
    private Coroutine  hurtCoroutine    = null;

    // ════════════════════════════════════════════════════════════
    //  LIFECYCLE
    // ════════════════════════════════════════════════════════════

    void Awake()
    {
        rb          = GetComponent<Rigidbody2D>();
        anim        = GetComponent<Animator>();
        health      = GetComponent<Health>();
        enemyAttack = GetComponent<EnemyAttack>();
        col         = GetComponent<Collider2D>();

        // Lưu vị trí spawn làm tâm patrol
        patrolOrigin = transform.position;
    }

    void Start()
    {
        // Bắt đầu ở trạng thái Idle với thời gian chờ đầy đủ
        idleTimer = idleWaitTime;
    }

    void OnEnable()
    {
        // Đăng ký lắng nghe event từ Health
        health.OnDamagedAmount += OnTakeDamage;
        health.OnDied    += OnDied;
    }

    void OnDisable()
    {
        // Hủy đăng ký khi script tắt để tránh memory leak
        health.OnDamagedAmount -= OnTakeDamage;
        health.OnDied    -= OnDied;
    }

    public void Configure(EnemyData data)
    {
        if (data == null) return;
        patrolSpeed = data.MoveSpeed * 0.5f;
        chaseSpeed = data.MoveSpeed;
        detectionRange = data.DetectionRange;
        attackRange = data.AttackRange;
        attackCooldown = data.AttackCooldown;
    }

    void Update()
    {
        // Không chạy AI nếu đã chết
        if (isDead) return;

        // Không chạy AI nếu đang trong trạng thái Hurt (để animation chạy xong)
        if (isHurting) return;

        // Đếm ngược cooldown tấn công mỗi frame
        attackTimer -= Time.deltaTime;

        // Chạy logic của state hiện tại
        switch (currentState)
        {
            case EnemyState.Idle:   UpdateIdle();   break;
            case EnemyState.Patrol: UpdatePatrol(); break;
            case EnemyState.Chase:  UpdateChase();  break;
            case EnemyState.Attack: UpdateAttack(); break;
            // Hurt và Death xử lý qua Coroutine + Event
        }
    }

    // ════════════════════════════════════════════════════════════
    //  STATE UPDATE METHODS
    // ════════════════════════════════════════════════════════════

    // ── IDLE ─────────────────────────────────────────────────────
    void UpdateIdle()
    {
        // Dừng di chuyển, bật animation Idle
        SetVelocityX(0f);
        anim.SetBool(Hash_IsWalking, false);

        // Ưu tiên: nếu phát hiện Player → Chase ngay lập tức
        if (TryDetectPlayer())
        {
            ChangeState(EnemyState.Chase);
            return;
        }

        // Đếm ngược: hết thời gian đứng yên → bắt đầu patrol
        idleTimer -= Time.deltaTime;
        if (idleTimer <= 0f)
            ChangeState(EnemyState.Patrol);
    }

    // ── PATROL ───────────────────────────────────────────────────
    void UpdatePatrol()
    {
        anim.SetBool(Hash_IsWalking, true);

        // Ưu tiên: phát hiện Player → Chase
        if (TryDetectPlayer())
        {
            ChangeState(EnemyState.Chase);
            return;
        }

        // Tính điểm đến: trái hoặc phải từ vị trí spawn
        float targetX = patrolGoingRight
            ? patrolOrigin.x + patrolDistance
            : patrolOrigin.x - patrolDistance;

        float dir = patrolGoingRight ? 1f : -1f;
        SetVelocityX(dir * patrolSpeed);
        FaceDirection(dir);

        // Đến điểm cuối → quay đầu → về Idle nghỉ
        if (Mathf.Abs(transform.position.x - targetX) < 0.15f)
        {
            patrolGoingRight = !patrolGoingRight;
            ChangeState(EnemyState.Idle);
        }
    }

    // ── CHASE ────────────────────────────────────────────────────
    void UpdateChase()
    {
        // Player đã biến mất khỏi bộ nhớ → về Patrol
        if (playerTransform == null)
        {
            ChangeState(EnemyState.Patrol);
            return;
        }

        float dist = Vector2.Distance(transform.position, playerTransform.position);

        // Player chạy ra ngoài vùng phát hiện → bỏ đuổi
        if (dist > detectionRange)
        {
            playerTransform = null;
            ChangeState(EnemyState.Patrol);
            return;
        }

        // Đủ gần để tấn công
        if (dist <= attackRange)
        {
            ChangeState(EnemyState.Attack);
            return;
        }

        // Đuổi theo Player
        anim.SetBool(Hash_IsWalking, true);
        float dir = playerTransform.position.x > transform.position.x ? 1f : -1f;
        SetVelocityX(dir * chaseSpeed);
        FaceDirection(dir);
    }

    // ── ATTACK ───────────────────────────────────────────────────
    void UpdateAttack()
    {
        // Dừng di chuyển khi tấn công
        SetVelocityX(0f);
        anim.SetBool(Hash_IsWalking, false);

        // Player mất dấu → về Patrol
        if (playerTransform == null)
        {
            ChangeState(EnemyState.Patrol);
            return;
        }

        float dist = Vector2.Distance(transform.position, playerTransform.position);

        // Player bỏ chạy khỏi tầm đánh → quay lại Chase
        if (dist > attackRange)
        {
            ChangeState(EnemyState.Chase);
            return;
        }

        // Luôn quay mặt về phía Player trong khi tấn công
        float dir = playerTransform.position.x > transform.position.x ? 1f : -1f;
        FaceDirection(dir);

        // Tấn công khi hết cooldown
        if (attackTimer <= 0f)
            PerformAttack();
    }

    // ════════════════════════════════════════════════════════════
    //  ACTIONS
    // ════════════════════════════════════════════════════════════

    void PerformAttack()
    {
        attackTimer = attackCooldown;

        // Random chọn OrcAttack1 (index=0) hoặc OrcAttack2 (index=1)
        int attackIndex = Random.Range(0, 2);
        anim.SetInteger(Hash_AttackIndex, attackIndex);

        // Set integer TRƯỚC rồi mới Set trigger để Animator đọc đúng giá trị
        anim.SetTrigger(Hash_AttackTrigger);
    }

    // ── Callback từ Health.OnDamaged ─────────────────────────────
    private void OnTakeDamage(int damage)
    {
        if (isDead) return;

        // Không bắt đầu Hurt mới nếu đang hurt rồi
        // (HP vẫn bị trừ bởi Health.cs, chỉ animation bị skip)
        if (isHurting) return;

        hurtCoroutine = StartCoroutine(HurtRoutine());
    }

    // ── Callback từ Health.OnDied ─────────────────────────────────
    private void OnDied()
    {
        // Nếu đang hurt thì dừng hurtCoroutine để chuyển ngay sang Death
        if (hurtCoroutine != null)
        {
            StopCoroutine(hurtCoroutine);
            hurtCoroutine = null;
            isHurting     = false;
        }

        StartCoroutine(DeathRoutine());
    }

    // ════════════════════════════════════════════════════════════
    //  COROUTINES
    // ════════════════════════════════════════════════════════════

    IEnumerator HurtRoutine()
    {
        isHurting    = true;
        currentState = EnemyState.Hurt;

        // Dừng mọi di chuyển, hủy attack trigger đang chờ
        SetVelocityX(0f);
        anim.ResetTrigger(Hash_AttackTrigger);
        anim.SetTrigger(Hash_HurtTrigger);

        // Chờ animation Hurt
        yield return new WaitForSeconds(hurtDuration);

        isHurting = false;

        // Sau Hurt: nếu vẫn còn sống, quyết định state tiếp theo
        if (!isDead)
        {
            bool playerInRange = playerTransform != null &&
                Vector2.Distance(transform.position, playerTransform.position) <= detectionRange;

            ChangeState(playerInRange ? EnemyState.Chase : EnemyState.Patrol);
        }
    }

    IEnumerator DeathRoutine()
    {
        isDead       = true;
        isHurting    = false;
        currentState = EnemyState.Death;

        // Dừng hoàn toàn
        SetVelocityX(0f);
        rb.bodyType = RigidbodyType2D.Kinematic;

        // Tắt collider để Player không bị cản bởi xác
        if (col != null) col.enabled = false;

        // Tắt EnemyAttack để không gây damage sau khi chết
        if (enemyAttack != null) enemyAttack.enabled = false;

        // Reset các bool animator
        anim.SetBool(Hash_IsWalking, false);
        anim.ResetTrigger(Hash_AttackTrigger);
        anim.ResetTrigger(Hash_HurtTrigger);

        if (hasDeathAnimation)
        {
            // Có OrcDeath state trong Animator → chạy animation
            anim.SetTrigger(Hash_DeathTrigger);
        }
        else
        {
            // Không có OrcDeath → đóng băng frame cuối rồi biến mất
            anim.speed = 0f;
        }

        // Chờ rồi Destroy (XP đã được trao bởi ExperienceReward trước đó)
        yield return new WaitForSeconds(deathDestroyDelay);

        Destroy(gameObject);
    }

    // ════════════════════════════════════════════════════════════
    //  HELPER METHODS
    // ════════════════════════════════════════════════════════════

    /// <summary>
    /// Dùng Physics2D tìm Player trong phạm vi detectionRange.
    /// Trả về true nếu tìm thấy và lưu Transform vào playerTransform.
    /// </summary>
    bool TryDetectPlayer()
    {
        Collider2D hit = Physics2D.OverlapCircle(
            transform.position, detectionRange, playerLayer);

        if (hit != null)
        {
            playerTransform = hit.transform;
            return true;
        }
        return false;
    }

    /// <summary>
    /// Set velocity X, giữ nguyên velocity Y để gravity hoạt động bình thường.
    /// Không dùng AddForce để tránh tích lũy force không kiểm soát được.
    /// </summary>
    void SetVelocityX(float x)
    {
        rb.linearVelocity = new Vector2(x, rb.linearVelocity.y);
    }

    /// <summary>
    /// Flip sprite theo hướng di chuyển bằng cách đổi dấu localScale.x.
    /// Phù hợp với cách PlayerController đang dùng.
    /// </summary>
    void FaceDirection(float dir)
    {
        if (dir > 0f)
        {
            transform.localScale = new Vector3(
                Mathf.Abs(transform.localScale.x),
                transform.localScale.y,
                transform.localScale.z);
        }
        else if (dir < 0f)
        {
            transform.localScale = new Vector3(
                -Mathf.Abs(transform.localScale.x),
                transform.localScale.y,
                transform.localScale.z);
        }
    }

    /// <summary>
    /// Đổi state và xử lý logic khởi tạo cho state mới.
    /// </summary>
    void ChangeState(EnemyState newState)
    {
        // Reset idleTimer mỗi khi vào Idle
        if (newState == EnemyState.Idle)
            idleTimer = idleWaitTime;

        currentState = newState;
    }

    // ════════════════════════════════════════════════════════════
    //  GIZMOS — hiện visualization trong Scene View
    // ════════════════════════════════════════════════════════════

    void OnDrawGizmosSelected()
    {
        // Vòng tròn VÀNG = detection range (Orc phát hiện Player)
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, detectionRange);

        // Vòng tròn ĐỎ = attack range (Orc bắt đầu tấn công)
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, attackRange);

        // Đường CYAN = phạm vi patrol (từ trái sang phải)
        Gizmos.color = Color.cyan;
        Vector3 origin = Application.isPlaying
            ? (Vector3)patrolOrigin
            : transform.position;
        Gizmos.DrawLine(
            origin + Vector3.left  * patrolDistance,
            origin + Vector3.right * patrolDistance);

        // Điểm tâm patrol
        Gizmos.DrawSphere(origin, 0.1f);
    }
}
