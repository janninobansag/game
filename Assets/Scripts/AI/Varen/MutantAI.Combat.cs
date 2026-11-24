// Chase, attack, animation, and audio behavior for Varen.
using UnityEngine;

public partial class MutantAI
{
    void UpdateChase(float dist, bool playerVisible)
    {
        agent.speed = chaseSpeed;
        agent.isStopped = false;
        SetAnimation(false, true);
        PlayChaseSound();

        // ── NEW: Lose player if in bush or too far ──
        if (!playerVisible || dist > detectionRange + 5f)
        {
            StopChaseSound();
            currentState = State.Roam;
            SetNewRoamDestination();
            return;
        }

        if (navigationRecovery.ShouldRepath(agent))
        {
            SetNewRoamDestination();
            navigationFallbackUntil = Time.time + 2f;
        }
        if (Time.time >= navigationFallbackUntil && Time.time >= nextNavigationDestinationUpdate)
        {
            nextNavigationDestinationUpdate = Time.time + 0.3f;
            if (!navigationRecovery.TrySetDestination(agent, player.position))
            {
                SetNewRoamDestination();
                navigationFallbackUntil = Time.time + 2f;
            }
        }

        FacePlayer();

        if (dist <= attackRange)
            currentState = State.Attack;
    }

    void UpdateAttack(float dist)
    {
        if (agent.isOnNavMesh)
            agent.SetDestination(transform.position);

        SetAnimation(false, false);
        StopChaseSound();
        FacePlayer();

        if (dist > attackRange + 0.5f)
        {
            currentState = State.Chase;
            return;
        }

        if (attackTimer <= 0f)
        {
            Attack();
            attackTimer = attackCooldown;
        }
    }

    void FacePlayer()
    {
        Vector3 dir = (player.position - transform.position).normalized;
        dir.y = 0f;
        if (dir != Vector3.zero)
        {
            Quaternion look = Quaternion.LookRotation(dir);
            transform.rotation = Quaternion.Slerp(
                transform.rotation, look,
                Time.deltaTime * rotationSpeed);
        }
    }

    void Attack()
    {
        if (AIEncounterGate.IsBlocked(this))
            return;

        if (JumpscareSystem.Instance != null &&
            !JumpscareSystem.Instance.TryTriggerJumpscare(gameObject))
            return;

        PlaySound(attackSound);

        PlayerHealth ph = player.GetComponent<PlayerHealth>();
        if (ph != null)
        {
            ph.TakeDamageForEnemyCatch(attackDamage);
            ph.ShowBloodDamageOverlay(1.2f);
        }
    }

    void SetAnimation(bool walking, bool running)
    {
        if (animator == null) return;
        animator.SetBool("isWalking", walking);
        animator.SetBool("isRunning", running);
    }

    void PlayChaseSound()
    {
        if (isPlayingChaseSound || chaseSound == null) return;
        audioSource.clip = chaseSound;
        audioSource.volume = chaseVolume;
        audioSource.loop = true;
        audioSource.Play();
        isPlayingChaseSound = true;
    }

    void StopChaseSound()
    {
        if (!isPlayingChaseSound) return;
        audioSource.Stop();
        isPlayingChaseSound = false;
    }

    void PlaySound(AudioClip clip)
    {
        if (clip == null) return;
        audioSource.PlayOneShot(clip);
    }

}
