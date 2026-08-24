using UnityEngine;

public class BatteryUse : MonoBehaviour
{
    [Header("Battery Settings")]
    public float rechargeAmount = 50f;

    private bool isHeld = false;
    private bool isUsed = false;

    void Update()
    {
        if (!isHeld || isUsed) return;

        if (Input.GetMouseButtonDown(0))
            UseBattery();
    }

    void UseBattery()
    {
        FlashlightPickup flashlight = null;

        foreach (GameObject item in Inventory.Instance.GetItems())
        {
            flashlight = item.GetComponent<FlashlightPickup>();
            if (flashlight != null) break;
        }

        if (flashlight == null)
        {
            return;
        }

        if (flashlight.IsFullBattery())
        {
            return;
        }

        flashlight.Recharge(rechargeAmount);
        isUsed = true;

        // ── Mark battery as used BEFORE destroying ──
        BatteryPickup batteryPickup = GetComponent<BatteryPickup>();
        if (batteryPickup != null)
        {
            batteryPickup.MarkAsUsed();

            // ── Save to database immediately ──
            if (SaveSystem.Instance != null)
            {
                SaveSystem.Instance.MarkBatteryAsUsed(gameObject.name);
            }
        }

        // ── Remove from inventory and destroy ──
        if (Inventory.Instance != null)
        {
            if (Inventory.Instance.GetItems().Contains(gameObject))
            {
                Inventory.Instance.RemoveItem(gameObject);
            }
        }

        Destroy(gameObject);
    }

    public void SetHeld(bool held)
    {
        isHeld = held;
    }

    void OnGUI()
    {
        if (!isHeld || isUsed) return;

        GUIStyle style = new GUIStyle();
        style.fontSize = 18;
        style.alignment = TextAnchor.MiddleCenter;
        style.normal.textColor = Color.yellow;

        GUIStyle shadow = new GUIStyle();
        shadow.fontSize = 18;
        shadow.alignment = TextAnchor.MiddleCenter;
        shadow.normal.textColor = Color.black;

        string msg = "Left Click to use Battery";
        GUI.Label(new Rect(Screen.width / 2 - 199, Screen.height / 2 + 81, 400, 30), msg, shadow);
        GUI.Label(new Rect(Screen.width / 2 - 200, Screen.height / 2 + 80, 400, 30), msg, style);
    }
}