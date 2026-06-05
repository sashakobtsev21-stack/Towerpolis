using System;

namespace Towerpolis.Core.Gameplay
{
    /// <summary>Deterministic scoring (spec §6). All arithmetic in Core; never PhysX (ADR-0002).</summary>
    public static class Scoring
    {
        public static int BaseScore(CoreConfig cfg, FloorType type)
        {
            if (cfg is null) throw new ArgumentNullException(nameof(cfg));
            return type switch
            {
                FloorType.Standard => cfg.ScoreStandard,
                FloorType.Balcony => cfg.ScoreBalcony,
                FloorType.Premium => cfg.ScorePremium,
                _ => 0,
            };
        }

        public static float GradeMultiplier(CoreConfig cfg, Grade grade)
        {
            if (cfg is null) throw new ArgumentNullException(nameof(cfg));
            return grade switch
            {
                Grade.Perfect => cfg.MultiplierPerfect,
                Grade.Good => cfg.MultiplierGood,
                Grade.Sloppy => cfg.MultiplierSloppy,
                _ => cfg.MultiplierMiss,
            };
        }

        /// <summary>Perfect-chain bonus (spec §6.2). Good/Sloppy/Miss reset the chain (bonus 0).</summary>
        public static int ChainBonus(CoreConfig cfg, int chainLength)
        {
            if (cfg is null) throw new ArgumentNullException(nameof(cfg));
            if (chainLength <= 0) return 0;
            if (chainLength <= 2) return cfg.ChainBonus1To2;
            if (chainLength <= 5) return cfg.ChainBonus3To5;
            if (chainLength <= 10) return cfg.ChainBonus6To10;
            return cfg.ChainBonus11Plus;
        }

        public static int BaseResidents(CoreConfig cfg, FloorType type)
        {
            if (cfg is null) throw new ArgumentNullException(nameof(cfg));
            return type switch
            {
                FloorType.Standard => cfg.ResidentsStandard,
                FloorType.Balcony => cfg.ResidentsBalcony,
                FloorType.Premium => cfg.ResidentsPremium,
                _ => 0,
            };
        }

        /// <summary>Bonus residents added to EVERY placed floor while a combo is alive (Phase A, Tower-Bloxx).
        /// Indexed by the current combo level (0 = no combo). Defensive: clamps the index to the table and
        /// returns 0 for an empty/missing table, so a misconfigured CoreConfig can never crash a run.</summary>
        public static int ComboResidentBonus(CoreConfig cfg, int comboLevel)
        {
            if (cfg is null) throw new ArgumentNullException(nameof(cfg));
            int[] table = cfg.ComboResidentBonus;
            if (table is null || table.Length == 0) return 0;
            int i = comboLevel < 0 ? 0 : (comboLevel >= table.Length ? table.Length - 1 : comboLevel);
            return table[i];
        }

        /// <summary>Extra residents a Perfect drop houses, per floor type (spec §6.3).</summary>
        public static int PerfectResidentBonus(CoreConfig cfg, FloorType type)
        {
            if (cfg is null) throw new ArgumentNullException(nameof(cfg));
            return type switch
            {
                FloorType.Standard => cfg.PerfectBonusStandard,
                FloorType.Balcony => cfg.PerfectBonusBalcony,
                FloorType.Premium => cfg.PerfectBonusPremium,
                _ => 0,
            };
        }

        /// <summary>Score for one placed floor (spec §6.1). Chain bonus applies on Perfect only;
        /// <paramref name="perfectChain"/> is the chain length AFTER this drop.</summary>
        public static int FloorScore(CoreConfig cfg, FloorType type, Grade grade, int perfectChain)
        {
            if (cfg is null) throw new ArgumentNullException(nameof(cfg));
            if (grade == Grade.Miss) return 0;

            int baseScore = BaseScore(cfg, type);
            float multiplier = GradeMultiplier(cfg, grade);
            int chainBonus = grade == Grade.Perfect ? ChainBonus(cfg, perfectChain) : 0;
            return (int)(baseScore * multiplier) + chainBonus;
        }
    }
}
