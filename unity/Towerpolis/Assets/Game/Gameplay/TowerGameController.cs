using System;
using System.Globalization;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using Towerpolis.Core.Determinism;
using Towerpolis.Core.Gameplay;
using Towerpolis.Core.Meta;
using Towerpolis.Game.Meta;

namespace Towerpolis.Game.Gameplay
{
    /// <summary>
    /// MVP core-loop state machine and the bridge to deterministic Core. Owns the <see cref="TowerRun"/>
    /// (grading/scoring), the seeded <see cref="BlockSequence"/> and swing stream, and drives the loop:
    /// spawn → swing → tap-drop → swept contact → Core grade → weld+slice → wobble → repeat, until the
    /// cumulative 2-strike topple ends the run. Score is decided entirely in Core before any animation.
    /// Juice (squash, dust, confetti, shake, chime), HUD and residents come in later passes.
    /// </summary>
    public sealed class TowerGameController : MonoBehaviour
    {
        enum State { Idle, Swinging, Falling, Settling, Over }

        [Header("Wiring")]
        [SerializeField] GameTuning tuning;
        [SerializeField] CraneController crane;
        [SerializeField] TowerController tower;
        [SerializeField] BlockSpawner spawner;
        [SerializeField] CameraRig cameraRig;

        [Header("Timing")]
        [SerializeField] float settleDelay = 0.25f;

        State _state = State.Idle;
        CoreConfig _coreConfig;
        TowerRun _run;
        BlockSequence _sequence;
        float _swingPhase;

        Transform _pendingBlock;
        FloorType _pendingType;
        FallingBlock _falling;
        float _settleTimer;

        public int Score => _run != null ? _run.RunScore : 0;
        public int Floors => _run != null ? _run.FloorCount : 0;
        public int Strikes => _run != null ? _run.MissStrikes : 0;
        public int PerfectChain => _run != null ? _run.PerfectChain : 0; // GameAudio climbs a scale with it
        public int ComboLevel => _run != null ? _run.ComboLevel : 0;     // Phase B: drives the HUD combo meter
        public bool IsOver => _state == State.Over;

        /// <summary>The current run's frozen result (for the meta deposit/scoring). Default if no run yet.</summary>
        public RunResult BuildRunResult() => _run != null ? RunResult.From(_run) : default;

        // HUD events — the HUDController subscribes to these.
        public event Action<int> ScoreChanged;   // new total score
        public event Action<int> FloorAdded;      // new floor count
        public event Action<int> StrikeAdded;     // strike number reached (1 or 2)
        public event Action<Vector3> PerfectHit;  // world position for the "PERFECT!" pop
        public event Action<Vector3, bool, int> FloorPlacedAt; // world base pos + isPerfect + residentsAdded — for VFX (dust/confetti/residents)
        public event Action<int> ComboCompleted; // the combo bar filled → bonus score awarded (HUD flash + "+N")
        public event Action RunToppled;
        public event Action RunStarted;

        float _overSince;

        void Start()
        {
            EnsureJuice();
            NewRun();
        }

        // Bring up the juice (audio + VFX) with zero scene wiring; if one was added manually (to tune it),
        // keep that and don't add a second.
        void EnsureJuice()
        {
            if (FindFirstObjectByType<Towerpolis.Game.Audio.GameAudio>() == null)
                gameObject.AddComponent<Towerpolis.Game.Audio.GameAudio>();
            if (FindFirstObjectByType<Towerpolis.Game.Vfx.GameVfx>() == null)
                gameObject.AddComponent<Towerpolis.Game.Vfx.GameVfx>();
            if (FindFirstObjectByType<Towerpolis.Game.Vfx.ResidentFlyIn>() == null)
                gameObject.AddComponent<Towerpolis.Game.Vfx.ResidentFlyIn>();
            if (FindFirstObjectByType<Towerpolis.Game.Rendering.LookDev>() == null)
                gameObject.AddComponent<Towerpolis.Game.Rendering.LookDev>();
            if (FindFirstObjectByType<Towerpolis.Game.Meta.MetaService>() == null)
                gameObject.AddComponent<Towerpolis.Game.Meta.MetaService>();
            if (FindFirstObjectByType<Towerpolis.Game.UI.MetaHud>() == null)
                gameObject.AddComponent<Towerpolis.Game.UI.MetaHud>();
            if (FindFirstObjectByType<Towerpolis.Game.Meta.Atmosphere>() == null)
                gameObject.AddComponent<Towerpolis.Game.Meta.Atmosphere>();
            if (FindFirstObjectByType<Towerpolis.Game.Meta.BackgroundLayer>() == null)
                gameObject.AddComponent<Towerpolis.Game.Meta.BackgroundLayer>();
        }

        public enum RunMode { Endless, Daily }
        public RunMode Mode { get; private set; } = RunMode.Endless;
        public string DailyDateKey { get; private set; } = "";

        /// <summary>Start a fresh ENDLESS run (also the retry path). Each one gets a new random seed.</summary>
        public void NewRun()
        {
            Mode = RunMode.Endless;
            BeginRun();
        }

        /// <summary>Start today's DAILY-seed run (same crane/sequence for everyone on this UTC day). The UI
        /// gates it to one attempt per day.</summary>
        public void StartDaily()
        {
            Mode = RunMode.Daily;
            DailyDateKey = DateTime.UtcNow.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
            BeginRun();
        }

        void BeginRun()
        {
            ClearTower();
            DiscardLooseBlock(); // drop any in-flight crane/falling block (e.g. switching districts mid-run)

            // Dress the run for the active district (block palette + sky).
            string district = MetaService.Instance != null ? MetaService.Instance.ActiveDistrictId : "downtown";
            DistrictTheme theme = DistrictThemes.For(district);
            if (spawner != null) spawner.ApplyTheme(theme);
            DistrictSky.SetDistrict(theme); // ground sky; the Atmosphere driver lerps it toward space with height

            _coreConfig = tuning != null ? tuning.BuildCoreConfig() : new CoreConfig();
            _run = new TowerRun(_coreConfig);

            ulong seed = Mode == RunMode.Daily
                ? DailySeed.ForDateUtc(DateTime.UtcNow)                            // shared global daily seed
                : SeedMix.SplitMix64(unchecked((ulong)DateTime.UtcNow.Ticks));     // fresh random endless seed
            _sequence = new BlockSequence(seed);
            XorShiftRng swingRng = RunSeeds.SwingRng(seed);
            _swingPhase = (float)(swingRng.NextDouble() * 2.0 * Mathf.PI);

            Transform baseBlock = spawner.CreateBase(_coreConfig.InitialBlockWidth);
            baseBlock.position = Vector3.zero;
            tower.Init(tuning, baseBlock, tuning.floorHeight, _coreConfig.InitialBlockWidth);
            if (cameraRig != null) cameraRig.Init(tuning, tower);

            SpawnNext();
            RunStarted?.Invoke();
        }

        void ClearTower()
        {
            if (tower == null) return;
            for (int i = tower.transform.childCount - 1; i >= 0; i--)
                Destroy(tower.transform.GetChild(i).gameObject);
            tower.transform.localRotation = Quaternion.identity;
        }

        // Destroy the block currently on the crane or mid-fall (not yet welded → not a tower child), so a
        // mid-run restart/district-switch doesn't leave a stray block the new tower builds through.
        void DiscardLooseBlock()
        {
            if (_pendingBlock != null) Destroy(_pendingBlock.gameObject);
            _pendingBlock = null;
            if (_falling != null) Destroy(_falling.gameObject);
            _falling = null;
        }

        void SpawnNext()
        {
            // Phase C: a Perfect streak can upgrade the seeded type to a specialty block (more residents).
            _pendingType = _run.NextSpawnType(_sequence.Next());
            int nextFloor = _run.FloorCount + 1;
            _pendingBlock = spawner.CreateBlock(_pendingType, tower.TopWidth, "Floor_" + nextFloor);
            float period = tuning.SwingPeriod(nextFloor);
            crane.BeginSwing(_pendingBlock, tower, tuning.craneHeight, tuning.SwingArc(nextFloor), period, _swingPhase, tuning.craneCableLength, tuning.craneTiltFactor, tuning.floorHeight);
            _state = State.Swinging;
        }

        void Update()
        {
            switch (_state)
            {
                case State.Swinging:
                    HandleSwingInput();
                    break;
                case State.Settling:
                    _settleTimer -= Time.deltaTime;
                    if (_settleTimer <= 0f) SpawnNext();
                    break;
                case State.Over:
                    if (Time.time - _overSince > 1.0f && TapPressed()) NewRun();
                    break;
            }
        }

        // Swing-phase input: a plain tap drops the swinging block on press.
        void HandleSwingInput()
        {
            if (InputGate.Suppress) return; // a modal UI is open
            Pointer p = Pointer.current;
            if (p == null || !p.press.wasPressedThisFrame) return;
            var es = EventSystem.current;
            if (es != null && es.IsPointerOverGameObject()) return; // tap landed on a UI button
            DropBlock();
        }

        void DropBlock()
        {
            Transform block = crane.Release();
            _swingPhase = crane.Phase; // continue the swing phase on the next floor
            _falling = block.gameObject.AddComponent<FallingBlock>();
            _falling.OnContact = OnBlockContact;
            _falling.Release(tower.TopY, tuning.gravityScale);
            _state = State.Falling;
        }

        void OnBlockContact(float contactX)
        {
            // Measure the offset against the CURRENT (swaying) top, so a clean hit on a leaning tower
            // doesn't read as off-centre.
            float offsetX = contactX - tower.TopWorldX;

            // Magnet upgrade: nudge the block toward centre BEFORE grading (progression-spec §2.2). Endless
            // only — GetMagnetFraction returns 0 in Daily, so the daily run grades the raw offset (fair).
            float magnet = MetaService.Instance != null ? MetaService.Instance.MagnetFraction(Mode == RunMode.Daily) : 0f;
            float gradedOffset = magnet > 0f ? offsetX * (1f - magnet) : offsetX;

            DropOutcome outcome = _run.PlaceBlock(_pendingType, gradedOffset);
            if (_falling != null) _falling.enabled = false; // its job is done either way

            if (outcome.FloorPlaced)
            {
                // Perfect → snap directly above the floor below (offset 0); Good → keep the (magnet-corrected)
                // overhang. The tower welds it FLUSH in local space (no gaps/overlaps under the sway).
                float offsetApplied = outcome.Grade == Grade.Perfect ? 0f : gradedOffset;
                tower.WeldPlaced(_pendingBlock, offsetApplied, outcome.TopWidth, _run.FloorCount, _run.LeanOffset);
                _pendingBlock.gameObject.AddComponent<SettleUpright>().Play(); // right the fall tilt
                // NB: placed blocks deliberately get NO active collider. They are kinematic Rigidbodies on the
                // swaying parent; a dynamic miss resolving against those swaying kinematic colliders snagged on
                // "air" (the collider's physics pose lagged the visual sway). The stack is fake-physics
                // (ADR-0002) and the swept fall-contact is scripted, so no collider is needed here.
            }
            else
            {
                // A miss free-tumbles off the side it overshot and falls away — no tower collision, so it can
                // never snag. Score is decided in Core, so this is pure juice and safe to decouple from physics.
                TumbleAway(_pendingBlock, offsetX);
            }

            Vector3 placedPos = _pendingBlock != null ? _pendingBlock.position : Vector3.zero;
            _pendingBlock = null;
            _falling = null;

            ScoreChanged?.Invoke(Score);
            if (outcome.FloorPlaced)
            {
                FloorAdded?.Invoke(Floors);
                FloorPlacedAt?.Invoke(placedPos, outcome.Grade == Grade.Perfect, outcome.ResidentsAdded);
            }
            if (outcome.Grade == Grade.Perfect)
                PerfectHit?.Invoke(placedPos + Vector3.up * tuning.floorHeight);
            if (outcome.Grade == Grade.Miss) StrikeAdded?.Invoke(Strikes);
            if (outcome.ComboBonusScore > 0) ComboCompleted?.Invoke(outcome.ComboBonusScore);

            if (outcome.Toppled)
            {
                _state = State.Over;
                _overSince = Time.time;
                crane.EndSwing(); // stop the empty hook swinging over the toppled tower
                RunToppled?.Invoke();
                Debug.Log("[Towerpolis] Run over — floors " + Floors + ", score " + Score);
            }
            else
            {
                _settleTimer = settleDelay;
                _state = State.Settling;
            }
        }

        static void TumbleAway(Transform block, float offsetX)
        {
            if (block == null) return;
            var rb = block.GetComponent<Rigidbody>();
            if (rb != null)
            {
                rb.isKinematic = false;
                rb.useGravity = true;
                rb.constraints = RigidbodyConstraints.None;
                // Knocked off the side it overshot: an outward push + a downward kick + a spin so it tumbles
                // clear of the tower and falls away. The block has no active collider, so it hits nothing and
                // can't snag — it just arcs off-screen and is cleaned up below.
                float dir = offsetX >= 0f ? 1f : -1f;
                rb.linearVelocity = new Vector3(dir * 1.6f, -1.0f, 0f);
                rb.angularVelocity = new Vector3(0f, 0f, -dir * 2.4f);
            }
            Destroy(block.gameObject, 4f);
        }

        static bool TapPressed()
        {
            if (InputGate.Suppress) return false; // a modal UI (e.g. the city view) is open
            var es = EventSystem.current;
            if (es != null && es.IsPointerOverGameObject()) return false; // tap landed on a UI button
            Pointer p = Pointer.current;
            return p != null && p.press.wasPressedThisFrame;
        }
    }
}
