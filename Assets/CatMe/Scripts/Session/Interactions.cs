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
        float roomEnteredAt; bool rainStopped;
        AudioSource rainAudio; AudioClip rainClip; AudioClip meow;
        string lightingBefore = "Sunset";
        Vector3 doorSpot;
        Transform roomButtons; Button feedButton, sleepButton;
        bool catSleeping;
        Vector3 houseSpot;
        GameObject houseRoom;
        HousePhotoFrame houseFrame;
        bool frameLeading, frameAtAisle;
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
            meow = Resources.Load<AudioClip>("CatMeAudio/cat-meow");
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
            if (state == State.InRoom && !rainStopped) target = .24f;
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
                active = houseFrame != null ? houseFrame.GetComponent<InteractionPoint>() : null;
                SetButton(NearFrame && houseFrame.CanInteract ? houseFrame.ButtonLabel : null); return;
            }
            if (state == State.Busy) { SetButton(null); return; }
            if (state == State.Sitting) { SeatedCatWander(); SetButton("Stand up"); return; }
            if (state == State.InRoom)
            {
                if (!rainStopped && Time.time - roomEnteredAt > RoomRainSeconds) { rainStopped = true; director.EndRain(); garden.SetAppStatus("The rain has stopped."); }
                SetButton(rainStopped ? "Go out" : null);
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
            Vector3? anchor = state == State.InHouse && houseFrame != null && show ? houseFrame.transform.position : state == State.Free && active != null && show ? active.transform.position + Vector3.up * 1.15f : (Vector3?)null;
            PlaceButton(anchor);
        }

        void PlaceButton(Vector3? world)
        {
            if (buttonRect == null) return;
            var cam = Camera.main;
            if (world.HasValue && cam != null)
            {
                Vector3 sp = cam.WorldToScreenPoint(world.Value);
                if (sp.z > .3f)
                {
                    RectTransformUtility.ScreenPointToLocalPointInRectangle(canvasRect, sp, null, out Vector2 local);
                    buttonRect.anchorMin = buttonRect.anchorMax = new Vector2(.5f, .5f); buttonRect.pivot = new Vector2(.5f, .5f);
                    buttonRect.anchoredPosition = local; buttonRect.localScale = Vector3.one * .85f;
                    return;
                }
            }
            buttonRect.anchorMin = buttonRect.anchorMax = new Vector2(.5f, 0f); buttonRect.pivot = new Vector2(.5f, 0f);
            buttonRect.anchoredPosition = new Vector2(0f, 120f); buttonRect.localScale = Vector3.one;
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
                case State.InHouse: if (NearFrame && houseFrame.CanInteract) houseFrame.Interact(); break;
                case State.Sitting: StartCoroutine(StandUp()); break;
                case State.InRoom: if (rainStopped) StartCoroutine(LeaveTent()); break;
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
            if (meow != null && PlayerPrefs.GetInt("CatMe.SoundEnabled", 1) == 1) AudioSource.PlayClipAtPoint(meow, Camera.main.transform.position, .8f);
            garden.SetAppStatus("Listen to the rain.");
            state = State.InRoom;
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
            houseFrame = houseRoom != null ? houseRoom.GetComponentInChildren<HousePhotoFrame>() : null;
            if (houseFrame != null)
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

        void ThrowBall() { garden.ClearLead(); frameLeading = false; garden.ReleaseCat(); catSleeping = false; garden.ThrowBall(); }

        void Feed()
        {
            garden.ClearLead(); frameLeading = false;
            var bowl = RoomFind("Cat_Bowl"); if (bowl == null) return;
            garden.ReleaseCat(); catSleeping = false;
            garden.SetCuriosity(bowl.position + bowl.forward * .45f, 9f);   // walks to the bowl and stays there
            garden.SetAppStatus("Dinner time.");
            if (meow != null && PlayerPrefs.GetInt("CatMe.SoundEnabled", 1) == 1) AudioSource.PlayClipAtPoint(meow, houseSpot, .7f);
        }

        void Sleep()
        {
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
            catSleeping = true; garden.SetAppStatus(PetRoster.NameForText(true) + " is asleep.");
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
        readonly CameraProfile roomProfile = new CameraProfile { name = "Room", distance = 1.55f, height = 1.7f, pitch = 12f, fov = 62f, blendIn = .15f };
        void EnterRoomView() { var rig = garden.CameraRig; if (rig == null) return; rig.PushProfile(roomProfile); rig.Snap(); }
        void ExitRoomView() { var rig = garden.CameraRig; if (rig == null) return; rig.PopProfile(roomProfile); }

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
                case "Get in the house": return "Go into the house";
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
