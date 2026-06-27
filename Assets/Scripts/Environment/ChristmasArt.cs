using UnityEngine;

/// <summary>
/// Phase E art upgrade: procedural sprites for the Christmas-night scene
/// behind the board. Each method builds a small texture that reads as its
/// subject (moon, pine row, cabin, christmas-tree body, gift, sleigh,
/// reindeer, santa, snow ground) without any imported assets. The animated
/// bits (tree lights, rotating star) are separate GameObjects layered on top
/// by ChristmasBackground.
/// </summary>
public static class ChristmasArt
{
    // --- Sky (deep indigo -> warm purple horizon -> warm moon glow) ---

    public static Sprite CreateSkySprite(int w = 256, int h = 256)
    {
        Texture2D tex = new Texture2D(w, h);
        tex.filterMode = FilterMode.Bilinear;
        tex.wrapMode = TextureWrapMode.Clamp;
        Color top = new Color(0.05f, 0.06f, 0.16f, 1f);
        Color mid = new Color(0.10f, 0.09f, 0.20f, 1f);
        Color horizon = new Color(0.20f, 0.16f, 0.24f, 1f); // warm purple-grey
        for (int y = 0; y < h; y++)
        {
            float t = (float)y / (h - 1);
            Color c = t < 0.55f ? Color.Lerp(top, mid, t / 0.55f) : Color.Lerp(mid, horizon, (t - 0.55f) / 0.45f);
            for (int x = 0; x < w; x++) tex.SetPixel(x, y, c);
        }
        tex.Apply();
        return Sprite.Create(tex, new Rect(0, 0, w, h), new Vector2(0.5f, 0.5f));
    }

    // --- Moon + glow ---

    public static Sprite CreateMoonSprite(int size = 64)
    {
        Texture2D tex = new Texture2D(size, size);
        tex.filterMode = FilterMode.Bilinear;
        Vector2 c = new Vector2(size / 2f, size / 2f);
        float r = size * 0.42f;
        Color cream = new Color(1f, 0.95f, 0.82f, 1f);
        for (int y = 0; y < size; y++)
            for (int x = 0; x < size; x++)
            {
                float d = Vector2.Distance(new Vector2(x, y), c);
                tex.SetPixel(x, y, d <= r ? cream : Color.clear);
            }
        tex.Apply();
        return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f));
    }

    public static Sprite CreateGlowSprite(Color color, int size = 128)
    {
        Texture2D tex = new Texture2D(size, size);
        tex.filterMode = FilterMode.Bilinear;
        Vector2 c = new Vector2(size / 2f, size / 2f);
        float r = size * 0.5f;
        for (int y = 0; y < size; y++)
            for (int x = 0; x < size; x++)
            {
                float d = Vector2.Distance(new Vector2(x, y), c);
                float a = Mathf.Clamp01(1f - d / r);
                a = a * a * 0.5f;
                tex.SetPixel(x, y, new Color(color.r, color.g, color.b, a));
            }
        tex.Apply();
        return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f));
    }

    // --- Pine forest band (dark trees with white snow caps + warm edge) ---

    public static Sprite CreatePineRowSprite(int w = 256, int h = 80)
    {
        Texture2D tex = new Texture2D(w, h);
        tex.filterMode = FilterMode.Bilinear;
        Color dark = new Color(0.04f, 0.06f, 0.10f, 1f);
        Color snow = new Color(0.85f, 0.87f, 0.92f, 1f);
        Color warm = new Color(0.55f, 0.40f, 0.30f, 1f); // warm moonlit edge
        System.Random rng = new System.Random(7);
        int x = 0;
        while (x < w)
        {
            int tw = rng.Next(14, 28);
            int peak = rng.Next(30, 60);
            int baseY = 0;
            int cx = x + tw / 2;
            for (int xx = x; xx < x + tw && xx < w; xx++)
            {
                float dx = Mathf.Abs(xx - cx) / (tw * 0.5f);
                int topY = baseY + Mathf.RoundToInt(peak * (1f - dx));
                for (int yy = baseY; yy <= topY && yy < h; yy++)
                {
                    Color c = dark;
                    // Snow cap near the top.
                    if (yy > topY - 4) c = snow;
                    // Warm edge on the left side (moonlit).
                    if (xx - x < 2 && yy > baseY + 2) c = warm;
                    tex.SetPixel(xx, yy, c);
                }
            }
            x += tw + rng.Next(0, 3);
        }
        tex.Apply();
        return Sprite.Create(tex, new Rect(0, 0, w, h), new Vector2(0.5f, 0.5f));
    }

    // --- Cabin (red roof + wood body + warm window + icicles) ---

    public static Sprite CreateCabinSprite(int size = 64)
    {
        Texture2D tex = new Texture2D(size, size);
        tex.filterMode = FilterMode.Bilinear;
        Color wood = new Color(0.40f, 0.26f, 0.16f, 1f);
        Color roof = new Color(0.65f, 0.18f, 0.16f, 1f);
        Color snow = new Color(0.85f, 0.87f, 0.92f, 1f);
        Color window = new Color(1f, 0.82f, 0.4f, 1f);
        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                Color c = Color.clear;
                // Body.
                if (x >= 12 && x <= 52 && y >= 12 && y <= 36) c = wood;
                // Window.
                if (x >= 24 && x <= 34 && y >= 20 && y <= 30) c = window;
                // Roof (triangle on top).
                int roofTop = 52 - Mathf.Abs(x - 32);
                if (y > 36 && y <= roofTop && x >= 8 && x <= 56) c = roof;
                // Snow on roof.
                if (y > 36 && y <= roofTop && y > roofTop - 3) c = snow;
                // Icicles under the roofline.
                if (y >= 9 && y <= 12 && (x == 16 || x == 24 || x == 40 || x == 48)) c = snow;
                tex.SetPixel(x, y, c);
            }
        }
        tex.Apply();
        return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f));
    }

    // --- Christmas tree body (stacked green triangles; lights are separate) ---

    public static Sprite CreateChristmasTreeBodySprite(int w = 64, int h = 96)
    {
        Texture2D tex = new Texture2D(w, h);
        tex.filterMode = FilterMode.Bilinear;
        Color green = new Color(0.10f, 0.32f, 0.16f, 1f);
        Color dark = new Color(0.06f, 0.22f, 0.12f, 1f);
        // Three tiers.
        DrawTreeTier(tex, 32, 92, 18, 14, green);
        DrawTreeTier(tex, 32, 70, 24, 20, green);
        DrawTreeTier(tex, 32, 46, 30, 26, green);
        // Trunk.
        for (int y = 8; y <= 20; y++)
            for (int x = 28; x <= 36; x++)
                if (x >= 0 && x < w && y >= 0 && y < h) tex.SetPixel(x, y, new Color(0.35f, 0.22f, 0.12f, 1f));
        tex.Apply();
        return Sprite.Create(tex, new Rect(0, 0, w, h), new Vector2(0.5f, 0.5f));
    }

    private static void DrawTreeTier(Texture2D tex, int cx, int topY, int halfW, int height, Color c)
    {
        int w = tex.width, h = tex.height;
        for (int x = cx - halfW; x <= cx + halfW; x++)
        {
            float dx = Mathf.Abs(x - cx) / halfW;
            int bottom = topY - height;
            int yTop = topY - Mathf.RoundToInt(height * dx);
            for (int y = bottom; y <= yTop; y++)
            {
                if (x < 0 || x >= w || y < 0 || y >= h) continue;
                tex.SetPixel(x, y, c);
            }
        }
    }

    // --- Gift box (colored square + ribbon cross) ---

    public static Sprite CreateGiftSprite(Color color, int size = 24)
    {
        Texture2D tex = new Texture2D(size, size);
        tex.filterMode = FilterMode.Bilinear;
        Color ribbon = Color.white;
        for (int y = 0; y < size; y++)
            for (int x = 0; x < size; x++)
            {
                Color c = (x >= 2 && x <= size - 3 && y >= 2 && y <= size - 3) ? color : Color.clear;
                if (x == size / 2 || y == size / 2) c = ribbon;
                tex.SetPixel(x, y, c);
            }
        tex.Apply();
        return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f));
    }

    // --- Sleigh (red boat) ---

    public static Sprite CreateSleighSprite(int w = 64, int h = 32)
    {
        Texture2D tex = new Texture2D(w, h);
        tex.filterMode = FilterMode.Bilinear;
        Color red = new Color(0.78f, 0.14f, 0.14f, 1f);
        Color gold = new Color(1f, 0.83f, 0.30f, 1f);
        for (int y = 0; y < h; y++)
            for (int x = 0; x < w; x++)
            {
                Color c = Color.clear;
                // Hull curve.
                if (y >= 8 && y <= 18 && x >= 4 && x <= 56)
                {
                    float curve = Mathf.Sin((x / (float)w) * Mathf.PI) * 4f;
                    if (y <= 14 + curve) c = red;
                }
                // Gold trim.
                if (y == 9 && x >= 4 && x <= 56) c = gold;
                // Runner (blade) underneath.
                if (y >= 20 && y <= 22 && x >= 8 && x <= 52) c = new Color(0.85f, 0.85f, 0.9f, 1f);
                tex.SetPixel(x, y, c);
            }
        tex.Apply();
        return Sprite.Create(tex, new Rect(0, 0, w, h), new Vector2(0.5f, 0.5f));
    }

    // --- Reindeer silhouette (body + legs + neck/head + antlers) ---

    public static Sprite CreateReindeerSprite(int w = 64, int h = 48)
    {
        Texture2D tex = new Texture2D(w, h);
        tex.filterMode = FilterMode.Bilinear;
        Color body = new Color(0.30f, 0.20f, 0.14f, 1f);
        Color bell = new Color(1f, 0.83f, 0.30f, 1f);
        for (int y = 0; y < h; y++)
            for (int x = 0; x < w; x++)
            {
                Color c = Color.clear;
                // Body (ellipse-ish).
                float bx = (x - 28f) / 16f; float by = (y - 26f) / 9f;
                if (bx * bx + by * by <= 1f) c = body;
                // Legs.
                if (x >= 18 && x <= 21 && y >= 26 && y <= 40) c = body;
                if (x >= 36 && x <= 39 && y >= 26 && y <= 40) c = body;
                // Neck + head.
                if (x >= 44 && x <= 52 && y >= 22 && y <= 32) c = body;
                if (x >= 50 && x <= 58 && y >= 28 && y <= 36) c = body;
                // Antlers.
                if (x >= 50 && x <= 52 && y >= 32 && y <= 42) c = body;
                if (x >= 48 && x <= 50 && y >= 36 && y <= 40) c = body;
                if (x >= 52 && x <= 54 && y >= 36 && y <= 40) c = body;
                // Bell dot.
                if (x == 46 && y == 30) c = bell;
                tex.SetPixel(x, y, c);
            }
        tex.Apply();
        return Sprite.Create(tex, new Rect(0, 0, w, h), new Vector2(0.5f, 0.5f));
    }

    // --- Santa (red coat + hat + white beard + "shh" arm) ---

    public static Sprite CreateSantaSprite(int w = 48, int h = 64)
    {
        Texture2D tex = new Texture2D(w, h);
        tex.filterMode = FilterMode.Bilinear;
        Color red = new Color(0.80f, 0.12f, 0.12f, 1f);
        Color white = new Color(0.95f, 0.95f, 0.95f, 1f);
        Color skin = new Color(0.95f, 0.78f, 0.62f, 1f);
        Color boot = new Color(0.20f, 0.15f, 0.10f, 1f);
        for (int y = 0; y < h; y++)
            for (int x = 0; x < w; x++)
            {
                Color c = Color.clear;
                // Coat (body).
                if (x >= 14 && x <= 34 && y >= 16 && y <= 44) c = red;
                // Coat trim (white).
                if (x >= 14 && x <= 34 && y >= 16 && y <= 19) c = white;
                if (x >= 14 && x <= 34 && y >= 41 && y <= 44) c = white;
                // Belt.
                if (x >= 14 && x <= 34 && y >= 30 && y <= 33) c = boot;
                // Boots.
                if (x >= 16 && x <= 22 && y >= 6 && y <= 16) c = boot;
                if (x >= 26 && x <= 32 && y >= 6 && y <= 16) c = boot;
                // Head + beard.
                if (x >= 20 && x <= 28 && y >= 44 && y <= 52) c = skin; // face
                if (x >= 18 && x <= 30 && y >= 40 && y <= 46) c = white; // beard
                // Hat.
                if (x >= 20 && x <= 30 && y >= 52 && y <= 60) c = red;
                if (x >= 20 && x <= 30 && y >= 58 && y <= 60) c = white; // hat trim
                if (x >= 28 && x <= 32 && y >= 58 && y <= 62) c = white; // pom
                // "Shh" arm (raised to mouth).
                if (x >= 30 && x <= 36 && y >= 44 && y <= 48) c = red;
                if (x >= 34 && x <= 38 && y >= 46 && y <= 50) c = skin;
                tex.SetPixel(x, y, c);
            }
        tex.Apply();
        return Sprite.Create(tex, new Rect(0, 0, w, h), new Vector2(0.5f, 0.5f));
    }

    // --- Snow ground strip ---

    public static Sprite CreateGroundSprite(int w = 256, int h = 32)
    {
        Texture2D tex = new Texture2D(w, h);
        tex.filterMode = FilterMode.Bilinear;
        Color snow = new Color(0.78f, 0.82f, 0.90f, 1f);
        Color shade = new Color(0.62f, 0.68f, 0.80f, 1f);
        for (int x = 0; x < w; x++)
        {
            // Gentle bumps along the top edge.
            float bump = Mathf.Sin(x * 0.12f) * 3f + Mathf.Sin(x * 0.05f) * 5f;
            int top = Mathf.RoundToInt(h * 0.4f + bump);
            for (int y = 0; y < h; y++)
                tex.SetPixel(x, y, y >= top ? (y > top + 4 ? shade : snow) : Color.clear);
        }
        tex.Apply();
        return Sprite.Create(tex, new Rect(0, 0, w, h), new Vector2(0.5f, 0.5f));
    }
}
