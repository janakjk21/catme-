using System;
using System.Linq;
using System.Reflection;
using NUnit.Framework;

namespace CatMe.Tests
{
    /// <summary>F11: pinching in at the normal minimum steps to a 0.9 m close-up; pinching out returns; the look target rises.</summary>
    public sealed class CloseUpTests
    {
        static Type M => AppDomain.CurrentDomain.GetAssemblies().First(a => a.GetName().Name == "Assembly-CSharp").GetType("CatMe.Garden.CameraMath", true);

        static (float zoom, bool closeUp) Step(float zoom, bool closeUp, float a, float b, float min = 1.8f, float max = 6f)
        {
            var args = new object[] { zoom, closeUp, a, b, min, max, false };
            float z = (float)M.GetMethod("PinchStep").Invoke(null, args);
            return (z, (bool)args[6]);
        }

        [Test]
        public void PinchingInAtTheMinimumStepsToCloseUp()
        {
            var first = Step(3f, false, 100f, 120f);
            Assert.That(first.zoom, Is.EqualTo(2.5f).Within(1e-4f), "normal zoom first");
            Assert.That(first.closeUp, Is.False);
            var r = Step(1.8f, false, 100f, 120f);
            Assert.That(r.closeUp, Is.True);
            Assert.That(r.zoom, Is.EqualTo(.9f).Within(1e-4f));
            Assert.That(Step(2.0f, false, 100f, 140f).closeUp, Is.False, "a big pinch from further out stops at the minimum first");
        }

        [Test]
        public void PinchingOutLeavesCloseUp()
        {
            Assert.That(Step(.9f, true, 100f, 95f).closeUp, Is.True, "a small wobble stays");
            var r = Step(.9f, true, 120f, 100f);
            Assert.That(r.closeUp, Is.False);
            Assert.That(r.zoom, Is.EqualTo(1.8f).Within(1e-4f), "back to the normal range");
        }

        [Test]
        public void LookTargetRisesToTheHeadOnlyInCloseUp()
        {
            var blend = M.GetMethod("CloseUpBlend");
            Assert.That(blend.Invoke(null, new object[] { 1.8f, 1.8f }), Is.EqualTo(0f));
            Assert.That(blend.Invoke(null, new object[] { .9f, 1.8f }), Is.EqualTo(1f));
            Assert.That((float)blend.Invoke(null, new object[] { 1.35f, 1.8f }), Is.EqualTo(.5f).Within(1e-4f));
        }
    }
}
