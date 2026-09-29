using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

namespace CatMe.Cat
{
    /// <summary>
    /// Phase 4 movement diagnostic. The gameplay root is moved by NavMeshAgent;
    /// imported model transforms remain owned by LocalCatAssetLoader.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class CatMotor : MonoBehaviour
    {
        [SerializeField] private float speed = 0.68f;
        [SerializeField] private float acceleration = 2.2f;
        [SerializeField] private float angularSpeed = 300f;
        [SerializeField] private float stoppingDistance = 0.07f;
        [SerializeField] private float destinationPause = 0.6f;
        [SerializeField] private float stuckTimeout = 15f;
        [SerializeField] private float walkAnimationMultiplier = 1.0f;
        [SerializeField] private float minimumAnimationSpeed = 0.45f;
        [SerializeField] private float animationStopThreshold = 0.055f;
        [SerializeField] private float turnSlowdownAngle = 32f;

        private static readonly string[] RouteNames =
        {
            "CatSpawn", "CallDestination", "WindowApproach", "SleepApproach",
            "FeedingApproach", "ToyApproach", "CatSpawn", "CallDestination",
            "WindowApproach", "SleepApproach", "FeedingApproach", "ToyApproach",
        };

        private readonly List<Transform> route = new List<Transform>();
        private NavMeshAgent agent;
        private LocalCatAssetLoader loader;
        private Animation animationComponent;
        private AnimationState walkState;
        private AnimationState idleState;
        private bool animationWasWalking;
        private Transform catModelRoot;
        private Transform axisCorrection;
        private Transform loadedRoot;
        private Vector3 initialModelPosition;
        private Vector3 initialAxisPosition;
        private Vector3 initialLoadedPosition;
        private int legIndex;
        private int completedLegs;
        private int failedCount;
        private int partialCount;
        private int offNavMeshCount;
        private int stuckCount;
        private float legElapsed;
        private float pauseRemaining;
        private float maxArrivalError;
        private float minForwardDot = 1f;
        private float maxLocalDrift;
        private float turnInRemaining;
        private Vector3 activeDestination;
        private bool alignedForLeg;
        private bool routeRunning;
        private bool legActive;
        private bool routeFinished;
        private bool commandRunning;
        private bool commandIsAmbient;
        private bool commandArrived;
        private Transform commandDestination;
        private Quaternion commandFacing;
        private float facingRemaining;
        private MovementProfile movementProfile;
        private float reactionDelayRemaining;
        private float profileSpeedMultiplier = 1f;
        private float profileAccelerationMultiplier = 1f;

        private enum MovementProfile { Explore, Call, Feeding, Ball, Laser, Sleep }

        public float Speed => speed;
        public float Acceleration => acceleration;
        public float AngularSpeed => angularSpeed;
        public float StoppingDistance => stoppingDistance;
        public bool IsDiagnosticFinished => routeFinished;
        public bool IsReady => agent != null && walkState != null && loader != null && loadedRoot != null;
        public bool IsMoving => routeRunning || commandRunning;
        public bool HasArrived => commandArrived;
        public bool HasFullyArrived => commandArrived && !commandRunning && facingRemaining <= 0f;
        public bool IsActivityLocked { get; private set; }
        public bool IsAmbientMoving => commandIsAmbient && commandRunning;
        public bool IsAmbientCommand => commandIsAmbient;
        public bool CanStartPlayerAction => (!IsMoving || IsAmbientMoving) && !IsActivityLocked;
        public bool HasDiagnosticFailed { get; private set; }
        public int CompletedLegs => completedLegs;
        public int FailedCount => failedCount;
        public int PartialCount => partialCount;
        public int OffNavMeshCount => offNavMeshCount;
        public int StuckCount => stuckCount;
        public float MaxArrivalError => maxArrivalError;
        public float MinForwardVelocityDot => minForwardDot;
        public float MaxLocalDrift => maxLocalDrift;

        public void Initialize(LocalCatAssetLoader assetLoader)
        {
            loader = assetLoader;
            catModelRoot = transform.Find("CatModelRoot");
            axisCorrection = catModelRoot == null ? null : catModelRoot.Find("AxisCorrection");
            loadedRoot = loader == null ? null : loader.LoadedRoot;
            animationComponent = loader == null ? null : loader.ImportedAnimation;
            walkState = loader == null || animationComponent == null || loader.SelectedWalkClip == null
                ? null
                : animationComponent[loader.SelectedWalkClip.name];
            idleState = loader == null || animationComponent == null || loader.SelectedIdleClip == null
                ? null
                : animationComponent[loader.SelectedIdleClip.name];

            if (loader == null || loadedRoot == null || walkState == null)
            {
                Debug.LogError("[CatMe][Locomotion] Missing loaded cat or walk animation; route was not started.");
                return;
            }

            initialModelPosition = catModelRoot.localPosition;
            initialAxisPosition = axisCorrection == null ? Vector3.zero : axisCorrection.localPosition;
            initialLoadedPosition = loadedRoot.localPosition;

            // Unity components can be represented by a managed reference after
            // their native object has gone away. Use Unity's overloaded null
            // check rather than C# null coalescing so a fresh agent is added.
            agent = GetComponent<NavMeshAgent>();
            if (agent == null)
            {
                agent = gameObject.AddComponent<NavMeshAgent>();
            }
            agent.speed = speed;
            agent.acceleration = acceleration;
            agent.angularSpeed = angularSpeed;
            agent.stoppingDistance = stoppingDistance;
            agent.updatePosition = true;
            agent.updateRotation = false;
            agent.radius = 0.12f;
            agent.height = 0.35f;
            agent.baseOffset = 0f;
            agent.autoBraking = true;
            agent.obstacleAvoidanceType = ObstacleAvoidanceType.LowQualityObstacleAvoidance;
            agent.enabled = false;
            // The shared loader starts the walk clip for the stationary-load
            // acceptance. HomeRoom has finished that acceptance by the time
            // the motor is created, so leave the cat idle until navigation starts.
            animationWasWalking = animationComponent.IsPlaying(walkState.name);
            SetWalking(false, 0f);
        }

        public void BeginDiagnosticRoute()
        {
            if (routeRunning || routeFinished || agent == null)
            {
                return;
            }

            route.Clear();
            foreach (string routeName in RouteNames)
            {
                GameObject pointObject = GameObject.Find(routeName);
                if (pointObject == null)
                {
                    Debug.LogError($"[CatMe][Locomotion] Missing destination: {routeName}; route was not started.");
                    failedCount++;
                    return;
                }

                route.Add(pointObject.transform);
            }

            if (!agent.isOnNavMesh)
            {
                if (!NavMesh.SamplePosition(transform.position, out NavMeshHit spawnHit, 0.5f, NavMesh.AllAreas))
                {
                    offNavMeshCount++;
                    Debug.LogError("[CatMe][Locomotion] CatRuntime could not be sampled onto the NavMesh.");
                    return;
                }

                transform.position = spawnHit.position;
                agent.enabled = true;
            }
            else
            {
                agent.enabled = true;
            }

            routeRunning = true;
            legIndex = 0;
            completedLegs = 0;
            failedCount = 0;
            partialCount = 0;
            offNavMeshCount = 0;
            stuckCount = 0;
            maxArrivalError = 0f;
            minForwardDot = 1f;
            maxLocalDrift = 0f;
            pauseRemaining = 0f;
            StartLeg();
        }

        public bool TryMoveTo(Transform destination, string reason)
        {
            if (!IsReady || destination == null)
            {
                Debug.LogError($"[CatMe][Locomotion] Cannot move for '{reason}': missing motor or destination.");
                return false;
            }

            if (commandIsAmbient)
            {
                if (commandRunning) CancelAmbientMovement();
                commandIsAmbient = false;
            }

            if (routeRunning || commandRunning || IsActivityLocked)
            {
                return false;
            }

            return StartMoveCommand(destination, reason, false);
        }

        public bool TryMoveToAmbient(Transform destination, string reason)
        {
            if (routeRunning || commandRunning || IsActivityLocked || !IsReady || destination == null) return false;
            if (!StartMoveCommand(destination, reason, false)) return false;
            commandIsAmbient = true;
            return true;
        }

        public bool CancelAmbientMovement()
        {
            if (!commandIsAmbient || !commandRunning) return false;
            commandIsAmbient = false;
            commandRunning = false;
            commandArrived = false;
            facingRemaining = 0f;
            if (agent != null && agent.enabled && agent.isOnNavMesh) agent.isStopped = true;
            SetWalking(false, 0f);
            return true;
        }

        /// <summary>Stops low-priority roaming so direct player actions can take over.</summary>
        public bool PrepareForPlayerAction()
        {
            if (IsAmbientMoving) CancelAmbientMovement();
            return !IsMoving && !IsActivityLocked;
        }

        /// <summary>Clears the ambient command after the cat has reached its destination.</summary>
        public bool TryClearAmbientCommand()
        {
            if (!commandIsAmbient || commandRunning || !commandArrived) return false;
            commandIsAmbient = false;
            commandArrived = false;
            commandDestination = null;
            return true;
        }

        /// <summary>Moves for the activity that already owns the motor lock.</summary>
        public bool TryMoveToLockedActivity(Transform destination, string reason)
        {
            if (commandIsAmbient)
            {
                if (commandRunning) CancelAmbientMovement();
                commandIsAmbient = false;
            }
            if (!IsActivityLocked || routeRunning || commandRunning || !IsReady || destination == null)
            {
                return false;
            }

            return StartMoveCommand(destination, reason, true);
        }

        /// <summary>Updates a moving activity target without releasing its motor lock.</summary>
        public bool UpdateLockedActivityDestination(Transform destination, float minimumRepathDistance = 0.28f)
        {
            if (!IsActivityLocked || !commandRunning || destination == null || agent == null ||
                !agent.enabled || !agent.isOnNavMesh || agent.pathPending)
            {
                return false;
            }

            if (!NavMesh.SamplePosition(destination.position, out NavMeshHit targetHit, 0.5f, NavMesh.AllAreas) ||
                Vector3.Distance(activeDestination, targetHit.position) < minimumRepathDistance)
            {
                return false;
            }

            NavMeshPath path = new NavMeshPath();
            if (!NavMesh.CalculatePath(transform.position, targetHit.position, NavMesh.AllAreas, path) ||
                path.status != NavMeshPathStatus.PathComplete || !agent.SetDestination(targetHit.position))
            {
                return false;
            }

            activeDestination = targetHit.position;
            commandDestination = destination;
            commandFacing = destination.rotation;
            return true;
        }

        public void CancelLockedActivityMovement()
        {
            if (!IsActivityLocked) return;
            commandRunning = false;
            commandArrived = false;
            facingRemaining = 0f;
            if (agent != null && agent.enabled && agent.isOnNavMesh)
            {
                agent.isStopped = true;
            }
            SetWalking(false, 0f);
        }

        private bool StartMoveCommand(Transform destination, string reason, bool allowSameDestination)
        {
            if (!IsReady || destination == null)
            {
                return false;
            }

            if (!allowSameDestination && commandArrived && commandDestination == destination)
            {
                Debug.Log($"[CatMe][Locomotion] Already at '{destination.name}'; call ignored.");
                return false;
            }

            if (!EnsureAgentOnNavMesh())
            {
                Debug.LogError($"[CatMe][Locomotion] Cannot move for '{reason}': CatRuntime is not on the NavMesh.");
                return false;
            }

            if (!NavMesh.SamplePosition(destination.position, out NavMeshHit targetHit, 0.5f, NavMesh.AllAreas))
            {
                Debug.LogError($"[CatMe][Locomotion] Destination '{destination.name}' is not on the NavMesh.");
                return false;
            }

            NavMeshPath path = new NavMeshPath();
            if (!NavMesh.CalculatePath(transform.position, targetHit.position, NavMesh.AllAreas, path) ||
                path.status != NavMeshPathStatus.PathComplete)
            {
                Debug.LogError($"[CatMe][Locomotion] Path to '{destination.name}' is invalid.");
                return false;
            }

            if (!agent.SetDestination(targetHit.position))
            {
                Debug.LogError($"[CatMe][Locomotion] NavMeshAgent rejected destination '{destination.name}'.");
                return false;
            }

            agent.isStopped = false;
            commandDestination = destination;
            activeDestination = targetHit.position;
            commandFacing = destination.rotation;
            commandRunning = true;
            commandIsAmbient = false;
            commandArrived = false;
            facingRemaining = 0f;
            legElapsed = 0f;
            turnInRemaining = 0.2f;
            alignedForLeg = false;
            movementProfile = ResolveProfile(reason);
            reactionDelayRemaining = movementProfile == MovementProfile.Call ? UnityEngine.Random.Range(0.22f, 0.38f) :
                movementProfile == MovementProfile.Laser ? UnityEngine.Random.Range(0.10f, 0.18f) : 0f;
            profileSpeedMultiplier = GetProfileSpeed(movementProfile);
            profileAccelerationMultiplier = GetProfileAcceleration(movementProfile);
            agent.speed = speed * profileSpeedMultiplier;
            agent.acceleration = acceleration * profileAccelerationMultiplier;
            agent.isStopped = reactionDelayRemaining > 0f;
            return true;
        }

        public void SetHiddenActivityLock(bool locked)
        {
            IsActivityLocked = locked;
        }

        public bool PlaceAtHiddenPoint(Transform hiddenPoint)
        {
            if (hiddenPoint == null || agent == null || routeRunning || commandRunning)
            {
                return false;
            }

            commandRunning = false;
            commandArrived = false;
            facingRemaining = 0f;
            SetWalking(false, 0f);
            agent.isStopped = true;
            agent.enabled = false;
            transform.SetPositionAndRotation(hiddenPoint.position, hiddenPoint.rotation);
            return true;
        }

        public bool RestoreAt(Transform approachPoint, out float positionError)
        {
            positionError = float.PositiveInfinity;
            if (approachPoint == null || agent == null || routeRunning || commandRunning ||
                !NavMesh.SamplePosition(approachPoint.position, out NavMeshHit sample, 0.5f, NavMesh.AllAreas))
            {
                return false;
            }

            agent.enabled = false;
            transform.SetPositionAndRotation(sample.position, approachPoint.rotation);
            agent.enabled = true;
            agent.isStopped = true;
            commandRunning = false;
            commandArrived = true;
            commandDestination = null;
            activeDestination = sample.position;
            facingRemaining = 0f;
            positionError = Vector3.Distance(transform.position, sample.position);
            return agent.isOnNavMesh;
        }

        private void Update()
        {
            if (agent == null)
            {
                return;
            }

            if (commandRunning)
            {
                UpdateCommand();
                return;
            }

            if (facingRemaining > 0f)
            {
                facingRemaining -= Time.deltaTime;
                transform.rotation = Quaternion.RotateTowards(transform.rotation, commandFacing, angularSpeed * Time.deltaTime);
                return;
            }

            if (!routeRunning)
            {
                return;
            }

            TrackLocalDrift();
            if (!agent.isOnNavMesh)
            {
                offNavMeshCount++;
                FailRoute("CatRuntime left the NavMesh.");
                return;
            }

            if (pauseRemaining > 0f)
            {
                pauseRemaining -= Time.deltaTime;
                SetWalking(false, 0f);
                if (pauseRemaining <= 0f)
                {
                    legIndex++;
                    if (legIndex >= route.Count - 1)
                    {
                        FinishRoute();
                    }
                    else
                    {
                        StartLeg();
                    }
                }

                return;
            }

            if (!legActive)
            {
                return;
            }

            legElapsed += Time.deltaTime;
            Vector3 horizontalVelocity = agent.velocity;
            horizontalVelocity.y = 0f;
            float horizontalSpeed = horizontalVelocity.magnitude;
            if (horizontalSpeed > 0.05f)
            {
                Vector3 direction = horizontalVelocity / horizontalSpeed;
                Quaternion targetRotation = Quaternion.LookRotation(direction, Vector3.up);
                transform.rotation = Quaternion.RotateTowards(transform.rotation, targetRotation, angularSpeed * Time.deltaTime);
                float forwardDot = Vector3.Dot(transform.forward, direction);
                if (turnInRemaining > 0f)
                {
                    turnInRemaining -= Time.deltaTime;
                }
                else if (!alignedForLeg)
                {
                    // Wait until the body is comfortably aligned before
                    // recording the minimum; sampling at the acceptance edge
                    // can round a 0.95 turn-in sample just below the gate.
                    alignedForLeg = forwardDot >= 0.97f;
                }
                else
                {
                    minForwardDot = Mathf.Min(minForwardDot, forwardDot);
                }
            }

            SetWalking(horizontalSpeed > 0.03f, horizontalSpeed);
            if (horizontalSpeed < 0.03f && legElapsed > 1f && agent.remainingDistance > stoppingDistance + 0.08f)
            {
                if (legElapsed >= stuckTimeout)
                {
                    stuckCount++;
                    FailRoute($"CatRuntime was stuck on route leg {legIndex + 1}.");
                }
                return;
            }

            if (!agent.pathPending && agent.remainingDistance <= stoppingDistance + 0.03f && horizontalSpeed <= 0.08f)
            {
                CompleteLeg();
            }
            else if (legElapsed >= stuckTimeout)
            {
                stuckCount++;
                FailRoute($"CatRuntime timed out on route leg {legIndex + 1}.");
            }
        }

        private void UpdateCommand()
        {
            TrackLocalDrift();
            if (!agent.isOnNavMesh)
            {
                commandRunning = false;
                agent.isStopped = true;
                SetWalking(false, 0f);
                Debug.LogError("[CatMe][Locomotion] CatRuntime left the NavMesh during a move command.");
                return;
            }

            legElapsed += Time.deltaTime;
            if (reactionDelayRemaining > 0f)
            {
                reactionDelayRemaining -= Time.deltaTime;
                SetWalking(false, 0f);
                if (reactionDelayRemaining > 0f) return;
                agent.isStopped = false;
            }

            Vector3 horizontalVelocity = agent.velocity;
            horizontalVelocity.y = 0f;
            float horizontalSpeed = horizontalVelocity.magnitude;
            ApplyCommandMotionProfile();

            if (horizontalSpeed > 0.05f)
            {
                Vector3 facingDirection = GetGuidedDirection(horizontalVelocity);
                Quaternion targetRotation = Quaternion.LookRotation(facingDirection, Vector3.up);
                transform.rotation = Quaternion.RotateTowards(transform.rotation, targetRotation, angularSpeed * Time.deltaTime);
            }

            SetWalking(horizontalSpeed > animationStopThreshold, horizontalSpeed);
            if (!agent.pathPending && agent.remainingDistance <= stoppingDistance + 0.03f && horizontalSpeed <= 0.08f)
            {
                commandRunning = false;
                commandArrived = true;
                agent.isStopped = true;
                SetWalking(false, 0f);
                facingRemaining = movementProfile == MovementProfile.Explore ? 0.95f : 0.48f;
                Debug.Log($"[CatMe][Locomotion] Arrived at '{commandDestination.name}' | " +
                          $"distance={Vector3.Distance(transform.position, activeDestination):0.####}m.");
            }
            else if (legElapsed >= stuckTimeout)
            {
                commandRunning = false;
                agent.isStopped = true;
                SetWalking(false, 0f);
                Debug.LogError($"[CatMe][Locomotion] Move command to '{commandDestination.name}' timed out.");
            }
        }

        private bool EnsureAgentOnNavMesh()
        {
            if (agent.enabled && agent.isOnNavMesh)
            {
                return true;
            }

            if (!NavMesh.SamplePosition(transform.position, out NavMeshHit spawnHit, 0.5f, NavMesh.AllAreas))
            {
                return false;
            }

            transform.position = spawnHit.position;
            agent.enabled = true;
            return agent.isOnNavMesh;
        }

        private void StartLeg()
        {
            if (legIndex + 1 >= route.Count)
            {
                FinishRoute();
                return;
            }

            Transform destination = route[legIndex + 1];
            if (!NavMesh.SamplePosition(destination.position, out NavMeshHit targetHit, 0.5f, NavMesh.AllAreas))
            {
                offNavMeshCount++;
                FailRoute($"Destination '{destination.name}' is not on the NavMesh.");
                return;
            }

            NavMeshPath path = new NavMeshPath();
            if (!NavMesh.CalculatePath(transform.position, targetHit.position, NavMesh.AllAreas, path))
            {
                failedCount++;
                FailRoute($"Path calculation failed for '{destination.name}'.");
                return;
            }

            if (path.status != NavMeshPathStatus.PathComplete)
            {
                partialCount++;
                FailRoute($"Path to '{destination.name}' is {path.status}.");
                return;
            }

            agent.isStopped = false;
            if (!agent.SetDestination(targetHit.position))
            {
                failedCount++;
                FailRoute($"NavMeshAgent rejected destination '{destination.name}'.");
                return;
            }

            activeDestination = targetHit.position;
            legElapsed = 0f;
            turnInRemaining = 0.2f;
            alignedForLeg = false;
            legActive = true;
        }

        private void CompleteLeg()
        {
            legActive = false;
            agent.isStopped = true;
            SetWalking(false, 0f);
            // An authored interaction point can be a few centimetres inside a
            // prop or wall. Measure arrival against the sampled NavMesh point
            // that was actually given to the agent.
            Vector3 offset = transform.position - activeDestination;
            offset.y = 0f;
            maxArrivalError = Mathf.Max(maxArrivalError, offset.magnitude);
            completedLegs++;
            pauseRemaining = destinationPause;
        }

        private void SetWalking(bool walking, float actualSpeed)
        {
            if (walkState == null || animationComponent == null)
            {
                return;
            }

            if (!walking || actualSpeed < animationStopThreshold)
            {
                if (!animationWasWalking) return;
                animationWasWalking = false;
                if (idleState != null)
                {
                    idleState.wrapMode = WrapMode.Loop;
                    idleState.speed = 1f;
                    animationComponent.CrossFade(idleState.name, 0.22f, PlayMode.StopAll);
                }
                else
                {
                    walkState.enabled = false;
                }
                return;
            }

            animationWasWalking = true;
            walkState.enabled = true;
            walkState.wrapMode = WrapMode.Loop;
            walkState.speed = Mathf.Clamp(actualSpeed / Mathf.Max(speed, 0.01f) * walkAnimationMultiplier, minimumAnimationSpeed, 1.55f);
            if (!animationComponent.IsPlaying(walkState.name))
            {
                animationComponent.CrossFade(walkState.name, 0.18f, PlayMode.StopAll);
            }
        }

        private void ApplyCommandMotionProfile()
        {
            float remaining = agent.pathPending ? float.PositiveInfinity : agent.remainingDistance;
            float stoppingSpeed = Mathf.Sqrt(Mathf.Max(0f, 2f * acceleration * profileAccelerationMultiplier * Mathf.Max(0f, remaining - stoppingDistance)));
            float desiredSpeed = Mathf.Min(speed * profileSpeedMultiplier, stoppingSpeed);
            Vector3 desiredDirection = agent.desiredVelocity;
            desiredDirection.y = 0f;
            if (desiredDirection.sqrMagnitude > 0.01f)
            {
                Vector3 forward = transform.forward;
                forward.y = 0f;
                if (forward.sqrMagnitude > 0.001f)
                {
                    float angle = Vector3.Angle(forward, desiredDirection.normalized);
                    float turnFactor = Mathf.Lerp(1f, 0.58f, Mathf.InverseLerp(turnSlowdownAngle, 105f, angle));
                    desiredSpeed *= turnFactor;
                }
            }
            agent.speed = Mathf.MoveTowards(agent.speed, Mathf.Max(0.08f, desiredSpeed), acceleration * profileAccelerationMultiplier * Time.deltaTime);
            agent.acceleration = acceleration * profileAccelerationMultiplier;
        }

        private Vector3 GetGuidedDirection(Vector3 velocity)
        {
            Vector3 direction = velocity.normalized;
            if (agent.pathPending || agent.path == null || agent.path.corners.Length < 2) return direction;
            Vector3 cornerDirection = agent.path.corners[1] - transform.position;
            cornerDirection.y = 0f;
            if (cornerDirection.sqrMagnitude < 0.001f) return direction;
            return Vector3.Slerp(direction, cornerDirection.normalized, 0.18f).normalized;
        }

        private static MovementProfile ResolveProfile(string reason)
        {
            string value = reason ?? string.Empty;
            if (value.IndexOf("call", StringComparison.OrdinalIgnoreCase) >= 0) return MovementProfile.Call;
            if (value.IndexOf("feed", StringComparison.OrdinalIgnoreCase) >= 0) return MovementProfile.Feeding;
            if (value.IndexOf("sleep", StringComparison.OrdinalIgnoreCase) >= 0) return MovementProfile.Sleep;
            if (value.IndexOf("laser", StringComparison.OrdinalIgnoreCase) >= 0) return MovementProfile.Laser;
            if (value.IndexOf("ball", StringComparison.OrdinalIgnoreCase) >= 0) return MovementProfile.Ball;
            return MovementProfile.Explore;
        }

        private static float GetProfileSpeed(MovementProfile profile)
        {
            switch (profile)
            {
                case MovementProfile.Call: return UnityEngine.Random.Range(1.08f, 1.18f);
                case MovementProfile.Feeding: return UnityEngine.Random.Range(0.84f, 0.92f);
                // Briskest supported gait; do not exceed the known walk clip.
                case MovementProfile.Ball: return 1.45f;
                case MovementProfile.Laser: return UnityEngine.Random.Range(1.0f, 1.12f);
                case MovementProfile.Sleep: return UnityEngine.Random.Range(0.72f, 0.82f);
                default: return UnityEngine.Random.Range(0.82f, 0.92f);
            }
        }

        private static float GetProfileAcceleration(MovementProfile profile)
        {
            switch (profile)
            {
                case MovementProfile.Call: return 1.0f;
                case MovementProfile.Feeding: return 0.9f;
                case MovementProfile.Ball: return 1.35f;
                case MovementProfile.Laser: return 1.05f;
                case MovementProfile.Sleep: return 0.78f;
                default: return 0.78f;
            }
        }

        private void TrackLocalDrift()
        {
            if (catModelRoot != null)
            {
                maxLocalDrift = Mathf.Max(maxLocalDrift, Vector3.Distance(catModelRoot.localPosition, initialModelPosition));
            }
            if (axisCorrection != null)
            {
                maxLocalDrift = Mathf.Max(maxLocalDrift, Vector3.Distance(axisCorrection.localPosition, initialAxisPosition));
            }
            if (loadedRoot != null)
            {
                maxLocalDrift = Mathf.Max(maxLocalDrift, Vector3.Distance(loadedRoot.localPosition, initialLoadedPosition));
            }
        }

        private void FailRoute(string message)
        {
            routeRunning = false;
            legActive = false;
            HasDiagnosticFailed = true;
            agent.isStopped = true;
            SetWalking(false, 0f);
            Debug.LogError("[CatMe][Locomotion] " + message);
        }

        private void FinishRoute()
        {
            routeRunning = false;
            routeFinished = true;
            agent.isStopped = true;
            SetWalking(false, 0f);
            bool pass = completedLegs >= 10 && failedCount == 0 && partialCount == 0 && offNavMeshCount == 0 &&
                        stuckCount == 0 && maxArrivalError <= 0.10f && minForwardDot >= 0.95f && maxLocalDrift <= 0.02f;
            string clipName = loader == null || loader.SelectedWalkClip == null ? "none" : loader.SelectedWalkClip.name;
            string summary = $"[CatMe][Locomotion] validation summary: {(pass ? "PASS" : "FAIL")} | " +
                             $"completedLegs={completedLegs} | failed={failedCount} | partial={partialCount} | " +
                             $"offNavMesh={offNavMeshCount} | stuck={stuckCount} | maxArrivalError={maxArrivalError:0.####}m | " +
                             $"minForwardVelocityDot={minForwardDot:0.###} | maxLocalDrift={maxLocalDrift:0.####}m | " +
                             $"speed={speed:0.###}m/s | acceleration={acceleration:0.###}m/s2 | " +
                             $"angularSpeed={angularSpeed:0.###}deg/s | stoppingDistance={stoppingDistance:0.###}m | " +
                             $"walkClip={clipName}";
            if (pass)
            {
                Debug.Log(summary);
            }
            else
            {
                Debug.LogError(summary);
            }
        }
    }
}
