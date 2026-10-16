using System.Collections;
using UnityEngine;

/// <summary>
/// Put this on a trigger collider. When the player enters, assigned generator
/// lights blink, then remain off for the rest of this scene session.
/// </summary>
[RequireComponent(typeof(Collider))]
public class GeneratorLightsOffTrigger : MonoBehaviour
{
    // All light components controlled by this one-time trigger.
    [Header("Generator Lights")]
    [Tooltip("Drag every generator Light component here.")]
    public Light[] generatorLights;
    public bool turnLightsOnAtSceneStart = true;

    // Timing for the warning flicker before the lights remain off.
    [Header("Blink")]
    [Min(1)] public int blinkCount = 3;
    [Min(0.02f)] public float blinkInterval = 0.12f;
    [Min(0f)] public float finalOffDelay = 0.15f;

    [Header("Blink Sound")]
    [Tooltip("Played once when the player enters and the generator lights start blinking.")]
    public AudioClip blinkSound;
    [Range(0f, 1f)] public float blinkSoundVolume = 1f;
    [Tooltip("Optional AudioSource for custom spatial audio settings. If empty, the sound plays at this trigger.")]
    public AudioSource blinkAudioSource;

    [Header("Debug")]
    public bool logTrigger = true;

    private bool hasTriggered;

    // Reports whether this trigger has at least one assigned light to save or restore.
    public bool HasConfiguredLights
    {
        get
        {
            if (generatorLights == null)
                return false;

            foreach (Light light in generatorLights)
            {
                if (light != null)
                    return true;
            }

            return false;
        }
    }

    // Reports the actual state of the complete blackout light list.
    public bool AreAnyControlledLightsOn
    {
        get
        {
            if (generatorLights == null)
                return false;

            foreach (Light light in generatorLights)
            {
                if (light != null && light.enabled && light.gameObject.activeInHierarchy)
                    return true;
            }

            return false;
        }
    }

    // Lets the generator timer apply one consistent state to this complete light list.
    public void SetControlledLights(bool enabled)
    {
        SetAllLights(enabled);
    }

    // Restores a completed blackout without replaying its flicker or sound.
    public void RestoreLightsOffState()
    {
        hasTriggered = true;
        SetControlledLights(false);

        Collider trigger = GetComponent<Collider>();
        if (trigger != null)
            trigger.enabled = false;
    }

    private void Reset()
    {
        Collider trigger = GetComponent<Collider>();
        trigger.isTrigger = true;
    }

    private void Start()
    {
        if (turnLightsOnAtSceneStart)
            SetControlledLights(true);
    }

    private void OnTriggerEnter(Collider other)
    {
        if (hasTriggered || !IsPlayer(other))
            return;

        hasTriggered = true;
        PlayBlinkSound();
        StartCoroutine(BlinkThenTurnOff());
    }

    private void PlayBlinkSound()
    {
        if (blinkSound == null)
            return;

        float volume = Mathf.Clamp01(blinkSoundVolume);
        if (blinkAudioSource != null)
            blinkAudioSource.PlayOneShot(blinkSound, volume);
        else
            AudioSource.PlayClipAtPoint(blinkSound, transform.position, volume);
    }

    private IEnumerator BlinkThenTurnOff()
    {
        for (int i = 0; i < blinkCount; i++)
        {
            SetControlledLights(false);
            yield return new WaitForSeconds(blinkInterval);
            SetControlledLights(true);
            yield return new WaitForSeconds(blinkInterval);
        }

        if (finalOffDelay > 0f)
            yield return new WaitForSeconds(finalOffDelay);

        SetControlledLights(false);
        Collider trigger = GetComponent<Collider>();
        if (trigger != null)
            trigger.enabled = false;
    }

    private static bool IsPlayer(Collider other)
    {
        return other.CompareTag("Player") || other.GetComponentInParent<PlayerController>() != null;
    }

    private void SetAllLights(bool enabled)
    {
        if (generatorLights == null)
            return;

        foreach (Light light in generatorLights)
        {
            if (light != null)
                light.enabled = enabled;
        }
    }
}
