// PURPOSE: Tracks battery pickup, dropped, and used states and provides the battery recharge value.
using UnityEngine;

public class BatteryPickup : MonoBehaviour
{
    // Values used when this world battery is picked up and later used.
    [Header("Battery Settings")]
    public string itemName = "Battery";
    public float rechargeAmount = 50f;
    public float pickupRange = 2f;
    public KeyCode pickupKey = KeyCode.E;

    public bool wasDropped = false;
    public bool wasUsed = false;
    public bool isHeld = false;

    private bool isPickedUp = false;
    private bool showPrompt = false;
    private Camera playerCamera;

    void Start()
    {
        playerCamera = Camera.main;
    }

    void Update()
    {
        // Show the pickup prompt only when the player looks directly at this battery.
        if (isPickedUp) return;

        showPrompt = false;

        Ray ray = playerCamera.ScreenPointToRay(
            new Vector3(Screen.width / 2, Screen.height / 2));
        RaycastHit hit;

        if (Physics.Raycast(ray, out hit, pickupRange))
        {
            if (hit.collider.gameObject == gameObject ||
                hit.collider.transform.IsChildOf(transform))
            {
                showPrompt = true;

                if (Input.GetKeyDown(pickupKey))
                    TryPickUp();
            }
        }
    }

    void TryPickUp()
    {
        // Move this battery into Inventory if there is an empty slot.
        if (Inventory.Instance.IsFull())
        {
            return;
        }

        bool added = Inventory.Instance.AddItem(gameObject);
        if (added)
        {
            isPickedUp = true;
            showPrompt = false;
            isHeld = true;
            wasDropped = false;
            wasUsed = false;

            DrawerInteraction drawer = GetComponentInParent<DrawerInteraction>();
            if (drawer != null)
            {
                drawer.SetBusy(true);
            }

            foreach (Collider c in GetComponentsInChildren<Collider>())
                c.enabled = false;

            Rigidbody rb = GetComponent<Rigidbody>();
            if (rb != null) rb.isKinematic = true;
        }

        ItemSubtitleTrigger st = GetComponent<ItemSubtitleTrigger>();
        if (st != null) st.OnPickedUp();
    }

    public void ResetItem()
    {
        // Restore physics and visibility after the battery is dropped.
        isPickedUp = false;
        isHeld = false;

        foreach (Collider c in GetComponentsInChildren<Collider>())
            c.enabled = true;

        Rigidbody rb = GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.isKinematic = false;
            rb.useGravity = true;
        }
    }

    public void MarkAsDropped()
    {
        wasDropped = true;
        isHeld = false;
    }

    public void MarkAsUsed()
    {
        wasUsed = true;
        wasDropped = false;
        isHeld = false;
        isPickedUp = false;
    }

    void OnGUI()
    {
        if (!showPrompt || isPickedUp) return;

        string msg = Inventory.Instance.IsFull()
            ? "Bag is full!"
            : $"Press E to interact {itemName}";

        GUIStyle style = new GUIStyle();
        style.fontSize = 22;
        style.alignment = TextAnchor.MiddleCenter;
        style.normal.textColor = Color.white;

        GUIStyle shadow = new GUIStyle();
        shadow.fontSize = 22;
        shadow.alignment = TextAnchor.MiddleCenter;
        shadow.normal.textColor = Color.black;

        GUI.Label(new Rect(Screen.width / 2 - 199,
            Screen.height / 2 + 51, 400, 40), msg, shadow);
        GUI.Label(new Rect(Screen.width / 2 - 200,
            Screen.height / 2 + 50, 400, 40), msg, style);
    }
}
