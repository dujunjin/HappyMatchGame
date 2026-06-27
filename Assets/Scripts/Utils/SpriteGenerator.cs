using UnityEngine;

/// <summary>
/// Generates colored circle sprites at runtime.
/// </summary>
public static class SpriteGenerator
{
    public static Sprite CreateCircleSprite(Color color, int size = 64)
    {
        Texture2D tex = new Texture2D(size, size);
        tex.filterMode = FilterMode.Bilinear;

        float radius = size * 0.45f;
        Vector2 center = new Vector2(size / 2f, size / 2f);

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float dist = Vector2.Distance(new Vector2(x, y), center);
                if (dist <= radius)
                {
                    float alpha = 1f - (dist / radius) * 0.15f;
                    tex.SetPixel(x, y, new Color(color.r, color.g, color.b, alpha));
                }
                else
                {
                    tex.SetPixel(x, y, Color.clear);
                }
            }
        }
        tex.Apply();

        return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f));
    }

    public static Sprite CreateSuitcaseSprite(Color color, int size = 64)
    {
        Texture2D tex = new Texture2D(size, size);
        tex.filterMode = FilterMode.Bilinear;

        float padding = size * 0.12f;
        Color outlineColor = new Color(color.r * 0.6f, color.g * 0.6f, color.b * 0.6f, 1f);

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                bool inRect = x >= padding && x < size - padding && y >= padding && y < size - padding;
                bool onBorder = x < padding + 3 || x >= size - padding - 3 ||
                                y < padding + 3 || y >= size - padding - 3;

                if (inRect)
                {
                    if (onBorder)
                        tex.SetPixel(x, y, outlineColor);
                    else
                        tex.SetPixel(x, y, color);
                }
                else
                {
                    tex.SetPixel(x, y, Color.clear);
                }
            }
        }
        tex.Apply();

        return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f));
    }

    public static Sprite CreateRocketSprite(Color color, int size = 64)
    {
        Texture2D tex = new Texture2D(size, size);
        tex.filterMode = FilterMode.Bilinear;

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float dist = Vector2.Distance(new Vector2(x, y), new Vector2(size / 2f, size / 2f));
                if (dist <= size * 0.4f)
                {
                    float alpha = 1f;
                    tex.SetPixel(x, y, new Color(color.r, color.g, color.b, alpha));
                }
                else
                {
                    tex.SetPixel(x, y, Color.clear);
                }
            }
        }
        tex.Apply();

        // Add a white star/cross indicator
        for (int i = 0; i < size; i++)
        {
            tex.SetPixel(size / 2, i, Color.white);
            tex.SetPixel(i, size / 2, Color.white);
        }
        tex.Apply();

        return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f));
    }

    public static Sprite CreateBombSprite(Color color, int size = 64)
    {
        Texture2D tex = new Texture2D(size, size);
        tex.filterMode = FilterMode.Bilinear;

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float dist = Vector2.Distance(new Vector2(x, y), new Vector2(size / 2f, size / 2f));
                if (dist <= size * 0.4f)
                {
                    float alpha = 1f;
                    tex.SetPixel(x, y, new Color(color.r, color.g, color.b, alpha));
                }
                else
                {
                    tex.SetPixel(x, y, Color.clear);
                }
            }
        }
        tex.Apply();

        // Add a circle indicator
        float indicatorRadius = size * 0.18f;
        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float dist = Vector2.Distance(new Vector2(x, y), new Vector2(size / 2f, size / 2f));
                if (dist <= indicatorRadius)
                {
                    tex.SetPixel(x, y, Color.black);
                }
            }
        }
        tex.Apply();

        return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f));
    }

    /// <summary>
    /// Procedural propeller: a colored hub with two crossed rotor blades and a
    /// white center cap. Distinguished from rocket/bomb by the blade shape.
    /// </summary>
    public static Sprite CreatePropellerSprite(Color color, int size = 64)
    {
        Texture2D tex = new Texture2D(size, size);
        tex.filterMode = FilterMode.Bilinear;

        Vector2 center = new Vector2(size / 2f, size / 2f);
        float hubRadius = size * 0.18f;
        float bladeLength = size * 0.42f;
        float bladeHalfWidth = size * 0.10f;

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                Vector2 p = new Vector2(x, y);
                Vector2 d = p - center;
                float dist = d.magnitude;

                Color c = Color.clear;

                // Hub
                if (dist <= hubRadius)
                {
                    c = color;
                }

                // Two crossed blades (along X and Y axes -> a plus/cross).
                if (dist <= bladeLength)
                {
                    if (Mathf.Abs(d.y) <= bladeHalfWidth || Mathf.Abs(d.x) <= bladeHalfWidth)
                    {
                        // Blade: slightly brighter than hub for contrast.
                        c = new Color(color.r * 0.75f + 0.25f, color.g * 0.75f + 0.25f, color.b * 0.75f + 0.25f, 1f);
                    }
                }

                tex.SetPixel(x, y, c);
            }
        }

        // White center cap
        float capRadius = size * 0.09f;
        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                if (Vector2.Distance(new Vector2(x, y), center) <= capRadius)
                    tex.SetPixel(x, y, Color.white);
            }
        }
        tex.Apply();

        return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f));
    }

    /// <summary>
    /// A solid square sprite (used for confetti / debris). Full alpha inside
    /// the rect, transparent outside.
    /// </summary>
    public static Sprite CreateSquareSprite(Color color, int size = 16)
    {
        Texture2D tex = new Texture2D(size, size);
        tex.filterMode = FilterMode.Bilinear;
        for (int y = 0; y < size; y++)
            for (int x = 0; x < size; x++)
                tex.SetPixel(x, y, color);
        tex.Apply();
        return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f));
    }

    /// <summary>
    /// A white starburst: rays radiating from the center, fading with
    /// distance. Used behind the big suitcase in the win sequence.
    /// </summary>
    public static Sprite CreateStarburstSprite(int size = 128, int rays = 12)
    {
        Texture2D tex = new Texture2D(size, size);
        tex.filterMode = FilterMode.Bilinear;
        Vector2 center = new Vector2(size / 2f, size / 2f);
        float maxDist = size * 0.5f;

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                Vector2 p = new Vector2(x, y);
                Vector2 d = p - center;
                float dist = d.magnitude;
                float angle = Mathf.Atan2(d.y, d.x);

                // Ray brightness: peaks at angles aligned to ray directions.
                float rayPhase = (angle * rays / (2f * Mathf.PI)) % 1f;
                if (rayPhase < 0f) rayPhase += 1f;
                // Sharp rays: brightness falls off quickly away from a ray axis.
                float ray = 1f - Mathf.Min(rayPhase, 1f - rayPhase) * rays;
                ray = Mathf.Clamp01(ray);

                float falloff = 1f - (dist / maxDist);
                float alpha = Mathf.Clamp01(falloff * 0.85f) * Mathf.Clamp01(ray);
                tex.SetPixel(x, y, new Color(1f, 0.97f, 0.85f, alpha));
            }
        }
        tex.Apply();
        return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f));
    }
}
