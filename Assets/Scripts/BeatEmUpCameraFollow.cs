using UnityEngine;

public class BeatEmUpCameraFollow : MonoBehaviour
{
    [SerializeField] private Transform target;
    [SerializeField] private Vector2 offset = new Vector2(30f, 0f);
    [SerializeField] private float smoothTime = 0.18f;
    [SerializeField] private bool followY = false;
    [SerializeField] private bool followBackward = false;
    [SerializeField] private bool snapToTargetOnStart = true;

    private Vector3 velocity;
    private bool isLocked;

    public bool IsLocked => isLocked;

    private void Awake()
    {
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

    private void Start()
    {
        if (snapToTargetOnStart && target != null)
        {
            Vector3 position = transform.position;
            position.x = target.position.x + offset.x;

            if (followY)
            {
                position.y = target.position.y + offset.y;
            }

            transform.position = position;
        }
    }

    private void LateUpdate()
    {
        if (target == null)
        {
            return;
        }

        Vector3 desiredPosition = transform.position;
        desiredPosition.x = target.position.x + offset.x;

        if (isLocked)
        {
            return;
        }

        if (!followBackward)
        {
            desiredPosition.x = Mathf.Max(transform.position.x, desiredPosition.x);
        }

        if (followY)
        {
            desiredPosition.y = target.position.y + offset.y;
        }

        transform.position = Vector3.SmoothDamp(transform.position, desiredPosition, ref velocity, smoothTime);
    }

    public void LockToCurrentPosition()
    {
        isLocked = true;
        velocity = Vector3.zero;
    }

    public void Unlock()
    {
        isLocked = false;
    }
}
