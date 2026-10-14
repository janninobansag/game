// PURPOSE: Saves and restores game progress and player state using the selected difficulty database.
using UnityEngine;
public partial class SaveSystem : MonoBehaviour
{
    void Start()
    {
        if (!isLoadComplete)
        {
            string currentScene = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name;
            if (currentScene != "menu" && currentScene != "MainMenu")
            {
                if (PlayerPrefs.GetInt("ShouldLoadSave", 0) == 1)
                {
                    StartCoroutine(LoadAfterSceneLoad());
                }
            }
        }
    }

    void OnEnable()
    {
        UnityEngine.SceneManagement.SceneManager.sceneLoaded += OnSceneLoaded;
    }

    void OnDisable()
    {
        UnityEngine.SceneManagement.SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    void OnSceneLoaded(UnityEngine.SceneManagement.Scene scene, UnityEngine.SceneManagement.LoadSceneMode mode)
    {
        if (scene.name == "menu" || scene.name == "MainMenu")
        {
            PlayerPrefs.SetInt("ShouldLoadSave", 0);
            PlayerPrefs.Save();
            isLoadComplete = false;
            return;
        }

        if (!isLoadComplete)
        {
            if (PlayerPrefs.GetInt("ShouldLoadSave", 0) == 1)
            {
                StartCoroutine(LoadAfterSceneLoad());
            }
        }
    }

    System.Collections.IEnumerator LoadAfterSceneLoad()
    {
        GameObject player = null;
        float waitTimer = 0f;
        while (player == null && waitTimer < 8f)
        {
            player = GameObject.FindGameObjectWithTag("Player");
            if (player == null)
            {
                yield return new WaitForSeconds(0.3f);
                waitTimer += 0.3f;
            }
        }

        if (player != null)
        {
            yield return new WaitForSeconds(0.3f);

            if (HasSaveFile())
            {
                LoadGame();
            }
            else
            {
            }
        }
        else
        {
        }

        PlayerPrefs.SetInt("ShouldLoadSave", 0);
        PlayerPrefs.SetInt("SkipIntro", 0);
        PlayerPrefs.Save();
        isLoadComplete = true;
    }
    void OnApplicationQuit()
    {
        if (autoSaveOnQuit && !isQuitting)
        {
            isQuitting = true;
            string sceneName = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name;
            if (sceneName != "menu" && sceneName != "MainMenu")
            {
                SaveGame();
            }
            isQuitting = false;
        }

        if (connection != null)
        {
            connection.Close();
            connection = null;
        }
    }

    void OnDestroy()
    {
        if (connection != null)
        {
            connection.Close();
            connection = null;
        }
    }
}
