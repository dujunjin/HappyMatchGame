using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;

/// <summary>
/// Phase D: the win sequence (spec §5.4). Played by GameUI.PlayWinSequence
/// once the victory barrier (Phase C) has resolved. Animates:
///   1. Board exit (0.45s scale-down + fade + slide down).
///   2. "Great" text appearing with a 0.24s elastic (back-out) curve, and a
///      background brighten.
///   3. A treasure chest (body + hinged lid) scales in at screen center, the
///      lid swings open with a white flash, and ribbons (long thin colored
///      strips) spray up out of it while fireworks (starburst bursts) pop at
///      random positions above, plus a brief camera shake.
///   4. Retry / Replay buttons. Replay re-runs the finale; Retry reloads.
/// Lose still uses the simple ResultDialog (this sequence is win-only).
/// Chest/ribbons/fireworks are procedural sprites here; Phase E will upgrade
/// them to pooled particles and add the win sound.
/// </summary>
public class WinSequence : MonoBehaviour
{
    private GameManager _gm;
    private Canvas _canvas;
    private readonly List<GameObject> _spawned = new List<GameObject>();

    public void Init(GameManager gm) { _gm = gm; }

    public void Play() { StartCoroutine(Sequence()); }

    private IEnumerator Sequence()
    {
        CreateCanvas();

        // 1. Board exit.
        yield return BoardExit(0.45f);

        // 2-4. Finale.
        yield return Finale();
    }

    /// <summary>Re-run just the finale (Great + burst + buttons).</summary>
    public void Replay()
    {
        CleanupSpawned();
        StartCoroutine(Finale());
    }

    private IEnumerator Finale()
    {
        ShowGreatText();
        BrightenBackground();
        yield return new WaitForSeconds(0.24f);

        yield return OpenChestBurst();

        ShowButtons();
    }

    // ---------------------------------------------------------------------
    //  Board exit
    // ---------------------------------------------------------------------

    private IEnumerator BoardExit(float dur)
    {
        if (_gm == null || _gm.boardController == null) yield break;

        Transform t = _gm.boardController.transform;
        Vector3 basePos = t.position;
        Vector3 baseScale = t.localScale;
        SpriteRenderer[] renderers = _gm.boardController.GetComponentsInChildren<SpriteRenderer>();
        Color[] startColors = new Color[renderers.Length];
        for (int i = 0; i < renderers.Length; i++)
            startColors[i] = renderers[i] != null ? renderers[i].color : Color.white;

        float elapsed = 0f;
        while (elapsed < dur)
        {
            elapsed += Time.deltaTime;
            float k = Mathf.Clamp01(elapsed / dur);
            float e = 1f - (1f - k) * (1f - k); // ease-out quad
            t.localScale = Vector3.Lerp(baseScale, baseScale * 0.8f, e);
            t.position = basePos + Vector3.down * (3f * e);
            for (int i = 0; i < renderers.Length; i++)
            {
                if (renderers[i] != null)
                {
                    Color c = startColors[i];
                    renderers[i].color = new Color(c.r, c.g, c.b, 1f - e);
                }
            }
            yield return null;
        }
    }

    // ---------------------------------------------------------------------
    //  Great text + background brighten
    // ---------------------------------------------------------------------

    private void ShowGreatText()
    {
        GameObject go = new GameObject("GreatText");
        go.transform.SetParent(_canvas.transform, false);
        RectTransform rt = go.AddComponent<RectTransform>();
        rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.62f);
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.sizeDelta = new Vector2(900f, 220f);
        rt.anchoredPosition = Vector2.zero;
        rt.localScale = Vector3.zero;

        TextMeshProUGUI tmp = go.AddComponent<TextMeshProUGUI>();
        tmp.text = "Great!";
        tmp.fontSize = 96;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.color = new Color(1f, 0.85f, 0.2f);
        tmp.fontStyle = FontStyles.Bold;

        _spawned.Add(go);
        StartCoroutine(ElasticIn(go.transform, 0.24f));
    }

    private void BrightenBackground()
    {
        Camera cam = Camera.main;
        if (cam == null) return;
        StartCoroutine(LerpBackgroundColor(cam, new Color(0.25f, 0.30f, 0.45f, 1f), 0.45f));
    }

    private IEnumerator LerpBackgroundColor(Camera cam, Color target, float dur)
    {
        Color from = cam.backgroundColor;
        float elapsed = 0f;
        while (elapsed < dur)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / dur);
            cam.backgroundColor = Color.Lerp(from, target, t);
            yield return null;
        }
    }

    // ---------------------------------------------------------------------
    //  Chest open + ribbons + fireworks
    // ---------------------------------------------------------------------

    private Transform _lidHinge;

    private IEnumerator OpenChestBurst()
    {
        // Chest (body + hinged lid) appears at screen center.
        GameObject chest = CreateChest();
        chest.transform.position = new Vector3(0f, -0.2f, 0f);
        chest.transform.localScale = Vector3.zero;
        _spawned.Add(chest);

        // Pop the chest in.
        yield return ScaleTo(chest.transform, 2.5f, 0.3f);

        // White flash at the lid seam as it opens.
        StartCoroutine(OpenFlash(chest.transform.position));

        // Swing the lid open.
        if (_lidHinge != null)
            yield return OpenLid(_lidHinge, 0.3f);

        // Spray ribbons from inside + launch fireworks above (concurrent).
        Vector3 mouth = chest.transform.position + new Vector3(0f, 0.3f, 0f);
        SpawnRibbons(28, mouth);
        StartCoroutine(FireworksSequence(7, 1.3f));

        yield return CameraShake(0.22f);
    }

    private GameObject CreateChest()
    {
        GameObject root = new GameObject("Chest");

        // Body.
        GameObject body = new GameObject("Body");
        body.transform.SetParent(root.transform, false);
        SpriteRenderer bsr = body.AddComponent<SpriteRenderer>();
        bsr.sprite = SpriteGenerator.CreateChestBodySprite();
        bsr.sortingOrder = 51;
        // Body sprite is 80x56 (0.80x0.56 world @100PPU). Center it below the seam.
        body.transform.localPosition = new Vector3(0f, -0.28f, 0f);

        // Lid hinge at the top seam of the body.
        GameObject hinge = new GameObject("LidHinge");
        hinge.transform.SetParent(root.transform, false);
        hinge.transform.localPosition = new Vector3(0f, 0f, 0f);
        _lidHinge = hinge.transform;

        // Lid (child of hinge) extends upward when closed.
        GameObject lid = new GameObject("Lid");
        lid.transform.SetParent(hinge.transform, false);
        SpriteRenderer lsr = lid.AddComponent<SpriteRenderer>();
        lsr.sprite = SpriteGenerator.CreateChestLidSprite();
        lsr.sortingOrder = 52;
        // Lid sprite is 80x28 (0.80x0.28 world). Place so its bottom sits at the hinge.
        lid.transform.localPosition = new Vector3(0f, 0.14f, 0f);

        return root;
    }

    private IEnumerator OpenLid(Transform hinge, float dur)
    {
        Quaternion from = hinge.localRotation;
        // Swing the lid up and back (-110° around z reads as "opened backward").
        Quaternion to = Quaternion.Euler(0f, 0f, -110f);
        float elapsed = 0f;
        while (elapsed < dur)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / dur);
            float e = 1f - (1f - t) * (1f - t); // ease-out
            hinge.localRotation = Quaternion.Slerp(from, to, e);
            yield return null;
        }
        hinge.localRotation = to;
    }

    private IEnumerator ScaleTo(Transform t, float target, float dur)
    {
        Vector3 start = t.localScale;
        Vector3 end = Vector3.one * target;
        float elapsed = 0f;
        while (elapsed < dur)
        {
            elapsed += Time.deltaTime;
            float k = Mathf.Clamp01(elapsed / dur);
            t.localScale = Vector3.Lerp(start, end, BackOut(k));
            yield return null;
        }
        t.localScale = end;
    }

    private IEnumerator OpenFlash(Vector3 pos)
    {
        GameObject go = new GameObject("OpenFlash");
        SpriteRenderer sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = SpriteGenerator.CreateCircleSprite(Color.white);
        sr.color = new Color(1f, 0.95f, 0.7f, 0.9f);
        sr.sortingOrder = 55;
        go.transform.position = pos;
        go.transform.localScale = Vector3.zero;
        _spawned.Add(go);

        float dur = 0.25f;
        float elapsed = 0f;
        while (elapsed < dur)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / dur);
            go.transform.localScale = Vector3.one * (1.6f * t);
            sr.color = new Color(1f, 0.95f, 0.7f, 0.9f * (1f - t));
            yield return null;
        }
        _spawned.Remove(go);
        Destroy(go);
    }

    private void SpawnRibbons(int n, Vector3 origin)
    {
        Sprite ribbonSprite = SpriteGenerator.CreateSquareSprite(Color.white);
        Color[] palette = new Color[] {
            new Color(1f, 0.3f, 0.3f), new Color(1f, 0.85f, 0.2f),
            new Color(0.4f, 0.7f, 1f), new Color(0.5f, 1f, 0.5f),
            new Color(1f, 0.5f, 1f),
        };
        for (int i = 0; i < n; i++)
        {
            GameObject go = new GameObject("Ribbon");
            SpriteRenderer sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = ribbonSprite;
            sr.color = palette[Random.Range(0, palette.Length)];
            sr.sortingOrder = 53;
            go.transform.position = origin + (Vector3)Random.insideUnitCircle * 0.1f;
            // Long thin strip.
            go.transform.localScale = new Vector3(0.05f, 0.4f, 1f);
            go.transform.rotation = Quaternion.Euler(0f, 0f, Random.Range(0f, 360f));
            _spawned.Add(go);
            StartCoroutine(RibbonFly(go, sr));
        }
    }

    private IEnumerator RibbonFly(GameObject go, SpriteRenderer sr)
    {
        // Upward burst velocity with spread.
        Vector3 vel = new Vector3(Random.Range(-3f, 3f), Random.Range(4f, 7.5f), 0f);
        float life = 1.6f;
        float elapsed = 0f;
        Color c = sr != null ? sr.color : Color.white;
        float spin = Random.Range(-720f, 720f);
        while (elapsed < life && go != null)
        {
            elapsed += Time.deltaTime;
            vel.y -= 6f * Time.deltaTime; // gravity
            go.transform.position += vel * Time.deltaTime;
            go.transform.Rotate(0f, 0f, spin * Time.deltaTime);
            if (sr != null) sr.color = new Color(c.r, c.g, c.b, 1f - elapsed / life);
            yield return null;
        }
        if (go != null)
        {
            _spawned.Remove(go);
            Destroy(go);
        }
    }

    private IEnumerator FireworksSequence(int count, float totalDuration)
    {
        Sprite burstSprite = SpriteGenerator.CreateStarburstSprite();
        Color[] palette = new Color[] {
            new Color(1f, 0.4f, 0.4f), new Color(1f, 0.85f, 0.2f),
            new Color(0.5f, 0.8f, 1f), new Color(0.6f, 1f, 0.6f),
            new Color(1f, 0.6f, 0.9f),
        };
        float interval = totalDuration / count;
        for (int i = 0; i < count; i++)
        {
            Vector3 pos = new Vector3(Random.Range(-2.5f, 2.5f), Random.Range(1f, 3f), 0f);
            Color c = palette[Random.Range(0, palette.Length)];
            StartCoroutine(FireworkBurst(pos, c, burstSprite));
            if (i < count - 1) yield return new WaitForSeconds(interval);
        }
    }

    private IEnumerator FireworkBurst(Vector3 pos, Color color, Sprite sprite)
    {
        GameObject go = new GameObject("Firework");
        SpriteRenderer sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = sprite;
        sr.color = color;
        sr.sortingOrder = 54;
        go.transform.position = pos;
        go.transform.localScale = Vector3.zero;
        _spawned.Add(go);

        float dur = 0.5f;
        float elapsed = 0f;
        while (elapsed < dur)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / dur);
            go.transform.localScale = Vector3.one * (1.6f * t);
            sr.color = new Color(color.r, color.g, color.b, 1f - t);
            yield return null;
        }
        _spawned.Remove(go);
        Destroy(go);
    }

    private IEnumerator CameraShake(float dur)
    {
        Camera cam = Camera.main;
        if (cam == null) yield break;
        Vector3 basePos = cam.transform.position;
        float elapsed = 0f;
        while (elapsed < dur)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / dur;
            float intensity = (1f - t) * 0.18f;
            Vector2 jitter = Random.insideUnitCircle * intensity;
            cam.transform.position = new Vector3(basePos.x + jitter.x, basePos.y + jitter.y, basePos.z);
            yield return null;
        }
        cam.transform.position = basePos;
    }

    // ---------------------------------------------------------------------
    //  Buttons
    // ---------------------------------------------------------------------

    private void ShowButtons()
    {
        CreateButton("Retry", new Vector2(0f, -160f), () => ReloadScene());
        CreateButton("Replay", new Vector2(0f, -250f), () => Replay());
    }

    private void CreateButton(string label, Vector2 anchorPos, System.Action onClick)
    {
        GameObject btn = new GameObject(label + "Button");
        btn.transform.SetParent(_canvas.transform, false);
        RectTransform rt = btn.AddComponent<RectTransform>();
        rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = anchorPos;
        rt.sizeDelta = new Vector2(320f, 72f);

        Image img = btn.AddComponent<Image>();
        img.color = new Color(0.2f, 0.6f, 1f);

        Button button = btn.AddComponent<Button>();
        var colors = button.colors;
        colors.highlightedColor = new Color(0.3f, 0.7f, 1f);
        button.colors = colors;

        GameObject txtGo = new GameObject("Text");
        txtGo.transform.SetParent(btn.transform, false);
        RectTransform txtRt = txtGo.AddComponent<RectTransform>();
        txtRt.anchorMin = Vector2.zero;
        txtRt.anchorMax = Vector2.one;
        txtRt.sizeDelta = Vector2.zero;
        TextMeshProUGUI tmp = txtGo.AddComponent<TextMeshProUGUI>();
        tmp.text = label;
        tmp.fontSize = 34;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.color = Color.white;

        button.onClick.AddListener(() => onClick());
        _spawned.Add(btn);
    }

    private void ReloadScene()
    {
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

    // ---------------------------------------------------------------------
    //  Canvas + cleanup
    // ---------------------------------------------------------------------

    private void CreateCanvas()
    {
        GameObject go = new GameObject("WinSequenceCanvas");
        _canvas = go.AddComponent<Canvas>();
        _canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        _canvas.sortingOrder = 200; // above the board, below result dialog
        CanvasScaler scaler = go.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        scaler.matchWidthOrHeight = 0.5f;
        go.AddComponent<GraphicRaycaster>();
        _spawned.Add(go);
    }

    private void CleanupSpawned()
    {
        StopAllCoroutines();
        for (int i = _spawned.Count - 1; i >= 0; i--)
        {
            if (_spawned[i] != null) Destroy(_spawned[i]);
        }
        _spawned.Clear();
        // Recreate the canvas for the next finale run.
        CreateCanvas();
    }

    /// <summary>Standard back-out ease: 0 -> slight overshoot -> 1.</summary>
    private float BackOut(float t)
    {
        const float c1 = 1.70158f;
        const float c3 = c1 + 1f;
        float x = t - 1f;
        return 1f + c3 * x * x * x + c1 * x * x;
    }

    private IEnumerator ElasticIn(Transform target, float dur)
    {
        float elapsed = 0f;
        while (elapsed < dur)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / dur);
            target.localScale = Vector3.one * BackOut(t);
            yield return null;
        }
        target.localScale = Vector3.one;
    }
}
