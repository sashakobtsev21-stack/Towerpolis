using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using Towerpolis.Game.UI;

namespace Towerpolis.Game.PlayTests
{
    /// <summary>PlayMode: SafeAreaRoot insets its RectTransform to Screen.safeArea each frame. In the editor
    /// game view the safe area is the full screen (anchors 0..1); on a notched device sim it shrinks — either
    /// way the anchors must equal the safe-area fractions and the offsets must be zero.</summary>
    public class SafeAreaRootPlayTests
    {
        [UnityTest]
        public IEnumerator SafeAreaRoot_AnchorsMatchSafeAreaFractions()
        {
            var canvasGo = new GameObject("canvas", typeof(Canvas));
            var childGo = new GameObject("safe", typeof(RectTransform));
            childGo.transform.SetParent(canvasGo.transform, false);
            childGo.AddComponent<SafeAreaRoot>();

            yield return null; // let OnEnable/Update apply

            var rt = (RectTransform)childGo.transform;
            Rect sa = Screen.safeArea;
            Assert.That(rt.anchorMin.x, Is.EqualTo(sa.xMin / Screen.width).Within(0.001f));
            Assert.That(rt.anchorMin.y, Is.EqualTo(sa.yMin / Screen.height).Within(0.001f));
            Assert.That(rt.anchorMax.x, Is.EqualTo(sa.xMax / Screen.width).Within(0.001f));
            Assert.That(rt.anchorMax.y, Is.EqualTo(sa.yMax / Screen.height).Within(0.001f));
            Assert.That(rt.offsetMin, Is.EqualTo(Vector2.zero));
            Assert.That(rt.offsetMax, Is.EqualTo(Vector2.zero));

            Object.Destroy(canvasGo);
        }
    }
}
