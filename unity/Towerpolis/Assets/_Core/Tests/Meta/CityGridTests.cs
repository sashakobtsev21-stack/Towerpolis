using System;
using NUnit.Framework;
using Towerpolis.Core.Meta;

namespace Towerpolis.Core.Tests.Meta
{
    public class CityGridTests
    {
        [Test]
        public void NewGrid_IsEmpty()
        {
            var grid = new CityGrid(20);
            Assert.That(grid.Capacity, Is.EqualTo(20));
            Assert.That(grid.OccupiedCount, Is.Zero);
            Assert.That(grid.Population, Is.Zero);
            Assert.That(grid.IsFull, Is.False);
        }

        [Test]
        public void Deposit_FillsNextPlot_AndSumsPopulation()
        {
            var grid = new CityGrid(20);
            Assert.That(grid.Deposit(floorCount: 12, residents: 30, timestampUtcTicks: 100), Is.True);
            Assert.That(grid.Deposit(8, 18, 200), Is.True);

            Assert.That(grid.OccupiedCount, Is.EqualTo(2));
            Assert.That(grid.Population, Is.EqualTo(48));
            Assert.That(grid.Plots[0].Occupied, Is.True);
            Assert.That(grid.Plots[0].FloorCount, Is.EqualTo(12));
            Assert.That(grid.Plots[0].Residents, Is.EqualTo(30));
            Assert.That(grid.Plots[0].TimestampUtcTicks, Is.EqualTo(100));
            Assert.That(grid.Plots[2].Occupied, Is.False); // untouched plots read empty
        }

        [Test]
        public void DepositRunResult_Convenience()
        {
            var grid = new CityGrid(5);
            var r = new RunResult(floorCount: 9, totalResidents: 21, runScore: 1000, perfectDrops: 3);
            Assert.That(grid.Deposit(r, 500), Is.True);
            Assert.That(grid.Population, Is.EqualTo(21));
            Assert.That(grid.Plots[0].FloorCount, Is.EqualTo(9));
        }

        [Test]
        public void FullGrid_RejectsFurtherDeposits()
        {
            var grid = new CityGrid(2);
            grid.Deposit(1, 5, 0);
            grid.Deposit(1, 5, 0);
            Assert.That(grid.IsFull, Is.True);

            Assert.That(grid.Deposit(1, 99, 0), Is.False);
            Assert.That(grid.OccupiedCount, Is.EqualTo(2));
            Assert.That(grid.Population, Is.EqualTo(10)); // unchanged by the rejected deposit
        }

        [Test]
        public void NonPositiveCapacity_Throws()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => new CityGrid(0));
            Assert.Throws<ArgumentOutOfRangeException>(() => new CityGrid(-3));
        }
    }
}
