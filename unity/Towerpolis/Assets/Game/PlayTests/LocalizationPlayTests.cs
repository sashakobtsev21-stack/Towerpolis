using NUnit.Framework;
using TMPro;
using UnityEngine;
using UnityEngine.TestTools;
using Towerpolis.Game.UI;

namespace Towerpolis.Game.PlayTests
{
    /// <summary>PlayMode: the Loc service + LocalizedLabel actually drive TMP text at runtime and follow a
    /// live language switch. Asserts against the string tables (not hard-coded literals) so it can't drift.</summary>
    public class LocalizationPlayTests
    {
        SystemLanguage _saved;

        [SetUp]
        public void SetUp()
        {
            Loc.Init();
            _saved = Loc.Language;
        }

        [TearDown]
        public void TearDown() => Loc.SetLanguage(_saved); // restore the player's language pref

        [Test]
        public void LocalizedLabel_ResolvesKey_AndFollowsLanguageSwitch()
        {
            var go = new GameObject("loc-test", typeof(RectTransform));
            var text = go.AddComponent<TextMeshProUGUI>();
            go.AddComponent<LocalizedLabel>().Bind(text, LocKeys.MetaClose);

            Loc.SetLanguage(SystemLanguage.English);
            Assert.That(text.text, Is.EqualTo(LocTables.En[LocKeys.MetaClose]));

            Loc.SetLanguage(SystemLanguage.Russian);
            Assert.That(text.text, Is.EqualTo(LocTables.Ru[LocKeys.MetaClose]));

            Object.DestroyImmediate(go); // runs OnDisable → LocalizedLabel unsubscribes before TearDown's switch
        }

        [Test]
        public void Loc_T_FillsFormatArguments()
        {
            Loc.SetLanguage(SystemLanguage.English);
            Assert.That(Loc.T(LocKeys.MetaCoins, 7), Does.Contain("7"));
        }

        [Test]
        public void Loc_MissingKey_ReturnsHashedKey()
        {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            LogAssert.Expect(LogType.Error, "[Loc] missing key: nonexistent.key.xyz");
#endif
            Assert.That(Loc.T("nonexistent.key.xyz"), Is.EqualTo("#nonexistent.key.xyz"));
        }
    }
}
