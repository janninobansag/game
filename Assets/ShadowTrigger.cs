using UnityEngine;

public class ShadowTrigger : MonoBehaviour
{
    [Header("Shadow Trigger")]
    public GameObject shadowPrefab;
    public Transform spawnPoint;
    public bool triggerOnce = true;
    public string playerTag = "Player";

    [Header("Layer Settings")]
    public LayerMask ignoreLayers = 0; // Layers to ignore (like Items)

    private bool hasTriggered = false;
    private GameObject spawnedShadow;

    void OnTriggerEnter(Collider other)
    {
        // ── IGNORE ITEMS ──
        if (ignoreLayers != 0 && (ignoreLayers & (1 << other.gameObject.layer)) != 0)
        {
            return; // Skip items
        }

        if (!other.CompareTag(playerTag)) return;
        if (triggerOnce && hasTriggered) return;

        hasTriggered = true;
        SpawnShadow();
    }

    void SpawnShadow()
    {
        if (shadowPrefab == null || spawnPoint == null) return;

        if (spawnedShadow != null)
            Destroy(spawnedShadow);

        spawnedShadow = Instantiate(shadowPrefab,
            spawnPoint.position, spawnPoint.rotation);
    }

    public void ResetTrigger()
    {
        hasTriggered = false;
        if (spawnedShadow != null)
            Destroy(spawnedShadow);
    }

    void OnDrawGizmos()
    {
        Gizmos.color = new Color(1f, 0f, 0f, 0.2f);
        BoxCollider box = GetComponent<BoxCollider>();
        if (box != null)
            Gizmos.DrawCube(transform.position + box.center, box.size);
    }
}