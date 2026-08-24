using System.Collections;
using UnityEngine;

public class CheckpointTrigger : MonoBehaviour
{
    public static CheckpointTrigger Instance;

    // ── ADDED: Public static variable for save system ──
    public static bool HasCheckpointSaved = false;

    [Header("Settings")]
    public string playerTag = "Player";
    public float respawnDelay = 2f;

    private Vector3 spawnPosition;
    private Quaternion spawnRotation;
    private bool checkpointSaved = false;
    private bool triggeredOnce = false;
    private bool isRespawning = false;
    private float blackAlpha = 0f;
    private float notifAlpha = 0f;
    private bool showNotif = false;

    void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);

        // Default spawn is player start position
        GameObject p = GameObject.FindGameObjectWithTag(playerTag);
        if (p != null)
        {
            spawnPosition = p.transform.position;
            spawnRotation = p.transform.rotation;
        }
    }

    void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag(playerTag)) return;
        if (triggeredOnce) return;

        // Save checkpoint only once
        triggeredOnce = true;
        spawnPosition = other.transform.position;
        spawnRotation = other.transform.rotation;
        checkpointSaved = true;

        // ── ADDED: Set the static flag for save system ──
        HasCheckpointSaved = true;

        StopAllCoroutines();
        StartCoroutine(ShowNotif());
    }

    public void Respawn(bool restoreFullHealth = true)
    {
        if (isRespawning) return;
        StartCoroutine(RespawnSequence(restoreFullHealth));
    }

    public bool HasCheckpoint() => checkpointSaved;

    IEnumerator RespawnSequence(bool restoreFullHealth)
    {
        isRespawning = true;

        // Fade to black
        float elapsed = 0f;
        while (elapsed < 1f)
        {
            elapsed += Time.deltaTime * 2f;
            blackAlpha = Mathf.Clamp01(elapsed);
            yield return null;
        }
        blackAlpha = 1f;

        yield return new WaitForSeconds(respawnDelay);

        // Teleport player
        GameObject playerObj =
            GameObject.FindGameObjectWithTag(playerTag);
        if (playerObj != null)
        {
            CharacterController cc =
                playerObj.GetComponent<CharacterController>();
            if (cc != null) cc.enabled = false;
            playerObj.transform.position = spawnPosition;
            playerObj.transform.rotation = spawnRotation;
            if (cc != null) cc.enabled = true;
        }

        // Restore player
        PlayerController pc = FindObjectOfType<PlayerController>();
        if (pc != null) pc.enabled = true;

        PlayerHealth ph = FindObjectOfType<PlayerHealth>();
        if (ph != null)
        {
            if (restoreFullHealth)
                ph.currentHealth = ph.maxHealth;
            ph.isDead = false;
        }

        // Reset jumpscare
        JumpscareSystem js = FindObjectOfType<JumpscareSystem>();
        if (js != null) js.ResetJumpscare();

        // Reset ritual tree (re-enables E interaction)
        RitualTree rt = FindObjectOfType<RitualTree>();
        if (rt != null) rt.ResetRitual();

        // Reset mutant
        MutantAI mutant = FindObjectOfType<MutantAI>();
        if (mutant != null) mutant.ResetToRoam();

        // Small delay to ensure everything is reset
        yield return new WaitForSeconds(0.5f);

        // Fade back in (smooth fade from black)
        float elapsed2 = 0f;
        float fadeInDuration = 1.5f;
        while (elapsed2 < fadeInDuration)
        {
            elapsed2 += Time.deltaTime;
            blackAlpha = Mathf.Clamp01(1f - (elapsed2 / fadeInDuration));
            yield return null;
        }

        blackAlpha = 0f;
        isRespawning = false;
    }

    IEnumerator ShowNotif()
    {
        showNotif = true;
        float e = 0f;
        while (e < 0.5f) { e += Time.deltaTime; notifAlpha = e / 0.5f; yield return null; }
        yield return new WaitForSeconds(2.5f);
        e = 0f;
        while (e < 0.5f) { e += Time.deltaTime; notifAlpha = 1f - e / 0.5f; yield return null; }
        showNotif = false;
        notifAlpha = 0f;
    }

    void OnGUI()
    {
        float sw = Screen.width;
        float sh = Screen.height;

        // Black fade
        if (blackAlpha > 0.01f)
        {
            GUI.color = new Color(0f, 0f, 0f, blackAlpha);
            GUI.DrawTexture(new Rect(0, 0, sw, sh),
                Texture2D.whiteTexture);

            if (blackAlpha > 0.5f && isRespawning)
            {
                GUIStyle s = new GUIStyle();
                s.fontSize = (int)(sh * 0.024f);
                s.alignment = TextAnchor.MiddleCenter;
                s.normal.textColor =
                    new Color(0.7f, 0.7f, 0.7f, blackAlpha);
                GUI.color = Color.white;
                GUI.Label(new Rect(0, sh / 2f - 20f, sw, 40f),
                    "Returning to checkpoint...", s);
            }
            GUI.color = Color.white;
        }

        // Notification
        if (showNotif && notifAlpha > 0.01f)
        {
            float pw = 280f;
            float ph = 52f;
            float px = sw - pw - 20f;
            float py = sh - ph - 80f;

            GUI.color = new Color(0f, 0f, 0f, 0.75f * notifAlpha);
            GUI.DrawTexture(new Rect(px, py, pw, ph),
                Texture2D.whiteTexture);

            GUI.color = new Color(0.2f, 0.85f, 0.3f, notifAlpha);
            GUI.DrawTexture(new Rect(px, py, 3f, ph),
                Texture2D.whiteTexture);

            GUI.color = Color.white;

            GUIStyle t = new GUIStyle();
            t.fontSize = (int)(sh * 0.018f);
            t.fontStyle = FontStyle.Bold;
            t.normal.textColor =
                new Color(0.3f, 0.95f, 0.4f, notifAlpha);
            GUI.Label(new Rect(px + 12f, py + 8f, pw, 20f),
                "Checkpoint saved!", t);

            GUIStyle sub = new GUIStyle();
            sub.fontSize = (int)(sh * 0.013f);
            sub.normal.textColor =
                new Color(0.7f, 0.75f, 0.7f, notifAlpha);
            GUI.Label(new Rect(px + 12f, py + 28f, pw, 18f),
                "You will respawn here if caught", sub);
        }
    }
}