using System.Collections;
using UnityEngine;
using Towerpolis.Game.Gameplay;
using Towerpolis.Game.Meta;

namespace Towerpolis.Game.Audio
{
    /// <summary>
    /// Plays SFX and looping music off the <see cref="TowerGameController"/> events using YOUR audio
    /// files. Each clip is taken from the inspector slot if set, otherwise auto-loaded from a Resources
    /// folder by a fixed name (drop files into <c>Assets/Audio/Resources/Sfx</c> and
    /// <c>Assets/Audio/Resources/Music</c> — see <c>docs/AUDIO_GUIDE.md</c>). Missing clips are simply
    /// skipped (silent, no error), so you can add sounds one at a time. A small AudioSource voice-pool
    /// lets overlapping hits ring out. Self-bootstraps (the controller adds it) so the Resources route
    /// needs no scene wiring; add the component manually only to drag clips in or tune volumes.
    /// </summary>
    public sealed class GameAudio : MonoBehaviour
    {
        [Header("Volumes")]
        [Range(0f, 1f)] [SerializeField] float masterVolume = 0.9f;
        [Range(0f, 1f)] [SerializeField] float musicVolume = 0.5f;
        [SerializeField] bool playMusic = true;
        [SerializeField] int voices = 6;

        [Header("SFX clips (optional — else Resources/Sfx/<name>)")]
        [SerializeField] AudioClip landClip;     // Resources/Sfx/land    — every placed floor
        [SerializeField] AudioClip perfectClip;  // Resources/Sfx/perfect — a Perfect hit
        [SerializeField] AudioClip missClip;     // Resources/Sfx/miss    — a missed block
        [SerializeField] AudioClip toppleClip;   // Resources/Sfx/topple  — run over (collapse)
        [SerializeField] AudioClip startClip;    // Resources/Sfx/start   — new run

        [Header("Music — per-district beds (optional). Each falls back to Resources/Music/<district>, then")]
        [Tooltip("the shared theme below. The active bed crossfades when you switch district (meta-spec §2/§9).")]
        [SerializeField] AudioClip downtownMusic;
        [SerializeField] AudioClip neonMusic;
        [SerializeField] AudioClip winterMusic;
        [Tooltip("Fallback bed used when a district has no track yet (else Resources/Music/theme).")]
        [SerializeField] AudioClip musicClip;
        [Tooltip("Seconds to crossfade between district beds on a district switch.")]
        [Range(0.1f, 4f)] [SerializeField] float musicCrossfade = 0.9f;

        [Header("Feel")]
        [Tooltip("Perfect pitches UP with the combo (a tonal chime/bell works best).")]
        [SerializeField] bool perfectClimbs = true;
        [Tooltip("Tiny random pitch on the land sound so repeats don't feel robotic.")]
        [Range(0f, 0.2f)] [SerializeField] float landPitchJitter = 0.04f;

        // Ascending scale (semitones) walked by the perfect-chain.
        static readonly int[] ChainScale = { 0, 2, 4, 7, 9, 12, 14, 16, 19 };

        TowerGameController _controller;
        AudioSource[] _sources;
        int _next;
        AudioClip _land, _perfect, _miss, _topple, _start, _theme;

        // Two music voices to crossfade between on a district switch; _active is the audible one.
        AudioSource _musicA, _musicB, _active;
        Coroutine _fade;
        string _currentDistrict = "";

        void Awake()
        {
            _land = landClip != null ? landClip : Resources.Load<AudioClip>("Sfx/land");
            _perfect = perfectClip != null ? perfectClip : Resources.Load<AudioClip>("Sfx/perfect");
            _miss = missClip != null ? missClip : Resources.Load<AudioClip>("Sfx/miss");
            _topple = toppleClip != null ? toppleClip : Resources.Load<AudioClip>("Sfx/topple");
            _start = startClip != null ? startClip : Resources.Load<AudioClip>("Sfx/start");
            _theme = musicClip != null ? musicClip : Resources.Load<AudioClip>("Music/theme");

            // Without a listener PlayOneShot is silent — guarantee one (the hand-wired camera may lack it).
            if (FindFirstObjectByType<AudioListener>() == null)
            {
                var cam = Camera.main;
                (cam != null ? cam.gameObject : gameObject).AddComponent<AudioListener>();
            }

            voices = Mathf.Max(1, voices);
            _sources = new AudioSource[voices];
            for (int i = 0; i < voices; i++)
            {
                var s = gameObject.AddComponent<AudioSource>();
                s.playOnAwake = false;
                s.spatialBlend = 0f; // 2D feedback sound
                _sources[i] = s;
            }

            // Two looping music voices so the bed can crossfade on a district switch. Which clip plays is
            // chosen on RunStarted from the active district (so retries keep the same bed, no re-fade).
            _musicA = NewMusicSource();
            _musicB = NewMusicSource();
        }

        AudioSource NewMusicSource()
        {
            var s = gameObject.AddComponent<AudioSource>();
            s.loop = true;
            s.playOnAwake = false;
            s.spatialBlend = 0f;
            s.volume = 0f;
            return s;
        }

        void OnEnable() => Bind();
        void Start() => Bind();
        void OnDisable() => Unbind();

        void Bind()
        {
            if (_controller != null) return;
            _controller = FindFirstObjectByType<TowerGameController>();
            if (_controller == null) return;
            _controller.FloorAdded += OnFloor;
            _controller.PerfectHit += OnPerfect;
            _controller.StrikeAdded += OnStrike;
            _controller.RunToppled += OnToppled;
            _controller.RunStarted += OnStarted;
        }

        void Unbind()
        {
            if (_controller == null) return;
            _controller.FloorAdded -= OnFloor;
            _controller.PerfectHit -= OnPerfect;
            _controller.StrikeAdded -= OnStrike;
            _controller.RunToppled -= OnToppled;
            _controller.RunStarted -= OnStarted;
            _controller = null;
        }

        void OnFloor(int floors) => Play(_land, 0.8f, RandPitch(landPitchJitter));

        void OnPerfect(Vector3 _)
        {
            float pitch = 1f;
            if (perfectClimbs)
            {
                int chain = Mathf.Max(1, _controller != null ? _controller.PerfectChain : 1);
                int step = ChainScale[Mathf.Min(chain - 1, ChainScale.Length - 1)];
                pitch = Mathf.Pow(2f, step / 12f);
            }
            Play(_perfect, 0.9f, pitch);
        }

        void OnStrike(int strikes) => Play(_miss, 0.9f, 1f);
        void OnToppled() => Play(_topple, 1f, 1f);

        void OnStarted()
        {
            Play(_start, 0.7f, 1f);
            string district = MetaService.Instance != null ? MetaService.Instance.ActiveDistrictId : "downtown";
            SetDistrictMusic(district);
        }

        // Crossfade to the active district's music bed. No-op while the district is unchanged (so an Endless
        // retry doesn't restart the track), and when two districts share the same fallback theme.
        void SetDistrictMusic(string districtId)
        {
            if (!playMusic || _musicA == null) return;
            if (districtId == _currentDistrict) return;
            _currentDistrict = districtId;

            AudioClip bed = MusicFor(districtId);
            if (_active != null && _active.clip == bed && _active.isPlaying) return; // same clip → keep playing

            AudioSource from = _active;
            AudioSource to = _active == _musicA ? _musicB : _musicA; // null _active → picks A first
            if (bed != null)
            {
                to.clip = bed;
                to.volume = 0f;
                to.Play();
                _active = to;
            }
            else
            {
                _active = null; // no bed for this district → just fade the old one out
            }

            if (_fade != null) StopCoroutine(_fade);
            _fade = StartCoroutine(Crossfade(from, bed != null ? to : null));
        }

        // Inspector slot → Resources/Music/<district> → the shared fallback theme.
        AudioClip MusicFor(string id)
        {
            AudioClip slot = id switch
            {
                "neon" => neonMusic,
                "winter" => winterMusic,
                "downtown" => downtownMusic,
                _ => null,
            };
            if (slot == null) slot = Resources.Load<AudioClip>("Music/" + id);
            return slot != null ? slot : _theme;
        }

        // Fade `from` out and `to` in over musicCrossfade seconds. Unscaled time so slow-mo/pause can't stall it.
        IEnumerator Crossfade(AudioSource from, AudioSource to)
        {
            float dur = Mathf.Max(0.05f, musicCrossfade);
            float fromStart = from != null ? from.volume : 0f;
            float e = 0f;
            while (e < dur)
            {
                e += Time.unscaledDeltaTime;
                float k = Mathf.Clamp01(e / dur);
                if (from != null) from.volume = Mathf.Lerp(fromStart, 0f, k);
                if (to != null) to.volume = Mathf.Lerp(0f, musicVolume, k);
                yield return null;
            }
            if (from != null) { from.Stop(); from.volume = 0f; }
            if (to != null) to.volume = musicVolume;
            _fade = null;
        }

        void Play(AudioClip clip, float volume, float pitch)
        {
            if (clip == null || _sources == null) return;
            var s = _sources[_next];
            _next = (_next + 1) % _sources.Length;
            s.pitch = pitch;
            s.PlayOneShot(clip, Mathf.Clamp01(volume * masterVolume));
        }

        static float RandPitch(float amount) => amount <= 0f ? 1f : 1f + Random.Range(-amount, amount);
    }
}
