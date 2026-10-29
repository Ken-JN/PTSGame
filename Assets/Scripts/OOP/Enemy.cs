using UnityEngine;
using UnityEngine.Serialization;
using System;

public class Enemy : MonoBehaviour, IDamageable
{
    [SerializeField] private int hp = 100;
    public float ms = 2f;

    protected Transform player;

    [Header("State Machine")]
    [FormerlySerializedAs("JarakDeteksi")]
    [SerializeField] private float detectionRange = 6f;

    [FormerlySerializedAs("JarakSerang")]
    [SerializeField] private float attackRange = 1.5f;

    [FormerlySerializedAs("JedaSerang")]
    [SerializeField] private float attackCooldown = 0.3f;

    [Header("Patrol")]
    [FormerlySerializedAs("radiusPatrol")]
    [SerializeField] private float patrolRadius = 3f;
    private Vector2 startingPoint;
    private Vector2 patrolDestination;

    // Current State
    private StateZombie state = StateZombie.IDLE;
    private float lastAttackTime;

    // Event when zombie dies
    public static event Action<Enemy> OnZombieDeath;

    [FormerlySerializedAs("damageSaatTabrakan")]
    [SerializeField] private int collisionDamage = 15;

    protected virtual void Start()
    {
        GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
        if (playerObj != null)
        {
            player = playerObj.transform;
        }

        startingPoint = transform.position;
        ChooseNewPatrolDestination();
    }

    void Update()
    {
        CheckStateTransitions();

        switch (state)
        {
            case StateZombie.IDLE: IdleBehavior(); break;
            case StateZombie.PATROL: PatrolBehavior(); break;
            case StateZombie.CHASE: ChaseBehavior(); break;
            case StateZombie.ATTACK: AttackBehavior(); break;
        }
    }

    public float DistanceToPlayer()
    {
        if (player == null) return Mathf.Infinity;
        return Vector2.Distance(transform.position, player.position);
    }

    void CheckStateTransitions()
    {
        float distance = DistanceToPlayer();

        if (distance <= attackRange)
        {
            state = StateZombie.ATTACK;
        }
        else if (distance <= detectionRange)
        {
            state = StateZombie.CHASE;
        }
        else
        {
            state = StateZombie.PATROL;
        }
    }

    void ChooseNewPatrolDestination()
    {
        Vector2 randomOffset = UnityEngine.Random.insideUnitCircle * patrolRadius;
        patrolDestination = startingPoint + randomOffset;
    }

    public void Chase()
    {
        if (player == null) return;

        transform.position = Vector2.MoveTowards(
            transform.position,
            player.position,
            ms * Time.deltaTime
        );
    }

    public virtual void Attack()
    {
        Debug.Log("Enemy Attacks");
    }

    void IdleBehavior()
    {
    }

    void PatrolBehavior()
    {
        transform.position = Vector2.MoveTowards(
            transform.position,
            patrolDestination,
            ms * 0.5f * Time.deltaTime
        );

        if (Vector2.Distance(transform.position, patrolDestination) < 0.1f)
        {
            ChooseNewPatrolDestination();
        }
    }

    void ChaseBehavior()
    {
        Chase();
    }

    void AttackBehavior()
    {
        if (Time.time >= lastAttackTime + attackCooldown)
        {
            Attack();
            lastAttackTime = Time.time;
        }
    }

    public void TakeDamage(int damage)
    {
        hp -= damage;
        Debug.Log($"Enemy took damage: {damage}, Current HP: {hp}");

        if (hp <= 0)
        {
            Die();
        }
    }

    protected virtual void Die()
    {
        Debug.Log("Enemy died!");
        OnZombieDeath?.Invoke(this);
        Destroy(gameObject);
    }

    [ContextMenu("Test Event: Zombie Death")]
    void TestDeathEvent()
    {
        Die();
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        AttackPlayerAndSelfDestruct(other.gameObject);
    }

    void OnCollisionEnter2D(Collision2D collision)
    {
        AttackPlayerAndSelfDestruct(collision.gameObject);
    }

    private void AttackPlayerAndSelfDestruct(GameObject target)
    {
        if (target.CompareTag("Player"))
        {
            IDamageable playerScript = target.GetComponentInParent<IDamageable>();
            if (playerScript != null)
            {
                playerScript.TakeDamage(collisionDamage);
            }

            Debug.Log($"<color=yellow>{gameObject.name} destroyed after attacking player!</color>");
            Destroy(gameObject);
        }
    }
}
