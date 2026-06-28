using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Phase E: two-layer snow + a little diagonal rain, pooled so no per-frame
/// allocation. Back layer sits behind the board (slow, small, low alpha);
/// front layer sits in front (faster, larger, moderate alpha) for a parallax
/// depth feel. A few thin rain streaks accent the front layer. Particles
/// recycle to the top when they fall below the view.
/// </summary>
public class SnowField : MonoBehaviour
{
    private class Flake
    {
        public Transform tr;
        public Vector3 vel;
        public float driftPhase;
        public float driftAmp;
        public float driftSpeed;
        public float spin;
    }

    private readonly List<Flake> _flakes = new List<Flake>();
    private Camera _cam;
    private float _xRange, _yTop, _yBottom;

    public void Init(int backCount = 34, int frontCount = 18, int rainCount = 7)
    {
        _cam = Camera.main;
        if (_cam == null) return;

        float viewH = _cam.orthographicSize * 2f;
        float viewW = viewH * _cam.aspect;
        _xRange = viewW * 0.5f + 1f;
        _yTop = _cam.orthographicSize + 1f;
        _yBottom = -_cam.orthographicSize - 1f;

        Sprite snow = SpriteGenerator.CreateCircleSprite(Color.white);
        Sprite rain = SpriteGenerator.CreateSquareSprite(new Color(0.7f, 0.8f, 0.95f, 0.5f));

        // Back layer: far, small + dense. Front layer: near, large + soft.
        // Both behind the board (sortingOrder < 1) so snow never obscures pieces.
        SpawnLayer(snow, backCount, slow: true, sortingOrder: -5);
        SpawnLayer(snow, frontCount, slow: false, sortingOrder: 3);
        SpawnRain(rain, rainCount);
    }

    private void SpawnLayer(Sprite sprite, int count, bool slow, int sortingOrder)
    {
        for (int i = 0; i < count; i++)
        {
            GameObject go = new GameObject("Snow");
            SpriteRenderer sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = sprite;
            sr.sortingOrder = sortingOrder;
            // Back (far): small + low alpha (dense). Front (near): large + softer.
            float size = slow ? Random.Range(0.04f, 0.08f) : Random.Range(0.16f, 0.26f);
            sr.color = new Color(1f, 1f, 1f, slow ? Random.Range(0.35f, 0.55f) : Random.Range(0.25f, 0.45f));
            go.transform.localScale = Vector3.one * size;
            go.transform.position = new Vector3(Random.Range(-_xRange, _xRange), Random.Range(_yBottom, _yTop), 0f);

            Flake f = new Flake
            {
                tr = go.transform,
                // Back falls slowly; front faster (closer to camera).
                vel = new Vector3(0f, slow ? Random.Range(-0.25f, -0.45f) : Random.Range(-0.7f, -1.1f), 0f),
                driftPhase = Random.Range(0f, 6.28f),
                driftAmp = slow ? Random.Range(0.08f, 0.2f) : Random.Range(0.25f, 0.45f),
                driftSpeed = Random.Range(0.8f, 1.6f),
                spin = Random.Range(-28f, 28f),
            };
            _flakes.Add(f);
        }
    }

    private void SpawnRain(Sprite sprite, int count)
    {
        for (int i = 0; i < count; i++)
        {
            GameObject go = new GameObject("Rain");
            SpriteRenderer sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = sprite;
            sr.sortingOrder = 2;
            sr.color = new Color(0.72f, 0.84f, 1f, Random.Range(0.14f, 0.28f));
            go.transform.localScale = new Vector3(0.012f, Random.Range(0.22f, 0.38f), 1f);
            go.transform.rotation = Quaternion.Euler(0f, 0f, -7f);
            go.transform.position = new Vector3(Random.Range(-_xRange, _xRange), Random.Range(_yBottom, _yTop), 0f);

            _flakes.Add(new Flake
            {
                tr = go.transform,
                vel = new Vector3(0.25f, -3.5f, 0f), // fast diagonal fall
                driftPhase = 0f,
                driftAmp = 0f,
                driftSpeed = 0f,
                spin = 0f,
            });
        }
    }

    private void Update()
    {
        if (_cam == null) return;
        for (int i = 0; i < _flakes.Count; i++)
        {
            Flake f = _flakes[i];
            if (f.tr == null) continue;
            Vector3 p = f.tr.position;
            p += f.vel * Time.deltaTime;
            if (f.driftAmp > 0f)
            {
                f.driftPhase += f.driftSpeed * Time.deltaTime;
                p.x += Mathf.Sin(f.driftPhase) * f.driftAmp * Time.deltaTime;
            }
            f.tr.position = p;
            if (Mathf.Abs(f.spin) > 0.01f)
                f.tr.Rotate(0f, 0f, f.spin * Time.deltaTime);

            if (p.y < _yBottom)
            {
                f.tr.position = new Vector3(Random.Range(-_xRange, _xRange), _yTop, 0f);
            }
        }
    }
}
