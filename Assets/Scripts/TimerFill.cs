using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

[RequireComponent(typeof(Image))]
public class TimerFill : MonoBehaviour
{
    [SerializeField] private Image fillImage;
    [SerializeField] private float duration = 10f;
    [SerializeField] private bool playOnStart = true;
    [SerializeField] private bool countDown = true;
    [SerializeField] private Color fillColor = Color.green;
    [SerializeField] private UnityEvent timerCompleted;

    private float elapsedTime;
    private bool isRunning;

    public float Duration
    {
        get => duration;
        set => duration = Mathf.Max(0.01f, value);
    }

    public float NormalizedTime => Mathf.Clamp01(elapsedTime / Duration);
    public bool IsRunning => isRunning;

    private void Reset()
    {
        fillImage = GetComponent<Image>();
        ConfigureImage();
    }

    private void Awake()
    {
        if (fillImage == null)
        {
            fillImage = GetComponent<Image>();
        }

        ConfigureImage();
    }

    private void Start()
    {
        if (playOnStart)
        {
            StartTimer(duration);
        }
        else
        {
            SetFill(countDown ? 1f : 0f);
        }
    }

    private void Update()
    {
        if (!isRunning)
        {
            return;
        }

        elapsedTime += Time.deltaTime;
        float progress = NormalizedTime;
        SetFill(countDown ? 1f - progress : progress);

        if (elapsedTime >= Duration)
        {
            isRunning = false;
            SetFill(countDown ? 0f : 1f);
            timerCompleted?.Invoke();
        }
    }

    public void StartTimer(float newDuration)
    {
        Duration = newDuration;
        elapsedTime = 0f;
        isRunning = true;
        SetFill(countDown ? 1f : 0f);
    }

    public void RestartTimer()
    {
        StartTimer(duration);
    }

    public void StopTimer()
    {
        isRunning = false;
    }

    public void SetFill(float amount)
    {
        if (fillImage != null)
        {
            fillImage.fillAmount = Mathf.Clamp01(amount);
        }
    }

    private void ConfigureImage()
    {
        if (fillImage == null)
        {
            return;
        }

        fillImage.color = fillColor;
        fillImage.type = Image.Type.Filled;
        fillImage.fillMethod = Image.FillMethod.Horizontal;
        fillImage.fillOrigin = 0;
        fillImage.preserveAspect = true;
    }
}
