using UnityEngine;
using System.Collections;

public class ObjectiveManager : MonoBehaviour
{
    public static ObjectiveManager Instance;

    private string currentObjective = "";
    private string previousObjective = "";
    private bool showObjective = false;

    // UI animation
    private float objectiveAlpha = 0f;
    private float newObjectiveFlash = 0f;
    private bool isNew = false;

    void Awake()
    {
        if (Instance == null)
            Instance = this;
        else
            Destroy(gameObject);
    }

    void Update()
    {
        // Fade in/out
        float targetAlpha = showObjective ? 1f : 0f;
        objectiveAlpha = Mathf.Lerp(objectiveAlpha,
            targetAlpha, Time.deltaTime * 3f);

        // Flash when new objective
        if (newObjectiveFlash > 0f)
            newObjectiveFlash -= Time.deltaTime;
    }

    public void SetObjective(string objective)
    {
        previousObjective = currentObjective;
        currentObjective = objective;
        showObjective = true;
        isNew = true;
        newObjectiveFlash = 3f; // flash for 3 seconds

        StopAllCoroutines();
        StartCoroutine(ShowThenFade());

    }

    public void ClearObjective()
    {
        showObjective = false;
        currentObjective = "";
    }

    IEnumerator ShowThenFade()
    {
        // Keep visible for 5 seconds then fade
        yield return new WaitForSeconds(5f);
        isNew = false;

        // Keep showing but less prominent
        yield return new WaitForSeconds(10f);

        // Fade out completely after 15 seconds
        showObjective = false;
    }

    public void ShowCurrentObjective()
    {
        showObjective = true;
        StopAllCoroutines();
        StartCoroutine(ShowThenFade());
    }

    void OnGUI()
    {
        if (objectiveAlpha <= 0.01f) return;

        float sw = Screen.width;
        float sh = Screen.height;

        // ── NEW OBJECTIVE BANNER ──
        if (newObjectiveFlash > 0f)
        {
            float flashAlpha = Mathf.Min(newObjectiveFlash, 1f) * objectiveAlpha;

            // Banner BG
            float bannerH = 55f;
            float bannerY = sh * 0.25f;

            GUI.color = new Color(0f, 0f, 0f, 0.75f * flashAlpha);
            GUI.DrawTexture(new Rect(0, bannerY, sw, bannerH),
                Texture2D.whiteTexture);

            // Left red accent
            GUI.color = new Color(0.8f, 0.1f, 0.1f, flashAlpha);
            GUI.DrawTexture(new Rect(0, bannerY, 4f, bannerH),
                Texture2D.whiteTexture);

            // Top/bottom lines
            GUI.color = new Color(0.6f, 0.1f, 0.1f, 0.5f * flashAlpha);
            GUI.DrawTexture(new Rect(0, bannerY, sw, 1.5f),
                Texture2D.whiteTexture);
            GUI.DrawTexture(new Rect(0, bannerY + bannerH - 1.5f, sw, 1.5f),
                Texture2D.whiteTexture);

            // NEW OBJECTIVE label
            GUIStyle newStyle = new GUIStyle();
            newStyle.fontSize = 12;
            newStyle.fontStyle = FontStyle.Bold;
            newStyle.normal.textColor =
                new Color(0.8f, 0.2f, 0.2f, flashAlpha);
            newStyle.alignment = TextAnchor.MiddleLeft;
            GUI.color = Color.white;
            GUI.Label(new Rect(20f, bannerY + 6f, 200f, 18f),
                GameplayLocalization.TranslateObjective("▶ NEW OBJECTIVE"), newStyle);

            // Objective text
            GUIStyle objStyle = new GUIStyle();
            objStyle.fontSize = 18;
            objStyle.fontStyle = FontStyle.Bold;
            objStyle.normal.textColor =
                new Color(0.92f, 0.88f, 0.85f, flashAlpha);
            objStyle.alignment = TextAnchor.MiddleLeft;
            GUI.Label(new Rect(20f, bannerY + 22f, sw - 40f, 26f),
                GameplayLocalization.TranslateObjective(currentObjective), objStyle);
        }

        // ── PERSISTENT OBJECTIVE (top left corner) ──
        if (!string.IsNullOrEmpty(currentObjective))
        {
            float cornerAlpha = isNew
                ? objectiveAlpha * 0.5f
                : objectiveAlpha;

            float px = 18f;
            float py = 18f;
            float pw = 280f;
            float ph = 52f;

            // BG
            GUI.color = new Color(0f, 0f, 0f, 0.55f * cornerAlpha);
            GUI.DrawTexture(new Rect(px, py, pw, ph),
                Texture2D.whiteTexture);

            // Left accent
            GUI.color = new Color(0.7f, 0.1f, 0.1f, cornerAlpha);
            GUI.DrawTexture(new Rect(px, py, 3f, ph),
                Texture2D.whiteTexture);

            // Border
            GUI.color = new Color(0.4f, 0.1f, 0.1f, 0.4f * cornerAlpha);
            GUI.DrawTexture(new Rect(px, py, pw, 1f),
                Texture2D.whiteTexture);
            GUI.DrawTexture(new Rect(px, py + ph - 1f, pw, 1f),
                Texture2D.whiteTexture);

            GUI.color = Color.white;

            // Objective label
            GUIStyle labelStyle = new GUIStyle();
            labelStyle.fontSize = 11;
            labelStyle.fontStyle = FontStyle.Bold;
            labelStyle.normal.textColor =
                new Color(0.7f, 0.2f, 0.2f, cornerAlpha);
            GUI.Label(new Rect(px + 8f, py + 5f, pw - 12f, 16f),
                GameplayLocalization.TranslateObjective("OBJECTIVE"), labelStyle);

            // Objective text
            GUIStyle textStyle = new GUIStyle();
            textStyle.fontSize = 13;
            textStyle.wordWrap = true;
            textStyle.normal.textColor =
                new Color(0.88f, 0.85f, 0.82f, cornerAlpha);
            GUI.Label(new Rect(px + 8f, py + 20f, pw - 16f, 28f),
                GameplayLocalization.TranslateObjective(currentObjective), textStyle);
        }

        GUI.color = Color.white;
    }
}