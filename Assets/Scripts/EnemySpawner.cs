using UnityEngine;
using UnityEngine.Serialization;

/// <summary>
///     RADIUS-BASED ENEMY SPAWNER
///     Periodically spawns enemies within a circular radius arena.
/// </summary>
public class EnemySpawner : MonoBehaviour
{
    [Header("=== 1. PREFAB & TARGET ===")]
    [Tooltip("Enemy prefab to instantiate")]
    public GameObject enemyPrefab;

    [Tooltip("Center of spawn area: if true, centers around the Player. If false, centers around this Spawner GameObject.")]
    [FormerlySerializedAs("spawnDiSekitarPlayer")]
    public bool spawnAroundPlayer = true;

    [Header("=== 2. RADIUS SETTINGS ===")]
    [Tooltip("Maximum radius for spawning enemies (furthest distance from center)")]
    [FormerlySerializedAs("spawnDistance")]
    public float spawnRadius = 8f;

    [Tooltip("Minimum radius for spawning enemies (safe distance so enemies do not spawn right on top of player)")]
    public float minSpawnRadius = 3f;

    [Header("=== 3. TIMING & LIMITS ===")]
    [Tooltip("Spawn interval in seconds between enemy waves")]
    public float spawnInterval = 3f;

    [Tooltip("Maximum number of alive enemies allowed in the arena at once")]
    public int maxEnemies = 15;

    // Internal references
    private Transform playerTarget;
    private float nextSpawnTime = 0f;

    private void Start()
    {
        FindPlayer();
        // Spawn immediately on start
        nextSpawnTime = Time.time;
    }

    private void Update()
    {
        if (enemyPrefab == null) return;

        // Find player if reference is null
        if (spawnAroundPlayer && playerTarget == null)
        {
            FindPlayer();
            if (playerTarget == null) return;
        }

        // Check if it is time to spawn
        if (Time.time >= nextSpawnTime)
        {
            nextSpawnTime = Time.time + spawnInterval;

            // Check current alive enemy count in the scene
            int currentEnemyCount = GameObject.FindGameObjectsWithTag("Enemy").Length;
            if (currentEnemyCount < maxEnemies)
            {
                SpawnEnemy();
            }
        }
    }

    /// <summary>
    ///     Finds the Player GameObject in the scene using tag "Player"
    /// </summary>
    public void FindPlayer()
    {
        GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
        if (playerObj != null)
        {
            playerTarget = playerObj.transform;
        }
    }

    /// <summary>
    ///     Spawns an enemy at a random position within the circular ring
    /// </summary>
    public void SpawnEnemy()
    {
        // 1. Determine center position
        Vector3 center = (spawnAroundPlayer && playerTarget != null) 
            ? playerTarget.position 
            : transform.position;

        // 2. Calculate random direction and distance in [minSpawnRadius, spawnRadius]
        Vector2 randomDir = Random.insideUnitCircle.normalized;
        float randomDist = Random.Range(minSpawnRadius, spawnRadius);

        // 3. Compose spawn position
        Vector3 spawnPos = center + new Vector3(randomDir.x, randomDir.y, 0f) * randomDist;

        // 4. Instantiate enemy
        GameObject newEnemy = Instantiate(enemyPrefab, spawnPos, Quaternion.identity);
        newEnemy.name = "Enemy_Spawned";
        newEnemy.tag = "Enemy";

        int enemyLayer = LayerMask.NameToLayer("Enemy");
        if (enemyLayer != -1)
        {
            newEnemy.layer = enemyLayer;
        }

        // Ensure enemy has TopDownEnemy component attached
        if (newEnemy.GetComponent<TopDownEnemy>() == null)
        {
            newEnemy.AddComponent<TopDownEnemy>();
        }

        Debug.Log($"<color=cyan>[SPAWNER]</color> Enemy spawned at: {spawnPos} (Distance: {randomDist:F1}m)");
    }

    /// <summary>
    ///     Draws gizmo circles in Scene view for visual debugging
    /// </summary>
    private void OnDrawGizmosSelected()
    {
        Vector3 center = (spawnAroundPlayer && Application.isPlaying && playerTarget != null) 
            ? playerTarget.position 
            : transform.position;

        // Outer circle (Max spawn radius) - Red
        Gizmos.color = new Color(1f, 0.2f, 0.2f, 0.7f);
        Gizmos.DrawWireSphere(center, spawnRadius);

        // Inner circle (Min spawn radius / safe zone) - Yellow
        Gizmos.color = new Color(1f, 0.92f, 0.016f, 0.7f);
        Gizmos.DrawWireSphere(center, minSpawnRadius);
    }
}
