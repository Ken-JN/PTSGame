using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Linq;
using TopDownCharacter2D.Attacks;
using TopDownCharacter2D.Attacks.Melee;
using TopDownCharacter2D.Attacks.Range;
using TopDownCharacter2D.Controllers;
using TopDownCharacter2D.FX;
using TopDownCharacter2D.Items;
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
        #region 1. Stats Settings (CharacterStatsHandler)

        [SerializeField] private CharacterStats baseStats;

        public readonly ObservableCollection<CharacterStats> statsModifiers = new ObservableCollection<CharacterStats>();
        public CharacterStats CurrentStats { get; private set; }

        private const float MinAttackDelay = 0.03f;
        private const float MinAttackPower = 0.5f;
        private const float MinAttackSize = 0.4f;
        private const float MinAttackSpeed = .1f;
        private const float MinSpeed = 1f;
        private const int MinMaxHealth = 1;

        #endregion

        #region 2. Aim & Rotation Settings (TopDownAimRotation)

        [SerializeField] private SpriteRenderer armRenderer;

        [SerializeField] private List<SpriteRenderer> characterRenderers = new List<SpriteRenderer>();

        [SerializeField] private Transform armPivot;

        #endregion

        #region 3. Shooting Settings (TopDownShooting)

        [SerializeField] private float bulletDamage = 25f;

        [SerializeField] private GameObject bulletPrefab;

        [SerializeField] [Range(0.0f, 2f)] private float recoilStrength = 0.5f;

        [SerializeField] private bool projectileSizeModifyRecoil = true;

        [SerializeField] private Transform projectileSpawnPosition;

        [SerializeField] private AudioClip shootSound;

        private Vector2 _aimDirection = Vector2.right;
        private ProjectileManager _projectileManager;

        #endregion

        #region 4. Light / Flashlight Settings (LightDirectionHandler)

        [Tooltip("Transform of 2D Light (child of player)")]
        [SerializeField] private Transform targetLightTransform;

        [SerializeField] private float lightRotationSpeed = 15f;

        [Tooltip("Rotation offset angle in degrees (e.g. -90 if light sprite faces upward)")]
        [SerializeField] private float lightRotationOffset = -90f;

        #endregion

        #region 5. Health & UI Settings (HealthSystem)

        [SerializeField] private bool isPlayer = true;

        [Tooltip("Maximum health points (set to at least 100 for player)")]
        [SerializeField] private float maxHealth = 100f;

        [SerializeField] private float currentHealth = 100f;

        [SerializeField] private float invincibilityDuration = 0.8f;

        [SerializeField] private Canvas healthBarCanvas;

        [SerializeField] private Slider healthSlider;

        [SerializeField] private Image healthBarFill;

        public GameObject diePanel;

        [Tooltip("Health bar position offset above character (if World Space)")]
        [SerializeField] private Vector3 healthBarOffset = new Vector3(0f, 1.2f, 0f);

        [SerializeField] private UnityEvent onDamage = new UnityEvent();
        [SerializeField] private UnityEvent onHeal = new UnityEvent();
        [SerializeField] private UnityEvent onDeath = new UnityEvent();
        [SerializeField] private UnityEvent onInvincibilityEnd = new UnityEvent();

        public float CurrentHealth => currentHealth;
        public float MaxHealth => maxHealth;
        public bool IsDead => _isDead;
        public UnityEvent OnDamage => onDamage;
        public UnityEvent OnHeal => onHeal;
        public UnityEvent OnDeath => onDeath;
        public UnityEvent OnInvincibilityEnd => onInvincibilityEnd;

        private SpriteRenderer _mainSpriteRenderer;
        private Color _originalSpriteColor = Color.white;
        private float _timeSinceLastDamage = -999f;
        private bool _isInvincible = false;
        private bool _isDead = false;

        #endregion

        #region 6. Knockback Settings (TopDownKnockBack)

        [SerializeField] private float knockBackPower = 5f;
        [SerializeField] private float timeBetweenKnockBack = 0.1f;

        private float _timeSinceLastKnockBack = float.MaxValue;

        #endregion

        #region 7. Animation & Dust Settings (SampleCharacterAnimation)

        [SerializeField] private Animator animator;
        [SerializeField] private bool createDustOnWalk = true;
        [SerializeField] private ParticleSystem dustParticleSystem;

        private static readonly int IsWalkingHash = Animator.StringToHash("IsWalking");
        private static readonly int AttackHash = Animator.StringToHash("Attack");
        private static readonly int IsHurtHash = Animator.StringToHash("IsHurt");

        #endregion

        #region 8. Visual & Sound Effects (TopDownFx)

        [Header("=== 8. FX (VISUAL & SOUND) ===")]
        [SerializeField] private List<Effect> effects = new List<Effect>();

        private PickupItem _pickup;

        #endregion

        #region Internal References

        private Camera _mainCamera;
        private Rigidbody2D _rb;
        private Vector2 _movementDirection = Vector2.zero;

        #endregion

        #region Unity Lifecycle

        protected override void Awake()
        {
            // 1. Initialize Stats first as base.Awake() on TopDownCharacterController
            //    may require stats components/data.
            UpdateCharacterStats(null, null);
            statsModifiers.CollectionChanged += UpdateCharacterStats;

            base.Awake();

            // 2. Ensure game time runs normally
            Time.timeScale = 1f;

            // 3. Cache main components
            _mainCamera = Camera.main;
            _rb = GetComponent<Rigidbody2D>();
            _pickup = GetComponent<PickupItem>();

            if (animator == null)
            {
                animator = GetComponentInChildren<Animator>();
            }

            // 4. Auto-detect Light2D
            if (targetLightTransform == null)
            {
                var light2D = GetComponentInChildren<Light2D>();
                if (light2D != null)
                {
                    targetLightTransform = light2D.transform;
                }
            }

            // 5. Setup Health System
            InitializeHealthSystem();

            // 6. Cache shoot sound if configured in effects list or load from Resources
            if (shootSound == null)
            {
                if (effects != null)
                {
                    var attackFx = effects.FirstOrDefault(e => e.triggerEvent == TriggerEvents.Attack && e.soundEffect != null);
                    if (attackFx != null) shootSound = attackFx.soundEffect;
                }
                if (shootSound == null)
                {
                    shootSound = Resources.Load<AudioClip>("Sounds/Shoot");
                }
            }
        }

        private void Start()
        {
            _projectileManager = ProjectileManager.instance;

            // Connect internal events (Movement, Aim, Shooting, Animation)
            OnMoveEvent.AddListener(OnMoveReceived);
            LookEvent.AddListener(OnAimReceived);
            OnAttackEvent.AddListener(OnAttackReceived);

            // Connect Health events to animations
            onDamage.AddListener(OnHurtAnimation);
            onInvincibilityEnd.AddListener(InvincibilityEndAnimation);

            // Setup UI Health Bar
            currentHealth = maxHealth;
            if (healthSlider != null)
            {
                healthSlider.minValue = 0f;
                healthSlider.maxValue = 1f;
            }
            UpdateHealthBarUI();

            if (diePanel != null)
            {
                diePanel.SetActive(false);
            }

            // Setup FX Listeners
            InitializeFxListeners();
        }

        [System.NonSerialized] private float _playerAttackTimer = float.MaxValue;

        protected override void Update()
        {
            // 0. Update input dan eksekusi menembak
            HandlePlayerShooting();

            // 1. Update flashlight direction (LightDirectionHandler)
            UpdateLightDirection();

            // 2. Update invincibility timer (HealthSystem)
            if (_isInvincible && Time.time >= _timeSinceLastDamage + invincibilityDuration)
            {
                _isInvincible = false;
                onInvincibilityEnd.Invoke();
            }

            // 3. Update timer knockback (TopDownKnockBack)
            if (_timeSinceLastKnockBack < timeBetweenKnockBack)
            {
                _timeSinceLastKnockBack += Time.deltaTime;
            }
        }

        private void HandlePlayerShooting()
        {
            if (_isDead) return;

            // Direct mouse click support (ensures firing if New Input System is not yet active)
            if (Mouse.current != null)
            {
                if (Mouse.current.leftButton.wasReleasedThisFrame) IsAttacking = false;
                else if (Mouse.current.leftButton.isPressed) IsAttacking = true;
            }
            else
            {
                if (Input.GetMouseButtonUp(0)) IsAttacking = false;
                else if (Input.GetMouseButton(0)) IsAttacking = true;
            }

            AttackConfig currentAttackConfig = (CurrentStats != null && CurrentStats.attackConfig != null)
                ? CurrentStats.attackConfig
                : (baseStats != null ? baseStats.attackConfig : null);

            if (currentAttackConfig == null) return;

            float attackDelay = currentAttackConfig.delay > 0 ? currentAttackConfig.delay : 0.2f;

            if (_playerAttackTimer <= attackDelay)
            {
                _playerAttackTimer += Time.deltaTime;
            }

            if (IsAttacking && _playerAttackTimer > attackDelay)
            {
                _playerAttackTimer = 0f;
                OnAttackEvent.Invoke(currentAttackConfig);
            }
        }

        private void FixedUpdate()
        {
            // Apply physics movement (TopDownMovement)
            ApplyMovement(_movementDirection);
        }

        private void LateUpdate()
        {
            // Keep health bar position & rotation above player if using World Space Canvas
            if (isPlayer && healthBarCanvas != null)
            {
                if (healthBarCanvas.renderMode == RenderMode.WorldSpace)
                {
                    healthBarCanvas.transform.position = transform.position + healthBarOffset;
                    healthBarCanvas.transform.rotation = Quaternion.identity;
                }
            }
        }

        #endregion

        #region Input System Methods (TopDownInputController)

        public void OnMove(InputValue value)
        {
            if (_isDead) return;
            Vector2 moveInput = value.Get<Vector2>().normalized;
            OnMoveEvent.Invoke(moveInput);
        }

        public void OnLook(InputValue value)
        {
            if (_isDead) return;
            Vector2 newAim = value.Get<Vector2>();
            if (!(newAim.normalized == newAim))
            {
                if (_mainCamera == null) _mainCamera = Camera.main;
                if (_mainCamera != null)
                {
                    Vector2 worldPos = _mainCamera.ScreenToWorldPoint(newAim);
                    newAim = (worldPos - (Vector2)transform.position).normalized;
                }
            }

            if (newAim.magnitude >= .9f)
            {
                LookEvent.Invoke(newAim);
            }
        }

        public void OnFire(InputValue value)
        {
            if (_isDead)
            {
                IsAttacking = false;
                return;
            }
            IsAttacking = value.isPressed;
        }

        #endregion

        #region Movement Logic (TopDownMovement)

        private void OnMoveReceived(Vector2 direction)
        {
            _movementDirection = direction;
            UpdateWalkAnimation(direction);
        }

        private void ApplyMovement(Vector2 direction)
        {
            if (_isDead || _rb == null || CurrentStats == null) return;
            _rb.linearVelocity += direction * CurrentStats.speed;
        }

        #endregion

        #region Aim & Shooting Logic (TopDownAimRotation & TopDownShooting)

        private void OnAimReceived(Vector2 newAimDirection)
        {
            _aimDirection = newAimDirection;
            RotateArm(newAimDirection);
        }

        private void RotateArm(Vector2 direction)
        {
            float rotZ = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;

            if (armRenderer != null)
            {
                armRenderer.flipY = Mathf.Abs(rotZ) > 90f;
                if (characterRenderers != null)
                {
                    foreach (SpriteRenderer charRenderer in characterRenderers)
                    {
                        if (charRenderer != null)
                        {
                            charRenderer.flipX = armRenderer.flipY;
                        }
                    }
                }
            }

            if (armPivot != null)
            {
                armPivot.rotation = Quaternion.Euler(0, 0, rotZ);
            }
        }

        private void OnAttackReceived(AttackConfig attackConfig)
        {
            TriggerAttackAnimation();

            if (attackConfig is RangedAttackConfig rangedAttackConfig)
            {
                ExecuteShoot(rangedAttackConfig);
            }
        }

        public void ExecuteShoot(RangedAttackConfig rangedAttackConfig)
        {
            if (rangedAttackConfig == null) return;

            float projectilesAngleSpace = rangedAttackConfig.multipleProjectilesAngle;
            float minAngle = -(rangedAttackConfig.numberOfProjectilesPerShot / 2f) * projectilesAngleSpace +
                             0.5f * rangedAttackConfig.multipleProjectilesAngle;

            for (int i = 0; i < rangedAttackConfig.numberOfProjectilesPerShot; i++)
            {
                float angle = minAngle + projectilesAngleSpace * i;
                float randomSpread = UnityEngine.Random.Range(-rangedAttackConfig.spread, rangedAttackConfig.spread);
                angle += randomSpread;
                CreateProjectile(rangedAttackConfig, angle);
            }

            if (_rb != null)
            {
                AddRecoil(rangedAttackConfig);
            }
        }

        private void CreateProjectile(RangedAttackConfig rangedAttackConfig, float angle)
        {
            if (_projectileManager == null)
            {
                _projectileManager = ProjectileManager.instance != null 
                    ? ProjectileManager.instance 
                    : FindFirstObjectByType<ProjectileManager>();
            }

            if (projectileSpawnPosition == null)
            {
                Transform foundSpawn = transform.Find("WeaponPivot/BulletSpawnPoint");
                if (foundSpawn != null) projectileSpawnPosition = foundSpawn;
                else projectileSpawnPosition = transform;
            }

            if (bulletDamage > 0)
            {
                rangedAttackConfig.power = bulletDamage;
            }

            Vector2 shootDir = RotateVector2(_aimDirection.normalized, angle);

            if (_projectileManager != null && projectileSpawnPosition != null)
            {
                _projectileManager.ShootBullet(
                    projectileSpawnPosition.position,
                    shootDir,
                    rangedAttackConfig
                );
            }
            else if (bulletPrefab != null && projectileSpawnPosition != null)
            {
                // Fallback direct instantiation if ProjectileManager is absent
                GameObject bulletObj = Instantiate(bulletPrefab, projectileSpawnPosition.position, Quaternion.identity);
                TopDownCharacter2D.Attacks.Range.RangedAttackController rangedAttack = bulletObj.GetComponent<TopDownCharacter2D.Attacks.Range.RangedAttackController>();
                if (rangedAttack != null)
                {
                    rangedAttack.InitializeAttack(shootDir, rangedAttackConfig, null);
                }
                bulletObj.SetActive(true);
            }
        }

        private static Vector2 RotateVector2(Vector2 v, float degrees)
        {
            return Quaternion.Euler(0, 0, degrees) * v;
        }

        private void AddRecoil(RangedAttackConfig rangedAttackConfig)
        {
            if (projectileSizeModifyRecoil)
            {
                _rb.AddForce(-(_aimDirection * (rangedAttackConfig.size * recoilStrength * 100f)), ForceMode2D.Impulse);
            }
            else
            {
                _rb.AddForce(-(_aimDirection * (recoilStrength * 100f)), ForceMode2D.Impulse);
            }
        }

        #endregion

        #region Flashlight / Light2D Logic (LightDirectionHandler)

        private void UpdateLightDirection()
        {
            if (targetLightTransform == null) return;

            if (_mainCamera == null)
            {
                _mainCamera = Camera.main;
                if (_mainCamera == null) return;
            }

            Vector2 mouseScreenPos;
            if (Mouse.current != null)
            {
                mouseScreenPos = Mouse.current.position.ReadValue();
            }
            else
            {
                mouseScreenPos = Input.mousePosition;
            }

            Vector3 mouseWorldPos = _mainCamera.ScreenToWorldPoint(
                new Vector3(mouseScreenPos.x, mouseScreenPos.y, -_mainCamera.transform.position.z)
            );
            mouseWorldPos.z = 0f;

            Vector2 directionToMouse = (Vector2)(mouseWorldPos - targetLightTransform.position);
            if (directionToMouse.sqrMagnitude < 0.001f) return;

            float angle = Mathf.Atan2(directionToMouse.y, directionToMouse.x) * Mathf.Rad2Deg;
            Quaternion targetRotation = Quaternion.Euler(0f, 0f, angle + lightRotationOffset);

            targetLightTransform.rotation = Quaternion.Lerp(
                targetLightTransform.rotation,
                targetRotation,
                Time.deltaTime * lightRotationSpeed
            );
        }

        #endregion

        #region Stats Handler Logic (CharacterStatsHandler)

        private void UpdateCharacterStats(object sender, NotifyCollectionChangedEventArgs e)
        {
            if (baseStats == null)
            {
                CurrentStats = new CharacterStats { maxHealth = (int)maxHealth, speed = 5f };
                return;
            }

            AttackConfig config = null;
            if (baseStats.attackConfig != null)
            {
                config = Instantiate(baseStats.attackConfig);
            }

            CurrentStats = new CharacterStats { attackConfig = config };
            UpdateStats((a, b) => b, baseStats);
            if (CurrentStats.attackConfig != null && baseStats.attackConfig != null)
            {
                CurrentStats.attackConfig.target = baseStats.attackConfig.target;
            }

            foreach (CharacterStats modifier in statsModifiers.OrderBy(o => o.statsChangeType))
            {
                if (modifier.statsChangeType == StatsChangeType.Override)
                {
                    UpdateStats((o, o1) => o1, modifier);
                }
                else if (modifier.statsChangeType == StatsChangeType.Add)
                {
                    UpdateStats((o, o1) => o + o1, modifier);
                }
                else if (modifier.statsChangeType == StatsChangeType.Multiply)
                {
                    UpdateStats((o, o1) => o * o1, modifier);
                }
            }

            LimitAllStats();
        }

        private void UpdateStats(Func<float, float, float> operation, CharacterStats newModifier)
        {
            if (newModifier == null) return;

            CurrentStats.maxHealth = (int)operation(CurrentStats.maxHealth, newModifier.maxHealth);
            CurrentStats.speed = operation(CurrentStats.speed, newModifier.speed);

            if (newModifier.attackConfig == null || CurrentStats.attackConfig == null)
            {
                return;
            }

            CurrentStats.attackConfig.delay = operation(CurrentStats.attackConfig.delay, newModifier.attackConfig.delay);
            CurrentStats.attackConfig.power = operation(CurrentStats.attackConfig.power, newModifier.attackConfig.power);
            CurrentStats.attackConfig.size = operation(CurrentStats.attackConfig.size, newModifier.attackConfig.size);
            CurrentStats.attackConfig.speed = operation(CurrentStats.attackConfig.speed, newModifier.attackConfig.speed);

            if (CurrentStats.attackConfig.GetType() != newModifier.attackConfig.GetType())
            {
                return;
            }

            switch (CurrentStats.attackConfig)
            {
                case RangedAttackConfig _:
                    ApplyRangedStats(operation, newModifier);
                    break;
                case MeleeAttackConfig _:
                    ApplyMeleeStats(operation, newModifier);
                    break;
            }
        }

        private void LimitAllStats()
        {
            if (CurrentStats == null) return;

            CurrentStats.speed = CurrentStats.speed < MinSpeed ? MinSpeed : CurrentStats.speed;
            CurrentStats.maxHealth = CurrentStats.maxHealth < MinMaxHealth ? MinMaxHealth : CurrentStats.maxHealth;

            if (CurrentStats.attackConfig == null) return;

            CurrentStats.attackConfig.delay = CurrentStats.attackConfig.delay < MinAttackDelay
                ? MinAttackDelay
                : CurrentStats.attackConfig.delay;
            CurrentStats.attackConfig.power = bulletDamage > 0 
                ? bulletDamage 
                : (CurrentStats.attackConfig.power < MinAttackPower ? MinAttackPower : CurrentStats.attackConfig.power);
            CurrentStats.attackConfig.size = CurrentStats.attackConfig.size < MinAttackSize
                ? MinAttackSize
                : CurrentStats.attackConfig.size;
            CurrentStats.attackConfig.speed = CurrentStats.attackConfig.speed < MinAttackSpeed
                ? MinAttackSpeed
                : CurrentStats.attackConfig.speed;
        }

        private void ApplyRangedStats(Func<float, float, float> operation, CharacterStats newModifier)
        {
            RangedAttackConfig currentRangedAttacks = (RangedAttackConfig)CurrentStats.attackConfig;

            if (!(newModifier.attackConfig is RangedAttackConfig rangedAttacksModifier))
            {
                return;
            }

            currentRangedAttacks.multipleProjectilesAngle =
                operation(currentRangedAttacks.multipleProjectilesAngle, rangedAttacksModifier.multipleProjectilesAngle);
            currentRangedAttacks.spread = operation(currentRangedAttacks.spread, rangedAttacksModifier.spread);
            currentRangedAttacks.duration = operation(currentRangedAttacks.duration, rangedAttacksModifier.duration);
            currentRangedAttacks.numberOfProjectilesPerShot = Mathf.CeilToInt(operation(
                currentRangedAttacks.numberOfProjectilesPerShot,
                rangedAttacksModifier.numberOfProjectilesPerShot));
            currentRangedAttacks.projectileColor = new Color(
                operation(currentRangedAttacks.projectileColor.r, rangedAttacksModifier.projectileColor.r),
                operation(currentRangedAttacks.projectileColor.g, rangedAttacksModifier.projectileColor.g),
                operation(currentRangedAttacks.projectileColor.b, rangedAttacksModifier.projectileColor.b),
                operation(currentRangedAttacks.projectileColor.a, rangedAttacksModifier.projectileColor.a));
        }

        private void ApplyMeleeStats(Func<float, float, float> operation, CharacterStats newModifier)
        {
            MeleeAttackConfig currentMeleeAttacks = (MeleeAttackConfig)CurrentStats.attackConfig;

            if (!(newModifier.attackConfig is MeleeAttackConfig meleeAttacksModifier))
            {
                return;
            }

            currentMeleeAttacks.swingAngle =
                operation(currentMeleeAttacks.swingAngle, meleeAttacksModifier.swingAngle);
            currentMeleeAttacks.thrustDistance =
                operation(currentMeleeAttacks.thrustDistance, meleeAttacksModifier.thrustDistance);
        }

        #endregion

        #region Health System Logic (HealthSystem)

        private void InitializeHealthSystem()
        {
            if (CompareTag("Player"))
            {
                isPlayer = true;
            }
            else if (CompareTag("Enemy"))
            {
                isPlayer = false;
            }

            _mainSpriteRenderer = GetComponentInChildren<SpriteRenderer>();
            if (_mainSpriteRenderer != null)
            {
                _originalSpriteColor = _mainSpriteRenderer.color;
            }

            if (CurrentStats != null && CurrentStats.maxHealth > maxHealth)
            {
                maxHealth = CurrentStats.maxHealth;
            }

            if (maxHealth < 100f && isPlayer)
            {
                maxHealth = 100f;
            }

            currentHealth = maxHealth;

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

        public void TakeDamage(int damage)
        {
            TakeDamage((float)damage);
        }


        public bool ChangeHealth(float change)
        {
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

            // If legacy scripts send 1 damage, boost to 15 for impactful feel
            if (damageAmount <= 1.1f)
            {
                damageAmount = 15f;
            }

            if (Time.time < _timeSinceLastDamage + invincibilityDuration)
            {
                return false;
            }

            _timeSinceLastDamage = Time.time;
            _isInvincible = true;
            currentHealth -= damageAmount;

            if (currentHealth < 0)
            {
                currentHealth = 0;
            }


            if (gameObject.activeInHierarchy)
            {
                StartCoroutine(FlashRedEffect());
            }

            onDamage.Invoke();
            UpdateHealthBarUI();

            if (currentHealth <= 0)
            {
                ProcessDeath();
            }

            return true;
        }

        private IEnumerator FlashRedEffect()
        {
            if (_mainSpriteRenderer != null)
            {
                _mainSpriteRenderer.color = new Color(1f, 0.25f, 0.25f, 0.9f);
                yield return new WaitForSeconds(0.15f);
                if (_mainSpriteRenderer != null && !_isDead)
                {
                    _mainSpriteRenderer.color = _originalSpriteColor;
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

                if (Camera.main != null && Camera.main.transform.IsChildOf(transform))
                {
                    Camera.main.transform.SetParent(null);
                }

                if (healthBarCanvas != null)
                {
                    healthBarCanvas.gameObject.SetActive(false);
                }

                DisablePlayerControls();

                if (diePanel != null)
                {
                    diePanel.SetActive(true);
                }

                if (GameManager.Instance != null)
                {
                    GameManager.Instance.GameOver();
                }

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
            if (_rb != null)
            {
                _rb.linearVelocity = Vector2.zero;
                _rb.simulated = false;
            }

            foreach (Collider2D col in GetComponentsInChildren<Collider2D>())
            {
                col.enabled = false;
            }

            PlayerInput playerInput = GetComponent<PlayerInput>();
            if (playerInput != null) playerInput.enabled = false;

            enabled = false;
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

        #endregion

        #region Knockback Logic (TopDownKnockBack)

        public void ApplyKnockBack(Transform other)
        {
            if (_timeSinceLastKnockBack < timeBetweenKnockBack || _rb == null || other == null)
            {
                return;
            }

            _timeSinceLastKnockBack = 0f;
            Vector2 direction = -(other.position - transform.position).normalized;
            _rb.AddForce(direction * (knockBackPower * 100f), ForceMode2D.Impulse);
        }

        #endregion

        #region Animation Logic (SampleCharacterAnimation)

        private void UpdateWalkAnimation(Vector2 movementDirection)
        {
            if (animator != null)
            {
                animator.SetBool(IsWalkingHash, movementDirection.magnitude > .5f);
            }
        }

        private void TriggerAttackAnimation()
        {
            if (animator != null)
            {
                animator.SetTrigger(AttackHash);
            }
        }

        private void OnHurtAnimation()
        {
            if (animator != null)
            {
                animator.SetBool(IsHurtHash, true);
                animator.Play("hurt", 2, 0f); // Force Hurt layer (Layer 2) to play hurt animation immediately
            }
        }

        public void InvincibilityEndAnimation()
        {
            if (animator != null)
            {
                animator.SetBool(IsHurtHash, false);
            }
        }

        public void CreateDustParticles()
        {
            if (createDustOnWalk && dustParticleSystem != null)
            {
                dustParticleSystem.Stop();
                dustParticleSystem.Play();
            }
        }

        #endregion

        #region Visual & Sound FX Logic (TopDownFx)

        private void InitializeFxListeners()
        {
            if (effects == null || effects.Count == 0) return;

            if (_pickup != null)
            {
                TryAddFxListener(_pickup.OnPickup, TriggerEvents.Pickup);
            }

            TryAddFxListener(onDamage, TriggerEvents.Damage);
            TryAddFxListener(onDeath, TriggerEvents.Death);
            TryAddFxListener(onHeal, TriggerEvents.Heal);
            TryAddFxListener(onInvincibilityEnd, TriggerEvents.InvincibilityEnd);

            TryAddFxListener(OnAttackEvent, TriggerEvents.Attack);
            TryAddFxListener(LookEvent, TriggerEvents.Look);
            TryAddFxListener(OnMoveEvent, TriggerEvents.Walk);
        }

        private void TryAddFxListener(object evt, TriggerEvents evtTrigger)
        {
            bool hasMatchingEffect = effects != null && effects.Any(effect => effect.triggerEvent == evtTrigger);
            if (!hasMatchingEffect && (evtTrigger != TriggerEvents.Attack || shootSound == null)) return;


            switch (evtTrigger)
            {
                case TriggerEvents.Attack:
                    ((UnityEvent<AttackConfig>)evt).AddListener(arg0 => TriggerEffects(evtTrigger, arg0));
                    break;
                case TriggerEvents.Look:
                case TriggerEvents.Walk:
                    ((UnityEvent<Vector2>)evt).AddListener(_ => TriggerEffects(evtTrigger));
                    break;
                default:
                    ((UnityEvent)evt).AddListener(() => TriggerEffects(evtTrigger));
                    break;
            }
        }

        private void TriggerEffects(TriggerEvents triggerEvent, AttackConfig attackConfig = null)
        {
            bool soundPlayed = false;

            if (effects != null)
            {
                foreach (Effect effect in effects)
                {
                    if (effect.triggerEvent != triggerEvent)
                    {
                        continue;
                    }

                    if (effect.particleSystem != null)
                    {
                        effect.particleSystem.Stop();
                        effect.particleSystem.Play();
                    }

                    if (effect.soundEffect != null)
                    {
                        SoundManager.PlaySoundEffect(effect.soundEffect);
                        soundPlayed = true;
                    }
                }
            }

            if (!soundPlayed && triggerEvent == TriggerEvents.Attack && shootSound != null)
            {
                SoundManager.PlaySoundEffect(shootSound);
            }
        }
        #endregion

        #region 9. Collectibles & Triggers

        private void OnTriggerEnter2D(Collider2D other)
        {
            // Collect Coin
            if (other.CompareTag("Coin"))
            {
                Destroy(other.gameObject);
                if (GameManager.Instance != null)
                {
                    GameManager.Instance.CollectCoin();
                }
            }
            else if (other.CompareTag("GoldKey") && other.GetComponent<Item>() == null)
            {
                Destroy(other.gameObject);
                if (GameManager.Instance != null)
                {
                    GameManager.Instance.CollectGoldKey();
                }
            }
            else if (other.CompareTag("SilverKey") && other.GetComponent<Item>() == null)
            {
                Destroy(other.gameObject);
                if (GameManager.Instance != null)
                {
                    GameManager.Instance.CollectSilverKey();
                }
            }
        }

        #endregion
    }
}
