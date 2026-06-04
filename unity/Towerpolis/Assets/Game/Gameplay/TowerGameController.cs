using System;
using UnityEngine;
using UnityEngine.InputSystem;
using Towerpolis.Core.Determinism;
using Towerpolis.Core.Gameplay;
using Towerpolis.Core.Meta;

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
        public bool IsOver => _state == State.Over;

        /// <summary>The current run's frozen result (for the meta deposit/scoring). Default if no run yet.</summary>
        public RunResult BuildRunResult() => _run != null ? RunResult.From(_run) : default;

        // HUD events — the HUDController subscribes to these.
        public event Action<int> ScoreChanged;   // new total score
        public event Action<int> FloorAdded;      // new floor count
        public event Action<int> StrikeAdded;     // strike number reached (1 or 2)
        public event Action<Vector3> PerfectHit;  // world position for the "PERFECT!" pop
        public event Action<Vector3, bool> FloorPlacedAt; // world base pos + isPerfect — for VFX (dust/confetti)
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
            if (FindFirstObjectByType<Towerpolis.Game.Rendering.LookDev>() == null)
                gameObject.AddComponent<Towerpolis.Game.Rendering.LookDev>();
            if (FindFirstObjectByType<Towerpolis.Game.Meta.MetaService>() == null)
                gameObject.AddComponent<Towerpolis.Game.Meta.MetaService>();
        }

        public void NewRun()
        {
            ClearTower();

            _coreConfig = tuning != null ? tuning.BuildCoreConfig() : new CoreConfig();
            _run = new TowerRun(_coreConfig);
            _sequence = new BlockSequence(RunSeeds.SeedMvp);
            XorShiftRng swingRng = RunSeeds.SwingRng(RunSeeds.SeedMvp);
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

        void SpawnNext()
        {
            _pendingType = _sequence.Next();
            int nextFloor = _run.FloorCount + 1;
            _pendingBlock = spawner.CreateBlock(_pendingType, tower.TopWidth, "Floor_" + nextFloor);
            float period = tuning.SwingPeriod(nextFloor);
            crane.BeginSwing(_pendingBlock, tower, tuning.craneHeight, tuning.swingHalfArc, period, _swingPhase, tuning.craneCableLength, tuning.craneTiltFactor, tuning.floorHeight);
            _state = State.Swinging;
        }

        void Update()
        {
            switch (_state)
            {
                case State.Swinging:
                    if (TapPressed()) DropBlock();
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
            DropOutcome outcome = _run.PlaceBlock(_pendingType, offsetX);
            if (_falling != null) _falling.enabled = false; // its job is done either way

            if (outcome.FloorPlaced)
            {
                // Perfect → MAGNET-snap directly above the floor below (offset 0); Good → keep the landed
                // overhang. The tower welds it FLUSH in local space (no gaps/overlaps under the sway).
                float offsetApplied = outcome.Grade == Grade.Perfect ? 0f : offsetX;
                tower.WeldPlaced(_pendingBlock, offsetApplied, outcome.TopWidth, _run.FloorCount, _run.LeanOffset);
                spawner.SetColliderEnabled(_pendingBlock, true); // solid face for missed blocks to land on / tip off
                _pendingBlock.gameObject.AddComponent<SettleUpright>().Play(); // right the fall tilt
            }
            else
            {
                spawner.SetColliderEnabled(_pendingBlock, true);
                TumbleAway(_pendingBlock, offsetX);
            }

            Vector3 placedPos = _pendingBlock != null ? _pendingBlock.position : Vector3.zero;
            _pendingBlock = null;
            _falling = null;

            ScoreChanged?.Invoke(Score);
            if (outcome.FloorPlaced)
            {
                FloorAdded?.Invoke(Floors);
                FloorPlacedAt?.Invoke(placedPos, outcome.Grade == Grade.Perfect);
            }
            if (outcome.Grade == Grade.Perfect) PerfectHit?.Invoke(placedPos + Vector3.up * tuning.floorHeight);
            if (outcome.Grade == Grade.Miss) StrikeAdded?.Invoke(Strikes);

            if (outcome.Toppled)
            {
                _state = State.Over;
                _overSince = Time.time;
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
                // Continuous detection so the falling block hits the block FACES accurately instead of
                // tunnelling through and catching on "air".
                rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
                rb.useGravity = true;
                rb.constraints = RigidbodyConstraints.None;
                // Gentle: it lands on the top block's edge, then its weight hanging off the edge + gravity
                // tip it off naturally and it tumbles down the side.
                float dir = offsetX >= 0f ? 1f : -1f;
                rb.linearVelocity = new Vector3(dir * 1.0f, -1.5f, 0f);
                rb.angularVelocity = new Vector3(0f, 0f, -dir * 1.0f);
            }
            Destroy(block.gameObject, 4f);
        }

        static bool TapPressed()
        {
            Pointer p = Pointer.current;
            return p != null && p.press.wasPressedThisFrame;
        }
    }
}
