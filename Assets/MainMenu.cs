using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections.Generic;

public class MainMenu : MonoBehaviour
{
    [Header("Audio")]
    public AudioClip hoverSound;
    [Range(0f, 1f)]
    public float hoverVolume = 1f;
    public AudioClip clickSound;
    public AudioClip openSound;

    private float masterVolume = 100f;

    private bool showAbout = false;
    private bool showSettings = false;
    private bool isLoading = false;
    private AsyncOperation loadingOperation;
    private AudioSource audioSource;

    // ── ANIMATION TIMERS ──
    private float menuSlideOffset = 0f;
    private float settingsSlideOffset = 0f;
    private float aboutSlideOffset = 0f;
    private float titleBreathTimer = 0f;
    private float buttonPressScale = 0f;
    private int pressedButtonIndex = -1;
    private float pressTimer = 0f;

    // VHS Tracking
    private float trackingTimer = 0f;
    private float[] trackingLines = new float[8];
    private float[] trackingAlpha = new float[8];
    private float[] trackingWidth = new float[8];
    private float nextTracking = 0f;
    private bool isTracking = false;
    private float trackingDuration = 0f;

    // Scanlines
    private float scanOffset = 0f;

    // Title flicker
    private float flickerTimer = 0f;
    private float flickerAlpha = 1f;
    private float nextFlicker = 0f;

    // Static noise
    private float staticTimer = 0f;
    private float nextStatic = 0f;
    private float staticAlpha = 0f;
    private float staticDuration = 0f;
    private bool isStatic = false;

    // Tape warp
    private float warpTimer = 0f;

    // Button hover
    private float[] btnHover = new float[4];
    private bool[] mainBtnHovered = new bool[4];
    private bool aboutBackHovered = false;
    private bool settingsBackHovered = false;

    // Intro
    private float animTimer = 0f;
    private float titleAlpha = 0f;
    private float buttonsAlpha = 0f;

    // REC blink
    private float recTimer = 0f;
    private bool recVisible = true;

    // Color bleed
    private float bleedTimer = 0f;

    // Timestamp
    private float timestampTimer = 0f;
    private int fakeHour = 11;
    private int fakeMin = 47;
    private int fakeSec = 0;

    // Settings panel state (to show controls)
    private bool showControlsTab = false;

void Start()
{
    // ── FORCE CURSOR VISIBLE (FIX FOR RETURNING FROM GAME) ──
    Cursor.visible = true;
    Cursor.lockState = CursorLockMode.None;
    
    for (int i = 0; i < trackingLines.Length; i++)
        ResetTrackingLine(i);

    audioSource = GetComponent<AudioSource>();
    if (audioSource == null)
    {
        audioSource = gameObject.AddComponent<AudioSource>();
    }

    // Load saved volume
    masterVolume = PlayerPrefs.GetFloat("MasterVolume", 100f);
    AudioListener.volume = masterVolume / 100f;

    // Initial animation offsets
    menuSlideOffset = 100f;
    settingsSlideOffset = 100f;
    aboutSlideOffset = 100f;
}

    void ResetTrackingLine(int i)
    {
        trackingLines[i] = Random.Range(0f, Screen.height);
        trackingAlpha[i] = Random.Range(0.3f, 0.7f);
        trackingWidth[i] = Random.Range(20f, 120f);
    }

    void Update()
    {
        // ── BREATHING TITLE TIMER ──
        titleBreathTimer += Time.unscaledDeltaTime * 0.8f;

        // ── BUTTON PRESS ANIMATION ──
        if (pressTimer > 0f)
        {
            pressTimer -= Time.unscaledDeltaTime * 5f;
            buttonPressScale = Mathf.Lerp(buttonPressScale, 0f, Time.unscaledDeltaTime * 10f);
        }
        else
        {
            buttonPressScale = 0f;
            pressedButtonIndex = -1;
        }

        // ── MENU SLIDE ANIMATIONS ──
        menuSlideOffset = Mathf.Lerp(menuSlideOffset, 0f, Time.unscaledDeltaTime * 8f);
        settingsSlideOffset = Mathf.Lerp(settingsSlideOffset, showSettings ? 0f : 100f, Time.unscaledDeltaTime * 12f);
        aboutSlideOffset = Mathf.Lerp(aboutSlideOffset, showAbout ? 0f : 100f, Time.unscaledDeltaTime * 12f);

        animTimer += Time.unscaledDeltaTime;
        titleAlpha = Mathf.Lerp(titleAlpha, 1f, Time.unscaledDeltaTime * 1.5f);
        if (animTimer > 0.8f)
            buttonsAlpha = Mathf.Lerp(buttonsAlpha, 1f, Time.unscaledDeltaTime * 2f);

        // Scanline scroll
        scanOffset += Time.unscaledDeltaTime * 40f;
        if (scanOffset > 4f) scanOffset = 0f;

        // Tape warp
        warpTimer += Time.unscaledDeltaTime;

        // Color bleed
        bleedTimer += Time.unscaledDeltaTime;

        // REC blink
        recTimer += Time.unscaledDeltaTime;
        if (recTimer >= 0.8f)
        {
            recVisible = !recVisible;
            recTimer = 0f;
        }

        // Fake timestamp
        timestampTimer += Time.unscaledDeltaTime;
        if (timestampTimer >= 1f)
        {
            timestampTimer = 0f;
            fakeSec++;
            if (fakeSec >= 60) { fakeSec = 0; fakeMin++; }
            if (fakeMin >= 60) { fakeMin = 0; fakeHour++; }
        }

        // Title flicker
        flickerTimer += Time.unscaledDeltaTime;
        if (flickerTimer >= nextFlicker)
        {
            flickerAlpha = Random.value < 0.12f ? Random.Range(0.2f, 0.75f) : 1f;
            flickerTimer = 0f;
            nextFlicker = Random.Range(0.05f, 5f);
        }

        // VHS tracking glitch
        trackingTimer += Time.unscaledDeltaTime;
        if (!isTracking && trackingTimer >= nextTracking)
        {
            isTracking = true;
            trackingDuration = Random.Range(0.1f, 0.5f);
            trackingTimer = 0f;
            nextTracking = Random.Range(2f, 8f);
            for (int i = 0; i < trackingLines.Length; i++)
                ResetTrackingLine(i);
        }
        if (isTracking)
        {
            trackingDuration -= Time.unscaledDeltaTime;
            if (trackingDuration <= 0f)
                isTracking = false;
        }

        // Static burst
        staticTimer += Time.unscaledDeltaTime;
        if (!isStatic && staticTimer >= nextStatic)
        {
            isStatic = true;
            staticDuration = Random.Range(0.05f, 0.2f);
            staticAlpha = Random.Range(0.1f, 0.25f);
            staticTimer = 0f;
            nextStatic = Random.Range(3f, 12f);
        }
        if (isStatic)
        {
            staticDuration -= Time.unscaledDeltaTime;
            if (staticDuration <= 0f)
            {
                isStatic = false;
                staticAlpha = 0f;
            }
        }

        if ((showAbout || showSettings) && Input.GetKeyDown(KeyCode.Escape))
        {
            PlaySound(openSound, 0.6f);
            showAbout = false;
            showSettings = false;
            showControlsTab = false;
        }
    }

    void PlaySound(AudioClip clip, float volumeScale = 1f)
    {
        if (clip != null && audioSource != null)
            audioSource.PlayOneShot(clip, hoverVolume * volumeScale);
    }

    void OnGUI()
    {
        float sw = Screen.width;
        float sh = Screen.height;
        float cx = sw / 2f;
        float cy = sh / 2f;

        // ── BACKGROUND ──
        GUI.color = new Color(0.04f, 0.03f, 0.03f, 1f);
        GUI.DrawTexture(new Rect(0, 0, sw, sh), Texture2D.whiteTexture);

        GUI.color = new Color(0.08f, 0f, 0.12f, 0.3f);
        GUI.DrawTexture(new Rect(0, 0, sw, sh), Texture2D.whiteTexture);

        // ── SCANLINES ──
        for (int y = 0; y < sh; y += 3)
        {
            GUI.color = new Color(0f, 0f, 0f, 0.18f);
            GUI.DrawTexture(new Rect(0, y + scanOffset, sw, 1.5f), Texture2D.whiteTexture);
        }

        // ── VHS TAPE WARP ──
        int warpLines = 5;
        for (int i = 0; i < warpLines; i++)
        {
            float wy = (sh / warpLines * i + Mathf.Sin(warpTimer * 0.7f + i * 1.3f) * sh * 0.3f + sh) % sh;
            float wAlpha = 0.04f + Mathf.Sin(warpTimer + i) * 0.02f;
            GUI.color = new Color(0.5f, 0.9f, 0.5f, wAlpha);
            GUI.DrawTexture(new Rect(0, wy, sw, 1f), Texture2D.whiteTexture);
        }

        // ── VHS TRACKING LINES ──
        if (isTracking)
        {
            for (int i = 0; i < trackingLines.Length; i++)
            {
                trackingLines[i] += Random.Range(-3f, 3f);
                GUI.color = new Color(0.85f, 0.9f, 0.85f, trackingAlpha[i] * 0.6f);
                GUI.DrawTexture(new Rect(Random.Range(-trackingWidth[i], 0), trackingLines[i], sw + trackingWidth[i], 2f), Texture2D.whiteTexture);
                GUI.color = new Color(0.2f, 0.8f, 0.3f, trackingAlpha[i] * 0.2f);
                GUI.DrawTexture(new Rect(0, trackingLines[i] + 2f, sw, 4f), Texture2D.whiteTexture);
            }

            GUI.color = new Color(0.8f, 0.9f, 0.8f, 0.08f);
            GUI.DrawTexture(new Rect(Random.Range(-15f, 15f), 0, sw, sh), Texture2D.whiteTexture);
        }

        // ── COLOR BLEED ──
        float bleedX = Mathf.Sin(bleedTimer * 0.3f) * 2.5f;
        GUI.color = new Color(0.9f, 0.1f, 0.1f, 0.04f);
        GUI.DrawTexture(new Rect(bleedX, 0, sw, sh), Texture2D.whiteTexture);
        GUI.color = new Color(0.1f, 0.9f, 0.3f, 0.03f);
        GUI.DrawTexture(new Rect(-bleedX, 0, sw, sh), Texture2D.whiteTexture);

        // ── STATIC BURST ──
        if (isStatic)
        {
            GUI.color = new Color(0.7f, 0.9f, 0.7f, staticAlpha);
            float staticH = Random.Range(sh * 0.1f, sh * 0.4f);
            float staticY = Random.Range(0f, sh - staticH);
            GUI.DrawTexture(new Rect(0, staticY, sw, staticH), Texture2D.whiteTexture);
        }

        // ── VIGNETTE ──
        int vsteps = 24;
        for (int i = 0; i < vsteps; i++)
        {
            float t = 1f - (i / (float)vsteps);
            GUI.color = new Color(0f, 0f, 0f, t * t * 0.55f);
            GUI.DrawTexture(new Rect(0, 0, sw * (i / (float)vsteps) * 0.28f, sh), Texture2D.whiteTexture);
            GUI.DrawTexture(new Rect(sw - sw * (i / (float)vsteps) * 0.28f, 0, sw * (i / (float)vsteps) * 0.28f, sh), Texture2D.whiteTexture);
            GUI.DrawTexture(new Rect(0, 0, sw, sh * (i / (float)vsteps) * 0.2f), Texture2D.whiteTexture);
            GUI.DrawTexture(new Rect(0, sh - sh * (i / (float)vsteps) * 0.2f, sw, sh * (i / (float)vsteps) * 0.2f), Texture2D.whiteTexture);
        }

        GUI.color = Color.white;

        // ── HUD OVERLAYS ──
        if (recVisible)
        {
            GUIStyle recStyle = new GUIStyle();
            recStyle.fontSize = (int)(Screen.height * 0.022f);
            recStyle.fontStyle = FontStyle.Bold;
            recStyle.normal.textColor = new Color(0.9f, 0.1f, 0.1f, 0.9f);
            GUI.Label(new Rect(22, 18, 80, 24), "● REC", recStyle);
        }

        GUIStyle tsStyle = new GUIStyle();
        tsStyle.fontSize = (int)(Screen.height * 0.018f);
        tsStyle.normal.textColor = new Color(0.7f, 0.9f, 0.7f, 0.7f);
        GUI.Label(new Rect(18, sh - 32, 200, 20), $"{fakeHour:D2}:{fakeMin:D2}:{fakeSec:D2}  SP  T-120", tsStyle);

        GUIStyle spStyle = new GUIStyle();
        spStyle.fontSize = 11;
        spStyle.normal.textColor = new Color(0.6f, 0.85f, 0.6f, 0.55f);
        spStyle.alignment = TextAnchor.MiddleRight;
        GUI.Label(new Rect(sw - 130, 18, 115, 20), "VHS  SP  CH03", spStyle);

        // ── MAIN MENU ──
        if (!showAbout && !showSettings && !isLoading)
        {
            DrawMainMenu(cx, cy, sw, sh);
        }
        else if (showSettings && !isLoading)
        {
            DrawSettingsPanel(cx, cy, sw, sh);
        }
        else if (showAbout && !isLoading)
        {
            DrawAboutPanel(cx, cy, sw, sh);
        }
        else if (isLoading)
        {
            DrawLoadingScreen(cx, cy, sw, sh);
        }

        GUI.color = Color.white;
    }

    void DrawMainMenu(float cx, float cy, float sw, float sh)
    {
        float slideX = menuSlideOffset;

        // ── TITLE WITH BREATHING ANIMATION ──
        float breathScale = 1f + Mathf.Sin(titleBreathTimer) * 0.02f;
        float titleY = cy - 280f;
        float breathOffset = (breathScale - 1f) * 20f;

        GUIStyle titleStyle = new GUIStyle();
        titleStyle.fontSize = (int)(Screen.height * 0.09f * breathScale);
        titleStyle.fontStyle = FontStyle.Bold;
        titleStyle.alignment = TextAnchor.MiddleCenter;

        for (int g = 3; g >= 1; g--)
        {
            titleStyle.normal.textColor = new Color(0.4f, 0.1f, 0.6f, 0.12f * g * titleAlpha * flickerAlpha);
            GUI.Label(new Rect(cx - 302 - g + slideX, titleY - breathOffset - g, 604 + g * 2, 75), "WHISPERS OF DARKNESS", titleStyle);
        }

        titleStyle.normal.textColor = new Color(0f, 0f, 0f, titleAlpha * 0.9f);
        GUI.Label(new Rect(cx - 301 + slideX, titleY - breathOffset + 3, 602, 75), "WHISPERS OF DARKNESS", titleStyle);

        float glowIntensity = 0.7f + Mathf.Sin(titleBreathTimer * 1.5f) * 0.2f;
        titleStyle.normal.textColor = new Color(0.88f * glowIntensity, 0.92f * glowIntensity, 0.85f * glowIntensity, titleAlpha * flickerAlpha);
        GUI.Label(new Rect(cx - 300 + slideX, titleY - breathOffset, 600, 75), "WHISPERS OF DARKNESS", titleStyle);

        GUIStyle subStyle = new GUIStyle();
        subStyle.fontSize = (int)(Screen.height * 0.018f);
        subStyle.fontStyle = FontStyle.Italic;
        subStyle.normal.textColor = new Color(0.5f, 0.3f, 0.7f, 0.6f * buttonsAlpha);
        subStyle.alignment = TextAnchor.MiddleCenter;
        GUI.Label(new Rect(cx - 300 + slideX, titleY + 85, 600, 25), "Munduan", subStyle);

        GUI.color = new Color(0.3f, 0.6f, 0.3f, 0.3f * buttonsAlpha);
        GUI.DrawTexture(new Rect(cx - 130 + slideX, titleY + 115, 260, 1), Texture2D.whiteTexture);
        GUI.color = Color.white;

        // ── BUTTONS ──
        string[] labels = { "PLAY", "SETTINGS", "ABOUT", "QUIT" };
        float btnW = (float)Screen.width * 0.22f;
        float btnH = (float)Screen.height * 0.08f;
        float startY = cy - 125f;
        float spacing = Screen.height * 0.11f;

        for (int i = 0; i < labels.Length; i++)
        {
            float hoverScale = 1f + (btnHover[i] * 0.05f);
            float pressScaleMulti = (pressedButtonIndex == i) ? 0.95f : 1f;
            float finalScale = hoverScale * pressScaleMulti;

            float bx = cx - (btnW * finalScale) / 2f + slideX;
            float by = startY + i * spacing - ((finalScale - 1f) * btnH / 2f);
            float scaledW = btnW * finalScale;
            float scaledH = btnH * finalScale;

            bool hover = new Rect(bx, by, scaledW, scaledH).Contains(Event.current.mousePosition);

            if (hover && !mainBtnHovered[i])
            {
                PlaySound(hoverSound, 0.8f);
            }
            mainBtnHovered[i] = hover;

            btnHover[i] = Mathf.Lerp(btnHover[i], hover ? 1f : 0f, Time.unscaledDeltaTime * 12f);

            GUI.color = new Color(
                0.15f + btnHover[i] * 0.1f,
                0.05f + btnHover[i] * 0.05f,
                0.25f + btnHover[i] * 0.1f,
                (0.55f + btnHover[i] * 0.3f) * buttonsAlpha);
            GUI.DrawTexture(new Rect(bx, by, scaledW, scaledH), Texture2D.whiteTexture);

            if (btnHover[i] > 0.5f)
            {
                GUI.color = new Color(0.7f, 0.3f, 0.9f, btnHover[i] * 0.2f);
                GUI.DrawTexture(new Rect(bx - 4, by - 4, scaledW + 8, scaledH + 8), Texture2D.whiteTexture);
            }

            GUI.color = new Color(0.6f, 0.2f, 0.8f, btnHover[i] * buttonsAlpha * 0.9f);
            GUI.DrawTexture(new Rect(bx, by, 3f, scaledH), Texture2D.whiteTexture);

            GUI.color = new Color(0.5f, 0.15f, 0.7f, (0.3f + btnHover[i] * 0.5f) * buttonsAlpha);
            GUI.DrawTexture(new Rect(bx, by, scaledW, 1.5f), Texture2D.whiteTexture);
            GUI.DrawTexture(new Rect(bx, by + scaledH - 1.5f, scaledW, 1.5f), Texture2D.whiteTexture);

            GUIStyle btnStyle = new GUIStyle();
            btnStyle.fontSize = (int)(scaledH * 0.35f);
            btnStyle.fontStyle = FontStyle.Bold;
            btnStyle.alignment = TextAnchor.MiddleCenter;
            btnStyle.normal.textColor = new Color(
                0.85f + btnHover[i] * 0.15f,
                0.55f + btnHover[i] * 0.1f,
                0.95f,
                buttonsAlpha);

            GUI.color = Color.white;
            GUI.Label(new Rect(bx, by, scaledW, scaledH), labels[i], btnStyle);

            if (hover && isTracking)
            {
                GUI.color = new Color(0.7f, 0.3f, 0.9f, 0.08f);
                GUI.DrawTexture(new Rect(bx + Random.Range(-4f, 4f), by, scaledW, scaledH), Texture2D.whiteTexture);
            }

            if (GUI.Button(new Rect(bx, by, scaledW, scaledH), GUIContent.none, GUIStyle.none))
            {
                PlaySound(clickSound, 1f);
                pressedButtonIndex = i;
                buttonPressScale = 1f;
                pressTimer = 0.15f;

                if (labels[i] == "PLAY")
                {
                    isLoading = true;
                    loadingOperation = SceneManager.LoadSceneAsync("chapter 1");
                }
                else if (labels[i] == "SETTINGS")
                {
                    PlaySound(openSound, 0.7f);
                    showSettings = true;
                    showControlsTab = false;
                }
                else if (labels[i] == "ABOUT")
                {
                    PlaySound(openSound, 0.7f);
                    showAbout = true;
                }
                else if (labels[i] == "QUIT")
                {
                    Application.Quit();
                }
            }
        }

        GUIStyle verStyle = new GUIStyle();
        verStyle.fontSize = (int)(Screen.height * 0.014f);
        verStyle.normal.textColor = new Color(0.3f, 0.5f, 0.3f, 0.5f);
        verStyle.alignment = TextAnchor.MiddleRight;
        GUI.Label(new Rect(sw - 110, sh - 25, 100, 18), "v1.0.0", verStyle);
    }

    void DrawSettingsPanel(float cx, float cy, float sw, float sh)
    {
        float panelW = 500f;
        float panelH = 520f;
        float px = cx - panelW / 2f + settingsSlideOffset;
        float py = cy - panelH / 2f;

        // Shadow
        GUI.color = new Color(0f, 0f, 0f, 0.8f);
        GUI.DrawTexture(new Rect(px + 5, py + 5, panelW, panelH), Texture2D.whiteTexture);

        // BG
        GUI.color = new Color(0.08f, 0.02f, 0.12f, 0.96f);
        GUI.DrawTexture(new Rect(px, py, panelW, panelH), Texture2D.whiteTexture);

        // Scanlines
        for (int y = 0; y < panelH; y += 3)
        {
            GUI.color = new Color(0f, 0f, 0f, 0.15f);
            GUI.DrawTexture(new Rect(px, py + y, panelW, 1f), Texture2D.whiteTexture);
        }

        // Border
        GUI.color = new Color(0.5f, 0.2f, 0.7f, 0.8f);
        GUI.DrawTexture(new Rect(px, py, panelW, 2), Texture2D.whiteTexture);
        GUI.DrawTexture(new Rect(px, py + panelH - 2, panelW, 2), Texture2D.whiteTexture);
        GUI.DrawTexture(new Rect(px, py, 2, panelH), Texture2D.whiteTexture);
        GUI.DrawTexture(new Rect(px + panelW - 2, py, 2, panelH), Texture2D.whiteTexture);
        GUI.color = Color.white;

        // Tab Buttons
        float tabW = 120f;
        float tabH = 30f;
        float tabY = py + 15f;
        float tabStartX = px + panelW / 2f - tabW;

        bool hoverAudio = new Rect(tabStartX, tabY, tabW, tabH).Contains(Event.current.mousePosition);
        bool hoverControls = new Rect(tabStartX + tabW + 10, tabY, tabW, tabH).Contains(Event.current.mousePosition);

        // Audio Tab Button
        GUI.color = new Color(0.2f, 0.05f, 0.3f, showControlsTab ? 0.6f : 0.9f);
        GUI.DrawTexture(new Rect(tabStartX, tabY, tabW, tabH), Texture2D.whiteTexture);
        GUI.color = new Color(0.5f, 0.2f, 0.7f, 0.8f);
        GUI.DrawTexture(new Rect(tabStartX, tabY, tabW, 1.5f), Texture2D.whiteTexture);
        GUI.DrawTexture(new Rect(tabStartX, tabY + tabH - 1.5f, tabW, 1.5f), Texture2D.whiteTexture);
        GUIStyle tabStyle = new GUIStyle();
        tabStyle.fontSize = 14;
        tabStyle.fontStyle = FontStyle.Bold;
        tabStyle.alignment = TextAnchor.MiddleCenter;
        tabStyle.normal.textColor = showControlsTab ? new Color(0.5f, 0.3f, 0.7f) : new Color(0.7f, 0.5f, 0.95f);
        GUI.color = Color.white;
        GUI.Label(new Rect(tabStartX, tabY, tabW, tabH), "AUDIO", tabStyle);
        if (GUI.Button(new Rect(tabStartX, tabY, tabW, tabH), GUIContent.none, GUIStyle.none))
        {
            PlaySound(clickSound, 0.7f);
            showControlsTab = false;
        }

        // Controls Tab Button
        GUI.color = new Color(0.2f, 0.05f, 0.3f, showControlsTab ? 0.9f : 0.6f);
        GUI.DrawTexture(new Rect(tabStartX + tabW + 10, tabY, tabW, tabH), Texture2D.whiteTexture);
        GUI.color = new Color(0.5f, 0.2f, 0.7f, 0.8f);
        GUI.DrawTexture(new Rect(tabStartX + tabW + 10, tabY, tabW, 1.5f), Texture2D.whiteTexture);
        GUI.DrawTexture(new Rect(tabStartX + tabW + 10, tabY + tabH - 1.5f, tabW, 1.5f), Texture2D.whiteTexture);
        tabStyle.normal.textColor = showControlsTab ? new Color(0.7f, 0.5f, 0.95f) : new Color(0.5f, 0.3f, 0.7f);
        GUI.Label(new Rect(tabStartX + tabW + 10, tabY, tabW, tabH), "CONTROLS", tabStyle);
        if (GUI.Button(new Rect(tabStartX + tabW + 10, tabY, tabW, tabH), GUIContent.none, GUIStyle.none))
        {
            PlaySound(clickSound, 0.7f);
            showControlsTab = true;
        }

        GUI.color = new Color(0.4f, 0.2f, 0.6f, 0.4f);
        GUI.DrawTexture(new Rect(px + 30, py + 50, panelW - 60, 1), Texture2D.whiteTexture);
        GUI.color = Color.white;

        if (!showControlsTab)
        {
            // ── AUDIO SETTINGS ──
            GUIStyle labelStyle = new GUIStyle();
            labelStyle.fontSize = 16;
            labelStyle.normal.textColor = new Color(0.75f, 0.55f, 0.9f);
            labelStyle.alignment = TextAnchor.MiddleLeft;

            GUIStyle valueStyle = new GUIStyle();
            valueStyle.fontSize = 15;
            valueStyle.normal.textColor = new Color(0.6f, 0.4f, 0.85f);
            valueStyle.alignment = TextAnchor.MiddleLeft;

            float labelX = px + 30f;
            float labelW = 120f;
            float sliderX = px + 160f;
            float sliderW = 220f;
            float valueX = px + 390f;
            float rowY = py + 70f;
            float rowH = 45f;

            // Volume Slider
            GUI.Label(new Rect(labelX, rowY, labelW, rowH), "Master Volume", labelStyle);
            
            // ── FIXED: Volume slider now properly updates AudioListener ──
            float newVolume = GUI.HorizontalSlider(new Rect(sliderX, rowY + 15f, sliderW, 16f), masterVolume, 0f, 100f);
            if (newVolume != masterVolume)
            {
                masterVolume = newVolume;
                AudioListener.volume = masterVolume / 100f;
            }
            
            GUI.Label(new Rect(valueX, rowY, 50f, rowH), $"{(int)masterVolume}%", valueStyle);
        }
        else
        {
            // ── CONTROLS SECTION ──
            GUIStyle controlTitleStyle = new GUIStyle();
            controlTitleStyle.fontSize = 14;
            controlTitleStyle.fontStyle = FontStyle.Bold;
            controlTitleStyle.normal.textColor = new Color(0.8f, 0.6f, 0.95f);
            controlTitleStyle.alignment = TextAnchor.MiddleLeft;

            GUIStyle controlTextStyle = new GUIStyle();
            controlTextStyle.fontSize = 13;
            controlTextStyle.normal.textColor = new Color(0.7f, 0.5f, 0.9f);
            controlTextStyle.alignment = TextAnchor.MiddleLeft;

            float startY = py + 70f;
            float rowH = 28f;
            float keyW = 100f;
            float actionW = 180f;
            float startX = px + 40f;

            // Movement
            GUI.Label(new Rect(startX, startY, keyW, rowH), "WASD", controlTitleStyle);
            GUI.Label(new Rect(startX + keyW, startY, actionW, rowH), "Move", controlTextStyle);
            
            GUI.Label(new Rect(startX, startY + rowH, keyW, rowH), "Mouse", controlTitleStyle);
            GUI.Label(new Rect(startX + keyW, startY + rowH, actionW, rowH), "Look around", controlTextStyle);
            
            GUI.Label(new Rect(startX, startY + rowH * 2, keyW, rowH), "Left Shift", controlTitleStyle);
            GUI.Label(new Rect(startX + keyW, startY + rowH * 2, actionW, rowH), "Sprint", controlTextStyle);
            
            GUI.Label(new Rect(startX, startY + rowH * 3, keyW, rowH), "Left Ctrl", controlTitleStyle);
            GUI.Label(new Rect(startX + keyW, startY + rowH * 3, actionW, rowH), "Crouch", controlTextStyle);
            
            GUI.Label(new Rect(startX, startY + rowH * 4, keyW, rowH), "Space", controlTitleStyle);
            GUI.Label(new Rect(startX + keyW, startY + rowH * 4, actionW, rowH), "Jump", controlTextStyle);
            
            // Interaction
            GUI.Label(new Rect(startX, startY + rowH * 6, keyW, rowH), "E", controlTitleStyle);
            GUI.Label(new Rect(startX + keyW, startY + rowH * 6, actionW, rowH), "Interact / Pick up", controlTextStyle);
            
            GUI.Label(new Rect(startX, startY + rowH * 7, keyW, rowH), "F", controlTitleStyle);
            GUI.Label(new Rect(startX + keyW, startY + rowH * 7, actionW, rowH), "Flashlight", controlTextStyle);
            
            GUI.Label(new Rect(startX, startY + rowH * 8, keyW, rowH), "G", controlTitleStyle);
            GUI.Label(new Rect(startX + keyW, startY + rowH * 8, actionW, rowH), "Drop item", controlTextStyle);
            
            GUI.Label(new Rect(startX, startY + rowH * 9, keyW, rowH), "Scroll Wheel", controlTitleStyle);
            GUI.Label(new Rect(startX + keyW, startY + rowH * 9, actionW, rowH), "Switch item", controlTextStyle);
            
            GUI.Label(new Rect(startX, startY + rowH * 10, keyW, rowH), "M", controlTitleStyle);
            GUI.Label(new Rect(startX + keyW, startY + rowH * 10, actionW, rowH), "Map", controlTextStyle);
            
            GUI.Label(new Rect(startX, startY + rowH * 11, keyW, rowH), "ESC", controlTitleStyle);
            GUI.Label(new Rect(startX + keyW, startY + rowH * 11, actionW, rowH), "Pause", controlTextStyle);
        }

        // ── SAVE + BACK BUTTONS ──
        float bbW = 120f;
        float bbH = 40f;
        float gap = 20f;
        float totalBtnW = bbW * 2 + gap;
        float btnStartX = cx - totalBtnW / 2f + settingsSlideOffset;
        float btnY = py + panelH - 65f;

        // Save button
        bool saveHov = new Rect(btnStartX, btnY, bbW, bbH).Contains(Event.current.mousePosition);
        DrawSmallButton(btnStartX, btnY, bbW, bbH, "SAVE", saveHov, false);
        if (GUI.Button(new Rect(btnStartX, btnY, bbW, bbH), GUIContent.none, GUIStyle.none))
        {
            PlaySound(clickSound, 0.8f);
            PlayerPrefs.SetFloat("MasterVolume", masterVolume);
            PlayerPrefs.Save();
        }

        // Back button
        bool backHov = new Rect(btnStartX + bbW + gap, btnY, bbW, bbH).Contains(Event.current.mousePosition);
        DrawSmallButton(btnStartX + bbW + gap, btnY, bbW, bbH, "BACK", backHov, true);
        if (GUI.Button(new Rect(btnStartX + bbW + gap, btnY, bbW, bbH), GUIContent.none, GUIStyle.none))
        {
            PlaySound(openSound, 0.6f);
            showSettings = false;
            showControlsTab = false;
        }
    }

    void DrawAboutPanel(float cx, float cy, float sw, float sh)
    {
        float pw = 430f;
        float ph = 260f;
        float px = cx - pw / 2f + aboutSlideOffset;
        float py = cy - ph / 2f;

        // Shadow
        GUI.color = new Color(0f, 0f, 0f, 0.8f);
        GUI.DrawTexture(new Rect(px + 5, py + 5, pw, ph), Texture2D.whiteTexture);

        // BG
        GUI.color = new Color(0.08f, 0.02f, 0.12f, 0.96f);
        GUI.DrawTexture(new Rect(px, py, pw, ph), Texture2D.whiteTexture);

        // Scanlines
        for (int y = 0; y < ph; y += 3)
        {
            GUI.color = new Color(0f, 0f, 0f, 0.15f);
            GUI.DrawTexture(new Rect(px, py + y, pw, 1f), Texture2D.whiteTexture);
        }

        // Border
        GUI.color = new Color(0.5f, 0.2f, 0.7f, 0.8f);
        GUI.DrawTexture(new Rect(px, py, pw, 2), Texture2D.whiteTexture);
        GUI.DrawTexture(new Rect(px, py + ph - 2, pw, 2), Texture2D.whiteTexture);
        GUI.DrawTexture(new Rect(px, py, 2, ph), Texture2D.whiteTexture);
        GUI.DrawTexture(new Rect(px + pw - 2, py, 2, ph), Texture2D.whiteTexture);
        GUI.color = Color.white;

        GUIStyle hStyle = new GUIStyle();
        hStyle.fontSize = 22;
        hStyle.fontStyle = FontStyle.Bold;
        hStyle.normal.textColor = new Color(0.7f, 0.4f, 0.9f);
        hStyle.alignment = TextAnchor.MiddleCenter;
        GUI.Label(new Rect(px, py + 18, pw, 30), "[ ABOUT ]", hStyle);

        GUI.color = new Color(0.4f, 0.2f, 0.6f, 0.4f);
        GUI.DrawTexture(new Rect(px + 30, py + 52, pw - 60, 1), Texture2D.whiteTexture);
        GUI.color = Color.white;

        GUIStyle bStyle = new GUIStyle();
        bStyle.fontSize = 14;
        bStyle.normal.textColor = new Color(0.7f, 0.5f, 0.9f);
        bStyle.alignment = TextAnchor.MiddleCenter;
        bStyle.wordWrap = true;
        GUI.Label(new Rect(px + 25, py + 70, pw - 50, 130),
            "Created by Bansag\n\nWhisper of the Dark\nMunduan\n\nVersion 1.0.0", bStyle);

        float bbW = 155f;
        float bbH = 40f;
        float bbX = cx - bbW / 2f + aboutSlideOffset;
        float bbY = py + ph - 58f;

        bool bHov = new Rect(bbX, bbY, bbW, bbH).Contains(Event.current.mousePosition);

        if (bHov && !aboutBackHovered)
        {
            PlaySound(hoverSound, 0.7f);
            aboutBackHovered = true;
        }
        else if (!bHov)
        {
            aboutBackHovered = false;
        }

        DrawSmallButton(bbX, bbY, bbW, bbH, "BACK", bHov, true);
        if (GUI.Button(new Rect(bbX, bbY, bbW, bbH), GUIContent.none, GUIStyle.none))
        {
            PlaySound(openSound, 0.6f);
            showAbout = false;
        }
    }

    void DrawSmallButton(float x, float y, float w, float h, string label, bool hover, bool isBack)
    {
        GUI.color = new Color(0.1f, 0f, hover ? 0.25f : 0.15f, 0.9f);
        GUI.DrawTexture(new Rect(x, y, w, h), Texture2D.whiteTexture);

        GUI.color = new Color(0.5f, 0.2f, 0.7f, 0.8f);
        GUI.DrawTexture(new Rect(x, y, w, 1.5f), Texture2D.whiteTexture);
        GUI.DrawTexture(new Rect(x, y + h - 1.5f, w, 1.5f), Texture2D.whiteTexture);
        GUI.DrawTexture(new Rect(x, y, 1.5f, h), Texture2D.whiteTexture);
        GUI.DrawTexture(new Rect(x + w - 1.5f, y, 1.5f, h), Texture2D.whiteTexture);

        GUIStyle style = new GUIStyle();
        style.fontSize = 15;
        style.fontStyle = FontStyle.Bold;
        style.alignment = TextAnchor.MiddleCenter;
        style.normal.textColor = new Color(0.7f + (hover ? 0.2f : 0f), 0.5f + (hover ? 0.2f : 0f), 0.95f, 1f);

        GUI.color = Color.white;
        GUI.Label(new Rect(x, y, w, h), label, style);
    }

    void DrawLoadingScreen(float cx, float cy, float sw, float sh)
    {
        GUI.color = new Color(0.04f, 0.03f, 0.03f, 1f);
        GUI.DrawTexture(new Rect(0, 0, sw, sh), Texture2D.whiteTexture);

        GUI.color = new Color(0.08f, 0f, 0.12f, 0.3f);
        GUI.DrawTexture(new Rect(0, 0, sw, sh), Texture2D.whiteTexture);

        for (int y = 0; y < sh; y += 3)
        {
            GUI.color = new Color(0f, 0f, 0f, 0.18f);
            GUI.DrawTexture(new Rect(0, y + scanOffset, sw, 1.5f), Texture2D.whiteTexture);
        }

        int loadVsteps = 24;
        for (int i = 0; i < loadVsteps; i++)
        {
            float t = 1f - (i / (float)loadVsteps);
            GUI.color = new Color(0f, 0f, 0f, t * t * 0.55f);
            GUI.DrawTexture(new Rect(0, 0, sw * (i / (float)loadVsteps) * 0.28f, sh), Texture2D.whiteTexture);
            GUI.DrawTexture(new Rect(sw - sw * (i / (float)loadVsteps) * 0.28f, 0, sw * (i / (float)loadVsteps) * 0.28f, sh), Texture2D.whiteTexture);
            GUI.DrawTexture(new Rect(0, 0, sw, sh * (i / (float)loadVsteps) * 0.2f), Texture2D.whiteTexture);
            GUI.DrawTexture(new Rect(0, sh - sh * (i / (float)loadVsteps) * 0.2f, sw, sh * (i / (float)loadVsteps) * 0.2f), Texture2D.whiteTexture);
        }

        GUI.color = Color.white;

        float pulse = (Mathf.Sin(Time.unscaledTime * 3f) + 1f) / 2f;
        GUIStyle loadStyle = new GUIStyle();
        loadStyle.fontSize = 32;
        loadStyle.fontStyle = FontStyle.Bold;
        loadStyle.alignment = TextAnchor.MiddleCenter;
        loadStyle.normal.textColor = new Color(0.88f + pulse * 0.1f, 0.92f, 0.85f, 1f);
        GUI.Label(new Rect(cx - 200, cy - 50, 400, 50), "LOADING...", loadStyle);

        float barW = 300f;
        float barH = 20f;
        float barX = cx - barW / 2f;
        float barY = cy + 20f;

        GUI.color = new Color(0.2f, 0.2f, 0.2f, 0.8f);
        GUI.DrawTexture(new Rect(barX, barY, barW, barH), Texture2D.whiteTexture);

        float progress = loadingOperation != null ? loadingOperation.progress : 0f;
        GUI.color = new Color(0.5f + pulse * 0.2f, 0.9f, 0.5f + pulse * 0.2f, 1f);
        GUI.DrawTexture(new Rect(barX, barY, barW * progress, barH), Texture2D.whiteTexture);

        GUI.color = new Color(0.7f, 0.3f, 0.9f, 0.3f);
        GUI.DrawTexture(new Rect(barX - 4, barY - 4, barW * progress + 8, barH + 8), Texture2D.whiteTexture);

        GUI.color = Color.white;

        GUIStyle progStyle = new GUIStyle();
        progStyle.fontSize = 14;
        progStyle.alignment = TextAnchor.MiddleCenter;
        progStyle.normal.textColor = new Color(0.7f, 0.5f, 0.9f, 1f);
        GUI.Label(new Rect(cx - 150, barY + barH + 10, 300, 20), $"{(progress * 100f):F0}%", progStyle);
    }
}