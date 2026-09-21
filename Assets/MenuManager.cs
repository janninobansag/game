using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using System.IO;
using SQLite4Unity3d;
using TMPro;
using UnityEngine.UI;

[ExecuteAlways]
public class MenuManager : MonoBehaviour
{
    [Header("Audio")]
    public AudioClip hoverSound;
    [Range(0f, 1f)]
    public float hoverVolume = 0.6f;
    public AudioClip clickSound;
    [Range(0f, 1f)]
    public float clickVolume = 0.8f;
    public SettingsPanel settingsPanel;
    public GameObject loadButton;

    public GameObject difficultyPanel;

    [Header("About Panel")]
    public string aboutTitle = "VAREN";
    [TextArea(3, 8)]
    public string aboutText = "A first-person Philippine-forest horror game made in Unity.\n\nExplore Malawak Forest, uncover the fate of its abandoned village, and survive spirits inspired by Philippine folklore.\n\nLearn the legends. Survive the darkness.\n\nCreated by Bansag";
    [SerializeField] private GameObject aboutPanel;
    [SerializeField] private TMP_FontAsset aboutHeadingFont;
    [SerializeField] private TMP_FontAsset aboutBodyFont;
    private Button aboutBackButton;
    private bool aboutEditableTextCaptured;
    private string editableAboutTitle;
    private string editableAboutContent;
    private string editableAboutBackLabel;

    // ── Load Panel ──
    [Header("Load Panel")]
    public GameObject loadPanel;           // The panel that shows save options
    public GameObject normalLoadButton;    // Button to load Normal mode save
    public GameObject hardLoadButton;      // Button to load Hard mode save
    public GameObject backFromLoadButton;  // Button to close load panel

    // ── NEW: Progression Display ──
    [Header("Progression Display")]
    public TextMeshProUGUI normalProgressText;   // Drag your Normal mode progress Text here
    public TextMeshProUGUI hardProgressText;     // Drag your Hard mode progress Text here

    private AudioSource audioSource;
    private bool isLoading = false;
    private AsyncOperation loadingOperation;

    void Start()
    {
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
        
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
        }
        audioSource.spatialBlend = 0f;
        
        float savedVolume = PlayerPrefs.GetFloat("MasterVolume", 100f);
        AudioListener.volume = savedVolume / 100f;

        if (difficultyPanel != null)
            difficultyPanel.SetActive(false);

        // ── Hide load panel at start ──
        if (loadPanel != null)
            loadPanel.SetActive(false);


        CreateAboutPanelIfNeeded();
        BindAboutBackButton();
        if (aboutPanel != null)
            aboutPanel.SetActive(false);
        UpdateLoadButtonVisibility();
        UpdateLoadButtonsVisibility();
        LoadAndDisplayProgression();
    }

    public void UpdateLoadButtonVisibility()
    {
        if (loadButton != null)
        {
            loadButton.SetActive(HasAnySaveFile());
        }
    }

    // ── Check if ANY save file exists (Normal or Hard) ──
    private bool HasAnySaveFile()
    {
        string normalPath = Path.Combine(Application.persistentDataPath, "gameSave_v2.db");
        string hardPath = Path.Combine(Application.persistentDataPath, "gameSave_Hard_v2.db");
        
        bool normalExists = File.Exists(normalPath);
        bool hardExists = File.Exists(hardPath);
        return normalExists || hardExists;
    }

    private bool CanLoadSave(string fileName)
    {
        string savePath = Path.Combine(Application.persistentDataPath, fileName);
        return File.Exists(savePath) && GetProgressFromDatabase(savePath) < 100;
    }

    // ── Check if Normal save exists ──
    private bool HasNormalSave()
    {
        string normalPath = Path.Combine(Application.persistentDataPath, "gameSave_v2.db");
        bool exists = File.Exists(normalPath);
        return exists;
    }

    // ── Check if Hard save exists ──
    private bool HasHardSave()
    {
        string hardPath = Path.Combine(Application.persistentDataPath, "gameSave_Hard_v2.db");
        bool exists = File.Exists(hardPath);
        return exists;
    }

    // ── Update visibility of Normal and Hard load buttons ──
    public void UpdateLoadButtonsVisibility()
    {
        // Completed saves remain visible but cannot be selected.
        bool hasNormalSave = HasNormalSave();
        bool canLoadNormalSave = CanLoadSave("gameSave_v2.db");
        
        // Check if Hard mode save exists
        bool hasHardSave = HasHardSave();
        bool canLoadHardSave = CanLoadSave("gameSave_Hard_v2.db");

        if (normalLoadButton != null)
        {
            normalLoadButton.SetActive(hasNormalSave);
            Button normalButton = normalLoadButton.GetComponent<Button>();
            if (normalButton != null)
                normalButton.interactable = canLoadNormalSave;
        }

        if (hardLoadButton != null)
        {
            hardLoadButton.SetActive(hasHardSave);
            Button hardButton = hardLoadButton.GetComponent<Button>();
            if (hardButton != null)
                hardButton.interactable = canLoadHardSave;
        }

        // ── Update progression text when buttons update ──
        LoadAndDisplayProgression();
    }

    // ── NEW: Load and display progression for both modes ──
    private void LoadAndDisplayProgression()
    {
        // ── Load Normal mode progression ──
        if (normalProgressText != null)
        {
            string normalPath = Path.Combine(Application.persistentDataPath, "gameSave_v2.db");
            int normalProgress = GetProgressFromDatabase(normalPath);
            
            if (normalProgress >= 100)
            {
                normalProgressText.text = "Completed";
                normalProgressText.color = new Color(0.5f, 0.85f, 0.5f, 1f);
            }
            else if (normalProgress > 0)
            {
                normalProgressText.text = $"Progress: {normalProgress}%";
                normalProgressText.color = Color.white;
            }
            else
            {
                normalProgressText.text = "No Save";
                normalProgressText.color = new Color(0.5f, 0.5f, 0.5f, 0.7f);
            }
        }

        // ── Load Hard mode progression ──
        if (hardProgressText != null)
        {
            string hardPath = Path.Combine(Application.persistentDataPath, "gameSave_Hard_v2.db");
            int hardProgress = GetProgressFromDatabase(hardPath);
            
            if (hardProgress >= 100)
            {
                hardProgressText.text = "Completed";
                hardProgressText.color = new Color(0.5f, 0.85f, 0.5f, 1f);
            }
            else if (hardProgress > 0)
            {
                hardProgressText.text = $"Progress: {hardProgress}%";
                hardProgressText.color = Color.white;
            }
            else
            {
                hardProgressText.text = "No Save";
                hardProgressText.color = new Color(0.5f, 0.5f, 0.5f, 0.7f);
            }
        }
    }

    // ── NEW: Get progress percentage from a database file ──
    private int GetProgressFromDatabase(string dbPath)
    {
        if (!File.Exists(dbPath))
        {
            return 0;
        }

        try
        {
            var connection = new SQLiteConnection(dbPath, SQLiteOpenFlags.ReadOnly);
            var data = connection.Table<ProgressionData>().FirstOrDefault();
            connection.Close();

            if (data != null && data.TotalPoints > 0)
            {
                // Calculate percentage
                float percent = (float)data.ProgressValue / data.TotalPoints * 100f;
                return Mathf.RoundToInt(percent);
            }
        }
        catch (System.Exception)
        {
        }

        return 0;
    }

    // ── Open Load Panel ──
    public void OpenLoadPanel()
    {
        PlayClickSound();
        
        // Update button visibility before showing panel
        UpdateLoadButtonsVisibility();
        
        // ── Load progression when panel opens ──
        LoadAndDisplayProgression();
        
        if (loadPanel != null)
            loadPanel.SetActive(true);
        
        if (difficultyPanel != null)
            difficultyPanel.SetActive(false);
    }

    // ── Close Load Panel ──
    public void CloseLoadPanel()
    {
        PlayClickSound();
        
        if (loadPanel != null)
            loadPanel.SetActive(false);

    }

    // ── Load Normal save ──
    public void LoadNormalSave()
    {
        PlayClickSound();

        string normalPath = Path.Combine(Application.persistentDataPath, "gameSave_v2.db");
        
        if (CanLoadSave("gameSave_v2.db"))
        {
            
            // Set the difficulty so SaveSystem loads the correct database
            PlayerPrefs.SetString("GameDifficulty", "Normal");
            PlayerPrefs.SetInt("ShouldLoadSave", 1);
            PlayerPrefs.SetInt("SkipIntro", 1);
            PlayerPrefs.SetString("SavedScene", "chapter 1");
            PlayerPrefs.Save();
            
            if (loadPanel != null)
            loadPanel.SetActive(false);

            
            // Force reload SaveSystem to use Normal database
            if (SaveSystem.Instance != null)
            {
                SaveSystem.Instance.ReinitializeDatabase();
            }
            
            SceneFader.Instance.FadeToScene("chapter 1");
        }
        else
        {
        }
    }

    // ── Load Hard save ──
    public void LoadHardSave()
    {
        PlayClickSound();

        string hardPath = Path.Combine(Application.persistentDataPath, "gameSave_Hard_v2.db");
        
        if (CanLoadSave("gameSave_Hard_v2.db"))
        {
            
            // Set the difficulty so SaveSystem loads the correct database
            PlayerPrefs.SetString("GameDifficulty", "Hard");
            PlayerPrefs.SetInt("ShouldLoadSave", 1);
            PlayerPrefs.SetInt("SkipIntro", 1);
            PlayerPrefs.SetString("SavedScene", "chapter 2");
            PlayerPrefs.Save();
            
            if (loadPanel != null)
            loadPanel.SetActive(false);

            
            // Force reload SaveSystem to use Hard database
            if (SaveSystem.Instance != null)
            {
                SaveSystem.Instance.ReinitializeDatabase();
            }
            
            SceneFader.Instance.FadeToScene("chapter 2");
        }
        else
        {
        }
    }

    public void PlayGame()
    {
        PlayClickSound();
        
        if (difficultyPanel != null)
        {
            difficultyPanel.SetActive(true);
        }
    }

    public void StartNormal()
    {
        PlayClickSound();
        ProgressionTrigger.ClearSavedStates();
        
        if (difficultyPanel != null)
            difficultyPanel.SetActive(false);

        // ── Clear all data when starting a new game ──
        if (SaveSystem.Instance != null)
        {
            SaveSystem.Instance.ClearKeyData();
            SaveSystem.Instance.ClearBatteryData();
            SaveSystem.Instance.ClearRitualItemData();
        }

        // ── Reset all PickupTriggerSpawner states ──
        PickupTriggerSpawner[] spawners = FindObjectsOfType<PickupTriggerSpawner>();
        foreach (PickupTriggerSpawner spawner in spawners)
        {
            spawner.ResetActivation();
        }

        // ── Reset ALL PlayerPrefs keys ──
        PlayerPrefs.SetInt("CandleOneRevealed", 0);
        PlayerPrefs.SetInt("BibleSpawner", 0);
        PlayerPrefs.SetInt("CrossSpawner", 0);
        PlayerPrefs.SetInt("ShouldLoadSave", 0);
        PlayerPrefs.SetInt("SkipIntro", 0);
        PlayerPrefs.SetString("GameDifficulty", "Normal");
        PlayerPrefs.SetString("SavedScene", "chapter 1");
        PlayerPrefs.SetString("SaveTime", System.DateTime.Now.ToString("MM/dd/yyyy HH:mm"));
        PlayerPrefs.DeleteKey("GameProgress");
        // A fresh game must choose a new Mansion key location.
        PlayerPrefs.DeleteKey("MansionKeySpawnIndex_Normal");
        PlayerPrefs.DeleteKey("MansionKeySpawnIndex_Hard");
        PlayerPrefs.DeleteKey("FlashlightSpawnIndex_Normal");
        PlayerPrefs.DeleteKey("FlashlightSpawnIndex_Hard");
        PlayerPrefs.Save();
        
        // Force SaveSystem to use Normal database
        if (SaveSystem.Instance != null)
        {
            SaveSystem.Instance.ReinitializeDatabase();
            SaveSystem.Instance.ClearNewGameWorldState();
            SaveSystem.Instance.ClearProgressionData();
            SaveSystem.Instance.ClearStoryIntroData();
            SaveSystem.Instance.ClearSubtitleData();
        }
        else
        {
            ClearProgressionDataFromFile("gameSave_v2.db");
        }
        
        SceneFader.Instance.FadeToScene("chapter 1");
    }

    public void StartHard()
    {
        PlayClickSound();
        ProgressionTrigger.ClearSavedStates();
        
        if (difficultyPanel != null)
            difficultyPanel.SetActive(false);

        // ── Clear all data when starting a new game ──
        if (SaveSystem.Instance != null)
        {
            SaveSystem.Instance.ClearKeyData();
            SaveSystem.Instance.ClearBatteryData();
            SaveSystem.Instance.ClearRitualItemData();
        }

        // ── Reset all PickupTriggerSpawner states ──
        PickupTriggerSpawner[] spawners = FindObjectsOfType<PickupTriggerSpawner>();
        foreach (PickupTriggerSpawner spawner in spawners)
        {
            spawner.ResetActivation();
        }

        // ── Reset ALL PlayerPrefs keys ──
        PlayerPrefs.SetInt("CandleOneRevealed", 0);
        PlayerPrefs.SetInt("BibleSpawner", 0);
        PlayerPrefs.SetInt("CrossSpawner", 0);
        PlayerPrefs.SetInt("ShouldLoadSave", 0);
        PlayerPrefs.SetInt("SkipIntro", 0);
        PlayerPrefs.SetString("GameDifficulty", "Hard");
        PlayerPrefs.SetString("SavedScene", "chapter 2");
        PlayerPrefs.SetString("SaveTime", System.DateTime.Now.ToString("MM/dd/yyyy HH:mm"));
        PlayerPrefs.DeleteKey("GameProgress");
        // A fresh game must choose a new Mansion key location.
        PlayerPrefs.DeleteKey("MansionKeySpawnIndex_Normal");
        PlayerPrefs.DeleteKey("MansionKeySpawnIndex_Hard");
        PlayerPrefs.DeleteKey("FlashlightSpawnIndex_Normal");
        PlayerPrefs.DeleteKey("FlashlightSpawnIndex_Hard");
        PlayerPrefs.Save();
        
        // Force SaveSystem to use Hard database
        if (SaveSystem.Instance != null)
        {
            SaveSystem.Instance.ReinitializeDatabase();
            SaveSystem.Instance.ClearNewGameWorldState();
            SaveSystem.Instance.ClearProgressionData();
            SaveSystem.Instance.ClearStoryIntroData();
            SaveSystem.Instance.ClearSubtitleData();
        }
        else
        {
            ClearProgressionDataFromFile("gameSave_Hard_v2.db");
        }
        
        SceneFader.Instance.FadeToScene("chapter 2");
    }

    private void ClearProgressionDataFromFile(string fileName)
    {
        string dbPath = Path.Combine(Application.persistentDataPath, fileName);

        if (!File.Exists(dbPath))
            return;

        try
        {
            using (var connection = new SQLiteConnection(dbPath, SQLiteOpenFlags.ReadWrite))
            {
                connection.CreateTable<AIPositionData>();
                connection.DeleteAll<ProgressionData>();
                connection.DeleteAll<AIPositionData>();
                connection.DeleteAll<DroppedItemData>();
                connection.DeleteAll<DrawerData>();

                if (fileName == "gameSave_Hard_v2.db")
                {
                    connection.CreateTable<WrenchData>();
                    connection.CreateTable<GeneratorCoverData>();
                    connection.CreateTable<GasData>();
                    connection.DeleteAll<WrenchData>();
                    connection.DeleteAll<GeneratorCoverData>();
                    connection.DeleteAll<GasData>();
                }
            }
        }
        catch (System.Exception)
        {
        }
    }

    public void CloseDifficultyPanel()
    {
        PlayClickSound();
        
        if (difficultyPanel != null)
            difficultyPanel.SetActive(false);
    }

    public void LoadGame()
    {
        PlayClickSound();

        if (HasAnySaveFile())
        {
            OpenLoadPanel();
        }
        else
        {
        }
    }

    public void OpenSettings()
    {
        PlayClickSound();
        if (settingsPanel != null)
            settingsPanel.OpenSettings();
    }

    public void OpenAbout()
    {
        PlayClickSound();
        CreateAboutPanelIfNeeded();

        if (aboutPanel != null)
        {
            aboutPanel.SetActive(true);

            if (EventSystem.current != null && aboutBackButton != null)
                EventSystem.current.SetSelectedGameObject(aboutBackButton.gameObject);
        }
    }

    public void CloseAbout()
    {
        PlayClickSound();

        if (aboutPanel != null)
            aboutPanel.SetActive(false);

        if (EventSystem.current != null && EventSystem.current.currentSelectedGameObject == aboutBackButton.gameObject)
            EventSystem.current.SetSelectedGameObject(null);
    }

    private void Update()
    {
        if (aboutPanel != null && aboutPanel.activeSelf && Input.GetKeyDown(KeyCode.Escape))
            CloseAbout();
    }

    private void CaptureEditableAboutText()
    {
        if (aboutPanel == null || aboutEditableTextCaptured)
            return;

        foreach (TextMeshProUGUI label in aboutPanel.GetComponentsInChildren<TextMeshProUGUI>(true))
        {
            if (label.gameObject.name == "Title") editableAboutTitle = label.text;
            else if (label.gameObject.name == "Content") editableAboutContent = label.text;
            else if (label.gameObject.name == "Label") editableAboutBackLabel = label.text;
        }

        aboutEditableTextCaptured = true;
    }
    public void RefreshAboutLocalization()
    {
        ApplyAboutPresentation();
    }

    private GameLanguage GetAboutLanguage()
    {
        return settingsPanel != null
            ? settingsPanel.GetSelectedLanguage()
            : (GameLanguage)Mathf.Clamp(PlayerPrefs.GetInt("GameLanguage", 0), 0, 2);
    }

    private string GetLocalizedAboutText(GameLanguage language)
    {
        switch (language)
        {
            case GameLanguage.Korean:
                return "Unity로 제작된 1인칭 필리핀 숲 공포 게임입니다.\n\n말라왁 숲을 탐험하고, 버려진 마을의 비밀을 밝히며, 필리핀 민속에서 영감을 받은 영혼들로부터 살아남으세요.\n\n전설을 배우고, 어둠에서 살아남으세요.\n\n제작: Bansag";
            case GameLanguage.Tagalog:
                return "Isang first-person na horror game sa kagubatan ng Pilipinas, na ginawa sa Unity.\n\nGalugarin ang Malawak Forest, tuklasin ang sinapit ng inabandonang nayon, at mabuhay laban sa mga espiritung hango sa kuwentong-bayan ng Pilipinas.\n\nAlamin ang mga alamat. Mabuhay sa dilim.\n\nGinawa ni Bansag";
            default:
                return aboutText;
        }
    }

    private void ApplyAboutPresentation()
    {
        if (aboutPanel == null)
            return;

        CaptureEditableAboutText();
        GameLanguage language = GetAboutLanguage();
        TMP_FontAsset koreanFont = settingsPanel != null ? settingsPanel.koreanFont : null;
        TextMeshProUGUI[] labels = aboutPanel.GetComponentsInChildren<TextMeshProUGUI>(true);
        foreach (TextMeshProUGUI label in labels)
        {
            if (label.gameObject.name == "Title")
            {
                label.text = string.IsNullOrEmpty(editableAboutTitle) ? aboutTitle : editableAboutTitle;
                if (aboutHeadingFont != null) label.font = aboutHeadingFont;
                label.fontSize = 54f;
            }
            else if (label.gameObject.name == "Content")
            {
                label.text = language == GameLanguage.English
                    ? (string.IsNullOrEmpty(editableAboutContent) ? aboutText : editableAboutContent)
                    : GetLocalizedAboutText(language);
                if (language == GameLanguage.Korean && koreanFont != null)
                    label.font = koreanFont;
                else if (aboutBodyFont != null)
                    label.font = aboutBodyFont;
                label.fontSize = 22f;
                label.color = new Color(0.94f, 0.9f, 0.9f, 1f);
            }
            else if (label.gameObject.name == "Label")
            {
                label.text = language == GameLanguage.Korean ? "뒤로"
                    : language == GameLanguage.Tagalog ? "BUMALIK"
                    : (string.IsNullOrEmpty(editableAboutBackLabel) ? "BACK" : editableAboutBackLabel);
                if (language == GameLanguage.Korean && koreanFont != null)
                    label.font = koreanFont;
                else if (aboutHeadingFont != null)
                    label.font = aboutHeadingFont;
                label.fontSize = 25f;
            }
        }

        Transform panel = aboutPanel.transform.Find("Panel");
        if (panel != null)
            panel.GetComponent<RectTransform>().sizeDelta = new Vector2(760f, 500f);
    }
    private void CreateAboutPanelIfNeeded()
    {
        if (aboutPanel != null)
            return;

        Canvas canvas = FindObjectOfType<Canvas>();
        if (canvas == null)
            return;

        aboutPanel = CreateUiObject("AboutPanel", canvas.transform);
        RectTransform overlayRect = aboutPanel.GetComponent<RectTransform>();
        overlayRect.anchorMin = Vector2.zero;
        overlayRect.anchorMax = Vector2.one;
        overlayRect.offsetMin = Vector2.zero;
        overlayRect.offsetMax = Vector2.zero;

        Image overlayImage = aboutPanel.AddComponent<Image>();
        overlayImage.color = new Color(0f, 0f, 0f, 0.78f);

        GameObject panel = CreateUiObject("Panel", aboutPanel.transform);
        RectTransform panelRect = panel.GetComponent<RectTransform>();
        panelRect.anchorMin = new Vector2(0.5f, 0.5f);
        panelRect.anchorMax = new Vector2(0.5f, 0.5f);
        panelRect.sizeDelta = new Vector2(640f, 390f);
        panelRect.anchoredPosition = Vector2.zero;

        Image panelImage = panel.AddComponent<Image>();
        panelImage.color = new Color(0.08f, 0.02f, 0.12f, 0.97f);

        CreateAboutText("Title", panel.transform, aboutTitle, 30f, FontStyles.Bold,
            new Vector2(0f, 132f), new Vector2(550f, 52f), new Color(0.95f, 0.35f, 0.38f));

        GameObject divider = CreateUiObject("Divider", panel.transform);
        RectTransform dividerRect = divider.GetComponent<RectTransform>();
        dividerRect.anchorMin = dividerRect.anchorMax = new Vector2(0.5f, 0.5f);
        dividerRect.anchoredPosition = new Vector2(0f, 83f);
        dividerRect.sizeDelta = new Vector2(535f, 2f);
        Image dividerImage = divider.AddComponent<Image>();
        dividerImage.color = new Color(0.85f, 0.15f, 0.2f, 0.85f);

        CreateAboutText("Content", panel.transform, aboutText, 20f, FontStyles.Normal,
            new Vector2(0f, -2f), new Vector2(540f, 180f), new Color(0.94f, 0.9f, 0.9f));

        GameObject backButton = CreateUiObject("BackButton", panel.transform);
        RectTransform buttonRect = backButton.GetComponent<RectTransform>();
        buttonRect.anchorMin = buttonRect.anchorMax = new Vector2(0.5f, 0.5f);
        buttonRect.anchoredPosition = new Vector2(0f, -138f);
        buttonRect.sizeDelta = new Vector2(180f, 48f);
        Image buttonImage = backButton.AddComponent<Image>();
        buttonImage.color = new Color(0.35f, 0.06f, 0.1f, 1f);
        Button button = backButton.AddComponent<Button>();
        aboutBackButton = button;
        button.targetGraphic = buttonImage;
        button.onClick.AddListener(CloseAbout);
        CreateAboutText("Label", backButton.transform, "BACK", 20f, FontStyles.Bold,
            Vector2.zero, new Vector2(180f, 48f), Color.white);

        // Visible while editing, hidden by Start when the game begins.
        aboutPanel.SetActive(!Application.isPlaying);
    }

    private void BindAboutBackButton()
    {
        if (aboutPanel == null)
            return;

        if (aboutBackButton == null)
            aboutBackButton = aboutPanel.GetComponentInChildren<Button>(true);

        if (aboutBackButton != null)
        {
            aboutBackButton.onClick.RemoveListener(CloseAbout);
            aboutBackButton.onClick.AddListener(CloseAbout);
        }
    }

    private static GameObject CreateUiObject(string objectName, Transform parent)
    {
        GameObject uiObject = new GameObject(objectName, typeof(RectTransform));
        uiObject.transform.SetParent(parent, false);
        return uiObject;
    }

    private static TextMeshProUGUI CreateAboutText(string objectName, Transform parent, string text,
        float fontSize, FontStyles fontStyle, Vector2 anchoredPosition, Vector2 size, Color color)
    {
        GameObject textObject = CreateUiObject(objectName, parent);
        RectTransform rect = textObject.GetComponent<RectTransform>();
        rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = anchoredPosition;
        rect.sizeDelta = size;

        TextMeshProUGUI label = textObject.AddComponent<TextMeshProUGUI>();
        label.font = TMP_Settings.defaultFontAsset;
        label.text = text;
        label.fontSize = fontSize;
        label.fontStyle = fontStyle;
        label.color = color;
        label.alignment = TextAlignmentOptions.Center;
        label.enableWordWrapping = true;
        label.raycastTarget = false;
        return label;
    }
    public void QuitGame()
    {
        PlayClickSound();
        
        if (SaveSystem.Instance != null)
        {
            SaveSystem.Instance.SaveGame();
        }
        
        Application.Quit();
    }

    public void PlayHoverSound()
    {
        if (hoverSound != null && audioSource != null)
        {
            audioSource.PlayOneShot(hoverSound, hoverVolume);
        }
    }

    public void PlayClickSound()
    {
        if (clickSound != null && audioSource != null)
        {
            audioSource.PlayOneShot(clickSound, clickVolume);
        }
    }

    void OnEnable()
    {
        if (!Application.isPlaying)
        {
            CreateAboutPanelIfNeeded();
            if (aboutPanel != null)
                aboutPanel.SetActive(true);
            return;
        }

        UpdateLoadButtonVisibility();
        UpdateLoadButtonsVisibility();
        LoadAndDisplayProgression();
    }}
