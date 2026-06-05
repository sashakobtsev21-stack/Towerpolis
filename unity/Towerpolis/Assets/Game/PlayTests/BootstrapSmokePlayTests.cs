using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using Towerpolis.Game.Gameplay;
using Towerpolis.Game.Meta;
using Towerpolis.Game.UI;

namespace Towerpolis.Game.PlayTests
{
    /// <summary>PlayMode smoke: load the real game scene and prove it boots clean — no exceptions/errors, the
    /// controller + MetaService + HUD wire up, and a fresh run starts. This is the regression net for the
    /// scene-wiring / "disappearing UI" / null-ref class of bugs that unit tests can't see.
    ///
    /// PREREQUISITE: add <c>Assets/Game/game.unity</c> to File ▸ Build Settings ▸ Scenes In Build. Without it
    /// the scene can't be loaded by name in play mode and these tests self-skip with that instruction.
    /// (Any Debug.LogError during load fails the test automatically — that's the core assertion.)</summary>
    public class BootstrapSmokePlayTests
    {
        const string SceneName = "game";

        static bool SceneAvailable => Application.CanStreamedLevelBeLoaded(SceneName);

        [UnityTest]
        public IEnumerator GameScene_BootsCleanly_AndWiresCoreObjects()
        {
            if (!SceneAvailable) { Assert.Ignore("Add Assets/Game/game.unity to Build Settings to run this smoke test."); yield break; }

            SceneManager.LoadScene(SceneName);
            yield return null; // Awake + Start (EnsureJuice, NewRun, HUD build)
            yield return null; // first run frame
            yield return null;

            Assert.That(Object.FindFirstObjectByType<TowerGameController>(), Is.Not.Null, "TowerGameController missing from scene");
            Assert.That(MetaService.Instance, Is.Not.Null, "MetaService did not bootstrap");
            Assert.That(Object.FindFirstObjectByType<HUDController>(), Is.Not.Null, "HUDController missing");
            Assert.That(Object.FindFirstObjectByType<Canvas>(), Is.Not.Null, "no UI canvas was built");
        }

        [UnityTest]
        public IEnumerator GameScene_FreshRun_IsLiveNotOver()
        {
            if (!SceneAvailable) { Assert.Ignore("Add Assets/Game/game.unity to Build Settings to run this smoke test."); yield break; }

            SceneManager.LoadScene(SceneName);
            yield return null;
            yield return null;

            var ctrl = Object.FindFirstObjectByType<TowerGameController>();
            Assert.That(ctrl, Is.Not.Null);
            Assert.That(ctrl.IsOver, Is.False, "a freshly started run must not be over");
            Assert.That(ctrl.Floors, Is.GreaterThanOrEqualTo(0), "Floors API should be queryable");
            Assert.That(ctrl.Strikes, Is.Zero, "no strikes on a fresh run");
        }
    }
}
