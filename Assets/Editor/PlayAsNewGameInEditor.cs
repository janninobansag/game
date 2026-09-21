using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

/// <summary>
/// Editor-only convenience: pressing Play from a gameplay scene deletes the
/// game-save databases and starts a new game. This is never included in a build.
/// </summary>
[InitializeOnLoad]
public static class PlayAsNewGameInEditor
{
    private static readonly string[] SaveDatabaseFiles =
    {
        "gameSave_v2.db",
        "gameSave_Hard_v2.db"
    };

    static PlayAsNewGameInEditor()
    {
        EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
    }

    private static void OnPlayModeStateChanged(PlayModeStateChange state)
    {
        if (state != PlayModeStateChange.ExitingEditMode)
            return;

        string sceneName = EditorSceneManager.GetActiveScene().name;
        if (sceneName != "chapter 1" && sceneName != "chapter 2")
            return;

        DeleteGameSaveDatabases();

        bool isChapterTwo = sceneName == "chapter 2";
        PlayerPrefs.SetInt("ShouldLoadSave", 0);
        PlayerPrefs.SetInt("SkipIntro", 0);
        PlayerPrefs.SetString("GameDifficulty", isChapterTwo ? "Hard" : "Normal");
        PlayerPrefs.SetString("SavedScene", sceneName);
        PlayerPrefs.DeleteKey("GameProgress");
        PlayerPrefs.DeleteKey("MansionKeySpawnIndex_Normal");
        PlayerPrefs.DeleteKey("MansionKeySpawnIndex_Hard");
        PlayerPrefs.DeleteKey("FlashlightSpawnIndex_Normal");
        PlayerPrefs.DeleteKey("FlashlightSpawnIndex_Hard");
        PlayerPrefs.Save();
    }

    private static void DeleteGameSaveDatabases()
    {
        foreach (string fileName in SaveDatabaseFiles)
        {
            string databasePath = Path.Combine(Application.persistentDataPath, fileName);
            DeleteIfPresent(databasePath);
            DeleteIfPresent(databasePath + "-wal");
            DeleteIfPresent(databasePath + "-shm");
        }
    }

    private static void DeleteIfPresent(string path)
    {
        if (File.Exists(path))
            File.Delete(path);
    }
}