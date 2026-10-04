using System;
using System.IO;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;

namespace CatMe.Tests
{
    /// <summary>Launch task L4: messages for failed creations and lost connections, the pending-key move, and the person model's one home.</summary>
    public sealed class ErrorsAndResumeTests
    {
        static Assembly App => AppDomain.CurrentDomain.GetAssemblies().First(a => a.GetName().Name == "Assembly-CSharp");
        static Type Client => App.GetType("CatMe.Onboarding.CreationClient", true);
        static Type Api => App.GetType("CatMe.Onboarding.MobileApi", true);
        static Type Storage => App.GetType("CatMe.Onboarding.AccountStorage", true);
        static Type Roster => App.GetType("CatMe.Pets.PetRoster", true);
        static string PendingKey => (string)Client.GetField("PendingKey").GetValue(null);
        static string LegacyKey => (string)Client.GetField("LegacyPendingKey").GetValue(null);

        string savedPending, savedLegacy, root;

        [SetUp]
        public void SetUp()
        {
            savedPending = PlayerPrefs.HasKey(PendingKey) ? PlayerPrefs.GetString(PendingKey) : null;
            savedLegacy = PlayerPrefs.HasKey(LegacyKey) ? PlayerPrefs.GetString(LegacyKey) : null;
            PlayerPrefs.DeleteKey(PendingKey); PlayerPrefs.DeleteKey(LegacyKey);
            root = Path.Combine(Path.GetTempPath(), "worntag-l4-" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(root);
            Roster.GetMethod("SetStorageRootForTests").Invoke(null, new object[] { root });
            Storage.GetMethod("UseAccount").Invoke(null, new object[] { "uid-l4" });
        }

        [TearDown]
        public void TearDown()
        {
            PlayerPrefs.DeleteKey(PendingKey); PlayerPrefs.DeleteKey(LegacyKey);
            if (savedPending != null) PlayerPrefs.SetString(PendingKey, savedPending);
            if (savedLegacy != null) PlayerPrefs.SetString(LegacyKey, savedLegacy);
            PlayerPrefs.Save();
            Roster.GetMethod("SetStorageRootForTests").Invoke(null, new object[] { null });
            if (Directory.Exists(root)) Directory.Delete(root, true);
        }

        [Test]
        public void CheckUnavailableHasItsOwnRetryLine()
        {
            var failed = Client.GetMethod("FailedMessage");
            Assert.That(failed.Invoke(null, new object[] { "CHECK_UNAVAILABLE" }), Is.EqualTo("We couldn't check your photo just now. Try again in a minute."));
            Assert.That(failed.Invoke(null, new object[] { null }), Is.EqualTo("We couldn't make your pet this time. Try again."));
        }

        [Test]
        public void OfflineMessageDoesNotClaimAPetIsSaved()
        {
            string offline = (string)Api.GetMethod("ErrorMessage", new[] { typeof(long), typeof(string) }).Invoke(null, new object[] { 0L, null });
            Assert.That(offline, Does.Contain("No internet"));
            Assert.That(offline, Does.Not.Contain("saved"));
            Assert.That(offline, Does.Not.Contain("pet"));
        }

        [Test]
        public void ServerRefusalMessagesArePassedThrough()
        {
            var message = Api.GetMethod("ErrorMessage", new[] { typeof(long), typeof(string) });
            string body = "{\"error\":{\"code\":\"QUOTA_EXHAUSTED\",\"message\":\"You've used your free pets. More are coming soon.\"}}";
            Assert.That(message.Invoke(null, new object[] { 409L, body }), Is.EqualTo("You've used your free pets. More are coming soon."));
        }

        [Test]
        public void OldPendingKeyMovesOnceAndKeepsSpecies()
        {
            PlayerPrefs.SetString(LegacyKey, "{\"photo\":\"p.jpg\",\"petKey\":\"k\",\"ownerKey\":\"o\",\"petId\":\"pet-9\",\"species\":\"dog\",\"name\":\"Biscuit\"}");
            Assert.That(Client.GetProperty("PendingPetId").GetValue(null), Is.EqualTo("pet-9"));
            Assert.That(PlayerPrefs.HasKey(LegacyKey), Is.False);
            Assert.That(Client.GetProperty("PendingSpecies").GetValue(null), Is.EqualTo("dog"));
            Assert.That(Client.GetProperty("PendingName").GetValue(null), Is.EqualTo("Biscuit"));
            Assert.That(Client.GetMethod("MigrateLegacyPending").Invoke(null, null), Is.False, "only once");

            // A current record is never overwritten by an old one.
            PlayerPrefs.SetString(LegacyKey, "{\"petId\":\"old\"}");
            Client.GetMethod("MigrateLegacyPending").Invoke(null, null);
            Assert.That(Client.GetProperty("PendingPetId").GetValue(null), Is.EqualTo("pet-9"));
        }

        [Test]
        public void OldRecordsWithoutSpeciesSayUnknown()
        {
            PlayerPrefs.SetString(PendingKey, "{\"petId\":\"pet-1\"}");
            Assert.That(Client.GetProperty("PendingSpecies").GetValue(null), Is.Null);
        }

        [Test]
        public void PersonModelHasOneHome()
        {
            string download = (string)Storage.GetProperty("PersonDownloadPath").GetValue(null);
            string home = (string)Storage.GetProperty("PersonModelPath").GetValue(null);
            var install = Storage.GetMethod("InstallPersonModel");
            Directory.CreateDirectory(Path.GetDirectoryName(download));

            File.WriteAllText(download, "stand-in");
            Assert.That(install.Invoke(null, new object[] { true }), Is.False);
            Assert.That(File.Exists(download), Is.False, "a stand-in is dropped");
            Assert.That(File.Exists(home), Is.False);

            File.WriteAllText(download, "real");
            Assert.That(install.Invoke(null, new object[] { false }), Is.True);
            Assert.That(File.ReadAllText(home), Is.EqualTo("real"));
            Assert.That(File.Exists(download), Is.False, "no second copy left behind");
        }
    }
}
