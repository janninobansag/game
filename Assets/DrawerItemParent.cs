// PURPOSE: Stores dropped pickup items inside a drawer so they move with it.
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Collider))]
public class DrawerItemParent : MonoBehaviour
{
    [Header("Storage Settings")]
    [Tooltip("Tags accepted for items that do not use one of the pickup components.")]
    public string[] itemTags = { "Pickup", "Item" };

    [Header("Stored Item Placement")]
    [Tooltip("Removes pitch and roll so stored items lie flat in the drawer.")]
    public bool layItemsFlat = true;

    private Collider[] drawerColliders;
    private readonly List<StoredItemPose> storedItems = new List<StoredItemPose>();

    private sealed class StoredItemPose
    {
        public Transform transform;
        public Vector3 localPosition;
        public Quaternion localRotation;
    }

    private void Awake()
    {
        drawerColliders = GetComponents<Collider>();
    }

    private void OnTriggerEnter(Collider other)
    {
        StoreDroppedItem(other);
    }

    private void OnTriggerStay(Collider other)
    {
        // Covers an item that was already inside the trigger when it was dropped.
        StoreDroppedItem(other);
    }

    private void LateUpdate()
    {
        SyncStoredItems();
    }

    /// <summary>Stores every dropped item currently inside this drawer's collider.</summary>
    public void StoreItemsInside()
    {
        if (drawerColliders == null || drawerColliders.Length == 0)
            drawerColliders = GetComponents<Collider>();

        if (drawerColliders.Length == 0)
            return;

        Physics.SyncTransforms();

        bool foundStorageTrigger = false;
        foreach (Collider candidate in drawerColliders)
        {
            if (candidate == null || !candidate.enabled || !candidate.isTrigger)
                continue;

            foundStorageTrigger = true;
            StoreItemsInsideCollider(candidate);
        }

        // Older drawers may not have a dedicated trigger. Keep them working by
        // using their first enabled collider as a fallback storage volume.
        if (!foundStorageTrigger)
        {
            foreach (Collider candidate in drawerColliders)
            {
                if (candidate == null || !candidate.enabled)
                    continue;

                StoreItemsInsideCollider(candidate);
                break;
            }
        }
    }

    private void StoreItemsInsideCollider(Collider storageCollider)
    {
        Collider[] overlapping;

        if (storageCollider is BoxCollider box)
        {
            Vector3 halfExtents = Vector3.Scale(box.size * 0.5f, Abs(box.transform.lossyScale));
            overlapping = Physics.OverlapBox(
                box.transform.TransformPoint(box.center),
                halfExtents,
                box.transform.rotation,
                Physics.AllLayers,
                QueryTriggerInteraction.Collide);
        }
        else if (storageCollider is SphereCollider sphere)
        {
            Vector3 scale = Abs(sphere.transform.lossyScale);
            float radius = sphere.radius * Mathf.Max(scale.x, scale.y, scale.z);
            overlapping = Physics.OverlapSphere(
                sphere.transform.TransformPoint(sphere.center),
                radius,
                Physics.AllLayers,
                QueryTriggerInteraction.Collide);
        }
        else
        {
            Bounds bounds = storageCollider.bounds;
            overlapping = Physics.OverlapBox(
                bounds.center,
                bounds.extents,
                Quaternion.identity,
                Physics.AllLayers,
                QueryTriggerInteraction.Collide);
        }

        foreach (Collider overlappingCollider in overlapping)
        {
            if (overlappingCollider != storageCollider)
                StoreDroppedItem(overlappingCollider);
        }
    }

    private void StoreDroppedItem(Collider other)
    {
        if (other == null)
            return;

        Transform itemTransform = GetItemTransform(other);
        if (itemTransform == null || itemTransform == transform)
            return;

        PickupItem pickup = itemTransform.GetComponent<PickupItem>();
        if (pickup != null && pickup.isPickedUp)
            return;

        if (!IsItem(itemTransform.gameObject))
            return;

        // Keep the item's world position when it enters the drawer. An item that is
        // already a child must still have its physics stopped so it follows the drawer.
        if (!itemTransform.IsChildOf(transform))
            itemTransform.SetParent(transform, true);

        if (layItemsFlat)
        {
            float worldYaw = itemTransform.eulerAngles.y;
            itemTransform.rotation = Quaternion.Euler(0f, worldYaw, 0f);
        }

        RememberStoredPose(itemTransform);

        Rigidbody body = itemTransform.GetComponent<Rigidbody>();
        if (body != null)
        {
            body.velocity = Vector3.zero;
            body.angularVelocity = Vector3.zero;
            body.isKinematic = true;
            body.useGravity = false;
            body.interpolation = RigidbodyInterpolation.None;
            body.Sleep();
        }
    }

    private void RememberStoredPose(Transform itemTransform)
    {
        foreach (StoredItemPose storedItem in storedItems)
        {
            if (storedItem.transform == itemTransform)
                return;
        }

        storedItems.Add(new StoredItemPose
        {
            transform = itemTransform,
            localPosition = itemTransform.localPosition,
            localRotation = itemTransform.localRotation
        });
    }

    public void SyncStoredItems()
    {
        for (int i = storedItems.Count - 1; i >= 0; i--)
        {
            StoredItemPose storedItem = storedItems[i];
            if (storedItem.transform == null || !storedItem.transform.IsChildOf(transform))
            {
                storedItems.RemoveAt(i);
                continue;
            }

            storedItem.transform.localPosition = storedItem.localPosition;
            storedItem.transform.localRotation = storedItem.localRotation;

            Rigidbody body = storedItem.transform.GetComponent<Rigidbody>();
            if (body != null)
            {
                body.position = storedItem.transform.position;
                body.rotation = storedItem.transform.rotation;
            }
        }
    }

    private Transform GetItemTransform(Collider other)
    {
        PickupItem pickup = other.GetComponentInParent<PickupItem>();
        if (pickup != null)
            return pickup.transform;

        Key key = other.GetComponentInParent<Key>();
        if (key != null)
            return key.transform;

        BatteryPickup battery = other.GetComponentInParent<BatteryPickup>();
        if (battery != null)
            return battery.transform;

        return other.attachedRigidbody != null ? other.attachedRigidbody.transform : other.transform;
    }

    private bool IsItem(GameObject item)
    {
        if (item.GetComponent<PickupItem>() != null ||
            item.GetComponent<Key>() != null ||
            item.GetComponent<BatteryPickup>() != null)
            return true;

        foreach (string itemTag in itemTags)
        {
            if (!string.IsNullOrEmpty(itemTag) && item.CompareTag(itemTag))
                return true;
        }

        return false;
    }

    private static Vector3 Abs(Vector3 value)
    {
        return new Vector3(Mathf.Abs(value.x), Mathf.Abs(value.y), Mathf.Abs(value.z));
    }

    private void OnDrawGizmosSelected()
    {
        Collider colliderToDraw = null;
        Collider[] colliders = GetComponents<Collider>();
        foreach (Collider candidate in colliders)
        {
            if (candidate != null && candidate.isTrigger)
            {
                colliderToDraw = candidate;
                break;
            }
        }

        if (colliderToDraw == null && colliders.Length > 0)
            colliderToDraw = colliders[0];

        if (colliderToDraw == null)
            return;

        Gizmos.color = new Color(0f, 1f, 0f, 0.3f);
        if (colliderToDraw is BoxCollider box)
            Gizmos.DrawCube(transform.TransformPoint(box.center), Vector3.Scale(box.size, transform.lossyScale));
        else if (colliderToDraw is SphereCollider sphere)
            Gizmos.DrawSphere(transform.TransformPoint(sphere.center), sphere.radius * Mathf.Max(transform.lossyScale.x, transform.lossyScale.y, transform.lossyScale.z));
    }
}
