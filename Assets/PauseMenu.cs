using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;

public class PauseMenu : MonoBehaviour
{
    public static PauseMenu Instance;

    [Header("Panel References")]
    public GameObject pausePanel;
    public GameObject settingsPanel;
    public GameObject crosshair;  // ← ADD THIS - Drag your Crosshair GameObject here

    [Header("Buttons")]
    public Button resumeButton;
    public Button settingsButton;
    public Button quitButton;
    public Button saveButton;
    public Button backButton;

    [Header("Settings UI")]
    public Slider volumeSlider;
    public TextMeshProUGUI volumeLabel;
    public Slider sensitivitySlider;
    public TextMeshProUGUI sensitivityLabel;
    public Slider brightnessSlider;
    public TextMeshProUGUI brightnessLabel;

    [Header("Settings Values")]
    public float defaultVolume = 100f;
    public float defaultSensitivity = 2f;
    public float defaultBrightness = 0.3f;

    public bool isPaused = false;
    private float currentVolume;
    private float currentSensitivity;
    private float currentBrightness;
    private PlayerController playerController;

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
    }

    void Start()
    {
        playerController = FindObjectOfType<PlayerController>();
        LoadSettings();

        if (pausePanel != null) pausePanel.SetActive(false);
        if (settingsPanel != null) settingsPanel.SetActive(false);

        if (resumeButton != null) resumeButton.onClick.AddListener(Resume);
        if (settingsButton != null) settingsButton.onClick.AddListener(OpenSettings);
        if (quitButton != null) quitButton.onClick.AddListener(QuitToMenu);
        if (saveButton != null) saveButton.onClick.AddListener(SaveSettings);
        if (backButton != null) backButton.onClick.AddListener(CloseSettings);

        if (volumeSlider != null)
        {
            volumeSlider.value = currentVolume;
            volumeSlider.onValueChanged.AddListener(OnVolumeChanged);
        }
        if (sensitivitySlider != null)
        {
            sensitivitySlider.value = currentSensitivity;
            sensitivitySlider.onValueChanged.AddListener(OnSensitivityChanged);
        }
        if (brightnessSlider != null)
        {
            brightnessSlider.value = currentBrightness;
            brightnessSlider.onValueChanged.AddListener(OnBrightnessChanged);
        }

        UpdateVolumeLabel(currentVolume);
        UpdateSensitivityLabel(currentSensitivity);
        UpdateBrightnessLabel(currentBrightness);
        
        // Lock cursor at start
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape) || Input.GetKeyDown(KeyCode.P))
        {
            if (isPaused)
                Resume();
            else
                Pause();
        }
    }

    public void Pause()
    {
        isPaused = true;
        Time.timeScale = 0f;
        
        // SHOW CURSOR
        Cursor.visible = true;
        Cursor.lockState = CursorLockMode.None;

        // Disable player control
        if (playerController != null)
            playerController.enabled = false;

        // DISABLE CROSSHAIR WHEN PAUSED
        if (crosshair != null)
            crosshair.SetActive(false);

        // Show pause panel
        if (pausePanel != null)
        {
            pausePanel.SetActive(true);
            BringToFront(pausePanel);
            if (settingsPanel != null)
                settingsPanel.SetActive(false);
        }
    }

    public void Resume()
    {
        isPaused = false;
        Time.timeScale = 1f;
        
        // HIDE CURSOR - Back to game mode
        Cursor.visible = false;
        Cursor.lockState = CursorLockMode.Locked;

        // Re-enable player control
        if (playerController != null)
            playerController.enabled = true;

        // RE-ENABLE CROSSHAIR WHEN RESUMED
        if (crosshair != null)
            crosshair.SetActive(true);

        // Hide panels
        if (pausePanel != null)
            pausePanel.SetActive(false);
        if (settingsPanel != null)
            settingsPanel.SetActive(false);
    }

    public void OpenSettings()
    {
        if (pausePanel != null) pausePanel.SetActive(false);
        if (settingsPanel != null)
        {
            settingsPanel.SetActive(true);
            BringToFront(settingsPanel);
        }
    }

    public void CloseSettings()
    {
        if (settingsPanel != null) settingsPanel.SetActive(false);
        if (pausePanel != null)
        {
            pausePanel.SetActive(true);
            BringToFront(pausePanel);
        }
    }

    private static void BringToFront(GameObject panel)
    {
        // Canvas UI draws later siblings on top. This keeps pause/settings above document UIs.
        panel.transform.SetAsLastSibling();
    }

    public void QuitToMenu()
    {
        // ── ADDED: Save game before quitting to menu ──
        if (SaveSystem.Instance != null)
        {
            SaveSystem.Instance.SaveGame();
        }

        Time.timeScale = 1f;
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
        SceneManager.LoadScene("menu");
    }

    void LoadSettings()
    {
        currentVolume = PlayerPrefs.GetFloat("MasterVolume", defaultVolume);
        currentSensitivity = PlayerPrefs.GetFloat("MouseSensitivity", defaultSensitivity);
        currentBrightness = PlayerPrefs.GetFloat("Brightness", defaultBrightness);

        AudioListener.volume = currentVolume / 100f;
        ApplySensitivity(currentSensitivity);
        ApplyBrightness(currentBrightness);
    }

    void SaveSettings()
    {
        PlayerPrefs.SetFloat("MasterVolume", currentVolume);
        PlayerPrefs.SetFloat("MouseSensitivity", currentSensitivity);
        PlayerPrefs.SetFloat("Brightness", currentBrightness);
        PlayerPrefs.Save();

        AudioListener.volume = currentVolume / 100f;
        ApplySensitivity(currentSensitivity);
        ApplyBrightness(currentBrightness);
    }

    void OnVolumeChanged(float value)
    {
        currentVolume = value;
        UpdateVolumeLabel(value);
        AudioListener.volume = value / 100f;
    }

    void OnSensitivityChanged(float value)
    {
        currentSensitivity = value;
        UpdateSensitivityLabel(value);
        ApplySensitivity(value);
    }

    void OnBrightnessChanged(float value)
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
        if (playerController != null)
            playerController.mouseSensitivity = value;
    }

    private void ApplyBrightness(float value)
    {
        RenderSettings.ambientIntensity = value;
        RenderSettings.reflectionIntensity = value;
    }

    void OnDestroy()
    {
        if (resumeButton != null) resumeButton.onClick.RemoveListener(Resume);
        if (settingsButton != null) settingsButton.onClick.RemoveListener(OpenSettings);
        if (quitButton != null) quitButton.onClick.RemoveListener(QuitToMenu);
        if (saveButton != null) saveButton.onClick.RemoveListener(SaveSettings);
        if (backButton != null) backButton.onClick.RemoveListener(CloseSettings);
        if (volumeSlider != null) volumeSlider.onValueChanged.RemoveListener(OnVolumeChanged);
        if (sensitivitySlider != null) sensitivitySlider.onValueChanged.RemoveListener(OnSensitivityChanged);
        if (brightnessSlider != null) brightnessSlider.onValueChanged.RemoveListener(OnBrightnessChanged);
    }
}
