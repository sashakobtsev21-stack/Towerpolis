using UnityEngine;
using Towerpolis.Core.Meta;
using Towerpolis.Game.UI;

namespace Towerpolis.Game.Meta
{
    /// <summary>Unity-side display data for a district (name, grid column count, accent colour) — the
    /// city view uses this. Full palette/skybox/music land as DistrictDefinition SOs later (ADR-0007).</summary>
    public readonly struct DistrictView
    {
        public readonly string Id;
        public readonly string DisplayName;
        public readonly int GridWidth;
        public readonly Color Accent;

        public DistrictView(string id, string displayName, int gridWidth, Color accent)
        {
            Id = id;
            DisplayName = displayName;
            GridWidth = gridWidth;
            Accent = accent;
        }
    }

    /// <summary>
    /// The 3 starter districts' GAMEPLAY numbers (meta-spec §2.2) as Core <see cref="DistrictInfo"/> —
    /// grid capacity, fill goal, completion reward, linear unlock order. This is the code source of truth
    /// for the meta math; per-district visuals (palette/skybox/music) land later as DistrictDefinition
    /// ScriptableObjects (ADR-0007) without changing these numbers.
    /// </summary>
    public static class DistrictCatalog
    {
        // Order is the unlock chain: downtown → neon → winter. Fill goals + rewards are the design values
        // from meta-spec §2.2 (Downtown 1200, Neon 1600, Winter 2200).
        public static readonly DistrictInfo[] All =
        {
            new DistrictInfo("downtown", gridCapacity: 5 * 4, fillGoal: 1200, rewardCoins: 200, rewardGems: 0),
            new DistrictInfo("neon",     gridCapacity: 5 * 4, fillGoal: 1600, rewardCoins: 350, rewardGems: 1),
            new DistrictInfo("winter",   gridCapacity: 7 * 4, fillGoal: 2200, rewardCoins: 500, rewardGems: 2),
        };

        static readonly DistrictView[] Views =
        {
            new DistrictView("downtown", LocKeys.DistDowntownName, 5, new Color(0.40f, 0.74f, 1.00f)),
            new DistrictView("neon",     LocKeys.DistNeonName,     5, new Color(0.18f, 0.74f, 0.69f)),
            new DistrictView("winter",   LocKeys.DistWinterName,   7, new Color(0.90f, 0.95f, 0.99f)),
        };

        public static DistrictInfo Get(string id)
        {
            foreach (DistrictInfo d in All)
                if (d.Id == id) return d;
            return All[0]; // default to Downtown
        }

        public static DistrictView GetView(string id)
        {
            foreach (DistrictView v in Views)
                if (v.Id == id) return v;
            return Views[0];
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
