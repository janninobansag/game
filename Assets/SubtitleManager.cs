using System.Collections;
using UnityEngine;

public class SubtitleManager : MonoBehaviour
{
    public static SubtitleManager Instance;

    [Header("Subtitle Settings")]
    public float defaultDisplayTime = 4f;
    public float fadeSpeed = 2f;
    public float characterDelay = 0.03f; // typewriter effect

    [Header("Positioning")]
    public float verticalOffset = 100f; // Pixels from bottom
    public float horizontalOffset = 0f; // Offset from center

    [Header("Appearance")]
    public Color backgroundColor = Color.black;
    [Range(0f, 1f)]
    public float backgroundOpacity = 0.55f;
    public Color textColor = Color.white;

    private string currentText = "";
    private string displayedText = "";
    private float subtitleAlpha = 0f;
    private bool isShowing = false;
    private bool isTyping = false;
    private Coroutine currentCoroutine;
    private Coroutine typeCoroutine;

    void Awake()
    {
        if (Instance == null)
            Instance = this;
        else
            Destroy(gameObject);
    }

    void Update()
    {
        if (isShowing)
            subtitleAlpha = Mathf.Lerp(subtitleAlpha, 1f, Time.deltaTime * fadeSpeed);
        else
            subtitleAlpha = Mathf.Lerp(subtitleAlpha, 0f, Time.deltaTime * fadeSpeed);
    }

    public void ShowSubtitle(string text, float duration = -1f)
    {
        if (duration < 0f) duration = defaultDisplayTime;

        if (currentCoroutine != null)
            StopCoroutine(currentCoroutine);
        if (typeCoroutine != null)
            StopCoroutine(typeCoroutine);

        currentText = text;
        displayedText = "";
        currentCoroutine = StartCoroutine(SubtitleRoutine(text, duration));
    }

    public void ShowSubtitleImmediate(string text, float duration = -1f)
    {
        if (duration < 0f) duration = defaultDisplayTime;

        if (currentCoroutine != null)
            StopCoroutine(currentCoroutine);
        if (typeCoroutine != null)
            StopCoroutine(typeCoroutine);

        currentText = text;
        displayedText = text;
        currentCoroutine = StartCoroutine(SubtitleRoutine(text, duration));
    }

    IEnumerator SubtitleRoutine(string text, float duration)
    {
        isShowing = true;

        // Typewriter effect
        typeCoroutine = StartCoroutine(TypeText(text));
        yield return typeCoroutine;

        // Hold
        yield return new WaitForSeconds(duration);

        // Fade out
        isShowing = false;
        displayedText = "";
    }

    IEnumerator TypeText(string text)
    {
        isTyping = true;
        displayedText = "";

        foreach (char c in text)
        {
            displayedText += c;
            yield return new WaitForSeconds(characterDelay);
        }

        isTyping = false;
    }

    public void HideSubtitle()
    {
        if (currentCoroutine != null)
            StopCoroutine(currentCoroutine);
        if (typeCoroutine != null)
            StopCoroutine(typeCoroutine);

        isShowing = false;
        displayedText = "";
    }

    void OnGUI()
    {
        if (subtitleAlpha <= 0.01f) return;

        float sw = Screen.width;
        float sh = Screen.height;
        float cx = sw / 2f;

        // Subtitle background
        float bgW = 600f;
        float bgH = 44f;
        float bgX = cx - bgW / 2f + horizontalOffset;
        float bgY = sh - verticalOffset;

        GUI.color = new Color(backgroundColor.r, backgroundColor.g, backgroundColor.b, backgroundOpacity * subtitleAlpha);
        GUI.DrawTexture(new Rect(bgX - 10, bgY - 6, bgW + 20, bgH),
            Texture2D.whiteTexture);

        // Left accent line
        GUI.DrawTexture(new Rect(bgX - 10, bgY - 6, 3f, bgH),
            Texture2D.whiteTexture);

        GUI.color = Color.white;

        // Shadow
        GUIStyle shadow = new GUIStyle();
        shadow.fontSize = 17;
        shadow.fontStyle = FontStyle.Italic;
        shadow.alignment = TextAnchor.MiddleCenter;
        shadow.wordWrap = true;
        shadow.normal.textColor = new Color(0f, 0f, 0f, subtitleAlpha * 0.7f);
        GUI.Label(new Rect(bgX + 1, bgY + 1, bgW, bgH),
            displayedText, shadow);

        // Main text
        GUIStyle style = new GUIStyle();
        style.fontSize = 17;
        style.fontStyle = FontStyle.Italic;
        style.alignment = TextAnchor.MiddleCenter;
        style.wordWrap = true;
        style.normal.textColor = new Color(textColor.r, textColor.g, textColor.b, subtitleAlpha);
        GUI.Label(new Rect(bgX, bgY, bgW, bgH),
            displayedText, style);
    }
}