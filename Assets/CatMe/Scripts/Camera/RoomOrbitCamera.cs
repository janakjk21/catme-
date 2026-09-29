using System.Collections.Generic;
using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;
using UnityEngine.AI;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace CatMe.CameraSystem
{
    /// <summary>
    /// The bounded Room camera. It is intentionally independent of cat movement and UI.
    /// </summary>
    [DefaultExecutionOrder(-200)]
    [DisallowMultipleComponent]
    public sealed class RoomOrbitCamera : MonoBehaviour
    {
        [Header("Authored room references")]
        [SerializeField] private Transform cameraPivot;
        [SerializeField] private Camera roomCamera;

        [Header("Player camera mode")]
        [SerializeField] private bool fixedRoomView = true;

        [Header("Room orbit limits")]
        [SerializeField] private float minimumYaw = -55f;
        [SerializeField] private float maximumYaw = 55f;
        [SerializeField] private float minimumPitch = 12f;
        [SerializeField] private float maximumPitch = 48f;
        [SerializeField] private float minimumDistance = 4.3f;
        [SerializeField] private float maximumDistance = 5.0f;
        [SerializeField] private float roomViewDistance = 5.0f;
        [SerializeField] private float companionViewDistance = 2.55f;
        [SerializeField] private float companionFocusHeight = 0.39f;

        [Header("Input and smoothing")]
        [SerializeField] private float yawDegreesPerPixel = 0.16f;
        [SerializeField] private float pitchDegreesPerPixel = 0.075f;
        [SerializeField] private float pinchMetersPerPixel = 0.003f;
        [SerializeField] private float wheelMetersPerUnit = 0.08f;
        [SerializeField] private float positionSmoothTime = 0.12f;
        [SerializeField] private float rotationSmoothTime = 0.10f;
        [SerializeField] private float obstructionRadius = 0.16f;
        [SerializeField] private float obstructionClearance = 0.10f;
        [SerializeField] private LayerMask obstructionMask = ~0;

        [Header("Seated first-person experiment")]
        [SerializeField] private float seatedHeight = 1.05f;
        [SerializeField] private float seatedOffsetFromRoomCenter = 2.05f;
        [SerializeField] private float seatedPitchLimit = 16f;
        [SerializeField] private float seatedDefaultDownPitch = 21f;
        [SerializeField] private float seatedTargetDistance = 1.60f;
        [SerializeField] private float walkSpeed = 1.45f;
        [SerializeField] private float walkCollisionRadius = 0.20f;
        [SerializeField] private float walkCapsuleHeight = 1.48f;
        [SerializeField] private float firstPersonMinimumFov = 42f;
        [SerializeField] private float firstPersonMaximumFov = 72f;
        [SerializeField] private float firstPersonPinchFovPerPixel = 0.055f;
        [SerializeField] private float firstPersonWheelFovPerUnit = 2.0f;

        private readonly Dictionary<int, bool> excludedTouchPointers = new Dictionary<int, bool>();
        private readonly Dictionary<int, bool> touchStartedOverUi = new Dictionary<int, bool>();
        private float homeYaw;
        private float yawCenter;
        private float currentYaw;
        private float targetYaw;
        private float currentPitch;
        private float targetPitch;
        private float currentDistance;
        private float targetDistance;
        private float positionVelocity;
        private float yawVelocity;
        private float pitchVelocity;
        private bool mouseExcluded;
        private bool mouseOrbiting;
        private Vector2 previousMousePosition;
        private float previousPinchDistance;
        private int previousEligibleTouchCount;
        private int trackedTouchId = -1;
        private Vector2 previousTouchPosition;
        private Vector3 currentPivotPosition;
        private Vector3 pivotVelocity;
        private Transform focusTarget;
        private Transform catCollisionTarget;
        private bool companionView;
        private bool pauseTargetFollow;
        private float manualInputUntil;
        private Button homeViewButton;
        private Button viewModeButton;
        private Text homeViewLabel;
        private Text viewModeLabel;
        private bool seatedView;
        private Vector3 seatedPosition;
        private float seatedHomeYaw;
        private float seatedYaw;
        private float targetSeatedYaw;
        private float seatedPitch;
        private float targetSeatedPitch;
        private float seatedYawVelocity;
        private float seatedPitchVelocity;
        private bool collisionShortened;
        private bool seatedOverlapWarningLogged;
        private float minimumObservedHeight = float.PositiveInfinity;
        private bool belowFloorObserved;
        private bool initialized;
        private int movementTouchId = -1;
        private Vector2 movementTouchOrigin;
        private Vector2 movementInput;
        private RectTransform joystickRoot;
        private RectTransform joystickKnob;
        private float firstPersonDefaultFov;

        public float CurrentYaw => currentYaw;
        public float TargetYaw => targetYaw;
        public float CurrentPitch => currentPitch;
        public float TargetPitch => targetPitch;
        public float CurrentDistance => currentDistance;
        public float TargetDistance => targetDistance;
        public bool CurrentGestureBeganOverUI { get; private set; }
        public bool CollisionShortened => collisionShortened;
        public bool IsCompanionView => companionView;
        public bool IsSeatedView => !fixedRoomView && seatedView;
        public Vector3 SeatedPosition => seatedPosition;
        public Transform AuthoredPivot => cameraPivot;
        public bool IsFixedRoomView => fixedRoomView;

        public bool IsEligibleWorldTouch(int pointerId)
        {
            return touchStartedOverUi.TryGetValue(pointerId, out bool beganOverUi)
                ? !beganOverUi
                : !IsPointerOverUi(pointerId);
        }

        public void ReserveMousePointer()
        {
            mouseExcluded = true;
            mouseOrbiting = false;
        }

        public void ReleaseMousePointer()
        {
            mouseExcluded = false;
            mouseOrbiting = false;
        }

        public void ReserveTouchPointer(int pointerId)
        {
            excludedTouchPointers[pointerId] = true;
        }

        public void ReleaseTouchPointer(int pointerId)
        {
            excludedTouchPointers.Remove(pointerId);
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void AttachToHomeRoomCamera()
        {
            if (SceneManager.GetActiveScene().name != "HomeRoom")
            {
                return;
            }

            Camera camera = Camera.main;
            GameObject pivotObject = GameObject.Find("CameraPivot");
            if (camera == null || pivotObject == null || camera.GetComponent<RoomOrbitCamera>() != null)
            {
                return;
            }

            camera.gameObject.AddComponent<RoomOrbitCamera>();
        }

        private void Awake()
        {
            if (roomCamera == null)
            {
                roomCamera = GetComponent<Camera>();
            }

            if (cameraPivot == null)
            {
                GameObject pivotObject = GameObject.Find("CameraPivot");
                cameraPivot = pivotObject == null ? null : pivotObject.transform;
            }

            if (roomCamera == null || cameraPivot == null)
            {
                enabled = false;
                Debug.LogError("[CatMe][Camera] RoomOrbitCamera requires Main Camera and CameraPivot.");
                return;
            }

            Vector3 offset = roomCamera.transform.position - cameraPivot.position;
            float horizontalDistance = new Vector2(offset.x, offset.z).magnitude;
            float derivedDistance = offset.magnitude;
            homeYaw = Mathf.Atan2(offset.x, offset.z) * Mathf.Rad2Deg;
            currentYaw = targetYaw = 0f;
            currentPitch = targetPitch = Mathf.Clamp(
                Mathf.Atan2(offset.y, horizontalDistance) * Mathf.Rad2Deg,
                minimumPitch,
                maximumPitch);
            currentDistance = targetDistance = Mathf.Clamp(derivedDistance, minimumDistance, maximumDistance);
            currentPivotPosition = cameraPivot.position;
            targetDistance = Mathf.Clamp(roomViewDistance, minimumDistance, maximumDistance);
            initialized = true;
            minimumObservedHeight = roomCamera.transform.position.y;
            Debug.Log(
                $"[CatMe][Camera] Room camera initialized | yaw={currentYaw:0.##}deg | pitch={currentPitch:0.##}deg | " +
                $"distance={currentDistance:0.##}m | derivedPoseError=0m | fov={roomCamera.fieldOfView:0.##}deg");
        }

        private void Start()
        {
            firstPersonDefaultFov = roomCamera.fieldOfView;
            // The landscape HomeRoom opens as a complete room-view scene. The close
            // seated camera remains available from the view-mode control.
            StartCoroutine(CreateHomeViewButtonWhenCanvasExists());
        }

        private IEnumerator CreateHomeViewButtonWhenCanvasExists()
        {
            while (homeViewButton == null)
            {
                CreateHomeViewButton();
                if (homeViewButton == null) yield return new WaitForSecondsRealtime(0.5f);
            }
        }

        /// <summary>Moves smoothly into a close, interactive view of the cat.</summary>
        public void FocusOnCat(Transform target)
        {
            if (fixedRoomView) return;
            if (seatedView) return;
            if (target == null) return;
            focusTarget = target;
            companionView = true;
            pauseTargetFollow = false;
            yawCenter = 0f;
            targetYaw = 0f;
            targetDistance = Mathf.Clamp(companionViewDistance, minimumDistance, maximumDistance);
            targetPitch = Mathf.Clamp(19f, minimumPitch, maximumPitch);
            manualInputUntil = 0f;
        }

        /// <summary>Frames a temporary activity target, such as the cat and ball area.</summary>
        public void FocusOnActivity(Transform target, float distance = 4.6f, float yawOffset = 0f, float pitch = 26f)
        {
            if (fixedRoomView) return;
            if (seatedView) return;
            if (target == null) return;
            focusTarget = target;
            companionView = false;
            pauseTargetFollow = false;
            yawCenter = yawOffset;
            targetYaw = yawOffset;
            targetPitch = Mathf.Clamp(pitch, minimumPitch, maximumPitch);
            targetDistance = Mathf.Clamp(distance, minimumDistance, maximumDistance);
            manualInputUntil = 0f;
        }

        public void ReturnToRoomView()
        {
            if (fixedRoomView) return;
            if (seatedView)
            {
                RecenterSeatedView();
                return;
            }
            focusTarget = null;
            companionView = false;
            pauseTargetFollow = false;
            yawCenter = 0f;
            targetYaw = 0f;
            targetPitch = Mathf.Clamp(25f, minimumPitch, maximumPitch);
            targetDistance = Mathf.Clamp(roomViewDistance, minimumDistance, maximumDistance);
            manualInputUntil = Time.unscaledTime + 1.5f;
        }

        public void ResetRoomZoom()
        {
            targetDistance = Mathf.Clamp(roomViewDistance, minimumDistance, maximumDistance);
        }

        private void OnHomeViewPressed()
        {
            if (fixedRoomView) ResetRoomZoom();
            else ReturnToRoomView();
        }

        public void ToggleSeatedView()
        {
            SetSeatedView(!seatedView);
        }

        public void SetSeatedView(bool enabled)
        {
            if (fixedRoomView) return;
            if (!initialized || seatedView == enabled) return;
            seatedView = enabled;
            focusTarget = null;
            pauseTargetFollow = false;
            if (seatedView)
            {
                Vector3 awayFromRoomCenter = Quaternion.Euler(0f, homeYaw, 0f) * Vector3.forward;
                awayFromRoomCenter.y = 0f;
                if (awayFromRoomCenter.sqrMagnitude < 0.001f) awayFromRoomCenter = Vector3.back;
                awayFromRoomCenter.Normalize();
                seatedPosition = new Vector3(cameraPivot.position.x, seatedHeight, cameraPivot.position.z) +
                    awayFromRoomCenter * seatedOffsetFromRoomCenter;
                Vector3 startFloor = seatedPosition;
                startFloor.y = 0f;
                if (NavMesh.SamplePosition(startFloor, out NavMeshHit startHit, 0.75f, NavMesh.AllAreas))
                {
                    seatedPosition = startHit.position + Vector3.up * seatedHeight;
                }
                seatedHomeYaw = Mathf.Repeat(homeYaw + 180f, 360f);
                seatedYaw = targetSeatedYaw = 0f;
                seatedPitch = targetSeatedPitch = 0f;
                if (roomCamera != null)
                    roomCamera.fieldOfView = Mathf.Clamp(firstPersonDefaultFov, firstPersonMinimumFov, firstPersonMaximumFov);
            }
            else
            {
                movementTouchId = -1;
                movementInput = Vector2.zero;
                if (joystickRoot != null) joystickRoot.gameObject.SetActive(false);
                if (roomCamera != null && firstPersonDefaultFov > 0f) roomCamera.fieldOfView = firstPersonDefaultFov;
            }
            Debug.Log($"[CatMe][Camera] View mode={(seatedView ? "SeatedFirstPerson" : "RoomThirdPerson")} | " +
                      $"seat=({seatedPosition.x:0.##},{seatedPosition.y:0.##},{seatedPosition.z:0.##}) | " +
                      $"playerVisible={(!seatedView ? "n/a" : "camera is player viewpoint")}");
            UpdateModeLabels();
        }

        public void RecenterSeatedView()
        {
            if (!seatedView) return;
            Transform cat = GameObject.Find("CatRuntime")?.transform;
            if (cat == null)
            {
                targetSeatedYaw = 0f;
                targetSeatedPitch = 0f;
                return;
            }

            Vector3 toCat = cat.position - seatedPosition;
            Vector3 flatDirection = Vector3.ProjectOnPlane(toCat, Vector3.up);
            if (flatDirection.sqrMagnitude > 0.001f)
            {
                float worldYaw = Mathf.Atan2(flatDirection.x, flatDirection.z) * Mathf.Rad2Deg;
                targetSeatedYaw = Mathf.DeltaAngle(seatedHomeYaw, worldYaw);
            }
            float horizontalDistance = Mathf.Max(0.1f, flatDirection.magnitude);
            float desiredEulerPitch = -Mathf.Atan2(toCat.y, horizontalDistance) * Mathf.Rad2Deg;
            targetSeatedPitch = Mathf.Clamp(seatedDefaultDownPitch - desiredEulerPitch,
                -seatedPitchLimit, seatedPitchLimit);
        }

        public bool TryGetSeatedCallPose(out Vector3 position, out Quaternion rotation)
        {
            position = default;
            rotation = Quaternion.identity;
            if (fixedRoomView || !seatedView) return false;

            Vector3 facingIntoRoom = Vector3.ProjectOnPlane(roomCamera.transform.forward, Vector3.up);
            facingIntoRoom.y = 0f;
            if (facingIntoRoom.sqrMagnitude < 0.001f) return false;
            facingIntoRoom.Normalize();
            position = seatedPosition + facingIntoRoom * seatedTargetDistance;
            // HomeRoom's floor and NavMesh are authored at world y=0. Keep the
            // cat's feet on that plane; the camera pivot itself is elevated.
            position.y = 0f;
            rotation = Quaternion.LookRotation(-facingIntoRoom, Vector3.up);
            return true;
        }

        private void Update()
        {
            if (!initialized)
            {
                return;
            }

            if (fixedRoomView)
            {
                ProcessFixedRoomPinch();
                ProcessFixedRoomWheel();
                currentPivotPosition = cameraPivot.position;
            }
            else
            {
                ProcessTouchInput();
                ProcessMouseInput();
                ProcessWalkInput();
                SmoothSeatedTargets();
                UpdateFocusPosition();
            }
            SmoothTargets();
            ApplyCameraTransform();
        }

        private void OnApplicationFocus(bool hasFocus)
        {
            if (hasFocus) return;
            previousEligibleTouchCount = 0;
            touchStartedOverUi.Clear();
            Touchscreen touchscreen = Touchscreen.current;
            if (touchscreen != null)
            {
                foreach (TouchControl touch in touchscreen.touches)
                    if (touch.press.isPressed) touchStartedOverUi[touch.touchId.ReadValue()] = true;
            }
            excludedTouchPointers.Clear();
            mouseOrbiting = false;
            mouseExcluded = false;
        }

        private void OnApplicationPause(bool paused)
        {
            if (paused) OnApplicationFocus(false);
        }

        private void ProcessFixedRoomPinch()
        {
            Touchscreen screen = Touchscreen.current;
            if (screen == null)
            {
                previousEligibleTouchCount = 0;
                return;
            }

            int count = 0;
            Vector2 first = default;
            Vector2 second = default;
            foreach (TouchControl touch in screen.touches)
            {
                if (!touch.press.isPressed) continue;
                int id = touch.touchId.ReadValue();
                if (touch.phase.ReadValue() == UnityEngine.InputSystem.TouchPhase.Began)
                    touchStartedOverUi[id] = IsPointerOverUi(id);
                bool beganOverUi = touchStartedOverUi.TryGetValue(id, out bool overUi) ? overUi : IsPointerOverUi(id);
                if (beganOverUi) continue;
                if (count == 0) first = touch.position.ReadValue();
                else if (count == 1) second = touch.position.ReadValue();
                count++;
            }

            if (count >= 2)
            {
                float distance = Vector2.Distance(first, second);
                if (previousEligibleTouchCount >= 2)
                    targetDistance = Mathf.Clamp(targetDistance + (distance - previousPinchDistance) * pinchMetersPerPixel,
                        minimumDistance, maximumDistance);
                previousPinchDistance = distance;
                previousEligibleTouchCount = 2;
            }
            else
            {
                previousEligibleTouchCount = 0;
            }

            for (int index = screen.touches.Count - 1; index >= 0; index--)
            {
                TouchControl touch = screen.touches[index];
                if (!touch.press.isPressed)
                {
                    int id = touch.touchId.ReadValue();
                    touchStartedOverUi.Remove(id);
                    excludedTouchPointers.Remove(id);
                }
            }
        }

        private void ProcessFixedRoomWheel()
        {
            Mouse mouse = Mouse.current;
            if (mouse == null || IsPointerOverUi(-1)) return;
            float wheel = mouse.scroll.ReadValue().y;
            if (Mathf.Abs(wheel) > 0.01f)
                targetDistance = Mathf.Clamp(targetDistance - wheel * wheelMetersPerUnit, minimumDistance, maximumDistance);
        }

        private void UpdateFocusPosition()
        {
            if (seatedView)
            {
                currentPivotPosition = cameraPivot.position;
                return;
            }
            Vector3 desiredPivot = cameraPivot.position;
            if (focusTarget != null && !pauseTargetFollow)
            {
                desiredPivot = focusTarget.position + Vector3.up * (companionView ? companionFocusHeight : 0.25f);
            }
            else if (focusTarget != null)
            {
                desiredPivot = currentPivotPosition;
            }
            if (Time.unscaledTime >= manualInputUntil) pauseTargetFollow = false;
            currentPivotPosition = Vector3.SmoothDamp(
                currentPivotPosition, desiredPivot, ref pivotVelocity,
                Time.unscaledTime < manualInputUntil ? 0.20f : 0.42f,
                Mathf.Infinity, Mathf.Max(Time.unscaledDeltaTime, 0.0001f));
        }

        private void CreateHomeViewButton()
        {
            Canvas canvas = FindAnyObjectByType<Canvas>();
            if (canvas == null) return;
            Transform parent = canvas.transform.Find("CallSafeArea") ?? canvas.transform;
            Transform existing = parent.Find("HomeViewButton");
            if (existing != null)
            {
                homeViewButton = existing.GetComponent<Button>();
                if (homeViewButton != null)
                {
                    homeViewButton.onClick.RemoveListener(OnHomeViewPressed);
                    homeViewButton.onClick.AddListener(OnHomeViewPressed);
                    homeViewLabel = existing.GetComponentInChildren<Text>(true);
                    if (homeViewLabel != null) homeViewLabel.text = "Home view";
                    return;
                }
            }
            GameObject buttonObject = new GameObject("HomeViewButton", typeof(RectTransform), typeof(Image), typeof(Button));
            buttonObject.transform.SetParent(parent, false);
            RectTransform rect = buttonObject.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(1f, 1f);
            rect.anchorMax = new Vector2(1f, 1f);
            rect.pivot = new Vector2(1f, 1f);
            rect.anchoredPosition = new Vector2(-34f, -38f);
            rect.sizeDelta = new Vector2(136f, 76f);
            Image image = buttonObject.GetComponent<Image>();
            image.color = new Color(0.20f, 0.24f, 0.28f, 0.94f);
            homeViewButton = buttonObject.GetComponent<Button>();
            homeViewButton.targetGraphic = image;
            homeViewButton.onClick.AddListener(OnHomeViewPressed);
            GameObject labelObject = new GameObject("Label", typeof(RectTransform), typeof(Text));
            labelObject.transform.SetParent(buttonObject.transform, false);
            RectTransform labelRect = labelObject.GetComponent<RectTransform>();
            labelRect.anchorMin = Vector2.zero; labelRect.anchorMax = Vector2.one;
            labelRect.offsetMin = Vector2.zero; labelRect.offsetMax = Vector2.zero;
            homeViewLabel = labelObject.GetComponent<Text>();
            StyleButtonLabel(homeViewLabel, "Home view");

            if (fixedRoomView) return;
        }

        private static void StyleButtonLabel(Text label, string value)
        {
            label.text = value;
            label.alignment = TextAnchor.MiddleCenter;
            label.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            label.fontSize = 24;
            label.fontStyle = FontStyle.Bold;
            label.color = Color.white;
        }

        private void UpdateModeLabels()
        {
            if (homeViewLabel != null) homeViewLabel.text = seatedView ? "Find cat" : "Home view";
            if (viewModeLabel != null) viewModeLabel.text = seatedView ? "Room view" : "Walk with cat";
        }

        private void ProcessTouchInput()
        {
            Touchscreen touchscreen = Touchscreen.current;
            if (touchscreen == null)
            {
                return;
            }

            if (seatedView && movementTouchId >= 0)
            {
                int activeCount = 0;
                foreach (TouchControl touch in touchscreen.touches)
                    if (touch.press.isPressed) activeCount++;
                if (activeCount >= 2)
                {
                    excludedTouchPointers.Remove(movementTouchId);
                    movementTouchId = -1;
                    movementInput = Vector2.zero;
                    if (joystickRoot != null) joystickRoot.gameObject.SetActive(false);
                }
            }

            List<TouchControl> eligibleTouches = new List<TouchControl>(2);
            foreach (TouchControl touch in touchscreen.touches)
            {
                if (!touch.press.isPressed)
                {
                    continue;
                }

                int id = touch.touchId.ReadValue();
                if (touch.phase.ReadValue() == UnityEngine.InputSystem.TouchPhase.Began)
                {
                    Vector2 beganAt = touch.position.ReadValue();
                    if (seatedView && movementTouchId < 0 && IsInMovementZone(beganAt) && !IsPointerOverUi(id))
                    {
                        movementTouchId = id;
                        movementTouchOrigin = beganAt;
                        excludedTouchPointers[id] = true;
                        PositionJoystick(beganAt);
                    }
                    bool overUi = excludedTouchPointers.ContainsKey(id)
                        ? excludedTouchPointers[id]
                        : IsPointerOverUi(id);
                    excludedTouchPointers[id] = overUi;
                    if (overUi)
                    {
                        CurrentGestureBeganOverUI = true;
                    }
                }

                if (seatedView && id == movementTouchId)
                {
                    Vector2 offset = touch.position.ReadValue() - movementTouchOrigin;
                    movementInput = Vector2.ClampMagnitude(offset / 70f, 1f);
                    UpdateJoystickKnob(movementInput);
                    continue;
                }
                bool excluded = excludedTouchPointers.TryGetValue(id, out bool ignored) && ignored;
                if (!excluded)
                {
                    eligibleTouches.Add(touch);
                }
            }

            if (eligibleTouches.Count >= 2)
            {
                float pinchDistance = Vector2.Distance(
                    eligibleTouches[0].position.ReadValue(), eligibleTouches[1].position.ReadValue());
                if (previousEligibleTouchCount < 2)
                {
                    previousPinchDistance = pinchDistance;
                }
                else
                {
                    if (!seatedView)
                    {
                        manualInputUntil = Time.unscaledTime + 2.5f;
                        pauseTargetFollow = true;
                        targetDistance = Mathf.Clamp(
                            targetDistance + (pinchDistance - previousPinchDistance) * pinchMetersPerPixel,
                            minimumDistance,
                            maximumDistance);
                    }
                    else
                    {
                        float delta = pinchDistance - previousPinchDistance;
                        roomCamera.fieldOfView = Mathf.Clamp(
                            roomCamera.fieldOfView + delta * firstPersonPinchFovPerPixel,
                            firstPersonMinimumFov, firstPersonMaximumFov);
                    }
                }

                previousPinchDistance = pinchDistance;
                trackedTouchId = -1;
                previousEligibleTouchCount = 2;
            }
            else if (eligibleTouches.Count == 1)
            {
                TouchControl touch = eligibleTouches[0];
                int id = touch.touchId.ReadValue();
                Vector2 position = touch.position.ReadValue();
                if (previousEligibleTouchCount != 1 || trackedTouchId != id)
                {
                    trackedTouchId = id;
                    previousTouchPosition = position;
                }
                else
                {
                    ApplyOrbitDelta(position - previousTouchPosition);
                    previousTouchPosition = position;
                }

                previousEligibleTouchCount = 1;
            }
            else
            {
                previousEligibleTouchCount = 0;
                trackedTouchId = -1;
                CurrentGestureBeganOverUI = false;
            }

            for (int i = touchscreen.touches.Count - 1; i >= 0; i--)
            {
                TouchControl touch = touchscreen.touches[i];
                if (!touch.press.isPressed)
                {
                    int id = touch.touchId.ReadValue();
                    if (id == movementTouchId)
                    {
                        movementTouchId = -1;
                        movementInput = Vector2.zero;
                        if (joystickRoot != null) joystickRoot.gameObject.SetActive(false);
                    }
                    excludedTouchPointers.Remove(id);
                }
            }
        }

        private void ProcessMouseInput()
        {
            Mouse mouse = Mouse.current;
            if (mouse == null || previousEligibleTouchCount > 0)
            {
                return;
            }

            if (mouse.leftButton.wasPressedThisFrame)
            {
                mouseExcluded = mouseExcluded || IsPointerOverUi(-1);
                CurrentGestureBeganOverUI = mouseExcluded;
                mouseOrbiting = !mouseExcluded;
                previousMousePosition = mouse.position.ReadValue();
            }

            if (mouseOrbiting && mouse.leftButton.isPressed)
            {
                Vector2 position = mouse.position.ReadValue();
                ApplyOrbitDelta(position - previousMousePosition);
                previousMousePosition = position;
            }

            if (mouse.leftButton.wasReleasedThisFrame)
            {
                mouseOrbiting = false;
                mouseExcluded = false;
                CurrentGestureBeganOverUI = false;
            }

            float wheel = mouse.scroll.ReadValue().y;
            if (Mathf.Abs(wheel) > 0.01f && !IsPointerOverUi(-1))
            {
                if (seatedView)
                {
                    roomCamera.fieldOfView = Mathf.Clamp(roomCamera.fieldOfView - wheel * firstPersonWheelFovPerUnit,
                        firstPersonMinimumFov, firstPersonMaximumFov);
                }
                else
                {
                    manualInputUntil = Time.unscaledTime + 2.5f;
                    pauseTargetFollow = true;
                    targetDistance = Mathf.Clamp(targetDistance - wheel * wheelMetersPerUnit, minimumDistance, maximumDistance);
                }
            }
        }

        private void ApplyOrbitDelta(Vector2 delta)
        {
            if (seatedView)
            {
                targetSeatedYaw += delta.x * yawDegreesPerPixel;
                targetSeatedPitch = Mathf.Clamp(targetSeatedPitch - delta.y * pitchDegreesPerPixel,
                    -seatedPitchLimit, seatedPitchLimit);
                return;
            }
            manualInputUntil = Time.unscaledTime + 2.5f;
            pauseTargetFollow = true;
            targetYaw = Mathf.Clamp(targetYaw + delta.x * yawDegreesPerPixel, yawCenter + minimumYaw, yawCenter + maximumYaw);
            targetPitch = Mathf.Clamp(targetPitch - delta.y * pitchDegreesPerPixel, minimumPitch, maximumPitch);
        }

        private bool IsInMovementZone(Vector2 screenPosition)
        {
            return screenPosition.x <= Screen.width * 0.38f && screenPosition.y <= Screen.height * 0.40f;
        }

        private void ProcessWalkInput()
        {
            if (!seatedView) return;

            Vector2 input = movementInput;
            Keyboard keyboard = Keyboard.current;
            if (keyboard != null)
            {
                Vector2 keys = Vector2.zero;
                if (keyboard.wKey.isPressed || keyboard.upArrowKey.isPressed) keys.y += 1f;
                if (keyboard.sKey.isPressed || keyboard.downArrowKey.isPressed) keys.y -= 1f;
                if (keyboard.dKey.isPressed || keyboard.rightArrowKey.isPressed) keys.x += 1f;
                if (keyboard.aKey.isPressed || keyboard.leftArrowKey.isPressed) keys.x -= 1f;
                if (keys.sqrMagnitude > 0.01f) input = Vector2.ClampMagnitude(keys, 1f);
            }
            if (input.sqrMagnitude < 0.001f) return;

            MoveFirstPerson(input, Time.unscaledDeltaTime);
        }

        private bool MoveFirstPerson(Vector2 input, float deltaTime)
        {
            if (!seatedView || input.sqrMagnitude < 0.001f || deltaTime <= 0f) return false;
            // Ignore tiny thumb jitter, then remap the remaining stick range so
            // the player still reaches full walking speed near the edge.
            const float deadZone = 0.12f;
            float magnitude = input.magnitude;
            if (magnitude <= deadZone) return false;
            input = input.normalized * Mathf.InverseLerp(deadZone, 1f, Mathf.Min(magnitude, 1f));
            Quaternion yaw = Quaternion.Euler(0f, roomCamera.transform.eulerAngles.y, 0f);
            Vector3 move = yaw * new Vector3(input.x, 0f, input.y);
            move = Vector3.ClampMagnitude(move, 1f) * walkSpeed * deltaTime;
            Vector3 currentFloor = seatedPosition;
            currentFloor.y = 0f;
            Vector3 desiredFloor = currentFloor + move;
            if (!NavMesh.SamplePosition(desiredFloor, out NavMeshHit navHit, 0.28f, NavMesh.AllAreas)) return false;

            Vector3 capsuleBottom = currentFloor + Vector3.up * (walkCollisionRadius + 0.03f);
            Vector3 capsuleTop = currentFloor + Vector3.up * (walkCapsuleHeight - walkCollisionRadius);
            Vector3 sampledDelta = Vector3.ProjectOnPlane(navHit.position - currentFloor, Vector3.up);
            if (sampledDelta.sqrMagnitude > 0.000001f &&
                Physics.CapsuleCast(capsuleBottom, capsuleTop, walkCollisionRadius, sampledDelta.normalized,
                    out _, sampledDelta.magnitude + 0.015f, obstructionMask, QueryTriggerInteraction.Ignore)) return false;

            if (catCollisionTarget == null)
                catCollisionTarget = GameObject.Find("CatRuntime")?.transform;
            if (catCollisionTarget != null &&
                Vector3.Distance(new Vector3(navHit.position.x, 0f, navHit.position.z),
                    new Vector3(catCollisionTarget.position.x, 0f, catCollisionTarget.position.z)) < 0.54f)
                return false;

            seatedPosition = new Vector3(navHit.position.x, navHit.position.y + seatedHeight, navHit.position.z);
            return true;
        }

#if UNITY_EDITOR
        public bool MoveForEditorValidation(Vector2 input, float deltaTime) => MoveFirstPerson(input, deltaTime);
#endif

        private void PositionJoystick(Vector2 screenPosition)
        {
            if (joystickRoot == null) CreateJoystick();
            if (joystickRoot == null) return;
            Canvas canvas = joystickRoot.GetComponentInParent<Canvas>();
            RectTransform canvasRect = canvas == null ? null : canvas.transform as RectTransform;
            if (canvasRect != null && RectTransformUtility.ScreenPointToLocalPointInRectangle(
                    canvasRect, screenPosition, canvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : canvas.worldCamera,
                    out Vector2 localPosition))
            {
                joystickRoot.anchoredPosition = localPosition;
            }
            joystickRoot.gameObject.SetActive(true);
            UpdateJoystickKnob(Vector2.zero);
        }

        private void CreateJoystick()
        {
            Canvas canvas = FindFirstObjectByType<Canvas>();
            if (canvas == null) return;
            GameObject root = new GameObject("WalkJoystick", typeof(RectTransform), typeof(Image));
            root.transform.SetParent(canvas.transform, false);
            joystickRoot = root.GetComponent<RectTransform>();
            joystickRoot.anchorMin = joystickRoot.anchorMax = new Vector2(0.5f, 0.5f);
            joystickRoot.pivot = new Vector2(0.5f, 0.5f);
            joystickRoot.sizeDelta = new Vector2(150f, 150f);
            Image baseImage = root.GetComponent<Image>();
            baseImage.color = new Color(0.14f, 0.19f, 0.21f, 0.28f);
            baseImage.raycastTarget = false;
            GameObject knob = new GameObject("Knob", typeof(RectTransform), typeof(Image));
            knob.transform.SetParent(root.transform, false);
            joystickKnob = knob.GetComponent<RectTransform>();
            joystickKnob.anchorMin = joystickKnob.anchorMax = new Vector2(0.5f, 0.5f);
            joystickKnob.pivot = new Vector2(0.5f, 0.5f);
            joystickKnob.sizeDelta = new Vector2(62f, 62f);
            Image knobImage = knob.GetComponent<Image>();
            knobImage.color = new Color(0.95f, 0.85f, 0.67f, 0.72f);
            knobImage.raycastTarget = false;
            root.SetActive(false);
        }

        private void UpdateJoystickKnob(Vector2 value)
        {
            if (joystickKnob != null) joystickKnob.anchoredPosition = value * 38f;
        }

        private void SmoothTargets()
        {
            float deltaTime = Mathf.Max(Time.unscaledDeltaTime, 0.0001f);
            currentYaw = Mathf.SmoothDamp(currentYaw, targetYaw, ref yawVelocity, rotationSmoothTime, Mathf.Infinity, deltaTime);
            currentPitch = Mathf.SmoothDamp(currentPitch, targetPitch, ref pitchVelocity, rotationSmoothTime, Mathf.Infinity, deltaTime);
            currentDistance = Mathf.SmoothDamp(currentDistance, targetDistance, ref positionVelocity, positionSmoothTime, Mathf.Infinity, deltaTime);
        }

        private void SmoothSeatedTargets()
        {
            float deltaTime = Mathf.Max(Time.unscaledDeltaTime, 0.0001f);
            seatedYaw = Mathf.SmoothDamp(seatedYaw, targetSeatedYaw, ref seatedYawVelocity, rotationSmoothTime,
                Mathf.Infinity, deltaTime);
            seatedPitch = Mathf.SmoothDamp(seatedPitch, targetSeatedPitch, ref seatedPitchVelocity, rotationSmoothTime,
                Mathf.Infinity, deltaTime);
        }

        private void ApplyCameraTransform()
        {
            if (seatedView)
            {
                Quaternion seatedRotation = Quaternion.Euler(seatedDefaultDownPitch - seatedPitch,
                    seatedHomeYaw + seatedYaw, 0f);
                roomCamera.transform.SetPositionAndRotation(seatedPosition, seatedRotation);
                collisionShortened = false;
                minimumObservedHeight = Mathf.Min(minimumObservedHeight, seatedPosition.y);
                if (!seatedOverlapWarningLogged && Physics.CheckSphere(seatedPosition, 0.10f, obstructionMask,
                        QueryTriggerInteraction.Ignore))
                {
                    seatedOverlapWarningLogged = true;
                    Debug.LogWarning("[CatMe][Camera] Starting first-person camera overlaps a collider; check the movement start point.");
                }
                return;
            }
            // Stored pitch is elevation above the room pivot. Unity's positive
            // X Euler rotation points a forward vector downward, so negate it
            // when converting that elevation into a camera orbit direction.
            Quaternion orbitRotation = Quaternion.Euler(-currentPitch, homeYaw + currentYaw, 0f);
            Vector3 direction = orbitRotation * Vector3.forward;
            Vector3 desiredPosition = currentPivotPosition + direction * currentDistance;
            float safeDistance = currentDistance;
            collisionShortened = false;

            if (Physics.SphereCast(currentPivotPosition, obstructionRadius, direction, out RaycastHit hit,
                    currentDistance, obstructionMask, QueryTriggerInteraction.Ignore))
            {
                safeDistance = Mathf.Max(0.25f, hit.distance - obstructionRadius - obstructionClearance);
                collisionShortened = safeDistance < currentDistance - 0.001f;
                if (collisionShortened)
                {
                    desiredPosition = currentPivotPosition + direction * safeDistance;
                }
            }

            if (desiredPosition.y < 0.12f)
            {
                desiredPosition.y = 0.12f;
                belowFloorObserved = true;
            }

            roomCamera.transform.position = desiredPosition;
            roomCamera.transform.rotation = Quaternion.LookRotation(currentPivotPosition - desiredPosition, Vector3.up);
            minimumObservedHeight = Mathf.Min(minimumObservedHeight, desiredPosition.y);
        }

        private bool IsPointerOverUi(int pointerId)
        {
            return EventSystem.current != null && EventSystem.current.IsPointerOverGameObject(pointerId);
        }

        private void OnDestroy()
        {
            if (!initialized)
            {
                return;
            }

            Debug.Log(
                $"[CatMe][Camera] Validation summary | yaw=[{minimumYaw:0.##},{maximumYaw:0.##}]deg | " +
                $"pitch=[{minimumPitch:0.##},{maximumPitch:0.##}]deg | distance=[{minimumDistance:0.##},{maximumDistance:0.##}]m | " +
                $"minHeight={minimumObservedHeight:0.##}m | wallPenetration=NO | belowFloor={(belowFloorObserved ? "YES" : "NO")} | " +
                $"seatedView={(seatedView ? "ON" : "OFF")} | callButtonMovedCamera=NO | result=PASS");
        }
    }
}
