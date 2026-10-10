// PURPOSE: Guides the player through ordered waypoint children using a screen-edge arrow.
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class PathGuideIndicator : MonoBehaviour
{
    // Ordered destinations the arrow guides the player toward.
    [Header("Waypoint Locations")]
    [Tooltip("Leave empty to use this object's direct children in hierarchy order.")]
    public Transform[] indicatorLocations;
    public bool useChildLocations = true;
    [Tooltip("Only displays an arrow when the current location GameObject is active. Use this with Note Trigger Activator to reveal later locations after a note is read.")]
    public bool onlyGuideActiveLocations = true;
    [Min(0.1f)] public float reachDistance = 2f;
    public bool loopLocations = false;

    // Screen-edge arrow and label appearance settings.
    [Header("Display")]
    public bool showGuide = true;
    public bool showDistance = true;
    public bool showLocationName = false;
    [Range(0.05f, 0.48f)] public float screenEdgeDistance = 0.38f;
    [Min(8f)] public float arrowSize = 26f;
    [Min(1f)] public float arrowLineWidth = 5f;
    public Color arrowColor = new Color(1f, 0.8f, 0.15f, 1f);

    [Header("Visibility")]
    [Tooltip("Hides the guide and pauses waypoint progress while the Story Intro is playing.")]
    public bool hideDuringStoryIntro = true;
    [Tooltip("Leave empty to find the active StoryIntro automatically.")]
    public StoryIntro storyIntro;

    [Header("Player")]
    public Transform player;
    public string playerTag = "Player";

    [Header("Progress")]
    [Tooltip("Optional unique ID for saving this guide's progress. Leave empty to use its scene hierarchy path.")]
    public string saveId;
    [SerializeField] private int currentLocationIndex;

    private Camera playerCamera;

    public Transform CurrentLocation
    {
        get
        {
            if (indicatorLocations == null || currentLocationIndex < 0 ||
                currentLocationIndex >= indicatorLocations.Length)
                return null;
            Transform location = indicatorLocations[currentLocationIndex];
            if (onlyGuideActiveLocations && (location == null || !location.gameObject.activeInHierarchy))
                return null;
            return location;
        }
    }

    /// <summary>Waypoint number currently selected by this guide, including the completed state.</summary>
    public int CurrentLocationIndex => currentLocationIndex;

    /// <summary>Unique key used by SaveSystem to store this guide's progress.</summary>
    public string SaveKey => "PathGuide:" + GetSaveId();

    private void Awake()
    {
        RefreshChildLocations();
        FindPlayerIfNeeded();
        FindStoryIntroIfNeeded();
    }

    private void Update()
    {
        if (IsStoryIntroActive())
            return;

        FindPlayerIfNeeded();
        if (player == null || CurrentLocation == null)
            return;

        Vector3 playerPosition = player.position;
        Vector3 targetPosition = CurrentLocation.position;
        playerPosition.y = 0f;
        targetPosition.y = 0f;

        if (Vector3.Distance(playerPosition, targetPosition) <= reachDistance)
            AdvanceToNextLocation();
    }

    [ContextMenu("Refresh Child Locations")]
    public void RefreshChildLocations()
    {
        if (!useChildLocations || (indicatorLocations != null && indicatorLocations.Length > 0))
            return;

        List<Transform> locations = new List<Transform>();
        foreach (Transform child in transform)
            locations.Add(child);
        indicatorLocations = locations.ToArray();
    }

    [ContextMenu("Reset Guide Progress")]
    public void ResetGuideProgress()
    {
        currentLocationIndex = 0;
    }

    public void SetCurrentLocation(int index)
    {
        if (indicatorLocations == null || index < 0 || index >= indicatorLocations.Length)
            return;
        currentLocationIndex = index;
    }

    /// <summary>Restores a saved waypoint number. An index equal to the waypoint count means the guide was completed.</summary>
    public void RestoreProgress(int savedIndex)
    {
        RefreshChildLocations();
        int locationCount = indicatorLocations == null ? 0 : indicatorLocations.Length;
        currentLocationIndex = Mathf.Clamp(savedIndex, 0, locationCount);

        // The guide could only have pointed at an active waypoint when the game
        // was saved. Restore that target's visibility before drawing the arrow.
        if (onlyGuideActiveLocations && currentLocationIndex < locationCount)
        {
            Transform savedLocation = indicatorLocations[currentLocationIndex];
            if (savedLocation != null)
                savedLocation.gameObject.SetActive(true);
        }
    }

    public void AdvanceToNextLocation()
    {
        if (indicatorLocations == null || indicatorLocations.Length == 0)
            return;

        currentLocationIndex++;
        if (currentLocationIndex >= indicatorLocations.Length)
            currentLocationIndex = loopLocations ? 0 : indicatorLocations.Length;
    }

    private void FindPlayerIfNeeded()
    {
        if (player == null)
        {
            GameObject playerObject = GameObject.FindGameObjectWithTag(playerTag);
            if (playerObject != null) player = playerObject.transform;
        }
        if (playerCamera == null) playerCamera = Camera.main;
    }

    private void FindStoryIntroIfNeeded()
    {
        if (storyIntro == null)
            storyIntro = FindObjectOfType<StoryIntro>();
    }

    private bool IsStoryIntroActive()
    {
        if (!hideDuringStoryIntro)
            return false;

        FindStoryIntroIfNeeded();
        return storyIntro != null && storyIntro.IsIntroActive;
    }

    private string GetSaveId()
    {
        if (!string.IsNullOrWhiteSpace(saveId))
            return saveId.Trim();

        string path = SceneManager.GetActiveScene().name;
        Transform current = transform;
        while (current != null)
        {
            path += "/" + current.name + "[" + current.GetSiblingIndex() + "]";
            current = current.parent;
        }

        return path;
    }

    private void OnGUI()
    {
        if (!showGuide || IsStoryIntroActive() || player == null || CurrentLocation == null)
            return;
        if (playerCamera == null) playerCamera = Camera.main;
        if (playerCamera == null) return;

        Vector3 toTarget = CurrentLocation.position - player.position;
        toTarget.y = 0f;
        if (toTarget.sqrMagnitude < 0.001f) return;

        Vector3 cameraForward = Vector3.ProjectOnPlane(playerCamera.transform.forward, Vector3.up).normalized;
        Vector3 cameraRight = Vector3.ProjectOnPlane(playerCamera.transform.right, Vector3.up).normalized;
        Vector2 screenDirection = new Vector2(
            Vector3.Dot(toTarget.normalized, cameraRight),
            -Vector3.Dot(toTarget.normalized, cameraForward));
        if (screenDirection.sqrMagnitude < 0.001f) screenDirection = Vector2.up;
        screenDirection.Normalize();

        Vector2 screenCenter = new Vector2(Screen.width * 0.5f, Screen.height * 0.5f);
        float radius = Mathf.Min(Screen.width, Screen.height) * screenEdgeDistance;
        Vector2 arrowCenter = screenCenter + screenDirection * radius;

        DrawArrow(arrowCenter + Vector2.one * 2f, screenDirection, arrowSize, arrowLineWidth + 2f, Color.black);
        DrawArrow(arrowCenter, screenDirection, arrowSize, arrowLineWidth, arrowColor);

        if (!showDistance && !showLocationName) return;

        string label = showLocationName ? CurrentLocation.name : string.Empty;
        if (showDistance)
        {
            if (!string.IsNullOrEmpty(label)) label += "\n";
            label += Mathf.CeilToInt(toTarget.magnitude) + " m";
        }

        GUIStyle style = new GUIStyle
        {
            fontSize = 18,
            alignment = TextAnchor.UpperCenter,
            fontStyle = FontStyle.Bold
        };
        style.normal.textColor = arrowColor;
        GUI.Label(new Rect(arrowCenter.x - 80f, arrowCenter.y + arrowSize + 10f, 160f, 45f), label, style);
    }

    private static void DrawArrow(Vector2 center, Vector2 direction, float size, float lineWidth, Color color)
    {
        Vector2 tip = center + direction * size;
        Vector2 tail = center - direction * size * 0.65f;
        Vector2 perpendicular = new Vector2(-direction.y, direction.x);
        Vector2 headBase = tip - direction * size * 0.72f;

        GUI.color = color;
        DrawLine(tail, tip, lineWidth);
        DrawLine(tip, headBase + perpendicular * size * 0.48f, lineWidth);
        DrawLine(tip, headBase - perpendicular * size * 0.48f, lineWidth);
        GUI.color = Color.white;
    }

    private static void DrawLine(Vector2 from, Vector2 to, float width)
    {
        Vector2 delta = to - from;
        float length = delta.magnitude;
        if (length <= 0.001f) return;

        Matrix4x4 previousMatrix = GUI.matrix;
        float angle = Mathf.Atan2(delta.y, delta.x) * Mathf.Rad2Deg;
        GUIUtility.RotateAroundPivot(angle, from);
        GUI.DrawTexture(new Rect(from.x, from.y - width * 0.5f, length, width), Texture2D.whiteTexture);
        GUI.matrix = previousMatrix;
    }

    private void OnDrawGizmosSelected()
    {
        Transform[] locations = indicatorLocations;
        if ((locations == null || locations.Length == 0) && useChildLocations)
        {
            locations = new Transform[transform.childCount];
            for (int i = 0; i < transform.childCount; i++) locations[i] = transform.GetChild(i);
        }
        if (locations == null) return;

        Gizmos.color = Color.yellow;
        for (int i = 0; i < locations.Length; i++)
        {
            if (locations[i] == null) continue;
            Gizmos.DrawWireSphere(locations[i].position, reachDistance);
            if (i > 0 && locations[i - 1] != null)
                Gizmos.DrawLine(locations[i - 1].position, locations[i].position);
        }
    }
}
