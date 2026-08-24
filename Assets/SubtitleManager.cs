using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

[ExecuteAlways]
public class SubtitleManager : MonoBehaviour
{
    public static SubtitleManager Instance;

    [Header("Subtitle Settings")]
    public float defaultDisplayTime = 4f;
    public float fadeSpeed = 2f;
    public float characterDelay = 0.03f;

    [Header("Initial UI Position")]
    public float verticalOffset = 100f;
    public float horizontalOffset = 0f;

    [Header("Appearance")]
    public Color backgroundColor = Color.black;
    [Range(0f, 1f)] public float backgroundOpacity = 0.55f;
    public Color textColor = Color.white;

    [Header("Canvas UI")]
    [SerializeField] private GameObject subtitlePanel;
    [SerializeField] private Image subtitleBackground;
    [SerializeField] private Image accentLine;
    [SerializeField] private TextMeshProUGUI shadowText;
    [SerializeField] private TextMeshProUGUI subtitleText;
    [SerializeField] private CanvasGroup subtitleCanvasGroup;

    private string displayedText = "";
    private float subtitleAlpha;
    private bool isShowing;
    private Coroutine currentCoroutine;
    private Coroutine typeCoroutine;

    void Awake()
    {
        if (Instance == null)
            Instance = this;
        else
        {
            Destroy(gameObject);
            return;
        }

        EnsureUi();
    }

    void OnEnable()
    {
        if (!Application.isPlaying)
            EnsureUi();
    }

    void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }

    void Update()
    {
        if (!Application.isPlaying) return;

        EnsureUi();
        subtitleAlpha = isShowing
            ? Mathf.Lerp(subtitleAlpha, 1f, Time.deltaTime * fadeSpeed)
            : Mathf.Lerp(subtitleAlpha, 0f, Time.deltaTime * fadeSpeed);
        RefreshUi();
    }

    public void ShowSubtitle(string text, float duration = -1f)
    {
        if (duration < 0f) duration = defaultDisplayTime;
        StopActiveSubtitles();
        displayedText = "";
        currentCoroutine = StartCoroutine(SubtitleRoutine(text, duration, true));
    }

    public void ShowSubtitleImmediate(string text, float duration = -1f)
    {
        if (duration < 0f) duration = defaultDisplayTime;
        StopActiveSubtitles();
        displayedText = text;
        currentCoroutine = StartCoroutine(SubtitleRoutine(text, duration, false));
    }

    IEnumerator SubtitleRoutine(string text, float duration, bool useTypewriter)
    {
        isShowing = true;

        if (useTypewriter)
        {
            typeCoroutine = StartCoroutine(TypeText(text));
            yield return typeCoroutine;
        }

        yield return new WaitForSeconds(duration);
        isShowing = false;
        displayedText = "";
    }

    IEnumerator TypeText(string text)
    {
        displayedText = "";
        foreach (char character in text)
        {
            displayedText += character;
            yield return new WaitForSeconds(characterDelay);
        }
    }

    public void HideSubtitle()
    {
        StopActiveSubtitles();
        isShowing = false;
        displayedText = "";
    }

    [ContextMenu("Rebuild Subtitle UI in Canvas")]
    public void RebuildSubtitleUiInCanvas()
    {
        if (subtitlePanel != null)
        {
            if (Application.isPlaying) Destroy(subtitlePanel);
            else DestroyImmediate(subtitlePanel);
        }

        subtitlePanel = null;
        subtitleBackground = null;
        accentLine = null;
        shadowText = null;
        subtitleText = null;
        subtitleCanvasGroup = null;
        EnsureUi();
    }

    private void StopActiveSubtitles()
    {
        if (currentCoroutine != null) StopCoroutine(currentCoroutine);
        if (typeCoroutine != null) StopCoroutine(typeCoroutine);
        currentCoroutine = null;
        typeCoroutine = null;
    }

    private void EnsureUi()
    {
        if (subtitlePanel != null)
        {
            if (subtitleCanvasGroup == null)
                subtitleCanvasGroup = subtitlePanel.GetComponent<CanvasGroup>();
            if (subtitleCanvasGroup == null)
                subtitleCanvasGroup = subtitlePanel.AddComponent<CanvasGroup>();
            return;
        }

        Canvas canvas = GetComponent<Canvas>();
        if (canvas == null) canvas = FindObjectOfType<Canvas>();
        if (canvas == null) return;

        subtitlePanel = CreateUiObject("Subtitle UI", canvas.transform);
        subtitleCanvasGroup = subtitlePanel.AddComponent<CanvasGroup>();

        RectTransform panelRect = subtitlePanel.GetComponent<RectTransform>();
        panelRect.anchorMin = new Vector2(0.5f, 0f);
        panelRect.anchorMax = new Vector2(0.5f, 0f);
        panelRect.pivot = new Vector2(0.5f, 0.5f);
        panelRect.anchoredPosition = new Vector2(horizontalOffset, verticalOffset);
        panelRect.sizeDelta = new Vector2(620f, 60f);

        subtitleBackground = subtitlePanel.AddComponent<Image>();
        subtitleBackground.raycastTarget = false;
        subtitleBackground.color = new Color(backgroundColor.r, backgroundColor.g, backgroundColor.b, backgroundOpacity);

        GameObject accentObject = CreateUiObject("Subtitle Accent", subtitlePanel.transform);
        accentLine = accentObject.AddComponent<Image>();
        accentLine.raycastTarget = false;
        accentLine.color = textColor;
        SetRect(accentObject.GetComponent<RectTransform>(), new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(4f, 0f), new Vector2(4f, 52f));

        shadowText = CreateSubtitleText("Subtitle Shadow", subtitlePanel.transform, new Vector2(1f, -1f), new Color(0f, 0f, 0f, 0.7f));
        subtitleText = CreateSubtitleText("Subtitle Text", subtitlePanel.transform, Vector2.zero, textColor);

        if (!Application.isPlaying)
        {
            subtitleText.text = "Subtitle Preview";
            shadowText.text = subtitleText.text;
        }
    }

    private void RefreshUi()
    {
        if (subtitlePanel == null) return;

        bool visible = isShowing || subtitleAlpha > 0.01f;
        if (subtitlePanel.activeSelf != visible)
            subtitlePanel.SetActive(visible);
        if (!visible) return;

        if (subtitleCanvasGroup != null)
            subtitleCanvasGroup.alpha = subtitleAlpha;
        if (shadowText != null) shadowText.text = displayedText;
        if (subtitleText != null) subtitleText.text = displayedText;
    }

    private static GameObject CreateUiObject(string objectName, Transform parent)
    {
        GameObject uiObject = new GameObject(objectName, typeof(RectTransform), typeof(CanvasRenderer));
        uiObject.layer = 5;
        uiObject.transform.SetParent(parent, false);
        return uiObject;
    }

    private static TextMeshProUGUI CreateSubtitleText(string objectName, Transform parent, Vector2 offset, Color color)
    {
        GameObject textObject = CreateUiObject(objectName, parent);
        TextMeshProUGUI text = textObject.AddComponent<TextMeshProUGUI>();
        text.font = TMP_Settings.defaultFontAsset;
        text.fontSize = 17f;
        text.fontStyle = FontStyles.Italic;
        text.alignment = TextAlignmentOptions.Center;
        text.enableWordWrapping = true;
        text.raycastTarget = false;
        text.color = color;
        SetRect(text.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), offset, new Vector2(580f, 52f));
        return text;
    }

    private static void SetRect(RectTransform rect, Vector2 anchorMin, Vector2 anchorMax, Vector2 position, Vector2 size)
    {
        rect.anchorMin = anchorMin;
        rect.anchorMax = anchorMax;
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = position;
        rect.sizeDelta = size;
    }
}