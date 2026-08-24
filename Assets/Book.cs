using UnityEngine;

public class Book : MonoBehaviour
{
    [Header("Book Settings")]
    public string bookTitle = "Old Book";
    public float readRange = 2f;
    public KeyCode readKey = KeyCode.E;
    public KeyCode nextPageKey = KeyCode.E;
    public KeyCode prevPageKey = KeyCode.Q;

    [Header("Pages")]
    [TextArea(3, 10)] public string[] pages = { "Page 1: The forest is old...", "Page 2: The creatures watch...", "Page 3: The ritual must be completed..." };

    [Header("Page Titles (Optional)")]
    public string[] pageTitles = { "Chapter 1", "Chapter 2", "Chapter 3" };

    [Header("Optional Audio")]
    public AudioClip pageFlipSound;
    public AudioClip openBookSound;
    public AudioClip closeBookSound;
    [Range(0f, 1f)] public float soundVolume = 0.7f;

    [Header("Interaction Settings")]
    public string playerTag = "Player";

    public bool IsReading => isReading;
    public int CurrentPage => currentPage;
    public int PageCount => pages == null ? 0 : pages.Length;
    public string CurrentPageText => PageCount == 0 ? "This book has no pages." : pages[currentPage];
    public string CurrentPageTitle => pageTitles != null && pageTitles.Length > currentPage && !string.IsNullOrEmpty(pageTitles[currentPage]) ? pageTitles[currentPage] : bookTitle;

    private static Book activeBook;
    private bool isReading;
    private int currentPage;
    private Camera playerCamera;
    private AudioSource audioSource;
    private PlayerController playerController;
    private bool playerControllerWasEnabled;

    private void Start()
    {
        playerCamera = Camera.main;
        audioSource = gameObject.AddComponent<AudioSource>();
        audioSource.spatialBlend = 0f;
        audioSource.playOnAwake = false;
        audioSource.volume = soundVolume;
        BookUIController.GetOrCreate();
    }

    private void Update()
    {
        if (PauseMenu.Instance != null && PauseMenu.Instance.isPaused) return;

        if (isReading)
        {
            if (Input.GetKeyDown(nextPageKey)) NextPage();
            if (Input.GetKeyDown(prevPageKey)) PreviousPage();
            if (Input.GetKeyDown(KeyCode.Escape) || Input.GetKeyDown(KeyCode.F)) CloseBook();
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
            return;
        }

        if (playerCamera == null) playerCamera = Camera.main;
        if (playerCamera == null) return;

        bool lookingAtThisBook = false;
        Ray ray = playerCamera.ScreenPointToRay(new Vector3(Screen.width * 0.5f, Screen.height * 0.5f));
        if (Physics.Raycast(ray, out RaycastHit hit, readRange))
            lookingAtThisBook = hit.collider.gameObject == gameObject || hit.collider.transform.IsChildOf(transform);

        BookUIController ui = BookUIController.GetOrCreate();
        if (ui == null) return;
        if (lookingAtThisBook)
        {
            ui.ShowPrompt(this, $"Press {readKey} to read {bookTitle}");
            if (Input.GetKeyDown(readKey)) OpenBook();
        }
        else ui.HidePrompt(this);
    }

    public void OpenBook()
    {
        if (PageCount == 0 || isReading) return;
        if (activeBook != null && activeBook != this) activeBook.CloseBook();
        activeBook = this;
        isReading = true;
        currentPage = 0;
        PlayerController.SetDocumentReading(true);
        LockPlayerMovement();
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
        BookUIController.GetOrCreate().Open(this);
        PlaySound(openBookSound);
    }

    public void CloseBook()
    {
        if (!isReading) return;
        isReading = false;
        if (activeBook == this) activeBook = null;
        PlayerController.SetDocumentReading(false);
        RestorePlayerMovement();
        BookUIController.GetOrCreate().Close(this);
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
        PlaySound(closeBookSound);
    }

    public void NextPage()
    {
        if (currentPage >= PageCount - 1) return;
        currentPage++;
        PlaySound(pageFlipSound);
        BookUIController.GetOrCreate().Refresh(this);
    }

    public void PreviousPage()
    {
        if (currentPage <= 0) return;
        currentPage--;
        PlaySound(pageFlipSound);
        BookUIController.GetOrCreate().Refresh(this);
    }

    private void LockPlayerMovement()
    {
        GameObject player = GameObject.FindGameObjectWithTag(playerTag);
        if (player != null)
        {
            playerController = player.GetComponent<PlayerController>();
            if (playerController == null) playerController = player.GetComponentInChildren<PlayerController>();
        }
        if (playerController == null) playerController = FindObjectOfType<PlayerController>();
        if (playerController == null) return;
        playerControllerWasEnabled = playerController.enabled;
        playerController.enabled = false;
    }

    private void RestorePlayerMovement()
    {
        if (playerController != null) playerController.enabled = playerControllerWasEnabled;
        playerController = null;
    }

    private void PlaySound(AudioClip clip)
    {
        if (clip != null && audioSource != null) audioSource.PlayOneShot(clip, soundVolume);
    }

    private void OnDisable()
    {
        if (isReading) CloseBook();
    }
}
