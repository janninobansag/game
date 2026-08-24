using System.Collections;
using UnityEngine;

public class IntroNarration : MonoBehaviour
{
    [Header("Settings")]
    public float textFadeSpeed = 1.5f;
    public float textHoldTime = 2.5f;
    public float blackFadeOutSpeed = 0.8f;
    public bool disablePlayerDuringIntro = true;

    // Narration lines
    private string[] lines = new string[]
    {
        "in 1969...",
        "Three people disappeared\nin this forest...",
        "The last place they were seen...",
        "was near an abandoned church."
    };

    private int currentLine = 0;
    private float textAlpha = 0f;
    private float blackAlpha = 1f;
    private bool introDone = false;

    // State machine
    private enum IntroState
    {
        FadeInText,
        HoldText,
        FadeOutText,
        NextLine,
        FadeOutBlack,
        Done
    }

    private IntroState state = IntroState.FadeInText;
    private float stateTimer = 0f;

    // Player lock
    private MonoBehaviour playerMovement;
    private MonoBehaviour playerLook;

    void Start()
    {
        // ── SKIP INTRO IF LOADING SAVED GAME ──
        if (PlayerPrefs.GetInt("SkipIntro", 0) == 1)
        {
            PlayerPrefs.SetInt("SkipIntro", 0);
            PlayerPrefs.Save();

            // Enable player immediately
            playerMovement = FindObjectOfType<PlayerController>();
            if (playerMovement != null)
                playerMovement.enabled = true;

            // Mark intro as done
            introDone = true;
            blackAlpha = 0f;
            textAlpha = 0f;

            // Unlock cursor
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;

            return;
        }

        // ── NORMAL INTRO FLOW ──
        // Lock player during intro
        if (disablePlayerDuringIntro)
        {
            playerMovement = FindObjectOfType<PlayerController>();
            if (playerMovement != null)
                playerMovement.enabled = false;

            // Lock cursor during intro
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }

        StartCoroutine(RunIntro());
    }

    IEnumerator RunIntro()
    {
        // Show each line
        for (int i = 0; i < lines.Length; i++)
        {
            currentLine = i;

            // Fade in text
            yield return StartCoroutine(FadeText(0f, 1f, textFadeSpeed));

            // Hold
            yield return new WaitForSeconds(textHoldTime);

            // Fade out text
            yield return StartCoroutine(FadeText(1f, 0f, textFadeSpeed));

            // Small gap between lines
            yield return new WaitForSeconds(0.3f);
        }

        // Fade out black screen
        yield return StartCoroutine(FadeBlack(1f, 0f, blackFadeOutSpeed));

        // Enable player
        introDone = true;
        if (playerMovement != null)
            playerMovement.enabled = true;
    }

    IEnumerator FadeText(float from, float to, float speed)
    {
        float elapsed = 0f;
        float duration = 1f / speed;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            textAlpha = Mathf.Lerp(from, to, elapsed * speed);
            yield return null;
        }

        textAlpha = to;
    }

    IEnumerator FadeBlack(float from, float to, float speed)
    {
        float elapsed = 0f;
        float duration = 1f / speed;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            blackAlpha = Mathf.Lerp(from, to, elapsed * speed);
            yield return null;
        }

        blackAlpha = to;
    }

    void OnGUI()
    {
        if (introDone) return;

        float sw = Screen.width;
        float sh = Screen.height;
        float cx = sw / 2f;
        float cy = sh / 2f;

        // ── BLACK BACKGROUND FIRST ──
        GUI.color = new Color(0f, 0f, 0f, blackAlpha);
        GUI.DrawTexture(new Rect(0, 0, sw, sh), Texture2D.whiteTexture);

        // ── RESET COLOR BEFORE TEXT ──
        GUI.color = Color.white;

        // ── NARRATION TEXT ──
        if (textAlpha > 0.01f && currentLine < lines.Length)
        {
            string line = lines[currentLine];
            bool isYear = currentLine == 0;

            if (isYear)
            {
                GUIStyle yearStyle = new GUIStyle();
                yearStyle.fontSize = 64;
                yearStyle.fontStyle = FontStyle.Bold;
                yearStyle.alignment = TextAnchor.MiddleCenter;
                yearStyle.normal.textColor =
                    new Color(0.9f, 0.85f, 0.75f, textAlpha);

                GUIStyle glowStyle = new GUIStyle();
                glowStyle.fontSize = 64;
                glowStyle.fontStyle = FontStyle.Bold;
                glowStyle.alignment = TextAnchor.MiddleCenter;
                glowStyle.normal.textColor =
                    new Color(0.8f, 0.7f, 0.5f, textAlpha * 0.3f);

                for (int g = 1; g <= 3; g++)
                {
                    GUI.Label(new Rect(cx - 202 - g, cy - 42 - g,
                        404 + g * 2, 84 + g * 2), line, glowStyle);
                }

                GUI.Label(new Rect(cx - 200, cy - 40, 400, 80), line, yearStyle);

                GUI.color = new Color(0.7f, 0.6f, 0.4f, textAlpha * 0.5f);
                GUI.DrawTexture(new Rect(cx - 130, cy + 30, 80, 1f),
                    Texture2D.whiteTexture);
                GUI.DrawTexture(new Rect(cx + 50, cy + 30, 80, 1f),
                    Texture2D.whiteTexture);
                GUI.color = Color.white;
            }
            else
            {
                GUIStyle textStyle = new GUIStyle();
                textStyle.fontSize = 22;
                textStyle.fontStyle = FontStyle.Italic;
                textStyle.alignment = TextAnchor.MiddleCenter;
                textStyle.wordWrap = true;
                textStyle.normal.textColor =
                    new Color(0.85f, 0.82f, 0.78f, textAlpha);

                GUIStyle shadowStyle = new GUIStyle();
                shadowStyle.fontSize = 22;
                shadowStyle.fontStyle = FontStyle.Italic;
                shadowStyle.alignment = TextAnchor.MiddleCenter;
                shadowStyle.wordWrap = true;
                shadowStyle.normal.textColor =
                    new Color(0f, 0f, 0f, textAlpha * 0.8f);

                GUI.Label(new Rect(cx - 299, cy - 49, 600, 100),
                    line, shadowStyle);
                GUI.Label(new Rect(cx - 300, cy - 50, 600, 100),
                    line, textStyle);
            }
        }

        GUI.color = Color.white;
    }
}