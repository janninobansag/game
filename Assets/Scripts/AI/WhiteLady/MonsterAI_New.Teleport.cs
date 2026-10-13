// Teleport spawn selection and NavMesh recovery for MonsterAI_New.
using UnityEngine;
using UnityEngine.AI;
using System.Collections;
using System.Collections.Generic;

public partial class MonsterAI_New
{
    [Header("White Lady Teleport Spawns")]
    [Tooltip("Assign the Whitelady spawn empty objects here. If left empty, child objects named 'Whitelady spawn' are found automatically.")]
    public Transform[] whiteLadyTeleportSpawns;
    [Min(0f)] public float spawnPointNavMeshSearchRadius = 3f;


    void TeleportMonsterAway()
    {
        Transform spawnPoint = GetNextTeleportSpawn();
        Vector3 destination = spawnPoint != null ? spawnPoint.position : startPosition;
        int areaMask = agent != null ? agent.areaMask : NavMesh.AllAreas;

        if (NavMesh.SamplePosition(destination, out NavMeshHit hit, spawnPointNavMeshSearchRadius, areaMask))
            destination = hit.position;

        if (agent != null)
        {
            // PerformJumpscare disables the agent. Re-enable it before teleporting so
            // Warp reconnects White Lady to the NavMesh instead of only moving Transform.
            if (!agent.enabled)
                agent.enabled = true;

            if (agent.isOnNavMesh)
            {
                agent.Warp(destination);
                agent.ResetPath();
                agent.isStopped = false;
                agent.speed = walkSpeed;
            }
            else
            {
                transform.position = destination;
            }
        }
        else
        {
            transform.position = destination;
        }

        if (spawnPoint != null)
            transform.rotation = spawnPoint.rotation;

        currentState = State.Patrol;
        isJumpscaring = false;
        isWaitingForQnA = false;
        jumpscareTimer = Mathf.Max(jumpscareTimer, jumpscareCooldown);
        hasTriggeredJumpscare = false;
        isChasing = false;
        isIdle = false;
        playerInSight = false;

        StartCoroutine(ResumePatrolAfterTeleport());
    }

    // Waits one frame after a Transform fallback so Unity can attach the NavMeshAgent,
    // then assigns a fresh patrol route. This prevents a teleport from leaving her stuck.
    private IEnumerator ResumePatrolAfterTeleport()
    {
        yield return null;

        if (agent == null)
            yield break;

        if (!agent.enabled)
            agent.enabled = true;

        if (!agent.isOnNavMesh &&
            NavMesh.SamplePosition(transform.position, out NavMeshHit nearestPoint,
                                   spawnPointNavMeshSearchRadius, agent.areaMask))
        {
            agent.Warp(nearestPoint.position);
        }

        if (!agent.isOnNavMesh)
        {
            Debug.LogWarning("White Lady could not resume after teleport because no NavMesh point was found near the selected spawn.", this);
            yield break;
        }

        agent.ResetPath();
        agent.isStopped = false;
        agent.speed = walkSpeed;
        SetNewPatrolTarget();
    }

    private Transform GetNextTeleportSpawn()
    {
        PopulateTeleportSpawnsIfNeeded();

        if (whiteLadyTeleportSpawns == null || whiteLadyTeleportSpawns.Length == 0)
        {
            Debug.LogWarning("White Lady has no teleport spawns assigned or found. Teleporting to her start position.", this);
            return null;
        }

        List<Transform> validSpawns = new List<Transform>();
        foreach (Transform spawn in whiteLadyTeleportSpawns)
        {
            if (spawn != null)
                validSpawns.Add(spawn);
        }

        if (validSpawns.Count == 0)
            return null;

        return validSpawns[Random.Range(0, validSpawns.Count)];
    }

    private void PopulateTeleportSpawnsIfNeeded()
    {
        if (whiteLadyTeleportSpawns != null && whiteLadyTeleportSpawns.Length > 0)
            return;

        List<Transform> foundSpawns = new List<Transform>();
        foreach (Transform child in GetComponentsInChildren<Transform>(true))
        {
            if (child != transform && child.name.StartsWith("Whitelady spawn", System.StringComparison.OrdinalIgnoreCase))
                foundSpawns.Add(child);
        }

        if (foundSpawns.Count > 0)
            whiteLadyTeleportSpawns = foundSpawns.ToArray();
    }

}