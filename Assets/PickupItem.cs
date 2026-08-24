using UnityEngine;

public class PickupItem : MonoBehaviour
{
    [Header("Item Settings")]
    public string itemName = "Item";
    public float pickupRange = 2f;
    public KeyCode pickupKey = KeyCode.E;
    public Vector3 heldPositionOffset = new Vector3(0.3f, -0.2f, 0.5f);
    public Vector3 heldRotation = new Vector3(90f, 0f, 0f);

    public bool isPickedUp = false;
    private bool showPrompt = false;
    private Camera playerCamera;

    void Start()
    {
        playerCamera = Camera.main;
    }

    void Update()
    {
        if (isPickedUp) return;

        showPrompt = false;

        Ray ray = playerCamera.ScreenPointToRay(new Vector3(Screen.width / 2, Screen.height / 2));
        RaycastHit hit;

        if (Physics.Raycast(ray, out hit, pickupRange))
        {
            if (hit.collider.gameObject == gameObject || hit.collider.transform.IsChildOf(transform))
            {
                showPrompt = true;

                if (Input.GetKeyDown(pickupKey))
                    TryPickUp();
            }
        }
    }

    void TryPickUp()
    {
        if (Inventory.Instance.IsFull())
        {
            return;
        }

        bool added = Inventory.Instance.AddItem(gameObject);
        if (added)
        {
            RemoveGlowLight();

            isPickedUp = true;
            showPrompt = false;

            FlashlightPickup flashlight = GetComponent<FlashlightPickup>();
            if (flashlight != null)
            {
                flashlight.ResetDroppedState();
            }

            DrawerInteraction drawer = GetComponentInParent<DrawerInteraction>();
            if (drawer != null)
            {
                drawer.SetBusy(true);
            }

            foreach (Collider c in GetComponentsInChildren<Collider>())
                c.enabled = false;

            Rigidbody rb = GetComponent<Rigidbody>();
            if (rb != null) rb.isKinematic = true;

            foreach (Renderer r in GetComponentsInChildren<Renderer>())
                r.enabled = false;

            ItemSubtitleTrigger st = GetComponent<ItemSubtitleTrigger>();
            if (st != null) st.OnPickedUp();
        }
    }

    public void RemoveGlowLight()
    {
        Transform glowLight = transform.Find("ItemGlowLight");
        if (glowLight != null)
        {
            Destroy(glowLight.gameObject);
        }
    }

    public void ResetItem()
    {
        isPickedUp = false;
        
        RemoveGlowLight();
        
        foreach (Collider c in GetComponentsInChildren<Collider>())
        {
            c.enabled = true;
            c.isTrigger = false;
        }
        
        foreach (Renderer r in GetComponentsInChildren<Renderer>())
            r.enabled = true;
        
        Rigidbody rb = GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.isKinematic = false;
            rb.useGravity = true;
            rb.velocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
        }
        
        FlashlightPickup flashlight = GetComponent<FlashlightPickup>();
        if (flashlight != null)
        {
            flashlight.ResetDroppedState();
        }
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

        GUI.Label(new Rect(Screen.width / 2 - 199, Screen.height / 2 + 51, 400, 40), msg, shadow);
        GUI.Label(new Rect(Screen.width / 2 - 200, Screen.height / 2 + 50, 400, 40), msg, style);
    }
}