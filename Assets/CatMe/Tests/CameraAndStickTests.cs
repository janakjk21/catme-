using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;

namespace CatMe.Tests
{
    /// <summary>Launch task L3: one camera zone at a time with a 1 m margin, and the stick's dead zone and easing.</summary>
    public sealed class CameraAndStickTests
    {
        static Assembly App => AppDomain.CurrentDomain.GetAssemblies().First(a => a.GetName().Name == "Assembly-CSharp");
        static Type Picker => App.GetType("CatMe.Garden.CameraZonePicker", true);
        static Type Candidate => Picker.GetNestedType("Candidate");
        static Type Stick => App.GetType("CatMe.Garden.JoystickResponse", true);

        // (priority, volume, inside, insideWithMargin, insideShrunk)
        static int Pick(int current, params (int p, float v, bool inside, bool margin, bool shrunk)[] zones)
        {
            var list = (IList)Activator.CreateInstance(typeof(List<>).MakeGenericType(Candidate));
            foreach (var z in zones)
            {
                object c = Activator.CreateInstance(Candidate);
                Candidate.GetField("priority").SetValue(c, z.p);
                Candidate.GetField("volume").SetValue(c, z.v);
                Candidate.GetField("inside").SetValue(c, z.inside);
                Candidate.GetField("insideWithMargin").SetValue(c, z.margin);
                Candidate.GetField("insideShrunk").SetValue(c, z.shrunk);
                list.Add(c);
            }
            return (int)Picker.GetMethod("Pick").Invoke(null, new object[] { list, current });
        }

        [Test]
        public void HigherPriorityWinsThenSmallerZone()
        {
            Assert.That(Pick(-1, (0, 100f, true, true, true), (2, 900f, true, true, true)), Is.EqualTo(1));
            Assert.That(Pick(-1, (0, 900f, true, true, true), (0, 100f, true, true, true)), Is.EqualTo(1));
            Assert.That(Pick(-1, (0, 100f, false, true, false)), Is.EqualTo(-1), "near a zone but not in it: none");
        }

        [Test]
        public void CurrentZoneHoldsWithinTheMargin()
        {
            // Just stepped out of zone 0 but still within 1 m: keep it.
            Assert.That(Pick(0, (0, 100f, false, true, false)), Is.EqualTo(0));
            // More than 1 m out: drop it.
            Assert.That(Pick(0, (0, 100f, false, false, false)), Is.EqualTo(-1));
            // Edge of a better zone: keep the current one until well inside the better one.
            Assert.That(Pick(0, (0, 900f, true, true, true), (1, 100f, true, true, false)), Is.EqualTo(0));
            Assert.That(Pick(0, (0, 900f, true, true, true), (1, 100f, true, true, true)), Is.EqualTo(1));
            // A worse zone never takes over while the current one holds.
            Assert.That(Pick(1, (0, 900f, true, true, true), (1, 100f, false, true, false)), Is.EqualTo(1));
        }

        static Vector2 Shape(Vector2 raw) => (Vector2)Stick.GetMethod("Shape").Invoke(null, new object[] { raw });

        [Test]
        public void StickHasADeadZoneAndEasesToFull()
        {
            Assert.That(Shape(new Vector2(.1f, 0f)), Is.EqualTo(Vector2.zero));
            Assert.That(Shape(new Vector2(0f, .15f)), Is.EqualTo(Vector2.zero));
            Assert.That(Shape(new Vector2(1f, 0f)).x, Is.EqualTo(1f).Within(1e-4f));
            Assert.That(Shape(new Vector2(3f, 0f)).magnitude, Is.EqualTo(1f).Within(1e-4f), "beyond travel is full speed, not more");
            float half = Shape(new Vector2(.575f, 0f)).x;   // halfway between dead zone and full
            Assert.That(half, Is.LessThan(.5f).And.GreaterThan(.3f), "eased: slower than linear in the middle");
            Vector2 diagonal = Shape(new Vector2(.7f, .7f));
            Assert.That(Mathf.Atan2(diagonal.y, diagonal.x), Is.EqualTo(Mathf.PI / 4f).Within(1e-4f), "direction is kept");
            Assert.That((float)Stick.GetField("Travel").GetValue(null), Is.EqualTo(110f));
        }
    }
}
