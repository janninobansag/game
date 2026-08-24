using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Video;

public class OutroManager : MonoBehaviour
{
    [Header("Video Settings")]
    public VideoPlayer videoPlayer;
    public string menuSceneName = "menu";

    [Header("Fade Settings")]
    public float fadeInDuration = 1f;
    public float fadeOutDuration = 1.5f;
    public float delayAfterVideo = 1f;
    public Color fadeColor = Color.black;

    private Texture2D fadeTexture;
    private float fadeAlpha = 0f;
    private bool isFading = false;
    private bool videoEnded = false;

    void Start()
    {
        // Create fade texture
        fadeTexture = new Texture2D(1, 1);
        fadeTexture.SetPixel(0, 0, fadeColor);
        fadeTexture.Apply();

        // Start with black screen
        fadeAlpha = 1f;

        // Get video player if not assigned
        if (videoPlayer == null)
            videoPlayer = GetComponent<VideoPlayer>();

        if (videoPlayer != null)
        {
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
        // Optional: Skip video with any key
        if (Input.anyKeyDown && !isFading && !videoEnded)
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
            fadeAlpha = Mathf.Lerp(1f, 0f, elapsed / fadeInDuration);
            yield return null;
        }
        fadeAlpha = 0f;
        
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
            fadeAlpha = Mathf.Lerp(0f, 1f, elapsed / fadeOutDuration);
            yield return null;
        }
        fadeAlpha = 1f;

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
        if (fadeAlpha > 0f)
        {
            GUI.color = new Color(fadeColor.r, fadeColor.g, fadeColor.b, fadeAlpha);
            GUI.DrawTexture(new Rect(0, 0, Screen.width, Screen.height), fadeTexture);
        }
    }

    void OnDestroy()
    {
        if (fadeTexture != null)
            Destroy(fadeTexture);
    }
}