using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;
using UnityEngine.Serialization;
using TopDownCharacter2D.Controllers;
using TopDownCharacter2D.Stats;

namespace TopDownCharacter2D.Health
{
    public class HealthSystem : MonoBehaviour, IDamageable
    {
        [SerializeField] private bool isPlayer = true;

        [Tooltip("Maximum health points (set to 100 so player takes multiple hits before dying)")]
        [SerializeField] private float maxHealth = 100f;

        [Tooltip("Current health points (starts full at game launch)")]
        [SerializeField] private float currentHealth = 100f;

        [FormerlySerializedAs("waktuKebal")]
        [SerializeField] private float invincibilityDuration = 0.8f;

        [Tooltip("Canvas holding the Slider (World Space above player or Screen Space on screen)")]
        [SerializeField] private Canvas healthBarCanvas;

        [SerializeField] private Slider healthSlider;

        [SerializeField] private Image healthBarFill;

        [FormerlySerializedAs("posisiBarDarah")]
        [SerializeField] private Vector3 healthBarOffset = new Vector3(0f, 1.2f, 0f);

        [Header("4. Events (Listened to by TopDownFx, Animations, and UI)")]
        [SerializeField] private UnityEvent onDamage;
        [SerializeField] private UnityEvent onHeal;
        [SerializeField] private UnityEvent onDeath;
        [SerializeField] private UnityEvent onInvincibilityEnd;

        // Internal state
        private CharacterStatsHandler _statsHandler;
        private SpriteRenderer _spriteRenderer;
        private Color _originalSpriteColor = Color.white;
        private float _timeSinceLastDamage = -999f;
        private bool _isInvincible = false;
        private bool _isDead = false;

        // Public properties
        public float CurrentHealth => currentHealth;
        public float MaxHealth => maxHealth;
        public bool IsDead => _isDead;
        public UnityEvent OnDamage => onDamage;
        public UnityEvent OnHeal => onHeal;
        public UnityEvent OnDeath => onDeath;
        public UnityEvent OnInvincibilityEnd => onInvincibilityEnd;

        private void Awake()
        {
            // 1. Ensure game time runs normally on start / scene reload
            Time.timeScale = 1f;

            // 2. Auto-detect tag (Only objects with tag Player are considered Player)
            if (CompareTag("Player"))
            {
                isPlayer = true;
            }
            else
            {
                isPlayer = false;
            }

            // 3. Cache SpriteRenderer for damage flash (exclude shadows)
            SpriteRenderer[] allRenderers = GetComponentsInChildren<SpriteRenderer>(true);
            foreach (var sr in allRenderers)
            {
                if (sr != null && !sr.gameObject.name.ToLower().Contains("shadow"))
                {
                    _spriteRenderer = sr;
                    break;
                }
            }
            if (_spriteRenderer == null && allRenderers.Length > 0)
            {
                _spriteRenderer = allRenderers[0];
            }
            if (_spriteRenderer != null)
            {
                _originalSpriteColor = _spriteRenderer.color;
            }

            // 4. Check CharacterStatsHandler: Ensure player has at least 100 HP!
            _statsHandler = GetComponent<CharacterStatsHandler>();
            if (_statsHandler != null && _statsHandler.CurrentStats != null)
            {
                if (_statsHandler.CurrentStats.maxHealth > maxHealth)
                {
                    maxHealth = _statsHandler.CurrentStats.maxHealth;
                }
            }

            if (maxHealth < 100f && isPlayer)
            {
                maxHealth = 100f;
            }

            // 5. FILL HEALTH FULLY IN AWAKE (never start with 0 HP!)
            currentHealth = maxHealth;

            // 6. Auto-detect UI Canvas / Fill if only Slider was assigned
            if (isPlayer && healthSlider != null)
            {
                if (healthBarCanvas == null)
                {
                    healthBarCanvas = healthSlider.GetComponentInParent<Canvas>();
                }

                if (healthBarFill == null && healthSlider.fillRect != null)
                {
                    healthBarFill = healthSlider.fillRect.GetComponent<Image>();
                }
            }
        }

        private void Start()
        {
            // Ensure health is full
            currentHealth = maxHealth;

            if (healthSlider != null)
            {
                healthSlider.minValue = 0f;
                healthSlider.maxValue = 1f;
            }

            UpdateHealthBarUI();
        }

        private void Update()
        {
            // Check invincibility timer
            if (_isInvincible && Time.time >= _timeSinceLastDamage + invincibilityDuration)
            {
                _isInvincible = false;
                onInvincibilityEnd.Invoke();
            }
        }

        private void LateUpdate()
        {
            // Keep health bar upright and follow player if using World Space canvas
            if (isPlayer && healthBarCanvas != null)
            {
                if (healthBarCanvas.renderMode == RenderMode.WorldSpace)
                {
                    healthBarCanvas.transform.position = transform.position + healthBarOffset;
                    healthBarCanvas.transform.rotation = Quaternion.identity;
                }
            }
        }

        public void TakeDamage(int damage)
        {
            TopDownEnemy enemy = GetComponent<TopDownEnemy>();
            if (enemy != null && enemy.enabled)
            {
                enemy.TakeDamage(damage);
                return;
            }
            TakeDamage((float)damage);
        }

        public bool ChangeHealth(float change)
        {
            TopDownEnemy enemy = GetComponent<TopDownEnemy>();
            if (enemy != null && enemy.enabled)
            {
                return enemy.ChangeHealth(change);
            }
            if (change < 0)
            {
                return TakeDamage(Mathf.Abs(change));
            }
            else if (change > 0)
            {
                return Heal(change);
            }
            return false;
        }

        public bool TakeDamage(float damageAmount)
        {
            if (_isDead) return false;

            // Check invincibility timer
            if (Time.time < _timeSinceLastDamage + invincibilityDuration)
            {
                return false; // Still invincible, ignore hit
            }

            _timeSinceLastDamage = Time.time;
            _isInvincible = true;
            currentHealth -= damageAmount;

            if (currentHealth < 0)
            {
                currentHealth = 0;
            }



            // 1. Visual flash effect
            if (gameObject.activeInHierarchy)
            {
                StartCoroutine(FlashRedEffect());
            }

            // 2. Trigger onDamage event (animates hurt & triggers FX)
            onDamage.Invoke();

            // 3. Update UI Slider
            UpdateHealthBarUI();

            // 4. IMPORTANT: Game DOES NOT freeze on damage!
            // Only when HP reaches 0 does the character die!
            if (currentHealth <= 0)
            {
                ProcessDeath();
            }

            return true;
        }

        private IEnumerator FlashRedEffect()
        {
            if (_spriteRenderer != null)
            {
                _spriteRenderer.color = new Color(1f, 0.25f, 0.25f, 0.9f);
                yield return new WaitForSeconds(0.15f);
                if (_spriteRenderer != null && !_isDead)
                {
                    _spriteRenderer.color = _originalSpriteColor;
                }
            }
        }

        public bool Heal(float healAmount)
        {
            if (_isDead) return false;

            currentHealth += healAmount;
            if (currentHealth > maxHealth)
            {
                currentHealth = maxHealth;
            }

            UpdateHealthBarUI();
            onHeal.Invoke();
            return true;
        }

        private void ProcessDeath()
        {
            if (_isDead) return;
            _isDead = true;

            onDeath.Invoke();

            if (isPlayer && CompareTag("Player"))
            {
   

                // Safely detach Main Camera so view is not lost
                if (Camera.main != null && Camera.main.transform.IsChildOf(transform))
                {
                    Camera.main.transform.SetParent(null);
                }

                // Hide health bar UI
                if (healthBarCanvas != null)
                {
                    healthBarCanvas.gameObject.SetActive(false);
                }

                // Disable player controllers & physics
                DisablePlayerControls();

                // Freeze game upon player death
                Time.timeScale = 0f;
            }
            else
            {

                Collider2D col = GetComponent<Collider2D>();
                if (col != null) col.enabled = false;

                Destroy(gameObject, 0.2f);
            }
        }

        private void DisablePlayerControls()
        {
            Rigidbody2D rb = GetComponent<Rigidbody2D>();
            if (rb != null)
            {
                rb.linearVelocity = Vector2.zero;
                rb.simulated = false;
            }

            foreach (Collider2D col in GetComponentsInChildren<Collider2D>())
            {
                col.enabled = false;
            }

            TopDownMovement movement = GetComponent<TopDownMovement>();
            if (movement != null) movement.enabled = false;

            TopDownCharacterController controller = GetComponent<TopDownCharacterController>();
            if (controller != null) controller.enabled = false;

            UnityEngine.InputSystem.PlayerInput playerInput = GetComponent<UnityEngine.InputSystem.PlayerInput>();
            if (playerInput != null) playerInput.enabled = false;
        }

        public void UpdateHealthBarUI()
        {
            if (!isPlayer || healthSlider == null) return;

            float percentage = maxHealth > 0 ? Mathf.Clamp01(currentHealth / maxHealth) : 0f;
            healthSlider.value = percentage;

            if (healthBarFill != null)
            {
                if (percentage > 0.5f)
                {
                    healthBarFill.color = Color.Lerp(Color.yellow, new Color(0.2f, 0.9f, 0.3f, 1f), (percentage - 0.5f) * 2f);
                }
                else
                {
                    healthBarFill.color = Color.Lerp(new Color(0.9f, 0.15f, 0.15f, 1f), Color.yellow, percentage * 2f);
                }
            }
        }
    }
}
