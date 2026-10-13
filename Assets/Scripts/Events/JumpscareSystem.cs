// PURPOSE: Coordinates the jumpscare sequence, audio, enemy positioning, and optional death or reload behavior.
using System.Collections;
using UnityEngine;

public class JumpscareSystem : MonoBehaviour
{
    public static JumpscareSystem Instance;

    // Timing used for the active jumpscare sequence.
    [Header("Jumpscare Settings")]
    public float jumpscareDuration = 3f;

    // What happens after the jumpscare finishes.
    [Header("Death Settings")]
    public float deathDelay = 2f;
    public bool reloadSceneOnDeath = true;

    // Positioning used to place the mutant in front of the player camera.
    [Header("Mutant Face Settings")]
    [Tooltip("Fallback distance when the catching enemy has no individual setting.")]
    public float mutantFaceDistance = 1.35f;
    public float mutantMoveSpeed = 30f;
    [Tooltip("Time used to turn the player camera toward Varen when he catches the player.")]
    [Min(0f)] public float cameraLookDuration = 0.25f;
    [Tooltip("Height on Varen used as the camera focus point.")]
    public float mutantFaceHeight = 1.5f;

    private bool isJumpscaring = false;
    private float jumpscareAlpha = 0f;
    private float flashAlpha = 0f;
    private bool isDead = false;
    private float deathAlpha = 0f;
    private float shakeTimer = 0f;
    private Vector2 shakeOffset;

    // Mutant reference
    private GameObject mutantObj;
    private Transform mutantTransform;
    private Vector3 mutantTargetPos;
    private Quaternion mutantTargetRot;
    private bool movingMutant = false;
    

    void Awake()
    {
        if (Instance == null)
            Instance = this;
        else
            Destroy(gameObject);
    }

    void Update()
    {
        if (shakeTimer > 0f)
        {
            shakeTimer -= Time.deltaTime;
            shakeOffset = new Vector2(
                Random.Range(-0.015f, 0.015f) * Screen.width,
                Random.Range(-0.015f, 0.015f) * Screen.height);
        }
        else shakeOffset = Vector2.zero;

        if (isDead)
            deathAlpha = Mathf.Lerp(deathAlpha, 1f, Time.deltaTime * 2f);

        // Smoothly move mutant to face position
        if (movingMutant && mutantTransform != null)
        {
            mutantTransform.position = Vector3.Lerp(
                mutantTransform.position, mutantTargetPos,
                Time.deltaTime * mutantMoveSpeed);
            mutantTransform.rotation = Quaternion.Slerp(
                mutantTransform.rotation, mutantTargetRot,
                Time.deltaTime * mutantMoveSpeed);
        }
    }

    public void TriggerJumpscare(GameObject mutant = null)
    {
        if (isJumpscaring) return;
        mutantObj = mutant;
        if (mutantObj == null)
            mutantObj = GameObject.FindGameObjectWithTag("Mutant");
        StartCoroutine(JumpscareSequence());
    }

    public void ResetJumpscare()
    {
        isJumpscaring = false;
        jumpscareAlpha = 0f;
        flashAlpha = 0f;
        isDead = false;
        deathAlpha = 0f;
        shakeTimer = 0f;
        shakeOffset = Vector2.zero;
        movingMutant = false;

    }

    IEnumerator JumpscareSequence()
    {
        isJumpscaring = true;

        // Disable ritual interaction immediately so player can't hold E
        RitualTree rt = FindObjectOfType<RitualTree>();
        if (rt != null) rt.DisableInteraction();

        // Disable player controller
        PlayerController pc = FindObjectOfType<PlayerController>();
        if (pc != null) pc.enabled = false;

        // Disable mutant AI and NavMesh
        if (mutantObj != null)
        {
            MutantAI ai = mutantObj.GetComponent<MutantAI>();
            if (ai != null) ai.enabled = false;

            UnityEngine.AI.NavMeshAgent agent =
                mutantObj.GetComponent<UnityEngine.AI.NavMeshAgent>();
            if (agent != null) agent.enabled = false;

            Camera cam = Camera.main;
            if (cam == null)
                yield break;

            // Turn toward Varen before bringing him in front of the player.
            float focusHeight = ai != null ? ai.jumpscareCameraFocusHeight : mutantFaceHeight;
            yield return StartCoroutine(FocusCameraOnMutant(cam.transform, mutantObj.transform, focusHeight));

            // Calculate position in front of camera
            float faceDistance = ai != null ? ai.jumpscareFaceDistance : mutantFaceDistance;
            mutantTargetPos = cam.transform.position +
                cam.transform.forward * faceDistance;
            mutantTargetPos.y = cam.transform.position.y - 1.5f;

            // Face the player
           mutantTargetRot = Quaternion.LookRotation(
    cam.transform.position - mutantTargetPos);

// Lock upright — prevent tilting
Vector3 euler = mutantTargetRot.eulerAngles;
euler.x = 0f;
euler.z = 0f;
mutantTargetRot = Quaternion.Euler(euler);

            mutantTransform = mutantObj.transform;
            movingMutant = true;
        }

        // White flash
        flashAlpha = 1f;
        shakeTimer = 0.6f;

        // Fade flash → red overlay
        float elapsed = 0f;
        while (elapsed < 0.2f)
        {
            elapsed += Time.deltaTime;
            flashAlpha = Mathf.Lerp(1f, 0f, elapsed / 0.2f);
            jumpscareAlpha = Mathf.Lerp(0f, 1f, elapsed / 0.2f);
            yield return null;
        }

        flashAlpha = 0f;
        jumpscareAlpha = 1f;

        // Brief pause before holding the jumpscare.
        yield return new WaitForSeconds(0.3f);

        // Hold jumpscare
        yield return new WaitForSeconds(jumpscareDuration);

        // Stop moving mutant
        movingMutant = false;

        // Trigger death
        isDead = true;

        yield return new WaitForSeconds(deathDelay);

        PlayerHealth playerHealth = FindObjectOfType<PlayerHealth>();

        // Zero HP means the run restarts from the beginning of this chapter.
        if (playerHealth != null && playerHealth.currentHealth <= 0f)
        {
            UnityEngine.SceneManagement.SceneManager.LoadScene(
                UnityEngine.SceneManagement.SceneManager.GetActiveScene().name);
            yield break;
        }

        // A surviving player returns to the latest checkpoint with the HP left after Varen's hit.
        CheckpointTrigger ct = FindObjectOfType<CheckpointTrigger>();
        if (ct != null && ct.HasCheckpoint())
        {
            ct.Respawn(false);
        }
        else if (reloadSceneOnDeath)
        {
            UnityEngine.SceneManagement.SceneManager.LoadScene(
                UnityEngine.SceneManagement.SceneManager.GetActiveScene().name);
        }
    }

    IEnumerator FocusCameraOnMutant(Transform playerCamera, Transform mutant, float focusHeight)
    {
        Vector3 direction = mutant.position + Vector3.up * focusHeight - playerCamera.position;
        if (direction.sqrMagnitude < 0.001f)
            yield break;

        Quaternion startRotation = playerCamera.rotation;
        Quaternion targetRotation = Quaternion.LookRotation(direction);
        float elapsed = 0f;

        while (elapsed < cameraLookDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            float progress = cameraLookDuration <= 0f ? 1f : elapsed / cameraLookDuration;
            playerCamera.rotation = Quaternion.Slerp(startRotation, targetRotation, progress);
            yield return null;
        }

        playerCamera.rotation = targetRotation;
    }

    void OnGUI()
    {
        float sw = Screen.width;
        float sh = Screen.height;
        float cx = sw / 2f;
        float cy = sh / 2f;

        float sx = shakeOffset.x;
        float sy = shakeOffset.y;

        // White flash
        if (flashAlpha > 0.01f)
        {
            GUI.color = new Color(1f, 1f, 1f, flashAlpha);
            GUI.DrawTexture(new Rect(sx, sy, sw, sh),
                Texture2D.whiteTexture);
        }

        // Jumpscare overlay
        if (jumpscareAlpha > 0.01f && !isDead)
        {
            // The Canvas blood overlay supplies the damage background.
            // Keep only the warning text in this legacy GUI system.

            // IT FOUND YOU text
            GUIStyle scareStyle = new GUIStyle();
            scareStyle.fontSize = 48;
            scareStyle.fontStyle = FontStyle.Bold;
            scareStyle.alignment = TextAnchor.MiddleCenter;
            scareStyle.normal.textColor =
                new Color(0.9f, 0.1f, 0.1f, jumpscareAlpha);

            // Shadow
            GUIStyle scareShadow = new GUIStyle(scareStyle);
            scareShadow.normal.textColor =
                new Color(0f, 0f, 0f, jumpscareAlpha);

            GUI.color = Color.white;
            GUI.Label(new Rect(sx + 2, sy + sh / 2 - 118, sw, 50),
                "IT FOUND YOU", scareShadow);
            GUI.Label(new Rect(sx, sy + sh / 2 - 120, sw, 50),
                "IT FOUND YOU", scareStyle);
        }

        // Death screen
        if (isDead)
        {
            GUI.color = new Color(0f, 0f, 0f, deathAlpha);
            GUI.DrawTexture(new Rect(0, 0, sw, sh),
                Texture2D.whiteTexture);

            if (deathAlpha > 0.5f)
            {
                GUIStyle deadStyle = new GUIStyle();
                deadStyle.fontSize = 36;
                deadStyle.fontStyle = FontStyle.Bold;
                deadStyle.alignment = TextAnchor.MiddleCenter;
                deadStyle.normal.textColor =
                    new Color(0.9f, 0.1f, 0.1f, deathAlpha);

                GUIStyle subStyle = new GUIStyle();
                subStyle.fontSize = 15;
                subStyle.alignment = TextAnchor.MiddleCenter;
                subStyle.normal.textColor =
                    new Color(0.6f, 0.6f, 0.6f, deathAlpha);

                GUI.color = Color.white;
                GUI.Label(new Rect(0, cy - 40f, sw, 50f),
                    "YOU WERE CAUGHT", deadStyle);
                GUI.Label(new Rect(0, cy + 20f, sw, 30f),
                    "Restarting...", subStyle);
            }

            GUI.color = Color.white;
        }
    }
}
