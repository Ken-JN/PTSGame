using UnityEngine;
using UnityEngine.Serialization;

namespace TopDownCharacter2D.Health
{
    /// <summary>
    ///     Skrip ini dipasang pada Musuh atau Bahaya (Spike/Jebakan).
    ///     Ketika menyentuh Player, skrip ini akan otomatis memberikan damage ke Player.
    /// </summary>
    public class ChangeHealthOnTouch : MonoBehaviour
    {
        [Header("Pengaturan Damage Kontak")]
        [Tooltip("Jumlah pengurangan darah saat menyentuh target")]
        [FormerlySerializedAs("value")]
        [SerializeField] private float damage = 15f;

        [Tooltip("Tag target yang boleh diserang")]
        [SerializeField] private string targetTag = "Player";

        [Tooltip("Jeda waktu antar serangan jika musuh terus menempel pada player (dalam detik)")]
        [SerializeField] private float jedaSerang = 0.5f;

        private float _waktuTerakhirSerang = -999f;

        // 1. Deteksi melalui Trigger (jika collider dicentang Is Trigger)
        private void OnTriggerEnter2D(Collider2D other)
        {
            BeriDamageKeTarget(other.gameObject);
        }

        private void OnTriggerStay2D(Collider2D other)
        {
            BeriDamageKeTarget(other.gameObject);
        }

        // 2. Deteksi melalui Tabrakan Fisik (Collider Biasa)
        private void OnCollisionEnter2D(Collision2D collision)
        {
            BeriDamageKeTarget(collision.gameObject);
        }

        private void OnCollisionStay2D(Collision2D collision)
        {
            BeriDamageKeTarget(collision.gameObject);
        }

        // 3. Fungsi utama memberikan damage
        private void BeriDamageKeTarget(GameObject target)
        {
            // Periksa apakah target memiliki Tag 'Player'
            if (!target.CompareTag(targetTag)) return;

            // Periksa apakah jeda waktu antar serangan sudah cukup
            if (Time.time < _waktuTerakhirSerang + jedaSerang) return;

            float damageDiberikan = Mathf.Abs(damage) > 0 ? Mathf.Abs(damage) : 15f;

            // 1. Cek TopDownPlayerMaster terlebih dahulu
            TopDownPlayerMaster playerMaster = target.GetComponentInParent<TopDownPlayerMaster>();
            if (playerMaster != null && playerMaster.enabled)
            {
                playerMaster.KurangiDarah(damageDiberikan);
                _waktuTerakhirSerang = Time.time;
                return;
            }

            // 2. Cek interface IDamageable
            IDamageable damageable = target.GetComponentInParent<IDamageable>();
            if (damageable != null)
            {
                damageable.KenaDamage((int)damageDiberikan);
                _waktuTerakhirSerang = Time.time;
                return;
            }

            // 3. Fallback ke HealthSystem
            HealthSystem hpSystem = target.GetComponentInParent<HealthSystem>();
            if (hpSystem != null && hpSystem.enabled)
            {
                hpSystem.KurangiDarah(damageDiberikan);
                _waktuTerakhirSerang = Time.time;
            }
        }
    }
}
