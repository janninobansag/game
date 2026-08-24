using System.Collections;
using UnityEngine;

public class AudioTrigger : MonoBehaviour
{
    [Header("Audio Settings")]
    public AudioClip audioClip;
    public float volume = 1f;
    public bool loop = false;
    public bool playOnce = true;

    [Header("Trigger Settings")]
    public string playerTag = "Player";

    [Header("Fade Settings")]
    public bool fadeIn = true;
    public float fadeInDuration = 1.5f;
    public bool fadeOut = true;
    public float fadeOutDuration = 1.5f;

    private AudioSource audioSource;
    private bool hasPlayed = false;
    private bool isPlaying = false;

   void Start()
{
    audioSource = gameObject.AddComponent<AudioSource>();
    audioSource.clip = audioClip;
    audioSource.volume = 0f;
    audioSource.loop = loop;
    audioSource.playOnAwake = false;
    audioSource.priority = 0;
    audioSource.spatialBlend = 1f;  // ← 1 = full 3D positional sound
    audioSource.minDistance = 500f;   // ← full volume within this range
    audioSource.maxDistance = 20f;  // ← completely silent beyond this
    audioSource.rolloffMode = AudioRolloffMode.Linear;

    Collider col = GetComponent<Collider>();
    if (col != null)
        col.isTrigger = true;
}
    void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag(playerTag)) return;
        if (playOnce && hasPlayed) return;

        hasPlayed = true;
        isPlaying = true;

        StopAllCoroutines();
        StartCoroutine(PlayAudio());
    }

    public void TriggerAudio()
    {
        if (playOnce && hasPlayed) return;

        hasPlayed = true;
        StopAllCoroutines();
        StartCoroutine(PlayAudio());
    }

    void OnTriggerExit(Collider other)
    {
        if (!other.CompareTag(playerTag)) return;
        if (!loop) return; // only fade out looping audio on exit

        StopAllCoroutines();
        StartCoroutine(FadeOutAudio());
    }

    IEnumerator PlayAudio()
    {
        audioSource.clip = audioClip;
        audioSource.volume = fadeIn ? 0f : volume;
        audioSource.Play();

        if (fadeIn)
        {
            float elapsed = 0f;
            while (elapsed < fadeInDuration)
            {
                elapsed += Time.deltaTime;
                audioSource.volume = Mathf.Lerp(0f, volume,
                    elapsed / fadeInDuration);
                yield return null;
            }
        }

        audioSource.volume = volume;
    }

    IEnumerator FadeOutAudio()
    {
        if (!fadeOut)
        {
            audioSource.Stop();
            yield break;
        }

        float startVolume = audioSource.volume;
        float elapsed = 0f;

        while (elapsed < fadeOutDuration)
        {
            elapsed += Time.deltaTime;
            audioSource.volume = Mathf.Lerp(startVolume, 0f,
                elapsed / fadeOutDuration);
            yield return null;
        }

        audioSource.Stop();
        audioSource.volume = 0f;
        isPlaying = false;
    }

    // Call this to manually reset trigger
    public void ResetTrigger()
    {
        hasPlayed = false;
        audioSource.Stop();
        audioSource.volume = 0f;
    }
}