using UnityEngine;
using UnityEngine.UI;
using TMPro;

public enum GameLanguage
{
    English = 0,
    Korean = 1,
    Tagalog = 2
}
public class SettingsPanel : MonoBehaviour
{
    [Header("UI References")]
    public Slider volumeSlider;
    public TextMeshProUGUI volumeLabel;
    
    public Slider sensitivitySlider;
    public TextMeshProUGUI sensitivityLabel;
    
    public Slider brightnessSlider;
    public TextMeshProUGUI brightnessLabel;
    
    [Header("Graphics Settings")]
    public Button lowButton;
    public Button mediumButton;
    public Button highButton;
    public TextMeshProUGUI graphicsLabel;

    [Header("Language Settings")]
    public Button englishLanguageButton;
    public Button koreanLanguageButton;
    public Button tagalogLanguageButton;
    [Tooltip("Your dynamic NotoSansKR TMP Font Asset. It is used while Korean is selected.")]
    public TMP_FontAsset koreanFont;
    [Range(0, 2)] public int defaultLanguage = 0;
    [HideInInspector] public int currentLanguage;
    
    [Header("Panel References")]
    public GameObject settingsPanel;
    public GameObject controlsPanel;
    [Tooltip("Hide only the visual settings window when the menu scene starts.")]
    public bool hideSettingsPanelOnStart = true;
    
    public Button controlsButton;
    public Button backToSettingsButton;
    public Button saveButton;
    public Button settingsBackButton;

    [Header("Default Settings")]
    public float defaultVolume = 100f;
    public float defaultSensitivity = 5f;
    public float defaultBrightness = 0.5f;
    public int defaultQuality = 2; // 0=Low, 1=Medium, 2=High

    public float currentVolume;
    public float currentSensitivity;
    public float currentBrightness;
    public int currentQuality;

    void Awake()
    {
        LoadAllSettings();
        ApplyAllSettings();
        ApplyFrameRateCap();
        ApplySelectedLanguage();
        
        // Setup button listeners
        if (controlsButton != null)
            controlsButton.onClick.AddListener(ShowControlsPanel);
        
        if (backToSettingsButton != null)
            backToSettingsButton.onClick.AddListener(ShowSettingsPanel);
        
        // Setup graphics button listeners
        if (lowButton != null)
            lowButton.onClick.AddListener(() => SetQuality(0));
        if (mediumButton != null)
            mediumButton.onClick.AddListener(() => SetQuality(1));
        if (highButton != null)
            highButton.onClick.AddListener(() => SetQuality(2));
    }

    private void Start()
    {
        // Keep this manager active so saved settings continue loading.
        // Only the visual SettingsPanel is hidden until the player presses Settings.
        if (hideSettingsPanelOnStart && settingsPanel != null)
            settingsPanel.SetActive(false);
    }
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

    public void SetLanguage(int languageIndex)
    {
        CaptureCurrentSliderValues();
        currentLanguage = Mathf.Clamp(languageIndex, 0, 2);
        SaveSettingsToDatabase();
        SyncPlayerPrefsCache();
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
                ? $"\uBCFC\uB968: {Mathf.RoundToInt(displayedValue)}%"
                : GetSelectedLanguage() == GameLanguage.Tagalog
                    ? $"Lakas ng tunog: {Mathf.RoundToInt(displayedValue)}%"
                    : $"Volume: {Mathf.RoundToInt(displayedValue)}%";
    }

    private void UpdateSensitivityLabel(float value)
    {
        float displayedValue = sensitivitySlider != null ? sensitivitySlider.value : value;
        if (sensitivityLabel != null)
            sensitivityLabel.text = GetSelectedLanguage() == GameLanguage.Korean && koreanFont != null
                ? $"\uAC10\uB3C4: {displayedValue:F1}"
                : GetSelectedLanguage() == GameLanguage.Tagalog
                    ? $"Sensitibidad: {displayedValue:F1}"
                    : $"Sensitivity: {displayedValue:F1}";
    }

    private void UpdateBrightnessLabel(float value)
    {
        float displayedValue = brightnessSlider != null ? brightnessSlider.value : value;
        if (brightnessLabel != null)
            brightnessLabel.text = GetSelectedLanguage() == GameLanguage.Korean && koreanFont != null
                ? $"\uBC1D\uAE30: {Mathf.RoundToInt(displayedValue * 100)}%"
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
        RenderSettings.ambientIntensity = value;
        RenderSettings.reflectionIntensity = value;
    }

    private void PlayClickSound()
    {
        // SoundManager removed - sounds handled by ButtonSound.cs
        // If you want click sounds, add ButtonSound component to each button
    }

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