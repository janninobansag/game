using UnityEngine;

public class KeyUse : MonoBehaviour
{
    private bool isHeld = false;
    private bool isUsed = false;
    private Key keyData;
    private Camera playerCamera;

    void Start()
    {
        keyData = GetComponent<Key>();
        playerCamera = Camera.main;
    }

    void Update()
    {
        if (!isHeld || isUsed) return;

        if (Input.GetKeyDown(KeyCode.E))
            TryUnlockDoor();
    }

    void TryUnlockDoor()
    {
        Ray ray = playerCamera.ScreenPointToRay(
            new Vector3(Screen.width / 2, Screen.height / 2));
        RaycastHit hit;

        if (Physics.Raycast(ray, out hit, 3f))
        {
            DoorInteraction door = hit.collider.GetComponentInParent<DoorInteraction>();

            if (door != null && door.gameObject.name == keyData.GetUnlocksTag())
            {
                if (!door.IsLocked())
                {
                    return;
                }

                door.Unlock();
                isUsed = true;

                string keyName = gameObject.name.Replace("(Clone)", "");

                // ── Mark key as used in database IMMEDIATELY ──
                if (SaveSystem.Instance != null)
                {
                    SaveSystem.Instance.MarkKeyAsUsed(keyName);
                }

                if (keyData != null)
                {
                    keyData.MarkAsUsed();
                }

                if (Inventory.Instance != null)
                {
                    if (Inventory.Instance.GetItems().Contains(gameObject))
                    {
                        Inventory.Instance.RemoveItem(gameObject);
                    }
                }

                Destroy(gameObject);
            }
            else if (door != null && door.IsLocked())
            {
                ShowWrongKey();
            }
        }
    }

    private float wrongKeyTimer = 0f;
    private bool showWrongKey = false;

    void ShowWrongKey()
    {
        showWrongKey = true;
        wrongKeyTimer = 2f;
    }

    public void SetHeld(bool held)
    {
        isHeld = held;

        if (!held)
        {
            showWrongKey = false;
            wrongKeyTimer = 0f;
        }
    }

    void OnGUI()
    {
        if (!isHeld || isUsed) return;

        GUIStyle style = new GUIStyle();
        style.fontSize = 18;
        style.alignment = TextAnchor.MiddleCenter;
        style.normal.textColor = new Color(1f, 0.9f, 0.3f);

        GUIStyle shadow = new GUIStyle();
        shadow.fontSize = 18;
        shadow.alignment = TextAnchor.MiddleCenter;
        shadow.normal.textColor = Color.black;

        Ray ray = Camera.main.ScreenPointToRay(
            new Vector3(Screen.width / 2, Screen.height / 2));
        RaycastHit hit;

        if (Physics.Raycast(ray, out hit, 3f))
        {
            DoorInteraction door = hit.collider.GetComponentInParent<DoorInteraction>();

            if (door != null && door.gameObject.name == keyData.GetUnlocksTag()
                && door.IsLocked())
            {
                string msg = "Press E to unlock door";
                GUI.Label(new Rect(Screen.width / 2 - 199,
                    Screen.height / 2 + 81, 400, 30), msg, shadow);
                GUI.Label(new Rect(Screen.width / 2 - 200,
                    Screen.height / 2 + 80, 400, 30), msg, style);
            }
        }

        if (showWrongKey)
        {
            wrongKeyTimer -= Time.deltaTime;
            if (wrongKeyTimer <= 0f) showWrongKey = false;

            GUIStyle wrongStyle = new GUIStyle();
            wrongStyle.fontSize = 18;
            wrongStyle.alignment = TextAnchor.MiddleCenter;
            wrongStyle.normal.textColor = Color.red;

            GUI.Label(new Rect(Screen.width / 2 - 200,
                Screen.height / 2 + 110, 400, 30),
                "Wrong key!", wrongStyle);
        }
    }
}