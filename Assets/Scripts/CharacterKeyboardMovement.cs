using UnityEngine;
using UnityEngine.InputSystem;

public class CharacterKeyboardMovement : MonoBehaviour
{
    [SerializeField] private float moveSpeed = 100f;
    [SerializeField] private float runMultiplier = 1.6f;
    [SerializeField] private float jumpVelocity = 85f;
    [SerializeField] private float gravity = 220f;
    [SerializeField] private string idleStateName = "PlayerIdle";
    [SerializeField] private string walkStateName = "PlayerWalk";
    [SerializeField] private string runStateName = "PlayerRun";
    [SerializeField] private string jumpStateName = "PlayerJump";

    private Animator animator;
    private CharacterMouseAttack mouseAttack;
    private CharacterSpellCaster spellCaster;
    private Rigidbody2D rb;
    private float groundY;
    private float verticalVelocity;
    private string currentAnimationState;

    private void Awake()
    {
        animator = GetComponent<Animator>();
        mouseAttack = GetComponent<CharacterMouseAttack>();
        spellCaster = GetComponent<CharacterSpellCaster>();
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
        bodyCollider.size = new Vector2(0.7f, 1.25f);
        bodyCollider.offset = new Vector2(0f, -0.1f);
        groundY = transform.position.y;
    }

    private void Update()
    {
        Keyboard keyboard = Keyboard.current;

        if (keyboard == null)
        {
            return;
        }

        float horizontalInput = 0f;

        if (keyboard.aKey.isPressed)
        {
            horizontalInput -= 1f;
        }

        if (keyboard.dKey.isPressed)
        {
            horizontalInput += 1f;
        }

        bool isGrounded = IsGrounded();
        bool isRunning = keyboard.sKey.isPressed && !Mathf.Approximately(horizontalInput, 0f);

        if (isGrounded && keyboard.spaceKey.wasPressedThisFrame)
        {
            verticalVelocity = jumpVelocity;
            PlayMovementAnimation(jumpStateName);
        }

        verticalVelocity -= gravity * Time.deltaTime;

        Vector3 position = transform.position;
        float currentMoveSpeed = isRunning ? moveSpeed * runMultiplier : moveSpeed;
        position.x += horizontalInput * currentMoveSpeed * Time.deltaTime;
        position.y += verticalVelocity * Time.deltaTime;
        if (BeatEmUpStageDirector.Instance != null)
        {
            position = BeatEmUpStageDirector.Instance.ClampPlayerPosition(position);
        }

        if (position.y <= groundY)
        {
            position.y = groundY;
            verticalVelocity = 0f;
        }

        if (rb != null)
        {
            rb.MovePosition(position);
        }
        else
        {
            transform.position = position;
        }
        Flip(horizontalInput);
        UpdateMovementAnimation(horizontalInput, isRunning);
    }

    private bool IsGrounded()
    {
        return transform.position.y <= groundY + 0.01f && verticalVelocity <= 0f;
    }

    private void Flip(float horizontalInput)
    {
        if (Mathf.Approximately(horizontalInput, 0f))
        {
            return;
        }

        float baseScaleX = Mathf.Abs(transform.localScale.x);
        float scaleX = horizontalInput > 0f ? baseScaleX : -baseScaleX;
        transform.localScale = new Vector3(scaleX, transform.localScale.y, transform.localScale.z);
    }

    private void UpdateMovementAnimation(float horizontalInput, bool isRunning)
    {
        if (animator == null || IsAttackPlaying())
        {
            return;
        }

        if (!IsGrounded())
        {
            PlayMovementAnimation(jumpStateName);
        }
        else if (!Mathf.Approximately(horizontalInput, 0f))
        {
            PlayMovementAnimation(isRunning ? runStateName : walkStateName);
        }
        else
        {
            PlayMovementAnimation(idleStateName);
        }
    }

    private void PlayMovementAnimation(string stateName)
    {
        if (animator == null || currentAnimationState == stateName)
        {
            return;
        }

        currentAnimationState = stateName;
        animator.Play(stateName, 0, 0f);
    }

    private bool IsAttackPlaying()
    {
        return (mouseAttack != null && mouseAttack.IsAttacking) || (spellCaster != null && spellCaster.IsCasting);
    }
}
