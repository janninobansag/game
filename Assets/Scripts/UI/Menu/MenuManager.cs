// PURPOSE: Handles menu actions for starting or loading Normal and Hard games and configuring menu flow.
using UnityEngine;
using TMPro;
using UnityEngine.UI;

[ExecuteAlways]
public partial class MenuManager : MonoBehaviour
{
    // Sounds and UI references used by the main-menu controls.
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

    [Header("About Panel")]
    public string aboutTitle = "VAREN";
    [TextArea(3, 8)]
    public string aboutText = "A first-person Philippine-forest horror game made in Unity.\n\nExplore Malawak Forest, uncover the fate of its abandoned village, and survive spirits inspired by Philippine folklore.\n\nLearn the legends. Survive the darkness.\n\nCreated by Bansag";
    [SerializeField] private GameObject aboutPanel;
    [SerializeField] private TMP_FontAsset aboutHeadingFont;
    [SerializeField] private TMP_FontAsset aboutBodyFont;
    private Button aboutBackButton;
    private bool aboutEditableTextCaptured;
    private string editableAboutTitle;
    private string editableAboutContent;
    private string editableAboutBackLabel;

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


        CreateAboutPanelIfNeeded();
        BindAboutBackButton();
        if (aboutPanel != null)
            aboutPanel.SetActive(false);
        UpdateLoadButtonVisibility();
        UpdateLoadButtonsVisibility();
        LoadAndDisplayProgression();
    }
}
