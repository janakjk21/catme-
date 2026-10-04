using System;
using System.Collections;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using GLTFast;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.UI;
using UnityEngine.Rendering.Universal;
using UnityEngine.UI;

namespace CatMe.Garden
{
    /// <summary>Standalone garden companion scene: human movement, cat roaming/follow, call and fetch.</summary>
    public sealed class GardenCompanionDemo : MonoBehaviour
    {
        private const string GardenAssets = "CompanionGarden";
        private static Sprite joystickDiscSprite;
        private static Sprite joystickRingSprite;
        // The playable garden extends past the camera view; it is a place to move through,
        // not a small fenced board that must fit in one screen.
        private readonly Vector3 gardenLimit = new Vector3(3.15f, 0f, 138f);
        private Transform humanRoot, catRoot, ball;
        private Transform gardenCamera;
        private Transform humanHand;
        private GameObject walkingHuman, callingHuman;
        private DeviceBridge device;
        private CatMeAppFlow appFlow;
        // GardenCompanionDemo remains the sole owner of both character transforms.
        // CatMotor/NavMesh are intentionally not enabled in this scene.
        private Collider groundCollider;
        private JourneyExperience journey;
        private Animation humanWalkAnimation, humanCallAnimation, catAnimation;
        private AnimationState humanWalkState, sitState, beckonState, catWalkState;
        private AudioSource audioSource;
        private Text status;
        private GameObject statusPanel, catTray, pauseSheet;
        private Text followActionLabel;
        private Text replayActionLabel;
        private bool replayArmed;
        private CanvasGroup statusGroup;
        private Coroutine statusFade;
        private Transform canvasRoot;
        private bool follow, calling, fetching;
        private bool catHidden;
        private bool gameplayHudVisible = true;
        private bool inputBlocked;
        private float nextCallAt;
        private int firstGardenStage;
        private float firstWalkDistance;
        private Vector2 moveInput, touchMoveInput;
        private Vector3 catTarget, ballVelocity;
        private float wanderAt, callStarted, ballStartedAt;
        private bool ballInFlight;
        private bool ballRejoining;
        // Dog fetch: the ball rides in the pet's mouth until it is dropped at the owner's feet. throwCount guards delayed hides.
        private bool ballCarried; private int throwCount; private float carryHeight = .25f;
        public event Action<float, bool> OwnerMoved;
        public event Action CallAccepted, CatArrived, CallUnavailable, BallReleased;
        public event Action<bool> BallFinished;
        private CompanionCamera companionCamera;
        private CatMe.Session.SessionDirector session;
        private Vector3 curiosityTarget;
        private float curiosityUntil;
        private float movementYaw;
        private bool movementLatched;
        private Vector3 scriptedWalk;
        private bool unboundedWorld;
        public bool DidCallGesturePlay { get; private set; }
        private CatMe.Pets.CompanionBehavior behavior = new CatMe.Pets.CatBehavior();
        public CatMe.Pets.CompanionBehavior Behavior => behavior;
        public CompanionVoice Voice { get; private set; }
        public AudioSource CompanionAudio => audioSource;
        private GameObject dogModel;
        private Vector3 leadTarget; private bool leadActive, leadHold, curiosityPlay;

        /// <summary>Applies the chosen pet: its species behaviour and model. Local fixtures only; the cat uses the known-good cat, the dog uses the Dogs Kit corgi.</summary>
        private string loadedCatModel;
        public void SetActivePet(CatMe.Pets.PetProfile pet)
        {
            if (pet == null) return;
            // A different cat with its own model: reload the characters so the right one walks beside the owner.
            if (IsReady && pet.Kind == CatMe.Pets.Species.Cat && !string.IsNullOrEmpty(pet.modelPath) && System.IO.File.Exists(pet.modelPath) && pet.modelPath != loadedCatModel)
                StartCoroutine(ReloadCharactersFromPhoneStorage());
            var next = CatMe.Pets.CompanionBehavior.For(pet.Kind);
            bool speciesChanged = behavior.Species != next.Species;
            behavior = next;
            if (speciesChanged || (pet.Kind == CatMe.Pets.Species.Dog && dogModel == null)) ApplyCompanionModel();
        }

        private void ApplyCompanionModel()
        {
            if (catRoot == null) return;
            bool dog = behavior.Species == CatMe.Pets.Species.Dog;
            if (catAnimation != null) catAnimation.gameObject.SetActive(!dog);
            if (dog)
            {
                if (dogModel == null)
                {
                    var prefab = Resources.Load<GameObject>("Companions/Dog_corgi");
                    if (prefab == null) { behavior = new CatMe.Pets.CatBehavior(); if (catAnimation != null) catAnimation.gameObject.SetActive(true); Debug.LogWarning("[CatMe] Dog fixture missing at Resources/Companions/Dog_corgi; staying with the cat."); return; }
                    dogModel = Instantiate(prefab, catRoot); dogModel.name = "DogCompanion"; dogModel.transform.localPosition = Vector3.zero; dogModel.transform.localRotation = Quaternion.identity;
                    var bounds = new Bounds(); bool any = false;
                    foreach (var r in dogModel.GetComponentsInChildren<Renderer>()) { if (!any) { bounds = r.bounds; any = true; } else bounds.Encapsulate(r.bounds); }
                    if (any && bounds.size.y > .001f) dogModel.transform.localScale *= 0.42f / bounds.size.y;
                    foreach (var a in dogModel.GetComponentsInChildren<Animator>(true)) a.applyRootMotion = false;
                    dogModel.AddComponent<DogAnimDriver>();
                }
                dogModel.SetActive(true);
                catWalkState = null;
            }
            else
            {
                if (dogModel != null) dogModel.SetActive(false);
                catWalkState = FirstState(catAnimation);
            }
        }

        /// <summary>Companion walks to a point and waits or leads there on its own. Used when it rains near the tent.</summary>
        public void SetLead(Vector3 target, bool hold) { leadTarget = target; leadActive = true; leadHold = hold; }
        public void ClearLead() { leadActive = false; leadHold = false; }
        public bool LeadActive => leadActive;
        public bool CatIsIdle => !calling && !fetching && !catHold;
        public bool DidMeow { get; private set; }
        public bool DidThrowBall { get; private set; }
        public bool DidCatChaseBall { get; private set; }

        private async void Start()
        {
            try
            {
                Application.targetFrameRate = 60;
                CreateCameraAndLight();
                CreateHud();
                CreateGarden();
                groundCollider = GameObject.Find("Gravel clearing and moss floor")?.GetComponent<Collider>();
                GroundProbe.LegacyGround = groundCollider;
                audioSource = gameObject.AddComponent<AudioSource>();
                audioSource.mute = PlayerPrefs.GetInt("CatMe.SoundEnabled", 1) == 0;
                humanRoot = new GameObject("HumanPlayer").transform;
                humanRoot.position = Grounded(new Vector3(0f, 0f, -2.15f));
                catRoot = new GameObject("CatCompanion").transform;
                catRoot.position = Grounded(new Vector3(1f, 0f, -1.7f));
                companionCamera = gardenCamera.gameObject.GetComponent<CompanionCamera>() ?? gardenCamera.gameObject.AddComponent<CompanionCamera>();
                companionCamera.Initialize(humanRoot, catRoot);
                var ballPrefab = Resources.Load<GameObject>(GardenAssets + "/RollingFetchBall");
                if(ballPrefab == null) throw new InvalidOperationException("Build the garden to create the rolling-ball prefab.");
                ball = Instantiate(ballPrefab).transform;
                ball.name = "FetchBall";
                ball.gameObject.SetActive(false);
                await LoadCharacters();
                journey = gameObject.AddComponent<JourneyExperience>();
                journey.SessionMode = CatMe.Session.SessionDirector.PlacesAvailable;
                journey.Initialize(this, humanRoot, catRoot, gardenCamera.GetComponent<Camera>(), canvasRoot, status);
                if (journey.SessionMode)
                {
                    session = gameObject.AddComponent<CatMe.Session.SessionDirector>();
                    session.Initialize(this, canvasRoot);
                }
            }
            catch (Exception exception)
            {
                Debug.LogException(exception, this);
                SetStatus("Garden setup failed: " + exception.Message);
            }
        }

        private void Update()
        {
            appFlow?.Tick(Time.unscaledDeltaTime);
            ReadMovement();
            if (inputBlocked && scriptedWalk.sqrMagnitude < 0.01f) { moveInput = Vector2.zero; touchMoveInput = Vector2.zero; return; }
            if (humanRoot != null)
            {
                Vector2 combinedInput = Vector2.ClampMagnitude(moveInput + touchMoveInput, 1f);
                Vector3 movement = CameraRelative(combinedInput);
                if (scriptedWalk.sqrMagnitude > 0.01f) movement = scriptedWalk;
                if (ownerFrozen) movement = Vector3.zero;
                if (movement.sqrMagnitude > 1f) movement.Normalize();
                float ownerWalkSpeed = follow && catRoot != null && Vector3.Distance(catRoot.position,humanRoot.position)>5f ? .55f : 1.1f;
                Vector3 beforeMove = humanRoot.position;
                MoveGrounded(humanRoot, movement * (ownerWalkSpeed * Time.deltaTime), 0.24f, CompanionScaleStandards.OwnerHeightMeters);
                OwnerMoved?.Invoke(Vector3.Distance(beforeMove, humanRoot.position), scriptedWalk.sqrMagnitude < .01f && combinedInput.sqrMagnitude > .01f);
                if (firstGardenStage == 3) firstWalkDistance += movement.magnitude * (ownerWalkSpeed * Time.deltaTime);
                if (movement.sqrMagnitude > 0.01f)
                {
                    if (callingHuman != null && callingHuman.activeSelf)
                    {
                        callingHuman.SetActive(false);
                        walkingHuman.SetActive(true);
                        calling = false;
                        if (humanWalkState != null) humanWalkAnimation.Play(humanWalkState.name);
                    }
                    humanRoot.rotation = Quaternion.Slerp(humanRoot.rotation, Quaternion.LookRotation(movement), 10f * Time.deltaTime);
                    if (humanWalkState != null) humanWalkState.speed = Mathf.Clamp(movement.magnitude, 0.5f, 1.2f);
                }
                if (humanWalkState != null) humanWalkState.enabled = !calling && movement.sqrMagnitude > 0.01f;
            }

            if (firstGardenStage == 3 && firstWalkDistance >= 3.4f)
            {
                firstGardenStage = 4;
                appFlow?.OnWalkTogetherComplete();
            }

            if (catRoot != null) UpdateCat();
            if (ballInFlight) UpdateBall();
        }

        private void LateUpdate()
        {
            if (gardenCamera == null || humanRoot == null) return;
            // CompanionCamera owns the camera transform once it exists.
            if (companionCamera != null) return;

            Vector3 focus = humanRoot.position;
            float separation = 0f;
            if (catRoot != null)
            {
                Vector3 catOffset = catRoot.position - humanRoot.position;
                separation = catOffset.magnitude;
                // Keep the player weighted at center while letting the cat remain in frame.
                focus += Vector3.ClampMagnitude(catOffset * 0.28f, 1.15f);
            }
            focus.y += 1.12f;

            // Stay close enough for the human and cat to feel like companions.
            // The garden remains larger than the camera view and is discovered by moving.
            float framing = 1f + Mathf.Clamp(separation - 2.2f, 0f, 7f) * 0.008f;
            Vector3 desiredPosition = focus + new Vector3(0f, 0.82f, -2.75f) * framing;
            Vector3 cameraOffset = desiredPosition-focus;
            if (Physics.SphereCast(focus,.22f,cameraOffset.normalized,out RaycastHit cameraHit,cameraOffset.magnitude,~0,QueryTriggerInteraction.Ignore))
                desiredPosition = focus + cameraOffset.normalized * Mathf.Max(.65f,cameraHit.distance-.18f);
            float blend = 1f - Mathf.Exp(-(PlayerPrefs.GetInt("CatMe.ReduceMotion",0)==1 ? 3.1f : 5.6f) * Time.deltaTime);
            gardenCamera.position = Vector3.Lerp(gardenCamera.position, desiredPosition, blend);
            Quaternion look = Quaternion.LookRotation(focus + Vector3.up * 0.15f - gardenCamera.position);
            gardenCamera.rotation = Quaternion.Slerp(gardenCamera.rotation, look, blend);
        }

        private void ReadMovement()
        {
            if (inputBlocked) { moveInput = Vector2.zero; return; }
            if (Keyboard.current == null) return;
            float x = (Keyboard.current.dKey.isPressed || Keyboard.current.rightArrowKey.isPressed ? 1f : 0f)
                    - (Keyboard.current.aKey.isPressed || Keyboard.current.leftArrowKey.isPressed ? 1f : 0f);
            float z = (Keyboard.current.wKey.isPressed || Keyboard.current.fKey.isPressed || Keyboard.current.upArrowKey.isPressed ? 1f : 0f)
                    - (Keyboard.current.sKey.isPressed || Keyboard.current.downArrowKey.isPressed ? 1f : 0f);
            moveInput = new Vector2(x, z);
        }

        private async Task LoadCharacters()
        {
            string folder = Path.Combine(Application.streamingAssetsPath, GardenAssets);
            string savedCat = DeviceBridge.CatModelPath;
            string savedHuman = DeviceBridge.HumanModelPath;
            string bundledCat = Path.Combine(folder, "cat-walking.glb"), bundledHuman = Path.Combine(folder, "human-walking.glb");
            string catPath = File.Exists(savedCat) ? savedCat : bundledCat;
            // Each cat keeps its own downloaded model; the active cat's model wins over the single legacy file.
            var activePet = CatMe.Pets.PetRoster.Active;
            if (activePet != null && activePet.Kind == CatMe.Pets.Species.Cat && !string.IsNullOrEmpty(activePet.modelPath) && File.Exists(activePet.modelPath)) catPath = activePet.modelPath;
            string humanPath = File.Exists(savedHuman) ? savedHuman : bundledHuman;
            // A generated person has only a walk clip, so the call gesture exists only for the bundled person.
            // Loading the same large person model a second time would only double its memory.
            string callPath = File.Exists(savedHuman) ? null : Path.Combine(folder, "human-call.glb");
            // Provider pixel framing and authored GLB units vary, so enforce one shared pair scale after import.
            try { catAnimation = await LoadCharacter(catPath, catRoot, CompanionScaleStandards.CatHeightMeters, true); }
            catch (Exception error) when (catPath != bundledCat)
            {
                Debug.LogWarning("[WornTag][Garden] downloaded pet model failed: " + error.Message);
                DestroyChildren(catRoot); TryDelete(catPath);
                if (activePet != null && activePet.modelPath == catPath) { activePet.modelPath = null; CatMe.Pets.PetRoster.SaveChanges(); }
                catPath = bundledCat;
                catAnimation = await LoadCharacter(catPath, catRoot, CompanionScaleStandards.CatHeightMeters, true);
                appFlow?.ShowModelLoadFailure(activePet?.name ?? "your pet", () => appFlow.RetryPetModel(activePet));
            }
            loadedCatModel = catPath;
            catWalkState = FirstState(catAnimation);
            walkingHuman = new GameObject("WalkingHuman");
            walkingHuman.transform.SetParent(humanRoot, false);
            try { humanWalkAnimation = await LoadCharacter(humanPath, walkingHuman.transform, CompanionScaleStandards.OwnerHeightMeters, true); }
            catch (Exception error) when (humanPath != bundledHuman)
            {
                Debug.LogWarning("[WornTag][Garden] downloaded person model failed: " + error.Message);
                DestroyChildren(walkingHuman.transform); TryDelete(humanPath);
                humanPath = bundledHuman;
                humanWalkAnimation = await LoadCharacter(humanPath, walkingHuman.transform, CompanionScaleStandards.OwnerHeightMeters, true);
                appFlow?.ShowModelLoadFailure("you", appFlow.RetryOwnerModel);
            }
            humanWalkState = FirstState(humanWalkAnimation);
            humanHand = FindHumanHand(walkingHuman.transform);
            callingHuman = new GameObject("CallingHuman");
            callingHuman.transform.SetParent(humanRoot, false);
            humanCallAnimation = callPath == null ? null : await LoadCharacter(callPath, callingHuman.transform, CompanionScaleStandards.OwnerHeightMeters, false);
            sitState = FindState(humanCallAnimation, "Chair_Sit_Idle_F");
            beckonState = FindState(humanCallAnimation, "Call_Gesture");
            if (sitState != null) sitState.wrapMode = WrapMode.Loop;
            callingHuman.SetActive(false);
            if (humanWalkState != null) { humanWalkState.wrapMode = WrapMode.Loop; humanWalkAnimation.Play(humanWalkState.name); }
            if (catWalkState != null) catWalkState.wrapMode = WrapMode.Loop;
            IsReady = catWalkState != null && humanWalkState != null;
            SetActivePet(CatMe.Pets.PetRoster.Active);
            SetStatus("Drag to walk. " + CatMe.Pets.PetRoster.NameForText(true) + " is right beside you.");
            Debug.Log($"[CatMe][Garden] Active local companions loaded | cat={Path.GetFileName(catPath)} | human={Path.GetFileName(humanPath)}.");
        }

        public IEnumerator ReloadCharactersFromPhoneStorage()
        {
            if (catRoot != null) DestroyChildren(catRoot);
            if (humanRoot != null) DestroyChildren(humanRoot);
            catAnimation = null; humanWalkAnimation = null; humanCallAnimation = null;
            catWalkState = null; humanWalkState = null; sitState = null; beckonState = null;
            walkingHuman = null; callingHuman = null; humanHand = null;
            follow = false; calling = false; fetching = false;
            yield return null;
            Task load = LoadCharacters();
            while (!load.IsCompleted) yield return null;
            if (load.IsFaulted)
            {
                Debug.LogException(load.Exception, this);
                SetStatus("Your new character was saved, but the garden could not reload it yet.");
            }
        }

        private static void DestroyChildren(Transform root)
        {
            for (int i = root.childCount - 1; i >= 0; i--) Destroy(root.GetChild(i).gameObject);
        }

        private static void TryDelete(string path)
        {
            try { if (!string.IsNullOrEmpty(path) && File.Exists(path)) File.Delete(path); }
            catch (Exception error) { Debug.LogWarning("[WornTag][Garden] could not remove a bad model: " + error.Message); }
        }

        private async Task<Animation> LoadCharacter(string path, Transform parent, float targetHeight, bool play)
        {
            if (!File.Exists(path)) throw new FileNotFoundException("Garden character asset is missing", path);
            GltfImport import = new GltfImport();
            bool loaded = await import.Load(new Uri(path).AbsoluteUri, new ImportSettings { AnimationMethod = AnimationMethod.Legacy, GenerateMipMaps = true });
            if (!loaded) throw new InvalidOperationException("glTFast could not load " + Path.GetFileName(path));
            GameObject loadedRoot = new GameObject(Path.GetFileNameWithoutExtension(path));
            loadedRoot.transform.SetParent(parent, false);
            var instantiator = new GameObjectInstantiator(import, loadedRoot.transform, settings: new InstantiationSettings { SceneObjectCreation = SceneObjectCreation.Always, SkinUpdateWhenOffscreen = true });
            if (!await import.InstantiateMainSceneAsync(instantiator)) throw new InvalidOperationException("glTFast could not instantiate " + Path.GetFileName(path));
            Transform scene = instantiator.SceneTransform;
            Bounds bounds = RenderBounds(scene);
            if (bounds.size.y <= 0.001f) throw new InvalidOperationException("Character has no visible model bounds: " + path);
            loadedRoot.transform.localScale = Vector3.one * CompanionScaleStandards.ScaleForMeasuredHeight(bounds.size.y, targetHeight);
            bounds = RenderBounds(scene);
            scene.position += new Vector3(parent.position.x - bounds.center.x, parent.position.y - bounds.min.y, parent.position.z - bounds.center.z);
            Animation animation = scene.GetComponent<Animation>() ?? scene.GetComponentInChildren<Animation>(true);
            if (animation == null) animation = scene.gameObject.AddComponent<Animation>();
            AnimationClip[] clips = import.GetAnimationClips() ?? Array.Empty<AnimationClip>();
            foreach (AnimationClip clip in clips) if (clip != null && animation.GetClip(clip.name) == null) animation.AddClip(clip, clip.name);
            animation.playAutomatically = false;
            if (play && clips.Length > 0) animation.Play(clips[0].name);
            return animation;
        }

        private void UpdateCat()
        {
            if (catHold) { if (catWalkState != null) catWalkState.enabled = false; return; }
            if (fetching && Time.time - ballStartedAt > 18f)
            {
                fetching = ballInFlight = false;
                ball.gameObject.SetActive(false);
                follow = true;
                ballRejoining = false; BallFinished?.Invoke(false);
                SetStatus("The ball is ready to try again.");
            }
            if (calling && Time.time >= callStarted + 9f) { calling = false; CallUnavailable?.Invoke(); }
            if (ballCarried && ball != null)
            {
                // Held in the mouth: just ahead of the pet's chest, at muzzle height for its size.
                ball.position = catRoot.position + catRoot.forward * .24f + Vector3.up * carryHeight;
            }
            if (ballRejoining && Vector3.Distance(catRoot.position, humanRoot.position) < 1.8f)
            {
                ballRejoining = false;
                if (ballCarried && ball != null)
                {
                    ballCarried = false;
                    Vector3 feet = humanRoot.position + humanRoot.forward * .55f;
                    ball.position = Grounded(feet) + Vector3.up * .12f;
                    CatMe.Pets.PetSounds.Play(audioSource, behavior.Species, CatMe.Pets.PetCue.Hello, .6f);
                    SetStatus(behavior.BroughtBall(CatMe.Pets.PetRoster.NameForText(true)));
                    StartCoroutine(HideBallLater(1.6f));
                }
                BallFinished?.Invoke(true);
            }
            else if (ballRejoining && Time.time - ballStartedAt > 25f)
            { ballRejoining = false; if (ballCarried) { ballCarried = false; StartCoroutine(HideBallLater(.1f)); } BallFinished?.Invoke(false); }
            if (fetching && !ball.gameObject.activeSelf) fetching = false;
            if (leadActive && !calling && !fetching)
            {
                // Pet-led moment: walk to the lead point; hold in place (looking back) when the owner lags.
                catTarget = leadHold ? catRoot.position : Grounded(leadTarget);
                if (leadHold) { Vector3 back = humanRoot.position - catRoot.position; back.y = 0f; if (back.sqrMagnitude > .01f) catRoot.rotation = Quaternion.Slerp(catRoot.rotation, Quaternion.LookRotation(back), 3f * Time.deltaTime); }
            }
            else if (follow && !calling && !fetching && Time.time < curiosityUntil)
            {
                // Curious trot: walk to the point, then face it and wait. Owner movement is never affected.
                catTarget = Grounded(curiosityPlay ? PlayPoint(curiosityTarget) : curiosityTarget);
            }
            else if (follow && !calling && !fetching)
            {
                // Walk beside the owner (right side, switching only when something blocks it), not trailing behind.
                catTarget = Grounded(ClampGarden(SideSpot(behavior.Lateral, behavior.Ahead), 0.4f)); // slightly ahead and to the side of the owner
            }
            else if (fetching && ball.gameObject.activeSelf)
            {
                DidCatChaseBall = true;
                catTarget = Grounded(ClampGarden(ball.position, 0.25f));
                if (Vector3.Distance(catRoot.position, catTarget) < 0.3f && ball.position.y < GroundY(ball.position) + 0.34f)
                {
                    fetching = false; ballInFlight = false;
                    follow = true; ballRejoining = true;
                    string name = CatMe.Pets.PetRoster.NameForText(true);
                    if (behavior.Retrieves) { ballCarried = true; carryHeight = PetHeight() * .62f; CatMe.Pets.PetSounds.Play(audioSource, behavior.Species, CatMe.Pets.PetCue.Excited, .6f); }
                    else StartCoroutine(HideBallLater(2.5f));   // a cat pounces on it and leaves it lying there for a moment
                    SetStatus(behavior.GotBall(name));
                    if (firstGardenStage == 4)
                    {
                        firstGardenStage = 5;
                        appFlow?.OnFirstFetchComplete();
                    }
                }
            }
            else if (calling)
            {
                catTarget = Grounded(SideSpot(.75f, .7f));
                if (Vector3.Distance(catRoot.position, catTarget) < 0.34f && Time.time > callStarted + 2f)
                {
                    calling = false;
                    CatArrived?.Invoke();
                    if (UnityEngine.Random.value < .35f) Voice?.Say(CatMe.Pets.Moment.Arrived, 1.8f);
                    CatMe.Pets.PetSounds.Play(audioSource, behavior.Species, CatMe.Pets.PetCue.Hello, .75f); DidMeow = true;
                    SetStatus(behavior.Came(CatMe.Pets.PetRoster.NameForText(true))); CatMe.World.Haptics.Soft();
                    if (firstGardenStage == 1)
                    {
                        firstGardenStage = 2;
                        appFlow?.OnCatReunited();
                    }
                }
            }
            else if (Vector3.Distance(catRoot.position, humanRoot.position) > 4.2f)
            {
                catTarget = Grounded(ClampGarden(SideSpot(behavior.Lateral, behavior.Ahead), 0.4f)); // explore mode never strays beyond about 4 m
            }
            else if (Time.time > wanderAt)
            {
                // Wander beside or behind the owner, never out in front.
                Vector3 local = new Vector3(UnityEngine.Random.Range(-2.4f, 2.4f), 0f, UnityEngine.Random.Range(-.4f, 3.0f)); // explores close by, mostly ahead
                catTarget = Grounded(ClampGarden(humanRoot.position + humanRoot.rotation * local, .3f));
                wanderAt = Time.time + UnityEngine.Random.Range(2f, 4f);
            }

            Vector3 delta = catTarget - catRoot.position;
            delta.y = 0f;
            if (catWalkState != null) catWalkState.speed = fetching ? 1.15f : follow || calling ? 1f : .75f;
            if (delta.magnitude > 0.06f)
            {
                float catSpeed = fetching ? 1.2f : follow || calling ? 1.18f : 0.48f;
                if (!fetching && !calling)
                {
                    // Always stay near the owner and mostly ahead: if the cat is behind or level it hurries past, and the further away
                    // it is the faster it closes in.
                    Vector3 offset = catRoot.position - humanRoot.position; offset.y = 0f;
                    float lead = Vector3.Dot(offset, humanRoot.forward), distance = offset.magnitude;
                    float leash = behavior.Leash;
                    if (lead < .4f) catSpeed = Mathf.Max(catSpeed, 1.9f * behavior.Pace);
                    if (distance > leash) catSpeed = Mathf.Max(catSpeed, 2.2f * behavior.Pace);
                    else if (distance > leash * .67f) catSpeed = Mathf.Max(catSpeed, 1.6f * behavior.Pace);
                    if (curiosityPlay && Time.time < curiosityUntil) catSpeed = Mathf.Max(catSpeed, 1.5f + .35f * Mathf.Sin(Time.time * 5f)); // bouncy, walk clip only
                }
                // Longer strides on a bigger cat: play the walk clip slower per metre so the paws do not skate.
                if (catWalkState != null) catWalkState.speed = Mathf.Clamp(catSpeed / (1.18f * CompanionScaleStandards.CatScale), .55f, 1.7f);
                MoveGrounded(catRoot, delta.normalized * catSpeed * Time.deltaTime, 0.16f * CompanionScaleStandards.CatScale, CompanionScaleStandards.CatHeightMeters);
                catRoot.rotation = Quaternion.Slerp(catRoot.rotation, Quaternion.LookRotation(delta), 8f * Time.deltaTime);
                if (catWalkState != null) catWalkState.enabled = true;
            }
            else if (catWalkState != null) catWalkState.enabled = false;
            if (Time.time < catLookUntil && delta.magnitude < .35f)
            {
                Vector3 look = catLookPoint - catRoot.position; look.y = 0f;
                if (look.sqrMagnitude > .01f) catRoot.rotation = Quaternion.Slerp(catRoot.rotation, Quaternion.LookRotation(look), 3f * Time.deltaTime);
            }
        }

        // Circles a point (a butterfly) at a steady rate; the walk clip plays throughout, so "play" is only motion.
        private Vector3 PlayPoint(Vector3 centre)
        {
            float a = Time.time * 1.7f, r = behavior.PlayRadius;
            return centre + new Vector3(Mathf.Cos(a), 0f, Mathf.Sin(a)) * r;
        }

        private void UpdateBall()
        {
            ballVelocity.y -= 9.8f * Time.deltaTime;
            Vector3 travel = ballVelocity * Time.deltaTime;
            // Place scenes have real walls and water edges; the ball bounces off them instead of leaving the play area.
            if (unboundedWorld && travel.sqrMagnitude > 1e-8f && Physics.SphereCast(ball.position, .1f, travel.normalized, out RaycastHit wall, travel.magnitude + .02f, ~0, QueryTriggerInteraction.Ignore) && Vector3.Dot(wall.normal, Vector3.up) < .6f)
            {
                ballVelocity = Vector3.Reflect(ballVelocity, wall.normal) * .35f;
                travel = ballVelocity * Time.deltaTime;
            }
            ball.position += travel;
            Vector3 horizontal = new Vector3(travel.x,0,travel.z);
            if(horizontal.sqrMagnitude > .000001f) ball.Rotate(Vector3.Cross(Vector3.up,horizontal.normalized), horizontal.magnitude / .12f * Mathf.Rad2Deg, Space.World);
            float floor = GroundY(ball.position) + 0.12f;
            if (ball.position.y < floor) { ball.position = new Vector3(ball.position.x, floor, ball.position.z); ballVelocity.y = Mathf.Abs(ballVelocity.y) > 0.7f ? ballVelocity.y * -0.27f : 0f; ballVelocity.x *= 0.84f; ballVelocity.z *= 0.84f; }
            if (!unboundedWorld && Mathf.Abs(ball.position.x) > gardenLimit.x - 0.15f) { ball.position = new Vector3(Mathf.Clamp(ball.position.x, -gardenLimit.x + 0.15f, gardenLimit.x - 0.15f), ball.position.y, ball.position.z); ballVelocity.x = 0f; }
            if (!unboundedWorld && (ball.position.z < -5.85f || ball.position.z > gardenLimit.z - 0.15f)) { ball.position = new Vector3(ball.position.x, ball.position.y, Mathf.Clamp(ball.position.z, -5.85f, gardenLimit.z - 0.15f)); ballVelocity.z = 0f; }
            if (ball.position.y <= floor + 0.001f && ballVelocity.magnitude < 0.45f) { CatMe.World.Haptics.Bump(); ballInFlight = false; ballVelocity = Vector3.zero; }
        }

        public void ToggleFollow()
        {
            follow = !follow; fetching = false; calling = false;
            if (followActionLabel != null) followActionLabel.text = follow ? "Roam" : "Together";
            SetStatus(follow ? CatMe.Pets.PetRoster.NameForText(true) + " is following you." : CatMe.Pets.PetRoster.NameForText(true) + " is exploring the garden.");
        }

        public void CallCat()
        {
            if (!IsReady || inputBlocked || humanRoot == null || catRoot == null || Time.time < nextCallAt) { CallUnavailable?.Invoke(); return; }
            nextCallAt = Time.time + 2f;
            CallAccepted?.Invoke();
            if (!catHidden && Vector3.Distance(catRoot.position,humanRoot.position)<.7f)
            {
                CatMe.Pets.PetSounds.Play(audioSource, behavior.Species, CatMe.Pets.PetCue.Hello, .55f);
                CatArrived?.Invoke();
                SetStatus(CatMe.Pets.PetRoster.NameForText(true) + " is right here beside you.");
                return;
            }
            if (catHidden)
            {
                catHidden = false;
                catRoot.gameObject.SetActive(true);
                catRoot.position = ClampGarden(humanRoot.position + humanRoot.forward * 2.2f + Vector3.right * 0.6f, 0.3f);
            }
            follow = false; fetching = false; calling = true; callStarted = Time.time;
            DidCallGesturePlay = false;
            catTarget = humanRoot.position;
            bool hasCompatibleCallClip = beckonState != null;
            if (hasCompatibleCallClip)
            {
                if (walkingHuman != null) walkingHuman.SetActive(false);
                if (callingHuman != null) callingHuman.SetActive(true);
                if (humanCallAnimation != null) { beckonState.wrapMode = WrapMode.Once; humanCallAnimation.Play(beckonState.name); DidCallGesturePlay = true; }
            }
            SetStatus("You call " + CatMe.Pets.PetRoster.NameForText() + ".");
        }

        // Height of the visible pet (cat model or dog), measured once when it picks up the ball.
        private float PetHeight()
        {
            var renderers = catRoot.GetComponentsInChildren<Renderer>(false);
            if (renderers.Length == 0) return .4f;
            Bounds b = renderers[0].bounds; foreach (var r in renderers) b.Encapsulate(r.bounds);
            return Mathf.Clamp(b.size.y, .2f, 1f);
        }

        private IEnumerator HideBallLater(float seconds)
        {
            int thrown = throwCount;
            yield return new WaitForSeconds(seconds);
            if (thrown == throwCount && !ballInFlight && !fetching && !ballCarried && ball != null) ball.gameObject.SetActive(false);
        }

        public void ThrowBall()
        {
            if (!IsReady || inputBlocked || humanRoot == null || ball == null) return;
            if (ballInFlight || fetching || ballRejoining) { SetStatus("Let this ball settle first."); return; }
            if (journey != null && !journey.CanThrowBall(humanRoot.position)) { SetStatus("The ball is for open ground."); return; }
            if (firstGardenStage == 4) firstGardenStage = 4;
            calling = false; fetching = true; ballCarried = false; throwCount++;
            ballStartedAt = Time.time;
            DidThrowBall = true;
            if (callingHuman != null) callingHuman.SetActive(false);
            if (walkingHuman != null) walkingHuman.SetActive(true);
            ball.position = humanRoot.position + Vector3.up * 0.45f + humanRoot.forward * 0.38f;
            ball.gameObject.SetActive(true);
            ballVelocity = humanRoot.forward * 3.3f + Vector3.up * 3.3f;
            ballInFlight = true;
            BallReleased?.Invoke();
            SetStatus("The ball rolls ahead. " + CatMe.Pets.PetRoster.NameForText(true) + " is chasing it.");
        }

        public void SetMove(Vector2 direction) { if (!inputBlocked) touchMoveInput = direction; }
        public void ClearMovement() { moveInput = touchMoveInput = Vector2.zero; }
        public void SetInputBlocked(bool blocked) { inputBlocked = blocked; if (blocked) ClearMovement(); }
        private void OnApplicationPause(bool paused)
        {
            if (!paused) return;
            ClearMovement();
            appFlow?.SaveProgress();
            journey?.SaveNow();
            if (Application.isMobilePlatform && pauseSheet != null && !pauseSheet.activeSelf) OpenPause();
        }
        private void OnDestroy() { appFlow?.SaveProgress(); appFlow?.Dispose(); }
        public void SetAppStatus(string value) => SetStatus(value);
        internal void SetGameplayHudVisible(bool visible)
        {
            gameplayHudVisible = visible;
            if (!visible && statusPanel != null) statusPanel.SetActive(false);
        }
        public void SetSoundEnabled(bool enabled) { if (audioSource != null) audioSource.mute = !enabled; journey?.SetSoundEnabled(enabled); }
        public void SetReducedMotion(bool enabled) => journey?.SetReducedMotion(enabled);

        public void BeginFirstGardenSession(bool visualGuidance)
        {
            if (catRoot == null || humanRoot == null) return;
            firstGardenStage = 1;
            firstWalkDistance = 0f;
            follow = false;
            calling = false;
            fetching = false;
            catHidden = true;
            Vector3 hideSpot = ClampGarden(humanRoot.position + new Vector3(1.8f, 0f, 2.2f), 0.3f);
            catRoot.position = hideSpot;
            catRoot.gameObject.SetActive(false);
            SetStatus(visualGuidance ? CatMe.Pets.PetRoster.NameForText(true) + " is nearby. Follow the flower path and call when you are ready." : CatMe.Pets.PetRoster.NameForText(true) + " is nearby, enjoying the garden.");
        }

        public void SkipFindCat()
        {
            if (catRoot == null || humanRoot == null) return;
            catHidden = false;
            catRoot.gameObject.SetActive(true);
            catRoot.position = ClampGarden(humanRoot.position + humanRoot.forward * 1.15f + Vector3.right * 0.45f, 0.3f);
            catTarget = catRoot.position;
            calling = false;
            firstGardenStage = 2;
            SetStatus("You found each other. Take a moment together.");
        }

        public void BeginWalkTogether()
        {
            firstGardenStage = 3;
            firstWalkDistance = 0f;
            follow = true;
            SetStatus("Walk a little way together. " + CatMe.Pets.PetRoster.NameForText(true) + " will catch up.");
        }

        public void EndFirstGardenSession()
        {
            firstGardenStage = 0;
            if (catHidden) SkipFindCat();
            firstGardenStage = 0;
        }
        public bool IsReady { get; private set; }
        public float CallSitDuration => sitState == null ? 0f : sitState.length;
        public float CatDistanceToHuman => catRoot == null || humanRoot == null ? float.MaxValue : Vector3.Distance(catRoot.position, humanRoot.position);
        public Vector3 HumanPosition => humanRoot == null ? Vector3.zero : humanRoot.position;
        public Vector3 CatPosition => catRoot == null ? Vector3.zero : catRoot.position;

        private void CreateGarden()
        {
            // Place scenes supply the world once they are in Build Settings. The shell scene still holds a saved copy of the
            // old route, whose ground would draw over the new terrain and leave characters sunk into it, so switch it off.
            if (CatMe.Session.SessionDirector.PlacesAvailable)
            {
                var legacy = GameObject.Find("GardenEnvironment");
                if (legacy != null) legacy.SetActive(false);
                return;
            }
            if (GameObject.Find("GardenEnvironment") != null) return;
            var prefab = Resources.Load<GameObject>("CompanionGarden/GardenEnvironment");
            if (prefab == null) throw new InvalidOperationException("Build the first garden using CatMe > Garden > Build First Garden.");
            Instantiate(prefab).name = "GardenEnvironment";
        }

        private float catSide = 1f;

        // A spot beside the owner for the cat. Keeps its side until that spot is blocked (fence, wall, rock), then swaps.
        private Vector3 SideSpot(float lateral, float ahead)
        {
            Vector3 Spot(float side) => humanRoot.position + humanRoot.right * (side * lateral) + humanRoot.forward * ahead;
            Vector3 preferred = Spot(catSide);
            if (SpotBlocked(preferred))
            {
                Vector3 other = Spot(-catSide);
                if (!SpotBlocked(other)) { catSide = -catSide; return other; }
                return humanRoot.position + humanRoot.forward * .5f;
            }
            return preferred;
        }

        private static bool SpotBlocked(Vector3 point) =>
            Physics.CheckSphere(GroundProbe.Snap(point) + Vector3.up * 0.3f, 0.2f, GroundProbe.ObstacleMask, QueryTriggerInteraction.Ignore);

        // Movement is relative to the camera's rest yaw, latched while the stick is held so a steady push walks straight.
        private Vector3 CameraRelative(Vector2 input)
        {
            if (input.sqrMagnitude < 0.0144f) { movementLatched = false; return Vector3.zero; }
            if (!movementLatched) { movementYaw = companionCamera != null ? companionCamera.ReferenceYaw : 0f; movementLatched = true; }
            Quaternion yaw = Quaternion.Euler(0f, movementYaw, 0f);
            float magnitude = Mathf.Pow(Mathf.Clamp01((input.magnitude - 0.12f) / 0.88f), 1.4f);
            return yaw * new Vector3(input.x, 0f, input.y).normalized * magnitude;
        }

        /// <summary>Lets the session place pair anywhere and removes the legacy garden clamp.</summary>
        public void SetUnboundedWorld(bool value) { unboundedWorld = value; }
        private bool catHold, ownerFrozen;
        private Vector3 catLookPoint; private float catLookUntil;
        /// <summary>The cat stops and turns its head (body) toward a point for a few seconds, e.g. when a dog visits.</summary>
        public void SetCatLookAt(Vector3 point, float seconds) { catLookPoint = point; catLookUntil = Time.time + seconds; }
        /// <summary>Stops the owner walking (house room) while the cat, ball and UI keep working.</summary>
        public void SetOwnerFrozen(bool value) { ownerFrozen = value; }
        /// <summary>Moves only the owner (used for sitting and the tent room).</summary>
        public void PlaceOwnerOnly(Vector3 position, float yawDegrees) { if (humanRoot != null) { humanRoot.SetPositionAndRotation(position, Quaternion.Euler(0f, yawDegrees, 0f)); movementLatched = false; } }
        public void SetOwnerVisible(bool visible) { if (humanRoot == null) return; foreach (var r in humanRoot.GetComponentsInChildren<Renderer>(true)) r.enabled = visible; }
        /// <summary>Puts the cat at a spot and keeps it still in its neutral pose until released.</summary>
        public void HoldCat(Vector3 position, float yawDegrees)
        {
            if (catRoot == null) return;
            catHold = true; catRoot.gameObject.SetActive(true);
            catRoot.SetPositionAndRotation(position, Quaternion.Euler(0f, yawDegrees, 0f));
            if (catWalkState != null) catWalkState.enabled = false;
        }
        public void ReleaseCat() { catHold = false; }
        /// <summary>Keeps the cat where it stands (petting) without moving it.</summary>
        public void SetCatStill(bool still) { catHold = still; if (still && catWalkState != null) catWalkState.enabled = false; }
        /// <summary>Puts the cat at a spot without holding it; it follows and plays normally.</summary>
        public void PlaceCatBeside(Vector3 position, float yawDegrees) { if (catRoot == null) return; catHold = false; catRoot.gameObject.SetActive(true); catRoot.SetPositionAndRotation(position, Quaternion.Euler(0f, yawDegrees, 0f)); catTarget = position; }
        public bool CatIsFollowing => follow && !calling && !fetching && Time.time >= curiosityUntil;
        public void SetCuriosity(Vector3 target, float seconds, bool play = false) { curiosityTarget = target; curiosityUntil = seconds > 0f ? Time.time + seconds : 0f; curiosityPlay = play && seconds > 0f; }
        public void SetScriptedWalk(Vector3 worldDirection) { scriptedWalk = worldDirection; }
        public CompanionCamera CameraRig => companionCamera;
        public Transform OwnerTransform => humanRoot;
        public Transform CatTransform => catRoot;
        public bool IsInputBlocked => inputBlocked;

        /// <summary>Puts the owner at a pose and the cat just behind-left, both on the ground. Used for place entries.</summary>
        public void PlacePair(Vector3 ownerPosition, float yawDegrees)
        {
            if (humanRoot == null || catRoot == null) return;
            Quaternion yaw = Quaternion.Euler(0f, yawDegrees, 0f);
            humanRoot.SetPositionAndRotation(Grounded(ownerPosition), yaw);
            catRoot.SetPositionAndRotation(Grounded(ownerPosition + yaw * new Vector3(.95f, 0f, .1f)), yaw);
            catRoot.gameObject.SetActive(true);
            catHidden = false;
            catTarget = catRoot.position;
            movementLatched = false;
            companionCamera?.Snap();
        }

        public void BeginConnectedOuting()
        {
            if (humanRoot == null || catRoot == null) return;
            firstGardenStage = 0;
            calling = fetching = false;
            catHidden = false;
            follow = true;
            if (followActionLabel != null) followActionLabel.text = "Roam";
            if (walkingHuman != null) walkingHuman.SetActive(true);
            if (callingHuman != null) callingHuman.SetActive(false);
            if (session != null) { catRoot.gameObject.SetActive(true); session.StartNewSession(); return; }
            humanRoot.position = Grounded(new Vector3(0f, 0f, -10f));
            catRoot.position = Grounded(new Vector3(-0.8f, 0f, -9.3f));
            catRoot.gameObject.SetActive(true);
            journey?.StartOuting();
        }

        public void RestoreJourneyPosition(float z)
        {
            if (humanRoot != null) humanRoot.position = Grounded(new Vector3(0f, 0f, z));
            if (catRoot != null) catRoot.position = Grounded(new Vector3(-0.8f, 0f, z - 0.65f));
        }

        public Vector3 SampledGroundPosition(Vector3 value) => Grounded(value);
        public Vector3 OwnerPosition => humanRoot == null ? Vector3.zero : humanRoot.position;
        public Vector3 CompanionPosition => catRoot == null ? Vector3.zero : catRoot.position;
        public float CurrentMoveMagnitude => Mathf.Clamp01(Vector2.ClampMagnitude(moveInput + touchMoveInput, 1f).magnitude);
        public void ResumeConnectedOuting()
        {
            if (session != null) { session.ResumeSession(); return; }
            if (journey == null) return;
            if (journey.IsActive) journey.ResumeOuting();
            else if (!journey.IsCompleted) BeginConnectedOuting();
        }

        private void CreateCameraAndLight()
        {
            Camera camera = Camera.main;
            if (camera == null) { var go = new GameObject("Main Camera", typeof(Camera), typeof(AudioListener)); camera = go.GetComponent<Camera>(); go.tag = "MainCamera"; }
            camera.transform.position = new Vector3(0f, 3.35f, -6.2f);
            camera.transform.rotation = Quaternion.Euler(21f, 0f, 0f);
            camera.fieldOfView = 39f;
            camera.clearFlags = CameraClearFlags.Skybox;
            var sunsetSky = Resources.Load<Material>("Skyboxes/Sky_Sunset");
            if (sunsetSky != null) RenderSettings.skybox = sunsetSky;
            else Debug.LogWarning("Stylized Skyboxes FREE sunset material was not found at Resources/Skyboxes/Sky_Sunset.");
            camera.backgroundColor = new Color(0.86f, 0.48f, 0.31f);
            RenderSettings.fog = true;
            RenderSettings.fogMode = FogMode.ExponentialSquared;
            RenderSettings.fogColor = camera.backgroundColor;
            RenderSettings.fogDensity = .011f;
            camera.allowHDR = true;
            // Place scenes carry a global post-processing volume (tone mapping, colour grade, bloom, vignette).
            camera.GetUniversalAdditionalCameraData().renderPostProcessing = true;
            gardenCamera = camera.transform;
            RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Trilight;
            RenderSettings.ambientSkyColor = new Color(0.83f, 0.56f, 0.43f);
            RenderSettings.ambientEquatorColor = new Color(0.70f, 0.52f, 0.39f);
            RenderSettings.ambientGroundColor = new Color(0.48f, 0.35f, 0.25f);
            RenderSettings.ambientIntensity = .8f;
            RenderSettings.ambientLight = new Color(0.88f, 0.63f, 0.46f);
            var sun = new GameObject("Garden sunlight", typeof(Light)).GetComponent<Light>();
            sun.type = LightType.Directional; sun.color = new Color(1f, 0.70f, 0.42f); sun.intensity = 1.15f;
            sun.shadows = LightShadows.Soft; sun.shadowStrength = 0.8f; sun.shadowResolution = UnityEngine.Rendering.LightShadowResolution.High;
            sun.shadowBias = 0.06f; sun.shadowNormalBias = 0.32f;
            sun.transform.rotation = Quaternion.Euler(18f, -32f, 0f);
            var fill = new GameObject("Garden sky fill", typeof(Light)).GetComponent<Light>();
            fill.type = LightType.Directional; fill.color = new Color(0.68f, 0.76f, 1f); fill.intensity = 0.08f;
            fill.shadows = LightShadows.None; fill.transform.rotation = Quaternion.Euler(30f, 145f, 0f);
        }

        private void CreateHud()
        {
            var eventSystem = FindAnyObjectByType<EventSystem>();
            if (eventSystem == null) { eventSystem = new GameObject("EventSystem", typeof(EventSystem), typeof(InputSystemUIInputModule)).GetComponent<EventSystem>(); }
            else if (eventSystem.GetComponent<InputSystemUIInputModule>() == null) eventSystem.gameObject.AddComponent<InputSystemUIInputModule>();
            var canvasObject = new GameObject("Garden HUD", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            Canvas canvas = canvasObject.GetComponent<Canvas>(); canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvasRoot = canvas.transform;
            CanvasScaler scaler = canvasObject.GetComponent<CanvasScaler>(); scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize; scaler.referenceResolution = new Vector2(1920, 1080); scaler.matchWidthOrHeight = 1f;
            statusPanel = new GameObject("Quiet garden hint", typeof(RectTransform), typeof(Image), typeof(CanvasGroup));
            statusPanel.transform.SetParent(canvas.transform, false);
            var statusRect = statusPanel.GetComponent<RectTransform>();
            statusRect.anchorMin = statusRect.anchorMax = new Vector2(0.5f, 1f);
            statusRect.anchoredPosition = new Vector2(0f, -74f);
            statusRect.sizeDelta = new Vector2(620f, 58f);
            CatMeUIStyle.ApplySurface(statusPanel.GetComponent<Image>(), CatMeUIStyle.CreamGlass);
            statusGroup = statusPanel.GetComponent<CanvasGroup>();
            statusGroup.alpha = 0f;
            status = MakeLabel(statusPanel.transform, "GardenStatus", "", Vector2.zero, Vector2.one, Vector2.zero, new Vector2(-32f, -12f), 20);
            status.color = CatMeUIStyle.Ink;
            CreateMoveJoystick(canvas.transform);
            // Cat button (opens the fan), Pause and Camera: round glass controls in the thumbs' reach.
            var catBtn = UiKit.IconButton(canvas.transform, "", Vector2.zero, ToggleCatTray, "Cat action entry");
            var catRect = (RectTransform)catBtn.transform; catRect.anchorMin = catRect.anchorMax = new Vector2(1f, 0f); catRect.sizeDelta = new Vector2(132f, 132f); catRect.anchoredPosition = new Vector2(-110f, 110f);
            var catInk = new Color(.153f, .137f, .129f, .96f);
            catBtn.GetComponent<Image>().color = catInk;
            catBtn.GetComponent<CatMeButtonMotion>()?.Tint(catBtn.GetComponent<Image>(), catInk);   // the press animation restores this colour, not the default paper
            UiKit.Icon(catBtn.transform, "paw", Vector2.zero, 74f, Color.white);
            var pauseBtn = UiKit.IconButton(canvas.transform, "pause", Vector2.zero, OpenPause, "Pause action");
            var pauseRect = (RectTransform)pauseBtn.transform; pauseRect.anchorMin = pauseRect.anchorMax = new Vector2(1f, 1f); pauseRect.anchoredPosition = new Vector2(-70f, -70f);
            var camera = UiKit.IconButton(canvas.transform, "camera", Vector2.zero, () => journey?.TakePhoto(), "Camera action");
            var camRect = (RectTransform)camera.transform; camRect.anchorMin = camRect.anchorMax = new Vector2(1f, 1f); camRect.anchoredPosition = new Vector2(-175f, -70f);
            BuildCatTray(canvas.transform);
            BuildPauseSheet(canvas.transform);
            device = gameObject.GetComponent<DeviceBridge>() ?? gameObject.AddComponent<DeviceBridge>();
            device.Initialize();
            appFlow = new CatMeAppFlow(canvas.transform, this, device);
            Voice = gameObject.GetComponent<CompanionVoice>() ?? gameObject.AddComponent<CompanionVoice>();
            Voice.Initialize(this, canvas.transform);
            SetStatus("Preparing your companions…");
        }

        private void BuildCatTray(Transform parent)
        {
            // The fan: three labelled round actions arcing out from the Cat button.
            catTray = new GameObject("Cat action tray", typeof(RectTransform));
            catTray.transform.SetParent(parent, false);
            RectTransform rect = catTray.GetComponent<RectTransform>();
            rect.anchorMin = rect.anchorMax = new Vector2(1f, 0f); rect.pivot = new Vector2(.5f, .5f); rect.sizeDelta = new Vector2(10f, 10f);
            rect.anchoredPosition = new Vector2(-110f, 110f);
            Button Fan(string name, string icon, string label, Vector2 pos, Action action)
            {
                var b = UiKit.Button(catTray.transform, "", pos, new Vector2(112f, 112f), CatMeUIStyle.PaperGlass, false, action, 30, name);
                var img = b.GetComponent<Image>(); img.sprite = UiKit.Circle; img.type = Image.Type.Simple;
                UiKit.Icon(b.transform, icon, Vector2.zero, 60f, CatMeUIStyle.Pine);
                var cap = UiKit.Label(b.transform, label, 28, Color.white, new Vector2(0f, -86f), new Vector2(200f, 40f), FontStyle.Bold, TextAnchor.MiddleCenter, "Caption");
                cap.gameObject.AddComponent<Shadow>().effectColor = new Color(0f, 0f, 0f, .7f);
                return b;
            }
            Fan("Ball action", "ball", "Ball", new Vector2(-185f, 20f), () => { CloseCatTray(); ThrowBall(); });
            var followButton = Fan("Follow action", "walk", "Roam", new Vector2(-150f, 190f), () => { CloseCatTray(); ToggleFollow(); });
            followActionLabel = followButton.transform.Find("Caption")?.GetComponent<Text>();
            Fan("Call action", "call", "Call", new Vector2(-10f, 230f), () => { CloseCatTray(); CallCat(); });
            catTray.SetActive(false);
        }

        private void ToggleCatTray()
        {
            if (catTray == null || pauseSheet.activeSelf) return;
            catTray.SetActive(!catTray.activeSelf);
            SetInputBlocked(catTray.activeSelf);
            if (catTray.activeSelf) catTray.transform.SetAsLastSibling();
        }

        private void CloseCatTray() { if (catTray != null) catTray.SetActive(false); SetInputBlocked(false); }

        private void BuildPauseSheet(Transform parent)
        {
            pauseSheet = new GameObject("Pause sheet", typeof(RectTransform), typeof(Image));
            pauseSheet.transform.SetParent(parent, false);
            var rect = pauseSheet.GetComponent<RectTransform>(); rect.anchorMin=Vector2.zero;rect.anchorMax=Vector2.one;rect.offsetMin=rect.offsetMax=Vector2.zero;
            pauseSheet.GetComponent<Image>().color = new Color(.03f,.06f,.05f,.75f);
            // Side sheet: five destinations and one way back. Starting a new outing and returning to the title are no longer offered here.
            var card = CatMeUIStyle.Card(pauseSheet.transform, "Pause card", new Vector2(640f, 900f), CatMeUIStyle.PaperGlass, CatMeUIStyle.Elevation.Floating);
            ((RectTransform)card.transform).anchoredPosition = new Vector2(580f, 0f);
            UiKit.Label(card.transform, "Paused", 54, CatMeUIStyle.Pine, new Vector2(-95f, 370f), new Vector2(380f, 80f), FontStyle.Bold, TextAnchor.MiddleLeft, "Title");
            void Tile(string name, string icon, string label, float x, float y, Action action) => UiKit.IconTile(card.transform, icon, label, new Vector2(x, y), new Vector2(280f, 180f), action, name);
            Tile("Your pets", "pets", "Your pets", -150f, 205f, () => { ResumePause(); appFlow?.OpenPets(); });
            Tile("Pet store", "store", "Store", 150f, 205f, () => { ResumePause(); appFlow?.OpenStore(); });
            Tile("Photo album", "album", "Photo album", -150f, 10f, () => { ResumePause(); journey?.OpenPhotoAlbum(); });
            Tile("Your companions", "account", "Account", 150f, 10f, () => { ResumePause(); appFlow?.OpenAccount(); });
            Tile("Settings", "gear", "Settings", -150f, -185f, () => { ResumePause(); appFlow?.OpenSettingsFromPause(); });
            UiKit.NestedButton(card.transform, "Keep exploring", new Vector2(0f, -330f), 560f, CatMeUIStyle.Pine, ResumePause, 36, "Keep exploring");
            pauseSheet.SetActive(false);
        }

        private void OpenPause() { if (pauseSheet == null) return; replayArmed=false;if(replayActionLabel!=null)replayActionLabel.text="Start new outing";CloseCatTray(); ClearMovement(); pauseSheet.SetActive(true); pauseSheet.transform.SetAsLastSibling(); SetInputBlocked(true); journey?.SaveNow(); Time.timeScale=0f; }
        private void ResumePause() { if (pauseSheet != null) pauseSheet.SetActive(false); replayArmed=false;Time.timeScale=1f; SetInputBlocked(false); ClearMovement(); }
        private void ConfirmReplay() { if(!replayArmed){replayArmed=true;replayActionLabel.text="Tap again to restart outing";return;}ResumePause();BeginConnectedOuting(); }

        private void CreateMoveJoystick(Transform parent)
        {
            GameObject baseObject = new GameObject("Movement joystick", typeof(RectTransform), typeof(Image));
            baseObject.transform.SetParent(parent, false);
            RectTransform baseRect = baseObject.GetComponent<RectTransform>();
            baseRect.anchorMin = baseRect.anchorMax = Vector2.zero;
            baseRect.pivot = new Vector2(0.5f, 0.5f);
            baseRect.anchoredPosition = new Vector2(172f, 170f);
            baseRect.sizeDelta = new Vector2(244f, 244f);
            Image baseImage = baseObject.GetComponent<Image>();
            baseImage.sprite = GetJoystickSprite(false);
            baseImage.color = new Color(0.10f, 0.18f, 0.15f, 0.50f);
            baseImage.alphaHitTestMinimumThreshold = 0.04f;

            GameObject track = new GameObject("Joystick track", typeof(RectTransform), typeof(Image));
            track.transform.SetParent(baseObject.transform, false);
            RectTransform trackRect = track.GetComponent<RectTransform>();
            trackRect.anchorMin = trackRect.anchorMax = new Vector2(0.5f, 0.5f);
            trackRect.sizeDelta = new Vector2(222f, 222f);
            Image trackImage = track.GetComponent<Image>(); trackImage.sprite = GetJoystickSprite(true);
            trackImage.color = new Color(0.91f, 0.94f, 0.86f, 0.54f); trackImage.raycastTarget = false;

            GameObject knobRim = new GameObject("Joystick thumb rim", typeof(RectTransform), typeof(Image));
            knobRim.transform.SetParent(baseObject.transform, false);
            RectTransform rimRect = knobRim.GetComponent<RectTransform>();
            rimRect.anchorMin = rimRect.anchorMax = new Vector2(0.5f, 0.5f);
            rimRect.sizeDelta = new Vector2(94f, 94f);
            Image rimImage = knobRim.GetComponent<Image>(); rimImage.sprite = GetJoystickSprite(false);
            rimImage.color = new Color(0.91f, 0.94f, 0.86f, 0.44f); rimImage.raycastTarget = false;

            GameObject knob = new GameObject("Joystick thumb", typeof(RectTransform), typeof(Image));
            knob.transform.SetParent(baseObject.transform, false);
            RectTransform knobRect = knob.GetComponent<RectTransform>();
            knobRect.anchorMin = knobRect.anchorMax = new Vector2(0.5f, 0.5f);
            knobRect.sizeDelta = new Vector2(74f, 74f);
            Image knobImage = knob.GetComponent<Image>(); knobImage.sprite = GetJoystickSprite(false);
            knobImage.color = new Color(0.94f, 0.96f, 0.90f, 0.90f); knobImage.raycastTarget = false;

            baseObject.AddComponent<MobileGardenJoystick>().Initialize(this, baseRect, knobRect, 78f);
        }

        private static Sprite GetJoystickSprite(bool ring)
        {
            if (!ring && joystickDiscSprite != null) return joystickDiscSprite;
            if (ring && joystickRingSprite != null) return joystickRingSprite;
            const int size = 128;
            var texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
            texture.name = ring ? "Garden joystick ring" : "Garden joystick disc";
            texture.filterMode = FilterMode.Bilinear; texture.wrapMode = TextureWrapMode.Clamp;
            var pixels = new Color32[size * size];
            for (int y = 0; y < size; y++)
            for (int x = 0; x < size; x++)
            {
                float dx = (x + 0.5f - size * 0.5f) / (size * 0.5f);
                float dy = (y + 0.5f - size * 0.5f) / (size * 0.5f);
                float distance = Mathf.Sqrt(dx * dx + dy * dy);
                float alpha = ring
                    ? Mathf.Clamp01((0.96f - distance) * 20f) * Mathf.Clamp01((distance - 0.86f) * 20f)
                    : Mathf.Clamp01((1f - distance) * 24f);
                pixels[y * size + x] = new Color32(255, 255, 255, (byte)Mathf.RoundToInt(alpha * 255f));
            }
            texture.SetPixels32(pixels); texture.Apply(false, false);
            Sprite sprite = Sprite.Create(texture, new Rect(0f, 0f, size, size), new Vector2(0.5f, 0.5f), 100f);
            sprite.name = ring ? "Garden joystick ring" : "Garden joystick disc";
            if (ring) joystickRingSprite = sprite; else joystickDiscSprite = sprite;
            return sprite;
        }

        private Button Button(string name, string label, Transform parent, Vector2 anchor, Vector2 position, Action action, int fontSize, Vector2? dimensions = null)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button)); go.transform.SetParent(parent, false);
            var rect = go.GetComponent<RectTransform>(); rect.anchorMin = anchor; rect.anchorMax = anchor; rect.pivot = new Vector2(0.5f, 0.5f); rect.anchoredPosition = position; rect.sizeDelta = dimensions ?? new Vector2(120f, 84f);
            Image image = go.GetComponent<Image>();
            bool primary = name == "Follow" || name == "Call cat";
            CatMeUIStyle.ApplySurface(image, primary ? CatMeUIStyle.Olive : CatMeUIStyle.CreamGlass);
            Button button = go.GetComponent<Button>(); button.targetGraphic = image; button.onClick.AddListener(() => action());
            MakeLabel(go.transform, "Label", label, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero, fontSize);
            CatMeUIStyle.StyleButton(button, primary ? CatMeUIStyle.Olive : CatMeUIStyle.CreamGlass, primary);
            return button;
        }

        private static Text MakeLabel(Transform parent, string name, string value, Vector2 amin, Vector2 amax, Vector2 pos, Vector2 size, int fontSize)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Text)); go.transform.SetParent(parent, false);
            var rect = go.GetComponent<RectTransform>(); rect.anchorMin = amin; rect.anchorMax = amax; rect.pivot = new Vector2(0.5f, 0.5f); rect.anchoredPosition = pos; rect.sizeDelta = size;
            Text text = go.GetComponent<Text>(); text.text = value; text.font = CatMe.Garden.CatMeUIStyle.UiFont; text.fontSize = fontSize; text.color = Color.white; text.alignment = TextAnchor.MiddleCenter; text.horizontalOverflow = HorizontalWrapMode.Wrap; text.raycastTarget = false; return text;
        }
        private void SetStatus(string value)
        {
            if (status == null || statusPanel == null) return;
            if (statusFade != null) StopCoroutine(statusFade);
            status.text = value ?? string.Empty;
            if (!gameplayHudVisible) { statusPanel.SetActive(false); return; }
            bool visible = !string.IsNullOrWhiteSpace(value);
            statusPanel.SetActive(visible);
            if (!visible) return;
            statusGroup.alpha = 1f;
            statusFade = StartCoroutine(FadeStatus());
        }

        private IEnumerator FadeStatus()
        {
            // Longer lines stay longer; the Story linger setting adds reading time to every line.
            bool linger = PlayerPrefs.GetFloat("CatMe.Journey.StoryHold", 4.2f) > 6f;
            int words = status != null ? status.text.Split(' ').Length : 0;
            yield return new WaitForSecondsRealtime(Mathf.Max(2.8f, words * .32f) * (linger ? 1.7f : 1f));
            float elapsed = 0f;
            while (elapsed < 0.42f && statusGroup != null)
            {
                elapsed += Time.unscaledDeltaTime;
                statusGroup.alpha = 1f - Mathf.Clamp01(elapsed / 0.42f);
                yield return null;
            }
            if (statusPanel != null) statusPanel.SetActive(false);
            statusFade = null;
        }
        private static AnimationState FirstState(Animation animation) => animation == null ? null : animation.Cast<AnimationState>().FirstOrDefault();
        private static AnimationState FindState(Animation animation, string name) => animation == null ? null : animation[name];
        private static Transform FindHumanHand(Transform root)
        {
            string[] names = { "hand_r", "right hand", "r_hand", "hand.right", "mixamorig:right hand", "mixamorig:hand_r" };
            return root.GetComponentsInChildren<Transform>(true).FirstOrDefault(item => names.Any(name => item.name.IndexOf(name, StringComparison.OrdinalIgnoreCase) >= 0));
        }
        private static Bounds RenderBounds(Transform root) { var renderers = root.GetComponentsInChildren<Renderer>(true); if (renderers.Length == 0) return new Bounds(root.position, Vector3.zero); Bounds result = renderers[0].bounds; for (int i = 1; i < renderers.Length; i++) result.Encapsulate(renderers[i].bounds); return result; }
        private Vector3 ClampGarden(Vector3 value, float inset) { if (unboundedWorld) return Grounded(value); value.x = Mathf.Clamp(value.x, -gardenLimit.x + inset, gardenLimit.x - inset); value.z = Mathf.Clamp(value.z, -16f + inset, gardenLimit.z - inset); return Grounded(value); }
        private float GroundY(Vector3 value)
        {
            return GroundProbe.Height(new Vector3(value.x, value.y, value.z), 0f);
        }
        private Vector3 Grounded(Vector3 value) { value.y = GroundY(value); return value; }
        private void MoveGrounded(Transform actor, Vector3 delta, float radius, float height)
        {
            if (delta.sqrMagnitude < 0.000001f) return;
            delta.y = 0f;
            float distance = delta.magnitude;
            Vector3 direction = delta / distance;
            Vector3 bottom = actor.position + Vector3.up * (radius + 0.02f);
            Vector3 top = actor.position + Vector3.up * Mathf.Max(radius + 0.04f, height - radius);
            if (Physics.CapsuleCast(bottom, top, radius, direction, out RaycastHit obstruction, distance + 0.025f, GroundProbe.ObstacleMask, QueryTriggerInteraction.Ignore))
                distance = Mathf.Max(0f, obstruction.distance - 0.035f);
            if (distance <= 0.0001f) return;
            Vector3 next = actor.position + direction * distance;
            next = ClampGarden(next, radius);
            float nextY = GroundY(next);
            // The current route is level. Keep the pitch limit ready for supported path
            // grades while rejecting cliffs or roof hits from the ground adapter.
            float rise = nextY - actor.position.y;
            if (Mathf.Abs(rise) > distance * 0.55f + 0.04f) return;
            next.y = nextY;
            actor.position = next;
        }
        private static Material MakeMaterial(Color color, float roughness)
        {
            // Mobile players strip shaders that are only requested by Shader.Find at runtime.
            // The glTF shader materials are kept in Resources by IOSDevelopmentBuild so they
            // remain available in the iOS player alongside the dynamically imported GLBs.
            Material source = Resources.Load<Material>("GltfRuntimeShaders/glTF-pbrMetallicRoughness")
                ?? Resources.Load<Material>("GltfRuntimeShaders/glTF-unlit");
            if (source == null)
                throw new InvalidOperationException("Garden material shader resources are missing from the player.");

            var material = new Material(source);
            if (material.HasProperty("baseColorFactor")) material.SetColor("baseColorFactor", color);
            if (material.HasProperty("_BaseColor")) material.SetColor("_BaseColor", color);
            if (material.HasProperty("_Color")) material.SetColor("_Color", color);
            if (material.HasProperty("_BaseColorFactor")) material.SetColor("_BaseColorFactor", color);
            if (material.HasProperty("_Smoothness")) material.SetFloat("_Smoothness", 1f - roughness);
            if (material.HasProperty("roughnessFactor")) material.SetFloat("roughnessFactor", roughness);
            if (material.HasProperty("metallicFactor")) material.SetFloat("metallicFactor", 0f);
            return material;
        }
        private static GameObject Cube(string name, Vector3 position, Vector3 scale, Material material) { var go = GameObject.CreatePrimitive(PrimitiveType.Cube); go.name = name; go.transform.position = position; go.transform.localScale = scale; go.GetComponent<Renderer>().material = material; go.GetComponent<Collider>().enabled = false; return go; }
        private static void Sphere(string name, Vector3 position, Vector3 scale, Material material) { var go = GameObject.CreatePrimitive(PrimitiveType.Sphere); go.name = name; go.transform.position = position; go.transform.localScale = scale; go.GetComponent<Renderer>().material = material; go.GetComponent<Collider>().enabled = false; }
        private static void Cylinder(string name, Vector3 position, Vector3 scale, Material material) { Cube(name, position, scale, material); }

    }

    internal sealed class MobileGardenJoystick : MonoBehaviour, IPointerDownHandler, IDragHandler, IPointerUpHandler
    {
        private GardenCompanionDemo owner;
        private RectTransform baseRect;
        private RectTransform thumb;
        private float radius;

        public void Initialize(GardenCompanionDemo demo, RectTransform joystickBase, RectTransform joystickThumb, float travelRadius)
        {
            owner = demo; baseRect = joystickBase; thumb = joystickThumb; radius = travelRadius;
        }

        public void OnPointerDown(PointerEventData eventData) => UpdateThumb(eventData);
        public void OnDrag(PointerEventData eventData) => UpdateThumb(eventData);
        public void OnPointerUp(PointerEventData eventData) => ResetThumb();
        private void OnDisable() => ResetThumb();

        private void UpdateThumb(PointerEventData eventData)
        {
            if (baseRect == null || thumb == null || owner == null) return;
            if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(baseRect, eventData.position, eventData.pressEventCamera, out Vector2 local)) return;
            Vector2 offset = Vector2.ClampMagnitude(local, radius);
            thumb.anchoredPosition = offset;
            Vector2 input = offset / radius;
            owner.SetMove(input.magnitude < 0.08f ? Vector2.zero : input);
        }

        private void ResetThumb()
        {
            if (thumb != null) thumb.anchoredPosition = Vector2.zero;
            owner?.SetMove(Vector2.zero);
        }
    }
}
