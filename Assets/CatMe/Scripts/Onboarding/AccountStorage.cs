using System;
using System.IO;
using UnityEngine;

namespace CatMe.Onboarding
{
    /// <summary>
    /// Where the signed-in account's files live on this phone: <c>Profiles/{uid}/</c> (pets, profile, store bag, drafts, downloads,
    /// the person model). Signing in as someone else never shows the previous person's pets, photos or frame.
    ///
    /// Before this existed every account shared <c>Profiles/local-guest/</c> and <c>Companion/</c>. Those legacy folders are moved,
    /// once per phone, into the first account that signs in after the update.
    /// </summary>
    public static class AccountStorage
    {
        public const string GuestFolder = "local-guest";
        const string LastUidKey = "CatMe.Account.LastUid";
        const string MarkerName = ".legacy-migrated";

        static string rootOverride;
        static string uid;
        static bool loaded;

        /// <summary>Raised after the account folder changes; in-memory caches (roster, store bag, profile) reload from the new folder.</summary>
        public static event Action Changed;

        public static string Root => string.IsNullOrEmpty(rootOverride) ? Application.persistentDataPath : rootOverride;

        /// <summary>The account whose folder is in use; "local-guest" before anyone has signed in.</summary>
        public static string CurrentUid
        {
            get
            {
                // The last account is remembered so the garden loads the right pets before Firebase finishes restoring the sign-in.
                if (!loaded) { loaded = true; if (string.IsNullOrEmpty(rootOverride)) uid = Nullable(PlayerPrefs.GetString(LastUidKey, "")); }
                return string.IsNullOrEmpty(uid) ? GuestFolder : uid;
            }
        }

        public static string ProfileFolder => FolderFor(CurrentUid);
        public static string FolderFor(string accountUid) => Path.Combine(Root, "Profiles", Safe(accountUid));
        public static string Drafts => Path.Combine(ProfileFolder, "drafts");
        public static string Downloads => Path.Combine(ProfileFolder, "downloads");
        public static string Models => Path.Combine(ProfileFolder, "models");
        public static string PersonModelPath => Path.Combine(Models, "person-walking.glb");
        public static string CatModelPath => Path.Combine(Models, "cat-walking.glb");

        /// <summary>Uses this account's folder from now on (null or empty: no account, e.g. after sign-out). Returns true if it changed.</summary>
        public static bool UseAccount(string accountUid)
        {
            string next = Nullable(accountUid);
            string before = string.IsNullOrEmpty(uid) ? null : uid;
            loaded = true;
            if (next != null) MigrateLegacyInto(next);
            if (next == before) return false;
            uid = next;
            if (string.IsNullOrEmpty(rootOverride)) { PlayerPrefs.SetString(LastUidKey, next ?? ""); PlayerPrefs.Save(); }
            Changed?.Invoke();
            return true;
        }

        /// <summary>
        /// Moves the old shared folders into this account, once per phone. Returns true when something was moved.
        /// Files already in the account win over legacy copies with the same name.
        /// </summary>
        public static bool MigrateLegacyInto(string accountUid)
        {
            if (string.IsNullOrEmpty(accountUid) || accountUid == GuestFolder) return false;
            string profiles = Path.Combine(Root, "Profiles");
            string marker = Path.Combine(profiles, MarkerName);
            if (File.Exists(marker)) return false;
            bool moved = false;
            try
            {
                Directory.CreateDirectory(profiles);
                string legacy = FolderFor(GuestFolder), target = FolderFor(accountUid);
                if (Directory.Exists(legacy))
                {
                    if (!Directory.Exists(target)) Directory.Move(legacy, target);
                    else { MergeInto(legacy, target); Directory.Delete(legacy, true); }
                    moved = true;
                }
                string companion = Path.Combine(Root, "Companion");
                if (Directory.Exists(companion))
                {
                    string models = Path.Combine(target, "models");
                    Directory.CreateDirectory(models);
                    foreach (string legacyName in new[] { "cat-walking.glb", "person-walking.glb" })
                    {
                        string from = Path.Combine(companion, legacyName), to = Path.Combine(models, legacyName);
                        if (File.Exists(from) && !File.Exists(to)) File.Move(from, to);
                    }
                    MergeInto(companion, models);
                    Directory.Delete(companion, true);
                    moved = true;
                }
                // The photo album and the walk save were shared too.
                foreach (var (legacyName, newName) in new[] { ("JourneyPhotos", "album"), ("CatMeSession", "session") })
                {
                    string from = Path.Combine(Root, legacyName);
                    if (!Directory.Exists(from)) continue;
                    MergeInto(from, Path.Combine(target, newName));
                    Directory.Delete(from, true);
                    moved = true;
                }
                File.WriteAllText(marker, accountUid);
            }
            catch (Exception e) when (e is IOException || e is UnauthorizedAccessException)
            {
                Debug.LogWarning("[WornTag][Account] Could not move the old local folder: " + e.Message);
            }
            return moved;
        }

        /// <summary>Deletes this account's local folder (account deletion).</summary>
        public static void DeleteFolder(string accountUid)
        {
            string folder = FolderFor(string.IsNullOrEmpty(accountUid) ? GuestFolder : accountUid);
            try { if (Directory.Exists(folder)) Directory.Delete(folder, true); }
            catch (Exception e) when (e is IOException || e is UnauthorizedAccessException) { Debug.LogWarning("[WornTag][Account] Could not delete local data: " + e.Message); }
        }

        static void MergeInto(string from, string to)
        {
            Directory.CreateDirectory(to);
            foreach (string file in Directory.GetFiles(from))
            {
                string destination = Path.Combine(to, Path.GetFileName(file));
                if (!File.Exists(destination)) File.Move(file, destination);
            }
            foreach (string directory in Directory.GetDirectories(from))
                MergeInto(directory, Path.Combine(to, Path.GetFileName(directory)));
        }

        // Firebase uids are letters and digits; never let anything else become part of a path.
        static string Safe(string accountUid)
        {
            if (string.IsNullOrEmpty(accountUid)) return GuestFolder;
            var chars = accountUid.ToCharArray();
            for (int i = 0; i < chars.Length; i++) if (!char.IsLetterOrDigit(chars[i]) && chars[i] != '-' && chars[i] != '_') chars[i] = '_';
            return new string(chars);
        }

        static string Nullable(string value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();

        /// <summary>Tests: use a temporary root and forget the remembered account.</summary>
        public static void SetRootForTests(string path) { rootOverride = path; uid = null; loaded = true; }
    }
}
