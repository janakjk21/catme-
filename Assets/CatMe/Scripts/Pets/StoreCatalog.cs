using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace CatMe.Pets
{
    public enum StoreCategory { Toy = 0, Food = 1 }

    public sealed class StoreItem
    {
        public string id, name, blurb;
        public StoreCategory category;
        public Species? only;           // null = suits any pet
        public string placeholderPrice; // layout-only; real pricing comes with the payment phase
        public Color tint;
        public string glyph;            // one letter shown on the placeholder icon tile
    }

    /// <summary>Local fixture catalog and ownership. "Buying" only flips a local flag: no payment, no server (layout-first phase).</summary>
    public static class StoreCatalog
    {
        public static readonly List<StoreItem> Items = new List<StoreItem>
        {
            new StoreItem { id = "toy.ball",    name = "Soft ball",       blurb = "A light ball that rolls far.",           category = StoreCategory.Toy,  placeholderPrice = "$0.99", tint = new Color(.93f,.62f,.55f), glyph = "B" },
            new StoreItem { id = "toy.feather", name = "Feather wand",    blurb = "Gentle, swishy, hard to resist.",        category = StoreCategory.Toy,  only = Species.Cat, placeholderPrice = "$1.49", tint = new Color(.55f,.74f,.78f), glyph = "F" },
            new StoreItem { id = "toy.mouse",   name = "Squeaky mouse",   blurb = "A tiny felt mouse with a squeak.",       category = StoreCategory.Toy,  only = Species.Cat, placeholderPrice = "$0.99", tint = new Color(.78f,.70f,.60f), glyph = "M" },
            new StoreItem { id = "toy.rope",    name = "Rope tug",        blurb = "Braided rope for a good tug.",           category = StoreCategory.Toy,  only = Species.Dog, placeholderPrice = "$1.49", tint = new Color(.82f,.66f,.42f), glyph = "R" },
            new StoreItem { id = "toy.disc",    name = "Soft disc",       blurb = "Light, floaty, easy to catch.",          category = StoreCategory.Toy,  only = Species.Dog, placeholderPrice = "$1.49", tint = new Color(.50f,.70f,.55f), glyph = "D" },
            new StoreItem { id = "food.salmon", name = "Salmon bites",    blurb = "A favourite for a happy cat.",           category = StoreCategory.Food, only = Species.Cat, placeholderPrice = "$0.99", tint = new Color(.95f,.65f,.52f), glyph = "S" },
            new StoreItem { id = "food.tuna",   name = "Tuna flakes",     blurb = "Soft flakes, sea-fresh.",                category = StoreCategory.Food, only = Species.Cat, placeholderPrice = "$0.99", tint = new Color(.62f,.74f,.88f), glyph = "T" },
            new StoreItem { id = "food.chicken",name = "Chicken dinner",  blurb = "A warm bowl for a hungry pup.",          category = StoreCategory.Food, only = Species.Dog, placeholderPrice = "$0.99", tint = new Color(.90f,.76f,.50f), glyph = "C" },
            new StoreItem { id = "food.treats", name = "Treat jar",       blurb = "A jar of small, tasty rewards.",         category = StoreCategory.Food, placeholderPrice = "$1.29", tint = new Color(.78f,.58f,.42f), glyph = "J" },
        };

        [Serializable] sealed class Save { public List<string> owned = new List<string>(); }
        static Save save;
        public static event Action Changed;

        static string FilePath => Path.Combine(Application.persistentDataPath, "Profiles", "local-guest", "inventory-v1.json");

        static void Ensure()
        {
            if (save != null) return;
            try { if (File.Exists(FilePath)) save = JsonUtility.FromJson<Save>(File.ReadAllText(FilePath)); } catch { save = null; }
            if (save == null) save = new Save();
            // A first soft ball and a first meal so the house room always has something to use.
            bool changed = false;
            foreach (var starter in new[] { "toy.ball" }) if (!save.owned.Contains(starter)) { save.owned.Add(starter); changed = true; }
            if (changed) Write();
        }

        static void Write()
        {
            try { Directory.CreateDirectory(Path.GetDirectoryName(FilePath)); File.WriteAllText(FilePath, JsonUtility.ToJson(save)); }
            catch (Exception e) { Debug.LogWarning("[CatMe][Store] Could not save inventory: " + e.Message); }
        }

        public static bool Owns(string id) { Ensure(); return save.owned.Contains(id); }
        public static int OwnedCount { get { Ensure(); return save.owned.Count; } }

        /// <summary>Local stand-in for a purchase: marks the item owned immediately. Replaced by the verified store flow in the payment phase.</summary>
        public static bool Buy(string id)
        {
            Ensure();
            if (save.owned.Contains(id) || Items.Find(i => i.id == id) == null) return false;
            save.owned.Add(id); Write(); Changed?.Invoke();
            return true;
        }

        public static List<StoreItem> For(StoreCategory category, Species species) =>
            Items.FindAll(i => i.category == category && (!i.only.HasValue || i.only.Value == species));

        /// <summary>First owned item of a category that suits the species (used by the house room until a picker exists).</summary>
        public static StoreItem FirstOwned(StoreCategory category, Species species) =>
            For(category, species).Find(i => Owns(i.id));

        public static void Reload() { save = null; }
    }
}
