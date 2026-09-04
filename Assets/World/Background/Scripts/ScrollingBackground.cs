using UnityEngine;

/// <summary>
/// Implements the two-background loop described on slide 20.
/// The object can live under Grid; LateUpdate keeps it aligned with the camera.
/// </summary>
[DisallowMultipleComponent]
[DefaultExecutionOrder(1000)]
public sealed class ScrollingBackground : MonoBehaviour
{
    [SerializeField] private SpriteRenderer backgroundA;
    [SerializeField] private SpriteRenderer backgroundB;
    [SerializeField, Min(0f)] private float speed = 1.2f;
    [SerializeField] private Camera targetCamera;

    private float panelWidth;
    private float visibleHalfWidth;
    private float lastAspect = -1f;
    private float lastOrthographicSize = -1f;

    public void Configure(SpriteRenderer first, SpriteRenderer second, Camera cameraToFollow, float movementSpeed)
    {
        backgroundA = first;
        backgroundB = second;
        targetCamera = cameraToFollow;
        speed = Mathf.Max(0f, movementSpeed);
        RebuildLayout();
    }

    private void Start()
    {
        if (targetCamera == null)
        {
            targetCamera = Camera.main;
        }

        RebuildLayout();
    }

    private void Update()
    {
        if (!IsReady())
        {
            return;
        }

        if (!Mathf.Approximately(lastAspect, targetCamera.aspect) ||
            !Mathf.Approximately(lastOrthographicSize, targetCamera.orthographicSize))
        {
            RebuildLayout();
        }

        float movement = speed * Time.deltaTime;
        backgroundA.transform.localPosition += Vector3.left * movement;
        backgroundB.transform.localPosition += Vector3.left * movement;

        WrapAfterOther(backgroundA, backgroundB);
        WrapAfterOther(backgroundB, backgroundA);
    }

    private void LateUpdate()
    {
        if (targetCamera == null)
        {
            return;
        }

        Vector3 cameraPosition = targetCamera.transform.position;
        transform.position = new Vector3(cameraPosition.x, cameraPosition.y, transform.position.z);
    }

    private void RebuildLayout()
    {
        if (!IsReady() || !targetCamera.orthographic)
        {
            return;
        }

        float visibleHeight = targetCamera.orthographicSize * 2f;
        float visibleWidth = visibleHeight * targetCamera.aspect;
        Vector2 spriteSize = backgroundA.sprite.bounds.size;
        float scale = Mathf.Max(visibleWidth / spriteSize.x, visibleHeight / spriteSize.y);

        backgroundA.transform.localScale = Vector3.one * scale;
        backgroundB.transform.localScale = Vector3.one * scale;

        panelWidth = spriteSize.x * scale;
        visibleHalfWidth = visibleWidth * 0.5f;
        backgroundA.transform.localPosition = Vector3.zero;
        backgroundB.transform.localPosition = new Vector3(panelWidth, 0f, 0f);

        Vector3 cameraPosition = targetCamera.transform.position;
        transform.position = new Vector3(cameraPosition.x, cameraPosition.y, transform.position.z);

        lastAspect = targetCamera.aspect;
        lastOrthographicSize = targetCamera.orthographicSize;
    }

    private void WrapAfterOther(SpriteRenderer candidate, SpriteRenderer other)
    {
        float candidateRightEdge = candidate.transform.localPosition.x + panelWidth * 0.5f;
        if (candidateRightEdge < -visibleHalfWidth)
        {
            Vector3 position = candidate.transform.localPosition;
            position.x = other.transform.localPosition.x + panelWidth;
            candidate.transform.localPosition = position;
        }
    }

    private bool IsReady()
    {
        return backgroundA != null && backgroundB != null && targetCamera != null &&
               backgroundA.sprite != null && backgroundB.sprite != null;
    }
}
