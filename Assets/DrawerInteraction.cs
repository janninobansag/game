using UnityEngine;

public class DrawerInteraction : MonoBehaviour
{
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
    private bool isOpen = false;
    private bool isAnimating = false;
    private Vector3 closedPosition;
    private Vector3 openPosition;
    private AudioSource audioSource;

    void Start()
    {
        closedPosition = transform.localPosition;
        Vector3 direction = GetDirectionVector();
        openPosition = closedPosition + direction * openDistance * distanceMultiplier;

        audioSource = gameObject.AddComponent<AudioSource>();
        audioSource.spatialBlend = 1f;
        audioSource.playOnAwake = false;
        audioSource.volume = soundVolume;
    }

    Vector3 GetDirectionVector()
    {
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
        if (isAnimating || isBusy) return;

        if (isLocked)
        {
            return;
        }

        isOpen = !isOpen;

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
        isBusy = busy;
    }

    System.Collections.IEnumerator SlideTo(Vector3 targetPosition)
    {
        isAnimating = true;

        Vector3 startPosition = transform.localPosition;
        float elapsed = 0f;

        while (elapsed < 1f)
        {
            elapsed += Time.deltaTime * animationSpeed;
            transform.localPosition = Vector3.Lerp(startPosition, targetPosition, elapsed);
            yield return null;
        }

        transform.localPosition = targetPosition;
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