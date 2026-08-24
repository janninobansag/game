using System.Collections;
using UnityEngine;

public class LightFlickerTrigger : MonoBehaviour
{
    [Header("Trigger Settings")]
    public string playerTag = "Player";
    public bool triggerOnce = true;

    [Header("Light Settings")]
    public Light[] lightsToFlicker;
    public bool flickerAllLights = false; // flicker ALL lights in scene

    [Header("Flicker Pattern")]
    public float flickerDuration = 5f;
    public float minOnTime = 0.05f;
    public float maxOnTime = 0.3f;
    public float minOffTime = 0.02f;
    public float maxOffTime = 0.25f;

    [Header("Intensity Settings")]
    public float normalIntensity = 1f;
    public float minFlickerIntensity = 0f;
    public float maxFlickerIntensity = 1.5f;

    [Header("Audio Settings")]
    public AudioClip flickerSound;
    public float flickerSoundVolume = 0.4f;

    [Header("After Flicker")]
    public bool turnOffAfter = false;
    public bool restoreAfter = true;

    private bool hasTriggered = false;
    private AudioSource audioSource;
    private Light[] allLights;
    private float[] originalIntensities;
    private bool[] originalStates;

    void Start()
    {
        // Setup audio
        audioSource = gameObject.AddComponent<AudioSource>();
        audioSource.spatialBlend = 0f;
        audioSource.playOnAwake = false;
        audioSource.volume = flickerSoundVolume;
        if (flickerSound != null)
            audioSource.clip = flickerSound;

        // Setup trigger collider
        Collider col = GetComponent<Collider>();
        if (col != null) col.isTrigger = true;

        // Get all lights if needed
        if (flickerAllLights)
            allLights = FindObjectsOfType<Light>();
        else
            allLights = lightsToFlicker;

        // Store original states
        if (allLights != null)
        {
            originalIntensities = new float[allLights.Length];
            originalStates = new bool[allLights.Length];
            for (int i = 0; i < allLights.Length; i++)
            {
                if (allLights[i] != null)
                {
                    originalIntensities[i] = allLights[i].intensity;
                    originalStates[i] = allLights[i].enabled;
                }
            }
        }
    }

    void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag(playerTag)) return;
        if (triggerOnce && hasTriggered) return;

        hasTriggered = true;
        StartCoroutine(FlickerRoutine());
    }

    IEnumerator FlickerRoutine()
    {
        float flickerEndTime = Time.time + flickerDuration;

        // Play flicker sound
        if (flickerSound != null)
            audioSource.Play();

        while (Time.time < flickerEndTime)
        {
            // Random flicker pattern
            int pattern = Random.Range(0, 4);

            if (pattern == 0)
            {
                // Quick on/off burst
                yield return StartCoroutine(QuickBurst());
            }
            else if (pattern == 1)
            {
                // Slow dim
                yield return StartCoroutine(SlowDim());
            }
            else if (pattern == 2)
            {
                // Random stutters
                yield return StartCoroutine(Stutter());
            }
            else
            {
                // Brief off
                SetLightsEnabled(false);
                yield return new WaitForSeconds(
                    Random.Range(minOffTime, maxOffTime * 2f));
                SetLightsEnabled(true);
                SetLightsIntensity(normalIntensity);
                yield return new WaitForSeconds(
                    Random.Range(minOnTime, maxOnTime));
            }
        }

        // After flicker ends
        if (turnOffAfter)
        {
            SetLightsEnabled(false);
        }
        else if (restoreAfter)
        {
            RestoreLights();
        }

        audioSource.Stop();
    }

    IEnumerator QuickBurst()
    {
        int bursts = Random.Range(3, 8);
        for (int i = 0; i < bursts; i++)
        {
            SetLightsEnabled(false);
            yield return new WaitForSeconds(
                Random.Range(minOffTime, maxOffTime));
            SetLightsEnabled(true);
            SetLightsIntensity(
                Random.Range(minFlickerIntensity, maxFlickerIntensity));
            yield return new WaitForSeconds(
                Random.Range(minOnTime, maxOnTime));
        }
        SetLightsIntensity(normalIntensity);
    }

    IEnumerator SlowDim()
    {
        float elapsed = 0f;
        float duration = Random.Range(0.3f, 0.8f);
        float targetIntensity = Random.Range(0.1f, 0.5f);

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float intensity = Mathf.Lerp(normalIntensity,
                targetIntensity, elapsed / duration);
            SetLightsIntensity(intensity);
            yield return null;
        }

        yield return new WaitForSeconds(Random.Range(0.1f, 0.4f));

        // Snap back
        elapsed = 0f;
        duration = Random.Range(0.1f, 0.3f);
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float intensity = Mathf.Lerp(targetIntensity,
                normalIntensity, elapsed / duration);
            SetLightsIntensity(intensity);
            yield return null;
        }

        SetLightsIntensity(normalIntensity);
    }

    IEnumerator Stutter()
    {
        int stutters = Random.Range(2, 6);
        for (int i = 0; i < stutters; i++)
        {
            SetLightsIntensity(
                Random.Range(minFlickerIntensity, maxFlickerIntensity));
            yield return new WaitForSeconds(Random.Range(0.02f, 0.08f));
        }
        SetLightsIntensity(normalIntensity);
        yield return new WaitForSeconds(Random.Range(0.05f, 0.2f));
    }

    void SetLightsEnabled(bool enabled)
    {
        if (allLights == null) return;
        foreach (Light l in allLights)
            if (l != null) l.enabled = enabled;
    }

    void SetLightsIntensity(float intensity)
    {
        if (allLights == null) return;
        foreach (Light l in allLights)
            if (l != null) l.intensity = intensity;
    }

    void RestoreLights()
    {
        if (allLights == null) return;
        for (int i = 0; i < allLights.Length; i++)
        {
            if (allLights[i] != null)
            {
                allLights[i].intensity = originalIntensities[i];
                allLights[i].enabled = originalStates[i];
            }
        }
    }

    public void ResetTrigger()
    {
        hasTriggered = false;
        RestoreLights();
    }
}