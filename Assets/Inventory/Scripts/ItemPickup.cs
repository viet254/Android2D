using System.Collections;
using UnityEngine;

[RequireComponent(typeof(Collider2D))]
public class ItemPickup : MonoBehaviour
{
    [SerializeField] private ItemData item;
    [SerializeField, Min(1)] private int quantity = 1;

    [Header("World Drop")]
    [SerializeField, Range(0.25f, 1f)] private float worldDropScale = 0.65f;
    [SerializeField, Min(0.1f)] private float dropAnimationDuration = 1f;
    [SerializeField, Min(1)] private int bounceCount = 3;
    [SerializeField, Min(0f)] private float bounceHeight = 0.65f;

    private bool isProcessing;
    private bool canCollect = true;
    private Coroutine dropAnimationRoutine;

    public ItemData Item => item;
    public int Quantity => quantity;
    public bool IsRuntimeDrop { get; private set; }

    public bool Configure(ItemData newItem, int newQuantity)
    {
        if (newItem == null || newQuantity <= 0)
            return false;

        item = newItem;
        quantity = newQuantity;
        transform.localScale = Vector3.one * Mathf.Clamp(worldDropScale, 0.25f, 1f);

        SpriteRenderer spriteRenderer = GetComponent<SpriteRenderer>();
        if (spriteRenderer != null)
        {
            spriteRenderer.sprite = item.Icon;
            spriteRenderer.enabled = item.Icon != null;
            spriteRenderer.sortingLayerName = "Player";
            spriteRenderer.sortingOrder = 1;
        }

        if (item.Icon == null)
            Debug.LogWarning($"{name}: Item '{item.DisplayName}' has no icon; pickup remains functional.", this);

        return true;
    }

    public bool RestoreRuntimeDrop(ItemData restoredItem, int restoredQuantity, Vector3 restoredPosition)
    {
        if (!Configure(restoredItem, restoredQuantity))
            return false;

        if (dropAnimationRoutine != null)
            StopCoroutine(dropAnimationRoutine);

        transform.position = restoredPosition;
        IsRuntimeDrop = true;
        canCollect = true;
        isProcessing = false;
        dropAnimationRoutine = null;
        return true;
    }

    public float GetVisualBottomOffset()
    {
        SpriteRenderer spriteRenderer = GetComponent<SpriteRenderer>();
        if (spriteRenderer == null || spriteRenderer.sprite == null)
            return 0f;

        return Mathf.Max(0f, transform.position.y - spriteRenderer.bounds.min.y);
    }

    public void PlayDropAnimation(Vector3 landingPosition)
    {
        IsRuntimeDrop = true;
        if (dropAnimationRoutine != null)
            StopCoroutine(dropAnimationRoutine);

        dropAnimationRoutine = StartCoroutine(AnimateDrop(landingPosition));
    }

    private void OnValidate()
    {
        quantity = Mathf.Max(1, quantity);
        worldDropScale = Mathf.Clamp(worldDropScale, 0.25f, 1f);
        dropAnimationDuration = Mathf.Max(0.1f, dropAnimationDuration);
        bounceCount = Mathf.Max(1, bounceCount);
        bounceHeight = Mathf.Max(0f, bounceHeight);
    }

    private void OnDisable()
    {
        isProcessing = false;
        canCollect = true;
        dropAnimationRoutine = null;
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!canCollect || isProcessing)
        {
            return;
        }

        if (item == null)
        {
            Debug.LogWarning($"{name}: ItemPickup has no ItemData assigned.", this);
            return;
        }

        if (quantity <= 0)
        {
            Debug.LogWarning($"{name}: ItemPickup quantity must be greater than zero.", this);
            return;
        }

        PlayerController player = other.GetComponentInParent<PlayerController>();
        if (player == null)
        {
            return;
        }

        Inventory inventory = player.GetComponent<Inventory>();
        if (inventory == null)
        {
            inventory = player.GetComponentInParent<Inventory>();
        }

        if (inventory == null)
        {
            Debug.LogWarning($"{name}: Player has no Inventory component.", player);
            return;
        }

        isProcessing = true;

        int remaining = inventory.AddItem(item, quantity);
        quantity = Mathf.Max(0, remaining);

        if (quantity == 0)
        {
            Destroy(gameObject);
            return;
        }

        StartCoroutine(ReleaseProcessingLock());
    }

    private IEnumerator AnimateDrop(Vector3 landingPosition)
    {
        canCollect = false;

        Vector3 launchPosition = transform.position;
        float duration = Mathf.Max(0.1f, dropAnimationDuration);
        int bounces = Mathf.Max(1, bounceCount);
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float progress = Mathf.Clamp01(elapsed / duration);
            Vector3 position = Vector3.Lerp(launchPosition, landingPosition, progress);
            float bounce = Mathf.Abs(Mathf.Sin(progress * Mathf.PI * bounces));
            float damping = 1f - progress * 0.65f;
            position.y += bounce * bounceHeight * damping;
            transform.position = position;
            yield return null;
        }

        transform.position = landingPosition;
        canCollect = true;
        dropAnimationRoutine = null;
    }

    private IEnumerator ReleaseProcessingLock()
    {
        yield return null;
        isProcessing = false;
    }
}
