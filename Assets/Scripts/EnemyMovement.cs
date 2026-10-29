using UnityEngine;

/// <summary>
///     Complete Top-Down Enemy Controller
///     Handles:
///     1. Detecting and moving towards the Player
///     2. Flipping sprite to face the Player
///     3. Damaging the Player and IMMEDIATELY DESTROYING ITSELF upon attack
///     4. Taking damage from the Player (IDamageable)
/// </summary>
public class EnemyMovement : MonoBehaviour, IDamageable
{
    [Header("Movement Settings")]
    [Tooltip("Movement speed while chasing player")]
    public float moveSpeed = 2.5f;

    [Tooltip("Minimum distance to player before attacking")]
    public float stoppingDistance = 0.5f;

    [Header("Combat Settings")]
    [Tooltip("Damage dealt to player on contact")]
    public float attackDamage = 15f;

    [Tooltip("Enemy health points")]
    public float health = 50f;

    private Transform playerTarget;
    private SpriteRenderer spriteRenderer;
    private Rigidbody2D rb;
    private bool hasAttacked = false;

    private void Awake()
    {
        spriteRenderer = GetComponentInChildren<SpriteRenderer>();
        rb = GetComponent<Rigidbody2D>();
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

        Debug.Log($"<color=red>[ENEMY ATTACK]</color> {gameObject.name} attacked Player dealing {attackDamage} damage!");

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
        Debug.Log($"<color=yellow>[DESTROY]</color> {gameObject.name} is destroyed after attacking!");
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
        Debug.Log($"<color=cyan>[ENEMY HIT]</color> {gameObject.name} took {damageAmount} damage! Remaining HP: {health}");

        if (health <= 0)
        {
            Die();
        }
    }

    private void Die()
    {
        Debug.Log($"<color=green>[ENEMY DEFEATED]</color> {gameObject.name} has been killed!");
        Destroy(gameObject);
    }
}

