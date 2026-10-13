// Spawn-point and first-encounter teleport behavior for Tikbalang.
using UnityEngine;
using UnityEngine.AI;

public partial class TikbalangAI
{
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

    // Teleports Tikbalang to a spawn point, with optional cooldown bypass.
    public bool TeleportToSpawnPoint()
    {
        return TeleportToSpawnPoint(false);
    }

    // Teleports Tikbalang to a spawn point, with optional cooldown bypass.
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

    // Picks a valid spawn point, avoiding the previous point when another is available.
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

    // Stops Tikbalang and disables movement animation before the first encounter.
}
