using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Renders safe, visual-only copies of inventory prefabs to small UI textures.
/// </summary>
public sealed class InventoryItemPreview : MonoBehaviour
{
    private const int PreviewLayer = 31;
    private const int PreviewSize = 256;

    private sealed class PreviewSlot
    {
        public GameObject source;
        public GameObject model;
        public RenderTexture texture;
    }

    private static InventoryItemPreview instance;
    private readonly List<PreviewSlot> slots = new List<PreviewSlot>();
    private Camera previewCamera;
    private GameObject previewRoot;
    private Light previewLight;

    public static Texture GetPreview(GameObject source, int slotIndex)
    {
        if (!Application.isPlaying || source == null)
            return null;

        if (instance == null)
        {
            GameObject previewObject = new GameObject("Inventory Item Preview Camera");
            previewObject.hideFlags = HideFlags.HideAndDontSave;
            DontDestroyOnLoad(previewObject);
            instance = previewObject.AddComponent<InventoryItemPreview>();
            instance.Initialize();
        }

        return instance.Render(source, Mathf.Max(0, slotIndex));
    }

    private void Initialize()
    {
        previewRoot = new GameObject("Inventory Item Preview Models");
        previewRoot.hideFlags = HideFlags.HideAndDontSave;
        previewRoot.transform.position = new Vector3(0f, -10000f, 0f);
        DontDestroyOnLoad(previewRoot);

        GameObject cameraObject = new GameObject("Preview Camera");
        cameraObject.hideFlags = HideFlags.HideAndDontSave;
        cameraObject.transform.SetParent(transform, false);
        previewCamera = cameraObject.AddComponent<Camera>();
        previewCamera.enabled = false;
        previewCamera.clearFlags = CameraClearFlags.SolidColor;
        previewCamera.backgroundColor = Color.clear;
        previewCamera.orthographic = true;
        previewCamera.nearClipPlane = 0.01f;
        previewCamera.farClipPlane = 50f;
        previewCamera.cullingMask = 1 << PreviewLayer;
        previewCamera.allowHDR = false;
        previewCamera.allowMSAA = false;

        previewLight = cameraObject.AddComponent<Light>();
        previewLight.type = LightType.Point;
        previewLight.intensity = 1.5f;
        previewLight.range = 12f;
        previewLight.color = new Color(1f, 0.9f, 0.78f);
        previewLight.cullingMask = 1 << PreviewLayer;
    }

    private Texture Render(GameObject source, int slotIndex)
    {
        PreviewSlot slot = GetSlot(slotIndex);
        if (slot.source != source || slot.model == null)
        {
            DestroyModel(slot);
            slot.source = source;
            slot.model = CreateVisualModel(source, slotIndex);
        }

        if (slot.model == null)
            return null;

        Bounds bounds;
        if (!TryGetBounds(slot.model, out bounds))
            return null;

        Vector3 previewPosition = previewRoot.transform.position + new Vector3(slotIndex * 20f, 0f, 0f);
        float largestExtent = Mathf.Max(bounds.extents.x, bounds.extents.y, bounds.extents.z);
        previewCamera.orthographicSize = Mathf.Max(0.25f, largestExtent * 1.55f);
        previewCamera.transform.position = bounds.center + Vector3.back * (largestExtent * 4f + 2f);
        previewCamera.transform.LookAt(bounds.center);
        previewLight.transform.position = previewCamera.transform.position + new Vector3(-largestExtent, largestExtent, 1f);
        previewLight.transform.LookAt(bounds.center);

        previewCamera.targetTexture = slot.texture;
        previewCamera.Render();
        previewCamera.targetTexture = null;
        return slot.texture;
    }

    private PreviewSlot GetSlot(int slotIndex)
    {
        while (slots.Count <= slotIndex)
        {
            PreviewSlot slot = new PreviewSlot();
            slot.texture = new RenderTexture(PreviewSize, PreviewSize, 16, RenderTextureFormat.ARGB32)
            {
                name = $"Inventory Preview {slots.Count + 1}",
                hideFlags = HideFlags.HideAndDontSave
            };
            slot.texture.Create();
            slots.Add(slot);
        }

        return slots[slotIndex];
    }

    private GameObject CreateVisualModel(GameObject source, int slotIndex)
    {
        GameObject model = Instantiate(source);
        model.name = $"Preview - {source.name}";
        model.hideFlags = HideFlags.HideAndDontSave;
        model.transform.SetParent(previewRoot.transform, false);
        model.transform.localPosition = new Vector3(slotIndex * 20f, 0f, 0f);
        model.transform.localRotation = Quaternion.Euler(15f, -25f, 0f);

        SetLayerRecursively(model, PreviewLayer);

        foreach (MonoBehaviour behaviour in model.GetComponentsInChildren<MonoBehaviour>(true))
            behaviour.enabled = false;
        foreach (Collider collider in model.GetComponentsInChildren<Collider>(true))
            collider.enabled = false;
        foreach (Rigidbody body in model.GetComponentsInChildren<Rigidbody>(true))
            body.isKinematic = true;
        foreach (AudioSource audioSource in model.GetComponentsInChildren<AudioSource>(true))
            audioSource.enabled = false;
        foreach (Light light in model.GetComponentsInChildren<Light>(true))
            light.enabled = false;
        foreach (Camera cameraComponent in model.GetComponentsInChildren<Camera>(true))
            cameraComponent.enabled = false;
        foreach (Renderer renderer in model.GetComponentsInChildren<Renderer>(true))
            renderer.enabled = true;

        model.SetActive(true);
        return model;
    }

    private static bool TryGetBounds(GameObject model, out Bounds bounds)
    {
        Renderer[] renderers = model.GetComponentsInChildren<Renderer>(true);
        bool foundRenderer = false;
        bounds = new Bounds();

        foreach (Renderer renderer in renderers)
        {
            if (!renderer.enabled) continue;
            if (!foundRenderer)
            {
                bounds = renderer.bounds;
                foundRenderer = true;
            }
            else
            {
                bounds.Encapsulate(renderer.bounds);
            }
        }

        return foundRenderer;
    }

    private static void SetLayerRecursively(GameObject target, int layer)
    {
        target.layer = layer;
        foreach (Transform child in target.transform)
            SetLayerRecursively(child.gameObject, layer);
    }

    private static void DestroyModel(PreviewSlot slot)
    {
        if (slot.model != null)
            Destroy(slot.model);
        slot.model = null;
    }

    private void OnDestroy()
    {
        foreach (PreviewSlot slot in slots)
        {
            DestroyModel(slot);
            if (slot.texture != null)
                slot.texture.Release();
        }

        if (previewRoot != null)
            Destroy(previewRoot);

        if (instance == this)
            instance = null;
    }
}