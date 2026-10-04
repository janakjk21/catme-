using System;
using System.Collections.Generic;
using CatMe.Pets;
using UnityEngine;
using UnityEngine.UI;

namespace CatMe.Garden
{
    /// <summary>
    /// The player's pets (MENUS_AND_ACCOUNT section 6): "Add a pet" first, then a card per pet. Tap "Walk with" to switch,
    /// "…" to rename or remove. The owner avatar is shared by every pet. Back returns to where it was opened from.
    /// </summary>
    internal sealed class PetsScreen
    {
        public Action<PetProfile> OnPetChosen;
        public Action OnAddPet, OnOpenStore, OnClosed;
        /// <summary>Set by the app flow; used to confirm removing a pet.</summary>
        public ConfirmDialog Confirm;
        readonly Transform canvasRoot;
        GameObject root; RectTransform panel, content, dotsHolder; ScrollRect scroll; Text subtitle;
        GameObject petMenu, renameSheet; Text petMenuTitle, renameError; InputField renameField; Button removeButton;
        PetProfile menuPet;
        int dotsCount = -1, dotsActive = -1;
        readonly List<Sprite> photoSprites = new List<Sprite>();

        const float CardWidth = 360f, CardHeight = 440f, Spacing = 28f, ScrollWidth = 1480f;

        public PetsScreen(Transform canvasRoot) { this.canvasRoot = canvasRoot; Build(); PetRoster.Changed += () => { if (root != null && root.activeSelf) Refresh(); }; }
        public bool IsOpen => root != null && root.activeSelf;

        void Build()
        {
            var screen = UiKit.Screen(canvasRoot, "Pets screen", new Color(.153f, .137f, .129f, .35f));
            root = screen.root;
            panel = ((RectTransform)CatMeUIStyle.Card(screen.safe, "Pets panel", new Vector2(1560f, 880f), CatMeUIStyle.Paper, CatMeUIStyle.Elevation.Floating).transform);
            UiKit.IconButton(panel, "back", new Vector2(-700f, 365f), Close, "Back from your pets");
            UiKit.Label(panel, "Your pets", 54, CatMeUIStyle.Ink, new Vector2(-170f, 375f), new Vector2(900f, 72f), FontStyle.Bold, TextAnchor.MiddleLeft, "Title");
            subtitle = UiKit.Label(panel, "", 26, CatMeUIStyle.MutedInk, new Vector2(-170f, 320f), new Vector2(900f, 40f), FontStyle.Normal, TextAnchor.MiddleLeft, "Subtitle");
            content = UiKit.HScroll(panel, new Vector2(0f, 20f), new Vector2(ScrollWidth, 520f), Spacing, 24);
            scroll = content.GetComponentInParent<ScrollRect>();
            scroll.onValueChanged.AddListener(_ => RefreshDots());
            dotsHolder = UiKit.Rect(panel, "Page dots", new Vector2(0f, -258f), new Vector2(600f, 24f));
            // The person is shared by every pet.
            var owner = CatMeUIStyle.Card(panel, "Owner chip", new Vector2(460f, 96f), CatMeUIStyle.Mist, CatMeUIStyle.Elevation.Resting);
            ((RectTransform)owner.transform).anchoredPosition = new Vector2(-500f, -360f);
            UiKit.Pic(owner.transform, Art.Load("PersonCircleIcon"), new Vector2(-175f, 0f), new Vector2(66f, 66f), true, "Owner icon");
            UiKit.Label(owner.transform, "You", 28, CatMeUIStyle.Ink, new Vector2(-30f, 16f), new Vector2(220f, 36f), FontStyle.Bold, TextAnchor.MiddleLeft);
            UiKit.Label(owner.transform, "Shared by every pet", 20, CatMeUIStyle.MutedInk, new Vector2(-30f, -18f), new Vector2(220f, 30f), FontStyle.Normal, TextAnchor.MiddleLeft);
            if (GardenCompanionDemo.StoreShipped)
                UiKit.Button(panel, "Pet store", new Vector2(600f, -360f), new Vector2(300f, 76f), CatMeUIStyle.SageDeep, true, () => OnOpenStore?.Invoke(), 28, "Open store");
            BuildPetMenu();
            BuildRename();
            root.SetActive(false);
        }

        public void Open() { Refresh(); root.SetActive(true); root.transform.SetAsLastSibling(); scroll.horizontalNormalizedPosition = 0f; }
        public void Close() { if (!IsOpen) return; petMenu.SetActive(false); renameSheet.SetActive(false); root.SetActive(false); OnClosed?.Invoke(); }
        public void Hide() { if (root != null) root.SetActive(false); }

        void Refresh()
        {
            foreach (Transform c in content) { c.gameObject.SetActive(false); UnityEngine.Object.Destroy(c.gameObject); }
            foreach (var s in photoSprites) if (s != null) { UnityEngine.Object.Destroy(s.texture); UnityEngine.Object.Destroy(s); }
            photoSprites.Clear();
            var active = PetRoster.Active;
            subtitle.text = "Choose who walks with you today.";
            string pending = CatMe.Onboarding.CreationClient.PendingPetId;
            BuildAddCard();
            foreach (var pet in PetRoster.Pets) BuildPetCard(pet, pet.id == active.id, !string.IsNullOrEmpty(pending) && pet.serverId == pending);
            dotsCount = -1; RefreshDots();
        }

        // Page dots, shown only when the cards don't all fit.
        void RefreshDots()
        {
            int cards = PetRoster.Pets.Count + 1;
            int perPage = Mathf.Max(1, Mathf.FloorToInt((ScrollWidth + Spacing) / (CardWidth + Spacing)));
            int pages = Mathf.CeilToInt(cards / (float)perPage);
            int page = pages <= 1 ? 0 : Mathf.Clamp(Mathf.RoundToInt(scroll.horizontalNormalizedPosition * (pages - 1)), 0, pages - 1);
            if (pages == dotsCount && page == dotsActive) return;
            dotsCount = pages; dotsActive = page;
            foreach (Transform c in dotsHolder) UnityEngine.Object.Destroy(c.gameObject);
            if (pages <= 1) return;
            for (int i = 0; i < pages; i++)
            {
                var d = UiKit.Rect(dotsHolder, "Dot", new Vector2((i - (pages - 1) / 2f) * 30f, 0f), new Vector2(i == page ? 22f : 12f, 12f));
                var img = d.gameObject.AddComponent<Image>(); img.sprite = UiKit.Circle; img.raycastTarget = false;
                img.color = i == page ? CatMeUIStyle.SageDeep : CatMeUIStyle.Stone;
            }
        }

        void BuildPetCard(PetProfile pet, bool isActive, bool beingMade)
        {
            var card = UiKit.LayoutCard(content, "Pet " + pet.name, new Vector2(CardWidth, CardHeight), isActive ? CatMeUIStyle.SageTint : CatMeUIStyle.Paper);
            if (isActive) { var o = card.AddComponent<Outline>(); o.effectColor = CatMeUIStyle.SageDeep; o.effectDistance = new Vector2(4f, -4f); }
            else { var o = card.AddComponent<Outline>(); o.effectColor = CatMeUIStyle.Stone; o.effectDistance = new Vector2(2f, -2f); }
            var photo = Art.FromFile(pet.photoPath); if (photo != null) photoSprites.Add(photo);
            Sprite fallback = Art.Load(pet.Kind == Species.Dog ? "GoodDogExample" : "GoodCatExample") ?? Art.Load("PlaceholderCat");
            UiKit.RoundPhoto(card.transform, photo ?? fallback, new Vector2(0f, 100f), 200f, isActive ? CatMeUIStyle.SageDeep : CatMeUIStyle.Stone, "Pet photo");
            UiKit.Label(card.transform, pet.name, 34, CatMeUIStyle.Ink, new Vector2(0f, -32f), new Vector2(300f, 44f), FontStyle.Bold);
            UiKit.Chip(card.transform, pet.Kind == Species.Dog ? "Dog" : "Cat", CatMeUIStyle.SageTint, CatMeUIStyle.SageDeep, new Vector2(0f, -80f), new Vector2(110f, 38f), 20);

            var p = pet;
            var more = UiKit.Button(card.transform, "…", new Vector2(CardWidth / 2f - 44f, CardHeight / 2f - 44f), new Vector2(64f, 56f), CatMeUIStyle.Mist, false, () => OpenPetMenu(p), 30, "Pet options");
            more.GetComponentInChildren<Text>().color = CatMeUIStyle.SageDeep;

            if (beingMade)
            {
                var track = UiKit.Rect(card.transform, "Making track", new Vector2(0f, -140f), new Vector2(260f, 8f));
                var ti = track.gameObject.AddComponent<Image>(); ti.sprite = UiKit.Circle; ti.color = CatMeUIStyle.Stone; ti.raycastTarget = false;
                var fill = UiKit.Rect(track, "Making fill", new Vector2(-65f, 0f), new Vector2(130f, 8f));
                var fi = fill.gameObject.AddComponent<Image>(); fi.sprite = UiKit.Circle; fi.color = CatMeUIStyle.Sage; fi.raycastTarget = false;
                UiKit.Label(card.transform, "Being made · about 3 min", 22, CatMeUIStyle.MutedInk, new Vector2(0f, -176f), new Vector2(320f, 34f));
                return;
            }
            if (pet.Kind == Species.Dog && (pet.isFixture || string.IsNullOrEmpty(pet.modelPath)) && !string.IsNullOrEmpty(pet.serverId))
                UiKit.Label(card.transform, "Stand-in until " + Shorten(pet.name) + " is ready", 19, CatMeUIStyle.MutedInk, new Vector2(0f, -116f), new Vector2(320f, 30f));
            if (isActive)
                UiKit.Chip(card.transform, "Walking now", CatMeUIStyle.SageDeep, CatMeUIStyle.Paper, new Vector2(0f, -164f), new Vector2(250f, 58f), 22);
            else
                UiKit.Button(card.transform, "Walk with " + Shorten(pet.name), new Vector2(0f, -164f), new Vector2(270f, 64f), CatMeUIStyle.SageDeep, true, () => { PetRoster.SetActive(p.id); OnPetChosen?.Invoke(p); Refresh(); }, 22, "Choose");
        }

        void BuildAddCard()
        {
            var card = UiKit.LayoutCard(content, "Add a pet", new Vector2(CardWidth, CardHeight), CatMeUIStyle.Mist);
            var o = card.AddComponent<Outline>(); o.effectColor = CatMeUIStyle.Stone; o.effectDistance = new Vector2(2f, -2f);
            var hit = card.AddComponent<Button>(); hit.targetGraphic = card.GetComponent<Image>(); hit.onClick.AddListener(() => OnAddPet?.Invoke());
            var plus = UiKit.Rect(card.transform, "Plus", new Vector2(0f, 70f), new Vector2(150f, 150f));
            var pi = plus.gameObject.AddComponent<Image>(); pi.sprite = UiKit.Circle; pi.color = CatMeUIStyle.Paper; pi.raycastTarget = false;
            UiKit.Icon(plus, "plus", Vector2.zero, 72f, CatMeUIStyle.SageDeep);
            UiKit.Label(card.transform, "Add a pet", 34, CatMeUIStyle.Ink, new Vector2(0f, -54f), new Vector2(300f, 44f), FontStyle.Bold);
            UiKit.Label(card.transform, "A cat or a dog, from a photo", 22, CatMeUIStyle.MutedInk, new Vector2(0f, -100f), new Vector2(300f, 56f));
            UiKit.Button(card.transform, "Add", new Vector2(0f, -164f), new Vector2(200f, 64f), CatMeUIStyle.SageDeep, true, () => OnAddPet?.Invoke(), 24, "Add pet");
        }

        // ---- "…": rename or remove -------------------------------------------------------------------------------

        void BuildPetMenu()
        {
            petMenu = UiKit.Rect(panel, "Pet options", Vector2.zero, new Vector2(1560f, 880f)).gameObject;
            var scrim = petMenu.AddComponent<Image>(); scrim.color = new Color(.153f, .137f, .129f, .25f);
            var close = petMenu.AddComponent<Button>(); close.transition = Selectable.Transition.None; close.onClick.AddListener(() => petMenu.SetActive(false));
            var card = (RectTransform)CatMeUIStyle.Card(petMenu.transform, "Options card", new Vector2(560f, 380f), CatMeUIStyle.Paper, CatMeUIStyle.Elevation.Floating).transform;
            petMenuTitle = UiKit.Label(card, "", 36, CatMeUIStyle.Ink, new Vector2(0f, 130f), new Vector2(480f, 56f), FontStyle.Bold, TextAnchor.MiddleLeft, "Title");
            var rename = UiKit.Button(card, "Rename", new Vector2(0f, 40f), new Vector2(480f, 80f), CatMeUIStyle.Mist, false, () => { petMenu.SetActive(false); OpenRename(menuPet); }, 28, "Rename");
            rename.GetComponentInChildren<Text>().color = CatMeUIStyle.SageDeep;
            removeButton = UiKit.Button(card, "Remove from my pets", new Vector2(0f, -56f), new Vector2(480f, 80f), CatMeUIStyle.Mist, false, () => { petMenu.SetActive(false); ConfirmRemove(menuPet); }, 28, "Remove");
            removeButton.GetComponentInChildren<Text>().color = CatMeUIStyle.Danger;
            UiKit.Link(card, "Cancel", new Vector2(0f, -140f), new Vector2(300f, 60f), () => petMenu.SetActive(false), 26, TextAnchor.MiddleCenter);
            petMenu.SetActive(false);
        }

        void OpenPetMenu(PetProfile pet)
        {
            menuPet = pet;
            petMenuTitle.text = pet.name;
            removeButton.gameObject.SetActive(PetRoster.CanRemove(pet.id, CatMe.Onboarding.CreationClient.PendingPetId));
            petMenu.SetActive(true); petMenu.transform.SetAsLastSibling();
        }

        void ConfirmRemove(PetProfile pet)
        {
            if (pet == null) return;
            void Remove()
            {
                bool wasActive = PetRoster.Active.id == pet.id;
                if (PetRoster.Remove(pet.id) && wasActive) OnPetChosen?.Invoke(PetRoster.Active);
            }
            if (Confirm == null) { Remove(); return; }
            Confirm.Show("Remove " + pet.name + "?", pet.name + " leaves your pets on this phone. This can't be undone.", "Keep " + Shorten(pet.name), "Remove", null, Remove);
        }

        void BuildRename()
        {
            renameSheet = UiKit.Rect(panel, "Rename pet", Vector2.zero, new Vector2(1560f, 880f)).gameObject;
            var scrim = renameSheet.AddComponent<Image>(); scrim.color = new Color(.153f, .137f, .129f, .25f);
            var card = (RectTransform)CatMeUIStyle.Card(renameSheet.transform, "Rename card", new Vector2(760f, 420f), CatMeUIStyle.Paper, CatMeUIStyle.Elevation.Floating).transform;
            UiKit.Label(card, "Rename", 40, CatMeUIStyle.Ink, new Vector2(0f, 150f), new Vector2(680f, 60f), FontStyle.Bold, TextAnchor.MiddleLeft, "Title");
            var box = CatMeUIStyle.Card(card, "Name field", new Vector2(680f, 100f), CatMeUIStyle.Mist, CatMeUIStyle.Elevation.Resting);
            ((RectTransform)box.transform).anchoredPosition = new Vector2(0f, 50f);
            var text = UiKit.Label(box.transform, "", 40, CatMeUIStyle.Ink, Vector2.zero, new Vector2(620f, 80f), FontStyle.Bold, TextAnchor.MiddleLeft);
            var hint = UiKit.Label(box.transform, "Name", 40, CatMeUIStyle.MutedInk, Vector2.zero, new Vector2(620f, 80f), FontStyle.Bold, TextAnchor.MiddleLeft);
            renameField = box.AddComponent<InputField>(); renameField.textComponent = text; renameField.placeholder = hint; renameField.characterLimit = PetRoster.NameMaxLength;
            renameField.onValueChanged.AddListener(_ => renameError.text = "");
            renameError = UiKit.Label(card, "", 22, CatMeUIStyle.Danger, new Vector2(0f, -30f), new Vector2(680f, 40f), FontStyle.Normal, TextAnchor.MiddleLeft, "Error");
            var cancel = UiKit.Button(card, "Cancel", new Vector2(-170f, -130f), new Vector2(320f, 84f), CatMeUIStyle.Mist, false, () => renameSheet.SetActive(false), 28, "Cancel rename");
            cancel.GetComponentInChildren<Text>().color = CatMeUIStyle.SageDeep;
            UiKit.Button(card, "Save", new Vector2(170f, -130f), new Vector2(320f, 84f), CatMeUIStyle.SageDeep, true, SaveRename, 28, "Save name");
            renameSheet.SetActive(false);
        }

        void OpenRename(PetProfile pet)
        {
            if (pet == null) return;
            menuPet = pet;
            renameField.text = pet.name; renameError.text = "";
            renameSheet.SetActive(true); renameSheet.transform.SetAsLastSibling();
            renameField.ActivateInputField();
        }

        void SaveRename()
        {
            if (menuPet == null) { renameSheet.SetActive(false); return; }
            if (!PetRoster.Rename(menuPet.id, renameField.text)) { renameError.text = "Use 1 to 20 letters, numbers, spaces, ' or -."; return; }
            renameSheet.SetActive(false);
        }

        static string Shorten(string n) => n.Length > 9 ? n.Substring(0, 8) + "…" : n;
    }
}
