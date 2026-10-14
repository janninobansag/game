// PURPOSE: Saves and restores game progress and player state using the selected difficulty database.
using System.IO;
using SQLite4Unity3d;
using UnityEngine;
using UnityEngine.SceneManagement;
public partial class SaveSystem : MonoBehaviour
{
    private static string GetDifficultyForActiveScene()
    {
        string sceneName = SceneManager.GetActiveScene().name;
        if (sceneName == "chapter 1")
        {
            PlayerPrefs.SetString("GameDifficulty", "Normal");
            PlayerPrefs.Save();
            return "Normal";
        }

        if (sceneName == "chapter 2")
        {
            PlayerPrefs.SetString("GameDifficulty", "Hard");
            PlayerPrefs.Save();
            return "Hard";
        }

        return PlayerPrefs.GetString("GameDifficulty", "Normal");
    }

    private bool IsHardModeDatabase()
    {
        return GetCurrentDifficulty() == "Hard";
    }
    private void EnsureDatabaseReady()
    {
        if (isDatabaseReady) return;
        InitializeDatabase();
    }

    void InitializeDatabase()
    {
        // Create missing tables and update the database structure before saving or loading.
        try
        {
            connection = new SQLiteConnection(savePath, SQLiteOpenFlags.ReadWrite | SQLiteOpenFlags.Create);
            RelationalSaveSchema.Create(connection, IsHardModeDatabase());

            connection.CreateTable<PlayerData>();
            connection.CreateTable<InventoryData>();
            connection.CreateTable<DoorData>();
            connection.CreateTable<DrawerData>();
            connection.CreateTable<RitualData>();
            connection.CreateTable<NoteData>();
            connection.CreateTable<GameStateData>();
            connection.CreateTable<DroppedItemData>();
            connection.CreateTable<FlashlightData>();
            connection.CreateTable<KeyData>();
            connection.CreateTable<BatteryData>();
            connection.CreateTable<RitualItemData>();
            connection.CreateTable<StaminaData>();
            connection.CreateTable<SubtitleData>();
            connection.CreateTable<IntroData>();
            connection.CreateTable<AIPositionData>();

            if (IsHardModeDatabase())
            {
                connection.CreateTable<GeneratorCoverData>();
                connection.CreateTable<WrenchData>();
                connection.CreateTable<GasData>();
            }
            else
            {
                // These systems exist only in Chapter 2 Hard mode.
                connection.Execute("DROP TABLE IF EXISTS GeneratorCoverData");
                connection.Execute("DROP TABLE IF EXISTS WrenchData");
                connection.Execute("DROP TABLE IF EXISTS GasData");
            }

            // Existing saves created before IsTriggered was added need this column.
            try { connection.Execute("ALTER TABLE SubtitleData ADD COLUMN IsTriggered INTEGER NOT NULL DEFAULT 0"); }
            catch (System.Exception) { }
            try { connection.Execute("ALTER TABLE FlashlightData ADD COLUMN IsOn INTEGER NOT NULL DEFAULT 0"); }
            catch (System.Exception) { }
            // ── NEW: ProgressionData table ──
            connection.CreateTable<ProgressionData>();
            // Added fields for generic dropped items. Older databases may already have them.
            try { connection.Execute("ALTER TABLE DroppedItemData ADD COLUMN IsHeld INTEGER NOT NULL DEFAULT 0"); }
            catch (System.Exception) { }
            try { connection.Execute("ALTER TABLE DroppedItemData ADD COLUMN IsDropped INTEGER NOT NULL DEFAULT 1"); }
            catch (System.Exception) { }

            isDatabaseReady = true;

            if (File.Exists(savePath))
            {
            }
        }
        catch (System.Exception e)
        {
            isDatabaseReady = false;
            Debug.LogError("[SaveSystem] Could not initialize the relational save database at " + savePath + ": " + e.Message);
        }
    }

    // ── NEW: Reinitialize database (call this when switching difficulties) ──
    public void ReinitializeDatabase()
    {
        if (connection != null)
        {
            connection.Close();
            connection = null;
        }

        isDatabaseReady = false;

        // ── Use different database files for different difficulties ──
        string difficulty = GetDifficultyForActiveScene();
        string fileName = difficulty == "Hard" ? "gameSave_Hard_v2.db" : "gameSave_v2.db";
        savePath = Path.Combine(Application.persistentDataPath, fileName);

        // We'll initialize on the next operation
    }

    // ── Get current save file path ──
    public string GetSavePath()
    {
        return savePath;
    }

    // ── Get current difficulty from PlayerPrefs ──
    public string GetCurrentDifficulty()
    {
        return PlayerPrefs.GetString("GameDifficulty", "Normal");
    }
}
