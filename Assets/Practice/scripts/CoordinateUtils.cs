using UnityEngine;

public static class CoordinateUtils
{
    public static Vector2 ToNormalized(Vector2 pixelPos, Vector2 areaSize)
    {
        float x = areaSize.x > 0f ? Mathf.Clamp01(pixelPos.x / areaSize.x) : 0f;
        float y = areaSize.y > 0f ? Mathf.Clamp01(pixelPos.y / areaSize.y) : 0f;
        return new Vector2(x, y);
    }

    public static Vector2 ToPixels(Vector2 normalizedPos, Vector2 areaSize)
    {
        return new Vector2(normalizedPos.x * areaSize.x, normalizedPos.y * areaSize.y);
    }
}
