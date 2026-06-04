using System.Collections.Generic;

namespace Towerpolis.Core.Meta
{
    /// <summary>
    /// Forward-only save migration (ADR-0007). Each schema bump adds one step that upgrades the previous
    /// shape in place, so an old save loads on a new build without data loss. The loop applies each step in
    /// order; every step has a golden old-shape fixture test.
    /// </summary>
    public static class SaveMigration
    {
        public static SaveData Migrate(SaveData save)
        {
            if (save == null) return new SaveData();

            // A 0/unset version means a pre-versioning save → treat as v1.
            if (save.SchemaVersion <= 0) save.SchemaVersion = 1;

            while (save.SchemaVersion < SaveData.CurrentVersion)
            {
                if (save.SchemaVersion == 1) MigrateV1ToV2(save);
                save.SchemaVersion++;
            }
            return save;
        }

        // v1 predates the Phase-4 progression fields (upgrades, cosmetics, freeze, login, missions,
        // achievements). JsonUtility leaves absent fields at their SaveData defaults; this guards against
        // null/empty so the equipped cosmetics are always valid and the new collections are never null.
        static void MigrateV1ToV2(SaveData s)
        {
            if (s.OwnedBlockSkins == null || s.OwnedBlockSkins.Count == 0)
                s.OwnedBlockSkins = new List<string> { "skin_default" };
            if (string.IsNullOrEmpty(s.EquippedBlockSkin)) s.EquippedBlockSkin = "skin_default";

            if (s.OwnedCraneSkins == null || s.OwnedCraneSkins.Count == 0)
                s.OwnedCraneSkins = new List<string> { "crane_default" };
            if (string.IsNullOrEmpty(s.EquippedCraneSkin)) s.EquippedCraneSkin = "crane_default";

            if (s.ActiveMissionIds == null) s.ActiveMissionIds = new List<string>();
            if (s.MissionProgress == null) s.MissionProgress = new List<IntEntry>();
            if (s.CompletedMissionIds == null) s.CompletedMissionIds = new List<string>();
            if (s.CompletedAchievementIds == null) s.CompletedAchievementIds = new List<string>();
            if (s.LoginCalendarLastClaim == null) s.LoginCalendarLastClaim = "";
            if (s.ActiveWeekKey == null) s.ActiveWeekKey = "";
        }
    }
}
