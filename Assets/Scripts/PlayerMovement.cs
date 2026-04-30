using UnityEngine;
using UnityEngine.InputSystem;
using static UnityEngine.RuleTile.TilingRuleOutput;

public class PlayerMovement : MonoBehaviour
{
    [SerializeField] private float moveSpeed = 5f;

    private Rigidbody2D rb;
    private float moveInput;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
    }

    public void OnMove(InputAction.CallbackContext context)
    {
        moveInput = context.ReadValue<float>();
    }

    private void FixedUpdate()
    {
        rb.linearVelocity = new Vector2(moveInput * moveSpeed, rb.linearVelocity.y);
    }

    private void Update()
    {
        float baseScaleX = Mathf.Abs(transform.localScale.x);

        if (moveInput > 0)
            transform.localScale = new Vector3(-baseScaleX, transform.localScale.y, transform.localScale.z);
        else if (moveInput < 0)
            transform.localScale = new Vector3(baseScaleX, transform.localScale.y, transform.localScale.z);
    }
}