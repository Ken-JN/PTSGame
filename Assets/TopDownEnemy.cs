using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public class TopDownEnemy : MonoBehaviour, IDamageable
{
    [Header("Movement")]
    [SerializeField] private float moveSpeed = 3f;
    [SerializeField] private float detectionRange = 12f;
    [SerializeField] private float stopDistance = 0.5f;

    [Header("Combat")]
    [SerializeField] private float contactDamage = 15f;
    [SerializeField] private float attackCooldown = 0.5f;
    [SerializeField] private bool destroyOnAttack = true;
    [SerializeField] private float knockbackForce = 3f;
    [SerializeField] private AudioClip attackSound;
    [SerializeField] private AudioClip hurtSound;

    [Header("Health")]
    [SerializeField] private float maxHealth = 50f;
    [SerializeField] private int scoreReward = 20;
    [SerializeField] private Color hitFlashColor = new Color(1f, 0.2f, 0.2f, 1f);

    // Internal components and state
    private Rigidbody2D rb;
    private Animator animator;
    private Transform playerTarget;

    private float currentHealth;
    private float lastAttackTime = -999f;
    private bool isDead;

    private readonly List<SpriteRenderer> bodyRenderers = new List<SpriteRenderer>();
    private readonly List<Color> originalColors = new List<Color>();
    private Coroutine flashCoroutine;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        animator = GetComponentInChildren<Animator>();

        currentHealth = maxHealth;
        CacheRenderersAndColors();

        if (attackSound == null) attackSound = Resources.Load<AudioClip>("Sounds/Slash");
        if (hurtSound == null) hurtSound = Resources.Load<AudioClip>("Sounds/Hurt_enemy");
    }

    private void Start()
    {
        FindPlayer();
    }

    private void Update()
    {
        if (isDead) return;

        if (playerTarget == null)
        {
            FindPlayer();
            return;
        }

        HandleFacingDirection();
    }

    private void FixedUpdate()
    {
        if (isDead || playerTarget == null) return;

        ChasePlayer();
    }

    // ================= MOVEMENT =================

    private void ChasePlayer()
    {
        float distance = Vector2.Distance(transform.position, playerTarget.position);

        if (distance <= detectionRange && distance > stopDistance)
        {
            Vector2 direction = ((Vector2)playerTarget.position - rb.position).normalized;
            rb.linearVelocity = direction * moveSpeed;

            if (animator != null) animator.SetBool("IsWalking", true);
        }
        else
        {
            rb.linearVelocity = Vector2.zero;
            if (animator != null) animator.SetBool("IsWalking", false);
        }
    }

    private void HandleFacingDirection()
    {
        bool flipLeft = playerTarget.position.x < transform.position.x;

        for (int i = 0; i < bodyRenderers.Count; i++)
        {
            if (bodyRenderers[i] != null)
            {
                bodyRenderers[i].flipX = flipLeft;
            }
        }
    }

    // ================= COMBAT & ATTACK =================

    private void OnCollisionEnter2D(Collision2D collision) => HandleContactAttack(collision.gameObject);
    private void OnCollisionStay2D(Collision2D collision) => HandleContactAttack(collision.gameObject);
    private void OnTriggerEnter2D(Collider2D other) => HandleContactAttack(other.gameObject);
    private void OnTriggerStay2D(Collider2D other) => HandleContactAttack(other.gameObject);

    private void HandleContactAttack(GameObject target)
    {
        if (isDead || target == null) return;
        if (Time.time < lastAttackTime + attackCooldown) return;

        // Try damaging player or another IDamageable target
        var player = target.GetComponentInParent<TopDownCharacter2D.TopDownPlayerMaster>();
        var damageable = target.GetComponentInParent<IDamageable>();

        if (player == null && damageable == null && !target.CompareTag("Player")) return;

        lastAttackTime = Time.time;

        if (player != null)
        {
            player.TakeDamage(contactDamage);
        }
        else if (damageable != null)
        {
            damageable.TakeDamage((int)contactDamage);
        }

        if (animator != null) animator.SetTrigger("Attack");
        if (attackSound != null) AudioSource.PlayClipAtPoint(attackSound, transform.position);

        if (destroyOnAttack)
        {
            Die();
        }
    }

    // ================= HEALTH & DAMAGE =================

    public void TakeDamage(int damage) => TakeDamage((float)damage);

    public bool ChangeHealth(float change)
    {
        if (change < 0) return TakeDamage(Mathf.Abs(change));
        return false;
    }

    public bool TakeDamage(float amount)
    {
        if (isDead) return false;

        currentHealth -= amount;

        if (hurtSound != null) AudioSource.PlayClipAtPoint(hurtSound, transform.position);

        if (animator != null)
        {
            animator.SetBool("IsHurt", true);
            animator.Play("hurt", -1, 0f);
        }

        if (gameObject.activeInHierarchy)
        {
            if (flashCoroutine != null) StopCoroutine(flashCoroutine);
            flashCoroutine = StartCoroutine(FlashFeedback());
        }

        // Apply knockback away from the player
        if (playerTarget != null)
        {
            Vector2 pushDir = ((Vector2)transform.position - (Vector2)playerTarget.position).normalized;
            rb.AddForce(pushDir * knockbackForce, ForceMode2D.Impulse);
        }

        if (currentHealth <= 0)
        {
            currentHealth = 0;
            Die();
        }

        return true;
    }

    public void ApplyKnockBack(Transform attacker)
    {
        if (attacker != null && rb != null)
        {
            Vector2 pushDir = ((Vector2)transform.position - (Vector2)attacker.position).normalized;
            rb.AddForce(pushDir * knockbackForce, ForceMode2D.Impulse);
        }
    }

    private IEnumerator FlashFeedback()
    {
        for (int i = 0; i < bodyRenderers.Count; i++)
        {
            if (bodyRenderers[i] != null) bodyRenderers[i].color = hitFlashColor;
        }

        yield return new WaitForSeconds(0.2f);

        for (int i = 0; i < bodyRenderers.Count; i++)
        {
            if (bodyRenderers[i] != null)
            {
                bodyRenderers[i].color = (i < originalColors.Count) ? originalColors[i] : Color.white;
            }
        }

        if (animator != null) animator.SetBool("IsHurt", false);
        flashCoroutine = null;
    }

    public void Die()
    {
        if (isDead) return;
        isDead = true;

        if (GameManager.Instance != null)
        {
            GameManager.Instance.AddPoints(scoreReward);
        }

        Destroy(gameObject);
    }

    // ================= HELPERS =================

    public void FindPlayer()
    {
        GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
        if (playerObj != null)
        {
            playerTarget = playerObj.transform;
        }
    }

    private void CacheRenderersAndColors()
    {
        bodyRenderers.Clear();
        originalColors.Clear();

        SpriteRenderer[] renderers = GetComponentsInChildren<SpriteRenderer>(true);
        foreach (var sr in renderers)
        {
            if (sr != null && !sr.gameObject.name.ToLower().Contains("shadow"))
            {
                bodyRenderers.Add(sr);
                originalColors.Add(sr.color);
            }
        }
    }
}