using System.Collections;
using System.Collections.Generic;
using CatMe.Garden;
using CatMe.World;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace CatMe.Session
{
    /// <summary>
    /// Lives in the persistent GardenCompanion shell. Loads one place scene at a time, walks the pair through
    /// transitions, applies per-place lighting/weather/camera and saves progress. See docs/production/03 and 04.
    /// </summary>
    public sealed class SessionDirector : MonoBehaviour
    {
        public const string PlaceFolder = "Assets/CatMe/Scenes/Places/";
        public static readonly string[] Order = { "HomeGarden" };

        /// <summary>Bumped when a place scene is regenerated under a new file name, so Unity can never serve a stale cached copy.</summary>
        public const string SceneRevision = "_r2";
        public static string ScenePath(string placeId) => PlaceFolder + "Place_" + placeId + SceneRevision + ".unity";
        /// <summary>True once the first place scene is in Build Settings; otherwise the legacy single route is used.</summary>
        public static bool PlacesAvailable => SceneUtility.GetBuildIndexByScenePath(ScenePath("HomeGarden")) >= 0;
        public static SessionDirector Instance { get; private set; }

        GardenCompanionDemo garden;
        PlaceLighting lighting;
        internal DayDirector Day { get; private set; }
        PlaceWeather weather;
        AudioSource ambience;
        CanvasGroup fade;
        Text placeLabel;
        PlaceRoot current;
        Scene currentScene;
        SessionSaveData save;
        bool busy;
        float ambienceTarget, ambienceFrom;

        public bool IsBusy => busy;
        internal Transform CameraRigTransform => garden != null && garden.CameraRig != null ? garden.CameraRig.transform : null;
        internal Vector3 OwnerPositionForWorld => garden != null ? garden.OwnerPosition : Vector3.zero;
        public string CurrentScenePath => currentScene.IsValid() ? currentScene.path : "(none)";
        public int CountNamed(string contains) { int n = 0; if (current != null) foreach (var t in current.GetComponentsInChildren<Transform>(true)) if (t.name.Contains(contains)) n++; return n; }
        internal PlaceLighting Lighting => lighting;
        internal PlaceWeather WeatherController => weather;
        internal string CurrentLightingName => lighting != null ? lighting.CurrentName : null;
        internal void SetRainSuppressed(bool value) { if (weather != null) weather.Suppressed = value; }
        internal void EndRain() { if (weather != null) weather.EndRain(); }
        internal Transform FindNamed(string name)
        {
            if (current == null) return null;
            foreach (var t in current.GetComponentsInChildren<Transform>(true)) if (t.name == name) return t;
            return null;
        }
        InteractionPoint[] interactions = new InteractionPoint[0];
        public InteractionPoint[] CurrentInteractions() => interactions;
        public string CurrentPlaceId => current != null ? current.placeId : null;
        public WeatherState Weather => weather != null ? weather.State : WeatherState.Dry;
        public event System.Action<string> PlaceEntered;
        public event System.Action<string> PlaceLeaving;
        public event System.Action<WeatherState> WeatherChanged;
        public IReadOnlyList<string> Keepsakes => save != null ? save.keepsakes : new List<string>();

        public void Initialize(GardenCompanionDemo demo, Transform canvasRoot)
        {
            Instance = this;
            garden = demo;
            garden.SetUnboundedWorld(true);
            gameObject.AddComponent<CatCuriosity>().Initialize(garden);
            gameObject.AddComponent<CompanionLeader>().Initialize(garden, this);
            lighting = gameObject.AddComponent<PlaceLighting>();
            weather = gameObject.AddComponent<PlaceWeather>();
            weather.Initialize(garden.CameraRig != null ? garden.CameraRig.transform : Camera.main.transform);
            weather.StateChanged += state =>
            {
                if (save != null) { save.weather = state.ToString(); }
                WeatherChanged?.Invoke(state);
                // The sky follows the weather: grey and cool when rain comes, back to sunset once it has passed.
                if (Day == null && lighting != null && (state == WeatherState.Clearing || state == WeatherState.Dry) && lighting.CurrentName == "Overcast") lighting.Apply("Sunset", 14f);
            };
            gameObject.AddComponent<Interactions>().Initialize(garden, this, canvasRoot);
            Day = gameObject.AddComponent<DayDirector>(); Day.Initialize(this, lighting, weather);
            gameObject.AddComponent<CatPetInput>().Initialize(garden, canvasRoot);
            weather.ShelterCleared += () => { lighting.Apply(current != null ? current.clearedLightingPreset : "Sunset", 20f); };
            ambience = gameObject.AddComponent<AudioSource>();
            ambience.loop = true; ambience.spatialBlend = 0f; ambience.volume = 0f;
            BuildFade(canvasRoot);
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            BuildDevSwitcher(canvasRoot);
#endif
            save = SessionSave.Load();
            // Saves from the old multi-place layout point at scenes that no longer exist.
            if (System.Array.IndexOf(Order, save.placeId) < 0) { save.placeId = Order[0]; save.entryId = "Start"; save.weather = "Dry"; }
#if UNITY_EDITOR
            // In the editor every Play starts in the home garden; use the on-screen buttons or keys 1-5 to jump elsewhere.
            save.placeId = "HomeGarden"; save.entryId = "Start"; save.homecoming = false;
#endif
            StartCoroutine(LoadFirst());
        }

        /// <summary>Begin a fresh outing from the garden start (Pause → new outing, or first Start walking).</summary>
        public void StartNewSession()
        {
            if (busy) return;
            save = new SessionSaveData { keepsakes = save != null ? save.keepsakes : new List<string>(), completedSessions = save != null ? save.completedSessions : 0 };
            StartCoroutine(Travel("HomeGarden", "Start", null, false));
        }

        public void ResumeSession()
        {
            if (busy || current == null) return;
            if (current.placeId != save.placeId) StartCoroutine(Travel(save.placeId, save.entryId, null, false));
        }

        public void RequestTransition(string place, string entry)
        {
            if (busy || string.IsNullOrEmpty(place)) return;
            StartCoroutine(Travel(place, entry, null, true));
        }

        void Update()
        {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            ReadPlaceShortcuts();
#endif
            if (current == null || garden == null || garden.OwnerTransform == null) return;
            Vector3 owner = garden.OwnerTransform.position;
            if (!busy)
            {
                foreach (var exit in current.Exits)
                    if (exit != null && exit.isActiveAndEnabled && exit.Contains(owner)) { RequestTransition(exit.targetPlace, exit.targetEntry); break; }
                UpdateZones(owner);
                if (garden.CatTransform != null) weather.Tick(owner, garden.CatTransform.position);
                // Sky greys over as the rain actually begins to fall on the path.
                if (Day == null && !weather.Suppressed && weather.Intensity > .05f && lighting.CurrentName != "Overcast" && lighting.CurrentName != "TentInterior" && (weather.State == WeatherState.RainStarting || weather.State == WeatherState.Raining))
                    lighting.Apply("Overcast", 8f);
            }
            if (ambience != null)
                ambience.volume = Mathf.MoveTowards(ambience.volume, PlayerPrefs.GetInt("CatMe.SoundEnabled", 1) == 0 ? 0f : ambienceTarget, Time.unscaledDeltaTime * .5f);
        }

#if UNITY_EDITOR || DEVELOPMENT_BUILD
        // Clickable place switcher built from regular UI buttons (the same input path the joystick uses).
        void BuildDevSwitcher(Transform canvasRoot)
        {
            if (Order.Length < 2) return; // a single place needs no switcher
            string[] labels = { "1 Garden", "2 Riverside", "3 Woodland", "4 Tent", "5 Retreat" };
            string[] entries = { "Start", "FromGarden", "FromRiverside", "FromWoodland", "FromShelter" };
            var row = new GameObject("Dev place switcher", typeof(RectTransform)); row.transform.SetParent(canvasRoot, false);
            var rowRect = row.GetComponent<RectTransform>(); rowRect.anchorMin = rowRect.anchorMax = new Vector2(0f, 1f); rowRect.pivot = new Vector2(0f, 1f);
            rowRect.anchoredPosition = new Vector2(24f, -24f); rowRect.sizeDelta = new Vector2(760f, 56f);
            for (int i = 0; i < labels.Length; i++)
            {
                int index = i;
                var go = new GameObject(labels[i], typeof(RectTransform), typeof(Image), typeof(Button)); go.transform.SetParent(row.transform, false);
                var r = go.GetComponent<RectTransform>(); r.anchorMin = r.anchorMax = new Vector2(0f, .5f); r.pivot = new Vector2(0f, .5f);
                r.anchoredPosition = new Vector2(i * 152f, 0f); r.sizeDelta = new Vector2(146f, 52f);
                var image = go.GetComponent<Image>(); image.color = new Color(.157f, .302f, .263f, .92f);
                go.GetComponent<Button>().targetGraphic = image;
                go.GetComponent<Button>().onClick.AddListener(() => { Debug.Log("[CatMe][Session] Switch requested: " + Order[index]); GoTo(Order[index], entries[index]); });
                var textGo = new GameObject("Label", typeof(RectTransform), typeof(Text)); textGo.transform.SetParent(go.transform, false);
                var tr = textGo.GetComponent<RectTransform>(); tr.anchorMin = Vector2.zero; tr.anchorMax = Vector2.one; tr.offsetMin = tr.offsetMax = Vector2.zero;
                var text = textGo.GetComponent<Text>(); text.text = labels[i]; text.alignment = TextAnchor.MiddleCenter; text.fontSize = 24; text.color = Color.white; text.raycastTarget = false;
                text.font = CatMe.Garden.CatMeUIStyle.UiFont;
            }
        }

        void GoTo(string place, string entry)
        {
            if (busy) { Debug.LogWarning("[CatMe][Session] Ignored switch: a transition is already running."); return; }
            StartCoroutine(Travel(place, entry, null, false));
        }

        // Development shortcut: keys 1-5 jump to garden, riverside, woodland, tent/shelter and retreat; 0 starts a fresh session.
        void ReadPlaceShortcuts()
        {
            var keyboard = UnityEngine.InputSystem.Keyboard.current;
            if (keyboard == null || busy) return;
            string[] entries = { "Start", "FromGarden", "FromRiverside", "FromWoodland", "FromShelter" };
            if (Order.Length < 2) return;
            var keys = new[] { keyboard.digit1Key, keyboard.digit2Key, keyboard.digit3Key, keyboard.digit4Key, keyboard.digit5Key };
            for (int i = 0; i < keys.Length; i++)
                if (keys[i].wasPressedThisFrame) { GoTo(Order[i], entries[i]); return; }
            if (keyboard.digit0Key.wasPressedThisFrame) StartNewSession();
        }
#endif

        // One zone at a time, by priority, with a 1 m margin (CameraZonePicker).
        CameraZone activeZone;
        readonly List<CameraZonePicker.Candidate> zoneCandidates = new List<CameraZonePicker.Candidate>();
        void UpdateZones(Vector3 owner)
        {
            var rig = garden.CameraRig;
            if (rig == null) return;
            var zones = current.Zones;
            zoneCandidates.Clear();
            int currentIndex = -1;
            for (int i = 0; i < zones.Count; i++)
            {
                var zone = zones[i];
                zoneCandidates.Add(zone != null ? zone.CandidateFor(owner) : default);
                if (zone != null && zone == activeZone) currentIndex = i;
            }
            int pick = CameraZonePicker.Pick(zoneCandidates, currentIndex);
            CameraZone next = pick >= 0 ? zones[pick] : null;
            if (next == activeZone) return;
            if (activeZone != null) rig.PopProfile(activeZone.Profile);
            if (next != null) rig.PushProfile(next.Profile);
            activeZone = next;
        }

        IEnumerator LoadFirst()
        {
            // Wait until the pair exists in the shell.
            while (garden.OwnerTransform == null || garden.CatTransform == null) yield return null;
            yield return Travel(save.placeId, save.entryId, null, false, initial: true);
        }

        IEnumerator Travel(string placeId, string entryId, string lightingOverride, bool walkOut, bool initial = false)
        {
            busy = true;
            Debug.Log("[CatMe][Session] Travelling to " + placeId + " (" + entryId + ")");
            var rig = garden.CameraRig;
            bool reduced = PlayerPrefs.GetInt("CatMe.ReduceMotion", 0) == 1;
            float fadeTime = reduced ? .4f : 1.2f;

            if (!initial && current != null)
            {
                PlaceLeaving?.Invoke(current.placeId);
                garden.SetInputBlocked(true);
                if (walkOut)
                {
                    var exit = FindExit(garden.OwnerTransform.position);
                    Vector3 dir = exit != null ? exit.transform.forward : garden.OwnerTransform.forward;
                    dir.y = 0f;
                    garden.SetScriptedWalk(dir.normalized * .8f);
                    rig?.SetExtraHeight(1.5f);
                }
                yield return FadeTo(1f, fadeTime);
                garden.SetScriptedWalk(Vector3.zero);
            }
            else if (initial)
            {
                SetFade(1f);
            }

            // Load the next place on top of the shell, then drop the old one.
            string path = ScenePath(placeId);
            Scene previous = currentScene;
            // If this place scene is already open (for example left open in the Editor Hierarchy), unload that copy first so the
            // game always loads the version saved on disk instead of a stale in-memory one.
            Scene stale = SceneManager.GetSceneByPath(path);
            if (stale.IsValid() && stale.isLoaded && stale != previous)
            {
                Debug.LogWarning("[CatMe][Session] Place scene was already open; reloading it from disk: " + path);
                yield return SceneManager.UnloadSceneAsync(stale);
            }
            var load = SceneManager.LoadSceneAsync(path, LoadSceneMode.Additive);
            if (load == null) { Debug.LogError("[CatMe][Session] Place scene is not in Build Settings: " + path); busy = false; yield return FadeTo(0f, fadeTime); yield break; }
            while (!load.isDone) yield return null;
            Scene next = SceneManager.GetSceneByPath(path);
            PlaceRoot root = null;
            foreach (var go in next.GetRootGameObjects()) { root = go.GetComponentInChildren<PlaceRoot>(true); if (root != null) break; }
            if (root == null) { Debug.LogError("[CatMe][Session] No PlaceRoot in " + path); busy = false; yield return FadeTo(0f, fadeTime); yield break; }
            if (previous.IsValid() && previous != next) { activeZone = null; rig?.PopAll(); yield return SceneManager.UnloadSceneAsync(previous); }
            currentScene = next; current = root;
            interactions = root.GetComponentsInChildren<InteractionPoint>(true);

            Transform entry = root.FindEntry(entryId);
            bool homecoming = placeId == "HomeGarden" && entryId == "Return";
            if (homecoming) { save.homecoming = true; save.completedSessions++; }
            else if (placeId == "HomeGarden" && entryId == "Start") save.homecoming = false;
            Vector3 position = entry != null ? entry.position : Vector3.zero;
            float yaw = entry != null ? entry.eulerAngles.y : 0f;
            garden.PlacePair(position, yaw);
            garden.SetScriptedWalk(Vector3.zero);
            rig?.SetBaseProfile(CameraProfile.Named(root.cameraProfile));
            rig?.Snap();
            rig?.SetExtraHeight(0f);

            lighting.Apply(homecoming || (save.homecoming && placeId == "HomeGarden") ? "BlueHour" : root.lightingPreset, 0f);
            weather.EnterPlace(root);
            if (initial) weather.Restore(save.weather);
            SwitchAmbience(root);
            SetNightObjects(root, lighting.CurrentName == "BlueHour");

            save.placeId = placeId; save.entryId = entryId; save.sessionStep = System.Array.IndexOf(Order, placeId);
            save.weather = weather.State.ToString();
            SessionSave.Write(save);
            ShowPlaceName(root);
            PlaceEntered?.Invoke(placeId);

            yield return FadeTo(0f, fadeTime);
            garden.SetInputBlocked(false);
            busy = false;
        }

        PlaceExit FindExit(Vector3 near)
        {
            if (current == null) return null;
            foreach (var exit in current.Exits) if (exit != null && exit.Contains(near)) return exit;
            return null;
        }

        // Lantern/window objects named Night_* are enabled only for the blue-hour look.
        internal void SetNightObjects(bool night) { if (current != null) SetNightObjects(current, night); }

        static void SetNightObjects(PlaceRoot root, bool night)
        {
            foreach (var t in root.GetComponentsInChildren<Transform>(true))
                if (t.name.StartsWith("Night_")) t.gameObject.SetActive(night);
        }

        void SwitchAmbience(PlaceRoot root)
        {
            if (string.IsNullOrEmpty(root.ambienceResource)) { ambienceTarget = 0f; return; }
            var clip = Resources.Load<AudioClip>(root.ambienceResource);
            if (clip == null) { ambienceTarget = 0f; return; }
            if (ambience.clip != clip) { ambience.clip = clip; ambience.volume = 0f; ambience.Play(); }
            ambienceTarget = root.ambienceVolume;
        }

        public void AddKeepsake(string id)
        {
            if (save == null || save.keepsakes.Contains(id)) return;
            save.keepsakes.Add(id);
            SessionSave.Write(save);
        }

        public void SaveNow() { if (save != null) SessionSave.Write(save); }

        void OnApplicationPause(bool paused) { if (paused) SaveNow(); }
        void OnApplicationQuit() { SaveNow(); }

        // ---- fade overlay -------------------------------------------------------------------------------

        void BuildFade(Transform canvasRoot)
        {
            var go = new GameObject("Session fade", typeof(RectTransform), typeof(Image), typeof(CanvasGroup));
            go.transform.SetParent(canvasRoot, false);
            var rect = go.GetComponent<RectTransform>(); rect.anchorMin = Vector2.zero; rect.anchorMax = Vector2.one; rect.offsetMin = rect.offsetMax = Vector2.zero;
            go.GetComponent<Image>().color = new Color(.969f, .965f, .949f, 1f); // warm white #F7F6F2
            go.GetComponent<Image>().raycastTarget = false;
            fade = go.GetComponent<CanvasGroup>(); fade.alpha = 0f; fade.blocksRaycasts = false; fade.interactable = false;
            CompanionCamera.CoverAlpha = () => fade != null ? fade.alpha : 0f;   // a camera cut under this fade needs no fade of its own
            var label = new GameObject("Place name", typeof(RectTransform), typeof(Text));
            label.transform.SetParent(go.transform, false);
            var lr = label.GetComponent<RectTransform>(); lr.anchorMin = lr.anchorMax = new Vector2(.5f, .5f); lr.sizeDelta = new Vector2(900, 80);
            placeLabel = label.GetComponent<Text>();
            placeLabel.font = CatMe.Garden.CatMeUIStyle.UiFont;
            placeLabel.fontSize = 34; placeLabel.alignment = TextAnchor.MiddleCenter; placeLabel.color = new Color(.157f, .302f, .263f, 1f); placeLabel.raycastTarget = false;
            go.transform.SetAsLastSibling();
        }

        void SetFade(float alpha) { if (fade != null) fade.alpha = alpha; }

        void ShowPlaceName(PlaceRoot root)
        {
            if (placeLabel == null) return;
            bool story = PlayerPrefs.GetInt("CatMe.Journey.StoryEnabled", 0) == 1;
            placeLabel.text = story ? root.displayName : string.Empty;
        }

        internal IEnumerator FadeTo(float target, float seconds)
        {
            if (fade == null) yield break;
            float start = fade.alpha;
            for (float t = 0f; t < seconds; t += Time.unscaledDeltaTime)
            {
                fade.alpha = Mathf.Lerp(start, target, t / seconds);
                yield return null;
            }
            fade.alpha = target;
        }
    }
}
