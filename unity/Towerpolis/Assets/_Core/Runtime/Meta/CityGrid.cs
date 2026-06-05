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

        /// <summary>Deposit a finished tower (meta-spec §1.2, "keep the best N"). While a plot is free the
        /// tower fills the next one. Once the grid is full it REPLACES the smallest-population tower if this
        /// one has more residents — so the city always shows your best buildings and <see cref="Population"/>
        /// can keep climbing toward the fill goal (no progression soft-lock). A tower no bigger than the
        /// current smallest is rejected (the city is unchanged). Returns true if it entered the grid
        /// (placed or replaced), false if rejected.</summary>
        public bool Deposit(int floorCount, int residents, long timestampUtcTicks)
        {
            if (!IsFull)
            {
                _plots[OccupiedCount] = new Plot(floorCount, residents, timestampUtcTicks);
                OccupiedCount++;
                Population += residents;
                return true;
            }

            // Full: find the smallest tower and replace it only if the newcomer beats it (keep the best N).
            int smallest = 0;
            for (int i = 1; i < _plots.Length; i++)
                if (_plots[i].Residents < _plots[smallest].Residents) smallest = i;

            if (residents <= _plots[smallest].Residents) return false; // not an improvement → keep the city
            Population += residents - _plots[smallest].Residents;
            _plots[smallest] = new Plot(floorCount, residents, timestampUtcTicks);
            return true;
        }

        /// <summary>Deposit a run result (convenience over <see cref="Deposit(int,int,long)"/>).</summary>
        public bool Deposit(in RunResult result, long timestampUtcTicks)
            => Deposit(result.FloorCount, result.TotalResidents, timestampUtcTicks);
    }
}
