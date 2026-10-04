using System;
using System.Linq;
using System.Reflection;
using NUnit.Framework;

namespace CatMe.Tests
{
    /// <summary>F3: a dog's size sets how tall its own model is shown and how deep its bark is; a cat ignores size.</summary>
    public sealed class DogSizeTests
    {
        static Assembly App => AppDomain.CurrentDomain.GetAssemblies().First(a => a.GetName().Name == "Assembly-CSharp");
        static Type Size => App.GetType("CatMe.Pets.PetSize", true);
        static object Species(string s) => Enum.Parse(App.GetType("CatMe.Pets.Species", true), s);
        static float Height(string size) => (float)Size.GetMethod("DogHeight").Invoke(null, new object[] { size });
        static float Pitch(string species, string size) => (float)Size.GetMethod("BasePitch").Invoke(null, new object[] { Species(species), size });

        [Test]
        public void BiggerDogsAreTallerAndDeeper()
        {
            Assert.That(Height("small"), Is.LessThan(Height("medium")));
            Assert.That(Height("medium"), Is.LessThan(Height("large")));
            Assert.That(Height(null), Is.EqualTo(Height("medium")), "unknown size is medium");
            Assert.That(Pitch("Dog", "small"), Is.EqualTo(1.3f).Within(1e-4f));
            Assert.That(Pitch("Dog", "large"), Is.EqualTo(1f).Within(1e-4f));
            Assert.That(Pitch("Dog", ""), Is.EqualTo(1.12f).Within(1e-4f));
            Assert.That(Pitch("Cat", "large"), Is.EqualTo(.92f).Within(1e-4f));
        }

        [Test]
        public void GardenUsesTheDogsSize()
        {
            var pet = Activator.CreateInstance(App.GetType("CatMe.Pets.PetProfile", true));
            pet.GetType().GetField("species").SetValue(pet, 1);
            pet.GetType().GetField("size").SetValue(pet, "large");
            var scale = App.GetType("CatMe.Garden.CompanionScaleStandards", true);
            var method = scale.GetMethod("PetHeightFor", new[] { pet.GetType() });
            Assert.That((float)method.Invoke(null, new[] { pet }), Is.EqualTo(Height("large")));
        }
    }
}
