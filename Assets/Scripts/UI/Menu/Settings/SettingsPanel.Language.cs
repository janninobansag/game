// PURPOSE: Selects the game language and refreshes localized settings labels and language-button highlights.
using UnityEngine;
using UnityEngine.UI;
using TMPro;
public partial class SettingsPanel : MonoBehaviour
{
    public void SetLanguage(int languageIndex)
    {
        CaptureCurrentSliderValues();
        currentLanguage = Mathf.Clamp(languageIndex, 0, 2);
        // Pause settings are a preview until the player presses Save. The main menu
        // keeps its existing immediate language-save behavior.
        if (PauseMenu.Instance == null || !PauseMenu.Instance.isPaused)
        {
            SaveSettingsToDatabase();
            SyncPlayerPrefsCache();
        }
        UpdateLanguageButtonsHighlight();
        ApplySelectedLanguage();
    }
    public GameLanguage GetSelectedLanguage()
    {
        return (GameLanguage)currentLanguage;
    }

    private void UpdateLanguageButtonsHighlight()
    {
        UpdateLanguageButton(englishLanguageButton, currentLanguage == (int)GameLanguage.English);
        UpdateLanguageButton(koreanLanguageButton, currentLanguage == (int)GameLanguage.Korean);
        UpdateLanguageButton(tagalogLanguageButton, currentLanguage == (int)GameLanguage.Tagalog);
    }

    private static void UpdateLanguageButton(Button button, bool selected)
    {
        if (button == null) return;

        ColorBlock colors = button.colors;
        colors.normalColor = selected ? new Color(0.5f, 0.2f, 0.7f, 1f) : Color.white;
        colors.selectedColor = colors.normalColor;
        button.colors = colors;

        TextMeshProUGUI label = button.GetComponentInChildren<TextMeshProUGUI>();
        if (label != null)
            label.color = selected ? new Color(0.486f, 0.051f, 0.027f, 1f) : Color.white;
    }

    private void UpdateVolumeLabel(float value)
    {
        float displayedValue = volumeSlider != null ? volumeSlider.value : value;
        if (volumeLabel != null)
            volumeLabel.text = GetSelectedLanguage() == GameLanguage.Korean && koreanFont != null
                ? $"볼륨: {Mathf.RoundToInt(displayedValue)}%"
                : GetSelectedLanguage() == GameLanguage.Tagalog
                    ? $"Lakas ng tunog: {Mathf.RoundToInt(displayedValue)}%"
                    : $"Volume: {Mathf.RoundToInt(displayedValue)}%";
    }

    private void UpdateSensitivityLabel(float value)
    {
        float displayedValue = sensitivitySlider != null ? sensitivitySlider.value : value;
        if (sensitivityLabel != null)
            sensitivityLabel.text = GetSelectedLanguage() == GameLanguage.Korean && koreanFont != null
                ? $"감도: {displayedValue:F1}"
                : GetSelectedLanguage() == GameLanguage.Tagalog
                    ? $"Sensitibidad: {displayedValue:F1}"
                    : $"Sensitivity: {displayedValue:F1}";
    }

    private void UpdateBrightnessLabel(float value)
    {
        float displayedValue = brightnessSlider != null ? brightnessSlider.value : value;
        if (brightnessLabel != null)
            brightnessLabel.text = GetSelectedLanguage() == GameLanguage.Korean && koreanFont != null
                ? $"밝기: {Mathf.RoundToInt(displayedValue * 100)}%"
                : GetSelectedLanguage() == GameLanguage.Tagalog
                    ? $"Liwanag: {Mathf.RoundToInt(displayedValue * 100)}%"
                    : $"Brightness: {Mathf.RoundToInt(displayedValue * 100)}%";
    }
    private void ApplySelectedLanguage()
    {
        MenuLocalization.Apply(GetSelectedLanguage(), koreanFont);
        UpdateVolumeLabel(currentVolume);
        UpdateSensitivityLabel(currentSensitivity);
        UpdateBrightnessLabel(currentBrightness);
    }
    private void ApplySensitivity(float value)
    {
        PlayerController playerController = FindObjectOfType<PlayerController>();
        if (playerController != null)
        {
            playerController.mouseSensitivity = value;
        }
    }

    private void ApplyBrightness(float value)
    {
        ScreenBrightnessController.Apply(value);
    }

    private void PlayClickSound()
    {
        // SoundManager removed - sounds handled by ButtonSound.cs
        // If you want click sounds, add ButtonSound component to each button
    }
}
