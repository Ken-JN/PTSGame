using UnityEngine;
using UnityEngine.Serialization;

public class EnemySpawner : MonoBehaviour
{
    public GameObject enemyPrefab;

    [FormerlySerializedAs("spawnDiSekitarPlayer")]
    public bool spawnAroundPlayer = true;

    [FormerlySerializedAs("spawnDistance")]
    public float spawnRadius = 8f;
    public float minSpawnRadius = 3f;
    public float spawnInterval = 3f;
    public int maxEnemies = 15;

    private Transform playerTarget;
    private float nextSpawnTime = 0f;

    private void Start()
    {
        FindPlayer();
        nextSpawnTime = Time.time;
    }

    private void Update()
    {
        if (enemyPrefab == null) return;

        if (spawnAroundPlayer && playerTarget == null)
        {
            FindPlayer();
            if (playerTarget == null) return;
        }

        if (Time.time >= nextSpawnTime)
        {
            nextSpawnTime = Time.time + spawnInterval;

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
        Vector3 center = (spawnAroundPlayer && playerTarget != null) 
            ? playerTarget.position 
            : transform.position;

        Vector2 randomDir = Random.insideUnitCircle.normalized;
        float randomDist = Random.Range(minSpawnRadius, spawnRadius);
        Vector3 spawnPos = center + new Vector3(randomDir.x, randomDir.y, 0f) * randomDist;

        GameObject newEnemy = Instantiate(enemyPrefab, spawnPos, Quaternion.identity);
        newEnemy.name = "Enemy_Spawned";
        newEnemy.tag = "Enemy";

        int enemyLayer = LayerMask.NameToLayer("Enemy");
        if (enemyLayer != -1)
        {
            newEnemy.layer = enemyLayer;
        }

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

        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(center, spawnRadius);

        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(center, minSpawnRadius);
    }
}
