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

        public static CameraProfile Default => new CameraProfile();
        public static CameraProfile Named(string profile)
        {
            switch (profile)
            {
                case "GardenClose": return new CameraProfile { name = profile, distance = 2.6f, height = 1.55f, pitch = 9f, fov = 48f };
                case "GateReveal": return new CameraProfile { name = profile, distance = 3.2f, height = 1.45f, pitch = 5f, fov = 52f };
                case "Riverside": return new CameraProfile { name = profile, distance = 3.2f, height = 1.7f, pitch = 8f, fov = 52f, screenOffsetX = .2f };
                case "Woodland": return new CameraProfile { name = profile, distance = 2.8f, height = 1.6f, pitch = 7f, fov = 50f };
                case "ShelterInside": return new CameraProfile { name = profile, distance = 3.4f, height = 1.6f, pitch = 4f, fov = 52f, screenOffsetX = -.24f };
                case "Overlook": return new CameraProfile { name = profile, distance = 5.5f, height = 2.2f, pitch = 6f, fov = 56f, screenOffsetX = -.3f };
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
        public void ExitFirstPerson() { fpAnchor = null; SetDof(0f); if (cam != null) cam.nearClipPlane = .1f; Snap(); }

        public void Initialize(Transform ownerTransform, Transform catTransform)
        {
            owner = ownerTransform; cat = catTransform; cam = GetComponent<Camera>();
            restYaw = owner != null ? owner.eulerAngles.y : 0f;
            lastOwnerPosition = owner != null ? owner.position : Vector3.zero;
            initialised = true;
            Snap();
        }

        public void SetBaseProfile(CameraProfile profile) { baseProfile = profile ?? CameraProfile.Default; }
        public void PushProfile(CameraProfile profile) { if (profile != null && !stack.Contains(profile)) stack.Add(profile); }
        public void PopProfile(CameraProfile profile) { stack.Remove(profile); }
        public void PopAll() { stack.Clear(); }
        /// <summary>Raises the camera for place arrival/exit moments. Reduced motion ignores it.</summary>
        public void SetExtraHeight(float meters) { extraHeightTarget = PlayerPrefs.GetInt("CatMe.ReduceMotion", 0) == 1 ? 0f : meters; }
        public void SetRestYaw(float yaw) { restYaw = yaw; yawOffset = 0f; }

        CameraProfile Active => stack.Count > 0 ? stack[stack.Count - 1] : baseProfile;

        /// <summary>Jump straight to the framing for the current owner pose (used after a place swap).</summary>
        public void Snap()
        {
            if (!initialised || owner == null) return;
            restYaw = owner.eulerAngles.y; yawOffset = 0f; pitchOffset = 0f;
            CameraProfile p = Active;
            distance = p.distance; height = p.height; pitch = p.pitch; fov = p.fov; offsetX = p.screenOffsetX; lookAhead = p.lookAheadScale;
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
            if (Time.timeScale == 0f) dt = 0f;
            if (dt <= 0f) return;

            CameraProfile p = Active;
            float blend = Mathf.Max(.05f, p.blendIn);
            distance = Mathf.SmoothDamp(distance, p.distance, ref distanceVelocity, blend, 100f, dt);
            height = Mathf.SmoothDamp(height, p.height, ref heightVelocity, blend, 100f, dt);
            pitch = Mathf.SmoothDamp(pitch, p.pitch, ref pitchVelocity, blend, 100f, dt);
            offsetX = Mathf.SmoothDamp(offsetX, p.screenOffsetX, ref offsetVelocity, blend, 100f, dt);
            extraHeight = Mathf.SmoothDamp(extraHeight, extraHeightTarget, ref extraHeightVelocity, .6f, 100f, dt);

            // Owner velocity drives look-ahead and the slow yaw follow.
            Vector3 velocity = (owner.position - lastOwnerPosition) / dt;
            lastOwnerPosition = owner.position;
            ownerVelocity = Vector3.Lerp(ownerVelocity, velocity, 1f - Mathf.Exp(-6f * dt));

            float planar = new Vector3(ownerVelocity.x, 0f, ownerVelocity.z).magnitude;
            idleTime = planar < .12f ? idleTime + dt : 0f;
            bool catClose = cat != null && Vector3.Distance(cat.position, owner.position) < 1.8f;
            idleBlend = Mathf.MoveTowards(idleBlend, !reduced && idleTime > 2.5f && catClose ? 1f : 0f, dt / 1.2f);
            ReadLookDrag(dt);
            FollowYaw(dt, reduced);

            // Keep the cat in frame by widening gently when it strays from the middle of the view.
            float widen = 0f, fovWiden = 0f;
            if (cat != null && cam != null)
            {
                Vector3 view = cam.WorldToViewportPoint(cat.position + Vector3.up * .2f);
                float off = Mathf.Max(Mathf.Abs(view.x - .5f) - .35f, view.z < 0f ? .4f : 0f);
                if (off > 0f) { widen = Mathf.Clamp(off * 6f, 0f, 1.2f); fovWiden = Mathf.Clamp(off * 24f, 0f, 6f); }
            }
            float targetFov = p.fov + fovWiden;
            fov = Mathf.SmoothDamp(fov, targetFov, ref fovVelocity, .8f, 100f, dt);
            if (cam != null) cam.fieldOfView = fov;

            Vector3 aheadTarget = reduced ? Vector3.zero : Vector3.ClampMagnitude(new Vector3(ownerVelocity.x, 0f, ownerVelocity.z) * .6f * lookAhead * p.lookAheadScale, 1.5f);
            lookAheadOffset = Vector3.SmoothDamp(lookAheadOffset, aheadTarget, ref lookAheadVelocity, .5f, 100f, dt);

            Pose pose = Desired(widen, out Vector3 focus);
            // Collision: move in quickly, back out slowly so walls never pop the camera.
            Vector3 toCamera = pose.position - focus;
            float wanted = toCamera.magnitude;
            Vector3 dir = toCamera / Mathf.Max(wanted, .001f);
            float allowed = wanted;
            if (Physics.SphereCast(focus, .25f, dir, out RaycastHit hit, wanted, ~0, QueryTriggerInteraction.Ignore))
                allowed = Mathf.Max(1.2f, hit.distance - .2f);
            Vector3 target = focus + dir * allowed;
            float smooth = reduced ? .6f : .35f;
            if (Vector3.Distance(transform.position, focus) > allowed + .05f) smooth = .08f; // pull in fast
            transform.position = Vector3.SmoothDamp(transform.position, target, ref positionVelocity, smooth, 100f, dt);
            Quaternion look = Quaternion.LookRotation(focus - transform.position + Vector3.up * .05f);
            transform.rotation = Quaternion.Slerp(transform.rotation, look, 1f - Mathf.Exp(-(reduced ? 4f : 7f) * dt));
        }

        Pose Desired(float widen, out Vector3 focus)
        {
            focus = owner.position;
            if (cat != null)
            {
                Vector3 catOffset = cat.position - owner.position;
                focus += Vector3.ClampMagnitude(catOffset * .3f, 1.2f);
            }
            focus += lookAheadOffset;
            focus.y += 1.05f;
            float yaw = restYaw + yawOffset;
            Quaternion rotation = Quaternion.Euler(0f, yaw, 0f);
            float totalPitch = Mathf.Clamp(pitch + pitchOffset, -5f, 40f);
            // Camera sits behind the focus by `distance`, at `height` above the ground under the owner.
            Vector3 behind = rotation * Vector3.back * (distance + widen - .45f * idleBlend);
            Vector3 position = focus + behind;
            position.y = owner.position.y + height - .22f * idleBlend + extraHeight + Mathf.Tan(Mathf.Deg2Rad * (totalPitch - pitch)) * (distance + widen);
            // Slide the pair off-centre on screen by moving the focus sideways.
            Vector3 right = rotation * Vector3.right;
            position -= right * offsetX * (distance + widen) * .55f;
            Vector3 aim = focus - right * offsetX * (distance + widen) * .55f;
            focus = aim;
            Quaternion look = Quaternion.LookRotation(aim - position);
            return new Pose(position, look);
        }

        void SetDof(float target)
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
            dofVolume.weight = Mathf.MoveTowards(dofVolume.weight, PlayerPrefs.GetInt("CatMe.ReduceMotion", 0) == 1 ? 0f : target, Time.unscaledDeltaTime * 1.5f);
        }

        void FollowYaw(float dt, bool reduced)
        {
            // Slowly swing the rest yaw toward the owner's facing while walking, so turns feel like walking behind someone.
            Vector3 flat = new Vector3(ownerVelocity.x, 0f, ownerVelocity.z);
            bool dragging = lookActive || Time.unscaledTime - lastLookInputAt < 1.5f;
            if (flat.magnitude > .25f && !dragging)
            {
                float targetYaw = Mathf.Atan2(flat.x, flat.z) * Mathf.Rad2Deg;
                float smooth = reduced ? 1.4f : .8f;
                restYaw = Mathf.SmoothDampAngle(restYaw, targetYaw, ref yawVelocity, smooth, 360f, dt);
            }
            if (!lookActive && Time.unscaledTime - lastLookInputAt > 1.5f)
            {
                float recentre = 1f - Mathf.Exp(-2.6f * dt);
                // Standing still for a moment: drift to a side angle that frames the owner and cat together.
                float orbit = !reduced && fpAnchor == null && idleTime > 2.5f ? 26f : 0f;
                yawOffset = Mathf.Lerp(yawOffset, orbit, recentre * (orbit != 0f ? .25f : 1f));
                pitchOffset = Mathf.Lerp(pitchOffset, 0f, recentre);
            }
        }

        void ReadLookDrag(float dt)
        {
            // Touch: one finger on the right half of the screen outside UI. Mouse: right button, for the editor.
            const float degPerPixel = .15f;
            var touchscreen = Touchscreen.current;
            if (touchscreen != null)
            {
                foreach (var touch in touchscreen.touches)
                {
                    int id = touch.touchId.ReadValue();
                    var phase = touch.phase.ReadValue();
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

        void ApplyLook(Vector2 delta, float degPerPixel)
        {
            lastLookInputAt = Time.unscaledTime;
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

}
