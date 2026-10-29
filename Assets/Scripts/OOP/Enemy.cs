using UnityEngine;
using System;

public class Enemy : MonoBehaviour, IDamageable
{
    [SerializeField] private int hp = 100;
    public float ms = 2f;

    protected Transform player;

    [Header("State Machine")]
    [SerializeField] private float JarakDeteksi = 6f;
    [SerializeField] private float JarakSerang = 1.5f;
    [SerializeField] private float JedaSerang = 0.3f;

    [Header("Patrol")]
    [SerializeField] private float radiusPatrol = 3f;
    private Vector2 titikAwal;
    private Vector2 tujuanPatrol;

    // State Now
    private StateZombie state = StateZombie.IDLE;
    private float waktuSerangTerakhir;

    // Event saat zombie mati
    public static event Action<Enemy> OnZombieMati;

    [SerializeField] private int damageSaatTabrakan = 15;

    protected virtual void Start()
    {
        GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
        if (playerObj != null)
        {
            player = playerObj.transform;
        }

        titikAwal = transform.position;
        PilihTujuanPatrolBaru();
    }

    void Update()
    {
        PeriksaTransisi();

        switch (state)
        {
            case StateZombie.IDLE: PerilakuIdle(); break;
            case StateZombie.PATROL: PerilakuPatrol(); break;
            case StateZombie.CHASE: PerilakuChase(); break;
            case StateZombie.ATTACK: PerilakuAttack(); break;
        }
    }

    public float JarakKePlayer()
    {
        if (player == null) return Mathf.Infinity;
        return Vector2.Distance(transform.position, player.position);
    }

    void PeriksaTransisi()
    {
        float jarak = JarakKePlayer();

        if (jarak <= JarakSerang)
        {
            state = StateZombie.ATTACK;
        }
        else if (jarak <= JarakDeteksi)
        {
            state = StateZombie.CHASE;
        }
        else
        {
            state = StateZombie.PATROL;
        }
    }

    void PilihTujuanPatrolBaru()
    {
        Vector2 acak = UnityEngine.Random.insideUnitCircle * radiusPatrol;
        tujuanPatrol = titikAwal + acak;
    }

    public void Kejar()
    {
        if (player == null) return;

        transform.position = Vector2.MoveTowards(
            transform.position,
            player.position,
            ms * Time.deltaTime
        );
    }

    public virtual void Serang()
    {
        Debug.Log("Enemy Serang");
    }

    void PerilakuIdle()
    {
    }

    void PerilakuPatrol()
    {
        transform.position = Vector2.MoveTowards(
            transform.position,
            tujuanPatrol,
            ms * 0.5f * Time.deltaTime
        );

        if (Vector2.Distance(transform.position, tujuanPatrol) < 0.1f)
        {
            PilihTujuanPatrolBaru();
        }
    }

    void PerilakuChase()
    {
        Kejar();
    }

    void PerilakuAttack()
    {
        if (Time.time >= waktuSerangTerakhir + JedaSerang)
        {
            Serang();
            waktuSerangTerakhir = Time.time;
        }
    }

    public void KenaDamage(int damage)
    {
        hp -= damage;
        Debug.Log("Enemy kena damage: " + damage + ", HP sekarang: " + hp);

        if (hp <= 0)
        {
            Mati();
        }
    }

    protected virtual void Mati()
    {
        Debug.Log("Enemy mati!");
        OnZombieMati?.Invoke(this);
        Destroy(gameObject);
    }

    [ContextMenu("Tes Event: Zombie Mati")]
    void TesEventMati()
    {
        Mati();
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        SerangPlayerDanHancur(other.gameObject);
    }

    void OnCollisionEnter2D(Collision2D collision)
    {
        SerangPlayerDanHancur(collision.gameObject);
    }

    private void SerangPlayerDanHancur(GameObject target)
    {
        if (target.CompareTag("Player"))
        {
            IDamageable playerScript = target.GetComponentInParent<IDamageable>();
            if (playerScript != null)
            {
                playerScript.KenaDamage(damageSaatTabrakan);
            }

            // Sesuai syarat tugas: Ketika sudah menyerang musuh akan hancur
            Debug.Log($"<color=yellow>{gameObject.name} hancur setelah menyerang player!</color>");
            Destroy(gameObject);
        }
    }
}
