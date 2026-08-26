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

    [Header("Hover Feedback")]
    public AudioClip hoverSound;
    [Range(0f, 1f)] public float hoverVolume = 0.6f;

    public bool isPaused = false;
    private float currentVolume;
    private float currentSensitivity;
    private float currentBrightness;
    private PlayerController playerController;
    private AudioSource hoverAudioSource;
    private Button hoveredPauseButton;
    private Slider draggedSettingsSlider;

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
        hoverAudioSource = gameObject.AddComponent<AudioSource>();
        hoverAudioSource.playOnAwake = false;
        hoverAudioSource.ignoreListenerPause = true;
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
        if (isPaused)
        {
            UpdatePauseMenuHover();
            UpdateSettingsSliderDrag();
        }

        if (isPaused && Input.GetMouseButtonUp(0))
            ClickPauseMenuButtonAtCursor();

        if (Input.GetKeyDown(KeyCode.Escape) || Input.GetKeyDown(KeyCode.P))
        {
            if (isPaused)
                Resume();
            else
                Pause();
        }
    }




    private void UpdateSettingsSliderDrag()
    {
        if (settingsPanel == null || !settingsPanel.activeInHierarchy)
        {
            draggedSettingsSlider = null;
            return;
        }

        Canvas canvas = settingsPanel.GetComponent<Canvas>();
        Camera eventCamera = canvas != null && canvas.renderMode != RenderMode.ScreenSpaceOverlay
            ? canvas.worldCamera
            : null;

        if (Input.GetMouseButtonDown(0))
        {
            foreach (Slider slider in settingsPanel.GetComponentsInChildren<Slider>(true))
            {
                if (slider.gameObject.activeInHierarchy && slider.interactable &&
                    RectTransformUtility.RectangleContainsScreenPoint(slider.transform as RectTransform, Input.mousePosition, eventCamera))
                {
                    draggedSettingsSlider = slider;
                    SetSliderFromCursor(slider, eventCamera);
                    break;
                }
            }
        }

        if (draggedSettingsSlider != null && Input.GetMouseButton(0))
            SetSliderFromCursor(draggedSettingsSlider, eventCamera);

        if (Input.GetMouseButtonUp(0))
            draggedSettingsSlider = null;
    }

    private static void SetSliderFromCursor(Slider slider, Camera eventCamera)
    {
        RectTransform sliderRect = slider.transform as RectTransform;
        if (sliderRect == null || !RectTransformUtility.ScreenPointToLocalPointInRectangle(sliderRect, Input.mousePosition, eventCamera, out Vector2 localPoint))
            return;

        float normalizedValue = Mathf.InverseLerp(sliderRect.rect.xMin, sliderRect.rect.xMax, localPoint.x);
        if (slider.direction == Slider.Direction.RightToLeft)
            normalizedValue = 1f - normalizedValue;
        slider.normalizedValue = normalizedValue;
    }
    private void UpdatePauseMenuHover()
    {
        GameObject activePanel = settingsPanel != null && settingsPanel.activeInHierarchy
            ? settingsPanel
            : pausePanel;
        if (activePanel == null || !activePanel.activeInHierarchy)
            return;

        Canvas canvas = activePanel.GetComponent<Canvas>();
        Camera eventCamera = canvas != null && canvas.renderMode != RenderMode.ScreenSpaceOverlay
            ? canvas.worldCamera
            : null;
        Button newHoveredButton = null;

        foreach (Button button in activePanel.GetComponentsInChildren<Button>(true))
        {
            if (button.gameObject.activeInHierarchy && button.interactable &&
                RectTransformUtility.RectangleContainsScreenPoint(button.transform as RectTransform, Input.mousePosition, eventCamera))
            {
                newHoveredButton = button;
                break;
            }
        }

        if (newHoveredButton == hoveredPauseButton)
            return;

        SetButtonHoverVisual(hoveredPauseButton, false);
        hoveredPauseButton = newHoveredButton;
        SetButtonHoverVisual(hoveredPauseButton, true);

        if (hoveredPauseButton != null && hoverSound != null && hoverAudioSource != null)
            hoverAudioSource.PlayOneShot(hoverSound, hoverVolume);
    }

    private static void SetButtonHoverVisual(Button button, bool hovered)
    {
        if (button == null || button.targetGraphic == null)
            return;

        Color color = hovered ? button.colors.highlightedColor : button.colors.normalColor;
        button.targetGraphic.CrossFadeColor(color * button.colors.colorMultiplier, button.colors.fadeDuration, true, true);
    }
    private void ClickPauseMenuButtonAtCursor()
    {
        GameObject activePanel = settingsPanel != null && settingsPanel.activeInHierarchy
            ? settingsPanel
            : pausePanel;
        if (activePanel == null || !activePanel.activeInHierarchy)
            return;

        Canvas canvas = activePanel.GetComponent<Canvas>();
        Camera eventCamera = canvas != null && canvas.renderMode != RenderMode.ScreenSpaceOverlay
            ? canvas.worldCamera
            : null;

        foreach (Button button in activePanel.GetComponentsInChildren<Button>(true))
        {
            if (!button.gameObject.activeInHierarchy || !button.interactable)
                continue;

            if (RectTransformUtility.RectangleContainsScreenPoint(button.transform as RectTransform, Input.mousePosition, eventCamera))
            {
                button.onClick.Invoke();
                return;
            }
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
            PreparePanelForInput(pausePanel);
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

        // Do not re-enable movement if the story intro is still playing.
        StoryIntro storyIntro = FindObjectOfType<StoryIntro>();
        bool storyIntroActive = storyIntro != null && storyIntro.IsIntroActive;
        if (playerController != null && !storyIntroActive)
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
            PreparePanelForInput(settingsPanel);
            BringToFront(settingsPanel);
        }
    }

    public void CloseSettings()
    {
        if (settingsPanel != null) settingsPanel.SetActive(false);
        if (pausePanel != null)
        {
            pausePanel.SetActive(true);
            PreparePanelForInput(pausePanel);
            BringToFront(pausePanel);
        }
    }


    private static void PreparePanelForInput(GameObject panel)
    {
        Canvas panelCanvas = panel.GetComponent<Canvas>();
        if (panelCanvas == null)
            panelCanvas = panel.AddComponent<Canvas>();
        panelCanvas.overrideSorting = true;
        panelCanvas.sortingOrder = 1000;

        GraphicRaycaster raycaster = panel.GetComponent<GraphicRaycaster>();
        if (raycaster == null)
            raycaster = panel.AddComponent<GraphicRaycaster>();
        raycaster.enabled = true;

        CanvasGroup canvasGroup = panel.GetComponent<CanvasGroup>();
        if (canvasGroup == null)
            canvasGroup = panel.AddComponent<CanvasGroup>();

        canvasGroup.interactable = true;
        canvasGroup.blocksRaycasts = true;

        // A full-screen Image or text label can otherwise intercept clicks intended
        // for controls behind it. Only selectable controls should receive raycasts.
        foreach (Graphic graphic in panel.GetComponentsInChildren<Graphic>(true))
        {
            if (graphic.GetComponent<Selectable>() == null)
                graphic.raycastTarget = false;
        }

        foreach (Selectable selectable in panel.GetComponentsInChildren<Selectable>(true))
            selectable.interactable = true;
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
