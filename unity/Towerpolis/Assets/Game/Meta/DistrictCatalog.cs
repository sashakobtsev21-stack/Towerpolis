using Towerpolis.Core.Meta;

namespace Towerpolis.Game.Meta
{
    /// <summary>
    /// The 3 starter districts' GAMEPLAY numbers (meta-spec §2.2) as Core <see cref="DistrictInfo"/> —
    /// grid capacity, fill goal, completion reward, linear unlock order. This is the code source of truth
    /// for the meta math; per-district visuals (palette/skybox/music) land later as DistrictDefinition
    /// ScriptableObjects (ADR-0007) without changing these numbers.
    /// </summary>
    public static class DistrictCatalog
    {
        // Order is the unlock chain: downtown → neon → winter.
        public static readonly DistrictInfo[] All =
        {
            new DistrictInfo("downtown", gridCapacity: 5 * 4, fillGoal: 1200, rewardCoins: 200, rewardGems: 0),
            new DistrictInfo("neon",     gridCapacity: 5 * 4, fillGoal: 1600, rewardCoins: 350, rewardGems: 1),
            new DistrictInfo("winter",   gridCapacity: 6 * 4, fillGoal: 2200, rewardCoins: 500, rewardGems: 2),
        };

        public static DistrictInfo Get(string id)
        {
            foreach (DistrictInfo d in All)
                if (d.Id == id) return d;
            return All[0]; // default to Downtown
        }

        /// <summary>The district unlocked AFTER the given one completes (null id if none / last).</summary>
        public static string NextId(string id)
        {
            for (int i = 0; i < All.Length - 1; i++)
                if (All[i].Id == id) return All[i + 1].Id;
            return "";
        }
    }
}
