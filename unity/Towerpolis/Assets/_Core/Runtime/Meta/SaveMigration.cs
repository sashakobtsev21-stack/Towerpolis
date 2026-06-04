namespace Towerpolis.Core.Meta
{
    /// <summary>
    /// Forward-only save migration (ADR-0007). Each schema bump adds one step that upgrades the previous
    /// shape in place, so an old save loads on a new build without data loss. v1 is the current schema, so
    /// today this only normalises the version stamp; the loop is the seam future versions extend (with a
    /// golden old-shape fixture test per step).
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
                // switch (save.SchemaVersion) { case 1: MigrateV1ToV2(save); break; }
                save.SchemaVersion++;
            }
            return save;
        }
    }
}
