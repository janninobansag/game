using UnityEngine;

public class ScreenEffects : MonoBehaviour
{
    [Header("Scanlines")]
    public bool showScanlines = true;
    public float scanlineOpacity = 0.03f;

    [Header("Vignette")]
    public bool showVignette = true;
    public float vignetteStrength = 0.2f;
    
    void OnGUI()
    {
        // Don't draw screen effects when game is paused
        if (PauseMenu.Instance != null && PauseMenu.Instance.isPaused)
        {
            return;
        }
        
        // Also don't draw if game is loading or not active
        if (Time.timeScale == 0f)
        {
            return;
        }

        float sw = Screen.width;
        float sh = Screen.height;

        // ── SCANLINES ──
        if (showScanlines)
        {
            for (int y = 0; y < sh; y += 3)
            {
                GUI.color = new Color(0f, 0f, 0f, scanlineOpacity);
                GUI.DrawTexture(new Rect(0, y, sw, 1.5f), Texture2D.whiteTexture);
            }
        }

        // ── VIGNETTE ──
        if (showVignette)
        {
            int steps = 28;

            // Left
            for (int i = 0; i < steps; i++)
            {
                float t = 1f - (i / (float)steps);
                GUI.color = new Color(0f, 0f, 0f, t * t * vignetteStrength);
                GUI.DrawTexture(new Rect(0, 0,
                    sw * (i / (float)steps) * 0.25f, sh),
                    Texture2D.whiteTexture);
            }
            // Right
            for (int i = 0; i < steps; i++)
            {
                float t = 1f - (i / (float)steps);
                GUI.color = new Color(0f, 0f, 0f, t * t * vignetteStrength);
                GUI.DrawTexture(new Rect(
                    sw - sw * (i / (float)steps) * 0.25f, 0,
                    sw * (i / (float)steps) * 0.25f, sh),
                    Texture2D.whiteTexture);
            }
            // Top
            for (int i = 0; i < steps; i++)
            {
                float t = 1f - (i / (float)steps);
                GUI.color = new Color(0f, 0f, 0f, t * t * vignetteStrength);
                GUI.DrawTexture(new Rect(0, 0,
                    sw, sh * (i / (float)steps) * 0.22f),
                    Texture2D.whiteTexture);
            }
            // Bottom
            for (int i = 0; i < steps; i++)
            {
                float t = 1f - (i / (float)steps);
                GUI.color = new Color(0f, 0f, 0f, t * t * vignetteStrength);
                GUI.DrawTexture(new Rect(
                    0, sh - sh * (i / (float)steps) * 0.22f,
                    sw, sh * (i / (float)steps) * 0.22f),
                    Texture2D.whiteTexture);
            }
        }

        GUI.color = Color.white;
    }
}