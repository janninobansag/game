// PURPOSE: Keeps a runtime DropLight a fixed world-space distance above its dropped item.
using UnityEngine;

public sealed class DropItemLightFollower : MonoBehaviour
{
    // The item that owns this temporary glow light.
    public Transform targetItem;
    // World-space offset so parent scale cannot move the light far from the item.
    public Vector3 worldOffset = new Vector3(0f, 0.2f, 0f);

    private void LateUpdate()
    {
        if (targetItem == null)
        {
            Destroy(gameObject);
            return;
        }

        // Use world position and compensate for parent scale every frame.
        transform.position = targetItem.position + worldOffset;
        transform.rotation = Quaternion.identity;

        Vector3 parentScale = targetItem.lossyScale;
        transform.localScale = new Vector3(
            SafeInverse(parentScale.x),
            SafeInverse(parentScale.y),
            SafeInverse(parentScale.z));
    }

    private static float SafeInverse(float value)
    {
        return Mathf.Abs(value) > 0.0001f ? 1f / value : 1f;
    }
}
