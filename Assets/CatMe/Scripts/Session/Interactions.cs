using System.Collections;
using CatMe.Garden;
using CatMe.Pets;
using CatMe.World;
using UnityEngine;
using UnityEngine.UI;

namespace CatMe.Session
{
    public enum InteractionKind { Sit, EnterTent, ExitTent, EnterHouse, HousePhoto }

    /// <summary>
    /// Shows one contextual button near an interaction point and runs it: sit on a bench (first-person view, cat in front)
    /// or get into the tent (dark first-person room, rain sound, cat beside you, rain stops, go back out).
    /// Needs no new animation: the owner is hidden in first person and the cat holds a neutral standing pose.
    /// </summary>
    public sealed class Interactions : MonoBehaviour
    {
        enum State { Free, Sitting, InRoom, InHouse, Busy }
        GardenCompanionDemo garden;
        SessionDirector director;
        Button button; Text label; CanvasGroup buttonGroup; RectTransform buttonRect, canvasRect;
        State state = State.Free;
        InteractionPoint active;
        InteractionPoint sitPoint;
        Transform eyeAnchor;
        float roomEnteredAt; bool rainStopped, rainedOnEntry;
        AudioSource rainAudio; AudioClip rainClip;
        string lightingBefore = "Sunset";
        Vector3 doorSpot;
        Transform roomButtons; Button feedButton, sleepButton;
        bool catSleeping;
        Vector3 houseSpot;
        GameObject houseRoom;
        HousePhotoFrame houseFrame;
        bool frameLeading, frameAtAisle;
        // Feeding: walk up to the bowl, fill it, and the pet comes over and eats it down.
        enum HouseAction { None, Bowl, Frame }
        enum FeedStage { Empty, Filled, Eating }
        HouseAction houseAction;
        FeedStage feed = FeedStage.Empty;
        Transform bowl, food; Vector3 bowlCentre; float bowlRadius = .25f, fullUntil;
        bool feedLeading;
        Coroutine eating;
        const float BowlReach = 1.8f;
        // Species decides the sounds, words and timing (a dog barks, gulps its food and retrieves; a cat meows, nibbles and purrs).
        CompanionBehavior Pet => garden.Behavior ?? CompanionBehavior.For(PetRoster.Active.Kind);
        float Flat(Vector3 a, Vector3 b) => Vector2.Distance(new Vector2(a.x, a.z), new Vector2(b.x, b.z));
        bool NearBowl => bowl != null && Flat(garden.OwnerTransform.position, bowlCentre) < BowlReach;
        Vector3 EatSpot { get { Vector3 toRoom = houseSpot - bowlCentre; toRoom.y = 0f; if (toRoom.sqrMagnitude < .01f) toRoom = Vector3.back; var p = bowlCentre + toRoom.normalized * (bowlRadius + .3f); p.y = houseSpot.y; return p; } }
        Vector3 FrameGuidePoint(float distance) { var p = houseFrame.transform.position + houseFrame.transform.forward * distance; p.y = houseSpot.y; return p; }
        bool NearFrame => houseFrame != null && Vector3.Distance(new Vector3(garden.OwnerTransform.position.x, 0, garden.OwnerTransform.position.z), new Vector3(houseFrame.transform.position.x, 0, houseFrame.transform.position.z)) < 2.6f;
        Transform RoomFind(string name)
        {
            if (houseRoom == null)
            {
                var prefab = Resources.Load<GameObject>("Rooms/HouseRoom_r3");
                if (prefab != null) { houseRoom = Instantiate(prefab); houseRoom.name = "HouseRoom (runtime)"; }
            }
            if (houseRoom != null) foreach (var t in houseRoom.GetComponentsInChildren<Transform>(true)) if (t.name == name) return t;
            return director.FindNamed(name); // fallback: a room still baked into the place scene
        }
        float doorYaw;

        const float RoomRainSeconds = 22f;

        public void Initialize(GardenCompanionDemo demo, SessionDirector sessionDirector, Transform canvasRoot)
        {
            garden = demo; director = sessionDirector;
            rainAudio = gameObject.AddComponent<AudioSource>(); rainAudio.loop = true; rainAudio.spatialBlend = 0f; rainAudio.volume = 0f;
            rainClip = Resources.Load<AudioClip>("CatMeAudio/rain-light") ?? MakeRainNoise(); // real rain recording; the generated noise is only a fallback
            rainAudio.clip = rainClip; rainAudio.Play();
            BuildButton(canvasRoot);
        }

        void Update()
        {
            if (garden == null || garden.OwnerTransform == null) return;
            // Rain bed: faint on the path while it rains, louder and muffled-feeling inside the tent.
            float target = 0f;
            var w = director.WeatherController;
            if (w != null && w.Intensity > .02f) target = Mathf.Lerp(0f, .1f, w.Intensity);
            if (state == State.InRoom && !rainStopped && rainedOnEntry) target = .24f;   // rain on the canvas only if it was raining
            if (PlayerPrefs.GetInt("CatMe.SoundEnabled", 1) == 0) target = 0f;
            rainAudio.volume = Mathf.MoveTowards(rainAudio.volume, target, Time.unscaledDeltaTime * .25f);

            SetRoomButtons(state == State.InHouse);
            if ((state == State.InHouse || state == State.InRoom) && garden.CameraRig != null)
            {
                // Fail-safe: nothing in a walkable room should leave input blocked.
                if (garden.IsInputBlocked) { Debug.LogWarning("[CatMe][Session] Input was blocked inside the room; unblocking."); garden.SetInputBlocked(false); }
                // Walk around in first person: the eye follows the owner, and the owner faces where you look (so thrown balls go there).
                var ownerT = garden.OwnerTransform;
                if (eyeAnchor != null) eyeAnchor.position = ownerT.position + Vector3.up * 1.15f;
                if (garden.CurrentMoveMagnitude < .05f) ownerT.rotation = Quaternion.Euler(0f, garden.CameraRig.LookYaw, 0f); // idle: face the view; moving: the normal walk turning
            }
            if (state == State.InHouse)
            {
                if (frameLeading && !frameAtAisle && Vector3.Distance(garden.CatPosition, FrameGuidePoint(4.45f)) < .35f)
                { frameAtAisle = true; garden.SetLead(FrameGuidePoint(1.3f), false); }
                if (frameLeading && NearFrame) { garden.ClearLead(); frameLeading = false; }
                if (feedLeading && NearBowl) { garden.ClearLead(); feedLeading = false; }
                // One contextual action at a time: the nearer of the bowl (when empty) and the frame.
                bool bowlReady = NearBowl && feed == FeedStage.Empty && Time.time >= fullUntil;
                bool frameReady = NearFrame && houseFrame.CanInteract;
                houseAction = bowlReady && (!frameReady || Flat(garden.OwnerTransform.position, bowlCentre) < Flat(garden.OwnerTransform.position, houseFrame.transform.position)) ? HouseAction.Bowl
                    : frameReady ? HouseAction.Frame : HouseAction.None;
                active = houseFrame != null ? houseFrame.GetComponent<InteractionPoint>() : null;
                SetButton(houseAction == HouseAction.Bowl ? "Fill the bowl" : houseAction == HouseAction.Frame ? houseFrame.ButtonLabel : null); return;
            }
            if (state == State.Busy) { SetButton(null); return; }
            if (state == State.Sitting) { SeatedCatWander(); SetButton("Stand up"); return; }
            if (state == State.InRoom)
            {
                if (!rainStopped && Time.time - roomEnteredAt > RoomRainSeconds) { rainStopped = true; director.EndRain(); if (rainedOnEntry) garden.SetAppStatus("The rain has stopped."); }
                // You can always step out after a moment; waiting out the rain is the reward, not a lock.
                SetButton(Time.time - roomEnteredAt > 3f ? "Go out" : null);
                return;
            }
            // Free: nearest interaction point in range
            active = null; float best = float.MaxValue;
            Vector3 p = garden.OwnerTransform.position;
            foreach (var point in director.CurrentInteractions())
            {
                if (point.kind == InteractionKind.ExitTent || point.kind == InteractionKind.HousePhoto) continue;
                float d = Vector3.Distance(p, point.transform.position);
                float reach = point.kind == InteractionKind.EnterHouse ? Mathf.Max(point.radius, 4.5f) : point.radius; // the house prompt shows from further away
                if (d < reach && d < best) { best = d; active = point; }
            }
            SetButton(active != null ? active.label : null);
        }

        void SetButton(string text)
        {
            if (button == null) return;
            bool show = !OnboardingFlow.AnyOpen && (!string.IsNullOrEmpty(text) && !garden.IsInputBlocked || state != State.Free && !string.IsNullOrEmpty(text));
            if (show) label.text = Friendly(text);
            // Ease in and out instead of popping (R-INT-05).
            float target = show ? 1f : 0f;
            buttonGroup.alpha = PlayerPrefs.GetInt("CatMe.ReduceMotion", 0) == 1
                ? target : Mathf.MoveTowards(buttonGroup.alpha, target, Time.unscaledDeltaTime * 7f);
            bool visible = buttonGroup.alpha > .01f || show;
            if (button.gameObject.activeSelf != visible) button.gameObject.SetActive(visible);
            // In the world, the prompt floats above the thing it belongs to (R-INT-04); otherwise it sits at the bottom.
            Vector3? anchor = state == State.InHouse && show ? HouseAnchor() : state == State.Free && active != null && show ? active.transform.position + Vector3.up * 1.15f : (Vector3?)null;
            PlaceButton(anchor);
        }

        Vector3? HouseAnchor()
        {
            if (houseAction == HouseAction.Bowl) return bowlCentre + Vector3.up * .75f;
            if (houseAction == HouseAction.Frame && houseFrame != null) return houseFrame.transform.position;
            return null;
        }

        // One contextual card at a fixed place above the paw button, never over the player (audit 27g, MENUS_AND_ACCOUNT.md section 9).
        // Inside a room the action dock sits at the bottom, so the card stays at the same corner there too.
        void PlaceButton(Vector3? world)
        {
            if (buttonRect == null) return;
            buttonRect.anchorMin = buttonRect.anchorMax = new Vector2(1f, 0f); buttonRect.pivot = new Vector2(1f, 0f);
            buttonRect.anchoredPosition = new Vector2(-40f, state == State.InHouse ? 200f : 210f); buttonRect.localScale = Vector3.one;
        }

        Text debugText;
        State lastLogged = State.Free;
        void LateUpdate()
        {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            // Small diagnostic line while inside a room, so a "nothing moves" report shows exactly why.
            if (state == State.InHouse || state == State.InRoom)
            {
                if (debugText == null) debugText = MakeDebugText();
                debugText.gameObject.SetActive(true);
                debugText.text = "scene=" + System.IO.Path.GetFileName(director.CurrentScenePath) + " room=" + (houseRoom != null ? "Home Kit r3, furniture pieces=" + houseRoom.GetComponentsInChildren<Renderer>(true).Length + ", sofa=" + (houseRoom.transform.Find("Sofa_2Seat") != null) : "NOT LOADED") + " | blocked=" + garden.IsInputBlocked + " | move=" + garden.CurrentMoveMagnitude.ToString("F2") + " | owner=" + garden.OwnerTransform.position.ToString("F1") + " | cat=" + garden.CatPosition.ToString("F1") + " | timeScale=" + Time.timeScale;
            }
            else if (debugText != null) debugText.gameObject.SetActive(false);
#endif
            if (state != lastLogged)
            {
                Debug.Log("[CatMe][Session] Interaction state: " + lastLogged + " -> " + state + " (input blocked: " + garden.IsInputBlocked + ", first person: " + (garden.CameraRig != null && garden.CameraRig.IsFirstPerson) + ")");
                lastLogged = state;
            }
        }

        void OnPressed()
        {
            UiSounds.Click(); Haptics.Tap();
            switch (state)
            {
                case State.Free: if (active == null) return; if (active.kind == InteractionKind.Sit) StartCoroutine(Sit(active)); else if (active.kind == InteractionKind.EnterTent) StartCoroutine(EnterTent()); else if (active.kind == InteractionKind.EnterHouse) StartCoroutine(EnterHouse()); break;
                case State.InHouse:
                    if (houseAction == HouseAction.Bowl) FillBowl();
                    else if (houseAction == HouseAction.Frame && NearFrame && houseFrame.CanInteract) houseFrame.Interact();
                    break;
                case State.Sitting: StartCoroutine(StandUp()); break;
                case State.InRoom: if (Time.time - roomEnteredAt > 3f) StartCoroutine(LeaveTent()); break;
            }
        }

        // ---- sitting -------------------------------------------------------------------------------------------------

        IEnumerator Sit(InteractionPoint point)
        {
            state = State.Busy; sitPoint = point; garden.SetInputBlocked(true);
            Vector3 seat = point.transform.position; Quaternion face = Quaternion.Euler(0f, point.transform.eulerAngles.y, 0f);
            // The camera glides from its follow position down to the seated eye position instead of cutting through black (R-CAM-02).
            garden.PlaceOwnerOnly(seat, face.eulerAngles.y);
            garden.SetOwnerVisible(false);
            EnterEye(seat + Vector3.up * 1.12f, face.eulerAngles.y, 66f, false, .95f);
            // The cat walks round to the front of the player, then settles there.
            Vector3 front = seat + face * Vector3.forward * 1.5f;
            garden.SetCuriosity(front, 4f);
            for (float t = 0f; t < 3f && Vector3.Distance(garden.CatPosition, front) > .35f; t += Time.deltaTime) yield return null;
            garden.SetCuriosity(Vector3.zero, 0f);
            sitFront = front; sitWanderAt = Time.time + 2.5f;   // it settles for a moment, then potters about near the bench
            state = State.Sitting;
        }

        Vector3 sitFront; float sitWanderAt;

        // First free ground spot in front of or beside the seat (grounded: the seat point is raised above the ground).
        Vector3 StandSpot(Vector3 seat, Quaternion face)
        {
            Vector3[] tries = { face * Vector3.forward * 1.3f, face * Vector3.forward * 1.9f, face * Vector3.right * 1.3f, face * Vector3.left * 1.3f, face * Vector3.back * 1.3f };
            foreach (var offset in tries)
            {
                Vector3 p = garden.SampledGroundPosition(seat + offset);
                if (!Physics.CheckSphere(p + Vector3.up * .9f, .3f, GroundProbe.ObstacleMask, QueryTriggerInteraction.Ignore)) return p;
            }
            return garden.SampledGroundPosition(seat + face * Vector3.forward * 1.9f);
        }

        void SeatedCatWander()
        {
            if (Time.time < sitWanderAt) return;
            sitWanderAt = Time.time + Random.Range(3.5f, 6f);
            Vector2 r = Random.insideUnitCircle.normalized * Random.Range(.4f, 1.1f);
            garden.SetCuriosity(garden.SampledGroundPosition(sitFront + new Vector3(r.x, 0f, r.y)), 7f);
        }

        IEnumerator StandUp()
        {
            state = State.Busy;
            yield return director.FadeTo(1f, .4f);
            Quaternion face = Quaternion.Euler(0f, sitPoint.transform.eulerAngles.y, 0f);
            garden.SetCuriosity(Vector3.zero, 0f);
            Vector3 stand = StandSpot(sitPoint.transform.position, face);
            garden.PlaceOwnerOnly(stand, face.eulerAngles.y);
            garden.SetOwnerVisible(true); garden.ReleaseCat();
            garden.PlaceCatBeside(garden.SampledGroundPosition(stand + face * Quaternion.Euler(0f, 90f, 0f) * Vector3.forward * .9f), face.eulerAngles.y);
            ExitEye();
            garden.SetInputBlocked(false);
            yield return director.FadeTo(0f, .4f);
            state = State.Free;
        }

        // ---- tent room -----------------------------------------------------------------------------------------------

        IEnumerator EnterTent()
        {
            state = State.Busy; garden.SetInputBlocked(true);
            var door = active.transform; doorSpot = door.position; doorYaw = door.eulerAngles.y;
            lightingBefore = director.CurrentLightingName ?? "Sunset";
            rainedOnEntry = director.WeatherController != null && director.WeatherController.Intensity > .1f;
            yield return director.FadeTo(1f, .5f);
            var spot = director.FindNamed("Room_Spot");
            if (spot == null) { Debug.LogWarning("[CatMe][Session] Tent room is missing in this place."); garden.SetInputBlocked(false); yield return director.FadeTo(0f, .3f); state = State.Free; yield break; }
            garden.PlaceOwnerOnly(spot.position, spot.eulerAngles.y);
            garden.SetOwnerVisible(true);
            director.SetRainSuppressed(true);
            if (director.Day != null) director.Day.Suspended = true;
            director.Lighting.Apply("TentInterior", 0f);
            EnterRoomView();
            garden.PlaceCatBeside(spot.position + spot.forward * 1.6f + spot.right * .5f, spot.eulerAngles.y + 160f); // the cat follows you around the tent
            roomEnteredAt = Time.time; rainStopped = false;
            yield return director.FadeTo(0f, .6f);
            garden.SetInputBlocked(false);
            PetSounds.PlayAt(Camera.main.transform.position, Pet.Species, PetCue.Hello, .8f);
            garden.SetAppStatus(rainedOnEntry ? "Warm and dry by the fire. Listen to the rain." : "A cosy spot by the fire.");
            // The pet pads over to its cushion by the fire and settles there, facing the flames.
            var cushion = director.FindNamed("Tent_CatSpot");
            if (cushion != null) StartCoroutine(SettleByFire(cushion));
            state = State.InRoom;
        }

        IEnumerator SettleByFire(Transform cushion)
        {
            yield return null;   // started just before the room state is set
            garden.SetCuriosity(cushion.position, 6f);
            for (float t = 0f; t < 6f && state == State.InRoom && Flat(garden.CatPosition, cushion.position) > .3f; t += Time.deltaTime) yield return null;
            if (state != State.InRoom) yield break;
            garden.SetCuriosity(Vector3.zero, 0f);
            garden.HoldCat(garden.SampledGroundPosition(cushion.position), cushion.eulerAngles.y);
            PetSounds.PlayAt(garden.CatPosition, Pet.Species, PetCue.Settle, .6f);
            garden.SetAppStatus(Pet.ByTheFire(PetRoster.NameForText(true)));
        }

        IEnumerator LeaveTent()
        {
            state = State.Busy;
            yield return director.FadeTo(1f, .5f);
            garden.PlaceOwnerOnly(doorSpot, doorYaw);
            garden.SetOwnerVisible(true); garden.ReleaseCat();
            ExitRoomView();
            director.SetRainSuppressed(false);
            if (director.Day != null) director.Day.Suspended = false;
            garden.PlacePair(doorSpot, doorYaw);
            garden.SetInputBlocked(false);
            yield return director.FadeTo(0f, .6f);
            state = State.Free;
        }

        // ---- house room: throw the ball, feed the cat, let it sleep -------------------------------------------------

        IEnumerator EnterHouse()
        {
            state = State.Busy; garden.SetInputBlocked(true);
            var door = active.transform; doorSpot = door.position; doorYaw = door.eulerAngles.y;
            lightingBefore = director.CurrentLightingName ?? "Sunset";
            yield return director.FadeTo(1f, .5f);
            var spot = RoomFind("House_Spot");
            if (spot == null) { Debug.LogWarning("[CatMe][Session] House room is missing in this place."); garden.SetInputBlocked(false); yield return director.FadeTo(0f, .3f); state = State.Free; yield break; }
            houseSpot = spot.position;
            Debug.Log("[CatMe][Session] Entering house room from " + director.CurrentScenePath + " (room prefab loaded: " + (houseRoom != null) + ")");
            garden.PlaceOwnerOnly(houseSpot, spot.eulerAngles.y);
            garden.SetOwnerVisible(true);
            director.SetRainSuppressed(true);
            if (director.Day != null) director.Day.Suspended = true;
            director.Lighting.Apply("HouseInterior", 0f);
            EnterRoomView();
            garden.PlaceCatBeside(houseSpot + spot.forward * 2.0f + spot.right * .6f, spot.eulerAngles.y + 180f);
            catSleeping = false;
            yield return director.FadeTo(0f, .6f);
            garden.SetInputBlocked(false); // walk around with the joystick and look with a drag
            state = State.InHouse;
            FindBowl();
            houseFrame = houseRoom != null ? houseRoom.GetComponentInChildren<HousePhotoFrame>() : null;
            if (houseFrame != null) { houseFrame.PhotoAdded -= OnFramePhotoAdded; houseFrame.PhotoAdded += OnFramePhotoAdded; }
            var leader = FindAnyObjectByType<CompanionLeader>();
            if (leader != null && leader.WantsDinner && bowl != null && feed == FeedStage.Empty)
            {
                Feed();   // a hungry pet goes straight to its bowl; the frame can wait
            }
            else if (houseFrame != null && !houseFrame.HasPhoto)
            {
                // The coffee table blocks a diagonal route. First use the open aisle beside it.
                garden.ReleaseCat(); garden.SetLead(FrameGuidePoint(4.45f), false); frameLeading = true; frameAtAisle = false;
                garden.SetAppStatus("Follow me. Let's put a photo in the frame.");
            }
        }

        IEnumerator LeaveHouse()
        {
            state = State.Busy;
            yield return director.FadeTo(1f, .5f);
            StopEating(); feedLeading = false;
            garden.ReleaseCat(); garden.ClearLead(); frameLeading = false; garden.SetCuriosity(Vector3.zero, 0f);
            garden.PlaceOwnerOnly(doorSpot, doorYaw);
            garden.SetOwnerVisible(true);
            ExitRoomView();
            director.SetRainSuppressed(false);
            if (director.Day != null) director.Day.Suspended = false;
            garden.PlacePair(doorSpot, doorYaw);
            garden.SetInputBlocked(false);
            yield return director.FadeTo(0f, .6f);
            state = State.Free;
        }

        void ThrowBall() { StopEating(); feedLeading = false; garden.ClearLead(); frameLeading = false; garden.ReleaseCat(); catSleeping = false; garden.ThrowBall(); }

        // ---- feeding ---------------------------------------------------------------------------------------------------

        void FindBowl()
        {
            bowl = RoomFind("Dog bowl") ?? RoomFind("Cat_Bowl");
            if (bowl == null) return;
            var renderers = bowl.GetComponentsInChildren<Renderer>(true);
            if (renderers.Length > 0)
            {
                Bounds b = renderers[0].bounds; foreach (var r in renderers) b.Encapsulate(r.bounds);
                bowlCentre = new Vector3(b.center.x, b.min.y, b.center.z); bowlRadius = Mathf.Max(.12f, Mathf.Min(b.extents.x, b.extents.z));
                if (food == null) food = BuildFood(bowl.parent, new Vector3(b.center.x, b.min.y + b.size.y * .5f, b.center.z), bowlRadius * .62f, b.size.y);
            }
            else { bowlCentre = bowl.position; bowlRadius = .25f; if (food == null) food = BuildFood(bowl.parent, bowl.position + Vector3.up * .06f, .15f, .12f); }
            food.gameObject.SetActive(feed != FeedStage.Empty);
        }

        // A small brown mound of kibble that sits inside the bowl (built once; hidden while the bowl is empty).
        static Transform BuildFood(Transform parent, Vector3 centre, float radius, float bowlHeight)
        {
            var root = new GameObject("Bowl food").transform; root.SetParent(parent, true); root.position = centre;
            Material light = null, dark = null;
            var rng = new System.Random(11);
            for (int i = 0; i < 30; i++)
            {
                var piece = GameObject.CreatePrimitive(PrimitiveType.Sphere); piece.name = "Kibble"; piece.transform.SetParent(root, false);
                Destroy(piece.GetComponent<Collider>());
                var renderer = piece.GetComponent<Renderer>();
                if (light == null) { light = new Material(renderer.sharedMaterial) { color = new Color(.55f, .33f, .16f) }; dark = new Material(light) { color = new Color(.40f, .23f, .11f) }; }
                renderer.sharedMaterial = i % 3 == 0 ? dark : light;
                renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                float angle = (float)rng.NextDouble() * Mathf.PI * 2f, d = Mathf.Sqrt((float)rng.NextDouble()) * radius;
                float mound = (1f - (d / Mathf.Max(radius, .01f)) * (d / Mathf.Max(radius, .01f))) * bowlHeight * .35f;
                float size = Mathf.Lerp(.035f, .05f, (float)rng.NextDouble()) * Mathf.Clamp(radius / .25f, .8f, 1.6f);
                piece.transform.localPosition = new Vector3(Mathf.Cos(angle) * d, mound, Mathf.Sin(angle) * d);
                piece.transform.localScale = new Vector3(size, size * .7f, size);
            }
            root.gameObject.SetActive(false);
            return root;
        }

        // Dock "Feed": the pet leads you to the bowl; at the bowl the contextual button fills it.
        void Feed()
        {
            garden.ClearLead(); frameLeading = false;
            if (bowl == null) FindBowl();
            if (bowl == null) return;
            string name = PetRoster.NameForText(true);
            if (feed == FeedStage.Filled) { StartEating(); return; }
            if (feed == FeedStage.Eating) return;
            if (Time.time < fullUntil) { garden.SetAppStatus(Pet.StillFull(name)); return; }
            if (NearBowl) { FillBowl(); return; }
            StopEating(); garden.ReleaseCat(); catSleeping = false;
            garden.SetLead(EatSpot, false); feedLeading = true;
            garden.SetAppStatus(Pet.HungryAtBowl(name));
            PetSounds.PlayAt(garden.CatPosition, Pet.Species, PetCue.Excited, .6f);
        }

        void FillBowl()
        {
            if (bowl == null || feed != FeedStage.Empty) return;
            feed = FeedStage.Filled; feedLeading = false; garden.ClearLead();
            Haptics.Tap();
            StartCoroutine(PourFood());
            StartEating();
        }

        IEnumerator PourFood()
        {
            food.gameObject.SetActive(true);
            bool reduced = PlayerPrefs.GetInt("CatMe.ReduceMotion", 0) == 1;
            for (int i = 0; i < food.childCount; i++)
            {
                var piece = food.GetChild(i); piece.gameObject.SetActive(true);
                if (!reduced) { Vector3 rest = piece.localPosition; piece.localPosition = rest + Vector3.up * .25f; StartCoroutine(Drop(piece, rest)); }
                if (!reduced && i % 3 == 0) yield return null;
            }
        }

        static IEnumerator Drop(Transform piece, Vector3 rest)
        {
            Vector3 from = piece.localPosition;
            for (float t = 0f; t < 1f && piece != null; t += Time.deltaTime / .22f) { piece.localPosition = Vector3.Lerp(from, rest, t * t); yield return null; }
            if (piece != null) piece.localPosition = rest;
        }

        void StartEating()
        {
            if (eating != null) StopCoroutine(eating);
            eating = StartCoroutine(EatRoutine());
        }

        void StopEating()
        {
            if (eating == null) return;
            StopCoroutine(eating); eating = null;
            if (feed == FeedStage.Eating) feed = FeedStage.Filled;   // the food stays; Feed resumes it
            garden.SetCuriosity(Vector3.zero, 0f); garden.ReleaseCat();
        }

        IEnumerator EatRoutine()
        {
            string name = PetRoster.NameForText(true);
            garden.ClearLead(); garden.ReleaseCat(); catSleeping = false;
            Vector3 spot = EatSpot;
            garden.SetCuriosity(spot, 10f);
            // Food from the store's bag makes dinner a little special.
            var treat = StoreCatalog.FirstOwned(StoreCategory.Food, PetRoster.Active.Kind);
            garden.SetAppStatus((treat != null ? "Dinner time: " + treat.name.ToLowerInvariant() : "Dinner time") + ". Here comes " + PetRoster.NameForText() + ".");
            for (float t = 0f; t < 10f && Flat(garden.CatPosition, spot) > .35f; t += Time.deltaTime) yield return null;
            garden.SetCuriosity(Vector3.zero, 0f);
            Vector3 look = bowlCentre - spot; look.y = 0f;
            garden.HoldCat(spot, look.sqrMagnitude > .001f ? Quaternion.LookRotation(look).eulerAngles.y : 0f);
            feed = FeedStage.Eating;
            PetSounds.PlayAt(garden.CatPosition, Pet.Species, PetCue.Eat, .7f);
            garden.SetAppStatus(Pet.Eating(name));
            // The mound goes down as the pet eats: pieces disappear one by one.
            int total = food.childCount, left = 0;
            for (int i = 0; i < total; i++) if (food.GetChild(i).gameObject.activeSelf) left++;
            float step = Pet.EatSeconds / Mathf.Max(1, left);
            for (int i = total - 1; i >= 0; i--)
            {
                var piece = food.GetChild(i).gameObject; if (!piece.activeSelf) continue;
                yield return new WaitForSeconds(step);
                piece.SetActive(false);
            }
            food.gameObject.SetActive(false);
            feed = FeedStage.Empty; fullUntil = Time.time + Pet.FullSeconds; eating = null;
            FindAnyObjectByType<CompanionLeader>()?.HadDinner();
            garden.ReleaseCat();
            FindAnyObjectByType<CatPetInput>()?.PopHearts(StoreCatalog.FirstOwned(StoreCategory.Food, PetRoster.Active.Kind) != null ? 5 : 3);
            PetSounds.PlayAt(garden.CatPosition, Pet.Species, PetCue.Happy, .5f);
            garden.SetAppStatus(Pet.AteAll(name));
        }

        // ---- photo frame: the pet comes to look at a new picture ----------------------------------------------------

        void OnFramePhotoAdded()
        {
            if (state != State.InHouse || houseFrame == null || eating != null) return;
            StartCoroutine(AdmireFrame());
        }

        IEnumerator AdmireFrame()
        {
            garden.ClearLead(); frameLeading = false; garden.ReleaseCat(); catSleeping = false;
            Vector3 spot = FrameGuidePoint(1.1f);
            garden.SetCuriosity(spot, 8f);
            for (float t = 0f; t < 8f && Flat(garden.CatPosition, spot) > .35f && state == State.InHouse; t += Time.deltaTime) yield return null;
            if (state != State.InHouse) yield break;
            garden.SetCuriosity(Vector3.zero, 0f);
            Vector3 look = houseFrame.transform.position - spot; look.y = 0f;
            garden.HoldCat(spot, look.sqrMagnitude > .001f ? Quaternion.LookRotation(look).eulerAngles.y : 0f);
            PetSounds.PlayAt(garden.CatPosition, Pet.Species, PetCue.Hello, .6f);
            garden.SetAppStatus(Pet.SeesPhoto(PetRoster.NameForText(true)));
            FindAnyObjectByType<CatPetInput>()?.PopHearts(2);
            yield return new WaitForSeconds(2.5f);
            if (state == State.InHouse && eating == null) garden.ReleaseCat();
        }

        void Sleep()
        {
            StopEating(); feedLeading = false;
            garden.ClearLead(); frameLeading = false;
            var bed = RoomFind("Cat_Bed"); if (bed == null) return;
            StartCoroutine(SleepRoutine(bed));
        }

        IEnumerator SleepRoutine(Transform bed)
        {
            garden.ReleaseCat(); catSleeping = false;
            garden.SetCuriosity(bed.position, 12f);
            for (float t = 0f; t < 12f && Vector3.Distance(garden.CatPosition, bed.position) > .35f; t += Time.deltaTime) yield return null;
            garden.SetCuriosity(Vector3.zero, 0f);
            garden.HoldCat(bed.position + Vector3.up * .12f, bed.eulerAngles.y);   // neutral pose: no sleep animation exists yet
            catSleeping = true; garden.SetAppStatus(Pet.Asleep(PetRoster.NameForText(true)));
            PetSounds.PlayAt(garden.CatPosition, Pet.Species, PetCue.Settle, .5f);
        }

        void SetRoomButtons(bool show)
        {
            if (roomButtons == null) return;
            if (roomButtons.gameObject.activeSelf != show) roomButtons.gameObject.SetActive(show);
        }

        // ---- first-person helper -------------------------------------------------------------------------------------

        void EnterEye(Vector3 position, float yaw, float fov, bool walkable = false, float glide = 0f)
        {
            if (eyeAnchor == null) eyeAnchor = new GameObject("FirstPersonEye").transform;
            eyeAnchor.SetPositionAndRotation(position, Quaternion.Euler(0f, yaw, 0f));
            garden.CameraRig?.EnterFirstPerson(eyeAnchor, fov, walkable, glide);
        }

        void ExitEye() { garden.CameraRig?.ExitFirstPerson(); }

        // Inside the tent and house you stay visible, with a close shoulder-height follow camera instead of a view from inside the head.
        readonly CameraProfile roomProfile = new CameraProfile { name = "Room", distance = 1.55f, height = 1.7f, pitch = 12f, fov = 62f, blendIn = .15f, zoomMin = 1.3f, zoomMax = 2.6f };   // F5: inside, the same controls with a tighter zoom range
        void EnterRoomView() { PetSounds.Indoors = true; var rig = garden.CameraRig; if (rig == null) return; rig.PushProfile(roomProfile); rig.Snap(); }
        void ExitRoomView() { PetSounds.Indoors = false; var rig = garden.CameraRig; if (rig == null) return; rig.PopProfile(roomProfile); }

        // ---- UI ------------------------------------------------------------------------------------------------------

        void BuildButton(Transform canvasRoot)
        {
            // In-world prompt: a glass pill with the action and an arrow disc (docs/production/07 in-game controls).
            var go = new GameObject("Interaction button", typeof(RectTransform), typeof(Image), typeof(Button), typeof(CanvasGroup)); go.transform.SetParent(canvasRoot, false);
            var r = go.GetComponent<RectTransform>(); r.anchorMin = r.anchorMax = new Vector2(.5f, 0f); r.pivot = new Vector2(.5f, 0f); r.anchoredPosition = new Vector2(0f, 120f); r.sizeDelta = new Vector2(400f, 92f);
            var image = go.GetComponent<Image>(); CatMe.Garden.CatMeUIStyle.ApplySurface(image, CatMeUIStyle.PaperGlass);
            CatMe.Garden.CatMeUIStyle.SetElevation(r, CatMe.Garden.CatMeUIStyle.Elevation.Raised);
            buttonRect = r; canvasRect = canvasRoot as RectTransform; buttonGroup = go.GetComponent<CanvasGroup>(); buttonGroup.alpha = 0f;
            button = go.GetComponent<Button>(); button.targetGraphic = image; button.onClick.AddListener(() => { UiSounds.Click(); Haptics.Tap(); OnPressed(); });
            var t = new GameObject("Label", typeof(RectTransform), typeof(Text)); t.transform.SetParent(go.transform, false);
            var tr = t.GetComponent<RectTransform>(); tr.anchorMin = Vector2.zero; tr.anchorMax = Vector2.one; tr.offsetMin = new Vector2(34f, 0f); tr.offsetMax = new Vector2(-96f, 0f);
            label = t.GetComponent<Text>(); label.alignment = TextAnchor.MiddleLeft; label.fontSize = 34; label.fontStyle = CatMe.Garden.CatMeUIStyle.Weight(FontStyle.Bold); label.color = CatMe.Garden.CatMeUIStyle.Pine; label.raycastTarget = false;
            label.font = CatMe.Garden.CatMeUIStyle.BoldFont;
            var disc = new GameObject("Arrow disc", typeof(RectTransform), typeof(Image)); disc.transform.SetParent(go.transform, false);
            var dr = (RectTransform)disc.transform; dr.anchorMin = dr.anchorMax = new Vector2(1f, .5f); dr.pivot = new Vector2(.5f, .5f); dr.sizeDelta = new Vector2(66f, 66f); dr.anchoredPosition = new Vector2(-46f, 0f);
            var di = disc.GetComponent<Image>(); di.color = CatMe.Garden.CatMeUIStyle.Pine; di.raycastTarget = false;
            CatMe.Garden.UiKit.Icon(disc.transform, "arrow", Vector2.zero, 40f, Color.white);
            go.SetActive(false);
            // Room dock (house interior): one primary action and three quiet ones on a glass pill.
            var row = new GameObject("House actions", typeof(RectTransform), typeof(Image)); row.transform.SetParent(canvasRoot, false);
            var rr = row.GetComponent<RectTransform>(); rr.anchorMin = rr.anchorMax = new Vector2(.5f, 0f); rr.pivot = new Vector2(.5f, 0f); rr.anchoredPosition = new Vector2(0f, 60f); rr.sizeDelta = new Vector2(1180f, 124f);
            CatMe.Garden.CatMeUIStyle.ApplySurface(row.GetComponent<Image>(), new Color(.98f, .99f, .98f, .9f));
            CatMe.Garden.CatMeUIStyle.SetElevation(rr, CatMe.Garden.CatMeUIStyle.Elevation.Floating);
            string[] names = { "Throw ball", "Feed", "Bed", "Go out" };
            System.Action[] actions = { ThrowBall, Feed, Sleep, () => { if (state == State.InHouse) StartCoroutine(LeaveHouse()); } };
            for (int i = 0; i < names.Length; i++)
            {
                int index = i;
                var tint = i == 0 ? CatMe.Garden.CatMeUIStyle.Pine : CatMeUIStyle.Tile;
                var b = new GameObject(names[i], typeof(RectTransform), typeof(Image), typeof(Button)); b.transform.SetParent(row.transform, false);
                var br = (RectTransform)b.transform; br.anchorMin = br.anchorMax = new Vector2(.5f, .5f); br.sizeDelta = new Vector2(260f, 84f); br.anchoredPosition = new Vector2((i - 1.5f) * 282f, 0f);
                var btn = b.GetComponent<Button>(); btn.onClick.AddListener(() => actions[index]());
                var tt = new GameObject("Label", typeof(RectTransform), typeof(Text)); tt.transform.SetParent(b.transform, false);
                var tr2 = (RectTransform)tt.transform; tr2.anchorMin = Vector2.zero; tr2.anchorMax = Vector2.one; tr2.offsetMin = tr2.offsetMax = Vector2.zero;
                var txt = tt.GetComponent<Text>(); txt.text = names[i]; txt.alignment = TextAnchor.MiddleCenter; txt.fontSize = 30; txt.raycastTarget = false;
                CatMe.Garden.CatMeUIStyle.StyleButton(btn, tint, i == 0);   // adds the shared press motion, tick sound and haptic
            }
            roomButtons = row.transform; row.SetActive(false);
        }

        // Friendly wording for the floating prompt (point labels in the scene stay short).
        static string Friendly(string text)
        {
            switch (text)
            {
                case "Sit": return "Sit down";
                case "Get in": return "Step inside";
                case "Get in the house": return "Go inside";
                default: return text;
            }
        }

        Text MakeDebugText()
        {
            var go = new GameObject("Room debug", typeof(RectTransform), typeof(Text)); go.transform.SetParent(button.transform.parent, false);
            var r = go.GetComponent<RectTransform>(); r.anchorMin = new Vector2(0f, 1f); r.anchorMax = new Vector2(0f, 1f); r.pivot = new Vector2(0f, 1f); r.anchoredPosition = new Vector2(16f, -16f); r.sizeDelta = new Vector2(1300f, 40f);
            var t = go.GetComponent<Text>(); t.fontSize = 20; t.color = new Color(1f, 1f, .6f); t.raycastTarget = false; t.font = CatMe.Garden.CatMeUIStyle.UiFont;
            return t;
        }

        // Soft broadband noise as a stand-in until a licensed rain loop is added at Resources/CatMeAudio/rain-light.
        static AudioClip MakeRainNoise()
        {
            const int rate = 22050, seconds = 4; var data = new float[rate * seconds]; var rng = new System.Random(7); float lp = 0f, lp2 = 0f;
            for (int i = 0; i < data.Length; i++)
            {
                float n = (float)(rng.NextDouble() * 2.0 - 1.0);
                lp += (n - lp) * .35f; lp2 += (lp - lp2) * .5f;
                data[i] = (lp - lp2) * 2.2f;
            }
            // Cross-fade the ends so the loop has no click.
            int fade = rate / 10;
            for (int i = 0; i < fade; i++) { float a = i / (float)fade; data[i] = data[i] * a + data[data.Length - fade + i] * (1f - a); }
            var clip = AudioClip.Create("rain-noise", data.Length, 1, rate, false); clip.SetData(data, 0); return clip;
        }
    }
}
