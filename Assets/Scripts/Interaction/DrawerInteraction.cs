// PURPOSE: Opens, closes, and locks a drawer with animation and sound while respecting item interactions.
using UnityEngine;

public class DrawerInteraction : MonoBehaviour
{
    // Distance, speed, and lock options for this drawer.
    [Header("Drawer Settings")]
    public float openDistance = 0.1f;
    public float animationSpeed = 2f;
    public bool isLocked = false;
    public KeyCode interactKey = KeyCode.E;

    [Header("Direction Settings")]
    public Direction openDirection = Direction.Up;

    [Header("Fine-Tune")]
    [Range(0.001f, 1f)]
    public float distanceMultiplier = 1f;

    public enum Direction
    {
        Forward,
        Back,
        Left,
        Right,
        Up,
        Down
    }

    [Header("Audio")]
    public AudioClip openSound;
    public AudioClip closeSound;
    [Range(0f, 1f)]
    public float soundVolume = 0.7f;

    [Header("Interaction Settings")]
    public float interactRange = 2f;
    public string playerTag = "Player";

    public bool isBusy = false;

    [Header("Debug")]
    [Tooltip("Logs any non-animation movement that changes this drawer's local position.")]
    public bool logUnexpectedPositionChanges = true;

    private bool isOpen = false;
    private bool isAnimating = false;
    private Vector3 closedPosition;
    private Vector3 openPosition;
    private AudioSource audioSource;
    private Vector3 lastDebugLocalPosition;
    private bool hasDebugPosition;

    void Start()
    {
        // Store the closed position and calculate where the open position will be.
        closedPosition = transform.localPosition;
        Vector3 direction = GetDirectionVector();
        openPosition = closedPosition + direction * openDistance * distanceMultiplier;

        audioSource = gameObject.AddComponent<AudioSource>();
        audioSource.spatialBlend = 1f;
        audioSource.playOnAwake = false;
        audioSource.volume = soundVolume;

        lastDebugLocalPosition = transform.localPosition;
        hasDebugPosition = true;
    }

    Vector3 GetDirectionVector()
    {
        // Convert the Inspector direction selection into this drawer's local movement direction.
        switch (openDirection)
        {
            case Direction.Forward: return transform.forward;
            case Direction.Back: return -transform.forward;
            case Direction.Left: return -transform.right;
            case Direction.Right: return transform.right;
            case Direction.Up: return transform.up;
            case Direction.Down: return -transform.up;
            default: return transform.forward;
        }
    }

    void Update()
    {
        // Let the player toggle the drawer only while looking at it and it is not busy.
        if (isAnimating) return;

        if (Input.GetKeyDown(interactKey))
        {
            // ── STEP 1: Always check what we're looking at FIRST ──
            Ray ray = Camera.main.ScreenPointToRay(new Vector3(Screen.width / 2, Screen.height / 2));
            RaycastHit hit;

            if (Physics.Raycast(ray, out hit, interactRange))
            {
                // ── If we're looking at a pickup/item, ABSOLUTELY DO NOT TOGGLE ──
                if (hit.collider.CompareTag("Pickup") || hit.collider.CompareTag("Item"))
                {
                    return;
                }

                // ── Only toggle drawer if looking at the drawer itself ──
                if (hit.collider.gameObject == gameObject)
                {
                    if (isBusy)
                    {
                        return;
                    }
                    
                    ToggleDrawer();
                }
            }
        }
    }

    public void ToggleDrawer()
    {
        // Start the open or close animation from the drawer's current position.
        if (isAnimating || isBusy) return;

        if (isLocked)
        {
            return;
        }

        isOpen = !isOpen;

        // Lock every stored pickup to the drawer before it moves. This keeps
        // item positions synchronized during both opening and closing.
        DrawerItemParent drawerStorage = GetComponent<DrawerItemParent>();
        if (drawerStorage != null)
            drawerStorage.StoreItemsInside();

        if (isOpen)
        {
            Vector3 direction = GetDirectionVector();
            openPosition = closedPosition + direction * openDistance * distanceMultiplier;
            StartCoroutine(SlideTo(openPosition));
            PlaySound(openSound);
        }
        else
        {
            StartCoroutine(SlideTo(closedPosition));
            PlaySound(closeSound);
        }
    }

    public void SetBusy(bool busy)
    {
        // Other scripts use this to stop opening while an item is being picked up or stored.
        isBusy = busy;
    }

    System.Collections.IEnumerator SlideTo(Vector3 targetPosition)
    {
        isAnimating = true;

        Vector3 startPosition = transform.localPosition;
        float elapsed = 0f;
        DrawerItemParent drawerStorage = GetComponent<DrawerItemParent>();

        while (elapsed < 1f)
        {
            elapsed += Time.deltaTime * animationSpeed;
            transform.localPosition = Vector3.Lerp(startPosition, targetPosition, elapsed);
            if (drawerStorage != null)
                drawerStorage.SyncStoredItems();
            yield return null;
        }

        transform.localPosition = targetPosition;
        if (drawerStorage != null)
            drawerStorage.SyncStoredItems();
        Physics.SyncTransforms();
        isAnimating = false;
    }

    void PlaySound(AudioClip clip)
    {
        if (clip != null && audioSource != null)
        {
            audioSource.PlayOneShot(clip, soundVolume);
        }
    }

    public void Lock() => isLocked = true;
    public void Unlock() => isLocked = false;
    public bool IsOpen() => isOpen;

    /// <summary>Applies a saved drawer pose after its Start setup has completed.</summary>
    public void RestoreSavedState(bool open, Vector3 savedLocalPosition)
    {
        StopAllCoroutines();
        isAnimating = false;
        isOpen = open;
        transform.localPosition = savedLocalPosition;
        openPosition = savedLocalPosition;

        DrawerItemParent drawerStorage = GetComponent<DrawerItemParent>();
        if (drawerStorage != null)
        {
            drawerStorage.SyncStoredItems();
        }

        Physics.SyncTransforms();
    }

    private void LateUpdate()
    {
        if (!logUnexpectedPositionChanges)
            return;

        if (!hasDebugPosition)
        {
            lastDebugLocalPosition = transform.localPosition;
            hasDebugPosition = true;
            return;
        }

        if (!isAnimating && (transform.localPosition - lastDebugLocalPosition).sqrMagnitude > 0.000001f)
        {
            Debug.LogWarning($"[Drawer Position Changed] {name} | open={isOpen} | from={lastDebugLocalPosition} | to={transform.localPosition}", this);
        }

        lastDebugLocalPosition = transform.localPosition;
    }

    void OnDrawGizmosSelected()
    {
        Vector3 direction = GetDirectionVector();
        Vector3 startPos = transform.position;
        Vector3 endPos = startPos + direction * openDistance * distanceMultiplier;

        Gizmos.color = Color.green;
        Gizmos.DrawLine(startPos, endPos);
        Gizmos.DrawSphere(endPos, 0.05f);

        Gizmos.color = Color.yellow;
        Gizmos.DrawRay(endPos, direction * 0.1f);
    }

    void OnGUI()
    {
        if (isAnimating || isBusy) return;

        Ray ray = Camera.main.ScreenPointToRay(new Vector3(Screen.width / 2, Screen.height / 2));
        RaycastHit hit;

        if (Physics.Raycast(ray, out hit, interactRange))
        {
            if (hit.collider.CompareTag("Pickup") || hit.collider.CompareTag("Item"))
            {
                return;
            }

            if (hit.collider.gameObject == gameObject)
            {
                string msg = isLocked ? "Drawer is locked!" : "Click to interact";

                GUIStyle style = new GUIStyle();
                style.fontSize = 22;
                style.alignment = TextAnchor.MiddleCenter;
                style.normal.textColor = isLocked ? Color.red : Color.white;

                GUIStyle shadow = new GUIStyle();
                shadow.fontSize = 22;
                shadow.alignment = TextAnchor.MiddleCenter;
                shadow.normal.textColor = Color.black;

                GUI.Label(new Rect(Screen.width / 2 - 199, Screen.height / 2 + 51, 400, 40), msg, shadow);
                GUI.Label(new Rect(Screen.width / 2 - 200, Screen.height / 2 + 50, 400, 40), msg, style);
            }
        }
    }
}
