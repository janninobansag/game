using UnityEngine;
using UnityEngine.UI;
using TMPro;

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
    
    [Header("Panel References")]
    public GameObject settingsPanel;
    public GameObject controlsPanel;
    
    public Button controlsButton;
    public Button backToSettingsButton;
    public Button saveButton;
    public Button settingsBackButton;

    [Header("Default Settings")]
    public float defaultVolume = 100f;
    public float defaultSensitivity = 2f;
    public float defaultBrightness = 0.3f;
    public int defaultQuality = 2; // 0=Low, 1=Medium, 2=High

    public float currentVolume;
    public float currentSensitivity;
    public float currentBrightness;
    public int currentQuality;

    void Awake()
    {
        LoadAllSettings();
        ApplyAllSettings();
        
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

    private void LoadAllSettings()
    {
        // Load saved volume
        if (PlayerPrefs.HasKey("MasterVolume"))
            currentVolume = PlayerPrefs.GetFloat("MasterVolume");
        else
            currentVolume = defaultVolume;
        
        // Load saved sensitivity
        if (PlayerPrefs.HasKey("MouseSensitivity"))
            currentSensitivity = PlayerPrefs.GetFloat("MouseSensitivity");
        else
            currentSensitivity = defaultSensitivity;
        
        // Load saved brightness
        if (PlayerPrefs.HasKey("Brightness"))
            currentBrightness = PlayerPrefs.GetFloat("Brightness");
        else
            currentBrightness = defaultBrightness;
        
        // Load saved quality
        if (PlayerPrefs.HasKey("QualityLevel"))
            currentQuality = PlayerPrefs.GetInt("QualityLevel");
        else
            currentQuality = defaultQuality;
        
        // Update UI sliders and labels
        if (volumeSlider != null)
            volumeSlider.value = currentVolume;
        UpdateVolumeLabel(currentVolume);
        
        if (sensitivitySlider != null)
            sensitivitySlider.value = currentSensitivity;
        UpdateSensitivityLabel(currentSensitivity);
        
        if (brightnessSlider != null)
            brightnessSlider.value = currentBrightness;
        UpdateBrightnessLabel(currentBrightness);
        
        // Update graphics buttons highlight
        UpdateGraphicsButtonsHighlight();
        
        // Add listeners to sliders
        if (volumeSlider != null)
            volumeSlider.onValueChanged.AddListener(OnVolumeChanged);
        if (sensitivitySlider != null)
            sensitivitySlider.onValueChanged.AddListener(OnSensitivityChanged);
        if (brightnessSlider != null)
            brightnessSlider.onValueChanged.AddListener(OnBrightnessChanged);
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
        
        // Show save and back buttons
        if (saveButton != null) saveButton.gameObject.SetActive(true);
        if (settingsBackButton != null) settingsBackButton.gameObject.SetActive(true);
        
        RefreshUI();
    }

    public void SetQuality(int qualityLevel)
    {
        currentQuality = qualityLevel;
        UpdateGraphicsButtonsHighlight();
        
        // Preview quality
        QualitySettings.SetQualityLevel(qualityLevel);
        
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

    private void RefreshUI()
    {
        currentVolume = PlayerPrefs.GetFloat("MasterVolume", defaultVolume);
        if (volumeSlider != null)
            volumeSlider.value = currentVolume;
        UpdateVolumeLabel(currentVolume);
        
        currentSensitivity = PlayerPrefs.GetFloat("MouseSensitivity", defaultSensitivity);
        if (sensitivitySlider != null)
            sensitivitySlider.value = currentSensitivity;
        UpdateSensitivityLabel(currentSensitivity);
        
        currentBrightness = PlayerPrefs.GetFloat("Brightness", defaultBrightness);
        if (brightnessSlider != null)
            brightnessSlider.value = currentBrightness;
        UpdateBrightnessLabel(currentBrightness);
        
        currentQuality = PlayerPrefs.GetInt("QualityLevel", defaultQuality);
        UpdateGraphicsButtonsHighlight();
    }

    public void SaveSettings()
    {
        // PlayClickSound(); // Commented out - no SoundManager
        
        PlayerPrefs.SetFloat("MasterVolume", currentVolume);
        PlayerPrefs.SetFloat("MouseSensitivity", currentSensitivity);
        PlayerPrefs.SetFloat("Brightness", currentBrightness);
        PlayerPrefs.SetInt("QualityLevel", currentQuality);
        PlayerPrefs.Save();
        
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

    private void UpdateVolumeLabel(float value)
    {
        if (volumeLabel != null)
            volumeLabel.text = $"Volume: {Mathf.RoundToInt(value)}%";
    }

    private void UpdateSensitivityLabel(float value)
    {
        if (sensitivityLabel != null)
            sensitivityLabel.text = $"Sensitivity: {value:F1}";
    }

    private void UpdateBrightnessLabel(float value)
    {
        if (brightnessLabel != null)
            brightnessLabel.text = $"Brightness: {Mathf.RoundToInt(value * 100)}%";
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