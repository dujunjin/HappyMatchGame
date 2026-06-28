using UnityEngine;

/// <summary>
/// Shared, deterministic presentation math. Keeping curves and duration
/// policy here makes gameplay animations consistent and directly testable.
/// </summary>
public static class PolishMotion
{
    public static float EaseOutBack(float t)
    {
        t = Mathf.Clamp01(t);
        const float c1 = 1.70158f;
        const float c3 = c1 + 1f;
        return 1f + c3 * Mathf.Pow(t - 1f, 3f) + c1 * Mathf.Pow(t - 1f, 2f);
    }

    public static float EaseInOutCubic(float t)
    {
        t = Mathf.Clamp01(t);
        return t < 0.5f
            ? 4f * t * t * t
            : 1f - Mathf.Pow(-2f * t + 2f, 3f) * 0.5f;
    }

    public static float EaseOutQuart(float t)
    {
        t = Mathf.Clamp01(t);
        return 1f - Mathf.Pow(1f - t, 4f);
    }

    public static float FallDuration(int rowDistance)
    {
        return Mathf.Clamp(0.18f + Mathf.Max(0, rowDistance - 1) * 0.025f, 0.18f, 0.34f);
    }

    public static bool IsColorMatchable(ElementType type)
    {
        return type == ElementType.Red ||
               type == ElementType.Blue ||
               type == ElementType.Yellow ||
               type == ElementType.Green;
    }
}
