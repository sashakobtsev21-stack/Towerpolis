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
            float holdY = tower.TopY + tuning.craneHeight;
            _pendingBlock.position = new Vector3(tower.TopX, holdY, 0f);
            float period = tuning.SwingPeriod(nextFloor);
            crane.BeginSwing(_pendingBlock, tower.TopX, holdY, tuning.swingHalfArc, period, _swingPhase);
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
                    if (TapPressed()) NewRun();
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
            float offsetX = contactX - tower.TopX;
            DropOutcome outcome = _run.PlaceBlock(_pendingType, offsetX);
            if (_falling != null) _falling.enabled = false; // its job is done either way

            if (outcome.FloorPlaced)
            {
                float newTopX = tower.TopX + offsetX * 0.5f; // overlap center (Stack model)
                spawner.SetWidth(_pendingBlock, outcome.TopWidth);
                _pendingBlock.position = new Vector3(newTopX, tower.TopY, 0f);
                tower.WeldPlaced(_pendingBlock, newTopX, outcome.TopWidth, _run.FloorCount, _run.LeanOffset);
            }
            else
            {
                TumbleAway(_pendingBlock);
            }

            _pendingBlock = null;
            _falling = null;

            if (outcome.Toppled)
            {
                _state = State.Over;
                Debug.Log("[Towerpolis] Run over — floors " + Floors + ", score " + Score);
            }
            else
            {
                _settleTimer = settleDelay;
                _state = State.Settling;
            }
        }

        static void TumbleAway(Transform block)
        {
            if (block == null) return;
            var rb = block.GetComponent<Rigidbody>();
            if (rb != null)
            {
                rb.isKinematic = false;
                rb.useGravity = true;
                rb.constraints = RigidbodyConstraints.None;
            }
            Destroy(block.gameObject, 3f);
        }

        static bool TapPressed()
        {
            Pointer p = Pointer.current;
            return p != null && p.press.wasPressedThisFrame;
        }
    }
}
