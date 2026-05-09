using UnityEngine;
using UnityEngine.Events;

public class SimpleBeatEmUpEnemyAI : MonoBehaviour
{
    [SerializeField] private Transform target;
    [SerializeField] private float moveSpeed = 35f;
    [SerializeField] private float attackRange = 18f;
    [SerializeField] private float attackCooldown = 1.2f;
    [SerializeField] private UnityEvent<GameObject> attackEvent;

    private Rigidbody2D rb;
    private float nextAttackTime;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();

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
        attackEvent?.Invoke(target.gameObject);
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
