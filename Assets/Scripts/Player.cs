using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;

public class Player : MonoBehaviour, IDamageable
{
    public float moveSpeed = 5f;
    private Vector2 moveInput;
    private Rigidbody2D rb;

    public Transform playerLight;
    public Transform weaponPivot;
    public float rotationSpeed = 15f;
    public float lightRotationOffset = -90f;
    private Vector2 aimDirection = Vector2.right;
    private Camera mainCamera;

    public Transform projectileSpawnPoint;
    public GameObject bulletPrefab;
    public float bulletSpeed = 12f;
    public float fireRate = 0.25f;
    public float recoilStrength = 1.5f;
    private float nextFireTime = 0f;

    public float meleeDamage = 30f;
    public float meleeRange = 1.5f;

    public float maxHealth = 100f;
    public float currentHealth = 100f;
    public float invincibilityDuration = 0.8f;
    private float lastDamageTime = -999f;
    private bool isDead = false;

    public Slider healthSlider;
    public Image healthBarFill;
    public Canvas healthBarCanvas;
    public Vector3 healthBarOffset = new Vector3(0f, 1.2f, 0f);
    public float damageKnockbackPower = 5f;

    public ParticleSystem dustParticleSystem;
    public AudioClip shootSound;
    public AudioClip hurtSound;
    public AudioClip deathSound;

    public int score = 0;

    private SpriteRenderer[] allRenderers;
    private Animator animator;

    private void Awake()
    {
        Time.timeScale = 1f;
        currentHealth = maxHealth;

        rb = GetComponent<Rigidbody2D>();
        animator = GetComponentInChildren<Animator>();
        allRenderers = GetComponentsInChildren<SpriteRenderer>();
        mainCamera = Camera.main;

        if (playerLight == null)
        {
            var light2D = GetComponentInChildren<UnityEngine.Rendering.Universal.Light2D>();
            if (light2D != null) playerLight = light2D.transform;
        }

        if (projectileSpawnPoint == null)
        {
            projectileSpawnPoint = transform;
        }

        if (shootSound == null) shootSound = Resources.Load<AudioClip>("Sounds/Shoot");
        if (hurtSound == null) hurtSound = Resources.Load<AudioClip>("Sounds/Hurt");
    }

    private void Start()
    {
        UpdateHealthBarUI();
    }

    private void Update()
    {
        if (isDead) return;

        HandleInput();
        HandleAiming();

        bool isShooting = Input.GetMouseButton(0);
        if (Mouse.current != null && Mouse.current.leftButton.isPressed) isShooting = true;

        if (isShooting && Time.time >= nextFireTime)
        {
            nextFireTime = Time.time + fireRate;
            Shoot();
        }

        bool isMelee = Input.GetKeyDown(KeyCode.Space);
        if (Keyboard.current != null && Keyboard.current.spaceKey.wasPressedThisFrame) isMelee = true;

        if (isMelee)
        {
            PerformMelee();
        }
    }

    private void FixedUpdate()
    {
        if (isDead) return;
        if (rb != null) rb.linearVelocity = moveInput * moveSpeed;
    }

    private void LateUpdate()
    {
        if (healthBarCanvas != null && healthBarCanvas.renderMode == RenderMode.WorldSpace)
        {
            healthBarCanvas.transform.position = transform.position + healthBarOffset;
            healthBarCanvas.transform.rotation = Quaternion.identity;
        }
    }

    private void HandleInput()
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

    private void HandleAiming()
    {
        if (mainCamera == null) mainCamera = Camera.main;
        if (mainCamera == null) return;

        Vector3 mouseScreen = Input.mousePosition;
        if (Mouse.current != null) mouseScreen = Mouse.current.position.ReadValue();

        Vector3 mouseWorld = mainCamera.ScreenToWorldPoint(mouseScreen);
        aimDirection = ((Vector2)(mouseWorld - transform.position)).normalized;

        float angle = Mathf.Atan2(aimDirection.y, aimDirection.x) * Mathf.Rad2Deg;

        bool flip = aimDirection.x < 0;
        foreach (var sr in allRenderers)
        {
            if (sr != null) sr.flipX = flip;
        }

        if (playerLight != null)
        {
            playerLight.rotation = Quaternion.Euler(0, 0, angle + lightRotationOffset);
        }

        if (weaponPivot != null)
        {
            weaponPivot.rotation = Quaternion.Euler(0, 0, angle);
        }
    }

    private void Shoot()
    {
        if (bulletPrefab != null && projectileSpawnPoint != null)
        {
            float angle = Mathf.Atan2(aimDirection.y, aimDirection.x) * Mathf.Rad2Deg;
            GameObject bullet = Instantiate(bulletPrefab, projectileSpawnPoint.position, Quaternion.Euler(0, 0, angle));
            Rigidbody2D bulletRb = bullet.GetComponent<Rigidbody2D>();
            if (bulletRb != null)
            {
                bulletRb.linearVelocity = aimDirection * bulletSpeed;
            }
        }

        if (shootSound != null) AudioSource.PlayClipAtPoint(shootSound, transform.position);

        if (rb != null)
        {
            rb.AddForce(-aimDirection * recoilStrength, ForceMode2D.Impulse);
        }
    }

    private void PerformMelee()
    {
        if (animator != null) animator.SetTrigger("Attack");

        Collider2D[] hits = Physics2D.OverlapCircleAll(transform.position, meleeRange);
        foreach (var hit in hits)
        {
            if (hit.CompareTag("Enemy"))
            {
                IDamageable damageable = hit.GetComponent<IDamageable>();
                if (damageable != null)
                {
                    damageable.TakeDamage((int)meleeDamage);
                }
            }
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

        if (hurtSound != null) AudioSource.PlayClipAtPoint(hurtSound, transform.position);

        StartCoroutine(FlashRed());

        if (currentHealth <= 0)
        {
            currentHealth = 0;
            Die();
        }

        return true;
    }

    private IEnumerator FlashRed()
    {
        foreach (var sr in allRenderers) if (sr != null) sr.color = new Color(1f, 0.2f, 0.2f, 1f);
        yield return new WaitForSecondsRealtime(0.15f);
        foreach (var sr in allRenderers) if (sr != null) sr.color = Color.white;
    }

    private void UpdateHealthBarUI()
    {
        if (healthSlider != null)
        {
            healthSlider.value = Mathf.Clamp01(currentHealth / maxHealth);
        }
    }

    private void Die()
    {
        if (isDead) return;
        isDead = true;

        Time.timeScale = 0f;

        if (deathSound != null) AudioSource.PlayClipAtPoint(deathSound, transform.position);
        if (rb != null) rb.linearVelocity = Vector2.zero;

        if (GameManager.Instance != null && GameManager.Instance.diePanel != null)
        {
            GameManager.Instance.diePanel.SetActive(true);
        }
    }

    public void CreateDustParticles()
    {
        if (dustParticleSystem != null)
        {
            dustParticleSystem.Stop();
            dustParticleSystem.Play();
        }
    }

    public void InvincibilityEnd()
    {
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Coin"))
        {
            Destroy(other.gameObject);
            score++;
            if (GameManager.Instance != null) GameManager.Instance.CollectCoin();
        }
        else if (other.CompareTag("GoldKey") && other.GetComponent<Item>() == null)
        {
            Destroy(other.gameObject);
            if (GameManager.Instance != null) GameManager.Instance.CollectGoldKey();
        }
        else if (other.CompareTag("SilverKey") && other.GetComponent<Item>() == null)
        {
            Destroy(other.gameObject);
            if (GameManager.Instance != null) GameManager.Instance.CollectSilverKey();
        }
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, meleeRange);
    }
}
