using UnityEngine;

public class DrawerItemParent : MonoBehaviour
{
    [Header("Settings")]
    public string[] itemTags = { "Pickup", "Item" };
    public float checkInterval = 0.1f;

    private Collider drawerCollider;
    private Transform drawerTransform;

    void Start()
    {
        drawerCollider = GetComponent<Collider>();
        drawerTransform = transform;
        
        if (drawerCollider == null)
        {
        }
    }

    void OnTriggerEnter(Collider other)
    {
        // Check if the object is a pickup item
        if (IsItem(other.gameObject))
        {
            // Don't parent if it's already a child of this drawer
            if (other.transform.parent == drawerTransform)
                return;

            // Don't parent if it's already picked up (in inventory)
            PickupItem pickup = other.GetComponent<PickupItem>();
            if (pickup != null && pickup.isPickedUp)
                return;

            // Parent the item to the drawer
            other.transform.SetParent(drawerTransform);
        }
    }

    void OnTriggerStay(Collider other)
    {
        // If item is inside the drawer but not a child, parent it
        if (IsItem(other.gameObject))
        {
            if (other.transform.parent != drawerTransform)
            {
                PickupItem pickup = other.GetComponent<PickupItem>();
                if (pickup != null && pickup.isPickedUp)
                    return;

                other.transform.SetParent(drawerTransform);
            }
        }
    }

    bool IsItem(GameObject obj)
    {
        foreach (string tag in itemTags)
        {
            if (obj.CompareTag(tag))
                return true;
        }
        return false;
    }

    // ── Optional: Visualize the trigger area ──
    void OnDrawGizmosSelected()
    {
        if (drawerCollider == null) return;

        Gizmos.color = new Color(0f, 1f, 0f, 0.3f);
        
        if (drawerCollider is BoxCollider box)
        {
            Gizmos.DrawCube(transform.position + box.center, box.size);
        }
        else if (drawerCollider is SphereCollider sphere)
        {
            Gizmos.DrawSphere(transform.position + sphere.center, sphere.radius);
        }
    }
}