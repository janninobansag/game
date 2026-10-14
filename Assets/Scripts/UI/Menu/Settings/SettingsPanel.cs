// PURPOSE: Defines the SettingsPanel component, its Inspector references, default values, and startup listener setup.
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public enum GameLanguage
{
    English = 0,
    Korean = 1,
    Tagalog = 2
}

public partial class SettingsPanel : MonoBehaviour
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
}
