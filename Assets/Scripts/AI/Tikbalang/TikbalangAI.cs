// PURPOSE: Controls Tikbalang detection, movement, chase audio, door handling, teleporting, Q&A, and jumpscares.
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.UI;

[RequireComponent(typeof(NavMeshAgent))]
public partial class TikbalangAI : MonoBehaviour
{
    [Header("Movement Debug")]
    [Tooltip("Writes one Console message when Tikbalang stops or becomes stuck while the player is walking or running.")]
    public bool logMovementStops = true;
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

    [Header("Intro Generator Objective Popup")]
    [Tooltip("Shows the generator objective after the first Tikbalang jumpscare ends.")]
    public bool showGeneratorObjectiveAfterIntro = true;
    [TextArea(2, 4)] public string generatorObjectiveMessage = "The lights are out! Find the Generator and fix it as soon as possible.";
    [Min(0.1f)] public float generatorObjectivePopupDuration = 6f;
    public Color generatorObjectiveColor = new Color(1f, 0.82f, 0.25f);

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

    [Header("Door Interaction")]
    [Tooltip("How far ahead Tikbalang checks for a door while chasing.")]
    [Min(0.1f)] public float doorDetectionDistance = 1.75f;
    [Tooltip("Height above Tikbalang's position where the forward door check begins.")]
    [Min(0f)] public float doorDetectionHeight = 1f;
    [Tooltip("Layers that can contain doors. Include the door collider's layer.")]
    public LayerMask doorDetectionMask = ~0;
    [Tooltip("How long Tikbalang waits after a door finishes opening.")]
    [Min(0f)] public float doorPauseAfterOpening = 2f;

    [Header("Chase Audio")]
    [Tooltip("Looped only while Tikbalang is running after the player sees it.")]
    public AudioClip chaseSound;
    [Range(0f, 1f)] public float chaseVolume = 0.8f;

    [Header("Walking Audio")]
    [Tooltip("Optional separate Audio Source for Tikbalang's walking sound. Leave empty to create one automatically at runtime.")]
    public AudioSource walkingAudioSource;
    [Tooltip("Looped while Tikbalang is walking, including when generator lights make him walk during a chase.")]
    public AudioClip walkingSound;
    [Range(0f, 1f)] public float walkingVolume = 0.65f;
    [Min(1f)] public float walkingMaxDistance = 12f;

    [Header("Player Sight Run Trigger")]
    [Tooltip("Once the player camera gets a clear view of Tikbalang, he keeps running for the rest of that chase.")]
    public bool runWhenPlayerSeesTikbalang = true;
    [Tooltip("0 means unlimited distance. A wall or other collider blocks this sight check.")]
    [Min(0f)] public float playerSightDistance = 0f;
    public LayerMask playerSightObstacleMask = ~0;

    [Header("Generator Light Run Block")]
    [Tooltip("Tikbalang walks instead of running while any assigned generator light is on.")]
    public bool preventRunningWhenGeneratorLightsOn = true;
    [Tooltip("Leave empty to find the active Generator Key Slot automatically.")]
    public GeneratorKeySlot generatorKeySlot;

    [Header("Flashlight Teleport")]
    [Min(0f)] public float teleportCooldown = 1f;
    [Min(0.1f)] public float spawnPointNavMeshSearchRadius = 3f;

    [Header("Chase NavMesh")]
    [Tooltip("How far from the player Tikbalang searches for a walkable NavMesh point.")]
    [Min(1f)] public float playerChaseNavMeshSearchRadius = 12f;

    [Header("Painted Tree Avoidance")]
    [Tooltip("Makes Tikbalang steer around trees painted on Unity Terrain, even when those trees are not baked into the NavMesh.")]
    public bool avoidPaintedTrees = true;
    [Tooltip("How far ahead Tikbalang checks for a painted tree that blocks his route.")]
    [Min(1f)] public float paintedTreeLookAheadDistance = 8f;
    [Tooltip("Extra space Tikbalang leaves around the trunk while moving past a painted tree.")]
    [Min(0.1f)] public float paintedTreeClearance = 1.5f;

    private NavMeshAgent agent;
    private float nextDestinationUpdate;
    private float nextDoorCheckTime;
    private float doorPauseUntil;
    private float nextTeleportTime;
    private int lastSpawnPointIndex = -1;
    private bool hasAwakened;
    // Reports whether Tikbalang first encounter has begun.
    public bool HasFirstEncounterStarted => hasAwakened;
    // Reports whether Tikbalang is actively chasing outside a jumpscare sequence.
    public bool IsChasingPlayer => hasAwakened && !isFirstEncounterPlaying && !isCatchSequencePlaying;
    private bool isFirstEncounterPlaying;
    private bool isCatchSequencePlaying;
    private bool isPlayingChaseSound;
    private bool isPlayingWalkingSound;
    private bool hasBeenSeenByPlayer;
    private bool showGeneratorObjectivePopup;
    private float generatorObjectivePopupTimer;
    private DoorInteraction doorBlockingPath;
    private float nextCatchTime;
    private Quaternion cameraRotationBeforeCatch;
    private PlayerController playerController;
    private QnAEntry currentQuestion;
    private bool waitingForAnswer;
    private bool answerReceived;
    private bool isQuestionPanelOpen;
    private bool wasPlayerControllerEnabled;
    private readonly System.Collections.Generic.List<Button> qnaButtons = new System.Collections.Generic.List<Button>();
    private string lastMovementStopReason;
    private readonly System.Collections.Generic.List<PaintedTreeObstacle> paintedTrees = new System.Collections.Generic.List<PaintedTreeObstacle>();

    private struct PaintedTreeObstacle
    {
        public Vector3 position;
        public float radius;

        public PaintedTreeObstacle(Vector3 position, float radius)
        {
            this.position = position;
            this.radius = radius;
        }
    }

    // Gets the NavMesh agent and finds the player, animator, and audio source if needed.
    private void Awake()
    {
        agent = GetComponent<NavMeshAgent>();
        if (animator == null) animator = GetComponentInChildren<Animator>();
        if (audioSource == null) audioSource = GetComponent<AudioSource>();
        EnsureWalkingAudioSource();
        if (generatorKeySlot == null) generatorKeySlot = FindObjectOfType<GeneratorKeySlot>();
        FindPlayerIfNeeded();
    }

    // Sets movement defaults, prepares the Q and A panel, and leaves Tikbalang dormant.
    private void Start()
    {
        agent.speed = walkSpeed;
        agent.stoppingDistance = stoppingDistance;
        playerController = player != null ? player.GetComponent<PlayerController>() : null;
        FindAndPrepareQnAPanel();
        if (jumpscareCamera == null && Camera.main != null) jumpscareCamera = Camera.main.transform;
        PlaceOnNavMeshIfNeeded();
        CachePaintedTrees();
        SetDormantState();
    }

    // Waits for the first encounter, checks whether Tikbalang caught the player, and chases.
    private void Update()
    {
        FindPlayerIfNeeded();
        UpdateGeneratorObjectivePopup();
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

    // Keeps the cursor visible and unlocked while the Q and A panel is open.
    private void LateUpdate()
    {
        // PlayerController and other gameplay scripts normally lock the cursor.
        // Keep Q&A clickable regardless of script execution order.
        if (isQuestionPanelOpen)
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }

        CheckMovementStopDebug();
    }
    // Called by TikbalangJumpscareTrigger when the player enters its detector collider.
    // Rejects invalid requests and starts the first encounter sequence when valid.
    private void ResumeChaseAfterEncounter()
    {
        if (agent == null || !agent.enabled || !agent.isOnNavMesh || player == null)
        {
            return;
        }

        nextDestinationUpdate = 0f;
        agent.isStopped = false;
        SetChaseDestination();
        SetMovementAnimation(true);
    }
    // Chooses walking or running, handles doors, updates the chase destination, and audio.
    private void SetDormantState()
    {
        if (agent.enabled) agent.isStopped = true;
        SetMovementAnimation(false);
    }

    // Finds and stores the GameObject tagged Player if no player was assigned.
    private void FindPlayerIfNeeded()
    {
        if (player != null) return;
        GameObject playerObject = GameObject.FindGameObjectWithTag("Player");
        if (playerObject != null) player = playerObject.transform;
    }

    // Warps the agent to the nearest walkable location if it starts off the NavMesh.
    private void PlaceOnNavMeshIfNeeded()
    {
        if (!agent.isOnNavMesh && NavMesh.SamplePosition(transform.position, out NavMeshHit hit, 5f, NavMesh.AllAreas))
            agent.Warp(hit.position);
    }

    // Checks whether the player camera can see Tikbalang within the configured view and range.
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

    // Sets available animator parameters for walking, running, and movement speed.
    private void ShowGeneratorObjectivePopup()
    {
        if (!showGeneratorObjectiveAfterIntro || string.IsNullOrWhiteSpace(generatorObjectiveMessage))
            return;

        showGeneratorObjectivePopup = true;
        generatorObjectivePopupTimer = Mathf.Max(0.1f, generatorObjectivePopupDuration);
    }

    private void UpdateGeneratorObjectivePopup()
    {
        if (!showGeneratorObjectivePopup)
            return;

        generatorObjectivePopupTimer -= Time.deltaTime;
        if (generatorObjectivePopupTimer <= 0f)
            showGeneratorObjectivePopup = false;
    }

    private void OnGUI()
    {
        if (!showGeneratorObjectivePopup)
            return;

        GUIStyle shadow = new GUIStyle
        {
            fontSize = 25,
            alignment = TextAnchor.MiddleCenter,
            wordWrap = true
        };
        shadow.normal.textColor = Color.black;

        GUIStyle text = new GUIStyle(shadow);
        text.normal.textColor = generatorObjectiveColor;

        Rect rect = new Rect(Screen.width * 0.5f - 320f, Screen.height * 0.18f, 640f, 80f);
        string localizedMessage = GameplayLocalization.TranslateSubtitle(generatorObjectiveMessage);
        GUI.Label(new Rect(rect.x + 2f, rect.y + 2f, rect.width, rect.height), localizedMessage, shadow);
        GUI.Label(rect, localizedMessage, text);
    }

    // Sets an animator trigger only if a matching trigger parameter exists.
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

    // Sets an animator bool only if a matching bool parameter exists.
    private void SetAnimatorBoolIfPresent(string parameterName, bool value)
    {
        foreach (AnimatorControllerParameter parameter in animator.parameters)
            if (parameter.name == parameterName && parameter.type == AnimatorControllerParameterType.Bool)
            {
                animator.SetBool(parameterName, value);
                return;
            }
    }

    // Sets an animator float only if a matching float parameter exists.
    private void SetAnimatorFloatIfPresent(string parameterName, float value)
    {
        foreach (AnimatorControllerParameter parameter in animator.parameters)
            if (parameter.name == parameterName && parameter.type == AnimatorControllerParameterType.Float)
            {
                animator.SetFloat(parameterName, value);
                return;
            }
    }

    private void CheckMovementStopDebug()
    {
        if (!logMovementStops || !hasAwakened || isCatchSequencePlaying || agent == null)
            return;

        if (!agent.enabled || !agent.isOnNavMesh)
        {
            ReportMovementStop("The NavMesh Agent is disabled or Tikbalang is outside the NavMesh.");
            return;
        }

        // This also catches a stop caused by another script, a blocked NavMesh path,
        // or an obstacle. It reports only once per stop, rather than every frame.
        bool isStalledOnPath = agent.hasPath && !agent.pathPending &&
                               agent.remainingDistance > agent.stoppingDistance + 0.15f &&
                               agent.velocity.sqrMagnitude < 0.0025f;
        if (agent.isStopped && string.IsNullOrEmpty(lastMovementStopReason))
            ReportMovementStop("NavMesh Agent isStopped is true. Check the earlier Tikbalang movement message for the reason.");
        else if (isStalledOnPath)
            ReportMovementStop("Tikbalang has a path but is not moving. An obstacle, an invalid NavMesh route, or another script may be blocking the agent.");
        else
            lastMovementStopReason = null;
    }

    private void ReportMovementStop(string reason)
    {
        if (!logMovementStops || lastMovementStopReason == reason)
            return;

        lastMovementStopReason = reason;
        bool playerIsMoving = Mathf.Abs(Input.GetAxisRaw("Horizontal")) > 0.01f ||
                              Mathf.Abs(Input.GetAxisRaw("Vertical")) > 0.01f;
        string playerState = playerIsMoving
            ? (playerController != null && playerController.IsSprinting ? "running" : "walking")
            : "standing still";
        Debug.Log($"[Tikbalang Movement Debug] {reason} Player is {playerState}. " +
                  $"remainingDistance={agent.remainingDistance:F2}, velocity={agent.velocity.magnitude:F2}, hasPath={agent.hasPath}.", this);
    }
}
