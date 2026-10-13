// First encounter, catch jumpscare, and camera behavior for Tikbalang.
using System.Collections;
using UnityEngine;
using UnityEngine.AI;

public partial class TikbalangAI
{
    public bool TriggerDetectionJumpscare(TikbalangJumpscareTrigger triggerSource)
    {
        FindPlayerIfNeeded();

        if (player == null)
        {
            return false;
        }

        if (isFirstEncounterPlaying || isCatchSequencePlaying)
        {
            return false;
        }

        StartCoroutine(PlayDetectionJumpscare());
        return true;
    }

    // Stops Tikbalang, teleports him in front of the player, and starts the catch sequence.
    private IEnumerator PlayDetectionJumpscare()
    {
        isFirstEncounterPlaying = true;
        hasAwakened = true;

        if (agent != null && agent.enabled && agent.isOnNavMesh)
        {
            agent.isStopped = true;
            agent.ResetPath();
        }

        SetMovementAnimation(false);
        TeleportInFrontOfPlayer();
        yield return StartCoroutine(PlayCatchJumpscare());
        isFirstEncounterPlaying = false;
    }
    // Runs the jumpscare, Q and A, teleport, camera restoration, and chase resume sequence.
    private IEnumerator PlayCatchJumpscare()
    {
        isCatchSequencePlaying = true;
        if (agent.enabled && agent.isOnNavMesh)
        {
            agent.isStopped = true;
            agent.ResetPath();
        }

        SetMovementAnimation(false);
        StopChaseSound();
        SetPlayerQuestionLock(true);
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
        yield return StartCoroutine(FocusCameraOnTikbalang());
        SetAnimatorTriggerIfPresent("jumpscare");

        if (audioSource != null && discoveryShout != null)
            audioSource.PlayOneShot(discoveryShout);

        yield return new WaitForSeconds(jumpscareBeforeTeleportDelay);
        yield return StartCoroutine(ShowJumpscareQuestion(false));

        TeleportToSpawnPoint(true);
yield return StartCoroutine(RestoreCameraAfterJumpscare());
        SetPlayerQuestionLock(false);
        ShowGeneratorObjectivePopup();
        nextCatchTime = Time.time + catchCooldown;
        isCatchSequencePlaying = false;
        ResumeChaseAfterEncounter();
    }

    // Smoothly turns the jumpscare camera toward Tikbalang face.
    private IEnumerator FocusCameraOnTikbalang()
    {
        if (jumpscareCamera == null && Camera.main != null)
            jumpscareCamera = Camera.main.transform;
        if (jumpscareCamera == null)
            yield break;

        cameraRotationBeforeCatch = jumpscareCamera.rotation;
        Vector3 targetPosition = transform.position + Vector3.up * tikbalangFaceHeight;
        Quaternion targetRotation = Quaternion.LookRotation((targetPosition - jumpscareCamera.position).normalized);
        float elapsed = 0f;

        while (elapsed < cameraLookDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            jumpscareCamera.rotation = Quaternion.Slerp(cameraRotationBeforeCatch, targetRotation,
                cameraLookDuration <= 0f ? 1f : elapsed / cameraLookDuration);
            yield return null;
        }

        jumpscareCamera.rotation = targetRotation;
    }

    // Smoothly returns the camera to its rotation from before the jumpscare.
    private IEnumerator RestoreCameraAfterJumpscare()
    {
        if (jumpscareCamera == null)
            yield break;

        Quaternion startRotation = jumpscareCamera.rotation;
        float elapsed = 0f;
        while (elapsed < cameraReturnDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            jumpscareCamera.rotation = Quaternion.Slerp(startRotation, cameraRotationBeforeCatch,
                cameraReturnDuration <= 0f ? 1f : elapsed / cameraReturnDuration);
            yield return null;
        }

        jumpscareCamera.rotation = cameraRotationBeforeCatch;
    }
    // Finds and hides the Q and A panel, then connects its answer buttons.
}
