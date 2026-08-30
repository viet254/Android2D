using UnityEngine;

[DefaultExecutionOrder(-100)]
[RequireComponent(typeof(Health), typeof(EnemyAI), typeof(EnemyAttack))]
public class Enemy : MonoBehaviour
{
    [SerializeField] private EnemyData data;
    public EnemyData Data => data;

    private void Awake()
    {
        if (data == null)
        {
            Debug.LogError($"[Enemy] {name} has no EnemyData assigned.", this);
            return;
        }
        GetComponent<Health>().ConfigureMaxHealth(data.MaxHealth);
        GetComponent<EnemyAI>().Configure(data);
        GetComponent<EnemyAttack>().Configure(data);
        ExperienceReward reward = GetComponent<ExperienceReward>();
        if (reward != null) reward.Configure(data);
    }
}