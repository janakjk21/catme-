using System;
using System.Collections.Generic;
using CatMe.Pets;
using UnityEngine;
using UnityEngine.UI;

namespace CatMe.Garden
{
    /// <summary>
    /// The player's pets as a scrollable row of cards: tap one to walk with them, or add another. The owner avatar is shared by every pet.
    /// Layout-first: all data is local (PetRoster); no account, no server.
    /// </summary>
    internal sealed class PetsScreen
    {
        public Action<PetProfile> OnPetChosen;
        public Action OnAddPet, OnOpenStore, OnClosed;
        readonly Transform canvasRoot;
        GameObject root; RectTransform panel, content; Text subtitle;
        readonly List<Sprite> photoSprites = new List<Sprite>();

        public PetsScreen(Transform canvasRoot) { this.canvasRoot = canvasRoot; Build(); PetRoster.Changed += () => { if (root != null && root.activeSelf) Refresh(); }; }
        public bool IsOpen => root != null && root.activeSelf;

        void Build()
        {
            var screen = UiKit.Screen(canvasRoot, "Pets screen", new Color(.12f, .1f, .09f, .62f));
            root = screen.root;
            panel = ((RectTransform)CatMeUIStyle.Card(screen.safe, "Pets panel", new Vector2(1560f, 860f), CatMeUIStyle.PaperGlass, CatMeUIStyle.Elevation.Floating).transform);
            UiKit.Label(panel, "Your pets", 54, CatMeUIStyle.Ink, new Vector2(-280f, 360f), new Vector2(900f, 80f), FontStyle.Bold, TextAnchor.MiddleLeft, "Title");
            subtitle = UiKit.Label(panel, "", 26, CatMeUIStyle.MutedInk, new Vector2(-280f, 300f), new Vector2(900f, 40f), FontStyle.Normal, TextAnchor.MiddleLeft, "Subtitle");
            UiKit.Button(panel, "Done", new Vector2(640f, 345f), new Vector2(190f, 70f), CatMeUIStyle.CreamSoft, false, Close, 26, "Close");
            content = UiKit.HScroll(panel, new Vector2(0f, 10f), new Vector2(1480f, 520f), 28f, 24);
            // Owner chip and Store shortcut along the bottom
            var owner = CatMeUIStyle.Card(panel, "Owner chip", new Vector2(420f, 92f), CatMeUIStyle.CreamSoft, CatMeUIStyle.Elevation.Resting);
            ((RectTransform)owner.transform).anchoredPosition = new Vector2(-520f, -358f);
            UiKit.Pic(owner.transform, Art.Load("PersonCircleIcon"), new Vector2(-155f, 0f), new Vector2(66f, 66f), true, "Owner icon");
            UiKit.Label(owner.transform, "You", 28, CatMeUIStyle.Ink, new Vector2(-40f, 14f), new Vector2(150f, 36f), FontStyle.Bold, TextAnchor.MiddleLeft);
            UiKit.Label(owner.transform, "Shared by every pet", 18, CatMeUIStyle.MutedInk, new Vector2(30f, -18f), new Vector2(280f, 30f), FontStyle.Normal, TextAnchor.MiddleLeft);
            UiKit.Button(panel, "Pet store", new Vector2(600f, -358f), new Vector2(300f, 76f), CatMeUIStyle.Pine, true, () => { OnOpenStore?.Invoke(); }, 28, "Open store");
            root.SetActive(false);
        }

        public void Open() { Refresh(); root.SetActive(true); root.transform.SetAsLastSibling(); }
        public void Close() { if (!IsOpen) return; root.SetActive(false); OnClosed?.Invoke(); }
        public void Hide() { if (root != null) root.SetActive(false); }

        void Refresh()
        {
            foreach (Transform c in content) { c.gameObject.SetActive(false); UnityEngine.Object.Destroy(c.gameObject); }
            foreach (var s in photoSprites) if (s != null) { UnityEngine.Object.Destroy(s.texture); UnityEngine.Object.Destroy(s); }
            photoSprites.Clear();
            var active = PetRoster.Active;
            subtitle.text = PetRoster.Pets.Count == 1 ? "Add more pets any time." : "Tap a pet to walk with them today.";
            foreach (var pet in PetRoster.Pets) BuildPetCard(pet, pet.id == active.id);
            BuildAddCard();
        }

        void BuildPetCard(PetProfile pet, bool isActive)
        {
            var card = UiKit.LayoutCard(content, "Pet " + pet.name, new Vector2(320f, 440f), isActive ? Color.white : CatMeUIStyle.CreamSoft);
            if (isActive) { var o = card.AddComponent<Outline>(); o.effectColor = CatMeUIStyle.Olive; o.effectDistance = new Vector2(5f, -5f); card.GetComponent<Image>().color = Color.white; }
            var photo = Art.FromFile(pet.photoPath); if (photo != null) photoSprites.Add(photo);
            Sprite fallback = pet.Kind == Species.Cat ? Art.Load("PlaceholderCat") : Art.Load("CatPawCircleIcon");
            UiKit.RoundPhoto(card.transform, photo ?? fallback, new Vector2(0f, 105f), 190f, isActive ? CatMeUIStyle.Olive : CatMeUIStyle.Hairline, "Pet photo");
            UiKit.Label(card.transform, pet.name, 32, CatMeUIStyle.Ink, new Vector2(0f, -38f), new Vector2(280f, 44f), FontStyle.Bold);
            UiKit.Chip(card.transform, pet.Kind == Species.Dog ? "Dog" : "Cat", pet.Kind == Species.Dog ? new Color(.93f, .80f, .62f) : new Color(.78f, .86f, .90f), CatMeUIStyle.Ink, new Vector2(0f, -86f), new Vector2(110f, 38f), 20);
            if (isActive)
            {
                UiKit.Chip(card.transform, "Walking with you", CatMeUIStyle.Olive, Color.white, new Vector2(0f, -158f), new Vector2(250f, 54f), 22);
            }
            else
            {
                var p = pet; UiKit.Button(card.transform, "Walk with " + Shorten(pet.name), new Vector2(0f, -158f), new Vector2(250f, 62f), CatMeUIStyle.Olive, true, () => { PetRoster.SetActive(p.id); OnPetChosen?.Invoke(p); Refresh(); }, 22, "Choose");
            }
        }

        void BuildAddCard()
        {
            var card = UiKit.LayoutCard(content, "Add a pet", new Vector2(320f, 440f), CatMeUIStyle.CreamSoft);
            var plus = UiKit.Rect(card.transform, "Plus", new Vector2(0f, 70f), new Vector2(150f, 150f));
            var pi = plus.gameObject.AddComponent<Image>(); pi.sprite = UiKit.Circle; pi.color = Color.white; pi.raycastTarget = false;
            UiKit.Label(plus, "+", 100, CatMeUIStyle.Olive, new Vector2(0f, 4f), new Vector2(150f, 150f), FontStyle.Bold);
            UiKit.Label(card.transform, "Add a pet", 32, CatMeUIStyle.Ink, new Vector2(0f, -54f), new Vector2(280f, 44f), FontStyle.Bold);
            UiKit.Label(card.transform, "A cat or a dog, with a photo", 20, CatMeUIStyle.MutedInk, new Vector2(0f, -100f), new Vector2(260f, 56f));
            UiKit.Button(card.transform, "Add", new Vector2(0f, -170f), new Vector2(200f, 60f), CatMeUIStyle.Olive, true, () => OnAddPet?.Invoke(), 24, "Add pet");
        }

        static string Shorten(string n) => n.Length > 9 ? n.Substring(0, 8) + "…" : n;
    }
}
