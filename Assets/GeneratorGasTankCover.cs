using UnityEngine;

/// <summary>
/// Put this on GasTankCoverPivot. Hold E while looking at it with the Wrench
/// selected to unscrew, lift, and drop the gas tank cover.
/// </summary>
[RequireComponent(typeof(Collider))]
public class GeneratorGasTankCover : MonoBehaviour
{
    [Header("Save")]
    [Tooltip("Must be unique if you add more than one generator cover in a chapter.")]
    public string saveId = "GeneratorGasTankCover";

    [Header("Interaction")]
    public string requiredItemName = "Wrench";
    public KeyCode interactKey = KeyCode.E;
    [Min(0.1f)] public float interactRange = 2.5f;
    [Min(0.1f)] public float holdDuration = 2f;

    [Header("Unscrewing Motion")]
    [Min(0f)] public float spinDegrees = 1080f;
    [Min(0f)] public float liftDistance = 0.2f;
    public Vector3 localSpinAxis = Vector3.up;

    [Header("Drop")]
    [Tooltip("Optional. Leave empty to use the Rigidbody on this pivot.")]
    public Rigidbody coverRigidbody;
    public Vector3 fallImpulse = new Vector3(0.2f, 0.4f, 0.2f);
    [Tooltip("Seconds after the cover falls before it is removed from the scene.")]
    [Min(0f)] public float destroyDelayAfterRemoval = 5f;

    private Camera playerCamera;
    private Vector3 closedLocalPosition;
    private Quaternion closedLocalRotation;
    private float holdProgress;
    private bool isRemoved;
    private bool showPrompt;

    private void Start()
    {
        // A cover already opened in this normal/hard save remains gone.
        if (SaveSystem.Instance != null && SaveSystem.Instance.IsGeneratorCoverRemoved(saveId))
        {
            Destroy(gameObject);
            return;
        }

        playerCamera = Camera.main;
        closedLocalPosition = transform.localPosition;
        closedLocalRotation = transform.localRotation;

        if (coverRigidbody == null)
            coverRigidbody = GetComponent<Rigidbody>();

        // The cover must remain attached until the interaction is complete.
        if (coverRigidbody != null)
        {
            coverRigidbody.isKinematic = true;
            coverRigidbody.useGravity = false;
        }
    }

    private void Update()
    {
        if (isRemoved)
            return;

        bool isLookingAtCover = IsLookingAtCover();
        bool holdingWrench = IsHoldingRequiredItem();
        showPrompt = isLookingAtCover && holdingWrench;

        if (isLookingAtCover && holdingWrench && Input.GetKey(interactKey))
        {
            holdProgress = Mathf.MoveTowards(holdProgress, 1f, Time.deltaTime / holdDuration);
            AnimateUnscrewing(holdProgress);

            if (holdProgress >= 1f)
                RemoveCover();
        }
        else if (holdProgress > 0f)
        {
            // Releasing E or looking away resets the partly-unscrewed cover.
            holdProgress = 0f;
            transform.localPosition = closedLocalPosition;
            transform.localRotation = closedLocalRotation;
        }
    }

    private bool IsLookingAtCover()
    {
        if (playerCamera == null)
            playerCamera = Camera.main;
        if (playerCamera == null)
            return false;

        Ray ray = playerCamera.ScreenPointToRay(new Vector3(Screen.width * 0.5f, Screen.height * 0.5f));
        if (!Physics.Raycast(ray, out RaycastHit hit, interactRange, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore))
            return false;

        return hit.collider.transform == transform || hit.collider.transform.IsChildOf(transform);
    }

    private bool IsHoldingRequiredItem()
    {
        if (Inventory.Instance == null)
            return false;

        int selected = Inventory.Instance.GetSelectedIndex();
        var items = Inventory.Instance.GetItems();
        if (selected < 0 || selected >= items.Count || items[selected] == null)
            return false;

        GameObject heldItem = items[selected];
        PickupItem pickup = heldItem.GetComponent<PickupItem>();
        string itemName = pickup != null && !string.IsNullOrEmpty(pickup.itemName)
            ? pickup.itemName
            : heldItem.name;

        return itemName.ToLowerInvariant().Contains(requiredItemName.ToLowerInvariant());
    }

    private void AnimateUnscrewing(float progress)
    {
        transform.localPosition = closedLocalPosition + Vector3.up * (liftDistance * progress);
        transform.localRotation = closedLocalRotation * Quaternion.AngleAxis(spinDegrees * progress, localSpinAxis.normalized);
    }

    private void RemoveCover()
    {
        isRemoved = true;
        showPrompt = false;

        if (SaveSystem.Instance != null)
            SaveSystem.Instance.MarkGeneratorCoverRemoved(saveId);

        // Detach first so the cover is no longer moved by the generator hierarchy.
        transform.SetParent(null, true);

        if (coverRigidbody == null)
            coverRigidbody = gameObject.AddComponent<Rigidbody>();

        coverRigidbody.isKinematic = false;
        coverRigidbody.useGravity = true;
        coverRigidbody.velocity = Vector3.zero;
        coverRigidbody.angularVelocity = Vector3.zero;
        coverRigidbody.AddForce(transform.TransformDirection(fallImpulse), ForceMode.Impulse);

        Destroy(gameObject, destroyDelayAfterRemoval);
    }

    private void OnGUI()
    {
        if (!showPrompt || isRemoved)
            return;

        int percent = Mathf.RoundToInt(holdProgress * 100f);
        string message = holdProgress > 0f
            ? "Hold E to unscrew Gas Tank Cover: " + percent + "%"
            : "Hold E to use Wrench";

        GUIStyle shadow = new GUIStyle();
        shadow.fontSize = 22;
        shadow.alignment = TextAnchor.MiddleCenter;
        shadow.normal.textColor = Color.black;

        GUIStyle text = new GUIStyle(shadow);
        text.normal.textColor = Color.white;

        Rect rect = new Rect(Screen.width * 0.5f - 200f, Screen.height * 0.5f + 50f, 400f, 35f);
        GUI.Label(new Rect(rect.x + 1f, rect.y + 1f, rect.width, rect.height), message, shadow);
        GUI.Label(rect, message, text);
    }
}