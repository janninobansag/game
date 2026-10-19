// PURPOSE: Stores and applies shared audio, sensitivity, and brightness settings across scenes.
using UnityEngine;

public class AudioManager : MonoBehaviour
{
    public static AudioManager Instance;

    // Values used when the player has not saved settings yet.
    [Header("Default Settings")]
    public float defaultVolume = 100f;
    public float defaultSensitivity = 5f;
    public float defaultBrightness = 0.5f;

    private float currentVolume;
    private float currentSensitivity;
    private float currentBrightness;

    void Awake()
    {
        // Keep exactly one manager alive while scenes change.
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
        LoadAndApplySettings();
    }

    public void LoadAndApplySettings()
    {
        LoadAndApplySettingsFromDatabase(null);
    }

    // Allows callers in this assembly to apply a selected settings database while
    // keeping the normal game path on SettingsDatabase's default persistent file.
    private void LoadAndApplySettingsFromDatabase(string databasePath)
    {
        // Read saved options, then apply them to the current game session.
        SettingsData savedSettings;
        bool hasSavedSettings = string.IsNullOrEmpty(databasePath)
            ? SettingsDatabase.TryLoad(out savedSettings)
            : SettingsDatabase.TryLoad(databasePath, out savedSettings);

        if (hasSavedSettings)
        {
            currentVolume = savedSettings.Volume;
            currentSensitivity = savedSettings.Sensitivity;
            currentBrightness = savedSettings.Brightness;
        }
        else
        {
            currentVolume = PlayerPrefs.GetFloat("MasterVolume", defaultVolume);
            currentSensitivity = PlayerPrefs.GetFloat("MouseSensitivity", defaultSensitivity);
            currentBrightness = PlayerPrefs.GetFloat("Brightness", defaultBrightness);
        }

        AudioListener.volume = currentVolume / 100f;
    }
    public float GetVolume()
    {
        return currentVolume;
    }

    public float GetSensitivity()
    {
        return currentSensitivity;
    }

    public float GetBrightness()
    {
        return currentBrightness;
    }
}
