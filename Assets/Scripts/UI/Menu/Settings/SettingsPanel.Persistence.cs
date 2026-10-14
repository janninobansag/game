// PURPOSE: Loads, saves, repairs, caches, and applies the player settings stored in SettingsDatabase and PlayerPrefs.
using UnityEngine;
public partial class SettingsPanel : MonoBehaviour
{
    private void LoadAllSettings()
    {
        SettingsData savedSettings;
        if (SettingsDatabase.TryLoad(out savedSettings))
        {
            currentVolume = savedSettings.Volume;
            currentSensitivity = savedSettings.Sensitivity;
            currentBrightness = savedSettings.Brightness;
            currentQuality = savedSettings.QualityLevel;
            currentLanguage = Mathf.Clamp(savedSettings.Language, 0, 2);
        }
        else
        {
            // One-time migration of settings saved by older game versions.
            LoadLegacyPlayerPrefs();
            SaveSettingsToDatabase();
        }

        // Compatibility cache for older scripts. SettingsData is the saved source.
        SyncPlayerPrefsCache();
        UpdateLanguageButtonsHighlight();

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

        if (volumeSlider != null)
            volumeSlider.onValueChanged.AddListener(OnVolumeChanged);
        if (sensitivitySlider != null)
            sensitivitySlider.onValueChanged.AddListener(OnSensitivityChanged);
        if (brightnessSlider != null)
            brightnessSlider.onValueChanged.AddListener(OnBrightnessChanged);
    }

    public void RestoreSavedSettings()
    {
        SettingsData savedSettings;
        if (SettingsDatabase.TryLoad(out savedSettings))
        {
            currentVolume = savedSettings.Volume;
            currentSensitivity = savedSettings.Sensitivity;
            currentBrightness = savedSettings.Brightness;
            currentQuality = savedSettings.QualityLevel;
            currentLanguage = Mathf.Clamp(savedSettings.Language, 0, 2);
        }
        else
        {
            // Older installs may only have the legacy PlayerPrefs values.
            LoadLegacyPlayerPrefs();
        }

        RefreshUI();
        UpdateLanguageButtonsHighlight();
        ApplyAllSettings();
        ApplyFrameRateCap();
        ApplySelectedLanguage();
    }
    private void LoadLegacyPlayerPrefs()
    {
        RepairLegacyZeroSettings();
        currentVolume = PlayerPrefs.GetFloat("MasterVolume", defaultVolume);
        currentSensitivity = PlayerPrefs.GetFloat("MouseSensitivity", defaultSensitivity);
        currentBrightness = PlayerPrefs.GetFloat("Brightness", defaultBrightness);
        currentQuality = PlayerPrefs.GetInt("QualityLevel", defaultQuality);
        currentLanguage = Mathf.Clamp(PlayerPrefs.GetInt("GameLanguage", defaultLanguage), 0, 2);
    }

    private void SaveSettingsToDatabase()
    {
        SettingsDatabase.Save(new SettingsData
        {
            Volume = currentVolume,
            Sensitivity = currentSensitivity,
            Brightness = currentBrightness,
            QualityLevel = currentQuality,
            Language = currentLanguage
        });
    }

    private void SyncPlayerPrefsCache()
    {
        PlayerPrefs.SetFloat("MasterVolume", currentVolume);
        PlayerPrefs.SetFloat("MouseSensitivity", currentSensitivity);
        PlayerPrefs.SetFloat("Brightness", currentBrightness);
        PlayerPrefs.SetInt("QualityLevel", currentQuality);
        PlayerPrefs.SetInt("GameLanguage", currentLanguage);
        PlayerPrefs.Save();
    }
    private void RepairLegacyZeroSettings()
    {
        const int repairedSettingsVersion = 2;
        if (PlayerPrefs.GetInt("SettingsRepairVersion", 0) >= repairedSettingsVersion)
            return;

        bool hasAllSettings = PlayerPrefs.HasKey("MasterVolume") &&
                              PlayerPrefs.HasKey("MouseSensitivity") &&
                              PlayerPrefs.HasKey("Brightness");
        bool allAreZero = hasAllSettings &&
                          Mathf.Approximately(PlayerPrefs.GetFloat("MasterVolume"), 0f) &&
                          Mathf.Approximately(PlayerPrefs.GetFloat("MouseSensitivity"), 0f) &&
                          Mathf.Approximately(PlayerPrefs.GetFloat("Brightness"), 0f);

        // Older language switching could save a full set of invalid zero values.
        // Repair that exact legacy state once; deliberately muted individual values remain valid.
        if (allAreZero)
        {
            PlayerPrefs.SetFloat("MasterVolume", defaultVolume);
            PlayerPrefs.SetFloat("MouseSensitivity", defaultSensitivity);
            PlayerPrefs.SetFloat("Brightness", defaultBrightness);
        }

        PlayerPrefs.SetInt("SettingsRepairVersion", repairedSettingsVersion);
        PlayerPrefs.Save();
    }
    private void ApplyAllSettings()
    {
        // Apply volume
        AudioListener.volume = currentVolume / 100f;

        // Apply sensitivity
        ApplySensitivity(currentSensitivity);

        // Apply brightness
        ApplyBrightness(currentBrightness);

        // Apply quality
        QualitySettings.SetQualityLevel(currentQuality);

    }

    private static void ApplyFrameRateCap()
    {
        // Do not cap capable PCs. The quality profiles still target stable performance on lower-end hardware.
        QualitySettings.vSyncCount = 0;
        Application.targetFrameRate = -1;
    }
}
