using UnityEngine;
using UnityEngine.Events;

public class SimpleBeatEmUpEnemyAI : MonoBehaviour
{
    [SerializeField] private Transform target;
    [SerializeField] private float moveSpeed = 35f;
    [SerializeField] private float attackRange = 18f;
    [SerializeField] private float attackCooldown = 1.2f;
    [SerializeField] private int attackDamage = 15;
    [SerializeField] private UnityEvent<GameObject> attackEvent;

    private Rigidbody2D rb;
    private float nextAttackTime;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        if (rb == null)
        {
            rb = gameObject.AddComponent<Rigidbody2D>();
        }

        rb.bodyType = RigidbodyType2D.Dynamic;
        rb.gravityScale = 0f;
        rb.freezeRotation = true;
        rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
        rb.interpolation = RigidbodyInterpolation2D.Interpolate;

        BoxCollider2D bodyCollider = GetComponent<BoxCollider2D>();
        if (bodyCollider == null)
        {
            bodyCollider = gameObject.AddComponent<BoxCollider2D>();
        }

        bodyCollider.isTrigger = false;
        bodyCollider.size = new Vector2(0.95f, 1.35f);
        bodyCollider.offset = new Vector2(0f, -0.08f);

        if (target == null)
        {
            GameObject player = GameObject.FindGameObjectWithTag("Player");
            if (player == null)
            {
                player = GameObject.Find("Player");
            }

            if (player != null)
            {
                target = player.transform;
            }
        }

        if (target == null)
        {
            CharacterHealth characterHealth = FindFirstObjectByType<CharacterHealth>();
            if (characterHealth != null)
            {
                target = characterHealth.transform;
            }
        }
    }

    private void FixedUpdate()
    {
        if (target == null)
        {
            StopMoving();
            return;
        }

        Vector2 direction = target.position - transform.position;
        if (direction.magnitude <= attackRange)
        {
            StopMoving();
            TryAttack();
            return;
        }

        Vector2 velocity = direction.normalized * moveSpeed;
        if (rb != null)
        {
            rb.linearVelocity = velocity;
        }
        else
        {
            transform.position += (Vector3)(velocity * Time.fixedDeltaTime);
        }

        FlipToward(direction.x);
    }

    private void StopMoving()
    {
        if (rb != null)
        {
            rb.linearVelocity = Vector2.zero;
        }
    }

    private void TryAttack()
    {
        if (Time.time < nextAttackTime)
        {
            return;
        }

        nextAttackTime = Time.time + attackCooldown;
        DamageTarget();
        attackEvent?.Invoke(target.gameObject);
    }

    private void DamageTarget()
    {
        if (target == null)
        {
            return;
        }

        CharacterHealth health = target.GetComponent<CharacterHealth>();
        if (health != null)
        {
            health.TakeDamage(attackDamage);
        }
    }

    private void FlipToward(float directionX)
    {
        if (Mathf.Abs(directionX) < 0.01f)
        {
            return;
        }

        Vector3 scale = transform.localScale;
        scale.x = Mathf.Abs(scale.x) * Mathf.Sign(directionX);
        transform.localScale = scale;
    }
}
