using System;
using System.Collections;
using System.IO;
using System.Linq;
using System.Reflection;
using NUnit.Framework;

namespace CatMe.Tests
{
    /// <summary>Launch task L1: every account keeps its own local folder, and the old shared folder moves once into the first account.</summary>
    public sealed class AccountStorageTests
    {
        static Assembly App => AppDomain.CurrentDomain.GetAssemblies().First(a => a.GetName().Name == "Assembly-CSharp");
        static Type AppType(string name) => App.GetType(name, true);
        static Type Storage => AppType("CatMe.Onboarding.AccountStorage");
        static Type Roster => AppType("CatMe.Pets.PetRoster");

        string root;

        [SetUp]
        public void SetUp()
        {
            root = Path.Combine(Path.GetTempPath(), "worntag-accounts-" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(root);
            Roster.GetMethod("SetStorageRootForTests").Invoke(null, new object[] { root });
        }

        [TearDown]
        public void TearDown()
        {
            Roster.GetMethod("SetStorageRootForTests").Invoke(null, new object[] { null });
            if (Directory.Exists(root)) Directory.Delete(root, true);
        }

        static void Use(string uid) => Storage.GetMethod("UseAccount").Invoke(null, new object[] { uid });
        static string ProfileFolder => (string)Storage.GetProperty("ProfileFolder").GetValue(null);
        static object[] Pets => ((IEnumerable)Roster.GetProperty("Pets").GetValue(null)).Cast<object>().ToArray();
        static string ServerId(object pet) => (string)pet.GetType().GetField("serverId").GetValue(pet);

        [Test]
        public void EachAccountUsesItsOwnFolder()
        {
            Use("uid-A");
            Assert.That(ProfileFolder, Is.EqualTo(Path.Combine(root, "Profiles", "uid-A")));
            Assert.That((string)Storage.GetProperty("PersonModelPath").GetValue(null), Is.EqualTo(Path.Combine(root, "Profiles", "uid-A", "models", "person-walking.glb")));
            Use(null);
            Assert.That(ProfileFolder, Is.EqualTo(Path.Combine(root, "Profiles", "local-guest")));
        }

        [Test]
        public void SecondAccountSeesNoneOfTheFirstAccountsPets()
        {
            Use("uid-A");
            Roster.GetMethod("AddServerPet").Invoke(null, new object[] { "server-A", "Mochi", "cat" });
            Assert.That(Pets.Select(ServerId), Does.Contain("server-A"));
            Assert.That(File.Exists(Path.Combine(root, "Profiles", "uid-A", "pets-v1.json")), Is.True);

            Use("uid-B");
            Assert.That(Pets.Select(ServerId), Does.Not.Contain("server-A"));

            Use("uid-A");
            Assert.That(Pets.Select(ServerId), Does.Contain("server-A"), "account A still has its pet after B used the phone");
        }

        [Test]
        public void LegacyGuestFolderMovesIntoTheFirstAccountOnlyOnce()
        {
            string legacy = Path.Combine(root, "Profiles", "local-guest");
            Directory.CreateDirectory(legacy);
            File.WriteAllText(Path.Combine(legacy, "profile-v1.json"), "{}");
            Directory.CreateDirectory(Path.Combine(root, "Companion"));
            File.WriteAllText(Path.Combine(root, "Companion", "person-walking.glb"), "glTF");
            Directory.CreateDirectory(Path.Combine(root, "JourneyPhotos"));
            File.WriteAllText(Path.Combine(root, "JourneyPhotos", "1.png"), "png");

            Use("uid-A");
            string a = Path.Combine(root, "Profiles", "uid-A");
            Assert.That(File.Exists(Path.Combine(a, "profile-v1.json")), Is.True);
            Assert.That(File.Exists(Path.Combine(a, "models", "person-walking.glb")), Is.True);
            Assert.That(File.Exists(Path.Combine(a, "album", "1.png")), Is.True);
            Assert.That(Directory.Exists(legacy), Is.False);
            Assert.That(Directory.Exists(Path.Combine(root, "Companion")), Is.False);

            // Whatever lands in the shared folder later is not given to the next account.
            Directory.CreateDirectory(legacy);
            File.WriteAllText(Path.Combine(legacy, "late.json"), "{}");
            Use("uid-B");
            Assert.That(File.Exists(Path.Combine(root, "Profiles", "uid-B", "late.json")), Is.False);
        }

        [Test]
        public void AccountScreenHelpersMaskEmailAndListPets()
        {
            MethodInfo mask = AppType("CatMe.Garden.CatMeAppFlow").GetMethod("MaskEmail", BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.Public);
            Assert.That(mask.Invoke(null, new object[] { "janak@gmail.com" }), Is.EqualTo("ja…@gmail.com"));
            Assert.That(mask.Invoke(null, new object[] { "not-an-email" }), Is.Null);

            Use("uid-A");
            MethodInfo line = AppType("CatMe.Garden.CatMeAppFlow").GetMethod("SavedPetsLine", BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.Public);
            Assert.That(line.Invoke(null, null), Is.Null, "nothing on the server yet");
            Roster.GetMethod("AddServerPet").Invoke(null, new object[] { "s1", "Mochi", "cat" });
            Roster.GetMethod("AddServerPet").Invoke(null, new object[] { "s2", "Miso", "dog" });
            Assert.That(line.Invoke(null, null), Is.EqualTo("Mochi and Miso are saved to your account."));
        }
    }
}
