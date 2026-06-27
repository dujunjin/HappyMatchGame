using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Phase E: synthesizes short AudioClips procedurally (no imported audio
/// assets). Each AudioCatalog.Event maps to a generated clip, cached after
/// first creation. Primitives: tone (sine + decay envelope), noise burst,
/// frequency sweep, and an arpeggio (chime). Composed into the SFX set the
/// spec lists (§6): swap, invalid, clear, cascade, suitcase break/collect,
/// rocket, bomb, propeller fly/hit, win, lose, UI click, target bounce.
/// </summary>
public static class ProceduralAudio
{
    private const float SR = 44100f;
    private static readonly Dictionary<AudioCatalog.Event, AudioClip> _cache = new Dictionary<AudioCatalog.Event, AudioClip>();

    public static AudioClip Get(AudioCatalog.Event ev)
    {
        if (_cache.TryGetValue(ev, out AudioClip c) && c != null) return c;
        c = Make(ev);
        _cache[ev] = c;
        return c;
    }

    private static AudioClip Make(AudioCatalog.Event ev)
    {
        switch (ev)
        {
            case AudioCatalog.Event.Swap: return Tone(620f, 0.08f, 0.5f);
            case AudioCatalog.Event.SwapInvalid: return Sweep(420f, 220f, 0.16f, 0.4f);
            case AudioCatalog.Event.MatchClear: return Tone(720f, 0.10f, 0.45f);
            case AudioCatalog.Event.Cascade: return Tone(820f, 0.10f, 0.45f);
            case AudioCatalog.Event.SuitcaseHit: return NoiseBurst(0.13f, 0.5f, 0.7f); // filtered-ish low
            case AudioCatalog.Event.SuitcaseCollect: return Arpeggio(new float[] { 740f, 990f, 1320f }, 0.16f, 0.4f);
            case AudioCatalog.Event.RocketActivate: return Sweep(300f, 1400f, 0.26f, 0.45f);
            case AudioCatalog.Event.BombActivate: return MixedLow(80f, 0.30f, 0.6f);
            case AudioCatalog.Event.PropellerFly: return Whir(0.30f, 0.35f);
            case AudioCatalog.Event.PropellerHit: return Tone(1250f, 0.08f, 0.4f);
            case AudioCatalog.Event.Win: return Arpeggio(new float[] { 523f, 659f, 784f, 1047f }, 0.6f, 0.45f);
            case AudioCatalog.Event.Lose: return Arpeggio(new float[] { 440f, 349f, 262f }, 0.5f, 0.4f);
            case AudioCatalog.Event.Shuffle: return Sweep(220f, 660f, 0.25f, 0.35f);
            case AudioCatalog.Event.UiClick: return Tone(880f, 0.05f, 0.35f);
            case AudioCatalog.Event.TargetBounce: return Tone(560f, 0.06f, 0.4f);
            default: return Tone(440f, 0.05f, 0.3f);
        }
    }

    // --- primitives ---

    private static AudioClip Tone(float freq, float dur, float vol)
    {
        int n = Mathf.Max(1, (int)(SR * dur));
        float[] data = new float[n];
        for (int i = 0; i < n; i++)
        {
            float t = i / SR;
            float env = DecayEnv(i, n, 0.25f);
            data[i] = Mathf.Sin(2f * Mathf.PI * freq * t) * env * vol;
        }
        return Build("tone", data);
    }

    private static AudioClip Sweep(float f0, float f1, float dur, float vol)
    {
        int n = Mathf.Max(1, (int)(SR * dur));
        float[] data = new float[n];
        float phase = 0f;
        for (int i = 0; i < n; i++)
        {
            float t = i / n;
            float freq = Mathf.Lerp(f0, f1, t);
            phase += 2f * Mathf.PI * freq / SR;
            float env = DecayEnv(i, n, 0.4f);
            data[i] = Mathf.Sin(phase) * env * vol;
        }
        return Build("sweep", data);
    }

    private static AudioClip NoiseBurst(float dur, float vol, float lowpass)
    {
        int n = Mathf.Max(1, (int)(SR * dur));
        float[] data = new float[n];
        float prev = 0f;
        for (int i = 0; i < n; i++)
        {
            float env = DecayEnv(i, n, 0.3f);
            float white = Random.value * 2f - 1f;
            // crude low-pass: blend with previous sample.
            prev = prev * (1f - lowpass) + white * lowpass;
            data[i] = prev * env * vol;
        }
        return Build("noise", data);
    }

    private static AudioClip Arpeggio(float[] freqs, float totalDur, float vol)
    {
        int total = Mathf.Max(1, (int)(SR * totalDur));
        float[] data = new float[total];
        int seg = total / Mathf.Max(1, freqs.Length);
        for (int s = 0; s < freqs.Length; s++)
        {
            for (int i = 0; i < seg && s * seg + i < total; i++)
            {
                int idx = s * seg + i;
                float t = i / SR;
                float env = DecayEnv(i, seg, 0.3f);
                data[idx] = Mathf.Sin(2f * Mathf.PI * freqs[s] * t) * env * vol;
            }
        }
        return Build("arp", data);
    }

    private static AudioClip MixedLow(float freq, float dur, float vol)
    {
        // Low sine + noise = a "boom".
        int n = Mathf.Max(1, (int)(SR * dur));
        float[] data = new float[n];
        for (int i = 0; i < n; i++)
        {
            float t = i / SR;
            float env = DecayEnv(i, n, 0.45f);
            float tone = Mathf.Sin(2f * Mathf.PI * freq * t);
            float noise = (Random.value * 2f - 1f) * 0.5f;
            data[i] = (tone + noise) * env * vol * 0.6f;
        }
        return Build("boom", data);
    }

    private static AudioClip Whir(float dur, float vol)
    {
        // Oscillating tone — propeller spin.
        int n = Mathf.Max(1, (int)(SR * dur));
        float[] data = new float[n];
        for (int i = 0; i < n; i++)
        {
            float t = i / SR;
            float env = DecayEnv(i, n, 0.5f);
            float freq = 500f + Mathf.Sin(t * 60f) * 200f;
            data[i] = Mathf.Sin(2f * Mathf.PI * freq * t) * env * vol;
        }
        return Build("whir", data);
    }

    private static float DecayEnv(int i, int n, float attackFrac)
    {
        float t = (float)i / n;
        // Quick attack then exponential decay.
        float attack = t < attackFrac ? (t / attackFrac) : 1f;
        float decay = Mathf.Exp(-t * 4f);
        return attack * decay;
    }

    private static AudioClip Build(string name, float[] data)
    {
        AudioClip c = AudioClip.Create(name, data.Length, 1, (int)SR, false);
        c.SetData(data, 0);
        return c;
    }
}
