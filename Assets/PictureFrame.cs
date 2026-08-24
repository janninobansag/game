using UnityEngine;

public class PictureFrame : MonoBehaviour
{
    [Header("Frame Settings")]
    public string frameTitle = "Old Photograph";
    public Texture2D picture;
    public float viewRange = 2f;
    public KeyCode viewKey = KeyCode.E;

    [Header("Optional Description")]
    [TextArea(2, 5)]
    public string description = "";

    private bool isViewing = false;
    private bool showPrompt = false;
    private Camera playerCamera;
    private PlayerController playerController;
    private bool playerControllerWasEnabled;

    // Animation
    private float frameAlpha = 0f;
    private float frameScale = 0.85f;

    void Start()
    {
        playerCamera = Camera.main;
    }

    void Update()
    {
        // A book or note owns the cursor while it is being read.
        if (PlayerController.IsReadingDocument && !isViewing)
            return;

        // Check if game is paused - don't process picture frame input
        if (PauseMenu.Instance != null && PauseMenu.Instance.isPaused)
        {
            return;
        }
        
        if (isViewing)
        {
            frameAlpha = Mathf.Lerp(frameAlpha, 1f, Time.deltaTime * 8f);
            frameScale = Mathf.Lerp(frameScale, 1f, Time.deltaTime * 8f);

            if (Input.GetKeyDown(viewKey) ||
                Input.GetKeyDown(KeyCode.Escape) ||
                Input.GetKeyDown(KeyCode.F))
                CloseFrame();

            // Only manage cursor when viewing
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;  // FIXED: Show cursor when viewing picture
            return;
        }
        else
        {
            frameAlpha = Mathf.Lerp(frameAlpha, 0f, Time.deltaTime * 8f);
        }

        // FIXED: Only lock cursor when NOT paused and NOT viewing
        if (PauseMenu.Instance == null || !PauseMenu.Instance.isPaused)
        {
            if (Cursor.lockState != CursorLockMode.Locked)
            {
                Cursor.lockState = CursorLockMode.Locked;
                Cursor.visible = false;
            }
        }

        if (playerCamera == null) return;

        showPrompt = false;

        // Raycast based detection
        Ray ray = playerCamera.ScreenPointToRay(
            new Vector3(Screen.width / 2, Screen.height / 2));
        RaycastHit hit;

        if (Physics.Raycast(ray, out hit, viewRange))
        {
            if (hit.collider.gameObject == gameObject ||
                hit.collider.transform.IsChildOf(transform))
            {
                showPrompt = true;
                if (Input.GetKeyDown(viewKey))
                    OpenFrame();
            }
        }
    }

    void OpenFrame()
    {
        isViewing = true;
        PlayerController.SetDocumentReading(true);
        LockPlayerMovement();
        showPrompt = false;
        frameAlpha = 0f;
        frameScale = 0.85f;
    }

    void CloseFrame()
    {
        isViewing = false;
        PlayerController.SetDocumentReading(false);
        RestorePlayerMovement();
    }

    private void LockPlayerMovement()
    {
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player != null) playerController = player.GetComponent<PlayerController>();
        if (playerController == null) playerController = FindObjectOfType<PlayerController>();
        if (playerController == null) return;

        playerControllerWasEnabled = playerController.enabled;
        playerController.enabled = false;
    }

    private void RestorePlayerMovement()
    {
        if (playerController != null) playerController.enabled = playerControllerWasEnabled;
        playerController = null;
    }

    private void OnDisable()
    {
        if (isViewing) CloseFrame();
    }

    void OnGUI()
    {
        // The Canvas pause menu must be the only screen UI while paused.
        if (PauseMenu.Instance != null && PauseMenu.Instance.isPaused)
            return;

        float sw = Screen.width;
        float sh = Screen.height;
        float cx = sw / 2f;
        float cy = sh / 2f;

        // ── PICKUP PROMPT ──
        if (showPrompt && !isViewing)
        {
            GUIStyle style = new GUIStyle();
            style.fontSize = 22;
            style.alignment = TextAnchor.MiddleCenter;
            style.normal.textColor = Color.white;

            GUIStyle shadow = new GUIStyle();
            shadow.fontSize = 22;
            shadow.alignment = TextAnchor.MiddleCenter;
            shadow.normal.textColor = Color.black;

            string msg = $"Press E to interact {frameTitle}";
            GUI.Label(new Rect(cx - 199, cy + 51, 400, 40), msg, shadow);
            GUI.Label(new Rect(cx - 200, cy + 50, 400, 40), msg, style);
        }

        // ── FRAME VIEWER ──
        if (isViewing || frameAlpha > 0.01f)
        {
            // Dark overlay
            GUI.color = new Color(0f, 0f, 0f, 0.85f * frameAlpha);
            GUI.DrawTexture(new Rect(0, 0, sw, sh), Texture2D.whiteTexture);

            // Frame dimensions
            float frameW = 420f * frameScale;
            float frameH = 520f * frameScale;
            float fx = cx - frameW / 2f;
            float fy = cy - frameH / 2f;

            // Outer frame shadow
            GUI.color = new Color(0f, 0f, 0f, 0.6f * frameAlpha);
            GUI.DrawTexture(new Rect(fx + 8, fy + 8, frameW, frameH),
                Texture2D.whiteTexture);

            // Outer frame (dark wood color)
            GUI.color = new Color(0.18f, 0.12f, 0.08f, frameAlpha);
            GUI.DrawTexture(new Rect(fx, fy, frameW, frameH),
                Texture2D.whiteTexture);

            // Frame border details
            float bord = 18f;
            GUI.color = new Color(0.28f, 0.2f, 0.13f, frameAlpha);
            GUI.DrawTexture(new Rect(fx + 4, fy + 4, frameW - 8, frameH - 8),
                Texture2D.whiteTexture);

            // Inner frame (darker)
            GUI.color = new Color(0.12f, 0.08f, 0.05f, frameAlpha);
            GUI.DrawTexture(new Rect(fx + bord, fy + bord,
                frameW - bord * 2, frameH - bord * 2),
                Texture2D.whiteTexture);

            // Corner decorations
            float cs = 12f;
            GUI.color = new Color(0.45f, 0.32f, 0.18f, frameAlpha);
            GUI.DrawTexture(new Rect(fx + 6, fy + 6, cs, cs),
                Texture2D.whiteTexture);
            GUI.DrawTexture(new Rect(fx + frameW - 6 - cs, fy + 6, cs, cs),
                Texture2D.whiteTexture);
            GUI.DrawTexture(new Rect(fx + 6, fy + frameH - 6 - cs, cs, cs),
                Texture2D.whiteTexture);
            GUI.DrawTexture(new Rect(fx + frameW - 6 - cs,
                fy + frameH - 6 - cs, cs, cs),
                Texture2D.whiteTexture);

            // Title area at top
            float titleH = 36f;
            GUI.color = new Color(0.1f, 0.07f, 0.04f, frameAlpha);
            GUI.DrawTexture(new Rect(fx + bord, fy + bord,
                frameW - bord * 2, titleH), Texture2D.whiteTexture);

            GUIStyle titleStyle = new GUIStyle();
            titleStyle.fontSize = 14;
            titleStyle.fontStyle = FontStyle.Bold;
            titleStyle.alignment = TextAnchor.MiddleCenter;
            titleStyle.normal.textColor =
                new Color(0.85f, 0.75f, 0.55f, frameAlpha);
            GUI.Label(new Rect(fx + bord, fy + bord,
                frameW - bord * 2, titleH),
                frameTitle, titleStyle);

            // Divider line
            GUI.color = new Color(0.45f, 0.32f, 0.18f, 0.5f * frameAlpha);
            GUI.DrawTexture(new Rect(fx + bord + 10,
                fy + bord + titleH, frameW - bord * 2 - 20, 1f),
                Texture2D.whiteTexture);

            // Picture area
            float picPad = 10f;
            float picY = fy + bord + titleH + 2f;
            float picH = string.IsNullOrEmpty(description)
                ? frameH - bord * 2 - titleH - picPad - 30f
                : frameH - bord * 2 - titleH - picPad - 70f;
            float picW = frameW - bord * 2 - picPad * 2;
            float picX = fx + bord + picPad;

            // Picture background
            GUI.color = new Color(0.08f, 0.06f, 0.04f, frameAlpha);
            GUI.DrawTexture(new Rect(picX, picY, picW, picH),
                Texture2D.whiteTexture);

            // Draw picture if assigned
            if (picture != null)
            {
                GUI.color = new Color(1f, 1f, 1f, frameAlpha);
                GUI.DrawTexture(new Rect(picX, picY, picW, picH),
                    picture, ScaleMode.ScaleToFit);
            }
            else
            {
                // Placeholder if no picture assigned
                GUI.color = new Color(0.15f, 0.12f, 0.1f, frameAlpha);
                GUI.DrawTexture(new Rect(picX, picY, picW, picH),
                    Texture2D.whiteTexture);

                GUIStyle noImgStyle = new GUIStyle();
                noImgStyle.fontSize = 13;
                noImgStyle.alignment = TextAnchor.MiddleCenter;
                noImgStyle.normal.textColor =
                    new Color(0.4f, 0.35f, 0.3f, frameAlpha);
                GUI.Label(new Rect(picX, picY, picW, picH),
                    "[ No image assigned ]", noImgStyle);
            }

            // Description text
            if (!string.IsNullOrEmpty(description))
            {
                float descY = picY + picH + 6f;
                GUI.color = new Color(0.1f, 0.07f, 0.04f, frameAlpha);
                GUI.DrawTexture(new Rect(fx + bord, descY,
                    frameW - bord * 2, 52f), Texture2D.whiteTexture);

                GUIStyle descStyle = new GUIStyle();
                descStyle.fontSize = 11;
                descStyle.fontStyle = FontStyle.Italic;
                descStyle.wordWrap = true;
                descStyle.alignment = TextAnchor.MiddleCenter;
                descStyle.normal.textColor =
                    new Color(0.75f, 0.65f, 0.5f, frameAlpha);
                GUI.Label(new Rect(fx + bord + 10, descY,
                    frameW - bord * 2 - 20, 52f),
                    description, descStyle);
            }

            // Close hint
            GUIStyle closeStyle = new GUIStyle();
            closeStyle.fontSize = 11;
            closeStyle.alignment = TextAnchor.MiddleCenter;
            closeStyle.normal.textColor =
                new Color(0.45f, 0.38f, 0.3f, frameAlpha * 0.8f);
            GUI.Label(new Rect(fx, fy + frameH + 8f, frameW, 18f),
                "Press E to close", closeStyle);

            GUI.color = Color.white;
        }
    }
}
