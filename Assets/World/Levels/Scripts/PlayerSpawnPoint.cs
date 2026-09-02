using UnityEngine;

[DisallowMultipleComponent]
public sealed class PlayerSpawnPoint : MonoBehaviour
{
    [SerializeField] private string spawnId = "default";

    public string SpawnId => spawnId;

    private void Start()
    {
        if (SceneLoader.CurrentLoadReason == SceneLoadReason.SaveRestore)
            return;

        PlayerController player = FindAnyObjectByType<PlayerController>();
        if (player == null)
        {
            Debug.LogError($"[SpawnPoint] No PlayerController exists for spawn '{spawnId}'.", this);
            return;
        }

        player.transform.position = transform.position;
        Rigidbody2D body = player.GetComponent<Rigidbody2D>();
        if (body != null)
        {
            body.position = transform.position;
            body.linearVelocity = Vector2.zero;
            body.angularVelocity = 0f;
        }

        Physics2D.SyncTransforms();
    }

    private void OnValidate()
    {
        if (string.IsNullOrWhiteSpace(spawnId))
            spawnId = "default";
    }

    private void OnDrawGizmos()
    {
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(transform.position, 0.35f);
        Gizmos.DrawLine(transform.position, transform.position + Vector3.up);
    }
}
