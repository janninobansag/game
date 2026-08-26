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
        // Load volume
        if (PlayerPrefs.HasKey("MasterVolume"))
            currentVolume = PlayerPrefs.GetFloat("MasterVolume");
        else
            currentVolume = defaultVolume;
        
        // Load sensitivity
        if (PlayerPrefs.HasKey("MouseSensitivity"))
            currentSensitivity = PlayerPrefs.GetFloat("MouseSensitivity");
        else
            currentSensitivity = defaultSensitivity;
        
        // Load brightness
        if (PlayerPrefs.HasKey("Brightness"))
            currentBrightness = PlayerPrefs.GetFloat("Brightness");
        else
            currentBrightness = defaultBrightness;
        
        // Apply
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