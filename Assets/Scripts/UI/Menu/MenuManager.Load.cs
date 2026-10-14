// PURPOSE: Opens the Load panel and loads existing Normal or Hard save data into the correct chapter.
using UnityEngine;
using System.IO;

public partial class MenuManager : MonoBehaviour
{
    // ── Open Load Panel ──
    public void OpenLoadPanel()
    {
        PlayClickSound();

        // Update button visibility before showing panel
        UpdateLoadButtonsVisibility();

        // ── Load progression when panel opens ──
        LoadAndDisplayProgression();

        if (loadPanel != null)
            loadPanel.SetActive(true);

        if (difficultyPanel != null)
            difficultyPanel.SetActive(false);
    }

    // ── Close Load Panel ──
    public void CloseLoadPanel()
    {
        PlayClickSound();

        if (loadPanel != null)
            loadPanel.SetActive(false);

    }

    // ── Load Normal save ──
    public void LoadNormalSave()
    {
        PlayClickSound();

        string normalPath = Path.Combine(Application.persistentDataPath, "gameSave_v2.db");

        if (CanLoadSave("gameSave_v2.db"))
        {

            // Set the difficulty so SaveSystem loads the correct database
            PlayerPrefs.SetString("GameDifficulty", "Normal");
            PlayerPrefs.SetInt("ShouldLoadSave", 1);
            PlayerPrefs.SetInt("SkipIntro", 1);
            PlayerPrefs.SetString("SavedScene", "chapter 1");
            PlayerPrefs.Save();

            if (loadPanel != null)
            loadPanel.SetActive(false);


            // Force reload SaveSystem to use Normal database
            if (SaveSystem.Instance != null)
            {
                SaveSystem.Instance.ReinitializeDatabase();
            }

            SceneFader.Instance.FadeToScene("chapter 1");
        }
        else
        {
        }
    }

    // ── Load Hard save ──
    public void LoadHardSave()
    {
        PlayClickSound();

        string hardPath = Path.Combine(Application.persistentDataPath, "gameSave_Hard_v2.db");

        if (CanLoadSave("gameSave_Hard_v2.db"))
        {

            // Set the difficulty so SaveSystem loads the correct database
            PlayerPrefs.SetString("GameDifficulty", "Hard");
            PlayerPrefs.SetInt("ShouldLoadSave", 1);
            PlayerPrefs.SetInt("SkipIntro", 1);
            PlayerPrefs.SetString("SavedScene", "chapter 2");
            PlayerPrefs.Save();

            if (loadPanel != null)
            loadPanel.SetActive(false);


            // Force reload SaveSystem to use Hard database
            if (SaveSystem.Instance != null)
            {
                SaveSystem.Instance.ReinitializeDatabase();
            }

            SceneFader.Instance.FadeToScene("chapter 2");
        }
        else
        {
        }
    }
}
