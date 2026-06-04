using System;
using System.Globalization;
using UnityEngine;
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

        static string TodayKey => DateTime.UtcNow.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);

        public bool HasPlayedDailyToday()
            => _city != null && DailyStreak.HasPlayed(_city.Streak, TodayKey);

        /// <summary>Coins the in-progress run would bank (for a live HUD preview).</summary>
        public int PreviewCoins(in RunResult r) => CoinEarnCalculator.RunCoins(in r, _config);

        /// <summary>Is the district available to play? Linear unlock: downtown → neon → winter.</summary>
        public bool IsDistrictUnlocked(string id)
        {
            if (_city == null) return id == "downtown";
            return id switch
            {
                "downtown" => true,
                "neon" => _city.IsRewarded("downtown"),
                "winter" => _city.IsRewarded("neon"),
                _ => false,
            };
        }

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
            if (_city == null || _controller == null) return;

            RunResult result = _controller.BuildRunResult();
            DistrictInfo district = DistrictCatalog.Get(_city.ActiveDistrictId);
            long ticks = DateTime.UtcNow.Ticks;

            RunEndOutcome outcome = _controller.Mode == TowerGameController.RunMode.Daily
                ? _city.EndDailyRun(district, result, ticks, _controller.DailyDateKey)
                : _city.EndEndlessRun(district, result, ticks);
            SaveManager.Save(SaveData.From(_city));

            RunBanked?.Invoke(outcome);
            Debug.Log($"[Towerpolis] Run banked: +{outcome.CoinsEarned} coins (total {_city.Coins}), " +
                      $"district pop {outcome.DistrictPopulation}, city pop {_city.TotalPopulation}" +
                      (outcome.DistrictCompletedNow ? " — DISTRICT COMPLETE!" : ""));
        }
    }
}
