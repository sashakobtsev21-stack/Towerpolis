using System;
using UnityEngine;
using UnityEngine.InputSystem;
using Towerpolis.Core.Determinism;
using Towerpolis.Core.Gameplay;

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
        public bool IsOver => _state == State.Over;

        // HUD events — the HUDController subscribes to these.
        public event Action<int> ScoreChanged;   // new total score
        public event Action<int> FloorAdded;      // new floor count
        public event Action<int> StrikeAdded;     // strike number reached (1 or 2)
        public event Action<Vector3> PerfectHit;  // world position for the "PERFECT!" pop
        public event Action RunToppled;
        public event Action RunStarted;

        float _overSince;

        void Start() => NewRun();

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
            crane.BeginSwing(_pendingBlock, tower, tuning.craneHeight, tuning.swingHalfArc, period, _swingPhase, tuning.craneCableLength, tuning.craneTiltFactor);
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
                _pendingBlock.gameObject.AddComponent<SettleUpright>().Play(); // right the fall tilt
            }
            else
            {
                TumbleAway(_pendingBlock, offsetX);
            }

            Vector3 placedPos = _pendingBlock != null ? _pendingBlock.position : Vector3.zero;
            _pendingBlock = null;
            _falling = null;

            ScoreChanged?.Invoke(Score);
            if (outcome.FloorPlaced) FloorAdded?.Invoke(Floors);
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
                rb.useGravity = true;
                rb.constraints = RigidbodyConstraints.None;
                // Nudge it toward the side it missed (physics + the tower's colliders do the rest — it
                // bounces off the building and tumbles down).
                float dir = offsetX >= 0f ? 1f : -1f;
                // Keep falling (don't pause at the contact line — that pause read as a "bounce off the
                // top block"): carry strong downward speed plus a sideways fling so it sails on past.
                rb.linearVelocity = new Vector3(dir * 4.0f, -9f, 0f);
                rb.angularVelocity = new Vector3(0f, 0f, -dir * 2.0f);
            }
            Destroy(block.gameObject, 3f); // no collider — it just falls clear and is cleaned up
        }

        static bool TapPressed()
        {
            Pointer p = Pointer.current;
            return p != null && p.press.wasPressedThisFrame;
        }
    }
}
