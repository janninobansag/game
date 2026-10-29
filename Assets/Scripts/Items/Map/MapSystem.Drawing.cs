// Immediate-mode drawing helpers for MapSystem.
using UnityEngine;

public partial class MapSystem
{
    private void DrawPlayerDirectionMarker(Vector2 position)
    {
        if (playerArrowTexture == null)
            playerArrowTexture = CreatePlayerArrowTexture();

        float angle = playerView != null
            ? playerView.eulerAngles.y
            : player.eulerAngles.y;
        Matrix4x4 previousMatrix = GUI.matrix;
        Color previousColor = GUI.color;
        GUIUtility.RotateAroundPivot(angle, position);

        GUI.color = new Color(0f, 0f, 0f, mapAlpha * 0.9f);
        GUI.DrawTexture(new Rect(position.x - 16f, position.y - 20f, 32f, 40f),
            playerArrowTexture);

        GUI.color = new Color(1f, 0.9f, 0.2f, mapAlpha);
        GUI.DrawTexture(new Rect(position.x - 11f, position.y - 15f, 22f, 30f),
            playerArrowTexture);

        GUI.matrix = previousMatrix;
        GUI.color = previousColor;
    }

    private Texture2D CreatePlayerArrowTexture()
    {
        const int textureWidth = 32;
        const int textureHeight = 40;
        Texture2D texture = new Texture2D(textureWidth, textureHeight, TextureFormat.RGBA32, false)
        {
            name = "Map Player Direction Triangle",
            filterMode = FilterMode.Bilinear,
            wrapMode = TextureWrapMode.Clamp,
            hideFlags = HideFlags.HideAndDontSave
        };

        Color32[] pixels = new Color32[textureWidth * textureHeight];
        for (int y = 0; y < textureHeight; y++)
        {
            float halfWidth = (1f - y / (float)(textureHeight - 1)) *
                (textureWidth - 1) * 0.5f;
            for (int x = 0; x < textureWidth; x++)
            {
                if (Mathf.Abs(x - (textureWidth - 1) * 0.5f) <= halfWidth)
                    pixels[y * textureWidth + x] = Color.white;
            }
        }

        texture.SetPixels32(pixels);
        texture.Apply();
        return texture;
    }
}
