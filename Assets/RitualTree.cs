using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

public class RitualTree : MonoBehaviour
{
    [Header("Ritual Settings")]
    public float ritualDuration = 15f;
    public float interactRange = 2f;
    public KeyCode ritualKey = KeyCode.E;

    [Header("References")]
    public RitualManager ritualManager;
    public GameObject mutant;

    [Header("Effects")]
    public AudioClip ritualLoopSound;
    public AudioClip ritualCompleteSound;
    public Light[] candleLights;

    [Header("Objective")]
    public ObjectiveTrigger objectiveTrigger;

    [Header("Scene Transition")]
    public string outroSceneName = "outro";
    public float fadeDuration = 2f;

    [Header("Progression Reward")]
    [Tooltip("Progression points awarded once when the ritual is successfully sealed. With a 100-point total, 5 points equals 5%.")]
    [Range(0, 100)] public int completionProgressPoints = 5;

    private float ritualProgress = 0f;
    private bool isRitualing = false;
    private bool ritualComplete = false;
    private bool showPrompt = false;
    private bool canInteract = true;
    private Camera playerCamera;
    private AudioSource audioSource;

    private float pulseTimer = 0f;
    private float vignetteStrength = 0f;

    private float fadeAlpha = 0f;
    private bool isLoading = false;

    void Start()
    {
        playerCamera = Camera.main;
        audioSource = gameObject.AddComponent<AudioSource>();
        audioSource.spatialBlend = 0f;
        audioSource.loop = true;
        audioSource.playOnAwake = false;

        if (ritualLoopSound != null)
            audioSource.clip = ritualLoopSound;
    }

    void Update()
    {
        if (ritualComplete) return;
        if (!canInteract) return;

        if (ritualManager != null && !ritualManager.IsRitualComplete())
        {
            showPrompt = false;
            return;
        }

        showPrompt = false;
        isRitualing = false;

        Ray ray = playerCamera.ScreenPointToRay(
            new Vector3(Screen.width / 2, Screen.height / 2));
        RaycastHit hit;

        if (Physics.Raycast(ray, out hit, interactRange))
        {
            if (hit.collider.gameObject == gameObject ||
                hit.collider.transform.IsChildOf(transform))
            {
                showPrompt = true;

                if (Input.GetKey(ritualKey))
                {
                    isRitualing = true;
                    ritualProgress += Time.deltaTime;

                    if (!audioSource.isPlaying && ritualLoopSound != null)
                        audioSource.Play();

                    vignetteStrength = Mathf.Lerp(
                        vignetteStrength, 0.6f,
                        Time.deltaTime * 3f);

                    pulseTimer += Time.deltaTime * 3f;

                    if (ritualProgress >= ritualDuration)
                    {
                        ritualProgress = ritualDuration;
                        StartCoroutine(CompleteRitual());
                    }
                }
                else
                {
                    StopRitual();
                }
            }
            else
            {
                StopRitual();
            }
        }
        else
        {
            StopRitual();
        }
    }

    void StopRitual()
    {
        isRitualing = false;
        if (audioSource.isPlaying)
            audioSource.Stop();

        vignetteStrength = Mathf.Lerp(
            vignetteStrength, 0f, Time.deltaTime * 5f);
    }

    IEnumerator CompleteRitual()
    {
        ritualComplete = true;
        isRitualing = false;

        audioSource.Stop();

        if (ritualCompleteSound != null)
            audioSource.PlayOneShot(ritualCompleteSound);

        vignetteStrength = 1f;

        if (mutant == null)
            mutant = GameObject.FindGameObjectWithTag("Mutant");

        if (mutant != null)
        {
            MutantAI mutantAI = mutant.GetComponent<MutantAI>();
            if (mutantAI != null) mutantAI.enabled = false;

            UnityEngine.AI.NavMeshAgent agent = mutant.GetComponent<UnityEngine.AI.NavMeshAgent>();
            if (agent != null) agent.enabled = false;
        }

        if (mutant != null)
            StartCoroutine(FadeMutant(mutant));

        if (candleLights != null && candleLights.Length > 0)
            StartCoroutine(FadeLights());

        if (objectiveTrigger != null)
            objectiveTrigger.TriggerObjective();

        // Award the completion reward once. ritualComplete is set before this
        // coroutine runs, so the player cannot earn it again by holding E.
        if (ProgressionSystem.Instance != null && completionProgressPoints > 0)
        {
            ProgressionSystem.Instance.AddProgress(completionProgressPoints);

            // The load panel reads progression from the SQLite save file, so
            // persist the reward before leaving for the outro/menu.
            if (SaveSystem.Instance != null)
                SaveSystem.Instance.SaveGame();
        }

        // ── FADE TO BLACK AND LOAD OUTRO ──
        yield return StartCoroutine(FadeToBlackAndLoadOutro());
    }

    IEnumerator FadeToBlackAndLoadOutro()
    {
        isLoading = true;

        // Fade to black
        float elapsed = 0f;
        while (elapsed < fadeDuration)
        {
            elapsed += Time.deltaTime;
            fadeAlpha = Mathf.Clamp01(elapsed / fadeDuration);
            yield return null;
        }
        fadeAlpha = 1f;

        yield return new WaitForSeconds(0.5f);

        // Unlock cursor for outro scene
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        Time.timeScale = 1f;

        // Load the outro scene
        AsyncOperation asyncLoad = SceneManager.LoadSceneAsync(outroSceneName);
        
        while (!asyncLoad.isDone)
        {
            yield return null;
        }
    }

    IEnumerator FadeMutant(GameObject m)
    {
        if (m == null) yield break;

        Renderer[] renderers = m.GetComponentsInChildren<Renderer>();
        float elapsed = 0f;

        while (elapsed < 2f)
        {
            elapsed += Time.deltaTime;
            float alpha = 1f - (elapsed / 2f);

            foreach (Renderer r in renderers)
            {
                foreach (Material mat in r.materials)
                {
                    if (mat.HasProperty("_Color"))
                    {
                        Color c = mat.color;
                        c.a = alpha;
                        mat.color = c;
                    }
                }
            }
            yield return null;
        }

        Destroy(m);
    }

    IEnumerator FadeLights()
    {
        float elapsed = 0f;
        float[] originalIntensities = new float[candleLights.Length];

        for (int i = 0; i < candleLights.Length; i++)
            if (candleLights[i] != null)
                originalIntensities[i] = candleLights[i].intensity;

        while (elapsed < 2f)
        {
            elapsed += Time.deltaTime;
            for (int i = 0; i < candleLights.Length; i++)
                if (candleLights[i] != null)
                    candleLights[i].intensity = Mathf.Lerp(
                        originalIntensities[i], 0f, elapsed / 2f);
            yield return null;
        }
    }

    public void ResetRitual()
    {
        ritualProgress = 0f;
        ritualComplete = false;
        isRitualing = false;
        vignetteStrength = 0f;
        pulseTimer = 0f;
        canInteract = true;
        isLoading = false;

        if (audioSource != null)
            audioSource.Stop();
    }

    public void DisableInteraction()
    {
        canInteract = false;
        showPrompt = false;
        isRitualing = false;
    }

    void OnGUI()
    {
        float sw = Screen.width;
        float sh = Screen.height;
        float cx = sw / 2f;
        float cy = sh / 2f;

        // ── PROMPT ──
        if (showPrompt && !ritualComplete)
        {
            string msg = ritualProgress > 0f
                ? "Hold E to continue the ritual..."
                : "Hold E to perform the ritual";

            GUIStyle style = new GUIStyle();
            style.fontSize = 22;
            style.alignment = TextAnchor.MiddleCenter;
            style.normal.textColor = Color.white;

            GUIStyle shadow = new GUIStyle();
            shadow.fontSize = 22;
            shadow.alignment = TextAnchor.MiddleCenter;
            shadow.normal.textColor = Color.black;

            GUI.Label(new Rect(cx - 199, cy + 51, 400, 40), msg, shadow);
            GUI.Label(new Rect(cx - 200, cy + 50, 400, 40), msg, style);
        }

        // ── RITUAL PROGRESS BAR ──
        if (ritualProgress > 0f && !ritualComplete)
        {
            float fill = ritualProgress / ritualDuration;
            float barW = 300f;
            float barH = 18f;
            float bx = cx - barW / 2f;
            float by = cy + 85f;

            if (isRitualing)
            {
                GUI.color = new Color(0f, 0f, 0f, 0.7f);
                GUI.DrawTexture(new Rect(bx - 1, by - 1, barW + 2, barH + 2), Texture2D.whiteTexture);

                Color barColor = Color.Lerp(
                    new Color(0.4f, 0.1f, 0.8f),
                    new Color(1f, 0.8f, 0.2f), fill);
                GUI.color = barColor;
                GUI.DrawTexture(new Rect(bx, by, barW * fill, barH), Texture2D.whiteTexture);

                float pulse = (Mathf.Sin(pulseTimer * 4f) + 1f) / 2f;
                GUI.color = new Color(1f, 1f, 1f, 0.15f * pulse);
                GUI.DrawTexture(new Rect(bx, by, barW * fill, barH), Texture2D.whiteTexture);

                GUIStyle timerStyle = new GUIStyle();
                timerStyle.fontSize = 13;
                timerStyle.alignment = TextAnchor.MiddleCenter;
                timerStyle.normal.textColor = Color.white;

                float remaining = ritualDuration - ritualProgress;
                GUI.color = Color.white;
                GUI.Label(new Rect(bx, by + 22f, barW, 20f),
                    $"Sealing Munduan... {remaining:F1}s remaining",
                    timerStyle);
            }
        }

        // ── RITUAL VIGNETTE ──
        if (vignetteStrength > 0.01f)
        {
            float pulse = isRitualing
                ? (Mathf.Sin(pulseTimer) + 1f) / 2f * 0.15f
                : 0f;

            GUI.color = new Color(0.08f, 0f, 0.15f, vignetteStrength * 0.65f + pulse);
            GUI.DrawTexture(new Rect(0, 0, sw, sh), Texture2D.whiteTexture);

            int steps = 15;
            for (int i = 0; i < steps; i++)
            {
                float t = 1f - (i / (float)steps);
                GUI.color = new Color(0.2f, 0f, 0.35f, t * t * vignetteStrength * 0.7f);
                GUI.DrawTexture(new Rect(0, 0, sw * (i / (float)steps) * 0.3f, sh), Texture2D.whiteTexture);
                GUI.DrawTexture(new Rect(sw - sw * (i / (float)steps) * 0.3f, 0, sw * (i / (float)steps) * 0.3f, sh), Texture2D.whiteTexture);
            }
        }

        // ── FADE TO BLACK ──
        if (fadeAlpha > 0f)
        {
            GUI.color = new Color(0f, 0f, 0f, fadeAlpha);
            GUI.DrawTexture(new Rect(0, 0, sw, sh), Texture2D.whiteTexture);
        }

        GUI.color = Color.white;
    }
}
