using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;

[RequireComponent(typeof(Rigidbody2D))]
public class TopDownEnemy : MonoBehaviour, IDamageable
{
    public float moveSpeed = 3f;

    [Tooltip("Detection distance to start chasing the player (in meters)")]
    public float followRange = 12f;

    public float stopDistance = 0.5f;

    public float contactDamage = 15f;

    [FormerlySerializedAs("hancurSetelahMenyerang")]
    public bool destroyOnAttack = true;

    [Tooltip("Cooldown delay between consecutive attacks if staying in contact with Player (seconds)")]
    [FormerlySerializedAs("jedaSerang")]
    public float attackCooldown = 0.5f;
    private float lastAttackTime = -999f;

    public AudioClip attackSound;

    public AudioClip hurtSound;

    public float maxHealth = 50f;

    public float currentHealth = 50f;

    [FormerlySerializedAs("warnaKenaHit")]
    public Color hitFlashColor = new Color(1f, 0.1f, 0.1f, 1f);

    [FormerlySerializedAs("poinSaatMati")]
    public int pointsOnDeath = 20;

    public float knockBackPower = 3f;

    // Internal components
    private Transform playerTarget;
    private Rigidbody2D rb;
    private Animator animator;
    private List<SpriteRenderer> bodyRenderers = new List<SpriteRenderer>();
    private List<Color> originalColorsList = new List<Color>();
    private bool isDead = false;
    private Coroutine hurtResetCoroutine;
    private Transform mainSpriteTransform;
    private Vector3 initialMainSpriteScale = Vector3.one;
    private float stunEndTime = 0f;
    private Vector2 knockbackVelocity = Vector2.zero;

    // Animator parameter hashes
    private static readonly int IsWalkingHash = Animator.StringToHash("IsWalking");
    private static readonly int AttackHash = Animator.StringToHash("Attack");
    private static readonly int IsHurtHash = Animator.StringToHash("IsHurt");
    private static readonly int HurtStateHash = Animator.StringToHash("hurt");

    private void Awake()
    {
        // 1. Disable legacy modular scripts if present to prevent conflicts
        DisableOldScripts();

        // 2. Cache Rigidbody & Animator components
        rb = GetComponent<Rigidbody2D>();
        animator = GetComponentInChildren<Animator>();

        // 3. Cache all body SpriteRenderers (excluding shadow)
        SpriteRenderer[] allRenderers = GetComponentsInChildren<SpriteRenderer>(true);
        bodyRenderers.Clear();
        originalColorsList.Clear();

        foreach (var sr in allRenderers)
        {
            if (sr != null && !sr.gameObject.name.ToLower().Contains("shadow"))
            {
                bodyRenderers.Add(sr);
                originalColorsList.Add(sr.color);
            }
        }

        // Fallback: if no non-shadow renderer found, use all available renderers
        if (bodyRenderers.Count == 0 && allRenderers.Length > 0)
        {
            foreach (var sr in allRenderers)
            {
                if (sr != null)
                {
                    bodyRenderers.Add(sr);
                    originalColorsList.Add(sr.color);
                }
            }
        }

        // 4. Cache MainSprite transform for squash & recoil hurt effect
        mainSpriteTransform = transform.Find("MainSprite");
        if (mainSpriteTransform != null)
        {
            initialMainSpriteScale = mainSpriteTransform.localScale;
        }

        // 5. Ensure full health on initialize
        currentHealth = maxHealth;

        // 6. Audio fallback
        if (attackSound == null)
        {
            attackSound = Resources.Load<AudioClip>("Sounds/Slash");
        }
        if (hurtSound == null)
        {
            hurtSound = Resources.Load<AudioClip>("Sounds/Hurt_enemy");
        }
    }

    private void Start()
    {
        FindPlayer();
        currentHealth = maxHealth;
    }

    private void Update()
    {
        if (isDead) return;

        // If currently stunned / knocked back from a hit, slide backward and pause chasing briefly
        if (Time.time < stunEndTime)
        {
            transform.position += (Vector3)knockbackVelocity * Time.deltaTime;
            knockbackVelocity = Vector2.Lerp(knockbackVelocity, Vector2.zero, Time.deltaTime * 10f);

            if (animator != null)
            {
                animator.SetBool(IsWalkingHash, false);
            }
            return;
        }

        // Search for player if reference is not yet set
        if (playerTarget == null)
        {
            FindPlayer();
            if (playerTarget == null) return;
        }

        // Calculate distance to player
        float distanceToPlayer = Vector2.Distance(transform.position, playerTarget.position);

        // Chase player if within follow range and outside stop distance
        if (distanceToPlayer <= followRange && distanceToPlayer > stopDistance)
        {
            ChasePlayer();
        }
        else
        {
            // Stop walking animation
            if (animator != null)
            {
                animator.SetBool(IsWalkingHash, false);
            }
        }
    }

    public void FindPlayer()
    {
        GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
        if (playerObj != null)
        {
            playerTarget = playerObj.transform;
        }
    }

    public void ChasePlayer()
    {
        if (playerTarget == null) return;

        // Move towards player
        transform.position = Vector2.MoveTowards(
            transform.position,
            playerTarget.position,
            moveSpeed * Time.deltaTime
        );

        // Flip sprites horizontally to face player direction
        float dirX = playerTarget.position.x - transform.position.x;
        if (Mathf.Abs(dirX) > 0.05f)
        {
            bool flip = dirX < 0;
            for (int i = 0; i < bodyRenderers.Count; i++)
            {
                if (bodyRenderers[i] != null)
                {
                    bodyRenderers[i].flipX = flip;
                }
            }
        }

        // Trigger walking animation
        if (animator != null)
        {
            animator.SetBool(IsWalkingHash, true);
        }
    }

    // =========================================================================
    // 2. CONTACT ATTACK (DAMAGE ON TOUCHING PLAYER)
    // =========================================================================

    private void OnCollisionEnter2D(Collision2D collision)
    {
        TryAttack(collision.gameObject);
    }

    private void OnCollisionStay2D(Collision2D collision)
    {
        TryAttack(collision.gameObject);
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        TryAttack(other.gameObject);
    }

    private void OnTriggerStay2D(Collider2D other)
    {
        TryAttack(other.gameObject);
    }

    public void TryAttack(GameObject target)
    {
        if (isDead || target == null) return;

        // Only attack targets tagged as "Player"
        if (!target.CompareTag("Player")) return;

        // Check attack cooldown
        if (Time.time < lastAttackTime + attackCooldown) return;
        lastAttackTime = Time.time;

        // Deal damage to Player
        TopDownCharacter2D.TopDownPlayerMaster playerMaster = target.GetComponent<TopDownCharacter2D.TopDownPlayerMaster>();
        if (playerMaster != null)
        {
            playerMaster.TakeDamage(contactDamage);
        }
        else
        {
            IDamageable damageable = target.GetComponent<IDamageable>();
            if (damageable != null)
            {
                damageable.TakeDamage(Mathf.RoundToInt(contactDamage));
            }
            else
            {
                TopDownCharacter2D.Health.HealthSystem hs = target.GetComponent<TopDownCharacter2D.Health.HealthSystem>();
                if (hs != null)
                {
                    hs.TakeDamage(contactDamage);
                }
            }
        }

        // Trigger attack animation if present
        if (animator != null)
        {
            animator.SetTrigger(AttackHash);
        }

        // Play attack sound effect
        if (attackSound != null)
        {
            TopDownCharacter2D.FX.SoundManager.PlaySoundEffect(attackSound, transform.position);
        }


        // Behaviour b: Enemy is destroyed after attacking/touching the player
        if (destroyOnAttack)
        {
            DestroyAfterAttack();
        }
    }

    public void DestroyAfterAttack()
    {
        if (isDead) return;
        isDead = true;



        // Disable all colliders to prevent duplicate trigger during destruction
        Collider2D[] allColliders = GetComponentsInChildren<Collider2D>();
        foreach (var col in allColliders)
        {
            if (col != null) col.enabled = false;
        }

        // Disable physics simulation
        if (rb != null)
        {
            rb.linearVelocity = Vector2.zero;
            rb.simulated = false;
        }

        // Destroy enemy GameObject
        Destroy(gameObject);
    }

    // =========================================================================
    // 3. HEALTH & HURT ANIMATION SYSTEM
    // =========================================================================

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
        return false;
    }

    public bool TakeDamage(float damageAmount)
    {
        if (isDead) return false;

        currentHealth -= damageAmount;

        // Play hurt sound effect
        if (hurtSound != null)
        {
            TopDownCharacter2D.FX.SoundManager.PlaySoundEffect(hurtSound, transform.position);
        }

        // 1. Apply knockback impulse away from player if not already assigned by projectile
        if (knockbackVelocity == Vector2.zero && playerTarget != null)
        {
            Vector2 pushDir = ((Vector2)transform.position - (Vector2)playerTarget.position).normalized;
            if (pushDir == Vector2.zero) pushDir = Vector2.up;
            knockbackVelocity = pushDir * (knockBackPower * 2.5f);
        }
        stunEndTime = Time.time + 0.2f;

        // 2. Trigger Hurt animation on Animator
        if (animator != null)
        {
            int hurtLayer = animator.GetLayerIndex("Hurt");
            if (hurtLayer != -1)
            {
                animator.SetLayerWeight(hurtLayer, 1f);
                animator.Play(HurtStateHash, hurtLayer, 0f);
            }
            else
            {
                animator.Play(HurtStateHash, 0, 0f);
            }

            animator.SetBool(IsHurtHash, true);
        }

        // 3. Trigger visual hurt flash & squash recoil
        if (gameObject.activeInHierarchy)
        {
            if (hurtResetCoroutine != null)
            {
                StopCoroutine(hurtResetCoroutine);
            }
            hurtResetCoroutine = StartCoroutine(ProcessHurtAnimation());
        }

        // 4. Enemy dies when HP reaches 0
        if (currentHealth <= 0)
        {
            currentHealth = 0;
            Die();
        }

        return true;
    }

    private IEnumerator ProcessHurtAnimation()
    {
        // A. Flash red on all body renderers
        for (int i = 0; i < bodyRenderers.Count; i++)
        {
            if (bodyRenderers[i] != null) bodyRenderers[i].color = hitFlashColor;
        }

        // B. Squash & recoil effect on MainSprite
        if (mainSpriteTransform != null)
        {
            mainSpriteTransform.localScale = new Vector3(
                initialMainSpriteScale.x * 0.8f,
                initialMainSpriteScale.y * 1.25f,
                initialMainSpriteScale.z
            );
        }

        yield return new WaitForSeconds(0.12f);

        // C. Smoothly restore original scale and color
        float returnDuration = 0.1f;
        float elapsed = 0f;
        Vector3 currentScale = mainSpriteTransform != null ? mainSpriteTransform.localScale : initialMainSpriteScale;

        while (elapsed < returnDuration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / returnDuration;

            if (mainSpriteTransform != null)
            {
                mainSpriteTransform.localScale = Vector3.Lerp(currentScale, initialMainSpriteScale, t);
            }

            for (int i = 0; i < bodyRenderers.Count; i++)
            {
                if (bodyRenderers[i] != null && i < originalColorsList.Count)
                {
                    bodyRenderers[i].color = Color.Lerp(hitFlashColor, originalColorsList[i], t);
                }
            }

            yield return null;
        }

        // Ensure scale and colors are exactly back to original
        if (mainSpriteTransform != null)
        {
            mainSpriteTransform.localScale = initialMainSpriteScale;
        }

        for (int i = 0; i < bodyRenderers.Count; i++)
        {
            if (bodyRenderers[i] != null && i < originalColorsList.Count)
            {
                bodyRenderers[i].color = originalColorsList[i];
            }
        }

        if (animator != null && !isDead)
        {
            animator.SetBool(IsHurtHash, false);
        }

        hurtResetCoroutine = null;
    }

    public void ApplyKnockBack(Transform attacker)
    {
        if (isDead || attacker == null) return;

        Vector2 pushDir = ((Vector2)transform.position - (Vector2)attacker.position).normalized;
        if (pushDir == Vector2.zero) pushDir = Vector2.up;

        knockbackVelocity = pushDir * (knockBackPower * 2.5f);
        stunEndTime = Time.time + 0.2f;

        if (rb != null)
        {
            rb.AddForce(pushDir * (knockBackPower * 80f), ForceMode2D.Impulse);
        }
    }

    // =========================================================================
    // 4. ENEMY DEATH
    // =========================================================================

    public void Die()
    {
        if (isDead) return;
        isDead = true;


        // Disable all colliders so it cannot trigger further collisions
        Collider2D[] allColliders = GetComponentsInChildren<Collider2D>();
        foreach (var col in allColliders)
        {
            if (col != null) col.enabled = false;
        }

        // Award score points via GameManager
        if (GameManager.Instance != null)
        {
            GameManager.Instance.AddPoints(pointsOnDeath);
        }

        // Destroy enemy object
        Destroy(gameObject);
    }

    public void DisableOldScripts()
    {
        string[] oldComponentNames =
        {
            "CharacterStatsHandler",
            "TopDownContactEnemyController",
            "TopDownMovement",
            "HealthSystem",
            "DisappearOnDeath",
            "ChangeHealthOnTouch",
            "TopDownKnockBack",
            "TopDownFx",
            "SampleCharacterAnimation",
            "EnemyChildren",
            "Enemy",
            "BucketHeadZombie",
            "FlagZombie",
            "ConeZombie"
        };

        foreach (MonoBehaviour mb in GetComponents<MonoBehaviour>())
        {
            if (mb != null && mb != this)
            {
                string typeName = mb.GetType().Name;
                foreach (string oldName in oldComponentNames)
                {
                    if (typeName == oldName)
                    {
                        mb.enabled = false;
                        break;
                    }
                }
            }
        }
    }
}
