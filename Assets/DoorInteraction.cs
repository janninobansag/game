// PURPOSE: Handles door opening, closing, locks, audio, and requests to open the door for an enemy.
using System.Collections;
using UnityEngine;

public class DoorInteraction : MonoBehaviour
{
    // Sounds played when this door opens or closes.
    [Header("Door Audio")]
    public AudioClip openSound;
    public AudioClip closeSound;
    public float doorVolume = 1f;

    // Movement settings for the door hinge animation.
    private AudioSource audioSource;
    public float openAngle = 90f;
    public float animationSpeed = 2f;
    public bool invertDirection = false;

    // A linked door unlocks together with this door, useful for double doors.
    [Header("Lock Settings")]
    public bool isLocked = false;
    public DoorInteraction linkedDoor;

    // interactionEnabled is disabled by the vault until its PIN sequence is complete.
    [Header("Interaction")]
    public float raycastRange = 3f;
    public KeyCode interactKey = KeyCode.E;
    public string doorTag = "Door";
    [Tooltip("Disable this when another interaction sequence controls when the door can open.")]
    public bool interactionEnabled = true;

    // Message shown after the player presses E on a locked door.
    [Header("Locked Prompt")]
    [TextArea]
    public string lockedDoorText = "This door is locked!";
    public float lockedPromptDuration = 2f;

    private bool isOpen = false;
    private bool isAnimating = false;
    private Quaternion closedRotation;
    private Quaternion openRotation;

    private bool showPrompt = false;
    private bool showLockedPrompt = false;
    private float lockedPromptTimer = 0f;

    void Start()
    {
        // Store the initial rotation so the door can return to its closed position.
        closedRotation = transform.rotation;
        float angle = invertDirection ? -openAngle : openAngle;
        openRotation = Quaternion.Euler(
            transform.eulerAngles + new Vector3(0, angle, 0));

        audioSource = gameObject.AddComponent<AudioSource>();
        audioSource.spatialBlend = 1f;
        audioSource.playOnAwake = false;
        audioSource.volume = doorVolume;
        audioSource.maxDistance = 10f;
        audioSource.rolloffMode = AudioRolloffMode.Linear;
    }

    void Update()
    {
        // Check whether the player is looking at this door from the centre of the screen.
        showPrompt = false;

        if (!interactionEnabled)
            return;

        Ray ray = Camera.main.ScreenPointToRay(
            new Vector3(Screen.width / 2, Screen.height / 2));
        RaycastHit hit;

        if (Physics.Raycast(ray, out hit, raycastRange))
        {
            if (hit.collider.CompareTag(doorTag) &&
                hit.collider.transform.IsChildOf(transform))
            {
                showPrompt = true;

                if (Input.GetKeyDown(interactKey) && !isAnimating)
                {
                    // Locked doors show a message. Unlocked doors toggle open and closed.
                    if (isLocked)
                    {
                        showLockedPrompt = true;
                        lockedPromptTimer = lockedPromptDuration;
                        return;
                    }

                    if (isOpen)
                        StartCoroutine(AnimateDoor(openRotation, closedRotation));
                    else
                        StartCoroutine(AnimateDoor(closedRotation, openRotation));

                    isOpen = !isOpen;
                }
            }
        }

        if (showLockedPrompt)
        {
            lockedPromptTimer -= Time.deltaTime;
            if (lockedPromptTimer <= 0f)
                showLockedPrompt = false;
        }
    }

    // ── NEW: Set open state without animation ──
    public void SetOpenState(bool open)
    {
        isOpen = open;
        if (open)
        {
            transform.rotation = openRotation;
        }
        else
        {
            transform.rotation = closedRotation;
        }
    }

    public void Unlock(bool openDoor = true)
    {
        // The vault passes false so its key unlocks the door without opening it.
        isLocked = false;
        showLockedPrompt = false;
        lockedPromptTimer = 0f;

        if (openDoor && !isOpen && !isAnimating)
        {
            Open();
        }

        if (linkedDoor != null && !linkedDoor.isOpen)
        {
            linkedDoor.isLocked = false;
            linkedDoor.UnlockSilent(openDoor);
        }
    }

    public void UnlockSilent(bool openDoor = true)
    {
        // Used for linked doors so they receive the same unlocked state.
        isLocked = false;
        showLockedPrompt = false;
        lockedPromptTimer = 0f;

        if (openDoor && !isOpen && !isAnimating)
        {
            Open();
        }
    }

    public void Open()
    {
        // Start opening only when the door is unlocked and not already moving.
        if (isLocked || isOpen || isAnimating)
            return;

        StartCoroutine(AnimateDoor(closedRotation, openRotation));
        isOpen = true;
    }

    public bool IsLocked() => isLocked;
    public bool IsOpen() => isOpen;

    public bool TryOpenForEnemy()
    {
        // Allows enemy AI to request opening without bypassing a lock.
        if (isLocked)
            return false;

        if (isOpen)
            return !isAnimating;

        if (!isAnimating)
        {
            StartCoroutine(AnimateDoor(closedRotation, openRotation));
            isOpen = true;
        }

        return false;
    }

    // ── NEW: Lock method ──
    public void Lock()
    {
        isLocked = true;
    }

    IEnumerator AnimateDoor(Quaternion from, Quaternion to)
    {
        // Smoothly rotate between the two door states.
        isAnimating = true;
        float elapsed = 0f;

        if (isOpen)
        {
            if (closeSound != null)
                audioSource.PlayOneShot(closeSound, doorVolume);
        }
        else
        {
            if (openSound != null)
                audioSource.PlayOneShot(openSound, doorVolume);
        }

        while (elapsed < 1f)
        {
            elapsed += Time.deltaTime * animationSpeed;
            transform.rotation = Quaternion.Slerp(from, to, elapsed);
            yield return null;
        }

        transform.rotation = to;
        isAnimating = false;
    }

    void OnGUI()
    {
        // Draw the normal interaction prompt or the locked message.
        if (showPrompt && !showLockedPrompt)
        {
            GUIStyle style = new GUIStyle();
            style.fontSize = 22;
            style.alignment = TextAnchor.MiddleCenter;
            style.normal.textColor = Color.white;

            GUIStyle shadow = new GUIStyle();
            shadow.fontSize = 22;
            shadow.alignment = TextAnchor.MiddleCenter;
            shadow.normal.textColor = Color.black;

            string msg = $"Press {interactKey} to open";
            GUI.Label(new Rect(Screen.width / 2 - 199,
                Screen.height / 2 + 51, 400, 40),
                msg, shadow);
            GUI.Label(new Rect(Screen.width / 2 - 200,
                Screen.height / 2 + 50, 400, 40),
                msg, style);
        }

        if (showLockedPrompt)
        {
            GUIStyle style = new GUIStyle();
            style.fontSize = 22;
            style.alignment = TextAnchor.MiddleCenter;
            style.normal.textColor = Color.red;

            GUIStyle shadow = new GUIStyle();
            shadow.fontSize = 22;
            shadow.alignment = TextAnchor.MiddleCenter;
            shadow.normal.textColor = Color.black;

            GUI.Label(new Rect(Screen.width / 2 - 199,
                Screen.height / 2 + 51, 400, 40),
                lockedDoorText, shadow);
            GUI.Label(new Rect(Screen.width / 2 - 200,
                Screen.height / 2 + 50, 400, 40),
                lockedDoorText, style);
        }
    }
}
