using UnityEngine;

namespace Towerpolis.Game.Data
{
    /// <summary>
    /// Authoring data for one city district (GDD §4.1). A new district is a new ASSET of this type
    /// — building set + resident set + skybox + palette + music + unlock + leaderboard id — and
    /// NOT new code. That is what makes seasonal districts solo-sustainable and server-gateable
    /// (Remote Config can ship a district without an app update).
    ///
    /// This is the Phase-1 reference for the data-driven ScriptableObject pattern the whole game
    /// uses: deterministic rules live in Towerpolis.Core; designers tune behaviour by editing
    /// assets like this, never by editing code.
    /// </summary>
    [CreateAssetMenu(menuName = "Towerpolis/District Definition", fileName = "District_")]
    public sealed class DistrictDefinition : ScriptableObject
    {
        [Header("Identity")]
        public string districtId = "downtown";
        [Tooltip("Localization key — never a raw user-facing string (RU+EN from Phase 5).")]
        public string displayNameKey = "district.downtown.name";

        [Header("Architecture — the floor/building style stacked in this district")]
        public GameObject[] floorVariants; // standard / balcony / premium meshes
        public GameObject baseVariant;
        public GameObject capVariant;

        [Header("Residents — parachuting characters for this district")]
        public GameObject[] residentVariants;

        [Header("Atmosphere")]
        public Material skyboxMaterial;
        public Gradient palette;
        public AudioClip musicBed;

        [Header("Progression")]
        [Tooltip("Population that fills this district and unlocks the next one (§4.1).")]
        public long populationGoal = 1000;
        public int unlockCostCoins;
        [Tooltip("Per-district leaderboard id (§4.4).")]
        public string leaderboardId = "";
    }
}
