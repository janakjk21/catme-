using System;
using System.Collections;
using System.IO;
using System.Linq;
using System.Reflection;
using NUnit.Framework;

namespace CatMe.Tests
{
    /// <summary>Launch task L2: the pet name rule, and removing a pet from Your pets.</summary>
    public sealed class PetRosterRulesTests
    {
        static Assembly App => AppDomain.CurrentDomain.GetAssemblies().First(a => a.GetName().Name == "Assembly-CSharp");
        static Type Roster => App.GetType("CatMe.Pets.PetRoster", true);
        static Type Storage => App.GetType("CatMe.Onboarding.AccountStorage", true);

        string root;

        [SetUp]
        public void SetUp()
        {
            root = Path.Combine(Path.GetTempPath(), "worntag-roster-" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(root);
            Roster.GetMethod("SetStorageRootForTests").Invoke(null, new object[] { root });
            Storage.GetMethod("UseAccount").Invoke(null, new object[] { "uid-rules" });
        }

        [TearDown]
        public void TearDown()
        {
            Roster.GetMethod("SetStorageRootForTests").Invoke(null, new object[] { null });
            if (Directory.Exists(root)) Directory.Delete(root, true);
        }

        static string Clean(string name) => (string)Roster.GetMethod("CleanName").Invoke(null, new object[] { name });
        static object[] Pets => ((IEnumerable)Roster.GetProperty("Pets").GetValue(null)).Cast<object>().ToArray();
        static string Field(object pet, string name) => (string)pet.GetType().GetField(name).GetValue(pet);
        static object AddServer(string id, string name) => Roster.GetMethod("AddServerPet").Invoke(null, new object[] { id, name, "cat" });

        [Test]
        public void NamesAreTrimmedAndLimited()
        {
            Assert.That(Clean("  Mochi  "), Is.EqualTo("Mochi"));
            Assert.That(Clean("Mr   Biscuit"), Is.EqualTo("Mr Biscuit"));
            Assert.That(Clean("O'Malley-Jones"), Is.EqualTo("O'Malley-Jones"));
            Assert.That(Clean("O’Malley"), Is.EqualTo("O'Malley"));
            Assert.That(Clean("Ünal 2"), Is.EqualTo("Ünal 2"));
            Assert.That(Clean(""), Is.Null);
            Assert.That(Clean("   "), Is.Null);
            Assert.That(Clean("Mochi!"), Is.Null);
            Assert.That(Clean("<b>Mochi</b>"), Is.Null);
            Assert.That(Clean(new string('a', 20)), Is.EqualTo(new string('a', 20)));
            Assert.That(Clean(new string('a', 21)), Is.Null);
        }

        [Test]
        public void RenameRefusesBadNames()
        {
            var pet = AddServer("s1", "Mochi");
            string id = Field(pet, "id");
            var rename = Roster.GetMethod("Rename");
            Assert.That(rename.Invoke(null, new object[] { id, "  Miso " }), Is.True);
            Assert.That(Field(pet, "name"), Is.EqualTo("Miso"));
            Assert.That(rename.Invoke(null, new object[] { id, "Miso!!" }), Is.False);
            Assert.That(Field(pet, "name"), Is.EqualTo("Miso"));
        }

        [Test]
        public void RemovedServerPetStaysRemovedAndTheLastPetStays()
        {
            var a = AddServer("s1", "Mochi");
            var b = AddServer("s2", "Miso");
            var remove = Roster.GetMethod("Remove");
            var canRemove = Roster.GetMethod("CanRemove");
            Assert.That(canRemove.Invoke(null, new object[] { Field(b, "id"), "s2" }), Is.False, "a pet being made can't be removed");
            Assert.That(remove.Invoke(null, new object[] { Field(a, "id") }), Is.True);
            Assert.That(Pets.Select(p => Field(p, "serverId")), Does.Not.Contain("s1"));
            Assert.That(AddServer("s1", "Mochi"), Is.Null, "signing in again doesn't bring a removed pet back");
            Assert.That(Field(Roster.GetProperty("Active").GetValue(null), "serverId"), Is.EqualTo("s2"));
            Assert.That(remove.Invoke(null, new object[] { Field(b, "id") }), Is.False, "the list never becomes empty");

            Roster.GetMethod("Reload").Invoke(null, null);
            Assert.That(AddServer("s1", "Mochi"), Is.Null, "removal is saved");
        }
    }
}
