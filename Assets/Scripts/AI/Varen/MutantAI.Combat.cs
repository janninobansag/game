// Chase, attack, animation, and audio behavior for Varen.
using UnityEngine;

public partial class MutantAI
{
    void UpdateChase(float dist, bool playerVisible)
    {
        agent.speed = chaseSpeed;
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

        if (agent.isOnNavMesh)
            agent.SetDestination(player.position);

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
        PlaySound(attackSound);

        if (JumpscareSystem.Instance != null)
            JumpscareSystem.Instance.TriggerJumpscare(gameObject);

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