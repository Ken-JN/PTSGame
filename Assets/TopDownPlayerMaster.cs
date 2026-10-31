using System.Collections;
using System.Collections.Generic;
using TopDownCharacter2D.Attacks.Range;
using TopDownCharacter2D.Controllers;
using TopDownCharacter2D.Stats;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.InputSystem;
using UnityEngine.Rendering.Universal;
using UnityEngine.UI;

namespace TopDownCharacter2D
{
    [RequireComponent(typeof(Rigidbody2D))]
    public class TopDownPlayerMaster : TopDownCharacterController, IDamageable
    {
        [SerializeField] private CharacterStats baseStats;

        [SerializeField] private SpriteRenderer armRenderer;
        [SerializeField] private List<SpriteRenderer> characterRenderers = new List<SpriteRenderer>();
        [SerializeField] private Transform armPivot;

        [SerializeField] private float bulletDamage = 25f;
        [SerializeField] private GameObject bulletPrefab;
        [SerializeField] [Range(0.0f, 2f)] private float recoilStrength = 0.5f;
        [SerializeField] private bool projectileSizeModifyRecoil = true;
        [SerializeField] private Transform projectileSpawnPosition;
        [SerializeField] private AudioClip shootSound;

        [SerializeField] private Transform targetLightTransform;
        [SerializeField] private float lightRotationSpeed = 15f;
        [SerializeField] private float lightRotationOffset = -90f;

        [SerializeField] private bool isPlayer = true;
        [SerializeField] private float maxHealth = 100f;
        [SerializeField] private float currentHealth = 100f;
        [SerializeField] private float invincibilityDuration = 0.8f;
        [SerializeField] private Canvas healthBarCanvas;
        [SerializeField] private Slider healthSlider;
        [SerializeField] private Image healthBarFill;
        public GameObject diePanel;
        [SerializeField] private Vector3 healthBarOffset = new Vector3(0f, 1.2f, 0f);

        [SerializeField] private bool createDustOnWalk = true;
        [SerializeField] private ParticleSystem dustParticleSystem;

        [SerializeField] private UnityEvent onDamage = new UnityEvent();
        [SerializeField] private UnityEvent onHeal = new UnityEvent();
        [SerializeField] private UnityEvent onDeath = new UnityEvent();
        [SerializeField] private UnityEvent onInvincibilityEnd = new UnityEvent();

        public float BulletDamage => bulletDamage;
        public bool IsPlayer => isPlayer;
        public float CurrentHealth => currentHealth;
        public float MaxHealth => maxHealth;
        public bool IsDead => isDead;
        public UnityEvent OnDamage => onDamage;
        public UnityEvent OnHeal => onHeal;
        public UnityEvent OnDeath => onDeath;
        public UnityEvent OnInvincibilityEnd => onInvincibilityEnd;

        private Rigidbody2D rb;
        private Camera mainCamera;
        private Animator animator;
        private List<SpriteRenderer> allBodyRenderers = new List<SpriteRenderer>();
        private List<Color> originalColors = new List<Color>();
        private Coroutine flashCoroutine;
        private Vector2 moveInput;
        private Vector2 aimDirection = Vector2.right;
        private float nextFireTime = 0f;
        private float lastDamageTime = -999f;
        private bool isDead = false;

        protected override void Awake()
        {
            base.Awake();

            rb = GetComponent<Rigidbody2D>();
            animator = GetComponentInChildren<Animator>();
            mainCamera = Camera.main;

            currentHealth = maxHealth;

            allBodyRenderers.Clear();
            originalColors.Clear();
            SpriteRenderer[] srs = GetComponentsInChildren<SpriteRenderer>(true);
            foreach (var sr in srs)
            {
                if (sr != null && !sr.gameObject.name.ToLower().Contains("shadow"))
                {
                    allBodyRenderers.Add(sr);
                    originalColors.Add(sr.color);
                }
            }

            if (targetLightTransform == null)
            {
                var light2D = GetComponentInChildren<Light2D>();
                if (light2D != null) targetLightTransform = light2D.transform;
            }

            if (shootSound == null) shootSound = Resources.Load<AudioClip>("Sounds/Shoot");
            if (dustParticleSystem == null) dustParticleSystem = GetComponentInChildren<ParticleSystem>();
        }

        private void Start()
        {
            Time.timeScale = 1f;
            if (diePanel != null) diePanel.SetActive(false);
            UpdateHealthBarUI();
        }

        protected override void Update()
        {
            if (isDead) return;

            HandleMovementInput();
            HandleAimingAndLight();
            HandleShooting();
        }

        private void FixedUpdate()
        {
            if (isDead) return;
            float speed = (baseStats != null && baseStats.speed > 0) ? baseStats.speed : 5f;
            rb.linearVelocity = moveInput * speed;
        }

        private void LateUpdate()
        {
            if (healthBarCanvas != null && healthBarCanvas.renderMode == RenderMode.WorldSpace)
            {
                healthBarCanvas.transform.position = transform.position + healthBarOffset;
                healthBarCanvas.transform.rotation = Quaternion.identity;
            }
        }

        private void HandleMovementInput()
        {
            float x = Input.GetAxisRaw("Horizontal");
            float y = Input.GetAxisRaw("Vertical");

            if (Keyboard.current != null)
            {
                if (Keyboard.current.aKey.isPressed || Keyboard.current.leftArrowKey.isPressed) x = -1;
                else if (Keyboard.current.dKey.isPressed || Keyboard.current.rightArrowKey.isPressed) x = 1;

                if (Keyboard.current.sKey.isPressed || Keyboard.current.downArrowKey.isPressed) y = -1;
                else if (Keyboard.current.wKey.isPressed || Keyboard.current.upArrowKey.isPressed) y = 1;
            }

            moveInput = new Vector2(x, y).normalized;

            if (animator != null)
            {
                animator.SetBool("IsWalking", moveInput.sqrMagnitude > 0.01f);
            }
        }

        private void HandleAimingAndLight()
        {
            if (mainCamera == null) mainCamera = Camera.main;
            if (mainCamera == null) return;

            Vector3 mouseScreen = Input.mousePosition;
            if (Mouse.current != null) mouseScreen = Mouse.current.position.ReadValue();

            Vector3 mouseWorld = mainCamera.ScreenToWorldPoint(mouseScreen);
            aimDirection = ((Vector2)(mouseWorld - transform.position)).normalized;

            float angle = Mathf.Atan2(aimDirection.y, aimDirection.x) * Mathf.Rad2Deg;

            if (armPivot != null)
            {
                armPivot.rotation = Quaternion.Euler(0, 0, angle);
            }

            bool flip = (aimDirection.x < 0);
            if (armRenderer != null) armRenderer.flipY = flip;

            for (int i = 0; i < characterRenderers.Count; i++)
            {
                if (characterRenderers[i] != null) characterRenderers[i].flipX = flip;
            }

            if (targetLightTransform != null)
            {
                Quaternion targetRot = Quaternion.Euler(0, 0, angle + lightRotationOffset);
                if (lightRotationSpeed > 0)
                {
                    targetLightTransform.rotation = Quaternion.Lerp(targetLightTransform.rotation, targetRot, lightRotationSpeed * Time.deltaTime);
                }
                else
                {
                    targetLightTransform.rotation = targetRot;
                }
            }
        }

        private void HandleShooting()
        {
            bool isPressingShoot = Input.GetMouseButton(0);
            if (Mouse.current != null && Mouse.current.leftButton.isPressed)
            {
                isPressingShoot = true;
            }

            if (!isPressingShoot) return;

            float delay = (baseStats != null && baseStats.attackConfig != null) ? baseStats.attackConfig.delay : 0.25f;

            if (Time.time >= nextFireTime)
            {
                nextFireTime = Time.time + delay;
                ShootBullet();
            }
        }

        private void ShootBullet()
        {
            Transform spawnPoint = projectileSpawnPosition != null ? projectileSpawnPosition : transform;

            if (ProjectileManager.instance != null && baseStats != null && baseStats.attackConfig is RangedAttackConfig rangedConfig)
            {
                ProjectileManager.instance.ShootBullet(spawnPoint.position, aimDirection, rangedConfig);
            }
            else if (bulletPrefab != null)
            {
                float angle = Mathf.Atan2(aimDirection.y, aimDirection.x) * Mathf.Rad2Deg;
                GameObject bullet = Instantiate(bulletPrefab, spawnPoint.position, Quaternion.Euler(0, 0, angle));
                Rigidbody2D bulletRb = bullet.GetComponent<Rigidbody2D>();
                if (bulletRb != null)
                {
                    bulletRb.linearVelocity = aimDirection * 12f;
                }
            }

            if (shootSound != null) AudioSource.PlayClipAtPoint(shootSound, transform.position);

            if (rb != null)
            {
                float recoil = projectileSizeModifyRecoil ? (recoilStrength * 4f) : (recoilStrength * 2f);
                rb.AddForce(-aimDirection * recoil, ForceMode2D.Impulse);
            }
        }

        public void TakeDamage(int damage) => TakeDamage((float)damage);

        public bool ChangeHealth(float change)
        {
            if (change < 0) return TakeDamage(Mathf.Abs(change));
            return false;
        }

        public bool TakeDamage(float damageAmount)
        {
            if (isDead) return false;

            if (Time.time < lastDamageTime + invincibilityDuration) return false;
            lastDamageTime = Time.time;

            currentHealth -= damageAmount;
            UpdateHealthBarUI();

            if (animator != null)
            {
                animator.SetBool("IsHurt", true);
                animator.Play("hurt", -1, 0f);
            }

            if (gameObject.activeInHierarchy)
            {
                if (flashCoroutine != null) StopCoroutine(flashCoroutine);
                flashCoroutine = StartCoroutine(FlashRed());
            }

            onDamage?.Invoke();

            if (currentHealth <= 0)
            {
                currentHealth = 0;
                Die();
            }

            return true;
        }

        private IEnumerator FlashRed()
        {
            Color hitColor = new Color(1f, 0.2f, 0.2f, 1f);

            for (int i = 0; i < allBodyRenderers.Count; i++)
            {
                if (allBodyRenderers[i] != null) allBodyRenderers[i].color = hitColor;
            }

            yield return new WaitForSecondsRealtime(0.2f);

            for (int i = 0; i < allBodyRenderers.Count; i++)
            {
                if (allBodyRenderers.Count > 0 && allBodyRenderers[i] != null)
                {
                    allBodyRenderers[i].color = (i < originalColors.Count) ? originalColors[i] : Color.white;
                }
            }

            if (animator != null) animator.SetBool("IsHurt", false);
            flashCoroutine = null;
        }

        private void UpdateHealthBarUI()
        {
            if (healthSlider != null)
            {
                healthSlider.value = Mathf.Clamp01(currentHealth / maxHealth);
            }
        }

        public void Die()
        {
            if (isDead) return;
            isDead = true;

            if (rb != null) rb.linearVelocity = Vector2.zero;
            Time.timeScale = 0f;

            if (diePanel != null)
            {
                diePanel.SetActive(true);
            }

            if (GameManager.Instance != null)
            {
                GameManager.Instance.GameOver();
            }

            onDeath?.Invoke();
        }

        public void CreateDustParticles()
        {
            if (createDustOnWalk && dustParticleSystem != null)
            {
                dustParticleSystem.Stop();
                dustParticleSystem.Play();
            }
        }

        public void InvincibilityEnd()
        {
            if (animator != null) animator.SetBool("IsHurt", false);
            onInvincibilityEnd?.Invoke();
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (other.CompareTag("Coin"))
            {
                Destroy(other.gameObject);
                if (GameManager.Instance != null)
                {
                    GameManager.Instance.CollectCoin();
                }
            }
        }
    }
}