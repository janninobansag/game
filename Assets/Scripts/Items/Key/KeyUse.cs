// PURPOSE: Updates the held state of a key item used by the inventory system.
using UnityEngine;

public class KeyUse : MonoBehaviour
{
    // Managed by Inventory when this key is selected or consumed.
    private bool isHeld = false;
    private bool isUsed = false;
    private Key keyData;
    private Camera playerCamera;

    void Start()
    {
        // Key stores the name of the door or vault this key can unlock.
        keyData = GetComponent<Key>();
        playerCamera = Camera.main;
    }

    void Update()
    {
        // A key only works while the player is holding it.
        if (!isHeld || isUsed) return;

        if (Input.GetKeyDown(KeyCode.E))
            TryUnlockDoor();
    }

    void TryUnlockDoor()
    {
        // Raycast from the screen centre so the player must look at the target.
        Ray ray = playerCamera.ScreenPointToRay(
            new Vector3(Screen.width / 2, Screen.height / 2));
        RaycastHit hit;

        if (Physics.Raycast(ray, out hit, 3f))
        {
            VaultDoorInteraction vault = hit.collider.GetComponentInParent<VaultDoorInteraction>();
            if (vault != null)
            {
                // Vaults use their own controller because they have extra steps after the key.
                if (!vault.MatchesKey(keyData.GetUnlocksTag()))
                {
                    ShowWrongKey();
                    return;
                }

                if (vault.TryUnlockWithKey())
                {
                    // A correct key is consumed after it unlocks the vault's first stage.
                    isUsed = true;
                    string keyName = gameObject.name.Replace("(Clone)", "");

                    if (SaveSystem.Instance != null)
                        SaveSystem.Instance.MarkKeyAsUsed(keyName);

                    if (keyData != null)
                        keyData.MarkAsUsed();

                    if (Inventory.Instance != null && Inventory.Instance.GetItems().Contains(gameObject))
                        Inventory.Instance.RemoveItem(gameObject);

                    Destroy(gameObject);
                }

                return;
            }

            DoorInteraction door = hit.collider.GetComponentInParent<DoorInteraction>();

            if (door != null && door.gameObject.name == keyData.GetUnlocksTag())
            {
                // Normal doors unlock and open immediately with their matching key.
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
        // Show a short warning when the player tries an incorrect key.
        showWrongKey = true;
        wrongKeyTimer = 2f;
    }

    public void SetHeld(bool held)
    {
        // Called by Inventory when this key becomes the active hand item.
        isHeld = held;

        if (!held)
        {
            showWrongKey = false;
            wrongKeyTimer = 0f;
        }
    }

    void OnGUI()
    {
        // Draw prompts only while this key is held.
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
            VaultDoorInteraction vault = hit.collider.GetComponentInParent<VaultDoorInteraction>();
            DoorInteraction door = hit.collider.GetComponentInParent<DoorInteraction>();

            if (vault == null && door != null && door.gameObject.name == keyData.GetUnlocksTag()
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
