// PURPOSE: Shows and hides the map after it has been unlocked by a MapPickup.
using UnityEngine;

public partial class MapSystem : MonoBehaviour
{
    // Key and map texture used by the player-owned map display.
    [Header("Map Settings")]
    public KeyCode mapKey = KeyCode.M;
    public Texture2D mapTexture; // drag your map image here

    // Controls whether M works immediately or only after MapPickup unlocks it.
    [Header("Map Unlock")]
    [Tooltip("When disabled, a MapPickup must unlock this Map System before the M key can open it.")]
    public bool mapStartsUnlocked = false;
    [SerializeField] private bool hasMap;

    [Header("Map Boundaries — match your world")]
    // World coordinates used to convert the player position into the map marker position.
    public float worldMinX = -200f;
    public float worldMaxX = 200f;
    public float worldMinZ = -200f;
    public float worldMaxZ = 200f;

    [Header("Map UI Settings")]
    public float mapWidth = 600f;
    public float mapHeight = 600f;

    [Header("Locations (optional)")]


    private bool isOpen = false;
    private float mapAlpha = 0f;
    private Transform player;
    private Transform playerView;
    private Texture2D playerArrowTexture;


    public bool HasMap => hasMap;

    void Start()
    {
        GameObject playerObj =
            GameObject.FindGameObjectWithTag("Player");
        if (playerObj != null)
            player = playerObj.transform;

        Camera playerCamera = Camera.main;
        if (playerCamera != null)
            playerView = playerCamera.transform;

        hasMap = mapStartsUnlocked;
    }

    private void OnDestroy()
    {
        if (playerArrowTexture != null)
            Destroy(playerArrowTexture);
    }

    void Update()
    {
        if (!hasMap)
        {
            if (isOpen) SetMapOpen(false);
            mapAlpha = Mathf.Lerp(mapAlpha, 0f, Time.deltaTime * 8f);
            return;
        }

        if (Input.GetKeyDown(mapKey))
            SetMapOpen(!isOpen);

        // Animate alpha
        float target = isOpen ? 1f : 0f;
        mapAlpha = Mathf.Lerp(mapAlpha, target, Time.deltaTime * 8f);
    }

    /// <summary>Called by MapPickup after the player collects the physical Map.</summary>
    public void UnlockMap()
    {
        hasMap = true;
    }

    /// <summary>Restores map ownership from SaveSystem after a saved game loads.</summary>
    public void RestoreMapOwnership(bool mapOwned)
    {
        hasMap = mapStartsUnlocked || mapOwned;
        if (!hasMap && isOpen)
        {
            SetMapOpen(false);
        }
    }

    private void SetMapOpen(bool open)
    {
        isOpen = open;

        // Lock/unlock cursor
        if (isOpen)
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = false;

            // Disable player look
            PlayerController pc = FindObjectOfType<PlayerController>();
            if (pc != null) pc.enabled = false;
        }
        else
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;

            PlayerController pc = FindObjectOfType<PlayerController>();
            if (pc != null) pc.enabled = true;
        }
    }


}
