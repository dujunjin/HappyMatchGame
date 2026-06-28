using UnityEngine;

/// <summary>
/// Generates frosted-glass sprites with rounded corners, gradient fill,
/// border highlight, and subtle noise — simulating Apple-style glassmorphism
/// without requiring URP or post-processing.
///
/// Matches the HTML prototype CSS:
///   .board  { background: rgba(255,255,255,0.12); border: 1px solid rgba(255,255,255,0.18); border-radius: 20px; }
///   .cell   { background: rgba(255,255,255,0.048); border: 1px solid rgba(255,255,255,0.072); border-radius: 8px; }
///   .topbar { background: rgba(255,255,255,0.12); border: 1px solid rgba(255,255,255,0.18); border-radius: 20px; }
/// </summary>
public static class GlassPanelTexture
{
    /// <summary>
    /// Creates a square frosted glass panel sprite with 9-slice borders.
    /// Used for square elements (cells, small panels).
    /// </summary>
    /// <param name="size">Texture size (square, e.g. 256)</param>
    /// <param name="cornerRadius">Corner radius in pixels</param>
    /// <param name="tint">Base color (RGB) and opacity (A)</param>
    /// <param name="borderAlpha">Border highlight alpha (0-1)</param>
    /// <param name="topGlow">Top inner highlight strength (0-1)</param>
    /// <param name="noiseStrength">Subtle frost noise (0-1)</param>
    public static Sprite CreateGlassPanel(
        int size, float cornerRadius,
        Color tint, float borderAlpha = 0.18f,
        float topGlow = 0.15f, float noiseStrength = 0.015f)
    {
        return CreateRectGlassPanel(size, size, cornerRadius, tint, borderAlpha, topGlow, noiseStrength);
    }

    /// <summary>
    /// Creates a RECTANGULAR frosted glass panel sprite at the exact target
    /// aspect ratio. This is the key method for non-square panels (board
    /// backdrop, top bar) — generating at the correct proportions means the
    /// rounded corners are never stretched into ovals by localScale.
    ///
    /// The texture has:
    /// - SDF-based rounded rectangle mask (smooth anti-aliased edges)
    /// - Vertical gradient (brighter top, darker bottom) for depth
    /// - Border glow (white highlight near edges, 3px wide)
    /// - Top inner highlight (bright line along top edge)
    /// - Deterministic noise for frost texture
    /// - 9-slice borders for minor stretching
    /// </summary>
    /// <param name="pixelWidth">Texture width in pixels</param>
    /// <param name="pixelHeight">Texture height in pixels</param>
    /// <param name="cornerRadius">Corner radius in pixels</param>
    /// <param name="tint">Base color (RGB) and opacity (A)</param>
    /// <param name="borderAlpha">Border highlight alpha (0-1)</param>
    /// <param name="topGlow">Top inner highlight strength (0-1)</param>
    /// <param name="noiseStrength">Subtle frost noise (0-1)</param>
    public static Sprite CreateRectGlassPanel(
        int pixelWidth, int pixelHeight, float cornerRadius,
        Color tint, float borderAlpha = 0.30f,
        float topGlow = 0.20f, float noiseStrength = 0.015f)
    {
        Texture2D tex = new Texture2D(pixelWidth, pixelHeight, TextureFormat.RGBA32, false);
        tex.filterMode = FilterMode.Bilinear;
        tex.wrapMode = TextureWrapMode.Clamp;
        tex.anisoLevel = 0;

        Color[] pixels = new Color[pixelWidth * pixelHeight];
        float invH = 1f / Mathf.Max(1, pixelHeight - 1);
        float borderWidth = 6f; // wider border glow simulates blur diffusion

        for (int y = 0; y < pixelHeight; y++)
        {
            for (int x = 0; x < pixelWidth; x++)
            {
                // SDF for rounded rectangle: negative=inside, positive=outside
                float sdf = RoundedRectSDF(x + 0.5f, y + 0.5f, pixelWidth, pixelHeight, cornerRadius);

                // Alpha mask with sub-pixel anti-aliasing
                float maskAlpha = Mathf.Clamp01(0.5f - sdf);

                // Gradient: y=pixelHeight-1 is top (brighter), y=0 is bottom (darker)
                float gradient = y * invH;

                // Border glow: strongest right inside the rounded edge
                float insideDist = -sdf;
                float borderGlow = 1f - Mathf.Clamp01(insideDist / borderWidth);

                // Top inner highlight: bright line at the very top edge
                float topHighlight = Mathf.Clamp01((y - (pixelHeight - 5)) / 4f) * topGlow;

                // Multi-octave noise for fake blur / frost effect:
                // 3 layers at different frequencies simulate the grain of frosted glass
                float n1 = HashNoise(x, y) * noiseStrength;
                float n2 = HashNoise(x / 3, y / 3) * noiseStrength * 0.6f;
                float n3 = HashNoise(x / 8, y / 8) * noiseStrength * 0.4f;
                float noise = n1 + n2 + n3;

                // Soft inner glow: simulates light scattering through frosted glass
                float scatterGlow = Mathf.Clamp01(0.3f - insideDist * 0.02f) * 0.04f;

                // Combine: fill + border glow + top highlight + noise + scatter
                float fillAlpha = tint.a * (0.6f + 0.4f * gradient);
                float alpha = maskAlpha * (fillAlpha
                                           + borderAlpha * borderGlow
                                           + topHighlight
                                           + noise
                                           + scatterGlow);

                Color c = tint;
                c.a = Mathf.Clamp01(alpha);
                pixels[y * pixelWidth + x] = c;
            }
        }

        tex.SetPixels(pixels);
        tex.Apply();

        // 9-slice borders for minor stretching (corners stay crisp)
        float br = Mathf.Min(cornerRadius, pixelWidth * 0.25f, pixelHeight * 0.25f);
        var border = new Vector4(br, br, br, br);
        return Sprite.Create(tex, new Rect(0, 0, pixelWidth, pixelHeight),
            new Vector2(0.5f, 0.5f), 100f, 0, SpriteMeshType.FullRect, border);
    }

    /// <summary>
    /// Creates a simple solid rounded-rectangle sprite (no gradient or effects).
    /// Used for Dynamic Island, home indicator, etc.
    /// </summary>
    public static Sprite CreateRoundedRect(int w, int h, float r, Color color)
    {
        int texSize = Mathf.Max(w, h);
        Texture2D tex = new Texture2D(texSize, texSize, TextureFormat.RGBA32, false);
        tex.filterMode = FilterMode.Bilinear;
        tex.wrapMode = TextureWrapMode.Clamp;

        Color[] pixels = new Color[texSize * texSize];

        for (int y = 0; y < texSize; y++)
        {
            for (int x = 0; x < texSize; x++)
            {
                float sdf = RoundedRectSDF(x + 0.5f, y + 0.5f, texSize, texSize, r);
                float alpha = Mathf.Clamp01(0.5f - sdf);
                Color c = color;
                c.a = color.a * alpha;
                pixels[y * texSize + x] = c;
            }
        }

        tex.SetPixels(pixels);
        tex.Apply();

        var border = new Vector4(r, r, r, r);
        return Sprite.Create(tex, new Rect(0, 0, texSize, texSize),
            new Vector2(0.5f, 0.5f), 100f, 0, SpriteMeshType.FullRect, border);
    }

    /// <summary>
    /// Signed distance field for a rounded rectangle centered in a w×h area.
    /// Negative = inside, positive = outside, 0 = on the edge.
    /// Uses float coordinates for sub-pixel accuracy.
    /// </summary>
    private static float RoundedRectSDF(float px, float py, float w, float h, float r)
    {
        float halfW = w * 0.5f, halfH = h * 0.5f;
        float dx = Mathf.Abs(px - halfW) - (halfW - r);
        float dy = Mathf.Abs(py - halfH) - (halfH - r);
        float ax = Mathf.Max(dx, 0f);
        float ay = Mathf.Max(dy, 0f);
        // CRITICAL: the "- r" term is required! Without it, the SDF computes
        // the distance to the inner rectangle (half-extents minus r), NOT the
        // rounded rectangle. Points in the border/corner region between the
        // inner rect and the rounded boundary would be treated as "outside"
        // (transparent), making the texture look like a sharp rectangle.
        return Mathf.Sqrt(ax * ax + ay * ay) + Mathf.Min(Mathf.Max(dx, dy), 0f) - r;
    }

    /// <summary>
    /// Deterministic hash-based noise in [0, 1).
    /// </summary>
    private static float HashNoise(int x, int y)
    {
        float h = Mathf.Sin(x * 12.9898f + y * 78.233f) * 43758.5453f;
        return h - Mathf.Floor(h);
    }
}
