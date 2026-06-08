using NUnit.Framework;
using Towerpolis.Core.Gameplay;
using Towerpolis.Core.Meta;

namespace Towerpolis.Core.Tests.Meta
{
    public class DailyAbandonTests
    {
        static DistrictInfo D() => new DistrictInfo("downtown", 20, 100000, 200, 0);
        static CoreConfig CfgFailed() => new CoreConfig { DailyQuitPolicy = DailyQuitPolicy.CountAsFailed };
        static CoreConfig CfgVoid()   => new CoreConfig { DailyQuitPolicy = DailyQuitPolicy.VoidAttempt };

        // --- CountAsFailed policy ---

        [Test]
        public void CountAsFailed_ResolvedOutcome_AdvancesStreakAndConsumesAttempt()
        {
            var s = new CityState(CfgFailed());
            s.BeginDailyAttempt("2026-06-08");

            AbandonedDailyOutcome outcome = s.ResolveAbandonedDaily("2026-06-08");

            Assert.That(outcome.Resolved,         Is.True,  "Resolved");
            Assert.That(outcome.AttemptConsumed,  Is.True,  "AttemptConsumed");
            Assert.That(outcome.StreakAdvanced,   Is.True,  "StreakAdvanced");
        }

        [Test]
        public void CountAsFailed_StreakIsActuallyIncremented()
        {
            var s = new CityState(CfgFailed());
            s.BeginDailyAttempt("2026-06-08");
            s.ResolveAbandonedDaily("2026-06-08");

            Assert.That(s.Streak.Current, Is.EqualTo(1));
            Assert.That(s.Streak.LastDate, Is.EqualTo("2026-06-08"));
        }

        [Test]
        public void CountAsFailed_HasPlayedIsTrue_CannotRetry()
        {
            var s = new CityState(CfgFailed());
            s.BeginDailyAttempt("2026-06-08");
            s.ResolveAbandonedDaily("2026-06-08");

            Assert.That(DailyStreak.HasPlayed(s.Streak, "2026-06-08"), Is.True);
        }

        [Test]
        public void CountAsFailed_AttemptDateClearedAfterResolve()
        {
            var s = new CityState(CfgFailed());
            s.BeginDailyAttempt("2026-06-08");
            s.ResolveAbandonedDaily("2026-06-08");

            Assert.That(s.LastDailyAttemptDate, Is.EqualTo(""));
        }

        // --- VoidAttempt policy ---

        [Test]
        public void VoidAttempt_ResolvedOutcome_NotConsumedNotAdvanced()
        {
            var s = new CityState(CfgVoid());
            s.BeginDailyAttempt("2026-06-08");

            AbandonedDailyOutcome outcome = s.ResolveAbandonedDaily("2026-06-08");

            Assert.That(outcome.Resolved,        Is.True,  "Resolved");
            Assert.That(outcome.AttemptConsumed, Is.False, "AttemptConsumed should be false");
            Assert.That(outcome.StreakAdvanced,  Is.False, "StreakAdvanced should be false");
        }

        [Test]
        public void VoidAttempt_StreakUnchanged_CanRetry()
        {
            var s = new CityState(CfgVoid());
            s.BeginDailyAttempt("2026-06-08");
            s.ResolveAbandonedDaily("2026-06-08");

            Assert.That(s.Streak.Current, Is.EqualTo(0),  "Streak should be unchanged");
            Assert.That(DailyStreak.HasPlayed(s.Streak, "2026-06-08"), Is.False, "Should be replayable");
        }

        [Test]
        public void VoidAttempt_AttemptDateClearedAfterResolve()
        {
            var s = new CityState(CfgVoid());
            s.BeginDailyAttempt("2026-06-08");
            s.ResolveAbandonedDaily("2026-06-08");

            Assert.That(s.LastDailyAttemptDate, Is.EqualTo(""));
        }

        // --- No pending attempt ---

        [Test]
        public void NoPendingAttempt_ReturnsNone()
        {
            var s = new CityState(CfgFailed());

            AbandonedDailyOutcome outcome = s.ResolveAbandonedDaily("2026-06-08");

            Assert.That(outcome.Resolved,        Is.False);
            Assert.That(outcome.AttemptConsumed, Is.False);
            Assert.That(outcome.StreakAdvanced,  Is.False);
        }

        [Test]
        public void NoPendingAttempt_StateUnchanged()
        {
            var s = new CityState(CfgFailed());
            int streakBefore = s.Streak.Current;

            s.ResolveAbandonedDaily("2026-06-08");

            Assert.That(s.Streak.Current, Is.EqualTo(streakBefore));
            Assert.That(s.LastDailyAttemptDate, Is.EqualTo(""));
        }

        // --- Already completed today ---

        [Test]
        public void AlreadyCompletedToday_AfterEndDailyRun_ReturnsNone()
        {
            var s = new CityState(CfgFailed());
            s.BeginDailyAttempt("2026-06-08");
            // Complete the daily run normally — this clears LastDailyAttemptDate
            s.EndDailyRun(D(), new RunResult(5, 10, 200, 1), timestampUtcTicks: 1, dateKey: "2026-06-08");

            AbandonedDailyOutcome outcome = s.ResolveAbandonedDaily("2026-06-08");

            Assert.That(outcome.Resolved, Is.False, "Already completed; nothing to resolve");
        }

        [Test]
        public void AlreadyCompletedToday_StreakNotDoubleAdvanced()
        {
            var s = new CityState(CfgFailed());
            s.BeginDailyAttempt("2026-06-08");
            s.EndDailyRun(D(), new RunResult(5, 10, 200, 1), timestampUtcTicks: 1, dateKey: "2026-06-08");
            int streakAfterRun = s.Streak.Current;

            s.ResolveAbandonedDaily("2026-06-08");

            Assert.That(s.Streak.Current, Is.EqualTo(streakAfterRun));
        }

        // --- Save round-trip ---

        [Test]
        public void SaveRoundTrip_PreservesLastDailyAttemptDate()
        {
            var cfg = CfgFailed();
            var s = new CityState(cfg);
            s.BeginDailyAttempt("2026-06-08");

            SaveData save = SaveData.From(s);
            CityState loaded = CityState.FromSave(save, cfg);

            Assert.That(loaded.LastDailyAttemptDate, Is.EqualTo("2026-06-08"));
        }

        [Test]
        public void SaveRoundTrip_EmptyAttemptDate_RemainsEmpty()
        {
            var cfg = CfgFailed();
            var s = new CityState(cfg);
            // no BeginDailyAttempt called

            SaveData save = SaveData.From(s);
            CityState loaded = CityState.FromSave(save, cfg);

            Assert.That(loaded.LastDailyAttemptDate, Is.EqualTo(""));
        }

        // --- AbandonedDailyOutcome.None sentinel ---

        [Test]
        public void AbandonedDailyOutcome_None_AllFalse()
        {
            AbandonedDailyOutcome none = AbandonedDailyOutcome.None;

            Assert.That(none.Resolved,        Is.False);
            Assert.That(none.AttemptConsumed, Is.False);
            Assert.That(none.StreakAdvanced,  Is.False);
        }
    }
}
