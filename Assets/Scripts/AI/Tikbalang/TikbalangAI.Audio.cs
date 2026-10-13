// Movement animation and 3D audio behavior for Tikbalang.
using UnityEngine;

public partial class TikbalangAI
{
    private void SetMovementAnimation(bool moving, bool running = false)
    {
        UpdateWalkingSound(moving && !running);
        if (animator == null) return;
        bool walking = moving && !running;
        SetAnimatorBoolIfPresent("walking", walking);
        SetAnimatorBoolIfPresent("run", running);
        SetAnimatorBoolIfPresent("isWalking", walking);
        SetAnimatorBoolIfPresent("isRunning", running);
        SetAnimatorFloatIfPresent("Speed", running ? runSpeed : (walking ? walkSpeed : 0f));
    }

    // Starts Tikbalang looping chase sound if it is not already playing.
    private void PlayChaseSound()
    {
        if (isPlayingChaseSound || audioSource == null || chaseSound == null)
            return;

        audioSource.clip = chaseSound;
        audioSource.volume = chaseVolume;
        audioSource.loop = true;
        audioSource.Play();
        isPlayingChaseSound = true;
    }

    // Stops the looping chase sound when Tikbalang is no longer running.
    private void StopChaseSound()
    {
        if (!isPlayingChaseSound || audioSource == null)
            return;

        if (audioSource.clip == chaseSound)
            audioSource.Stop();
        isPlayingChaseSound = false;
    }

    // Creates a dedicated source so walking audio never interrupts the chase or jumpscare sounds.
    private void EnsureWalkingAudioSource()
    {
        if (walkingAudioSource != null && walkingAudioSource != audioSource)
            return;

        GameObject sourceObject = new GameObject("Tikbalang Walking Audio");
        sourceObject.transform.SetParent(transform, false);
        walkingAudioSource = sourceObject.AddComponent<AudioSource>();
        walkingAudioSource.spatialBlend = 1f;
        walkingAudioSource.playOnAwake = false;
        walkingAudioSource.rolloffMode = AudioRolloffMode.Linear;
    }

    // Starts or stops the looping walk sound whenever Tikbalang changes movement state.
    private void UpdateWalkingSound(bool shouldPlay)
    {
        if (walkingAudioSource == null)
            EnsureWalkingAudioSource();

        if (walkingAudioSource == null || walkingSound == null)
            return;

        // Enforce a positional source even when a source was assigned in the Inspector.
        walkingAudioSource.spatialBlend = 1f;
        walkingAudioSource.rolloffMode = AudioRolloffMode.Linear;

        if (shouldPlay)
        {
            if (walkingAudioSource.clip != walkingSound)
            {
                walkingAudioSource.Stop();
                walkingAudioSource.clip = walkingSound;
            }

            walkingAudioSource.volume = walkingVolume;
            walkingAudioSource.maxDistance = walkingMaxDistance;
            walkingAudioSource.loop = true;
            if (!walkingAudioSource.isPlaying)
                walkingAudioSource.Play();
            isPlayingWalkingSound = true;
            return;
        }

        if (isPlayingWalkingSound && walkingAudioSource.isPlaying)
            walkingAudioSource.Stop();
        isPlayingWalkingSound = false;
    }

    // Shows the Chapter 2 objective only after the first Tikbalang jumpscare releases the player.
}
