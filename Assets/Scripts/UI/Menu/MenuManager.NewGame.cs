// PURPOSE: Starts new Normal or Hard games and clears the correct progression and save state.
using UnityEngine;
using System.IO;
using SQLite4Unity3d;

public partial class MenuManager : MonoBehaviour
{
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
        PlayerPrefs.DeleteKey("WaitingRoomKeySpawnIndex_Normal");
        PlayerPrefs.DeleteKey("WaitingRoomKeySpawnIndex_Hard");
        PlayerPrefs.DeleteKey("FlashlightSpawnIndex_Normal");
        PlayerPrefs.DeleteKey("FlashlightSpawnIndex_Hard");
        PlayerPrefs.DeleteKey("WrenchSpawnIndex_Normal");
        PlayerPrefs.DeleteKey("WrenchSpawnIndex_Hard");
        PlayerPrefs.Save();

        // Force SaveSystem to use Normal database
        if (SaveSystem.Instance != null)
        {
            SaveSystem.Instance.ReinitializeDatabase();
            SaveSystem.Instance.ClearKeyData();
            SaveSystem.Instance.ClearBatteryData();
            SaveSystem.Instance.ClearRitualItemData();
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
        PlayerPrefs.DeleteKey("WaitingRoomKeySpawnIndex_Normal");
        PlayerPrefs.DeleteKey("WaitingRoomKeySpawnIndex_Hard");
        PlayerPrefs.DeleteKey("FlashlightSpawnIndex_Normal");
        PlayerPrefs.DeleteKey("FlashlightSpawnIndex_Hard");
        PlayerPrefs.DeleteKey("WrenchSpawnIndex_Normal");
        PlayerPrefs.DeleteKey("WrenchSpawnIndex_Hard");
        PlayerPrefs.Save();

        // Force SaveSystem to use Hard database
        if (SaveSystem.Instance != null)
        {
            SaveSystem.Instance.ReinitializeDatabase();
            SaveSystem.Instance.ClearKeyData();
            SaveSystem.Instance.ClearBatteryData();
            SaveSystem.Instance.ClearRitualItemData();
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
}
