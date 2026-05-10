using UnityEngine;

public class SegmentedHealthBar : MonoBehaviour
{
    [SerializeField] private CharacterHealth health;
    [SerializeField] private int segmentCount = 7;
    [SerializeField] private Vector3 localOffset = new Vector3(0f, 1.1f, 0f);
    [SerializeField] private Vector2 segmentSize = new Vector2(0.12f, 0.04f);
    [SerializeField] private float segmentSpacing = 0.02f;
    [SerializeField] private Color filledColor = new Color(0.9f, 0.05f, 0.05f, 1f);
    [SerializeField] private Color emptyColor = new Color(0.15f, 0.15f, 0.15f, 1f);
    [SerializeField] private int sortingOrder = 20;

    private SpriteRenderer[] segments;
    private Sprite segmentSprite;

    private void Awake()
    {
        if (health == null)
        {
            health = GetComponent<CharacterHealth>();
        }

        BuildSegments();
    }

    private void OnEnable()
    {
        if (health == null)
        {
            health = GetComponent<CharacterHealth>();
        }

        if (health != null)
        {
            health.HealthChanged += SetHealth;
        }
    }

    private void OnDisable()
    {
        if (health != null)
        {
            health.HealthChanged -= SetHealth;
        }
    }

    private void Start()
    {
        if (health != null)
        {
            SetHealth(health.CurrentHealth, health.MaxHealth);
        }
    }

    public void SetHealth(int currentHealth, int maxHealth)
    {
        if (segments == null || segments.Length == 0)
        {
            return;
        }

        if (maxHealth <= 0)
        {
            maxHealth = segments.Length;
        }

        int filledSegments = Mathf.CeilToInt((float)currentHealth / maxHealth * segments.Length);

        for (int i = 0; i < segments.Length; i++)
        {
            segments[i].color = i < filledSegments ? filledColor : emptyColor;
        }
    }

    private void BuildSegments()
    {
        segmentCount = Mathf.Max(1, segmentCount);
        segments = new SpriteRenderer[segmentCount];

        float totalWidth = segmentCount * segmentSize.x + (segmentCount - 1) * segmentSpacing;
        float startX = -totalWidth * 0.5f + segmentSize.x * 0.5f;

        for (int i = 0; i < segmentCount; i++)
        {
            GameObject segment = new GameObject($"Health Segment {i + 1}");
            segment.name = $"Health Segment {i + 1}";
            segment.transform.SetParent(transform);
            segment.transform.localPosition = localOffset + new Vector3(startX + i * (segmentSize.x + segmentSpacing), 0f, 0f);
            segment.transform.localRotation = Quaternion.identity;
            segment.transform.localScale = new Vector3(segmentSize.x, segmentSize.y, 1f);

            SpriteRenderer renderer = segment.AddComponent<SpriteRenderer>();
            renderer.sprite = GetSegmentSprite();
            renderer.color = filledColor;
            renderer.sortingOrder = sortingOrder;
            segments[i] = renderer;
        }
    }

    private Sprite GetSegmentSprite()
    {
        if (segmentSprite != null)
        {
            return segmentSprite;
        }

        Texture2D texture = new Texture2D(1, 1);
        texture.SetPixel(0, 0, Color.white);
        texture.Apply();

        segmentSprite = Sprite.Create(texture, new Rect(0f, 0f, 1f, 1f), new Vector2(0.5f, 0.5f));
        return segmentSprite;
    }
}
