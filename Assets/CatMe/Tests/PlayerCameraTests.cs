using System;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;

namespace CatMe.Tests
{
    /// <summary>F5: the camera is the player's: pitch clamped 5 to 55 degrees, pinch zoom 1.8 to 6 m, an orbit around the player.</summary>
    public sealed class PlayerCameraTests
    {
        static Type M => AppDomain.CurrentDomain.GetAssemblies().First(a => a.GetName().Name == "Assembly-CSharp").GetType("CatMe.Garden.CameraMath", true);
        static float Pinch(float zoom, float a, float b, float min = 1.8f, float max = 6f) => (float)M.GetMethod("Pinch").Invoke(null, new object[] { zoom, a, b, min, max });

        [Test]
        public void PitchIsClamped()
        {
            var clamp = M.GetMethod("ClampPitch");
            Assert.That(clamp.Invoke(null, new object[] { -20f }), Is.EqualTo(5f));
            Assert.That(clamp.Invoke(null, new object[] { 80f }), Is.EqualTo(55f));
            Assert.That(clamp.Invoke(null, new object[] { 30f }), Is.EqualTo(30f));
        }

        [Test]
        public void SpreadingFingersZoomsInWithinRange()
        {
            Assert.That(Pinch(4f, 100f, 200f), Is.EqualTo(2f).Within(1e-4f));
            Assert.That(Pinch(4f, 200f, 100f), Is.EqualTo(6f).Within(1e-4f), "clamped at 6 m");
            Assert.That(Pinch(2f, 100f, 400f), Is.EqualTo(1.8f).Within(1e-4f), "clamped at 1.8 m");
            Assert.That(Pinch(3f, 100f, 400f, 1.3f, 2.6f), Is.EqualTo(1.3f).Within(1e-4f), "a room allows a tighter range");
            Assert.That(Pinch(3f, 0f, 100f), Is.EqualTo(3f), "no jump on the first frame");
        }

        [Test]
        public void OrbitSitsBehindAndAbove()
        {
            var pos = (Vector3)M.GetMethod("Orbit").Invoke(null, new object[] { Vector3.zero, 0f, 30f, 4f });
            Assert.That(pos.z, Is.LessThan(0f), "behind at yaw 0");
            Assert.That(pos.y, Is.GreaterThan(0f), "above at a positive pitch");
            Assert.That(pos.magnitude, Is.EqualTo(4f).Within(1e-4f));
        }
    }
}
