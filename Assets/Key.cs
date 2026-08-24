using UnityEngine;

public class Key : MonoBehaviour
{
    [Header("Key Settings")]
    public string itemName = "Key";
    public string unlocksTag = "DoorHinge4";
    public float pickupRange = 2f;
    public KeyCode pickupKey = KeyCode.E;

    public bool isPickedUp = false;
    public bool wasDropped = false;
    public bool wasUsed = false;
    private bool showPrompt = false;
    private Camera playerCamera;

    public bool IsPickedUp => isPickedUp;

    void Start()
    {
        playerCamera = Camera.main;
        itemName = gameObject.name.Replace("(Clone)", "");
    }

    void Update()
    {
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
        if (Inventory.Instance.IsFull())
        {
            return;
        }

        DrawerInteraction drawer = GetComponentInParent<DrawerInteraction>();
        if (drawer != null)
        {
            drawer.SetBusy(true);
        }

        string cleanName = gameObject.name.Replace("(Clone)", "");
        gameObject.name = cleanName;
        itemName = cleanName;

        bool added = Inventory.Instance.AddItem(gameObject);

        if (added)
        {
            isPickedUp = true;
            wasDropped = false;
            showPrompt = false;

            foreach (Collider c in GetComponentsInChildren<Collider>())
                c.enabled = false;

            Rigidbody rb = GetComponent<Rigidbody>();
            if (rb != null)
            {
                rb.isKinematic = true;
                rb.useGravity = false;
            }

            foreach (Renderer r in GetComponentsInChildren<Renderer>())
                r.enabled = false;
        }

        if (drawer != null)
        {
            drawer.SetBusy(false);
        }

        ItemSubtitleTrigger st = GetComponent<ItemSubtitleTrigger>();
        if (st != null) st.OnPickedUp();
    }

    public void ResetItem()
    {
        isPickedUp = false;
        wasDropped = true;

        gameObject.SetActive(true);

        foreach (Collider c in GetComponentsInChildren<Collider>())
            c.enabled = true;

        foreach (Renderer r in GetComponentsInChildren<Renderer>())
            r.enabled = true;

        Rigidbody rb = GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.isKinematic = false;
            rb.useGravity = true;
        }
    }

    public string GetUnlocksTag() => unlocksTag;

    public void MarkAsUsed()
    {
        wasUsed = true;
        isPickedUp = false;
        wasDropped = false;
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