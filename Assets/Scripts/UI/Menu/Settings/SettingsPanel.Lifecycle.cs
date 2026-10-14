// PURPOSE: Removes UI listeners when SettingsPanel is destroyed to prevent stale callbacks.
using UnityEngine;
public partial class SettingsPanel : MonoBehaviour
{
    void OnDestroy()
    {
        if (volumeSlider != null)
            volumeSlider.onValueChanged.RemoveListener(OnVolumeChanged);
        if (sensitivitySlider != null)
            sensitivitySlider.onValueChanged.RemoveListener(OnSensitivityChanged);
        if (brightnessSlider != null)
            brightnessSlider.onValueChanged.RemoveListener(OnBrightnessChanged);
        if (controlsButton != null)
            controlsButton.onClick.RemoveListener(ShowControlsPanel);
        if (backToSettingsButton != null)
            backToSettingsButton.onClick.RemoveListener(ShowSettingsPanel);
        if (lowButton != null)
            lowButton.onClick.RemoveAllListeners();
        if (mediumButton != null)
            mediumButton.onClick.RemoveAllListeners();
        if (highButton != null)
            highButton.onClick.RemoveAllListeners();
    }
}
