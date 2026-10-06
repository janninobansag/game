// PURPOSE: Lets the player pour Gas into the generator after its cover is opened and records the fuel state.
using System.Collections;
using UnityEngine;

[RequireComponent(typeof(Collider))]
public class GeneratorFuelInteraction : MonoBehaviour
{
    // Persistent identifiers used to restore fuel and gas-cover state after loading.
    [Header("Save")]
    public string fuelSaveId = "GeneratorFuel";
    public string coverSaveId = "GeneratorGasTankCover";

    [Header("Interaction")]
    [Tooltip("Generator root to recognize when aiming at any of its visible parts. Leave empty to use this object only.")]
    public Transform interactionRoot;
    public string requiredItemName = "Gas";
    public KeyCode interactKey = KeyCode.E;
    [Min(0.1f)] public float interactRange = 2.5f;
    [Min(0.1f)] public float pourDuration = 3f;

    [Header("Pour Motion")]
    public Transform pourPoint;
    public Vector3 pourRotationOffset = new Vector3(0f, 0f, 95f);
    public GameObject fueledEffect;

    [Header("Debug")]
    [Tooltip("Log cover state only when the cover is removed or Gas is aimed at the generator.")]
    public bool debugInteraction = true;

    private Camera playerCamera;
    private bool isPouring;
    private bool isFueled;
    private bool coverRemovedThisSession;
    private bool coverOpenFromSave;
    private bool coverMissingFromGenerator;
    private bool showPrompt;
    private bool showClosedCoverPrompt;
    private float pourProgress;
    private string lastDebugState;

    private void Start()
    {
        // Restore previously saved fuel and cover state for this generator.
        playerCamera = Camera.main;
        isFueled = SaveSystem.Instance != null && SaveSystem.Instance.IsGeneratorCoverRemoved(fuelSaveId);
        coverOpenFromSave = SaveSystem.Instance != null && SaveSystem.Instance.IsGeneratorCoverRemoved(coverSaveId);
        if (fueledEffect != null) fueledEffect.SetActive(isFueled);
        if (isFueled) DestroyActiveSceneGas();
    }

    private void Update()
    {
        // Start, continue, or cancel pouring based on the selected Gas item and held E key.
        if (isFueled || isPouring) return;

        if (!coverOpenFromSave && !coverRemovedThisSession && SaveSystem.Instance != null)
            coverOpenFromSave = SaveSystem.Instance.IsGeneratorCoverRemoved(coverSaveId);
        // The cover is detached from this generator when it is opened. This also
        // handles an already-detached cover when scripts reload during Play Mode.
        if (!coverMissingFromGenerator && interactionRoot != null)
            coverMissingFromGenerator = interactionRoot.GetComponentInChildren<GeneratorGasTankCover>(true) == null;
        bool coverIsOpen = coverRemovedThisSession || coverOpenFromSave || coverMissingFromGenerator;
        bool lookingAtGenerator = IsLookingAtGenerator();
        GameObject gas = GetSelectedGas();
        showPrompt = coverIsOpen && lookingAtGenerator && gas != null;
        showClosedCoverPrompt = !coverIsOpen && lookingAtGenerator && gas != null;

        if (lookingAtGenerator && gas != null)
            ReportDebug("Gas aimed at Generator. Cover event=" + coverRemovedThisSession +
                ", cover detached=" + coverMissingFromGenerator +
                ", saved cover=" + coverOpenFromSave + ", can pour=" + showPrompt + ".");
        else
            lastDebugState = null;

        if (showPrompt && Input.GetKeyDown(interactKey))
            StartCoroutine(PourGas(gas));
    }

    public void NotifyCoverRemoved(string removedCoverId)
    {
        // Called by the cover interaction so fuel cannot be poured while the cover is closed.
        if (!string.Equals(removedCoverId, coverSaveId, System.StringComparison.Ordinal)) return;
        coverRemovedThisSession = true;
        coverOpenFromSave = SaveSystem.Instance != null && SaveSystem.Instance.IsGeneratorCoverRemoved(coverSaveId);
        ReportDebug("Cover removal received. SaveSystem present=" + (SaveSystem.Instance != null) +
            ", saved cover=" + coverOpenFromSave + ".");
    }

    private bool IsLookingAtGenerator()
    {
        if (playerCamera == null) playerCamera = Camera.main;
        if (playerCamera == null) return false;
        Ray ray = playerCamera.ScreenPointToRay(new Vector3(Screen.width * 0.5f, Screen.height * 0.5f));
        if (!Physics.Raycast(ray, out RaycastHit hit, interactRange, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore)) return false;
        Transform root = interactionRoot != null ? interactionRoot : transform;
        return hit.collider.transform == root || hit.collider.transform.IsChildOf(root);
    }

    private GameObject GetSelectedGas()
    {
        if (Inventory.Instance == null) return null;
        int selected = Inventory.Instance.GetSelectedIndex();
        var items = Inventory.Instance.GetItems();
        if (selected < 0 || selected >= items.Count) return null;
        GameObject item = items[selected];
        if (item == null) return null;
        PickupItem pickup = item.GetComponent<PickupItem>();
        string itemName = pickup != null && !string.IsNullOrEmpty(pickup.itemName) ? pickup.itemName : item.name;
        return IsAcceptedGasName(itemName) ? item : null;
    }

    private bool IsAcceptedGasName(string itemName)
    {
        if (string.IsNullOrEmpty(itemName)) return false;
        return string.Equals(itemName, requiredItemName, System.StringComparison.OrdinalIgnoreCase) ||
               itemName.StartsWith(requiredItemName + " ", System.StringComparison.OrdinalIgnoreCase);
    }
    private IEnumerator PourGas(GameObject gas)
    {
        // Tilt the selected gas can, show progress, then consume it after pouring finishes.
        if (gas == null) yield break;
        isPouring = true;
        showPrompt = false;
        showClosedCoverPrompt = false;

        PickupItem pickup = gas.GetComponent<PickupItem>();
        if (pickup != null) { pickup.isHeld = false; pickup.enabled = false; }
        foreach (Collider itemCollider in gas.GetComponentsInChildren<Collider>()) itemCollider.enabled = false;
        Rigidbody body = gas.GetComponent<Rigidbody>();
        if (body != null) { body.isKinematic = true; body.useGravity = false; body.velocity = Vector3.zero; body.angularVelocity = Vector3.zero; }

        gas.transform.SetParent(null, true);
        Vector3 startPosition = gas.transform.position;
        Quaternion startRotation = gas.transform.rotation;
        Vector3 targetPosition = pourPoint != null ? pourPoint.position : transform.position + transform.up * 0.35f;
        Quaternion targetRotation = startRotation * Quaternion.Euler(pourRotationOffset);
        float elapsed = 0f;
        while (elapsed < pourDuration && gas != null)
        {
            if (!Input.GetKey(interactKey))
            {
                CancelPouring(gas);
                yield break;
            }

            elapsed += Time.deltaTime;
            pourProgress = Mathf.Clamp01(elapsed / pourDuration);
            float progress = Mathf.SmoothStep(0f, 1f, pourProgress);
            gas.transform.position = Vector3.Lerp(startPosition, targetPosition, progress);
            gas.transform.rotation = Quaternion.Slerp(startRotation, targetRotation, progress);
            yield return null;
        }

        if (gas != null && Inventory.Instance != null) Inventory.Instance.RemoveAndDestroy(gas);
        else if (gas != null) Destroy(gas);
        pourProgress = 1f;
        isFueled = true;
        isPouring = false;
        if (SaveSystem.Instance != null) SaveSystem.Instance.MarkGeneratorCoverRemoved(fuelSaveId);
        if (fueledEffect != null) fueledEffect.SetActive(true);
    }

    private void CancelPouring(GameObject gas)
    {
        isPouring = false;
        pourProgress = 0f;

        if (gas == null) return;

        PickupItem pickup = gas.GetComponent<PickupItem>();
        if (pickup != null)
        {
            pickup.enabled = true;
            pickup.isPickedUp = true;
            pickup.isHeld = true;
            pickup.wasDropped = false;
        }

        foreach (Collider itemCollider in gas.GetComponentsInChildren<Collider>())
            itemCollider.enabled = false;

        Rigidbody body = gas.GetComponent<Rigidbody>();
        if (body != null)
        {
            body.isKinematic = true;
            body.useGravity = false;
            body.velocity = Vector3.zero;
            body.angularVelocity = Vector3.zero;
        }

        if (Inventory.Instance != null && Inventory.Instance.GetItems().Contains(gas))
            Inventory.Instance.SetItemHeld(gas, true);
    }

    public bool HasFuel() => isFueled;

    public void ConsumeFuel()
    {
        // Removes the fueled state when another system consumes generator fuel.
        if (!isFueled) return;
        isFueled = false;
        if (SaveSystem.Instance != null) SaveSystem.Instance.ClearGeneratorState(fuelSaveId);
        if (fueledEffect != null) fueledEffect.SetActive(false);
    }
    private void DestroyActiveSceneGas()
    {
        foreach (PickupItem pickup in Resources.FindObjectsOfTypeAll<PickupItem>())
        {
            if (pickup == null || !pickup.enabled || pickup.gameObject.scene != gameObject.scene) continue;
            if (!IsAcceptedGasName(pickup.itemName)) continue;
            if (Inventory.Instance != null) Inventory.Instance.RemoveAndDestroy(pickup.gameObject);
            else Destroy(pickup.gameObject);
            return;
        }
    }

    private void ReportDebug(string message)
    {
        if (!debugInteraction || lastDebugState == message) return;
        lastDebugState = message;
        Debug.Log("Generator fuel: " + message, this);
    }

    private void OnGUI()
    {
        if (isFueled) return;
        GUIStyle shadow = new GUIStyle { fontSize = 22, alignment = TextAnchor.MiddleCenter };
        shadow.normal.textColor = Color.black;
        GUIStyle text = new GUIStyle(shadow); text.normal.textColor = Color.white;
        Rect rect = new Rect(Screen.width * 0.5f - 200f, Screen.height * 0.5f + 50f, 400f, 35f);
        string message;
        if (isPouring)
            message = "Hold " + interactKey + " to pour Gas: " + Mathf.RoundToInt(pourProgress * 100f) + "%";
        else if (showClosedCoverPrompt)
            message = "Remove the gas tank cover first";
        else if (showPrompt)
            message = "Hold " + interactKey + " to pour Gas";
        else
            return;
        GUI.Label(new Rect(rect.x + 1f, rect.y + 1f, rect.width, rect.height), message, shadow);
        GUI.Label(rect, message, text);
    }
}
