using UnityEngine;
using UnityEngine.AI;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;

public class MonsterAI_New : MonoBehaviour
{
    [Header("Movement Settings")]
    public float walkSpeed = 2f;
    public float chaseSpeed = 5f;
    public float patrolRadius = 15f;
    public float idleTimeMin = 2f;
    public float idleTimeMax = 6f;
    
    [Header("Detection Settings")]
    public float detectionRange = 15f;
    public float attackRange = 2f;
    public bool useFieldOfView = false;
    public float fieldOfView = 60f;
    
    [Header("Obstacle Detection")]
    public bool checkObstacles = true;
    public LayerMask obstacleLayer = -1;
    
    [Header("Bush Detection")]
    public string bushTag = "Bush";
    public bool checkBush = true;
    
    [Header("Jumpscare Settings")]
    public float jumpscareCooldown = 2f;
    public float jumpscareDamage = 25f;

    [Header("Question & Answer Settings")]
    public GameObject qnaPanel;           
    public float questionDisplayTime = 30f;
    public int wrongAnswerDamage = 15;
    public float teleportRadius = 25f;

    [Header("Q&A Questions")]
    public QnAEntry[] questions;

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
    private float jumpscareAnimTimer = 0f;
    private Vector3 jumpscareLockPosition;
    private bool hasTriggeredJumpscare = false;

    // ── Q&A State ──
    private bool isQnAActive = false;
    private bool isClosingQnA = false;
    private QnAEntry currentQuestion;
    private int currentQuestionIndex = -1;
    private List<int> usedQuestionIndices = new List<int>();
    private float questionTimer = 0f;
    private bool isWaitingForAnswer = false;
    private bool hasAnswered = false;
    private CanvasGroup qnaCanvasGroup;
    private bool qnaPanelHiddenForPause = false;

    // ── Player Control References ──
    private PlayerController playerController;
    private CharacterController characterController;
    private Rigidbody playerRigidbody;
    private PauseMenu pauseMenu;
    private bool wasPlayerControllerEnabled = false;
    private bool isPlayerLocked = false;
    private bool isQnAComplete = false;
    private bool hasUnlockedAfterQnA = false;
    private readonly List<MutantAI> frozenMutantAIs = new List<MutantAI>();
    private readonly List<bool> frozenMutantAIStates = new List<bool>();
    private readonly List<NavMeshAgent> frozenMutantAgents = new List<NavMeshAgent>();
    private readonly List<bool> frozenMutantAgentStates = new List<bool>();
    private readonly List<Button> qnaButtons = new List<Button>();
    private readonly List<UnityEngine.Events.UnityAction> qnaButtonActions = new List<UnityEngine.Events.UnityAction>();

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
            if (jumpscareAnimTimer <= 0f)
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

    private void UnlockPlayer()
    {
        if (playerController != null)
        {
            if (pauseMenu == null || !pauseMenu.isPaused)
            {
                playerController.enabled = true;
                isPlayerLocked = false;
            }
            else
            {
                isPlayerLocked = false;
                hasUnlockedAfterQnA = true;
            }
        }
        else
        {
            isPlayerLocked = false;
        }
    }

    public void TriggerQnA()
    {
        if (isQnAActive) return;
        if (questions == null || questions.Length == 0)
        {
            return;
        }

        StartCoroutine(QnASequence());
    }

    IEnumerator QnASequence()
    {
        isQnAActive = true;
        isClosingQnA = false;
        isQnAComplete = false;
        hasUnlockedAfterQnA = false;
        currentState = State.QnA;
        isWaitingForAnswer = false;
        hasAnswered = false;
        agent.isStopped = true;
        isPlayerLocked = true;

        LockAllPlayerControls(true);
        FreezeOtherAI();

        currentQuestionIndex = GetRandomQuestionIndex();
        if (currentQuestionIndex == -1)
        {
            usedQuestionIndices.Clear();
            currentQuestionIndex = Random.Range(0, questions.Length);
        }
        usedQuestionIndices.Add(currentQuestionIndex);
        currentQuestion = questions[currentQuestionIndex];

        if (qnaPanel != null)
        {
            qnaPanel.SetActive(true);
            qnaCanvasGroup.alpha = 0f;
            
            float fadeElapsed = 0f;
            while (fadeElapsed < 0.3f)
            {
                fadeElapsed += Time.deltaTime;
                qnaCanvasGroup.alpha = Mathf.Lerp(0f, 1f, fadeElapsed / 0.3f);
                yield return null;
            }
            qnaCanvasGroup.alpha = 1f;

            UpdateQnAUI();
        }

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        isWaitingForAnswer = true;
        questionTimer = 0f;

        while (isWaitingForAnswer && questionTimer < questionDisplayTime)
        {
            questionTimer += Time.deltaTime;
            UpdateQnATimer();
            
            if (Cursor.visible == false)
            {
                Cursor.visible = true;
                Cursor.lockState = CursorLockMode.None;
            }
            
            if (playerController != null && playerController.enabled)
            {
                playerController.enabled = false;
                isPlayerLocked = true;
            }
            
            yield return null;
        }

        if (isWaitingForAnswer && !hasAnswered)
        {
            yield return StartCoroutine(WrongAnswerSequence());
        }

        yield return new WaitForSeconds(0.5f);
        
        if (!isClosingQnA)
        {
            CloseQnA();
        }
    }

    void LockAllPlayerControls(bool lockControls)
    {
        if (playerController != null)
        {
            if (lockControls)
            {
                wasPlayerControllerEnabled = playerController.enabled;
                playerController.enabled = false;
                isPlayerLocked = true;
            }
            else
            {
                if (!isQnAActive)
                {
                    if (pauseMenu == null || !pauseMenu.isPaused)
                    {
                        playerController.enabled = true;
                        isPlayerLocked = false;
                    }
                    else
                    {
                        isPlayerLocked = false;
                    }
                }
                else
                {
                }
            }
        }

        if (characterController != null && lockControls)
        {
            characterController.Move(Vector3.zero);
        }

        if (playerRigidbody != null && lockControls)
        {
            playerRigidbody.velocity = Vector3.zero;
            playerRigidbody.angularVelocity = Vector3.zero;
        }

        if (lockControls)
        {
            Input.ResetInputAxes();
        }

        if (pauseMenu != null && lockControls)
        {
            if (pauseMenu.isPaused)
            {
                pauseMenu.Resume();
            }
        }

        if (lockControls)
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }
        else
        {
            if (pauseMenu == null || !pauseMenu.isPaused)
            {
                Cursor.lockState = CursorLockMode.Locked;
                Cursor.visible = false;
            }
        }
    }

    int GetRandomQuestionIndex()
    {
        List<int> availableIndices = new List<int>();
        for (int i = 0; i < questions.Length; i++)
        {
            if (!usedQuestionIndices.Contains(i))
                availableIndices.Add(i);
        }

        if (availableIndices.Count == 0)
            return -1;

        return availableIndices[Random.Range(0, availableIndices.Count)];
    }

    void UpdateQnAUI()
    {
        if (qnaPanel == null || currentQuestion == null) return;

        // ── Get TextMeshPro components from the children ──
        Transform questionText = qnaPanel.transform.Find("QuestionText");
        Transform optionAText = qnaPanel.transform.Find("OptionA/Text (TMP)");
        Transform optionBText = qnaPanel.transform.Find("OptionB/Text (TMP)");
        Transform optionCText = qnaPanel.transform.Find("OptionC/Text (TMP)");
        Transform optionDText = qnaPanel.transform.Find("OptionD/Text (TMP)");
        Transform timerText = qnaPanel.transform.Find("TimerText");
        Transform feedbackText = qnaPanel.transform.Find("FeedbackText");

        if (feedbackText != null)
        {
            var tmpro = feedbackText.GetComponent<TMPro.TextMeshProUGUI>();
            if (tmpro != null) tmpro.text = "";
        }

        if (questionText != null)
        {
            var tmpro = questionText.GetComponent<TMPro.TextMeshProUGUI>();
            if (tmpro != null) tmpro.text = currentQuestion.question;
        }

        // ── Update option texts ──
        if (optionAText != null)
        {
            var tmpro = optionAText.GetComponent<TMPro.TextMeshProUGUI>();
            if (tmpro != null && currentQuestion.options != null && currentQuestion.options.Length > 0)
                tmpro.text = $"A. {currentQuestion.options[0]}";
        }

        if (optionBText != null)
        {
            var tmpro = optionBText.GetComponent<TMPro.TextMeshProUGUI>();
            if (tmpro != null && currentQuestion.options != null && currentQuestion.options.Length > 1)
                tmpro.text = $"B. {currentQuestion.options[1]}";
        }

        if (optionCText != null)
        {
            var tmpro = optionCText.GetComponent<TMPro.TextMeshProUGUI>();
            if (tmpro != null && currentQuestion.options != null && currentQuestion.options.Length > 2)
                tmpro.text = $"C. {currentQuestion.options[2]}";
        }

        if (optionDText != null)
        {
            var tmpro = optionDText.GetComponent<TMPro.TextMeshProUGUI>();
            if (tmpro != null && currentQuestion.options != null && currentQuestion.options.Length > 3)
                tmpro.text = $"D. {currentQuestion.options[3]}";
        }

        if (timerText != null)
        {
            var tmpro = timerText.GetComponent<TMPro.TextMeshProUGUI>();
            if (tmpro != null) tmpro.text = $"Time: {questionDisplayTime:F1}s";
        }
    }

    void UpdateQnATimer()
    {
        if (qnaPanel == null) return;

        Transform timerText = qnaPanel.transform.Find("TimerText");
        if (timerText != null)
        {
            float remaining = Mathf.Max(0f, questionDisplayTime - questionTimer);
            string timerString = $"Time: {remaining:F1}s";
            
            var tmpro = timerText.GetComponent<TMPro.TextMeshProUGUI>();
            if (tmpro != null) tmpro.text = timerString;
        }
    }

    void WireQnAButtons()
    {
        string[] buttonNames = { "OptionA", "OptionB", "OptionC", "OptionD" };

        for (int i = 0; i < buttonNames.Length; i++)
        {
            Transform option = qnaPanel.transform.Find(buttonNames[i]);
            Button button = option != null
                ? option.GetComponentInChildren<Button>(true)
                : null;

            if (button == null)
                continue;

            int answerIndex = i;
            UnityEngine.Events.UnityAction action = () => AnswerQnA(answerIndex);
            button.onClick.AddListener(action);
            qnaButtons.Add(button);
            qnaButtonActions.Add(action);
        }
    }

    void OnDestroy()
    {
        for (int i = 0; i < qnaButtons.Count; i++)
        {
            if (qnaButtons[i] != null)
                qnaButtons[i].onClick.RemoveListener(qnaButtonActions[i]);
        }
    }

    public void AnswerQnA(int selectedIndex)
    {
        if (!isWaitingForAnswer || hasAnswered) return;
        hasAnswered = true;
        isWaitingForAnswer = false;

        if (currentQuestion == null) return;

        bool isCorrect = (selectedIndex == currentQuestion.correctAnswerIndex);

        if (isCorrect)
        {
            StartCoroutine(CorrectAnswerSequence());
        }
        else
        {
            StartCoroutine(WrongAnswerSequence());
        }
    }

    IEnumerator CorrectAnswerSequence()
    {
        ShowFeedback("Correct!", Color.green);
        yield return new WaitForSeconds(0.8f);

        TeleportMonsterAway();
        
        yield return new WaitForSeconds(0.2f);
        CloseQnA();
    }

    IEnumerator WrongAnswerSequence()
    {
        ShowFeedback("Wrong!", Color.red);
        yield return new WaitForSeconds(0.8f);

        if (player != null)
        {
            PlayerHealth playerHealth = player.GetComponent<PlayerHealth>();
            if (playerHealth != null)
            {
                playerHealth.TakeDamage(wrongAnswerDamage);
            }
        }

        TeleportMonsterAway();
        
        yield return new WaitForSeconds(0.2f);
        CloseQnA();
    }

    void TeleportMonsterAway()
    {
        Vector3 randomPos = GetRandomTeleportPosition();
        
        if (agent != null && agent.isOnNavMesh)
        {
            agent.Warp(randomPos);
            agent.ResetPath();
            agent.isStopped = false;
            agent.speed = walkSpeed;
        }
        transform.position = randomPos;
        
        currentState = State.Patrol;
        hasTriggeredJumpscare = false;
        isChasing = false;
        isIdle = false;
        playerInSight = false;
        
        SetNewPatrolTarget();
    }

    Vector3 GetRandomTeleportPosition()
    {
        Vector3 randomDir = Random.insideUnitSphere * teleportRadius;
        randomDir.y = 0f;
        Vector3 targetPos = player.position + randomDir;

        NavMeshHit hit;
        if (NavMesh.SamplePosition(targetPos, out hit, teleportRadius, NavMesh.AllAreas))
        {
            return hit.position;
        }

        if (NavMesh.SamplePosition(startPosition, out hit, 10f, NavMesh.AllAreas))
        {
            return hit.position;
        }

        return targetPos;
    }

    void ShowFeedback(string message, Color color)
    {
        if (qnaPanel == null) return;

        Transform feedbackText = qnaPanel.transform.Find("FeedbackText");
        if (feedbackText != null)
        {
            var tmpro = feedbackText.GetComponent<TMPro.TextMeshProUGUI>();
            if (tmpro != null)
            {
                tmpro.text = message;
                tmpro.color = color;
            }
        }
    }

    void FreezeOtherAI()
    {
        frozenMutantAIs.Clear();
        frozenMutantAIStates.Clear();
        frozenMutantAgents.Clear();
        frozenMutantAgentStates.Clear();

        foreach (MutantAI mutantAI in FindObjectsOfType<MutantAI>())
        {
            if (mutantAI == null || mutantAI.gameObject == gameObject)
                continue;

            NavMeshAgent mutantAgent = mutantAI.GetComponent<NavMeshAgent>();

            frozenMutantAIs.Add(mutantAI);
            frozenMutantAIStates.Add(mutantAI.enabled);
            frozenMutantAgents.Add(mutantAgent);
            frozenMutantAgentStates.Add(mutantAgent != null && mutantAgent.enabled);

            mutantAI.enabled = false;
            if (mutantAgent != null)
                mutantAgent.enabled = false;
        }
    }

    void RestoreOtherAI()
    {
        for (int i = 0; i < frozenMutantAIs.Count; i++)
        {
            if (frozenMutantAgents[i] != null)
                frozenMutantAgents[i].enabled = frozenMutantAgentStates[i];

            if (frozenMutantAIs[i] != null)
                frozenMutantAIs[i].enabled = frozenMutantAIStates[i];
        }

        frozenMutantAIs.Clear();
        frozenMutantAIStates.Clear();
        frozenMutantAgents.Clear();
        frozenMutantAgentStates.Clear();
    }

    void CloseQnA()
    {
        if (isClosingQnA) return;
        isClosingQnA = true;

        isQnAActive = false;
        qnaPanelHiddenForPause = false;
        isWaitingForAnswer = false;
        hasAnswered = false;
        currentState = State.Patrol;
        agent.isStopped = false;
        agent.speed = walkSpeed;

        if (qnaPanel != null)
        {
            qnaPanel.SetActive(false);
        }

        RestoreOtherAI();

        LockAllPlayerControls(false);
        isQnAComplete = true;

        ResetMonsterState();

        StartCoroutine(ResetCloseFlag());
    }

    void ResetMonsterState()
    {
        isChasing = false;
        isIdle = false;
        playerInSight = false;
        hasTriggeredJumpscare = false;
        isJumpscaring = false;
        jumpscareAnimTimer = 0f;
        idleTimer = 0f;
        
        if (agent != null)
        {
            agent.isStopped = false;
            agent.speed = walkSpeed;
            agent.ResetPath();
        }
        
        if (animator != null)
        {
            animator.ResetTrigger("jumpscare");
            animator.SetBool("walking", false);
            animator.SetBool("run", false);
        }
        
        currentState = State.Patrol;
        
        SetNewPatrolTarget();
        
        StartCoroutine(DelayedAnimationReset());
    }

    IEnumerator DelayedAnimationReset()
    {
        yield return new WaitForSeconds(0.2f);
        
        if (animator != null)
        {
            if (agent != null && agent.velocity.magnitude > 0.1f)
            {
                animator.SetBool("walking", true);
            }
            else
            {
                yield return new WaitForSeconds(0.3f);
                if (agent != null && agent.velocity.magnitude > 0.1f)
                {
                    animator.SetBool("walking", true);
                }
                else
                {
                    animator.SetBool("walking", true);
                }
            }
        }
    }

    IEnumerator ResetCloseFlag()
    {
        yield return new WaitForSeconds(0.5f);
        isClosingQnA = false;
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
            agent.SetDestination(player.position);
            
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

        if (jumpscareSound != null && audioSource != null)
        {
            audioSource.PlayOneShot(jumpscareSound, 0.8f);
        }

        if (animator != null)
        {
            animator.SetTrigger("jumpscare");
        }

        if (JumpscareSystemNEW.Instance != null)
        {
            JumpscareSystemNEW.Instance.TriggerJumpscare(gameObject);
        }

        isJumpscaring = true;
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
                agent.SetDestination(hit.position);
                agent.isStopped = false;
                return;
            }
        }
        
        if (NavMesh.SamplePosition(startPosition, out NavMeshHit fallbackHit, 10f, NavMesh.AllAreas))
        {
            agent.SetDestination(fallbackHit.position);
            agent.isStopped = false;
        }
        else
        {
            agent.SetDestination(transform.position);
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

// ── Q&A Data Structure ──
[System.Serializable]
public class QnAEntry
{
    [TextArea(2, 4)]
    public string question;
    public string[] options = new string[4];
    public int correctAnswerIndex;

    public QnAEntry()
    {
        options = new string[4];
        correctAnswerIndex = 0;
    }
}