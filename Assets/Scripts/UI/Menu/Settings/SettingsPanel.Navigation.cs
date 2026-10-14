// PURPOSE: Opens and closes the settings UI, and switches between the Settings and Controls panels.
using UnityEngine;
public partial class SettingsPanel : MonoBehaviour
{
    public void OpenSettings()
    {
        if (settingsPanel != null)
            settingsPanel.SetActive(true);

        ShowSettingsPanel();
        RefreshUI();
    }

    public void CloseSettings()
    {
        // PlayClickSound(); // Commented out - no SoundManager
        if (settingsPanel != null)
            settingsPanel.SetActive(false);
    }

    public void ShowControlsPanel()
    {
        // PlayClickSound(); // Commented out - no SoundManager

        // Hide settings controls
        if (volumeSlider != null) volumeSlider.gameObject.SetActive(false);
        if (volumeLabel != null) volumeLabel.gameObject.SetActive(false);
        if (sensitivitySlider != null) sensitivitySlider.gameObject.SetActive(false);
        if (sensitivityLabel != null) sensitivityLabel.gameObject.SetActive(false);
        if (brightnessSlider != null) brightnessSlider.gameObject.SetActive(false);
        if (brightnessLabel != null) brightnessLabel.gameObject.SetActive(false);

        // Hide graphics UI
        if (graphicsLabel != null) graphicsLabel.gameObject.SetActive(false);
        if (lowButton != null) lowButton.gameObject.SetActive(false);
        if (mediumButton != null) mediumButton.gameObject.SetActive(false);
        if (highButton != null) highButton.gameObject.SetActive(false);

        // Hide controls button
        if (controlsButton != null) controlsButton.gameObject.SetActive(false);

        // Hide the language heading and all language buttons while controls are shown.
        SetLanguageControlsVisible(false);

        // Hide save and back buttons
        if (saveButton != null) saveButton.gameObject.SetActive(false);
        if (settingsBackButton != null) settingsBackButton.gameObject.SetActive(false);

        // Show controls panel
        if (controlsPanel != null) controlsPanel.SetActive(true);
    }

    public void ShowSettingsPanel()
    {
        // PlayClickSound(); // Commented out - no SoundManager

        // Hide controls panel
        if (controlsPanel != null) controlsPanel.SetActive(false);

        // Show settings controls
        if (volumeSlider != null) volumeSlider.gameObject.SetActive(true);
        if (volumeLabel != null) volumeLabel.gameObject.SetActive(true);
        if (sensitivitySlider != null) sensitivitySlider.gameObject.SetActive(true);
        if (sensitivityLabel != null) sensitivityLabel.gameObject.SetActive(true);
        if (brightnessSlider != null) brightnessSlider.gameObject.SetActive(true);
        if (brightnessLabel != null) brightnessLabel.gameObject.SetActive(true);

        // Show graphics UI
        if (graphicsLabel != null) graphicsLabel.gameObject.SetActive(true);
        if (lowButton != null) lowButton.gameObject.SetActive(true);
        if (mediumButton != null) mediumButton.gameObject.SetActive(true);
        if (highButton != null) highButton.gameObject.SetActive(true);

        // Show controls button
        if (controlsButton != null) controlsButton.gameObject.SetActive(true);

        // Restore the language heading and all language buttons.
        SetLanguageControlsVisible(true);

        // Show save and back buttons
        if (saveButton != null) saveButton.gameObject.SetActive(true);
        if (settingsBackButton != null) settingsBackButton.gameObject.SetActive(true);

        RefreshUI();
    }

    private void SetLanguageControlsVisible(bool visible)
    {
        // The menu scene places the language heading and its buttons under the
        // same parent. Hiding that parent keeps the Controls page uncluttered.
        Transform languageRoot = null;
        if (englishLanguageButton != null) languageRoot = englishLanguageButton.transform.parent;
        else if (koreanLanguageButton != null) languageRoot = koreanLanguageButton.transform.parent;
        else if (tagalogLanguageButton != null) languageRoot = tagalogLanguageButton.transform.parent;

        if (languageRoot != null)
            languageRoot.gameObject.SetActive(visible);
        else
        {
            if (englishLanguageButton != null) englishLanguageButton.gameObject.SetActive(visible);
            if (koreanLanguageButton != null) koreanLanguageButton.gameObject.SetActive(visible);
            if (tagalogLanguageButton != null) tagalogLanguageButton.gameObject.SetActive(visible);
        }
}
}
