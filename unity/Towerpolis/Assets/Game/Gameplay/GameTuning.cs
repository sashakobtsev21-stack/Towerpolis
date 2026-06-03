using UnityEngine;
using Towerpolis.Core.Gameplay;

namespace Towerpolis.Game.Gameplay
{
    /// <summary>
    /// Unity-layer tunables for the MVP core loop (crane, drop, wobble, camera) — spec §1, §2, §5, §8.
    /// The deterministic Core tunables live in <see cref="CoreConfig"/>; for the MVP this asset uses the
    /// Core defaults (which already encode the spec values). Designers tune feel by editing this asset.
    /// </summary>
    [CreateAssetMenu(menuName = "Towerpolis/Game Tuning", fileName = "GameTuning")]
    public sealed class GameTuning : ScriptableObject
    {
        [Header("World")]
        [Tooltip("Floor height in world units (block art is 2×2×1.5).")]
        public float floorHeight = 1.5f;
        [Tooltip("Height above the tower top at which the crane holds the pending block.")]
        public float craneHeight = 5.0f;

        [Header("Crane swing (spec §1)")]
        public float swingHalfArc = 1.4f;
        [Tooltip("Pendulum cable length — longer = gentler arc; the block hangs and swings on this.")]
        public float craneCableLength = 4.0f;
        public float periodFloor1 = 2.8f;
        public float periodMinClamp = 2.0f;
        public float periodRampFactor = 0.012f;

        [Header("Drop (spec §2)")]
        public float gravityScale = 2.5f;

        [Header("Wobble (spec §5)")]
        public float wobbleAmpBase = 0.5f;
        public float wobbleAmpPerFloor = 0.08f;
        public float wobbleAmpMax = 6.0f;
        public float wobbleAmpMin = 0.5f;
        public float wobbleLeanBias = 1.2f;
        public float wobblePeriodBase = 2.5f;   // slower, weightier sway (was 0.6 — too fast)
        public float wobblePeriodPerFloor = 0.03f;
        public float wobblePeriodMax = 5.0f;
        public float wobblePeriodMin = 1.5f;
        public float idleWobbleScale = 0.30f;
        public float dampingRate = 2.0f;
        public float dampingRateMin = 0.5f;
        public float dampingDecayPerFloor = 0.025f;
        public float maxLeanBiasAngle = 4.0f;

        [Header("Camera (spec §8)")]
        public float cameraTargetOffsetY = 4.0f;
        public float cameraFollowSmoothTime = 0.4f;
        public float cameraDistance = 10.0f;
        public float cameraDistancePerFloor = 0.08f;
        public float maxCameraDistance = 18.0f;
        public float cameraAngleX = 25.0f;

        /// <summary>Builds the deterministic Core config. MVP uses spec defaults; later this asset can
        /// mirror and override the Core tunables too.</summary>
        public CoreConfig BuildCoreConfig() => new CoreConfig();

        public float SwingPeriod(int floor) =>
            Mathf.Max(periodMinClamp, periodFloor1 - periodRampFactor * (floor - 1));
    }
}
