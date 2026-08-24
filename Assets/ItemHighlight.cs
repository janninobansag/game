using UnityEngine;

public class ItemHighlight : MonoBehaviour
{
    [Header("Highlight Settings")]
    public Color highlightColor = new Color(1f, 0.8f, 0.2f, 1f);
    public float highlightRange = 3f;
    public float pulseSpeed = 2f;
    public float pulseMinIntensity = 0.3f;
    public float pulseMaxIntensity = 1.2f;
    public float outlineWidth = 1.03f;

    private Camera playerCamera;
    private Renderer[] renderers;
    private Material[] originalMaterials;
    private Material[] highlightMaterials;
    private bool isHighlighted = false;
    private float pulseTimer = 0f;

    void Start()
    {
        playerCamera = Camera.main;
        renderers = GetComponentsInChildren<Renderer>();

        // Store original materials
        originalMaterials = new Material[renderers.Length];
        highlightMaterials = new Material[renderers.Length];

        for (int i = 0; i < renderers.Length; i++)
        {
            originalMaterials[i] = renderers[i].material;

            // Create highlight material based on original
            highlightMaterials[i] = new Material(renderers[i].material);
            highlightMaterials[i].EnableKeyword("_EMISSION");
            highlightMaterials[i].SetColor("_EmissionColor",
                highlightColor * pulseMinIntensity);
        }
    }

    void Update()
    {
        // Check if picked up — disable highlight
        PickupItem pi = GetComponent<PickupItem>();
        BatteryPickup bp = GetComponent<BatteryPickup>();
        Key k = GetComponent<Key>();

        bool pickedUp = (pi != null && pi.isPickedUp) ||
                        (bp != null && !bp.enabled) ||
                        (k != null && !k.enabled);

        if (pickedUp)
        {
            SetHighlight(false);
            return;
        }

        // Raycast check
        bool shouldHighlight = false;
        Ray ray = playerCamera.ScreenPointToRay(
            new Vector3(Screen.width / 2, Screen.height / 2));
        RaycastHit hit;

        if (Physics.Raycast(ray, out hit, highlightRange))
        {
            if (hit.collider.gameObject == gameObject ||
                hit.collider.transform.IsChildOf(transform))
            {
                shouldHighlight = true;
            }
        }

        SetHighlight(shouldHighlight);

        // Pulse emission when highlighted
        if (isHighlighted)
        {
            pulseTimer += Time.deltaTime * pulseSpeed;
            float pulse = Mathf.Lerp(pulseMinIntensity, pulseMaxIntensity,
                (Mathf.Sin(pulseTimer) + 1f) / 2f);

            for (int i = 0; i < renderers.Length; i++)
            {
                if (renderers[i] != null)
                {
                    highlightMaterials[i].SetColor("_EmissionColor",
                        highlightColor * pulse);
                }
            }
        }
    }

    void SetHighlight(bool highlight)
    {
        if (isHighlighted == highlight) return;
        isHighlighted = highlight;

        for (int i = 0; i < renderers.Length; i++)
        {
            if (renderers[i] == null) continue;

            if (highlight)
                renderers[i].material = highlightMaterials[i];
            else
                renderers[i].material = originalMaterials[i];
        }
    }

   void OnDestroy()
{
    if (highlightMaterials == null) return;

    for (int i = 0; i < highlightMaterials.Length; i++)
    {
        if (highlightMaterials[i] != null)
            Destroy(highlightMaterials[i]);
    }
}
}