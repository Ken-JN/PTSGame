using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
///     MASTER ENEMY CONTROLLER (TOPDOWN ENEMY) - ALL-IN-ONE
///     Skrip musuh tunggal yang komprehensif, mirip dengan TopDownPlayerMaster:
///     1. AI & Pergerakan (Mengejar Player, membalikkan sprite tubuh, animasi jalan)
///     2. Serangan Kontak (Memberikan 15 damage ke Player saat menyentuh fisik/trigger)
///     3. Sistem Darah & Animasi Hurt (Mengurangi HP, kilatan merah pada tubuh, animasi hurt)
///     4. Kematian Musuh (Musuh hancur, poin bertambah, TIDAK membekukan game)
///     5. Efek Terpental (Knockback saat terkena peluru)
/// </summary>
[RequireComponent(typeof(Rigidbody2D))]
public class TopDownEnemy : MonoBehaviour, IDamageable
{
    [Header("=== 1. AI & PERGERAKAN ===")]
    [Tooltip("Kecepatan jalan musuh mengejar player")]
    public float moveSpeed = 3f;

    [Tooltip("Jarak deteksi pandangan untuk mulai mengejar Player (dalam meter)")]
    public float followRange = 12f;

    [Tooltip("Jarak berhenti agar musuh tidak tumpang tindih dengan player")]
    public float stopDistance = 0.5f;

    [Header("=== 2. SERANGAN KONTAK ===")]
    [Tooltip("Jumlah damage yang diberikan ke Player saat menyentuh")]
    public float contactDamage = 15f;

    [Tooltip("Jeda waktu antar serangan jika terus menempel pada Player (detik)")]
    public float jedaSerang = 0.5f;
    private float waktuSerangTerakhir = -999f;

    [Header("=== 3. SISTEM DARAH & KEMATIAN ===")]
    [Tooltip("Darah maksimal musuh")]
    public float maxHealth = 50f;

    [Tooltip("Darah musuh saat ini")]
    public float currentHealth = 50f;

    [Tooltip("Warna kilatan merah saat terkena tembakan")]
    public Color warnaKenaHit = new Color(1f, 0.1f, 0.1f, 1f);

    [Tooltip("Poin yang didapat pemain ketika mengalahkan musuh ini")]
    public int poinSaatMati = 20;

    [Header("=== 4. TERPENTAL (KNOCKBACK) ===")]
    [Tooltip("Kekuatan terpental saat tertembak peluru")]
    public float knockBackPower = 3f;

    // Komponen Internal
    private Transform playerTarget;
    private Rigidbody2D rb;
    private Animator animator;
    private List<SpriteRenderer> bodyRenderers = new List<SpriteRenderer>();
    private List<Color> warnaAsliList = new List<Color>();
    private bool isDead = false;
    private Coroutine hurtResetCoroutine;
    private Transform mainSpriteTransform;
    private Vector3 initialMainSpriteScale = Vector3.one;
    private float stunEndTime = 0f;
    private Vector2 knockbackVelocity = Vector2.zero;

    // Hash Parameter Animator
    private static readonly int IsWalkingHash = Animator.StringToHash("IsWalking");
    private static readonly int AttackHash = Animator.StringToHash("Attack");
    private static readonly int IsHurtHash = Animator.StringToHash("IsHurt");
    private static readonly int HurtStateHash = Animator.StringToHash("hurt");

    private void Awake()
    {
        // 1. Matikan skrip-skrip modular lama jika ada agar tidak bentrok
        MatikanSkripLama();

        // 2. Ambil referensi komponen Rigidbody & Animator
        rb = GetComponent<Rigidbody2D>();
        animator = GetComponentInChildren<Animator>();

        // 3. Ambil semua SpriteRenderer pada tubuh (kecuali bayangan/shadow)
        SpriteRenderer[] allRenderers = GetComponentsInChildren<SpriteRenderer>(true);
        bodyRenderers.Clear();
        warnaAsliList.Clear();

        foreach (var sr in allRenderers)
        {
            if (sr != null && !sr.gameObject.name.ToLower().Contains("shadow"))
            {
                bodyRenderers.Add(sr);
                warnaAsliList.Add(sr.color);
            }
        }

        // Jika tidak ada selain shadow, pakai semua yang ada
        if (bodyRenderers.Count == 0 && allRenderers.Length > 0)
        {
            foreach (var sr in allRenderers)
            {
                if (sr != null)
                {
                    bodyRenderers.Add(sr);
                    warnaAsliList.Add(sr.color);
                }
            }
        }

        // 4. Ambil transform MainSprite untuk efek squash hurt
        mainSpriteTransform = transform.Find("MainSprite");
        if (mainSpriteTransform != null)
        {
            initialMainSpriteScale = mainSpriteTransform.localScale;
        }

        // 5. Pastikan darah penuh di awal
        currentHealth = maxHealth;
    }

    private void Start()
    {
        CariPlayer();
        currentHealth = maxHealth;
    }

    private void Update()
    {
        if (isDead) return;

        // Jika sedang terkena hit (stagger / stun), dorong musuh ke belakang dan jeda pengejaran sejenak
        if (Time.time < stunEndTime)
        {
            transform.position += (Vector3)knockbackVelocity * Time.deltaTime;
            knockbackVelocity = Vector2.Lerp(knockbackVelocity, Vector2.zero, Time.deltaTime * 10f);

            if (animator != null)
            {
                animator.SetBool(IsWalkingHash, false);
            }
            return;
        }

        // Cari target player jika belum ada
        if (playerTarget == null)
        {
            CariPlayer();
            if (playerTarget == null) return;
        }

        // Hitung jarak ke Player
        float jarakKePlayer = Vector2.Distance(transform.position, playerTarget.position);

        // Jika Player dalam jangkauan deteksi, kejar!
        if (jarakKePlayer <= followRange && jarakKePlayer > stopDistance)
        {
            KejarPlayer();
        }
        else
        {
            // Berhenti berjalan
            if (animator != null)
            {
                animator.SetBool(IsWalkingHash, false);
            }
        }
    }

    /// <summary>
    ///     Mencari GameObject Player di dalam scene
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
    ///     Menggerakkan musuh mendekati posisi Player dan membalikkan sprite tubuh
    /// </summary>
    private void KejarPlayer()
    {
        // Gerakkan musuh mendekati player
        transform.position = Vector2.MoveTowards(
            transform.position,
            playerTarget.position,
            moveSpeed * Time.deltaTime
        );

        // Arahkan sprite menghadap posisi Player (Kiri / Kanan)
        float arahX = playerTarget.position.x - transform.position.x;
        if (Mathf.Abs(arahX) > 0.05f)
        {
            bool flip = arahX < 0;
            for (int i = 0; i < bodyRenderers.Count; i++)
            {
                if (bodyRenderers[i] != null)
                {
                    bodyRenderers[i].flipX = flip;
                }
            }
        }

        // Aktifkan animasi berjalan
        if (animator != null)
        {
            animator.SetBool(IsWalkingHash, true);
        }
    }

    // =========================================================================
    // 2. SERANGAN KONTAK (DAMAGE SAAT MENYENTUH PLAYER)
    // =========================================================================

    private void OnCollisionEnter2D(Collision2D collision)
    {
        CobaSerang(collision.gameObject);
    }

    private void OnCollisionStay2D(Collision2D collision)
    {
        CobaSerang(collision.gameObject);
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        CobaSerang(other.gameObject);
    }

    private void OnTriggerStay2D(Collider2D other)
    {
        CobaSerang(other.gameObject);
    }

    /// <summary>
    ///     Memeriksa apakah target adalah Player dan memberikan damage kontak
    /// </summary>
    private void CobaSerang(GameObject target)
    {
        if (isDead) return;
        if (target == null) return;

        // Hanya serang jika target bertag "Player"
        if (!target.CompareTag("Player")) return;

        // Cek jeda serang (cooldown)
        if (Time.time < waktuSerangTerakhir + jedaSerang) return;
        waktuSerangTerakhir = Time.time;

        // Berikan damage ke Player
        TopDownCharacter2D.TopDownPlayerMaster playerMaster = target.GetComponent<TopDownCharacter2D.TopDownPlayerMaster>();
        if (playerMaster != null)
        {
            playerMaster.KurangiDarah(contactDamage);
        }
        else
        {
            IDamageable damageable = target.GetComponent<IDamageable>();
            if (damageable != null)
            {
                damageable.KenaDamage(Mathf.RoundToInt(contactDamage));
            }
            else
            {
                TopDownCharacter2D.Health.HealthSystem hs = target.GetComponent<TopDownCharacter2D.Health.HealthSystem>();
                if (hs != null)
                {
                    hs.KurangiDarah(contactDamage);
                }
            }
        }

        // Animasi serang jika ada
        if (animator != null)
        {
            animator.SetTrigger(AttackHash);
        }

        Debug.Log($"<color=red>[ENEMY ATTACK]</color> {gameObject.name} menyerang Player sebesar {contactDamage} damage!");
    }

    // =========================================================================
    // 3. SISTEM DARAH & ANIMASI HURT
    // =========================================================================

    /// <summary>
    ///     Dipanggil ketika musuh tertembak oleh peluru Player (implementasi IDamageable)
    /// </summary>
    public void KenaDamage(int damage)
    {
        KurangiDarah(damage);
    }

    /// <summary>
    ///     Dukungan kompatibilitas dengan skrip proyektil lama
    /// </summary>
    public bool ChangeHealth(float change)
    {
        if (change < 0)
        {
            return KurangiDarah(Mathf.Abs(change));
        }
        return false;
    }

    public bool KurangiDarah(float jumlahDamage)
    {
        if (isDead) return false;

        currentHealth -= jumlahDamage;
        Debug.Log($"<color=orange>[ENEMY HIT]</color> {gameObject.name} terkena tembakan {jumlahDamage} damage! Sisa HP: {currentHealth}/{maxHealth}");

        // 1. Berikan efek terpental (Knockback) menjauhi player jika belum diisi oleh peluru
        if (knockbackVelocity == Vector2.zero && playerTarget != null)
        {
            Vector2 arahDorong = ((Vector2)transform.position - (Vector2)playerTarget.position).normalized;
            if (arahDorong == Vector2.zero) arahDorong = Vector2.up;
            knockbackVelocity = arahDorong * (knockBackPower * 2.5f);
        }
        stunEndTime = Time.time + 0.2f;

        // 2. Mainkan animasi Hurt pada Animator musuh
        if (animator != null)
        {
            int hurtLayer = animator.GetLayerIndex("Hurt");
            if (hurtLayer != -1)
            {
                animator.SetLayerWeight(hurtLayer, 1f);
                animator.Play(HurtStateHash, hurtLayer, 0f);
            }
            else
            {
                animator.Play(HurtStateHash, 0, 0f);
            }

            animator.SetBool(IsHurtHash, true);
        }

        // 3. Mainkan efek visual animasi Hurt (Flash warna & Squash recoil)
        if (gameObject.activeInHierarchy)
        {
            if (hurtResetCoroutine != null)
            {
                StopCoroutine(hurtResetCoroutine);
            }
            hurtResetCoroutine = StartCoroutine(ProsesAnimasiHurt());
        }

        // 4. Jika darah habis, musuh mati
        if (currentHealth <= 0)
        {
            currentHealth = 0;
            Mati();
        }

        return true;
    }

    private IEnumerator ProsesAnimasiHurt()
    {
        // A. Kilatan warna merah terang pada semua sprite tubuh musuh
        for (int i = 0; i < bodyRenderers.Count; i++)
        {
            if (bodyRenderers[i] != null) bodyRenderers[i].color = warnaKenaHit;
        }

        // B. Efek deformasi squash & recoil pada MainSprite (terlihat jelas di layar)
        if (mainSpriteTransform != null)
        {
            mainSpriteTransform.localScale = new Vector3(
                initialMainSpriteScale.x * 0.8f,
                initialMainSpriteScale.y * 1.25f,
                initialMainSpriteScale.z
            );
        }

        yield return new WaitForSeconds(0.12f);

        // C. Kembalikan bentuk dan warna secara halus
        float durasiKembali = 0.1f;
        float elapsed = 0f;
        Vector3 currentScale = mainSpriteTransform != null ? mainSpriteTransform.localScale : initialMainSpriteScale;

        while (elapsed < durasiKembali)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / durasiKembali;

            if (mainSpriteTransform != null)
            {
                mainSpriteTransform.localScale = Vector3.Lerp(currentScale, initialMainSpriteScale, t);
            }

            for (int i = 0; i < bodyRenderers.Count; i++)
            {
                if (bodyRenderers[i] != null && i < warnaAsliList.Count)
                {
                    bodyRenderers[i].color = Color.Lerp(warnaKenaHit, warnaAsliList[i], t);
                }
            }

            yield return null;
        }

        // Pastikan kembali persis ke ukuran & warna semula
        if (mainSpriteTransform != null)
        {
            mainSpriteTransform.localScale = initialMainSpriteScale;
        }

        for (int i = 0; i < bodyRenderers.Count; i++)
        {
            if (bodyRenderers[i] != null && i < warnaAsliList.Count)
            {
                bodyRenderers[i].color = warnaAsliList[i];
            }
        }

        if (animator != null && !isDead)
        {
            animator.SetBool(IsHurtHash, false);
        }

        hurtResetCoroutine = null;
    }

    /// <summary>
    ///     Efek terpental ke belakang saat terkena peluru
    /// </summary>
    public void ApplyKnockBack(Transform attacker)
    {
        if (isDead || attacker == null) return;

        Vector2 arahDorong = ((Vector2)transform.position - (Vector2)attacker.position).normalized;
        if (arahDorong == Vector2.zero) arahDorong = Vector2.up;

        knockbackVelocity = arahDorong * (knockBackPower * 2.5f);
        stunEndTime = Time.time + 0.2f;

        if (rb != null)
        {
            rb.AddForce(arahDorong * (knockBackPower * 80f), ForceMode2D.Impulse);
        }
    }

    // =========================================================================
    // 4. KEMATIAN MUSUH (TIDAK MEMBEKUKAN GAME!)
    // =========================================================================

    private void Mati()
    {
        if (isDead) return;
        isDead = true;

        // PENTING: Kematian musuh BUKAN kematian player! Waktu game tetap berjalan normal!
        Debug.Log($"<color=green>[ENEMY DEFEATED]</color> Musuh '{gameObject.name}' dikalahkan! (+{poinSaatMati} Poin)");

        // Matikan collider agar tidak bisa terkena peluru lagi saat sedang hancur
        Collider2D col = GetComponent<Collider2D>();
        if (col != null) col.enabled = false;

        // Tambah poin pemain ke GameManager
        if (GameManager.Instance != null)
        {
            GameManager.Instance.TambahPoin(poinSaatMati);
        }

        // Hancurkan musuh
        Destroy(gameObject);
    }

    /// <summary>
    ///     Mematikan skrip pecahan lama agar tidak bentrok dengan TopDownEnemy
    /// </summary>
    private void MatikanSkripLama()
    {
        string[] oldComponentNames =
        {
            "CharacterStatsHandler",
            "TopDownContactEnemyController",
            "TopDownMovement",
            "HealthSystem",
            "DisappearOnDeath",
            "ChangeHealthOnTouch",
            "TopDownKnockBack",
            "TopDownFx",
            "SampleCharacterAnimation",
            "EnemyChildren",
            "Enemy",
            "BucketHeadZombie",
            "FlagZombie",
            "ConeZombie"
        };

        foreach (MonoBehaviour mb in GetComponents<MonoBehaviour>())
        {
            if (mb != null && mb != this)
            {
                string typeName = mb.GetType().Name;
                foreach (string oldName in oldComponentNames)
                {
                    if (typeName == oldName)
                    {
                        mb.enabled = false;
                        break;
                    }
                }
            }
        }
    }
}
