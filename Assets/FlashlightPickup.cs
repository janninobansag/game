using UnityEngine;

public class FlashlightPickup : MonoBehaviour
{
    [Header("Flashlight Settings")]
    public float batteryLife = 100f;
    public float drainRate = 2f;

    [Header("Light Settings")]
    [Tooltip("Maximum brightness of the flashlight beam when the battery is full.")]
    [Min(0f)] public float intensity = 5f;
    [Min(0f)] public float range = 40f;

    [Header("Beam Shape")]
    [Range(1f, 179f)] public float spotAngle = 42f;
    [Range(0f, 179f)] public float innerSpotAngle = 28f;
    public LightShadows shadows = LightShadows.None;
    [Header("Near Object Dimming")]
    [Tooltip("Dims the beam when it hits an object close to the flashlight.")]
    public bool dimNearObjects = true;
    [Min(0.05f)] public float dimmingDistance = 2f;
    [Range(0f, 1f)] public float minimumNearObjectIntensity = 0.3f;
    public LayerMask dimmingLayers = ~0;

    [Header("Beam Aim")]
    [Tooltip("Keeps the light beam aimed where the player camera looks, even though the flashlight model is rotated in the hand.")]
    public bool aimBeamWithCamera = true;

    [Header("State")]
    public bool wasDropped = false;
    public bool isSpawnedFromPrefab = false;

    private bool isHeld = false;
    private bool isOn = false;
    private Light flashlight;
    private float currentBattery;
    private bool isInitialized = false; // ÃƒÂ¢Ã¢â‚¬ÂÃ¢â€šÂ¬ÃƒÂ¢Ã¢â‚¬ÂÃ¢â€šÂ¬ NEW: Track if initialized

    public static FlashlightPickup HeldFlashlight { get; private set; }
    public bool IsHeld => isHeld;
    public bool IsOn => isOn;


    private void OnValidate()
    {
        Light previewLight = GetComponentInChildren<Light>();
        if (previewLight == null)
            return;

        previewLight.intensity = intensity;
        previewLight.range = range;
        previewLight.spotAngle = spotAngle;
        previewLight.innerSpotAngle = Mathf.Min(innerSpotAngle, spotAngle);
        previewLight.shadows = shadows;
    }
    void Start()
    {
        InitializeFlashlight();
    }

    // ÃƒÂ¢Ã¢â‚¬ÂÃ¢â€šÂ¬ÃƒÂ¢Ã¢â‚¬ÂÃ¢â€šÂ¬ NEW: Separate initialization method ÃƒÂ¢Ã¢â‚¬ÂÃ¢â€šÂ¬ÃƒÂ¢Ã¢â‚¬ÂÃ¢â€šÂ¬
    private void InitializeFlashlight()
    {
        if (isInitialized) return;

        flashlight = GetComponentInChildren<Light>();
        if (flashlight != null)
        {
            flashlight.enabled = false;
            flashlight.type = LightType.Spot;
            flashlight.intensity = intensity;
            flashlight.range = range;
            flashlight.spotAngle = spotAngle;
            flashlight.innerSpotAngle = Mathf.Min(innerSpotAngle, spotAngle);
            flashlight.shadows = shadows;
        }

        // ÃƒÂ¢Ã¢â‚¬ÂÃ¢â€šÂ¬ÃƒÂ¢Ã¢â‚¬ÂÃ¢â€šÂ¬ Only set battery to full if NOT spawned from prefab ÃƒÂ¢Ã¢â‚¬ÂÃ¢â€šÂ¬ÃƒÂ¢Ã¢â‚¬ÂÃ¢â€šÂ¬
        if (!isSpawnedFromPrefab)
        {
            currentBattery = batteryLife;
        }
        else
        {
            // If spawned from prefab, battery should already be set
        }

        isInitialized = true;
    }

    void Update()
    {
        if (isHeld && Input.GetKeyDown(KeyCode.F))
        {
            if (!isOn && currentBattery <= 0f)
            {
                return;
            }

            isOn = !isOn;
            if (flashlight != null)
                flashlight.enabled = isOn;

        }

        if (isOn)
        {
            if (currentBattery > 0f)
            {
                currentBattery -= drainRate * Time.deltaTime;

                if (flashlight != null)
                {
                    float batteryPercent = currentBattery / batteryLife;
                    float batteryIntensity = intensity * Mathf.Clamp01(batteryPercent + 0.2f);
                    flashlight.intensity = batteryIntensity * GetNearObjectDimmingMultiplier();
                }

                if (currentBattery <= 0f)
                {
                    currentBattery = 0f;
                    isOn = false;

                    if (flashlight != null)
                    {
                        flashlight.enabled = false;
                        flashlight.intensity = intensity;
                    }

                }
            }
            else
            {
                isOn = false;
                if (flashlight != null)
                    flashlight.enabled = false;

            }
        }
    }


    private float GetNearObjectDimmingMultiplier()
    {
        if (!dimNearObjects || flashlight == null)
            return 1f;

        Vector3 origin = flashlight.transform.position + flashlight.transform.forward * 0.05f;
        if (!Physics.Raycast(origin, flashlight.transform.forward, out RaycastHit hit, dimmingDistance, dimmingLayers, QueryTriggerInteraction.Ignore))
            return 1f;

        // Full brightness at the edge of the dimming distance; minimum brightness at point-blank range.
        float distancePercent = Mathf.Clamp01(hit.distance / dimmingDistance);
        return Mathf.Lerp(minimumNearObjectIntensity, 1f, distancePercent);
    }
    void LateUpdate()
    {
        if (!isHeld || !aimBeamWithCamera || flashlight == null)
            return;

        Camera cameraToFollow = Camera.main;
        if (cameraToFollow != null)
            flashlight.transform.rotation = cameraToFollow.transform.rotation;
    }

    public void SetHeld(bool held)
    {
        isHeld = held;

        if (held)
            HeldFlashlight = this;
        else if (HeldFlashlight == this)
            HeldFlashlight = null;
    }

    public void Recharge(float amount)
    {
        currentBattery = Mathf.Min(currentBattery + amount, batteryLife);
    }

    public void SetBattery(float amount)
    {
        // ÃƒÂ¢Ã¢â‚¬ÂÃ¢â€šÂ¬ÃƒÂ¢Ã¢â‚¬ÂÃ¢â€šÂ¬ Ensure initialization first ÃƒÂ¢Ã¢â‚¬ÂÃ¢â€šÂ¬ÃƒÂ¢Ã¢â‚¬ÂÃ¢â€šÂ¬
        if (!isInitialized)
        {
            InitializeFlashlight();
        }
        
        currentBattery = Mathf.Clamp(amount, 0f, batteryLife);
    }

    public bool IsFullBattery()
    {
        return currentBattery >= batteryLife;
    }

    public float GetBatteryPercent()
    {
        return currentBattery / batteryLife;
    }

    public void ResetDroppedState()
    {
        wasDropped = false;
    }

    void OnDestroy()
    {
        if (HeldFlashlight == this)
            HeldFlashlight = null;    }
}
