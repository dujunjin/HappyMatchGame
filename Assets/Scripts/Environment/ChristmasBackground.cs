using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Phase E art upgrade: composes the Christmas-night scene behind the board
/// from procedural ChristmasArt sprites — deep-indigo sky with a warm-glow
/// moon, a snow-capped pine forest band, a red-roofed cabin with a warm
/// window, a tall Christmas tree with blinking colored lights + a rotating
/// gold star that sheds star particles, gifts, a red sleigh, two reindeer,
/// Santa walking over with a "shh" gesture, and a snow ground. All at
/// sortingOrder &lt; 0 so the board (sortingOrder 1) renders on top.
///
/// Animated in Update: tree lights blink on a staggered rhythm, the tree-top
/// star rotates and periodically emits a gold spark.
/// </summary>
public class ChristmasBackground : MonoBehaviour
{
    private readonly List<SpriteRenderer> _treeLights = new List<SpriteRenderer>();
    private Transform _treeStar;
    private float _sparkTimer;
    private Sprite _sparkSprite;

    private void Awake()
    {
        Camera cam = Camera.main;
        if (cam == null) return;
        float viewH = cam.orthographicSize * 2f;
        float viewW = viewH * cam.aspect;

        // Sky.
        Sprite sky = ChristmasArt.CreateSkySprite();
        MakeSpriteGO("Sky", sky, new Vector3(0, 0, 0), ScaleToCover(sky, viewW, viewH), -12, Color.white);

        // Moon + glow (upper right).
        Vector3 moonPos = new Vector3(viewW * 0.30f, viewH * 0.30f, 0f);
        Sprite glow = ChristmasArt.CreateGlowSprite(new Color(1f, 0.9f, 0.7f));
        MakeSpriteGO("MoonGlow", glow, moonPos, Vector3.one * 3f, -11, Color.white);
        Sprite moon = ChristmasArt.CreateMoonSprite();
        MakeSpriteGO("Moon", moon, moonPos, Vector3.one * 1.4f, -10, Color.white);

        // Snow ground.
        Sprite ground = ChristmasArt.CreateGroundSprite();
        Vector3 groundPos = new Vector3(0f, -cam.orthographicSize * 0.78f, 0f);
        MakeSpriteGO("Ground", ground, groundPos, ScaleToCover(ground, viewW + 4f, viewH * 0.35f), -10, Color.white);

        // Pine forest band (behind the figures, below the board).
        Sprite forest = ChristmasArt.CreatePineRowSprite();
        MakeSpriteGO("Forest", forest, new Vector3(0f, -cam.orthographicSize * 0.30f, 0f),
            ScaleToCover(forest, viewW + 4f, viewH * 0.28f), -9, new Color(1f, 1f, 1f, 0.95f));

        // Cabin (left).
        Sprite cabin = ChristmasArt.CreateCabinSprite();
        MakeSpriteGO("Cabin", cabin, new Vector3(-viewW * 0.30f, -cam.orthographicSize * 0.30f, 0f),
            Vector3.one * 1.3f, -8, Color.white);

        // Christmas tree (right) + body.
        Vector3 treePos = new Vector3(viewW * 0.28f, -cam.orthographicSize * 0.30f, 0f);
        Sprite treeBody = ChristmasArt.CreateChristmasTreeBodySprite();
        GameObject treeGO = MakeSpriteGO("XmasTree", treeBody, treePos, Vector3.one * 1.4f, -8, Color.white);

        // Gifts under the tree.
        Color[] giftColors = { new Color(0.9f, 0.2f, 0.2f), new Color(0.2f, 0.5f, 0.9f),
                                new Color(1f, 0.83f, 0.3f), new Color(0.3f, 0.8f, 0.4f) };
        for (int i = 0; i < 5; i++)
        {
            Sprite gift = ChristmasArt.CreateGiftSprite(giftColors[i % giftColors.Length]);
            Vector3 gp = treePos + new Vector3(Random.Range(-0.6f, 0.6f), -1.2f - (i % 2) * 0.25f, 0f);
            MakeSpriteGO("Gift", gift, gp, Vector3.one * 0.9f, -7, Color.white);
        }

        // Tree lights (colored dots blinking).
        Color[] lightCols = { new Color(1f, 0.2f, 0.2f), new Color(1f, 0.83f, 0.3f),
                              new Color(0.3f, 0.6f, 1f), new Color(0.3f, 1f, 0.4f) };
        Sprite lightSprite = SpriteGenerator.CreateCircleSprite(Color.white);
        _sparkSprite = SpriteGenerator.CreateStarSprite(new Color(1f, 0.85f, 0.3f), 24);
        for (int i = 0; i < 14; i++)
        {
            Vector3 lp = treePos + new Vector3(Random.Range(-0.7f, 0.7f), Random.Range(-0.4f, 1.6f), 0f);
            GameObject lgo = MakeSpriteGO("Light", lightSprite, lp, Vector3.one * 0.10f, -6, lightCols[i % lightCols.Length]);
            _treeLights.Add(lgo.GetComponent<SpriteRenderer>());
        }

        // Tree-top star (rotating, emits sparks).
        Vector3 starPos = treePos + new Vector3(0f, 1.9f, 0f);
        Sprite starSprite = SpriteGenerator.CreateStarSprite(new Color(1f, 0.83f, 0.3f), 32);
        GameObject starGO = MakeSpriteGO("TreeStar", starSprite, starPos, Vector3.one * 0.5f, -6, Color.white);
        _treeStar = starGO.transform;

        // Sleigh + reindeer + santa (foreground bottom).
        Sprite sleigh = ChristmasArt.CreateSleighSprite();
        MakeSpriteGO("Sleigh", sleigh, new Vector3(-viewW * 0.18f, -cam.orthographicSize * 0.62f, 0f),
            Vector3.one * 1.1f, -6, Color.white);

        Sprite reindeer = ChristmasArt.CreateReindeerSprite();
        GameObject r1 = MakeSpriteGO("Reindeer1", reindeer, new Vector3(-viewW * 0.08f, -cam.orthographicSize * 0.60f, 0f),
            Vector3.one * 0.9f, -6, Color.white);
        GameObject r2 = MakeSpriteGO("Reindeer2", reindeer, new Vector3(viewW * 0.02f, -cam.orthographicSize * 0.60f, 0f),
            Vector3.one * 0.9f, -6, Color.white);
        r2.transform.localScale = new Vector3(-0.9f, 0.9f, 1f); // flip to face the other way

        Sprite santa = ChristmasArt.CreateSantaSprite();
        MakeSpriteGO("Santa", santa, new Vector3(viewW * 0.10f, -cam.orthographicSize * 0.58f, 0f),
            Vector3.one * 1.0f, -5, Color.white);
    }

    private GameObject MakeSpriteGO(string name, Sprite sprite, Vector3 pos, Vector3 scale, int sortingOrder, Color color)
    {
        GameObject go = new GameObject(name);
        SpriteRenderer sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = sprite;
        sr.sortingOrder = sortingOrder;
        sr.color = color;
        go.transform.SetParent(transform, false);
        go.transform.position = pos;
        go.transform.localScale = scale;
        return go;
    }

    private Vector3 ScaleToCover(Sprite s, float targetW, float targetH)
    {
        const float ppu = 100f;
        float sw = s.textureRect.width / ppu;
        float sh = s.textureRect.height / ppu;
        return new Vector3(targetW / sw, targetH / sh, 1f);
    }

    private void Update()
    {
        // Blink tree lights on a staggered, firefly-like rhythm.
        float t = Time.time;
        for (int i = 0; i < _treeLights.Count; i++)
        {
            var sr = _treeLights[i];
            if (sr == null) continue;
            float phase = i * 0.7f;
            float v = Mathf.Sin((t * 3f) + phase) * 0.5f + 0.5f;
            v = Mathf.Pow(v, 2f);
            Color c = sr.color;
            sr.color = new Color(c.r, c.g, c.b, 0.25f + v * 0.75f);
        }

        // Rotate the tree-top star.
        if (_treeStar != null)
            _treeStar.rotation = Quaternion.Euler(0f, 0f, Time.time * 60f);

        // Emit gold sparks from the star.
        if (_sparkTimer <= 0f && _treeStar != null)
        {
            _sparkTimer = 0.18f;
            GameObject go = new GameObject("StarSpark");
            SpriteRenderer sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = _sparkSprite;
            sr.sortingOrder = -5;
            sr.color = new Color(1f, 0.85f, 0.3f, 1f);
            go.transform.position = _treeStar.position + (Vector3)Random.insideUnitCircle * 0.15f;
            go.transform.localScale = Vector3.one * 0.08f;
            StartCoroutine(SparkFall(go, sr));
        }
        _sparkTimer -= Time.deltaTime;
    }

    private System.Collections.IEnumerator SparkFall(GameObject go, SpriteRenderer sr)
    {
        Vector3 vel = new Vector3(Random.Range(-0.4f, 0.4f), Random.Range(0.2f, 0.6f), 0f);
        float life = 1.0f;
        float e = 0f;
        Color c = sr != null ? sr.color : Color.white;
        while (e < life && go != null)
        {
            e += Time.deltaTime;
            vel.y -= 1.2f * Time.deltaTime;
            go.transform.position += vel * Time.deltaTime;
            if (sr != null) sr.color = new Color(c.r, c.g, c.b, 1f - e / life);
            yield return null;
        }
        if (go != null) Destroy(go);
    }
}
