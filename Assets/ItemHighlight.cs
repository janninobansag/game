// PURPOSE: Highlights the item under the player's crosshair with a configurable pulse.
using System.Collections.Generic;
using UnityEngine;

public class ItemHighlight : MonoBehaviour
{
    // Appearance and scan distance of the crosshair item highlight.
    [Header("Highlight Settings")]
    public Color highlightColor = new Color(1f, 0.8f, 0.2f, 1f);
    public float highlightRange = 3f;
    public float pulseSpeed = 2f;
    public float pulseMinIntensity = 0.3f;
    public float pulseMaxIntensity = 1.2f;
    public float outlineWidth = 1.03f;

    // Limit crosshair scans so every highlight component does not raycast every frame.
    private const float ScanInterval = 0.05f;
    private static readonly int EmissionColorId = Shader.PropertyToID("_EmissionColor");
    private static readonly List<ItemHighlight> activeItems = new List<ItemHighlight>();
    private static ItemHighlight scanner;
    private static ItemHighlight currentTarget;
    private static float nextScanTime;

    private Camera playerCamera;
    private PickupItem pickupItem;
    private BatteryPickup batteryPickup;
    private Key keyItem;
    private Renderer[] renderers;
    private Material[][] originalMaterials;
    private Material[][] highlightMaterials;
    private bool isHighlighted;
    private float pulseTimer;

    private void Awake()
    {
        playerCamera = Camera.main;
        pickupItem = GetComponent<PickupItem>();
        batteryPickup = GetComponent<BatteryPickup>();
        keyItem = GetComponent<Key>();
        renderers = GetComponentsInChildren<Renderer>();

        originalMaterials = new Material[renderers.Length][];
        highlightMaterials = new Material[renderers.Length][];
        for (int i = 0; i < renderers.Length; i++)
            originalMaterials[i] = renderers[i].sharedMaterials;
    }

    private void OnEnable()
    {
        activeItems.Add(this);
        if (scanner == null || !scanner.isActiveAndEnabled)
            scanner = this;
    }

    private void OnDisable()
    {
        activeItems.Remove(this);

        if (currentTarget == this)
        {
            SetHighlight(false);
            currentTarget = null;
        }

        if (scanner == this)
        {
            scanner = null;
            for (int i = 0; i < activeItems.Count; i++)
            {
                if (activeItems[i] != null && activeItems[i].isActiveAndEnabled)
                {
                    scanner = activeItems[i];
                    break;
                }
            }
        }
    }

    private void Update()
    {
        if (scanner != this)
            return;

        if (Time.unscaledTime >= nextScanTime)
        {
            nextScanTime = Time.unscaledTime + ScanInterval;
            ScanForTarget();
        }

        if (currentTarget != null && currentTarget.isHighlighted)
            currentTarget.UpdatePulse(Time.deltaTime);
    }

    private void ScanForTarget()
    {
        float maxRange = 0f;
        for (int i = 0; i < activeItems.Count; i++)
        {
            ItemHighlight item = activeItems[i];
            if (item != null && item.isActiveAndEnabled)
                maxRange = Mathf.Max(maxRange, item.highlightRange);
        }

        if (maxRange <= 0f)
        {
            SetCurrentTarget(null);
            return;
        }

        if (playerCamera == null)
            playerCamera = Camera.main;
        if (playerCamera == null)
        {
            SetCurrentTarget(null);
            return;
        }

        Ray ray = playerCamera.ScreenPointToRay(
            new Vector3(Screen.width * 0.5f, Screen.height * 0.5f));

        ItemHighlight target = null;
        if (Physics.Raycast(ray, out RaycastHit hit, maxRange))
        {
            ItemHighlight candidate = hit.collider.GetComponentInParent<ItemHighlight>();
            if (candidate != null && candidate.CanBeHighlighted() &&
                hit.distance <= candidate.highlightRange)
            {
                target = candidate;
            }
        }

        SetCurrentTarget(target);
    }

    private bool CanBeHighlighted()
    {
        return isActiveAndEnabled &&
               !(pickupItem != null && pickupItem.isPickedUp) &&
               !(batteryPickup != null && !batteryPickup.enabled) &&
               !(keyItem != null && !keyItem.enabled);
    }

    private static void SetCurrentTarget(ItemHighlight target)
    {
        if (currentTarget == target)
            return;

        if (currentTarget != null)
            currentTarget.SetHighlight(false);

        currentTarget = target;
        if (currentTarget != null)
            currentTarget.SetHighlight(true);
    }

    private void SetHighlight(bool highlight)
    {
        if (isHighlighted == highlight)
            return;

        isHighlighted = highlight;
        if (highlight)
        {
            pulseTimer = 0f;
            CreateHighlightMaterialsIfNeeded();
        }

        for (int i = 0; i < renderers.Length; i++)
        {
            if (renderers[i] == null)
                continue;

            Material[] materials = highlight ? highlightMaterials[i] : originalMaterials[i];
            if (materials != null)
                renderers[i].sharedMaterials = materials;
        }
    }

    private void CreateHighlightMaterialsIfNeeded()
    {
        for (int i = 0; i < renderers.Length; i++)
        {
            if (highlightMaterials[i] != null)
                continue;

            Material[] sourceMaterials = originalMaterials[i];
            Material[] copies = new Material[sourceMaterials.Length];
            for (int j = 0; j < sourceMaterials.Length; j++)
            {
                Material source = sourceMaterials[j];
                if (source == null)
                    continue;

                Material copy = new Material(source);
                copy.EnableKeyword("_EMISSION");
                if (copy.HasProperty(EmissionColorId))
                    copy.SetColor(EmissionColorId, highlightColor * pulseMinIntensity);
                copies[j] = copy;
            }

            highlightMaterials[i] = copies;
        }
    }

    private void UpdatePulse(float deltaTime)
    {
        pulseTimer += deltaTime * pulseSpeed;
        float pulse = Mathf.Lerp(pulseMinIntensity, pulseMaxIntensity,
            (Mathf.Sin(pulseTimer) + 1f) * 0.5f);

        for (int i = 0; i < highlightMaterials.Length; i++)
        {
            Material[] materials = highlightMaterials[i];
            if (materials == null)
                continue;

            for (int j = 0; j < materials.Length; j++)
            {
                if (materials[j] != null && materials[j].HasProperty(EmissionColorId))
                    materials[j].SetColor(EmissionColorId, highlightColor * pulse);
            }
        }
    }

    private void OnDestroy()
    {
        if (highlightMaterials == null)
            return;

        for (int i = 0; i < highlightMaterials.Length; i++)
        {
            Material[] materials = highlightMaterials[i];
            if (materials == null)
                continue;

            for (int j = 0; j < materials.Length; j++)
            {
                if (materials[j] != null)
                    Destroy(materials[j]);
            }
        }
    }
}
