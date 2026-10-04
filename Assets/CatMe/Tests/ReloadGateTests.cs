using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using NUnit.Framework;

namespace CatMe.Tests
{
    /// <summary>F4: however many reloads are asked for at once, only one load runs at a time, so only one person is added.</summary>
    public sealed class ReloadGateTests
    {
        static Assembly App => AppDomain.CurrentDomain.GetAssemblies().First(a => a.GetName().Name == "Assembly-CSharp");
        static object NewGate() => Activator.CreateInstance(App.GetType("CatMe.Garden.ReloadGate", true), true);
        static bool Call(object gate, string method) => (bool)gate.GetType().GetMethod(method).Invoke(gate, null);

        // A fake loader: a "load" adds one person when it finishes, after first clearing the old ones (as LoadCharacters does).
        sealed class FakeGarden
        {
            public readonly List<string> people = new List<string>();
            public int running, maxParallel, loads;
            public void Start() { running++; maxParallel = Math.Max(maxParallel, running); people.Clear(); }
            public void End() { people.Add("WalkingHuman"); running--; loads++; }
        }

        [Test]
        public void ParallelRequestsBecomeOneFollowUp()
        {
            var gate = NewGate(); var garden = new FakeGarden();
            Assert.That(Call(gate, "Request"), Is.True);   // first caller starts
            garden.Start();
            // Four more callers while it runs (CatMeAppFlow lines ~156, ~219, ~232, ~553 and SetActivePet).
            for (int i = 0; i < 4; i++) Assert.That(Call(gate, "Request"), Is.False);
            garden.End();
            Assert.That(Call(gate, "Finished"), Is.True, "one queued follow-up runs");
            garden.Start(); garden.End();
            Assert.That(Call(gate, "Finished"), Is.False, "nothing else queued");
            Assert.That(garden.maxParallel, Is.EqualTo(1));
            Assert.That(garden.loads, Is.EqualTo(2));
            Assert.That(garden.people.Count, Is.EqualTo(1), "exactly one person");
        }

        [Test]
        public void ARequestAfterFinishingStartsAgain()
        {
            var gate = NewGate();
            Assert.That(Call(gate, "Request"), Is.True);
            Assert.That(Call(gate, "Finished"), Is.False);
            Assert.That(Call(gate, "Request"), Is.True);
        }
    }
}
