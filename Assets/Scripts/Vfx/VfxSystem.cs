using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Phase E: pooled particle effects for clears and special activations.
/// Particles are bare SpriteRenderer GameObjects recycled through an internal
/// pool (no per-spawn allocation after warm-up). Shared procedural sprites are
/// cached once. All effects are fire-and-forget coroutines hosted here.
///
/// Hooks (called by gameplay code):
///   SpawnClearBurst(pos, color)      — match clear: shards + white star + ring.
///   SpawnRocketTrail(from, to, col) — directional beam + tail.
///   SpawnBombBlast(pos)             — shock ring + sparks + camera shake.
///   SpawnPropellerTrail(pos)        — brief gold trail.
///   SpawnPropellerHit(pos)          — starburst at impact.
/// </summary>
public class VfxSystem : MonoBehaviour
{
    private readonly Stack<GameObject> _pool = new Stack<GameObject>();
    private Sprite _shard, _star, _ring, _starburst;
    private Transform _root;

    public void Init()
    {
        _root = transform;
        _shard = SpriteGenerator.CreateSquareSprite(Color.white);
        _star = SpriteGenerator.CreateCircleSprite(Color.white);
        _ring = SpriteGenerator.CreateCircleSprite(Color.white);
        _starburst = SpriteGenerator.CreateStarburstSprite(96, 8);
    }

    // --- pool ---

    private GameObject Acquire(Sprite sprite, int sortingOrder)
    {
        GameObject go = _pool.Count > 0 ? _pool.Pop() : null;
        if (go == null)
        {
            go = new GameObject("Vfx");
            go.transform.SetParent(_root, false);
            go.AddComponent<SpriteRenderer>();
        }
        go.SetActive(true);
        var sr = go.GetComponent<SpriteRenderer>();
        sr.sprite = sprite;
        sr.sortingOrder = sortingOrder;
        sr.color = Color.white;
        go.transform.localScale = Vector3.one;
        go.transform.rotation = Quaternion.identity;
        return go;
    }

    private void Release(GameObject go)
    {
        if (go == null) return;
        go.SetActive(false);
        _pool.Push(go);
    }

    // --- effects ---

    public void SpawnClearBurst(Vector3 pos, Color color)
    {
        // Colored shards.
        int n = 6;
        for (int i = 0; i < n; i++)
        {
            GameObject go = Acquire(_shard, 20);
            go.transform.position = pos;
            go.transform.localScale = Vector3.one * Random.Range(0.05f, 0.09f);
            var sr = go.GetComponent<SpriteRenderer>();
            sr.color = color;
            Vector2 dir = Random.insideUnitCircle.normalized;
            StartCoroutine(ShardFly(go, sr, pos, dir * Random.Range(1.5f, 3f), 0.4f));
        }
        // White star flash.
        {
            GameObject go = Acquire(_star, 21);
            go.transform.position = pos;
            var sr = go.GetComponent<SpriteRenderer>();
            sr.color = Color.white;
            StartCoroutine(Flash(go, sr, 0.18f, 0.2f));
        }
        // Expanding ring.
        {
            GameObject go = Acquire(_ring, 19);
            go.transform.position = pos;
            go.transform.localScale = Vector3.one * 0.2f;
            var sr = go.GetComponent<SpriteRenderer>();
            sr.color = new Color(color.r, color.g, color.b, 0.6f);
            StartCoroutine(RingExpand(go, sr, 0.9f, 0.3f));
        }
    }

    public void SpawnRocketTrail(Vector3 from, Vector3 to, Color color)
    {
        // Beam along the line.
        GameObject beam = Acquire(_shard, 18);
        beam.transform.position = (from + to) * 0.5f;
        Vector3 d = to - from; float len = d.magnitude;
        if (len > 0.001f) beam.transform.rotation = Quaternion.Euler(0, 0, Mathf.Atan2(d.y, d.x) * Mathf.Rad2Deg);
        beam.transform.localScale = new Vector3(len * 6f, 0.4f, 1f); // scale (sprite is 0.16 world)
        var bsr = beam.GetComponent<SpriteRenderer>();
        bsr.color = new Color(color.r, color.g, color.b, 0.7f);
        StartCoroutine(Flash(beam, bsr, 0.18f, 0.25f));

        // Tail particles along the line.
        int n = 5;
        for (int i = 0; i < n; i++)
        {
            float t = (i + 1f) / (n + 1f);
            Vector3 p = Vector3.Lerp(from, to, t);
            GameObject go = Acquire(_star, 20);
            go.transform.position = p;
            go.transform.localScale = Vector3.one * 0.08f;
            var sr = go.GetComponent<SpriteRenderer>();
            sr.color = new Color(1f, 0.9f, 0.6f, 0.8f);
            StartCoroutine(Flash(go, sr, 0.06f, 0.3f));
        }
    }

    public void SpawnBombBlast(Vector3 pos)
    {
        // Shock ring.
        GameObject ring = Acquire(_ring, 19);
        ring.transform.position = pos;
        ring.transform.localScale = Vector3.one * 0.3f;
        var rsr = ring.GetComponent<SpriteRenderer>();
        rsr.color = new Color(1f, 0.85f, 0.4f, 0.7f);
        StartCoroutine(RingExpand(ring, rsr, 1.4f, 0.35f));

        // Sparks.
        int n = 10;
        for (int i = 0; i < n; i++)
        {
            GameObject go = Acquire(_star, 21);
            go.transform.position = pos;
            go.transform.localScale = Vector3.one * 0.07f;
            var sr = go.GetComponent<SpriteRenderer>();
            sr.color = new Color(1f, 0.8f, 0.3f);
            Vector2 dir = Random.insideUnitCircle.normalized;
            StartCoroutine(ShardFly(go, sr, pos, dir * Random.Range(2.5f, 4.5f), 0.45f));
        }

        StartCoroutine(CameraShake(0.18f, 0.12f));
    }

    public void SpawnPropellerTrail(Vector3 pos)
    {
        GameObject go = Acquire(_star, 20);
        go.transform.position = pos;
        go.transform.localScale = Vector3.one * 0.06f;
        var sr = go.GetComponent<SpriteRenderer>();
        sr.color = new Color(1f, 0.95f, 0.7f, 0.7f);
        StartCoroutine(Flash(go, sr, 0.04f, 0.3f));
    }

    public void SpawnPropellerHit(Vector3 pos)
    {
        GameObject burst = Acquire(_starburst, 21);
        burst.transform.position = pos;
        burst.transform.localScale = Vector3.zero;
        var sr = burst.GetComponent<SpriteRenderer>();
        sr.color = new Color(1f, 0.95f, 0.7f, 1f);
        StartCoroutine(RingExpand(burst, sr, 1.2f, 0.35f));
    }

    // --- particle coroutines ---

    private IEnumerator ShardFly(GameObject go, SpriteRenderer sr, Vector3 start, Vector3 vel, float life)
    {
        float e = 0f;
        Color c = sr != null ? sr.color : Color.white;
        while (e < life && go != null && go.activeInHierarchy)
        {
            e += Time.deltaTime;
            vel *= (1f - Time.deltaTime * 2f);
            vel.y -= 3f * Time.deltaTime;
            go.transform.position += vel * Time.deltaTime;
            go.transform.Rotate(0, 0, 540f * Time.deltaTime);
            if (sr != null) sr.color = new Color(c.r, c.g, c.b, 1f - e / life);
            yield return null;
        }
        if (go != null) Release(go);
    }

    private IEnumerator Flash(GameObject go, SpriteRenderer sr, float startScale, float life)
    {
        float e = 0f;
        Color c = sr != null ? sr.color : Color.white;
        while (e < life && go != null && go.activeInHierarchy)
        {
            e += Time.deltaTime;
            float t = e / life;
            go.transform.localScale = Vector3.one * (startScale * (1f + t * 0.5f));
            if (sr != null) sr.color = new Color(c.r, c.g, c.b, c.a * (1f - t));
            yield return null;
        }
        if (go != null) Release(go);
    }

    private IEnumerator RingExpand(GameObject go, SpriteRenderer sr, float maxScale, float life)
    {
        float e = 0f;
        Color c = sr != null ? sr.color : Color.white;
        float startScale = go.transform.localScale.x;
        while (e < life && go != null && go.activeInHierarchy)
        {
            e += Time.deltaTime;
            float t = e / life;
            go.transform.localScale = Vector3.one * Mathf.Lerp(startScale, maxScale, t);
            if (sr != null) sr.color = new Color(c.r, c.g, c.b, c.a * (1f - t));
            yield return null;
        }
        if (go != null) Release(go);
    }

    private IEnumerator CameraShake(float dur, float amp)
    {
        Camera cam = Camera.main;
        if (cam == null) yield break;
        Vector3 basePos = cam.transform.position;
        float e = 0f;
        while (e < dur)
        {
            e += Time.deltaTime;
            float t = e / dur;
            Vector2 j = Random.insideUnitCircle * (amp * (1f - t));
            cam.transform.position = new Vector3(basePos.x + j.x, basePos.y + j.y, basePos.z);
            yield return null;
        }
        cam.transform.position = basePos;
    }
}
