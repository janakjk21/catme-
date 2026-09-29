using System.Collections;
using CatMe.CameraSystem;
using CatMe.Cat;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;
using UnityEngine.UI;

namespace CatMe.Toys
{
    /// <summary>
    /// A single reusable physics ball. The Rigidbody owns ball motion and
    /// CatMotor owns the cat's pursuit and approach.
    /// </summary>
    [DefaultExecutionOrder(-108)]
    [DisallowMultipleComponent]
    public sealed class CatBallInteraction : MonoBehaviour
    {
        private const float BallRadius = 0.075f;
        private const int MaximumBats = 3;

        private CatMotor motor;
        private CatCompanionJournal journal;
        private RoomOrbitCamera roomCamera;
        private Camera inputCamera;
        private Transform openPlayZone;
        private Transform ball;
        private Transform pursuitTarget;
        private Transform cameraFocus;
        private GameObject runtimeRoot;
        private Rigidbody body;
        private Collider ballCollider;
        private LineRenderer preview;
        private Material ballMaterial;
        private Material previewMaterial;
        private Material trailMaterial;
        private PhysicsMaterial ballPhysicsMaterial;
        private PhysicsMaterial rugPhysicsMaterial;
        private Collider rugCollider;
        private float currentGroundResistance;
        private CatBallImpactAudio impactAudio;
        private Button playButton;
        private Button recallButton;
        private Text playLabel;
        private Canvas uiCanvas;
        private RectTransform uiParent;
        private RectTransform catTrackerRoot;
        private Text catTrackerArrow;
        private Text catTrackerLabel;
        private Text playHintLabel;
        private int touchId = -1;
        private bool mouseCaptured;
        private Vector2 dragStart;
        private Vector2 dragCurrent;
        private float pursuitStartedAt;
        private float nextRepathAt;
        private float nextBatAt;
        private float quietSince;
        private float noticeUntil;
        private int batCount;
        private int successfulThrows;
        private int failedPaths;
        private bool initialized;

        public enum BallState { Idle, Aiming, Noticing, Chasing, Watching }
        public BallState CurrentState { get; private set; } = BallState.Idle;
        public int ThrowCount => successfulThrows;
        public int BatCount => batCount;
        public int FailedPathCount => failedPaths;
        public bool HasSingleBall => ball != null && body != null;
        public bool IsActivityLockActive => motor != null && motor.IsActivityLocked;

        public void Initialize(CatMotor readyMotor)
        {
            if (initialized || readyMotor == null) return;
            motor = readyMotor;
            journal = readyMotor.GetComponent<CatCompanionJournal>();
            roomCamera = FindAnyObjectByType<RoomOrbitCamera>();
            inputCamera = Camera.main;
            openPlayZone = FindTransform("Room_Blockout/Zones/OpenPlayZone");
            if (openPlayZone == null || inputCamera == null)
            {
                Debug.LogError("[CatMe][Ball] OpenPlayZone or Main Camera is missing; ball play is unavailable.");
                return;
            }

            ConfigureWovenRugCollision();
            CreateBall();
            CreatePursuitTarget();
            CreatePreview();
            CreateControls();
            initialized = true;
        }

        private void Update()
        {
            if (!initialized) return;
            UpdateControls();
            if (CurrentState == BallState.Aiming)
            {
                ProcessTouchInput();
                ProcessMouseInput();
                UpdatePreview();
            }
            else if (CurrentState == BallState.Noticing || CurrentState == BallState.Chasing || CurrentState == BallState.Watching)
            {
                UpdateCatPlay();
            }
            if (CurrentState == BallState.Aiming && preview != null && preview.enabled && roomCamera != null && roomCamera.IsSeatedView)
                PositionBallInPlayerView();
            UpdateFocusPoint();
            UpdateCatTracker();
        }

        private void FixedUpdate()
        {
            if (!initialized || body == null || body.isKinematic) return;
            bool grounded = TryGetFloor(out RaycastHit floorHit);
            bool onRug = grounded && rugCollider != null && floorHit.collider == rugCollider;
            float targetResistance = !grounded ? 0.08f : onRug ? 0.64f : 0.13f;
            float delta = Time.fixedDeltaTime;
            currentGroundResistance = Mathf.MoveTowards(currentGroundResistance, targetResistance,
                Mathf.Max(0.01f, Mathf.Abs(targetResistance - currentGroundResistance) * delta / 0.10f));
            body.linearDamping = grounded ? (onRug ? 0.42f : 0.12f) : 0.08f;
            body.angularDamping = grounded ? (onRug ? 0.60f : 0.20f) : 0.12f;
            if (grounded)
            {
                Vector3 planarVelocity = Vector3.ProjectOnPlane(body.linearVelocity, Vector3.up);
                if (planarVelocity.sqrMagnitude > 0.0004f)
                    body.AddForce(-planarVelocity.normalized * currentGroundResistance, ForceMode.Acceleration);
            }
            impactAudio?.SetOnWovenSurface(onRug);
            ClampBallToPlayArea();
        }

        private void ConfigureWovenRugCollision()
        {
            Transform rug = GameObject.Find("WovenOvalRug")?.transform;
            if (rug == null) return;
            rugCollider = rug.GetComponent<Collider>();
            if (rugCollider == null)
            {
                MeshFilter meshFilter = rug.GetComponent<MeshFilter>();
                if (meshFilter == null || meshFilter.sharedMesh == null) return;
                MeshCollider meshCollider = rug.gameObject.AddComponent<MeshCollider>();
                meshCollider.sharedMesh = meshFilter.sharedMesh;
                rugCollider = meshCollider;
            }
            rugPhysicsMaterial = new PhysicsMaterial("SoftWovenRug")
            {
                dynamicFriction = 0.72f,
                staticFriction = 0.86f,
                bounciness = 0.02f,
                frictionCombine = PhysicsMaterialCombine.Maximum,
                bounceCombine = PhysicsMaterialCombine.Minimum
            };
            rugCollider.sharedMaterial = rugPhysicsMaterial;
        }

        private bool TryGetFloor(out RaycastHit hit)
        {
            Vector3 origin = ball.position + Vector3.up * (BallRadius + 0.02f);
            float distance = BallRadius + 0.10f;
            if (Physics.Raycast(origin, Vector3.down, out hit, distance, ~0, QueryTriggerInteraction.Ignore) &&
                hit.collider != ballCollider)
                return true;
            return false;
        }

        private Vector3 PlaceOnRoomSurface(Vector3 position, float fallbackY)
        {
            bool colliderWasEnabled = ballCollider != null && ballCollider.enabled;
            if (ballCollider != null) ballCollider.enabled = false;
            Vector3 origin = new Vector3(position.x, 3f, position.z);
            if (Physics.Raycast(origin, Vector3.down, out RaycastHit hit, 6f, ~0, QueryTriggerInteraction.Ignore) &&
                (hit.collider == rugCollider || hit.collider.name.IndexOf("floor", System.StringComparison.OrdinalIgnoreCase) >= 0))
                position.y = hit.point.y + BallRadius + 0.003f;
            else
                position.y = fallbackY;
            if (ballCollider != null) ballCollider.enabled = colliderWasEnabled;
            return position;
        }

        private void CreateBall()
        {
            GameObject ballObject = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            ballObject.transform.SetParent(CreateRuntimeRoot(), false);
            ballObject.name = "ReusableCatPlayBall";
            ball = ballObject.transform;
            Vector3 forward = Vector3.ProjectOnPlane(inputCamera.transform.forward, Vector3.up).normalized;
            if (forward.sqrMagnitude < 0.01f) forward = Vector3.forward;
            ball.position = ClampToZone(motor.transform.position + forward * 0.62f, BallRadius + 0.025f);
            ball.position = new Vector3(ball.position.x, BallRadius + 0.005f, ball.position.z);
            ball.localScale = Vector3.one * (BallRadius * 2f);
            ballCollider = ballObject.GetComponent<SphereCollider>();
            ball.position = PlaceOnRoomSurface(ball.position, BallRadius + 0.005f);
            impactAudio = ballObject.AddComponent<CatBallImpactAudio>();
            impactAudio.Initialize();
            ballPhysicsMaterial = new PhysicsMaterial("WovenBallFriction")
            {
                dynamicFriction = 0.42f,
                staticFriction = 0.52f,
                bounciness = 0.10f,
                frictionCombine = PhysicsMaterialCombine.Average,
                bounceCombine = PhysicsMaterialCombine.Minimum
            };
            ballCollider.sharedMaterial = ballPhysicsMaterial;
            body = ballObject.AddComponent<Rigidbody>();
            body.mass = 0.06f;
            body.linearDamping = 0.08f;
            body.angularDamping = 0.12f;
            body.interpolation = RigidbodyInterpolation.Interpolate;
            body.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
            body.sleepThreshold = 0.035f;
            body.isKinematic = true;
            ballMaterial = new Material(Shader.Find("Universal Render Pipeline/Lit"));
            ballMaterial.name = "WovenBall_RuntimeMaterial";
            ballMaterial.color = new Color(0.72f, 0.43f, 0.25f, 1f);
            ballMaterial.SetFloat("_Smoothness", 0.08f);
            Texture2D wovenTexture = Resources.Load<Texture2D>("CatMeArt/WovenRug");
            if (wovenTexture != null)
            {
                ballMaterial.SetTexture("_BaseMap", wovenTexture);
                ballMaterial.SetTextureScale("_BaseMap", new Vector2(3f, 3f));
                ballMaterial.SetTexture("_MainTex", wovenTexture);
                ballMaterial.SetTextureScale("_MainTex", new Vector2(3f, 3f));
            }
            ballObject.GetComponent<Renderer>().sharedMaterial = ballMaterial;

            TrailRenderer trail = ballObject.AddComponent<TrailRenderer>();
            trail.time = 0f;
            trail.startWidth = 0f;
            trail.endWidth = 0f;
            trailMaterial = new Material(Shader.Find("Universal Render Pipeline/Unlit"));
            trail.material = trailMaterial;
            trail.startColor = new Color(1f, 0.60f, 0.38f, 0.34f);
            trail.endColor = new Color(1f, 0.60f, 0.38f, 0f);
            trail.minVertexDistance = 0.025f;
        }

        private void CreatePursuitTarget()
        {
            GameObject target = new GameObject("BallPursuitTarget");
            target.transform.SetParent(CreateRuntimeRoot(), false);
            pursuitTarget = target.transform;
            target.transform.position = motor.transform.position;
            cameraFocus = new GameObject("BallPlayCameraFocus").transform;
            cameraFocus.SetParent(runtimeRoot.transform, false);
            cameraFocus.position = motor.transform.position;
        }

        private void CreatePreview()
        {
            GameObject previewObject = new GameObject("BallThrowPreview");
            previewObject.transform.SetParent(CreateRuntimeRoot(), false);
            preview = previewObject.AddComponent<LineRenderer>();
            preview.useWorldSpace = true;
            preview.positionCount = 14;
            preview.startWidth = 0.022f;
            preview.endWidth = 0.008f;
            preview.numCapVertices = 2;
            previewMaterial = new Material(Shader.Find("Universal Render Pipeline/Unlit"));
            previewMaterial.color = new Color(1f, 0.83f, 0.60f, 0.9f);
            preview.sharedMaterial = previewMaterial;
            preview.startColor = new Color(1f, 0.83f, 0.60f, 0.95f);
            preview.endColor = new Color(1f, 0.83f, 0.60f, 0.08f);
            preview.enabled = false;
        }

        private Transform CreateRuntimeRoot()
        {
            if (runtimeRoot == null)
            {
                runtimeRoot = new GameObject("CatBallRuntime");
                runtimeRoot.transform.SetPositionAndRotation(Vector3.zero, Quaternion.identity);
            }
            return runtimeRoot.transform;
        }

        private void CreateControls()
        {
            Canvas canvas = FindFirstObjectByType<Canvas>();
            if (canvas == null) return;
            Transform parent = canvas.transform.Find("CallSafeArea") ?? canvas.transform;
            uiCanvas = canvas;
            uiParent = parent.GetComponent<RectTransform>();
            playButton = CreateButton(parent, "BallPlayButton", "Ball", new Vector2(0f, 286f), new Color(0.68f, 0.25f, 0.13f, 0.96f), out playLabel);
            recallButton = CreateButton(parent, "BallRecallButton", "Recall", new Vector2(210f, 286f), new Color(0.30f, 0.29f, 0.25f, 0.96f), out _);
            playButton.onClick.AddListener(OnPlayPressed);
            recallButton.onClick.AddListener(RecallBall);
            CreateFirstPersonFeedback(parent);
        }

        private void CreateFirstPersonFeedback(Transform parent)
        {
            GameObject hint = new GameObject("BallPlayHint", typeof(RectTransform), typeof(Text));
            hint.transform.SetParent(parent, false);
            RectTransform hintRect = hint.GetComponent<RectTransform>();
            hintRect.anchorMin = new Vector2(0.5f, 0f);
            hintRect.anchorMax = new Vector2(0.5f, 0f);
            hintRect.pivot = new Vector2(0.5f, 0f);
            hintRect.anchoredPosition = new Vector2(0f, 374f);
            hintRect.sizeDelta = new Vector2(450f, 58f);
            playHintLabel = hint.GetComponent<Text>();
            playHintLabel.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            playHintLabel.fontSize = 22;
            playHintLabel.alignment = TextAnchor.MiddleCenter;
            playHintLabel.color = new Color(0.20f, 0.18f, 0.15f, 0.95f);
            playHintLabel.raycastTarget = false;
            playHintLabel.enabled = false;

            GameObject tracker = new GameObject("CatDirectionCue", typeof(RectTransform), typeof(Image), typeof(CanvasGroup));
            tracker.transform.SetParent(parent, false);
            catTrackerRoot = tracker.GetComponent<RectTransform>();
            catTrackerRoot.sizeDelta = new Vector2(82f, 82f);
            catTrackerRoot.anchorMin = new Vector2(0.5f, 0.5f);
            catTrackerRoot.anchorMax = new Vector2(0.5f, 0.5f);
            catTrackerRoot.pivot = new Vector2(0.5f, 0.5f);
            Image trackerImage = tracker.GetComponent<Image>();
            trackerImage.color = new Color(0.13f, 0.17f, 0.18f, 0.82f);
            trackerImage.raycastTarget = false;
            tracker.GetComponent<CanvasGroup>().blocksRaycasts = false;

            GameObject arrow = new GameObject("Arrow", typeof(RectTransform), typeof(Text));
            arrow.transform.SetParent(tracker.transform, false);
            RectTransform arrowRect = arrow.GetComponent<RectTransform>();
            arrowRect.anchorMin = Vector2.zero;
            arrowRect.anchorMax = Vector2.one;
            arrowRect.offsetMin = new Vector2(0f, 14f);
            arrowRect.offsetMax = Vector2.zero;
            catTrackerArrow = arrow.GetComponent<Text>();
            catTrackerArrow.text = "▲";
            catTrackerArrow.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            catTrackerArrow.fontSize = 38;
            catTrackerArrow.alignment = TextAnchor.MiddleCenter;
            catTrackerArrow.color = new Color(1f, 0.89f, 0.67f, 1f);
            catTrackerArrow.raycastTarget = false;

            GameObject label = new GameObject("Label", typeof(RectTransform), typeof(Text));
            label.transform.SetParent(tracker.transform, false);
            RectTransform labelRect = label.GetComponent<RectTransform>();
            labelRect.anchorMin = Vector2.zero;
            labelRect.anchorMax = Vector2.one;
            labelRect.offsetMin = new Vector2(0f, 1f);
            labelRect.offsetMax = new Vector2(0f, -38f);
            catTrackerLabel = label.GetComponent<Text>();
            catTrackerLabel.text = "CAT";
            catTrackerLabel.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            catTrackerLabel.fontSize = 14;
            catTrackerLabel.fontStyle = FontStyle.Bold;
            catTrackerLabel.alignment = TextAnchor.MiddleCenter;
            catTrackerLabel.color = Color.white;
            catTrackerLabel.raycastTarget = false;
            catTrackerRoot.gameObject.SetActive(false);
        }

        private static Button CreateButton(Transform parent, string name, string title, Vector2 position, Color color, out Text label)
        {
            GameObject buttonObject = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button));
            buttonObject.transform.SetParent(parent, false);
            RectTransform rect = buttonObject.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 0f);
            rect.anchorMax = new Vector2(0.5f, 0f);
            rect.pivot = new Vector2(0.5f, 0f);
            rect.anchoredPosition = position;
            rect.sizeDelta = new Vector2(176f, 82f);
            Image image = buttonObject.GetComponent<Image>();
            image.color = color;
            Button button = buttonObject.GetComponent<Button>();
            button.targetGraphic = image;
            GameObject labelObject = new GameObject("Label", typeof(RectTransform), typeof(Text));
            labelObject.transform.SetParent(buttonObject.transform, false);
            RectTransform labelRect = labelObject.GetComponent<RectTransform>();
            labelRect.anchorMin = Vector2.zero; labelRect.anchorMax = Vector2.one;
            labelRect.offsetMin = Vector2.zero; labelRect.offsetMax = Vector2.zero;
            label = labelObject.GetComponent<Text>();
            label.text = title;
            label.alignment = TextAnchor.MiddleCenter;
            label.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            label.fontSize = 26;
            label.fontStyle = FontStyle.Bold;
            label.color = Color.white;
            return button;
        }

        private void UpdateControls()
        {
            if (playButton == null)
            {
                CreateControls();
                return;
            }
            bool ready = CurrentState == BallState.Idle && motor.CanStartPlayerAction;
            playButton.interactable = ready || CurrentState == BallState.Aiming;
            recallButton.interactable = CurrentState == BallState.Idle && !motor.IsActivityLocked;
            if (playLabel != null) playLabel.text = CurrentState == BallState.Aiming ? "Cancel" : "Ball";
            if (playHintLabel != null)
            {
                bool firstPerson = roomCamera != null && roomCamera.IsSeatedView;
                playHintLabel.enabled = firstPerson && CurrentState == BallState.Aiming && preview != null && preview.enabled;
                if (playHintLabel.enabled) playHintLabel.text = "Drag to choose a direction · release to throw";
            }
        }

        private void OnPlayPressed()
        {
            if (CurrentState == BallState.Aiming)
            {
                EndAiming(false);
                return;
            }
            TryBeginAiming();
        }

        private bool TryBeginAiming()
        {
            if (CurrentState != BallState.Idle || !motor.PrepareForPlayerAction()) return false;
            motor.SetHiddenActivityLock(true);
            body.isKinematic = true;
            body.angularVelocity = Vector3.zero;
            body.linearVelocity = Vector3.zero;
            if (roomCamera != null && roomCamera.IsSeatedView)
                PositionBallInPlayerView();
            else
                ball.position = ClampToZone(ball.position, BallRadius + 0.025f);
            CurrentState = BallState.Aiming;
            preview.enabled = true;
            roomCamera?.FocusOnActivity(cameraFocus, 4.8f, -8f, 25f);
            Debug.Log("[CatMe][Ball] Drag in the room to preview a throw; release to toss the ball.");
            return true;
        }

        /// <summary>Starts the existing chase loop when the player taps the mouse toy on the rug.</summary>
        public bool TryPlayWithFloorToy(Transform floorToy)
        {
            if (!initialized || floorToy == null || CurrentState != BallState.Idle || !motor.PrepareForPlayerAction())
                return false;

            motor.SetHiddenActivityLock(true);
            body.isKinematic = true;
            body.linearVelocity = Vector3.zero;
            body.angularVelocity = Vector3.zero;
            ball.position = ClampToZone(floorToy.position, BallRadius + 0.04f);
            ball.position = PlaceOnRoomSurface(ball.position, BallRadius + 0.005f);
            ball.rotation = Quaternion.identity;
            ball.GetComponent<TrailRenderer>().Clear();
            CurrentState = BallState.Aiming;
            preview.enabled = false;

            Vector3 awayFromCat = Vector3.ProjectOnPlane(ball.position - motor.transform.position, Vector3.up);
            if (awayFromCat.sqrMagnitude < 0.04f)
                awayFromCat = Vector3.ProjectOnPlane(inputCamera.transform.forward, Vector3.up);
            if (awayFromCat.sqrMagnitude < 0.01f) awayFromCat = Vector3.forward;
            awayFromCat = (awayFromCat.normalized + Vector3.Cross(Vector3.up, awayFromCat.normalized) * Random.Range(-0.45f, 0.45f)).normalized;

            roomCamera?.FocusOnActivity(cameraFocus, 4.7f, -8f, 25f);
            Debug.Log("[CatMe][Ball] Floor mouse tapped; rolling the ball for the cat to chase.");
            return LaunchBall(awayFromCat * 2.15f + Vector3.up * 0.20f);
        }

#if UNITY_EDITOR
        public bool ThrowForValidation(Vector3 direction, float speed)
        {
            if (!initialized || CurrentState != BallState.Idle || !motor.PrepareForPlayerAction() || direction.sqrMagnitude < 0.001f)
            {
                return false;
            }
            motor.SetHiddenActivityLock(true);
            body.isKinematic = true;
            CurrentState = BallState.Aiming;
            Vector3 horizontal = Vector3.ProjectOnPlane(direction, Vector3.up).normalized;
            Vector3 launchVelocity = horizontal * Mathf.Clamp(speed, 1f, 4f) + Vector3.up * 0.75f;
            return LaunchBall(launchVelocity);
        }
#endif

        private void ProcessTouchInput()
        {
            Touchscreen screen = Touchscreen.current;
            if (screen == null) return;
            int pressedCount = 0;
            foreach (TouchControl touch in screen.touches)
                if (touch.press.isPressed && (roomCamera == null || roomCamera.IsEligibleWorldTouch(touch.touchId.ReadValue()))) pressedCount++;
            if (pressedCount >= 2 && touchId >= 0)
            {
                ReleasePointer();
                preview.enabled = false;
                EndAiming(false);
                return;
            }
            foreach (TouchControl touch in screen.touches)
            {
                int id = touch.touchId.ReadValue();
                UnityEngine.InputSystem.TouchPhase phase = touch.phase.ReadValue();
                if (phase == UnityEngine.InputSystem.TouchPhase.Began && touchId < 0 && !mouseCaptured && pressedCount == 1 && !IsPointerOverUi(id))
                {
                    touchId = id;
                    dragStart = dragCurrent = touch.position.ReadValue();
                    roomCamera?.ReserveTouchPointer(id);
                }
                else if (touchId == id && touch.press.isPressed)
                {
                    dragCurrent = touch.position.ReadValue();
                }
                if (touchId == id && (phase == UnityEngine.InputSystem.TouchPhase.Ended || phase == UnityEngine.InputSystem.TouchPhase.Canceled || !touch.press.isPressed))
                {
                    bool throwBall = phase != UnityEngine.InputSystem.TouchPhase.Canceled;
                    ReleasePointer();
                    if (throwBall) ThrowBall(); else EndAiming(false);
                }
            }
        }

        private void ProcessMouseInput()
        {
            Mouse mouse = Mouse.current;
            if (mouse == null || touchId >= 0) return;
            if (mouse.leftButton.wasPressedThisFrame && !IsPointerOverUi(-1))
            {
                mouseCaptured = true;
                dragStart = dragCurrent = mouse.position.ReadValue();
                roomCamera?.ReserveMousePointer();
            }
            if (mouseCaptured && mouse.leftButton.isPressed) dragCurrent = mouse.position.ReadValue();
            if (mouseCaptured && mouse.leftButton.wasReleasedThisFrame)
            {
                ReleasePointer();
                ThrowBall();
            }
        }

        private void UpdatePreview()
        {
            if (preview == null || !preview.enabled) return;
            Vector3 velocity = CalculateThrowVelocity();
            Vector3 start = ball.position + Vector3.up * BallRadius;
            const float step = 0.12f;
            for (int index = 0; index < preview.positionCount; index++)
            {
                float time = step * index;
                Vector3 point = start + velocity * time + Physics.gravity * (0.5f * time * time);
                point = ClampToZone(point, BallRadius + 0.015f);
                preview.SetPosition(index, point);
            }
        }

        private Vector3 CalculateThrowVelocity()
        {
            Vector2 delta = dragCurrent - dragStart;
            Vector3 right = Vector3.ProjectOnPlane(inputCamera.transform.right, Vector3.up).normalized;
            Vector3 forward = Vector3.ProjectOnPlane(inputCamera.transform.forward, Vector3.up).normalized;
            if (forward.sqrMagnitude < 0.01f) forward = Vector3.forward;
            Vector3 horizontal = right * delta.x + forward * -delta.y;
            if (horizontal.sqrMagnitude < 100f) horizontal = forward;
            horizontal.Normalize();
            float speed = Mathf.Clamp(delta.magnitude * 0.0065f, 1.1f, 4.0f);
            float loft = Mathf.Clamp(0.10f + speed * 0.045f, 0.14f, 0.28f);
            return horizontal * speed + Vector3.up * loft;
        }

        private void ThrowBall()
        {
            LaunchBall(CalculateThrowVelocity());
        }

        private bool LaunchBall(Vector3 launchVelocity)
        {
            preview.enabled = false;
            body.isKinematic = false;
            body.linearVelocity = Vector3.zero;
            body.angularVelocity = Vector3.zero;
            body.AddForce(launchVelocity, ForceMode.VelocityChange);
            successfulThrows++;
            batCount = 0;
            pursuitStartedAt = Time.time;
            nextRepathAt = 0f;
            quietSince = -1f;
            CurrentState = BallState.Noticing;
            noticeUntil = Time.time + Random.Range(0.15f, 0.25f);
            cameraFocus.position = (motor.transform.position + ball.position) * 0.5f;
            roomCamera?.FocusOnActivity(cameraFocus, 4.7f, -8f, 25f);
            if (!SetPursuitTarget(true))
            {
                failedPaths++;
                EndAiming(false);
                Debug.LogWarning("[CatMe][Ball] Cat could not reach the ball; the ball remains playable.");
                return false;
            }
            Debug.Log($"[CatMe][Ball] Throw {successfulThrows} started | launchSpeed={body.linearVelocity.magnitude:0.##}m/s");
            return true;
        }

        private void UpdateCatPlay()
        {
            if (Time.time - pursuitStartedAt > 18f)
            {
                FinishPlay("pursuit timeout");
                return;
            }

            float speed = body.linearVelocity.magnitude;
            if (speed < 0.12f)
            {
                if (quietSince < 0f) quietSince = Time.time;
            }
            else quietSince = -1f;

            if (CurrentState == BallState.Noticing)
            {
                if (Time.time < noticeUntil) return;
                CurrentState = BallState.Chasing;
                if (!SetPursuitTarget(true) || !motor.TryMoveToLockedActivity(pursuitTarget, "Chase ball"))
                {
                    failedPaths++;
                    FinishPlay("ball position was unreachable");
                }
                return;
            }

            if (CurrentState == BallState.Watching)
            {
                if (Time.time >= nextBatAt)
                {
                    CurrentState = BallState.Chasing;
                    if (!SetPursuitTarget(true) || !motor.IsMoving && !motor.TryMoveToLockedActivity(pursuitTarget, "Play with ball"))
                    {
                        failedPaths++;
                        FinishPlay("cat could not resume pursuit");
                    }
                }
                return;
            }

            if (!motor.IsMoving && motor.HasArrived)
            {
                Vector3 arrivalOffset = ball.position - motor.transform.position;
                arrivalOffset.y = 0f;
                float forwardDot = Vector3.Dot(motor.transform.forward,
                    arrivalOffset.sqrMagnitude > 0.001f ? arrivalOffset.normalized : motor.transform.forward);
                if (arrivalOffset.magnitude <= 0.42f && forwardDot >= 0.72f &&
                    ball.position.y <= motor.transform.position.y + 0.22f && motor.HasFullyArrived && Time.time >= nextBatAt)
                {
                    BatBall(arrivalOffset);
                    return;
                }
                if (!motor.HasFullyArrived) return;
            }

            if (Time.time >= nextRepathAt)
            {
                nextRepathAt = Time.time + 0.22f;
                bool targetValid = SetPursuitTarget(false);
                if (!targetValid)
                {
                    failedPaths++;
                    if (failedPaths >= 3) FinishPlay("unreachable ball");
                    return;
                }
                if (motor.IsMoving) motor.UpdateLockedActivityDestination(pursuitTarget, 0.34f);
                else if (!motor.TryMoveToLockedActivity(pursuitTarget, "Continue ball chase"))
                {
                    failedPaths++;
                    if (failedPaths >= 3) FinishPlay("unreachable ball");
                }
            }

            if (!motor.IsMoving && motor.HasFullyArrived && quietSince >= 0f && Time.time - quietSince > 1.8f)
            {
                FinishPlay("cat investigated the stopped ball");
            }
        }

        private void BatBall(Vector3 towardBall)
        {
            if (batCount >= MaximumBats)
            {
                FinishPlay("play complete");
                return;
            }
            if (towardBall.sqrMagnitude < 0.001f) towardBall = motor.transform.forward;
            towardBall.Normalize();
            body.AddForce(towardBall * 1.5f + Vector3.up * 0.12f, ForceMode.VelocityChange);
            impactAudio?.PlayBat();
            batCount++;
            journal?.RecordDiscovery("first_successful_ball_play");
            nextBatAt = Time.time + 0.30f;
            CurrentState = BallState.Watching;
            quietSince = -1f;
            Debug.Log($"[CatMe][Ball] Cat batted the ball | bats={batCount}");
        }

        private bool SetPursuitTarget(bool force)
        {
            Vector3 towardCat = motor.transform.position - ball.position;
            towardCat.y = 0f;
            if (towardCat.sqrMagnitude < 0.01f) towardCat = -motor.transform.forward;
            towardCat.Normalize();
            Vector3 predictedBall = ball.position + Vector3.ClampMagnitude(body.linearVelocity * 0.22f, 0.48f);
            predictedBall.y = motor.transform.position.y;
            Vector3 approach = ClampToZone(predictedBall + towardCat * 0.34f, 0.20f);
            if (!NavMesh.SamplePosition(approach, out NavMeshHit sample, 0.8f, NavMesh.AllAreas)) return false;
            {
                NavMeshPath path = new NavMeshPath();
                if (!NavMesh.CalculatePath(motor.transform.position, sample.position, NavMesh.AllAreas, path) ||
                    path.status != NavMeshPathStatus.PathComplete)
                {
                    return false;
                }
                pursuitTarget.SetPositionAndRotation(sample.position, Quaternion.LookRotation(-towardCat, Vector3.up));
                if (force) nextRepathAt = 0f;
                return true;
            }
        }

        private void FinishPlay(string reason)
        {
            motor.CancelLockedActivityMovement();
            motor.SetHiddenActivityLock(false);
            // Let the dynamic ball finish rolling after the cat's play lock ends.
            CurrentState = BallState.Idle;
            preview.enabled = false;
            roomCamera?.ReturnToRoomView();
            Debug.Log($"[CatMe][Ball] Play finished | reason={reason} | bats={batCount}");
        }

        private void EndAiming(bool ignored)
        {
            ReleasePointer();
            if (preview != null) preview.enabled = false;
            if (body != null && body.linearVelocity.sqrMagnitude < 0.001f) body.isKinematic = true;
            if (motor != null)
            {
                motor.CancelLockedActivityMovement();
                motor.SetHiddenActivityLock(false);
            }
            CurrentState = BallState.Idle;
            roomCamera?.ReturnToRoomView();
        }

        private void RecallBall()
        {
            if (CurrentState != BallState.Idle || motor.IsActivityLocked) return;
            body.isKinematic = true;
            body.linearVelocity = Vector3.zero;
            body.angularVelocity = Vector3.zero;
            Vector3 forward = Vector3.ProjectOnPlane(inputCamera.transform.forward, Vector3.up).normalized;
            ball.position = ClampToZone(motor.transform.position + forward * 0.62f, BallRadius + 0.025f);
            ball.position = PlaceOnRoomSurface(ball.position, BallRadius + 0.005f);
            ball.rotation = Quaternion.identity;
            ball.GetComponent<TrailRenderer>().Clear();
        }

        private void ClampBallToPlayArea()
        {
            Vector3 position = ball.position;
            Vector3 min = ZoneMinimum(BallRadius + 0.015f);
            Vector3 max = ZoneMaximum(BallRadius + 0.015f);
            Vector3 velocity = body.linearVelocity;
            if (position.x < min.x) { position.x = min.x; velocity.x = Mathf.Abs(velocity.x) * 0.36f; }
            if (position.x > max.x) { position.x = max.x; velocity.x = -Mathf.Abs(velocity.x) * 0.36f; }
            if (position.z < min.z) { position.z = min.z; velocity.z = Mathf.Abs(velocity.z) * 0.36f; }
            if (position.z > max.z) { position.z = max.z; velocity.z = -Mathf.Abs(velocity.z) * 0.36f; }
            ball.position = position;
            body.linearVelocity = velocity;
        }

        private Vector3 ClampToZone(Vector3 position, float inset)
        {
            Vector3 min = ZoneMinimum(inset);
            Vector3 max = ZoneMaximum(inset);
            position.x = Mathf.Clamp(position.x, min.x, max.x);
            position.z = Mathf.Clamp(position.z, min.z, max.z);
            return position;
        }

        private Vector3 ZoneMinimum(float inset)
        {
            Vector3 half = Vector3.Scale(openPlayZone.lossyScale, Vector3.one) * 0.5f;
            return new Vector3(openPlayZone.position.x - Mathf.Abs(half.x) + inset, 0f, openPlayZone.position.z - Mathf.Abs(half.z) + inset);
        }

        private Vector3 ZoneMaximum(float inset)
        {
            Vector3 half = Vector3.Scale(openPlayZone.lossyScale, Vector3.one) * 0.5f;
            return new Vector3(openPlayZone.position.x + Mathf.Abs(half.x) - inset, 0f, openPlayZone.position.z + Mathf.Abs(half.z) - inset);
        }

        private void UpdateFocusPoint()
        {
            if (cameraFocus != null && ball != null)
            {
                cameraFocus.position = (motor.transform.position + ball.position) * 0.5f + Vector3.up * 0.2f;
            }
        }

        private void PositionBallInPlayerView()
        {
            if (ball == null || inputCamera == null) return;
            Transform view = inputCamera.transform;
            Vector3 forward = Vector3.ProjectOnPlane(view.forward, Vector3.up).normalized;
            if (forward.sqrMagnitude < 0.001f) forward = Vector3.forward;
            Vector3 right = Vector3.ProjectOnPlane(view.right, Vector3.up).normalized;
            Vector3 heldPosition = view.position + forward * 0.52f + right * 0.24f - Vector3.up * 0.56f;
            heldPosition.y = Mathf.Max(BallRadius + 0.06f, heldPosition.y);
            ball.position = heldPosition;
            ball.rotation = Quaternion.identity;
            TrailRenderer trail = ball.GetComponent<TrailRenderer>();
            if (trail != null) trail.Clear();
        }

        private void UpdateCatTracker()
        {
            if (catTrackerRoot == null || inputCamera == null || roomCamera == null || motor == null)
                return;
            bool inChase = CurrentState == BallState.Noticing || CurrentState == BallState.Chasing || CurrentState == BallState.Watching;
            if (!inChase || !roomCamera.IsSeatedView)
            {
                if (catTrackerRoot.gameObject.activeSelf) catTrackerRoot.gameObject.SetActive(false);
                return;
            }

            Vector3 catScreen = inputCamera.WorldToScreenPoint(motor.transform.position + Vector3.up * 0.28f);
            Vector2 center = new Vector2(Screen.width * 0.5f, Screen.height * 0.5f);
            Vector2 direction = new Vector2(catScreen.x, catScreen.y) - center;
            bool behind = catScreen.z <= 0f;
            if (behind) direction = -direction;
            bool offscreen = behind || catScreen.x < Screen.width * 0.07f || catScreen.x > Screen.width * 0.93f ||
                             catScreen.y < Screen.height * 0.10f || catScreen.y > Screen.height * 0.90f;
            catTrackerRoot.gameObject.SetActive(offscreen);
            if (!offscreen || direction.sqrMagnitude < 0.001f) return;

            direction.Normalize();
            float marginX = Screen.width * 0.10f;
            float marginY = Screen.height * 0.20f;
            float extentX = Mathf.Max(1f, center.x - marginX);
            float extentY = Mathf.Max(1f, center.y - marginY);
            float scale = 1f / Mathf.Max(Mathf.Abs(direction.x) / extentX, Mathf.Abs(direction.y) / extentY);
            Vector2 screenPosition = center + direction * scale;
            Camera canvasCamera = uiCanvas != null && uiCanvas.renderMode != RenderMode.ScreenSpaceOverlay ? uiCanvas.worldCamera : null;
            if (uiParent != null && RectTransformUtility.ScreenPointToLocalPointInRectangle(uiParent, screenPosition, canvasCamera, out Vector2 localPosition))
                catTrackerRoot.anchoredPosition = localPosition;
            float degrees = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg - 90f;
            catTrackerArrow.rectTransform.localRotation = Quaternion.Euler(0f, 0f, degrees);
        }

        private void ReleasePointer()
        {
            if (mouseCaptured) roomCamera?.ReleaseMousePointer();
            else if (touchId >= 0) roomCamera?.ReleaseTouchPointer(touchId);
            mouseCaptured = false;
            touchId = -1;
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

        private void OnDestroy()
        {
            ReleasePointer();
            if (motor != null && motor.IsActivityLocked)
            {
                motor.CancelLockedActivityMovement();
                motor.SetHiddenActivityLock(false);
            }
            if (ballMaterial != null) Destroy(ballMaterial);
            if (previewMaterial != null) Destroy(previewMaterial);
            if (trailMaterial != null) Destroy(trailMaterial);
            if (ballPhysicsMaterial != null) Destroy(ballPhysicsMaterial);
            if (rugPhysicsMaterial != null) Destroy(rugPhysicsMaterial);
            if (runtimeRoot != null) Destroy(runtimeRoot);
            if (body != null && CurrentState != BallState.Idle)
            {
                Debug.LogWarning("[CatMe][Ball] Scene ended during ball play; activity lock released and fallback cat remains available.");
            }
            Debug.Log($"[CatMe][Ball] Summary | throws={successfulThrows} | bats={batCount} | failedPaths={failedPaths} | balls={(HasSingleBall ? 1 : 0)}");
        }

        private void OnApplicationFocus(bool hasFocus)
        {
            if (!hasFocus) CancelUncommittedInput();
        }

        private void OnApplicationPause(bool paused)
        {
            if (paused) CancelUncommittedInput();
        }

        private void CancelUncommittedInput()
        {
            ReleasePointer();
            if (CurrentState == BallState.Aiming) EndAiming(false);
        }
    }
}
