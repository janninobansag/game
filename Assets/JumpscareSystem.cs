using System.Collections;
using UnityEngine;

public class JumpscareSystem : MonoBehaviour
{
    public static JumpscareSystem Instance;

    [Header("Jumpscare Settings")]
    public AudioClip jumpscareSound;
    public AudioClip screamSound;
    public float jumpscareDuration = 3f;

    [Header("Death Settings")]
    public float deathDelay = 2f;
    public bool reloadSceneOnDeath = true;

    [Header("Mutant Face Settings")]
    public float mutantFaceDistance = 0.8f;
    public float mutantMoveSpeed = 30f;

    private bool isJumpscaring = false;
    private float jumpscareAlpha = 0f;
    private float flashAlpha = 0f;
    private bool isDead = false;
    private float deathAlpha = 0f;
    private float shakeTimer = 0f;
    private Vector2 shakeOffset;
    private AudioSource audioSource;

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

    void Start()
    {
        audioSource = gameObject.AddComponent<AudioSource>();
        audioSource.spatialBlend = 0f;
        audioSource.playOnAwake = false;
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

            // Calculate position in front of camera
            Camera cam = Camera.main;
            mutantTargetPos = cam.transform.position +
                cam.transform.forward * mutantFaceDistance;
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

        // Play jumpscare sound
        if (jumpscareSound != null)
            audioSource.PlayOneShot(jumpscareSound);

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

        // Play scream after short delay
        yield return new WaitForSeconds(0.3f);
        if (screamSound != null)
            audioSource.PlayOneShot(screamSound);

        // Hold jumpscare
        yield return new WaitForSeconds(jumpscareDuration);

        // Stop moving mutant
        movingMutant = false;

        // Trigger death
        isDead = true;

        yield return new WaitForSeconds(deathDelay);

        // Respawn at checkpoint instead of reloading scene
        CheckpointTrigger ct = FindObjectOfType<CheckpointTrigger>();
        if (ct != null && ct.HasCheckpoint())
        {
            ct.Respawn();
        }
        else if (reloadSceneOnDeath)
        {
            UnityEngine.SceneManagement.SceneManager.LoadScene(
                UnityEngine.SceneManagement.SceneManager
                .GetActiveScene().name);
        }
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
            // Dark vignette
            GUI.color = new Color(0f, 0f, 0f, 0.5f * jumpscareAlpha);
            GUI.DrawTexture(new Rect(sx, sy, sw, sh),
                Texture2D.whiteTexture);

            // Red edges
            int steps = 60;
            for (int i = 0; i < steps; i++)
            {
                float t = 1f - (i / (float)steps);
                GUI.color = new Color(0.25f, 0f, 0f,
                    t * t * jumpscareAlpha * 0.3f);
                GUI.DrawTexture(new Rect(sx, sy,
                    sw * (i / (float)steps) * 0.03f, sh),
                    Texture2D.whiteTexture);
                GUI.DrawTexture(new Rect(
                    sx + sw - sw * (i / (float)steps) * 0.03f, sy,
                    sw * (i / (float)steps) * 0.03f, sh),
                    Texture2D.whiteTexture);
                GUI.DrawTexture(new Rect(sx, sy,
                    sw, sh * (i / (float)steps) * 0.02f),
                    Texture2D.whiteTexture);
                GUI.DrawTexture(new Rect(sx,
                    sy + sh - sh * (i / (float)steps) * 0.02f,
                    sw, sh * (i / (float)steps) * 0.02f),
                    Texture2D.whiteTexture);
            }

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