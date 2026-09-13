using System.Collections;
using UnityEngine;

[RequireComponent(typeof(Collider))]
public class GeneratorFuelInteraction : MonoBehaviour
{
    [Header("Save")]
    public string fuelSaveId = "GeneratorFuel";
    public string coverSaveId = "GeneratorGasTankCover";

    [Header("Interaction")]
    public string requiredItemName = "Gas";
    public KeyCode interactKey = KeyCode.E;
    [Min(0.1f)] public float interactRange = 2.5f;
    [Min(0.1f)] public float pourDuration = 3f;

    [Header("Pour Motion")]
    public Transform pourPoint;
    public Vector3 pourRotationOffset = new Vector3(0f, 0f, 95f);
    public GameObject fueledEffect;

    [Header("Debug")]
    [Tooltip("Prints a message only when the interaction state changes.")]
    public bool debugInteraction = true;

    private Camera playerCamera;
    private bool isPouring;
    private bool isFueled;
    private bool showPrompt;
    private string lastDebugState;

    private void Start()
    {
        playerCamera = Camera.main;
        isFueled = SaveSystem.Instance != null && SaveSystem.Instance.IsGeneratorCoverRemoved(fuelSaveId);
        if (fueledEffect != null) fueledEffect.SetActive(isFueled);
        if (isFueled) DestroyActiveSceneGas();
        ReportDebug(isFueled ? "Generator is already fueled." : "Waiting for cover to open. Cover Save Id=" + coverSaveId);
    }

    private void Update()
    {
        if (isFueled)
        {
            ReportDebug("Generator is already fueled.");
            return;
        }
        if (isPouring)
        {
            ReportDebug("Gas is pouring.");
            return;
        }

        bool coverIsOpen = SaveSystem.Instance != null && SaveSystem.Instance.IsGeneratorCoverRemoved(coverSaveId);
        bool lookingAtGenerator = coverIsOpen && IsLookingAtGenerator();
        GameObject gas = GetSelectedGas();
        showPrompt = lookingAtGenerator && gas != null;

        if (!coverIsOpen)
            ReportDebug("Blocked: cover is not recorded open. Expected Cover Save Id=" + coverSaveId);
        else if (gas == null)
            ReportDebug("Blocked: select an item whose Pickup Item Name is " + requiredItemName + ".");
        else if (!lookingAtGenerator)
            ReportDebug("Blocked: aim at this object's non-trigger collider within " + interactRange + " meters.");
        else
            ReportDebug("Ready: press " + interactKey + " to pour Gas.");

        if (showPrompt && Input.GetKeyDown(interactKey))
        {
            ReportDebug("Pour started.");
            StartCoroutine(PourGas(gas));
        }
    }

    private bool IsLookingAtGenerator()
    {
        if (playerCamera == null) playerCamera = Camera.main;
        if (playerCamera == null) return false;
        Ray ray = playerCamera.ScreenPointToRay(new Vector3(Screen.width * 0.5f, Screen.height * 0.5f));
        if (!Physics.Raycast(ray, out RaycastHit hit, interactRange, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore)) return false;
        return hit.collider.transform == transform || hit.collider.transform.IsChildOf(transform);
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
        if (gas == null) yield break;
        isPouring = true;
        showPrompt = false;
        ReportDebug("Moving Gas to pour point.");

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
            elapsed += Time.deltaTime;
            float progress = Mathf.SmoothStep(0f, 1f, elapsed / pourDuration);
            gas.transform.position = Vector3.Lerp(startPosition, targetPosition, progress);
            gas.transform.rotation = Quaternion.Slerp(startRotation, targetRotation, progress);
            yield return null;
        }

        if (gas != null && Inventory.Instance != null) Inventory.Instance.RemoveAndDestroy(gas);
        else if (gas != null) Destroy(gas);
        isFueled = true;
        isPouring = false;
        if (SaveSystem.Instance != null) SaveSystem.Instance.MarkGeneratorCoverRemoved(fuelSaveId);
        if (fueledEffect != null) fueledEffect.SetActive(true);
        ReportDebug("Pour complete: Gas was consumed and generator state was saved.");
    }

    public bool HasFuel() => isFueled;

    public void ConsumeFuel()
    {
        if (!isFueled) return;
        isFueled = false;
        if (SaveSystem.Instance != null) SaveSystem.Instance.ClearGeneratorState(fuelSaveId);
        if (fueledEffect != null) fueledEffect.SetActive(false);
        ReportDebug("Fuel consumed. Refill Gas to run the generator again.");
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
        Debug.Log("[Generator Fuel Debug] " + message, this);
    }

    private void OnGUI()
    {
        if (!showPrompt || isPouring || isFueled) return;
        GUIStyle shadow = new GUIStyle { fontSize = 22, alignment = TextAnchor.MiddleCenter };
        shadow.normal.textColor = Color.black;
        GUIStyle text = new GUIStyle(shadow); text.normal.textColor = Color.white;
        Rect rect = new Rect(Screen.width * 0.5f - 200f, Screen.height * 0.5f + 50f, 400f, 35f);
        GUI.Label(new Rect(rect.x + 1f, rect.y + 1f, rect.width, rect.height), "Press E to pour Gas into Generator", shadow);
        GUI.Label(rect, "Press E to pour Gas into Generator", text);
    }
}