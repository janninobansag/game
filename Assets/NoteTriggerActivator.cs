// PURPOSE: Enables a linked trigger after the configured note has been read.
using UnityEngine;

public class NoteTriggerActivator : MonoBehaviour
{
    // The note that must be read before this progression action can run.
    [Header("Note Reference")]
    public Note targetNote; // drag the note object here

    // Optional object enabled after the required note is read.
    [Header("Trigger to Activate")]
    public GameObject triggerToActivate; // drag the AudioTrigger object here

    // Waypoints that become visible after the player reads the note.
    [Header("Waypoint Locations To Activate")]
    [Tooltip("Drag the indicator location GameObjects that should appear after this note is read.")]
    public GameObject[] waypointLocationsToActivate;
    [Tooltip("Hides the assigned waypoint locations until the note is read.")]
    public bool hideWaypointLocationsAtStart = true;
    [Tooltip("Optional. Assign the INDICATOR's Path Guide Indicator to select a location immediately after reading.")]
    public PathGuideIndicator pathGuide;
    [Tooltip("Set to 0 or higher to select this guide location index after the note is read. Use -1 to leave normal proximity progression unchanged.")]
    public int guideLocationIndexAfterRead = -1;

    [Header("Settings")]
    public float checkInterval = 0.2f;

    private bool activated = false;
    private float checkTimer = 0f;

    void Start()
    {
        // Make sure trigger is disabled at start
        if (triggerToActivate != null)
            triggerToActivate.SetActive(false);

        if (hideWaypointLocationsAtStart)
            SetWaypointLocationsActive(false);
    }

    void Update()
    {
        if (activated) return;
        if (targetNote == null) return;

        checkTimer += Time.deltaTime;
        if (checkTimer < checkInterval) return;
        checkTimer = 0f;

        // Check if note has been read
        if (targetNote.HasBeenRead())
        {
            activated = true;
            ActivateTrigger();
        }
    }

    void ActivateTrigger()
    {
        if (triggerToActivate != null)
            triggerToActivate.SetActive(true);

        SetWaypointLocationsActive(true);

        if (pathGuide != null && guideLocationIndexAfterRead >= 0)
            pathGuide.SetCurrentLocation(guideLocationIndexAfterRead);
    }

    private void SetWaypointLocationsActive(bool active)
    {
        if (waypointLocationsToActivate == null) return;

        foreach (GameObject waypointLocation in waypointLocationsToActivate)
        {
            if (waypointLocation != null)
                waypointLocation.SetActive(active);
        }
    }
}
