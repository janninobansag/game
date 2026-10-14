// PURPOSE: Updates graphics quality, slider values, labels, and runtime audio, sensitivity, and brightness previews.
using UnityEngine;
using TMPro;
public partial class SettingsPanel : MonoBehaviour
{
    public void SetQuality(int qualityLevel)
    {
        currentQuality = qualityLevel;
        UpdateGraphicsButtonsHighlight();

        // Preview quality
        QualitySettings.SetQualityLevel(qualityLevel);
        ApplyFrameRateCap();

        // PlayClickSound(); // Commented out - no SoundManager
    }

    private void UpdateGraphicsButtonsHighlight()
    {
        // Colors
        Color normalColor = new Color(1f, 1f, 1f, 1f);
        Color selectedColor = new Color(0.5f, 0.2f, 0.7f, 1f);
        Color textNormal = new Color(1f, 1f, 1f, 1f);
        Color textSelected = new Color(0.486f, 0.051f, 0.027f, 1f);

        // Update Low Button
        if (lowButton != null)
        {
            var colors = lowButton.colors;
            if (currentQuality == 0)
            {
                colors.normalColor = selectedColor;
                colors.selectedColor = selectedColor;
                lowButton.colors = colors;

                var text = lowButton.GetComponentInChildren<TextMeshProUGUI>();
                if (text != null) text.color = textSelected;
            }
            else
            {
                colors.normalColor = normalColor;
                colors.selectedColor = normalColor;
                lowButton.colors = colors;

                var text = lowButton.GetComponentInChildren<TextMeshProUGUI>();
                if (text != null) text.color = textNormal;
            }
        }

        // Update Medium Button
        if (mediumButton != null)
        {
            var colors = mediumButton.colors;
            if (currentQuality == 1)
            {
                colors.normalColor = selectedColor;
                colors.selectedColor = selectedColor;
                mediumButton.colors = colors;

                var text = mediumButton.GetComponentInChildren<TextMeshProUGUI>();
                if (text != null) text.color = textSelected;
            }
            else
            {
                colors.normalColor = normalColor;
                colors.selectedColor = normalColor;
                mediumButton.colors = colors;

                var text = mediumButton.GetComponentInChildren<TextMeshProUGUI>();
                if (text != null) text.color = textNormal;
            }
        }

        // Update High Button
        if (highButton != null)
        {
            var colors = highButton.colors;
            if (currentQuality == 2)
            {
                colors.normalColor = selectedColor;
                colors.selectedColor = selectedColor;
                highButton.colors = colors;

                var text = highButton.GetComponentInChildren<TextMeshProUGUI>();
                if (text != null) text.color = textSelected;
            }
            else
            {
                colors.normalColor = normalColor;
                colors.selectedColor = normalColor;
                highButton.colors = colors;

                var text = highButton.GetComponentInChildren<TextMeshProUGUI>();
                if (text != null) text.color = textNormal;
            }
        }
    }

    private void CaptureCurrentSliderValues()
    {
        // Slider values are the current UI source of truth while the panel is open.
        // Capture them before changing language so labels never fall back to stale data.
        if (volumeSlider != null) currentVolume = volumeSlider.value;
        if (sensitivitySlider != null) currentSensitivity = sensitivitySlider.value;
        if (brightnessSlider != null) currentBrightness = brightnessSlider.value;
    }
    private void RefreshUI()
    {
        // Keep the player's current slider values. Re-reading PlayerPrefs here
        // discarded unsaved changes when opening settings or choosing a language.
        if (volumeSlider != null)
            volumeSlider.SetValueWithoutNotify(currentVolume);
        UpdateVolumeLabel(currentVolume);

        if (sensitivitySlider != null)
            sensitivitySlider.SetValueWithoutNotify(currentSensitivity);
        UpdateSensitivityLabel(currentSensitivity);

        if (brightnessSlider != null)
            brightnessSlider.SetValueWithoutNotify(currentBrightness);
        UpdateBrightnessLabel(currentBrightness);

        UpdateGraphicsButtonsHighlight();
    }
    public void SaveSettings()
    {
        // Save exactly what is currently visible in the three sliders.
        CaptureCurrentSliderValues();
        SaveSettingsToDatabase();
        SyncPlayerPrefsCache();
        ApplyAllSettings();
    }
    public void OnVolumeChanged(float value)
    {
        currentVolume = value;
        UpdateVolumeLabel(value);
        AudioListener.volume = value / 100f;
    }

    public void OnSensitivityChanged(float value)
    {
        currentSensitivity = value;
        UpdateSensitivityLabel(value);
        ApplySensitivity(value);
    }

    public void OnBrightnessChanged(float value)
    {
        currentBrightness = value;
        UpdateBrightnessLabel(value);
        ApplyBrightness(value);
    }
}
