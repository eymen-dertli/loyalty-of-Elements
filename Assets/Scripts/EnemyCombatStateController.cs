using UnityEngine;
using UnityEngine.Events;

public class EnemyCombatStateController : MonoBehaviour
{
    [Header("Target")]
    [SerializeField] private Transform target;

    [Header("Movement")]
    [SerializeField] private float moveSpeed = 35f;
    [SerializeField] private float stopDistance = 12f;

    [Header("Attack")]
    [SerializeField] private float meleeAttackDistance = 14f;
    [SerializeField] private float rangeAttackDistance = 55f;
    [SerializeField] private float attackCooldown = 1.1f;
    [SerializeField] private bool canUseRangeAttack = true;

    [Header("Health")]
    [SerializeField] private int maxHealth = 3;
    [SerializeField] private bool destroyOnDeath = false;
    [SerializeField] private float destroyDelay = 1.5f;

    [Header("Animator Parameters")]
    [SerializeField] private string isWalkingParameter = "isWalking";
    [SerializeField] private string meleeAttackParameter = "meleeAttack";
    [SerializeField] private string rangeAttackParameter = "rangeAttack";
    [SerializeField] private string isDeadParameter = "isDead";

    [Header("Events")]
    [SerializeField] private UnityEvent<GameObject> meleeAttackEvent;
    [SerializeField] private UnityEvent<GameObject> rangeAttackEvent;
    [SerializeField] private UnityEvent deathEvent;

    private Animator animator;
    private Rigidbody2D rb;
    private int currentHealth;
    private float nextAttackTime;
    private bool isDead;

    private void Awake()
    {
        animator = GetComponent<Animator>();
        rb = GetComponent<Rigidbody2D>();
        currentHealth = maxHealth;

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
        if (isDead || target == null)
        {
            StopWalking();
            return;
        }

        Vector2 toTarget = target.position - transform.position;
        float distance = toTarget.magnitude;

        FlipToward(toTarget.x);

        if (distance <= meleeAttackDistance)
        {
            StopWalking();
            TryMeleeAttack();
            return;
        }

        if (canUseRangeAttack && distance <= rangeAttackDistance)
        {
            StopWalking();
            TryRangeAttack();
            return;
        }

        WalkToward(toTarget);
    }

    public void TakeDamage(int damage)
    {
        if (isDead)
        {
            return;
        }

        currentHealth -= Mathf.Max(1, damage);
        if (currentHealth <= 0)
        {
            Die();
        }
    }

    public void Die()
    {
        if (isDead)
        {
            return;
        }

        isDead = true;
        StopWalking();
        SetBool(isDeadParameter, true);
        deathEvent?.Invoke();

        if (destroyOnDeath)
        {
            Destroy(gameObject, destroyDelay);
        }
    }

    private void WalkToward(Vector2 toTarget)
    {
        if (toTarget.magnitude <= stopDistance)
        {
            StopWalking();
            return;
        }

        Vector2 velocity = toTarget.normalized * moveSpeed;
        if (rb != null)
        {
            rb.linearVelocity = velocity;
        }
        else
        {
            transform.position += (Vector3)(velocity * Time.fixedDeltaTime);
        }

        SetBool(isWalkingParameter, true);
    }

    private void StopWalking()
    {
        if (rb != null)
        {
            rb.linearVelocity = Vector2.zero;
        }

        SetBool(isWalkingParameter, false);
    }

    private void TryMeleeAttack()
    {
        if (!CanAttack())
        {
            return;
        }

        nextAttackTime = Time.time + attackCooldown;
        SetTrigger(meleeAttackParameter);
        meleeAttackEvent?.Invoke(target.gameObject);
    }

    private void TryRangeAttack()
    {
        if (!CanAttack())
        {
            return;
        }

        nextAttackTime = Time.time + attackCooldown;
        SetTrigger(rangeAttackParameter);
        rangeAttackEvent?.Invoke(target.gameObject);
    }

    private bool CanAttack()
    {
        return target != null && Time.time >= nextAttackTime;
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

    private void SetBool(string parameterName, bool value)
    {
        if (animator != null && !string.IsNullOrWhiteSpace(parameterName))
        {
            animator.SetBool(parameterName, value);
        }
    }

    private void SetTrigger(string parameterName)
    {
        if (animator != null && !string.IsNullOrWhiteSpace(parameterName))
        {
            animator.SetTrigger(parameterName);
        }
    }
}
