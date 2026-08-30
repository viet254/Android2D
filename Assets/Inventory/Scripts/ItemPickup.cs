using System.Collections;
using UnityEngine;

[RequireComponent(typeof(Collider2D))]
public class ItemPickup : MonoBehaviour
{
    [SerializeField] private ItemData item;
    [SerializeField, Min(1)] private int quantity = 1;

    private bool isProcessing;

    public ItemData Item => item;
    public int Quantity => quantity;

    private void OnValidate()
    {
        quantity = Mathf.Max(1, quantity);
    }

    private void OnDisable()
    {
        isProcessing = false;
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (isProcessing)
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

    private IEnumerator ReleaseProcessingLock()
    {
        yield return null;
        isProcessing = false;
    }
}
