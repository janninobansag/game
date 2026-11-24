// Roaming, patrol reset, and target-loss behavior for Varen.
using UnityEngine;
using UnityEngine.AI;

public partial class MutantAI
{
    private readonly AINavigationRecovery navigationRecovery = new AINavigationRecovery();
    private float nextNavigationDestinationUpdate;
    private float navigationFallbackUntil;

    void UpdateRoam(float dist, bool playerVisible)
    {
        agent.speed = roamSpeed;
        agent.isStopped = false;

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

        if (navigationRecovery.ShouldRepath(agent))
            SetNewRoamDestination();
    }

    void SetNewRoamDestination()
    {
        if (!navigationRecovery.TrySetRoamDestination(agent, startPosition, roamRadius))
            navigationRecovery.TrySetRoamDestination(agent, transform.position, 8f);
    }

}
