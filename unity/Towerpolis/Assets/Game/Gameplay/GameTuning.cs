using UnityEngine;
using Towerpolis.Core.Gameplay;

namespace Towerpolis.Game.Gameplay
{
    /// <summary>
    /// Unity-layer feel tunables for the MVP core loop (crane, drop, wobble, camera). The deterministic
    /// Core tunables (grading bands, scoring, lean factors) live in <see cref="CoreConfig"/>. Designers
    /// tune feel by editing this asset live in the inspector.
    /// </summary>
    [CreateAssetMenu(menuName = "Towerpolis/Game Tuning", fileName = "GameTuning")]
    public sealed class GameTuning : ScriptableObject
    {
        [Header("World")]
        [Tooltip("Floor height in world units — the stacking step and block mesh height.")]
        public float floorHeight = 1.5f;
        [Tooltip("How far above the tower top the crane holds the block (the gap before it drops).")]
        public float craneHeight = 3.0f;

        [Header("Crane swing")]
        [Tooltip("Half of the horizontal swing range (m). Bigger = swings wider, easier to miss.")]
        public float swingHalfArc = 2.5f;
        [Tooltip("Pendulum cable length — longer = flatter, more horizontal arc.")]
        public float craneCableLength = 6.0f;
        [Tooltip("How much the block tilts with the swing (0 = always upright, 1 = full pendulum tilt).")]
        public float craneTiltFactor = 0.4f;
        [Tooltip("Swing period at floor 1 (seconds per full swing). Bigger = slower crane.")]
        public float periodFloor1 = 3.0f;
        [Tooltip("Fastest the swing can ever get (minimum period).")]
        public float periodMinClamp = 2.0f;
        [Tooltip("How many seconds shorter the period gets per floor (slight speed-up with height).")]
        public float periodRampFactor = 0.012f;
        [Tooltip("How much wider the swing arc gets per floor (rope swings a bit more with height).")]
        public float swingArcPerFloor = 0.06f;
        [Tooltip("Widest the swing arc can ever get (m) — capped so it can't exceed the cable / go off-screen.")]
        public float swingArcMax = 4.2f;

        [Header("Drop")]
        [Tooltip("Gravity multiplier while falling. Bigger = faster, snappier drop.")]
        public float gravityScale = 2.5f;

        [Header("Wobble (building sway — driven by overhang/lean)")]
        [Tooltip("KEY: sway strength per unit of accumulated overhang. Bigger = shakes harder when crooked.")]
        public float wobbleLeanBias = 6.0f;
        [Tooltip("Cap on sway amplitude (degrees) so it never swings off-screen.")]
        public float wobbleAmpMax = 6.0f;
        [Tooltip("Sway period (seconds) on a low tower. Bigger = slower, weightier sway.")]
        public float wobblePeriodBase = 6.0f;
        [Tooltip("How much the sway period grows per floor (taller towers sway slower).")]
        public float wobblePeriodPerFloor = 0.04f;
        [Tooltip("Slowest possible sway (max period).")]
        public float wobblePeriodMax = 8.0f;
        [Tooltip("Fastest possible sway (min period).")]
        public float wobblePeriodMin = 4.0f;
        [Tooltip("Static lean angle (deg) toward the overhang side, that the sway oscillates around.")]
        public float maxLeanBiasAngle = 4.0f;

        [Header("Camera")]
        [Tooltip("Camera follow smoothing (bigger = lazier, smoother follow).")]
        public float cameraFollowSmoothTime = 0.4f;
        [Tooltip("Downward tilt of the camera (degrees) — the 3/4 view angle.")]
        public float cameraAngleX = 25.0f;
        [Tooltip("Farthest the camera ever pulls back.")]
        public float maxCameraDistance = 18.0f;

        /// <summary>Builds the deterministic Core config (MVP uses the Core spec defaults).</summary>
        public CoreConfig BuildCoreConfig() => new CoreConfig();

        public float SwingPeriod(int floor) =>
            Mathf.Max(periodMinClamp, periodFloor1 - periodRampFactor * (floor - 1));

        /// <summary>Swing half-arc for a floor — grows with height so the crane swings wider as you climb.</summary>
        public float SwingArc(int floor) =>
            Mathf.Min(swingArcMax, swingHalfArc + swingArcPerFloor * (floor - 1));
    }
}
