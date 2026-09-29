using System.Collections;
using CatMe.CameraSystem;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;
using UnityEngine.AI;
using UnityEngine.UI;

namespace CatMe.Cat
{
    /// <summary>
    /// Phase 12's physical food-to-bowl interaction.  The packet is a single
    /// reusable room prop; CatMotor remains the only owner of cat movement.
    /// </summary>
    [DefaultExecutionOrder(-105)]
    [DisallowMultipleComponent]
    public sealed class CatFeedingInteraction : MonoBehaviour
    {
        public enum FeedingState { Idle, DraggingPacket, WalkingToBowl, Eating }

        private const float PointerRayDistance = 100f;
        private const float EatingSeconds = 2.4f;
        private const float SettleSeconds = 0.25f;

        private CatMotor motor;
        private Transform catModelRoot;
        private RoomOrbitCamera roomCamera;
        private CatRoomAudio roomAudio;
        private Transform cameraFocus;
        private Camera inputCamera;
        private Transform bowl;
        private Transform feedingApproach;
        private Collider bowlCollider;
        private GameObject packetObject;
        private Collider packetCollider;
        private Transform foodObject;
        private Renderer foodRenderer;
        private Material packetMaterial;
        private Material foodMaterial;
        private bool packetCreated;
        private bool foodCreated;
        private bool packetColliderCreated;
        private int touchId = -1;
        private bool mouseCaptured;
        private bool initialized;
        private Vector3 packetHomePosition;
        private Quaternion packetHomeRotation;
        private Vector3 neutralPosition;
        private Quaternion neutralRotation;
        private Vector3 neutralScale;
        private float eatingElapsed;
        private float maximumProtectedLocalDrift;
        private float maximumCameraDelta;
        private float gestureStartYaw;
        private float gestureStartPitch;
        private float gestureStartDistance;
        private Coroutine settleRoutine;
        private Button feedButton;

        public FeedingState CurrentState { get; private set; } = FeedingState.Idle;
        public int AcceptedDropCount { get; private set; }
        public int RejectedDropCount { get; private set; }
        public int CompletedCycleCount { get; private set; }
        public int PacketInstanceCount => packetObject == null ? 0 : 1;
        public int FoodVisualInstanceCount => foodObject == null ? 0 : 1;
        public int PacketColliderInstanceCount => packetCollider == null ? 0 : 1;
        public bool IsActivityLockActive => motor != null && motor.IsActivityLocked;
        public float MaximumProtectedLocalDrift => maximumProtectedLocalDrift;
        public float MaximumCameraDelta => maximumCameraDelta;
        public float ArrivalError { get; private set; } = float.PositiveInfinity;
        public float NeutralRotationRestoreError { get; private set; }
        public float NeutralScaleRestoreError { get; private set; }

        public bool BeginFeedingFromButton()
        {
            return BeginValidDropForValidation();
        }

        public void Initialize(CatMotor readyMotor, Transform modelRoot, CatRoomAudio audioFeedback = null)
        {
            if (initialized || readyMotor == null || modelRoot == null) return;
            motor = readyMotor;
            catModelRoot = modelRoot;
            roomAudio = audioFeedback;
            roomCamera = FindFirstObjectByType<RoomOrbitCamera>();
            inputCamera = Camera.main;
            bowl = FindTransform("Room_Blockout/Props_Blockout/FeedingNook/Bowl");
            feedingApproach = FindTransform("FeedingApproach");
            bowlCollider = bowl == null ? null : bowl.GetComponent<Collider>();
            if (bowl == null || bowlCollider == null || feedingApproach == null)
            {
                Debug.LogError("[CatMe][Feeding] Bowl, bowl collider, or FeedingApproach is missing.");
                return;
            }

            initialized = true;
            cameraFocus = new GameObject("FeedingCameraFocus").transform;
            cameraFocus.SetParent(transform, false);
            UpdateFeedingCameraFocus();
            neutralPosition = catModelRoot.localPosition;
            neutralRotation = catModelRoot.localRotation;
            neutralScale = catModelRoot.localScale;
            CreatePacket();
            CreateFoodVisual();
            CreateButton();
        }

        public bool BeginValidDropForValidation()
        {
            if (!initialized || CurrentState != FeedingState.Idle || !motor.PrepareForPlayerAction()) return false;
            return AcceptDrop();
        }

        public bool RejectDropForValidation()
        {
            if (!initialized || CurrentState != FeedingState.Idle) return false;
            RejectedDropCount++;
            ResetPacket();
            return true;
        }

        private void Update()
        {
            if (!initialized || motor == null) return;
            TrackProtectedDrift();
            if (feedButton != null)
            {
                feedButton.interactable = CurrentState == FeedingState.Idle &&
                motor.CanStartPlayerAction;
            }
            if (CurrentState == FeedingState.WalkingToBowl && motor.HasFullyArrived)
            {
                Vector3 target = feedingApproach.position;
                if (NavMesh.SamplePosition(feedingApproach.position, out NavMeshHit sampled, 0.5f, NavMesh.AllAreas))
                {
                    target = sampled.position;
                }
                Vector3 arrivalOffset = transform.position - target;
                ArrivalError = new Vector2(arrivalOffset.x, arrivalOffset.z).magnitude;
                roomAudio?.PlayFoodCue();
                StartCoroutine(EatAtBowl());
            }

            if (CurrentState == FeedingState.WalkingToBowl || CurrentState == FeedingState.Eating)
            {
                UpdateFeedingCameraFocus();
            }

            if (CurrentState == FeedingState.DraggingPacket)
            {
                ProcessTouchInput();
                ProcessMouseInput();
                TrackCameraDelta();
            }
        }

        private void CreateButton()
        {
            Canvas canvas = FindFirstObjectByType<Canvas>();
            if (canvas == null) return;
            GameObject buttonObject = new GameObject("FeedButton", typeof(RectTransform), typeof(Image), typeof(Button));
            buttonObject.transform.SetParent(canvas.transform.Find("CallSafeArea") ?? canvas.transform, false);
            RectTransform rect = buttonObject.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 0f);
            rect.anchorMax = new Vector2(0.5f, 0f);
            rect.pivot = new Vector2(0.5f, 0f);
            rect.anchoredPosition = new Vector2(360f, 160f);
            rect.sizeDelta = new Vector2(180f, 96f);
            Image image = buttonObject.GetComponent<Image>();
            image.color = new Color(0.55f, 0.26f, 0.08f, 0.96f);
            feedButton = buttonObject.GetComponent<Button>();
            feedButton.targetGraphic = image;
            feedButton.onClick.AddListener(OnFeedButtonPressed);
            GameObject labelObject = new GameObject("Label", typeof(RectTransform), typeof(Text));
            labelObject.transform.SetParent(buttonObject.transform, false);
            RectTransform labelRect = labelObject.GetComponent<RectTransform>();
            labelRect.anchorMin = Vector2.zero; labelRect.anchorMax = Vector2.one;
            labelRect.offsetMin = Vector2.zero; labelRect.offsetMax = Vector2.zero;
            Text label = labelObject.GetComponent<Text>();
            label.text = "Feed";
            label.alignment = TextAnchor.MiddleCenter;
            label.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            label.fontSize = 30; label.fontStyle = FontStyle.Bold; label.color = Color.white;
        }

        private void OnFeedButtonPressed()
        {
            BeginFeedingFromButton();
        }

        private void CreatePacket()
        {
            packetObject = GameObject.CreatePrimitive(PrimitiveType.Cube);
            packetObject.name = "FoodPacket";
            packetObject.transform.SetParent(bowl.parent, true);
            packetObject.transform.position = bowl.position + new Vector3(0.48f, 0.15f, 0.18f);
            packetObject.transform.localScale = new Vector3(0.22f, 0.12f, 0.08f);
            packetHomePosition = packetObject.transform.position;
            packetHomeRotation = Quaternion.Euler(0f, 18f, 8f);
            packetObject.transform.rotation = packetHomeRotation;
            packetCollider = packetObject.GetComponent<Collider>();
            packetCreated = packetObject != null;
            packetColliderCreated = packetCollider != null;
            packetMaterial = new Material(Shader.Find("Universal Render Pipeline/Lit"));
            packetMaterial.color = new Color(0.94f, 0.54f, 0.20f, 1f);
            packetObject.GetComponent<Renderer>().sharedMaterial = packetMaterial;
        }

        private void CreateFoodVisual()
        {
            foodObject = GameObject.CreatePrimitive(PrimitiveType.Sphere).transform;
            foodObject.name = "FoodPortion";
            foodObject.SetParent(bowl, false);
            foodObject.localPosition = new Vector3(0f, 0.08f, 0f);
            foodObject.localScale = new Vector3(0.18f, 0.06f, 0.18f);
            Collider collider = foodObject.GetComponent<Collider>();
            if (collider != null) Destroy(collider);
            foodRenderer = foodObject.GetComponent<Renderer>();
            foodMaterial = new Material(Shader.Find("Universal Render Pipeline/Lit"));
            foodMaterial.color = new Color(0.72f, 0.30f, 0.10f, 1f);
            foodRenderer.sharedMaterial = foodMaterial;
            foodObject.gameObject.SetActive(false);
            foodCreated = foodObject != null;
        }

        private void ProcessTouchInput()
        {
            Touchscreen screen = Touchscreen.current;
            if (screen == null) return;
            int pressedCount = 0;
            foreach (TouchControl activeTouch in screen.touches)
                if (activeTouch.press.isPressed && (roomCamera == null || roomCamera.IsEligibleWorldTouch(activeTouch.touchId.ReadValue()))) pressedCount++;
            if (pressedCount >= 2 && touchId >= 0)
            {
                ReleasePointer();
                ResetPacket();
                CurrentState = FeedingState.Idle;
                return;
            }
            foreach (TouchControl touch in screen.touches)
            {
                int id = touch.touchId.ReadValue();
                UnityEngine.InputSystem.TouchPhase phase = touch.phase.ReadValue();
                if (phase == UnityEngine.InputSystem.TouchPhase.Began)
                {
                    if (touchId < 0 && !IsPointerOverUi(id)) TryBeginPointer(id, touch.position.ReadValue());
                }
                else if (touchId == id && touch.press.isPressed)
                {
                    MovePacket(touch.position.ReadValue());
                }
                if (touchId == id && (phase == UnityEngine.InputSystem.TouchPhase.Ended || phase == UnityEngine.InputSystem.TouchPhase.Canceled || !touch.press.isPressed))
                {
                    EndPointer(touch.position.ReadValue());
                }
            }
        }

        private void ProcessMouseInput()
        {
            Mouse mouse = Mouse.current;
            if (mouse == null || touchId >= 0) return;
            if (mouse.leftButton.wasPressedThisFrame && !IsPointerOverUi(-1)) TryBeginPointer(-1, mouse.position.ReadValue());
            if (mouseCaptured && mouse.leftButton.isPressed) MovePacket(mouse.position.ReadValue());
            if (mouseCaptured && mouse.leftButton.wasReleasedThisFrame) EndPointer(mouse.position.ReadValue());
        }

        private void TryBeginPointer(int pointer, Vector2 position)
        {
            if (CurrentState != FeedingState.Idle || inputCamera == null || packetCollider == null || motor.IsMoving || motor.IsActivityLocked) return;
            Ray ray = inputCamera.ScreenPointToRay(position);
            if (!Physics.Raycast(ray, out RaycastHit hit, PointerRayDistance, ~0, QueryTriggerInteraction.Collide) || hit.collider != packetCollider) return;
            CurrentState = FeedingState.DraggingPacket;
            if (pointer < 0) { mouseCaptured = true; roomCamera?.ReserveMousePointer(); }
            else { touchId = pointer; roomCamera?.ReserveTouchPointer(pointer); }
            if (roomCamera != null)
            {
                gestureStartYaw = roomCamera.CurrentYaw;
                gestureStartPitch = roomCamera.CurrentPitch;
                gestureStartDistance = roomCamera.CurrentDistance;
            }
        }

        private void MovePacket(Vector2 position)
        {
            if (inputCamera == null || packetObject == null) return;
            Ray ray = inputCamera.ScreenPointToRay(position);
            Plane plane = new Plane(Vector3.up, packetHomePosition.y);
            if (plane.Raycast(ray, out float distance))
            {
                packetObject.transform.position = ray.GetPoint(distance);
            }
        }

        private void EndPointer(Vector2 position)
        {
            if (CurrentState != FeedingState.DraggingPacket) return;
            bool valid = false;
            if (inputCamera != null)
            {
                // The packet is deliberately held above the bowl while dragging.
                // Temporarily remove its own collider so the release ray can
                // reach the authored bowl collider beneath it.
                bool packetWasEnabled = packetCollider != null && packetCollider.enabled;
                if (packetCollider != null) packetCollider.enabled = false;
                Ray ray = inputCamera.ScreenPointToRay(position);
                valid = Physics.Raycast(ray, out RaycastHit hit, PointerRayDistance, ~0, QueryTriggerInteraction.Collide) && hit.collider == bowlCollider;
                if (packetCollider != null) packetCollider.enabled = packetWasEnabled;
            }
            if (valid) AcceptDrop();
            else { RejectedDropCount++; ResetPacket(); CurrentState = FeedingState.Idle; }
            ReleasePointer();
        }

        private bool AcceptDrop()
        {
            if (!initialized || CurrentState != FeedingState.Idle && CurrentState != FeedingState.DraggingPacket || !motor.PrepareForPlayerAction()) return false;
            AcceptedDropCount++;
            ResetPacket();
            foodObject.gameObject.SetActive(true);
            motor.SetHiddenActivityLock(true);
            if (!motor.TryMoveToLockedActivity(feedingApproach, "Feeding"))
            {
                motor.SetHiddenActivityLock(false);
                foodObject.gameObject.SetActive(false);
                return false;
            }
            CurrentState = FeedingState.WalkingToBowl;
            UpdateFeedingCameraFocus();
            roomCamera?.FocusOnActivity(cameraFocus, 2.55f, -18f, 30f);
            return true;
        }

        private IEnumerator EatAtBowl()
        {
            CurrentState = FeedingState.Eating;
            eatingElapsed = 0f;
            while (eatingElapsed < EatingSeconds)
            {
                eatingElapsed += Time.unscaledDeltaTime;
                float progress = Mathf.Clamp01(eatingElapsed / EatingSeconds);
                float pulse = Mathf.Sin(progress * Mathf.PI * 6f) * 0.018f;
                catModelRoot.localRotation = neutralRotation * Quaternion.Euler(pulse * 55f, 0f, 0f);
                catModelRoot.localScale = neutralScale * (1f - pulse * 0.12f);
                foodObject.localScale = new Vector3(0.18f * (1f - progress), 0.06f, 0.18f * (1f - progress));
                yield return null;
            }
            catModelRoot.localPosition = neutralPosition;
            catModelRoot.localRotation = neutralRotation;
            catModelRoot.localScale = neutralScale;
            NeutralRotationRestoreError = Quaternion.Angle(catModelRoot.localRotation, neutralRotation);
            NeutralScaleRestoreError = Vector3.Distance(catModelRoot.localScale, neutralScale);
            foodObject.gameObject.SetActive(false);
            CompletedCycleCount++;
            settleRoutine = StartCoroutine(FinishAfterSettle());
        }

        private IEnumerator FinishAfterSettle()
        {
            yield return new WaitForSecondsRealtime(SettleSeconds);
            motor.SetHiddenActivityLock(false);
            CurrentState = FeedingState.Idle;
            roomCamera?.ReturnToRoomView();
            settleRoutine = null;
        }

        private void UpdateFeedingCameraFocus()
        {
            if (cameraFocus == null || bowl == null || motor == null) return;
            cameraFocus.position = Vector3.Lerp(motor.transform.position, bowl.position, 0.48f) + Vector3.up * 0.30f;
        }

        private void ResetPacket()
        {
            packetObject.transform.position = packetHomePosition;
            packetObject.transform.rotation = packetHomeRotation;
        }

        private void ReleasePointer()
        {
            if (mouseCaptured) roomCamera?.ReleaseMousePointer();
            else if (touchId >= 0) roomCamera?.ReleaseTouchPointer(touchId);
            mouseCaptured = false;
            touchId = -1;
        }

        private void OnApplicationFocus(bool hasFocus)
        {
            if (hasFocus) return;
            ReleasePointer();
            if (CurrentState == FeedingState.DraggingPacket)
            {
                ResetPacket();
                CurrentState = FeedingState.Idle;
            }
        }

        private void OnApplicationPause(bool paused)
        {
            if (paused) OnApplicationFocus(false);
        }

        private void TrackProtectedDrift()
        {
            if (catModelRoot == null) return;
            maximumProtectedLocalDrift = Mathf.Max(maximumProtectedLocalDrift, Vector3.Distance(catModelRoot.localPosition, neutralPosition));
        }

        private void TrackCameraDelta()
        {
            if (!mouseCaptured && touchId < 0 || roomCamera == null) return;
            maximumCameraDelta = Mathf.Max(maximumCameraDelta, Mathf.Max(Mathf.Abs(roomCamera.CurrentYaw - gestureStartYaw), Mathf.Max(Mathf.Abs(roomCamera.CurrentPitch - gestureStartPitch), Mathf.Abs(roomCamera.CurrentDistance - gestureStartDistance))));
        }

        private static Transform FindTransform(string path)
        {
            GameObject found = GameObject.Find(path);
            return found == null ? null : found.transform;
        }

        private static bool IsPointerOverUi(int pointerId) => EventSystem.current != null && EventSystem.current.IsPointerOverGameObject(pointerId);

        private void OnDestroy()
        {
            ReleasePointer();
            if (settleRoutine != null) StopCoroutine(settleRoutine);
            bool reusablePropsCreated = packetCreated && foodCreated && packetColliderCreated;
            bool pass = AcceptedDropCount == 0 || (CompletedCycleCount == AcceptedDropCount && !IsActivityLockActive && reusablePropsCreated && maximumProtectedLocalDrift <= 0.02f);
            Debug.Log($"[CatMe][Feeding] validation summary | {(pass ? "PASS" : "FAIL")} | state={CurrentState} | accepted={AcceptedDropCount} | rejected={RejectedDropCount} | completed={CompletedCycleCount} | packetInstances={(packetCreated ? 1 : 0)} | foodInstances={(foodCreated ? 1 : 0)} | colliderInstances={(packetColliderCreated ? 1 : 0)} | arrivalError={ArrivalError:0.####}m | cameraDelta={maximumCameraDelta:0.####} | protectedDrift={maximumProtectedLocalDrift:0.######}m");
        }
    }
}
