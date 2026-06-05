using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using NUnit.Framework;
using Towerpolis.Game.UI;

namespace Towerpolis.Game.Tests
{
    /// <summary>
    /// Guards the localization tables (ADR-0008): RU and EN must define the SAME keys, no empty values, and
    /// matching {n} placeholders per key (so string.Format never throws and word-order swaps stay safe).
    /// Runs in the Unity Test Runner (EditMode) — the Game layer references UnityEngine, so it is not part
    /// of the standalone `dotnet test` Core suite.
    /// </summary>
    public class LocCompletenessTests
    {
        static readonly Regex Placeholder = new(@"\{(\d+)\}");

        [Test]
        public void Ru_And_En_HaveTheSameKeys()
        {
            var ru = new HashSet<string>(LocTables.Ru.Keys);
            var en = new HashSet<string>(LocTables.En.Keys);
            var ruOnly = ru.Except(en).OrderBy(k => k).ToArray();
            var enOnly = en.Except(ru).OrderBy(k => k).ToArray();
            Assert.That(ruOnly, Is.Empty, "Keys only in RU: " + string.Join(", ", ruOnly));
            Assert.That(enOnly, Is.Empty, "Keys only in EN: " + string.Join(", ", enOnly));
        }

        [Test]
        public void NoEmptyValues()
        {
            foreach (var kv in LocTables.Ru)
                Assert.That(kv.Value, Is.Not.Null.And.Not.Empty, "Empty RU value for " + kv.Key);
            foreach (var kv in LocTables.En)
                Assert.That(kv.Value, Is.Not.Null.And.Not.Empty, "Empty EN value for " + kv.Key);
        }

        [Test]
        public void PlaceholderSetsMatchPerKey()
        {
            foreach (var kv in LocTables.Ru)
            {
                if (!LocTables.En.TryGetValue(kv.Key, out string en)) continue; // covered by the key-parity test
                var ruArgs = Indices(kv.Value);
                var enArgs = Indices(en);
                Assert.That(enArgs, Is.EquivalentTo(ruArgs), "Placeholder mismatch for key " + kv.Key);
            }
        }

        static HashSet<int> Indices(string s)
        {
            var set = new HashSet<int>();
            foreach (Match m in Placeholder.Matches(s)) set.Add(int.Parse(m.Groups[1].Value));
            return set;
        }
    }
}
