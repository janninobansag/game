// PURPOSE: Runs the vault sequence: key unlock, handle turn, PIN entry, then door opening.
using UnityEngine;

[DisallowMultipleComponent]
public class VaultDoorInteraction : MonoBehaviour
{
    // The order the player must follow to open the vault.
    private enum VaultState { KeyLocked, HandleReady, PinReady, Open }

    // Drag the vault parts here. The script also tries to find them by name when the game starts.
    [Header("References")]
    [Tooltip("Usually the DoorInteraction on this vault door.")]
    [SerializeField] private DoorInteraction vaultDoor;
    [SerializeField] private Transform vaultHandle;
    [SerializeField] private Transform vaultPin;

    // This value must match the "Unlocks Tag" value on the Vault key's Key component.
    [Header("Key")]
    [Tooltip("Must match the Vault key's Unlocks Tag.")]
    [SerializeField] private string requiredKeyTarget = "vault door";

    // Settings for the hold-E handle animation after the key unlocks the vault.
    [Header("Handle")]
    [SerializeField, Min(0.1f)] private float interactionDistance = 3f;
    [SerializeField, Min(0.1f)] private float handleTurnDuration = 2f;
    [SerializeField] private Vector3 handleLocalRotationAxis = Vector3.forward;
    [SerializeField] private bool resetHandleWhenReleased = true;

    // The correct code and maximum number of characters accepted by the PIN field.
    [Header("PIN")]
    [SerializeField] private string correctPin = "1234";
    [SerializeField, Min(1)] private int pinLength = 4;

    // Text shown to the player while the vault is still locked.
    [Header("Prompts")]
    [SerializeField] private string unlockPrompt = "Press E to unlock vault";
    [SerializeField] private string lockedMessage = "This vault is locked, find vault key!";
    [SerializeField, Min(0.1f)] private float lockedMessageDuration = 2f;

    // Runtime values. These are changed while the game is playing and are not setup fields.
    private VaultState state = VaultState.KeyLocked;
    private Camera playerCamera;
    private Quaternion handleStartRotation;
    private float handleProgress;
    private bool lookingAtHandle;
    private bool lookingAtPin;
    private bool lookingAtVaultDoor;
    private bool pinUiOpen;
    private string enteredPin = string.Empty;
    private string pinFeedback = string.Empty;
    private float pinFeedbackTimer;
    private float lockedMessageTimer;

    // Used by KeyUse.cs to know whether this vault can still accept its key.
    public bool NeedsKey => state == VaultState.KeyLocked;

    private void Awake()
    {
        // Find the camera and missing vault references before interaction begins.
        playerCamera = Camera.main;
        Transform vaultRoot = transform.name == "vault door" && transform.parent != null
            ? transform.parent
            : transform;
        if (vaultDoor == null) vaultDoor = GetComponent<DoorInteraction>();
        if (vaultDoor == null) vaultDoor = vaultRoot.GetComponentInChildren<DoorInteraction>();

        if (vaultHandle == null) vaultHandle = vaultRoot.Find("vault handle");
        if (vaultPin == null) vaultPin = vaultRoot.Find("vault PIN");
        if (vaultHandle != null) handleStartRotation = vaultHandle.localRotation;
        // The normal door script stays disabled until the PIN is correct.
        if (vaultDoor != null) vaultDoor.interactionEnabled = false;
    }

    private void Update()
    {
        // While the PIN window is open, only allow closing it with Escape.
        if (pinUiOpen)
        {
            if (pinFeedbackTimer > 0f)
            {
                pinFeedbackTimer -= Time.deltaTime;
                if (pinFeedbackTimer <= 0f) pinFeedback = string.Empty;
            }
            if (Input.GetKeyDown(KeyCode.Escape)) ClosePinUi();
            return;
        }

        lookingAtHandle = false;
        lookingAtPin = false;
        lookingAtVaultDoor = false;
        if (lockedMessageTimer > 0f)
            lockedMessageTimer -= Time.deltaTime;

        if (state == VaultState.Open || PlayerController.IsReadingDocument) return;

        if (playerCamera == null) playerCamera = Camera.main;
        if (playerCamera == null) return;

        // Check what the centre of the player's screen is pointing at.
        Ray ray = new Ray(playerCamera.transform.position, playerCamera.transform.forward);
        bool hitSomething = Physics.Raycast(ray, out RaycastHit hit, interactionDistance, ~0, QueryTriggerInteraction.Ignore);
        bool handleTargeted = (hitSomething && IsChildOf(hit.collider.transform, vaultHandle)) || IsLookingAtTarget(vaultHandle);
        bool pinTargeted = (hitSomething && IsChildOf(hit.collider.transform, vaultPin)) || IsLookingAtTarget(vaultPin);
        bool doorTargeted = vaultDoor != null &&
                            ((hitSomething && IsChildOf(hit.collider.transform, vaultDoor.transform)) ||
                             IsLookingAtTarget(vaultDoor.transform));

        if (state == VaultState.KeyLocked)
        {
            // Players without the key see the vault prompt and locked message here.
            lookingAtVaultDoor = doorTargeted;
            if (lookingAtVaultDoor && Input.GetKeyDown(KeyCode.E))
                lockedMessageTimer = lockedMessageDuration;
            return;
        }

        if (state == VaultState.HandleReady && handleTargeted)
        {
            // The handle only turns while the player looks at it and holds E.
            lookingAtHandle = true;
            UpdateHandleTurn();
            return;
        }

        if (state == VaultState.PinReady && pinTargeted)
        {
            // The PIN can only be entered after the handle makes one full turn.
            lookingAtPin = true;
            if (Input.GetKeyDown(KeyCode.E)) OpenPinUi();
        }
    }

    public bool MatchesKey(string keyTarget)
    {
        // Called by KeyUse.cs to check if the currently held key is the vault key.
        return string.Equals(requiredKeyTarget, keyTarget, System.StringComparison.OrdinalIgnoreCase);
    }

    public bool TryUnlockWithKey()
    {
        // Unlock the key stage only. Do not open the door yet.
        if (state != VaultState.KeyLocked) return false;
        state = VaultState.HandleReady;
        lockedMessageTimer = 0f;
        if (vaultDoor != null)
        {
            vaultDoor.Unlock(false);
            vaultDoor.interactionEnabled = false;
        }
        return true;
    }

    private void UpdateHandleTurn()
    {
        // Releasing E can reset the handle when Reset Handle When Released is enabled.
        if (!Input.GetKey(KeyCode.E))
        {
            if (resetHandleWhenReleased && handleProgress > 0f)
            {
                handleProgress = 0f;
                ApplyHandleRotation();
            }
            return;
        }

        handleProgress = Mathf.Clamp01(handleProgress + Time.deltaTime / handleTurnDuration);
        ApplyHandleRotation();
        if (handleProgress >= 1f) state = VaultState.PinReady;
    }

    private void ApplyHandleRotation()
    {
        // Apply 0 to 360 degrees around the axis selected in the Inspector.
        if (vaultHandle == null) return;
        Vector3 axis = handleLocalRotationAxis.sqrMagnitude > 0f ? handleLocalRotationAxis.normalized : Vector3.forward;
        vaultHandle.localRotation = handleStartRotation * Quaternion.AngleAxis(360f * handleProgress, axis);
    }

    private void OpenPinUi()
    {
        // Stop movement/look input and show the PIN interface.
        pinUiOpen = true;
        enteredPin = string.Empty;
        pinFeedback = string.Empty;
        PlayerController.SetDocumentReading(true);
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    private void ClosePinUi()
    {
        // Restore normal player controls after cancelling or submitting the PIN.
        pinUiOpen = false;
        PlayerController.SetDocumentReading(false);
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    private void SubmitPin()
    {
        // The correct PIN unlocks the normal door script and opens the vault.
        if (enteredPin == correctPin)
        {
            state = VaultState.Open;
            ClosePinUi();
            if (vaultDoor != null)
            {
                vaultDoor.interactionEnabled = true;
                vaultDoor.Open();
            }
            return;
        }

        // A wrong PIN clears the input and briefly shows feedback.
        enteredPin = string.Empty;
        pinFeedback = "Incorrect PIN. Look for the PIN in the Guide Book in the bedroom.";
        pinFeedbackTimer = 3f;
    }

    private void OnGUI()
    {
        // Draw the world interaction prompts and the current PIN window.
        if (pinUiOpen) { DrawPinUi(); return; }
        if (PlayerController.IsReadingDocument) return;

        if (state == VaultState.KeyLocked)
        {
            if (lookingAtVaultDoor)
                DrawCenteredText(unlockPrompt, Color.white, Screen.height * 0.5f + 50f, 22);
            if (lockedMessageTimer > 0f)
                DrawCenteredText(lockedMessage, Color.red, Screen.height * 0.5f + 82f, 22);
            return;
        }

        string prompt = lookingAtHandle
            ? "Hold E to turn vault handle: " + Mathf.RoundToInt(handleProgress * 100f) + "%"
            : lookingAtPin ? "Press E to enter vault PIN" : null;

        if (!string.IsNullOrEmpty(prompt)) DrawCenteredText(prompt, Color.white, Screen.height * 0.5f + 50f, 22);
    }

    private void DrawPinUi()
    {
        // Draw the PIN window in the centre of the screen.
        float width = Mathf.Min(420f, Screen.width - 40f);
        Rect windowRect = new Rect((Screen.width - width) * 0.5f, Screen.height * 0.5f - 145f, width, 290f);
        GUI.Box(windowRect, "Vault PIN");
        GUI.Label(new Rect(windowRect.x + 30f, windowRect.y + 55f, windowRect.width - 60f, 25f), "Enter the vault PIN", CenteredStyle(20, Color.white));

        GUI.SetNextControlName("VaultPinInput");
        enteredPin = GUI.PasswordField(new Rect(windowRect.x + 70f, windowRect.y + 100f, windowRect.width - 140f, 38f), enteredPin, '*', pinLength);
        GUI.FocusControl("VaultPinInput");

        if (!string.IsNullOrEmpty(pinFeedback))
        {
            GUIStyle feedbackStyle = CenteredStyle(18, Color.red);
            feedbackStyle.wordWrap = true;
            GUI.Label(new Rect(windowRect.x + 30f, windowRect.y + 145f, windowRect.width - 60f, 45f), pinFeedback, feedbackStyle);
        }

        bool submit = GUI.Button(new Rect(windowRect.x + 70f, windowRect.y + 205f, 125f, 38f), "Unlock") ||
                      (Event.current.type == EventType.KeyDown && Event.current.keyCode == KeyCode.Return);
        if (submit) SubmitPin();
        if (GUI.Button(new Rect(windowRect.xMax - 195f, windowRect.y + 205f, 125f, 38f), "Cancel")) ClosePinUi();
    }

    private static bool IsChildOf(Transform target, Transform parent)
    {
        // Allows colliders on a child mesh to count as the assigned vault part.
        return target != null && parent != null && (target == parent || target.IsChildOf(parent));
    }

    private bool IsLookingAtTarget(Transform target)
    {
        // Fallback targeting used when the handle or PIN mesh has no collider.
        if (target == null || playerCamera == null)
            return false;

        Vector3 direction = target.position - playerCamera.transform.position;
        float distance = direction.magnitude;
        if (distance > interactionDistance || distance < 0.01f)
            return false;

        return Vector3.Dot(playerCamera.transform.forward, direction / distance) >= 0.98f;
    }

    private static void DrawCenteredText(string message, Color color, float y, int fontSize)
    {
        // Draw readable text with a one-pixel black shadow.
        GUIStyle shadow = CenteredStyle(fontSize, Color.black);
        GUIStyle text = CenteredStyle(fontSize, color);
        Rect rect = new Rect(Screen.width * 0.5f - 250f, y, 500f, 35f);
        GUI.Label(new Rect(rect.x + 1f, rect.y + 1f, rect.width, rect.height), message, shadow);
        GUI.Label(rect, message, text);
    }

    private static GUIStyle CenteredStyle(int fontSize, Color color)
    {
        // Reusable centred text style for prompts and the PIN window.
        GUIStyle style = new GUIStyle(GUI.skin.label) { fontSize = fontSize, alignment = TextAnchor.MiddleCenter };
        style.normal.textColor = color;
        return style;
    }
}
