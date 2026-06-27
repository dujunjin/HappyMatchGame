using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Phase E: plays procedural SFX through four mix groups (Master / Ambient /
/// SFX / UI). Each group has its own volume multiplier; Master scales
/// everything. Clips come from ProceduralAudio (generated from AudioCatalog
/// events). Per-event cooldown and max-concurrent caps prevent harsh stacking
/// during cascades (spec §6: "同类短音效设置最小触发间隔与最大并发数").
///
/// Usage: GameManager owns one AudioManager; gameplay code calls
/// AudioManager.Play(AudioCatalog.Event.X). Optional pitchMult shifts the
/// whole event (e.g. cascade levels raise the clear tone).
/// </summary>
public class AudioManager : MonoBehaviour
{
    public enum Group { Master, Ambient, SFX, UI }

    [Range(0f, 1f)] public float masterVolume = 1f;
    [Range(0f, 1f)] public float ambientVolume = 0.6f;
    [Range(0f, 1f)] public float sfxVolume = 0.85f;
    [Range(0f, 1f)] public float uiVolume = 0.8f;

    private readonly List<AudioSource> _pool = new List<AudioSource>();
    private readonly Dictionary<AudioCatalog.Event, float> _lastPlay = new Dictionary<AudioCatalog.Event, float>();
    private readonly Dictionary<AudioCatalog.Event, List<AudioSource>> _active = new Dictionary<AudioCatalog.Event, List<AudioSource>>();
    private AudioCatalog _catalog;

    public void Init(AudioCatalog catalog)
    {
        _catalog = catalog ?? AudioCatalog.Default;
        // Pre-warm a few AudioSources.
        for (int i = 0; i < 6; i++) _pool.Add(NewSource());
    }

    public void Play(AudioCatalog.Event ev, float pitchMult = 1f)
    {
        if (this == null) return;
        AudioCatalog.Entry entry = _catalog != null ? _catalog.Get(ev) : null;
        if (entry == null) entry = new AudioCatalog.Entry();

        float now = Time.time;
        if (_lastPlay.TryGetValue(ev, out float last) && entry.cooldown > 0f && now - last < entry.cooldown)
            return;

        // Prune finished sources for this event.
        if (_active.TryGetValue(ev, out var list))
            list.RemoveAll(s => s == null || !s.isPlaying);
        else
            _active[ev] = list = new List<AudioSource>();

        if (list.Count >= entry.maxConcurrent) return;

        AudioClip clip = ProceduralAudio.Get(ev);
        if (clip == null) return;

        AudioSource src = AcquireSource();
        if (src == null) return;

        Group g = GroupFor(ev);
        float vol = masterVolume * GroupVolume(g) * entry.volume;
        float pitch = Random.Range(entry.pitchRange.x, entry.pitchRange.y) * pitchMult;

        src.clip = clip;
        src.volume = vol;
        src.pitch = Mathf.Max(0.1f, pitch);
        src.spatialBlend = 0f; // 2D
        src.Play();

        _lastPlay[ev] = now;
        list.Add(src);
    }

    private AudioSource AcquireSource()
    {
        for (int i = 0; i < _pool.Count; i++)
            if (!_pool[i].isPlaying) return _pool[i];
        // Grow the pool.
        AudioSource src = NewSource();
        _pool.Add(src);
        return src;
    }

    private AudioSource NewSource()
    {
        GameObject go = new GameObject("AudioSrc");
        go.transform.SetParent(transform, false);
        AudioSource s = go.AddComponent<AudioSource>();
        s.playOnAwake = false;
        return s;
    }

    private float GroupVolume(Group g)
    {
        switch (g)
        {
            case Group.Ambient: return ambientVolume;
            case Group.SFX: return sfxVolume;
            case Group.UI: return uiVolume;
            default: return 1f;
        }
    }

    private static Group GroupFor(AudioCatalog.Event ev)
    {
        switch (ev)
        {
            case AudioCatalog.Event.UiClick:
            case AudioCatalog.Event.TargetBounce:
            case AudioCatalog.Event.Win:
            case AudioCatalog.Event.Lose:
                return Group.UI;
            default:
                return Group.SFX;
        }
    }
}
