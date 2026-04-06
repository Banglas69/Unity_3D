using UnityEngine;

public class EnemySpawnerArea : MonoBehaviour
{
    [Header("Enemy")]
    public GameObject enemyPrefab;
    public int spawnCount = 3;

    [Header("Spawn Area")]
    public Vector3 boxSize = new Vector3(10f, 2f, 10f);
    public bool useWorldSpace = true;

    [Header("Spawn Settings")]
    public bool spawnOnStart = false;
    public bool onlySpawnOnce = false;
    public float groundRayHeight = 10f;
    public float groundRayDistance = 30f;
    public LayerMask groundMask = ~0;

    private bool hasSpawned;

    private void Start()
    {
        if (spawnOnStart)
            SpawnEnemies();
    }

    public void SpawnEnemies()
    {
        if (enemyPrefab == null)
            return;

        if (onlySpawnOnce && hasSpawned)
            return;

        hasSpawned = true;

        for (int i = 0; i < spawnCount; i++)
        {
            Vector3 spawnPos = GetRandomPointInBox();

            if (TryFindGround(spawnPos, out Vector3 groundedPos))
                spawnPos = groundedPos;

            Instantiate(enemyPrefab, spawnPos, Quaternion.identity);
        }
    }

    private Vector3 GetRandomPointInBox()
    {
        Vector3 half = boxSize * 0.5f;

        Vector3 localOffset = new Vector3(
            Random.Range(-half.x, half.x),
            Random.Range(-half.y, half.y),
            Random.Range(-half.z, half.z)
        );

        if (useWorldSpace)
            return transform.position + localOffset;

        return transform.TransformPoint(localOffset);
    }

    private bool TryFindGround(Vector3 point, out Vector3 groundedPoint)
    {
        Vector3 rayStart = point + Vector3.up * groundRayHeight;

        if (Physics.Raycast(rayStart, Vector3.down, out RaycastHit hit, groundRayDistance, groundMask, QueryTriggerInteraction.Ignore))
        {
            groundedPoint = hit.point;
            return true;
        }

        groundedPoint = point;
        return false;
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.green;

        Matrix4x4 oldMatrix = Gizmos.matrix;

        if (!useWorldSpace)
            Gizmos.matrix = transform.localToWorldMatrix;

        Gizmos.DrawWireCube(useWorldSpace ? transform.position : Vector3.zero, boxSize);

        Gizmos.matrix = oldMatrix;
    }
}