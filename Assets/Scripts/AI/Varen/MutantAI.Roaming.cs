// Roaming, patrol reset, and target-loss behavior for Varen.
using UnityEngine;
using UnityEngine.AI;

public partial class MutantAI
{
    void UpdateRoam(float dist, bool playerVisible)
    {
        agent.speed = roamSpeed;

        // Switch to chase if player in range AND visible (not in bush)
        if (dist <= detectionRange && playerVisible)
        {
            StopChaseSound();
            SetAnimation(false, true);
            currentState = State.Chase;
            return;
        }

        // Walking animation
        SetAnimation(true, false);
        StopChaseSound();

        if (isWaiting)
        {
            SetAnimation(false, false);
            roamTimer += Time.deltaTime;
            if (roamTimer >= roamWaitTime)
            {
                isWaiting = false;
                roamTimer = 0f;
                SetNewRoamDestination();
            }
            return;
        }

        if (!agent.pathPending && agent.remainingDistance < 0.5f)
        {
            isWaiting = true;
            roamTimer = 0f;
            roamWaitTime = Random.Range(roamWaitMin, roamWaitMax);
        }
    }

    void SetNewRoamDestination()
    {
        for (int i = 0; i < 10; i++)
        {
            Vector3 randomDir = Random.insideUnitSphere * roamRadius;
            randomDir += startPosition;

            NavMeshHit hit;
            if (NavMesh.SamplePosition(randomDir, out hit,
                roamRadius, NavMesh.AllAreas))
            {
                agent.SetDestination(hit.position);
                return;
            }
        }
    }

}