using UnityEngine;
using System.Collections.Generic;

public class ProgressionSystem : MonoBehaviour
{
    public static ProgressionSystem Instance;

    [Header("Progression Settings")]
    public int totalProgressPoints = 100;
    public string saveKey = "GameProgress";

    [Header("Progression Triggers")]
    public List<ProgressionTrigger> progressionTriggers = new List<ProgressionTrigger>();

    private int currentProgress = 0;
    private bool isInitialized = false;

    void Awake()
    {

        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    void Start()
    {
        LoadProgress();
        RegisterAllTriggers();
    }

    void RegisterAllTriggers()
    {

        ProgressionTrigger[] triggers = FindObjectsOfType<ProgressionTrigger>();

        foreach (ProgressionTrigger trigger in triggers)
        {
            if (!progressionTriggers.Contains(trigger))
            {
                progressionTriggers.Add(trigger);
                trigger.OnProgressionTriggered += AddProgress;
            }
            else
            {
            }
        }
    }

    public void AddProgress(int points)
    {

        if (!isInitialized)
        {
            LoadProgress();
        }

        int oldProgress = currentProgress;
        currentProgress = Mathf.Min(currentProgress + points, totalProgressPoints);
        int actualAdded = currentProgress - oldProgress;

        if (actualAdded > 0)
        {
            SaveProgress();
        }
        else
        {
        }
    }

    public void SetProgress(int value)
    {
        currentProgress = Mathf.Clamp(value, 0, totalProgressPoints);
        SaveProgress();
    }

    public int GetCurrentProgress()
    {
        if (!isInitialized) LoadProgress();
        return currentProgress;
    }

    public float GetProgressPercentage()
    {
        if (!isInitialized) LoadProgress();
        return (float)currentProgress / totalProgressPoints * 100f;
    }

    public string GetProgressString()
    {
        return $"{GetProgressPercentage():F0}%";
    }

    public void LoadProgress()
    {
        currentProgress = PlayerPrefs.GetInt(saveKey, 0);
        isInitialized = true;
    }

    public void SaveProgress()
    {
        PlayerPrefs.SetInt(saveKey, currentProgress);
        PlayerPrefs.Save();
    }

    public void ResetProgress()
    {
        currentProgress = 0;
        PlayerPrefs.DeleteKey(saveKey);
        PlayerPrefs.Save();
    }

    public int GetProgressForSave()
    {
        return currentProgress;
    }

    public void LoadProgressFromSave(int progress)
    {
        currentProgress = Mathf.Clamp(progress, 0, totalProgressPoints);
        SaveProgress();
    }

    void OnApplicationQuit()
    {
        SaveProgress();
    }
}