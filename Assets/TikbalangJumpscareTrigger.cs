using UnityEngine;

/// <summary>
/// Starts Tikbalang's first teleport-and-jumpscare sequence when the player
/// enters this object's trigger collider.
/// </summary>
[RequireComponent(typeof(Collider))]
public class TikbalangJumpscareTrigger : MonoBehaviour
{
    [Tooltip("The Tikbalang AI that will teleport in front of the player and start the jumpscare.")]
    public TikbalangAI tikbalang;

    [Tooltip("When enabled, this detector can start the encounter only once per play session.")]
    public bool triggerOnce = true;

    [Header("Trigger Debug")]
    [Tooltip("Shows detector activity in the Console. Disable after testing.")]
    public bool debugTrigger = true;

    private Collider triggerCollider;
    private PlayerController playerController;
    private bool hasTriggered;

    private void Awake()
    {
        triggerCollider = GetComponent<Collider>();
        triggerCollider.isTrigger = true;

        if (tikbalang == null)
            tikbalang = FindObjectOfType<TikbalangAI>();

        playerController = FindObjectOfType<PlayerController>();
    }

    private void OnEnable()
    {
        LogDebug("Detector armed. Waiting for the player to enter.");
    }

    private void OnTriggerEnter(Collider other)
    {
        string objectName = other.gameObject.name;
        string rootName = other.transform.root.name;
        bool isPlayer = IsPlayer(other);

        LogDebug($"OnTriggerEnter: Object='{objectName}', Root='{rootName}', Tag='{other.tag}', Layer='{LayerMask.LayerToName(other.gameObject.layer)}', IsPlayer={isPlayer}.");

        if (hasTriggered)
        {
            LogDebug("Ignored: this detector has already been used.");
            return;
        }

        if (!isPlayer)
        {
            LogDebug("Ignored: the entering collider is not part of the player.");
            return;
        }

        if (tikbalang == null)
        {
            LogDebug("Blocked: no Tikbalang AI is assigned.");
            return;
        }

        if (!tikbalang.TriggerDetectionJumpscare(this))
        {
            LogDebug("Blocked: Tikbalang AI rejected the trigger request. Check its debug message.");
            return;
        }

        hasTriggered = true;
        LogDebug("Accepted player entry. Tikbalang jumpscare has been requested.");

        if (triggerOnce && triggerCollider != null)
            triggerCollider.enabled = false;
    }

    // Only the collider attached to the PlayerController object may activate this detector.
    // This prevents child objects such as "telepoter" from starting the jumpscare.
    private bool IsPlayer(Collider other)
    {
        if (playerController == null)
            playerController = FindObjectOfType<PlayerController>();

        return playerController != null && other.transform == playerController.transform;
    }

    private void LogDebug(string message)
    {
        if (debugTrigger)
            Debug.Log($"[Tikbalang Trigger Debug] {message}", this);
    }
}