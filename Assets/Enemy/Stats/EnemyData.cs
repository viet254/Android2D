using UnityEngine;

[CreateAssetMenu(fileName = "EnemyData", menuName = "Android2D/Enemy Data")]
public class EnemyData : ScriptableObject
{
    [SerializeField] private string id = "enemy";
    [SerializeField] private string displayName = "Enemy";
    [SerializeField, Min(1)] private int maxHealth = 50;
    [SerializeField, Min(0)] private int damage = 10;
    [SerializeField, Min(0f)] private float moveSpeed = 3f;
    [SerializeField, Min(0f)] private float detectionRange = 5f;
    [SerializeField, Min(0f)] private float attackRange = 1.2f;
    [SerializeField, Min(0)] private int experienceReward = 20;
    [SerializeField, Min(0f)] private float attackCooldown = 1.5f;
    [SerializeField] private LootTable lootTable;
    public string ID => id;
    public string DisplayName => displayName;
    public int MaxHealth => maxHealth;
    public int Damage => damage;
    public float MoveSpeed => moveSpeed;
    public float DetectionRange => detectionRange;
    public float AttackRange => attackRange;
    public int ExperienceReward => experienceReward;
    public float AttackCooldown => attackCooldown;
    public LootTable LootTable => lootTable;
}
