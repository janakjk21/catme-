using UnityEngine;

namespace CatMe.Cat
{
    /// <summary>
    /// Owns the small, reversible response used by direct cat petting.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class CatPetReaction : MonoBehaviour
    {
        [SerializeField] private AudioClip purrClip;
        [SerializeField] private float reactionCooldown = 0.75f;
        [SerializeField] private float purrReleaseDelay = 0.55f;

        private Transform catModelRoot;
        private Transform axisCorrection;
        private Transform loadedRoot;
        private CatMotor motor;
        private CatCompanionJournal journal;
        private BoxCollider petCollider;
        private AudioSource purrSource;
        private Quaternion neutralRotation;
        private Vector3 neutralScale;
        private Vector3 neutralModelPosition;
        private Vector3 neutralAxisPosition;
        private Vector3 neutralLoadedPosition;
        private float nextReactionTime;
        private float purrStopTime;
        private float reactionRemaining;
        private float leanSign = 1f;
        private float maximumProtectedPositionDrift;
        private float maximumNeutralRotationError;
        private float maximumNeutralScaleError;
        private int validStrokeCount;
        private int reactionCount;
        private int hapticRequestCount;
        private bool initialized;

        public Collider PetTarget => petCollider;
        public int ValidStrokeCount => validStrokeCount;
        public int ReactionCount => reactionCount;
        public int HapticRequestCount => hapticRequestCount;
        public int PurrStartCount { get; private set; }
        public bool HasPurrClip => purrSource != null && purrSource.clip != null;
        public int RejectedTapCount { get; private set; }
        public bool IsPurrPlaying => purrSource != null && purrSource.isPlaying;
        public float MaximumProtectedPositionDrift => maximumProtectedPositionDrift;
        public float MaximumNeutralRotationError => maximumNeutralRotationError;
        public float MaximumNeutralScaleError => maximumNeutralScaleError;

        public void Initialize(LocalCatAssetLoader loader, Transform modelRoot, CatMotor readyMotor, AudioClip clip)
        {
            if (initialized || loader == null || modelRoot == null || loader.LoadedRoot == null)
            {
                return;
            }

            initialized = true;
            catModelRoot = modelRoot;
            axisCorrection = modelRoot.Find("AxisCorrection");
            loadedRoot = loader.LoadedRoot;
            motor = readyMotor;
            journal = readyMotor.GetComponent<CatCompanionJournal>();
            purrClip = clip != null ? clip : purrClip;
            neutralRotation = catModelRoot.localRotation;
            neutralScale = catModelRoot.localScale;
            neutralModelPosition = catModelRoot.localPosition;
            neutralAxisPosition = axisCorrection == null ? Vector3.zero : axisCorrection.localPosition;
            neutralLoadedPosition = loadedRoot.localPosition;
            CreatePetTarget();
            CreatePurrSource();
        }

        public bool IsPetTarget(Collider candidate)
        {
            return petCollider != null && candidate == petCollider;
        }

        public bool TryReact(float horizontalStroke)
        {
            if (!initialized || motor == null || Time.unscaledTime < nextReactionTime || !motor.PrepareForPlayerAction())
            {
                return false;
            }

            nextReactionTime = Time.unscaledTime + reactionCooldown;
            reactionRemaining = 0.34f;
            leanSign = Mathf.Abs(horizontalStroke) < 0.01f ? leanSign : Mathf.Sign(horizontalStroke);
            validStrokeCount++;
            reactionCount++;
            journal?.RecordDiscovery("first_relaxed_petting");
            StartPurr();
            RequestHaptic();
            return true;
        }

        public void RejectTap()
        {
            RejectedTapCount++;
        }

        public void EndPetting()
        {
            purrStopTime = Time.unscaledTime + purrReleaseDelay;
        }

        public void CancelPetting()
        {
            reactionRemaining = 0f;
            EndPetting();
        }

        private void Update()
        {
            if (!initialized)
            {
                return;
            }

            if (motor != null && motor.IsMoving)
            {
                CancelPetting();
            }

            float deltaTime = Mathf.Max(Time.unscaledDeltaTime, 0.0001f);
            float response = reactionRemaining > 0f ? Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(reactionRemaining / 0.34f)) : 0f;
            reactionRemaining = Mathf.Max(0f, reactionRemaining - deltaTime);
            if (catModelRoot != null)
            {
                catModelRoot.localRotation = Quaternion.Slerp(neutralRotation, neutralRotation * Quaternion.Euler(0f, 0f, 2.2f * leanSign), response);
                catModelRoot.localScale = Vector3.Lerp(neutralScale, neutralScale * 1.004f, response);
            }

            if (purrSource != null && purrSource.isPlaying && Time.unscaledTime >= purrStopTime && reactionRemaining <= 0f)
            {
                purrSource.Stop();
            }

            TrackProtectedTransforms();
        }

        private void CreatePetTarget()
        {
            GameObject target = new GameObject("CatPetTarget");
            target.transform.SetParent(transform, false);
            petCollider = target.AddComponent<BoxCollider>();
            petCollider.isTrigger = true;

            Bounds worldBounds = new Bounds(loadedRoot.position, Vector3.zero);
            bool foundRenderer = false;
            foreach (Renderer renderer in loadedRoot.GetComponentsInChildren<Renderer>(true))
            {
                if (!foundRenderer)
                {
                    worldBounds = renderer.bounds;
                    foundRenderer = true;
                }
                else
                {
                    worldBounds.Encapsulate(renderer.bounds);
                }
            }

            if (!foundRenderer)
            {
                Debug.LogError("[CatMe][Petting] Could not fit CatPetTarget: loaded cat has no renderer.");
                return;
            }

            Vector3 localMin = new Vector3(float.PositiveInfinity, float.PositiveInfinity, float.PositiveInfinity);
            Vector3 localMax = new Vector3(float.NegativeInfinity, float.NegativeInfinity, float.NegativeInfinity);
            for (int x = -1; x <= 1; x += 2)
            for (int y = -1; y <= 1; y += 2)
            for (int z = -1; z <= 1; z += 2)
            {
                Vector3 worldCorner = worldBounds.center + Vector3.Scale(worldBounds.extents, new Vector3(x, y, z));
                Vector3 localCorner = transform.InverseTransformPoint(worldCorner);
                localMin = Vector3.Min(localMin, localCorner);
                localMax = Vector3.Max(localMax, localCorner);
            }

            petCollider.center = (localMin + localMax) * 0.5f;
            petCollider.size = Vector3.Max(localMax - localMin, new Vector3(0.08f, 0.08f, 0.08f));
            petCollider.name = "CatPetTarget_FittedBox";
            Debug.Log($"[CatMe][Petting] CatPetTarget fitted | type=BoxTrigger | size={petCollider.size}m | center={petCollider.center}m");
        }

        private void CreatePurrSource()
        {
            GameObject audioObject = new GameObject("CatPurrAudio");
            audioObject.transform.SetParent(transform, false);
            purrSource = audioObject.AddComponent<AudioSource>();
            purrSource.clip = purrClip;
            purrSource.loop = true;
            purrSource.playOnAwake = false;
            purrSource.volume = 0.42f;
            purrSource.spatialBlend = 0.35f;
            purrSource.rolloffMode = AudioRolloffMode.Linear;
            purrSource.minDistance = 1.5f;
            purrSource.maxDistance = 8f;
        }

        private void StartPurr()
        {
            if (purrSource == null || purrSource.clip == null)
            {
                return;
            }

            purrStopTime = Time.unscaledTime + purrReleaseDelay;
            if (!purrSource.isPlaying)
            {
                purrSource.Play();
                PurrStartCount++;
            }
        }

        private void RequestHaptic()
        {
            if (!Application.isMobilePlatform)
            {
                return;
            }

            Handheld.Vibrate();
            hapticRequestCount++;
        }

        private void TrackProtectedTransforms()
        {
            maximumProtectedPositionDrift = Mathf.Max(maximumProtectedPositionDrift, Vector3.Distance(catModelRoot.localPosition, neutralModelPosition));
            if (axisCorrection != null)
            {
                maximumProtectedPositionDrift = Mathf.Max(maximumProtectedPositionDrift, Vector3.Distance(axisCorrection.localPosition, neutralAxisPosition));
            }
            maximumProtectedPositionDrift = Mathf.Max(maximumProtectedPositionDrift, Vector3.Distance(loadedRoot.localPosition, neutralLoadedPosition));
            maximumNeutralRotationError = Mathf.Max(maximumNeutralRotationError, Quaternion.Angle(catModelRoot.localRotation, neutralRotation));
            maximumNeutralScaleError = Mathf.Max(maximumNeutralScaleError, Vector3.Distance(catModelRoot.localScale, neutralScale));
        }

        private void OnDestroy()
        {
            bool pass = petCollider != null && maximumProtectedPositionDrift <= 0.0001f &&
                        reactionRemaining <= 0.001f && purrSource != null;
            Debug.Log($"[CatMe][Petting] Validation summary | captured=runtime-input | valid={validStrokeCount} | rejectedTaps={RejectedTapCount} | " +
                      $"reactions={reactionCount} | haptics={hapticRequestCount} | purrClip={HasPurrClip} | purrStarts={PurrStartCount} | " +
                      $"purrPlaying={IsPurrPlaying} | maxProtectedPositionDrift={maximumProtectedPositionDrift:0.######}m | " +
                      $"maxRotationError={maximumNeutralRotationError:0.######}deg | maxScaleError={maximumNeutralScaleError:0.######} | result={(pass ? "PASS" : "FAIL")} |");
        }
    }
}
