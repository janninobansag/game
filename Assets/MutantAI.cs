using System.Collections;
using UnityEngine;
using UnityEngine.AI;

public class MutantAI : MonoBehaviour
{
    [Header("Detection")]
    public float detectionRange = 15f;
    public float attackRange = 1.8f;

    [Header("Movement")]
    public float roamSpeed = 1.5f;
    public float chaseSpeed = 5f;
    public float rotationSpeed = 6f;

    [Header("Roaming")]
    public float roamRadius = 20f;
    public float roamWaitMin = 2f;
    public float roamWaitMax = 5f;

    [Header("Attack")]
    public float attackDamage = 30f;
    public float attackCooldown = 1.5f;

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

    void UpdateRoam(float dist, bool playerVisible)
    {
        agent.speed = roamSpeed;

        // Switch to chase if player in range AND visible (not in bush)
        if (dist <= detectionRange && playerVisible)
        {
            StopChaseSound();
            SetAnimation(false, true);
            currentState = State.Chase;
            return;
        }

        // Walking animation
        SetAnimation(true, false);
        StopChaseSound();

        if (isWaiting)
        {
            SetAnimation(false, false);
            roamTimer += Time.deltaTime;
            if (roamTimer >= roamWaitTime)
            {
                isWaiting = false;
                roamTimer = 0f;
                SetNewRoamDestination();
            }
            return;
        }

        if (!agent.pathPending && agent.remainingDistance < 0.5f)
        {
            isWaiting = true;
            roamTimer = 0f;
            roamWaitTime = Random.Range(roamWaitMin, roamWaitMax);
        }
    }

    void SetNewRoamDestination()
    {
        for (int i = 0; i < 10; i++)
        {
            Vector3 randomDir = Random.insideUnitSphere * roamRadius;
            randomDir += startPosition;

            NavMeshHit hit;
            if (NavMesh.SamplePosition(randomDir, out hit,
                roamRadius, NavMesh.AllAreas))
            {
                agent.SetDestination(hit.position);
                return;
            }
        }
    }

    void UpdateChase(float dist, bool playerVisible)
    {
        agent.speed = chaseSpeed;
        SetAnimation(false, true);
        PlayChaseSound();

        // ── NEW: Lose player if in bush or too far ──
        if (!playerVisible || dist > detectionRange + 5f)
        {
            StopChaseSound();
            currentState = State.Roam;
            SetNewRoamDestination();
            return;
        }

        if (agent.isOnNavMesh)
            agent.SetDestination(player.position);

        FacePlayer();

        if (dist <= attackRange)
            currentState = State.Attack;
    }

    void UpdateAttack(float dist)
    {
        if (agent.isOnNavMesh)
            agent.SetDestination(transform.position);

        SetAnimation(false, false);
        StopChaseSound();
        FacePlayer();

        if (dist > attackRange + 0.5f)
        {
            currentState = State.Chase;
            return;
        }

        if (attackTimer <= 0f)
        {
            Attack();
            attackTimer = attackCooldown;
        }
    }

    void FacePlayer()
    {
        Vector3 dir = (player.position - transform.position).normalized;
        dir.y = 0f;
        if (dir != Vector3.zero)
        {
            Quaternion look = Quaternion.LookRotation(dir);
            transform.rotation = Quaternion.Slerp(
                transform.rotation, look,
                Time.deltaTime * rotationSpeed);
        }
    }

    void Attack()
    {
        PlaySound(attackSound);

        if (JumpscareSystem.Instance != null)
            JumpscareSystem.Instance.TriggerJumpscare(gameObject);

        PlayerHealth ph = player.GetComponent<PlayerHealth>();
        if (ph != null) ph.TakeDamageForEnemyCatch(attackDamage);
    }

    void SetAnimation(bool walking, bool running)
    {
        if (animator == null) return;
        animator.SetBool("isWalking", walking);
        animator.SetBool("isRunning", running);
    }

    void PlayChaseSound()
    {
        if (isPlayingChaseSound || chaseSound == null) return;
        audioSource.clip = chaseSound;
        audioSource.volume = chaseVolume;
        audioSource.loop = true;
        audioSource.Play();
        isPlayingChaseSound = true;
    }

    void StopChaseSound()
    {
        if (!isPlayingChaseSound) return;
        audioSource.Stop();
        isPlayingChaseSound = false;
    }

    void PlaySound(AudioClip clip)
    {
        if (clip == null) return;
        audioSource.PlayOneShot(clip);
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