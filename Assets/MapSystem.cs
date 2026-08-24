using UnityEngine;

public class MapSystem : MonoBehaviour
{
    [Header("Map Settings")]
    public KeyCode mapKey = KeyCode.M;
    public Texture2D mapTexture; // drag your map image here

    [Header("Map Boundaries — match your world")]
    public float worldMinX = -200f;
    public float worldMaxX = 200f;
    public float worldMinZ = -200f;
    public float worldMaxZ = 200f;

    [Header("Map UI Settings")]
    public float mapWidth = 600f;
    public float mapHeight = 600f;

    [Header("Locations (optional)")]
    

    private bool isOpen = false;
    private float mapAlpha = 0f;
    private Transform player;
    private Camera playerCamera;

    // Arrow points for player indicator
    private Vector2[] arrowPoints = new Vector2[3];

    void Start()
    {
        GameObject playerObj =
            GameObject.FindGameObjectWithTag("Player");
        if (playerObj != null)
            player = playerObj.transform;

        playerCamera = Camera.main;
    }

    void Update()
    {
        if (Input.GetKeyDown(mapKey))
        {
            isOpen = !isOpen;

            // Lock/unlock cursor
            if (isOpen)
            {
                Cursor.lockState = CursorLockMode.None;
                Cursor.visible = false;

                // Disable player look
                PlayerController pc =
                    FindObjectOfType<PlayerController>();
                if (pc != null) pc.enabled = false;
            }
            else
            {
                Cursor.lockState = CursorLockMode.Locked;
                Cursor.visible = false;

                PlayerController pc =
                    FindObjectOfType<PlayerController>();
                if (pc != null) pc.enabled = true;
            }
        }

        // Animate alpha
        float target = isOpen ? 1f : 0f;
        mapAlpha = Mathf.Lerp(mapAlpha, target, Time.deltaTime * 8f);
    }

    // Convert world position to map UI position
    Vector2 WorldToMap(Vector3 worldPos, float mx, float my)
    {
        float nx = Mathf.InverseLerp(worldMinX, worldMaxX, worldPos.x);
        float nz = Mathf.InverseLerp(worldMinZ, worldMaxZ, worldPos.z);

        float mapX = mx + nx * mapWidth;
        float mapY = my + (1f - nz) * mapHeight;

        return new Vector2(mapX, mapY);
    }

    void OnGUI()
    {
        if (mapAlpha <= 0.01f) return;

        float sw = Screen.width;
        float sh = Screen.height;
        float cx = sw / 2f;
        float cy = sh / 2f;

        float mx = cx - mapWidth / 2f;
        float my = cy - mapHeight / 2f;

        // ── DARK OVERLAY ──
        GUI.color = new Color(0f, 0f, 0f, 0.75f * mapAlpha);
        GUI.DrawTexture(new Rect(0, 0, sw, sh),
            Texture2D.whiteTexture);

        // ── MAP PANEL SHADOW ──
        GUI.color = new Color(0f, 0f, 0f, 0.6f * mapAlpha);
        GUI.DrawTexture(new Rect(mx + 6, my + 6,
            mapWidth, mapHeight), Texture2D.whiteTexture);

        // ── MAP BACKGROUND ──
        if (mapTexture != null)
        {
            GUI.color = new Color(1f, 1f, 1f, mapAlpha);
            GUI.DrawTexture(new Rect(mx, my, mapWidth, mapHeight),
                mapTexture);
        }
        else
        {
            // Default dark map if no texture assigned
            GUI.color = new Color(0.08f, 0.1f, 0.08f, mapAlpha);
            GUI.DrawTexture(new Rect(mx, my, mapWidth, mapHeight),
                Texture2D.whiteTexture);

            // Grid lines
            int gridLines = 8;
            for (int i = 0; i <= gridLines; i++)
            {
                float gx = mx + (mapWidth / gridLines) * i;
                float gy = my + (mapHeight / gridLines) * i;

                GUI.color = new Color(0.2f, 0.3f, 0.2f,
                    0.3f * mapAlpha);
                GUI.DrawTexture(new Rect(gx, my, 1f, mapHeight),
                    Texture2D.whiteTexture);
                GUI.DrawTexture(new Rect(mx, gy, mapWidth, 1f),
                    Texture2D.whiteTexture);
            }
        }

        // ── MAP BORDER ──
        GUI.color = new Color(0.4f, 0.5f, 0.35f, mapAlpha);
        GUI.DrawTexture(new Rect(mx, my, mapWidth, 2f),
            Texture2D.whiteTexture);
        GUI.DrawTexture(new Rect(mx, my + mapHeight - 2f,
            mapWidth, 2f), Texture2D.whiteTexture);
        GUI.DrawTexture(new Rect(mx, my, 2f, mapHeight),
            Texture2D.whiteTexture);
        GUI.DrawTexture(new Rect(mx + mapWidth - 2f, my,
            2f, mapHeight), Texture2D.whiteTexture);

        // ── LOCATION MARKERS ──
        

        // ── PLAYER ARROW ──
        if (player != null)
        {
            Vector2 playerMapPos = WorldToMap(
                player.position, mx, my);

            // Clamp to map bounds
            playerMapPos.x = Mathf.Clamp(
                playerMapPos.x, mx + 10f, mx + mapWidth - 10f);
            playerMapPos.y = Mathf.Clamp(
                playerMapPos.y, my + 10f, my + mapHeight - 10f);

            // Get player rotation on Y axis
            float angle = player.eulerAngles.y;
            float rad = angle * Mathf.Deg2Rad;

            // Arrow size
            float arrowSize = 14f;

            // Calculate arrow triangle points
            // Tip (forward direction)
            float tipX = playerMapPos.x +
                Mathf.Sin(rad) * arrowSize;
            float tipY = playerMapPos.y -
                Mathf.Cos(rad) * arrowSize;

            // Left base
            float leftX = playerMapPos.x +
                Mathf.Sin(rad - 2.4f) * arrowSize * 0.6f;
            float leftY = playerMapPos.y -
                Mathf.Cos(rad - 2.4f) * arrowSize * 0.6f;

            // Right base
            float rightX = playerMapPos.x +
                Mathf.Sin(rad + 2.4f) * arrowSize * 0.6f;
            float rightY = playerMapPos.y -
                Mathf.Cos(rad + 2.4f) * arrowSize * 0.6f;

            // Draw arrow outline (black)
            float outS = 2f;
            GUI.color = new Color(0f, 0f, 0f, mapAlpha * 0.8f);
            DrawTriangle(
                new Vector2(tipX, tipY),
                new Vector2(leftX - outS, leftY + outS),
                new Vector2(rightX + outS, rightY + outS),
                outS);

            // Draw arrow (yellow/gold)
            GUI.color = new Color(1f, 0.9f, 0.2f, mapAlpha);
            DrawTriangle(
                new Vector2(tipX, tipY),
                new Vector2(leftX, leftY),
                new Vector2(rightX, rightY),
                1.5f);

            // Center dot
            GUI.color = new Color(1f, 1f, 1f, mapAlpha);
            GUI.DrawTexture(new Rect(
                playerMapPos.x - 3f,
                playerMapPos.y - 3f,
                6f, 6f), Texture2D.whiteTexture);
        }

        GUI.color = Color.white;

        // ── TITLE ──
        GUIStyle titleStyle = new GUIStyle();
        titleStyle.fontSize = (int)(Screen.height * 0.025f);
        titleStyle.fontStyle = FontStyle.Bold;
        titleStyle.alignment = TextAnchor.MiddleCenter;
        titleStyle.normal.textColor =
            new Color(0.85f, 0.82f, 0.7f, mapAlpha);
        GUI.Label(new Rect(cx - 200f, my - 40f, 400f, 35f),
            "MAP", titleStyle);

        // ── CLOSE HINT ──
        GUIStyle hintStyle = new GUIStyle();
        hintStyle.fontSize = (int)(Screen.height * 0.016f);
        hintStyle.alignment = TextAnchor.MiddleCenter;
        hintStyle.normal.textColor =
            new Color(0.5f, 0.5f, 0.5f, mapAlpha * 0.8f);
        GUI.Label(new Rect(cx - 200f, my + mapHeight + 10f,
            400f, 25f), "Press M to close", hintStyle);

        // ── LEGEND ──
        GUIStyle legendTitle = new GUIStyle();
        legendTitle.fontSize = (int)(Screen.height * 0.016f);
        legendTitle.fontStyle = FontStyle.Bold;
        legendTitle.normal.textColor =
            new Color(0.7f, 0.7f, 0.6f, mapAlpha);

        GUIStyle legendText = new GUIStyle();
        legendText.fontSize = (int)(Screen.height * 0.014f);
        legendText.normal.textColor =
            new Color(0.6f, 0.6f, 0.5f, mapAlpha);

        float lx = mx + mapWidth + 20f;
        float ly = my;

        GUI.Label(new Rect(lx, ly, 120f, 20f),
            "LEGEND", legendTitle);

        // Player arrow legend
        GUI.color = new Color(1f, 0.9f, 0.2f, mapAlpha);
        GUI.DrawTexture(new Rect(lx, ly + 28f, 12f, 12f),
            Texture2D.whiteTexture);
        GUI.color = Color.white;
        GUI.Label(new Rect(lx + 18f, ly + 26f, 100f, 18f),
            "You", legendText);

        // Location markers legend
        
    }

    void DrawTriangle(Vector2 p1, Vector2 p2,
        Vector2 p3, float lineWidth)
    {
        DrawLine(p1, p2, lineWidth);
        DrawLine(p2, p3, lineWidth);
        DrawLine(p3, p1, lineWidth);

        // Fill triangle with small rects
        float minX = Mathf.Min(p1.x, p2.x, p3.x);
        float maxX = Mathf.Max(p1.x, p2.x, p3.x);
        float minY = Mathf.Min(p1.y, p2.y, p3.y);
        float maxY = Mathf.Max(p1.y, p2.y, p3.y);

        for (float y = minY; y <= maxY; y += 1.5f)
        {
            float x1 = maxX, x2 = minX;
            if (IsInsideTriangle(new Vector2(minX, y),
                p1, p2, p3)) x2 = minX;
            if (IsInsideTriangle(new Vector2(maxX, y),
                p1, p2, p3)) x1 = maxX;

            for (float x = minX; x <= maxX; x += 1f)
            {
                if (IsInsideTriangle(new Vector2(x, y),
                    p1, p2, p3))
                {
                    if (x < x2) x2 = x;
                    if (x > x1) x1 = x;
                }
            }

            if (x1 >= x2)
                GUI.DrawTexture(new Rect(x2, y,
                    x1 - x2 + 1f, 1.5f),
                    Texture2D.whiteTexture);
        }
    }

    bool IsInsideTriangle(Vector2 p,
        Vector2 a, Vector2 b, Vector2 c)
    {
        float d1 = Sign(p, a, b);
        float d2 = Sign(p, b, c);
        float d3 = Sign(p, c, a);

        bool hasNeg = (d1 < 0) || (d2 < 0) || (d3 < 0);
        bool hasPos = (d1 > 0) || (d2 > 0) || (d3 > 0);

        return !(hasNeg && hasPos);
    }

    float Sign(Vector2 p1, Vector2 p2, Vector2 p3)
    {
        return (p1.x - p3.x) * (p2.y - p3.y) -
               (p2.x - p3.x) * (p1.y - p3.y);
    }

    void DrawLine(Vector2 from, Vector2 to, float width)
    {
        float angle = Mathf.Atan2(
            to.y - from.y, to.x - from.x) * Mathf.Rad2Deg;
        float length = Vector2.Distance(from, to);

        GUIUtility.RotateAroundPivot(angle, from);
        GUI.DrawTexture(new Rect(from.x, from.y - width / 2f,
            length, width), Texture2D.whiteTexture);
        GUIUtility.RotateAroundPivot(-angle, from);
    }
}