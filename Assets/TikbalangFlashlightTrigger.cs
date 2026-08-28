using UnityEngine;

[RequireComponent(typeof(BoxCollider))]
public class TikbalangFlashlightTrigger : MonoBehaviour
{
    [Header("References")]
    public TikbalangAI tikbalang;

    [Header("Trigger Rules")]
    [Tooltip("Only teleports Tikbalang while the held flashlight is switched on.")]
    public bool requireFlashlightOn = true;

    private void Awake()
    {
        // Keep this invisible flashlight volume out of every normal interaction raycast.
        // Unity's built-in Ignore Raycast layer is still valid for trigger detection.
        gameObject.layer = LayerMask.NameToLayer("Ignore Raycast");
        GetComponent<BoxCollider>().isTrigger = true;
    }

    private void Reset()
    {
        gameObject.layer = LayerMask.NameToLayer("Ignore Raycast");
        GetComponent<BoxCollider>().isTrigger = true;
    }

    private void OnTriggerEnter(Collider other)
    {
        TryTeleport(other);
    }

    private void OnTriggerStay(Collider other)
    {
        TryTeleport(other);
    }

    private void TryTeleport(Collider other)
    {
        if (requireFlashlightOn)
        {
            FlashlightPickup heldFlashlight = FlashlightPickup.HeldFlashlight;
            if (heldFlashlight == null || !heldFlashlight.IsOn) return;
        }

        TikbalangAI target = other.GetComponentInParent<TikbalangAI>();
        if (target == null) target = tikbalang;
        if (target != null) target.TeleportToSpawnPoint();
    }
}