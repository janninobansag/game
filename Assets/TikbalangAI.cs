using System.Collections;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.UI;
using TMPro;

[RequireComponent(typeof(NavMeshAgent))]
public class TikbalangAI : MonoBehaviour
{
    [Header("References")]
    public Transform player;
    public Animator animator;
    public AudioSource audioSource;
    [Tooltip("Assign the spawn point children from TELEPORTER FOR ENEMY here.")]
    public Transform[] teleportSpawnPoints;

    [Header("First Encounter")]
    [Tooltip("Tikbalang stays still until the player enters this radius for the first time.")]
    [Min(0.1f)] public float awakeningRadius = 10f;
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
    [Tooltip("Prevents an immediate second catch after Tikbalang teleports away.")]
    [Min(0f)] public float catchCooldown = 3f;
    [Tooltip("Optional. Leave empty to use the Main Camera.")]
    public Transform jumpscareCamera;
    [Min(0f)] public float cameraLookDuration = 0.3f;
    [Min(0f)] public float cameraReturnDuration = 0.25f;
    [Min(0f)] public float tikbalangFaceHeight = 1.5f;
    [Header("Chase After First Encounter")]
    [Min(0.1f)] public float walkSpeed = 3.25f;
    [Min(0f)] public float stoppingDistance = 1.2f;
    [Min(0.02f)] public float destinationRefreshRate = 0.1f;

    [Header("Flashlight Teleport")]
    [Min(0f)] public float teleportCooldown = 1f;
    [Min(0.1f)] public float spawnPointNavMeshSearchRadius = 3f;

    private NavMeshAgent agent;
    private float nextDestinationUpdate;
    private float nextTeleportTime;
    private int lastSpawnPointIndex = -1;
    private bool hasAwakened;
    private bool isFirstEncounterPlaying;
    private bool isCatchSequencePlaying;
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

        // First time the player finds Tikbalang: reveal, shout, teleport, then chase forever.
        if (!hasAwakened)
        {
            if (!isFirstEncounterPlaying && Vector3.Distance(transform.position, player.position) <= awakeningRadius)
                StartCoroutine(PlayFirstEncounter());
            return;
        }

        if (isCatchSequencePlaying)
            return;

        if (Time.time >= nextCatchTime && Vector3.Distance(transform.position, player.position) <= catchRange)
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
    private IEnumerator PlayFirstEncounter()
    {
        isFirstEncounterPlaying = true;
        agent.isStopped = true;
        SetMovementAnimation(false);

        // First reveal: appear in front of the player, but do not start the Q&A yet.
        TeleportInFrontOfPlayer();
        // The first sighting only wakes Tikbalang. Jumpscares happen when it catches the player.
        if (audioSource != null && discoveryShout != null)
            audioSource.PlayOneShot(discoveryShout);

        yield return new WaitForSeconds(0.15f);
        hasAwakened = true;
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
            Debug.LogWarning("[Tikbalang Q&A] QnAPanel was not found. Assign it in the Tikbalang AI Inspector.", this);
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
            Debug.LogWarning("[Tikbalang Q&A] A question needs four answer options.", this);
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
            PlayerHealth health = player != null ? player.GetComponent<PlayerHealth>() : null;
            if (health != null)
                health.TakeDamage(wrongAnswerDamage);
        }

        yield return new WaitForSecondsRealtime(0.8f);
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
    private void ChasePlayer()
    {
        if (!agent.enabled || !agent.isOnNavMesh)
        {
            SetMovementAnimation(false);
            return;
        }

        agent.speed = walkSpeed;
        agent.stoppingDistance = stoppingDistance;
        agent.isStopped = false;

        if (Time.time >= nextDestinationUpdate)
        {
            agent.SetDestination(player.position);
            nextDestinationUpdate = Time.time + destinationRefreshRate;
        }

        // Tikbalang uses the walk animation while chasing, never the run animation.
        SetMovementAnimation(true);
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

    private void SetMovementAnimation(bool walking)
    {
        if (animator == null) return;
        SetAnimatorBoolIfPresent("walking", walking);
        SetAnimatorBoolIfPresent("run", false);
        SetAnimatorBoolIfPresent("isWalking", walking);
        SetAnimatorBoolIfPresent("isRunning", false);
        SetAnimatorFloatIfPresent("Speed", walking ? walkSpeed : 0f);
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
}