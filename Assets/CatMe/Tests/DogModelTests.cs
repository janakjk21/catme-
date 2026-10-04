using System;
using System.Linq;
using System.Reflection;
using NUnit.Framework;

namespace CatMe.Tests
{
    /// <summary>Launch task L7: a dog walks with its own model when it has one; the corgi stands in otherwise.</summary>
    public sealed class DogModelTests
    {
        static Assembly App => AppDomain.CurrentDomain.GetAssemblies().First(a => a.GetName().Name == "Assembly-CSharp");
        static Type Choice => App.GetType("CatMe.Garden.PetModelChoice", true);
        static Type Species => App.GetType("CatMe.Pets.Species", true);
        static object Dog => Enum.Parse(Species, "Dog");
        static object Cat => Enum.Parse(Species, "Cat");

        static string Own(string path, bool exists) => (string)Choice.GetMethod("OwnModel").Invoke(null, new object[] { path, exists });
        static bool Corgi(object kind, string path, bool exists) => (bool)Choice.GetMethod("UsesCorgi").Invoke(null, new object[] { kind, path, exists });

        [Test]
        public void ADogWithItsOwnModelUsesIt()
        {
            Assert.That(Own("/pets/d1/model.glb", true), Is.EqualTo("/pets/d1/model.glb"));
            Assert.That(Corgi(Dog, "/pets/d1/model.glb", true), Is.False);
        }

        [Test]
        public void TheCorgiStandsInUntilTheDogsModelExists()
        {
            Assert.That(Corgi(Dog, null, false), Is.True);
            Assert.That(Corgi(Dog, "/pets/d1/model.glb", false), Is.True, "a path to a missing file is no model");
            Assert.That(Own("/pets/d1/model.glb", false), Is.Null);
        }

        [Test]
        public void CatsNeverGetTheCorgi()
        {
            Assert.That(Corgi(Cat, null, false), Is.False);
        }

        [Test]
        public void DogsAreShownALittleTallerThanCats()
        {
            var scale = App.GetType("CatMe.Garden.CompanionScaleStandards", true);
            var byKind = scale.GetMethod("PetHeightFor", new[] { Species });
            float dog = (float)byKind.Invoke(null, new[] { Dog });
            float cat = (float)byKind.Invoke(null, new[] { Cat });
            Assert.That(dog, Is.GreaterThan(cat));
        }
    }
}
