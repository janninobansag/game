// PURPOSE: Checks save files and displays Normal and Hard progression in the Load panel.
using UnityEngine;
using System.IO;
using SQLite4Unity3d;
using UnityEngine.UI;

public partial class MenuManager : MonoBehaviour
{
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
}
