// PURPOSE: Lets the player open and read a note while temporarily locking and restoring player movement.
using UnityEngine;

public class Note : MonoBehaviour
{
    // Optional systems fired when this note opens or closes.
    public PlayerSubtitleTrigger linkedSubtitleTrigger;
    [Header("Objectives")] public ObjectiveTrigger onCloseObjective;
    [Header("On Close Audio")] public AudioTrigger onCloseAudio;
    // Text and interaction distance for this readable note.
    [Header("English Note")]
    public string noteTitle = "Old Note";
    [TextArea(3, 10)] public string noteContent = "Write your note content here...";

    [Header("Korean Translation")]
    public string koreanTitle;
    [TextArea(3, 10)] public string koreanContent;

    [Header("Tagalog Translation")]
    public string tagalogTitle;
    [TextArea(3, 10)] public string tagalogContent;

    public float readRange = 2f;
    public KeyCode readKey = KeyCode.E;

    public bool IsReading => isReading;
    // Other scripts use this to detect whether the player has already read the note.
    public bool HasBeenRead() => hasBeenRead;

    public string GetLocalizedTitle()
    {
        if (GameplayLocalization.IsKorean && !string.IsNullOrWhiteSpace(koreanTitle)) return koreanTitle;
        if (GameplayLocalization.IsTagalog && !string.IsNullOrWhiteSpace(tagalogTitle)) return tagalogTitle;
        return noteTitle;
    }

    public string GetLocalizedContent()
    {
        if (GameplayLocalization.IsKorean && !string.IsNullOrWhiteSpace(koreanContent)) return koreanContent;
        if (GameplayLocalization.IsTagalog && !string.IsNullOrWhiteSpace(tagalogContent)) return tagalogContent;
        return noteContent;
    }

    private string GetReadPrompt()
    {
        string title = GetLocalizedTitle();
        if (GameplayLocalization.IsKorean) return $"{readKey} 키를 눌러 {title} 읽기";
        if (GameplayLocalization.IsTagalog) return $"Pindutin ang {readKey} upang basahin ang {title}";
        return $"Press {readKey} to read {title}";
    }

    private static Note activeNote;
    private bool hasBeenRead;
    private bool isReading;
    private Camera playerCamera;
    private PlayerController playerController;
    private bool playerControllerWasEnabled;

    private void Start()
    {
        playerCamera = Camera.main;
        NoteUIController.GetOrCreate();
    }

    private void Update()
    {
        if (PauseMenu.Instance != null && PauseMenu.Instance.isPaused) return;
        if (isReading)
        {
            if (Input.GetKeyDown(readKey) || Input.GetKeyDown(KeyCode.Escape) || Input.GetKeyDown(KeyCode.F)) CloseNote();
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
            return;
        }

        if (playerCamera == null) playerCamera = Camera.main;
        if (playerCamera == null) return;
        bool lookingAtNote = false;
        Ray ray = playerCamera.ScreenPointToRay(new Vector3(Screen.width * 0.5f, Screen.height * 0.5f));
        if (Physics.Raycast(ray, out RaycastHit hit, readRange))
            lookingAtNote = hit.collider.gameObject == gameObject || hit.collider.transform.IsChildOf(transform);

        NoteUIController ui = NoteUIController.GetOrCreate();
        if (ui == null) return;
        if (lookingAtNote)
        {
            ui.ShowPrompt(this, GetReadPrompt());
            if (Input.GetKeyDown(readKey)) OpenNote();
        }
        else ui.HidePrompt(this);
    }

    public void OpenNote()
    {
        if (isReading) return;
        if (activeNote != null && activeNote != this) activeNote.CloseNote();
        activeNote = this;
        isReading = true;
        PlayerController.SetDocumentReading(true);
        LockPlayerMovement();
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
        NoteUIController.GetOrCreate().Open(this);
    }

    public void CloseNote()
    {
        if (!isReading) return;
        isReading = false;
        if (activeNote == this) activeNote = null;
        PlayerController.SetDocumentReading(false);
        RestorePlayerMovement();
        NoteUIController.GetOrCreate().Close(this);
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
        hasBeenRead = true;

        DrawerInteraction drawer = GetComponentInParent<DrawerInteraction>();
        if (drawer != null) drawer.SetBusy(true);
        if (onCloseAudio != null) onCloseAudio.TriggerAudio();
        if (onCloseObjective != null) onCloseObjective.TriggerObjective();
        ObjectiveTriggerActivator activator = GetComponent<ObjectiveTriggerActivator>();
        if (activator != null) activator.ActivateObjectiveTrigger();
        ItemSubtitleTrigger subtitle = GetComponent<ItemSubtitleTrigger>();
        if (subtitle != null) subtitle.OnPickedUp();
        if (linkedSubtitleTrigger != null) linkedSubtitleTrigger.gameObject.SetActive(true);
        if (drawer != null) drawer.SetBusy(false);
    }

    private void LockPlayerMovement()
    {
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player != null) playerController = player.GetComponent<PlayerController>();
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
    private void OnDisable() { if (isReading) CloseNote(); }
}
