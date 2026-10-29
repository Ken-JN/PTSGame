using UnityEngine;
using UnityEngine.Serialization;

namespace TopDownCharacter2D.Health
{
    /// <summary>
    ///     Attached to Enemies or Hazards (Spikes/Traps).
    ///     Automatically deals contact damage when touching the Player.
    /// </summary>
    public class ChangeHealthOnTouch : MonoBehaviour
    {
        [Header("Contact Damage Settings")]
        [Tooltip("Amount of damage dealt upon contact with target")]
        [FormerlySerializedAs("value")]
        [SerializeField] private float damage = 15f;

        [Tooltip("Tag of valid targets to attack")]
        [SerializeField] private string targetTag = "Player";

        [Tooltip("Cooldown delay in seconds between consecutive contact attacks")]
        [FormerlySerializedAs("jedaSerang")]
        [SerializeField] private float attackCooldown = 0.5f;

        private float _lastAttackTime = -999f;

        // 1. Trigger detection (if collider is set to Is Trigger)
        private void OnTriggerEnter2D(Collider2D other)
        {
            DealDamageToTarget(other.gameObject);
        }

        private void OnTriggerStay2D(Collider2D other)
        {
            DealDamageToTarget(other.gameObject);
        }

        // 2. Physical collision detection (solid colliders)
        private void OnCollisionEnter2D(Collision2D collision)
        {
            DealDamageToTarget(collision.gameObject);
        }

        private void OnCollisionStay2D(Collision2D collision)
        {
            DealDamageToTarget(collision.gameObject);
        }

        // 3. Main damage dealing logic
        private void DealDamageToTarget(GameObject target)
        {
            if (!target.CompareTag(targetTag)) return;

            if (Time.time < _lastAttackTime + attackCooldown) return;

            float damageAmount = Mathf.Abs(damage) > 0 ? Mathf.Abs(damage) : 15f;

            // 1. Check TopDownPlayerMaster first
            TopDownPlayerMaster playerMaster = target.GetComponentInParent<TopDownPlayerMaster>();
            if (playerMaster != null && playerMaster.enabled)
            {
                playerMaster.TakeDamage(damageAmount);
                _lastAttackTime = Time.time;
                return;
            }

            // 2. Check IDamageable interface
            IDamageable damageable = target.GetComponentInParent<IDamageable>();
            if (damageable != null)
            {
                damageable.TakeDamage(Mathf.RoundToInt(damageAmount));
                _lastAttackTime = Time.time;
                return;
            }

            // 3. Fallback to HealthSystem
            HealthSystem hpSystem = target.GetComponentInParent<HealthSystem>();
            if (hpSystem != null && hpSystem.enabled)
            {
                hpSystem.TakeDamage(damageAmount);
                _lastAttackTime = Time.time;
            }
        }
    }
}
