// PURPOSE: Quickly moves the White Lady through configured teleport points when the player enters this trigger.
using System.Collections;
using UnityEngine;
using UnityEngine.AI;

[RequireComponent(typeof(Collider))]
public class WhiteLadyTeleportTrigger : MonoBehaviour
{
    [Header("White Lady")]
    [Tooltip("Drag the White Lady root GameObject here. A NavMeshAgent on this object is used when available.")]
    public Transform whiteLady;

    [Header("Teleport Points")]
    [Tooltip("Drag teleport 1, teleport 2, and teleport 3 here in the order they should appear.")]
    public Transform[] teleportPoints;
    [Tooltip("Time between each fast teleport.")]
    [Min(0.01f)] public float teleportInterval = 0.12f;
    [Tooltip("Runs only once per New Game; its completed state is saved.")]
    public bool triggerOnce = true;
    [Tooltip("Optional stable ID for save files. Leave empty to use the scene hierarchy path.")]
    public string saveId;

    [Header("Teleport Sound Effects")]
    [Tooltip("Optional. Drag an AudioSource here. If empty, the script looks for one on the White Lady or this trigger.")]
    public AudioSource teleportAudioSource;
    [Tooltip("Played at every teleport point except the final point.")]
    public AudioClip teleportSound;
    [Range(0f, 1f)] public float teleportSoundVolume = 1f;

    [Header("Final Teleport Sound Effects")]
    [Tooltip("All clips in this list play together at the final teleport point.")]
    public AudioClip[] finalTeleportSounds;
    [Range(0f, 1f)] public float finalTeleportSoundVolume = 1f;
    [Tooltip("Keeps final sounds audible after the White Lady disappears, regardless of player distance.")]
    public bool finalSoundsAre2D = true;

    [Header("After Final Teleport")]
    [Tooltip("Hides the White lady jumpscare GameObject after the final teleport.")]
    public bool hideWhiteLadyAfterSequence = true;
    [Min(0f)] public float hideDelay = 0.12f;

    private bool hasTriggered;
    private bool retryAfterEncounter;
    private bool isTeleporting;
    private NavMeshAgent whiteLadyAgent;

    public bool HasTriggered => hasTriggered;
    public string SaveStateKey => SaveSystem.GetOneShotStateKey(this, "WhiteLadyTeleport", saveId);

    public void RestoreTriggeredState(bool wasTriggered)
    {
        if (!triggerOnce)
            return;

        hasTriggered = wasTriggered;
        if (wasTriggered)
        {
            retryAfterEncounter = false;
            StopAllCoroutines();
            isTeleporting = false;
            if (hideWhiteLadyAfterSequence && whiteLady != null)
                whiteLady.gameObject.SetActive(false);
        }
    }

    private void Awake()
    {
        Collider triggerCollider = GetComponent<Collider>();
        triggerCollider.isTrigger = true;

        if (whiteLady != null)
            whiteLadyAgent = whiteLady.GetComponent<NavMeshAgent>();

        if (teleportAudioSource == null && whiteLady != null)
            teleportAudioSource = whiteLady.GetComponentInChildren<AudioSource>();
        if (teleportAudioSource == null)
            teleportAudioSource = GetComponent<AudioSource>();
    }

    private void OnTriggerEnter(Collider other)
    {
        TryStartTeleport(other);
    }

    private void OnTriggerStay(Collider other)
    {
        if (retryAfterEncounter)
            TryStartTeleport(other);
    }

    private void OnTriggerExit(Collider other)
    {
        if (IsPlayer(other))
            retryAfterEncounter = false;
    }

    private void TryStartTeleport(Collider other)
    {
        if (isTeleporting || (triggerOnce && hasTriggered) || !IsPlayer(other) ||
            whiteLady == null || teleportPoints == null || teleportPoints.Length == 0)
            return;

        if (PlayerPrefs.GetInt("ShouldLoadSave", 0) == 1)
        {
            retryAfterEncounter = true;
            return;
        }

        if (AIEncounterGate.IsActive)
        {
            retryAfterEncounter = true;
            return;
        }

        retryAfterEncounter = false;
        isTeleporting = true;
        if (triggerOnce)
        {
            hasTriggered = true;
            SaveSystem.Instance?.MarkOneShotEventCompleted(SaveStateKey);
        }
        StartCoroutine(TeleportSequence());
    }

    private IEnumerator TeleportSequence()
    {
        for (int index = 0; index < teleportPoints.Length; index++)
        {
            while (AIEncounterGate.IsActive)
                yield return null;

            Transform teleportPoint = teleportPoints[index];
            if (teleportPoint == null)
                continue;

            TeleportWhiteLady(teleportPoint);
            bool isFinalPoint = index == teleportPoints.Length - 1;
            if (isFinalPoint)
                PlayFinalTeleportSounds(teleportPoint.position);
            else
                PlayTeleportSound();
            yield return new WaitForSeconds(teleportInterval);
        }

        if (hideWhiteLadyAfterSequence && whiteLady != null)
        {
            if (hideDelay > 0f)
                yield return new WaitForSeconds(hideDelay);

            while (AIEncounterGate.IsActive)
                yield return null;

            whiteLady.gameObject.SetActive(false);
        }

        isTeleporting = false;
    }

    private void TeleportWhiteLady(Transform teleportPoint)
    {
        if (whiteLadyAgent == null && whiteLady != null)
            whiteLadyAgent = whiteLady.GetComponent<NavMeshAgent>();

        Vector3 destination = teleportPoint.position;
        if (whiteLadyAgent != null && whiteLadyAgent.enabled && whiteLadyAgent.isOnNavMesh)
        {
            if (NavMesh.SamplePosition(destination, out NavMeshHit hit, 3f, whiteLadyAgent.areaMask))
                destination = hit.position;

            whiteLadyAgent.Warp(destination);
            whiteLadyAgent.ResetPath();
        }
        else
        {
            whiteLady.position = destination;
        }

        whiteLady.rotation = teleportPoint.rotation;
    }

    private void PlayTeleportSound()
    {
        if (teleportAudioSource == null || teleportSound == null)
            return;

        teleportAudioSource.PlayOneShot(teleportSound, teleportSoundVolume);
    }

    private void PlayFinalTeleportSounds(Vector3 soundPosition)
    {
        if (finalTeleportSounds == null)
            return;

        foreach (AudioClip clip in finalTeleportSounds)
        {
            if (clip == null)
                continue;

            // This source is not a child of the White Lady, so hiding the model
            // after teleport 3 cannot cut off the final sound effect.
            GameObject soundObject = new GameObject("White Lady Final Teleport Sound");
            soundObject.transform.position = soundPosition;
            AudioSource source = soundObject.AddComponent<AudioSource>();
            source.spatialBlend = finalSoundsAre2D ? 0f : 1f;
            source.playOnAwake = false;
            source.clip = clip;
            source.volume = finalTeleportSoundVolume;
            source.Play();
            Destroy(soundObject, clip.length + 0.1f);
        }
    }

    private bool IsPlayer(Collider other)
    {
        PlayerController playerController = other.GetComponentInParent<PlayerController>();
        return playerController != null || other.CompareTag("Player");
    }
}
