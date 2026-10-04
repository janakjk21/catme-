using System;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;

namespace CatMe.Tests
{
    public sealed class SprintTests
    {
        static Assembly App => AppDomain.CurrentDomain.GetAssemblies().First(a => a.GetName().Name == "Assembly-CSharp");

        [Test]
        public void EdgeHoldStartsAndEaseStopsRunning()
        {
            var type = App.GetType("CatMe.Garden.EdgeSprint", true);
            var sprint = Activator.CreateInstance(type);
            var update = type.GetMethod("Update");
            var running = type.GetProperty("Running");
            update.Invoke(sprint, new object[] { .96f, .49f });
            Assert.That(running.GetValue(sprint), Is.False);
            update.Invoke(sprint, new object[] { .96f, .02f });
            Assert.That(running.GetValue(sprint), Is.True);
            update.Invoke(sprint, new object[] { .79f, .01f });
            Assert.That(running.GetValue(sprint), Is.False);
        }

        [Test]
        public void RunningDoesNotSlowForDistantPet()
        {
            var type = App.GetType("CatMe.Garden.GardenCompanionDemo", true);
            var speed = type.GetMethod("WalkSpeed", BindingFlags.Static | BindingFlags.NonPublic);
            Assert.That((float)speed.Invoke(null, new object[] { true, true }), Is.EqualTo(1.98f).Within(.001f));
            Assert.That((float)speed.Invoke(null, new object[] { true, false }), Is.EqualTo(.7f));
        }

        [Test]
        public void RunButtonStaysInsideRightSafeArea()
        {
            var type = App.GetType("CatMe.Garden.SprintButton", true);
            var position = type.GetMethod("Position");
            var screen = new Vector2(2796f, 1290f);
            var safe = new Rect(177f, 63f, 2442f, 1227f);
            var canvas = new Vector2(2340f, 1080f);
            var point = (Vector2)position.Invoke(null, new object[] { safe, screen, canvas });
            float right = safe.xMax * canvas.x / screen.x;
            Assert.That(point.x, Is.EqualTo(right - 200f).Within(.01f));
            Assert.That(point.x + 60f, Is.LessThan(right));
            Assert.That(point.y + 60f, Is.LessThan(canvas.y));
        }
    }
}
