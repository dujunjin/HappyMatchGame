using UnityEngine;

/// <summary>
/// Phase E: a procedural winter-night background rendered behind the board.
/// Generates a vertical cold-blue gradient with a strip of dark silhouettes
/// (trees + houses) along the lower third, all on one texture so there are no
/// imported assets. The sprite is sized to cover the camera's orthographic
/// view and placed at sortingOrder -10 (behind every board element).
/// </summary>
public class WinterBackground : MonoBehaviour
{
    private void Awake()
    {
        Camera cam = Camera.main;
        if (cam == null) return;

        // Texture resolution (keep small; it's a soft gradient).
        int w = 128, h = 256;
        Texture2D tex = new Texture2D(w, h);
        tex.filterMode = FilterMode.Bilinear;
        tex.wrapMode = TextureWrapMode.Clamp;

        Color top = new Color(0.05f, 0.07f, 0.16f, 1f);       // cold deep blue
        Color mid = new Color(0.08f, 0.11f, 0.22f, 1f);
        Color bottom = new Color(0.13f, 0.16f, 0.27f, 1f);    // slightly lifted near the ground
        Color silhouette = new Color(0.02f, 0.03f, 0.06f, 1f);

        for (int y = 0; y < h; y++)
        {
            float t = (float)y / (h - 1); // 0 bottom -> 1 top
            Color c;
            if (t < 0.6f) c = Color.Lerp(bottom, mid, t / 0.6f);
            else c = Color.Lerp(mid, top, (t - 0.6f) / 0.4f);

            for (int x = 0; x < w; x++)
            {
                tex.SetPixel(x, y, c);
            }
        }

        // Silhouette strip in the lower third: a row of dark triangles (trees)
        // and rectangles (houses) of varying heights.
        int stripTop = Mathf.RoundToInt(h * 0.38f);
        System.Random rng = new System.Random(42); // deterministic silhouette layout
        int xCursor = 0;
        while (xCursor < w)
        {
            int kind = rng.Next(2);
            int width = rng.Next(4, 10);
            int peak = rng.Next(8, 24);
            int baseY = 0;
            for (int x = xCursor; x < xCursor + width && x < w; x++)
            {
                int topY;
                if (kind == 0)
                {
                    // Triangle (tree): peak in the middle.
                    float dx = Mathf.Abs(x - (xCursor + width * 0.5f)) / (width * 0.5f);
                    topY = baseY + Mathf.RoundToInt(peak * (1f - dx));
                }
                else
                {
                    // Rectangle (house) with a flat-ish roof.
                    topY = baseY + peak - (x == xCursor || x == xCursor + width - 1 ? 1 : 0);
                }
                for (int y = baseY; y <= topY && y < stripTop; y++)
                {
                    tex.SetPixel(x, y, silhouette);
                }
            }
            xCursor += width + rng.Next(0, 3);
        }
        tex.Apply();

        Sprite sprite = Sprite.Create(tex, new Rect(0, 0, w, h), new Vector2(0.5f, 0.5f));

        // Put the SpriteRenderer on this MonoBehaviour's own GameObject
        // (GameManager created it via AddComponent<WinterBackground>()).
        SpriteRenderer sr = gameObject.GetComponent<SpriteRenderer>();
        if (sr == null) sr = gameObject.AddComponent<SpriteRenderer>();
        sr.sprite = sprite;
        sr.sortingOrder = -10;
        sr.color = Color.white;

        // Size to cover the camera view (with a margin). Sprite world size at
        // scale 1 = (w/PPU, h/PPU) with PPU=100, so scale = targetWorld / spriteWorld.
        const float ppu = 100f;
        float spriteW = w / ppu;
        float spriteH = h / ppu;
        float viewH = cam.orthographicSize * 2f;
        float viewW = viewH * cam.aspect;
        transform.localScale = new Vector3((viewW + 2f) / spriteW, (viewH + 2f) / spriteH, 1f);
        transform.position = new Vector3(0f, 0f, 0f);
    }
}
