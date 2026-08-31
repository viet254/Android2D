using UnityEngine;

[CreateAssetMenu(fileName = "Consumable", menuName = "Android2D/Items/Consumable")]
public class ConsumableData : ItemData
{
    [Header("Consumable Effect")]
    [SerializeField, Min(1)] private int healAmount = 25;

    public int HealAmount => healAmount;

    public bool TryUse(GameObject user)
    {
        if (user == null || healAmount <= 0)
            return false;

        Health health = user.GetComponent<Health>();
        if (health == null)
            health = user.GetComponentInParent<Health>();

        return health != null && health.Heal(healAmount);
    }

    protected override void OnValidate()
    {
        base.OnValidate();
        healAmount = Mathf.Max(1, healAmount);
    }
}
