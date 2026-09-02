using UnityEngine;

public readonly struct EnemyPersistenceState
{
    public EnemyPersistenceState(
        string persistentId,
        bool alive,
        Vector3 position,
        int currentHealth)
    {
        PersistentId = persistentId;
        Alive = alive;
        Position = position;
        CurrentHealth = currentHealth;
    }

    public string PersistentId { get; }
    public bool Alive { get; }
    public Vector3 Position { get; }
    public int CurrentHealth { get; }
}
