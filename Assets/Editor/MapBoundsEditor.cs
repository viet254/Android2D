using UnityEditor;
using UnityEngine;

/// <summary>
/// Custom Editor cho MapBounds — kéo tay trực tiếp trong Scene View.
///
/// Cách dùng:
///   • Kéo 4 GÓC  (chấm cyan)    → resize tự do cả X và Y
///   • Kéo 4 CẠNH (chấm nhỏ hơn) → resize 1 chiều (trái/phải/trên/dưới)
///   • Kéo TÂM    (vòng tròn vàng)→ di chuyển toàn bộ bounds
/// </summary>
[CustomEditor(typeof(MapBounds))]
public class MapBoundsEditor : Editor
{
    // Màu sắc
    private static readonly Color ColorBorder  = new Color(0f,  0.85f, 1f,  0.9f);
    private static readonly Color ColorFill    = new Color(0f,  0.85f, 1f,  0.06f);
    private static readonly Color ColorCorner  = new Color(0f,  0.85f, 1f,  1f);
    private static readonly Color ColorEdge    = new Color(0f,  0.6f,  0.8f,1f);
    private static readonly Color ColorCenter  = new Color(1f,  0.9f,  0f,  0.9f);

    void OnSceneGUI()
    {
        MapBounds mb = (MapBounds)target;
        serializedObject.Update();

        // Lấy serialized properties để Undo/Redo hoạt động tự động
        SerializedProperty pCenter = serializedObject.FindProperty("center");
        SerializedProperty pSize   = serializedObject.FindProperty("size");
        SerializedProperty pOffset = serializedObject.FindProperty("offset");

        Vector2 center = pCenter.vector2Value;
        Vector2 size   = pSize.vector2Value;
        Vector2 offset = pOffset.vector2Value;
        Vector2 ec     = center + offset;  // effective center

        float hw = Mathf.Max(size.x * 0.5f, 0.1f);
        float hh = Mathf.Max(size.y * 0.5f, 0.1f);

        // Tọa độ 4 cạnh
        float left   = ec.x - hw;
        float right  = ec.x + hw;
        float bottom = ec.y - hh;
        float top    = ec.y + hh;

        // Kích thước handle theo zoom level (tự động scale)
        float hs = HandleUtility.GetHandleSize(new Vector3(ec.x, ec.y, 0f)) * 0.1f;

        // ── Vẽ nền và viền ───────────────────────────────────────
        Handles.color = ColorFill;
        Handles.DrawSolidRectangleWithOutline(
            new Rect(left, bottom, size.x, size.y),
            ColorFill,
            ColorBorder);

        // ── Biến lưu giá trị mới ─────────────────────────────────
        float newLeft   = left;
        float newRight  = right;
        float newBottom = bottom;
        float newTop    = top;
        bool  resized   = false;
        bool  moved     = false;

        // ════════════════════════════════════════════════════════
        //  4 GÓC — kéo tự do cả X lẫn Y
        // ════════════════════════════════════════════════════════

        Handles.color = ColorCorner;

        // Góc dưới-trái (BL)
        EditorGUI.BeginChangeCheck();
        Vector3 hBL = Handles.FreeMoveHandle(
            new Vector3(left, bottom, 0f), hs, Vector3.zero, Handles.DotHandleCap);
        if (EditorGUI.EndChangeCheck())
        { newLeft = hBL.x; newBottom = hBL.y; resized = true; }

        // Góc trên-trái (TL)
        EditorGUI.BeginChangeCheck();
        Vector3 hTL = Handles.FreeMoveHandle(
            new Vector3(left, top, 0f), hs, Vector3.zero, Handles.DotHandleCap);
        if (EditorGUI.EndChangeCheck())
        { newLeft = hTL.x; newTop = hTL.y; resized = true; }

        // Góc trên-phải (TR)
        EditorGUI.BeginChangeCheck();
        Vector3 hTR = Handles.FreeMoveHandle(
            new Vector3(right, top, 0f), hs, Vector3.zero, Handles.DotHandleCap);
        if (EditorGUI.EndChangeCheck())
        { newRight = hTR.x; newTop = hTR.y; resized = true; }

        // Góc dưới-phải (BR)
        EditorGUI.BeginChangeCheck();
        Vector3 hBR = Handles.FreeMoveHandle(
            new Vector3(right, bottom, 0f), hs, Vector3.zero, Handles.DotHandleCap);
        if (EditorGUI.EndChangeCheck())
        { newRight = hBR.x; newBottom = hBR.y; resized = true; }

        // ════════════════════════════════════════════════════════
        //  4 CẠNH — kéo 1 chiều
        // ════════════════════════════════════════════════════════

        Handles.color = ColorEdge;
        float hsEdge = hs * 0.75f;

        // Cạnh trái (chỉ kéo ngang)
        EditorGUI.BeginChangeCheck();
        Vector3 hEL = Handles.Slider(
            new Vector3(left, ec.y, 0f), Vector3.right,
            hsEdge, Handles.DotHandleCap, 0f);
        if (EditorGUI.EndChangeCheck())
        { newLeft = hEL.x; resized = true; }

        // Cạnh phải (chỉ kéo ngang)
        EditorGUI.BeginChangeCheck();
        Vector3 hER = Handles.Slider(
            new Vector3(right, ec.y, 0f), Vector3.right,
            hsEdge, Handles.DotHandleCap, 0f);
        if (EditorGUI.EndChangeCheck())
        { newRight = hER.x; resized = true; }

        // Cạnh dưới (chỉ kéo dọc)
        EditorGUI.BeginChangeCheck();
        Vector3 hEB = Handles.Slider(
            new Vector3(ec.x, bottom, 0f), Vector3.up,
            hsEdge, Handles.DotHandleCap, 0f);
        if (EditorGUI.EndChangeCheck())
        { newBottom = hEB.y; resized = true; }

        // Cạnh trên (chỉ kéo dọc)
        EditorGUI.BeginChangeCheck();
        Vector3 hET = Handles.Slider(
            new Vector3(ec.x, top, 0f), Vector3.up,
            hsEdge, Handles.DotHandleCap, 0f);
        if (EditorGUI.EndChangeCheck())
        { newTop = hET.y; resized = true; }

        // ════════════════════════════════════════════════════════
        //  TÂM — kéo để di chuyển toàn bộ bounds
        // ════════════════════════════════════════════════════════

        Handles.color = ColorCenter;
        EditorGUI.BeginChangeCheck();
        Vector3 hC = Handles.FreeMoveHandle(
            new Vector3(ec.x, ec.y, 0f), hs * 1.8f, Vector3.zero, Handles.CircleHandleCap);
        if (EditorGUI.EndChangeCheck())
        {
            // Di chuyển → cập nhật offset (giữ nguyên center, chỉ đổi offset)
            pOffset.vector2Value = new Vector2(
                offset.x + (hC.x - ec.x),
                offset.y + (hC.y - ec.y));
            serializedObject.ApplyModifiedProperties();
            moved = true;
        }

        // ════════════════════════════════════════════════════════
        //  Áp kết quả resize
        // ════════════════════════════════════════════════════════

        if (resized && !moved)
        {
            // Chống đảo chiều (left không được vượt qua right)
            if (newRight - newLeft < 0.5f)
            {
                float mid = (newLeft + newRight) * 0.5f;
                newLeft  = mid - 0.25f;
                newRight = mid + 0.25f;
            }
            if (newTop - newBottom < 0.5f)
            {
                float mid = (newBottom + newTop) * 0.5f;
                newBottom = mid - 0.25f;
                newTop    = mid + 0.25f;
            }

            float newSizeX   = newRight  - newLeft;
            float newSizeY   = newTop    - newBottom;
            float newEffCX   = (newLeft  + newRight)  * 0.5f;
            float newEffCY   = (newBottom + newTop)   * 0.5f;

            // effectiveCenter = center + offset → center = effectiveCenter - offset
            pCenter.vector2Value = new Vector2(newEffCX - offset.x, newEffCY - offset.y);
            pSize.vector2Value   = new Vector2(newSizeX, newSizeY);
            serializedObject.ApplyModifiedProperties();
        }

        // ════════════════════════════════════════════════════════
        //  Label hướng dẫn + kích thước hiện tại
        // ════════════════════════════════════════════════════════

        // Cập nhật lại size để label hiện đúng sau khi kéo
        Vector2 currentSize = pSize.vector2Value;

        Handles.color = Color.cyan;
        Handles.Label(
            new Vector3(left, top + hs * 2.5f, 0f),
            $"  W: {currentSize.x:F1}   H: {currentSize.y:F1}\n" +
            $"  ● Kéo GÓC → resize tự do\n" +
            $"  ● Kéo CẠNH → resize 1 chiều\n" +
            $"  ● Kéo TÂM (vàng) → di chuyển");
    }
}
