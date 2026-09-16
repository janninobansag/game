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
    [SerializeField] private TMP_FontAsset koreanFont;

    private string displayedText = "";
    private float subtitleAlpha;
    private bool isShowing;
    private Coroutine currentCoroutine;
    private Coroutine typeCoroutine;

    void Awake()
    {
        if (Instance == null) Instance = this;
        else { Destroy(gameObject); return; }
        ConfigureCanvasScaler();
        EnsureUi();
    }

    void OnEnable()
    {
        if (!Application.isPlaying)
        {
            ConfigureCanvasScaler();
            EnsureUi();
        }
    }

    void OnDestroy() { if (Instance == this) Instance = null; }

    void OnRectTransformDimensionsChange() => ApplyResponsiveLayout();

    void Update()
    {
        if (!Application.isPlaying) return;
        EnsureUi();
        subtitleAlpha = isShowing ? Mathf.Lerp(subtitleAlpha, 1f, Time.deltaTime * fadeSpeed) : Mathf.Lerp(subtitleAlpha, 0f, Time.deltaTime * fadeSpeed);
        RefreshUi();
    }

    public void ShowSubtitle(string text, float duration = -1f)
    {
        text = GameplayLocalization.TranslateSubtitle(text);
        ApplyLanguageFont();
        if (duration < 0f) duration = defaultDisplayTime;
        StopActiveSubtitles();
        displayedText = "";
        currentCoroutine = StartCoroutine(SubtitleRoutine(text, duration, true));
    }

    public void ShowSubtitleImmediate(string text, float duration = -1f)
    {
        text = GameplayLocalization.TranslateSubtitle(text);
        ApplyLanguageFont();
        if (duration < 0f) duration = defaultDisplayTime;
        StopActiveSubtitles();
        displayedText = text;
        currentCoroutine = StartCoroutine(SubtitleRoutine(text, duration, false));
    }

    IEnumerator SubtitleRoutine(string text, float duration, bool useTypewriter)
    {
        isShowing = true;
        if (useTypewriter) { typeCoroutine = StartCoroutine(TypeText(text)); yield return typeCoroutine; }
        yield return new WaitForSeconds(duration);
        isShowing = false;
        displayedText = "";
    }

    IEnumerator TypeText(string text)
    {
        displayedText = "";
        foreach (char character in text) { displayedText += character; yield return new WaitForSeconds(characterDelay); }
    }

    public void HideSubtitle() { StopActiveSubtitles(); isShowing = false; displayedText = ""; }

    [ContextMenu("Rebuild Subtitle UI in Canvas")]
    public void RebuildSubtitleUiInCanvas()
    {
        if (subtitlePanel != null) { if (Application.isPlaying) Destroy(subtitlePanel); else DestroyImmediate(subtitlePanel); }
        subtitlePanel = null; subtitleBackground = null; accentLine = null; shadowText = null; subtitleText = null; subtitleCanvasGroup = null;
        EnsureUi();
    }

    private void StopActiveSubtitles()
    {
        if (currentCoroutine != null) StopCoroutine(currentCoroutine);
        if (typeCoroutine != null) StopCoroutine(typeCoroutine);
        currentCoroutine = null; typeCoroutine = null;
    }

    private void EnsureUi()
    {
        if (subtitlePanel != null)
        {
            if (subtitleCanvasGroup == null) subtitleCanvasGroup = subtitlePanel.GetComponent<CanvasGroup>();
            if (subtitleCanvasGroup == null) subtitleCanvasGroup = subtitlePanel.AddComponent<CanvasGroup>();
            ApplyResponsiveLayout();
            return;
        }

        Canvas canvas = GetComponent<Canvas>();
        if (canvas == null) canvas = FindObjectOfType<Canvas>();
        if (canvas == null) return;
        ConfigureCanvasScaler();

        subtitlePanel = CreateUiObject("Subtitle UI", canvas.transform);
        subtitleCanvasGroup = subtitlePanel.AddComponent<CanvasGroup>();
        RectTransform panelRect = subtitlePanel.GetComponent<RectTransform>();
        panelRect.anchorMin = new Vector2(.5f, 0f);
        panelRect.anchorMax = new Vector2(.5f, 0f);
        panelRect.pivot = new Vector2(.5f, .5f);
        panelRect.anchoredPosition = new Vector2(horizontalOffset, verticalOffset);
        panelRect.sizeDelta = new Vector2(620f, 68f);

        subtitleBackground = subtitlePanel.AddComponent<Image>();
        subtitleBackground.raycastTarget = false;
        subtitleBackground.color = new Color(backgroundColor.r, backgroundColor.g, backgroundColor.b, backgroundOpacity);
        GameObject accentObject = CreateUiObject("Subtitle Accent", subtitlePanel.transform);
        accentLine = accentObject.AddComponent<Image>();
        accentLine.raycastTarget = false;
        accentLine.color = textColor;
        SetRect(accentObject.GetComponent<RectTransform>(), new Vector2(0f, .5f), new Vector2(0f, .5f), new Vector2(4f, 0f), new Vector2(4f, 58f));
        shadowText = CreateSubtitleText("Subtitle Shadow", subtitlePanel.transform, new Vector2(1f, -1f), new Color(0f, 0f, 0f, .7f));
        subtitleText = CreateSubtitleText("Subtitle Text", subtitlePanel.transform, Vector2.zero, textColor);
        if (!Application.isPlaying) { subtitleText.text = "Subtitle Preview"; shadowText.text = subtitleText.text; }
        ApplyResponsiveLayout();
    }

    private void ConfigureCanvasScaler()
    {
        Canvas canvas = GetComponent<Canvas>();
        if (canvas == null) canvas = FindObjectOfType<Canvas>();
        if (canvas == null) return;
        CanvasScaler scaler = canvas.GetComponent<CanvasScaler>();
        if (scaler == null) scaler = canvas.gameObject.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        scaler.matchWidthOrHeight = .5f;
    }

    private void ApplyResponsiveLayout()
    {
        if (subtitlePanel == null) return;
        Canvas canvas = GetComponent<Canvas>();
        if (canvas == null) canvas = FindObjectOfType<Canvas>();
        CanvasScaler scaler = canvas != null ? canvas.GetComponent<CanvasScaler>() : null;
        float scale = scaler != null && scaler.scaleFactor > 0f ? scaler.scaleFactor : 1f;
        float width = canvas != null && canvas.pixelRect.width > 0f ? Mathf.Clamp(canvas.pixelRect.width / scale * .72f, 360f, 840f) : 620f;
        RectTransform panel = subtitlePanel.GetComponent<RectTransform>();
        panel.sizeDelta = new Vector2(width, 68f);
        if (subtitleText != null) subtitleText.rectTransform.sizeDelta = new Vector2(width - 44f, 58f);
        if (shadowText != null) shadowText.rectTransform.sizeDelta = new Vector2(width - 44f, 58f);
        if (accentLine != null) accentLine.rectTransform.sizeDelta = new Vector2(4f, 58f);
    }

    private void ApplyLanguageFont()
    {
        TMP_FontAsset font = GameplayLocalization.IsKorean && koreanFont != null ? koreanFont : TMP_Settings.defaultFontAsset;
        if (subtitleText != null) subtitleText.font = font;
        if (shadowText != null) shadowText.font = font;
    }

    private void RefreshUi()
    {
        if (subtitlePanel == null) return;
        bool visible = isShowing || subtitleAlpha > .01f;
        if (subtitlePanel.activeSelf != visible) subtitlePanel.SetActive(visible);
        if (!visible) return;
        if (subtitleCanvasGroup != null) subtitleCanvasGroup.alpha = subtitleAlpha;
        if (shadowText != null) shadowText.text = displayedText;
        if (subtitleText != null) subtitleText.text = displayedText;
    }

    private static GameObject CreateUiObject(string name, Transform parent)
    {
        GameObject item = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer));
        item.layer = 5;
        item.transform.SetParent(parent, false);
        return item;
    }

    private static TextMeshProUGUI CreateSubtitleText(string name, Transform parent, Vector2 offset, Color color)
    {
        GameObject item = CreateUiObject(name, parent);
        TextMeshProUGUI text = item.AddComponent<TextMeshProUGUI>();
        text.font = TMP_Settings.defaultFontAsset;
        text.fontSize = 19f;
        text.enableAutoSizing = true;
        text.fontSizeMin = 14f;
        text.fontSizeMax = 26f;
        text.fontStyle = FontStyles.Italic;
        text.alignment = TextAlignmentOptions.Center;
        text.enableWordWrapping = true;
        text.raycastTarget = false;
        text.color = color;
        SetRect(text.rectTransform, new Vector2(.5f, .5f), new Vector2(.5f, .5f), offset, new Vector2(580f, 58f));
        return text;
    }

    private static void SetRect(RectTransform rect, Vector2 min, Vector2 max, Vector2 position, Vector2 size)
    {
        rect.anchorMin = min;
        rect.anchorMax = max;
        rect.pivot = new Vector2(.5f, .5f);
        rect.anchoredPosition = position;
        rect.sizeDelta = size;
    }
}