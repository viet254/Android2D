using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(Enemy), typeof(Health))]
public class EnemyPersistentEntity : MonoBehaviour
{
    [SerializeField] private string persistentId;
    [SerializeField] private GameObject prefabSource;

    private EnemyPersistenceRegistry registry;
    private Health health;

    public string PersistentId => persistentId;
    public GameObject PrefabSource => prefabSource;
    public Health Health => health != null ? health : GetComponent<Health>();

    private void Awake()
    {
        health = GetComponent<Health>();
    }

    private void OnEnable()
    {
        if (health == null)
            health = GetComponent<Health>();

        health.OnDied -= HandleDied;
        health.OnDied += HandleDied;

        if (registry == null)
            registry = FindAnyObjectByType<EnemyPersistenceRegistry>();

        if (registry != null && !string.IsNullOrWhiteSpace(persistentId))
            registry.Register(this);
    }

    private void OnDisable()
    {
        if (health != null)
            health.OnDied -= HandleDied;
    }

    private void OnDestroy()
    {
        if (registry != null && !string.IsNullOrWhiteSpace(persistentId))
            registry.NotifyDestroyed(persistentId, this);
    }

    internal void ConfigureRuntime(
        string restoredId,
        GameObject restoredPrefab,
        EnemyPersistenceRegistry owner)
    {
        persistentId = restoredId;
        prefabSource = restoredPrefab;
        registry = owner;
        registry.Register(this);
    }

    internal void RestoreAlive(Vector3 position, int currentHealth)
    {
        transform.position = position;

        Rigidbody2D body = GetComponent<Rigidbody2D>();
        if (body != null)
        {
            body.position = position;
            body.linearVelocity = Vector2.zero;
            body.angularVelocity = 0f;
        }

        health.RestoreState(currentHealth, true);

        EnemyAI enemyAI = GetComponent<EnemyAI>();
        if (enemyAI != null)
            enemyAI.RestoreAliveState();

        ExperienceReward reward = GetComponent<ExperienceReward>();
        if (reward != null)
            reward.ResetForRestore();

        LootDropper lootDropper = GetComponent<LootDropper>();
        if (lootDropper != null)
            lootDropper.ResetForRestore();
    }

    internal void SuppressAsDead(Vector3 position)
    {
        transform.position = position;

        Rigidbody2D body = GetComponent<Rigidbody2D>();
        if (body != null)
        {
            body.linearVelocity = Vector2.zero;
            body.angularVelocity = 0f;
        }

        health.RestoreState(0, false);
        gameObject.SetActive(false);
    }

    private void HandleDied()
    {
        if (registry != null)
            registry.MarkDead(persistentId, transform.position);
    }
}
