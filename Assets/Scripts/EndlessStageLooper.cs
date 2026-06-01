using UnityEngine;

#pragma warning disable 0414
public class EndlessStageLooper : MonoBehaviour
{
    [SerializeField] private Transform target;
    [SerializeField] private float segmentWidth = 354f;
    [SerializeField] private int segmentsBefore = 0;
    [SerializeField] private int segmentsAfter = 4;
    [SerializeField] private bool loopSegments = false;
    [SerializeField] private float recycleDistanceInSegments = 2f;

    private float previousTargetX;
    private bool hasPreviousTargetX;

    public float SegmentWidth => Mathf.Max(1f, segmentWidth);

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

    private void LateUpdate()
    {
        if (!loopSegments || target == null)
        {
            return;
        }

        float targetX = target.position.x;
        previousTargetX = targetX;
        hasPreviousTargetX = true;
    }
}
#pragma warning restore 0414
