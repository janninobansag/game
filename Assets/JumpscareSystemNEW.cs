using System.Collections;
using UnityEngine;

public class JumpscareSystemNEW : MonoBehaviour
{
    public static JumpscareSystemNEW Instance;

    [Header("Jumpscare Settings")]
    public float jumpscareDuration = 1.5f;
    public float lookSpeed = 5f;

    [Header("Breathing Settings")]
    public float breathingDuration = 3f;
    public float breathingIntensity = 0.05f;
    public AudioClip breathingSound;
    [Range(0f, 1f)]
    public float breathingVolume = 0.7f;
    public bool startBreathingDuringJumpscare = true;  // ← NEW

    [Header("Monster Teleport")]
    public float teleportRadius = 20f;
    public LayerMask teleportLayerMask = -1;

    private bool isJumpscaring = false;
    private Transform playerCamera;
    private Transform playerTransform;
    private PlayerController playerController;
    private CameraHeadBob headBob;
    private Vector3 originalCameraPos;
    private Quaternion originalCameraRot;
    private Vector3 targetLookDirection;
    private float breathingTimer = 0f;
    private bool isBreathing = false;
    private float originalFOV;
    private Camera cam;
    private AudioSource breathingAudioSource;

    void Awake()
    {
        if (Instance == null)
            Instance = this;
        else
            Destroy(gameObject);
    }

    void Start()
    {
        playerCamera = Camera.main.transform;
        if (playerCamera != null)
        {
            cam = playerCamera.GetComponent<Camera>();
            if (cam != null)
                originalFOV = cam.fieldOfView;
        }

        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player != null)
        {
            playerTransform = player.transform;
            playerController = player.GetComponent<PlayerController>();
            headBob = player.GetComponentInChildren<CameraHeadBob>();
        }

        breathingAudioSource = gameObject.AddComponent<AudioSource>();
        breathingAudioSource.spatialBlend = 0f;
        breathingAudioSource.playOnAwake = false;
        breathingAudioSource.loop = true;
        breathingAudioSource.volume = breathingVolume;
    }

    void Update()
    {
        if (isBreathing)
        {
            breathingTimer -= Time.deltaTime;
            
            // Camera shake / breathing effect
            if (headBob != null)
            {
                float breath = Mathf.Sin(Time.time * 2f) * breathingIntensity;
                headBob.externalShakeAmount = Mathf.Abs(breath) * 0.5f;
            }

            if (breathingTimer <= 1f && breathingAudioSource != null && breathingAudioSource.isPlaying)
            {
                breathingAudioSource.volume = Mathf.Lerp(0f, breathingVolume, breathingTimer / 1f);
            }

            if (breathingTimer <= 0f)
            {
                isBreathing = false;
                if (headBob != null)
                    headBob.externalShakeAmount = 0f;
                
                if (breathingAudioSource != null)
                {
                    breathingAudioSource.Stop();
                    breathingAudioSource.volume = breathingVolume;
                }
                
            }
        }
    }

    public void TriggerJumpscare(GameObject monster)
    {
        if (isJumpscaring) return;
        StartCoroutine(JumpscareSequence(monster));
    }

    IEnumerator JumpscareSequence(GameObject monster)
    {
        isJumpscaring = true;

        // Disable player control
        if (playerController != null)
            playerController.enabled = false;

        // Get monster's face position
        Transform monsterTransform = monster.transform;
        Vector3 monsterFacePos = monsterTransform.position + Vector3.up * 1.5f;

        // Store original camera rotation
        originalCameraRot = playerCamera.rotation;

        // ── SMOOTHLY LOOK AT MONSTER ──
        float elapsed = 0f;
        float lookDuration = 0.3f;

        while (elapsed < lookDuration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / lookDuration;

            Vector3 directionToMonster = (monsterFacePos - playerCamera.position).normalized;
            Quaternion targetRot = Quaternion.LookRotation(directionToMonster);

            playerCamera.rotation = Quaternion.Slerp(originalCameraRot, targetRot, t);
            yield return null;
        }

        // Final look at monster
        Vector3 finalDir = (monsterFacePos - playerCamera.position).normalized;
        playerCamera.rotation = Quaternion.LookRotation(finalDir);

        // ── PLAY MONSTER JUMPSCARE ANIMATION ──
        Animator monsterAnimator = monster.GetComponent<Animator>();
        if (monsterAnimator != null)
        {
            monsterAnimator.SetTrigger("jumpscare");
        }

        // ── PLAY JUMPSCARE SOUND ──
        AudioSource monsterAudio = monster.GetComponent<AudioSource>();
        if (monsterAudio != null && monsterAudio.clip != null)
        {
            monsterAudio.PlayOneShot(monsterAudio.clip, 0.8f);
        }

        // ── START BREATHING DURING JUMPSCARE (if enabled) ──
        if (startBreathingDuringJumpscare)
        {
            StartBreathing();
        }

        // ── HOLD JUMPSCARE ──
        yield return new WaitForSeconds(jumpscareDuration);

        // ── TELEPORT MONSTER ──
        Vector3 randomPos = GetRandomTeleportPosition();
        monsterTransform.position = randomPos;

        // ── RESET CAMERA ──
        elapsed = 0f;
        float resetDuration = 0.5f;
        Quaternion startRot = playerCamera.rotation;
        Quaternion endRot = originalCameraRot;

        while (elapsed < resetDuration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / resetDuration;
            playerCamera.rotation = Quaternion.Slerp(startRot, endRot, t);
            yield return null;
        }
        playerCamera.rotation = endRot;

        // ── RE-ENABLE PLAYER CONTROL ──
        if (playerController != null)
            playerController.enabled = true;

        // ── START BREATHING AFTER JUMPSCARE (if not already started) ──
        if (!startBreathingDuringJumpscare)
        {
            StartBreathing();
        }

        isJumpscaring = false;
    }

    Vector3 GetRandomTeleportPosition()
    {
        Vector3 randomDir = Random.insideUnitSphere * teleportRadius;
        randomDir.y = 0f;
        Vector3 targetPos = playerTransform.position + randomDir;

        UnityEngine.AI.NavMeshHit hit;
        if (UnityEngine.AI.NavMesh.SamplePosition(targetPos, out hit, teleportRadius, teleportLayerMask))
        {
            return hit.position;
        }

        return targetPos;
    }

    void StartBreathing()
    {
        isBreathing = true;
        breathingTimer = breathingDuration;
        
        if (breathingSound != null && breathingAudioSource != null)
        {
            breathingAudioSource.clip = breathingSound;
            breathingAudioSource.volume = breathingVolume;
            breathingAudioSource.Play();
        }
        
    }

    public void ResetJumpscare()
    {
        isJumpscaring = false;
        isBreathing = false;
        if (headBob != null)
            headBob.externalShakeAmount = 0f;
        
        if (breathingAudioSource != null && breathingAudioSource.isPlaying)
        {
            breathingAudioSource.Stop();
        }
    }
}