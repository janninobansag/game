using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

public class SceneLoader : MonoBehaviour
{
    public static SceneLoader Instance;

    private float loadingAlpha = 0f;
    private bool isLoading = false;
    private float dotTimer = 0f;
    private int dotCount = 0;

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    void Update()
    {
        if (isLoading)
        {
            dotTimer += Time.deltaTime;
            if (dotTimer >= 0.4f)
            {
                dotTimer = 0f;
                dotCount = (dotCount + 1) % 4;
            }
        }
    }

    public void LoadScene(string sceneName)
    {
        StartCoroutine(LoadSceneAsync(sceneName));
    }

    IEnumerator LoadSceneAsync(string sceneName)
    {
        isLoading = true;

        // Fade to black
        float elapsed = 0f;
        while (elapsed < 1f)
        {
            elapsed += Time.deltaTime * 2f;
            loadingAlpha = Mathf.Lerp(0f, 1f, elapsed);
            yield return null;
        }
        loadingAlpha = 1f;

        // Start loading scene in background
        AsyncOperation op = SceneManager.LoadSceneAsync(sceneName);
        op.allowSceneActivation = false;

        // Wait until scene is almost ready
        while (op.progress < 0.9f)
            yield return null;

        // Small delay so loading screen shows
        yield return new WaitForSeconds(0.5f);

        // Activate scene
        op.allowSceneActivation = true;

        // Wait for scene to fully load
        while (!op.isDone)
            yield return null;

        // Fade back in
        elapsed = 0f;
        while (elapsed < 1f)
        {
            elapsed += Time.deltaTime * 1.5f;
            loadingAlpha = Mathf.Lerp(1f, 0f, elapsed);
            yield return null;
        }

        loadingAlpha = 0f;
        isLoading = false;
    }

    void OnGUI()
    {
        if (loadingAlpha <= 0.01f) return;

        float sw = Screen.width;
        float sh = Screen.height;
        float cx = sw / 2f;
        float cy = sh / 2f;

        // Black overlay
        GUI.color = new Color(0f, 0f, 0f, loadingAlpha);
        GUI.DrawTexture(new Rect(0, 0, sw, sh), Texture2D.whiteTexture);

        if (isLoading)
        {
            // Loading text
            string dots = new string('.', dotCount);
            GUIStyle loadStyle = new GUIStyle();
            loadStyle.fontSize = 16;
            loadStyle.normal.textColor =
                new Color(0.5f, 0.5f, 0.5f, loadingAlpha);
            loadStyle.alignment = TextAnchor.MiddleCenter;
            GUI.Label(new Rect(cx - 100, sh - 50, 200, 30),
                $"Loading{dots}", loadStyle);
        }

        GUI.color = Color.white;
    }
}