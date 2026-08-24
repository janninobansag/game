using UnityEngine;

public class CandleHolder : MonoBehaviour
{
    [Header("Holder Settings")]
    public Transform placementPoint; // where candle will be placed
    public float interactRange = 2f;
    public KeyCode interactKey = KeyCode.E;
    public string promptMessage = "Press E to place Candle";

    [Header("Optional — Trigger Objective")]
    public ObjectiveTrigger objectiveTrigger;
    public string objectiveText = "Candle placed on the vase";

    private bool hasCandle = false;
    private bool showPrompt = false;
    private Camera playerCamera;
    private GameObject placedCandle;

    void Start()
    {
        playerCamera = Camera.main;

        // Auto create placement point if not assigned
        if (placementPoint == null)
        {
            GameObject point = new GameObject("CandlePlacementPoint");
            point.transform.SetParent(transform);
            point.transform.localPosition = new Vector3(0f, 0.5f, 0f);
            placementPoint = point.transform;
        }
    }

    void Update()
    {
        if (hasCandle) return;

        showPrompt = false;

        // Only show prompt if player is holding candle
        if (!IsHoldingCandle()) return;

        // Raycast check
        Ray ray = playerCamera.ScreenPointToRay(
            new Vector3(Screen.width / 2, Screen.height / 2));
        RaycastHit hit;

        if (Physics.Raycast(ray, out hit, interactRange))
        {
            if (hit.collider.gameObject == gameObject ||
                hit.collider.transform.IsChildOf(transform))
            {
                showPrompt = true;

                if (Input.GetKeyDown(interactKey))
                    PlaceCandle();
            }
        }
    }

    bool IsHoldingCandle()
    {
        if (Inventory.Instance == null) return false;

        var items = Inventory.Instance.GetItems();
        int selected = Inventory.Instance.GetSelectedIndex();

        if (selected < 0 || selected >= items.Count) return false;

        GameObject held = items[selected];
        if (held == null) return false;

        // Check if held item is a candle
        CandleItem ci = held.GetComponent<CandleItem>();
        PickupItem pi = held.GetComponent<PickupItem>();

        if (ci != null) return true;
        if (pi != null && pi.itemName.ToLower().Contains("candle")) return true;

        return false;
    }

    void PlaceCandle()
    {
        if (Inventory.Instance == null) return;

        var items = Inventory.Instance.GetItems();
        int selected = Inventory.Instance.GetSelectedIndex();

        if (selected < 0 || selected >= items.Count) return;

        GameObject candle = items[selected];
        if (candle == null) return;

        string candleName = candle.name.Replace("(Clone)", "");

        // Remove from inventory
        Inventory.Instance.RemoveItem(candle);

        // Place candle on vase
        candle.transform.SetParent(placementPoint);
        candle.transform.localPosition = Vector3.zero;
        candle.transform.localRotation = Quaternion.identity;

        // Enable renderers
        foreach (Renderer r in candle.GetComponentsInChildren<Renderer>())
            r.enabled = true;

        // Disable physics
        Rigidbody rb = candle.GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.isKinematic = true;
            rb.useGravity = false;
        }

        // Disable collider so player doesn't interact with it again
        foreach (Collider c in candle.GetComponentsInChildren<Collider>())
            c.enabled = false;

        // Disable pickup scripts
        PickupItem pi = candle.GetComponent<PickupItem>();
        if (pi != null) pi.enabled = false;

        CandleItem ci = candle.GetComponent<CandleItem>();
        if (ci != null) ci.SetHeld(true); // keep light on

        placedCandle = candle;
        hasCandle = true;
        showPrompt = false;

        // ── SAVE: Mark as placed in database ──
        if (SaveSystem.Instance != null)
        {
            SaveSystem.Instance.MarkRitualItemAsPlaced(
                candleName,
                candle.transform.position,
                candle.transform.rotation
            );
        }

        // Trigger objective if assigned
        if (objectiveTrigger != null)
            objectiveTrigger.TriggerObjective();
    }

    public bool HasCandle() => hasCandle;
    public GameObject GetPlacedCandle() => placedCandle;

    void OnGUI()
    {
        if (!showPrompt || hasCandle) return;

        GUIStyle style = new GUIStyle();
        style.fontSize = 22;
        style.alignment = TextAnchor.MiddleCenter;
        style.normal.textColor = Color.white;

        GUIStyle shadow = new GUIStyle();
        shadow.fontSize = 22;
        shadow.alignment = TextAnchor.MiddleCenter;
        shadow.normal.textColor = Color.black;

        GUI.Label(new Rect(Screen.width / 2 - 199,
            Screen.height / 2 + 51, 400, 40),
            promptMessage, shadow);
        GUI.Label(new Rect(Screen.width / 2 - 200,
            Screen.height / 2 + 50, 400, 40),
            promptMessage, style);
    }

    void OnDrawGizmosSelected()
    {
        if (placementPoint == null) return;
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(placementPoint.position, 0.1f);
    }
}