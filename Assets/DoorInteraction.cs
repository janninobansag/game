using System.Collections;
using UnityEngine;

public class DoorInteraction : MonoBehaviour
{
    [Header("Door Audio")]
    public AudioClip openSound;
    public AudioClip closeSound;
    public float doorVolume = 1f;

    private AudioSource audioSource;
    public float openAngle = 90f;
    public float animationSpeed = 2f;
    public bool invertDirection = false;

    [Header("Lock Settings")]
    public bool isLocked = false;
    public DoorInteraction linkedDoor;

    [Header("Interaction")]
    public float raycastRange = 3f;
    public KeyCode interactKey = KeyCode.E;
    public string doorTag = "Door";

    private bool isOpen = false;
    private bool isAnimating = false;
    private Quaternion closedRotation;
    private Quaternion openRotation;

    private bool showPrompt = false;
    private bool showLockedPrompt = false;
    private float lockedPromptTimer = 0f;

    void Start()
    {
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
        showPrompt = false;

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
                    if (isLocked)
                    {
                        showLockedPrompt = true;
                        lockedPromptTimer = 2f;
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

    public void Unlock()
    {
        isLocked = false;
        showLockedPrompt = false;
        lockedPromptTimer = 0f;

        if (!isOpen && !isAnimating)
        {
            StartCoroutine(AnimateDoor(closedRotation, openRotation));
            isOpen = true;
        }

        if (linkedDoor != null && !linkedDoor.isOpen)
        {
            linkedDoor.isLocked = false;
            linkedDoor.UnlockSilent();
        }
    }

    public void UnlockSilent()
    {
        isLocked = false;
        showLockedPrompt = false;
        lockedPromptTimer = 0f;

        if (!isOpen && !isAnimating)
        {
            StartCoroutine(AnimateDoor(closedRotation, openRotation));
            isOpen = true;
        }
    }

    public bool IsLocked() => isLocked;
    public bool IsOpen() => isOpen;

    // ── NEW: Lock method ──
    public void Lock()
    {
        isLocked = true;
    }

    IEnumerator AnimateDoor(Quaternion from, Quaternion to)
    {
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
                "This door is locked!", shadow);
            GUI.Label(new Rect(Screen.width / 2 - 200,
                Screen.height / 2 + 50, 400, 40),
                "This door is locked!", style);
        }
    }
}