using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// ScriptableObject container for every sound event the game can play.
/// Each entry carries an optional AudioClip (real audio is dropped in
/// later), plus per-event volume, pitch range, cooldown, and a cap on
/// concurrent playbacks. Phase A only builds the container and a
/// programmatic fallback (null clips => no audio). Phase E will wire an
/// AudioManager to actually play these.
/// </summary>
[CreateAssetMenu(fileName = "AudioCatalog", menuName = "HappyMatch/AudioCatalog", order = 2)]
public class AudioCatalog : ScriptableObject
{
    public enum Event
    {
        Swap,
        SwapInvalid,
        MatchClear,
        Cascade,
        RocketActivate,
        BombActivate,
        SuitcaseHit,
        SuitcaseCollect,
        Win,
        Lose,
        Shuffle,
        UiClick,
    }

    [System.Serializable]
    public class Entry
    {
        public AudioClip clip;
        [Range(0f, 1f)] public float volume = 1f;
        public Vector2 pitchRange = new Vector2(1f, 1f);
        [Min(0f)] public float cooldown = 0f;
        [Min(1)] public int maxConcurrent = 4;
    }

    // Indexed by Event for stable lookup. Missing entries default to a
    // silent no-op entry so callers never null-deref.
    public List<Entry> entries = new List<Entry>();

    private Dictionary<Event, Entry> _lookup;
    private static readonly Entry Silent = new Entry();

    public Entry Get(Event ev)
    {
        if (_lookup == null) RebuildLookup();
        return _lookup.TryGetValue(ev, out Entry e) ? e : Silent;
    }

    public bool HasClip(Event ev)
    {
        Entry e = Get(ev);
        return e != null && e.clip != null;
    }

    public void RebuildLookup()
    {
        _lookup = new Dictionary<Event, Entry>();
        if (entries == null) return;
        for (int i = 0; i < entries.Count; i++)
        {
            Event ev = (Event)i;
            if (i < entries.Count) _lookup[ev] = entries[i];
        }
    }

    private static AudioCatalog _default;

    public static AudioCatalog Default
    {
        get
        {
            if (_default == null)
            {
                _default = CreateInstance<AudioCatalog>();
                // One silent entry per event so Get() always returns something.
                for (int i = 0; i < System.Enum.GetValues(typeof(Event)).Length; i++)
                    _default.entries.Add(new Entry());
                _default.RebuildLookup();
            }
            return _default;
        }
    }
}
