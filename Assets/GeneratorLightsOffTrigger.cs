using System.Collections;
using UnityEngine;

/// <summary>
/// Put this on a trigger collider. When the player enters, assigned generator
/// lights blink, then remain off for the rest of this scene session.
/// </summary>
[RequireComponent(typeof(Collider))]
public class GeneratorLightsOffTrigger : MonoBehaviour
{
    [Header("Generator Lights")]
    [Tooltip("Drag every generator Light component here.")]
    public Light[] generatorLights;
    public bool turnLightsOnAtSceneStart = true;

    [Header("Blink")]
    [Min(1)] public int blinkCount = 3;
    [Min(0.02f)] public float blinkInterval = 0.12f;
    [Min(0f)] public float finalOffDelay = 0.15f;

    [Header("Debug")]
    public bool logTrigger = true;

    private bool hasTriggered;

    private void Reset()
    {
        Collider trigger = GetComponent<Collider>();
        trigger.isTrigger = true;
    }

    private void Start()
    {
        if (turnLightsOnAtSceneStart)
            SetAllLights(true);
    }

    private void OnTriggerEnter(Collider other)
    {
        if (hasTriggered || !IsPlayer(other))
            return;

        hasTriggered = true;
        StartCoroutine(BlinkThenTurnOff());
    }

    private IEnumerator BlinkThenTurnOff()
    {
        for (int i = 0; i < blinkCount; i++)
        {
            SetAllLights(false);
            yield return new WaitForSeconds(blinkInterval);
            SetAllLights(true);
            yield return new WaitForSeconds(blinkInterval);
        }

        if (finalOffDelay > 0f)
            yield return new WaitForSeconds(finalOffDelay);

        SetAllLights(false);
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