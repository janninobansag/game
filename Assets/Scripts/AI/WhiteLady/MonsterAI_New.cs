// PURPOSE: Controls the White Lady patrol, detection, chase, Q&A encounter, damage, and jumpscare behavior.
using UnityEngine;
using UnityEngine.AI;
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
    private readonly AIEncounterPause encounterPause = new AIEncounterPause();

    // FINITE STATE MACHINE (FSM): Update selects one high-level behavior for White Lady each frame.
    private enum State
    {
        Patrol,    // Walk between patrol destinations while the player is not detected.
        Chase,     // Move toward a visible player who is within detection range.
        Jumpscare, // Stop close to the player and start the catch sequence when in attack range.
        Idle,      // Pause briefly between patrol destinations.
        QnA        // Stay still while the player answers the encounter question.
    }

    private State currentState = State.Patrol; // Start in patrol mode until detection changes the state.

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
        while (AIEncounterGate.IsBlocked(this))
            yield return null;

        currentState = State.Patrol; // Initialize the FSM in its normal roaming state.
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
        while (AIEncounterGate.IsBlocked(this))
            yield return null;

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
        if (encounterPause.Update(this, agent, animator, audioSource))
            return;

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

        float distanceToPlayer = Vector3.Distance(transform.position, player.position); // Measure distance for FSM detection and attack transitions.
        bool canSeePlayer = CanSeePlayer();

        bool isInBush = IsPlayerInBush();
        bool playerVisible = IsPlayerVisible() && !isInBush; // Bushes and obstacles prevent the FSM from treating the player as visible.

        // FSM transition: a visible player inside detection range changes White Lady to Chase or Jumpscare.
        if (playerVisible && distanceToPlayer <= detectionRange)
        {
            playerInSight = true; // Record that the detection condition is currently satisfied.
            if (distanceToPlayer <= attackRange && !hasTriggeredJumpscare)
                currentState = State.Jumpscare; // Enter Jumpscare when close enough and no jumpscare is already active.
            else
                currentState = State.Chase; // Otherwise pursue the visible player.
        }
        else
        {
            playerInSight = false; // Clear the detection flag when the player cannot be seen nearby.
            if (currentState != State.Jumpscare && currentState != State.Idle && currentState != State.QnA)
                currentState = State.Patrol; // Return to patrol unless an interrupt state must finish first.
        }

        // FSM dispatch: run the behavior belonging to the state chosen above.
        switch (currentState)
        {
            case State.Patrol: // The patrol state moves White Lady between destinations.
                Patrol(); // Execute patrol behavior for this frame.
                break; // Finish this state branch so no other state runs this frame.
            case State.Chase: // The chase state follows the detected player.
                Chase(); // Execute chase behavior for this frame.
                break; // Finish this state branch so no other state runs this frame.
            case State.Jumpscare: // The jumpscare state checks range and starts the encounter.
                Jumpscare(); // Execute jumpscare behavior for this frame.
                break; // Finish this state branch so no other state runs this frame.
            case State.Idle: // The idle state pauses before choosing the next patrol destination.
                Idle(); // Execute idle behavior for this frame.
                break; // Finish this state branch so no other state runs this frame.
            case State.QnA: // Q&A is controlled by its coroutine and input callbacks, so Update does no movement here.
                break; // Keep White Lady in Q&A without running another movement state.
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

    // Patrol state's actions and its transition to Idle when the current route ends.
    void Patrol()
    {
        isChasing = false; // Stop chase audio and report that active pursuit has ended.
        agent.speed = walkSpeed; // Use the walking speed while patrolling.
        agent.isStopped = false; // Allow the NavMeshAgent to follow its patrol path.

        // A missing or completed path means patrol movement has reached a pause point.
        if (!agent.hasPath || agent.remainingDistance < 0.5f)
        {
            if (!isIdle)
            {
                isIdle = true; // Prevent repeatedly starting the same idle transition.
                idleTimer = Random.Range(idleTimeMin, idleTimeMax); // Choose how long this idle state lasts.
                currentState = State.Idle; // Transition from Patrol to Idle.
            }
        }
    }

    // Idle state's actions and transition back to Patrol after its timer expires.
    void Idle()
    {
        agent.isStopped = true; // Hold position while the idle state is active.
        isChasing = false; // Ensure chase audio is not treated as active while idle.

        idleTimer -= Time.deltaTime; // Count down the remaining idle duration.
        if (idleTimer <= 0f)
        {
            isIdle = false; // Clear the idle flag before resuming movement.
            agent.isStopped = false; // Allow the NavMeshAgent to move again.
            SetNewPatrolTarget(); // Select the route for the next patrol cycle.
            currentState = State.Patrol; // Transition from Idle back to Patrol.
        }
    }

    // Chase state's actions; the state remains Chase while the player is detected in range.
    void Chase()
    {
        isChasing = true; // Mark pursuit active so the chase audio and animation can play.
        agent.speed = chaseSpeed; // Apply the faster movement speed used in Chase.
        agent.isStopped = false; // Let the NavMeshAgent move toward the player.

        if (player != null) // Only calculate pursuit movement when the player reference exists.
        {
            if (Time.time >= nextPaintedTreeDestinationUpdate) // Refresh the route only at the configured interval.
            {
                SetDestinationAvoidingPaintedTrees(player.position); // Set a route toward the player that accounts for painted trees.
                nextPaintedTreeDestinationUpdate = Time.time + paintedTreeDestinationRefreshRate; // Schedule the next route update.
            }

            Vector3 direction = (player.position - transform.position).normalized; // Find the direction from White Lady to the player.
            direction.y = 0f; // Keep rotation level so she does not tilt vertically.
            if (direction != Vector3.zero) // Avoid creating a rotation from a zero-length direction.
            {
                Quaternion lookRotation = Quaternion.LookRotation(direction); // Compute the horizontal facing rotation.
                transform.rotation = Quaternion.Slerp(transform.rotation, lookRotation, Time.deltaTime * 5f); // Turn smoothly toward the player.
            }
        }
    }

    // Jumpscare state's actions; it returns to Chase if the player leaves attack range.
    void Jumpscare()
    {
        if (isJumpscaring || hasTriggeredJumpscare) return; // Do not start a second jumpscare while one is already running.

        agent.isStopped = true; // Stop navigation while checking whether the player is close enough to catch.

        if (player != null) // A catch can only happen when the player reference is valid.
        {
            Vector3 direction = (player.position - transform.position).normalized; // Find the horizontal direction toward the player.
            direction.y = 0f; // Keep White Lady upright while she turns.
            if (direction != Vector3.zero) // Skip rotation if White Lady and player occupy the same position.
            {
                Quaternion lookRotation = Quaternion.LookRotation(direction); // Compute the facing rotation for the encounter.
                transform.rotation = Quaternion.Slerp(transform.rotation, lookRotation, Time.deltaTime * 5f); // Turn smoothly toward the player.
            }

            float distance = Vector3.Distance(transform.position, player.position); // Measure whether the player is still in catch range.
            if (distance > attackRange + 1f) // Add a small tolerance so NavMesh spacing does not block the transition.
            {
                currentState = State.Chase; // Transition back to Chase if the player is no longer close enough to catch.
                agent.isStopped = false; // Resume movement after returning to Chase.
                return; // Stop this state update before any jumpscare can start.
            }

            if (jumpscareTimer <= 0f) // Start a catch only after the cooldown has elapsed.
            {
                PerformJumpscare(); // Begin the jumpscare/Q&A encounter sequence.
                jumpscareTimer = jumpscareCooldown; // Set the cooldown before another catch can be attempted.
            }
        }
    }

    // Entry action called by the Jumpscare state after its range and cooldown checks pass.
    void PerformJumpscare()
    {
        if (hasTriggeredJumpscare || !AIEncounterGate.TryBegin(this)) return; // Stop duplicates or defer if another AI owns the encounter.
        hasTriggeredJumpscare = true; // Lock this catch so Update cannot start it again.

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

        isJumpscaring = true; // Tell Update and animation logic that the jumpscare sequence is active.
        isWaitingForQnA = true; // Keep the jumpscare state active until the question flow starts or aborts.
        jumpscareAnimTimer = 2.23f; // Set how long the jumpscare animation lock should last.
        jumpscareLockPosition = transform.position; // Save White Lady's position for the locked animation.
        agent.enabled = false; // Disable navigation so the agent cannot move during the jumpscare.
        transform.position = jumpscareLockPosition; // Hold the monster at the saved jumpscare position.

        StartCoroutine(TriggerQnADelayed()); // Start the delayed transition from jumpscare to Q&A.
    }

    IEnumerator TriggerQnADelayed()
    {
        yield return new WaitForSeconds(2.5f);

        if (player != null)
        {
            PlayerHealth playerHealth = player.GetComponent<PlayerHealth>();
            if (playerHealth != null && !playerHealth.IsDead() &&
                questions != null && questions.Length > 0)
            {
                TriggerQnA();
                yield break;
            }
        }

        // A dead or missing player (or an unconfigured question set) cannot
        // complete Q&A, so do not leave every other AI frozen indefinitely.
        isJumpscaring = false;
        isWaitingForQnA = false;
        agent.enabled = true;
        hasTriggeredJumpscare = false;
        jumpscareTimer = jumpscareCooldown;
        currentState = State.Patrol; // Resume the normal FSM state if the encounter cannot proceed to Q&A.
        SetNewPatrolTarget();
        JumpscareSystemNEW.Instance?.ReleaseJumpscareLock();
        yield return StartCoroutine(WaitForJumpscareRelease());
        AIEncounterGate.End(this);
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
        currentState = State.Patrol; // Reset the FSM so the monster restarts in patrol mode.
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

        AIEncounterGate.End(this);

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

    private void OnDisable()
    {
        encounterPause.Restore(agent, animator, audioSource);
        AIEncounterGate.End(this);
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
