using System;
using System.Collections.Generic;
using System.Globalization;
using UnityEngine;
using Towerpolis.Core.Determinism;
using Towerpolis.Core.Gameplay;
using Towerpolis.Core.Meta;
using Towerpolis.Game.Gameplay;

namespace Towerpolis.Game.Meta
{
    /// <summary>
    /// Owns the persistent meta-state (<see cref="CityState"/>) across runs: loads the save on startup,
    /// banks each finished run into the active district (deposit + coins + leaderboard), and saves. The
    /// deterministic work is all in Core; this is the thin Unity bridge (ADR-0007). Self-bootstraps off
    /// the controller, so no scene wiring is needed. Daily-seed mode + the city view subscribe to
    /// <see cref="RunBanked"/> later.
    /// </summary>
    public sealed class MetaService : MonoBehaviour
    {
        public static MetaService Instance { get; private set; }

        readonly CoreConfig _config = new CoreConfig();
        CityState _city;
        TowerGameController _controller;

        public CityState City => _city;
        public int Coins => _city != null ? _city.Coins : 0;
        public int Gems => _city != null ? _city.Gems : 0;
        public int TotalPopulation => _city != null ? _city.TotalPopulation : 0;
        public string ActiveDistrictId => _city != null ? _city.ActiveDistrictId : "downtown";
        public int StreakCurrent => _city != null ? _city.Streak.Current : 0;

        // --- Phase 4 progression: read ---
        public int FreezeCharges => _city != null ? _city.FreezeCharges : 0;
        public int FreezeCost => _config.StreakFreezeCost;
        public int FreezeMax => _config.StreakFreezeMaxCharges;

        public int UpgradeLevel(UpgradeKind kind) => _city == null ? 0 : kind switch
        {
            UpgradeKind.Magnet => _city.Upgrades.MagnetLevel,
            _ => _city.Upgrades.CityBonusLevel,
        };

        int[] UpgradeCosts(UpgradeKind kind) => kind switch
        {
            UpgradeKind.Magnet => _config.MagnetUpgradeCosts,
            _ => _config.CityBonusUpgradeCosts,
        };

        public int UpgradeMaxLevel(UpgradeKind kind) => UpgradeCosts(kind).Length;
        public bool IsUpgradeMaxed(UpgradeKind kind) => UpgradeLevel(kind) >= UpgradeMaxLevel(kind);
        public int NextUpgradeCost(UpgradeKind kind)
        {
            int lvl = UpgradeLevel(kind);
            int[] c = UpgradeCosts(kind);
            return lvl < c.Length ? c[lvl] : 0;
        }

        // Effective gameplay effect — Endless only; the caller passes isDaily so it's suppressed in Daily.
        public float MagnetFraction(bool isDaily)
            => _city == null ? 0f : UpgradeService.GetMagnetFraction(_city.Upgrades.MagnetLevel, _config, isDaily);

        // --- Phase 4 progression: spend (persist + notify) ---
        /// <summary>Fires whenever coins/upgrades change from a purchase or claim (UI refresh).</summary>
        public event Action ProgressionChanged;

        public bool BuyUpgrade(UpgradeKind kind)
        {
            if (_city == null || !_city.TryBuyUpgrade(kind)) return false;
            Persist();
            return true;
        }

        public bool BuyFreezeCharge()
        {
            if (_city == null || !_city.TryBuyFreezeCharge()) return false;
            Persist();
            return true;
        }

        public bool CanClaimLoginToday() => _city != null && _city.CanClaimLogin(TodayKey);

        public LoginCalendarReward ClaimLoginToday()
        {
            if (_city == null) return default;
            LoginCalendarReward r = _city.ClaimLogin(TodayKey);
            Persist();
            return r;
        }

        void Persist()
        {
            SaveManager.Save(SaveData.From(_city));
            ProgressionChanged?.Invoke();
        }

        /// <summary>Wipe ALL progress to a fresh guest city (the "start over" / Заново action) and persist.</summary>
        public void ResetProgress()
        {
            _city = CityState.FromSave(null, _config); // fresh: 0 coins, downtown, no missions
            EnsureWeek();
            Persist(); // save + ProgressionChanged so the HUD repaints
        }

        static string TodayKey => DateTime.UtcNow.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);

        // ISO week key + the Monday-of-week seed, so the same 3 missions are drawn worldwide each week.
        static string WeekKey
        {
            get
            {
                DateTime d = DateTime.UtcNow;
                return ISOWeek.GetYear(d) + "-W" + ISOWeek.GetWeekOfYear(d).ToString("D2", CultureInfo.InvariantCulture);
            }
        }
        static ulong WeekSeed
        {
            get
            {
                DateTime d = DateTime.UtcNow.Date;
                DateTime monday = d.AddDays(-(((int)d.DayOfWeek + 6) % 7));
                return DailySeed.ForDateUtc(monday);
            }
        }

        /// <summary>Draw this week's missions if needed (call on load / when the missions screen opens).</summary>
        public void EnsureWeek()
        {
            if (_city != null) _city.EnsureWeeklyMissions(WeekKey, WeekSeed, MissionCatalog.Infos);
        }

        // --- missions / achievements (read, for the UI) ---
        public IReadOnlyList<string> ActiveMissionIds => _city != null ? _city.ActiveMissionIds : Array.Empty<string>();
        public int MissionProgressFor(string id) => _city != null && _city.MissionProgress.TryGetValue(id, out int p) ? p : 0;
        public bool IsMissionComplete(string id) => _city != null && _city.IsMissionCompleted(id);
        public bool IsAchievementUnlocked(string id) => _city != null && _city.IsAchievementUnlocked(id);

        /// <summary>Fires after the weekly-mission + achievement pass at run-end (newly completed/unlocked).</summary>
        public event Action<RunSystemsOutcome> SystemsResolved;

        public bool HasPlayedDailyToday()
            => _city != null && DailyStreak.HasPlayed(_city.Streak, TodayKey);

        /// <summary>Coins the in-progress run would bank (for a live HUD preview).</summary>
        public int PreviewCoins(in RunResult r) => CoinEarnCalculator.RunCoins(in r, _config);

        /// <summary>Is the district available to play? Linear gate (meta-spec §2.3): Downtown is always open;
        /// Neon unlocks once Downtown's fill goal is reached (rewarded), Winter once Neon is.</summary>
        public bool IsDistrictUnlocked(string id) => id switch
        {
            "neon"   => _city != null && _city.IsRewarded("downtown"),
            "winter" => _city != null && _city.IsRewarded("neon"),
            _        => true, // downtown (and any unknown id) is always available
        };

        /// <summary>Switch the active district (if unlocked) and persist. The next run uses its look.</summary>
        public bool SetActiveDistrict(string id)
        {
            if (_city == null || !IsDistrictUnlocked(id)) return false;
            _city.ActiveDistrictId = id;
            SaveManager.Save(SaveData.From(_city));
            return true;
        }

        /// <summary>Begin today's daily-seed run if it hasn't been played yet. Returns false if already used.</summary>
        public bool TryStartDaily()
        {
            if (_controller == null || HasPlayedDailyToday()) return false;
            _controller.StartDaily();
            return true;
        }

        /// <summary>Fires after a finished run is banked into the city — carries what changed (coins,
        /// population, district-complete) for the HUD / city view to animate.</summary>
        public event Action<RunEndOutcome> RunBanked;

        void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(this); return; }
            Instance = this;
            _city = CityState.FromSave(SaveManager.Load(), _config);
            EnsureWeek(); // make sure this week's missions are drawn
        }

        void OnEnable() => Bind();
        void Start() => Bind();

        void OnDisable()
        {
            if (_controller != null) _controller.RunToppled -= OnRunToppled;
            _controller = null;
            if (Instance == this) Instance = null;
        }

        void Bind()
        {
            if (_controller != null) return;
            _controller = FindFirstObjectByType<TowerGameController>();
            if (_controller != null) _controller.RunToppled += OnRunToppled;
        }

        void OnRunToppled()
        {
            if (_city == null || _controller == null)
            {
                Debug.LogWarning("[Towerpolis] Run NOT banked — meta not bound (city/controller null).");
                return;
            }

            RunResult result = _controller.BuildRunResult();
            DistrictInfo district = DistrictCatalog.Get(_city.ActiveDistrictId);
            long ticks = DateTime.UtcNow.Ticks;

            bool daily = _controller.Mode == TowerGameController.RunMode.Daily;
            RunEndOutcome outcome = daily
                ? _city.EndDailyRun(district, result, ticks, _controller.DailyDateKey)
                : _city.EndEndlessRun(district, result, ticks);

            // Weekly missions + achievements (after EndRun so streak/lifetime stats are current).
            RunSystemsOutcome sys = _city.ProcessRunSystems(result, daily, district.Id, WeekKey, WeekSeed,
                MissionCatalog.Infos, AchievementCatalog.Infos);

            SaveManager.Save(SaveData.From(_city));

            RunBanked?.Invoke(outcome);
            SystemsResolved?.Invoke(sys);
            Debug.Log($"[Towerpolis] Run banked: +{outcome.CoinsEarned} coins (total {_city.Coins}), " +
                      $"district pop {outcome.DistrictPopulation}, city pop {_city.TotalPopulation}" +
                      (outcome.DistrictCompletedNow ? " — DISTRICT COMPLETE!" : "") +
                      (sys.BonusCoins > 0 ? $" — +{sys.BonusCoins} mission/achievement coins" : ""));
        }
    }
}
