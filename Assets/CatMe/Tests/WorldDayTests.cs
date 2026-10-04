using System;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;

namespace CatMe.Tests
{
    /// <summary>Launch task L6: a new day starts in the morning, and the unlit water darkens after sunset.</summary>
    public sealed class WorldDayTests
    {
        static Assembly App => AppDomain.CurrentDomain.GetAssemblies().First(a => a.GetName().Name == "Assembly-CSharp");
        static Type Day => App.GetType("CatMe.World.DayDirector", true);
        static Color Tint(float t) => (Color)Day.GetMethod("WaterTint").Invoke(null, new object[] { t });

        [Test]
        public void NewDayStartsInTheMorning()
        {
            Assert.That((float)Day.GetField("NewDayStart").GetValue(null), Is.EqualTo(.12f).Within(.001f));
        }

        [Test]
        public void WaterIsFullColourByDayAndDarkAtNight()
        {
            Assert.That(Tint(.3f), Is.EqualTo(Color.white));
            Color night = Tint(.85f);
            Assert.That(night.r, Is.LessThan(.4f));
            Assert.That(night.b, Is.GreaterThan(night.r), "night water stays blue, not grey");
            Assert.That(Tint(.6f).r, Is.LessThan(1f).And.GreaterThan(night.r), "it dims through sunset");
            Assert.That(Tint(0f), Is.EqualTo(Tint(.99f)), "no jump at dawn");
            Assert.That(Tint(.5f).a, Is.EqualTo(1f), "alpha 1 tells the shader the tint is set");
        }
    }
}
