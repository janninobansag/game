using System.Collections;
using UnityEngine;

/// Attach this to the Generator Key Slot collider in Chapter 2.
[RequireComponent(typeof(Collider))]
public class GeneratorKeySlot : MonoBehaviour
{
    [Header("References")]
    public GeneratorFuelInteraction fuelInteraction;
    public Transform keyInsertPoint;
    public Light generatorLight1;
    [Tooltip("Optional second generator light. Both lights turn on and off together.")]
    public Light generatorLight2;
    [Tooltip("Add every other duplicated generator light here. All assigned lights turn on and off together.")]
    public Light[] additionalGeneratorLights;

    [Header("Key")]
    public string requiredKeyName = "Generator key";
    public string keyInsertedSaveId = "GeneratorKeyInserted";
    public KeyCode interactKey = KeyCode.E;
    [Min(0.1f)] public float interactRange = 2.5f;
    [Min(1f)] public float runningSeconds = 60f;
    public Vector3 turnRotation = new Vector3(0f, 90f, 0f);
    [Tooltip("Local scale after insertion. Generatorbox is scaled to 100, so 0.01 keeps the key normal-sized.")]
    public Vector3 insertedKeyLocalScale = new Vector3(0.01f, 0.01f, 0.01f);

    [Header("Debug")]
    public bool debugKeySlot = true;

    private Camera playerCamera;
    private GameObject insertedKey;
    private Quaternion insertedRotation;
    private bool isRunning;
    private bool showPrompt;
    private string lastDebugState;

    private void Start()
    {
        playerCamera = Camera.main;
        // The generator area starts lit. A separate GeneratorLightsOffTrigger
        // handles the scripted blink and blackout when the player reaches it.
        SetGeneratorLights(true);
        if (SaveSystem.Instance != null && SaveSystem.Instance.IsGeneratorCoverRemoved(keyInsertedSaveId))
            RestoreInsertedKey();
    }

    private void Update()
    {
        showPrompt = false;
        if (!IsLookingAtSlot()) { ReportDebug("Blocked: aim at the key-slot collider within " + interactRange + " meters."); return; }

        if (insertedKey == null)
        {
            GameObject key = GetSelectedKey();
            if (key == null) { ReportDebug("Blocked: insert Generator key first."); return; }
            ReportDebug("Ready: press E to insert Generator key.");
            showPrompt = true;
            if (Input.GetKeyDown(interactKey)) InsertKey(key, true);
            return;
        }

        if (isRunning) { ReportDebug("Generator is currently running."); return; }
        if (fuelInteraction == null) { ReportDebug("Blocked: Fuel Interaction reference is missing."); return; }
        if (!fuelInteraction.HasFuel()) { ReportDebug("Blocked: generator has no fuel. Refill Gas first."); return; }

        ReportDebug("Ready: press E to turn Generator on for " + runningSeconds + " seconds.");
        showPrompt = true;
        if (Input.GetKeyDown(interactKey)) StartCoroutine(RunGenerator());
    }
    private bool IsLookingAtSlot()
    {
        if (playerCamera == null) playerCamera = Camera.main;
        if (playerCamera == null) return false;
        Ray ray = playerCamera.ScreenPointToRay(new Vector3(Screen.width * .5f, Screen.height * .5f));
        if (!Physics.Raycast(ray, out RaycastHit hit, interactRange, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore)) return false;
        return hit.collider.transform == transform || hit.collider.transform.IsChildOf(transform);
    }

    private GameObject GetSelectedKey()
    {
        if (Inventory.Instance == null) return null;
        int selected = Inventory.Instance.GetSelectedIndex();
        var items = Inventory.Instance.GetItems();
        if (selected < 0 || selected >= items.Count || items[selected] == null) return null;
        GameObject item = items[selected];
        PickupItem pickup = item.GetComponent<PickupItem>();
        Key key = item.GetComponent<Key>();
        string itemName = pickup != null ? pickup.itemName : key != null ? key.itemName : item.name;
        return string.Equals(itemName, requiredKeyName, System.StringComparison.OrdinalIgnoreCase) ? item : null;
    }

    private void InsertKey(GameObject key, bool save)
    {
        if (key == null) return;
        if (Inventory.Instance != null) Inventory.Instance.RemoveWithoutDestroy(key);
        key.transform.SetParent(keyInsertPoint != null ? keyInsertPoint : transform, false);
        key.transform.localPosition = Vector3.zero;
        key.transform.localRotation = Quaternion.identity;
        key.transform.localScale = insertedKeyLocalScale;
        insertedRotation = key.transform.localRotation;
        insertedKey = key;
        foreach (Collider c in key.GetComponentsInChildren<Collider>()) c.enabled = false;
        Rigidbody body = key.GetComponent<Rigidbody>();
        if (body != null) { body.isKinematic = true; body.useGravity = false; }
        PickupItem pickup = key.GetComponent<PickupItem>();
        if (pickup != null) { pickup.isHeld = false; pickup.enabled = false; }
        Key keyComponent = key.GetComponent<Key>();
        if (keyComponent != null) keyComponent.enabled = false;
        if (save && SaveSystem.Instance != null) SaveSystem.Instance.MarkGeneratorCoverRemoved(keyInsertedSaveId);
    }

    private IEnumerator RunGenerator()
    {
        isRunning = true;
        showPrompt = false;
        SetGeneratorLights(true);
        if (insertedKey != null) insertedKey.transform.localRotation = insertedRotation * Quaternion.Euler(turnRotation);
        yield return new WaitForSeconds(runningSeconds);
        SetGeneratorLights(false);
        if (insertedKey != null) insertedKey.transform.localRotation = insertedRotation;
        if (fuelInteraction != null) fuelInteraction.ConsumeFuel();
        isRunning = false;
    }

    private void SetGeneratorLights(bool enabled)
    {
        SetLightState(generatorLight1, enabled);
        SetLightState(generatorLight2, enabled);
        if (additionalGeneratorLights == null) return;
        foreach (Light light in additionalGeneratorLights)
            SetLightState(light, enabled);
    }

    private static void SetLightState(Light light, bool enabled)
    {
        if (light != null) light.enabled = enabled;
    }

    private void RestoreInsertedKey()
    {
        foreach (GameObject obj in Resources.FindObjectsOfTypeAll<GameObject>())
        {
            if (obj == null || obj.scene != gameObject.scene) continue;
            PickupItem pickup = obj.GetComponent<PickupItem>();
            Key key = obj.GetComponent<Key>();
            string itemName = pickup != null ? pickup.itemName : key != null ? key.itemName : obj.name;
            if (!string.Equals(itemName, requiredKeyName, System.StringComparison.OrdinalIgnoreCase)) continue;
            InsertKey(obj, false);
            return;
        }
    }

    private void ReportDebug(string message)
    {
        if (!debugKeySlot || lastDebugState == message) return;
        lastDebugState = message;
        Debug.Log("[Generator Key Debug] " + message, this);
    }
    private void OnGUI()
    {
        if (!showPrompt || isRunning) return;
        string message = insertedKey == null ? "Press E to insert Generator Key" : "Press E to turn Generator on";
        GUIStyle shadow = new GUIStyle { fontSize = 22, alignment = TextAnchor.MiddleCenter };
        shadow.normal.textColor = Color.black;
        GUIStyle text = new GUIStyle(shadow); text.normal.textColor = Color.white;
        Rect rect = new Rect(Screen.width * .5f - 200f, Screen.height * .5f + 50f, 400f, 35f);
        GUI.Label(new Rect(rect.x + 1f, rect.y + 1f, rect.width, rect.height), message, shadow);
        GUI.Label(rect, message, text);
    }
}