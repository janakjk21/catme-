using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace CatMe.Pets
{
    public enum Species { Cat = 0, Dog = 1 }

    [Serializable]
    public sealed class PetProfile
    {
        public string id;
        public string name;
        public int species;          // Species
        public string photoPath;     // the owner's pet photo, kept on this phone
        public string createdUtc;
        public string serverId;      // id of the pet on the WornTag server, once created there
        public string modelPath;     // downloaded model on this phone (empty for fixture or built-in models)
        public bool isFixture;       // true while a stand-in model is used
        public Species Kind => species == (int)Species.Dog ? Species.Dog : Species.Cat;
    }

    /// <summary>
    /// The player's pets, stored locally (layout-first phase: no account, no server). One pet is "active" and walks with the owner.
    /// The owner avatar is shared by every pet. Always contains at least one pet so the garden is never empty.
    /// </summary>
    public static class PetRoster
    {
        [Serializable] sealed class Save { public int schema = 1; public string activeId; public List<PetProfile> pets = new List<PetProfile>(); public List<string> hiddenServerIds = new List<string>(); }

        static Save data;
        public static event Action Changed;

        // Each account keeps its own pets (AccountStorage: Profiles/{uid}/); switching accounts reloads the list.
        static string Folder => CatMe.Onboarding.AccountStorage.ProfileFolder;
        static PetRoster() { CatMe.Onboarding.AccountStorage.Changed += Reload; }
#if UNITY_EDITOR
        public static void SetStorageRootForTests(string path) { CatMe.Onboarding.AccountStorage.SetRootForTests(path); Reload(); }
#endif
        static string FilePath => Path.Combine(Folder, "pets-v1.json");
        public static string PetFolder(string id) => Path.Combine(Folder, "pets", id);

        public static IReadOnlyList<PetProfile> Pets { get { Ensure(); return data.pets; } }
        public static PetProfile Active { get { Ensure(); return data.pets.Find(p => p.id == data.activeId) ?? data.pets[0]; } }

        static void Ensure()
        {
            if (data != null) return;
            try { if (File.Exists(FilePath)) data = JsonUtility.FromJson<Save>(File.ReadAllText(FilePath)); }
            catch (Exception e) { Debug.LogWarning("[CatMe][Pets] Could not read pets file: " + e.Message); }
            if (data == null || data.pets == null || data.pets.Count == 0)
            {
                data = new Save();
                // First launch: one default companion, matching the pair already on this phone.
                var first = new PetProfile { id = NewId(), name = "Your cat", species = (int)Species.Cat, createdUtc = DateTime.UtcNow.ToString("O") };
                data.pets.Add(first); data.activeId = first.id;
                Write();
            }
        }

        static string NewId() => Guid.NewGuid().ToString("N").Substring(0, 10);

        static void Write()
        {
            try
            {
                Directory.CreateDirectory(Folder);
                string pending = FilePath + ".pending";
                File.WriteAllText(pending, JsonUtility.ToJson(data));
                if (File.Exists(FilePath)) File.Replace(pending, FilePath, FilePath + ".bak"); else File.Move(pending, FilePath);
            }
            catch (Exception e) { Debug.LogWarning("[CatMe][Pets] Could not save pets: " + e.Message); }
        }

        /// <summary>True once the player has made a pet of their own (not the stand-in pet every new phone starts with).</summary>
        public static bool HasOwnPet { get { Ensure(); return data.pets.Exists(IsOwn); } }
        static bool IsOwn(PetProfile p) => !string.IsNullOrEmpty(p.serverId) || !string.IsNullOrEmpty(p.photoPath);

        /// <summary>The active pet's name for player-facing text (COPY.md [Name]); "your cat" or "your dog" while it has none.</summary>
        public static string NameForText(bool startOfSentence = false)
        {
            PetProfile pet = Active;
            string name = pet?.name?.Trim();
            // The stand-in pet is called "Your cat"; that is not a name.
            if (string.IsNullOrEmpty(name) || name.StartsWith("Your ", StringComparison.Ordinal))
                name = pet != null && pet.Kind == Species.Dog ? "your dog" : "your cat";
            return startOfSentence && name.Length > 0 ? char.ToUpperInvariant(name[0]) + name.Substring(1) : name;
        }

        public static PetProfile Get(string id) { Ensure(); return data.pets.Find(p => p.id == id); }

        /// <summary>Adds a pet found in the signed-in account without duplicating one already restored on this phone.</summary>
        public static PetProfile AddServerPet(string serverId, string name, string serverSpecies)
        {
            Ensure();
            if (string.IsNullOrEmpty(serverId)) return null;
            var existing = data.pets.Find(p => p.serverId == serverId);
            if (existing != null) return existing;
            if (data.hiddenServerIds != null && data.hiddenServerIds.Contains(serverId)) return null;   // removed from this player's pets
            data.pets.RemoveAll(p => !IsOwn(p));
            Species kind = string.Equals(serverSpecies, "dog", StringComparison.OrdinalIgnoreCase) ? Species.Dog : Species.Cat;
            var pet = new PetProfile
            {
                id = NewId(), serverId = serverId, name = string.IsNullOrWhiteSpace(name) ? (kind == Species.Dog ? "Your dog" : "Your cat") : name.Trim(),
                species = (int)kind, createdUtc = DateTime.UtcNow.ToString("O")
            };
            data.pets.Add(pet);
            if (string.IsNullOrEmpty(data.activeId) || !data.pets.Exists(p => p.id == data.activeId)) data.activeId = pet.id;
            Write(); Changed?.Invoke();
            return pet;
        }

        /// <summary>Adds a pet (copying its photo into the pet's own folder) and makes it active.</summary>
        public static PetProfile Add(string name, Species species, string sourcePhotoPath)
        {
            Ensure();
            data.pets.RemoveAll(p => !IsOwn(p));     // the first real pet replaces the stand-in
            var pet = new PetProfile { id = NewId(), name = string.IsNullOrWhiteSpace(name) ? (species == Species.Dog ? "Your dog" : "Your cat") : name.Trim(), species = (int)species, createdUtc = DateTime.UtcNow.ToString("O") };
            if (!string.IsNullOrEmpty(sourcePhotoPath) && File.Exists(sourcePhotoPath))
            {
                try { Directory.CreateDirectory(PetFolder(pet.id)); pet.photoPath = Path.Combine(PetFolder(pet.id), "photo.jpg"); File.Copy(sourcePhotoPath, pet.photoPath, true); }
                catch (Exception e) { Debug.LogWarning("[CatMe][Pets] Could not copy pet photo: " + e.Message); pet.photoPath = null; }
            }
            data.pets.Add(pet); data.activeId = pet.id; Write(); Changed?.Invoke();
            return pet;
        }

        public static void SetActive(string id) { Ensure(); if (data.pets.Exists(p => p.id == id) && data.activeId != id) { data.activeId = id; Write(); Changed?.Invoke(); } }
        /// <summary>Renames a pet. Returns false when the name breaks the rule in <see cref="CleanName"/>.</summary>
        public static bool Rename(string id, string name)
        {
            Ensure();
            var p = Get(id); string clean = CleanName(name);
            if (p == null || clean == null) return false;
            p.name = clean; Write(); Changed?.Invoke();
            return true;
        }

        public const int NameMaxLength = 20;

        /// <summary>
        /// The name rule (MENUS_AND_ACCOUNT section 6): spaces trimmed and runs of spaces made single, 1 to 20 characters,
        /// letters, numbers, spaces, apostrophes and hyphens only. Returns null when the name can't be used.
        /// </summary>
        public static string CleanName(string name)
        {
            if (string.IsNullOrWhiteSpace(name)) return null;
            var sb = new System.Text.StringBuilder();
            bool space = false;
            foreach (char c in name.Trim())
            {
                if (char.IsWhiteSpace(c)) { if (!space) sb.Append(' '); space = true; continue; }
                space = false;
                if (!char.IsLetterOrDigit(c) && c != '\'' && c != '-' && c != '\u2019') return null;
                sb.Append(c == '\u2019' ? '\'' : c);
            }
            string clean = sb.ToString();
            return clean.Length >= 1 && clean.Length <= NameMaxLength ? clean : null;
        }

        /// <summary>True when the pet can be removed: not the last one, and not one still being made.</summary>
        public static bool CanRemove(string id, string pendingServerId)
        {
            Ensure();
            var p = Get(id);
            return p != null && data.pets.Count > 1 && (string.IsNullOrEmpty(pendingServerId) || p.serverId != pendingServerId);
        }

        /// <summary>
        /// Removes a pet from this account's list on this phone, with its photo and model copies. A pet saved on the server is
        /// remembered as removed so signing in again doesn't bring it back. The list never becomes empty.
        /// </summary>
        public static bool Remove(string id)
        {
            Ensure();
            var p = Get(id);
            if (p == null || data.pets.Count <= 1) return false;
            data.pets.Remove(p);
            if (!string.IsNullOrEmpty(p.serverId))
            {
                if (data.hiddenServerIds == null) data.hiddenServerIds = new List<string>();
                if (!data.hiddenServerIds.Contains(p.serverId)) data.hiddenServerIds.Add(p.serverId);
            }
            if (data.activeId == id) data.activeId = data.pets[0].id;
            try { string folder = PetFolder(id); if (Directory.Exists(folder)) Directory.Delete(folder, true); }
            catch (Exception e) { Debug.LogWarning("[CatMe][Pets] Could not delete pet files: " + e.Message); }
            Write(); Changed?.Invoke();
            return true;
        }

        /// <summary>Test hook: forget the in-memory copy so the file is re-read.</summary>
        public static void RemovePhotoCopies()
        {
            Ensure();
            foreach (var pet in data.pets)
            {
                string ownPhoto = Path.Combine(PetFolder(pet.id), "photo.jpg");
                if (File.Exists(ownPhoto)) File.Delete(ownPhoto);
                pet.photoPath = null;
            }
            Write();
            // Backups must not retain paths to deleted photos.
            if (File.Exists(FilePath + ".bak")) File.Delete(FilePath + ".bak");
            Changed?.Invoke();
        }

        public static void Reload() { data = null; }
        /// <summary>Saves changes made to a pet object returned by Add or Get.</summary>
        public static void SaveChanges() { Ensure(); Write(); Changed?.Invoke(); }
    }
}
