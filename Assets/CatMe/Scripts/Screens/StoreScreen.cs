using System;
using System.Collections.Generic;
using CatMe.Pets;
using UnityEngine;
using UnityEngine.UI;

namespace CatMe.Garden
{
    /// <summary>
    /// Toys and food for the active pet. Layout-first: "Buy" marks the item owned on this phone immediately, with placeholder prices and no payment.
    /// </summary>
    internal sealed class StoreScreen
    {
        public Action OnClosed;
        readonly Transform canvasRoot;
        GameObject root; RectTransform panel, content; Text shopper, note;
        Button toysTab, foodTab, bagTab; StoreCategory tab = StoreCategory.Toy; bool bag;

        public StoreScreen(Transform canvasRoot) { this.canvasRoot = canvasRoot; Build(); StoreCatalog.Changed += () => { if (IsOpen) Refresh(); }; }
        public bool IsOpen => root != null && root.activeSelf;

        void Build()
        {
            var screen = UiKit.Screen(canvasRoot, "Store screen", new Color(.153f, .137f, .129f, .35f));
            root = screen.root;
            panel = ((RectTransform)CatMeUIStyle.Card(screen.safe, "Store panel", new Vector2(1560f, 860f), CatMeUIStyle.Paper, CatMeUIStyle.Elevation.Floating).transform);
            UiKit.IconButton(panel, "back", new Vector2(-700f, 355f), Close, "Back from the store");
            UiKit.Label(panel, "Store", 54, CatMeUIStyle.Ink, new Vector2(-170f, 365f), new Vector2(900f, 72f), FontStyle.Bold, TextAnchor.MiddleLeft, "Title");
            shopper = UiKit.Label(panel, "", 26, CatMeUIStyle.MutedInk, new Vector2(-170f, 310f), new Vector2(900f, 40f), FontStyle.Normal, TextAnchor.MiddleLeft, "Shopping for");
            toysTab = UiKit.Button(panel, "Toys", new Vector2(-260f, 220f), new Vector2(210f, 66f), CatMeUIStyle.SageDeep, true, () => { tab = StoreCategory.Toy; bag = false; Refresh(); }, 26, "Toys tab");
            foodTab = UiKit.Button(panel, "Food", new Vector2(-30f, 220f), new Vector2(210f, 66f), CatMeUIStyle.Mist, false, () => { tab = StoreCategory.Food; bag = false; Refresh(); }, 26, "Food tab");
            bagTab = UiKit.Button(panel, "Your bag", new Vector2(200f, 220f), new Vector2(210f, 66f), CatMeUIStyle.Mist, false, () => { bag = true; Refresh(); }, 26, "Bag tab");
            content = UiKit.HScroll(panel, new Vector2(0f, -40f), new Vector2(1480f, 500f), 26f, 24);
            note = UiKit.Label(panel, "", 20, CatMeUIStyle.MutedInk, new Vector2(0f, -378f), new Vector2(1300f, 36f));
            root.SetActive(false);
        }

        public void Open() { Refresh(); root.SetActive(true); root.transform.SetAsLastSibling(); }
        public void Close() { if (!IsOpen) return; root.SetActive(false); OnClosed?.Invoke(); }
        public void Hide() { if (root != null) root.SetActive(false); }

        void Refresh()
        {
            var pet = PetRoster.Active;
            shopper.text = "Shopping for " + PetRoster.NameForText() + " · " + (pet.Kind == Species.Dog ? "dog" : "cat");
            Tint(toysTab, !bag && tab == StoreCategory.Toy); Tint(foodTab, !bag && tab == StoreCategory.Food); Tint(bagTab, bag);
            foreach (Transform c in content) { c.gameObject.SetActive(false); UnityEngine.Object.Destroy(c.gameObject); }
            var items = new List<StoreItem>();
            if (bag) { foreach (var c in new[] { StoreCategory.Toy, StoreCategory.Food }) foreach (var i in StoreCatalog.For(c, pet.Kind)) if (StoreCatalog.Owns(i.id)) items.Add(i); }
            else items.AddRange(StoreCatalog.For(tab, pet.Kind));
            foreach (var item in items) BuildItem(item);
            if (items.Count == 0) UiKit.Label(content, "Nothing here yet.", 34, CatMeUIStyle.BodyInk, Vector2.zero, new Vector2(600f, 80f));
        }

        static void Tint(Button b, bool on)
        {
            CatMeUIStyle.StyleButton(b, on ? CatMeUIStyle.SageDeep : CatMeUIStyle.Mist, on);
            var t = b.GetComponentInChildren<Text>(); if (t != null) t.color = on ? CatMeUIStyle.Paper : CatMeUIStyle.SageDeep;
        }

        void BuildItem(StoreItem item)
        {
            var card = UiKit.LayoutCard(content, "Item " + item.name, new Vector2(300f, 420f), CatMeUIStyle.Paper);
            { var o = card.AddComponent<Outline>(); o.effectColor = CatMeUIStyle.Stone; o.effectDistance = new Vector2(2f, -2f); }
            var tile = CatMeUIStyle.Card(card.transform, "Icon", new Vector2(150f, 150f), item.tint, CatMeUIStyle.Elevation.Raised);
            ((RectTransform)tile.transform).anchoredPosition = new Vector2(0f, 110f);
            UiKit.Label(tile.transform, item.glyph, 84, new Color(1f, 1f, 1f, .92f), Vector2.zero, new Vector2(150f, 150f), FontStyle.Bold);
            UiKit.Label(card.transform, item.name, 28, CatMeUIStyle.Ink, new Vector2(0f, -4f), new Vector2(270f, 40f), FontStyle.Bold);
            UiKit.Label(card.transform, item.blurb, 19, CatMeUIStyle.MutedInk, new Vector2(0f, -56f), new Vector2(260f, 56f));
            bool owned = StoreCatalog.Owns(item.id);
            if (owned) UiKit.Chip(card.transform, "Owned", CatMeUIStyle.SageTint, CatMeUIStyle.SageDeep, new Vector2(0f, -150f), new Vector2(200f, 58f), 24);
            else
            {
                var id = item.id;
                UiKit.Button(card.transform, "Get", new Vector2(0f, -150f), new Vector2(200f, 58f), CatMeUIStyle.SageDeep, true, () => StoreCatalog.Buy(id), 24, "Get");
            }
        }
    }
}
