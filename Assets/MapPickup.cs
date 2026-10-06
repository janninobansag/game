// PURPOSE: Lets the player collect a physical Map and unlock a player-attached MapSystem.
using UnityEngine;

[RequireComponent(typeof(Collider))]
public class MapPickup : MonoBehaviour
{
    // World-object interaction settings for collecting the physical map.
    [Header("Pickup")]
    public string mapName = "Map";
    [Min(0.1f)] public float pickupRange = 2f;
    public KeyCode pickupKey = KeyCode.E;

    // The player-attached system that becomes available after the map is collected.
    [Header("Map System")]
    [Tooltip("Drag the MapSystem component from the Player here. Leave empty to find it automatically.")]
    public MapSystem mapSystem;

    private Camera playerCamera;
    private bool isCollected;
    private bool showPickupPrompt;

    private void Start()
    {
        // Cache the player camera used by the centre-screen pickup raycast.
        playerCamera = Camera.main;
        FindMapSystemIfNeeded();
    }

    private void Update()
    {
        if (isCollected) return;

        showPickupPrompt = false;
        if (playerCamera == null) playerCamera = Camera.main;
        if (playerCamera == null) return;

        Ray ray = playerCamera.ScreenPointToRay(new Vector3(Screen.width * 0.5f, Screen.height * 0.5f));
        if (!Physics.Raycast(ray, out RaycastHit hit, pickupRange, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore))
            return;
        if (hit.collider.transform != transform && !hit.collider.transform.IsChildOf(transform))
            return;

        showPickupPrompt = true;
        if (Input.GetKeyDown(pickupKey))
            CollectMap();
    }

    private void CollectMap()
    {
        FindMapSystemIfNeeded();
        if (mapSystem == null)
        {
            Debug.LogWarning("Map Pickup: Assign the MapSystem from the Player.", this);
            return;
        }

        isCollected = true;
        showPickupPrompt = false;
        mapSystem.UnlockMap();
        gameObject.SetActive(false);
    }

    private void FindMapSystemIfNeeded()
    {
        if (mapSystem == null)
            mapSystem = FindObjectOfType<MapSystem>();
    }

    private void OnGUI()
    {
        if (!showPickupPrompt || isCollected) return;

        GUIStyle shadow = new GUIStyle { fontSize = 22, alignment = TextAnchor.MiddleCenter };
        shadow.normal.textColor = Color.black;
        GUIStyle text = new GUIStyle(shadow);
        text.normal.textColor = Color.white;
        Rect rect = new Rect(Screen.width * 0.5f - 200f, Screen.height * 0.5f + 50f, 400f, 35f);
        string message = "Press " + pickupKey + " to pick up " + mapName;
        GUI.Label(new Rect(rect.x + 1f, rect.y + 1f, rect.width, rect.height), message, shadow);
        GUI.Label(rect, message, text);
    }
}
