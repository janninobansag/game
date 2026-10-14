// PURPOSE: Saves and restores game progress and player state using the selected difficulty database.
using System.IO;
using SQLite4Unity3d;
using UnityEngine;
public partial class SaveSystem : MonoBehaviour
{
    public static SaveSystem Instance;

    // File name and automatic-save option used by this persistent game manager.
    [Header("Save Settings")]
    public string saveFileName = "gameSave_v2.db";
    public bool autoSaveOnQuit = true;

    [Header("Debug")]
    [Tooltip("Writes each drawer's saved and restored state to the Unity Console.")]
    public bool logDrawerSaveLoad = true;

    private const string GasRecordId = "Gas";

    private string savePath;
    private bool isLoading = false;
    private bool isSaving = false;
    private bool isQuitting = false;
    private bool isLoadComplete = false;

    private SQLiteConnection connection;
    private bool isDatabaseReady = false;
    void Awake()
    {
        // Keep one save manager alive across scenes and prepare the selected database.
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }

        // ── FIX: Don't create database immediately ──
        // Just set the path based on difficulty, but don't initialize yet
        string difficulty = GetDifficultyForActiveScene();
        string fileName = difficulty == "Hard" ? "gameSave_Hard_v2.db" : "gameSave_v2.db";
        savePath = Path.Combine(Application.persistentDataPath, fileName);

        // ── Don't call InitializeDatabase() here ──
        // We will initialize only when needed
    }

}
