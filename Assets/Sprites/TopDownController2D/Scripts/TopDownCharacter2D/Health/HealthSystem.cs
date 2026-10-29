using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;
using TopDownCharacter2D.Controllers;
using TopDownCharacter2D.Stats;

namespace TopDownCharacter2D.Health
{
    /// <summary>
    ///     Complete Health System for Player and Enemies.
    ///     Integrated with TopDownController2D, TopDownFx, SampleCharacterAnimation, and UI Slider.
    ///     Taking damage DOES NOT freeze the game! Freezing/Game Over ONLY happens when HP reaches 0!
    /// </summary>
    public class HealthSystem : MonoBehaviour, IDamageable
    {
        [Header("1. Character Type")]
        [Tooltip("Check if this component is on PLAYER. Uncheck if on ENEMY.")]
        [SerializeField] private bool isPlayer = true;

        [Header("2. Health Settings")]
        [Tooltip("Maximum health points (set to 100 so player takes multiple hits before dying)")]
        [SerializeField] private float maxHealth = 100f;

        [Tooltip("Current health points (starts full at game launch)")]
        [SerializeField] private float currentHealth = 100f;

        [Tooltip("Invincibility duration in seconds after being hit to prevent multi-hits in 1 frame")]
        [SerializeField] private float waktuKebal = 0.8f;

        [Header("3. Health Bar UI")]
        [Tooltip("Canvas holding the Slider (World Space above player or Screen Space on screen)")]
        [SerializeField] private Canvas healthBarCanvas;

        [Tooltip("UI Slider component for health")]
        [SerializeField] private Slider healthSlider;

        [Tooltip("Image component inside Slider Fill Area for color transition")]
        [SerializeField] private Image healthBarFill;

        [Tooltip("Position offset of health bar above character")]
        [SerializeField] private Vector3 posisiBarDarah = new Vector3(0f, 1.2f, 0f);

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

            // 2. Auto-detect tag (Hanya objek dengan tag Player yang dianggap Player)
            if (CompareTag("Player"))
            {
                isPlayer = true;
            }
            else
            {
                isPlayer = false;
            }

            // 3. Cache SpriteRenderer for damage flash (jangan ambil shadow)
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

            UpdateTampilanHealthBar();
        }

        private void Update()
        {
            // Check invincibility timer
            if (_isInvincible && Time.time >= _timeSinceLastDamage + waktuKebal)
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
                    healthBarCanvas.transform.position = transform.position + posisiBarDarah;
                    healthBarCanvas.transform.rotation = Quaternion.identity;
                }
            }
        }

        /// <summary>
        ///     IDamageable interface implementation
        /// </summary>
        public void KenaDamage(int damage)
        {
            TopDownEnemy enemy = GetComponent<TopDownEnemy>();
            if (enemy != null && enemy.enabled)
            {
                enemy.KenaDamage(damage);
                return;
            }
            KurangiDarah(damage);
        }

        /// <summary>
        ///     TopDownController2D compatibility
        /// </summary>
        public bool ChangeHealth(float change)
        {
            TopDownEnemy enemy = GetComponent<TopDownEnemy>();
            if (enemy != null && enemy.enabled)
            {
                return enemy.ChangeHealth(change);
            }
            if (change < 0)
            {
                return KurangiDarah(Mathf.Abs(change));
            }
            else if (change > 0)
            {
                return TambahDarah(change);
            }
            return false;
        }

        /// <summary>
        ///     Applies damage to the character
        /// </summary>
        public bool KurangiDarah(float jumlahDamage)
        {
            if (_isDead) return false;

            // Check invincibility timer
            if (Time.time < _timeSinceLastDamage + waktuKebal)
            {
                return false; // Still invincible, ignore hit
            }

            _timeSinceLastDamage = Time.time;
            _isInvincible = true;
            currentHealth -= jumlahDamage;

            if (currentHealth < 0)
            {
                currentHealth = 0;
            }

            Debug.Log($"<color=orange>[HIT]</color> {gameObject.name} took {jumlahDamage} damage! Remaining HP: {currentHealth}/{maxHealth}");

            // 1. Visual flash effect
            if (gameObject.activeInHierarchy)
            {
                StartCoroutine(FlashRedEffect());
            }

            // 2. Trigger onDamage event (animates hurt & triggers FX)
            onDamage.Invoke();

            // 3. Update UI Slider
            UpdateTampilanHealthBar();

            // 4. IMPORTANT: Game DOES NOT freeze on damage!
            // Only when HP reaches 0 does the character die!
            if (currentHealth <= 0)
            {
                ProsesKematian();
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

        public bool TambahDarah(float jumlahHeal)
        {
            if (_isDead) return false;

            currentHealth += jumlahHeal;
            if (currentHealth > maxHealth)
            {
                currentHealth = maxHealth;
            }

            UpdateTampilanHealthBar();
            onHeal.Invoke();
            return true;
        }

        private void ProsesKematian()
        {
            if (_isDead) return;
            _isDead = true;

            onDeath.Invoke();

            if (isPlayer && CompareTag("Player"))
            {
                Debug.Log("<color=red><b>[GAME OVER] PLAYER HAS DIED! GAME FREEZES!</b></color>");

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
                MatikanKontrolPlayer();

                // Freeze game upon player death
                Time.timeScale = 0f;
            }
            else
            {
                Debug.Log($"<color=green>[VICTORY] Enemy '{gameObject.name}' defeated!</color>");

                Collider2D col = GetComponent<Collider2D>();
                if (col != null) col.enabled = false;

                Destroy(gameObject, 0.2f);
            }
        }

        private void MatikanKontrolPlayer()
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

        public void UpdateTampilanHealthBar()
        {
            if (!isPlayer || healthSlider == null) return;

            float persentase = maxHealth > 0 ? Mathf.Clamp01(currentHealth / maxHealth) : 0f;
            healthSlider.value = persentase;

            if (healthBarFill != null)
            {
                if (persentase > 0.5f)
                {
                    healthBarFill.color = Color.Lerp(Color.yellow, new Color(0.2f, 0.9f, 0.3f, 1f), (persentase - 0.5f) * 2f);
                }
                else
                {
                    healthBarFill.color = Color.Lerp(new Color(0.9f, 0.15f, 0.15f, 1f), Color.yellow, persentase * 2f);
                }
            }
        }
    }
}
