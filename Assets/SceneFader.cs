using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

public class SceneFader : MonoBehaviour
{
    public static SceneFader Instance;

    [Header("Fade Settings")]
    public float fadeDuration = 1f;
    public Color fadeColor = Color.black;

    private Texture2D fadeTexture;
    private float fadeAlpha = 0f;
    private bool isFading = false;

    void Awake()
    {
        // Singleton pattern
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }

        // Create fade texture
        fadeTexture = new Texture2D(1, 1);
        fadeTexture.SetPixel(0, 0, fadeColor);
        fadeTexture.Apply();
    }

    void OnEnable()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    void OnDisable()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        // Fade in when any scene loads (except if we're already fading)
        if (!isFading)
        {
            StartCoroutine(FadeInOnly());
        }
    }

    public void FadeToScene(string sceneName)
    {
        if (!isFading)
            StartCoroutine(FadeOutAndLoad(sceneName));
    }

    IEnumerator FadeOutAndLoad(string sceneName)
    {
        isFading = true;

        // Fade to black
        float elapsed = 0f;
        while (elapsed < fadeDuration)
        {
            elapsed += Time.deltaTime;
            fadeAlpha = Mathf.Lerp(0f, 1f, elapsed / fadeDuration);
            yield return null;
        }
        fadeAlpha = 1f;

        // Load scene
        SceneManager.LoadScene(sceneName);

        // Scene will trigger OnSceneLoaded, which will handle fade in
        isFading = false;
    }

    IEnumerator FadeInOnly()
    {
        // Wait a tiny moment for the scene to fully load
        yield return null;
        
        // If we're already fully transparent, no need to fade
        if (fadeAlpha <= 0.01f)
            yield break;
        
        isFading = true;
        
        // Fade to clear
        float elapsed = 0f;
        while (elapsed < fadeDuration)
        {
            elapsed += Time.deltaTime;
            fadeAlpha = Mathf.Lerp(1f, 0f, elapsed / fadeDuration);
            yield return null;
        }
        fadeAlpha = 0f;
        
        isFading = false;
    }

    void OnGUI()
    {
        if (fadeAlpha > 0f)
        {
            GUI.color = new Color(fadeColor.r, fadeColor.g, fadeColor.b, fadeAlpha);
            GUI.DrawTexture(new Rect(0, 0, Screen.width, Screen.height), fadeTexture);
        }
    }
}