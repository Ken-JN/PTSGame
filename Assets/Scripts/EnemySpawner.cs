using UnityEngine;
using UnityEngine.Serialization;

public class EnemySpawner : MonoBehaviour
{
    public GameObject enemyPrefab;

    [FormerlySerializedAs("spawnDiSekitarPlayer")]
    public bool spawnAroundPlayer = true;

    [Tooltip("Maximum radius for spawning enemies (furthest distance from center)")]
    [FormerlySerializedAs("spawnDistance")]
    public float spawnRadius = 8f;

    [Tooltip("Minimum radius for spawning enemies (safe distance so enemies do not spawn right on top of player)")]
    public float minSpawnRadius = 3f;

    public float spawnInterval = 3f;

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

    public void FindPlayer()
    {
        GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
        if (playerObj != null)
        {
            playerTarget = playerObj.transform;
        }
    }

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
    }

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
