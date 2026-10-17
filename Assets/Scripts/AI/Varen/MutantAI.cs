// PURPOSE: Controls mutant roaming, player detection, chasing, attacks, audio, and hit effects.
using System.Collections;
using UnityEngine;
using UnityEngine.AI;

public partial class MutantAI : MonoBehaviour
{
    // Distances used to spot and attack the player.
    [Header("Detection")]
    public float detectionRange = 15f;
    public float attackRange = 1.8f;

    // Speeds used while roaming and chasing.
    [Header("Movement")]
    public float roamSpeed = 1.5f;
    public float chaseSpeed = 5f;
    public float rotationSpeed = 6f;

    // Random patrol area and pause timing when the mutant is not chasing.
    [Header("Roaming")]
    public float roamRadius = 20f;
    public float roamWaitMin = 2f;
    public float roamWaitMax = 5f;

    // Damage and cooldown settings for close-range attacks.
    [Header("Attack")]
    public float attackDamage = 30f;
    public float attackCooldown = 1.5f;

    [Header("Catch Jumpscare")]
    [Tooltip("How far from the player Varen appears after catching them.")]
    [Min(0.1f)] public float jumpscareFaceDistance = 1.35f;
    [Tooltip("Height on Varen the player camera looks at during the catch.")]
    [Min(0f)] public float jumpscareCameraFocusHeight = 1.8f;

    [Header("Audio")]
    public AudioClip chaseSound;
    [Range(0f, 1f)] public float chaseVolume = 100f;
    public float maxAudioDistance = 50f;
    public AudioClip attackSound;

    [Header("Proximity Effects")]
    public float effectDistance = 8f;
    public float maxShakeIntensity = 0.3f;

    [Header("Bush Detection")]
    public string bushTag = "Bush";      // ← NEW: Tag for bushes
    public bool checkBush = true;        // ← NEW: Toggle bush detection

    private enum State { Roam, Chase, Attack }
    private State currentState = State.Roam;

    private NavMeshAgent agent;
    private Transform player;
    private AudioSource audioSource;
    private Animator animator;
    private bool isPlayingChaseSound = false;
    private float attackTimer = 0f;
    private bool isReady = false;
    private CameraHeadBob playerCameraBob;
    private readonly AIEncounterPause encounterPause = new AIEncounterPause();

    // Roaming
    private float roamTimer = 0f;
    private float roamWaitTime = 0f;
    private bool isWaiting = false;
    public Vector3 startPosition;

    void Start()
    {
        audioSource = gameObject.AddComponent<AudioSource>();
        audioSource.spatialBlend = 1f;
        audioSource.maxDistance = maxAudioDistance;
        audioSource.playOnAwake = false;

        agent = GetComponent<NavMeshAgent>();
        animator = GetComponentInChildren<Animator>();

        GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
        if (playerObj != null)
        {
            player = playerObj.transform;
            playerCameraBob = playerObj.GetComponentInChildren<CameraHeadBob>();
        }

        startPosition = transform.position;

        StartCoroutine(InitializeOnNavMesh());
    }

    IEnumerator InitializeOnNavMesh()
    {
        yield return new WaitForSeconds(0.2f);
        while (AIEncounterGate.IsBlocked(this))
            yield return null;

        NavMeshHit hit;
        if (NavMesh.SamplePosition(transform.position,
            out hit, 25f, NavMesh.AllAreas))
        {
            agent.Warp(hit.position);
            agent.enabled = true;
            agent.speed = roamSpeed;
            isReady = true;

            SetNewRoamDestination();
        }
        else
        {
            StartCoroutine(RetryNavMesh());
        }
    }

    IEnumerator RetryNavMesh()
    {
        int attempts = 0;
        while (!isReady && attempts < 10)
        {
            yield return new WaitForSeconds(1f);
            while (AIEncounterGate.IsBlocked(this))
                yield return null;
            attempts++;

            NavMeshHit hit;
            if (NavMesh.SamplePosition(transform.position,
                out hit, 25f, NavMesh.AllAreas))
            {
                agent.Warp(hit.position);
                agent.enabled = true;
                agent.speed = roamSpeed;
                isReady = true;
                SetNewRoamDestination();
                yield break;
            }
        }
    }

    void Update()
    {
        if (encounterPause.Update(this, agent, animator, audioSource))
        {
            if (playerCameraBob != null)
                playerCameraBob.externalShakeAmount = 0f;
            return;
        }

        if (!isReady || player == null) return;
        if (!agent.isOnNavMesh) return;

        attackTimer -= Time.deltaTime;

        float dist = Vector3.Distance(
            transform.position, player.position);

        // Apply proximity shake
        if (playerCameraBob != null)
        {
            if (dist <= effectDistance)
            {
                float strength = 1f - (dist / effectDistance);
                playerCameraBob.externalShakeAmount = strength * maxShakeIntensity;
            }
            else
            {
                playerCameraBob.externalShakeAmount = 0f;
            }
        }

        // ── NEW: Check if player is in bush ──
        bool isInBush = IsPlayerInBush();

        // ── NEW: If player is in bush, don't chase ──
        bool playerVisible = !isInBush;

        switch (currentState)
        {
            case State.Roam:   UpdateRoam(dist, playerVisible); break;
            case State.Chase:  UpdateChase(dist, playerVisible); break;
            case State.Attack: UpdateAttack(dist); break;
        }
    }

    private void OnDisable()
    {
        encounterPause.Restore(agent, animator, audioSource);
    }

    // ── NEW: Check if player is inside a bush ──
    bool IsPlayerInBush()
    {
        if (!checkBush) return false;
        if (player == null) return false;

        Collider[] hitColliders = Physics.OverlapSphere(player.position, 0.5f);

        foreach (Collider collider in hitColliders)
        {
            if (collider.CompareTag(bushTag))
            {
                return true;
            }
        }

        return false;
    }

    public void ResetToRoam()
    {
        StopChaseSound();

        if (agent != null) agent.enabled = false;
        transform.position = startPosition;
        if (agent != null)
        {
            agent.enabled = true;
            agent.velocity = Vector3.zero;
        }

        currentState = State.Roam;
        isWaiting = false;
        roamTimer = 0f;
        attackTimer = 0f;
        enabled = true;

        UnityEngine.AI.NavMeshAgent nav =
            GetComponent<UnityEngine.AI.NavMeshAgent>();
        if (nav != null) nav.enabled = true;

        if (animator != null)
        {
            animator.SetBool("isWalking", false);
            animator.SetBool("isRunning", false);
        }

        if (agent != null && agent.isOnNavMesh)
            SetNewRoamDestination();
    }

    // ── NEW: Method to lose player (called when hiding) ──
    public void LosePlayer()
    {
        if (currentState == State.Chase || currentState == State.Attack)
        {
            currentState = State.Roam;
            SetNewRoamDestination();
            StopChaseSound();
   
        }
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, detectionRange);
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, attackRange);
        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(startPosition, roamRadius);
    }
}
