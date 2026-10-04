using System;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;

namespace CatMe.Tests
{
    /// <summary>F2: haptics default on, the switch uses CatMe.Haptics, and a switched-off setting sends nothing.</summary>
    public sealed class HapticsTests
    {
        static Type H => AppDomain.CurrentDomain.GetAssemblies().First(a => a.GetName().Name == "Assembly-CSharp").GetType("CatMe.World.Haptics", true);
        string saved;
        [SetUp] public void SetUp() { saved = PlayerPrefs.HasKey("CatMe.Haptics") ? PlayerPrefs.GetInt("CatMe.Haptics").ToString() : null; PlayerPrefs.DeleteKey("CatMe.Haptics"); }
        [TearDown] public void TearDown() { if (saved == null) PlayerPrefs.DeleteKey("CatMe.Haptics"); else PlayerPrefs.SetInt("CatMe.Haptics", int.Parse(saved)); }

        static int Count => (int)H.GetProperty("Count").GetValue(null);

        [Test]
        public void OnByDefaultAndCountsTaps()
        {
            Assert.That(H.GetProperty("Enabled").GetValue(null), Is.True);
            int before = Count;
            H.GetMethod("Tap").Invoke(null, null);
            Assert.That(Count, Is.EqualTo(before + 1));
            Assert.That(H.GetProperty("LastStyle").GetValue(null), Is.EqualTo(1), "a tap is the light style");
        }

        [Test]
        public void OffSendsNothingAndUsesTheSettingsKey()
        {
            H.GetProperty("Enabled").SetValue(null, false);
            Assert.That(PlayerPrefs.GetInt("CatMe.Haptics", 1), Is.EqualTo(0));
            int before = Count;
            H.GetMethod("Bump").Invoke(null, null);
            Assert.That(Count, Is.EqualTo(before));
        }
    }
}
