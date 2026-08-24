using UnityEngine;

public class LightPulser : MonoBehaviour
{
    [Header("Pulse Settings")]
    [Tooltip("Minimum brightness (goes down to 0)")]
    public float minIntensity = 0f;
    
    [Tooltip("Maximum brightness (peak brightness)")]
    public float maxIntensity = 0.4f;
    
    [Tooltip("How fast the light pulses (lower = slower)")]
    public float pulseSpeed = 1.2f;

    private Light targetLight;
    private float timer = 0f;

    void Start()
    {
        targetLight = GetComponent<Light>();
        if (targetLight == null)
        {
            Destroy(this);
        }
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