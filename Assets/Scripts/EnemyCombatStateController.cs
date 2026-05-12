using UnityEngine;
using UnityEngine.Events;

public class EnemyCombatStateController : MonoBehaviour
{
    [Header("Target")]
    [SerializeField] private Transform target;

    [Header("Movement")]
    [SerializeField] private float moveSpeed = 75f;
    [SerializeField] private float stopDistance = 36f;
    [SerializeField] private bool lockToGroundY = true;
    [SerializeField] private float lockedGroundY;

    [Header("Attack")]
    [SerializeField] private float meleeAttackDistance = 45f;
    [SerializeField] private float rangeAttackDistance = 55f;
    [SerializeField] private float attackCooldown = 1.1f;
    [SerializeField] private int attackDamage = 15;
    [SerializeField] private bool canUseRangeAttack = false;
    [SerializeField] private bool logDamage = true;

    [Header("Health")]
    [SerializeField] private int maxHealth = 50;
    [SerializeField] private bool destroyOnDeath = false;
    [SerializeField] private float destroyDelay = 1.5f;

    [Header("Animator Parameters")]
    [SerializeField] private string isWalkingParameter = "isWalking";
    [SerializeField] private string meleeAttackParameter = "meleeAttack";
    [SerializeField] private string rangeAttackParameter = "rangeAttack";
    [SerializeField] private string isDeadParameter = "isDead";
    [SerializeField] private string idleStateName = "EnemyIdle";
    [SerializeField] private string walkStateName = "EnemyWalk";
    [SerializeField] private string meleeAttackStateName = "EnemyMeleeAttack";
    [SerializeField] private string rangeAttackStateName = "EnemyRangeAttack";
    [SerializeField] private string deathStateName = "EnemyDeath";

    [Header("Events")]
    [SerializeField] private UnityEvent<GameObject> meleeAttackEvent;
    [SerializeField] private UnityEvent<GameObject> rangeAttackEvent;
    [SerializeField] private UnityEvent deathEvent;

    private Animator animator;
    private Rigidbody2D rb;
    private CharacterHealth targetHealth;
    private int currentHealth;
    private float nextAttackTime;
    private float attackAnimationEndTime;
    private string currentAnimationState;
    private bool isDead;

    public bool IsDead => isDead;

    private void Awake()
    {
        animator = GetComponent<Animator>();
        rb = GetComponent<Rigidbody2D>();
        if (rb == null)
        {
            rb = gameObject.AddComponent<Rigidbody2D>();
        }

        rb.bodyType = RigidbodyType2D.Dynamic;
        rb.gravityScale = 0f;
        rb.freezeRotation = true;
        rb.constraints = RigidbodyConstraints2D.FreezeRotation | RigidbodyConstraints2D.FreezePositionY;
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
        lockedGroundY = transform.position.y;
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

        if (target == null)
        {
            CharacterHealth characterHealth = FindFirstObjectByType<CharacterHealth>();
            if (characterHealth != null)
            {
                target = characterHealth.transform;
            }
        }

        CacheTargetHealth();
    }

    private void FixedUpdate()
    {
        LockToGroundY();

        if (attackAnimationEndTime > 0f && Time.time >= attackAnimationEndTime)
        {
            attackAnimationEndTime = 0f;
            PlayState(idleStateName);
        }

        if (isDead || target == null)
        {
            StopWalking();
            return;
        }

        Vector2 toTarget = target.position - transform.position;
        float horizontalDistance = Mathf.Abs(toTarget.x);
        float verticalDistance = Mathf.Abs(toTarget.y);

        FlipToward(toTarget.x);

        if (horizontalDistance <= meleeAttackDistance && verticalDistance <= meleeAttackDistance)
        {
            StopWalking();
            TryMeleeAttack();
            return;
        }

        if (canUseRangeAttack && horizontalDistance <= rangeAttackDistance)
        {
            StopWalking();
            TryRangeAttack();
            return;
        }

        WalkToward(toTarget);
    }

    private void OnCollisionStay2D(Collision2D collision)
    {
        TryAttackCollidingCharacter(collision.collider);
    }

    private void OnTriggerStay2D(Collider2D other)
    {
        TryAttackCollidingCharacter(other);
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

    public void SetMaxHealth(int health)
    {
        maxHealth = Mathf.Max(1, health);
        currentHealth = maxHealth;
        isDead = false;
    }

    public void SetGroundY(float groundY)
    {
        lockedGroundY = groundY;
        lockToGroundY = true;
        LockToGroundY();
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
        PlayState(deathStateName, true);
        deathEvent?.Invoke();

        if (destroyOnDeath)
        {
            Destroy(gameObject, destroyDelay);
        }
    }

    private void WalkToward(Vector2 toTarget)
    {
        float horizontalDistance = Mathf.Abs(toTarget.x);
        if (horizontalDistance <= stopDistance)
        {
            StopWalking();
            return;
        }

        float directionX = Mathf.Sign(toTarget.x);
        Vector2 nextPosition = new Vector2(
            transform.position.x + directionX * moveSpeed * Time.fixedDeltaTime,
            lockToGroundY ? lockedGroundY : transform.position.y);

        if (BeatEmUpStageDirector.Instance != null)
        {
            nextPosition = BeatEmUpStageDirector.Instance.ClampCombatantPosition(nextPosition);
        }

        if (rb != null)
        {
            rb.MovePosition(nextPosition);
        }
        else
        {
            transform.position = nextPosition;
        }

        SetBool(isWalkingParameter, true);
        PlayState(walkStateName);
    }

    private void StopWalking()
    {
        if (rb != null)
        {
            rb.linearVelocity = Vector2.zero;
        }

        LockToGroundY();

        SetBool(isWalkingParameter, false);
        if (!isDead && attackAnimationEndTime <= 0f)
        {
            PlayState(idleStateName);
        }
    }

    private void TryMeleeAttack()
    {
        if (!CanAttack())
        {
            return;
        }

        nextAttackTime = Time.time + attackCooldown;
        SetTrigger(meleeAttackParameter);
        PlayAttackState(meleeAttackStateName);
        DamageTarget();
        meleeAttackEvent?.Invoke(target.gameObject);
    }

    private void TryAttackCollidingCharacter(Collider2D other)
    {
        if (isDead || other == null)
        {
            return;
        }

        CharacterHealth health = other.GetComponentInParent<CharacterHealth>();
        if (health == null)
        {
            return;
        }

        targetHealth = health;
        target = health.transform;
        TryMeleeAttack();
    }

    private void TryRangeAttack()
    {
        if (!CanAttack())
        {
            return;
        }

        nextAttackTime = Time.time + attackCooldown;
        SetTrigger(rangeAttackParameter);
        PlayAttackState(rangeAttackStateName);
        DamageTarget();
        rangeAttackEvent?.Invoke(target.gameObject);
    }

    private bool CanAttack()
    {
        return target != null && Time.time >= nextAttackTime;
    }

    private void DamageTarget()
    {
        CacheTargetHealth();

        if (targetHealth == null)
        {
            if (logDamage)
            {
                Debug.LogWarning($"{name} attacked but could not find a {nameof(CharacterHealth)} target.");
            }

            return;
        }

        targetHealth.TakeDamage(attackDamage);
        if (logDamage)
        {
            Debug.Log($"{name} dealt {attackDamage} damage to {targetHealth.name}. Health: {targetHealth.CurrentHealth}/{targetHealth.MaxHealth}");
        }
    }

    private void CacheTargetHealth()
    {
        if (targetHealth != null)
        {
            return;
        }

        if (target != null)
        {
            targetHealth = target.GetComponent<CharacterHealth>();
            if (targetHealth == null)
            {
                targetHealth = target.GetComponentInParent<CharacterHealth>();
            }
        }

        if (targetHealth == null)
        {
            targetHealth = FindFirstObjectByType<CharacterHealth>();
        }

        if (targetHealth != null)
        {
            target = targetHealth.transform;
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

    private void PlayState(string stateName, bool restart = false)
    {
        if (animator == null || string.IsNullOrWhiteSpace(stateName))
        {
            return;
        }

        if (!restart && currentAnimationState == stateName)
        {
            return;
        }

        currentAnimationState = stateName;
        animator.Play(stateName, 0, 0f);
    }

    private void PlayAttackState(string stateName)
    {
        PlayState(stateName, true);
        attackAnimationEndTime = Time.time + GetAnimationLength(stateName);
    }

    private float GetAnimationLength(string stateName)
    {
        if (animator == null || animator.runtimeAnimatorController == null)
        {
            return 0.35f;
        }

        foreach (AnimationClip clip in animator.runtimeAnimatorController.animationClips)
        {
            if (clip.name == stateName)
            {
                return clip.length;
            }
        }

        return 0.35f;
    }

    private void LockToGroundY()
    {
        if (!lockToGroundY)
        {
            return;
        }

        Vector3 position = transform.position;
        if (!Mathf.Approximately(position.y, lockedGroundY))
        {
            position.y = lockedGroundY;
            transform.position = position;
        }

        if (rb != null)
        {
            rb.linearVelocity = new Vector2(rb.linearVelocity.x, 0f);
            rb.constraints = RigidbodyConstraints2D.FreezeRotation | RigidbodyConstraints2D.FreezePositionY;
        }
    }
}
