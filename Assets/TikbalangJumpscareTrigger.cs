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

    private void OnTriggerEnter(Collider other)
    {
        if (hasTriggered)
            return;

        if (!IsPlayer(other))
            return;

        if (tikbalang == null)
            return;

        if (!tikbalang.TriggerDetectionJumpscare(this))
            return;

        hasTriggered = true;

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

}
