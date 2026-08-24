using System.Collections;
using UnityEngine;

public class StoryIntro : MonoBehaviour
{
    [Header("Settings")]
    public float textFadeSpeed = 1.2f;
    public float textHoldTime = 3f;
    public float sectionPauseTime = 0.8f;
    public bool skipOnAnyKey = true;

    private float blackAlpha = 1f;
    private float textAlpha = 0f;
    private bool introDone = false;
    private bool isSkipping = false;

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

    void Start()
    {
        // Disable player during intro
        playerController = FindObjectOfType<PlayerController>();
        if (playerController != null)
            playerController.enabled = false;

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        InitSections();
        InitNoise();
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
            // Section 1 — Title
            new IntroSection {
                title = "",
                lines = new string[] { "VAREN" },
                isChapter = true,
                holdTime = 3.5f
            },

            // Section 2 — The Forest
            new IntroSection {
                title = "",
                lines = new string[] {
                    "Deep in the heart of the Philippine forest,",
                    "a village once stood.",
                    "",
                    "Now... it is empty."
                },
                isChapter = false,
                holdTime = 3.5f
            },

            // Section 3 — The Disappearance
            new IntroSection {
                title = "",
                lines = new string[] {
                    "People vanished without a trace.",
                    "No bodies. No blood. No struggle.",
                    "",
                    "Just... silence."
                },
                isChapter = false,
                holdTime = 3.5f
            },

            // Section 4 — Varen
            new IntroSection {
                title = "",
                lines = new string[] {
                    "The elders spoke of an ancient spirit.",
                    "They called it...",
                    "",
                    "VAREN."
                },
                isChapter = true,
                holdTime = 4f
            },

            // Section 5 — Varen's Nature
            new IntroSection {
                title = "",
                lines = new string[] {
                    "Varen is not a monster that hunts.",
                    "It is a presence that waits.",
                    "",
                    "It feeds on fear.",
                    "It grows stronger with every whisper of its name.",
                    "It watches from the trees.",
                    "",
                    "And when you feel its gaze...",
                    "it is already too late."
                },
                isChapter = false,
                holdTime = 5f
            },

            // Section 6 — The White Lady
            new IntroSection {
                title = "",
                lines = new string[] {
                    "Some say they saw her.",
                    "A woman in white.",
                    "",
                    "She appears at the edge of the forest.",
                    "Standing still.",
                    "Watching.",
                    "",
                    "They say if you look into her eyes,",
                    "you will see your death."
                },
                isChapter = false,
                holdTime = 5f
            },

            // Section 7 — The Tikbalang
            new IntroSection {
                title = "",
                lines = new string[] {
                    "At night, you can hear the Tikbalang.",
                    "A creature that leads travelers astray.",
                    "Half-man, half-horse.",
                    "",
                    "It plays tricks to confuse and trap you.",
                    "",
                    "If you hear its footsteps behind you...",
                    "do not turn around."
                },
                isChapter = false,
                holdTime = 5f
            },

            // Section 8 — The Whispers
            new IntroSection {
                title = "",
                lines = new string[] {
                    "The forest is alive with whispers.",
                    "",
                    "Voices call your name.",
                    "They sound like your mother.",
                    "Your friends.",
                    "Yourself.",
                    "",
                    "Do not answer."
                },
                isChapter = false,
                holdTime = 4.5f
            },

            // Section 9 — The Ritual
            new IntroSection {
                title = "",
                lines = new string[] {
                    "The elders tried to seal Varen.",
                    "They performed a ritual at the heart of the forest.",
                    "",
                    "It failed."
                },
                isChapter = false,
                holdTime = 3.5f
            },

            // Section 10 — The Curse
            new IntroSection {
                title = "",
                lines = new string[] {
                    "Now the forest is cursed.",
                    "",
                    "Those who enter... never leave.",
                    "",
                    "And Varen is always watching."
                },
                isChapter = false,
                holdTime = 4f
            },

            // Section 11 — The Player
            new IntroSection {
                title = "",
                lines = new string[] {
                    "You are the only one who can uncover the truth.",
                    "",
                    "But be careful.",
                    "",
                    "Varen knows you are here."
                },
                isChapter = false,
                holdTime = 4.5f
            },

            // Section 12 — Final Warning
            new IntroSection {
                title = "",
                lines = new string[] {
                    "Do not trust the shadows.",
                    "Do not answer the voices.",
                    "",
                    "...and never stop running."
                },
                isChapter = true,
                holdTime = 5f
            },
        };
    }

    void Update()
    {
        // Update noise
        noiseTimer += Time.deltaTime;
        if (noiseTimer > 0.05f)
        {
            noiseTimer = 0f;
            for (int i = 0; i < noiseX.Length; i++)
                if (Random.value > 0.7f) ResetNoise(i);
        }

        // Skip on any key
        if (skipOnAnyKey && !isSkipping &&
            (Input.anyKeyDown && !Input.GetKeyDown(KeyCode.Escape)))
        {
            if (!introDone)
            {
                isSkipping = true;
                StopAllCoroutines();
                StartCoroutine(SkipIntro());
            }
        }

        // Force skip with Escape
        if (Input.GetKeyDown(KeyCode.Escape) && !introDone)
        {
            isSkipping = true;
            StopAllCoroutines();
            StartCoroutine(SkipIntro());
        }
    }

    IEnumerator RunIntro()
    {
        // Initial black hold
        yield return new WaitForSeconds(1f);

        foreach (var section in sections)
        {
            // Show chapter title if exists
            if (!string.IsNullOrEmpty(section.title))
            {
                currentText = section.title;
                currentSubText = "";
                isChapterTitle = true;
                isCenterText = false;

                yield return StartCoroutine(FadeText(0f, 1f));
                yield return new WaitForSeconds(1.5f);
                yield return StartCoroutine(FadeText(1f, 0f));
                yield return new WaitForSeconds(0.3f);
            }

            // Show lines one by one or all at once
            if (section.isChapter)
            {
                // Show all lines centered together
                currentText = string.Join("\n", section.lines);
                currentSubText = "";
                isChapterTitle = true;
                isCenterText = true;

                yield return StartCoroutine(FadeText(0f, 1f));
                yield return new WaitForSeconds(section.holdTime);
                yield return StartCoroutine(FadeText(1f, 0f));
            }
            else
            {
                // Show lines one by one
                string accumulated = "";
                foreach (string line in section.lines)
                {
                    if (line == "")
                    {
                        accumulated += "\n";
                        continue;
                    }

                    accumulated += (accumulated.Length > 0 ? "\n" : "") + line;
                    currentText = accumulated;
                    isChapterTitle = false;
                    isCenterText = false;

                    yield return StartCoroutine(FadeText(
                        textAlpha, 1f, textFadeSpeed * 1.5f));
                    yield return new WaitForSeconds(textHoldTime * 0.6f);
                }

                yield return new WaitForSeconds(section.holdTime);
                yield return StartCoroutine(FadeText(1f, 0f));
            }

            yield return new WaitForSeconds(sectionPauseTime);
        }

        // Fade out black screen
        yield return StartCoroutine(FadeBlack(1f, 0f));

        FinishIntro();
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
        textAlpha = 0f;
        yield return StartCoroutine(FadeBlack(blackAlpha, 0f));
        FinishIntro();
    }

    void FinishIntro()
    {
        introDone = true;
        blackAlpha = 0f;
        textAlpha = 0f;

        if (playerController != null)
            playerController.enabled = true;
    }

    void OnGUI()
    {
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
                "Any key to skip", skipStyle);
        }

        GUI.color = Color.white;
    }
}