using UnityEngine;

public class Note : MonoBehaviour
{
    public PlayerSubtitleTrigger linkedSubtitleTrigger;
    [Header("Objectives")] public ObjectiveTrigger onCloseObjective;
    [Header("On Close Audio")] public AudioTrigger onCloseAudio;
    [Header("Note Settings")]
    public string noteTitle = "Old Note";
    [TextArea(3, 10)] public string noteContent = "Write your note content here...";
    public float readRange = 2f;
    public KeyCode readKey = KeyCode.E;

    public bool IsReading => isReading;
    public bool HasBeenRead() => hasBeenRead;

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
            ui.ShowPrompt(this, $"Press {readKey} to read {noteTitle}");
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
