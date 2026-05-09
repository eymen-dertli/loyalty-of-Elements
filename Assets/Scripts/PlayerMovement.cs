using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerMovement : MonoBehaviour
{
    [SerializeField] private float moveSpeed = 5f;

    private Rigidbody2D rb;
    private BeatEmUpStageDirector stageDirector;
    private float moveInput;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        stageDirector = BeatEmUpStageDirector.Instance;
    }

    private void Start()
    {
        if (stageDirector == null)
        {
            stageDirector = BeatEmUpStageDirector.Instance;
        }
    }

    public void OnMove(InputAction.CallbackContext context)
    {
        moveInput = context.ReadValue<float>();
    }

    private void FixedUpdate()
    {
        float horizontalVelocity = moveInput * moveSpeed;

        if (stageDirector != null)
        {
            horizontalVelocity = stageDirector.FilterHorizontalVelocity(rb.position.x, horizontalVelocity);
        }

        rb.linearVelocity = new Vector2(horizontalVelocity, rb.linearVelocity.y);

        if (stageDirector != null)
        {
            rb.position = stageDirector.ClampPlayerPosition(rb.position);
        }
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
