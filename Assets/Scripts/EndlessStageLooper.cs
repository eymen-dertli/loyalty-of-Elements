using System.Collections.Generic;
using UnityEngine;

public class EndlessStageLooper : MonoBehaviour
{
    [SerializeField] private Transform target;
    [SerializeField] private float segmentWidth = 40f;
    [SerializeField] private int segmentsBefore = 2;
    [SerializeField] private int segmentsAfter = 3;
    [SerializeField] private bool loopSegments = false;
    [SerializeField] private float recycleDistanceInSegments = 2f;

    private readonly List<Transform> segments = new List<Transform>();
    private float previousTargetX;
    private bool hasPreviousTargetX;

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
        if (segmentWidth <= 0f)
        {
            Debug.LogWarning($"{nameof(EndlessStageLooper)} on {name} needs a positive segment width.");
            enabled = false;
            return;
        }

        BuildRuntimeSegments();
    }

    private void LateUpdate()
    {
        if (target == null || segments.Count == 0)
        {
            return;
        }

        if (!loopSegments)
        {
            return;
        }

        float targetX = transform.InverseTransformPoint(target.position).x;
        RecycleSegmentsAround(targetX);
        previousTargetX = targetX;
        hasPreviousTargetX = true;
    }

    private void BuildRuntimeSegments()
    {
        List<Transform> originalChildren = new List<Transform>();
        for (int i = 0; i < transform.childCount; i++)
        {
            originalChildren.Add(transform.GetChild(i));
        }

        if (originalChildren.Count == 0)
        {
            Debug.LogWarning($"{nameof(EndlessStageLooper)} on {name} has no child objects to loop.");
            enabled = false;
            return;
        }

        Transform baseSegment = CreateSegmentRoot("Stage Segment 0", 0f);
        foreach (Transform child in originalChildren)
        {
            child.SetParent(baseSegment, false);
        }

        segments.Add(baseSegment);

        for (int i = 1; i <= segmentsAfter; i++)
        {
            segments.Add(CloneSegment(baseSegment, i));
        }

        for (int i = 1; i <= segmentsBefore; i++)
        {
            segments.Add(CloneSegment(baseSegment, -i));
        }
    }

    private Transform CreateSegmentRoot(string segmentName, float localX)
    {
        GameObject segmentObject = new GameObject(segmentName);
        Transform segmentTransform = segmentObject.transform;
        segmentTransform.SetParent(transform, false);
        segmentTransform.localPosition = new Vector3(localX, 0f, 0f);
        segmentTransform.localRotation = Quaternion.identity;
        segmentTransform.localScale = Vector3.one;
        return segmentTransform;
    }

    private Transform CloneSegment(Transform baseSegment, int index)
    {
        Transform clone = Instantiate(baseSegment, transform);
        clone.name = $"Stage Segment {index}";
        clone.localPosition = new Vector3(index * segmentWidth, 0f, 0f);
        clone.localRotation = Quaternion.identity;
        clone.localScale = Vector3.one;
        return clone;
    }

    private void RecycleSegmentsAround(float targetX)
    {
        if (!hasPreviousTargetX)
        {
            previousTargetX = targetX;
            hasPreviousTargetX = true;
            return;
        }

        float distance = Mathf.Max(1f, recycleDistanceInSegments) * segmentWidth;

        if (targetX >= previousTargetX)
        {
            while (targetX - GetLeftmostSegment().localPosition.x > distance)
            {
                Transform leftmost = GetLeftmostSegment();
                Transform rightmost = GetRightmostSegment();
                leftmost.localPosition = new Vector3(rightmost.localPosition.x + segmentWidth, 0f, 0f);
            }

            return;
        }

        while (GetRightmostSegment().localPosition.x - targetX > distance)
        {
            Transform rightmost = GetRightmostSegment();
            Transform leftmost = GetLeftmostSegment();
            rightmost.localPosition = new Vector3(leftmost.localPosition.x - segmentWidth, 0f, 0f);
        }
    }

    private Transform GetLeftmostSegment()
    {
        Transform leftmost = segments[0];
        for (int i = 1; i < segments.Count; i++)
        {
            if (segments[i].localPosition.x < leftmost.localPosition.x)
            {
                leftmost = segments[i];
            }
        }

        return leftmost;
    }

    private Transform GetRightmostSegment()
    {
        Transform rightmost = segments[0];
        for (int i = 1; i < segments.Count; i++)
        {
            if (segments[i].localPosition.x > rightmost.localPosition.x)
            {
                rightmost = segments[i];
            }
        }

        return rightmost;
    }
}
