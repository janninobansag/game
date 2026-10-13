// Chase routing, doors, and painted-tree avoidance for Tikbalang.
using UnityEngine;
using UnityEngine.AI;

public partial class TikbalangAI
{
    private void ChasePlayer()
    {
        if (!agent.enabled || !agent.isOnNavMesh)
        {
            ReportMovementStop("The NavMesh Agent is disabled or Tikbalang is outside the NavMesh.");
            SetMovementAnimation(false);
            return;
        }

        if (runWhenPlayerSeesTikbalang && !hasBeenSeenByPlayer && CanPlayerSeeTikbalang())
            hasBeenSeenByPlayer = true;

        // Seeing Tikbalang once starts a persistent run; looking away does not cancel it.
        // Generator light temporarily makes Tikbalang walk, even after he has been seen.
        bool generatorLightsOn = preventRunningWhenGeneratorLightsOn &&
                                 generatorKeySlot != null &&
                                 generatorKeySlot.AreAnyGeneratorLightsOn;
        bool shouldRun = runWhenPlayerSeesTikbalang && hasBeenSeenByPlayer && !generatorLightsOn;
        agent.speed = shouldRun ? runSpeed : walkSpeed;
        agent.stoppingDistance = stoppingDistance;

        if (HandleDoorAhead())
        {
            SetMovementAnimation(false);
            StopChaseSound();
            return;
        }

        agent.isStopped = false;

        if (Time.time >= nextDestinationUpdate)
        {
            SetChaseDestination();
            nextDestinationUpdate = Time.time + destinationRefreshRate;
        }

        SetMovementAnimation(true, shouldRun);
        if (shouldRun)
            PlayChaseSound();
        else
            StopChaseSound();
    }

    // Targets the closest walkable point near the player rather than the player's
    // exact transform position, which can sit slightly outside the baked NavMesh.
    private bool SetChaseDestination()
    {
        if (player == null || agent == null || !agent.enabled || !agent.isOnNavMesh)
            return false;

        float searchRadius = Mathf.Max(1f, playerChaseNavMeshSearchRadius);
        if (!NavMesh.SamplePosition(player.position, out NavMeshHit playerNavMeshPoint,
                                    searchRadius, agent.areaMask))
        {
            agent.ResetPath();
            SetMovementAnimation(false);
            ReportMovementStop("No NavMesh point was found near the player, so Tikbalang cannot calculate a chase route.");
            return false;
        }

        NavMeshPath chasePath = new NavMeshPath();
        if (!agent.CalculatePath(playerNavMeshPoint.position, chasePath) ||
            chasePath.status != NavMeshPathStatus.PathComplete)
        {
            agent.ResetPath();
            SetMovementAnimation(false);
            ReportMovementStop("The player and Tikbalang are on disconnected NavMesh areas. Bake or connect the NavMesh between their locations.");
            return false;
        }

        if (avoidPaintedTrees && TrySetPaintedTreeDetour(playerNavMeshPoint.position))
            return true;

        agent.SetPath(chasePath);
        return true;
    }

    // Reads Terrain tree instances once because painted trees are not consistently available
    // as ordinary scene objects or NavMesh obstacles at runtime.
    [ContextMenu("Refresh Painted Tree Cache")]
    public void CachePaintedTrees()
    {
        paintedTrees.Clear();

        Terrain[] terrains = Terrain.activeTerrains;
        for (int terrainIndex = 0; terrainIndex < terrains.Length; terrainIndex++)
        {
            Terrain terrain = terrains[terrainIndex];
            if (terrain == null || terrain.terrainData == null)
                continue;

            TerrainData terrainData = terrain.terrainData;
            TreeInstance[] instances = terrainData.treeInstances;
            for (int treeIndex = 0; treeIndex < instances.Length; treeIndex++)
            {
                TreeInstance instance = instances[treeIndex];
                Vector3 localPosition = Vector3.Scale(instance.position, terrainData.size);
                Vector3 worldPosition = terrain.transform.TransformPoint(localPosition);
                float radius = Mathf.Max(0.5f, instance.widthScale * 0.5f);
                paintedTrees.Add(new PaintedTreeObstacle(worldPosition, radius));
            }
        }
    }

    // Finds the first painted tree in Tikbalang's direct route and uses a reachable side.
    private bool TrySetPaintedTreeDetour(Vector3 destination)
    {
        if (paintedTrees.Count == 0)
            return false;

        Vector3 start = transform.position;
        start.y = 0f;
        Vector3 end = destination;
        end.y = 0f;
        Vector3 route = end - start;
        float routeLength = route.magnitude;
        if (routeLength < 0.1f)
            return false;

        Vector3 routeDirection = route / routeLength;
        float lookAhead = Mathf.Min(routeLength, Mathf.Max(1f, paintedTreeLookAheadDistance));
        PaintedTreeObstacle closestTree = default;
        float closestDistance = float.MaxValue;
        bool foundBlockingTree = false;

        for (int i = 0; i < paintedTrees.Count; i++)
        {
            PaintedTreeObstacle tree = paintedTrees[i];
            Vector3 toTree = tree.position - start;
            toTree.y = 0f;
            float alongRoute = Vector3.Dot(toTree, routeDirection);
            if (alongRoute <= 0f || alongRoute > lookAhead)
                continue;

            Vector3 closestPoint = start + routeDirection * alongRoute;
            float clearance = tree.radius + paintedTreeClearance;
            Vector3 flatTreePosition = tree.position;
            flatTreePosition.y = 0f;
            float squaredDistance = (flatTreePosition - closestPoint).sqrMagnitude;
            if (squaredDistance > clearance * clearance || alongRoute >= closestDistance)
                continue;

            closestTree = tree;
            closestDistance = alongRoute;
            foundBlockingTree = true;
        }

        if (!foundBlockingTree)
            return false;

        Vector3 side = Vector3.Cross(Vector3.up, routeDirection).normalized;
        float detourDistance = closestTree.radius + paintedTreeClearance;
        Vector3 left = closestTree.position + side * detourDistance;
        Vector3 right = closestTree.position - side * detourDistance;

        return TrySetDetourPath(left) || TrySetDetourPath(right);
    }

    // Sets a complete NavMesh route to a candidate point beside a painted tree.
    private bool TrySetDetourPath(Vector3 candidate)
    {
        if (!NavMesh.SamplePosition(candidate, out NavMeshHit detourPoint, paintedTreeClearance + 1f, agent.areaMask))
            return false;

        NavMeshPath detourPath = new NavMeshPath();
        if (!agent.CalculatePath(detourPoint.position, detourPath) || detourPath.status != NavMeshPathStatus.PathComplete)
            return false;

        agent.SetPath(detourPath);
        return true;
    }

    // Detects a door ahead, asks it to open, and pauses Tikbalang while it opens.
    private bool HandleDoorAhead()
    {
        if (Time.time < doorPauseUntil)
        {
            agent.isStopped = true;
            ReportMovementStop("Tikbalang is waiting after opening a door.");
            return true;
        }

        if (doorBlockingPath != null)
        {
            if (doorBlockingPath.TryOpenForEnemy())
            {
                doorBlockingPath = null;
                doorPauseUntil = Time.time + doorPauseAfterOpening;
                agent.isStopped = true;
                nextDestinationUpdate = 0f;
                ReportMovementStop("Tikbalang opened a door and is waiting for it to finish moving.");
                return true;
            }

            agent.isStopped = true;
            ReportMovementStop("A closed or locked door is still blocking Tikbalang's path.");
            return true;
        }

        if (Time.time < nextDoorCheckTime)
            return false;

        nextDoorCheckTime = Time.time + 0.15f;
        Vector3 origin = transform.position + Vector3.up * doorDetectionHeight;
        if (!Physics.Raycast(origin, transform.forward, out RaycastHit hit,
                doorDetectionDistance, doorDetectionMask, QueryTriggerInteraction.Ignore))
            return false;

        DoorInteraction door = hit.collider.GetComponentInParent<DoorInteraction>();
        if (door == null || door.TryOpenForEnemy())
            return false;

        doorBlockingPath = door;
        agent.isStopped = true;
        ReportMovementStop("A door was detected directly in Tikbalang's path.");
        return true;
    }

    // Used only for the first encounter. Flashlight teleports continue to use spawn points.
    // Places Tikbalang on the NavMesh in front of the player and turns him toward them.
}
