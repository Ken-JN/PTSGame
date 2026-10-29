using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;

/// <summary>
///     ALL-IN-ONE PLAYER CONTROLLER
///     Consolidates all modular top-down systems into ONE single clean script:
///     1. TopDown Movement (4 directions, WASD / Arrow Keys / Input System)
///     2. TopDown Aim & Flashlight (smooth rotation towards mouse cursor)
///     3. TopDown Shooting (bullet projectile spawning, fire rate, and recoil)
///     4. TopDown FX (dust particles while walking, hit/shoot sounds, and red flash)
///     5. Health System & Healthbar UI (100 HP, smooth gradient, NO freeze on hit, freeze ONLY on death)
///     6. TopDown Knockback (pushback impulse when taking damage)
///     7. Sample Character Animation (IsWalking, Attack, Hurt triggers and sprite flip)
///     8. Collectibles (coins and inventory items)
/// </summary>
public class Player : MonoBehaviour, IDamageable
{
    [Header("=== 1. MOVEMENT ===")]
    [Tooltip("Movement speed in units per second")]
    public float moveSpeed = 5f;
    private Vector2 moveInput;
    private Rigidbody2D rb;

    [Header("=== 2. AIMING & FLASHLIGHT ===")]
    [Tooltip("Transform of the 2D Light or Flashlight attached to player")]
    public Transform playerLight;
    [Tooltip("Transform of the weapon/arm pivot to aim with mouse")]
    public Transform weaponPivot;
    [Tooltip("Rotation smoothness speed")]
    public float rotationSpeed = 15f;
    [Tooltip("Offset angle in degrees (default -90 for sprites pointing up)")]
    public float lightRotationOffset = -90f;
    private Vector2 aimDirection = Vector2.right;
    private Camera mainCamera;

    [Header("=== 3. SHOOTING & ATTACK ===")]
    [Tooltip("Point where bullets are spawned")]
    public Transform projectileSpawnPoint;
    [Tooltip("Prefab of the bullet to shoot")]
    public GameObject bulletPrefab;
    [Tooltip("Bullet flight speed")]
    public float bulletSpeed = 12f;
    [Tooltip("Shooting delay between shots in seconds")]
    public float fireRate = 0.25f;
    [Tooltip("Recoil pushback strength when firing")]
    public float recoilStrength = 1.5f;
    private float nextFireTime = 0f;

    [Tooltip("Melee attack damage")]
    public float meleeDamage = 30f;
    [Tooltip("Melee attack range circle")]
    public float meleeRange = 1.5f;

    [Header("=== 4. HEALTH & HEALTHBAR ===")]
    [Tooltip("Maximum health points")]
    public float maxHealth = 100f;
    [Tooltip("Current health points (starts at 100)")]
    public float currentHealth = 100f;
    [Tooltip("Invincibility duration in seconds after taking damage")]
    public float invincibilityDuration = 0.8f;
    private float lastDamageTime = -999f;
    private bool isDead = false;

    [Tooltip("UI Slider to display health")]
    public Slider healthSlider;
    [Tooltip("UI Image Fill for color transition (Green -> Yellow -> Red)")]
    public Image healthBarFill;
    [Tooltip("Canvas holding the slider (World Space above player or Screen Space)")]
    public Canvas healthBarCanvas;
    [Tooltip("Offset position above player head")]
    public Vector3 healthBarOffset = new Vector3(0f, 1.2f, 0f);

    [Header("=== 5. KNOCKBACK ===")]
    [Tooltip("Force pushed backwards when taking damage from an enemy")]
    public float damageKnockbackPower = 5f;

    [Header("=== 6. ANIMATION & FX ===")]
    [Tooltip("Particle system for dust when walking")]
    public ParticleSystem dustParticleSystem;
    [Tooltip("Sound played when shooting")]
    public AudioClip shootSound;
    [Tooltip("Sound played when taking damage")]
    public AudioClip hurtSound;
    [Tooltip("Sound played upon death")]
    public AudioClip deathSound;

    private SpriteRenderer[] allRenderers;
    private Animator animator;
    private AudioSource audioSource;
    private static readonly int IsWalkingHash = Animator.StringToHash("IsWalking");
    private static readonly int AttackHash = Animator.StringToHash("Attack");
    private static readonly int IsHurtHash = Animator.StringToHash("IsHurt");

    [Header("=== 7. SCORE ===")]
    public int score = 0;

    private void Awake()
    {
        // 1. Ensure time scale is running normally
        Time.timeScale = 1f;

        // 2. Health is full immediately in Awake (never starts at 0!)
        currentHealth = maxHealth;

        // 3. Cache components
        rb = GetComponent<Rigidbody2D>();
        animator = GetComponentInChildren<Animator>();
        allRenderers = GetComponentsInChildren<SpriteRenderer>();
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null) audioSource = gameObject.AddComponent<AudioSource>();

        mainCamera = Camera.main;

        // 4. Auto-detect child Light 2D if not assigned
        if (playerLight == null)
        {
            var light2D = GetComponentInChildren<UnityEngine.Rendering.Universal.Light2D>();
            if (light2D != null) playerLight = light2D.transform;
        }

        // 5. Auto-detect projectile spawn point
        if (projectileSpawnPoint == null)
        {
            Transform foundSpawn = transform.Find("WeaponPivot/BulletSpawnPoint");
            if (foundSpawn != null) projectileSpawnPoint = foundSpawn;
            else projectileSpawnPoint = transform;
        }

        // 6. Auto-detect UI components
        if (healthSlider != null)
        {
            if (healthBarCanvas == null) healthBarCanvas = healthSlider.GetComponentInParent<Canvas>();
            if (healthBarFill == null && healthSlider.fillRect != null) healthBarFill = healthSlider.fillRect.GetComponent<Image>();
        }
    }

    private void Start()
    {
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
        if (isDead) return;

        // 1. Movement input
        ReadMovementInput();

        // 2. Aiming & mouse tracking
        CalculateAimDirection();

        // 3. Visuals & animations
        UpdateVisuals();

        // 4. Shooting input (Left Mouse Click)
        if (Mouse.current != null && Mouse.current.leftButton.isPressed)
        {
            TryShoot();
        }

        // 5. Melee attack input (Spacebar)
        if (Keyboard.current != null && Keyboard.current.spaceKey.wasPressedThisFrame)
        {
            PerformMeleeAttack();
        }
    }

    private void FixedUpdate()
    {
        if (isDead) return;

        // Apply movement physics
        if (rb != null)
        {
            rb.linearVelocity = moveInput * moveSpeed;
        }
        else
        {
            transform.position += (Vector3)(moveInput * moveSpeed * Time.fixedDeltaTime);
        }
    }

    private void LateUpdate()
    {
        // Keep world space healthbar upright and positioned above player head
        if (healthBarCanvas != null && healthBarCanvas.renderMode == RenderMode.WorldSpace)
        {
            healthBarCanvas.transform.position = transform.position + healthBarOffset;
            healthBarCanvas.transform.rotation = Quaternion.identity;
        }
    }

    // ========================================================
    // 1. MOVEMENT INPUT (4 DIRECTIONS)
    // ========================================================
    private void ReadMovementInput()
    {
        float inputX = 0f;
        float inputY = 0f;

        if (Keyboard.current != null)
        {
            if (Keyboard.current.wKey.isPressed || Keyboard.current.upArrowKey.isPressed) inputY = 1f;
            else if (Keyboard.current.sKey.isPressed || Keyboard.current.downArrowKey.isPressed) inputY = -1f;

            if (Keyboard.current.dKey.isPressed || Keyboard.current.rightArrowKey.isPressed) inputX = 1f;
            else if (Keyboard.current.aKey.isPressed || Keyboard.current.leftArrowKey.isPressed) inputX = -1f;

            moveInput = new Vector2(inputX, inputY).normalized;
        }
    }

    public void OnMove(InputValue value)
    {
        moveInput = value.Get<Vector2>();
    }

    public void OnFire()
    {
        TryShoot();
    }

    // ========================================================
    // 2. AIMING & FLASHLIGHT ROTATION
    // ========================================================
    private void CalculateAimDirection()
    {
        if (mainCamera == null) mainCamera = Camera.main;
        if (mainCamera == null) return;

        Vector2 mouseScreen;
        if (Mouse.current != null)
        {
            mouseScreen = Mouse.current.position.ReadValue();
        }
        else
        {
            mouseScreen = Input.mousePosition;
        }

        Vector3 mouseWorld = mainCamera.ScreenToWorldPoint(new Vector3(mouseScreen.x, mouseScreen.y, -mainCamera.transform.position.z));
        mouseWorld.z = 0f;

        Vector2 playerPos = (Vector2)transform.position;
        aimDirection = (Vector2)(mouseWorld - transform.position).normalized;

        float angle = Mathf.Atan2(aimDirection.y, aimDirection.x) * Mathf.Rad2Deg;

        // Rotate flashlight
        if (playerLight != null)
        {
            Quaternion targetRot = Quaternion.Euler(0f, 0f, angle + lightRotationOffset);
            playerLight.rotation = Quaternion.Lerp(playerLight.rotation, targetRot, Time.deltaTime * rotationSpeed);
        }

        // Rotate weapon pivot
        if (weaponPivot != null)
        {
            weaponPivot.rotation = Quaternion.Euler(0f, 0f, angle);
        }
    }

    // ========================================================
    // 3. ANIMATION & FX
    // ========================================================
    private void UpdateVisuals()
    {
        bool isMoving = moveInput.sqrMagnitude > 0.01f;

        // Flip sprites horizontally to face aim/movement direction
        if (allRenderers != null && allRenderers.Length > 0)
        {
            bool faceLeft = aimDirection.x < 0;
            foreach (var sr in allRenderers)
            {
                sr.flipX = faceLeft;
            }
        }

        // Animator parameters
        if (animator != null)
        {
            animator.SetBool(IsWalkingHash, isMoving);
            animator.SetBool("isMoving", isMoving);
            animator.SetFloat("Speed", moveInput.sqrMagnitude);
        }

        // Dust particle system
        if (dustParticleSystem != null)
        {
            if (isMoving && !dustParticleSystem.isPlaying) dustParticleSystem.Play();
            else if (!isMoving && dustParticleSystem.isPlaying) dustParticleSystem.Stop();
        }
    }

    // ========================================================
    // 4. SHOOTING & ATTACK
    // ========================================================
    public void TryShoot()
    {
        if (isDead || Time.time < nextFireTime) return;

        nextFireTime = Time.time + fireRate;

        // Trigger attack animation
        if (animator != null)
        {
            animator.SetTrigger(AttackHash);
        }

        // Play shoot audio
        if (shootSound != null && audioSource != null)
        {
            audioSource.PlayOneShot(shootSound);
        }

        // 1. Shoot via bulletPrefab if assigned
        Vector3 spawnPos = projectileSpawnPoint != null ? projectileSpawnPoint.position : transform.position;

        if (bulletPrefab != null)
        {
            GameObject bullet = Instantiate(bulletPrefab, spawnPos, Quaternion.identity);
            Rigidbody2D bulletRb = bullet.GetComponent<Rigidbody2D>();
            if (bulletRb != null)
            {
                bulletRb.linearVelocity = aimDirection * bulletSpeed;
            }
            Destroy(bullet, 3f);
        }
        else
        {
            // 2. Or fallback to raycast shot
            RaycastHit2D hit = Physics2D.Raycast(spawnPos, aimDirection, 15f);
            if (hit.collider != null && hit.collider.CompareTag("Enemy"))
            {
                IDamageable enemy = hit.collider.GetComponentInParent<IDamageable>();
                if (enemy != null) enemy.TakeDamage((int)meleeDamage);
            }
        }

        // Apply recoil impulse to player
        if (rb != null && recoilStrength > 0f)
        {
            rb.AddForce(-aimDirection * (recoilStrength * 50f), ForceMode2D.Impulse);
        }
    }

    public void PerformMeleeAttack()
    {
        if (isDead) return;

        if (animator != null) animator.SetTrigger(AttackHash);

        Collider2D[] hits = Physics2D.OverlapCircleAll(transform.position, meleeRange);
        foreach (Collider2D hit in hits)
        {
            if (hit.CompareTag("Enemy"))
            {
                IDamageable enemy = hit.GetComponentInParent<IDamageable>();
                if (enemy != null) enemy.TakeDamage((int)meleeDamage);
            }
        }
    }

    // ========================================================
    // 5. HEALTH, DAMAGE, KNOCKBACK & DEATH
    // ========================================================
    public void TakeDamage(int damage)
    {
        TakeDamage((float)damage);
    }

    public void TakeDamage(float damage)
    {
        if (isDead) return;

        // Check invincibility timer
        if (Time.time < lastDamageTime + invincibilityDuration)
        {
            return; // Invincible, ignore hit
        }

        lastDamageTime = Time.time;
        currentHealth -= damage;
        if (currentHealth < 0) currentHealth = 0;

        Debug.Log($"<color=orange>[HIT]</color> Player took {damage} damage! Remaining HP: {currentHealth}/{maxHealth}");

        // Play hurt sound
        if (hurtSound != null && audioSource != null)
        {
            audioSource.PlayOneShot(hurtSound);
        }

        // Flash red visual effect
        if (gameObject.activeInHierarchy)
        {
            StartCoroutine(FlashRedEffect());
        }

        // Animator hurt state
        if (animator != null)
        {
            animator.SetTrigger("Hurt");
            animator.SetBool(IsHurtHash, true);
        }

        // Update healthbar UI
        UpdateHealthBarUI();

        // IMPORTANT: Game DOES NOT freeze when taking damage!
        // Game ONLY freezes when player health reaches 0 (Death)!
        if (currentHealth <= 0)
        {
            Die();
        }
    }

    public void ApplyKnockBack(Transform attacker)
    {
        if (rb == null || attacker == null) return;
        Vector2 pushDir = ((Vector2)transform.position - (Vector2)attacker.position).normalized;
        rb.AddForce(pushDir * (damageKnockbackPower * 50f), ForceMode2D.Impulse);
    }

    private IEnumerator FlashRedEffect()
    {
        if (allRenderers != null)
        {
            foreach (var sr in allRenderers) sr.color = new Color(1f, 0.25f, 0.25f, 0.9f);
            yield return new WaitForSeconds(0.15f);
            if (!isDead)
            {
                foreach (var sr in allRenderers) sr.color = Color.white;
            }
            if (animator != null)
            {
                animator.SetBool(IsHurtHash, false);
            }
        }
    }

    public void UpdateHealthBarUI()
    {
        if (healthSlider == null) return;

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

    private void Die()
    {
        if (isDead) return;
        isDead = true;

        Debug.Log("<color=red><b>[GAME OVER] PLAYER HAS DIED! GAME FREEZES!</b></color>");

        if (deathSound != null && audioSource != null)
        {
            audioSource.PlayOneShot(deathSound);
        }

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

        // Stop physics
        if (rb != null)
        {
            rb.linearVelocity = Vector2.zero;
            rb.simulated = false;
        }

        // Disable colliders
        foreach (Collider2D col in GetComponentsInChildren<Collider2D>())
        {
            col.enabled = false;
        }

        // FREEZE GAME
        Time.timeScale = 0f;
    }

    // ========================================================
    // 6. COLLECTIBLES & COINS
    // ========================================================
    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Coin"))
        {
            Destroy(other.gameObject);
            score++;
            Debug.Log($"<color=yellow>[COIN]</color> Collected! Current Score: {score}");

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
        else if (other.CompareTag("FakeKey") && other.GetComponent<Item>() == null)
        {
            Destroy(other.gameObject);
            if (GameManager.Instance != null)
            {
                GameManager.Instance.CollectFakeKey();
            }
        }
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, meleeRange);
    }
}
