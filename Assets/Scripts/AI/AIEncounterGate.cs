// Ensures only one enemy controls the player during a catch and Q&A sequence.
using UnityEngine;
using UnityEngine.AI;

public static class AIEncounterGate
{
    private static Object owner;

    public static bool IsActive => owner != null;

    public static bool IsBlocked(Object contender)
    {
        return owner != null && owner != contender;
    }

    public static bool IsOwnedBy(Object contender)
    {
        return owner != null && owner == contender;
    }

    public static bool TryBegin(Object contender)
    {
        if (contender == null || owner != null)
            return false;

        owner = contender;
        return true;
    }

    public static void End(Object contender)
    {
        if (owner == contender)
            owner = null;
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void Reset()
    {
        owner = null;
    }
}

// Pauses an enemy's navigation, animation, and audio while another enemy owns
// the encounter, then restores the states it had before the pause.
public sealed class AIEncounterPause
{
    private bool isPaused;
    private bool hasAgentState;
    private bool wasAgentStopped;
    private float animatorSpeed;
    private bool pausedAudio;

    public bool Update(Object contender, NavMeshAgent agent, Animator animator, AudioSource audioSource)
    {
        if (!AIEncounterGate.IsBlocked(contender))
        {
            Restore(agent, animator, audioSource);
            return false;
        }

        if (!isPaused)
        {
            isPaused = true;
            if (animator != null)
                animatorSpeed = animator.speed;
            if (audioSource != null && audioSource.isPlaying)
            {
                audioSource.Pause();
                pausedAudio = true;
            }
        }

        if (agent != null && agent.enabled && agent.isOnNavMesh)
        {
            if (!hasAgentState)
            {
                wasAgentStopped = agent.isStopped;
                hasAgentState = true;
            }
            agent.isStopped = true;
        }

        if (animator != null)
            animator.speed = 0f;

        return true;
    }

    public void Restore(NavMeshAgent agent, Animator animator, AudioSource audioSource)
    {
        if (!isPaused)
            return;

        if (hasAgentState && agent != null && agent.enabled && agent.isOnNavMesh)
            agent.isStopped = wasAgentStopped;
        if (animator != null)
            animator.speed = animatorSpeed;
        if (pausedAudio && audioSource != null)
            audioSource.UnPause();

        isPaused = false;
        hasAgentState = false;
        pausedAudio = false;
    }
}
