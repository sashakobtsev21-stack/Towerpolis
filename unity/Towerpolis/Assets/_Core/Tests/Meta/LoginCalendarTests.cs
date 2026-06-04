using System;
using NUnit.Framework;
using Towerpolis.Core.Gameplay;
using Towerpolis.Core.Meta;

namespace Towerpolis.Core.Tests.Meta
{
    public class LoginCalendarTests
    {
        static readonly CoreConfig Cfg = new CoreConfig();

        [Test]
        public void CanClaim_TrueOnFirstUse()
        {
            Assert.That(LoginCalendar.CanClaim(LoginCalendarState.Empty, "2026-06-04"), Is.True);
        }

        [Test]
        public void CanClaim_FalseIfAlreadyClaimedToday()
        {
            var (next, _) = LoginCalendar.Claim(LoginCalendarState.Empty, "2026-06-04", Cfg);
            Assert.That(LoginCalendar.CanClaim(next, "2026-06-04"), Is.False);
            Assert.That(LoginCalendar.CanClaim(next, "2026-06-05"), Is.True);
        }

        [Test]
        public void Claim_FirstUse_GivesDay1()
        {
            var (next, reward) = LoginCalendar.Claim(LoginCalendarState.Empty, "2026-06-04", Cfg);
            Assert.That(next.Day, Is.EqualTo(1));
            Assert.That(reward.DayNumber, Is.EqualTo(1));
            Assert.That(reward.Coins, Is.EqualTo(10)); // day 1 = 10 coins
            Assert.That(reward.FreezeCharges, Is.Zero);
        }

        [Test]
        public void Claim_Day7_Is50Coins()
        {
            var s = new LoginCalendarState(6, "2026-06-09");
            var (next, reward) = LoginCalendar.Claim(s, "2026-06-10", Cfg);
            Assert.That(next.Day, Is.EqualTo(7));
            Assert.That(reward.Coins, Is.EqualTo(50));
            Assert.That(reward.FreezeCharges, Is.Zero);
        }

        [Test]
        public void Claim_Day3_GivesFreezeCharge()
        {
            var s = new LoginCalendarState(2, "2026-06-05");
            var (next, reward) = LoginCalendar.Claim(s, "2026-06-06", Cfg);
            Assert.That(next.Day, Is.EqualTo(3));
            Assert.That(reward.Coins, Is.EqualTo(15));
            Assert.That(reward.FreezeCharges, Is.EqualTo(1));
        }

        [Test]
        public void Claim_AfterDay30_CyclesToDay1()
        {
            var s = new LoginCalendarState(30, "2026-07-03");
            var (next, reward) = LoginCalendar.Claim(s, "2026-07-04", Cfg);
            Assert.That(next.Day, Is.EqualTo(1));
            Assert.That(reward.DayNumber, Is.EqualTo(1));
        }

        [Test]
        public void Claim_SameDayTwice_SecondIsNoOp()
        {
            var (afterFirst, _) = LoginCalendar.Claim(LoginCalendarState.Empty, "2026-06-04", Cfg);
            var (afterSecond, reward) = LoginCalendar.Claim(afterFirst, "2026-06-04", Cfg);
            Assert.That(afterSecond.Day, Is.EqualTo(afterFirst.Day)); // unchanged
            Assert.That(reward.Coins, Is.Zero);
            Assert.That(reward.FreezeCharges, Is.Zero);
        }

        [Test]
        public void Claim_NullConfig_Throws()
        {
            Assert.Throws<ArgumentNullException>(
                () => LoginCalendar.Claim(LoginCalendarState.Empty, "2026-06-04", null!));
        }

        [Test]
        public void Claim_EmptyKey_Throws()
        {
            Assert.Throws<ArgumentException>(
                () => LoginCalendar.Claim(LoginCalendarState.Empty, "", Cfg));
        }
    }
}
