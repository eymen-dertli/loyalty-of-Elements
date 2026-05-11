using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;

public class MainMenu : MonoBehaviour
{
    private const string MusicEnabledKey = "MusicEnabled";
    private const string MusicVolumeKey = "MusicVolume";

    [Header("Music")]
    [SerializeField] private AudioClip musicClip;
    [SerializeField, Range(0f, 1f)] private float defaultMusicVolume = 0.7f;
    [SerializeField] private bool useGeneratedFallbackMusic = true;

    [Header("Settings UI")]
    [SerializeField] private Canvas menuCanvas;
    [SerializeField] private GameObject settingsPanel;
    [SerializeField] private Button settingsButton;
    [SerializeField] private Button closeSettingsButton;
    [SerializeField] private Toggle musicToggle;
    [SerializeField] private Slider musicVolumeSlider;

    private AudioSource musicSource;

    private void Awake()
    {
        EnsureMusicSource();
        BuildSettingsUiIfNeeded();
        LoadMusicSettings();
        CloseSettings();
    }

    private void Start()
    {
        ApplyMusicSettings();
    }

    public void PlayGame()
    {
        SceneManager.LoadScene("SampleScene");
    }

    public void OpenSettings()
    {
        if (settingsPanel != null)
        {
            settingsPanel.SetActive(true);
        }
    }

    public void CloseSettings()
    {
        if (settingsPanel != null)
        {
            settingsPanel.SetActive(false);
        }
    }

    public void SetMusicEnabled(bool enabled)
    {
        PlayerPrefs.SetInt(MusicEnabledKey, enabled ? 1 : 0);
        PlayerPrefs.Save();
        ApplyMusicSettings();
    }

    public void SetMusicVolume(float volume)
    {
        PlayerPrefs.SetFloat(MusicVolumeKey, Mathf.Clamp01(volume));
        PlayerPrefs.Save();
        ApplyMusicSettings();
    }

    private void EnsureMusicSource()
    {
        musicSource = GetComponent<AudioSource>();
        if (musicSource == null)
        {
            musicSource = gameObject.AddComponent<AudioSource>();
        }

        if (musicSource.clip == null)
        {
            musicSource.clip = musicClip;
        }

        if (musicSource.clip == null && useGeneratedFallbackMusic)
        {
            musicSource.clip = CreateFallbackMusicClip();
        }

        if (musicSource.clip != null && musicSource.clip.loadState == AudioDataLoadState.Unloaded)
        {
            musicSource.clip.LoadAudioData();
        }

        musicSource.loop = true;
        musicSource.playOnAwake = false;
        musicSource.spatialBlend = 0f;
        musicSource.dopplerLevel = 0f;
        musicSource.priority = 0;
    }

    private AudioClip CreateFallbackMusicClip()
    {
        const int sampleRate = 44100;
        const float lengthSeconds = 4f;
        int sampleCount = Mathf.RoundToInt(sampleRate * lengthSeconds);
        float[] samples = new float[sampleCount];
        float[] notes = { 261.63f, 329.63f, 392f, 523.25f };

        for (int i = 0; i < sampleCount; i++)
        {
            float time = (float)i / sampleRate;
            int noteIndex = Mathf.FloorToInt(time * 2f) % notes.Length;
            float tone = Mathf.Sin(2f * Mathf.PI * notes[noteIndex] * time);
            float harmony = Mathf.Sin(2f * Mathf.PI * notes[(noteIndex + 2) % notes.Length] * 0.5f * time);
            samples[i] = (tone * 0.08f) + (harmony * 0.04f);
        }

        AudioClip clip = AudioClip.Create("GeneratedMenuMusic", sampleCount, 1, sampleRate, false);
        clip.SetData(samples, 0);
        return clip;
    }

    private void LoadMusicSettings()
    {
        bool musicEnabled = PlayerPrefs.GetInt(MusicEnabledKey, 1) == 1;
        float musicVolume = PlayerPrefs.GetFloat(MusicVolumeKey, defaultMusicVolume);

        if (musicToggle != null)
        {
            musicToggle.SetIsOnWithoutNotify(musicEnabled);
            musicToggle.onValueChanged.RemoveListener(SetMusicEnabled);
            musicToggle.onValueChanged.AddListener(SetMusicEnabled);
        }

        if (musicVolumeSlider != null)
        {
            musicVolumeSlider.SetValueWithoutNotify(musicVolume);
            musicVolumeSlider.onValueChanged.RemoveListener(SetMusicVolume);
            musicVolumeSlider.onValueChanged.AddListener(SetMusicVolume);
        }
    }

    private void ApplyMusicSettings()
    {
        if (musicSource == null)
        {
            return;
        }

        bool musicEnabled = PlayerPrefs.GetInt(MusicEnabledKey, 1) == 1;
        float musicVolume = PlayerPrefs.GetFloat(MusicVolumeKey, defaultMusicVolume);
        musicSource.volume = musicVolume;
        musicSource.mute = !musicEnabled;

        if (musicSource.clip != null && musicEnabled && !musicSource.isPlaying)
        {
            musicSource.Play();
        }
        else if (!musicEnabled && musicSource.isPlaying)
        {
            musicSource.Pause();
        }
    }

    private void BuildSettingsUiIfNeeded()
    {
        if (menuCanvas == null)
        {
            menuCanvas = FindFirstObjectByType<Canvas>();
        }

        if (menuCanvas == null)
        {
            return;
        }

        if (settingsButton == null)
        {
            settingsButton = CreateButton("SettingsButton", "Ayarlar", new Vector2(0f, -185f), menuCanvas.transform);
            settingsButton.onClick.AddListener(OpenSettings);
        }

        if (settingsPanel == null)
        {
            settingsPanel = CreateSettingsPanel(menuCanvas.transform);
        }
    }

    private GameObject CreateSettingsPanel(Transform parent)
    {
        GameObject panel = new GameObject("SettingsPanel", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        panel.transform.SetParent(parent, false);

        RectTransform rectTransform = panel.GetComponent<RectTransform>();
        rectTransform.anchorMin = new Vector2(0.5f, 0.5f);
        rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
        rectTransform.anchoredPosition = Vector2.zero;
        rectTransform.sizeDelta = new Vector2(360f, 260f);

        Image panelImage = panel.GetComponent<Image>();
        panelImage.color = new Color(0.08f, 0.1f, 0.14f, 0.92f);

        CreateText("SettingsTitle", "Ayarlar", new Vector2(0f, 92f), new Vector2(320f, 42f), 32f, panel.transform);
        musicToggle = CreateToggle("MusicToggle", "Muzik", new Vector2(-86f, 32f), panel.transform);
        CreateText("VolumeLabel", "Ses", new Vector2(-120f, -28f), new Vector2(90f, 34f), 22f, panel.transform);
        musicVolumeSlider = CreateSlider("MusicVolumeSlider", new Vector2(55f, -28f), panel.transform);
        closeSettingsButton = CreateButton("CloseSettingsButton", "Kapat", new Vector2(0f, -92f), panel.transform);
        closeSettingsButton.onClick.AddListener(CloseSettings);

        return panel;
    }

    private Button CreateButton(string objectName, string label, Vector2 anchoredPosition, Transform parent)
    {
        GameObject buttonObject = new GameObject(objectName, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
        buttonObject.transform.SetParent(parent, false);

        RectTransform rectTransform = buttonObject.GetComponent<RectTransform>();
        rectTransform.anchorMin = new Vector2(0.5f, 0.5f);
        rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
        rectTransform.anchoredPosition = anchoredPosition;
        rectTransform.sizeDelta = new Vector2(150f, 50f);

        Image image = buttonObject.GetComponent<Image>();
        image.color = Color.white;

        Button button = buttonObject.GetComponent<Button>();
        button.targetGraphic = image;
        CreateText($"{objectName}Text", label, Vector2.zero, Vector2.zero, 24f, buttonObject.transform, true);
        return button;
    }

    private Toggle CreateToggle(string objectName, string label, Vector2 anchoredPosition, Transform parent)
    {
        GameObject toggleObject = new GameObject(objectName, typeof(RectTransform), typeof(Toggle));
        toggleObject.transform.SetParent(parent, false);

        RectTransform rectTransform = toggleObject.GetComponent<RectTransform>();
        rectTransform.anchorMin = new Vector2(0.5f, 0.5f);
        rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
        rectTransform.anchoredPosition = anchoredPosition;
        rectTransform.sizeDelta = new Vector2(190f, 38f);

        GameObject background = new GameObject("Background", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        background.transform.SetParent(toggleObject.transform, false);
        RectTransform backgroundRect = background.GetComponent<RectTransform>();
        backgroundRect.anchorMin = new Vector2(0f, 0.5f);
        backgroundRect.anchorMax = new Vector2(0f, 0.5f);
        backgroundRect.anchoredPosition = new Vector2(18f, 0f);
        backgroundRect.sizeDelta = new Vector2(28f, 28f);
        background.GetComponent<Image>().color = Color.white;

        GameObject checkmark = new GameObject("Checkmark", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        checkmark.transform.SetParent(background.transform, false);
        RectTransform checkmarkRect = checkmark.GetComponent<RectTransform>();
        checkmarkRect.anchorMin = new Vector2(0.5f, 0.5f);
        checkmarkRect.anchorMax = new Vector2(0.5f, 0.5f);
        checkmarkRect.anchoredPosition = Vector2.zero;
        checkmarkRect.sizeDelta = new Vector2(18f, 18f);
        checkmark.GetComponent<Image>().color = new Color(0.15f, 0.75f, 0.25f, 1f);

        CreateText($"{objectName}Text", label, new Vector2(74f, 0f), new Vector2(120f, 34f), 22f, toggleObject.transform);

        Toggle toggle = toggleObject.GetComponent<Toggle>();
        toggle.targetGraphic = background.GetComponent<Image>();
        toggle.graphic = checkmark.GetComponent<Image>();
        return toggle;
    }

    private Slider CreateSlider(string objectName, Vector2 anchoredPosition, Transform parent)
    {
        GameObject sliderObject = new GameObject(objectName, typeof(RectTransform), typeof(Slider));
        sliderObject.transform.SetParent(parent, false);

        RectTransform rectTransform = sliderObject.GetComponent<RectTransform>();
        rectTransform.anchorMin = new Vector2(0.5f, 0.5f);
        rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
        rectTransform.anchoredPosition = anchoredPosition;
        rectTransform.sizeDelta = new Vector2(210f, 24f);

        GameObject background = CreateSliderImage("Background", sliderObject.transform, new Color(0.25f, 0.25f, 0.25f, 1f));
        GameObject fillArea = new GameObject("Fill Area", typeof(RectTransform));
        fillArea.transform.SetParent(sliderObject.transform, false);
        RectTransform fillAreaRect = fillArea.GetComponent<RectTransform>();
        fillAreaRect.anchorMin = Vector2.zero;
        fillAreaRect.anchorMax = Vector2.one;
        fillAreaRect.offsetMin = new Vector2(6f, 0f);
        fillAreaRect.offsetMax = new Vector2(-6f, 0f);

        GameObject fill = CreateSliderImage("Fill", fillArea.transform, new Color(0.15f, 0.75f, 0.25f, 1f));
        GameObject handle = CreateSliderImage("Handle", sliderObject.transform, Color.white);

        RectTransform handleRect = handle.GetComponent<RectTransform>();
        handleRect.sizeDelta = new Vector2(20f, 28f);

        Slider slider = sliderObject.GetComponent<Slider>();
        slider.minValue = 0f;
        slider.maxValue = 1f;
        slider.wholeNumbers = false;
        slider.targetGraphic = handle.GetComponent<Image>();
        slider.fillRect = fill.GetComponent<RectTransform>();
        slider.handleRect = handleRect;

        RectTransform backgroundRect = background.GetComponent<RectTransform>();
        backgroundRect.anchorMin = Vector2.zero;
        backgroundRect.anchorMax = Vector2.one;
        backgroundRect.offsetMin = new Vector2(0f, 7f);
        backgroundRect.offsetMax = new Vector2(0f, -7f);

        return slider;
    }

    private GameObject CreateSliderImage(string objectName, Transform parent, Color color)
    {
        GameObject imageObject = new GameObject(objectName, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        imageObject.transform.SetParent(parent, false);
        RectTransform rectTransform = imageObject.GetComponent<RectTransform>();
        rectTransform.anchorMin = Vector2.zero;
        rectTransform.anchorMax = Vector2.one;
        rectTransform.offsetMin = Vector2.zero;
        rectTransform.offsetMax = Vector2.zero;
        imageObject.GetComponent<Image>().color = color;
        return imageObject;
    }

    private TMP_Text CreateText(
        string objectName,
        string text,
        Vector2 anchoredPosition,
        Vector2 size,
        float fontSize,
        Transform parent,
        bool stretch = false)
    {
        GameObject textObject = new GameObject(objectName, typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
        textObject.transform.SetParent(parent, false);

        RectTransform rectTransform = textObject.GetComponent<RectTransform>();
        if (stretch)
        {
            rectTransform.anchorMin = Vector2.zero;
            rectTransform.anchorMax = Vector2.one;
            rectTransform.offsetMin = Vector2.zero;
            rectTransform.offsetMax = Vector2.zero;
        }
        else
        {
            rectTransform.anchorMin = new Vector2(0.5f, 0.5f);
            rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
            rectTransform.anchoredPosition = anchoredPosition;
            rectTransform.sizeDelta = size;
        }

        TMP_Text label = textObject.GetComponent<TMP_Text>();
        label.text = text;
        label.fontSize = fontSize;
        label.alignment = TextAlignmentOptions.Center;
        label.color = Color.white;
        label.raycastTarget = false;
        return label;
    }
}
