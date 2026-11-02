// PURPOSE: Starts new Normal or Hard games and clears the correct progression and save state.
using UnityEngine;
using System.IO;
using SQLite4Unity3d;

public partial class MenuManager : MonoBehaviour
{
    public void PlayGame()
    {
        if (!RequireProfileSelection()) return;
        PlayClickSound();

        if (difficultyPanel != null)
        {
            difficultyPanel.SetActive(true);
        }
    }

    public void StartNormal()
    {
        if (!RequireProfileSelection()) return;
        PlayClickSound();
        ProgressionTrigger.ClearSavedStates();
        ObjectiveTrigger.ClearSavedStates();

        if (difficultyPanel != null)
            difficultyPanel.SetActive(false);

        // ── Reset all PickupTriggerSpawner states ──
        PickupTriggerSpawner[] spawners = FindObjectsOfType<PickupTriggerSpawner>();
        foreach (PickupTriggerSpawner spawner in spawners)
        {
            spawner.ResetActivation();
        }

        // ── Reset ALL PlayerPrefs keys ──
        PlayerPrefs.SetInt(GameProfiles.ProgressKey("CandleOneRevealed"), 0);
        PlayerPrefs.SetInt(GameProfiles.ProgressKey("BibleSpawner"), 0);
        PlayerPrefs.SetInt(GameProfiles.ProgressKey("CrossSpawner"), 0);
        PlayerPrefs.SetInt(GameProfiles.ProgressKey("CrossRevealed"), 0);
        PlayerPrefs.SetInt(GameProfiles.ProgressKey("BibbleRevealed"), 0);
        PlayerPrefs.SetInt("ShouldLoadSave", 0);
        PlayerPrefs.SetInt("SkipIntro", 0);
        PlayerPrefs.SetString("GameDifficulty", "Normal");
        PlayerPrefs.SetString("SavedScene", "chapter 1");
        PlayerPrefs.SetString("SaveTime", System.DateTime.Now.ToString("MM/dd/yyyy HH:mm"));
        PlayerPrefs.DeleteKey(GameProfiles.ProgressKey("GameProgress"));
        // A fresh game must choose a new Mansion key location.
        ClearSavedSpawnChoices();
        PlayerPrefs.Save();

        if (ProgressionSystem.Instance != null)
            ProgressionSystem.Instance.ResetProgress();
        CheckpointTrigger.HasCheckpointSaved = false;

        // Force SaveSystem to use Normal database
        if (SaveSystem.Instance != null)
        {
            SaveSystem.Instance.ReinitializeDatabase();
            SaveSystem.Instance.ClearKeyData();
            SaveSystem.Instance.ClearBatteryData();
            SaveSystem.Instance.ClearRitualItemData();
            SaveSystem.Instance.ClearNewGameWorldState();
            SaveSystem.Instance.ClearOneShotEventStates();
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
        if (!RequireProfileSelection()) return;
        PlayClickSound();
        ProgressionTrigger.ClearSavedStates();
        ObjectiveTrigger.ClearSavedStates();

        if (difficultyPanel != null)
            difficultyPanel.SetActive(false);

        // ── Reset all PickupTriggerSpawner states ──
        PickupTriggerSpawner[] spawners = FindObjectsOfType<PickupTriggerSpawner>();
        foreach (PickupTriggerSpawner spawner in spawners)
        {
            spawner.ResetActivation();
        }

        // ── Reset ALL PlayerPrefs keys ──
        PlayerPrefs.SetInt(GameProfiles.ProgressKey("CandleOneRevealed"), 0);
        PlayerPrefs.SetInt(GameProfiles.ProgressKey("BibleSpawner"), 0);
        PlayerPrefs.SetInt(GameProfiles.ProgressKey("CrossSpawner"), 0);
        PlayerPrefs.SetInt(GameProfiles.ProgressKey("CrossRevealed"), 0);
        PlayerPrefs.SetInt(GameProfiles.ProgressKey("BibbleRevealed"), 0);
        PlayerPrefs.SetInt("ShouldLoadSave", 0);
        PlayerPrefs.SetInt("SkipIntro", 0);
        PlayerPrefs.SetString("GameDifficulty", "Hard");
        PlayerPrefs.SetString("SavedScene", "chapter 2");
        PlayerPrefs.SetString("SaveTime", System.DateTime.Now.ToString("MM/dd/yyyy HH:mm"));
        PlayerPrefs.DeleteKey(GameProfiles.ProgressKey("GameProgress"));
        // A fresh game must choose a new Mansion key location.
        ClearSavedSpawnChoices();
        PlayerPrefs.Save();

        if (ProgressionSystem.Instance != null)
            ProgressionSystem.Instance.ResetProgress();
        CheckpointTrigger.HasCheckpointSaved = false;

        // Force SaveSystem to use Hard database
        if (SaveSystem.Instance != null)
        {
            SaveSystem.Instance.ReinitializeDatabase();
            SaveSystem.Instance.ClearKeyData();
            SaveSystem.Instance.ClearBatteryData();
            SaveSystem.Instance.ClearRitualItemData();
            SaveSystem.Instance.ClearNewGameWorldState();
            SaveSystem.Instance.ClearOneShotEventStates();
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
        string dbPath = GameProfiles.GetSavePath(fileName);

        if (!File.Exists(dbPath))
            return;

        try
        {
            using (var connection = new SQLiteConnection(dbPath, SQLiteOpenFlags.ReadWrite))
            {
                connection.CreateTable<AIPositionData>();
                connection.CreateTable<GameStateData>();
                connection.CreateTable<NoteData>();
                connection.DeleteAll<ProgressionData>();
                connection.DeleteAll<NoteData>();
                connection.Execute("DELETE FROM GameStateData WHERE \"Key\" LIKE ?", "OneShot:%");
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

    private static void ClearSavedSpawnChoices()
    {
        string[] keys =
        {
            "MansionKeySpawnIndex_Normal", "MansionKeySpawnIndex_Hard",
            "WaitingRoomKeySpawnIndex_Normal", "WaitingRoomKeySpawnIndex_Hard",
            "FlashlightSpawnIndex_Normal", "FlashlightSpawnIndex_Hard",
            "WrenchSpawnIndex_Normal", "WrenchSpawnIndex_Hard",
            "VaultKeySpawnIndex_Normal", "VaultKeySpawnIndex_Hard",
            "OfficeKeySpawnIndex_Normal", "OfficeKeySpawnIndex_Hard"
        };

        foreach (string key in keys)
            PlayerPrefs.DeleteKey(GameProfiles.ProgressKey(key));
    }
}
