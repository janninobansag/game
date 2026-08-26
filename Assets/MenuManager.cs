using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using System.IO;
using SQLite4Unity3d;
using TMPro;
using UnityEngine.UI;

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
        string normalPath = Path.Combine(Application.persistentDataPath, "gameSave.db");
        string hardPath = Path.Combine(Application.persistentDataPath, "gameSave_Hard.db");
        
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
        string normalPath = Path.Combine(Application.persistentDataPath, "gameSave.db");
        bool exists = File.Exists(normalPath);
        return exists;
    }

    // ── Check if Hard save exists ──
    private bool HasHardSave()
    {
        string hardPath = Path.Combine(Application.persistentDataPath, "gameSave_Hard.db");
        bool exists = File.Exists(hardPath);
        return exists;
    }

    // ── Update visibility of Normal and Hard load buttons ──
    public void UpdateLoadButtonsVisibility()
    {
        // Completed saves remain visible but cannot be selected.
        bool hasNormalSave = HasNormalSave();
        bool canLoadNormalSave = CanLoadSave("gameSave.db");
        
        // Check if Hard mode save exists
        bool hasHardSave = HasHardSave();
        bool canLoadHardSave = CanLoadSave("gameSave_Hard.db");

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
            string normalPath = Path.Combine(Application.persistentDataPath, "gameSave.db");
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
            string hardPath = Path.Combine(Application.persistentDataPath, "gameSave_Hard.db");
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
        catch (System.Exception e)
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

        string normalPath = Path.Combine(Application.persistentDataPath, "gameSave.db");
        
        if (CanLoadSave("gameSave.db"))
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

        string hardPath = Path.Combine(Application.persistentDataPath, "gameSave_Hard.db");
        
        if (CanLoadSave("gameSave_Hard.db"))
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
        PlayerPrefs.Save();
        
        // Force SaveSystem to use Normal database
        if (SaveSystem.Instance != null)
        {
            SaveSystem.Instance.ReinitializeDatabase();
            SaveSystem.Instance.ClearProgressionData();
            SaveSystem.Instance.ClearStoryIntroData();
            SaveSystem.Instance.ClearSubtitleData();
        }
        else
        {
            ClearProgressionDataFromFile("gameSave.db");
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
        PlayerPrefs.Save();
        
        // Force SaveSystem to use Hard database
        if (SaveSystem.Instance != null)
        {
            SaveSystem.Instance.ReinitializeDatabase();
            SaveSystem.Instance.ClearProgressionData();
            SaveSystem.Instance.ClearStoryIntroData();
            SaveSystem.Instance.ClearSubtitleData();
        }
        else
        {
            ClearProgressionDataFromFile("gameSave_Hard.db");
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
                connection.DeleteAll<ProgressionData>();
            }
        }
        catch (System.Exception e)
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
        // About panel logic can be added here.
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
        UpdateLoadButtonVisibility();
        UpdateLoadButtonsVisibility();
        LoadAndDisplayProgression();
    }
}
