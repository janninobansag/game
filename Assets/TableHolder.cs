using UnityEngine;

public class TableHolder : MonoBehaviour
{
    [Header("Holder Settings")]
    public Transform placementPoint;
    public float interactRange = 2f;
    public KeyCode interactKey = KeyCode.E;
    public string itemNameRequired = "Bible";
    public string promptMessage = "Press E to place Bible";

    [Header("Optional — Trigger Objective")]
    public ObjectiveTrigger objectiveTrigger;

    [Header("Optional — Trigger Audio")]
    public AudioTrigger audioTrigger;

    private bool hasItem = false;
    private bool showPrompt = false;
    private Camera playerCamera;
    private GameObject placedItem;

    void Start()
    {
        playerCamera = Camera.main;

        // Auto create placement point if not assigned
        if (placementPoint == null)
        {
            GameObject point = new GameObject("PlacementPoint");
            point.transform.SetParent(transform);
            point.transform.localPosition = new Vector3(0f, 0.5f, 0f);
            placementPoint = point.transform;
        }
    }

    void Update()
    {
        if (hasItem) return;

        showPrompt = false;

        if (!IsHoldingRequiredItem()) return;

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
                    PlaceItem();
            }
        }
    }

    bool IsHoldingRequiredItem()
    {
        if (Inventory.Instance == null) return false;

        var items = Inventory.Instance.GetItems();
        int selected = Inventory.Instance.GetSelectedIndex();

        if (selected < 0 || selected >= items.Count) return false;

        GameObject held = items[selected];
        if (held == null) return false;

        PickupItem pi = held.GetComponent<PickupItem>();
        if (pi != null && pi.itemName.ToLower()
            .Contains(itemNameRequired.ToLower()))
            return true;

        // Also check by object name
        if (held.name.ToLower().Contains(itemNameRequired.ToLower()))
            return true;

        return false;
    }

    void PlaceItem()
    {
        if (Inventory.Instance == null) return;

        var items = Inventory.Instance.GetItems();
        int selected = Inventory.Instance.GetSelectedIndex();

        if (selected < 0 || selected >= items.Count) return;

        GameObject item = items[selected];
        if (item == null) return;

        string itemName = item.name.Replace("(Clone)", "");

        // Remove from inventory
        Inventory.Instance.RemoveItem(item);

        // Place item on table
        item.transform.SetParent(placementPoint);
        item.transform.localPosition = Vector3.zero;
        item.transform.localRotation = Quaternion.identity;

        // Enable renderers
        foreach (Renderer r in item.GetComponentsInChildren<Renderer>())
            r.enabled = true;

        // Disable physics
        Rigidbody rb = item.GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.isKinematic = true;
            rb.useGravity = false;
        }

        // Disable collider and pickup script
        foreach (Collider c in item.GetComponentsInChildren<Collider>())
            c.enabled = false;

        PickupItem pi = item.GetComponent<PickupItem>();
        if (pi != null) pi.enabled = false;

        placedItem = item;
        hasItem = true;
        showPrompt = false;

        // ── SAVE: Mark as placed in database ──
        if (SaveSystem.Instance != null)
        {
            SaveSystem.Instance.MarkRitualItemAsPlaced(
                itemName,
                item.transform.position,
                item.transform.rotation
            );
        }

        // Trigger objective
        if (objectiveTrigger != null)
            objectiveTrigger.TriggerObjective();

        // Trigger audio
        if (audioTrigger != null)
            audioTrigger.enabled = true;
    }

    public bool HasItem()
    {
        return hasItem;
    }

    public GameObject GetPlacedItem() => placedItem;

    void OnGUI()
    {
        if (!showPrompt || hasItem) return;

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
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(placementPoint.position, 0.1f);
    }
}