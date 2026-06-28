using UnityEngine;
using System.Collections;
using System.Collections.Generic;

/// <summary>
/// Coroutine-based animation helpers for tweening transforms.
///
/// Every coroutine is null-safe: if the target Transform/GameObject is
/// destroyed mid-tween (e.g. a special item is consumed while its PopIn is
/// still running, or the scene reloads), the coroutine bails out instead of
/// throwing MissingReferenceException.
/// </summary>
public static class AnimationHelper
{
    public static IEnumerator TweenPosition(Transform target, Vector3 from, Vector3 to, float duration)
    {
        if (target == null) yield break;
        float elapsed = 0f;
        while (elapsed < duration)
        {
            if (target == null) yield break;
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            // Ease out quad
            t = 1f - (1f - t) * (1f - t);
            target.position = Vector3.Lerp(from, to, t);
            yield return null;
        }
        if (target != null) target.position = to;
    }

    /// <summary>
    /// Tween many transforms from their 'from' positions to their 'to' positions
    /// in parallel over a single duration (instead of one after another).
    /// </summary>
    public static IEnumerator TweenPositions(List<(Transform tr, Vector3 from, Vector3 to)> movers, float duration)
    {
        if (movers == null || movers.Count == 0) yield break;

        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            t = PolishMotion.EaseInOutCubic(t);
            for (int i = 0; i < movers.Count; i++)
            {
                var m = movers[i];
                if (m.tr != null)
                    m.tr.position = Vector3.Lerp(m.from, m.to, t);
            }
            yield return null;
        }
        for (int i = 0; i < movers.Count; i++)
        {
            var m = movers[i];
            if (m.tr != null)
                m.tr.position = m.to;
        }
    }

    /// <summary>Moves both swapped pieces concurrently with a tiny lift and scale accent.</summary>
    public static IEnumerator TweenSwapPair(
        Transform first, Vector3 firstFrom, Vector3 firstTo,
        Transform second, Vector3 secondFrom, Vector3 secondTo,
        float duration)
    {
        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            float eased = PolishMotion.EaseInOutCubic(t);
            float lift = Mathf.Sin(t * Mathf.PI) * 0.055f;
            float scale = 1f + Mathf.Sin(t * Mathf.PI) * 0.06f;

            if (first != null)
            {
                first.position = Vector3.Lerp(firstFrom, firstTo, eased) + Vector3.up * lift;
                first.localScale = Vector3.one * scale;
            }
            if (second != null)
            {
                second.position = Vector3.Lerp(secondFrom, secondTo, eased) - Vector3.up * lift;
                second.localScale = Vector3.one * scale;
            }
            yield return null;
        }

        if (first != null) { first.position = firstTo; first.localScale = Vector3.one; }
        if (second != null) { second.position = secondTo; second.localScale = Vector3.one; }
    }

    /// <summary>Distance-aware fall with a restrained overshoot and landing squash.</summary>
    public static IEnumerator TweenPositionsJuicy(List<(Transform tr, Vector3 from, Vector3 to, int distance)> movers)
    {
        if (movers == null || movers.Count == 0) yield break;

        float duration = 0f;
        for (int i = 0; i < movers.Count; i++)
            duration = Mathf.Max(duration, PolishMotion.FallDuration(movers[i].distance));

        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            for (int i = 0; i < movers.Count; i++)
            {
                var m = movers[i];
                if (m.tr == null) continue;
                float local = Mathf.Clamp01(elapsed / PolishMotion.FallDuration(m.distance));
                float eased = PolishMotion.EaseOutBack(local);
                m.tr.position = Vector3.LerpUnclamped(m.from, m.to, eased);
                float squash = 1f - Mathf.Sin(local * Mathf.PI) * 0.035f;
                m.tr.localScale = new Vector3(1f / squash, squash, 1f);
            }
            yield return null;
        }

        for (int i = 0; i < movers.Count; i++)
        {
            var m = movers[i];
            if (m.tr == null) continue;
            m.tr.position = m.to;
            m.tr.localScale = Vector3.one;
        }
    }

    public static IEnumerator PunchScales(Transform first, Transform second, float amount, float duration)
    {
        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            float scale = 1f + Mathf.Sin(t * Mathf.PI) * amount;
            if (first != null) first.localScale = Vector3.one * scale;
            if (second != null) second.localScale = Vector3.one * scale;
            yield return null;
        }
        if (first != null) first.localScale = Vector3.one;
        if (second != null) second.localScale = Vector3.one;
    }

    public static IEnumerator TweenScale(Transform target, Vector3 from, Vector3 to, float duration)
    {
        if (target == null) yield break;
        float elapsed = 0f;
        while (elapsed < duration)
        {
            if (target == null) yield break;
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            t = 1f - (1f - t) * (1f - t);
            target.localScale = Vector3.Lerp(from, to, t);
            yield return null;
        }
        if (target != null) target.localScale = to;
    }

    public static IEnumerator ShrinkAndDestroy(GameObject obj, float duration)
    {
        if (obj == null) yield break;
        Transform tr = obj.transform;
        yield return TweenScale(tr, Vector3.one, Vector3.zero, duration);
        if (obj != null) Object.Destroy(obj);
    }

    public static IEnumerator PopIn(GameObject obj, float duration)
    {
        if (obj == null) yield break;
        obj.transform.localScale = Vector3.zero;
        yield return TweenScale(obj.transform, Vector3.zero, Vector3.one, duration);
    }
}
