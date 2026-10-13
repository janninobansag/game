// PURPOSE: Controls the White Lady patrol, detection, chase, Q&A encounter, damage, and jumpscare behavior.
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;

public partial class MonsterAI_New : MonoBehaviour
{
    // White Lady speeds, patrol area, and idle timing.
    [Header("Movement Settings")]
    public float walkSpeed = 2f;
    public float chaseSpeed = 5f;
    public float patrolRadius = 15f;
    public float idleTimeMin = 2f;
    public float idleTimeMax = 6f;

    // Range and optional field-of-view settings used to notice the player.
    [Header("Detection Settings")]
    public float detectionRange = 15f;
    public float attackRange = 2f;
    public bool useFieldOfView = false;
    public float fieldOfView = 60f;

    [Header("Obstacle Detection")]
    public bool checkObstacles = true;
    public LayerMask obstacleLayer = -1;

    [Header("Painted Tree Avoidance")]
    [Tooltip("Makes White Lady steer around trees painted on Unity Terrain, even when those trees are not baked into the NavMesh.")]
    public bool avoidPaintedTrees = true;
    [Tooltip("How far ahead White Lady checks for a painted tree that blocks her route.")]
    [Min(1f)] public float paintedTreeLookAheadDistance = 8f;
    [Tooltip("Extra space White Lady leaves around the trunk while moving past a painted tree.")]
    [Min(0.1f)] public float paintedTreeClearance = 1.5f;
    [Tooltip("How often White Lady updates her tree-aware chase destination.")]
    [Min(0.02f)] public float paintedTreeDestinationRefreshRate = 0.12f;

    [Header("Bush Detection")]
    public string bushTag = "Bush";
    public bool checkBush = true;

    [Header("Jumpscare Settings")]
    public float jumpscareCooldown = 2f;
    public float jumpscareDamage = 25f;

    [Header("References")]
    public Transform player;
    public Animator animator;
    public AudioSource audioSource;
    public AudioClip chaseSound;
    public AudioClip jumpscareSound;

    public bool showGizmos = true;

    private NavMeshAgent agent;
    private Vector3 startPosition;
    private float jumpscareTimer = 0f;
    private float idleTimer = 0f;
    private bool isChasing = false;
    private bool isIdle = false;
    private bool playerInSight = false;

    private bool isJumpscaring = false;
    private bool isWaitingForQnA = false;
    private float jumpscareAnimTimer = 0f;
    private Vector3 jumpscareLockPosition;
    private bool hasTriggeredJumpscare = false;

    // ── Q&A State ──
    private enum State { Patrol, Chase, Jumpscare, Idle, QnA }
    private State currentState = State.Patrol;

    void Start()
    {
        agent = GetComponent<NavMeshAgent>();
        if (agent == null) agent = gameObject.AddComponent<NavMeshAgent>();

        if (player == null)
        {
            GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
            if (playerObj != null) player = playerObj.transform;
        }

        if (animator == null) animator = GetComponent<Animator>();
        if (audioSource == null) audioSource = GetComponent<AudioSource>();

        startPosition = transform.position;
        PopulateTeleportSpawnsIfNeeded();
        CachePaintedTrees();
        agent.speed = walkSpeed;
        agent.isStopped = false;

        if (agent.isOnNavMesh == false)
        {
            NavMeshHit hit;
            if (NavMesh.SamplePosition(transform.position, out hit, 10f, NavMesh.AllAreas))
            {
                agent.Warp(hit.position);
            }
        }

        if (player != null)
        {
            playerController = player.GetComponent<PlayerController>();
            characterController = player.GetComponent<CharacterController>();
            playerRigidbody = player.GetComponent<Rigidbody>();

            if (playerController != null)
            {
                wasPlayerControllerEnabled = playerController.enabled;
            }
        }

        pauseMenu = PauseMenu.Instance;
        if (pauseMenu != null)
        {
        }

        if (qnaPanel != null)
        {
            qnaCanvasGroup = qnaPanel.GetComponent<CanvasGroup>();
            if (qnaCanvasGroup == null)
                qnaCanvasGroup = qnaPanel.AddComponent<CanvasGroup>();
            WireQnAButtons();
            qnaPanel.SetActive(false);
        }
        else
        {
        }

        StartCoroutine(InitializeAndPatrol());
    }

    IEnumerator InitializeAndPatrol()
    {
        yield return new WaitForSeconds(0.3f);

        currentState = State.Patrol;
        isIdle = false;
        agent.isStopped = false;
        agent.speed = walkSpeed;

        SetNewPatrolTarget();

        if (animator != null)
        {
            animator.SetBool("walking", true);
            animator.SetBool("run", false);
            animator.ResetTrigger("jumpscare");
        }

        StartCoroutine(EnsurePatrolMovement());
    }

    IEnumerator EnsurePatrolMovement()
    {
        yield return new WaitForSeconds(0.5f);

        if (agent != null && agent.velocity.magnitude < 0.1f && !agent.pathPending)
        {
            SetNewPatrolTarget();

            if (animator != null)
            {
                animator.SetBool("walking", true);
                animator.SetBool("run", false);
            }
        }
    }

    void Update()
    {
        bool isPaused = pauseMenu != null && pauseMenu.isPaused;

        if (qnaPanel != null && isQnAActive)
        {
            if (isPaused && !qnaPanelHiddenForPause)
            {
                qnaPanel.SetActive(false);
                qnaPanelHiddenForPause = true;
            }
            else if (!isPaused && qnaPanelHiddenForPause)
            {
                qnaPanel.SetActive(true);
                qnaPanelHiddenForPause = false;
            }
        }

        if (player == null) return;

        if (jumpscareTimer > 0f) jumpscareTimer -= Time.deltaTime;

        if (isJumpscaring)
        {
            jumpscareAnimTimer -= Time.deltaTime;
            if (jumpscareAnimTimer <= 0f && !isWaitingForQnA)
            {
                isJumpscaring = false;
                agent.enabled = true;
                hasTriggeredJumpscare = false;

                if (animator != null)
                {
                    animator.ResetTrigger("jumpscare");
                }
            }
            return;
        }

        if (isQnAActive || currentState == State.QnA)
        {
            if (agent != null && agent.enabled && agent.isOnNavMesh)
                agent.isStopped = true;
            if (playerController != null && playerController.enabled)
            {
                playerController.enabled = false;
                isPlayerLocked = true;
            }
            return;
        }

        if (isQnAComplete && isPlayerLocked && !hasUnlockedAfterQnA)
        {
            UnlockPlayer();
            isQnAComplete = false;
            hasUnlockedAfterQnA = true;
        }

        if (isPlayerLocked && !isQnAActive)
        {
            if (playerController != null && playerController.enabled)
            {
                isPlayerLocked = false;
            }
            else if (!isQnAComplete)
            {
                UnlockPlayer();
            }
        }

        float distanceToPlayer = Vector3.Distance(transform.position, player.position);
        bool canSeePlayer = CanSeePlayer();

        bool isInBush = IsPlayerInBush();
        bool playerVisible = IsPlayerVisible() && !isInBush;

        if (playerVisible && distanceToPlayer <= detectionRange)
        {
            playerInSight = true;
            if (distanceToPlayer <= attackRange && !hasTriggeredJumpscare)
                currentState = State.Jumpscare;
            else
                currentState = State.Chase;
        }
        else
        {
            playerInSight = false;
            if (currentState != State.Jumpscare && currentState != State.Idle && currentState != State.QnA)
                currentState = State.Patrol;
        }

        switch (currentState)
        {
            case State.Patrol:
                Patrol();
                break;
            case State.Chase:
                Chase();
                break;
            case State.Jumpscare:
                Jumpscare();
                break;
            case State.Idle:
                Idle();
                break;
            case State.QnA:
                break;
        }

        UpdateAnimations();
        UpdateAudio();
    }

    bool CanSeePlayer()
    {
        if (player == null) return false;
        if (!useFieldOfView) return true;

        Vector3 directionToPlayer = (player.position - transform.position).normalized;
        float angle = Vector3.Angle(transform.forward, directionToPlayer);
        if (angle > fieldOfView) return false;
        return true;
    }

    bool IsPlayerInBush()
    {
        if (!checkBush) return false;
        if (player == null) return false;

        Collider[] hitColliders = Physics.OverlapSphere(player.position, 0.5f);
        foreach (Collider collider in hitColliders)
        {
            if (collider.CompareTag(bushTag))
                return true;
        }
        return false;
    }

    bool IsPlayerVisible()
    {
        if (player == null) return false;
        if (!checkObstacles) return true;

        Vector3 directionToPlayer = (player.position - transform.position).normalized;
        float distance = Vector3.Distance(transform.position, player.position);

        RaycastHit hit;
        Vector3 origin = transform.position + Vector3.up * 0.5f;

        if (Physics.Raycast(origin, directionToPlayer, out hit, distance, obstacleLayer))
        {
            if (!hit.transform.CompareTag("Player"))
                return false;
        }
        return true;
    }

    void Patrol()
    {
        isChasing = false;
        agent.speed = walkSpeed;
        agent.isStopped = false;

        if (!agent.hasPath || agent.remainingDistance < 0.5f)
        {
            if (!isIdle)
            {
                isIdle = true;
                idleTimer = Random.Range(idleTimeMin, idleTimeMax);
                currentState = State.Idle;
            }
        }
    }

    void Idle()
    {
        agent.isStopped = true;
        isChasing = false;

        idleTimer -= Time.deltaTime;
        if (idleTimer <= 0f)
        {
            isIdle = false;
            agent.isStopped = false;
            SetNewPatrolTarget();
            currentState = State.Patrol;
        }
    }

    void Chase()
    {
        isChasing = true;
        agent.speed = chaseSpeed;
        agent.isStopped = false;

        if (player != null)
        {
            if (Time.time >= nextPaintedTreeDestinationUpdate)
            {
                SetDestinationAvoidingPaintedTrees(player.position);
                nextPaintedTreeDestinationUpdate = Time.time + paintedTreeDestinationRefreshRate;
            }

            Vector3 direction = (player.position - transform.position).normalized;
            direction.y = 0f;
            if (direction != Vector3.zero)
            {
                Quaternion lookRotation = Quaternion.LookRotation(direction);
                transform.rotation = Quaternion.Slerp(transform.rotation, lookRotation, Time.deltaTime * 5f);
            }
        }
    }

    void Jumpscare()
    {
        if (isJumpscaring || hasTriggeredJumpscare) return;

        agent.isStopped = true;

        if (player != null)
        {
            Vector3 direction = (player.position - transform.position).normalized;
            direction.y = 0f;
            if (direction != Vector3.zero)
            {
                Quaternion lookRotation = Quaternion.LookRotation(direction);
                transform.rotation = Quaternion.Slerp(transform.rotation, lookRotation, Time.deltaTime * 5f);
            }

            float distance = Vector3.Distance(transform.position, player.position);
            if (distance > attackRange + 1f)
            {
                currentState = State.Chase;
                agent.isStopped = false;
                return;
            }

            if (jumpscareTimer <= 0f)
            {
                PerformJumpscare();
                jumpscareTimer = jumpscareCooldown;
            }
        }
    }

    void PerformJumpscare()
    {
        if (hasTriggeredJumpscare) return;
        hasTriggeredJumpscare = true;

        // The jumpscare and chase use this same AudioSource. Stop the looping chase
        // before playing the one-shot so the two sounds never overlap.
        isChasing = false;
        if (audioSource != null)
        {
            if (audioSource.isPlaying)
                audioSource.Stop();

            audioSource.clip = null;
            audioSource.loop = false;

            if (jumpscareSound != null)
                audioSource.PlayOneShot(jumpscareSound, 0.8f);
        }
        if (animator != null)
        {
            animator.SetTrigger("jumpscare");
        }

        if (JumpscareSystemNEW.Instance != null)
        {
            JumpscareSystemNEW.Instance.TriggerJumpscare(gameObject, true);
        }

        isJumpscaring = true;
        isWaitingForQnA = true;
        jumpscareAnimTimer = 2.23f;
        jumpscareLockPosition = transform.position;
        agent.enabled = false;
        transform.position = jumpscareLockPosition;

        StartCoroutine(TriggerQnADelayed());
    }

    IEnumerator TriggerQnADelayed()
    {
        yield return new WaitForSeconds(2.5f);

        if (player != null)
        {
            PlayerHealth playerHealth = player.GetComponent<PlayerHealth>();
            if (playerHealth != null && !playerHealth.IsDead())
            {
                TriggerQnA();
            }
            else
            {
                isJumpscaring = false;
                isWaitingForQnA = false;
                agent.enabled = true;
                hasTriggeredJumpscare = false;
                currentState = State.Patrol;
                SetNewPatrolTarget();
            }
        }
    }

    void SetNewPatrolTarget()
    {
        if (agent == null) return;

        if (!agent.enabled)
        {
            agent.enabled = true;
        }

        for (int i = 0; i < 15; i++)
        {
            Vector3 randomDirection = Random.insideUnitSphere * patrolRadius;
            randomDirection += startPosition;

            NavMeshHit hit;
            if (NavMesh.SamplePosition(randomDirection, out hit, patrolRadius, NavMesh.AllAreas))
            {
                SetDestinationAvoidingPaintedTrees(hit.position);
                agent.isStopped = false;
                return;
            }
        }

        if (NavMesh.SamplePosition(startPosition, out NavMeshHit fallbackHit, 10f, NavMesh.AllAreas))
        {
            SetDestinationAvoidingPaintedTrees(fallbackHit.position);
            agent.isStopped = false;
        }
        else
        {
            SetDestinationAvoidingPaintedTrees(transform.position);
            agent.isStopped = false;
        }
    }

    void UpdateAnimations()
    {
        if (animator == null) return;

        float speed = agent.velocity.magnitude;
        bool isMoving = speed > 0.1f;

        if (agent.hasPath && agent.remainingDistance > 0.5f)
        {
            isMoving = true;
        }

        if (!isJumpscaring && !isQnAActive)
        {
            animator.ResetTrigger("jumpscare");
        }

        if (isJumpscaring || isQnAActive)
        {
            return;
        }

        if (currentState == State.Chase)
        {
            animator.SetBool("run", true);
            animator.SetBool("walking", false);
        }
        else if (isMoving && !isIdle && currentState != State.Idle)
        {
            animator.SetBool("walking", true);
            animator.SetBool("run", false);
        }
        else
        {
            animator.SetBool("walking", false);
            animator.SetBool("run", false);
        }
    }

    void UpdateAudio()
    {
        if (audioSource == null) return;

        if (isChasing && chaseSound != null)
        {
            if (!audioSource.isPlaying)
            {
                audioSource.clip = chaseSound;
                audioSource.loop = true;
                audioSource.Play();
            }
        }
        else if (!isChasing && audioSource.isPlaying && audioSource.clip == chaseSound)
        {
            audioSource.Stop();
        }
    }

    public void ResetMonster()
    {
        currentState = State.Patrol;
        agent.Warp(startPosition);
        agent.ResetPath();
        agent.isStopped = false;
        agent.speed = walkSpeed;
        isChasing = false;
        isIdle = false;
        playerInSight = false;
        isJumpscaring = false;
        jumpscareAnimTimer = 0f;
        hasTriggeredJumpscare = false;
        isQnAActive = false;
        isClosingQnA = false;
        isQnAComplete = false;
        isWaitingForAnswer = false;
        hasAnswered = false;
        isPlayerLocked = false;
        hasUnlockedAfterQnA = false;
        qnaPanelHiddenForPause = false;
        agent.enabled = true;

        RestoreOtherAI();

        if (qnaPanel != null)
            qnaPanel.SetActive(false);

        if (playerController != null)
        {
            playerController.enabled = true;
        }

        if (animator != null)
        {
            animator.SetBool("walking", false);
            animator.SetBool("run", false);
            animator.ResetTrigger("jumpscare");
            animator.SetTrigger("Reset");
        }

        SetNewPatrolTarget();

        StartCoroutine(DelayedAnimationReset());

        if (audioSource != null) audioSource.Stop();
    }

    void OnDrawGizmosSelected()
    {
        if (!showGizmos) return;

        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, detectionRange);

        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, attackRange);

        Gizmos.color = Color.blue;
        Gizmos.DrawWireSphere(startPosition, patrolRadius);

        if (player != null)
        {
            Gizmos.color = playerInSight ? Color.green : Color.white;
            Gizmos.DrawLine(transform.position + Vector3.up * 0.5f, player.position + Vector3.up * 0.5f);
        }
    }
}
