using System.Collections;
using UnityEngine;
using UnityEngine.AI;

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

        ChasePlayer();
    }

    private IEnumerator PlayFirstEncounter()
    {
        isFirstEncounterPlaying = true;
        agent.isStopped = true;

        // Appear in front of the player first, then begin the animation and shout
        // in the same frame. It no longer teleports after the jumpscare.
        TeleportInFrontOfPlayer();
        SetMovementAnimation(false);
        SetAnimatorTriggerIfPresent("jumpscare");

        if (audioSource != null && discoveryShout != null)
            audioSource.PlayOneShot(discoveryShout);

        yield return new WaitForSeconds(jumpscareBeforeTeleportDelay);

        // After the reveal animation, disappear to a random enemy spawn point
        // before beginning the permanent chase.
        TeleportToSpawnPoint(true);
        hasAwakened = true;
        isFirstEncounterPlaying = false;
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