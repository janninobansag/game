using System.Collections;
using UnityEngine;

public class ScaryEffectTrigger : MonoBehaviour
{
    [Header("Trigger Settings")]
    public string playerTag = "Player";
    public bool triggerOnce = true;

    [Header("Effect Selection")]
    public bool doScreenBlood = true;
    public bool doHeartbeat = true;
    public bool doStaticGlitch = true;
    public bool doVignetteClose = true;
    public bool doWhisperText = true;
    public bool doScreenShake = true;
    public bool doColorDrain = true;

    [Header("Duration Settings")]
    public float effectDuration = 5f;
    public float fadeOutDuration = 2f;
    
    [Header("Heartbeat Settings")]
    public float heartbeatSpeed = 1.5f;
    public AudioClip heartbeatSound;
    public AudioClip staticSound;
    public AudioClip whisperSound;

    [Header("Whisper Text")]
    public string[] whisperMessages = {
        "get out...",
        "you shouldn't be here...",
        "leave now...",
        "it sees you...",
        "run..."
    };

    // Internal state
    private bool hasTriggered = false;
    private bool isActive = false;
    private float effectTimer = 0f;
    private float alpha = 0f;

    // Blood splatter
    private struct BloodSplat
    {
        public float x, y, w, h, alpha, rotation;
    }
    private BloodSplat[] bloodSplats = new BloodSplat[8];

    // Glitch
    private float glitchTimer = 0f;
    private bool isGlitching = false;
    private float glitchOffsetX = 0f;
    private float[] glitchBarY = new float[5];
    private float[] glitchBarH = new float[5];
    private float[] glitchBarA = new float[5];

    // Vignette
    private float vignetteAlpha = 0f;

    // Heartbeat pulse
    private float heartbeatTimer = 0f;
    private float heartbeatPulse = 0f;

    // Screen shake
    private float shakeIntensity = 0f;
    private Vector3 originalCameraPos;
    private Camera playerCamera;

    // Whisper text
    private string currentWhisper = "";
    private float whisperAlpha = 0f;
    private float whisperTimer = 0f;
    private float nextWhisper = 0f;

    // Color drain
    private float colorDrainAmount = 0f;

    // Static
    private float staticAlpha = 0f;
    private float staticTimer2 = 0f;

    // Audio
    private AudioSource heartbeatSource;
    private AudioSource staticSource;
    private AudioSource whisperSource;

    void Start()
    {
        playerCamera = Camera.main;

        Collider col = GetComponent<Collider>();
        if (col != null) col.isTrigger = true;

        // Setup audio sources
        heartbeatSource = gameObject.AddComponent<AudioSource>();
        heartbeatSource.spatialBlend = 0f;
        heartbeatSource.loop = true;
        heartbeatSource.playOnAwake = false;
        heartbeatSource.volume = 0f;
        if (heartbeatSound != null)
            heartbeatSource.clip = heartbeatSound;

        staticSource = gameObject.AddComponent<AudioSource>();
        staticSource.spatialBlend = 0f;
        staticSource.loop = true;
        staticSource.playOnAwake = false;
        staticSource.volume = 0f;
        if (staticSound != null)
            staticSource.clip = staticSound;

        whisperSource = gameObject.AddComponent<AudioSource>();
        whisperSource.spatialBlend = 0f;
        whisperSource.playOnAwake = false;
        whisperSource.volume = 0f;
        if (whisperSound != null)
            whisperSource.clip = whisperSound;

        // Init blood splats
        for (int i = 0; i < bloodSplats.Length; i++)
            ResetBloodSplat(ref bloodSplats[i]);

        // Init glitch bars
        for (int i = 0; i < glitchBarY.Length; i++)
            ResetGlitchBar(i);
    }

    void ResetBloodSplat(ref BloodSplat s)
    {
        s.x = Random.Range(0f, Screen.width);
        s.y = Random.Range(0f, Screen.height);
        s.w = Random.Range(30f, 120f);
        s.h = Random.Range(8f, 30f);
        s.alpha = 0f;
        s.rotation = Random.Range(-45f, 45f);
    }

    void ResetGlitchBar(int i)
    {
        glitchBarY[i] = Random.Range(0f, Screen.height);
        glitchBarH[i] = Random.Range(3f, 18f);
        glitchBarA[i] = Random.Range(0.1f, 0.4f);
    }

    void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag(playerTag)) return;
        if (triggerOnce && hasTriggered) return;

        hasTriggered = true;
        isActive = true;
        effectTimer = 0f;
        alpha = 0f;

        if (playerCamera != null)
            originalCameraPos = playerCamera.transform.localPosition;

        StartCoroutine(RunEffect());
    }

    IEnumerator RunEffect()
    {
        // Fade in
        float fadeInTime = 0.5f;
        float elapsed = 0f;
        while (elapsed < fadeInTime)
        {
            elapsed += Time.deltaTime;
            alpha = Mathf.Lerp(0f, 1f, elapsed / fadeInTime);
            yield return null;
        }
        alpha = 1f;

        // Start audio
        if (heartbeatSound != null)
        {
            heartbeatSource.Play();
            heartbeatSource.volume = 0.8f;
        }
        if (staticSound != null)
        {
            staticSource.Play();
            staticSource.volume = 0.3f;
        }

        // Hold effect
        yield return new WaitForSeconds(effectDuration);

        // Fade out
        elapsed = 0f;
        while (elapsed < fadeOutDuration)
        {
            elapsed += Time.deltaTime;
            alpha = Mathf.Lerp(1f, 0f, elapsed / fadeOutDuration);

            heartbeatSource.volume = Mathf.Lerp(0.8f, 0f,
                elapsed / fadeOutDuration);
            staticSource.volume = Mathf.Lerp(0.3f, 0f,
                elapsed / fadeOutDuration);

            yield return null;
        }

        // Cleanup
        alpha = 0f;
        isActive = false;
        heartbeatSource.Stop();
        staticSource.Stop();

        // Restore camera
        if (playerCamera != null)
            playerCamera.transform.localPosition = originalCameraPos;

        colorDrainAmount = 0f;
        vignetteAlpha = 0f;
        shakeIntensity = 0f;
        whisperAlpha = 0f;
        currentWhisper = "";
    }

    void Update()
    {
        if (!isActive) return;

        effectTimer += Time.deltaTime;

        // ── HEARTBEAT PULSE ──
        if (doHeartbeat)
        {
            heartbeatTimer += Time.deltaTime * heartbeatSpeed;
            heartbeatPulse = Mathf.Abs(Mathf.Sin(heartbeatTimer * 3f));
            vignetteAlpha = Mathf.Lerp(vignetteAlpha,
                0.3f + heartbeatPulse * 0.4f, Time.deltaTime * 5f);
        }

        // ── SCREEN SHAKE ──
        if (doScreenShake && playerCamera != null)
        {
            shakeIntensity = Mathf.Lerp(shakeIntensity,
                0.03f + heartbeatPulse * 0.02f, Time.deltaTime * 3f);
            float shakeX = Random.Range(-shakeIntensity, shakeIntensity);
            float shakeY = Random.Range(-shakeIntensity, shakeIntensity);
            playerCamera.transform.localPosition = originalCameraPos +
                new Vector3(shakeX, shakeY, 0f);
        }

        // ── GLITCH UPDATES ──
        if (doStaticGlitch)
        {
            glitchTimer += Time.deltaTime;
            if (glitchTimer > Random.Range(0.1f, 0.4f))
            {
                glitchTimer = 0f;
                isGlitching = Random.value > 0.4f;
                glitchOffsetX = Random.Range(-12f, 12f);
                for (int i = 0; i < glitchBarY.Length; i++)
                    ResetGlitchBar(i);
            }
            staticTimer2 += Time.deltaTime;
            staticAlpha = Mathf.Abs(Mathf.Sin(staticTimer2 * 8f)) * 0.15f * alpha;
        }

        // ── BLOOD SPLAT ──
        if (doScreenBlood)
        {
            for (int i = 0; i < bloodSplats.Length; i++)
            {
                float targetAlpha = (i / (float)bloodSplats.Length)
                    < (effectTimer / effectDuration) ? 0.6f : 0f;
                bloodSplats[i].alpha = Mathf.Lerp(bloodSplats[i].alpha,
                    targetAlpha * alpha, Time.deltaTime * 2f);
            }
        }

        // ── COLOR DRAIN ──
        if (doColorDrain)
        {
            colorDrainAmount = Mathf.Lerp(colorDrainAmount,
                0.7f * alpha, Time.deltaTime * 1.5f);
        }

        // ── WHISPER TEXT ──
        if (doWhisperText)
        {
            whisperTimer += Time.deltaTime;
            if (whisperTimer >= nextWhisper)
            {
                whisperTimer = 0f;
                nextWhisper = Random.Range(1.5f, 3f);
                currentWhisper = whisperMessages[
                    Random.Range(0, whisperMessages.Length)];
                StartCoroutine(FlashWhisper());

                if (whisperSound != null)
                {
                    whisperSource.volume = 0.5f * alpha;
                    whisperSource.Play();
                }
            }
        }
    }

    IEnumerator FlashWhisper()
    {
        // Fade in
        float elapsed = 0f;
        while (elapsed < 0.3f)
        {
            elapsed += Time.deltaTime;
            whisperAlpha = Mathf.Lerp(0f, alpha, elapsed / 0.3f);
            yield return null;
        }

        yield return new WaitForSeconds(1.2f);

        // Fade out
        elapsed = 0f;
        while (elapsed < 0.5f)
        {
            elapsed += Time.deltaTime;
            whisperAlpha = Mathf.Lerp(alpha, 0f, elapsed / 0.5f);
            yield return null;
        }
        whisperAlpha = 0f;
    }

    void OnGUI()
    {
        if (!isActive && alpha <= 0.01f) return;

        float sw = Screen.width;
        float sh = Screen.height;
        float cx = sw / 2f;
        float cy = sh / 2f;

        // ── COLOR DRAIN (desaturate overlay) ──
        if (doColorDrain)
        {
            GUI.color = new Color(0f, 0f, 0f, colorDrainAmount * 0.5f);
            GUI.DrawTexture(new Rect(0, 0, sw, sh), Texture2D.whiteTexture);
        }

        // ── BLOOD CORNER SPLATTERS ──
        if (doScreenBlood)
        {
            // Corner bleeds
            GUI.color = new Color(0.6f, 0f, 0f, 0.35f * alpha);
            GUI.DrawTexture(new Rect(0, 0, sw * 0.15f, sh * 0.2f),
                Texture2D.whiteTexture);
            GUI.DrawTexture(new Rect(sw * 0.85f, 0, sw * 0.15f, sh * 0.2f),
                Texture2D.whiteTexture);
            GUI.DrawTexture(new Rect(0, sh * 0.8f, sw * 0.15f, sh * 0.2f),
                Texture2D.whiteTexture);
            GUI.DrawTexture(new Rect(sw * 0.85f, sh * 0.8f,
                sw * 0.15f, sh * 0.2f), Texture2D.whiteTexture);

            // Blood splats
            foreach (var s in bloodSplats)
            {
                if (s.alpha <= 0.01f) continue;
                GUI.color = new Color(0.55f, 0f, 0.02f, s.alpha);
                GUIUtility.RotateAroundPivot(s.rotation,
                    new Vector2(s.x + s.w / 2f, s.y + s.h / 2f));
                GUI.DrawTexture(new Rect(s.x, s.y, s.w, s.h),
                    Texture2D.whiteTexture);
                GUI.matrix = Matrix4x4.identity;
            }
        }

        // ── STATIC GLITCH ──
        if (doStaticGlitch && isGlitching)
        {
            // Offset layer
            GUI.color = new Color(0.8f, 0f, 0f, 0.06f * alpha);
            GUI.DrawTexture(new Rect(glitchOffsetX, 0, sw, sh),
                Texture2D.whiteTexture);
            GUI.color = new Color(0f, 0f, 0.8f, 0.06f * alpha);
            GUI.DrawTexture(new Rect(-glitchOffsetX, 0, sw, sh),
                Texture2D.whiteTexture);

            // Glitch bars
            for (int i = 0; i < glitchBarY.Length; i++)
            {
                GUI.color = new Color(0.7f, 0.9f, 0.7f,
                    glitchBarA[i] * alpha);
                GUI.DrawTexture(new Rect(Random.Range(-10f, 10f),
                    glitchBarY[i], sw, glitchBarH[i]),
                    Texture2D.whiteTexture);
            }
        }

        // Static noise
        if (staticAlpha > 0.01f)
        {
            GUI.color = new Color(0.5f, 0.7f, 0.5f, staticAlpha);
            GUI.DrawTexture(new Rect(0, 0, sw, sh), Texture2D.whiteTexture);
        }

        // ── HEARTBEAT VIGNETTE ──
        if (doVignetteClose)
        {
            int steps = 30;
            float va = vignetteAlpha * alpha;

            for (int i = 0; i < steps; i++)
            {
                float t = 1f - (i / (float)steps);
                GUI.color = new Color(0.3f, 0f, 0f, t * t * va);
                GUI.DrawTexture(new Rect(0, 0,
                    sw * (i / (float)steps) * 0.35f, sh),
                    Texture2D.whiteTexture);
                GUI.DrawTexture(new Rect(
                    sw - sw * (i / (float)steps) * 0.35f, 0,
                    sw * (i / (float)steps) * 0.35f, sh),
                    Texture2D.whiteTexture);
                GUI.DrawTexture(new Rect(0, 0,
                    sw, sh * (i / (float)steps) * 0.3f),
                    Texture2D.whiteTexture);
                GUI.DrawTexture(new Rect(0,
                    sh - sh * (i / (float)steps) * 0.3f,
                    sw, sh * (i / (float)steps) * 0.3f),
                    Texture2D.whiteTexture);
            }

            // Red heartbeat flash
            GUI.color = new Color(0.5f, 0f, 0f,
                heartbeatPulse * 0.2f * alpha);
            GUI.DrawTexture(new Rect(0, 0, sw, sh), Texture2D.whiteTexture);
        }

        // ── WHISPER TEXT ──
        if (doWhisperText && whisperAlpha > 0.01f &&
            currentWhisper != "")
        {
            // Random position each time
            float tx = Random.Range(cx - 200f, cx + 50f);
            float ty = Random.Range(cy - 100f, cy + 100f);

            GUIStyle whisperStyle = new GUIStyle();
            whisperStyle.fontSize = Random.Range(14, 22);
            whisperStyle.fontStyle = FontStyle.Italic;
            whisperStyle.normal.textColor =
                new Color(0.85f, 0.82f, 0.82f, whisperAlpha);
            whisperStyle.alignment = TextAnchor.MiddleCenter;

            GUIStyle whisperShadow = new GUIStyle();
            whisperShadow.fontSize = whisperStyle.fontSize;
            whisperShadow.fontStyle = FontStyle.Italic;
            whisperShadow.normal.textColor =
                new Color(0.4f, 0f, 0f, whisperAlpha * 0.8f);
            whisperShadow.alignment = TextAnchor.MiddleCenter;

            GUI.Label(new Rect(tx + 1, ty + 1, 300, 40),
                currentWhisper, whisperShadow);
            GUI.Label(new Rect(tx, ty, 300, 40),
                currentWhisper, whisperStyle);
        }

        GUI.color = Color.white;
    }

    public void ResetTrigger()
    {
        hasTriggered = false;
        isActive = false;
        alpha = 0f;
        colorDrainAmount = 0f;
        vignetteAlpha = 0f;
        whisperAlpha = 0f;
        currentWhisper = "";
        StopAllCoroutines();

        if (playerCamera != null)
            playerCamera.transform.localPosition = originalCameraPos;
    }
}