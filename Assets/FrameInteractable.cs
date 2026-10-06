using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Put this component on each framed picture. When the player looks at the
/// frame and presses the configured key, the assigned UI popup displays it.
/// </summary>
[DisallowMultipleComponent]
public sealed class FrameInteractable : MonoBehaviour
{
    // Camera and raycast settings used to detect the framed picture.
    [Header("Interaction")]
    [SerializeField] private Camera playerCamera;
    [SerializeField] private KeyCode interactKey = KeyCode.E;
    [SerializeField, Min(0.1f)] private float interactionDistance = 3f;
    [SerializeField] private LayerMask interactionLayers = ~0;

    [Header("Frame Content")]
    [SerializeField] private Sprite frameImage;
    [SerializeField] private string frameTitle = "Family Portrait";
    [SerializeField, TextArea(2, 6)] private string frameDescription;
    [SerializeField] private bool preserveImageAspect = true;

    [Header("Popup UI")]
    [SerializeField] private GameObject popupRoot;
    [SerializeField] private Image popupArtwork;
    [SerializeField] private TMP_Text popupTitle;
    [SerializeField] private TMP_Text popupDescription;
    [SerializeField] private Button closeButton;
    [SerializeField] private KeyCode closeKey = KeyCode.Escape;

    [Header("While Popup Is Open")]
    [Tooltip("Add the player's movement/look scripts here so they do not move behind the popup.")]
    [SerializeField] private Behaviour[] gameplayBehavioursToDisable;
    [SerializeField] private bool unlockCursor = true;
    [SerializeField] private bool pauseTime = false;

    private static FrameInteractable activePopup;

    private bool[] previousBehaviourStates;
    private CursorLockMode previousCursorLockMode;
    private bool previousCursorVisibility;
    private float previousTimeScale;
    private bool isLookingAtFrame;

    private void Awake()
    {
        // Ensure the popup starts hidden and has a camera reference.
        if (playerCamera == null)
            playerCamera = Camera.main;

        if (popupRoot != null)
            popupRoot.SetActive(false);
    }

    private void Update()
    {
        // Only one frame popup can be open at a time.
        if (PauseMenu.Instance != null && PauseMenu.Instance.isPaused)
        {
            isLookingAtFrame = false;
            return;
        }
        if (activePopup != null)
        {
            if (activePopup == this && (Input.GetKeyDown(closeKey) || Input.GetKeyDown(interactKey)))
                ClosePopup();

            return;
        }

        if (playerCamera == null)
            playerCamera = Camera.main;

        if (playerCamera == null)
            return;

        isLookingAtFrame = false;
        Ray ray = new Ray(playerCamera.transform.position, playerCamera.transform.forward);
        if (Physics.Raycast(ray, out RaycastHit hit, interactionDistance, interactionLayers,
                QueryTriggerInteraction.Ignore) &&
            hit.collider.GetComponentInParent<FrameInteractable>() == this)
        {
            isLookingAtFrame = true;

            if (Input.GetKeyDown(interactKey))
                OpenPopup();
        }
    }

    private void OnGUI()
    {
        if ((PauseMenu.Instance != null && PauseMenu.Instance.isPaused) ||
            PlayerController.IsReadingDocument || activePopup != null || !isLookingAtFrame)
            return;

        string message = $"Press {interactKey} to interact";

        GUIStyle shadow = new GUIStyle
        {
            fontSize = 22,
            alignment = TextAnchor.MiddleCenter
        };
        shadow.normal.textColor = Color.black;

        GUIStyle text = new GUIStyle(shadow);
        text.normal.textColor = Color.white;

        Rect promptRect = new Rect(Screen.width * 0.5f - 200f, Screen.height * 0.5f + 50f, 400f, 40f);
        GUI.Label(new Rect(promptRect.x + 1f, promptRect.y + 1f, promptRect.width, promptRect.height), message, shadow);
        GUI.Label(promptRect, message, text);
    }

    private void OnDisable()
    {
        if (activePopup == this)
            ClosePopup();
    }

    public void OpenPopup()
    {
        // Fill the Canvas popup with this frame's image and text, then stop player movement.
        if (activePopup != null || popupRoot == null)
        {
            if (popupRoot == null)
                Debug.LogWarning($"{nameof(FrameInteractable)} on '{name}' needs a Popup Root assigned.", this);
            return;
        }

        activePopup = this;
        PlayerController.SetDocumentReading(true);

        if (popupArtwork == null && popupRoot != null)
        {
            foreach (Image candidate in popupRoot.GetComponentsInChildren<Image>(true))
            {
                if (candidate.transform != popupRoot.transform)
                {
                    popupArtwork = candidate;
                    break;
                }
            }
        }

        if (popupArtwork != null)
        {
            popupArtwork.overrideSprite = null;
            popupArtwork.sprite = frameImage;
            popupArtwork.preserveAspect = preserveImageAspect;
            popupArtwork.enabled = frameImage != null;
        }

        if (popupTitle != null)
            popupTitle.text = frameTitle;

        if (popupDescription != null)
            popupDescription.text = frameDescription;

        popupRoot.SetActive(true);

        if (closeButton != null)
        {
            closeButton.onClick.RemoveListener(ClosePopup);
            closeButton.onClick.AddListener(ClosePopup);
        }

        if (gameplayBehavioursToDisable != null)
        {
            previousBehaviourStates = new bool[gameplayBehavioursToDisable.Length];
            for (int i = 0; i < gameplayBehavioursToDisable.Length; i++)
            {
                Behaviour behaviour = gameplayBehavioursToDisable[i];
                if (behaviour == null || behaviour == this)
                    continue;

                previousBehaviourStates[i] = behaviour.enabled;
                behaviour.enabled = false;
            }
        }

        if (unlockCursor)
        {
            previousCursorLockMode = Cursor.lockState;
            previousCursorVisibility = Cursor.visible;
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }

        if (pauseTime)
        {
            previousTimeScale = Time.timeScale;
            Time.timeScale = 0f;
        }
    }

    public void ClosePopup()
    {
        // Hide the popup and restore the cursor, movement, and previous time scale.
        if (activePopup != this)
            return;

        if (popupRoot != null)
            popupRoot.SetActive(false);

        if (closeButton != null)
            closeButton.onClick.RemoveListener(ClosePopup);

        if (gameplayBehavioursToDisable != null && previousBehaviourStates != null)
        {
            for (int i = 0; i < gameplayBehavioursToDisable.Length; i++)
            {
                Behaviour behaviour = gameplayBehavioursToDisable[i];
                if (behaviour != null && behaviour != this)
                    behaviour.enabled = previousBehaviourStates[i];
            }
        }

        if (unlockCursor)
        {
            Cursor.lockState = previousCursorLockMode;
            Cursor.visible = previousCursorVisibility;
        }

        if (pauseTime)
            Time.timeScale = previousTimeScale;

        previousBehaviourStates = null;
        PlayerController.SetDocumentReading(false);
        activePopup = null;
    }
}
