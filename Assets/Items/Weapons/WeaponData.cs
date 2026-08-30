using UnityEngine;

[CreateAssetMenu(fileName = "WeaponData", menuName = "Android2D/Items/Weapon")]
public class WeaponData : ItemData
{
    [Header("Weapon")]
    [SerializeField, Min(0)] private int damage = 10;
    [SerializeField, Min(0.01f)] private float attackSpeed = 1f;
    [SerializeField] private DamageType damageType = DamageType.Physical;

    public int Damage => damage;
    public float AttackSpeed => attackSpeed;
    public DamageType DamageType => damageType;

    protected override void OnValidate()
    {
        base.OnValidate();
        damage = Mathf.Max(0, damage);
        attackSpeed = Mathf.Max(0.01f, attackSpeed);
    }
}
