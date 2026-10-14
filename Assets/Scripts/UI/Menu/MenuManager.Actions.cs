// PURPOSE: Handles shared menu actions: panel navigation, settings, quit, button audio, and enable refreshes.
using UnityEngine;

public partial class MenuManager : MonoBehaviour
{
    public void CloseDifficultyPanel()
    {
        PlayClickSound();

        if (difficultyPanel != null)
            difficultyPanel.SetActive(false);
    }

    public void LoadGame()
    {
        PlayClickSound();

        if (HasAnySaveFile())
        {
            OpenLoadPanel();
        }
        else
        {
        }
    }

    public void OpenSettings()
    {
        PlayClickSound();
        if (settingsPanel != null)
            settingsPanel.OpenSettings();
    }
    public void QuitGame()
    {
        PlayClickSound();

        if (SaveSystem.Instance != null)
        {
            SaveSystem.Instance.SaveGame();
        }

        Application.Quit();
    }

    public void PlayHoverSound()
    {
        if (hoverSound != null && audioSource != null)
        {
            audioSource.PlayOneShot(hoverSound, hoverVolume);
        }
    }

    public void PlayClickSound()
    {
        if (clickSound != null && audioSource != null)
        {
            audioSource.PlayOneShot(clickSound, clickVolume);
        }
    }

    void OnEnable()
    {
        if (!Application.isPlaying)
        {
            CreateAboutPanelIfNeeded();
            if (aboutPanel != null)
                aboutPanel.SetActive(true);
            return;
        }

        UpdateLoadButtonVisibility();
        UpdateLoadButtonsVisibility();
        LoadAndDisplayProgression();
    }
}
