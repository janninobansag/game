// PURPOSE: Plays the ending video with fades and returns to the menu when it finishes.
using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Video;
using UnityEngine.UI;

public class OutroManager : MonoBehaviour
{
    // Video player and destination scene used by the ending sequence.
    [Header("Video Settings")]
    public VideoPlayer videoPlayer;
    public string menuSceneName = "menu";

    // Fade timing and overlay color used before and after the outro video.
    [Header("Fade Settings")]
    public float fadeInDuration = 1f;
    public float fadeOutDuration = 1.5f;
    public float delayAfterVideo = 1f;
    public Color fadeColor = Color.black;

    private Texture2D fadeTexture;
    private float fadeAlpha = 0f;
    private bool isFading = false;
    private bool videoEnded = false;
    private Canvas overlayCanvas;
    private RawImage videoImage;
    private Image fadeImage;
    private RenderTexture videoTexture;
    private int textureWidth;
    private int textureHeight;

    void Start()
    {
        // Create fade texture
        fadeTexture = new Texture2D(1, 1);
        fadeTexture.SetPixel(0, 0, fadeColor);
        fadeTexture.Apply();

        // Start with black screen
        SetFadeAlpha(1f);

        // Get video player if not assigned
        if (videoPlayer == null)
            videoPlayer = GetComponent<VideoPlayer>();

        if (videoPlayer != null)
        {
            CreateVideoOverlay();
            // Register event for when video ends
            videoPlayer.loopPointReached += OnVideoEnd;
            
            // Start playing
            videoPlayer.Play();
            
            // Fade in
            StartCoroutine(FadeIn());
        }
        else
        {
            // Skip to menu if no video
            StartCoroutine(DelayedLoadMenu());
        }

        // Unlock cursor for outro
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        // Reset time scale
        Time.timeScale = 1f;
    }

    void Update()
    {
        EnsureVideoTexture();

        // Credits can only be skipped with Enter.
        bool enterPressed = Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter);
        if (enterPressed && !isFading && !videoEnded)
        {
            SkipVideo();
        }
    }

    void OnVideoEnd(VideoPlayer vp)
    {
        videoEnded = true;
        StartCoroutine(FadeOutAndLoadMenu());
    }

    void SkipVideo()
    {
        if (videoEnded || isFading) return;

        
        if (videoPlayer != null && videoPlayer.isPlaying)
            videoPlayer.Stop();

        StartCoroutine(FadeOutAndLoadMenu());
    }

    IEnumerator FadeIn()
    {
        isFading = true;
        
        float elapsed = 0f;
        while (elapsed < fadeInDuration)
        {
            elapsed += Time.deltaTime;
            SetFadeAlpha(Mathf.Lerp(1f, 0f, elapsed / fadeInDuration));
            yield return null;
        }
        SetFadeAlpha(0f);
        
        isFading = false;
    }

    IEnumerator FadeOutAndLoadMenu()
    {
        isFading = true;
        
        // Small delay before fading out
        yield return new WaitForSeconds(delayAfterVideo);

        // Fade to black
        float elapsed = 0f;
        while (elapsed < fadeOutDuration)
        {
            elapsed += Time.deltaTime;
            SetFadeAlpha(Mathf.Lerp(0f, 1f, elapsed / fadeOutDuration));
            yield return null;
        }
        SetFadeAlpha(1f);

        // Wait a moment
        yield return new WaitForSeconds(0.3f);

        // Load menu
        SceneManager.LoadScene(menuSceneName);
    }

    IEnumerator DelayedLoadMenu()
    {
        yield return new WaitForSeconds(2f);
        SceneManager.LoadScene(menuSceneName);
    }

    void OnGUI()
    {
        if (fadeImage == null && fadeAlpha > 0f)
        {
            GUI.color = new Color(fadeColor.r, fadeColor.g, fadeColor.b, fadeAlpha);
            GUI.DrawTexture(new Rect(0, 0, Screen.width, Screen.height), fadeTexture);
        }
    }

    void OnDestroy()
    {
        if (videoTexture != null)
        {
            videoTexture.Release();
            Destroy(videoTexture);
        }
        if (fadeTexture != null)
            Destroy(fadeTexture);
    }

    // Draw the outro as screen-space UI so camera render scale and post effects cannot alter the video.
    private void CreateVideoOverlay()
    {
        GameObject canvasObject = new GameObject("Outro Video Overlay", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        overlayCanvas = canvasObject.GetComponent<Canvas>();
        overlayCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
        overlayCanvas.sortingOrder = 1000;

        GameObject videoObject = new GameObject("Video", typeof(RectTransform), typeof(RawImage), typeof(AspectRatioFitter));
        videoObject.transform.SetParent(canvasObject.transform, false);
        videoImage = videoObject.GetComponent<RawImage>();
        videoImage.raycastTarget = false;
        StretchToScreen(videoImage.rectTransform);

        AspectRatioFitter aspectFitter = videoObject.GetComponent<AspectRatioFitter>();
        aspectFitter.aspectMode = AspectRatioFitter.AspectMode.FitInParent;
        aspectFitter.aspectRatio = videoPlayer.clip != null && videoPlayer.clip.height > 0
            ? (float)videoPlayer.clip.width / videoPlayer.clip.height
            : 16f / 9f;

        GameObject fadeObject = new GameObject("Fade", typeof(RectTransform), typeof(Image));
        fadeObject.transform.SetParent(canvasObject.transform, false);
        fadeImage = fadeObject.GetComponent<Image>();
        fadeImage.raycastTarget = false;
        StretchToScreen(fadeImage.rectTransform);
        SetFadeAlpha(fadeAlpha);

        videoPlayer.renderMode = VideoRenderMode.RenderTexture;
        EnsureVideoTexture();
    }

    private void EnsureVideoTexture()
    {
        if (videoPlayer == null || videoImage == null) return;

        int width = Mathf.Max(1, Screen.width);
        int height = Mathf.Max(1, Screen.height);
        if (videoTexture != null && width == textureWidth && height == textureHeight) return;

        if (videoTexture != null)
        {
            videoTexture.Release();
            Destroy(videoTexture);
        }

        textureWidth = width;
        textureHeight = height;
        videoTexture = new RenderTexture(width, height, 0, RenderTextureFormat.ARGB32);
        videoTexture.Create();
        videoPlayer.targetTexture = videoTexture;
        videoImage.texture = videoTexture;
    }

    private void SetFadeAlpha(float alpha)
    {
        fadeAlpha = alpha;
        if (fadeImage == null) return;

        Color color = fadeColor;
        color.a *= fadeAlpha;
        fadeImage.color = color;
    }

    private static void StretchToScreen(RectTransform rect)
    {
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
    }
}
