using UnityEngine;

public class AudioManager : MonoBehaviour
{
    public static AudioManager Instance;

    [Header("Default Settings")]
    public float defaultVolume = 100f;
    public float defaultSensitivity = 5f;
    public float defaultBrightness = 0.5f;

    private float currentVolume;
    private float currentSensitivity;
    private float currentBrightness;

    void Awake()
    {
        // Singleton pattern
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
        // Use settings.db so this also works when a gameplay scene is launched directly.
        SettingsData savedSettings;
        if (SettingsDatabase.TryLoad(out savedSettings))
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