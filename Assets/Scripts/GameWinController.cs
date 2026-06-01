using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class GameWinController : MonoBehaviour
{
    public const string OpenLevelPanelKey = "OpenLevelPanelOnLoad";

    [SerializeField] private string mainSceneName = "MainScene";
    [SerializeField] private GameObject winPanel;
    [SerializeField] private Button continueButton;

    private static bool isShowing;

    public static void ShowWin()
    {
        if (isShowing)
        {
            return;
        }

        GameWinController controller = FindFirstObjectByType<GameWinController>();
        if (controller == null)
        {
            GameObject controllerObject = new GameObject(nameof(GameWinController));
            controller = controllerObject.AddComponent<GameWinController>();
        }

        controller.ShowWinPanel();
    }

    private void Awake()
    {
        if (winPanel != null)
        {
            winPanel.SetActive(false);
        }

        if (continueButton != null)
        {
            continueButton.onClick.RemoveListener(ContinueToLevelPanel);
            continueButton.onClick.AddListener(ContinueToLevelPanel);
        }
    }

    public void ShowWinPanel()
    {
        isShowing = true;

        if (winPanel == null)
        {
            BuildWinPanel();
        }

        winPanel.SetActive(true);
        Time.timeScale = 0f;
    }

    public void ContinueToLevelPanel()
    {
        Time.timeScale = 1f;
        isShowing = false;
        PlayerPrefs.SetInt(OpenLevelPanelKey, 1);
        PlayerPrefs.Save();
        SceneManager.LoadScene(mainSceneName);
    }

    private void BuildWinPanel()
    {
        Canvas canvas = FindFirstObjectByType<Canvas>();
        if (canvas == null)
        {
            GameObject canvasObject = new GameObject("Canvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            canvas = canvasObject.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;

            CanvasScaler scaler = canvasObject.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
        }

        winPanel = new GameObject("WinPanel", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        winPanel.transform.SetParent(canvas.transform, false);

        RectTransform panelRect = winPanel.GetComponent<RectTransform>();
        panelRect.anchorMin = Vector2.zero;
        panelRect.anchorMax = Vector2.one;
        panelRect.offsetMin = Vector2.zero;
        panelRect.offsetMax = Vector2.zero;

        Image panelImage = winPanel.GetComponent<Image>();
        panelImage.color = new Color(0f, 0f, 0f, 0.392f);

        TMP_Text title = CreateText("YouWinText", "YOU WIN", new Vector2(0f, 0f), new Vector2(200f, 50f), 80f, winPanel.transform);
        CopyTextStyle(FindSceneComponentByObjectName<TMP_Text>("YouLoseText"), title);
        title.color = new Color(0.39607844f, 0.9607843f, 0.30588236f, 1f);
        title.faceColor = title.color;

        continueButton = CreateButton("ContinueButton", "CONTINUE", new Vector2(23.904388f, -132f), winPanel.transform);
        CopyButtonStyle(FindSceneComponentByObjectName<Button>("TryAgainButton"), continueButton);
        continueButton.onClick.AddListener(ContinueToLevelPanel);
    }

    private Button CreateButton(string objectName, string label, Vector2 anchoredPosition, Transform parent)
    {
        GameObject buttonObject = new GameObject(objectName, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
        buttonObject.transform.SetParent(parent, false);

        RectTransform rectTransform = buttonObject.GetComponent<RectTransform>();
        rectTransform.anchorMin = new Vector2(0.5f, 0.5f);
        rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
        rectTransform.anchoredPosition = anchoredPosition;
        rectTransform.sizeDelta = new Vector2(247.8098f, 46.3209f);

        Image image = buttonObject.GetComponent<Image>();
        image.color = new Color(1f, 1f, 1f, 0f);

        Button button = buttonObject.GetComponent<Button>();
        button.targetGraphic = image;

        TMP_Text text = CreateText($"{objectName}Text", label, Vector2.zero, Vector2.zero, 31.03f, buttonObject.transform, true);
        Button tryAgainButton = FindSceneComponentByObjectName<Button>("TryAgainButton");
        TMP_Text tryAgainText = tryAgainButton != null ? tryAgainButton.GetComponentInChildren<TMP_Text>(true) : null;
        CopyTextStyle(tryAgainText, text);
        text.text = label;
        return button;
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

    private void CopyButtonStyle(Button source, Button target)
    {
        if (source == null || target == null)
        {
            return;
        }

        RectTransform sourceRect = source.GetComponent<RectTransform>();
        RectTransform targetRect = target.GetComponent<RectTransform>();
        if (sourceRect != null && targetRect != null)
        {
            targetRect.sizeDelta = sourceRect.sizeDelta;
        }

        target.transition = source.transition;
        target.colors = source.colors;
        target.spriteState = source.spriteState;
        target.animationTriggers = source.animationTriggers;

        Image sourceImage = source.GetComponent<Image>();
        Image targetImage = target.GetComponent<Image>();
        if (sourceImage != null && targetImage != null)
        {
            targetImage.color = sourceImage.color;
            targetImage.sprite = sourceImage.sprite;
            targetImage.type = sourceImage.type;
            targetImage.preserveAspect = sourceImage.preserveAspect;
        }
    }

    private void CopyTextStyle(TMP_Text source, TMP_Text target)
    {
        if (source == null || target == null)
        {
            return;
        }

        target.font = source.font;
        target.fontSharedMaterial = source.fontSharedMaterial;
        target.fontSize = source.fontSize;
        target.fontStyle = source.fontStyle;
        target.color = source.color;
        target.alignment = source.alignment;
        target.enableAutoSizing = source.enableAutoSizing;
        target.fontSizeMin = source.fontSizeMin;
        target.fontSizeMax = source.fontSizeMax;
        target.margin = source.margin;
        target.textWrappingMode = source.textWrappingMode;
        target.overflowMode = source.overflowMode;
        target.characterSpacing = source.characterSpacing;
        target.wordSpacing = source.wordSpacing;
        target.lineSpacing = source.lineSpacing;
        target.paragraphSpacing = source.paragraphSpacing;
        target.raycastTarget = source.raycastTarget;
    }

    private T FindSceneComponentByObjectName<T>(string objectName) where T : Component
    {
        T[] components = Resources.FindObjectsOfTypeAll<T>();
        for (int i = 0; i < components.Length; i++)
        {
            if (components[i] != null
                && components[i].gameObject.name == objectName
                && components[i].gameObject.scene.IsValid())
            {
                return components[i];
            }
        }

        return null;
    }
}
