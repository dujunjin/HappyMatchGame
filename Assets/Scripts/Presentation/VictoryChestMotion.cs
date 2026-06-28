using UnityEngine;

/// <summary>
/// Pure timing model for the three authored chest poses. Keeping this math
/// outside WinSequence makes the opening order deterministic and testable.
/// </summary>
public static class VictoryChestMotion
{
    public const float OpeningDuration = 0.76f;
    public const float RetryButtonY = -212f;
    public const float ReplayButtonY = -268f;

    public readonly struct Weights
    {
        public readonly float closed;
        public readonly float cracked;
        public readonly float ajar;
        public readonly float wide;
        public readonly float open;

        public Weights(float closed, float cracked, float ajar, float wide, float open)
        {
            this.closed = closed;
            this.cracked = cracked;
            this.ajar = ajar;
            this.wide = wide;
            this.open = open;
        }
    }

    public static float NormalizedProgress(float elapsed)
    {
        return Mathf.Clamp01(elapsed / OpeningDuration);
    }

    public static Weights Evaluate(float progress)
    {
        float p = Mathf.Clamp01(progress);

        const float crackedKey = 0.23f;
        const float ajarKey = 0.46f;
        const float wideKey = 0.72f;

        if (p <= crackedKey)
        {
            float t = SmoothStep01(p / crackedKey);
            return new Weights(1f - t, t, 0f, 0f, 0f);
        }

        if (p <= ajarKey)
        {
            float t = SmoothStep01(Mathf.InverseLerp(crackedKey, ajarKey, p));
            return new Weights(0f, 1f - t, t, 0f, 0f);
        }

        if (p <= wideKey)
        {
            float t = SmoothStep01(Mathf.InverseLerp(ajarKey, wideKey, p));
            return new Weights(0f, 0f, 1f - t, t, 0f);
        }

        float release = 1f - Mathf.Pow(1f - Mathf.InverseLerp(wideKey, 1f, p), 3f);
        return new Weights(0f, 0f, 0f, 1f - release, release);
    }

    private static float SmoothStep01(float value)
    {
        float t = Mathf.Clamp01(value);
        return t * t * (3f - 2f * t);
    }
}
