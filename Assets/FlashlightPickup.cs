using UnityEngine;

public class FlashlightPickup : MonoBehaviour
{
    [Header("Flashlight Settings")]
    public float batteryLife = 100f;
    public float drainRate = 2f;
    public float intensity = 5f;
    public float range = 40f;

    [Header("Fog Settings")]
    public bool adjustFogWhenOn = false;
    public float fogStartDistance = 10f;
    public float fogEndDistance = 80f;
    public float fogDensity = 0.02f;

    [Header("State")]
    public bool wasDropped = false;
    public bool isSpawnedFromPrefab = false;

    private bool isHeld = false;
    private bool isOn = false;
    private Light flashlight;
    private float currentBattery;
    private float originalFogStart;
    private float originalFogEnd;
    private float originalFogDensity;
    private bool originalFogEnabled;
    private bool isInitialized = false; // ── NEW: Track if initialized

    public static FlashlightPickup HeldFlashlight { get; private set; }
    public bool IsHeld => isHeld;

    void Start()
    {
        InitializeFlashlight();
    }

    // ── NEW: Separate initialization method ──
    private void InitializeFlashlight()
    {
        if (isInitialized) return;

        flashlight = GetComponentInChildren<Light>();
        if (flashlight != null)
        {
            flashlight.enabled = false;
            flashlight.intensity = intensity;
            flashlight.range = range;
        }

        // ── Only set battery to full if NOT spawned from prefab ──
        if (!isSpawnedFromPrefab)
        {
            currentBattery = batteryLife;
        }
        else
        {
            // If spawned from prefab, battery should already be set
        }

        originalFogStart = RenderSettings.fogStartDistance;
        originalFogEnd = RenderSettings.fogEndDistance;
        originalFogDensity = RenderSettings.fogDensity;
        originalFogEnabled = RenderSettings.fog;

        isInitialized = true;
    }

    void Update()
    {
        if (!isHeld) return;

        if (Input.GetKeyDown(KeyCode.F))
        {
            if (!isOn && currentBattery <= 0f)
            {
                return;
            }

            isOn = !isOn;
            if (flashlight != null)
                flashlight.enabled = isOn;

            if (adjustFogWhenOn)
            {
                if (isOn)
                {
                    RenderSettings.fogStartDistance = fogStartDistance;
                    RenderSettings.fogEndDistance = fogEndDistance;
                    RenderSettings.fogDensity = fogDensity;
                }
                else
                {
                    RenderSettings.fogStartDistance = originalFogStart;
                    RenderSettings.fogEndDistance = originalFogEnd;
                    RenderSettings.fogDensity = originalFogDensity;
                }
            }
        }

        if (isOn)
        {
            if (currentBattery > 0f)
            {
                currentBattery -= drainRate * Time.deltaTime;

                if (flashlight != null)
                {
                    float batteryPercent = currentBattery / batteryLife;
                    flashlight.intensity = intensity * Mathf.Clamp01(batteryPercent + 0.2f);
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

                    if (adjustFogWhenOn)
                    {
                        RenderSettings.fogStartDistance = originalFogStart;
                        RenderSettings.fogEndDistance = originalFogEnd;
                        RenderSettings.fogDensity = originalFogDensity;
                    }
                }
            }
            else
            {
                isOn = false;
                if (flashlight != null)
                    flashlight.enabled = false;

                if (adjustFogWhenOn)
                {
                    RenderSettings.fogStartDistance = originalFogStart;
                    RenderSettings.fogEndDistance = originalFogEnd;
                    RenderSettings.fogDensity = originalFogDensity;
                }
            }
        }
    }

    public void SetHeld(bool held)
    {
        isHeld = held;

        if (held)
            HeldFlashlight = this;
        else if (HeldFlashlight == this)
            HeldFlashlight = null;
        
        if (!held)
        {
            wasDropped = true;
            isOn = false;
            if (flashlight != null)
                flashlight.enabled = false;

            if (adjustFogWhenOn)
            {
                RenderSettings.fogStartDistance = originalFogStart;
                RenderSettings.fogEndDistance = originalFogEnd;
                RenderSettings.fogDensity = originalFogDensity;
            }
        }
    }

    public void Recharge(float amount)
    {
        currentBattery = Mathf.Min(currentBattery + amount, batteryLife);
    }

    public void SetBattery(float amount)
    {
        // ── Ensure initialization first ──
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
            HeldFlashlight = null;
        if (adjustFogWhenOn)
        {
            RenderSettings.fogStartDistance = originalFogStart;
            RenderSettings.fogEndDistance = originalFogEnd;
            RenderSettings.fogDensity = originalFogDensity;
        }
    }
}
