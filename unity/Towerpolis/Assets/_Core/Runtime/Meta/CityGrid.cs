using System;
using System.Collections.Generic;

namespace Towerpolis.Core.Meta
{
    /// <summary>One deposited tower's data in a city plot (meta-spec §1.1). Display-only; never re-scored.</summary>
    public readonly struct Plot
    {
        public readonly bool Occupied;
        public readonly int FloorCount;
        public readonly int Residents;
        public readonly long TimestampUtcTicks;

        public Plot(int floorCount, int residents, long timestampUtcTicks)
        {
            Occupied = true;
            FloorCount = floorCount;
            Residents = residents;
            TimestampUtcTicks = timestampUtcTicks;
        }
    }

    /// <summary>
    /// One district's grid of plots and its population (meta-spec §1.1–1.4). Completed runs deposit into
    /// the next empty plot (left-to-right, back-to-front); <see cref="Population"/> = sum of deposited
    /// residents = the meta-score. Pure Core state (no engine, no clock — the timestamp is passed in), so
    /// it is deterministic and save/load round-trippable (ADR-0002, ADR-0007).
    /// </summary>
    public sealed class CityGrid
    {
        readonly Plot[] _plots;

        public int Capacity => _plots.Length;
        public int OccupiedCount { get; private set; }
        public int Population { get; private set; }
        public bool IsFull => OccupiedCount >= _plots.Length;
        public IReadOnlyList<Plot> Plots => _plots;

        public CityGrid(int capacity)
        {
            if (capacity <= 0) throw new ArgumentOutOfRangeException(nameof(capacity), "capacity must be > 0");
            _plots = new Plot[capacity];
        }

        /// <summary>Deposit a finished tower into the next empty plot. Returns false if the grid is full
        /// (the tower is rejected and the population is unchanged — meta-spec §1.2).</summary>
        public bool Deposit(int floorCount, int residents, long timestampUtcTicks)
        {
            if (IsFull) return false;
            _plots[OccupiedCount] = new Plot(floorCount, residents, timestampUtcTicks);
            OccupiedCount++;
            Population += residents;
            return true;
        }

        /// <summary>Deposit a run result (convenience over <see cref="Deposit(int,int,long)"/>).</summary>
        public bool Deposit(in RunResult result, long timestampUtcTicks)
            => Deposit(result.FloorCount, result.TotalResidents, timestampUtcTicks);
    }
}
