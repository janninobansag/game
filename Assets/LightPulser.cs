// PURPOSE: Continuously varies a light intensity between configured minimum and maximum values.
using UnityEngine;

public class LightPulser : MonoBehaviour
{
    // Brightness range and speed for a repeating light pulse.
    [Header("Pulse Settings")]
    [Tooltip("Minimum brightness (goes down to 0)")]
    public float minIntensity = 0f;
    
    [Tooltip("Maximum brightness (peak brightness)")]
    public float maxIntensity = 0.4f;
    
    [Tooltip("How fast the light pulses (lower = slower)")]
    public float pulseSpeed = 1.2f;

    [Header("Follow Object")]
    [Tooltip("Optional. Drag the prop this light should stay attached to here.")]
    public Transform followTarget;
    [Tooltip("Position relative to the followed prop. Adjust this to place the light closer to the object.")]
    public Vector3 localPositionOffset = Vector3.zero;

    private Light targetLight;
    private float timer = 0f;

    void Start()
    {
        // Use the Light component attached to this same GameObject.
        targetLight = GetComponent<Light>();
        if (targetLight == null)
        {
            Destroy(this);
        }
    }

    void LateUpdate()
    {
        // Follow after the prop has moved so the light stays at the chosen local offset.
        if (followTarget != null)
            transform.position = followTarget.TransformPoint(localPositionOffset);
    }

    void Update()
    {
        if (targetLight == null) return;

        timer += Time.deltaTime * pulseSpeed;
        // Sine wave goes from -1 to 1, convert to 0 to 1, then scale to intensity range
        float intensity = minIntensity + (Mathf.Sin(timer) + 1f) / 2f * (maxIntensity - minIntensity);
        targetLight.intensity = intensity;
    }
}
