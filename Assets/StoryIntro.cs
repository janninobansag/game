using System.Collections;
using UnityEngine;

public class StoryIntro : MonoBehaviour
{
    [Header("Settings")]
    public float textFadeSpeed = 1.2f;
    public float textHoldTime = 3f;
    public float sectionPauseTime = 0.8f;
    [Tooltip("Kept for older scenes. The intro now uses left click to continue and Enter to skip.")]
    public bool skipOnAnyKey = false;
    [Tooltip("NotoSansKR-Regular font used only for the Korean story intro.")] public Font koreanFont;

    private float blackAlpha = 1f;
    private float textAlpha = 0f;
    private bool introDone = false;
    private bool isSkipping = false;
    private bool advanceRequested = false;

    private string currentText = "";
    private string currentSubText = "";
    private bool isChapterTitle = false;
    private bool isCenterText = false;

    private MonoBehaviour playerController;

    // Intro sections
    private struct IntroSection
    {
        public string title;
        public string[] lines;
        public bool isChapter;
        public float holdTime;
    }

    private IntroSection[] sections;
    private int currentSection = 0;
    private int currentLine = 0;

    // Noise/grain effect
    private float noiseTimer = 0f;
    private float[] noiseX = new float[40];
    private float[] noiseY = new float[40];
    private float[] noiseW = new float[40];
    private float[] noiseH = new float[40];
    private float[] noiseA = new float[40];

    public bool IsIntroActive => !introDone;

    void Start()
    {
        playerController = FindObjectOfType<PlayerController>();
        if (playerController != null)
            playerController.enabled = false;

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        InitSections();
        InitNoise();

        int savedSection;
        int savedLine;
        bool wasCompleted;
        if (SaveSystem.Instance != null &&
            SaveSystem.Instance.TryGetStoryIntroProgress(out savedSection, out savedLine, out wasCompleted))
        {
            if (wasCompleted)
            {
                FinishIntro();
                return;
            }

            currentSection = Mathf.Clamp(savedSection, 0, sections.Length - 1);
            currentLine = Mathf.Clamp(savedLine, 0, sections[currentSection].lines.Length - 1);
        }

        StartCoroutine(RunIntro());
    }
    void InitNoise()
    {
        for (int i = 0; i < noiseX.Length; i++)
            ResetNoise(i);
    }

    void ResetNoise(int i)
    {
        noiseX[i] = Random.Range(0f, Screen.width);
        noiseY[i] = Random.Range(0f, Screen.height);
        noiseW[i] = Random.Range(2f, 80f);
        noiseH[i] = Random.Range(1f, 4f);
        noiseA[i] = Random.Range(0.02f, 0.08f);
    }

    void InitSections()
    {
        sections = new IntroSection[]
        {
            new IntroSection { lines = new[] { "VAREN" }, isChapter = true, holdTime = 3.5f },
            new IntroSection { lines = new[] { "1989.", "Malawak Forest was once a living village.", "Father Mateo guided its people." }, holdTime = 3.5f },
            new IntroSection { lines = new[] { "An ancient shadow named Varen possessed Father Mateo.", "It prepared a ritual to claim his body forever." }, holdTime = 4f },
            new IntroSection { lines = new[] { "During a prayer in the church, Father Mateo begged everyone to stop.", "The ritual broke.", "Varen killed the people gathered inside." }, holdTime = 4.5f },
            new IntroSection { lines = new[] { "Mil and Jude escaped with candles, a Bible, and a cross.", "They purified the sacred items.", "But Varen found them.", "Jude died outside House 3. Mil died inside." }, holdTime = 5f },
            new IntroSection { lines = new[] { "Laica became the White Lady.", "Jude became the Tikbalang.", "Both spirits could not accept their deaths." }, holdTime = 4.5f },
            new IntroSection { lines = new[] { "Now, an adventurer on vacation finds the abandoned village.", "Explore the guard house, the three homes, and the church.", "Return the sacred items. Pray at the Ritual Tree.", "Seal Varen before Malawak Forest claims you." }, holdTime = 5f }
        };

        if (GameplayLocalization.IsLocalized)
        {
            for (int i = 0; i < sections.Length; i++)
            {
                IntroSection section = sections[i];
                for (int line = 0; line < section.lines.Length; line++)
                    section.lines[line] = GameplayLocalization.TranslateSubtitle(section.lines[line]);
                sections[i] = section;
            }
        }
    }
    void Update()
    {
        noiseTimer += Time.deltaTime;
        if (noiseTimer > 0.05f)
        {
            noiseTimer = 0f;
            for (int i = 0; i < noiseX.Length; i++)
                if (Random.value > 0.7f) ResetNoise(i);
        }

        if (introDone || isSkipping || (PauseMenu.Instance != null && PauseMenu.Instance.isPaused))
            return;

        // Left mouse button advances exactly one sentence.
        if (Input.GetMouseButtonDown(0) || Input.GetKeyDown(KeyCode.LeftArrow))
            advanceRequested = true;

        // Enter skips the remaining intro and marks it complete.
        if (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter))
        {
            isSkipping = true;
            StopAllCoroutines();
            StartCoroutine(SkipIntro());
        }
    }
    IEnumerator RunIntro()
    {
        yield return new WaitForSeconds(0.5f);

        for (; currentSection < sections.Length; currentSection++)
        {
            IntroSection section = sections[currentSection];
            int firstLine = currentLine;

            for (int line = firstLine; line < section.lines.Length; line++)
            {
                currentLine = line;
                currentText = section.lines[line];
                currentSubText = "";
                isChapterTitle = section.isChapter;
                isCenterText = section.isChapter;
                textAlpha = 0f;

                SaveCurrentProgress(false);
                yield return StartCoroutine(FadeText(0f, 1f));

                // A click is required before the following sentence is shown.
                advanceRequested = false;
                while (!advanceRequested)
                    yield return null;

                yield return StartCoroutine(FadeText(1f, 0f));
            }

            currentLine = 0;
            SaveCurrentProgress(false);
        }

        SaveCurrentProgress(true);
        yield return StartCoroutine(FadeBlack(1f, 0f));
        FinishIntro();
    }

    private void SaveCurrentProgress(bool isComplete)
    {
        if (SaveSystem.Instance != null)
            SaveSystem.Instance.SaveStoryIntroProgress(currentSection, currentLine, isComplete);
    }
    IEnumerator FadeText(float from, float to, float speed = -1f)
    {
        if (speed < 0) speed = textFadeSpeed;
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

    IEnumerator FadeBlack(float from, float to)
    {
        float elapsed = 0f;
        float duration = 2f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            blackAlpha = Mathf.Lerp(from, to, elapsed / duration);
            yield return null;
        }
        blackAlpha = to;
    }

    IEnumerator SkipIntro()
    {
        SaveCurrentProgress(true);
        textAlpha = 0f;
        yield return StartCoroutine(FadeBlack(blackAlpha, 0f));
        FinishIntro();
    }

    void FinishIntro()
    {
        introDone = true;
        blackAlpha = 0f;
        textAlpha = 0f;
        SaveCurrentProgress(true);

        if (playerController != null)
            playerController.enabled = true;
    }
    void OnGUI()
    {
        // The pause Canvas must appear above the intro overlay.
        if (PauseMenu.Instance != null && PauseMenu.Instance.isPaused) return;
        if (introDone && blackAlpha <= 0.01f) return;

        float sw = Screen.width;
        float sh = Screen.height;
        float cx = sw / 2f;
        float cy = sh / 2f;

        // ── BLACK BACKGROUND ──
        GUI.color = new Color(0f, 0f, 0f, blackAlpha);
        GUI.DrawTexture(new Rect(0, 0, sw, sh), Texture2D.whiteTexture);

        // ── FILM GRAIN / NOISE ──
        if (blackAlpha > 0.3f)
        {
            for (int i = 0; i < noiseX.Length; i++)
            {
                GUI.color = new Color(1f, 1f, 1f, noiseA[i] * blackAlpha);
                GUI.DrawTexture(new Rect(noiseX[i], noiseY[i],
                    noiseW[i], noiseH[i]), Texture2D.whiteTexture);
            }
        }

        // ── VIGNETTE ──
        if (blackAlpha > 0.1f)
        {
            int steps = 20;
            for (int i = 0; i < steps; i++)
            {
                float t = 1f - (i / (float)steps);
                GUI.color = new Color(0f, 0f, 0f, t * t * 0.4f * blackAlpha);
                GUI.DrawTexture(new Rect(0, 0,
                    sw * (i / (float)steps) * 0.2f, sh),
                    Texture2D.whiteTexture);
                GUI.DrawTexture(new Rect(
                    sw - sw * (i / (float)steps) * 0.2f, 0,
                    sw * (i / (float)steps) * 0.2f, sh),
                    Texture2D.whiteTexture);
            }
        }

        GUI.color = Color.white;

        if (textAlpha <= 0.01f) return;

        // ── ALL TEXT IS CENTERED ──
        // Special styling for title/quote text
        bool isLargeTitle = currentText.Trim() == "VAREN" || 
                           currentText.Trim() == "Do not trust the shadows." ||
                           currentText.Trim() == "...and never stop running.";

        if (isLargeTitle)
        {
            // Large title style
            GUIStyle titleStyle = new GUIStyle();
            titleStyle.fontSize = 64;
            if (GameplayLocalization.IsKorean && koreanFont != null) titleStyle.font = koreanFont;
            titleStyle.fontStyle = FontStyle.Bold;
            titleStyle.alignment = TextAnchor.MiddleCenter;
            titleStyle.wordWrap = true;

            // Glow effect
            for (int g = 1; g <= 3; g++)
            {
                titleStyle.normal.textColor =
                    new Color(0.9f, 0.8f, 0.6f, 0.1f * g * textAlpha);
                GUI.Label(new Rect(cx - 202 - g, cy - 42 - g,
                    404 + g * 2, 84 + g * 2),
                    currentText, titleStyle);
            }

            titleStyle.normal.textColor =
                new Color(0.95f, 0.9f, 0.85f, textAlpha);
            GUI.Label(new Rect(cx - 200, cy - 40, 400, 80),
                currentText, titleStyle);

            // Decorative lines
            GUI.color = new Color(0.7f, 0.5f, 0.3f, 0.4f * textAlpha);
            GUI.DrawTexture(new Rect(cx - 100, cy + 38, 60, 1f),
                Texture2D.whiteTexture);
            GUI.DrawTexture(new Rect(cx + 40, cy + 38, 60, 1f),
                Texture2D.whiteTexture);
            GUI.color = Color.white;
        }
        else
        {
            // ── STORY TEXT (centered) ──
            float textX = cx - sw * 0.35f;
            float textW = sw * 0.7f;
            float textY = cy - 120f;

            // Main story text
            GUIStyle textStyle = new GUIStyle();
            textStyle.fontSize = 20;
            if (GameplayLocalization.IsKorean && koreanFont != null) textStyle.font = koreanFont;
            textStyle.fontStyle = FontStyle.Italic;
            textStyle.wordWrap = true;
            textStyle.richText = true;
            textStyle.alignment = TextAnchor.MiddleCenter;
            textStyle.normal.textColor =
                new Color(0.85f, 0.82f, 0.78f, textAlpha);

            GUIStyle shadowStyle = new GUIStyle(textStyle);
            shadowStyle.normal.textColor =
                new Color(0f, 0f, 0f, textAlpha * 0.6f);

            GUI.Label(new Rect(textX + 1, textY + 1, textW, 300f),
                currentText, shadowStyle);
            GUI.Label(new Rect(textX, textY, textW, 300f),
                currentText, textStyle);
        }

        // ── SKIP HINT ──
        if (!isSkipping && blackAlpha > 0.5f)
        {
            GUIStyle skipStyle = new GUIStyle();
            skipStyle.fontSize = 10;
            skipStyle.normal.textColor =
                new Color(0.4f, 0.38f, 0.35f, textAlpha * 0.6f);
            skipStyle.alignment = TextAnchor.MiddleRight;
            GUI.Label(new Rect(sw - 160f, sh - 30f, 140f, 18f),
                "Left Click: Continue | Enter: Skip", skipStyle);
        }

        GUI.color = Color.white;
    }
}