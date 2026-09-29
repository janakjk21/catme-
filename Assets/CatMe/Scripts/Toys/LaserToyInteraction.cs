using System.Collections;
using CatMe.CameraSystem;
using CatMe.Cat;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;
using UnityEngine.AI;
using UnityEngine.UI;

namespace CatMe.Toys
{
    /// <summary>Small, prop-led laser activity using the existing room toy.</summary>
    [DefaultExecutionOrder(-110)]
    [DisallowMultipleComponent]
    public sealed class LaserToyInteraction : MonoBehaviour
    {
        public enum LaserState { Idle, Aiming, Chasing, Catch }

        private const int CatchLimit = 3;
        private const float PointerRayDistance = 100f;
        private const float CatchSeconds = 0.38f;
        private const float MinimumTargetDistance = 0.35f;

        private CatMotor motor;
        private CatEnergy energy;
        private Transform catModelRoot;
        private RoomOrbitCamera roomCamera;
        private Camera inputCamera;
        private Transform toy;
        private Collider toyTrigger;
        private Collider floorCollider;
        private Transform openPlayZone;
        private Transform targetTransform;
        private GameObject dotObject;
        private Renderer dotRenderer;
        private Light dotLight;
        private Material dotMaterial;
        private int touchId = -1;
        private bool mouseCaptured;
        private bool activationPointer;
        private bool initialized;
        private bool targetCandidate;
        private Vector3 candidatePoint;
        private Vector3 neutralModelScale;
        private Quaternion neutralModelRotation;
        private float catchElapsed;
        private float gestureStartYaw;
        private float gestureStartPitch;
        private float gestureStartDistance;
        private float maximumCameraDelta;
        private float maximumProtectedLocalPositionDrift;
        private float maximumRotationRestoreError;
        private float maximumScaleRestoreError;
        private int sessionCount;
        private int acceptedTargetCount;
        private int rejectedTargetCount;
        private int successfulCatchCount;
        private int hapticRequestCount;
        private bool summaryLogged;
        private bool validationAppliesEnergy;
        private Button laserButton;

        public LaserState CurrentState { get; private set; } = LaserState.Idle;
        public int SessionCount => sessionCount;
        public int AcceptedTargetCount => acceptedTargetCount;
        public int RejectedTargetCount => rejectedTargetCount;
        public int SuccessfulCatchCount => successfulCatchCount;
        public int HapticRequestCount => hapticRequestCount;
        public bool IsToyPointerCaptured => mouseCaptured || touchId >= 0;
        public bool IsActivityLockActive => motor != null && motor.IsActivityLocked;
        public int LaserDotInstanceCount => dotObject == null ? 0 : 1;
        public float MaximumCameraDelta => maximumCameraDelta;
        public float MaximumProtectedLocalPositionDrift => maximumProtectedLocalPositionDrift;
        public float MaximumRotationRestoreError => maximumRotationRestoreError;
        public float MaximumScaleRestoreError => maximumScaleRestoreError;
        public bool IsPlayAllowed => energy == null || energy.IsPlayAllowed;
        private bool IsPointerCaptured => mouseCaptured || touchId >= 0;

        public bool BeginSessionFromButton()
        {
            return BeginSessionForValidation(true);
        }

        public bool BeginSessionForValidation()
        {
            return BeginSessionForValidation(false);
        }

        public bool BeginSessionForValidation(bool applyEnergy)
        {
            if (!initialized || !IsPlayAllowed || CurrentState != LaserState.Idle || !motor.PrepareForPlayerAction())
            {
                return false;
            }

            motor.SetHiddenActivityLock(true);
            CurrentState = LaserState.Aiming;
            sessionCount++;
            acceptedTargetCount = 0;
            rejectedTargetCount = 0;
            successfulCatchCount = 0;
            summaryLogged = false;
            validationAppliesEnergy = applyEnergy;
            SetDotVisible(false);
            return true;
        }

        public bool PlaceTargetForValidation(Vector3 point)
        {
            if (CurrentState != LaserState.Aiming || !TryResolveTarget(new Ray(point + Vector3.up * 5f, Vector3.down), out Vector3 sampled))
            {
                rejectedTargetCount++;
                return false;
            }

            targetTransform.position = sampled;
            acceptedTargetCount++;
            CurrentState = LaserState.Chasing;
            SetDotVisible(true);
            if (motor.TryMoveToLockedActivity(targetTransform, "Laser validation target"))
            {
                return true;
            }

            acceptedTargetCount--;
            rejectedTargetCount++;
            CurrentState = LaserState.Aiming;
            SetDotVisible(false);
            return false;
        }

        public void Initialize(CatMotor readyMotor, Transform modelRoot)
        {
            if (initialized || readyMotor == null || modelRoot == null)
            {
                return;
            }

            motor = readyMotor;
            catModelRoot = modelRoot;
            roomCamera = FindFirstObjectByType<RoomOrbitCamera>();
            inputCamera = Camera.main;
            toy = FindTransform("Room_Blockout/Props_Blockout/ToyBasket/Toy");
            floorCollider = FindTransform("Room_Blockout/Architecture/Floor")?.GetComponent<Collider>();
            openPlayZone = FindTransform("Room_Blockout/Zones/OpenPlayZone");
            if (toy == null || floorCollider == null || openPlayZone == null)
            {
                Debug.LogError("[CatMe][Laser] Toy, floor, or OpenPlayZone is missing; laser was not started.");
                return;
            }

            initialized = true;
            neutralModelRotation = catModelRoot.localRotation;
            neutralModelScale = catModelRoot.localScale;
            CreateToyTrigger();
            CreateTargetAndDot();
            CreateButton();
        }

        public void BindEnergy(CatEnergy readyEnergy)
        {
            energy = readyEnergy;
        }

        private void Update()
        {
            if (!initialized || motor == null)
            {
                return;
            }

            TrackProtectedPosition();
            if (laserButton != null)
            {
                laserButton.interactable = IsPlayAllowed && CurrentState == LaserState.Idle &&
                    !motor.IsMoving && !motor.IsActivityLocked;
            }
            if (CurrentState == LaserState.Chasing && motor.HasFullyArrived)
            {
                StartCoroutine(PerformCatch());
            }

            if (CurrentState == LaserState.Idle || CurrentState == LaserState.Aiming)
            {
                ProcessTouchInput();
                ProcessMouseInput();
                TrackCameraDelta();
            }
            if (dotObject != null && dotObject.activeSelf)
            {
                float pulse = 0.82f + Mathf.Sin(Time.unscaledTime * 7.5f) * 0.18f;
                dotObject.transform.localScale = new Vector3(0.064f, 0.008f, 0.064f) * pulse;
                if (dotLight != null) dotLight.intensity = 1.15f * pulse;
            }
        }

        private void CreateButton()
        {
            Canvas canvas = FindFirstObjectByType<Canvas>();
            if (canvas == null) return;
            GameObject buttonObject = new GameObject("LaserButton", typeof(RectTransform), typeof(Image), typeof(Button));
            buttonObject.transform.SetParent(canvas.transform.Find("CallSafeArea") ?? canvas.transform, false);
            RectTransform rect = buttonObject.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 0f);
            rect.anchorMax = new Vector2(0.5f, 0f);
            rect.pivot = new Vector2(0.5f, 0f);
            rect.anchoredPosition = new Vector2(-360f, 160f);
            rect.sizeDelta = new Vector2(180f, 96f);
            Image image = buttonObject.GetComponent<Image>();
            image.color = new Color(0.25f, 0.10f, 0.30f, 0.96f);
            laserButton = buttonObject.GetComponent<Button>();
            laserButton.targetGraphic = image;
            laserButton.onClick.AddListener(OnLaserButtonPressed);
            GameObject labelObject = new GameObject("Label", typeof(RectTransform), typeof(Text));
            labelObject.transform.SetParent(buttonObject.transform, false);
            RectTransform labelRect = labelObject.GetComponent<RectTransform>();
            labelRect.anchorMin = Vector2.zero; labelRect.anchorMax = Vector2.one;
            labelRect.offsetMin = Vector2.zero; labelRect.offsetMax = Vector2.zero;
            Text label = labelObject.GetComponent<Text>();
            label.text = "Laser";
            label.alignment = TextAnchor.MiddleCenter;
            label.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            label.fontSize = 30; label.fontStyle = FontStyle.Bold; label.color = Color.white;
        }

        private void OnLaserButtonPressed()
        {
            BeginSessionFromButton();
        }

        private void CreateToyTrigger()
        {
            GameObject target = new GameObject("LaserToyTapTarget");
            target.transform.SetParent(toy, false);
            target.transform.localPosition = Vector3.zero;
            // Art roots may be scaled tiny; keep the interaction target in world metres.
            Vector3 inherited = toy.lossyScale;
            target.transform.localScale = new Vector3(
                1f / Mathf.Max(Mathf.Abs(inherited.x), .0001f),
                1f / Mathf.Max(Mathf.Abs(inherited.y), .0001f),
                1f / Mathf.Max(Mathf.Abs(inherited.z), .0001f));
            BoxCollider box = target.AddComponent<BoxCollider>();
            box.size = new Vector3(0.58f, 0.72f, 0.58f);
            box.center = new Vector3(0f, 0.1f, 0f);
            box.isTrigger = true;
            toyTrigger = box;
        }

        private void CreateTargetAndDot()
        {
            GameObject target = new GameObject("LaserTarget");
            targetTransform = target.transform;
            target.SetActive(false);

            dotObject = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            dotObject.name = "LaserDot";
            dotObject.transform.localScale = new Vector3(0.064f, 0.008f, 0.064f);
            Collider collider = dotObject.GetComponent<Collider>();
            if (collider != null)
            {
                Destroy(collider);
            }

            dotRenderer = dotObject.GetComponent<Renderer>();
            dotMaterial = new Material(Shader.Find("Universal Render Pipeline/Lit"));
            dotMaterial.color = new Color(1f, 0.025f, 0.01f, 1f);
            dotMaterial.EnableKeyword("_EMISSION");
            dotMaterial.SetColor("_EmissionColor", new Color(1f, 0.01f, 0f, 1f) * 2.5f);
            dotRenderer.sharedMaterial = dotMaterial;
            dotLight = dotObject.AddComponent<Light>();
            dotLight.type = LightType.Point;
            dotLight.color = new Color(1f, 0.04f, 0.015f);
            dotLight.range = 0.48f;
            dotLight.intensity = 1.15f;
            dotLight.shadows = LightShadows.None;
            dotObject.SetActive(false);
        }

        private void ProcessTouchInput()
        {
            Touchscreen touchscreen = Touchscreen.current;
            if (touchscreen == null)
            {
                return;
            }

            int pressedCount = 0;
            foreach (TouchControl touch in touchscreen.touches)
            {
                if (touch.press.isPressed && (roomCamera == null || roomCamera.IsEligibleWorldTouch(touch.touchId.ReadValue()))) pressedCount++;
            }

            if (pressedCount >= 2 && touchId >= 0)
            {
                CancelPointer();
                return;
            }

            foreach (TouchControl touch in touchscreen.touches)
            {
                int id = touch.touchId.ReadValue();
                UnityEngine.InputSystem.TouchPhase phase = touch.phase.ReadValue();
                if (phase == UnityEngine.InputSystem.TouchPhase.Began && touchId < 0 && !mouseCaptured && pressedCount == 1)
                {
                    Vector2 position = touch.position.ReadValue();
                    if (!IsPointerOverUi(id) && TryBeginLaserPointer(id, position))
                    {
                        if (!activationPointer) ProcessTargetPosition(position);
                    }
                }
                else if (touchId == id && touch.press.isPressed && !activationPointer)
                {
                    ProcessTargetPosition(touch.position.ReadValue());
                }

                if (touchId == id && (phase == UnityEngine.InputSystem.TouchPhase.Ended ||
                    phase == UnityEngine.InputSystem.TouchPhase.Canceled || !touch.press.isPressed))
                {
                    if (phase == UnityEngine.InputSystem.TouchPhase.Canceled) CancelPointer();
                    else EndPointer();
                }
            }
        }

        private void ProcessMouseInput()
        {
            Mouse mouse = Mouse.current;
            if (mouse == null || touchId >= 0)
            {
                return;
            }

            bool startedFromToy = false;
            if (CurrentState == LaserState.Idle && mouse.leftButton.wasPressedThisFrame && !IsPointerOverUi(-1))
            {
                startedFromToy = TryBeginLaserPointer(-1, mouse.position.ReadValue());
            }

            if (CurrentState == LaserState.Aiming && !startedFromToy && mouse.leftButton.wasPressedThisFrame && !IsPointerOverUi(-1))
            {
                if (TryBeginLaserPointer(-1, mouse.position.ReadValue()))
                {
                    ProcessTargetPosition(mouse.position.ReadValue());
                }
            }

            if (mouseCaptured && mouse.leftButton.isPressed && !activationPointer)
            {
                ProcessTargetPosition(mouse.position.ReadValue());
            }

            if (mouseCaptured && mouse.leftButton.wasReleasedThisFrame)
            {
                EndPointer();
            }
        }

        private bool TryBeginLaserPointer(int pointer, Vector2 position)
        {
            if (CurrentState == LaserState.Idle)
            {
                if (!TryStartFromToy(position)) return false;
                activationPointer = true;
            }

            if (CurrentState != LaserState.Aiming || IsPointerCaptured || inputCamera == null)
            {
                return false;
            }

            if (!activationPointer && !TryResolveTarget(inputCamera.ScreenPointToRay(position), out _))
            {
                rejectedTargetCount++;
                return false;
            }

            if (pointer < 0)
            {
                mouseCaptured = true;
                roomCamera?.ReserveMousePointer();
            }
            else
            {
                touchId = pointer;
                roomCamera?.ReserveTouchPointer(pointer);
            }

            targetCandidate = false;
            if (roomCamera != null)
            {
                gestureStartYaw = roomCamera.CurrentYaw;
                gestureStartPitch = roomCamera.CurrentPitch;
                gestureStartDistance = roomCamera.CurrentDistance;
            }
            return true;
        }

        private bool TryStartFromToy(Vector2 position)
        {
            if (!IsPlayAllowed || CurrentState != LaserState.Idle || inputCamera == null)
            {
                return false;
            }

            Ray ray = inputCamera.ScreenPointToRay(position);
            if (!Physics.Raycast(ray, out RaycastHit hit, PointerRayDistance, ~0, QueryTriggerInteraction.Collide) ||
                !IsToyCollider(hit.collider))
            {
                return false;
            }

            if (!motor.PrepareForPlayerAction()) return false;

            motor.SetHiddenActivityLock(true);
            CurrentState = LaserState.Aiming;
            validationAppliesEnergy = true;
            sessionCount++;
            acceptedTargetCount = 0;
            rejectedTargetCount = 0;
            successfulCatchCount = 0;
            summaryLogged = false;
            SetDotVisible(false);
            roomCamera?.FocusOnActivity(motor.transform, 4.6f, -14f, 25f);
            return true;
        }

        private void ProcessTargetPosition(Vector2 position)
        {
            if (CurrentState != LaserState.Aiming || inputCamera == null)
            {
                return;
            }

            Ray ray = inputCamera.ScreenPointToRay(position);
            targetCandidate = TryResolveTarget(ray, out candidatePoint);
            if (targetCandidate)
            {
                targetTransform.position = candidatePoint;
                SetDotVisible(true);
            }
            else
            {
                SetDotVisible(false);
            }
        }

        private bool TryResolveTarget(Ray ray, out Vector3 point)
        {
            point = Vector3.zero;
            if (!Physics.Raycast(ray, out RaycastHit hit, PointerRayDistance, ~0, QueryTriggerInteraction.Ignore) ||
                hit.collider != floorCollider || !InsideOpenPlayZone(hit.point) || IsCatCollider(hit.collider))
            {
                return false;
            }

            if (!NavMesh.SamplePosition(hit.point, out NavMeshHit navHit, 0.3f, NavMesh.AllAreas) ||
                !InsideOpenPlayZone(navHit.position) || Vector3.Distance(motor.transform.position, navHit.position) < MinimumTargetDistance)
            {
                return false;
            }

            NavMeshPath path = new NavMeshPath();
            if (!NavMesh.CalculatePath(motor.transform.position, navHit.position, NavMesh.AllAreas, path) ||
                path.status != NavMeshPathStatus.PathComplete)
            {
                return false;
            }

            point = navHit.position + Vector3.up * 0.018f;
            return true;
        }

        private void EndPointer()
        {
            if (!IsPointerCaptured)
            {
                return;
            }

            if (activationPointer)
            {
                CancelPointer();
                return;
            }

            bool accepted = targetCandidate;
            if (accepted)
            {
                targetTransform.position = candidatePoint;
                acceptedTargetCount++;
                CurrentState = LaserState.Chasing;
                SetDotVisible(true);
                if (!motor.TryMoveToLockedActivity(targetTransform, "Laser target"))
                {
                    acceptedTargetCount--;
                    rejectedTargetCount++;
                    CurrentState = LaserState.Aiming;
                    SetDotVisible(false);
                }
            }
            else
            {
                rejectedTargetCount++;
                SetDotVisible(false);
            }

            ReleasePointer();
            targetCandidate = false;
        }

        private void CancelPointer()
        {
            targetCandidate = false;
            SetDotVisible(false);
            ReleasePointer();
        }

        private void OnApplicationFocus(bool hasFocus)
        {
            if (!hasFocus) CancelPointer();
        }

        private void OnApplicationPause(bool paused)
        {
            if (paused) CancelPointer();
        }

        private void ReleasePointer()
        {
            if (mouseCaptured)
            {
                roomCamera?.ReleaseMousePointer();
            }
            else if (touchId >= 0)
            {
                roomCamera?.ReleaseTouchPointer(touchId);
            }
            mouseCaptured = false;
            touchId = -1;
            activationPointer = false;
        }

        private IEnumerator PerformCatch()
        {
            if (CurrentState != LaserState.Chasing)
            {
                yield break;
            }

            CurrentState = LaserState.Catch;
            successfulCatchCount++;
            SetDotVisible(false);
            RequestHaptic();
            catModelRoot.localRotation = neutralModelRotation * Quaternion.Euler(2f, 0f, 0f);
            catModelRoot.localScale = neutralModelScale * 1.01f;
            catchElapsed = 0f;
            while (catchElapsed < CatchSeconds)
            {
                catchElapsed += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(catchElapsed / CatchSeconds);
                float pulse = Mathf.Sin(t * Mathf.PI);
                catModelRoot.localRotation = neutralModelRotation * Quaternion.Euler(2f * pulse, 0f, 0f);
                catModelRoot.localScale = neutralModelScale * (1f + 0.01f * pulse);
                yield return null;
            }

            catModelRoot.localRotation = neutralModelRotation;
            catModelRoot.localScale = neutralModelScale;
            maximumRotationRestoreError = Mathf.Max(maximumRotationRestoreError,
                Quaternion.Angle(catModelRoot.localRotation, neutralModelRotation));
            maximumScaleRestoreError = Mathf.Max(maximumScaleRestoreError,
                Vector3.Distance(catModelRoot.localScale, neutralModelScale));

            if (energy != null && validationAppliesEnergy)
            {
                energy.ApplySuccessfulCatch();
            }

            if (successfulCatchCount >= CatchLimit || !IsPlayAllowed)
            {
                FinishSession();
            }
            else
            {
                CurrentState = LaserState.Aiming;
            }
        }

        private void FinishSession()
        {
            SetDotVisible(false);
            ReleasePointer();
            motor.SetHiddenActivityLock(false);
            CurrentState = LaserState.Idle;
            energy?.NotifyLaserSessionFinished();
            if (!summaryLogged)
            {
                summaryLogged = true;
                bool pass = successfulCatchCount == CatchLimit && LaserDotInstanceCount == 1 &&
                    maximumProtectedLocalPositionDrift <= 0.02f && maximumRotationRestoreError <= 0.001f &&
                    maximumScaleRestoreError <= 0.001f;
                Debug.Log($"[CatMe][Laser] validation summary: {(pass ? "PASS" : "FAIL")} | " +
                    $"session={sessionCount} | accepted={acceptedTargetCount} | rejected={rejectedTargetCount} | " +
                    $"catches={successfulCatchCount} | dotInstances={LaserDotInstanceCount} | " +
                    $"cameraDelta={maximumCameraDelta:0.###} | protectedLocalDrift={maximumProtectedLocalPositionDrift:0.####}m | " +
                    $"rotationRestore={maximumRotationRestoreError:0.####}deg | scaleRestore={maximumScaleRestoreError:0.####} | " +
                    $"haptics={hapticRequestCount}");
            }
        }

        private void RequestHaptic()
        {
#if UNITY_IOS || UNITY_ANDROID
            Handheld.Vibrate();
            hapticRequestCount++;
#endif
        }

        private void SetDotVisible(bool visible)
        {
            if (dotObject != null)
            {
                dotObject.transform.position = targetTransform == null ? dotObject.transform.position : targetTransform.position;
                dotObject.SetActive(visible);
                if (dotLight != null) dotLight.enabled = visible;
            }
        }

        private bool InsideOpenPlayZone(Vector3 position)
        {
            Vector3 center = openPlayZone.position;
            Vector3 scale = openPlayZone.lossyScale;
            return Mathf.Abs(position.x - center.x) <= Mathf.Abs(scale.x) * 0.5f &&
                Mathf.Abs(position.z - center.z) <= Mathf.Abs(scale.z) * 0.5f;
        }

        private bool IsToyCollider(Collider collider)
        {
            return collider == toyTrigger || (collider != null && collider.transform.IsChildOf(toy));
        }

        private bool IsCatCollider(Collider collider)
        {
            return collider != null && collider.transform.IsChildOf(motor.transform);
        }

        private void TrackProtectedPosition()
        {
            if (catModelRoot != null)
            {
                maximumProtectedLocalPositionDrift = Mathf.Max(maximumProtectedLocalPositionDrift,
                    catModelRoot.localPosition.magnitude);
            }
        }

        private void TrackCameraDelta()
        {
            if (!IsPointerCaptured || roomCamera == null)
            {
                return;
            }

            maximumCameraDelta = Mathf.Max(maximumCameraDelta,
                Mathf.Max(Mathf.Abs(roomCamera.CurrentYaw - gestureStartYaw),
                    Mathf.Max(Mathf.Abs(roomCamera.CurrentPitch - gestureStartPitch),
                        Mathf.Abs(roomCamera.CurrentDistance - gestureStartDistance))));
        }

        private void OnDestroy()
        {
            ReleasePointer();
            if (motor != null && CurrentState != LaserState.Idle && motor.IsActivityLocked)
            {
                motor.SetHiddenActivityLock(false);
            }
            if (dotMaterial != null)
            {
                Destroy(dotMaterial);
            }
        }

        private static Transform FindTransform(string path)
        {
            GameObject found = GameObject.Find(path);
            return found == null ? null : found.transform;
        }

        private static bool IsPointerOverUi(int pointerId)
        {
            return EventSystem.current != null && EventSystem.current.IsPointerOverGameObject(pointerId);
        }
    }
}
