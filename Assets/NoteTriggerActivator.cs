using UnityEngine;

public class NoteTriggerActivator : MonoBehaviour
{
    [Header("Note Reference")]
    public Note targetNote; // drag the note object here

    [Header("Trigger to Activate")]
    public GameObject triggerToActivate; // drag the AudioTrigger object here

    [Header("Settings")]
    public float checkInterval = 0.2f;

    private bool activated = false;
    private float checkTimer = 0f;

    void Start()
    {
        // Make sure trigger is disabled at start
        if (triggerToActivate != null)
            triggerToActivate.SetActive(false);
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
        if (triggerToActivate == null) return;

        triggerToActivate.SetActive(true);
    }
}
