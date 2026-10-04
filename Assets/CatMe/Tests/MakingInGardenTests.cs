using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using NUnit.Framework;

namespace CatMe.Tests
{
    /// <summary>Launch task L5: the pet is named before the wait, and the garden banner and reminder read right.</summary>
    public sealed class MakingInGardenTests
    {
        static Assembly App => AppDomain.CurrentDomain.GetAssemblies().First(a => a.GetName().Name == "Assembly-CSharp");
        const BindingFlags Any = BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic;

        static List<string> Flow(bool addMode) => (List<string>)App.GetType("CatMe.Garden.OnboardingFlow", true).GetMethod("FlowFor", Any).Invoke(null, new object[] { addMode });

        [Test]
        public void NameComesLastBeforeTheGarden()
        {
            Assert.That(Flow(false), Is.EqualTo(new[] { "species", "photo", "person", "name" }));
            Assert.That(Flow(true), Is.EqualTo(new[] { "species", "photo", "name" }), "adding a pet skips the selfie but is still named first");
        }

        [Test]
        public void BannerFollowsTheServerStage()
        {
            var text = App.GetType("CatMe.Garden.CatMeAppFlow", true).GetMethod("MakingBannerText", Any);
            string Line(string name, string stage) => (string)text.Invoke(null, new object[] { name, stage });
            Assert.That(Line("Mochi", "checking"), Is.EqualTo("Mochi is being made · about 3 min"));
            Assert.That(Line("Mochi", "queued"), Is.EqualTo("Mochi is being made · waiting in line"));
            Assert.That(Line("Mochi", "generating"), Is.EqualTo("Mochi is being made · about 2 min"));
            Assert.That(Line(" Mochi ", "downloading"), Is.EqualTo("Mochi is almost here"));
            Assert.That(Line(null, null), Is.EqualTo("Your pet is being made · about 3 min"));
        }

        [Test]
        public void ReminderDoesNotPromiseThePetIsDone()
        {
            var body = (string)App.GetType("CatMe.World.LocalNotifications", true).GetMethod("ReminderBody").Invoke(null, new object[] { "Mochi" });
            Assert.That(body, Is.EqualTo("Mochi should be ready soon. Come and say hello."));
        }
    }
}
