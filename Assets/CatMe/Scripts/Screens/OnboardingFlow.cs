using System;
using System.Collections.Generic;
using System.IO;
using CatMe.Pets;
using UnityEngine;
using UnityEngine.UI;

namespace CatMe.Garden
{
    /// <summary>
    /// Landscape onboarding, one decision per screen: login, cat or dog, pet photo, selfie, then the garden. When the pet is ready: name, then reveal.
    /// Layout and copy follow the approved prototype (docs/production/07). Account buttons are UI only here; real sign-in is wired in Phase 7.
    /// </summary>
    internal sealed class OnboardingFlow
    {
        public Action<PetProfile> OnFinished;
        /// <summary>Runs the real server creation (upload, generate, download). Set by the app flow once an account session exists.</summary>
        public Func<string, Species, string, System.Threading.Tasks.Task<string>, Action<string, string>, System.Threading.Tasks.Task<CatMe.Onboarding.CreationResult>> OnCreate;   // name, species, pet photo, selfie (arrives later), progress
        /// <summary>Raised when a creation finished successfully, with the new pet (also when the player already left the screen).</summary>
        public Action<PetProfile, CatMe.Onboarding.CreationResult> OnCreated;
        public Action OnCancelled;
        public IPhotoValidator Validator = new LocalPhotoValidator();
        readonly Transform canvasRoot;
        readonly DeviceBridge photos;
        GameObject root; RectTransform safe;
        bool addMode, signedInFlow; Species species; string petPhoto, personPhoto, petName = "";
        PetProfile created;
        InputField nameField;
        string step = "species";

        const float LeftX = -820f, RightX = 80f;   // text blocks start here (left-aligned) on the left or right half

        public OnboardingFlow(Transform canvasRoot, DeviceBridge photos) { this.canvasRoot = canvasRoot; this.photos = photos; current = this; }
        public bool IsOpen => root != null && root.activeSelf;
        static OnboardingFlow current;
        /// <summary>True while any onboarding or account screen covers the game; in-world prompts stay hidden.</summary>
        public static bool AnyOpen => current != null && current.IsOpen;

        List<string> Flow() => FlowFor(addMode);
        /// <summary>The three tips on the pet photo screen, for cats and dogs alike.</summary>
        internal static readonly string[] PetPhotoTips = { "Their whole body, nose to tail", "Lots of light", "Only your pet" };
        /// <summary>The steps before the garden. The pet is named before the wait, so the garden can say "Mochi is being made".</summary>
        internal static List<string> FlowFor(bool addMode) => addMode ? new List<string> { "species", "photo", "name" } : new List<string> { "species", "photo", "person", "name" };

        /// <summary>The player went into the garden while the pet is made (launch task L5). Argument: the pet's name.</summary>
        public Action<string> OnMakingInGarden;
        /// <summary>The pet finished while the player was in the garden: reveal it there, with no extra screens.</summary>
        public Action<PetProfile> OnRevealInGarden;
        /// <summary>True from entering the garden during Making until the pet is revealed or the creation fails.</summary>
        public bool MakingInGarden { get; private set; }
        /// <summary>The server's stage for the creation in progress (checking, queued, generating, downloading).</summary>
        public string MakingStage => remoteStage;
        public string MakingName => PetLabel;

        public void Begin(bool addMode)
        {
            accountOnly = false; finishedResult = null; this.addMode = addMode; signedInFlow = addMode || (IsSignedIn != null && IsSignedIn()); loginFirst = false; species = Species.Cat; petPhoto = personPhoto = null; personFailMessage = null; failHard = false; petName = ""; created = null; dogSize = PetSize.Medium;
            if (root == null) { var s = UiKit.Screen(canvasRoot, "Onboarding", CatMeUIStyle.Mist); root = s.root; safe = s.safe; }
            root.SetActive(true); root.transform.SetAsLastSibling();
            Show("species");
        }

        public void Hide() { if (root != null) root.SetActive(false); }

        /// <summary>Jumps straight to a step so each screen can be rendered without driving the flow.</summary>
        public void Preview(string s)
        {
            Begin(false); created = PetRoster.Active; petName = "Mochi";
            switch (s)
            {
                case "photo": Show("photo"); break;
                case "photofail": failMessage = "It's too dark to see them. Try again near a window."; Show("photo"); break;
                case "login": BeginLogin(); break;
                case "person": Show("person"); break;
                case "making": signedInFlow = true; OnCreate = (n, sp, ph, ow, rep) => new System.Threading.Tasks.TaskCompletionSource<CatMe.Onboarding.CreationResult>().Task; petPhoto = null; BeginEarlyCreation(); ShowMaking(); break;
                case "name": ShowReady(PetRoster.Active); break;
                case "reveal": ShowReveal(); break;
                default: Show("species"); break;
            }
        }

        string failMessage, personFailMessage;
        bool failHard;   // the phone is sure the photo is wrong (other animal, several pets): no "Use it anyway"

        // ---- the pet starts being made as soon as its photo is chosen --------------------------------------------------

        System.Threading.Tasks.TaskCompletionSource<string> ownerTcs; bool creationStarted; CatMe.Onboarding.CreationResult finishedResult;
        string dogSize = PetSize.Medium;
        string PetLabel => string.IsNullOrWhiteSpace(petName) ? (species == Species.Dog ? "Your dog" : "Your cat") : petName.Trim();

        void BeginEarlyCreation()
        {
            if (creationStarted) return;
            creationStarted = true; finishedResult = null; creationStartedAt = Time.realtimeSinceStartup;
            CatMe.Onboarding.Telemetry.Track("creation_started", "species", species == Species.Dog ? "dog" : "cat");
            ownerTcs = new System.Threading.Tasks.TaskCompletionSource<string>();
            if (addMode) ownerTcs.TrySetResult(null);
            else if (personPhoto != null) ownerTcs.TrySetResult(personPhoto);
            ObserveCreation(PetLabel);
        }

        async void ObserveCreation(string who)
        {
            CatMe.Onboarding.CreationResult result;
            try { result = await OnCreate(who, species, petPhoto, ownerTcs.Task, (stage, message) => { if (!string.IsNullOrEmpty(stage)) remoteStage = stage; }); }
            catch (Exception e) { Debug.LogWarning("[WornTag][Creation] " + e.Message); result = new CatMe.Onboarding.CreationResult { error = "Something went wrong. Check your connection and try again." }; }
            creationStarted = false;
            TrackResult(result);
            bool watching = IsOpen && step == "making";   // the player waited on the making screen
            if (result.ok)
            {
                created = PetRoster.Add(PetLabel, species, petPhoto);   // the name chosen while it was being made
                if (species == Species.Dog) created.size = PetSize.Normalize(dogSize);
                created.serverId = result.petId; created.isFixture = result.isFixture; PetRoster.SaveChanges();
                OnCreated?.Invoke(created, result);          // saves the models and loads them into the garden
                if (MakingInGarden) { MakingInGarden = false; OnRevealInGarden?.Invoke(created); }
                else if (watching) ShowReady(created);       // name, then reveal, then the garden
            }
            else if (watching) ShowCreateFailed(result, PetLabel);
            else { MakingInGarden = false; finishedResult = result; OnCreateFailed?.Invoke(string.IsNullOrEmpty(result.error) ? "We couldn't make your pet this time." : result.error); }
        }

        float creationStartedAt;

        // Funnel: how creations end (a reason code, never the photo or the name) and how long a ready pet took.
        void TrackResult(CatMe.Onboarding.CreationResult result)
        {
            string outcome = result.ok ? "ready" : !string.IsNullOrEmpty(result.rejectReason) ? result.rejectReason : "failed";
            CatMe.Onboarding.Telemetry.Track("creation_result", "result", outcome);
            if (result.ok && creationStartedAt > 0f) CatMe.Onboarding.Telemetry.Track("time_to_ready", "seconds", (long)(Time.realtimeSinceStartup - creationStartedAt));
        }

        public void ResumeCreation(PetProfile pet, Func<Action<string, string>, System.Threading.Tasks.Task<CatMe.Onboarding.CreationResult>> resume)
        {
            if (pet == null || resume == null || creationStarted) return;
            accountOnly = false; addMode = false; signedInFlow = true; loginFirst = false;
            created = pet; petName = pet.name; species = pet.Kind; creationStarted = true; finishedResult = null;
            if (OnMakingInGarden != null) { MakingInGarden = true; Hide(); OnMakingInGarden(PetLabel); ObserveResume(resume); return; }
            if (root == null) { var s = UiKit.Screen(canvasRoot, "Onboarding", CatMeUIStyle.Mist); root = s.root; safe = s.safe; }
            root.SetActive(true); root.transform.SetAsLastSibling(); ShowMaking();
            ObserveResume(resume);
        }

        async void ObserveResume(Func<Action<string, string>, System.Threading.Tasks.Task<CatMe.Onboarding.CreationResult>> resume)
        {
            CatMe.Onboarding.CreationResult result;
            try { result = await resume((stage, message) => { if (!string.IsNullOrEmpty(stage)) remoteStage = stage; }); }
            catch (Exception e) { Debug.LogWarning("[WornTag][Creation] " + e.Message); result = new CatMe.Onboarding.CreationResult { error = "Something went wrong. Check your connection and try again." }; }
            creationStarted = false;
            TrackResult(result);
            if (result.ok)
            {
                OnCreated?.Invoke(created, result);
                if (MakingInGarden) { MakingInGarden = false; OnRevealInGarden?.Invoke(created); } else ShowReady(created);
            }
            else if (MakingInGarden) { MakingInGarden = false; OnCreateFailed?.Invoke(string.IsNullOrEmpty(result.error) ? "We couldn't make your pet this time." : result.error); }
            else ShowCreateFailed(result, PetLabel);
        }

        // ---- the making screen: the player waits here until the pet and the person are ready -----------------------------------

        static readonly string[] Stages = { "Checking the photo", "Waiting in line", "Building a 3D copy", "Almost ready" };
        string remoteStage = "checking"; int stageShown = -1;
        RectTransform spinner, companion; Vector2 companionHome; Text[] stageLabels; Image[] stageDots;

        static int StageIndex(string stage)
        {
            switch (stage) { case "queued": return 1; case "generating": return 2; case "downloading": case "ready": return 3; default: return 0; }
        }

        void ShowMaking()
        {
            step = "making"; Clear(); stageShown = -1; remoteStage = "checking";
            // A ring of dots turns around the pet they already have (or the garden cat) while the new one is made.
            var prev = PetRoster.Active;
            var art = prev != null && !string.IsNullOrEmpty(prev.photoPath) ? Art.FromFile(prev.photoPath) : null;
            if (art == null) art = Art.Load(species == Species.Dog ? "GoodDogExample" : "PlaceholderCat") ?? Art.Load("PlaceholderCat");
            companionHome = new Vector2(-400f, 0f);
            companion = UiKit.Rect(safe, "Companion", companionHome, new Vector2(340f, 340f));
            UiKit.RoundPhoto(companion, art, Vector2.zero, 300f, CatMeUIStyle.Pine, "Previous pet");
            spinner = UiKit.Rect(safe, "Spinner", companionHome, new Vector2(480f, 480f));
            for (int i = 0; i < 10; i++)
            {
                float a = i / 10f * Mathf.PI * 2f; float s = 14f + i * 1.6f;
                var d = UiKit.Rect(spinner, "Dot", new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * 230f, new Vector2(s, s));
                var im = d.gameObject.AddComponent<Image>(); im.sprite = UiKit.Circle; im.color = new Color(CatMeUIStyle.Sage.r, CatMeUIStyle.Sage.g, CatMeUIStyle.Sage.b, .25f + .075f * i); im.raycastTarget = false;
            }
            Title(RightX, 250f, PetLabel + " is being made");
            stageLabels = new Text[Stages.Length]; stageDots = new Image[Stages.Length];
            for (int i = 0; i < Stages.Length; i++)
            {
                float y = 100f - i * 70f;
                var dot = UiKit.Rect(safe, "Dot", new Vector2(RightX + 14f, y), new Vector2(22f, 22f)); var di = dot.gameObject.AddComponent<Image>(); di.sprite = UiKit.Circle; di.raycastTarget = false; stageDots[i] = di;
                stageLabels[i] = UiKit.Label(safe, Stages[i], 34, CatMeUIStyle.BodyInk, new Vector2(RightX + 45f + 320f, y), new Vector2(640f, 56f), FontStyle.Normal, TextAnchor.MiddleLeft);
            }
            UiKit.Label(safe, "This takes a few minutes. Your garden opens when it is ready.", 24, CatMeUIStyle.BodyInk, new Vector2(RightX + 400f, -230f), new Vector2(800f, 80f), FontStyle.Normal, TextAnchor.UpperLeft, "Note");
        }

        void ShowCreateFailed(CatMe.Onboarding.CreationResult result, string who)
        {
            step = "failed"; Clear();
            Title(RightX, 220f, "We could not make " + who);
            Sub(RightX, 40f, string.IsNullOrEmpty(result.error) ? "Something went wrong." : result.error, 200f);
            Primary(RightX, -170f, "Try again", () => { failMessage = null; BeginEarlyCreation(); ShowMaking(); }, 470f);
            TextLink(RightX, -290f, "Choose another photo", () => { failMessage = string.IsNullOrEmpty(result.rejectReason) ? null : result.error; Show("photo"); }, 420f);
        }

        void Clear()
        {
            spinner = null; companion = null;
            foreach (Transform c in safe) { c.gameObject.SetActive(false); UnityEngine.Object.Destroy(c.gameObject); }
            ScreenFade.Play(safe);   // every screen is built right after Clear, so it fades in
        }

        // ---- shared pieces -------------------------------------------------------------------------------------------

        void Chrome(string current, bool back = true)
        {
            if (creationStarted || (!addMode && current == "species")) back = false;
            var flow = Flow(); int i = flow.IndexOf(current);
            if (back) UiKit.IconButton(safe, "back", new Vector2(-860f, 440f), GoBack, "Back");
            if (!loginFirst && !creationStarted && addMode) UiKit.IconButton(safe, "close", new Vector2(860f, 440f), () => { Hide(); OnCancelled?.Invoke(); }, "Close");
            if (i >= 0 && !accountOnly) UiKit.Rail(safe, flow.Count, i, new Vector2(0f, -490f));
        }

        void GoBack()
        {
            var flow = Flow(); int i = flow.IndexOf(step);
            if (i <= 0) { Hide(); OnCancelled?.Invoke(); return; }
            failMessage = null; Show(flow[i - 1]);
        }

        void Next()
        {
            var flow = Flow(); int i = flow.IndexOf(step);
            if (step == "photo" && signedInFlow && OnCreate != null) BeginEarlyCreation();   // the pet is made while the selfie is taken
            failMessage = null;
            bool selfieDone = i >= 0 && i + 1 < flow.Count && flow[i + 1] == "person" && personPhoto != null;   // retry with a selfie already taken
            if (i >= 0 && i + 1 < flow.Count && !selfieDone) { Show(flow[i + 1]); return; }
            // Last step done: the garden opens straight away while the pet is made (no waiting screen).
            if (creationStarted)
            {
                if (OnMakingInGarden == null) { ShowMaking(); return; }
                MakingInGarden = true; Hide(); OnMakingInGarden(PetLabel); return;
            }
            Hide();
            OnFinished?.Invoke(PetRoster.Active);
        }

        public Action<string> OnCreateFailed;

        /// <summary>Shared pieces of the design: a big picture card on the left and check-mark tips on the right.</summary>
        RectTransform ArtCard(Sprite sprite, float focusY, bool belowTopBar, float focusX = .5f)
        {
            var size = belowTopBar ? new Vector2(860f, 860f) : new Vector2(880f, 1000f);
            var core = UiKit.Bezel(safe, "Picture card", new Vector2(-470f, belowTopBar ? -60f : 0f), size);
            if (sprite != null) UiKit.CoverFrame(core, sprite, Vector2.zero, core.sizeDelta - new Vector2(30f, 30f), 1f, "Picture", focusY, focusX);
            return core;
        }

        void Tips(float y, params string[] items)
        {
            for (int i = 0; i < items.Length; i++)
            {
                float ty = y - i * 82f;
                var dot = UiKit.Rect(safe, "Tip mark", new Vector2(RightX + 34f, ty), new Vector2(68f, 68f));
                var di = dot.gameObject.AddComponent<Image>(); di.sprite = UiKit.Circle; di.color = CatMeUIStyle.Tile; di.raycastTarget = false;
                UiKit.Icon(dot, "check", Vector2.zero, 36f, CatMeUIStyle.Pine);
                UiKit.Label(safe, items[i], 32, CatMeUIStyle.BodyInk, new Vector2(RightX + 96f + 370f, ty), new Vector2(740f, 70f), FontStyle.Normal, TextAnchor.MiddleLeft, "Tip");
            }
        }

        Text Title(float x, float y, string text) { var t = UiKit.Label(safe, text, 64, CatMeUIStyle.Pine, new Vector2(x + 430f, y), new Vector2(860f, 170f), FontStyle.Bold, TextAnchor.MiddleLeft, "Title"); return t; }
        Text Sub(float x, float y, string text, float h = 110f) => UiKit.Label(safe, text, 34, CatMeUIStyle.BodyInk, new Vector2(x + 360f, y), new Vector2(720f, h), FontStyle.Normal, TextAnchor.UpperLeft, "Subtitle");
        Button Primary(float x, float y, string label, Action a, float w = 470f) => UiKit.NestedButton(safe, label, new Vector2(x + w / 2f, y), w, CatMeUIStyle.SageDeep, a);
        Button Ghost(float x, float y, string label, Action a, float w = 470f)
        {
            var b = UiKit.NestedButton(safe, label, new Vector2(x + w / 2f, y), w, CatMeUIStyle.Tile, a, 32, label, false);
            b.GetComponentInChildren<Text>().color = CatMeUIStyle.Pine; return b;
        }
        Button TextLink(float x, float y, string label, Action a, float w = 360f) => UiKit.Link(safe, label, new Vector2(x + w / 2f, y), new Vector2(w, 70f), a);

        void Show(string s)
        {
            step = s; Clear();
            CatMe.Onboarding.Telemetry.Track("onboarding_step", "step", s);
            switch (s)
            {
                case "species": ShowSpecies(); break;
                case "photo": ShowPetPhoto(); break;
                case "person": ShowPersonPhoto(); break;
                case "name": ShowName(); break;
                case "consent": ShowConsent(); break;
            }
        }

        // ---- species -------------------------------------------------------------------------------------------------

        void ShowSpecies()
        {
            Chrome("species");
            Title(LeftX, 120f, "Who is coming\non the walk?");
            Sub(LeftX, -60f, "Pick one. You can add more pets later.");
            Choice(250f, "Cat", Species.Cat, Art.Load("PlaceholderCat"));
            Choice(700f, "Dog", Species.Dog, Art.Load("GoodDogExample"));
        }

        void Choice(float x, string label, Species s, Sprite photo)
        {
            var card = CatMeUIStyle.Card(safe, label + " card", new Vector2(400f, 620f), CatMeUIStyle.Paper, CatMeUIStyle.Elevation.Raised);
            ((RectTransform)card.transform).anchoredPosition = new Vector2(x, -40f);
            UiKit.RoundPhoto(card.transform, photo ?? Art.Load("CatPawCircleIcon"), new Vector2(0f, 50f), 230f, CatMeUIStyle.Paper, "Photo");
            UiKit.Label(card.transform, label, 46, CatMeUIStyle.Pine, new Vector2(0f, -140f), new Vector2(300f, 70f), FontStyle.Bold);
            var hit = UiKit.Link(card.transform, "", Vector2.zero, new Vector2(400f, 620f), () => { species = s; failMessage = null; Next(); });
            hit.name = label + " choice";
        }

        // ---- pet photo -----------------------------------------------------------------------------------------------

        void ShowPetPhoto()
        {
            Chrome("photo");
            string who = species == Species.Dog ? "dog" : "cat";
            bool bad = failMessage != null;
            var ring = bad ? CatMeUIStyle.Danger : petPhoto != null ? CatMeUIStyle.SageDeep : CatMeUIStyle.Sage;
            Sprite pic = petPhoto != null ? Art.FromFile(petPhoto) : null;
            ArtCard(pic ?? Art.Load(species == Species.Dog ? "GoodDogExample" : "GoodCatExample"), .5f, true);
            Title(RightX, 260f, "Show us your " + who);
            if (bad)
            {
                var card = CatMeUIStyle.Card(safe, "Photo problem", new Vector2(800f, 190f), new Color(.98f, .91f, .88f), CatMeUIStyle.Elevation.Resting);
                ((RectTransform)card.transform).anchoredPosition = new Vector2(RightX + 400f, 40f);
                var thumb = UiKit.CoverFrame(card.transform, Art.Load("GoodCatExample"), new Vector2(-310f, 0f), new Vector2(150f, 150f), 1f, "Good example");
                UiKit.Label(card.transform, failMessage, 30, new Color(.35f, .14f, .09f), new Vector2(80f, 0f), new Vector2(540f, 160f), FontStyle.Bold, TextAnchor.MiddleLeft);
                Primary(RightX, -120f, "Choose another", ChoosePetPhoto, 520f);
                if (!failHard) TextLink(RightX + 560f, -120f, "Use it anyway", () => { failMessage = null; Next(); }, 330f);
            }
            else
            {
                Sub(RightX, 140f, "Take or pick one photo. We use it to build a 3D copy.", 110f);
                // The whole animal builds a better 3D pet than a face close-up (launch task L8).
                Tips(30f, PetPhotoTips);
                Primary(RightX, -250f, petPhoto != null ? "Choose another" : "Choose a photo", ChoosePetPhoto, 520f);
                TextLink(RightX, -360f, "Take a photo instead", ChoosePetPhoto, 360f);
            }
        }

        // The first photo waits for consent: the consent screen comes first, then the picker opens.
        void ChoosePetPhoto()
        {
            if (!CatMe.Onboarding.CreationClient.ConsentGiven) { Show("consent"); return; }
            PickPetPhoto();
        }

        // ---- consent: shown once (per consent version) before the first photo is chosen --------------------------------

        void ShowConsent()
        {
            Chrome("consent", false);
            ArtCard(Art.Load(species == Species.Dog ? "GoodDogExample" : "GoodCatExample"), .5f, true);
            Title(RightX, 285f, "Before we make your pet");
            Tips(165f, "Your photos go to our 3D partners to build your pet and you",
                       "They stay private to your account",
                       "Delete them any time in Settings",
                       "Only use photos of your own pet and yourself");
            UiKit.Label(safe, "Tripo makes pets, Meshy makes people and OpenAI checks that photos are safe. The 3D characters are made by AI and may not look exactly like the photo. By agreeing you confirm you are 13 or older.",
                22, CatMeUIStyle.BodyInk, new Vector2(RightX + 400f, -190f), new Vector2(800f, 92f), FontStyle.Normal, TextAnchor.UpperLeft, "Consent detail");
            Primary(RightX, -285f, "I agree", () => { CatMe.Onboarding.CreationClient.RecordConsent(); Show("photo"); PickPetPhoto(); }, 520f);
            TextLink(RightX + 540f, -285f, "Not now", () => Show("photo"), 240f);
            TextLink(RightX, -385f, "Privacy policy", () => Application.OpenURL(CatMe.Onboarding.CreationClient.PrivacyUrl), 300f);
            TextLink(RightX + 320f, -385f, "Terms and AI policy", () => Application.OpenURL(CatMe.Onboarding.CreationClient.TermsUrl), 400f);
        }

        void PickPetPhoto()
        {
            photos.RequestPhoto(false, (path, message) =>
            {
                failHard = false;
                if (path == null) { failMessage = string.IsNullOrEmpty(message) ? null : message; Show("photo"); return; }
                petPhoto = path;
                string animal = PetCheckMessage(photos.LastPetCheck, out failHard);
                if (animal != null) { failMessage = animal; Show("photo"); return; }
                var check = ValidatePath(path, false);
                if (check.ok) { failMessage = null; Next(); } else { failMessage = check.message; Show("photo"); }
            }, null, false, true);
        }

        // The phone's own cat-or-dog check (Apple Vision, CatMePhotoPicker.mm). The server checks again before anything is made.
        string PetCheckMessage(string seen, out bool hard)
        {
            hard = false;
            string want = species == Species.Dog ? "dog" : "cat", other = species == Species.Dog ? "cat" : "dog";
            switch (seen)
            {
                case null: return null;                          // not checked (editor)
                case "many": hard = true; return "There is more than one in this photo. Try one with only your pet.";
                case "none": return "We can't see a " + want + " in this photo. Try another one.";
                default:
                    if (seen == want) return null;
                    hard = true; return "This looks like a " + other + ". Choose a photo of your " + want + ", or go back and pick " + (other == "cat" ? "Cat" : "Dog") + ".";
            }
        }

        PhotoCheck ValidatePath(string path, bool person)
        {
            try
            {
                var tex = new Texture2D(2, 2);
                tex.LoadImage(File.ReadAllBytes(path));
                var result = Validator.Validate(tex, species, person);
                UnityEngine.Object.Destroy(tex);
                return result;
            }
            catch (Exception) { return PhotoCheck.Pass(); }
        }

        // ---- sign-in: the login page is the only sign-in screen (first launch, and Account in the pause menu) ----

        public Action<string> OnProvider;                       // "guest", "apple", "google"
        string emailError;                                      // last sign-in error shown under the buttons
        bool signingIn;       // a tapped sign-in is in progress: the restored-session shortcut in Tick must not jump ahead of it
        void Provider(string provider)
        {
            signingIn = true;
            if (OnProvider != null) OnProvider(provider);
            else AccountFailed("Account services are not connected yet.");
        }
        // Account screen opened from the pause menu: no pet steps, closes when done.
        bool accountOnly;
        public Action OnAccountDone;
        public void BeginAccountOnly()
        {
            accountOnly = true; loginFirst = false; emailError = null;
            if (root == null) { var s = UiKit.Screen(canvasRoot, "Onboarding", CatMeUIStyle.Mist); root = s.root; safe = s.safe; }
            root.SetActive(true); root.transform.SetAsLastSibling(); ShowLogin();
        }

        /// <summary>Account screen for a person who is already signed in: who they are, and a way to sign out.</summary>
        /// <summary>What the Account screen shows (MENUS_AND_ACCOUNT.md section 4).</summary>
        public sealed class AccountView
        {
            public bool guest;
            public string identityLine;   // "Signed in with Google · ja…@gmail.com"
            public string petsLine;       // "Mochi and Miso are saved to your account."
            public Sprite picture;
            public Action saveWithGoogle, signOut, deletePhotos, deleteAccount, privacy, terms;
            public string version;
        }

        /// <summary>Account, in two states: a guest (save the pets with Google) or a signed-in player (who you are, sign out). Both end with "Your data".</summary>
        public void ShowAccount(AccountView v)
        {
            accountOnly = true;
            if (root == null) { var s = UiKit.Screen(canvasRoot, "Onboarding", CatMeUIStyle.Mist); root = s.root; safe = s.safe; }
            root.SetActive(true); root.transform.SetAsLastSibling(); step = "account"; Clear();
            UiKit.IconButton(safe, "close", new Vector2(860f, 440f), () => { Hide(); OnAccountDone?.Invoke(); }, "Back to the garden");
            if (v.picture != null)
            {
                var core = UiKit.Bezel(safe, "Picture card", new Vector2(500f, -40f), new Vector2(700f, 700f));
                UiKit.CoverFrame(core, v.picture, Vector2.zero, core.sizeDelta - new Vector2(30f, 30f), 1f, "Picture", .5f, .5f);
            }
            if (v.guest)
            {
                Title(LeftX, 330f, "You're playing as a guest");
                Sub(LeftX, 225f, "Your pets live only on this phone. Save them to keep them if you get a new phone.", 110f);
                Primary(LeftX, 70f, "Save my pets with Google", v.saveWithGoogle, 560f);
                TextLink(LeftX, -35f, "Sign out of this guest account", v.signOut, 520f);
            }
            else
            {
                Title(LeftX, 330f, "Your account");
                Sub(LeftX, 225f, v.identityLine, 60f);
                if (!string.IsNullOrEmpty(v.petsLine)) Sub(LeftX, 160f, v.petsLine, 60f);
                Ghost(LeftX, 50f, "Sign out", v.signOut, 420f);
            }
            // Your data: separated by a hairline, the two destructive actions and the policies.
            var line = UiKit.Rect(safe, "Divider", new Vector2(LeftX + 430f, -110f), new Vector2(860f, 2f));
            var li = line.gameObject.AddComponent<Image>(); li.color = CatMeUIStyle.Hairline; li.raycastTarget = false;
            UiKit.Label(safe, "Your data", 34, CatMeUIStyle.Ink, new Vector2(LeftX + 430f, -160f), new Vector2(860f, 50f), FontStyle.Bold, TextAnchor.MiddleLeft, "Your data");
            DangerButton(LeftX, -250f, "Delete my photos", v.deletePhotos, 410f);
            DangerButton(LeftX + 450f, -250f, "Delete account", v.deleteAccount, 410f);
            TextLink(LeftX, -360f, "Privacy policy", v.privacy, 260f);
            TextLink(LeftX + 280f, -360f, "Terms and AI policy", v.terms, 330f);
            UiKit.Label(safe, v.version ?? "", 24, CatMeUIStyle.MutedInk, new Vector2(LeftX + 760f, -360f), new Vector2(220f, 50f), FontStyle.Normal, TextAnchor.MiddleRight, "Version");
        }

        // A light button with Danger text: never the biggest button on the screen, and it always confirms first.
        Button DangerButton(float x, float y, string label, Action a, float w)
        {
            var b = Ghost(x, y, label, a, w);
            var t = b.GetComponentInChildren<Text>(); if (t != null) t.color = CatMeUIStyle.Danger;
            return b;
        }

        public void AccountSucceeded()
        {
            emailError = null; signedInFlow = true; signingIn = false;
            if (loginFirst) { loginFirst = false; Show("species"); return; }
            if (accountOnly) { accountOnly = false; Hide(); OnAccountDone?.Invoke(); }
        }
        public void AccountFailed(string message)
        {
            emailError = message; signingIn = false;
            ShowLogin();
        }

        Button ProviderButton(string icon, string label, Vector2 pos, float width, Color tint, Color textColor, Action action, string name)
        {
            var b = UiKit.Button(safe, label, pos, new Vector2(width, 100f), tint, false, action, 32, name);
            var t = b.GetComponentInChildren<Text>(); t.color = textColor; t.alignment = TextAnchor.MiddleLeft;
            t.rectTransform.sizeDelta = new Vector2(width - 160f, 100f); t.rectTransform.anchoredPosition = new Vector2(40f, 0f);
            UiKit.Icon(b.transform, icon, new Vector2(-width / 2f + 62f, 0f), 44f, textColor);
            return b;
        }

        // ---- first page: sign in or create an account, with the hero on the left ---------------------------------------

        bool loginFirst;
        public Func<bool> IsSignedIn;
        public Action OnAlreadySignedIn;

        public void BeginLogin()
        {
            accountOnly = false; addMode = false; loginFirst = true; emailError = null;
            if (root == null) { var s = UiKit.Screen(canvasRoot, "Onboarding", CatMeUIStyle.Mist); root = s.root; safe = s.safe; }
            root.SetActive(true); root.transform.SetAsLastSibling();
            ShowLogin();
        }

        /// <summary>Turn on once the paid Apple team and Sign in with Apple work on the phone.</summary>
        public static bool AppleSignInAvailable = false;

        void ShowLogin()
        {
            step = "login"; Clear();
            ArtCard(Art.Load("WelcomeGardenHero"), .5f, false, 1f);
            if (accountOnly) UiKit.IconButton(safe, "close", new Vector2(860f, 470f), () => { accountOnly = false; Hide(); OnAccountDone?.Invoke(); }, "Close");   // keep the walker and the cat, who are on the right of the painting
            const float x = RightX + 310f, w = 620f;
            // Wordmark (no dot), title, one line of 20 words or fewer with the reassurance, then the buttons; nothing under them.
            UiKit.Label(safe, "WornTag", 36, CatMeUIStyle.Ink, new Vector2(RightX + 100f, 390f), new Vector2(200f, 50f), FontStyle.Bold, TextAnchor.MiddleLeft, "Brand");
            Title(RightX, 250f, "Your pet, in a garden,\nwith you.");
            Sub(RightX, 70f, "We turn a photo of your pet into a little 3D pet. Sign in to keep it on a new phone.", 120f);
            float y = -90f;
            // Apple appears only once Sign in with Apple works (paid Apple team).
            if (AppleSignInAvailable) { ProviderButton("apple", "Continue with Apple", new Vector2(x, y), w, new Color(.118f, .102f, .094f), Color.white, () => Provider("apple"), "Apple"); y -= 120f; }
            ProviderButton("google", "Continue with Google", new Vector2(x, y), w, CatMeUIStyle.Paper, CatMeUIStyle.Ink, () => Provider("google"), "Google");
            var free = UiKit.Button(safe, "Try it free", new Vector2(x, y - 115f), new Vector2(w, 84f), CatMeUIStyle.Mist, false, () => Provider("guest"), 30, "Try it free");
            free.GetComponentInChildren<Text>().color = CatMeUIStyle.SageDeep;
            TextLink(LeftX, -445f, "Privacy policy", () => Application.OpenURL(CatMe.Onboarding.CreationClient.PrivacyUrl), 300f);
            TextLink(LeftX + 320f, -445f, "Terms and AI policy", () => Application.OpenURL(CatMe.Onboarding.CreationClient.TermsUrl), 400f);
            if (!string.IsNullOrEmpty(emailError)) UiKit.Label(safe, emailError, 24, CatMeUIStyle.Danger, new Vector2(x, y - 210f), new Vector2(w, 56f), FontStyle.Bold, TextAnchor.MiddleLeft, "Account error");
        }

        // ---- owner photo ---------------------------------------------------------------------------------------------

        void ShowPersonPhoto()
        {
            Chrome("person");
            bool bad = personFailMessage != null;
            ArtCard(personPhoto != null ? Art.FromFile(personPhoto) : Art.Load("PersonPhotoExample"), 0f, true);
            Title(RightX, 280f, "Now show us you");
            if (bad)
            {
                var card = CatMeUIStyle.Card(safe, "Photo problem", new Vector2(800f, 190f), new Color(.98f, .91f, .88f), CatMeUIStyle.Elevation.Resting);
                ((RectTransform)card.transform).anchoredPosition = new Vector2(RightX + 400f, 60f);
                UiKit.CoverFrame(card.transform, Art.Load("PersonPhotoExample"), new Vector2(-310f, 0f), new Vector2(150f, 150f), 1f, "Good example");
                UiKit.Label(card.transform, personFailMessage, 30, new Color(.35f, .14f, .09f), new Vector2(80f, 0f), new Vector2(540f, 160f), FontStyle.Bold, TextAnchor.MiddleLeft);
            }
            else
            {
                Sub(RightX, 170f, "One happy photo of your face. We make a little 3D you to walk with your pet.", 110f);
                Tips(50f, "Your face is clear", "There is lots of light", "Only you are in the photo");
            }
            Primary(RightX, -250f, personPhoto != null || bad ? "Take another selfie" : "Take a selfie", () => PickPersonPhoto(true), 520f);
            // Selfie only: the camera is the one way in. If camera access was refused, the player can turn it on in Settings.
            if (personFailMessage == DeviceBridge.CameraDeniedMessage) TextLink(RightX, -345f, "Open Settings", DeviceBridge.OpenAppSettings, 420f);
            UiKit.Label(safe, "Your photos are private. You can delete them any time in Settings.", 24, CatMeUIStyle.BodyInk, new Vector2(RightX + 400f, -425f), new Vector2(800f, 44f), FontStyle.Normal, TextAnchor.MiddleLeft, "Privacy note");
        }

        // The phone checks for exactly one face and keeps only the head and shoulders (DeviceBridge, CatMePhotoPicker.mm).
        void PickPersonPhoto(bool selfieCamera)
        {
            photos.RequestPhoto(true, (path, message) =>
            {
                if (path == null) { personFailMessage = string.IsNullOrEmpty(message) ? personFailMessage : message; Show("person"); return; }
                personFailMessage = null; personPhoto = path; ownerTcs?.TrySetResult(path); Next();
            }, null, selfieCamera);
        }

        // ---- name ----------------------------------------------------------------------------------------------------

        void ShowName()
        {
            if (!namingReady) Chrome("name");
            Title(LeftX, 300f, "What is their name?");
            Sub(LeftX, 180f, "Pick a name. You can change it later.", 80f);
            var box = CatMeUIStyle.Card(safe, "Name field", new Vector2(700f, 120f), CatMeUIStyle.Paper, CatMeUIStyle.Elevation.Resting);
            ((RectTransform)box.transform).anchoredPosition = new Vector2(LeftX + 350f, 70f);
            var text = UiKit.Label(box.transform, "", 48, CatMeUIStyle.Pine, Vector2.zero, new Vector2(640f, 90f), FontStyle.Bold, TextAnchor.MiddleLeft);
            var hint = UiKit.Label(box.transform, "Name", 48, new Color(.55f, .63f, .59f), Vector2.zero, new Vector2(640f, 90f), FontStyle.Bold, TextAnchor.MiddleLeft);
            string[] ideas = species == Species.Dog ? new[] { "Biscuit", "Juniper", "Waffles" } : new[] { "Mochi", "Juniper", "Miso" };
            if (string.IsNullOrWhiteSpace(petName)) petName = ideas[0];   // a suggested name is selected by default
            nameField = box.AddComponent<InputField>(); nameField.textComponent = text; nameField.placeholder = hint; nameField.characterLimit = PetRoster.NameMaxLength; nameField.text = petName;
            nameField.onValueChanged.AddListener(v => petName = v);
            for (int i = 0; i < ideas.Length; i++)
            {
                string n = ideas[i];
                var chip = UiKit.Button(safe, n, new Vector2(LeftX + 110f + i * 230f, -50f), new Vector2(210f, 66f), CatMeUIStyle.Tile, false, () => { petName = n; if (nameField != null) nameField.text = n; }, 28, "Idea " + n);
            }
            Primary(LeftX, -190f, "That's their name", () =>
            {
                petName = PetRoster.CleanName(petName) ?? (species == Species.Dog ? "Your dog" : "Your cat");
                if (!namingReady) { Next(); return; }
                created.name = petName.Trim(); PetRoster.SaveChanges(); namingReady = false; ShowReveal();
            }, 520f);
            UiKit.RoundPhoto(safe, petPhoto != null ? Art.FromFile(petPhoto) : Art.Load(species == Species.Dog ? "GoodDogExample" : "PlaceholderCat"), new Vector2(480f, 30f), 400f, CatMeUIStyle.SageDeep, "Pet photo");
            if (species == Species.Dog && !namingReady)
            {
                // Dog size sets how big their 3D dog is shown and how deep their bark is (F3).
                UiKit.Label(safe, "How big are they?", 28, CatMeUIStyle.BodyInk, new Vector2(LeftX + 350f, -330f), new Vector2(700f, 44f), FontStyle.Bold, TextAnchor.MiddleLeft, "Size title");
                string[] sizes = { PetSize.Small, PetSize.Medium, PetSize.Large }; string[] labels = { "Small", "Medium", "Large" };
                for (int i = 0; i < 3; i++)
                {
                    string size = sizes[i]; bool on = PetSize.Normalize(dogSize) == size;
                    var chip = UiKit.Button(safe, labels[i], new Vector2(LeftX + 110f + i * 230f, -400f), new Vector2(210f, 66f), on ? CatMeUIStyle.SageDeep : CatMeUIStyle.Mist, on, () => { dogSize = size; Show("name"); }, 28, "Size " + labels[i]);
                    if (!on) chip.GetComponentInChildren<Text>().color = CatMeUIStyle.SageDeep;
                }
            }
        }

        /// <summary>Call every frame: a saved sign-in that is restored after launch skips the login page.</summary>
        public void Tick()
        {
            if (loginFirst && !signingIn && IsOpen && IsSignedIn != null && IsSignedIn()) { loginFirst = false; Hide(); OnAlreadySignedIn?.Invoke(); return; }
            if (step != "making" || !IsOpen) return;
            if (spinner != null) spinner.localEulerAngles = new Vector3(0f, 0f, -Time.unscaledTime * 200f);
            if (companion != null) companion.anchoredPosition = companionHome + new Vector2(0f, Mathf.Sin(Time.unscaledTime * 3f) * 14f);
            int now = StageIndex(remoteStage);
            if (now != stageShown && stageLabels != null)
            {
                stageShown = now;
                for (int i = 0; i < Stages.Length; i++)
                {
                    stageLabels[i].color = i == now ? CatMeUIStyle.Ink : i < now ? CatMeUIStyle.Pine : new Color(.49f, .56f, .52f);
                    stageLabels[i].fontStyle = i == now ? FontStyle.Bold : FontStyle.Normal;
                    stageDots[i].color = i == now ? CatMeUIStyle.SageDeep : i < now ? CatMeUIStyle.Sage : new Color(.153f, .137f, .129f, .18f);
                }
            }
        }

        // ---- reveal --------------------------------------------------------------------------------------------------

        bool namingReady;

        /// <summary>The pet is ready (the player is in the garden): ask its name, then show it.</summary>
        public void ShowReady(PetProfile pet)
        {
            if (pet == null) return;
            accountOnly = false; loginFirst = false; created = pet; species = pet.Kind; petPhoto = pet.photoPath;
            petName = pet.name != null && pet.name.StartsWith("Your ") ? "" : pet.name;
            if (root == null) { var s = UiKit.Screen(canvasRoot, "Onboarding", CatMeUIStyle.Mist); root = s.root; safe = s.safe; }
            root.SetActive(true); root.transform.SetAsLastSibling();
            namingReady = true; step = "name"; Clear(); ShowName();
        }

        void ShowReveal()
        {
            step = "reveal"; Clear();
            if (created == null) created = PetRoster.Active;
            var art = Art.Load(created.Kind == Species.Cat ? "CatReveal" : "DogReveal");
            ArtCard(art ?? Art.FromFile(created.photoPath) ?? Art.Load("PlaceholderCat"), .5f, false);
            Title(RightX, 150f, "Look who is here!\nMeet " + created.name);
            Sub(RightX, -10f, "Say hello to " + created.name + ". You can walk together now.");
            Primary(RightX, -170f, "Say hello", () => { Hide(); OnFinished?.Invoke(created); }, 520f);
        }
    }

    /// <summary>A short fade-in for each onboarding screen. Reduce motion shows the screen at once. Runs on unscaled time,
    /// because the game is paused while onboarding is open.</summary>
    internal sealed class ScreenFade : MonoBehaviour
    {
        const float Seconds = .22f;
        CanvasGroup group;
        float started;

        public static void Play(RectTransform target)
        {
            if (target == null) return;
            var fade = target.GetComponent<ScreenFade>();
            if (fade == null) fade = target.gameObject.AddComponent<ScreenFade>();
            fade.Begin();
        }

        void Begin()
        {
            if (group == null) group = GetComponent<CanvasGroup>();
            if (group == null) group = gameObject.AddComponent<CanvasGroup>();
            // Batch renders (Editor/UiCapture) build screens outside Play Mode and capture them at once.
            bool reduced = !Application.isPlaying || PlayerPrefs.GetInt("CatMe.ReduceMotion", 0) == 1;
            group.alpha = reduced ? 1f : 0f;
            started = Time.unscaledTime;
            enabled = !reduced;
        }

        void Update()
        {
            float t = Mathf.Clamp01((Time.unscaledTime - started) / Seconds);
            group.alpha = 1f - (1f - t) * (1f - t);   // ease out
            if (t >= 1f) enabled = false;
        }

        // A screen hidden mid-fade must never come back half transparent.
        void OnDisable() { if (group != null) group.alpha = 1f; }
    }
}
