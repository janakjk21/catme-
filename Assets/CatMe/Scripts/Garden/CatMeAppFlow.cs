using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using CatMe.Onboarding;
using CatMe.Pets;

namespace CatMe.Garden
{
    /// <summary>First-launch, first-session and settings layouts for the garden app.</summary>
    internal sealed class CatMeAppFlow
    {
        private const string FirstSessionKey = "CatMe.FirstGardenSessionComplete";
        private const string SoundKey = "CatMe.SoundEnabled";
        private const string GuidanceKey = "CatMe.VisualGuidanceEnabled";
        private const string StoryKey = "CatMe.Journey.StoryEnabled";

        private readonly Transform canvasRoot;
        private readonly GardenCompanionDemo garden;
        private readonly DeviceBridge device;
        private GameObject welcome, intro, search, reunion, walkTip, ballTip, settings;
        private readonly List<Image> settingTracks = new List<Image>();
        private readonly List<Image> settingKnobs = new List<Image>();
        private readonly LocalPlayerProfile profile;
        private readonly TutorialController tutorial;
        private GameObject guide;
        private Text guideHint;
        private Button guideAction, guideLater, guideHelp;
        private float guideTime;
        private bool guideHelpShown;
        
        internal CatMeAppFlow(Transform canvasRoot, GardenCompanionDemo garden, DeviceBridge device)
        {
            this.canvasRoot = canvasRoot;
            this.garden = garden;
            this.device = device;
            profile = new LocalPlayerProfile(Application.persistentDataPath, PlayerPrefs.GetInt(FirstSessionKey, 0) == 1);
            AccountStorage.Changed += OnAccountChanged;
            tutorial = new TutorialController(profile);
            garden.OwnerMoved += tutorial.OwnerMoved;
            garden.CallAccepted += tutorial.CallRequested;
            garden.CatArrived += tutorial.CatArrived;
            garden.CallUnavailable += tutorial.CallUnavailable;
            garden.BallReleased += tutorial.BallReleased;
            garden.BallFinished += tutorial.BallFinished;
            tutorial.Changed += RefreshGuide;
            tutorial.Finished += FinishFirstSession;
            BuildWelcome();
            BuildSessionSheets();
            BuildSettings();
            AddGardenMenuButton();
            SetGameplayControlsVisible(false);
        }

        private CatMeAppFlow(Transform canvasRoot)
        {
            this.canvasRoot = canvasRoot;
            BuildSettings();
            settings.SetActive(true);
        }

        internal static void ShowSettingsPreview(Transform canvasRoot) => new CatMeAppFlow(canvasRoot);

        private void BuildWelcome()
        {
            welcome = Overlay("Welcome screen");
            welcome.GetComponent<Image>().color = CatMeUIStyle.Mist;
            // Only a holding screen while the garden loads: the first page is the login (OnboardingFlow.BeginLogin), not this one.
            var dot = UiKit.Rect(welcome.transform, "Brand dot", new Vector2(-92f, 24f), new Vector2(22f, 22f)); var dimg = dot.gameObject.AddComponent<Image>(); dimg.sprite = UiKit.Circle; dimg.color = CatMeUIStyle.Sunset; dimg.raycastTarget = false;
            UiKit.Label(welcome.transform, "WornTag", 48, CatMeUIStyle.Pine, new Vector2(20f, 24f), new Vector2(260f, 60f), FontStyle.Bold, TextAnchor.MiddleLeft, "Brand");
            UiKit.Label(welcome.transform, "Getting your garden ready", 30, CatMeUIStyle.BodyInk, new Vector2(0f, -50f), new Vector2(700f, 60f), FontStyle.Normal, TextAnchor.MiddleCenter, "Loading line");
            if (!string.IsNullOrEmpty(profile.Notice)) Label(welcome.transform, "Save notice", profile.Notice, 22, CatMeUIStyle.Ink, new Vector2(0, -470f), new Vector2(1250, 60), FontStyle.Normal);

            pets = new PetsScreen(canvasRoot); store = new StoreScreen(canvasRoot); onboarding = new OnboardingFlow(canvasRoot, device);
            Confirm = new ConfirmDialog(canvasRoot); PetToast = Toast.Create(canvasRoot); State = new ScreenState(canvasRoot);
            pets.OnAddPet = () => { pets.Hide(); StartOnboarding(true); };
            pets.OnOpenStore = () => { pets.Hide(); store.Open(); };
            pets.OnPetChosen = pet => garden.SetActivePet(pet);
            pets.OnClosed = () => { if (!welcome.activeSelf) CloseOverlayScreen(); };
            store.OnClosed = () => { if (!welcome.activeSelf) CloseOverlayScreen(); };
            onboarding.OnCancelled = () => { if (onboardingFromWelcome) { welcome.SetActive(true); welcome.transform.SetAsLastSibling(); } else CloseOverlayScreen(); };
            onboarding.OnFinished = pet => { Time.timeScale = 1f; garden.SetActivePet(pet); EnterGarden(); };
            onboarding.OnCreateFailed = message => PetToast?.Show(message, "Try again", () => StartOnboarding(false), 30f);
            var identity = new MobileIdentityService(device.AccountService); identityForAccount = identity;
            var apiConfig = Resources.Load<MobileApiConfig>("MobileApiConfig");
            IMobileApi api = apiConfig != null ? new MobileApi(apiConfig, identity) : null;
            mobileApi = api;
            privacyApi = api as IPrivacyApi;
            onboarding.IsSignedIn = () => identityForAccount != null && !string.IsNullOrEmpty(identityForAccount.UserId);
            onboarding.OnAlreadySignedIn = RouteSignedIn;
            // Sign-in: guest ("Try it free"), Google and Apple (Apple needs a paid Apple developer team). Every success opens a server session before going on.
            async System.Threading.Tasks.Task OpenSessionAsync()
            {
                if (api == null) throw new InvalidOperationException("The account server is not configured.");
                AccountStorage.UseAccount(identity.UserId);   // this account's own folder before anything is restored into it
                var session = await api.OpenSessionAsync();
                await RestoreAccountAsync(api, session);
                onboarding.AccountSucceeded();
            }
            onboarding.OnProvider = async provider =>
            {
                try
                {
                    await EnsureAccountReadyAsync();
                    if (provider == "guest") { await identity.ContinueAsGuestAsync(); await OpenSessionAsync(); }
                    else if (provider == "apple")
                        device.SignInWithApple(async error =>
                        {
                            if (error != null) { onboarding.AccountFailed(error); return; }
                            try { await OpenSessionAsync(); }
                            catch (Exception ex) { Debug.LogWarning("[WornTag][Account] session after Apple failed: " + ex.Message); onboarding.AccountFailed(ex.Message); }
                        });
                    else if (provider == "google")
                        device.SignInWithGoogle(async error =>
                        {
                            if (error == "Sign-in cancelled.") { onboarding.AccountFailed(null); return; }
                            if (error != null) { onboarding.AccountFailed(error); return; }
                            try { await OpenSessionAsync(); }
                            catch (Exception ex) { Debug.LogWarning("[WornTag][Account] session after Google failed: " + ex.Message); onboarding.AccountFailed(ex.Message); }
                        });
                    else throw new InvalidOperationException("That sign-in is not available yet. Tap Try it free for now.");
                }
                catch (Exception ex) { Debug.LogWarning("[WornTag][Account] " + provider + " failed: " + ex.GetType().Name + ": " + ex.Message); onboarding.AccountFailed(ex is InvalidOperationException ? ex.Message : "Sign-in is unavailable. Check your connection."); }
            };
            onboarding.OnCreate = (name, species, photo, ownerTask, report) =>
            {
                if (api == null) return System.Threading.Tasks.Task.FromResult(new CreationResult { error = "The account server is not configured." });
                string cache = AccountStorage.Downloads;
                return new CreationClient(api).RunAsync(new CreationRequest
                {
                    petName = name, species = species == Species.Dog ? "dog" : "cat", petPhotoPath = photo, ownerPhotoTask = ownerTask,
                    petModelDestination = species == Species.Dog ? null : System.IO.Path.Combine(cache, "pet-" + System.Guid.NewGuid().ToString("N") + ".glb"),
                    ownerModelDestination = System.IO.Path.Combine(cache, "owner.glb"),
                }, report);
            };
            onboarding.OnCreated = (pet, result) =>
            {
                try
                {
                    string cache = AccountStorage.Downloads;
                    var newest = System.IO.Directory.Exists(cache) ? new System.IO.DirectoryInfo(cache).GetFiles("pet-*.glb") : new System.IO.FileInfo[0];
                    System.Array.Sort(newest, (x, y) => y.LastWriteTimeUtc.CompareTo(x.LastWriteTimeUtc));
                    if (result.petModelSaved && newest.Length > 0 && pet.Kind == Species.Cat)
                    {
                        string own = System.IO.Path.Combine(PetRoster.PetFolder(pet.id), "model.glb");
                        System.IO.Directory.CreateDirectory(System.IO.Path.GetDirectoryName(own));
                        System.IO.File.Copy(newest[0].FullName, own, true);          // every cat keeps its own model
                        pet.modelPath = own; PetRoster.SaveChanges();
                    }
                    string ownerCache = System.IO.Path.Combine(cache, "owner.glb");
                    // A stand-in owner model matches the one already bundled, which also carries the call and sit clips, so only real models replace it.
                    if (result.ownerModelSaved && !result.isFixture && System.IO.File.Exists(ownerCache))
                        System.IO.File.Copy(ownerCache, DeviceBridge.HumanModelPath, true);
                }
                catch (Exception e) { Debug.LogWarning("[WornTag][Creation] could not place the new model: " + e.Message); }
                garden.SetActivePet(pet);
                if (result.ownerModelSaved && !result.isFixture) garden.StartCoroutine(garden.ReloadCharactersFromPhoneStorage());   // a real owner model replaced the bundled one
                if (!onboarding.IsOpen) PetToast?.Show(pet.name + " is ready. Come and say hello.", "Meet " + pet.name, () => OpenOverlayScreen(() => onboarding.ShowReady(pet)), 30f);
            };
            // First page: the login. Someone already signed in goes straight on: to the garden if they have a pet, otherwise to choosing one.
            if (!onboarding.IsSignedIn())
            {
                welcome.SetActive(false);
                onboardingFromWelcome = false;
                onboarding.BeginLogin();
            }
            else RouteSignedIn();
        }

        private async System.Threading.Tasks.Task RestoreAccountAsync(IMobileApi api, MobileSession session)
        {
            if (session == null) return;
            foreach (var serverPet in session.pets ?? new CreationStatus[0])
            {
                var local = PetRoster.AddServerPet(serverPet.id, serverPet.name, serverPet.species);
                if (local == null || serverPet.status != "ready" || (!string.IsNullOrEmpty(local.modelPath) && System.IO.File.Exists(local.modelPath))) continue;
                try
                {
                    var current = await api.GetPetAsync(serverPet.id);
                    if (current?.status != "ready" || string.IsNullOrEmpty(current.modelUrl)) continue;
                    string destination = System.IO.Path.Combine(PetRoster.PetFolder(local.id), "model.glb");
                    await api.DownloadFileAsync(current.modelUrl, destination, null);
                    local.modelPath = destination; local.isFixture = current.isFixture; PetRoster.SaveChanges();
                    needsCharacterReload = true;
                }
                catch (Exception e) { Debug.LogWarning("[WornTag][Restore] could not restore " + local.name + ": " + e.Message); }
            }
            if (session.owner?.status == "ready" && !System.IO.File.Exists(DeviceBridge.HumanModelPath))
            {
                try
                {
                    var owner = await api.GetOwnerAsync();
                    if (owner?.status == "ready" && !string.IsNullOrEmpty(owner.modelUrl)) { await api.DownloadFileAsync(owner.modelUrl, DeviceBridge.HumanModelPath, null); needsCharacterReload = true; }
                }
                catch (Exception e) { Debug.LogWarning("[WornTag][Restore] could not restore the owner: " + e.Message); }
            }
        }

        /// <summary>Shared moments the server flow will drive: delete confirmations, "pet is ready", loading/offline/failed screens.</summary>
        internal ConfirmDialog Confirm { get; private set; }
        internal Toast PetToast { get; private set; }
        internal ScreenState State { get; private set; }
        internal void NotifyPetReady(string petName) => PetToast?.Show(petName + " is ready", "Meet " + petName, OpenPets);
        internal void ShowModelLoadFailure(string name, Action retry) => PetToast?.Show("We couldn't load " + name + ".", "Try again", retry, 30f);

        internal async void RetryPetModel(PetProfile pet)
        {
            if (pet == null || string.IsNullOrEmpty(pet.serverId) || mobileApi == null) return;
            try
            {
                var current = await mobileApi.GetPetAsync(pet.serverId);
                if (current?.status != "ready" || string.IsNullOrEmpty(current.modelUrl)) throw new InvalidOperationException("The model is not ready to download.");
                string destination = System.IO.Path.Combine(PetRoster.PetFolder(pet.id), "model.glb");
                await mobileApi.DownloadFileAsync(current.modelUrl, destination, null);
                pet.modelPath = destination; PetRoster.SaveChanges();
                garden.StartCoroutine(garden.ReloadCharactersFromPhoneStorage());
            }
            catch (Exception e) { ShowModelLoadFailure(pet.name, () => RetryPetModel(pet)); Debug.LogWarning("[WornTag][Restore] pet retry failed: " + e.Message); }
        }

        internal async void RetryOwnerModel()
        {
            if (mobileApi == null) return;
            try
            {
                var owner = await mobileApi.GetOwnerAsync();
                if (owner?.status != "ready" || string.IsNullOrEmpty(owner.modelUrl)) throw new InvalidOperationException("The model is not ready to download.");
                await mobileApi.DownloadFileAsync(owner.modelUrl, DeviceBridge.HumanModelPath, null);
                garden.StartCoroutine(garden.ReloadCharactersFromPhoneStorage());
            }
            catch (Exception e) { ShowModelLoadFailure("you", RetryOwnerModel); Debug.LogWarning("[WornTag][Restore] owner retry failed: " + e.Message); }
        }

        /// <summary>Waits for Firebase to start (retrying once if it failed) and explains the real reason if it cannot.</summary>
        private async System.Threading.Tasks.Task EnsureAccountReadyAsync()
        {
            var account = device.AccountService;
            if (!account.IsReady) await account.RetryAsync();
            if (!account.IsReady) throw new InvalidOperationException(account.LastError ?? "Account services are still starting. Try again in a moment.");
        }

        private PetsScreen pets;
        private StoreScreen store;
        private OnboardingFlow onboarding;
        private bool onboardingFromWelcome, enterWhenReady;

        /// <summary>After login: a person with a pet walks straight into the garden; a new person starts choosing a pet.</summary>
        private async void RouteSignedIn()
        {
            MobileSession session = null;
            AccountStorage.UseAccount(identityForAccount?.UserId);
            if (mobileApi != null)
            {
                try
                {
                    await EnsureAccountReadyAsync();
                    session = await mobileApi.OpenSessionAsync();
                    await RestoreAccountAsync(mobileApi, session);
                }
                catch (Exception e) { Debug.LogWarning("[WornTag][Restore] session restore failed: " + e.Message); }
            }
            string pendingId = CreationClient.PendingPetId;
            CreationStatus pending = null;
            if (session?.pets != null)
            {
                foreach (var pet in session.pets)
                    if ((!string.IsNullOrEmpty(pendingId) && pet.id == pendingId) || (pending == null && pet.status != "ready" && pet.status != "failed" && pet.status != "rejected")) pending = pet;
            }
            if (pending == null && !string.IsNullOrEmpty(pendingId)) pending = new CreationStatus { id = pendingId, name = "Your pet", species = "cat", status = "checking" };
            if (pending != null)
            {
                var local = PetRoster.AddServerPet(pending.id, pending.name, pending.species);
                string cache = AccountStorage.Downloads;
                string petDestination = System.IO.Path.Combine(cache, "pet-" + System.Guid.NewGuid().ToString("N") + ".glb");
                onboarding.ResumeCreation(local, report => new CreationClient(mobileApi).ResumeAsync(pending.id, petDestination, System.IO.Path.Combine(cache, "owner.glb"), report));
                welcome.SetActive(false);
                return;
            }
            bool hasPet = PetRoster.HasOwnPet;
            if (hasPet) { welcome.SetActive(true); welcome.transform.SetAsLastSibling(); enterWhenReady = true; }   // opened from Tick, once everything is built and the garden is ready
            else StartOnboarding(false);
        }

        private void StartOnboarding() => StartOnboarding(false);
        private void StartOnboarding(bool addMode)
        {
            SetGameplayControlsVisible(false);
            onboardingFromWelcome = welcome.activeSelf;
            welcome.SetActive(false);
            onboarding.Begin(addMode);
        }

        private void OpenOverlayScreen(Action open) { SetGameplayControlsVisible(false); Time.timeScale = 0f; open(); }
        private void CloseOverlayScreen() { Time.timeScale = 1f; SetGameplayControlsVisible(garden.IsReady && !welcome.activeSelf); }
        internal void OpenPets() => OpenOverlayScreen(pets.Open);
        /// <summary>Account from the pause menu: sign-in options for a new player, or who you are and sign out.</summary>
        internal void OpenAccount()
        {
            SetGameplayControlsVisible(false); Time.timeScale = 0f;
            onboarding.OnAccountDone = () => CloseOverlayScreen();
            if (identityForAccount == null || string.IsNullOrEmpty(identityForAccount.UserId)) { onboarding.BeginAccountOnly(); return; }
            onboarding.ShowAccount(BuildAccountView());
        }

        private OnboardingFlow.AccountView BuildAccountView()
        {
            bool guest = identityForAccount.IsGuest;
            var account = device.AccountService;
            string provider = account?.SignInProvider;
            string who = provider == "google" ? "Google" : provider == "apple" ? "Apple" : "your account";
            string email = MaskEmail(account?.Email);
            var active = PetRoster.Active;
            return new OnboardingFlow.AccountView
            {
                guest = guest,
                identityLine = "Signed in with " + who + (string.IsNullOrEmpty(email) ? "" : " · " + email),
                petsLine = SavedPetsLine(),
                picture = Art.FromFile(active?.photoPath) ?? Art.Load(active != null && active.Kind == Species.Dog ? "GoodDogExample" : "GoodCatExample"),
                saveWithGoogle = SaveWithGoogle,
                signOut = guest ? (Action)ConfirmGuestSignOut : ConfirmSignOut,
                deletePhotos = () => ConfirmDeletion(false),
                deleteAccount = () => ConfirmDeletion(true),
                privacy = () => Application.OpenURL(CreationClient.PrivacyUrl),
                terms = () => Application.OpenURL(CreationClient.TermsUrl),
                version = "WornTag " + Application.version,
            };
        }

        /// <summary>"Mochi, Miso and 2 more are saved to your account." Only pets the server knows about count.</summary>
        internal static string SavedPetsLine()
        {
            var names = new List<string>();
            foreach (var p in PetRoster.Pets) if (!string.IsNullOrEmpty(p.serverId) && !string.IsNullOrWhiteSpace(p.name)) names.Add(p.name.Trim());
            if (names.Count == 0) return null;
            string list = names.Count == 1 ? names[0] : names.Count == 2 ? names[0] + " and " + names[1] : names.Count == 3 ? names[0] + ", " + names[1] + " and " + names[2] : names[0] + ", " + names[1] + " and " + (names.Count - 2) + " more";
            return list + (names.Count == 1 ? " is" : " are") + " saved to your account.";
        }

        /// <summary>"janak@gmail.com" becomes "ja…@gmail.com".</summary>
        internal static string MaskEmail(string email)
        {
            if (string.IsNullOrWhiteSpace(email)) return null;
            int at = email.IndexOf('@');
            if (at <= 0) return null;
            string local = email.Substring(0, at);
            return (local.Length <= 2 ? local.Substring(0, 1) : local.Substring(0, 2)) + "…" + email.Substring(at);
        }

        // Guest -> Google: the anonymous account is linked, so the uid, pets and garden stay.
        private void SaveWithGoogle()
        {
            device.SignInWithGoogle(error =>
            {
                if (error == null)
                {
                    onboarding.Hide();
                    PetToast?.Show("Saved. " + PetRoster.NameForText(true) + " is safe.", null, null);
                    OpenAccount();
                    return;
                }
                if (error == DeviceBridge.GoogleAccountInUse)
                {
                    Confirm.ShowWithCancel("This Google account already has pets", "Switch to it? The pets in this guest account can't be recovered after you switch.",
                        "Stay as guest", "Switch", null, SignOutAndReload);
                    return;
                }
                if (error != "Sign-in cancelled.") PetToast?.Show(error, null, null);
            });
        }

        // C1: a guest's pets are lost on sign-out, so saving with Google is offered first.
        private void ConfirmGuestSignOut()
        {
            Confirm.ShowWithCancel("Lose these pets?", "Guest pets can't be recovered after you sign out. Save them with Google first.",
                "Save with Google", "Sign out anyway", SaveWithGoogle, SignOutAndReload);
        }

        // C2
        private void ConfirmSignOut()
        {
            Confirm.Show("Sign out?", "Your pets stay saved to your account. Sign in again to see them.", "Cancel", "Sign out", null, SignOutAndReload, false);
        }

        /// <summary>Sign out: save, sign out of Firebase, forget this account's in-memory data and go back to Login (scene reload).</summary>
        internal void SignOutAndReload()
        {
            try { tutorial.Suspend(); } catch (Exception e) { Debug.LogWarning("[WornTag][Account] save before sign-out: " + e.Message); }
            try { identityForAccount?.SignOut(); } catch (Exception e) { Debug.LogWarning("[WornTag][Account] sign-out: " + e.Message); }
            AccountStorage.UseAccount(null);          // the next person starts with nothing from this account
            PlayerPrefs.DeleteKey(CreationClient.PendingKey); PlayerPrefs.Save();
            PetRoster.Reload(); StoreCatalog.Reload();
            onboarding.Hide(); settings.SetActive(false); guide.SetActive(false);
            enterWhenReady = false; Time.timeScale = 1f; SetGameplayControlsVisible(false);
            UnityEngine.SceneManagement.SceneManager.LoadScene("GardenCompanion");
        }

        // A different account's folder is now in use: read its profile, and reload the pet and person once the garden is ready.
        private bool needsCharacterReload;
        private void OnAccountChanged()
        {
            profile.SwitchTo(AccountStorage.ProfileFolder);
            needsCharacterReload = true;
        }
        private IIdentityService identityForAccount;
        private IMobileApi mobileApi;
        private IPrivacyApi privacyApi;
        private bool deleting;
        internal void OpenStore() => OpenOverlayScreen(store.Open);

        private void BuildSessionSheets()
        {
            guide = new GameObject("Controls guide", typeof(RectTransform));
            guide.transform.SetParent(canvasRoot, false);
            var rect = guide.GetComponent<RectTransform>(); rect.anchorMin = Vector2.zero; rect.anchorMax = Vector2.one; rect.offsetMin = rect.offsetMax = Vector2.zero;
            guideHint = Label(guide.transform, "Guide hint", "", 24, CatMeUIStyle.Ink, new Vector2(0, -150), new Vector2(600, 70), FontStyle.Normal);
            var surface = Card(guide.transform, "Hint surface", new Vector2(650, 76), CatMeUIStyle.CreamGlass);
            surface.GetComponent<RectTransform>().anchoredPosition = new Vector2(0, -150);
            surface.GetComponent<Image>().raycastTarget = false;
            surface.transform.SetAsFirstSibling();
            guideHint.transform.SetParent(surface.transform, false); guideHint.rectTransform.anchoredPosition = Vector2.zero;
            guideAction = MakeButton(guide.transform, "Teaching action", "Call", new Vector2(1, 0), new Vector2(-170, 164), new Vector2(260, 70), CatMeUIStyle.Olive, true, () => { if (tutorial.Progress.objective == "call") garden.CallCat(); else garden.ThrowBall(); });
            guideLater = MakeButton(guide.transform, "Skip ball", "Not now", new Vector2(1, 0), new Vector2(-170, 90), new Vector2(260, 58), CatMeUIStyle.CreamGlass, false, () => tutorial.SkipBall());
            MakeButton(guide.transform, "Skip introduction", "Skip introduction", new Vector2(.5f, 1), new Vector2(0, -65), new Vector2(300, 60), CatMeUIStyle.CreamGlass, false, () => tutorial.Skip());
            guideHelp = MakeButton(guide.transform, "Guide help", "Show hint", new Vector2(.5f, 0), new Vector2(0, 85), new Vector2(220, 56), CatMeUIStyle.CreamGlass, false, () => { guideTime = 0; guideHint.transform.parent.gameObject.SetActive(true); });
            guide.SetActive(false);
        }

        private void RefreshGuide()
        {
            guideTime = 0; guideHelpShown = false;
            if (guide == null) return;
            string step = tutorial.Progress.objective;
            string name = PetRoster.NameForText();
            guideHint.text = step == "walk" ? "Drag to walk." : step == "call" ? "Call " + name + ". They will come to you." : "Roll the ball. Watch " + name + " run after it.";
            guideAction.GetComponentInChildren<Text>().text = step == "call" ? "Call " + name : "Throw the ball";
            guideAction.gameObject.SetActive(step == "call" || step == "ball");
            guideLater.gameObject.SetActive(step == "ball");
            guideHelp.gameObject.SetActive(false);
            guideHint.transform.parent.gameObject.SetActive(true);
            Tick(0);
        }

        internal void Tick(float delta)
        {
            if (needsCharacterReload && garden.IsReady) { needsCharacterReload = false; garden.StartCoroutine(garden.ReloadCharactersFromPhoneStorage()); }
            if (enterWhenReady && garden.IsReady) { enterWhenReady = false; EnterGarden(); }
            onboarding?.Tick();
            bool visible = tutorial.Active && garden.IsReady && !garden.IsInputBlocked && !welcome.activeSelf && !settings.activeSelf;
            guide.SetActive(visible);
            var catAction = canvasRoot.Find("Cat action entry");
            if (catAction != null && !garden.IsInputBlocked) catAction.gameObject.SetActive(!tutorial.Active && !welcome.activeSelf);
            if (!visible || Time.timeScale == 0) return;
            guideTime += delta;
            bool guidance = PlayerPrefs.GetInt(GuidanceKey, 1) == 1;
            guideHint.transform.parent.gameObject.SetActive(guidance && guideTime < PlayerPrefs.GetFloat("CatMe.Journey.StoryHold", 4.2f));
            if (guidance && guideTime >= 15 && !guideHelpShown) { guideHelpShown = true; guideHelp.gameObject.SetActive(true); }
        }
        internal void SaveProgress() => tutorial.Suspend();
        internal void Dispose()
        {
            garden.OwnerMoved -= tutorial.OwnerMoved; garden.CallAccepted -= tutorial.CallRequested; garden.CatArrived -= tutorial.CatArrived;
            garden.CallUnavailable -= tutorial.CallUnavailable; garden.BallReleased -= tutorial.BallReleased; garden.BallFinished -= tutorial.BallFinished;
            tutorial.Changed -= RefreshGuide; tutorial.Finished -= FinishFirstSession;
            AccountStorage.Changed -= OnAccountChanged;
        }

        private void BuildSettings()
        {
            settings = Overlay("Garden settings screen");
            settings.GetComponent<Image>().color = new Color(0.035f, 0.055f, 0.045f, 0.24f);
            var card = Card(settings.transform, "Settings card", new Vector2(1480f, 920f), CatMeUIStyle.PaperGlass);
            RectTransform settingsRect = card.GetComponent<RectTransform>();
            settingsRect.anchorMin = settingsRect.anchorMax = new Vector2(0.5f, 0.5f);
            settingsRect.pivot = new Vector2(0.5f, 0.5f);
            settingsRect.anchoredPosition = Vector2.zero;
            UiKit.IconButton(card.transform, "back", new Vector2(-650f, 380f), CloseSettings, "Back from settings");
            Label(card.transform, "Settings title", "Settings", 48, CatMeUIStyle.Pine, new Vector2(-400f, 380f), new Vector2(430f, 66f), FontStyle.Bold, TextAnchor.MiddleLeft);
            BuildSettingRow(card.transform, "Sound", "Hear the garden and your pet", "speaker", new Vector2(-340f, 260f), () => PlayerPrefs.GetInt(SoundKey, 1) == 1, ToggleSound);
            BuildSettingRow(card.transform, "Guidance", "Show helpful prompts while you walk", "bulb", new Vector2(-340f, 164f), () => PlayerPrefs.GetInt(GuidanceKey, 1) == 1, ToggleGuidance);
            BuildSettingRow(card.transform, "Story notes", "Show quiet story lines", "book", new Vector2(-340f, 68f), () => PlayerPrefs.GetInt(StoryKey, 0) == 1, ToggleStory);
            BuildSettingRow(card.transform, "Story linger", "Give story lines more reading time", "clock", new Vector2(-340f, -28f), () => PlayerPrefs.GetFloat("CatMe.Journey.StoryHold", 4.2f) > 6f, ToggleStoryHold);
            BuildSettingRow(card.transform, "Reduce motion", "Keep movement and transitions gentle", "motion", new Vector2(-340f, -124f), () => PlayerPrefs.GetInt("CatMe.ReduceMotion", 0) == 1, ToggleReducedMotion);
            BuildSettingRow(card.transform, "Haptics", "Feel a light tap on actions", "vibrate", new Vector2(-340f, -220f), () => CatMe.World.Haptics.Enabled, ToggleHaptics);
            MakeButton(card.transform, "Replay controls", "Replay controls guide", new Vector2(-340f, -330f), new Vector2(400f, 66f), CatMeUIStyle.CreamSoft, false, () => { CloseSettings(); tutorial.Replay(); });
            UiKit.Button(card.transform, "Delete my photos", new Vector2(370f, 170f), new Vector2(570f, 84f), CatMeUIStyle.Tile, false, () => ConfirmDeletion(false), 30, "Delete my photos");
            UiKit.Label(card.transform, "Remove your photos from our server. Your pets stay.", 26, CatMeUIStyle.BodyInk, new Vector2(370f, 70f), new Vector2(570f, 100f), FontStyle.Normal, TextAnchor.UpperLeft);
            UiKit.Button(card.transform, "Delete account", new Vector2(370f, -80f), new Vector2(570f, 84f), CatMeUIStyle.Tile, false, () => ConfirmDeletion(true), 30, "Delete account");
            UiKit.Label(card.transform, "Remove your account, pets and photos for good.", 26, CatMeUIStyle.BodyInk, new Vector2(370f, -180f), new Vector2(570f, 100f), FontStyle.Normal, TextAnchor.UpperLeft);
            UiKit.Link(card.transform, "Privacy policy", new Vector2(235f, -345f), new Vector2(260f, 60f), () => Application.OpenURL(CreationClient.PrivacyUrl), 24);
            UiKit.Link(card.transform, "Terms and AI policy", new Vector2(500f, -345f), new Vector2(300f, 60f), () => Application.OpenURL(CreationClient.TermsUrl), 24);
            RefreshSettingLabels();
            settings.SetActive(false);
        }

        private void ConfirmDeletion(bool account)
        {
            if (deleting) return;
            if (privacyApi == null || string.IsNullOrEmpty(identityForAccount?.UserId))
            { PetToast?.Show("Sign in before removing your photos or account.", null, null); return; }
            if (account)   // C4: hold the button for 3 seconds
                Confirm.Show("Delete your account?", "This removes your pets, photos and saved garden from WornTag for good. It can't be undone.",
                    "Cancel", "Hold to delete", null, () => _ = DeleteDataAsync(true), true, 3f);
            else           // C3
                Confirm.Show("Delete your photos?", "We'll remove the photos you used to make your pets. Your pets stay.",
                    "Cancel", "Delete photos", null, () => _ = DeleteDataAsync(false));
        }

        private async System.Threading.Tasks.Task DeleteDataAsync(bool account)
        {
            if (deleting) return;
            deleting = true;
            try
            {
                if (account) await privacyApi.DeleteAccountAsync(); else await privacyApi.DeletePhotosAsync();
                string root = Application.persistentDataPath;
                if (account)
                {
                    // Only app-owned account files; settings preferences remain readable.
                    foreach (string folder in new[] { "Companion", "CatMeSession", "CatMeJourney", "JourneyPhotos", "PhotoAlbum" })
                    {
                        string path = System.IO.Path.Combine(root, folder);
                        if (System.IO.Directory.Exists(path)) System.IO.Directory.Delete(path, true);
                    }
                    string frames = System.IO.Path.Combine(root, "HousePhotos", identityForAccount.UserId);
                    if (System.IO.Directory.Exists(frames)) System.IO.Directory.Delete(frames, true);
                    AccountStorage.DeleteFolder(identityForAccount.UserId);   // only this account; other accounts on the phone keep theirs
                    AccountStorage.DeleteFolder(AccountStorage.GuestFolder);
                    AccountStorage.UseAccount(null);
                    PlayerPrefs.DeleteKey(CreationClient.PendingKey);
                    PlayerPrefs.DeleteKey(FirstSessionKey); PlayerPrefs.Save();
                    PetRoster.Reload();
                    identityForAccount.SignOut();
                    tutorial.Suspend(); guide.SetActive(false); settings.SetActive(false); welcome.SetActive(false);
                    enterWhenReady = false; Time.timeScale = 1f; SetGameplayControlsVisible(false);
                    // Reload releases all in-memory pictures, roster, inventory and character models.
                    UnityEngine.SceneManagement.SceneManager.LoadScene("GardenCompanion");
                }
                else
                {
                    PetRoster.RemovePhotoCopies();
                    string housePhotos = System.IO.Path.Combine(root, "HousePhotos", identityForAccount.UserId);   // this account's frame only
                    if (System.IO.Directory.Exists(housePhotos)) System.IO.Directory.Delete(housePhotos, true);
                    foreach (var frame in UnityEngine.Object.FindObjectsByType<CatMe.Session.HousePhotoFrame>(FindObjectsInactive.Include)) frame.ClearDisplayedPhoto();
                    string drafts = AccountStorage.Drafts;
                    if (System.IO.Directory.Exists(drafts)) System.IO.Directory.Delete(drafts, true);
                    PlayerPrefs.DeleteKey(CreationClient.PendingKey); PlayerPrefs.Save();
                    PetToast?.Show("Your photos have been removed. Your pets stay.", null, null);
                }
            }
            catch (Exception ex) { PetToast?.Show(ex.Message, "Try again", () => ConfirmDeletion(account), 30f); }
            finally { deleting = false; }
        }

        private void BuildSettingRow(Transform root, string label, string reason, string icon, Vector2 position, Func<bool> getValue, Action action)
        {
            GameObject row = new GameObject(label + " row", typeof(RectTransform), typeof(Image), typeof(Button));
            row.transform.SetParent(root, false);
            RectTransform rect = row.GetComponent<RectTransform>();
            rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = new Vector2(680f, 84f);
            rect.anchoredPosition = position;
            CatMeUIStyle.ApplySurface(row.GetComponent<Image>(), CatMeUIStyle.CreamSoft);
            Button button = row.GetComponent<Button>();
            button.targetGraphic = row.GetComponent<Image>();
            button.onClick.AddListener(() => action());
            CatMeUIStyle.StyleButton(button, CatMeUIStyle.CreamSoft, false);
            UiKit.Icon(row.transform, icon, new Vector2(-286f, 0f), 44f, CatMeUIStyle.Pine);
            Label(row.transform, "Label", label, 25, CatMeUIStyle.Ink, new Vector2(-94f, 15f), new Vector2(330f, 36f), FontStyle.Bold, TextAnchor.MiddleLeft);
            Label(row.transform, "Reason", reason, 20, CatMeUIStyle.MutedInk, new Vector2(-75f, -18f), new Vector2(370f, 32f), FontStyle.Normal, TextAnchor.MiddleLeft);

            GameObject trackObject = new GameObject("Switch track", typeof(RectTransform), typeof(Image));
            trackObject.transform.SetParent(row.transform, false);
            RectTransform trackRect = trackObject.GetComponent<RectTransform>();
            trackRect.anchorMin = trackRect.anchorMax = new Vector2(1f, 0.5f);
            trackRect.pivot = new Vector2(1f, 0.5f);
            trackRect.anchoredPosition = new Vector2(-18f, 0f);
            trackRect.sizeDelta = new Vector2(88f, 52f);
            CatMeUIStyle.ApplySurface(trackObject.GetComponent<Image>(), CatMeUIStyle.Hairline);
            trackObject.GetComponent<Image>().raycastTarget = false;

            GameObject knobObject = new GameObject("Switch knob", typeof(RectTransform), typeof(Image));
            knobObject.transform.SetParent(trackObject.transform, false);
            RectTransform knobRect = knobObject.GetComponent<RectTransform>();
            knobRect.anchorMin = knobRect.anchorMax = new Vector2(0f, 0.5f);
            knobRect.pivot = new Vector2(0.5f, 0.5f);
            knobRect.sizeDelta = new Vector2(40f, 40f);
            Image knob = knobObject.GetComponent<Image>();
            CatMeUIStyle.ApplySurface(knob, CatMeUIStyle.CreamGlass, false);
            knob.raycastTarget = false;
            knobObject.AddComponent<CatMeSwitchThumb>();
            settingTracks.Add(trackObject.GetComponent<Image>());
            settingKnobs.Add(knob);
            settingValues.Add(getValue);
        }

        private readonly List<Func<bool>> settingValues = new List<Func<bool>>();

        private void AddGardenMenuButton()
        {
            // The gameplay Pause entry owns access to Settings.
        }

        private void SetGameplayControlsVisible(bool visible)
        {
            garden.SetGameplayHudVisible(visible);
            garden.SetInputBlocked(!visible);
            string[] controlNames = { "Movement joystick", "Cat action entry", "Pause action", "Camera action", "Quiet garden hint" };
            foreach (string name in controlNames)
            {
                Transform item = canvasRoot.Find(name);
                if (item != null) item.gameObject.SetActive(visible);
            }
            if (!visible) { var tray = canvasRoot.Find("Cat action tray"); if (tray != null) tray.gameObject.SetActive(false); }
        }

        private void EnterGarden()
        {
            if (!garden.IsReady)
            {
                SetGameplayControlsVisible(false);
                garden.SetAppStatus("Getting your garden ready…");
                welcome.SetActive(true);
                enterWhenReady = true;       // Tick opens the garden as soon as it has loaded
                return;
            }
            welcome.SetActive(false);
            SetGameplayControlsVisible(true);
            garden.EndFirstGardenSession();
            garden.ResumeConnectedOuting();
            tutorial.Begin();
        }

        // Legacy callbacks stay inert for older editor checks; the event-based guide owns progress.
        internal void OnCatReunited() { }
        internal void OnWalkTogetherComplete() { }
        internal void OnFirstFetchComplete() { }
        private void FinishFirstSession()
        {
            garden.EndFirstGardenSession();
            SetGameplayControlsVisible(true);
            garden.SetAppStatus(tutorial.Progress.skipped ? "" : "You did it. The garden is yours.");
        }

        private void OpenSettings()
        {
            SetGameplayControlsVisible(false);
            settings.SetActive(true);
            settings.transform.SetAsLastSibling();
            Time.timeScale = 0f;
        }

        internal void OpenSettingsFromPause() => OpenSettings();

        internal void ReturnToTitle()
        {
            tutorial.Suspend();
            guide.SetActive(false);
            SetGameplayControlsVisible(false);
            if (onboarding.IsSignedIn()) { welcome.SetActive(true); welcome.transform.SetAsLastSibling(); enterWhenReady = true; }
            else { welcome.SetActive(false); onboarding.BeginLogin(); }
        }

        private void CloseSettings()
        {
            settings.SetActive(false);
            Time.timeScale = 1f;
            SetGameplayControlsVisible(garden.IsReady);
        }

        private void ToggleSound()
        {
            bool enabled = PlayerPrefs.GetInt(SoundKey, 1) == 1;
            PlayerPrefs.SetInt(SoundKey, enabled ? 0 : 1);
            PlayerPrefs.Save();
            garden.SetSoundEnabled(!enabled);
            RefreshSettingLabels();
        }

        private void ToggleGuidance()
        {
            bool enabled = PlayerPrefs.GetInt(GuidanceKey, 1) == 1;
            PlayerPrefs.SetInt(GuidanceKey, enabled ? 0 : 1);
            PlayerPrefs.Save();
            RefreshSettingLabels();
        }

        private void ToggleStory()
        {
            bool enabled = PlayerPrefs.GetInt(StoryKey, 0) == 1;
            PlayerPrefs.SetInt(StoryKey, enabled ? 0 : 1);
            PlayerPrefs.Save();
            RefreshSettingLabels();
        }

        private void ToggleStoryHold()
        {
            float seconds=PlayerPrefs.GetFloat("CatMe.Journey.StoryHold",4.2f);
            PlayerPrefs.SetFloat("CatMe.Journey.StoryHold",seconds<6f?7.5f:4.2f);
            PlayerPrefs.Save();RefreshSettingLabels();
        }

        private void ToggleReducedMotion()
        {
            garden.SetReducedMotion(PlayerPrefs.GetInt("CatMe.ReduceMotion",0)==0);
            RefreshSettingLabels();
        }

        private void ToggleHaptics()
        {
            CatMe.World.Haptics.Enabled = !CatMe.World.Haptics.Enabled;
            RefreshSettingLabels();
        }

        private void RefreshSettingLabels()
        {
            for (int i = 0; i < settingValues.Count; i++)
            {
                bool enabled = settingValues[i]();
                settingTracks[i].color = enabled ? CatMeUIStyle.Olive : CatMeUIStyle.Hairline;
                settingKnobs[i].GetComponent<CatMeSwitchThumb>().SetValue(enabled);
            }
        }

        private GameObject Sheet(string name, string title, string body, string primary, Action primaryAction, string secondary, Action secondaryAction)
        {
            GameObject root = Overlay(name);
            root.GetComponent<Image>().color = new Color(0.035f, 0.055f, 0.045f, 0.10f);
            var card = Card(root.transform, name + " card", new Vector2(760f, 290f), CatMeUIStyle.CreamGlass);
            RectTransform sheetRect = card.GetComponent<RectTransform>();
            sheetRect.anchorMin = sheetRect.anchorMax = new Vector2(0.5f, 0f);
            sheetRect.pivot = new Vector2(0.5f, 0f);
            sheetRect.anchoredPosition = new Vector2(0f, 36f);
            Label(card.transform, "Title", title, 30, CatMeUIStyle.Ink, new Vector2(0f, 85f), new Vector2(680f, 48f), FontStyle.Bold);
            Label(card.transform, "Message", body, 18, CatMeUIStyle.MutedInk, new Vector2(-340f, 68f), new Vector2(680f, 56f), FontStyle.Normal);
            MakeButton(card.transform, "Primary", primary, new Vector2(0f, -58f), new Vector2(340f, 58f), CatMeUIStyle.Olive, true, primaryAction);
            MakeButton(card.transform, "Secondary", secondary, new Vector2(0f, -119f), new Vector2(320f, 44f), CatMeUIStyle.CreamGlass, false, secondaryAction);
            root.SetActive(false);
            return root;
        }

        private static void Feature(Transform root, string title, string copy, Vector2 position)
        {
            var card = Card(root, title + " feature card", new Vector2(420f, 120f), CatMeUIStyle.CreamSoft);
            card.GetComponent<RectTransform>().anchoredPosition = position;
            Label(card.transform, "Title", title, 23, CatMeUIStyle.Ink, new Vector2(0f, 24f), new Vector2(370f, 36f), FontStyle.Bold);
            Label(card.transform, "Copy", copy, 16, CatMeUIStyle.MutedInk, new Vector2(0f, -16f), new Vector2(370f, 48f), FontStyle.Normal);
        }

        private GameObject Overlay(string name)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Image));
            go.transform.SetParent(canvasRoot, false);
            var rect = go.GetComponent<RectTransform>();
            rect.anchorMin = Vector2.zero; rect.anchorMax = Vector2.one; rect.offsetMin = Vector2.zero; rect.offsetMax = Vector2.zero;
            go.GetComponent<Image>().color = new Color(0.08f, 0.11f, 0.08f, 0.58f);
            return go;
        }

        private static GameObject Card(Transform root, string name, Vector2 size, Color color)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Image)); go.transform.SetParent(root, false);
            var rect = go.GetComponent<RectTransform>(); rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f); rect.pivot = new Vector2(0.5f, 0.5f); rect.sizeDelta = size;
            CatMeUIStyle.ApplySurface(go.GetComponent<Image>(), color);
            return go;
        }

        private static Text Label(Transform root, string name, string value, int size, Color color, Vector2 position, Vector2 dimensions, FontStyle style, TextAnchor alignment = TextAnchor.MiddleCenter)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Text)); go.transform.SetParent(root, false);
            var rect = go.GetComponent<RectTransform>(); rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f); rect.sizeDelta = dimensions; rect.anchoredPosition = position;
            var text = go.GetComponent<Text>(); text.font = CatMe.Garden.CatMeUIStyle.UiFont; text.text = value; text.fontSize = size; text.fontStyle = style; text.color = color; text.alignment = alignment; text.horizontalOverflow = HorizontalWrapMode.Wrap; text.verticalOverflow = VerticalWrapMode.Truncate; text.raycastTarget = false;
            return text;
        }

        private static Button MakeButton(Transform root, string name, string label, Vector2 position, Vector2 dimensions, Color tint, bool primary, Action action)
            => MakeButton(root, name, label, new Vector2(0.5f, 0.5f), position, dimensions, tint, primary, action);

        private static Button MakeButton(Transform root, string name, string label, Vector2 anchor, Vector2 position, Vector2 dimensions, Color tint, bool primary, Action action)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button)); go.transform.SetParent(root, false);
            var rect = go.GetComponent<RectTransform>(); rect.anchorMin = rect.anchorMax = anchor; rect.sizeDelta = dimensions; rect.anchoredPosition = position;
            CatMeUIStyle.ApplySurface(go.GetComponent<Image>(), tint);
            var button = go.GetComponent<Button>(); button.onClick.AddListener(() => action());
            var labelText = Label(go.transform, "Label", label, 22, primary ? Color.white : CatMeUIStyle.Ink, Vector2.zero, dimensions - new Vector2(24f, 14f), FontStyle.Bold);
            labelText.raycastTarget = false;
            CatMeUIStyle.StyleButton(button, tint, primary);
            return button;
        }
    }

    internal sealed class CatMeSwitchThumb : MonoBehaviour
    {
        private RectTransform rect;
        private float target;
        private float velocity;
        private bool initialized;

        internal void SetValue(bool enabled)
        {
            if (rect == null) rect = (RectTransform)transform;
            target = enabled ? 1f : 0f;
            if (!initialized || PlayerPrefs.GetInt("CatMe.ReduceMotion", 0) == 1)
            {
                Apply(target);
                initialized = true;
            }
        }

        private void Update()
        {
            if (rect == null || PlayerPrefs.GetInt("CatMe.ReduceMotion", 0) == 1) return;
            float next = Mathf.SmoothDamp(rect.anchorMin.x, target, ref velocity, 0.16f, Mathf.Infinity, Time.unscaledDeltaTime);
            Apply(next);
        }

        private void Apply(float value)
        {
            rect.anchorMin = rect.anchorMax = new Vector2(value, 0.5f);
            rect.anchoredPosition = new Vector2(Mathf.Lerp(26f, -26f, value), 0f);
        }
    }
}
