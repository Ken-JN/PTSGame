using UnityEngine;

public class EnemyMovement : MonoBehaviour, IDamageable
{
    public float moveSpeed = 2.5f;

    public float stoppingDistance = 0.5f;

    public float attackDamage = 15f;

    public float health = 50f;

    public AudioClip attackSound;

    public AudioClip hurtSound;

    private Transform playerTarget;
    private SpriteRenderer spriteRenderer;
    private Rigidbody2D rb;
    private bool hasAttacked = false;

    private void Awake()
    {
        spriteRenderer = GetComponentInChildren<SpriteRenderer>();
        rb = GetComponent<Rigidbody2D>();
        if (attackSound == null) attackSound = Resources.Load<AudioClip>("Sounds/Slash");
        if (hurtSound == null) hurtSound = Resources.Load<AudioClip>("Sounds/Hurt_enemy");
    }

    private void Start()
    {
        // 1. Find the Player in the scene using the "Player" Tag
        GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
        if (playerObj != null)
        {
            playerTarget = playerObj.transform;
        }
    }

    private void Update()
    {
        // If already attacked or player does not exist, stop moving
        if (hasAttacked || playerTarget == null) return;

        // Move towards player
        MoveTowardsPlayer();

        // Flip sprite to face player
        FlipSprite();
    }

    // ========================================================
    // 1. CHASE PLAYER
    // ========================================================
    private void MoveTowardsPlayer()
    {
        float distance = Vector2.Distance(transform.position, playerTarget.position);

        if (distance > stoppingDistance)
        {
            // Move smoothly towards player position
            transform.position = Vector2.MoveTowards(
                transform.position,
                playerTarget.position,
                moveSpeed * Time.deltaTime
            );
        }
    }

    private void FlipSprite()
    {
        if (spriteRenderer != null && playerTarget != null)
        {
            if (playerTarget.position.x < transform.position.x)
            {
                spriteRenderer.flipX = true;  // Face Left
            }
            else
            {
                spriteRenderer.flipX = false; // Face Right
            }
        }
    }

    // ========================================================
    // 2. ATTACK PLAYER & DESTROY SELF
    // ========================================================
    private void OnTriggerEnter2D(Collider2D collision)
    {
        HandlePlayerContact(collision.gameObject);
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        HandlePlayerContact(collision.gameObject);
    }

    private void HandlePlayerContact(GameObject target)
    {
        if (hasAttacked) return;

        // Verify if the target touched is the Player
        if (!target.CompareTag("Player")) return;

        hasAttacked = true;

        if (attackSound != null)
        {
            TopDownCharacter2D.FX.SoundManager.PlaySoundEffect(attackSound, transform.position);
        }

    

        // 1. Deal damage to player via PlayerMovement
        PlayerMovement player = target.GetComponentInParent<PlayerMovement>();
        if (player != null)
        {
            player.TakeDamage(attackDamage);
        }
        else
        {
            // Fallback for IDamageable
            IDamageable damageable = target.GetComponentInParent<IDamageable>();
            if (damageable != null)
            {
                damageable.TakeDamage((int)attackDamage);
            }
        }

        // 2. Disable colliders to prevent duplicate hits
        Collider2D col = GetComponent<Collider2D>();
        if (col != null) col.enabled = false;

        // 3. DESTROY SELF IMMEDIATELY AFTER ATTACKING

        Destroy(gameObject);
    }

    // ========================================================
    // 3. RECEIVING DAMAGE (IDamageable)
    // ========================================================
    public void TakeDamage(int damageAmount)
    {
        TakeDamage((float)damageAmount);
    }

    public void TakeDamage(float damageAmount)
    {
        health -= damageAmount;
        if (hurtSound != null)
        {
            TopDownCharacter2D.FX.SoundManager.PlaySoundEffect(hurtSound, transform.position);
        }
       

        if (health <= 0)
        {
            Die();
        }
    }

    private void Die()
    {
        Destroy(gameObject);
    }
}

