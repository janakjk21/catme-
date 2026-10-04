using System;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;

namespace CatMe.Tests
{
    /// <summary>F10: sprint is about 1.8x, its button is smaller than the stick and sits above it inside the safe area.</summary>
    public sealed class SprintTests
    {
        static Type S => AppDomain.CurrentDomain.GetAssemblies().First(a => a.GetName().Name == "Assembly-CSharp").GetType("CatMe.Garden.SprintButton", true);

        [Test]
        public void AboutOnePointEightTimesAndSmallerThanTheStick()
        {
            Assert.That((float)S.GetField("Factor").GetValue(null), Is.EqualTo(1.8f).Within(.01f));
            Assert.That((float)S.GetField("Size").GetValue(null), Is.LessThan(220f));
        }

        [Test]
        public void SitsAboveTheStickInsideTheSafeArea()
        {
            var screen = new Vector2(2796f, 1290f); var safe = new Rect(177f, 63f, 2442f, 1227f); var canvas = new Vector2(2340f, 1080f);
            var p = (Vector2)S.GetMethod("Position").Invoke(null, new object[] { safe, screen, canvas });
            float left = 177f * canvas.x / screen.x;
            Assert.That(p.x, Is.EqualTo(left + 200f).Within(.01f), "same x as the stick's resting spot");
            float stickY = 63f * 1080f / 1290f + 1227f * 1080f / 1290f * .22f;
            Assert.That(p.y, Is.EqualTo(stickY + 230f).Within(.01f), "above the stick, clear of its 110 travel");
            Assert.That(p.y + 60f, Is.LessThan(canvas.y), "inside the screen");
        }
    }
}
