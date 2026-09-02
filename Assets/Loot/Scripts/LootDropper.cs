using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Enemy), typeof(Health))]
public class LootDropper : MonoBehaviour
{
    [SerializeField] private ItemPickup itemPickupPrefab;
    [SerializeField] private Vector2 spawnOffset = new Vector2(0f, 0.5f);
    [SerializeField, Min(0.5f)] private float minimumPlayerDistance = 1.5f;
    [Tooltip("Manual vertical contact adjustment after matching the sprite bottom to the ground.")]
    [SerializeField, Range(-0.5f, 0.5f)] private float groundClearance;
    [SerializeField, Min(0.05f)] private float groundProbeHeight = 0.25f;
    [SerializeField, Min(1f)] private float groundProbeDistance = 12f;

    private Enemy enemy;
    private Health health;
    private bool hasDroppedLoot;

    private void Awake()
    {
        enemy = GetComponent<Enemy>();
        health = GetComponent<Health>();
    }

    private void OnEnable()
    {
        if (health != null)
            health.OnDied += HandleDied;
    }

    private void OnDisable()
    {
        if (health != null)
            health.OnDied -= HandleDied;
    }

    public void ResetForRestore()
    {
        hasDroppedLoot = false;
    }

    private void HandleDied()
    {
        if (hasDroppedLoot)
            return;

        hasDroppedLoot = true;

        LootTable lootTable = enemy != null && enemy.Data != null
            ? enemy.Data.LootTable
            : null;
        if (lootTable == null)
            return;

        if (itemPickupPrefab == null)
        {
            Debug.LogWarning($"[LootDropper] {name} has no generic ItemPickup prefab assigned.", this);
            return;
        }

        List<LootResult> results = lootTable.Roll();
        Vector3 dropOrigin = CalculateDropOrigin();
        for (int i = 0; i < results.Count; i++)
            SpawnPickup(results[i], i, results.Count, dropOrigin);
    }

    private Vector3 CalculateDropOrigin()
    {
        Vector3 origin = transform.position + (Vector3)spawnOffset;
        PlayerController player = FindAnyObjectByType<PlayerController>();
        if (player != null)
        {
            float safeDistance = Mathf.Max(0.5f, minimumPlayerDistance);
            float horizontalDelta = origin.x - player.transform.position.x;
            if (Mathf.Abs(horizontalDelta) < safeDistance)
            {
                float direction = Mathf.Abs(horizontalDelta) > 0.01f
                    ? Mathf.Sign(horizontalDelta)
                    : (player.transform.position.x >= transform.position.x ? -1f : 1f);
                origin.x = player.transform.position.x + direction * safeDistance;
            }
        }

        int groundMask = LayerMask.GetMask("Ground");
        Vector2 probeOrigin = new Vector2(
            origin.x,
            transform.position.y + Mathf.Max(0.05f, groundProbeHeight));
        float probeDistance = Mathf.Max(1f, groundProbeDistance);
        RaycastHit2D hit = Physics2D.Raycast(probeOrigin, Vector2.down, probeDistance, groundMask);
        if (hit.collider != null)
        {
            origin.y = hit.point.y;
        }
        else
        {
            Debug.LogWarning(
                $"[LootDropper] No Ground-layer surface found below drop position for {name}.",
                this);
        }

        return origin;
    }

    private void SpawnPickup(
        LootResult result,
        int index,
        int resultCount,
        Vector3 dropOrigin)
    {
        if (result.Item == null || result.Quantity <= 0)
            return;

        float horizontalOffset = (index - (resultCount - 1) * 0.5f) * 0.35f;
        Vector3 spawnPosition = dropOrigin + Vector3.right * horizontalOffset;
        Vector3 launchPosition = transform.position + Vector3.up * 0.25f;
        ItemPickup pickup = Instantiate(itemPickupPrefab, launchPosition, Quaternion.identity);

        if (!pickup.Configure(result.Item, result.Quantity))
        {
            Debug.LogWarning("[LootDropper] Rolled loot could not configure its ItemPickup.", pickup);
            Destroy(pickup.gameObject);
            return;
        }

        float visualBottomOffset = pickup.GetVisualBottomOffset();
        spawnPosition.y += visualBottomOffset + groundClearance;
        pickup.PlayDropAnimation(spawnPosition);
    }
}
