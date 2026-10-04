using System;
using System.IO;
using UnityEngine;

namespace CatMe.Onboarding
{
    [Serializable] public sealed class TutorialProgress
    {
        public int version = 1;
        public string objective = "walk";
        public bool walk, call, ball, skipped, complete;
    }

    [Serializable] public sealed class LocalPlayerProfileData
    {
        public int schema = 1;
        public string ownerId = "local-guest";
        public TutorialProgress tutorial = new TutorialProgress();
        public bool legacyInventoryRecorded;
        public bool hadLocalCat, hadLocalPerson;
        public string draftStep = "cat";
    }

    // Per account (AccountStorage: Profiles/{uid}/). SwitchTo reloads it when a different account signs in.
    public sealed class LocalPlayerProfile
    {
        public LocalPlayerProfileData Data { get; private set; }
        public bool ReadOnly { get; private set; }
        public string Notice { get; private set; }
        public string Folder { get; private set; }
        string path;
        readonly string persistentRoot;
        readonly bool legacyComplete;
        public LocalPlayerProfile(string persistentRoot, bool legacyComplete) : this(persistentRoot, AccountStorage.ProfileFolder, legacyComplete) { }
        public LocalPlayerProfile(string persistentRoot, string folder, bool legacyComplete)
        {
            this.persistentRoot = persistentRoot; this.legacyComplete = legacyComplete;
            Load(folder);
        }

        /// <summary>Reads the profile in another account's folder (after sign-in or account switch).</summary>
        public void SwitchTo(string folder) { if (folder != Folder) Load(folder); }

        void Load(string folder)
        {
            Folder = folder;
            path = Path.Combine(Folder, "profile-v1.json");
            Data = null; ReadOnly = false; Notice = null;
            try
            {
                Data = Read(path) ?? Read(path + ".bak");
                if (Data != null && Data.schema != 1)
                {
                    ReadOnly = true;
                    Notice = "This save needs a different app version. Your existing save is preserved.";
                    Data = new LocalPlayerProfileData { tutorial = new TutorialProgress { complete = true, objective = "free_play" } };
                    return;
                }
                if (Data == null)
                {
                    if (File.Exists(path) || File.Exists(path + ".bak"))
                    {
                        ReadOnly = true;
                        Notice = "Your saved guide could not be read. It is preserved; you can still explore.";
                    }
                    Data = new LocalPlayerProfileData();
                    Data.tutorial.complete = legacyComplete || ReadOnly;
                    if (Data.tutorial.complete) Data.tutorial.objective = "free_play";
                }
                if (Data.tutorial == null) Data.tutorial = new TutorialProgress();
                if (Data.tutorial.version != 1 || Array.IndexOf(new[] { "walk", "call", "ball", "free_play" }, Data.tutorial.objective) < 0)
                { ReadOnly = true; Notice = "This controls guide save is unsupported and has been preserved."; Data.tutorial = new TutorialProgress { complete = true, objective = "free_play" }; }
                if (!Data.legacyInventoryRecorded)
                {
                    Data.hadLocalCat = File.Exists(Path.Combine(persistentRoot, "Companion", "cat-walking.glb"));
                    Data.hadLocalPerson = File.Exists(Path.Combine(persistentRoot, "Companion", "person-walking.glb"));
                    Data.legacyInventoryRecorded = true;
                }
                Save();
            }
            catch (IOException) { Data = Data ?? new LocalPlayerProfileData(); ReadOnly = true; Notice = "There is not enough storage to save progress. Free some space and restart."; }
            catch (UnauthorizedAccessException) { Data = Data ?? new LocalPlayerProfileData(); ReadOnly = true; Notice = "Progress storage is unavailable. Your existing files are preserved."; }
        }
        static LocalPlayerProfileData Read(string file)
        {
            if (!File.Exists(file)) return null;
            try { return JsonUtility.FromJson<LocalPlayerProfileData>(File.ReadAllText(file)); }
            catch (Exception e) when (e is IOException || e is ArgumentException) { return null; }
        }
        public bool Save()
        {
            if (ReadOnly) return false;
            try { Directory.CreateDirectory(Folder); AtomicWrite(path, JsonUtility.ToJson(Data)); return true; }
            catch (Exception e) when (e is IOException || e is UnauthorizedAccessException) { Notice = "Progress could not be saved. Free some storage and try again."; return false; }
        }
        public static void AtomicWrite(string destination, string content)
        {
            string pending = destination + ".pending";
            File.WriteAllText(pending, content);
            if (File.Exists(destination)) File.Replace(pending, destination, destination + ".bak");
            else File.Move(pending, destination);
        }
    }
}
