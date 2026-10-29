using UnityEngine;
using UnityEngine.Serialization;

/// <summary>
///     SPAWNER MUSUH (ENEMY SPAWNER) BERBASIS RADIUS
///     Memunculkan musuh secara berkala di dalam area radius lingkaran.
/// </summary>
public class EnemySpawner : MonoBehaviour
{
    [Header("=== 1. PREFAB & TARGET ===")]
    [Tooltip("Prefab musuh yang akan dimunculkan")]
    public GameObject enemyPrefab;

    [Tooltip("Pusat area spawn: jika dicentang, radius berpusat pada Player. Jika tidak, berpusat pada GameObject Spawner ini.")]
    public bool spawnDiSekitarPlayer = true;

    [Header("=== 2. PENGATURAN RADIUS ===")]
    [Tooltip("Radius maksimal area spawn musuh (jarak terjauh dari pusat)")]
    [FormerlySerializedAs("spawnDistance")]
    public float spawnRadius = 8f;

    [Tooltip("Radius minimal area spawn (jarak aman agar musuh tidak langsung muncul tepat di badan player)")]
    public float minSpawnRadius = 3f;

    [Header("=== 3. WAKTU & BATASAN ===")]
    [Tooltip("Jeda waktu antar pemunculan musuh dalam detik")]
    public float spawnInterval = 3f;

    [Tooltip("Batas maksimal jumlah musuh aktif yang ada di arena")]
    public int maxEnemies = 15;

    // Referensi internal
    private Transform playerTarget;
    private float nextSpawnTime = 0f;

    private void Start()
    {
        CariPlayer();
        // Langsung spawn di awal permainan tanpa menunggu jeda pertama
        nextSpawnTime = Time.time;
    }

    private void Update()
    {
        if (enemyPrefab == null) return;

        // Jika player belum ditemukan, coba cari lagi
        if (spawnDiSekitarPlayer && playerTarget == null)
        {
            CariPlayer();
            if (playerTarget == null) return;
        }

        // Cek apakah sudah waktunya memunculkan musuh
        if (Time.time >= nextSpawnTime)
        {
            nextSpawnTime = Time.time + spawnInterval;

            // Hitung jumlah musuh yang saat ini masih hidup di arena
            int currentEnemyCount = GameObject.FindGameObjectsWithTag("Enemy").Length;
            if (currentEnemyCount < maxEnemies)
            {
                SpawnEnemy();
            }
        }
    }

    /// <summary>
    ///     Mencari GameObject Player di dalam scene menggunakan Tag "Player"
    /// </summary>
    private void CariPlayer()
    {
        GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
        if (playerObj != null)
        {
            playerTarget = playerObj.transform;
        }
    }

    /// <summary>
    ///     Memunculkan musuh di posisi acak dalam radius lingkaran
    /// </summary>
    private void SpawnEnemy()
    {
        // 1. Tentukan titik pusat spawn (posisi player atau posisi spawner ini)
        Vector3 titikPusat = (spawnDiSekitarPlayer && playerTarget != null) 
            ? playerTarget.position 
            : transform.position;

        // 2. Hitung arah sudut acak dan jarak acak dalam rentang [minSpawnRadius, spawnRadius]
        Vector2 arahAcak = Random.insideUnitCircle.normalized;
        float jarakAcak = Random.Range(minSpawnRadius, spawnRadius);

        // 3. Gabungkan menjadi posisi koordinat spawn
        Vector3 posisiSpawn = titikPusat + new Vector3(arahAcak.x, arahAcak.y, 0f) * jarakAcak;

        // 4. Munculkan musuh
        GameObject musuhBaru = Instantiate(enemyPrefab, posisiSpawn, Quaternion.identity);
        musuhBaru.name = "Enemy_Spawned";
        musuhBaru.tag = "Enemy";

        int enemyLayer = LayerMask.NameToLayer("Enemy");
        if (enemyLayer != -1)
        {
            musuhBaru.layer = enemyLayer;
        }

        // Pastikan musuh menggunakan skrip tunggal TopDownEnemy
        if (musuhBaru.GetComponent<TopDownEnemy>() == null)
        {
            musuhBaru.AddComponent<TopDownEnemy>();
        }

        Debug.Log($"<color=cyan>[SPAWNER]</color> Musuh muncul di koordinat: {posisiSpawn} (Jarak dari pusat: {jarakAcak:F1} meter)");
    }

    /// <summary>
    ///     Menampilkan visual lingkaran radius di Scene View Unity agar mudah dilihat
    /// </summary>
    private void OnDrawGizmosSelected()
    {
        Vector3 pusat = (spawnDiSekitarPlayer && Application.isPlaying && playerTarget != null) 
            ? playerTarget.position 
            : transform.position;

        // Lingkaran luar (Spawn Radius Maksimal) - Warna Merah
        Gizmos.color = new Color(1f, 0.2f, 0.2f, 0.7f);
        Gizmos.DrawWireSphere(pusat, spawnRadius);

        // Lingkaran dalam (Radius Minimal / Zona Aman) - Warna Kuning
        Gizmos.color = new Color(1f, 0.92f, 0.016f, 0.7f);
        Gizmos.DrawWireSphere(pusat, minSpawnRadius);
    }
}
