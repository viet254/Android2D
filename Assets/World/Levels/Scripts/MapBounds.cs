using UnityEngine;

/// <summary>
/// Tạo ranh giới map — tường vô hình ngăn Player/Enemy thoát ra ngoài.
/// PolygonCollider2D trên object này dùng cho Cinemachine Confiner 2D (camera dừng tại biên).
///
/// Setup:
///   1. Tạo empty GameObject tên "MapBounds"
///   2. Gắn script này (tự thêm PolygonCollider2D)
///   3. Chỉnh size / center / offset trong Inspector → thấy khung xanh trong Scene View
///   4. Gắn PolygonCollider2D này vào Cinemachine Confiner 2D (xem hướng dẫn bên dưới)
/// </summary>
[ExecuteAlways]
[RequireComponent(typeof(PolygonCollider2D))]
public class MapBounds : MonoBehaviour
{
    // ─────────────────────────────────────────────────────────────
    //  INSPECTOR
    // ─────────────────────────────────────────────────────────────

    [Header("Kích thước & Vị trí Map")]
    [Tooltip("Tâm của map. Thường để (0, 0) rồi dùng Offset để dịch chuyển.")]
    [SerializeField] private Vector2 center = Vector2.zero;

    [Tooltip("Chiều rộng (X) và chiều cao (Y) của toàn bộ map.")]
    [SerializeField] private Vector2 size = new Vector2(30f, 15f);

    [Tooltip("Dịch chuyển toàn bộ ranh giới. Dùng khi tâm map không ở gốc tọa độ.")]
    [SerializeField] private Vector2 offset = Vector2.zero;

    [Header("Tường vô hình")]
    [Tooltip("Độ dày của tường vô hình ở 4 cạnh.\nTăng lên nếu nhân vật vẫn xuyên qua.")]
    [SerializeField] private float wallThickness = 2f;

    // ─────────────────────────────────────────────────────────────
    //  PRIVATE
    // ─────────────────────────────────────────────────────────────

    private PolygonCollider2D boundsCollider;
    private GameObject        wallContainer;

    // Tâm thực tế sau khi áp offset
    private Vector2 EffectiveCenter => center + offset;

    // ─────────────────────────────────────────────────────────────
    //  LIFECYCLE
    // ─────────────────────────────────────────────────────────────

    void Awake()
    {
        boundsCollider = GetComponent<PolygonCollider2D>();
        UpdateBoundsCollider();

        // Tường chỉ tạo lúc chạy game
        if (Application.isPlaying)
            BuildWalls();
    }

#if UNITY_EDITOR
    // Gọi mỗi khi đổi giá trị trong Inspector (Edit Mode) → cập nhật ngay
    void OnValidate()
    {
        boundsCollider = GetComponent<PolygonCollider2D>();
        UpdateBoundsCollider();
    }
#endif

    // ─────────────────────────────────────────────────────────────
    //  CẬP NHẬT POLYGON COLLIDER 2D
    //  Shape này dùng cho Cinemachine Confiner 2D để giới hạn camera
    // ─────────────────────────────────────────────────────────────

    void UpdateBoundsCollider()
    {
        if (boundsCollider == null) return;

        Vector2 c  = EffectiveCenter;
        float   hw = size.x * 0.5f;
        float   hh = size.y * 0.5f;

        // Tạo hình chữ nhật bằng 4 điểm theo chiều ngược kim đồng hồ
        boundsCollider.SetPath(0, new Vector2[]
        {
            new Vector2(c.x - hw, c.y - hh),   // Góc dưới trái
            new Vector2(c.x - hw, c.y + hh),   // Góc trên trái
            new Vector2(c.x + hw, c.y + hh),   // Góc trên phải
            new Vector2(c.x + hw, c.y - hh),   // Góc dưới phải
        });

        // IsTrigger = true: không chặn vật lý, chỉ là shape cho Cinemachine
        boundsCollider.isTrigger = true;
    }

    // ─────────────────────────────────────────────────────────────
    //  TẠO 4 TƯỜNG VÔ HÌNH (chỉ lúc Play Mode)
    // ─────────────────────────────────────────────────────────────

    void BuildWalls()
    {
        // Xóa container cũ để tránh duplicate
        if (wallContainer != null) Destroy(wallContainer);

        wallContainer = new GameObject("_BoundaryWalls");
        wallContainer.transform.SetParent(transform);
        wallContainer.transform.localPosition = Vector3.zero;

        Vector2 c  = EffectiveCenter;
        float   hw = size.x * 0.5f;
        float   hh = size.y * 0.5f;
        float   t  = wallThickness;

        //        Tên          Vị trí tâm tường                         Kích thước tường
        CreateWall("Wall_Left",   new Vector2(c.x - hw - t * 0.5f, c.y),  new Vector2(t, size.y + t * 2f));
        CreateWall("Wall_Right",  new Vector2(c.x + hw + t * 0.5f, c.y),  new Vector2(t, size.y + t * 2f));
        CreateWall("Wall_Bottom", new Vector2(c.x, c.y - hh - t * 0.5f),  new Vector2(size.x + t * 2f, t));
        CreateWall("Wall_Top",    new Vector2(c.x, c.y + hh + t * 0.5f),  new Vector2(size.x + t * 2f, t));
    }

    void CreateWall(string wallName, Vector2 position, Vector2 wallSize)
    {
        GameObject wall = new GameObject(wallName);
        wall.transform.SetParent(wallContainer.transform);
        wall.transform.position = new Vector3(position.x, position.y, 0f);

        // BoxCollider2D: tường vật lý thật sự chặn Player & Enemy
        BoxCollider2D box = wall.AddComponent<BoxCollider2D>();
        box.size      = wallSize;
        box.isTrigger = false;

        // Rigidbody2D Static: không bị gravity, không di chuyển, nhưng vẫn là physics object
        Rigidbody2D rb = wall.AddComponent<Rigidbody2D>();
        rb.bodyType    = RigidbodyType2D.Static;
    }

    // ─────────────────────────────────────────────────────────────
    //  GIZMOS — hiện khung ranh giới trong Scene View
    // ─────────────────────────────────────────────────────────────

    void OnDrawGizmos()
    {
        Vector2 c   = EffectiveCenter;
        Vector3 pos = new Vector3(c.x, c.y, 0f);
        Vector3 sz  = new Vector3(size.x, size.y, 0.1f);

        // Vùng nền map (xanh cyan trong suốt)
        Gizmos.color = new Color(0f, 0.85f, 1f, 0.06f);
        Gizmos.DrawCube(pos, sz);

        // Đường viền ranh giới
        Gizmos.color = new Color(0f, 0.85f, 1f, 0.9f);
        Gizmos.DrawWireCube(pos, sz);

        // 4 điểm góc
        Gizmos.color = Color.cyan;
        float hw = size.x * 0.5f;
        float hh = size.y * 0.5f;
        float dotSize = 0.15f;
        Gizmos.DrawSphere(new Vector3(c.x - hw, c.y - hh, 0f), dotSize);
        Gizmos.DrawSphere(new Vector3(c.x - hw, c.y + hh, 0f), dotSize);
        Gizmos.DrawSphere(new Vector3(c.x + hw, c.y + hh, 0f), dotSize);
        Gizmos.DrawSphere(new Vector3(c.x + hw, c.y - hh, 0f), dotSize);

#if UNITY_EDITOR
        // Label kích thước hiện ở góc trên trái
        UnityEditor.Handles.color = Color.cyan;
        UnityEditor.Handles.Label(
            new Vector3(c.x - hw, c.y + hh + 0.4f, 0f),
            $"Map Bounds  {size.x:F1} × {size.y:F1}");
#endif
    }
}
