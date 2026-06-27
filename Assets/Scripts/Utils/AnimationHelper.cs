using UnityEngine;
using System.Collections;
using System.Collections.Generic;

/// <summary>
/// Coroutine-based animation helpers for tweening transforms.
/// </summary>
public static class AnimationHelper
{
    public static IEnumerator TweenPosition(Transform target, Vector3 from, Vector3 to, float duration)
    {
        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            // Ease out quad
            t = 1f - (1f - t) * (1f - t);
            target.position = Vector3.Lerp(from, to, t);
            yield return null;
        }
        target.position = to;
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
            t = 1f - (1f - t) * (1f - t); // ease out quad
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

    public static IEnumerator TweenScale(Transform target, Vector3 from, Vector3 to, float duration)
    {
        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            t = 1f - (1f - t) * (1f - t);
            target.localScale = Vector3.Lerp(from, to, t);
            yield return null;
        }
        target.localScale = to;
    }

    public static IEnumerator ShrinkAndDestroy(GameObject obj, float duration)
    {
        yield return TweenScale(obj.transform, Vector3.one, Vector3.zero, duration);
        Object.Destroy(obj);
    }

    public static IEnumerator PopIn(GameObject obj, float duration)
    {
        obj.transform.localScale = Vector3.zero;
        yield return TweenScale(obj.transform, Vector3.zero, Vector3.one, duration);
    }
}
