#nullable enable
using System;
using System.Collections.Generic;
using Towerpolis.Core.Gameplay;

namespace Towerpolis.Core.Meta
{
    /// <summary>The plain values a Unity <c>DistrictDefinition</c> SO feeds Core for a run-end (ADR-0007):
    /// stable id, grid capacity, fill goal, and the one-time completion reward.</summary>
    public readonly struct DistrictInfo
    {
        public readonly string Id;
        public readonly int GridCapacity;
        public readonly int FillGoal;
        public readonly int RewardCoins;
        public readonly int RewardGems;

        public DistrictInfo(string id, int gridCapacity, int fillGoal, int rewardCoins, int rewardGems)
        {
            Id = id;
            GridCapacity = gridCapacity;
            FillGoal = fillGoal;
            RewardCoins = rewardCoins;
            RewardGems = rewardGems;
        }
    }

    /// <summary>What happened at run-end (meta-spec §1.3) — the Unity layer animates from this.</summary>
    public readonly struct RunEndOutcome
    {
        public readonly bool Deposited;
        public readonly int CoinsEarned;          // run coins + first-win + milestone + district reward
        public readonly int GemsEarned;
        public readonly bool DistrictCompletedNow;
        public readonly bool NewLeaderboardBest;
        public readonly int DistrictPopulation;   // after this deposit
        public readonly int StreakMilestoneCoins; // 0 unless a milestone was hit this run

        public RunEndOutcome(bool deposited, int coinsEarned, int gemsEarned, bool districtCompletedNow,
            bool newLeaderboardBest, int districtPopulation, int streakMilestoneCoins)
        {
            Deposited = deposited;
            CoinsEarned = coinsEarned;
            GemsEarned = gemsEarned;
            DistrictCompletedNow = districtCompletedNow;
            NewLeaderboardBest = newLeaderboardBest;
            DistrictPopulation = districtPopulation;
            StreakMilestoneCoins = streakMilestoneCoins;
        }
    }

    /// <summary>
    /// The aggregate, save-able player meta-state (ADR-0007): per-district city grids, currency, daily
    /// streak, local leaderboard, and which districts have already paid out their completion reward. Owns
    /// the deterministic run-end flow (deposit → coins → daily bonus/streak → fill-goal reward). Unity-free
    /// and NUnit-tested; the Unity <c>SaveManager</c> only does file I/O around it.
    /// </summary>
    public sealed class CityState
    {
        readonly CoreConfig _cfg;
        readonly Dictionary<string, CityGrid> _grids = new();
        readonly HashSet<string> _rewarded = new();

        public int Coins { get; private set; }
        public int Gems { get; private set; }
        public DailyStreakState Streak { get; private set; } = DailyStreakState.Empty;
        public LocalLeaderboard Leaderboard { get; private set; } = new();
        public string ActiveDistrictId { get; set; } = "downtown";

        public CityState(CoreConfig cfg) => _cfg = cfg ?? throw new ArgumentNullException(nameof(cfg));

        public IReadOnlyDictionary<string, CityGrid> Grids => _grids;
        public IReadOnlyCollection<string> RewardedDistricts => _rewarded;
        public bool IsRewarded(string id) => _rewarded.Contains(id);

        /// <summary>Total population across every district — the headline meta-score (meta-spec §1.4).</summary>
        public int TotalPopulation
        {
            get
            {
                int p = 0;
                foreach (CityGrid g in _grids.Values) p += g.Population;
                return p;
            }
        }

        /// <summary>The grid for a district, created on first use at the district's capacity.</summary>
        public CityGrid GridFor(in DistrictInfo d)
        {
            if (_grids.TryGetValue(d.Id, out CityGrid existing)) return existing;
            var grid = new CityGrid(Math.Max(1, d.GridCapacity));
            _grids[d.Id] = grid;
            return grid;
        }

        public RunEndOutcome EndEndlessRun(in DistrictInfo d, in RunResult r, long timestampUtcTicks)
            => EndRun(in d, in r, timestampUtcTicks, daily: false, dateKey: null);

        public RunEndOutcome EndDailyRun(in DistrictInfo d, in RunResult r, long timestampUtcTicks, string dateKey)
            => EndRun(in d, in r, timestampUtcTicks, daily: true, dateKey: dateKey);

        RunEndOutcome EndRun(in DistrictInfo d, in RunResult r, long ticks, bool daily, string? dateKey)
        {
            CityGrid grid = GridFor(in d);
            int coins = CoinEarnCalculator.RunCoins(in r, _cfg);
            bool deposited = r.FloorCount > 0 && grid.Deposit(in r, ticks);

            bool newBest = daily
                ? Leaderboard.SubmitDaily(in r, dateKey ?? "")
                : Leaderboard.SubmitEndless(in r, d.Id);

            int milestoneCoins = 0;
            if (daily && !string.IsNullOrEmpty(dateKey) && !DailyStreak.HasPlayed(Streak, dateKey!))
            {
                coins += _cfg.DailySeedFirstWinCoins;              // first daily completion of the UTC day
                Streak = DailyStreak.Record(Streak, dateKey!, Streak.FreezeCharges).next; // freeze bridges a missed day
                milestoneCoins = DailyStreak.MilestoneCoins(Streak.Current, _cfg);
                coins += milestoneCoins;
            }

            int gemsEarned = 0;
            bool completedNow = false;
            if (!_rewarded.Contains(d.Id) && DistrictGoal.IsReached(grid.Population, d.FillGoal))
            {
                _rewarded.Add(d.Id);
                coins += d.RewardCoins;
                gemsEarned = d.RewardGems;
                completedNow = true;
            }

            Coins += coins;
            Gems += gemsEarned;
            return new RunEndOutcome(deposited, coins, gemsEarned, completedNow, newBest, grid.Population, milestoneCoins);
        }

        /// <summary>Rebuild meta-state from a loaded save (ADR-0007). Inverse of <see cref="SaveData.From"/>.</summary>
        public static CityState FromSave(SaveData save, CoreConfig cfg)
        {
            var state = new CityState(cfg);
            if (save == null) return state;

            state.Coins = save.Coins;
            state.Gems = save.Gems;
            state.ActiveDistrictId = string.IsNullOrEmpty(save.ActiveDistrictId) ? "downtown" : save.ActiveDistrictId;
            state.Streak = new DailyStreakState(save.StreakCurrent, save.StreakLongest, save.StreakLastDate);
            state.Leaderboard = new LocalLeaderboard(save.LeaderboardMap());

            foreach (string id in save.RewardedDistricts) state._rewarded.Add(id);

            foreach (DistrictSave ds in save.Districts)
            {
                var grid = new CityGrid(Math.Max(1, ds.Capacity));
                foreach (PlotSave p in ds.Plots) grid.Deposit(p.FloorCount, p.Residents, p.TimestampUtcTicks);
                state._grids[ds.Id] = grid;
            }
            return state;
        }
    }
}
