using System.Collections;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.UI;
using TMPro;

[RequireComponent(typeof(NavMeshAgent))]
public class TikbalangAI : MonoBehaviour
{
    [Header("Trigger Debug")]
    [Tooltip("Shows why Tikbalang was asked to teleport. Disable after testing.")]
    public bool debugTrigger = true;
    [Header("References")]
    public Transform player;
    public Animator animator;
    public AudioSource audioSource;
    [Tooltip("Assign the spawn point children from TELEPORTER FOR ENEMY here.")]
    public Transform[] teleportSpawnPoints;

    [Tooltip("Sound played when Tikbalang is first discovered.")]
    public AudioClip discoveryShout;
    [Tooltip("How long to show the jumpscare animation before teleporting.")]
    [Min(0f)] public float jumpscareBeforeTeleportDelay = 1.5f;
    [Tooltip("Distance in front of the player where Tikbalang appears after the first jumpscare.")]
    [Min(1f)] public float firstEncounterFrontDistance = 4f;

    [Header("Question & Answer Settings")]
    [Tooltip("Drag the scene QnAPanel here. The game can find it automatically if left empty.")]
    public GameObject qnaPanel;
    [Min(1f)] public float questionDisplayTime = 30f;
    [Min(0f)] public float wrongAnswerDamage = 40f;

    [Header("Q&A Questions")]
    public QnAEntry[] questions;

    [Header("Catch Jumpscare")]
    [Tooltip("Tikbalang catches the player and starts the Q&A jumpscare within this distance.")]
    [Min(0.1f)] public float catchRange = 1.4f;
    [Tooltip("Catch distance used after Tikbalang has started running. Keep this larger than Catch Range so the running jumpscare is not too close.")]
    [Min(0.1f)] public float runningCatchRange = 2.3f;
    [Tooltip("Prevents an immediate second catch after Tikbalang teleports away.")]
    [Min(0f)] public float catchCooldown = 3f;
    [Tooltip("Optional. Leave empty to use the Main Camera.")]
    public Transform jumpscareCamera;
    [Min(0f)] public float cameraLookDuration = 0.3f;
    [Min(0f)] public float cameraReturnDuration = 0.25f;
    [Min(0f)] public float tikbalangFaceHeight = 1.5f;
    [Header("Chase After First Encounter")]
    [Min(0.1f)] public float walkSpeed = 3.25f;
    [Min(0.1f)] public float runSpeed = 6f;
    [Min(0f)] public float stoppingDistance = 1.2f;
    [Min(0.02f)] public float destinationRefreshRate = 0.1f;

    [Header("Chase Audio")]
    [Tooltip("Looped only while Tikbalang is running after the player sees it.")]
    public AudioClip chaseSound;
    [Range(0f, 1f)] public float chaseVolume = 0.8f;

    [Header("Player Sight Run Trigger")]
    [Tooltip("Once the player camera gets a clear view of Tikbalang, he keeps running for the rest of that chase.")]
    public bool runWhenPlayerSeesTikbalang = true;
    [Tooltip("0 means unlimited distance. A wall or other collider blocks this sight check.")]
    [Min(0f)] public float playerSightDistance = 0f;
    public LayerMask playerSightObstacleMask = ~0;
    [Header("Flashlight Teleport")]
    [Min(0f)] public float teleportCooldown = 1f;
    [Min(0.1f)] public float spawnPointNavMeshSearchRadius = 3f;

    private NavMeshAgent agent;
    private float nextDestinationUpdate;
    private float nextTeleportTime;
    private int lastSpawnPointIndex = -1;
    private bool hasAwakened;
    public bool HasFirstEncounterStarted => hasAwakened;
    public bool IsChasingPlayer => hasAwakened && !isFirstEncounterPlaying && !isCatchSequencePlaying;
    private bool isFirstEncounterPlaying;
    private bool isCatchSequencePlaying;
    private bool isPlayingChaseSound;
    private bool hasBeenSeenByPlayer;
    private float nextCatchTime;
    private Quaternion cameraRotationBeforeCatch;
    private PlayerController playerController;
    private QnAEntry currentQuestion;
    private bool waitingForAnswer;
    private bool answerReceived;
    private bool isQuestionPanelOpen;
    private bool wasPlayerControllerEnabled;
    private readonly System.Collections.Generic.List<Button> qnaButtons = new System.Collections.Generic.List<Button>();
    private void Awake()
    {
        agent = GetComponent<NavMeshAgent>();
        if (animator == null) animator = GetComponentInChildren<Animator>();
        if (audioSource == null) audioSource = GetComponent<AudioSource>();
        FindPlayerIfNeeded();
    }

    private void Start()
    {
        agent.speed = walkSpeed;
        agent.stoppingDistance = stoppingDistance;
        playerController = player != null ? player.GetComponent<PlayerController>() : null;
        FindAndPrepareQnAPanel();
        if (jumpscareCamera == null && Camera.main != null) jumpscareCamera = Camera.main.transform;
        PlaceOnNavMeshIfNeeded();
        SetDormantState();
    }

    private void Update()
    {
        FindPlayerIfNeeded();
        if (player == null) return;

        // Tikbalang remains dormant until TikbalangJumpscareTrigger starts the first encounter.
        if (!hasAwakened)
            return;

        if (isCatchSequencePlaying)
            return;

        float activeCatchRange = hasBeenSeenByPlayer ? Mathf.Max(catchRange, runningCatchRange) : catchRange;
        if (Time.time >= nextCatchTime && Vector3.Distance(transform.position, player.position) <= activeCatchRange)
        {
            StartCoroutine(PlayCatchJumpscare());
            return;
        }

        ChasePlayer();
    }

    private void LateUpdate()
    {
        // PlayerController and other gameplay scripts normally lock the cursor.
        // Keep Q&A clickable regardless of script execution order.
        if (!isQuestionPanelOpen)
            return;

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }
    // Called by TikbalangJumpscareTrigger when the player enters its detector collider.
    public bool TriggerDetectionJumpscare(TikbalangJumpscareTrigger triggerSource)
    {
        FindPlayerIfNeeded();
        string sourceName = triggerSource != null ? triggerSource.name : "unknown source";

        if (player == null)
        {
            LogTriggerDebug($"Rejected teleport request from '{sourceName}': Player reference was not found.");
            return false;
        }

        if (isFirstEncounterPlaying || isCatchSequencePlaying)
        {
            LogTriggerDebug($"Rejected teleport request from '{sourceName}': a Tikbalang sequence is already running.");
            return false;
        }

        LogTriggerDebug($"Accepted teleport request from detector '{sourceName}'.");
        StartCoroutine(PlayDetectionJumpscare());
        return true;
    }

    private IEnumerator PlayDetectionJumpscare()
    {
        isFirstEncounterPlaying = true;
        hasAwakened = true;

        if (agent != null && agent.enabled && agent.isOnNavMesh)
        {
            agent.isStopped = true;
            agent.ResetPath();
        }

        SetMovementAnimation(false);
        TeleportInFrontOfPlayer();
        yield return StartCoroutine(PlayCatchJumpscare());
        isFirstEncounterPlaying = false;
    }
    private IEnumerator PlayCatchJumpscare()
    {
        isCatchSequencePlaying = true;
        if (agent.enabled && agent.isOnNavMesh)
        {
            agent.isStopped = true;
            agent.ResetPath();
        }

        SetMovementAnimation(false);
        StopChaseSound();
        SetPlayerQuestionLock(true);
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
        yield return StartCoroutine(FocusCameraOnTikbalang());
        SetAnimatorTriggerIfPresent("jumpscare");

        if (audioSource != null && discoveryShout != null)
            audioSource.PlayOneShot(discoveryShout);

        yield return new WaitForSeconds(jumpscareBeforeTeleportDelay);
        yield return StartCoroutine(ShowJumpscareQuestion(false));

        TeleportToSpawnPoint(true);
yield return StartCoroutine(RestoreCameraAfterJumpscare());
        SetPlayerQuestionLock(false);
        nextCatchTime = Time.time + catchCooldown;
        isCatchSequencePlaying = false;
        ResumeChaseAfterEncounter();
    }

    private IEnumerator FocusCameraOnTikbalang()
    {
        if (jumpscareCamera == null && Camera.main != null)
            jumpscareCamera = Camera.main.transform;
        if (jumpscareCamera == null)
            yield break;

        cameraRotationBeforeCatch = jumpscareCamera.rotation;
        Vector3 targetPosition = transform.position + Vector3.up * tikbalangFaceHeight;
        Quaternion targetRotation = Quaternion.LookRotation((targetPosition - jumpscareCamera.position).normalized);
        float elapsed = 0f;

        while (elapsed < cameraLookDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            jumpscareCamera.rotation = Quaternion.Slerp(cameraRotationBeforeCatch, targetRotation,
                cameraLookDuration <= 0f ? 1f : elapsed / cameraLookDuration);
            yield return null;
        }

        jumpscareCamera.rotation = targetRotation;
    }

    private IEnumerator RestoreCameraAfterJumpscare()
    {
        if (jumpscareCamera == null)
            yield break;

        Quaternion startRotation = jumpscareCamera.rotation;
        float elapsed = 0f;
        while (elapsed < cameraReturnDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            jumpscareCamera.rotation = Quaternion.Slerp(startRotation, cameraRotationBeforeCatch,
                cameraReturnDuration <= 0f ? 1f : elapsed / cameraReturnDuration);
            yield return null;
        }

        jumpscareCamera.rotation = cameraRotationBeforeCatch;
    }
    private void FindAndPrepareQnAPanel()
    {
        if (qnaPanel == null)
        {
            foreach (Transform candidate in Resources.FindObjectsOfTypeAll<Transform>())
            {
                if (candidate != null && candidate.name == "QnAPanel" && candidate.gameObject.scene.IsValid())
                {
                    qnaPanel = candidate.gameObject;
                    break;
                }
            }
        }

        if (qnaPanel == null)
        {
            return;
        }

        qnaPanel.SetActive(false);
        WireAnswerButton("OptionA", 0);
        WireAnswerButton("OptionB", 1);
        WireAnswerButton("OptionC", 2);
        WireAnswerButton("OptionD", 3);
    }

    private IEnumerator ShowJumpscareQuestion(bool unlockPlayerAfterQuestion = true)
    {
        if (qnaPanel == null)
            yield break;

        if (questions == null || questions.Length == 0)
        {
            questions = new[]
            {
                new QnAEntry
                {
                    question = "What Philippine mythical creature is half-human and half-horse?",
                    options = new[] { "Tikbalang", "Aswang", "Kapre", "Manananggal" },
                    correctAnswerIndex = 0
                }
            };
        }

        currentQuestion = questions[Random.Range(0, questions.Length)];
        if (currentQuestion == null || currentQuestion.options == null || currentQuestion.options.Length < 4)
        {
            yield break;
        }

        SetPlayerQuestionLock(true);
        PopulateQuestionUI();
        qnaPanel.SetActive(true);
        isQuestionPanelOpen = true;
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
        SetAnswerButtonsInteractable(true);

        waitingForAnswer = true;
        answerReceived = false;
        float elapsed = 0f;
        while (waitingForAnswer && elapsed < questionDisplayTime)
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
            elapsed += Time.unscaledDeltaTime;
            SetText("TimerText", "Time: " + Mathf.Max(0f, questionDisplayTime - elapsed).ToString("F1") + "s");
            yield return null;
        }

        bool correct = answerReceived && currentQuestion != null && selectedAnswerIndex == currentQuestion.correctAnswerIndex;
        if (correct)
        {
            SetFeedback("Correct!", Color.green);
        }
        else
        {
            SetFeedback("Wrong!", Color.red);
        }

        // Keep the feedback visible first. Applying damage afterward lets the shared
        // PlayerHealth blood/vignette effect remain visible as the Q&A panel closes.
        yield return new WaitForSecondsRealtime(0.8f);

        if (!correct)
        {
            PlayerHealth health = player != null ? player.GetComponent<PlayerHealth>() : null;
            if (health != null)
                health.TakeDamage(wrongAnswerDamage);

            yield return new WaitForSecondsRealtime(0.2f);
        }
        isQuestionPanelOpen = false;
        qnaPanel.SetActive(false);
        SetPlayerQuestionLock(false);
    }

    private int selectedAnswerIndex = -1;

    private void WireAnswerButton(string buttonName, int answerIndex)
    {
        Transform option = qnaPanel.transform.Find(buttonName);
        Button button = option != null ? option.GetComponentInChildren<Button>(true) : null;
        if (button == null) return;

        button.onClick.AddListener(() => SelectAnswer(answerIndex));
        qnaButtons.Add(button);
    }

    private void SelectAnswer(int answerIndex)
    {
        if (!waitingForAnswer || answerReceived) return;
        selectedAnswerIndex = answerIndex;
        answerReceived = true;
        waitingForAnswer = false;
        SetAnswerButtonsInteractable(false);
    }

    private void PopulateQuestionUI()
    {
        selectedAnswerIndex = -1;
        SetText("QuestionText", currentQuestion.question);
        SetText("OptionA/Text (TMP)", "A. " + currentQuestion.options[0]);
        SetText("OptionB/Text (TMP)", "B. " + currentQuestion.options[1]);
        SetText("OptionC/Text (TMP)", "C. " + currentQuestion.options[2]);
        SetText("OptionD/Text (TMP)", "D. " + currentQuestion.options[3]);
        SetFeedback(string.Empty, Color.white);
    }

    private void SetText(string path, string value)
    {
        Transform textTransform = qnaPanel.transform.Find(path);
        TextMeshProUGUI text = textTransform != null ? textTransform.GetComponent<TextMeshProUGUI>() : null;
        if (text != null) text.text = value;
    }

    private void SetFeedback(string message, Color color)
    {
        Transform feedbackTransform = qnaPanel.transform.Find("FeedbackText");
        TextMeshProUGUI feedback = feedbackTransform != null ? feedbackTransform.GetComponent<TextMeshProUGUI>() : null;
        if (feedback != null)
        {
            feedback.text = message;
            feedback.color = color;
        }
    }

    private void SetAnswerButtonsInteractable(bool value)
    {
        foreach (Button button in qnaButtons)
            if (button != null) button.interactable = value;
    }

    private void SetPlayerQuestionLock(bool locked)
    {
        if (playerController == null && player != null)
            playerController = player.GetComponent<PlayerController>();

        if (playerController == null) return;

        if (locked)
        {
            if (playerController.enabled)
                wasPlayerControllerEnabled = true;
            playerController.enabled = false;
        }
        else if (wasPlayerControllerEnabled)
        {
            playerController.enabled = true;
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }
    }
    private void ResumeChaseAfterEncounter()
    {
        if (agent == null || !agent.enabled || !agent.isOnNavMesh || player == null)
        {
            LogTriggerDebug("First encounter finished, but Tikbalang could not resume chase because the agent or player is unavailable.");
            return;
        }

        nextDestinationUpdate = 0f;
        agent.isStopped = false;
        agent.SetDestination(player.position);
        SetMovementAnimation(true);
        LogTriggerDebug("First encounter finished. Tikbalang is now chasing the player.");
    }
    private void ChasePlayer()
    {
        if (!agent.enabled || !agent.isOnNavMesh)
        {
            SetMovementAnimation(false);
            return;
        }

        if (runWhenPlayerSeesTikbalang && !hasBeenSeenByPlayer && CanPlayerSeeTikbalang())
            hasBeenSeenByPlayer = true;

        // Seeing Tikbalang once starts a persistent run; looking away does not cancel it.
        bool shouldRun = runWhenPlayerSeesTikbalang && hasBeenSeenByPlayer;
        agent.speed = shouldRun ? runSpeed : walkSpeed;
        agent.stoppingDistance = stoppingDistance;
        agent.isStopped = false;

        if (Time.time >= nextDestinationUpdate)
        {
            agent.SetDestination(player.position);
            nextDestinationUpdate = Time.time + destinationRefreshRate;
        }

        SetMovementAnimation(true, shouldRun);
        if (shouldRun)
            PlayChaseSound();
        else
            StopChaseSound();
    }

    // Used only for the first encounter. Flashlight teleports continue to use spawn points.
    private void TeleportInFrontOfPlayer()
    {
        if (player == null)
        {
            TeleportToSpawnPoint(true);
            return;
        }

        Transform view = Camera.main != null ? Camera.main.transform : player;
        Vector3 forward = Vector3.ProjectOnPlane(view.forward, Vector3.up).normalized;
        if (forward.sqrMagnitude < 0.01f)
            forward = player.forward;

        Vector3 destination = player.position + forward * firstEncounterFrontDistance;
        if (NavMesh.SamplePosition(destination, out NavMeshHit hit, spawnPointNavMeshSearchRadius, NavMesh.AllAreas))
            destination = hit.position;

        if (agent.enabled && agent.isOnNavMesh)
            agent.Warp(destination);
        else
            transform.position = destination;

        Vector3 lookDirection = player.position - transform.position;
        lookDirection.y = 0f;
        if (lookDirection.sqrMagnitude > 0.001f)
            transform.rotation = Quaternion.LookRotation(lookDirection);
    }

    public bool TeleportToSpawnPoint()
    {
        return TeleportToSpawnPoint(false);
    }

    private bool TeleportToSpawnPoint(bool bypassCooldown)
    {
        if ((!bypassCooldown && Time.time < nextTeleportTime) || teleportSpawnPoints == null || teleportSpawnPoints.Length == 0)
            return false;

        int spawnIndex = ChooseSpawnPointIndex();
        if (spawnIndex < 0) return false;

        Transform spawnPoint = teleportSpawnPoints[spawnIndex];
        if (spawnPoint == null) return false;

        Vector3 destination = spawnPoint.position;
        if (NavMesh.SamplePosition(destination, out NavMeshHit hit, spawnPointNavMeshSearchRadius, NavMesh.AllAreas))
            destination = hit.position;

        if (agent.enabled && agent.isOnNavMesh) agent.Warp(destination);
        else transform.position = destination;

        transform.rotation = spawnPoint.rotation;
        lastSpawnPointIndex = spawnIndex;

        // A teleport starts a fresh stalking phase. Tikbalang runs again only after
        // the player gets another clear view of him.
        hasBeenSeenByPlayer = false;
        StopChaseSound();
        nextTeleportTime = Time.time + teleportCooldown;
        return true;
    }

    private int ChooseSpawnPointIndex()
    {
        int validCount = 0;
        for (int i = 0; i < teleportSpawnPoints.Length; i++)
            if (teleportSpawnPoints[i] != null && i != lastSpawnPointIndex) validCount++;

        if (validCount == 0)
        {
            for (int i = 0; i < teleportSpawnPoints.Length; i++)
                if (teleportSpawnPoints[i] != null) return i;
            return -1;
        }

        int pick = Random.Range(0, validCount);
        for (int i = 0; i < teleportSpawnPoints.Length; i++)
        {
            if (teleportSpawnPoints[i] == null || i == lastSpawnPointIndex) continue;
            if (pick-- == 0) return i;
        }
        return -1;
    }

    private void SetDormantState()
    {
        if (agent.enabled) agent.isStopped = true;
        SetMovementAnimation(false);
    }

    private void FindPlayerIfNeeded()
    {
        if (player != null) return;
        GameObject playerObject = GameObject.FindGameObjectWithTag("Player");
        if (playerObject != null) player = playerObject.transform;
    }

    private void PlaceOnNavMeshIfNeeded()
    {
        if (!agent.isOnNavMesh && NavMesh.SamplePosition(transform.position, out NavMeshHit hit, 5f, NavMesh.AllAreas))
            agent.Warp(hit.position);
    }

    private bool CanPlayerSeeTikbalang()
    {
        Camera playerCamera = Camera.main;
        if (playerCamera == null)
            return false;

        Vector3 targetPosition = transform.position + Vector3.up * tikbalangFaceHeight;
        Vector3 viewportPosition = playerCamera.WorldToViewportPoint(targetPosition);
        if (viewportPosition.z <= 0f || viewportPosition.x < 0f || viewportPosition.x > 1f ||
            viewportPosition.y < 0f || viewportPosition.y > 1f)
            return false;

        Vector3 direction = targetPosition - playerCamera.transform.position;
        float distance = direction.magnitude;
        if (playerSightDistance > 0f && distance > playerSightDistance)
            return false;
        if (distance <= 0.001f)
            return true;

        if (Physics.Raycast(playerCamera.transform.position, direction / distance, out RaycastHit hit,
            distance, playerSightObstacleMask, QueryTriggerInteraction.Ignore))
        {
            return hit.collider.GetComponentInParent<TikbalangAI>() == this;
        }

        // If Tikbalang has no collider, the camera-frustum test is still a valid fallback.
        return true;
    }

    private void SetMovementAnimation(bool moving, bool running = false)
    {
        if (animator == null) return;
        bool walking = moving && !running;
        SetAnimatorBoolIfPresent("walking", walking);
        SetAnimatorBoolIfPresent("run", running);
        SetAnimatorBoolIfPresent("isWalking", walking);
        SetAnimatorBoolIfPresent("isRunning", running);
        SetAnimatorFloatIfPresent("Speed", running ? runSpeed : (walking ? walkSpeed : 0f));
    }

    private void PlayChaseSound()
    {
        if (isPlayingChaseSound || audioSource == null || chaseSound == null)
            return;

        audioSource.clip = chaseSound;
        audioSource.volume = chaseVolume;
        audioSource.loop = true;
        audioSource.Play();
        isPlayingChaseSound = true;
    }

    private void StopChaseSound()
    {
        if (!isPlayingChaseSound || audioSource == null)
            return;

        if (audioSource.clip == chaseSound)
            audioSource.Stop();
        isPlayingChaseSound = false;
    }

    private void SetAnimatorTriggerIfPresent(string parameterName)
    {
        if (animator == null) return;
        foreach (AnimatorControllerParameter parameter in animator.parameters)
            if (parameter.name == parameterName && parameter.type == AnimatorControllerParameterType.Trigger)
            {
                animator.SetTrigger(parameterName);
                return;
            }
    }

    private void SetAnimatorBoolIfPresent(string parameterName, bool value)
    {
        foreach (AnimatorControllerParameter parameter in animator.parameters)
            if (parameter.name == parameterName && parameter.type == AnimatorControllerParameterType.Bool)
            {
                animator.SetBool(parameterName, value);
                return;
            }
    }

    private void SetAnimatorFloatIfPresent(string parameterName, float value)
    {
        foreach (AnimatorControllerParameter parameter in animator.parameters)
            if (parameter.name == parameterName && parameter.type == AnimatorControllerParameterType.Float)
            {
                animator.SetFloat(parameterName, value);
                return;
            }
    }

    private void LogTriggerDebug(string message)
    {
        if (debugTrigger)
            Debug.Log($"[Tikbalang Trigger Debug] {message}", this);
    }}