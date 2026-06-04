namespace Towerpolis.Core.Meta
{
    /// <summary>
    /// The player's purchased crane/meta upgrade levels (progression-spec §2). Pure, immutable data;
    /// the *effect* of each level lives in <see cref="UpgradeService"/> and is suppressed in Daily Seed
    /// for the gameplay tracks (Magnet, Slow-Mo) so the shared seed stays fair across devices.
    /// </summary>
    public readonly struct UpgradeState
    {
        public readonly int MagnetLevel;    // 0–4
        public readonly int SlowMoLevel;    // 0–4
        public readonly int CityBonusLevel; // 0–3

        public UpgradeState(int magnetLevel, int slowMoLevel, int cityBonusLevel)
        {
            MagnetLevel = magnetLevel;
            SlowMoLevel = slowMoLevel;
            CityBonusLevel = cityBonusLevel;
        }

        /// <summary>A fresh player: nothing upgraded.</summary>
        public static UpgradeState Default => new UpgradeState(0, 0, 0);

        // Copy-with helpers so the caller can apply a single purchase result without rebuilding by hand.
        public UpgradeState WithMagnet(int level) => new UpgradeState(level, SlowMoLevel, CityBonusLevel);
        public UpgradeState WithSlowMo(int level) => new UpgradeState(MagnetLevel, level, CityBonusLevel);
        public UpgradeState WithCityBonus(int level) => new UpgradeState(MagnetLevel, SlowMoLevel, level);
    }
}
