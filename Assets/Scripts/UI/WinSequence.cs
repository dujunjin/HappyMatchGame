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
///   3. Treasure chest opening in four sub-phases:
///        a. Seal-break: lid lifts to 70° with a golden light beam leaking
///           from the crack, body tremor, gold sparks; a brief stutter
///           (resistance), then a snap open + a soft gold shockwave ring.
///        b. Bloom: 3-5 chrysanthemum fireworks shoot up and burst (main
///           rays + secondary star points), and 6-10 ribbons spiral out on
///           sine-wave paths with decaying spin/velocity.
///        c. Treasure surge: a burst-wave of coins/small gems, then waves of
///           large gems arcing out with low-gravity hover and a bounce.
///        d. Aftermath: chest glow dims to a faint pulse; effects fade.
///   4. Retry / Replay buttons. Replay re-runs the finale; Retry reloads.
/// Lose still uses the simple ResultDialog (this sequence is win-only).
/// All art is procedural; Phase E will upgrade to pooled particles + sound.
/// </summary>
public class WinSequence : MonoBehaviour
{
    private GameManager _gm;
    private Canvas _canvas;
    private readonly List<GameObject> _spawned = new List<GameObject>();

    private Transform _lidHinge;
    private Transform _chestBody;
    private SpriteRenderer _chestGlow;

    public void Init(GameManager gm) { _gm = gm; }

    public void Play() { StartCoroutine(Sequence()); }

    private IEnumerator Sequence()
    {
        CreateCanvas();
        if (_gm != null && _gm.gameUI != null) _gm.gameUI.BounceTarget();
        yield return new WaitForSeconds(0.16f);
        CreateVignette();
        yield return BoardExit(0.45f);
        yield return Finale();
    }

    /// <summary>Re-run just the finale (Great + chest + buttons).</summary>
    public void Replay()
    {
        CleanupSpawned();
        CreateVignette();
        StartCoroutine(Finale());
    }

    private IEnumerator Finale()
    {
        // Phase E: win fanfare.
        _gm.Audio?.Play(AudioCatalog.Event.Win);
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
            float e = 1f - (1f - k) * (1f - k);
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

        UnityEngine.UI.Outline outline = go.AddComponent<UnityEngine.UI.Outline>();
        outline.effectColor = new Color(0.26f, 0.08f, 0.02f, 0.82f);
        outline.effectDistance = new Vector2(3f, -3f);

        _spawned.Add(go);
        StartCoroutine(ElasticIn(go.transform, 0.24f));
    }

    private void CreateVignette()
    {
        if (_canvas == null) return;
        GameObject go = new GameObject("VictoryVignette");
        go.transform.SetParent(_canvas.transform, false);
        RectTransform rt = go.AddComponent<RectTransform>();
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
        UnityEngine.UI.Image image = go.AddComponent<UnityEngine.UI.Image>();
        image.color = new Color(0.015f, 0.025f, 0.09f, 0.22f);
        image.raycastTarget = false;
        _spawned.Add(go);
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

    // =====================================================================
    //  Chest open — 4 phases
    // =====================================================================

    private IEnumerator OpenChestBurst()
    {
        GameObject chest = CreateChest();
        chest.transform.position = new Vector3(0f, -0.2f, 0f);
        chest.transform.localScale = Vector3.zero;
        _spawned.Add(chest);

        // Pop the chest in.
        yield return ScaleTo(chest.transform, 2.5f, 0.25f);

        // Mouth = lid seam (where effects emerge). Chest root is at y=-0.2,
        // hinge at local y=0 -> world y=-0.2; "mouth" just above the seam.
        Vector3 mouth = chest.transform.position + new Vector3(0f, 0.05f, 0f);

        // Phase 1: seal-break (lid lift + beam + tremor + sparks + snap + shockwave).
        yield return LiftLid(mouth);

        // Phase 2: bloom (fireworks + spiral ribbons), concurrent.
        StartCoroutine(FireworksBloom(mouth, 4));
        StartCoroutine(RibbonsSpiral(mouth, 8));
        StartCoroutine(CameraShake(0.22f));

        // Phase 3: treasure surge, overlapping phase 2's tail.
        yield return new WaitForSeconds(0.15f);
        StartCoroutine(TreasureSurge(mouth));

        // Let phases 2 + 3 play out.
        yield return new WaitForSeconds(1.1f);

        // Phase 4: aftermath — chest glow dims to a faint pulse.
        StartCoroutine(ChestGlowPulse());
        yield return new WaitForSeconds(0.7f);
    }

    private GameObject CreateChest()
    {
        GameObject root = new GameObject("Chest");

        // Glow behind the chest (phase 4 pulse).
        GameObject glow = new GameObject("Glow");
        glow.transform.SetParent(root.transform, false);
        SpriteRenderer gsr = glow.AddComponent<SpriteRenderer>();
        gsr.sprite = SpriteGenerator.CreateCircleSprite(new Color(1f, 0.8f, 0.3f, 0.45f));
        gsr.sortingOrder = 49;
        glow.transform.localPosition = new Vector3(0f, -0.1f, 0f);
        glow.transform.localScale = new Vector3(1.6f, 1.6f, 1f);
        _chestGlow = gsr;

        // Body (80x56 sprite, 0.80x0.56 world @100PPU).
        GameObject body = new GameObject("Body");
        body.transform.SetParent(root.transform, false);
        SpriteRenderer bsr = body.AddComponent<SpriteRenderer>();
        bsr.sprite = SpriteGenerator.CreateChestBodySprite();
        bsr.sortingOrder = 51;
        body.transform.localPosition = new Vector3(0f, -0.28f, 0f);
        _chestBody = body.transform;

        // Lid hinge at the BACK edge of the seam (right-top corner of the body,
        // = the lid's back-bottom corner). The lid extends left (front) from
        // this hinge and rotates around it as a rigid body, so only the front
        // edge arcs up — not a pendulum swing from the lid's center.
        GameObject hinge = new GameObject("LidHinge");
        hinge.transform.SetParent(root.transform, false);
        hinge.transform.localPosition = new Vector3(0.4f, 0f, 0f);
        _lidHinge = hinge.transform;

        // Lid (80x28 sprite) extends left from the hinge when closed, lying on
        // top of the body. localPosition (-0.4, 0.14) puts the lid's right
        // edge + bottom at the hinge, so it spans chest-root x in [-0.4,0.4],
        // y in [0, 0.28].
        GameObject lid = new GameObject("Lid");
        lid.transform.SetParent(hinge.transform, false);
        SpriteRenderer lsr = lid.AddComponent<SpriteRenderer>();
        lsr.sprite = SpriteGenerator.CreateChestLidSprite();
        lsr.sortingOrder = 52;
        lid.transform.localPosition = new Vector3(-0.4f, 0.14f, 0f);

        return root;
    }

    // --- Phase 1: seal-break (three-stage rigid-body rotation around the hinge) ---

    private IEnumerator LiftLid(Vector3 mouth)
    {
        // Gold light beam leaking from the crack, placed at the FRONT of the
        // seam (where the lid lifts first). Sibling of the hinge (chest root).
        SpriteRenderer beamSr = null;
        if (_lidHinge != null && _lidHinge.parent != null)
        {
            GameObject beam = new GameObject("Beam");
            beam.transform.SetParent(_lidHinge.parent, false);
            beam.transform.localPosition = new Vector3(-0.2f, 0.1f, 0f);
            beamSr = beam.AddComponent<SpriteRenderer>();
            beamSr.sprite = SpriteGenerator.CreateSquareSprite(new Color(1f, 0.85f, 0.4f));
            beamSr.color = new Color(1f, 0.85f, 0.4f, 0f);
            beamSr.sortingOrder = 50;
            beam.transform.localScale = new Vector3(0.15f, 1.5f, 1f);
            _spawned.Add(beam);
        }

        Vector3 bodyBase = _chestBody != null ? _chestBody.localPosition : Vector3.zero;

        // The lid extends in -x from the hinge, so a NEGATIVE z rotation (CW)
        // swings the front edge up = opening. Angles below are negative.

        // Stage 1 "蓄力微启" (0~0.1s): 0 -> -6°, EaseIn (slow start, the lid
        // is gently nudged by the internal force; light leaks from the crack).
        float e = 0f;
        while (e < 0.1f)
        {
            e += Time.deltaTime;
            float t = Mathf.Clamp01(e / 0.1f);
            float ti = t * t; // EaseIn
            float angle = Mathf.Lerp(0f, -6f, ti);
            if (_lidHinge != null) _lidHinge.localRotation = Quaternion.Euler(0f, 0f, angle);
            SetBeam(beamSr, Mathf.Abs(angle));
            Tremor(_chestBody, bodyBase, 0.008f);
            if (Random.value < 0.25f) StartCoroutine(Spark(mouth));
            yield return null;
        }

        // Stage 2 "主开合" (0.1~0.2s): -6 -> -75°, EaseInOut (accelerate then
        // decelerate; the front edge arcs up fast, slowing near 70-80°).
        e = 0f;
        while (e < 0.1f)
        {
            e += Time.deltaTime;
            float t = Mathf.Clamp01(e / 0.1f);
            float tio = t < 0.5f ? 2f * t * t : 1f - Mathf.Pow(-2f * t + 2f, 2f) / 2f; // EaseInOut
            float angle = Mathf.Lerp(-6f, -75f, tio);
            if (_lidHinge != null) _lidHinge.localRotation = Quaternion.Euler(0f, 0f, angle);
            SetBeam(beamSr, Mathf.Abs(angle));
            Tremor(_chestBody, bodyBase, 0.014f);
            if (Random.value < 0.5f) StartCoroutine(Spark(mouth));
            yield return null;
        }

        // Stage 3 "弹顶回弹" (0.2~0.4s): overshoot to -105° (past vertical),
        // then rebound to settle at -95° — a snappy finish.
        e = 0f;
        while (e < 0.1f)
        {
            e += Time.deltaTime;
            float t = Mathf.Clamp01(e / 0.1f);
            float angle = Mathf.Lerp(-75f, -105f, t);
            if (_lidHinge != null) _lidHinge.localRotation = Quaternion.Euler(0f, 0f, angle);
            SetBeam(beamSr, Mathf.Abs(angle));
            yield return null;
        }
        e = 0f;
        while (e < 0.1f)
        {
            e += Time.deltaTime;
            float t = Mathf.Clamp01(e / 0.1f);
            float angle = Mathf.Lerp(-105f, -95f, t); // rebound to settle
            if (_lidHinge != null) _lidHinge.localRotation = Quaternion.Euler(0f, 0f, angle);
            SetBeam(beamSr, Mathf.Abs(angle));
            yield return null;
        }

        // Open complete: hide beam + shockwave.
        if (beamSr != null) beamSr.color = new Color(1f, 0.85f, 0.4f, 0f);
        StartCoroutine(Shockwave(mouth));
    }

    private void SetBeam(SpriteRenderer sr, float angle)
    {
        if (sr == null) return;
        // Intensity grows with the opening angle (0 -> ~95°).
        float k = Mathf.Clamp01(angle / 95f);
        sr.color = new Color(1f, 0.85f, 0.4f, k * 0.85f);
    }

    private void Tremor(Transform body, Vector3 basePos, float amp)
    {
        if (body == null) return;
        body.localPosition = basePos + (Vector3)Random.insideUnitCircle * amp;
    }

    private IEnumerator Spark(Vector3 origin)
    {
        GameObject go = new GameObject("Spark");
        SpriteRenderer sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = SpriteGenerator.CreateCircleSprite(new Color(1f, 0.9f, 0.5f));
        sr.sortingOrder = 55;
        go.transform.position = origin;
        go.transform.localScale = Vector3.one * 0.1f;
        _spawned.Add(go);

        Vector3 vel = new Vector3(Random.Range(-1.2f, 1.2f), Random.Range(1f, 2.5f), 0f);
        float life = 0.4f;
        float e = 0f;
        while (e < life && go != null)
        {
            e += Time.deltaTime;
            vel.y -= 5f * Time.deltaTime;
            go.transform.position += vel * Time.deltaTime;
            if (sr != null) sr.color = new Color(1f, 0.9f, 0.5f, 1f - e / life);
            yield return null;
        }
        if (go != null) { _spawned.Remove(go); Destroy(go); }
    }

    private IEnumerator Shockwave(Vector3 origin)
    {
        GameObject go = new GameObject("Shockwave");
        SpriteRenderer sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = SpriteGenerator.CreateCircleSprite(new Color(1f, 0.8f, 0.3f, 0.5f));
        sr.sortingOrder = 49;
        go.transform.position = origin;
        go.transform.localScale = Vector3.one * 0.3f;
        _spawned.Add(go);

        float dur = 0.5f;
        float e = 0f;
        while (e < dur && go != null)
        {
            e += Time.deltaTime;
            float t = Mathf.Clamp01(e / dur);
            go.transform.localScale = Vector3.one * (0.3f + 4f * t);
            if (sr != null) sr.color = new Color(1f, 0.8f, 0.3f, 0.5f * (1f - t));
            yield return null;
        }
        if (go != null) { _spawned.Remove(go); Destroy(go); }
    }

    // --- Phase 2: bloom (chrysanthemum fireworks + spiral ribbons) ---

    private IEnumerator FireworksBloom(Vector3 origin, int count)
    {
        Color[] palette = new Color[] {
            new Color(1f, 0.8f, 0.2f),  // gold
            new Color(1f, 0.3f, 0.3f),  // red
            new Color(0.7f, 0.4f, 1f),  // purple
            new Color(0.5f, 0.9f, 1f),  // cyan accent
        };
        for (int i = 0; i < count; i++)
        {
            Vector3 apex = origin + new Vector3(Random.Range(-1.6f, 1.6f), Random.Range(2.5f, 3.5f), 0f);
            Color c = palette[Random.Range(0, palette.Length)];
            StartCoroutine(ChrysanthemumBurst(origin, apex, c));
            if (i < count - 1) yield return new WaitForSeconds(0.08f);
        }
    }

    private IEnumerator ChrysanthemumBurst(Vector3 origin, Vector3 apex, Color color)
    {
        // Streak up.
        yield return Streak(origin, apex, color, 0.35f);

        // Burst at apex: main rays (starburst) + secondary star points.
        GameObject burst = new GameObject("Chrysanthemum");
        SpriteRenderer sr = burst.AddComponent<SpriteRenderer>();
        sr.sprite = SpriteGenerator.CreateStarburstSprite(128, 7);
        sr.color = color;
        sr.sortingOrder = 54;
        burst.transform.position = apex;
        burst.transform.localScale = Vector3.zero;
        _spawned.Add(burst);

        int rays = 7;
        for (int i = 0; i < rays; i++)
        {
            float ang = (i * (360f / rays) + Random.Range(-12f, 12f)) * Mathf.Deg2Rad;
            Vector3 dir = new Vector3(Mathf.Cos(ang), Mathf.Sin(ang), 0f);
            StartCoroutine(StarPoint(apex, dir, color));
        }

        float dur = 0.5f;
        float e = 0f;
        while (e < dur && burst != null)
        {
            e += Time.deltaTime;
            float t = Mathf.Clamp01(e / dur);
            burst.transform.localScale = Vector3.one * (2f * t);
            if (sr != null) sr.color = new Color(color.r, color.g, color.b, 1f - t);
            yield return null;
        }
        if (burst != null) { _spawned.Remove(burst); Destroy(burst); }
    }

    private IEnumerator Streak(Vector3 from, Vector3 to, Color color, float dur)
    {
        GameObject go = new GameObject("Streak");
        SpriteRenderer sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = SpriteGenerator.CreateSquareSprite(color);
        sr.sortingOrder = 53;
        go.transform.position = from;
        Vector3 dir = to - from;
        if (dir.sqrMagnitude > 0.0001f)
        {
            float angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
            go.transform.rotation = Quaternion.Euler(0f, 0f, angle);
        }
        go.transform.localScale = new Vector3(2.5f, 0.25f, 1f); // elongated
        _spawned.Add(go);

        float e = 0f;
        while (e < dur && go != null)
        {
            e += Time.deltaTime;
            float t = Mathf.Clamp01(e / dur);
            go.transform.position = Vector3.Lerp(from, to, t);
            if (sr != null) sr.color = new Color(color.r, color.g, color.b, 1f - t * 0.6f);
            yield return null;
        }
        if (go != null) { _spawned.Remove(go); Destroy(go); }
    }

    private IEnumerator StarPoint(Vector3 origin, Vector3 dir, Color color)
    {
        GameObject go = new GameObject("Star");
        SpriteRenderer sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = SpriteGenerator.CreateCircleSprite(color);
        sr.sortingOrder = 55;
        go.transform.position = origin;
        go.transform.localScale = Vector3.one * 0.1f;
        _spawned.Add(go);

        Vector3 vel = dir * 2.2f;
        float life = 0.55f;
        float e = 0f;
        while (e < life && go != null)
        {
            e += Time.deltaTime;
            vel *= (1f - Time.deltaTime * 1.8f);
            go.transform.position += vel * Time.deltaTime;
            if (sr != null) sr.color = new Color(color.r, color.g, color.b, 1f - e / life);
            yield return null;
        }
        if (go != null) { _spawned.Remove(go); Destroy(go); }
    }

    private IEnumerator RibbonsSpiral(Vector3 origin, int count)
    {
        Color[] palette = new Color[] {
            new Color(0.9f, 0.3f, 0.4f), new Color(0.95f, 0.75f, 0.3f),
            new Color(0.4f, 0.6f, 0.95f), new Color(0.5f, 0.85f, 0.6f),
        };
        for (int i = 0; i < count; i++)
        {
            StartCoroutine(RibbonSpiral(origin, palette[Random.Range(0, palette.Length)]));
            if (i < count - 1) yield return new WaitForSeconds(0.04f);
        }
    }

    private IEnumerator RibbonSpiral(Vector3 origin, Color color)
    {
        GameObject go = new GameObject("Ribbon");
        SpriteRenderer sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = SpriteGenerator.CreateSquareSprite(color);
        sr.color = new Color(color.r, color.g, color.b, 0.85f);
        sr.sortingOrder = 53;
        go.transform.position = origin + (Vector3)Random.insideUnitCircle * 0.05f;
        go.transform.localScale = new Vector3(0.25f, 2.5f, 1f);
        go.transform.rotation = Quaternion.Euler(0f, 0f, Random.Range(0f, 360f));
        _spawned.Add(go);

        Vector3 vel = new Vector3(Random.Range(-1.2f, 1.2f), Random.Range(3.5f, 5f), 0f);
        float spin = Random.Range(-360f, 360f);
        float phase = Random.Range(0f, 6.28f);
        float life = 1.8f;
        float e = 0f;
        while (e < life && go != null)
        {
            e += Time.deltaTime;
            float sine = Mathf.Sin(e * 5f + phase) * 0.6f;
            vel.y -= 2.5f * Time.deltaTime;
            vel *= (1f - Time.deltaTime * 0.5f);
            spin *= (1f - Time.deltaTime * 0.6f);
            go.transform.position += (vel + new Vector3(sine, 0f, 0f)) * Time.deltaTime;
            go.transform.Rotate(0f, 0f, spin * Time.deltaTime);
            if (sr != null) sr.color = new Color(color.r, color.g, color.b, 0.85f * (1f - e / life));
            yield return null;
        }
        if (go != null) { _spawned.Remove(go); Destroy(go); }
    }

    // --- Phase 3: treasure surge ---

    private IEnumerator TreasureSurge(Vector3 mouth)
    {
        // Burst wave: coins + small gems, high velocity, +-30° spread.
        for (int i = 0; i < 14; i++)
        {
            float ang = 90f + Random.Range(-30f, 30f);
            Vector3 dir = new Vector3(Mathf.Cos(ang * Mathf.Deg2Rad), Mathf.Sin(ang * Mathf.Deg2Rad), 0f);
            bool coin = Random.value < 0.6f;
            Color c = coin ? new Color(1f, 0.83f, 0.2f) : RandomGemColor();
            StartCoroutine(Treasure(mouth, dir * Random.Range(4f, 6f), c, coin ? 0.6f : 0.7f, false));
        }

        yield return new WaitForSeconds(0.2f);

        // Surge flow: waves of large gems with low-gravity hover.
        int waves = 5;
        for (int w = 0; w < waves; w++)
        {
            int n = Random.Range(2, 4);
            for (int i = 0; i < n; i++)
            {
                float ang = 90f + Random.Range(-25f, 25f);
                Vector3 dir = new Vector3(Mathf.Cos(ang * Mathf.Deg2Rad), Mathf.Sin(ang * Mathf.Deg2Rad), 0f);
                StartCoroutine(Treasure(mouth, dir * Random.Range(2.5f, 3.5f), RandomGemColor(), 1.2f, true));
            }
            yield return new WaitForSeconds(Random.Range(0.1f, 0.15f));
        }
    }

    private Color RandomGemColor()
    {
        Color[] gems = new Color[] {
            new Color(0.7f, 0.95f, 1f),  // diamond
            new Color(1f, 0.3f, 0.4f),   // ruby
            new Color(0.3f, 0.9f, 0.5f), // emerald
            new Color(0.85f, 0.85f, 0.95f), // silver
        };
        return gems[Random.Range(0, gems.Length)];
    }

    private IEnumerator Treasure(Vector3 origin, Vector3 vel, Color color, float size, bool lowGravity)
    {
        GameObject go = new GameObject("Treasure");
        SpriteRenderer sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = SpriteGenerator.CreateSquareSprite(color);
        sr.sortingOrder = 56;
        go.transform.position = origin;
        go.transform.localScale = Vector3.one * size;
        go.transform.rotation = Quaternion.Euler(0f, 0f, 45f); // diamond-ish
        _spawned.Add(go);

        float spin = Random.Range(-540f, 540f);
        float life = 2.2f;
        float e = 0f;
        bool bounced = false;
        while (e < life && go != null)
        {
            e += Time.deltaTime;
            float grav = lowGravity ? 3f : 5f;
            vel.y -= grav * Time.deltaTime;
            // Hover: decelerate downward velocity near the apex.
            if (vel.y < 0f && vel.y > -1.5f) vel.y *= (1f - Time.deltaTime * 0.6f);
            go.transform.position += vel * Time.deltaTime;
            go.transform.Rotate(0f, 0f, spin * Time.deltaTime);
            spin *= (1f - Time.deltaTime * 0.3f);

            // Bounce off the "ground".
            if (go.transform.position.y < -2.8f && vel.y < 0f && !bounced)
            {
                vel.y = -vel.y * 0.4f;
                vel.x *= 0.6f;
                bounced = true;
            }

            float a = e > life - 0.5f ? (1f - (e - (life - 0.5f)) / 0.5f) : 1f;
            if (sr != null) sr.color = new Color(color.r, color.g, color.b, a);
            yield return null;
        }
        if (go != null) { _spawned.Remove(go); Destroy(go); }
    }

    // --- Phase 4: aftermath ---

    private IEnumerator ChestGlowPulse()
    {
        if (_chestGlow == null) yield break;
        float dur = 1.2f;
        float e = 0f;
        while (e < dur)
        {
            e += Time.deltaTime;
            float t = e / dur;
            float pulse = 0.15f + 0.1f * Mathf.Sin(t * 8f);
            float a = Mathf.Lerp(0.45f, pulse, t);
            if (_chestGlow != null) _chestGlow.color = new Color(1f, 0.8f, 0.3f, a);
            yield return null;
        }
    }

    // =====================================================================
    //  Shared helpers
    // =====================================================================

    private IEnumerator ScaleTo(Transform t, float target, float dur)
    {
        if (t == null) yield break;
        Vector3 start = t.localScale;
        Vector3 end = Vector3.one * target;
        float elapsed = 0f;
        while (elapsed < dur && t != null)
        {
            elapsed += Time.deltaTime;
            float k = Mathf.Clamp01(elapsed / dur);
            t.localScale = Vector3.Lerp(start, end, BackOut(k));
            yield return null;
        }
        if (t != null) t.localScale = end;
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
        _canvas.sortingOrder = 200;
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
        _lidHinge = null;
        _chestBody = null;
        _chestGlow = null;
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
        if (target == null) yield break;
        float elapsed = 0f;
        while (elapsed < dur && target != null)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / dur);
            target.localScale = Vector3.one * BackOut(t);
            yield return null;
        }
        if (target != null) target.localScale = Vector3.one;
    }
}
