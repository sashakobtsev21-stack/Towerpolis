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
        public readonly int DistrictRewardCoins;  // the City-Bonused district-completion coins (0 unless completed now)

        public RunEndOutcome(bool deposited, int coinsEarned, int gemsEarned, bool districtCompletedNow,
            bool newLeaderboardBest, int districtPopulation, int streakMilestoneCoins, int districtRewardCoins)
        {
            Deposited = deposited;
            CoinsEarned = coinsEarned;
            GemsEarned = gemsEarned;
            DistrictCompletedNow = districtCompletedNow;
            NewLeaderboardBest = newLeaderboardBest;
            DistrictPopulation = districtPopulation;
            StreakMilestoneCoins = streakMilestoneCoins;
            DistrictRewardCoins = districtRewardCoins;
        }
    }

    /// <summary>What the weekly-mission + achievement pass produced at run-end (progression-spec §4): the ids
    /// that newly completed/unlocked this run and the coins they paid (already added to the wallet).</summary>
    public readonly struct RunSystemsOutcome
    {
        public readonly IReadOnlyList<string> CompletedMissions;
        public readonly IReadOnlyList<string> UnlockedAchievements;
        public readonly int BonusCoins;

        public RunSystemsOutcome(IReadOnlyList<string> missions, IReadOnlyList<string> achievements, int bonusCoins)
        {
            CompletedMissions = missions;
            UnlockedAchievements = achievements;
            BonusCoins = bonusCoins;
        }
    }

    /// <summary>The purchasable upgrade tracks (progression-spec §2).</summary>
    public enum UpgradeKind { Magnet, CityBonus }

    /// <summary>
    /// The aggregate, save-able player meta-state (ADR-0007): per-district city grids, currency, daily
    /// streak, local leaderboard, which districts have paid out, and the Phase-4 progression (upgrades,
    /// cosmetics, streak freeze, login calendar). Owns the deterministic run-end flow (deposit → coins →
    /// daily bonus/streak → fill-goal reward with City Bonus) and all coin spends. Unity-free and
    /// NUnit-tested; the Unity <c>SaveManager</c> only does file I/O around it.
    /// </summary>
    public sealed class CityState
    {
        readonly CoreConfig _cfg;
        readonly Dictionary<string, CityGrid> _grids = new();
        readonly HashSet<string> _rewarded = new();

        public int Coins { get; private set; }
        public int Gems { get; private set; }
        public DailyStreakState Streak { get; private set; } = DailyStreakState.Empty;
        /// <summary>UTC yyyy-MM-dd of a daily run STARTED but not yet completed; "" when none in flight.</summary>
        public string LastDailyAttemptDate { get; private set; } = "";
        public LocalLeaderboard Leaderboard { get; private set; } = new();
        public string ActiveDistrictId { get; set; } = "downtown";

        // --- Prestige / endless loop (endless-spec §2) ---
        public int TotalPrestigeStars { get; private set; }    // cumulative, never resets
        public int PrestigeCount { get; private set; }         // how many times prestiged
        public int LifetimeBestPopulation { get; private set; } // peak total population across any cycle (trophy)
        /// <summary>Permanent residents multiplier from accumulated Stars (1.0 = none).</summary>
        public float PrestigeBonusMult => 1.0f + TotalPrestigeStars * _cfg.PrestigeStarBonusPerStar;

        // --- Phase 4 progression (progression-spec §2/§3) ---
        public UpgradeState Upgrades { get; private set; } = UpgradeState.Default;
        public int FreezeCharges => Streak.FreezeCharges;

        public LoginCalendarState Login { get; private set; } = LoginCalendarState.Empty;

        // --- Weekly missions, achievements & lifetime stats (progression-spec §4) ---
        public int LifetimePerfects { get; private set; }
        public int BestFloorCount { get; private set; }
        public string ActiveWeekKey { get; private set; } = "";
        readonly List<string> _activeMissionIds = new();
        readonly Dictionary<string, int> _missionProgress = new();
        readonly HashSet<string> _completedMissionIds = new();
        readonly HashSet<string> _completedAchievements = new();
        public IReadOnlyList<string> ActiveMissionIds => _activeMissionIds;
        public IReadOnlyDictionary<string, int> MissionProgress => _missionProgress;
        public IReadOnlyCollection<string> CompletedMissionIds => _completedMissionIds;
        public IReadOnlyCollection<string> CompletedAchievementIds => _completedAchievements;

        /// <summary>Lifetime count of deposited towers across every district (achievement stat).</summary>
        public int TotalTowers
        {
            get { int n = 0; foreach (CityGrid g in _grids.Values) n += g.OccupiedCount; return n; }
        }

        public bool IsMissionCompleted(string id) => _completedMissionIds.Contains(id);
        public bool IsAchievementUnlocked(string id) => _completedAchievements.Contains(id);

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

        // --- Phase 4 purchases & claims (all spend Coins; mutate only on success) ---

        /// <summary>Buy the next level of an upgrade track. Returns false if maxed or unaffordable.</summary>
        public bool TryBuyUpgrade(UpgradeKind kind)
        {
            int level;
            int[] costs;
            switch (kind)
            {
                case UpgradeKind.Magnet:    level = Upgrades.MagnetLevel;    costs = _cfg.MagnetUpgradeCosts;    break;
                case UpgradeKind.CityBonus: level = Upgrades.CityBonusLevel; costs = _cfg.CityBonusUpgradeCosts; break;
                default: return false;
            }
            (bool ok, int newCoins, int newLevel) = UpgradeService.TryPurchase(kind.ToString(), level, costs, costs.Length, Coins);
            if (!ok) return false;
            Coins = newCoins;
            Upgrades = kind switch
            {
                UpgradeKind.Magnet => Upgrades.WithMagnet(newLevel),
                _ => Upgrades.WithCityBonus(newLevel),
            };
            return true;
        }

        /// <summary>Buy one streak-freeze charge (up to the cap).</summary>
        public bool TryBuyFreezeCharge()
        {
            int charges = Streak.FreezeCharges;
            if (charges >= _cfg.StreakFreezeMaxCharges || Coins < _cfg.StreakFreezeCost) return false;
            Coins -= _cfg.StreakFreezeCost;
            Streak = Streak.WithFreezeCharges(charges + 1);
            return true;
        }

        public bool CanClaimLogin(string todayKey) => LoginCalendar.CanClaim(Login, todayKey);

        /// <summary>Claim today's login-calendar slot, banking its coins/freeze. Returns the reward (Coins=0
        /// if already claimed today).</summary>
        public LoginCalendarReward ClaimLogin(string todayKey)
        {
            (LoginCalendarState next, LoginCalendarReward reward) = LoginCalendar.Claim(Login, todayKey, _cfg);
            Login = next;
            Coins += reward.Coins;
            if (reward.FreezeCharges > 0)
            {
                int c = Math.Min(_cfg.StreakFreezeMaxCharges, Streak.FreezeCharges + reward.FreezeCharges);
                Streak = Streak.WithFreezeCharges(c);
            }
            return reward;
        }

        /// <summary>Record that a daily run has been started for <paramref name="dateKey"/> (UTC yyyy-MM-dd).
        /// Call at the moment the daily seed is launched; resolved by <see cref="EndDailyRun"/> or
        /// <see cref="ResolveAbandonedDaily"/> on the next cold start.</summary>
        public void BeginDailyAttempt(string dateKey) => LastDailyAttemptDate = dateKey ?? "";

        /// <summary>Attempt to resolve an in-flight daily that was abandoned by an app-quit.
        /// Returns <see cref="AbandonedDailyOutcome.None"/> when there is nothing to resolve:
        /// no attempt in flight, the in-flight key doesn't match <paramref name="todayKey"/>, or
        /// today was already completed normally. Otherwise applies the configured
        /// <see cref="CoreConfig.DailyQuitPolicy"/> and returns the outcome.</summary>
        public AbandonedDailyOutcome ResolveAbandonedDaily(string todayKey)
        {
            if (string.IsNullOrEmpty(todayKey)
                || LastDailyAttemptDate != todayKey
                || DailyStreak.HasPlayed(Streak, todayKey))
                return AbandonedDailyOutcome.None;

            LastDailyAttemptDate = "";
            if (_cfg.DailyQuitPolicy == DailyQuitPolicy.VoidAttempt)
                return new AbandonedDailyOutcome(resolved: true, consumed: false, streakAdvanced: false);

            // CountAsFailed: advance the streak as a failed run (no deposit, no coins)
            Streak = DailyStreak.Record(Streak, todayKey, Streak.FreezeCharges).next;
            return new AbandonedDailyOutcome(resolved: true, consumed: true, streakAdvanced: true);
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
            if (daily) LastDailyAttemptDate = ""; // a completed daily run resolves any in-flight attempt

            int gemsEarned = 0;
            int districtRewardCoins = 0;
            bool completedNow = false;
            if (!_rewarded.Contains(d.Id) && DistrictGoal.IsReached(grid.Population, d.FillGoal))
            {
                _rewarded.Add(d.Id);
                districtRewardCoins = CoinEarnCalculator.CityBonusedReward(d.RewardCoins, Upgrades.CityBonusLevel, _cfg); // City Bonus upgrade
                coins += districtRewardCoins;
                gemsEarned = d.RewardGems;
                completedNow = true;
            }

            Coins += coins;
            Gems += gemsEarned;
            LifetimePerfects += r.PerfectDrops;
            if (r.FloorCount > BestFloorCount) BestFloorCount = r.FloorCount;
            return new RunEndOutcome(deposited, coins, gemsEarned, completedNow, newBest, grid.Population, milestoneCoins, districtRewardCoins);
        }

        // --- Prestige (endless-spec §2) ---

        /// <summary>True once every district in <paramref name="allDistricts"/> has been completed this cycle
        /// (i.e. the player can prestige). The Unity layer calls this after a district-complete.</summary>
        public bool IsPrestigeReady(IReadOnlyList<DistrictInfo> allDistricts)
        {
            if (allDistricts == null || allDistricts.Count == 0) return false;
            foreach (DistrictInfo d in allDistricts)
                if (!_rewarded.Contains(d.Id)) return false;
            return true;
        }

        /// <summary>Prestige the city: bank the current total population into permanent Stars (a residents
        /// multiplier), record the lifetime-best population, retain a fraction of coins, then wipe the grids,
        /// rewarded set, upgrades and active district so the climb restarts faster. Returns the Stars earned.
        /// Caller should verify <see cref="IsPrestigeReady"/> first. Deterministic; no clock/RNG.</summary>
        public int Prestige()
        {
            int pop = TotalPopulation;
            if (pop > LifetimeBestPopulation) LifetimeBestPopulation = pop;

            int starsPerPop = Math.Max(1, _cfg.PrestigeStarsPerPop);
            int newStars = Math.Max(1, pop / starsPerPop); // at least 1 for completing the climb
            TotalPrestigeStars += newStars;
            PrestigeCount += 1;

            Coins = (int)(Coins * _cfg.PrestigeCoinRetainFraction / 10f) * 10; // keep a fraction, floored to 10s
            _grids.Clear();
            _rewarded.Clear();
            Upgrades = UpgradeState.Default;
            ActiveDistrictId = "downtown";
            return newStars;
        }

        // --- Weekly missions & achievements (progression-spec §4) ---

        /// <summary>Ensure this week's 3 missions are drawn for <paramref name="weekKey"/>. On a new week,
        /// redraw (seeded by <paramref name="weekSeed"/>) and reset progress/completions. Safe to call any
        /// time (e.g. when the missions screen opens).</summary>
        public void EnsureWeeklyMissions(string weekKey, ulong weekSeed, IReadOnlyList<MissionInfo> allMissions)
        {
            if (allMissions == null) return;
            if (weekKey == ActiveWeekKey && _activeMissionIds.Count > 0) return; // already drawn this week

            ActiveWeekKey = weekKey;
            var ids = new List<string>(allMissions.Count);
            foreach (MissionInfo m in allMissions) ids.Add(m.MissionId);
            _activeMissionIds.Clear();
            _activeMissionIds.AddRange(MissionTracker.DrawWeeklyMissions(ids, weekSeed, 3));
            _missionProgress.Clear();
            _completedMissionIds.Clear();
        }

        /// <summary>Fold a finished run into the weekly missions + achievements: advance mission progress,
        /// bank any newly completed mission/achievement rewards (added to <see cref="Coins"/>), and report
        /// what unlocked. Call AFTER <see cref="EndEndlessRun"/>/<see cref="EndDailyRun"/> so the streak and
        /// lifetime stats are current.</summary>
        public RunSystemsOutcome ProcessRunSystems(in RunResult r, bool isDaily, string districtId,
            string weekKey, ulong weekSeed,
            IReadOnlyList<MissionInfo> allMissions, IReadOnlyList<AchievementInfo> achievements)
        {
            EnsureWeeklyMissions(weekKey, weekSeed, allMissions);

            int bonus = 0;
            var completedMissions = new List<string>();
            List<MissionInfo> active = ActiveMissionInfos(allMissions);

            var evt = new MissionEvent(r.FloorCount, r.PerfectDrops, r.TotalResidents, r.MaxPerfectChain,
                isDaily, districtId, Streak.Current);
            Dictionary<string, int> progress = MissionTracker.Record(in evt, active, _missionProgress);
            _missionProgress.Clear();
            foreach (KeyValuePair<string, int> kv in progress) _missionProgress[kv.Key] = kv.Value;

            foreach (MissionInfo m in active)
            {
                if (_completedMissionIds.Contains(m.MissionId)) continue;
                if (MissionTracker.IsComplete(m.MissionId, _missionProgress, m.Target))
                {
                    _completedMissionIds.Add(m.MissionId);
                    Coins += m.RewardCoins;
                    bonus += m.RewardCoins;
                    completedMissions.Add(m.MissionId);
                }
            }

            var unlocked = new List<string>();
            if (achievements != null)
            {
                var snap = new AchievementSnapshot(TotalTowers, TotalPopulation, LifetimePerfects,
                    Streak.Longest, BestFloorCount, _rewarded.Count);
                IReadOnlyList<string> newAch = AchievementEvaluator.Evaluate(in snap, achievements, _completedAchievements);
                foreach (string id in newAch)
                {
                    _completedAchievements.Add(id);
                    int reward = AchievementReward(achievements, id);
                    Coins += reward;
                    bonus += reward;
                    unlocked.Add(id);
                }
            }

            return new RunSystemsOutcome(completedMissions, unlocked, bonus);
        }

        List<MissionInfo> ActiveMissionInfos(IReadOnlyList<MissionInfo> allMissions)
        {
            var list = new List<MissionInfo>();
            if (allMissions == null) return list;
            foreach (string id in _activeMissionIds)
                foreach (MissionInfo m in allMissions)
                    if (m.MissionId == id) { list.Add(m); break; }
            return list;
        }

        static int AchievementReward(IReadOnlyList<AchievementInfo> defs, string id)
        {
            foreach (AchievementInfo a in defs) if (a.AchievementId == id) return a.RewardCoins;
            return 0;
        }

        /// <summary>Rebuild meta-state from a loaded save (ADR-0007). Inverse of <see cref="SaveData.From"/>.</summary>
        public static CityState FromSave(SaveData save, CoreConfig cfg)
        {
            var state = new CityState(cfg);
            if (save == null) return state;

            state.Coins = save.Coins;
            state.Gems = save.Gems;
            state.ActiveDistrictId = string.IsNullOrEmpty(save.ActiveDistrictId) ? "downtown" : save.ActiveDistrictId;
            state.Streak = new DailyStreakState(save.StreakCurrent, save.StreakLongest, save.StreakLastDate, save.StreakFreezeCharges);
            state.LastDailyAttemptDate = save.LastDailyAttemptDate ?? "";
            state.Leaderboard = new LocalLeaderboard(save.LeaderboardMap());
            state.TotalPrestigeStars = save.TotalPrestigeStars;
            state.PrestigeCount = save.PrestigeCount;
            state.LifetimeBestPopulation = save.LifetimeBestPopulation;

            // Phase 4 progression.
            state.Upgrades = new UpgradeState(save.MagnetLevel, save.CityBonusLevel);
            state.Login = new LoginCalendarState(save.LoginCalendarDay, save.LoginCalendarLastClaim);

            // Weekly missions, achievements & lifetime stats.
            state.LifetimePerfects = save.LifetimePerfects;
            state.BestFloorCount = save.BestFloorCount;
            state.ActiveWeekKey = save.ActiveWeekKey ?? "";
            if (save.ActiveMissionIds != null) state._activeMissionIds.AddRange(save.ActiveMissionIds);
            if (save.MissionProgress != null)
                foreach (IntEntry e in save.MissionProgress)
                    if (!string.IsNullOrEmpty(e.Key)) state._missionProgress[e.Key] = e.Value;
            if (save.CompletedMissionIds != null)
                foreach (string id in save.CompletedMissionIds) state._completedMissionIds.Add(id);
            if (save.CompletedAchievementIds != null)
                foreach (string id in save.CompletedAchievementIds) state._completedAchievements.Add(id);

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

    /// <summary>Result of <see cref="CityState.ResolveAbandonedDaily"/>.</summary>
    public readonly struct AbandonedDailyOutcome
    {
        /// <summary>True when there was an in-flight attempt that was resolved (consumed or voided).</summary>
        public readonly bool Resolved;
        /// <summary>True when the attempt was consumed (the daily cannot be retried today).</summary>
        public readonly bool AttemptConsumed;
        /// <summary>True when the streak was advanced as part of resolution.</summary>
        public readonly bool StreakAdvanced;

        public AbandonedDailyOutcome(bool resolved, bool consumed, bool streakAdvanced)
        {
            Resolved = resolved;
            AttemptConsumed = consumed;
            StreakAdvanced = streakAdvanced;
        }

        /// <summary>No in-flight attempt was found; nothing was changed.</summary>
        public static AbandonedDailyOutcome None => new AbandonedDailyOutcome(false, false, false);
    }
}
