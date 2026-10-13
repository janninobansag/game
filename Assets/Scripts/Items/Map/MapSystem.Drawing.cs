// Immediate-mode drawing helpers for MapSystem.
using UnityEngine;

public partial class MapSystem
{
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
    }}