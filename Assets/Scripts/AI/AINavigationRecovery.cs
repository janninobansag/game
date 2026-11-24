using UnityEngine;
using UnityEngine.AI;

// Shared routing checks. Call only while an enemy is meant to patrol or chase;
// encounter pauses, cutscenes and dormant states remain owned by its controller.
public sealed class AINavigationRecovery
{
    // This helper is constructed by MonoBehaviour field initializers. Creating
    // a native NavMeshPath there aborts Unity's component initialization.
    private NavMeshPath path;
    private Vector3 progressPosition;
    private float stalledTime;
    private float nextRouteCheck;
    private float nextNavMeshCheck;
    private bool trackingProgress;

    public void ResetProgress()
    {
        trackingProgress = false;
        stalledTime = 0f;
    }

    public bool EnsureOnNavMesh(NavMeshAgent agent)
    {
        if (agent == null || !agent.enabled || !agent.gameObject.activeInHierarchy)
            return false;
        if (agent.isOnNavMesh)
            return true;
        if (Time.time < nextNavMeshCheck)
            return false;

        nextNavMeshCheck = Time.time + 1f;
        NavMeshQueryFilter filter = new NavMeshQueryFilter
        {
            agentTypeID = agent.agentTypeID,
            areaMask = agent.areaMask
        };
        if (!NavMesh.SamplePosition(agent.transform.position, out NavMeshHit hit, 2f, filter))
            return false;
        ResetProgress();
        return agent.Warp(hit.position);
    }

    public bool ShouldRepath(NavMeshAgent agent)
    {
        if (!EnsureOnNavMesh(agent) || Time.deltaTime <= 0f)
            return false;

        if (!trackingProgress || (agent.transform.position - progressPosition).sqrMagnitude >= 0.04f)
        {
            progressPosition = agent.transform.position;
            trackingProgress = true;
            stalledTime = 0f;
        }
        else
        {
            stalledTime += Time.deltaTime;
        }

        if (Time.time < nextRouteCheck)
            return false;
        bool routeFailed = !agent.pathPending && (!agent.hasPath ||
            agent.pathStatus != NavMeshPathStatus.PathComplete ||
            agent.remainingDistance <= Mathf.Max(0.5f, agent.stoppingDistance + 0.2f));
        if (!routeFailed && stalledTime < 2f)
            return false;

        nextRouteCheck = Time.time + 1f;
        ResetProgress();
        return true;
    }

    public bool TrySetDestination(NavMeshAgent agent, Vector3 destination)
    {
        if (!EnsureOnNavMesh(agent))
            return false;
        // Navigation calls run after initialization on Unity's main thread.
        if (path == null)
            path = new NavMeshPath();
        NavMeshQueryFilter filter = new NavMeshQueryFilter
        {
            agentTypeID = agent.agentTypeID,
            areaMask = agent.areaMask
        };
        if (!NavMesh.SamplePosition(destination, out NavMeshHit hit, 2f, filter) ||
            !agent.CalculatePath(hit.position, path) || path.status != NavMeshPathStatus.PathComplete)
            return false;
        agent.isStopped = false;
        return agent.SetPath(path);
    }

    public bool TrySetRoamDestination(NavMeshAgent agent, Vector3 centre, float radius)
    {
        if (!EnsureOnNavMesh(agent))
            return false;
        float searchRadius = Mathf.Max(3f, radius);
        for (int attempt = 0; attempt < 12; attempt++)
        {
            Vector2 offset = Random.insideUnitCircle * searchRadius;
            Vector3 candidate = centre + new Vector3(offset.x, 0f, offset.y);
            if ((candidate - agent.transform.position).sqrMagnitude < 4f)
                continue;
            if (TrySetDestination(agent, candidate) &&
                (agent.destination - agent.transform.position).sqrMagnitude >= 1f)
            {
                ResetProgress();
                return true;
            }
        }
        return false;
    }
}
