using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

namespace CatMe.Garden
{
    /// <summary>Camera numbers for one framing. Zones push these while the owner is inside.</summary>
    [System.Serializable]
    public sealed class CameraProfile
    {
        public string name = "Default";
        public float distance = 2.9f, height = 1.6f, pitch = 8f, fov = 50f;
        public float screenOffsetX;          // -1..1, positive moves the pair to the right of the screen
        public float lookAheadScale = 1f;
        public float blendIn = 1.5f;
        /// <summary>F5: the only thing a zone may change: the zoom range the player can choose within.</summary>
        public float zoomMin = CameraMath.ZoomMin, zoomMax = CameraMath.ZoomMax;

        public static CameraProfile Default => new CameraProfile();
        public static CameraProfile Named(string profile)
        {
            switch (profile)
            {
                case "GardenClose": return new CameraProfile { name = profile, distance = 2.6f, height = 1.55f, pitch = 9f, fov = 48f };
                case "GateReveal": return new CameraProfile { name = profile, distance = 3.1f, height = 1.5f, pitch = 6f, fov = 51f };
                case "Riverside": return new CameraProfile { name = profile, distance = 3.1f, height = 1.65f, pitch = 8f, fov = 51f, screenOffsetX = .12f };
                case "Woodland": return new CameraProfile { name = profile, distance = 2.8f, height = 1.6f, pitch = 7f, fov = 50f, zoomMax = 4.5f };
                case "ShelterInside": return new CameraProfile { name = profile, distance = 3.2f, height = 1.6f, pitch = 5f, fov = 51f, screenOffsetX = .12f, zoomMax = 3.2f };
                case "Overlook": return new CameraProfile { name = profile, distance = 3.8f, height = 1.9f, pitch = 6f, fov = 53f, screenOffsetX = .15f };
                case "Homecoming": return new CameraProfile { name = profile, distance = 3.0f, height = 1.6f, pitch = 7f, fov = 50f };
                case "CatEye": return new CameraProfile { name = profile, distance = 1.6f, height = .45f, pitch = 2f, fov = 58f };
                default: return Default;
            }
        }
    }

    /// <summary>Calm trailing camera: low, shows the horizon, damped, collision aware.</summary>
    public sealed class CompanionCamera : MonoBehaviour
    {
        Transform owner, cat;
        Camera cam;
        readonly List<CameraProfile> stack = new List<CameraProfile>();
        CameraProfile baseProfile = CameraProfile.Default;
        // Smoothed live values
        float distance = 2.9f, height = 1.6f, pitch = 8f, fov = 50f, offsetX, lookAhead = 1f;
        float restYaw, yawOffset, pitchOffset, lastLookInputAt = -100f, extraHeight, extraHeightTarget, extraHeightVelocity;
        Vector3 positionVelocity, lastOwnerPosition, ownerVelocity, lookAheadOffset, lookAheadVelocity;
        float yawVelocity, fovVelocity, distanceVelocity, heightVelocity, pitchVelocity, offsetVelocity;
        bool initialised, lookActive;
        int lookTouchId = -1;
        Vector2 lookLast;

        /// <summary>Yaw the movement stick is relative to. Does not change while the player drags the view.</summary>
        public float ReferenceYaw => restYaw;
        public bool Frozen { get; set; }
        Transform fpAnchor; float fpFov = 70f;
        public bool IsFirstPerson => fpAnchor != null;
        public float LookYaw => restYaw + yawOffset;

        // F5: the player owns the third-person camera. restYaw is the orbit yaw (drag), orbitPitch the pitch (drag), zoom the
        // chosen distance (pinch, remembered). Nothing else turns or moves it, except collision pull-in.
        const string ZoomKey = "CatMe.Camera.Zoom";
        float orbitPitch = 18f, zoom = 3.2f, pinchLast;
        bool pinching, closeUp;
        /// <summary>F11: pinched in past the normal range to frame the player's face and the pet.</summary>
        public bool InCloseUp => closeUp;

        /// <summary>Eye-level view from a fixed anchor; drag to look around a little. The owner model is hidden by the caller.</summary>
        bool fpWalkable;
        // Glide into first person (R-CAM-02), situational idle framing (R-CAM-01), walk sway (R-CAM-03).
        float fpBlend = 1f, fpBlendTime = .9f, idleTime, idleBlend, walkPhase;
        Vector3 fpStartPos; Quaternion fpStartRot;
        UnityEngine.Rendering.Volume dofVolume; UnityEngine.Rendering.Universal.DepthOfField dof;
        public float IdleSeconds => idleTime;
        /// <param name="walkable">True: same controls as outdoors (joystick, turning by walking, drag to look); false: seated, look only.</param>
        public void EnterFirstPerson(Transform anchor, float fov, bool walkable = false, float glideSeconds = 0f)
        {
            fpStartPos = transform.position; fpStartRot = transform.rotation; fpBlendTime = Mathf.Max(.05f, glideSeconds); fpBlend = glideSeconds > 0f && PlayerPrefs.GetInt("CatMe.ReduceMotion", 0) == 0 ? 0f : 1f;
            fpAnchor = anchor; fpFov = fov; fpWalkable = walkable; yawOffset = 0f; pitchOffset = 0f; restYaw = anchor.eulerAngles.y;
            lastOwnerPosition = owner != null ? owner.position : Vector3.zero; ownerVelocity = Vector3.zero;
            if (cam != null) cam.nearClipPlane = .05f;
        }
        public void ExitFirstPerson()
        {
            // No teleport here, so blend back to the trailing view instead of cutting (LateUpdate eases the position and rotation).
            fpAnchor = null; SetDof(0f); if (cam != null) cam.nearClipPlane = .1f;
            if (owner != null) { restYaw = owner.eulerAngles.y; lastOwnerPosition = owner.position; }   // face the way the player sits
            yawOffset = 0f; pitchOffset = 0f; ownerVelocity = Vector3.zero; lookAheadOffset = Vector3.zero; positionVelocity = Vector3.zero;
        }

        public void Initialize(Transform ownerTransform, Transform catTransform)
        {
            owner = ownerTransform; cat = catTransform; cam = GetComponent<Camera>();
            restYaw = owner != null ? owner.eulerAngles.y : 0f;
            lastOwnerPosition = owner != null ? owner.position : Vector3.zero;
            zoom = Mathf.Clamp(PlayerPrefs.GetFloat(ZoomKey, 3.2f), CameraMath.ZoomMin, CameraMath.ZoomMax);
            initialised = true;
            SnapNow();
        }

        public void SetBaseProfile(CameraProfile profile) { baseProfile = profile ?? CameraProfile.Default; }
        public void PushProfile(CameraProfile profile) { if (profile != null && !stack.Contains(profile)) stack.Add(profile); }
        public void PopProfile(CameraProfile profile) { stack.Remove(profile); }
        public void PopAll() { stack.Clear(); }
        /// <summary>Raises the camera for place arrival/exit moments. Reduced motion ignores it.</summary>
        public void SetExtraHeight(float meters) { extraHeightTarget = PlayerPrefs.GetInt("CatMe.ReduceMotion", 0) == 1 ? 0f : meters; }
        public void SetRestYaw(float yaw) { restYaw = yaw; yawOffset = 0f; }

        CameraProfile Active => stack.Count > 0 ? stack[stack.Count - 1] : baseProfile;

        /// <summary>The session fade's opacity; a cut while it covers the screen needs no fade of its own.</summary>
        public static System.Func<float> CoverAlpha;
        public const float CutFadeSeconds = .25f;
        static CanvasGroup cutFade;
        bool cutFading;

        /// <summary>
        /// Moves to the framing for the current owner pose (after a teleport). Under the session fade, or with Reduce motion, it
        /// cuts at once; otherwise the screen dips to warm white for 0.25 s around the cut.
        /// </summary>
        public void Snap()
        {
            if (!initialised || owner == null) return;
            bool covered = CoverAlpha != null && CoverAlpha() > .95f;
            if (covered || cutFading || !isActiveAndEnabled || PlayerPrefs.GetInt("CatMe.ReduceMotion", 0) == 1) { SnapNow(); return; }
            StartCoroutine(FadeCut());
        }

        System.Collections.IEnumerator FadeCut()
        {
            cutFading = true;
            var group = CutFadeOverlay();
            float half = CutFadeSeconds * .5f;
            for (float t = 0f; t < half; t += Time.unscaledDeltaTime) { group.alpha = t / half; yield return null; }
            group.alpha = 1f;
            SnapNow();
            yield return null;
            for (float t = 0f; t < half; t += Time.unscaledDeltaTime) { group.alpha = 1f - t / half; yield return null; }
            group.alpha = 0f;
            cutFading = false;
        }

        static CanvasGroup CutFadeOverlay()
        {
            if (cutFade != null) return cutFade;
            var go = new GameObject("Camera cut fade", typeof(Canvas), typeof(CanvasGroup));
            DontDestroyOnLoad(go);
            var canvas = go.GetComponent<Canvas>(); canvas.renderMode = RenderMode.ScreenSpaceOverlay; canvas.sortingOrder = 900;
            var image = new GameObject("Fill", typeof(RectTransform), typeof(UnityEngine.UI.Image));
            image.transform.SetParent(go.transform, false);
            var r = (RectTransform)image.transform; r.anchorMin = Vector2.zero; r.anchorMax = Vector2.one; r.offsetMin = r.offsetMax = Vector2.zero;
            var img = image.GetComponent<UnityEngine.UI.Image>(); img.color = new Color(.969f, .965f, .949f, 1f); img.raycastTarget = false;   // the session fade's warm white
            cutFade = go.GetComponent<CanvasGroup>(); cutFade.alpha = 0f; cutFade.blocksRaycasts = false; cutFade.interactable = false;
            return cutFade;
        }

        /// <summary>Jump straight to the framing for the current owner pose, with no fade.</summary>
        public void SnapNow()
        {
            if (!initialised || owner == null) return;
            restYaw = owner.eulerAngles.y; yawOffset = 0f; pitchOffset = 0f;
            CameraProfile p = Active;
            distance = Mathf.Clamp(zoom, p.zoomMin, p.zoomMax); fov = 50f; offsetX = 0f; lookAhead = 0f;
            lastOwnerPosition = owner.position; ownerVelocity = Vector3.zero; lookAheadOffset = Vector3.zero;
            Pose pose = Desired(0f, out Vector3 focus);
            transform.SetPositionAndRotation(pose.position, pose.rotation);
            if (cam != null) cam.fieldOfView = fov;
        }

        void LateUpdate()
        {
            if (!initialised || owner == null || Frozen) return;
            if (fpAnchor != null)
            {
                float fdt = Mathf.Max(Time.unscaledDeltaTime, 0.0001f);
                if (fpWalkable && Time.timeScale > 0f)
                {
                    // Same as outdoors: you turn by walking, and a drag looks around and eases back.
                    Vector3 v = (owner.position - lastOwnerPosition) / fdt; lastOwnerPosition = owner.position;
                    ownerVelocity = Vector3.Lerp(ownerVelocity, v, 1f - Mathf.Exp(-6f * fdt));
                    ReadLookDrag(fdt);
                    FollowYaw(fdt, PlayerPrefs.GetInt("CatMe.ReduceMotion", 0) == 1);
                    yawOffset = Mathf.Clamp(yawOffset, -90f, 90f); pitchOffset = Mathf.Clamp(pitchOffset, -50f, 50f);
                }
                else
                {
                    ReadLookDrag(fdt);
                    lastLookInputAt = Time.unscaledTime; // seated: hold the view where you leave it
                    yawOffset = Mathf.Clamp(yawOffset, -140f, 140f); pitchOffset = Mathf.Clamp(pitchOffset, -60f, 60f);
                }
                Vector3 eye = fpAnchor.position;
                if (fpWalkable && PlayerPrefs.GetInt("CatMe.ReduceMotion", 0) == 0)
                {
                    // A few centimetres of walk bob while moving, and a slow breath while still.
                    float speed = new Vector3(ownerVelocity.x, 0f, ownerVelocity.z).magnitude;
                    walkPhase += speed * fdt * 5.2f;
                    eye.y += Mathf.Sin(walkPhase) * .018f * Mathf.Clamp01(speed) + Mathf.Sin(Time.unscaledTime * 1.3f) * .004f;
                }
                Quaternion eyeRot = Quaternion.Euler(pitchOffset, restYaw + yawOffset, 0f);
                if (fpBlend < 1f)
                {
                    fpBlend = Mathf.Min(1f, fpBlend + fdt / fpBlendTime);
                    float k = Mathf.SmoothStep(0f, 1f, fpBlend);
                    transform.SetPositionAndRotation(Vector3.Lerp(fpStartPos, eye, k), Quaternion.Slerp(fpStartRot, eyeRot, k));
                }
                else transform.SetPositionAndRotation(eye, eyeRot);
                if (cam != null) cam.fieldOfView = Mathf.Lerp(cam.fieldOfView, fpFov, fpBlend < 1f ? 4f * fdt : 1f);
                SetDof(!fpWalkable ? 1f : 0f);   // seated: background softens, the cat stays sharp
                return;
            }
            bool reduced = PlayerPrefs.GetInt("CatMe.ReduceMotion", 0) == 1;
            float dt = Mathf.Max(Time.unscaledDeltaTime, 0.0001f);
            if (Time.timeScale == 0f) return;

            CameraProfile p = Active;
            ReadLookDrag(dt);
            // The chosen zoom, clamped to what this zone allows; easing only so a zone clamp or pinch isn't a jump.
            float wantDistance = closeUp ? CameraMath.CloseUp : Mathf.Clamp(zoom, p.zoomMin, p.zoomMax);
            if (closeUp) orbitPitch = Mathf.Min(orbitPitch, CameraMath.CloseUpPitchMax);
            SetDof(closeUp && !reduced ? 1f : 0f, closeUp);
            distance = Mathf.SmoothDamp(distance, wantDistance, ref distanceVelocity, .18f, 100f, dt);
            extraHeight = Mathf.SmoothDamp(extraHeight, extraHeightTarget, ref extraHeightVelocity, .6f, 100f, dt);
            Vector3 velocity = (owner.position - lastOwnerPosition) / dt;
            lastOwnerPosition = owner.position;
            ownerVelocity = Vector3.Lerp(ownerVelocity, velocity, 1f - Mathf.Exp(-6f * dt));
            idleTime = new Vector3(ownerVelocity.x, 0f, ownerVelocity.z).magnitude < .12f ? idleTime + dt : 0f;
            if (cam != null) cam.fieldOfView = Mathf.Lerp(cam.fieldOfView, 50f, 1f - Mathf.Exp(-4f * dt));

            Pose pose = Desired(0f, out Vector3 focus);
            // Collision: move in quickly, back out slowly so walls never pop the camera.
            Vector3 toCamera = pose.position - focus;
            float wanted = toCamera.magnitude;
            Vector3 dir = toCamera / Mathf.Max(wanted, .001f);
            float allowed = wanted;
            if (Physics.SphereCast(focus, .25f, dir, out RaycastHit hit, wanted, ~0, QueryTriggerInteraction.Ignore))
                allowed = Mathf.Max(CameraMath.CollisionMin, hit.distance - .2f);
            Vector3 target = focus + dir * allowed;
            if (cat != null)
            {
                // Never inside the pet: keep the camera at least 0.45 m from its middle by lifting it over.
                Vector3 petMiddle = cat.position + Vector3.up * .25f;
                float gap = Vector3.Distance(target, petMiddle);
                if (gap < .45f) target += Vector3.up * (.45f - gap + .05f);
            }
            float smooth = reduced ? .25f : .12f;
            if (Vector3.Distance(transform.position, focus) > allowed + .05f) smooth = .06f; // pull in fast
            transform.position = Vector3.SmoothDamp(transform.position, target, ref positionVelocity, smooth, 100f, dt);
            transform.rotation = Quaternion.LookRotation(focus - transform.position);
        }

        // The camera orbits the player at the player's yaw, pitch and zoom.
        Pose Desired(float widen, out Vector3 focus)
        {
            float k = closeUp ? CameraMath.CloseUpBlend(distance, Active.zoomMin) : 0f;
            focus = owner.position + Vector3.up * (Mathf.Lerp(CameraMath.BodyHeight, CameraMath.HeadHeight, k) + extraHeight);
            if (k > 0f && cat != null) { Vector3 toPet = cat.position - owner.position; toPet.y = 0f; focus += Vector3.ClampMagnitude(toPet * .35f, .6f) * k; }   // face and pet together
            Vector3 position = CameraMath.Orbit(focus, restYaw, orbitPitch, distance + widen);
            return new Pose(position, Quaternion.LookRotation(focus - position));
        }

        void SetDof(float target) => SetDof(target, false);

        // Seated: background softens from 2.2 m. Close-up (F11): from 1.4 m, so the face and pet stay sharp.
        void SetDof(float target, bool closeUpFocus)
        {
            if (target <= 0f && dofVolume == null) return;
            if (dofVolume == null)
            {
                var go = new GameObject("Seated depth of field"); go.transform.SetParent(transform, false);
                dofVolume = go.AddComponent<UnityEngine.Rendering.Volume>(); dofVolume.isGlobal = true; dofVolume.priority = 20; dofVolume.weight = 0f;
                var profile = ScriptableObject.CreateInstance<UnityEngine.Rendering.VolumeProfile>();
                dof = profile.Add<UnityEngine.Rendering.Universal.DepthOfField>(true);
                dof.mode.Override(UnityEngine.Rendering.Universal.DepthOfFieldMode.Gaussian);
                dof.gaussianStart.Override(2.2f); dof.gaussianEnd.Override(9f); dof.gaussianMaxRadius.Override(1f); dof.highQualitySampling.Override(false);
                dofVolume.profile = profile;
            }
            if (dof != null) { dof.gaussianStart.Override(closeUpFocus ? 1.4f : 2.2f); dof.gaussianEnd.Override(closeUpFocus ? 6f : 9f); }
            dofVolume.weight = Mathf.MoveTowards(dofVolume.weight, PlayerPrefs.GetInt("CatMe.ReduceMotion", 0) == 1 ? 0f : target, Time.unscaledDeltaTime * 1.5f);
        }

        public const float IdleOrbitDelay = 8f, IdleOrbitDegrees = 10f;

        void FollowYaw(float dt, bool reduced)
        {
            // Slowly swing the rest yaw toward the owner's facing while walking, so turns feel like walking behind someone.
            Vector3 flat = new Vector3(ownerVelocity.x, 0f, ownerVelocity.z);
            bool dragging = lookActive || Time.unscaledTime - lastLookInputAt < 1.5f;
            if (flat.magnitude > .25f && !dragging)
            {
                float targetYaw = Mathf.Atan2(flat.x, flat.z) * Mathf.Rad2Deg;
                // About 2 s: slow enough that the view never seems to steer by itself. The stick's frame is latched while it is held
                // (GardenCompanionDemo.CameraRelative), so this swing doesn't bend a held direction into a curve.
                float smooth = reduced ? 2.6f : 2f;
                restYaw = Mathf.SmoothDampAngle(restYaw, targetYaw, ref yawVelocity, smooth, 120f, dt);
            }
            if (!lookActive && Time.unscaledTime - lastLookInputAt > 1.5f)
            {
                float recentre = 1f - Mathf.Exp(-2.6f * dt);
                // Standing still for a long while: a small, very slow drift to a side angle that frames the owner and pet together.
                float orbit = !reduced && fpAnchor == null && idleTime > IdleOrbitDelay ? IdleOrbitDegrees : 0f;
                yawOffset = Mathf.Lerp(yawOffset, orbit, recentre * (orbit != 0f ? .05f : 1f));
                pitchOffset = Mathf.Lerp(pitchOffset, 0f, recentre);
            }
        }

        void ReadLookDrag(float dt)
        {
            // Touch: one finger on the right half of the screen outside UI. Mouse: right button, for the editor.
            const float degPerPixel = .15f;
            var touchscreen = Touchscreen.current;
            if (touchscreen != null && fpAnchor == null && ReadPinch(touchscreen)) return;
            if (touchscreen != null)
            {
                foreach (var touch in touchscreen.touches)
                {
                    // Skip empty touch slots: only read a touch while it is live (or ending this frame).
                    var phase = touch.phase.ReadValue();
                    if (phase == UnityEngine.InputSystem.TouchPhase.None) continue;
                    int id = touch.touchId.ReadValue();
                    Vector2 position = touch.position.ReadValue();
                    if (phase == UnityEngine.InputSystem.TouchPhase.Began && lookTouchId < 0 && (fpAnchor != null || position.x > Screen.width * .5f) && !OverUi(id))
                    { lookTouchId = id; lookLast = position; lookActive = true; lastLookInputAt = Time.unscaledTime; }
                    else if (id == lookTouchId)
                    {
                        if (phase == UnityEngine.InputSystem.TouchPhase.Moved || phase == UnityEngine.InputSystem.TouchPhase.Stationary)
                        { ApplyLook(position - lookLast, degPerPixel); lookLast = position; }
                        if (phase == UnityEngine.InputSystem.TouchPhase.Ended || phase == UnityEngine.InputSystem.TouchPhase.Canceled)
                        { lookTouchId = -1; lookActive = false; lastLookInputAt = Time.unscaledTime; }
                    }
                }
                if (lookTouchId >= 0) return;
            }
            var mouse = Mouse.current;
            if (mouse != null && fpAnchor == null)
            {
                float wheel = mouse.scroll.ReadValue().y;
                if (Mathf.Abs(wheel) > .01f) { CameraProfile p = Active; zoom = Mathf.Clamp(zoom * (wheel > 0f ? .9f : 1.1f), p.zoomMin, p.zoomMax); PlayerPrefs.SetFloat(ZoomKey, zoom); }
            }
            if (mouse != null)
            {
                bool fp = fpAnchor != null;
                bool pressed = mouse.rightButton.wasPressedThisFrame || fp && mouse.leftButton.wasPressedThisFrame;
                bool held = mouse.rightButton.isPressed || fp && mouse.leftButton.isPressed;
                bool released = mouse.rightButton.wasReleasedThisFrame || fp && mouse.leftButton.wasReleasedThisFrame;
                if (pressed && !OverUi(-1)) { lookActive = true; lookLast = mouse.position.ReadValue(); }
                if (lookActive && held) { Vector2 now = mouse.position.ReadValue(); ApplyLook(now - lookLast, degPerPixel); lookLast = now; }
                if (lookActive && released) { lookActive = false; lastLookInputAt = Time.unscaledTime; }
            }
        }

        // Two fingers off the buttons: pinch to zoom (F5). Returns true while pinching, so the look drag pauses.
        bool ReadPinch(Touchscreen touchscreen)
        {
            Vector2 a = default, b = default; int live = 0;
            foreach (var touch in touchscreen.touches)
            {
                var phase = touch.phase.ReadValue();
                if (phase != UnityEngine.InputSystem.TouchPhase.Began && phase != UnityEngine.InputSystem.TouchPhase.Moved && phase != UnityEngine.InputSystem.TouchPhase.Stationary) continue;
                if (pinching == false && OverUi(touch.touchId.ReadValue())) continue;
                if (live == 0) a = touch.position.ReadValue(); else if (live == 1) b = touch.position.ReadValue();
                live++;
            }
            if (live < 2) { if (pinching) { pinching = false; PlayerPrefs.SetFloat(ZoomKey, zoom); } return false; }
            float span = Vector2.Distance(a, b);
            if (!pinching) { pinching = true; pinchLast = span; lookTouchId = -1; lookActive = false; return true; }
            CameraProfile p = Active;
            float before = pinchLast;
            zoom = CameraMath.PinchStep(zoom, closeUp, before, span, p.zoomMin, p.zoomMax, out bool nowCloseUp);
            // A step in or out restarts the measure, so one long pinch doesn't bounce between the two.
            if (nowCloseUp != closeUp) { closeUp = nowCloseUp; pinchLast = span; return true; }
            pinchLast = closeUp ? Mathf.Max(pinchLast, span) : span;   // in close-up, pinching out is measured from the widest spread
            return true;
        }

        void ApplyLook(Vector2 delta, float degPerPixel)
        {
            lastLookInputAt = Time.unscaledTime;
            if (fpAnchor == null)
            {
                // Third person: orbit around the player; yaw free, pitch 5 to 55 degrees.
                restYaw = Mathf.Repeat(restYaw + delta.x * degPerPixel, 360f);
                orbitPitch = CameraMath.ClampPitch(orbitPitch - delta.y * degPerPixel);
                return;
            }
            // Seated / in the tent: a wide, free look. On the path: a gentle limited look that recentres.
            float yawLimit = fpAnchor != null ? 140f : 70f, pitchMin = fpAnchor != null ? -60f : -5f, pitchMax = fpAnchor != null ? 60f : 25f;
            yawOffset = Mathf.Clamp(yawOffset + delta.x * degPerPixel * (fpAnchor != null ? 1.4f : 1f), -yawLimit, yawLimit);
            pitchOffset = Mathf.Clamp(pitchOffset - delta.y * degPerPixel * (fpAnchor != null ? 1.4f : 1f), pitchMin, pitchMax);
        }

        static bool OverUi(int fingerId)
        {
            var es = EventSystem.current;
            if (es == null) return false;
            return fingerId >= 0 ? es.IsPointerOverGameObject(fingerId) : es.IsPointerOverGameObject();
        }
    }


    /// <summary>F5 camera numbers and maths, pure so they are tested.</summary>
    public static class CameraMath
    {
        public const float ZoomMin = 1.8f, ZoomMax = 6f, PitchMin = 5f, PitchMax = 55f, CollisionMin = .6f;
        /// <summary>F11: the close-up step past the normal minimum, and how high the look target rises there.</summary>
        public const float CloseUp = .9f, BodyHeight = 1.2f, HeadHeight = 1.55f, CloseUpPitchMax = 25f;

        /// <summary>
        /// Pinch with the close-up step: pinching in at the normal minimum moves to the close-up; pinching out of the close-up by
        /// 15% returns to the normal minimum. Otherwise the normal pinch.
        /// </summary>
        public static float PinchStep(float zoom, bool closeUp, float previousSpan, float span, float min, float max, out bool nowCloseUp)
        {
            nowCloseUp = closeUp;
            if (previousSpan < 1f || span < 1f) return closeUp ? CloseUp : Mathf.Clamp(zoom, min, max);
            float ratio = previousSpan / span;   // < 1 when the fingers spread (zoom in)
            if (closeUp)
            {
                if (ratio > 1.15f) { nowCloseUp = false; return min; }
                return CloseUp;
            }
            float next = zoom * ratio;
            if (next < min && zoom <= min + .05f && ratio < .97f) { nowCloseUp = true; return CloseUp; }
            return Mathf.Clamp(next, min, max);
        }

        /// <summary>0 at the normal minimum or further, 1 at the close-up: how far the look target has risen to the head.</summary>
        public static float CloseUpBlend(float distance, float min) => Mathf.Clamp01(Mathf.InverseLerp(min, CloseUp, distance));

        public static float ClampPitch(float pitch) => Mathf.Clamp(pitch, PitchMin, PitchMax);

        /// <summary>Spreading the fingers zooms in (shorter distance) in proportion to the change in finger distance.</summary>
        public static float Pinch(float zoom, float previousSpan, float span, float min, float max)
        {
            if (previousSpan < 1f || span < 1f) return Mathf.Clamp(zoom, min, max);
            return Mathf.Clamp(zoom * previousSpan / span, min, max);
        }

        /// <summary>Camera position orbiting a focus point: behind it at the yaw, raised by the pitch, at the distance.</summary>
        public static Vector3 Orbit(Vector3 focus, float yaw, float pitch, float distance) => focus + Quaternion.Euler(pitch, yaw, 0f) * Vector3.back * distance;
    }
}
