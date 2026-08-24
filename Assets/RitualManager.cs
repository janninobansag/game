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
    public float delayBeforeLightsOff = 2f;
    public float lightFadeSpeed = 1.5f;

    private bool ritualComplete = false;
    private bool checkingRitual = false;
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

        // Play whisper
        if (whisperSound != null)
            audioSource.PlayOneShot(whisperSound);

        // Wait before lights go out
        yield return new WaitForSeconds(delayBeforeLightsOff);

        // Play ritual sound
        if (ritualCompleteSound != null)
            audioSource.PlayOneShot(ritualCompleteSound);

        // Fade out all candle lights
        StartCoroutine(FadeOutAllLights());

        // Trigger objective
        if (objectiveTrigger != null)
            objectiveTrigger.TriggerObjective();

        // Wait then spawn mutant
        yield return new WaitForSeconds(delayBeforeSpawn);

        SpawnMutant();

        ritualComplete = true;
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
        if (candleLights == null || candleLights.Length == 0)
        {
            CandleItem[] candles = FindObjectsOfType<CandleItem>();
            candleLights = new Light[candles.Length];
            for (int i = 0; i < candles.Length; i++)
                candleLights[i] = candles[i].GetComponentInChildren<Light>();
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