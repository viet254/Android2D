using System;
using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
[DefaultExecutionOrder(-200)]
public class EnemyPersistenceRegistry : MonoBehaviour
{
    private sealed class EnemyRecord
    {
        public string PersistentId;
        public EnemyPersistentEntity Entity;
        public GameObject PrefabSource;
        public bool Alive;
        public Vector3 Position;
        public int CurrentHealth;
    }

    private readonly Dictionary<string, EnemyRecord> records =
        new Dictionary<string, EnemyRecord>(StringComparer.Ordinal);

    public bool ValidateSceneSetup(out string error)
    {
        error = null;
        Enemy[] enemies = UnityEngine.Object.FindObjectsByType<Enemy>(FindObjectsInactive.Include);
        for (int i = 0; i < enemies.Length; i++)
        {
            Enemy enemy = enemies[i];
            if (enemy.gameObject.scene != gameObject.scene)
                continue;

            EnemyPersistentEntity persistent = enemy.GetComponent<EnemyPersistentEntity>();
            if (persistent == null)
            {
                error = $"Enemy '{enemy.name}' has no EnemyPersistentEntity. Run Tools/Android2D/Save/Setup Enemy Persistent IDs, then save the Scene.";
                return false;
            }

            if (string.IsNullOrWhiteSpace(persistent.PersistentId))
            {
                error = $"Enemy '{enemy.name}' has an empty persistent ID.";
                return false;
            }

            if (persistent.PrefabSource == null || persistent.PrefabSource.scene.IsValid())
            {
                error = $"Enemy '{enemy.name}' does not reference a prefab asset and cannot be respawned. Run the Enemy Persistent IDs setup again.";
                return false;
            }

            if (!records.TryGetValue(persistent.PersistentId, out EnemyRecord record)
                || record.Entity != persistent)
            {
                error = $"Enemy '{enemy.name}' with ID '{persistent.PersistentId}' is not registered.";
                return false;
            }
        }

        return true;
    }

    public bool Register(EnemyPersistentEntity entity)
    {
        if (entity == null || string.IsNullOrWhiteSpace(entity.PersistentId))
            return false;

        string id = entity.PersistentId;
        if (records.TryGetValue(id, out EnemyRecord existing))
        {
            if (existing.Entity != null && existing.Entity != entity)
            {
                Debug.LogError(
                    $"[EnemyPersistence] Duplicate persistent ID '{id}' on " +
                    $"'{existing.Entity.name}' and '{entity.name}'.",
                    entity);
                return false;
            }

            existing.Entity = entity;
            if (entity.PrefabSource != null)
                existing.PrefabSource = entity.PrefabSource;
            UpdateRecordFromEntity(existing);
            return true;
        }

        EnemyRecord record = new EnemyRecord
        {
            PersistentId = id,
            Entity = entity,
            PrefabSource = entity.PrefabSource
        };
        UpdateRecordFromEntity(record);
        records.Add(id, record);
        return true;
    }

    public void MarkDead(string persistentId, Vector3 position)
    {
        if (string.IsNullOrWhiteSpace(persistentId)
            || !records.TryGetValue(persistentId, out EnemyRecord record))
        {
            return;
        }

        record.Alive = false;
        record.Position = position;
        record.CurrentHealth = 0;
    }

    public void NotifyDestroyed(string persistentId, EnemyPersistentEntity entity)
    {
        if (!string.IsNullOrWhiteSpace(persistentId)
            && records.TryGetValue(persistentId, out EnemyRecord record)
            && record.Entity == entity)
        {
            record.Entity = null;
        }
    }

    public List<EnemyPersistenceState> CaptureStates()
    {
        List<EnemyPersistenceState> states = new List<EnemyPersistenceState>(records.Count);
        foreach (EnemyRecord record in records.Values)
        {
            if (record.Entity != null && record.Entity.gameObject.activeInHierarchy)
                UpdateRecordFromEntity(record);

            states.Add(new EnemyPersistenceState(
                record.PersistentId,
                record.Alive,
                record.Position,
                record.CurrentHealth));
        }

        states.Sort((left, right) =>
            string.Compare(left.PersistentId, right.PersistentId, StringComparison.Ordinal));
        return states;
    }

    public void RestoreStates(IReadOnlyList<EnemyPersistenceState> states)
    {
        if (states == null)
            return;

        HashSet<string> restoredIds = new HashSet<string>(StringComparer.Ordinal);
        for (int i = 0; i < states.Count; i++)
        {
            EnemyPersistenceState state = states[i];
            if (string.IsNullOrWhiteSpace(state.PersistentId)
                || !restoredIds.Add(state.PersistentId))
            {
                Debug.LogError(
                    $"[EnemyPersistence] Invalid or duplicate enemy ID at restore index {i}.",
                    this);
                continue;
            }

            if (!records.TryGetValue(state.PersistentId, out EnemyRecord record))
            {
                Debug.LogWarning(
                    $"[EnemyPersistence] Saved enemy ID '{state.PersistentId}' is not registered in this Scene.",
                    this);
                continue;
            }

            record.Alive = state.Alive;
            record.Position = state.Position;
            record.CurrentHealth = state.Alive ? Mathf.Max(1, state.CurrentHealth) : 0;

            if (state.Alive)
                RestoreAlive(record);
            else
                RestoreDead(record);
        }

        Physics2D.SyncTransforms();
    }

    private void RestoreAlive(EnemyRecord record)
    {
        Vector3 savedPosition = record.Position;
        int savedHealth = record.CurrentHealth;
        EnemyPersistentEntity entity = record.Entity;
        if (entity == null)
        {
            if (record.PrefabSource == null)
            {
                Debug.LogError(
                    $"[EnemyPersistence] Cannot respawn '{record.PersistentId}': prefab source is missing.",
                    this);
                return;
            }

            GameObject instance = Instantiate(
                record.PrefabSource,
                savedPosition,
                Quaternion.identity);
            entity = instance.GetComponent<EnemyPersistentEntity>();
            if (entity == null)
            {
                Debug.LogError(
                    $"[EnemyPersistence] Prefab '{record.PrefabSource.name}' has no EnemyPersistentEntity.",
                    instance);
                Destroy(instance);
                return;
            }

            entity.ConfigureRuntime(record.PersistentId, record.PrefabSource, this);
            record.Entity = entity;
        }
        else if (!entity.gameObject.activeSelf)
        {
            entity.gameObject.SetActive(true);
        }

        entity.RestoreAlive(savedPosition, savedHealth);
        record.Entity = entity;
        record.Alive = true;
        record.Position = savedPosition;
        record.CurrentHealth = savedHealth;
        Debug.Log(
            $"[EnemyPersistence] Restored '{record.PersistentId}' alive at {savedPosition} with {savedHealth} HP.",
            entity);
    }

    private static void RestoreDead(EnemyRecord record)
    {
        if (record.Entity != null && record.Entity.gameObject.activeSelf)
            record.Entity.SuppressAsDead(record.Position);
    }

    private static void UpdateRecordFromEntity(EnemyRecord record)
    {
        Health health = record.Entity.Health;
        record.Position = record.Entity.transform.position;
        record.Alive = health != null && !health.IsDead && health.CurrentHealth > 0;
        record.CurrentHealth = health != null ? health.CurrentHealth : 0;
    }
}
