using System;
using System.Collections;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;

namespace CatMe.Tests
{
    /// <summary>Launch task L8: pet photo tips, and funnel events that are well-formed and carry no free text.</summary>
    public sealed class TelemetryAndTipsTests
    {
        static Assembly App => AppDomain.CurrentDomain.GetAssemblies().First(a => a.GetName().Name == "Assembly-CSharp");
        static Type T => App.GetType("CatMe.Onboarding.Telemetry", true);
        string savedEnabled;

        [SetUp] public void SetUp() { savedEnabled = PlayerPrefs.HasKey("CatMe.Telemetry.Enabled") ? PlayerPrefs.GetInt("CatMe.Telemetry.Enabled").ToString() : null; T.GetMethod("ClearRecentForTests").Invoke(null, null); }
        [TearDown] public void TearDown() { if (savedEnabled == null) PlayerPrefs.DeleteKey("CatMe.Telemetry.Enabled"); else PlayerPrefs.SetInt("CatMe.Telemetry.Enabled", int.Parse(savedEnabled)); }

        [Test]
        public void PetPhotoTipsAreTheThreeAgreedLines()
        {
            var tips = (string[])App.GetType("CatMe.Garden.OnboardingFlow", true).GetField("PetPhotoTips", BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.Public).GetValue(null);
            Assert.That(tips, Is.EqualTo(new[] { "Their whole body, nose to tail", "Lots of light", "Only your pet" }));
        }

        [Test]
        public void EventNamesFollowFirebaseRules()
        {
            var name = T.GetMethod("EventName");
            Assert.That(name.Invoke(null, new object[] { "Onboarding step" }), Is.EqualTo("onboarding_step"));
            Assert.That(name.Invoke(null, new object[] { "  time--to ready! " }), Is.EqualTo("time_to_ready"));
            Assert.That(name.Invoke(null, new object[] { "3d" }), Is.EqualTo("e_3d"));
            Assert.That(((string)name.Invoke(null, new object[] { new string('a', 80) })).Length, Is.EqualTo(40));
        }

        [Test]
        public void TrackRecordsShortCodes()
        {
            T.GetMethod("Track", new[] { typeof(string), typeof(string), typeof(string) }).Invoke(null, new object[] { "creation_result", "result", "not_a_cat" });
            T.GetMethod("Track", new[] { typeof(string), typeof(string), typeof(long) }).Invoke(null, new object[] { "time_to_ready", "seconds", 184L });
            var recent = ((IEnumerable)T.GetProperty("Recent").GetValue(null)).Cast<string>().ToArray();
            Assert.That(recent, Is.EqualTo(new[] { "creation_result result=not_a_cat", "time_to_ready seconds=184" }));
        }

        [Test]
        public void SwitchingOffIsRemembered()
        {
            var enabled = T.GetProperty("Enabled");
            enabled.SetValue(null, false);
            Assert.That(enabled.GetValue(null), Is.False);
            enabled.SetValue(null, true);
            Assert.That(enabled.GetValue(null), Is.True);
        }
    }
}
