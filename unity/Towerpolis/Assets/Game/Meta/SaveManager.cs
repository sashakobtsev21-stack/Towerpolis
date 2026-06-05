using System;
using System.IO;
using UnityEngine;
using Towerpolis.Core.Meta;

namespace Towerpolis.Game.Meta
{
    /// <summary>
    /// Writes/reads the meta <see cref="SaveData"/> to <c>persistentDataPath/save/city.json</c> with the
    /// built-in JsonUtility (ADR-0007). Atomic-ish write (temp file → <see cref="File.Replace"/>) with a
    /// rolling <c>.bak</c>; a corrupt/missing primary falls back to the backup, and a corrupt-both falls
    /// back to a fresh guest city — it never crash-loops. Loaded data is run through
    /// <see cref="SaveMigration"/> so old schema versions upgrade cleanly.
    /// </summary>
    public static class SaveManager
    {
        static string SaveDir => Path.Combine(Application.persistentDataPath, "save");
        static string MainPath => Path.Combine(SaveDir, "city.json");
        static string TmpPath => MainPath + ".tmp";
        static string BakPath => MainPath + ".bak";

        public static void Save(SaveData data)
        {
            if (data == null) return;
            try
            {
                Directory.CreateDirectory(SaveDir);
                File.WriteAllText(TmpPath, JsonUtility.ToJson(data));
                if (File.Exists(MainPath)) File.Replace(TmpPath, MainPath, BakPath); // atomic swap + backup
                else File.Move(TmpPath, MainPath);
            }
            catch (Exception e)
            {
                Debug.LogError("[Towerpolis] Save failed: " + e.Message);
            }
            CloudSave.Backend.Push(data); // sync to the cloud backend (no-op until GPGS is wired — ADR-0009)
        }

        /// <summary>Load + migrate the city save; returns a fresh default if none exists or both copies are corrupt.
        /// Local-first; cloud reconciliation (CloudSave.Backend.Pull + newest-wins by a save timestamp) lands when
        /// GPGS is wired in Phase 7 — ADR-0009.</summary>
        public static SaveData Load()
        {
            SaveData data = TryRead(MainPath);
            if (data == null) data = TryRead(BakPath);
            return SaveMigration.Migrate(data); // null → fresh default
        }

        static SaveData TryRead(string path)
        {
            try
            {
                if (!File.Exists(path)) return null;
                string json = File.ReadAllText(path);
                if (string.IsNullOrWhiteSpace(json)) return null;
                return JsonUtility.FromJson<SaveData>(json);
            }
            catch (Exception e)
            {
                Debug.LogWarning("[Towerpolis] Save read failed (" + path + "): " + e.Message);
                return null;
            }
        }
    }
}
