using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Video;

public class IntroVideo : MonoBehaviour
{
    [Header("Fade Settings")]
    public float fadeInDuration = 0.5f;
    public float fadeOutDuration = 1f;
    public Color fadeColor = Color.black;

    private VideoPlayer videoPlayer;
    private bool isFading = false;
    private float fadeAlpha = 1f;
    private Texture2D fadeTexture;
    private bool videoEnded = false;
    private bool isSkipping = false;

    void Start()
    {
        // Create fade texture
        fadeTexture = new Texture2D(1, 1);
        fadeTexture.SetPixel(0, 0, fadeColor);
        fadeTexture.Apply();

        videoPlayer = GetComponent<VideoPlayer>();
        
        if (videoPlayer != null)
        {
            videoPlayer.loopPointReached += OnVideoEnd;
            videoPlayer.Play();
            StartCoroutine(FadeIn());
        }
        else
        {
            GoToMainMenu();
        }
    }

    void Update()
    {
        if (!isFading && !videoEnded && !isSkipping && Input.anyKeyDown)
        {
            SkipIntro();
        }
    }

    System.Collections.IEnumerator FadeIn()
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

    void OnVideoEnd(VideoPlayer vp)
    {
        videoEnded = true;
        StartCoroutine(FadeOutAndGoToMenu());
    }

    void SkipIntro()
    {
        isSkipping = true;
        
        if (videoPlayer != null && videoPlayer.isPlaying)
        {
            videoPlayer.Stop();
        }
        
        StartCoroutine(FadeOutAndGoToMenu());
    }

    System.Collections.IEnumerator FadeOutAndGoToMenu()
    {
        isFading = true;
        
        float elapsed = 0f;
        while (elapsed < fadeOutDuration)
        {
            elapsed += Time.deltaTime;
            fadeAlpha = Mathf.Lerp(0f, 1f, elapsed / fadeOutDuration);
            yield return null;
        }
        fadeAlpha = 1f;
        
        yield return new WaitForSeconds(0.1f);
        
        GoToMainMenu();
    }

    void GoToMainMenu()
    {
        SceneManager.LoadScene("menu");
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