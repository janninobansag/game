using System.Collections;
using UnityEngine;

public class RitualManager : MonoBehaviour
{
    [Header("Ritual Holders")]
    public CandleHolder candleHolder1;
    public CandleHolder candleHolder2;
    public TableHolder bibleHolder;   // ← ADD THIS (for Bible)
    public TableHolder crossHolder;   // ← ADD THIS (for Cross)

    [Header("Candle Lights to turn off")]
    public Light[] candleLights;

    [Header("Mutant Spawn Settings")]
    public GameObject mutantPrefab;
    public Transform mutantSpawnPoint;
    public float delayBeforeSpawn = 3f;

    [Header("Ritual Effects")]
    public AudioClip ritualCompleteSound;
    public AudioClip whisperSound;
    public ObjectiveTrigger objectiveTrigger;

    [Header("Settings")]
    [Min(0f)] public float finalRitualDelay = 3f;
    public float lightFadeSpeed = 1.5f;

    private bool ritualComplete = false;
    private bool checkingRitual = false;
    private bool candleLightsTurnedOff = false;
    private AudioSource audioSource;

    void Start()
    {
        audioSource = gameObject.AddComponent<AudioSource>();
        audioSource.spatialBlend = 0f;
        audioSource.playOnAwake = false;
    }

    void Update()
    {
        if (ritualComplete) return;

        if (AllItemsPlaced() && !checkingRitual)
        {
            checkingRitual = true;
            StartCoroutine(RitualSequence());
        }
    }

    bool AllItemsPlaced()
    {
        bool candle1 = candleHolder1 != null && candleHolder1.HasCandle();
        bool candle2 = candleHolder2 != null && candleHolder2.HasCandle();
        bool bible = bibleHolder != null && bibleHolder.HasItem();
        bool cross = crossHolder != null && crossHolder.HasItem();

 
        return candle1 && candle2 && bible && cross;
    }

    IEnumerator RitualSequence()
    {
        // Wait after the final ritual item is placed before the scare begins.
        if (finalRitualDelay > 0f)
            yield return new WaitForSeconds(finalRitualDelay);

        // The sounds, all candle lights fading out, and VAREN spawn begin together.
        if (whisperSound != null)
            audioSource.PlayOneShot(whisperSound);
        if (ritualCompleteSound != null)
            audioSource.PlayOneShot(ritualCompleteSound);

        if (!candleLightsTurnedOff)
        {
            candleLightsTurnedOff = true;
            StartCoroutine(FadeOutAllLights());
        }

        SpawnMutant();

        if (objectiveTrigger != null)
            objectiveTrigger.TriggerObjective();

        ritualComplete = true;
        yield break;
    }
    void SpawnMutant()
    {
        if (mutantPrefab == null)
        {
            return;
        }

        if (mutantSpawnPoint == null)
        {
            return;
        }

        GameObject mutant = Instantiate(mutantPrefab,
            mutantSpawnPoint.position,
            mutantSpawnPoint.rotation);

    }

    IEnumerator FadeOutAllLights()
    {
        // Keep manually assigned ritual lights, then also include every CandleItem light in the scene.
        var lightsToFade = new System.Collections.Generic.List<Light>();
        if (candleLights != null)
        {
            foreach (Light light in candleLights)
                if (light != null && !lightsToFade.Contains(light))
                    lightsToFade.Add(light);
        }

        foreach (CandleItem candle in FindObjectsOfType<CandleItem>())
        {
            Light candleLight = candle.GetComponentInChildren<Light>();
            if (candleLight != null && !lightsToFade.Contains(candleLight))
                lightsToFade.Add(candleLight);
        }

        candleLights = lightsToFade.ToArray();
        if (candleLights.Length == 0)
        {
            Debug.LogWarning("[RitualManager] No candle lights are assigned. Add all four candle Light components to Candle Lights in the Inspector.");
            yield break;
        }
        float[] originalIntensities = new float[candleLights.Length];
        for (int i = 0; i < candleLights.Length; i++)
            if (candleLights[i] != null)
                originalIntensities[i] = candleLights[i].intensity;

        float elapsed = 0f;
        while (elapsed < 1f)
        {
            elapsed += Time.deltaTime * lightFadeSpeed;
            for (int i = 0; i < candleLights.Length; i++)
                if (candleLights[i] != null)
                    candleLights[i].intensity = Mathf.Lerp(
                        originalIntensities[i], 0f, elapsed);
            yield return null;
        }

        foreach (Light l in candleLights)
            if (l != null)
            {
                l.intensity = 0f;
                l.enabled = false;
            }
    }

    public bool IsRitualComplete() => ritualComplete;
}