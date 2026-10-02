using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerMovement : MonoBehaviour, IDamageable
{
    [Header("Pergerakan")]
    public float kecepatan = 5f;
    private Vector2 arahGerak;

    [Header("Referensi Cahaya")]
    public Transform playerLight; // Drag Transform Light 2D di Inspector
    public float kecepatanRotasiLight = 15f;
    private Vector2 arahTerakhirCahaya = Vector2.down;

    [Header("Sistem Skor")]
    public int skor = 0;

    [Header("Health")]
    public int hp = 100;
    public float waktuKebalDetik = 0.5f;
    private float waktuKenaTerakhir = -999f;

    [Header("Attack")]
    public int damageSerang = 30;
    public float jarakSerang = 1.5f;

    void OnMove(InputValue value)
    {
        arahGerak = value.Get<Vector2>();

        // Simpan arah terakhir saat player bergerak agar cahaya tidak kembali ke (0,0) saat berhenti
        if (arahGerak.sqrMagnitude > 0.01f)
        {
            arahTerakhirCahaya = arahGerak.normalized;
        }
    }

    void OnFire()
    {
        Serang();
    }

    void Serang()
    {
        Debug.Log("<color=green>Player Menyerang!</color>");

        Collider2D[] hits = Physics2D.OverlapCircleAll(transform.position, jarakSerang);

        foreach (Collider2D hit in hits)
        {
            IDamageable damageable = hit.GetComponent<IDamageable>();
            if (damageable != null && hit.CompareTag("Enemy"))
            {
                damageable.KenaDamage(damageSerang);
                Debug.Log("<color=green>Kena Enemy!</color>");
            }
        }
    }

    void Update()
    {
        // 1. Pergerakan Player
        Vector3 arah = new Vector3(arahGerak.x, arahGerak.y, 0);
        transform.position += arah * kecepatan * Time.deltaTime;

        // 2. Rotasi Light 2D Mengikuti Arah
        RotateLight();
    }

    void RotateLight()
    {
        if (playerLight == null) return;

        // Hitung sudut dari Vector2 arahTerakhirCahaya
        float angle = Mathf.Atan2(arahTerakhirCahaya.y, arahTerakhirCahaya.x) * Mathf.Rad2Deg;

        // Sesuaikan offset (-90f jika cahaya bawaan menghadap Atas pada Euler 0)
        Quaternion targetRotation = Quaternion.Euler(0f, 0f, angle - 90f);

        playerLight.rotation = Quaternion.Lerp(
            playerLight.rotation,
            targetRotation,
            Time.deltaTime * kecepatanRotasiLight
        );
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Coin"))
        {
            Destroy(other.gameObject);
            skor++;
            Debug.Log($"<color=yellow>Skor Kamu Saat Ini: {skor}</color>");

            GameManager gm = Object.FindFirstObjectByType<GameManager>();
            if (gm != null)
            {
                gm.AmbilKoin();
            }
        }
    }

    public void KenaDamage(int jumlah)
    {
        if (Time.time < waktuKenaTerakhir + waktuKebalDetik)
        {
            return;
        }

        waktuKenaTerakhir = Time.time;
        hp -= jumlah;
        Debug.Log($"<color=red>Player kena {jumlah} damage! HP: {hp}</color>");

        if (hp <= 0)
        {
            Debug.Log("<color=red>GAME OVER!</color>");
            Destroy(gameObject);
        }
    }
}