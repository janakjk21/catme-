using CatMe.Pets;
using UnityEngine;

namespace CatMe.Garden
{
    /// <summary>Builds each new screen on a given canvas so editor tooling can render it (UiKit and the screens are internal).</summary>
    public static class UiPreview
    {
        public static readonly string[] Names = { "pets", "store-toys", "store-food", "onb-login", "onb-species", "onb-photo", "onb-photofail", "onb-person", "onb-name", "onb-making", "onb-reveal", "icons", "pause", "settings", "account-only", "dialog", "toast", "loading", "offline", "failed" };

        public static void Show(Transform canvasRoot, string name)
        {
            if (PetRoster.Pets.Count < 3) { PetRoster.Add("Biscuit", Species.Dog, null); PetRoster.Add("Miso", Species.Cat, null); }
            if (name == "pets") { var s = new PetsScreen(canvasRoot); s.Open(); }
            else if (name == "settings") CatMeAppFlow.ShowSettingsPreview(canvasRoot);
            else if (name.StartsWith("store")) { PetRoster.SetActive(name == "store-food" ? PetRoster.Pets[1].id : PetRoster.Pets[0].id); var s = new StoreScreen(canvasRoot); s.Open(); if (name == "store-food") StoreCatalog.Buy("food.treats"); }
            else if (name == "icons")
            {
                var bg = UiKit.Rect(canvasRoot, "bg", Vector2.zero, new Vector2(2000f, 1200f)); bg.gameObject.AddComponent<UnityEngine.UI.Image>().color = CatMeUIStyle.Mist;
                string[] all = { "paw", "pause", "call", "walk", "ball", "sit", "arrow", "camera", "album", "gear", "store", "pets", "account", "bowl", "bed", "door", "up", "back", "close", "plus", "check", "trash", "share", "heart" };
                for (int i = 0; i < all.Length; i++)
                {
                    var p = new Vector2(-690f + (i % 8) * 197f, 300f - (i / 8) * 260f);
                    UiKit.Icon(canvasRoot, all[i], p, 120f, CatMeUIStyle.Pine);
                    UiKit.Label(canvasRoot, all[i], 24, CatMeUIStyle.BodyInk, p + new Vector2(0f, -90f), new Vector2(180f, 36f));
                }
            }
            else if (name == "pause")
            {
                var bg = UiKit.Rect(canvasRoot, "bg", Vector2.zero, new Vector2(2000f, 1200f)); bg.gameObject.AddComponent<UnityEngine.UI.Image>().color = new Color(.2f, .3f, .24f);
                var card = CatMeUIStyle.Card(canvasRoot, "Pause card", new Vector2(640f, 900f), CatMeUIStyle.PaperGlass, CatMeUIStyle.Elevation.Floating);
                ((RectTransform)card.transform).anchoredPosition = new Vector2(580f, 0f);
                UiKit.Label(card.transform, "Paused", 54, CatMeUIStyle.Pine, new Vector2(-95f, 370f), new Vector2(380f, 80f), FontStyle.Bold, TextAnchor.MiddleLeft, "Title");
                UiKit.IconTile(card.transform, "pets", "Your pets", new Vector2(-150f, 205f), new Vector2(280f, 180f), () => { });
                UiKit.IconTile(card.transform, "store", "Store", new Vector2(150f, 205f), new Vector2(280f, 180f), () => { });
                UiKit.IconTile(card.transform, "album", "Photo album", new Vector2(-150f, 10f), new Vector2(280f, 180f), () => { });
                UiKit.IconTile(card.transform, "account", "Account", new Vector2(150f, 10f), new Vector2(280f, 180f), () => { });
                UiKit.IconTile(card.transform, "gear", "Settings", new Vector2(-150f, -185f), new Vector2(280f, 180f), () => { });
                UiKit.NestedButton(card.transform, "Keep exploring", new Vector2(0f, -330f), 560f, CatMeUIStyle.Pine, () => { }, 36, "Keep exploring");
            }
            else if (name == "dialog" || name == "toast")
            {
                var bg = UiKit.Rect(canvasRoot, "bg", Vector2.zero, new Vector2(2000f, 1200f)); bg.gameObject.AddComponent<UnityEngine.UI.Image>().color = new Color(.2f, .3f, .24f);
                if (name == "dialog") new ConfirmDialog(canvasRoot).Show("Delete your account?", "This removes your pets, photos and saved walks. It cannot be undone.", "Keep it", "Delete everything", null, null);
                else { var toast = Toast.Create(canvasRoot); toast.Show("Mochi is ready", "Meet Mochi", null, 600f); toast.SnapShown(); }
            }
            else if (name == "loading" || name == "offline" || name == "failed")
            {
                var state = new ScreenState(canvasRoot);
                if (name == "loading") state.ShowLoading("Tip: touch and hold your pet to give it a stroke.");
                else if (name == "offline") state.ShowOffline(null, null); else state.ShowFailed("Mochi", null, null);
            }
            else if (name == "account-only") new OnboardingFlow(canvasRoot, null).BeginAccountOnly();
            else if (name.StartsWith("onb-")) new OnboardingFlow(canvasRoot, null).Preview(name.Substring(4));
            foreach (var follow in canvasRoot.GetComponentsInChildren<ShadowFollow>(true)) follow.SyncNow();   // no frames run in batch captures
        }
    }
}
