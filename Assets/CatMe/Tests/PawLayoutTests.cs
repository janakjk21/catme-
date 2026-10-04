using System;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;

namespace CatMe.Tests
{
    /// <summary>F6: the paw button is the joystick's size and its mirror image inside the safe area.</summary>
    public sealed class PawLayoutTests
    {
        static Assembly App => AppDomain.CurrentDomain.GetAssemblies().First(a => a.GetName().Name == "Assembly-CSharp");
        static Type Paw => App.GetType("CatMe.Garden.PawLayout", true);

        [Test]
        public void AboutTheJoysticksSize()
        {
            float size = (float)Paw.GetField("Size").GetValue(null);
            Assert.That(size, Is.InRange(180f, 240f), "the stick's base is 220");
        }

        [Test]
        public void MirrorsTheJoystickInsideTheSafeArea()
        {
            // iPhone 14 Pro Max landscape: 2796 x 1290 with the island on the left and the home bar at the bottom.
            var screen = new Vector2(2796f, 1290f); var safe = new Rect(177f, 63f, 2442f, 1227f); var canvas = new Vector2(2340f, 1080f);
            var c = (Vector2)Paw.GetMethod("Centre").Invoke(null, new object[] { safe, screen, canvas });
            float rightInset = (2796f - safe.xMax) * canvas.x / screen.x;
            Assert.That(c.x, Is.EqualTo(-(rightInset + 200f)).Within(.01f), "200 in from the safe edge, like the stick on the left");
            Assert.That(c.y, Is.EqualTo(63f * 1080f / 1290f + 1227f * 1080f / 1290f * .22f).Within(.01f), "22% up the safe area, like the stick");
        }

        [Test]
        public void FanStaysAboveAndLeftOfThePaw()
        {
            for (int i = 0; i < 4; i++)
            {
                var slot = (Vector2)Paw.GetMethod("FanSlot").Invoke(null, new object[] { i, 4 });
                Assert.That(slot.x, Is.LessThan(70f), "never far past the paw toward the screen edge");
                Assert.That(slot.y, Is.GreaterThanOrEqualTo(0f));
                Assert.That(slot.magnitude, Is.EqualTo(250f).Within(.01f));
            }
        }
    }
}
