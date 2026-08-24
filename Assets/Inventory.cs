using System.Collections.Generic;
using UnityEngine;

public class Inventory : MonoBehaviour
{
    public static Inventory Instance;

    [Header("Bag Settings")]
    public int maxCapacity = 3;

    [Header("Drop Item Light (Optional)")]
    public bool enableDropLight = true;
    public Color dropLightColor = new Color(1f, 0.7f, 0.3f);
    public float dropLightIntensity = 0.5f;
    public float dropLightRange = 2.5f;
    public float pulseSpeed = 1.2f;

    private List<GameObject> items = new List<GameObject>();
    private int selectedIndex = -1;

    void Awake()
    {
        if (Instance == null)
            Instance = this;
        else
            Destroy(gameObject);
    }

    void Update()
    {
        float scroll = Input.GetAxis("Mouse ScrollWheel");
        if (scroll > 0f) CycleItem(-1);
        else if (scroll < 0f) CycleItem(1);

        if (Input.GetKeyDown(KeyCode.G) && selectedIndex >= 0 && selectedIndex < items.Count)
            DropItem(selectedIndex);
    }

    void CycleItem(int direction)
    {
        int slotCount = Mathf.Max(1, maxCapacity);
        int newIndex = selectedIndex;
        if (newIndex < 0) newIndex = direction > 0 ? -1 : 0;
        newIndex = (newIndex + direction + slotCount) % slotCount;
        if (newIndex == selectedIndex) return;

        if (selectedIndex >= 0 && selectedIndex < items.Count)
            SetItemHeld(items[selectedIndex], false);

        selectedIndex = newIndex;
        if (selectedIndex < items.Count)
            SetItemHeld(items[selectedIndex], true);
    }

    public void SetItemHeld(GameObject item, bool held)
    {
        if (item == null) return;

        FlashlightPickup fp = item.GetComponent<FlashlightPickup>();
        if (fp != null) fp.SetHeld(held);

        BatteryUse bu = item.GetComponent<BatteryUse>();
        if (bu != null) bu.SetHeld(held);

        KeyUse ku = item.GetComponent<KeyUse>();
        if (ku != null) ku.SetHeld(held);

        CandleItem ci = item.GetComponent<CandleItem>();
        if (ci != null) ci.SetHeld(held);

        // ── NEW: Update BatteryPickup isHeld state ──
        BatteryPickup bp = item.GetComponent<BatteryPickup>();
        if (bp != null)
        {
            bp.isHeld = held;
            if (!held)
            {
                bp.wasDropped = true;
            }
        }

        if (held)
        {
            Camera cam = Camera.main;
            if (item.transform.parent != cam.transform)
            {
                item.transform.SetParent(cam.transform);

                PickupItem pi = item.GetComponent<PickupItem>();
                if (pi != null)
                {
                    item.transform.localPosition = pi.heldPositionOffset;
                    item.transform.localRotation = Quaternion.Euler(pi.heldRotation);
                }
                else
                {
                    item.transform.localPosition = new Vector3(0.3f, -0.2f, 0.5f);
                    item.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
                }
            }

            foreach (Renderer r in item.GetComponentsInChildren<Renderer>())
                r.enabled = true;
        }
        else
        {
            bool keepFlashlightLightWithPlayer = fp != null && fp.IsOn;
            if (!keepFlashlightLightWithPlayer)
                item.transform.SetParent(null);
            foreach (Renderer r in item.GetComponentsInChildren<Renderer>())
                r.enabled = false;
        }
    }

    public bool AddItem(GameObject item)
    {
        if (items.Count >= maxCapacity)
        {
            return false;
        }

        if (item == null)
        {
            return false;
        }

        string cleanName = item.name.Replace("(Clone)", "");
        item.name = cleanName;

        PickupItem pickup = item.GetComponent<PickupItem>();
        if (pickup != null)
        {
            pickup.itemName = cleanName;
        }

        Key key = item.GetComponent<Key>();
        if (key != null)
        {
            key.itemName = cleanName;
        }

        // ── NEW: Mark battery as held when added to inventory ──
        BatteryPickup bp = item.GetComponent<BatteryPickup>();
        if (bp != null)
        {
            bp.isHeld = true;
            bp.wasDropped = false;
            bp.wasUsed = false;
        }

        items.Add(item);

        RemoveDropLight(item);

        // Make each newly collected item the active item in the player's hand.
        if (selectedIndex >= 0 && selectedIndex < items.Count - 1)
            SetItemHeld(items[selectedIndex], false);

        selectedIndex = items.Count - 1;
        SetItemHeld(item, true);
        return true;
    }

    public void DropItem(int index)
    {
        if (index < 0 || index >= items.Count) return;

        GameObject item = items[index];
        if (item == null)
        {
            items.RemoveAt(index);
            selectedIndex = items.Count > 0 ? 0 : -1;
            if (selectedIndex >= 0)
                SetItemHeld(items[selectedIndex], true);
            return;
        }

        string cleanName = item.name.Replace("(Clone)", "");

        // ── Get battery value BEFORE destroying the item ──
        float batteryValue = -1f;
        FlashlightPickup fp = item.GetComponent<FlashlightPickup>();
        if (fp != null)
        {
            batteryValue = fp.GetBatteryPercent() * fp.batteryLife;
        }

        // ── Mark battery as dropped before removing ──
        BatteryPickup bp = item.GetComponent<BatteryPickup>();
        if (bp != null)
        {
            bp.isHeld = false;
            bp.wasDropped = true;
        }

        items.RemoveAt(index);

        if (PrefabManager.Instance != null)
        {
            Camera cam = Camera.main;
            Vector3 dropPos = cam.transform.position + cam.transform.forward * 0.8f;

            Vector3 playerFeet = new Vector3(dropPos.x, cam.transform.position.y - 1.5f, dropPos.z);
            RaycastHit groundHit;
            if (Physics.Raycast(playerFeet + Vector3.up * 0.5f, Vector3.down, out groundHit, 5f))
            {
                dropPos.y = groundHit.point.y + 0.15f;
            }
            else
            {
                dropPos.y = cam.transform.position.y - 1.2f;
            }

            GameObject droppedItem = PrefabManager.Instance.SpawnDroppedItem(cleanName, dropPos, Quaternion.identity, batteryValue);

            if (droppedItem != null)
            {
                Destroy(item);

                Key key = droppedItem.GetComponent<Key>();
                if (key != null)
                {
                    key.wasDropped = true;
                    key.isPickedUp = false;
                }

                FlashlightPickup fpNew = droppedItem.GetComponent<FlashlightPickup>();
                if (fpNew != null)
                {
                    fpNew.wasDropped = true;
                    fpNew.SetHeld(false);
                }

                BatteryPickup bpNew = droppedItem.GetComponent<BatteryPickup>();
                if (bpNew != null)
                {
                    bpNew.wasDropped = true;
                    bpNew.isHeld = false;
                }

                if (enableDropLight)
                {
                    AddDropLight(droppedItem);
                }
            }
            else
            {
                DropItemFallback(item, dropPos, batteryValue);
            }
        }
        else
        {
            Camera cam = Camera.main;
            Vector3 dropPos = cam.transform.position + cam.transform.forward * 0.8f;
            Vector3 playerFeet = new Vector3(dropPos.x, cam.transform.position.y - 1.5f, dropPos.z);
            RaycastHit groundHit;
            if (Physics.Raycast(playerFeet + Vector3.up * 0.5f, Vector3.down, out groundHit, 5f))
            {
                dropPos.y = groundHit.point.y + 0.15f;
            }
            else
            {
                dropPos.y = cam.transform.position.y - 1.2f;
            }
            DropItemFallback(item, dropPos, batteryValue);
        }

        selectedIndex = items.Count > 0 ? 0 : -1;

        if (selectedIndex >= 0)
            SetItemHeld(items[selectedIndex], true);
    }

    private void DropItemFallback(GameObject item, Vector3 dropPos, float batteryValue = -1f)
    {
        if (item == null) return;

        item.transform.SetParent(null);
        item.transform.position = dropPos;
        item.transform.rotation = Quaternion.identity;

        foreach (Renderer r in item.GetComponentsInChildren<Renderer>())
            r.enabled = true;

        foreach (Collider c in item.GetComponentsInChildren<Collider>())
            c.enabled = true;

        Rigidbody rb = item.GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.isKinematic = false;
            rb.useGravity = true;
            rb.velocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
        }

        if (enableDropLight)
        {
            AddDropLight(item);
        }

        FlashlightPickup fp = item.GetComponent<FlashlightPickup>();
        if (fp != null)
        {
            if (batteryValue >= 0f)
            {
                fp.SetBattery(batteryValue);
            }
            fp.wasDropped = true;
            fp.SetHeld(false);
        }

        PickupItem pi = item.GetComponent<PickupItem>();
        if (pi != null) pi.ResetItem();

        BatteryPickup bp = item.GetComponent<BatteryPickup>();
        if (bp != null)
        {
            bp.ResetItem();
            bp.wasDropped = true;
            bp.isHeld = false;
        }

        Key k = item.GetComponent<Key>();
        if (k != null) k.ResetItem();

        BatteryUse bu = item.GetComponent<BatteryUse>();
        if (bu != null) bu.SetHeld(false);

        KeyUse ku = item.GetComponent<KeyUse>();
        if (ku != null) ku.SetHeld(false);
    }

    public void CleanupNullItems()
    {
        items.RemoveAll(item => item == null);
        if (items.Count == 0)
            selectedIndex = -1;
    }

    private void AddDropLight(GameObject item)
    {
        RemoveDropLight(item);

        GameObject lightObj = new GameObject("DropLight");
        lightObj.transform.SetParent(item.transform);
        lightObj.transform.localPosition = new Vector3(0f, 0.2f, 0f);

        Light dropLight = lightObj.AddComponent<Light>();
        dropLight.type = LightType.Point;
        dropLight.color = dropLightColor;
        dropLight.intensity = dropLightIntensity;
        dropLight.range = 1.5f;
        dropLight.shadows = LightShadows.None;

        LightPulser pulser = lightObj.AddComponent<LightPulser>();
        pulser.minIntensity = 0f;
        pulser.maxIntensity = dropLightIntensity;
        pulser.pulseSpeed = pulseSpeed;
    }

    private void RemoveDropLight(GameObject item)
    {
        Transform lightObj = item.transform.Find("DropLight");
        if (lightObj != null)
        {
            Destroy(lightObj.gameObject);
        }
    }

    public void RemoveAndDestroy(GameObject item)
    {
        int index = items.IndexOf(item);
        if (index < 0) return;

        items.RemoveAt(index);

        if (item != null)
        {
            Destroy(item);
        }

        selectedIndex = items.Count > 0 ? 0 : -1;

        if (selectedIndex >= 0)
            SetItemHeld(items[selectedIndex], true);
    }

    public void RemoveItem(GameObject item)
    {
        if (item == null) return;
        items.Remove(item);
    }

    public int GetCount() => items.Count;
    public int GetMax() => maxCapacity;
    public bool IsFull() => items.Count >= maxCapacity;
    public List<GameObject> GetItems() => items;
    public int GetSelectedIndex() => selectedIndex;
}