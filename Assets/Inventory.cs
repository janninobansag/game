// PURPOSE: Stores carried items, enforces bag capacity, selects items, and handles item drops.
using System.Collections.Generic;
using UnityEngine;

public class Inventory : MonoBehaviour
{
    public static Inventory Instance;

    // Maximum number of items the player can carry at one time.
    [Header("Bag Settings")]
    public int maxCapacity = 3;

    // Position checks used to keep dropped items in front of the player and outside nearby walls.
    [Header("Drop Position")]
    [Tooltip("How far in front of the player an item is placed when dropped.")]
    [Min(0.25f)] public float dropDistance = 1.5f;
    [Tooltip("Height relative to the camera where a dropped item begins. A small negative value keeps it below the crosshair.")]
    public float dropStartHeightOffset = 0f;
    [Tooltip("Minimum space left between a dropped item and a wall in front of the player.")]
    [Min(0.05f)] public float dropWallClearance = 0.25f;

    // Physics force and spin applied to an item when the player presses G.
    [Header("Drop Throw")]
    [Tooltip("Forward speed applied when an item is dropped.")]
    [Min(0f)] public float dropThrowSpeed = 2.5f;
    [Tooltip("Small upward speed so the item feels tossed instead of placed.")]
    [Min(0f)] public float dropThrowUpwardSpeed = 1.5f;
    [Tooltip("Rotation speed applied while the dropped item is in the air.")]
    [Min(0f)] public float dropSpinSpeed = 3f;
    [Tooltip("How much a dropped item bounces when it hits a wall or floor.")]
    [Range(0f, 1f)] public float dropBounce = 0.25f;

    [Header("Drop Item Light (Optional)")]
    public bool enableDropLight = true;
    public Color dropLightColor = new Color(1f, 0.7f, 0.3f);
    public float dropLightIntensity = 0.5f;
    public float dropLightRange = 2.5f;
    [Tooltip("World-space height above a dropped item where its glow is placed.")]
    [Min(0f)] public float dropLightHeight = 0.2f;
    public float pulseSpeed = 1.2f;

    private List<GameObject> items = new List<GameObject>();
    private int selectedIndex = -1;
    private PhysicMaterial dropPhysicsMaterial;

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
        if (fp != null)
        {
            fp.SetHeld(held);
            Debug.Log($"[Inventory] Flashlight {(held ? "equipped" : "unequipped")}: {item.name}", item);
        }

        BatteryUse bu = item.GetComponent<BatteryUse>();
        if (bu != null) bu.SetHeld(held);

        KeyUse ku = item.GetComponent<KeyUse>();
        if (ku != null) ku.SetHeld(held);

        CandleItem ci = item.GetComponent<CandleItem>();
        if (ci != null) ci.SetHeld(held);
        PickupItem pickupItem = item.GetComponent<PickupItem>();
        if (pickupItem != null)
            pickupItem.isHeld = held;

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
            if (cam == null) return;

            // A held item follows the camera directly. It must not remain a
            // simulated Rigidbody after it was previously dropped.
            Vector3 worldScaleBeforePickup = item.transform.lossyScale;
            item.transform.SetParent(cam.transform, false);
            item.transform.localScale = GetLocalScaleForWorldScale(
                worldScaleBeforePickup,
                cam.transform.lossyScale);

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

            Rigidbody rb = item.GetComponent<Rigidbody>();
            if (rb != null)
            {
                rb.isKinematic = true;
                rb.useGravity = false;
                rb.interpolation = RigidbodyInterpolation.None;
                rb.velocity = Vector3.zero;
                rb.angularVelocity = Vector3.zero;
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

    private static Vector3 GetLocalScaleForWorldScale(Vector3 worldScale, Vector3 parentWorldScale)
    {
        return new Vector3(
            SafeScaleDivision(worldScale.x, parentWorldScale.x),
            SafeScaleDivision(worldScale.y, parentWorldScale.y),
            SafeScaleDivision(worldScale.z, parentWorldScale.z));
    }

    private static float SafeScaleDivision(float worldScale, float parentScale)
    {
        return Mathf.Abs(parentScale) > 0.0001f ? worldScale / parentScale : worldScale;
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
            pickup.isPickedUp = true;
            pickup.wasDropped = false;
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
            Vector3 dropPos = GetDropPosition(item, out bool wallIsTooClose);
            GameObject droppedItem = PrefabManager.Instance.HasExactPrefab(cleanName)
                ? PrefabManager.Instance.SpawnDroppedItem(cleanName, dropPos, Quaternion.identity, batteryValue)
                : null;

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
                PickupItem droppedPickup = droppedItem.GetComponent<PickupItem>();
                if (droppedPickup != null)
                {
                    droppedPickup.isPickedUp = false;
                    droppedPickup.isHeld = false;
                    droppedPickup.wasDropped = true;
                }


                BatteryPickup bpNew = droppedItem.GetComponent<BatteryPickup>();
                if (bpNew != null)
                {
                    bpNew.wasDropped = true;
                    bpNew.isHeld = false;
                }

                if (enableDropLight && ShouldUseDropLight(droppedItem))
                {
                    AddDropLight(droppedItem);
                }

                ApplyDropThrow(droppedItem, wallIsTooClose);
            }
            else
            {
                DropItemFallback(item, dropPos, batteryValue, wallIsTooClose);
            }
        }
        else
        {
            Vector3 dropPos = GetDropPosition(item, out bool wallIsTooClose);
            DropItemFallback(item, dropPos, batteryValue, wallIsTooClose);
        }

        selectedIndex = items.Count > 0 ? 0 : -1;

        if (selectedIndex >= 0)
            SetItemHeld(items[selectedIndex], true);
    }

    private Vector3 GetDropPosition(GameObject item, out bool wallIsTooClose)
    {
        wallIsTooClose = false;
        Camera cam = Camera.main;
        if (cam == null)
            return transform.position + transform.forward * dropDistance;

        Vector3 dropDirection = GetDropDirection();
        float distanceToDrop = dropDistance;
        float itemRadius = GetDropCollisionRadius(item);
        float castDistance = dropDistance + itemRadius + dropWallClearance;
        if (Physics.SphereCast(cam.transform.position, itemRadius, dropDirection, out RaycastHit obstacleHit, castDistance))
        {
            float safeDistance = obstacleHit.distance - itemRadius - dropWallClearance;
            // Do not apply a forward throw when any wall is inside the
            // requested drop distance. This keeps the item on this side even
            // if the wall has a thin collider.
            wallIsTooClose = safeDistance < dropDistance;

            // A wall is directly against the player. Put the item behind the
            // camera, on the player's side of the wall, and do not throw it
            // forward into the obstacle.
            distanceToDrop = safeDistance > 0f
                ? Mathf.Min(dropDistance, safeDistance)
                : -(itemRadius + dropWallClearance);
        }

        return cam.transform.position + dropDirection * distanceToDrop + Vector3.up * dropStartHeightOffset;
    }

    private float GetDropCollisionRadius(GameObject item)
    {
        float radius = dropWallClearance;
        if (item == null) return radius;

        foreach (Collider collider in item.GetComponentsInChildren<Collider>(true))
        {
            if (collider is BoxCollider box)
            {
                Vector3 scaledSize = Vector3.Scale(box.size, box.transform.lossyScale);
                radius = Mathf.Max(radius, Mathf.Max(scaledSize.x, scaledSize.z) * 0.5f);
            }
            else if (collider is SphereCollider sphere)
            {
                Vector3 scale = boxSafeAbs(collider.transform.lossyScale);
                radius = Mathf.Max(radius, sphere.radius * Mathf.Max(scale.x, scale.y, scale.z));
            }
            else
            {
                Bounds bounds = collider.bounds;
                radius = Mathf.Max(radius, Mathf.Max(bounds.extents.x, bounds.extents.z));
            }
        }

        return radius;
    }

    private static Vector3 boxSafeAbs(Vector3 value)
    {
        return new Vector3(Mathf.Abs(value.x), Mathf.Abs(value.y), Mathf.Abs(value.z));
    }

    private Vector3 GetDropDirection()
    {
        Camera cam = Camera.main;
        Vector3 forward = cam != null ? cam.transform.forward : transform.forward;
        Vector3 forwardOnGround = Vector3.ProjectOnPlane(forward, Vector3.up).normalized;
        return forwardOnGround.sqrMagnitude >= 0.001f ? forwardOnGround : transform.forward;
    }

    private void ApplyDropThrow(GameObject item, bool wallIsTooClose = false)
    {
        Rigidbody rb = item.GetComponent<Rigidbody>();
        if (rb == null) return;

        rb.isKinematic = false;
        rb.useGravity = true;
        rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
        rb.interpolation = RigidbodyInterpolation.Interpolate;
        rb.velocity = Vector3.zero;
        rb.angularVelocity = Vector3.zero;

        if (dropBounce > 0f)
        {
            if (dropPhysicsMaterial == null)
                dropPhysicsMaterial = new PhysicMaterial("Dropped Item Bounce");

            dropPhysicsMaterial.bounciness = dropBounce;
            dropPhysicsMaterial.bounceCombine = PhysicMaterialCombine.Maximum;
            dropPhysicsMaterial.dynamicFriction = 0.4f;
            dropPhysicsMaterial.staticFriction = 0.4f;

            foreach (Collider collider in item.GetComponentsInChildren<Collider>())
                collider.material = dropPhysicsMaterial;
        }

        float forwardSpeed = wallIsTooClose ? 0f : dropThrowSpeed;
        rb.AddForce(GetDropDirection() * forwardSpeed + Vector3.up * dropThrowUpwardSpeed, ForceMode.VelocityChange);
        if (dropSpinSpeed > 0f)
            rb.AddTorque(Random.insideUnitSphere * dropSpinSpeed, ForceMode.VelocityChange);

    }

    private void DropItemFallback(GameObject item, Vector3 dropPos, float batteryValue = -1f, bool wallIsTooClose = false)
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

        if (enableDropLight && ShouldUseDropLight(item))
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
        if (pi != null)
        {
            pi.ResetItem();
            pi.isHeld = false;
            pi.wasDropped = true;
        }

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

        ApplyDropThrow(item, wallIsTooClose);
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

        // The follower keeps the light near the item even when the item has an unusual scale.
        DropItemLightFollower follower = lightObj.AddComponent<DropItemLightFollower>();
        follower.targetItem = item.transform;
        follower.worldOffset = Vector3.up * dropLightHeight;

        Light dropLight = lightObj.AddComponent<Light>();
        dropLight.type = LightType.Point;
        dropLight.color = dropLightColor;
        dropLight.intensity = dropLightIntensity;
        dropLight.range = dropLightRange;
        dropLight.shadows = LightShadows.None;

        LightPulser pulser = lightObj.AddComponent<LightPulser>();
        pulser.minIntensity = 0f;
        pulser.maxIntensity = dropLightIntensity;
        pulser.pulseSpeed = pulseSpeed;
    }

    /// <summary>
    /// Recreates the runtime drop light after SaveSystem restores a dropped item.
    /// </summary>
    public void RestoreDropLight(GameObject item)
    {
        if (enableDropLight && ShouldUseDropLight(item))
        {
            AddDropLight(item);
        }
    }

    private static bool ShouldUseDropLight(GameObject item)
    {
        if (item == null) return false;

        PickupItem pickup = item.GetComponent<PickupItem>();
        string itemName = pickup != null && !string.IsNullOrEmpty(pickup.itemName)
            ? pickup.itemName
            : item.name;

        return !string.Equals(itemName.Replace("(Clone)", "").Trim(), "Gas", System.StringComparison.OrdinalIgnoreCase);
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

    public void RemoveWithoutDestroy(GameObject item)
    {
        int index = items.IndexOf(item);
        if (index < 0) return;
        items.RemoveAt(index);
        selectedIndex = items.Count > 0 ? Mathf.Min(index, items.Count - 1) : -1;
        if (selectedIndex >= 0) SetItemHeld(items[selectedIndex], true);
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

    /// <summary>Selects one restored inventory item and applies its held state.</summary>
    public void SelectItem(int index)
    {
        if (index < 0 || index >= items.Count || index == selectedIndex)
            return;

        if (selectedIndex >= 0 && selectedIndex < items.Count)
        {
            SetItemHeld(items[selectedIndex], false);
        }

        selectedIndex = index;
        SetItemHeld(items[selectedIndex], true);
    }
}
