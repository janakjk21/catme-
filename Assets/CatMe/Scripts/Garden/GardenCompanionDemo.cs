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
        private float waitHintAt;
        private Vector3 scriptedWalk;
        private bool unboundedWorld;
        public bool DidCallGesturePlay { get; private set; }
        private CatMe.Pets.CompanionBehavior behavior = new CatMe.Pets.CatBehavior();
        public CatMe.Pets.CompanionBehavior Behavior => behavior;
        public CompanionVoice Voice { get; private set; }
        public AudioSource CompanionAudio => audioSource;
        private GameObject dogModel;
        private Vector3 leadTarget; private bool leadActive, leadHold, curiosityPlay;

        /// <summary>Applies the chosen pet: its species behaviour and model. A pet's own model when it has one; otherwise the bundled cat, or the Dogs Kit corgi for a dog.</summary>
        private string loadedCatModel;
        private bool loadedOwnModel;   // the walking pet is the active pet's own downloaded model (cat or dog)
        public void SetActivePet(CatMe.Pets.PetProfile pet)
        {
            if (pet == null) return;
            CatMe.Pets.PetSounds.UseVoice(pet);   // the same pet always sounds like itself
            // A different cat with its own model: reload the characters so the right one walks beside the owner.
            // Cats and dogs alike (launch task L7): a pet with its own model reloads it; a pet without one returns to the stand-in.
            string own = PetModelChoice.OwnModel(pet.modelPath, !string.IsNullOrEmpty(pet.modelPath) && System.IO.File.Exists(pet.modelPath));
            bool wrongModel = own != null ? own != loadedCatModel : loadedOwnModel;
            if (IsReady && wrongModel) { behavior = CatMe.Pets.CompanionBehavior.For(pet.Kind); StartCoroutine(ReloadCharactersFromPhoneStorage()); return; }
            var next = CatMe.Pets.CompanionBehavior.For(pet.Kind);
            bool speciesChanged = behavior.Species != next.Species;
            behavior = next;
            if (speciesChanged || (pet.Kind == CatMe.Pets.Species.Dog && dogModel == null && !loadedOwnModel)) ApplyCompanionModel();
        }

        private void ApplyCompanionModel()
        {
            if (catRoot == null) return;
            // The corgi stands in only for a dog without its own model; a dog's own model walks like the cat's (one walk clip).
            bool dog = behavior.Species == CatMe.Pets.Species.Dog && !loadedOwnModel;
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
                reloadGate.Request();   // the first load counts as a reload, so nothing else loads characters at the same time
                try { await LoadCharacters(); }
                finally { if (reloadGate.Finished()) StartCoroutine(ReloadLoop()); }
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
                // Walk slower while the pet is far behind, and say so once in a while, so it never feels like the stick broke.
                bool petFarBehind = follow && catRoot != null && Vector3.Distance(catRoot.position, humanRoot.position) > 5f;
                float ownerWalkSpeed = petFarBehind ? .7f : 1.1f;
                if (petFarBehind && combinedInput.sqrMagnitude > .04f && Time.unscaledTime > waitHintAt)
                { waitHintAt = Time.unscaledTime + 45f; SetStatus("Slowing down so " + CatMe.Pets.PetRoster.NameForText() + " can catch up."); }
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
            ClearCharacters();   // never add a second person or pet next to the old ones
            string folder = Path.Combine(Application.streamingAssetsPath, GardenAssets);
            string savedCat = DeviceBridge.CatModelPath;
            string savedHuman = DeviceBridge.HumanModelPath;
            string bundledCat = Path.Combine(folder, "cat-walking.glb"), bundledHuman = Path.Combine(folder, "human-walking.glb");
            string catPath = File.Exists(savedCat) ? savedCat : bundledCat;
            // Each pet keeps its own downloaded model; the active pet's model wins over the single legacy file. A dog without one gets the corgi.
            var activePet = CatMe.Pets.PetRoster.Active;
            string ownModel = activePet == null ? null : PetModelChoice.OwnModel(activePet.modelPath, !string.IsNullOrEmpty(activePet.modelPath) && File.Exists(activePet.modelPath));
            if (ownModel != null) catPath = ownModel;
            float petHeight = ownModel != null && activePet != null ? CompanionScaleStandards.PetHeightFor(activePet) : CompanionScaleStandards.CatHeightMeters;
            string humanPath = File.Exists(savedHuman) ? savedHuman : bundledHuman;
            // A generated person has only a walk clip, so the call gesture exists only for the bundled person.
            // Loading the same large person model a second time would only double its memory.
            string callPath = File.Exists(savedHuman) ? null : Path.Combine(folder, "human-call.glb");
            // Provider pixel framing and authored GLB units vary, so enforce one shared pair scale after import.
            try { catAnimation = await LoadCharacter(catPath, catRoot, petHeight, true); }
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
            loadedOwnModel = ownModel != null && catPath == ownModel;
            if (dogModel != null && loadedOwnModel) dogModel.SetActive(false);
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

        // F4 (double person): reloads used to run in parallel, each adding a WalkingHuman. Now one runs at a time, and any number
        // of requests while it runs become one follow-up reload.
        private readonly ReloadGate reloadGate = new ReloadGate();

        /// <summary>Reload the pet and person from the phone. Safe to call any number of times; StartCoroutine on it as before.</summary>
        public IEnumerator ReloadCharactersFromPhoneStorage()
        {
            if (reloadGate.Request()) StartCoroutine(ReloadLoop());
            yield break;
        }

        private IEnumerator ReloadLoop()
        {
            do
            {
                ClearCharacters();
                yield return null;
                Task load = LoadCharacters();
                while (!load.IsCompleted) yield return null;
                if (load.IsFaulted)
                {
                    Debug.LogException(load.Exception, this);
                    SetStatus("Your new character was saved, but the garden could not reload it yet.");
                }
            }
            while (reloadGate.Finished());
        }

        // Removes the pet and person models at once (detached first, so nothing counts them before Destroy runs).
        private void ClearCharacters()
        {
            if (catRoot != null) DetachAndDestroyChildren(catRoot);
            if (humanRoot != null) DetachAndDestroyChildren(humanRoot);
            catAnimation = null; humanWalkAnimation = null; humanCallAnimation = null;
            catWalkState = null; humanWalkState = null; sitState = null; beckonState = null;
            walkingHuman = null; callingHuman = null; humanHand = null;
            follow = false; calling = false; fetching = false;
        }

        private static void DetachAndDestroyChildren(Transform root)
        {
            for (int i = root.childCount - 1; i >= 0; i--) { var child = root.GetChild(i); child.SetParent(null, false); child.gameObject.SetActive(false); Destroy(child.gameObject); }
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
            appFlow?.OnAppPaused(paused);
            if (!paused) return;
            ClearMovement();
            appFlow?.SaveProgress();
            journey?.SaveNow();
            // Only in the garden: never over Login, onboarding or the Making screen (audit 27c).
            if (Application.isMobilePlatform && gameplayHudVisible && pauseSheet != null && !pauseSheet.activeSelf) OpenPause();
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
            // The touch stick shapes its own input (JoystickResponse: dead zone and easing); keys are full or nothing.
            if (input.sqrMagnitude < 0.0001f) { movementLatched = false; return Vector3.zero; }
            if (!movementLatched) { movementYaw = companionCamera != null ? companionCamera.ReferenceYaw : 0f; movementLatched = true; }
            Quaternion yaw = Quaternion.Euler(0f, movementYaw, 0f);
            return yaw * new Vector3(input.x, 0f, input.y).normalized * Mathf.Clamp01(input.magnitude);
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
            // One input module only. A scene-saved StandaloneInputModule next to the Input System one makes both read the same
            // touches (legacy and new input are both enabled), a likely source of "Touch was already deallocated" (audit 27b).
            foreach (var module in eventSystem.GetComponents<BaseInputModule>())
                if (!(module is InputSystemUIInputModule)) { module.enabled = false; Destroy(module); }
            // Every other EventSystem (from additively loaded place scenes) is switched off; only this one drives the UI.
            foreach (var other in FindObjectsByType<EventSystem>(FindObjectsInactive.Exclude, FindObjectsSortMode.None))
                if (other != eventSystem) other.gameObject.SetActive(false);
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
                // Readable over any scene: the label sits on a Paper pill, never loose white text (audit 27f).
                var pill = CatMeUIStyle.Card(b.transform, "Caption pill", new Vector2(120f, 44f), CatMeUIStyle.Paper, CatMeUIStyle.Elevation.Resting);
                ((RectTransform)pill.transform).anchoredPosition = new Vector2(0f, -86f); pill.GetComponent<Image>().raycastTarget = false;
                var cap = UiKit.Label(pill.transform, label, 26, CatMeUIStyle.Ink, Vector2.zero, new Vector2(116f, 40f), FontStyle.Bold, TextAnchor.MiddleCenter, "Caption");
                return b;
            }
            Fan("Ball action", "ball", "Ball", new Vector2(-185f, 20f), () => { CloseCatTray(); ThrowBall(); });
            var followButton = Fan("Follow action", "walk", "Roam", new Vector2(-150f, 190f), () => { CloseCatTray(); ToggleFollow(); });
            followActionLabel = followButton.transform.Find("Caption pill/Caption")?.GetComponent<Text>();
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

        // ---- Menu (docs/MENUS_AND_ACCOUNT.md section 3): a right-side sheet with one list and one way back ----------------------

        /// <summary>Turn on when the store ships; until then it has no entry point.</summary>
        internal const bool StoreShipped = false;
        private Text menuPetLine, menuAccountNote;
        private Image menuPetPhoto;
        private Sprite menuPetSprite;

        private void BuildPauseSheet(Transform parent)
        {
            var screen = UiKit.Screen(parent, "Pause sheet", new Color(CatMeUIStyle.Ink.r, CatMeUIStyle.Ink.g, CatMeUIStyle.Ink.b, .35f));
            pauseSheet = screen.root;
            // Tapping the dimmed game closes the menu.
            var scrim = pauseSheet.AddComponent<Button>(); scrim.transition = Selectable.Transition.None; scrim.onClick.AddListener(ResumePause);
            var sheet = CatMeUIStyle.Card(screen.safe, "Menu sheet", new Vector2(720f, 960f), CatMeUIStyle.Paper, CatMeUIStyle.Elevation.Floating);
            var sr = (RectTransform)sheet.transform;
            sr.anchorMin = new Vector2(1f, 0f); sr.anchorMax = new Vector2(1f, 1f); sr.pivot = new Vector2(1f, .5f);
            sr.sizeDelta = new Vector2(720f, -48f); sr.anchoredPosition = new Vector2(-24f, 0f);
            RectTransform AtTop(RectTransform r, float y) { r.anchorMin = r.anchorMax = new Vector2(.5f, 1f); r.anchoredPosition = new Vector2(r.anchoredPosition.x, y); return r; }

            AtTop(UiKit.Label(sheet.transform, "Menu", 56, CatMeUIStyle.Ink, new Vector2(0f, 0f), new Vector2(624f, 76f), FontStyle.Bold, TextAnchor.MiddleLeft, "Title").rectTransform, -70f);
            // The active pet: tapping it opens Your pets.
            var petRow = UiKit.Button(sheet.transform, "", Vector2.zero, new Vector2(624f, 96f), CatMeUIStyle.Paper, false, () => { ResumePause(); appFlow?.OpenFromMenu("pets"); }, 26, "Walking with");
            AtTop((RectTransform)petRow.transform, -160f);
            var photo = new GameObject("Pet photo", typeof(RectTransform), typeof(Image)); photo.transform.SetParent(petRow.transform, false);
            var pr = (RectTransform)photo.transform; pr.sizeDelta = new Vector2(72f, 72f); pr.anchoredPosition = new Vector2(-266f, 0f);
            menuPetPhoto = photo.GetComponent<Image>(); menuPetPhoto.sprite = UiKit.Circle; menuPetPhoto.color = CatMeUIStyle.SageTint; menuPetPhoto.raycastTarget = false; menuPetPhoto.preserveAspect = true;
            photo.AddComponent<Mask>().showMaskGraphic = true;
            menuPetLine = UiKit.Label(petRow.transform, "", 28, CatMeUIStyle.MutedInk, new Vector2(40f, 0f), new Vector2(500f, 60f), FontStyle.Normal, TextAnchor.MiddleLeft, "Walking with line");

            float y = -280f;
            void Row(string name, string icon, string title, Action action, bool withNote = false)
            {
                var row = UiKit.Button(sheet.transform, "", Vector2.zero, new Vector2(624f, 112f), CatMeUIStyle.Mist, false, action, 26, name);
                AtTop((RectTransform)row.transform, y); y -= 136f;
                var disc = new GameObject("Icon disc", typeof(RectTransform), typeof(Image)); disc.transform.SetParent(row.transform, false);
                var dr = (RectTransform)disc.transform; dr.sizeDelta = new Vector2(64f, 64f); dr.anchoredPosition = new Vector2(-250f, 0f);
                var di = disc.GetComponent<Image>(); di.sprite = UiKit.Circle; di.color = CatMeUIStyle.SageTint; di.raycastTarget = false;
                UiKit.Icon(disc.transform, icon, Vector2.zero, 40f, CatMeUIStyle.SageDeep);
                UiKit.Label(row.transform, title, 34, CatMeUIStyle.Ink, new Vector2(10f, withNote ? 16f : 0f), new Vector2(400f, 48f), FontStyle.Normal, TextAnchor.MiddleLeft, "Title");
                if (withNote) menuAccountNote = UiKit.Label(row.transform, "", 24, CatMeUIStyle.SageDeep, new Vector2(10f, -22f), new Vector2(400f, 36f), FontStyle.Normal, TextAnchor.MiddleLeft, "Note");
                UiKit.Icon(row.transform, "arrow", new Vector2(270f, 0f), 34f, CatMeUIStyle.Sage);
            }
            Row("Your pets", "pets", "Your pets", () => { ResumePause(); appFlow?.OpenFromMenu("pets"); });
            Row("Photo album", "album", "Photo album", () => { ResumePause(); appFlow?.OpenFromMenu("album"); });
            if (StoreShipped) Row("Pet store", "store", "Store", () => { ResumePause(); appFlow?.OpenFromMenu("store"); });
            Row("Your companions", "account", "Account", () => { ResumePause(); appFlow?.OpenFromMenu("account"); }, true);
            Row("Settings", "gear", "Settings", () => { ResumePause(); appFlow?.OpenFromMenu("settings"); });

            var back = UiKit.NestedButton(sheet.transform, "Back to the garden", Vector2.zero, 624f, CatMeUIStyle.SageDeep, ResumePause, 32, "Back to the garden");
            var br = (RectTransform)back.transform; br.anchorMin = br.anchorMax = new Vector2(.5f, 0f); br.anchoredPosition = new Vector2(0f, 90f);
            pauseSheet.SetActive(false);
        }

        // Filled in each time the menu opens: who is walking now, and the account nudge.
        private void RefreshMenu()
        {
            var pet = CatMe.Pets.PetRoster.Active;
            if (menuPetLine != null) menuPetLine.text = "Walking with " + CatMe.Pets.PetRoster.NameForText();
            if (menuPetPhoto != null)
            {
                if (menuPetSprite != null) { Destroy(menuPetSprite.texture); Destroy(menuPetSprite); menuPetSprite = null; }
                menuPetSprite = Art.FromFile(pet?.photoPath);
                menuPetPhoto.sprite = menuPetSprite != null ? menuPetSprite : UiKit.Circle;
                menuPetPhoto.color = menuPetSprite != null ? Color.white : CatMeUIStyle.SageTint;
            }
            if (menuAccountNote != null) menuAccountNote.text = appFlow?.AccountRowNote() ?? "";
        }

        /// <summary>Back from a screen opened from the menu returns to the menu.</summary>
        internal void OpenMenu() => OpenPause();
        /// <summary>The photo album from the menu; <paramref name="onClosed"/> runs when its Back is tapped.</summary>
        internal void OpenPhotoAlbum(System.Action onClosed)
        {
            if (journey == null) { onClosed?.Invoke(); return; }
            journey.AlbumClosed = onClosed;
            journey.OpenPhotoAlbum();
        }

        private void OpenPause() { if (pauseSheet == null || !gameplayHudVisible) return; replayArmed=false;if(replayActionLabel!=null)replayActionLabel.text="Start new outing";CloseCatTray(); ClearMovement(); RefreshMenu(); pauseSheet.SetActive(true); pauseSheet.transform.SetAsLastSibling(); SetInputBlocked(true); journey?.SaveNow(); Time.timeScale=0f; }
        private void ResumePause() { if (pauseSheet != null) pauseSheet.SetActive(false); replayArmed=false;Time.timeScale=1f; SetInputBlocked(false); ClearMovement(); }
        private void ConfirmReplay() { if(!replayArmed){replayArmed=true;replayActionLabel.text="Tap again to restart outing";return;}ResumePause();BeginConnectedOuting(); }

        // Floating stick (MENUS_AND_ACCOUNT section 9): the left 40% of the safe area is the touch zone; the stick appears where the
        // thumb lands and rests, faint, at 22% from the bottom of the safe area. Base 220, travel 110, thumb 88.
        private void CreateMoveJoystick(Transform parent)
        {
            GameObject zone = new GameObject("Movement joystick", typeof(RectTransform), typeof(Image));
            zone.transform.SetParent(parent, false);
            RectTransform zoneRect = zone.GetComponent<RectTransform>();
            zoneRect.pivot = Vector2.zero;
            zone.GetComponent<Image>().color = new Color(0f, 0f, 0f, 0f);   // invisible, but catches touches

            GameObject baseObject = new GameObject("Joystick base", typeof(RectTransform), typeof(Image));
            baseObject.transform.SetParent(zone.transform, false);
            RectTransform baseRect = baseObject.GetComponent<RectTransform>();
            baseRect.anchorMin = baseRect.anchorMax = Vector2.zero;
            baseRect.pivot = new Vector2(0.5f, 0.5f);
            baseRect.sizeDelta = new Vector2(220f, 220f);
            Image baseImage = baseObject.GetComponent<Image>();
            baseImage.sprite = GetJoystickSprite(false);
            baseImage.color = new Color(CatMeUIStyle.Paper.r, CatMeUIStyle.Paper.g, CatMeUIStyle.Paper.b, .35f);
            baseImage.raycastTarget = false;

            GameObject ring = new GameObject("Joystick ring", typeof(RectTransform), typeof(Image));
            ring.transform.SetParent(baseObject.transform, false);
            RectTransform ringRect = ring.GetComponent<RectTransform>();
            ringRect.anchorMin = ringRect.anchorMax = new Vector2(0.5f, 0.5f);
            ringRect.sizeDelta = new Vector2(220f, 220f);
            Image ringImage = ring.GetComponent<Image>(); ringImage.sprite = GetJoystickSprite(true);
            ringImage.color = CatMeUIStyle.Stone; ringImage.raycastTarget = false;

            GameObject knob = new GameObject("Joystick thumb", typeof(RectTransform), typeof(Image));
            knob.transform.SetParent(baseObject.transform, false);
            RectTransform knobRect = knob.GetComponent<RectTransform>();
            knobRect.anchorMin = knobRect.anchorMax = new Vector2(0.5f, 0.5f);
            knobRect.sizeDelta = new Vector2(88f, 88f);
            Image knobImage = knob.GetComponent<Image>(); knobImage.sprite = GetJoystickSprite(false);
            knobImage.color = CatMeUIStyle.Paper; knobImage.raycastTarget = false;

            var group = baseObject.AddComponent<CanvasGroup>(); group.blocksRaycasts = false; group.interactable = false;
            zone.AddComponent<MobileGardenJoystick>().Initialize(this, zoneRect, baseRect, knobRect, group);
#if UNITY_IOS && !UNITY_EDITOR
            // Swipes that start at the bottom edge go to the stick first; iOS asks for a second swipe to go home.
            UnityEngine.iOS.Device.deferSystemGesturesMode = UnityEngine.iOS.SystemGestureDeferMode.BottomEdge;
#endif
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

    /// <summary>The stick's response curve: a 0.15 dead zone, then an eased ramp to full speed. Pure, so it is tested.</summary>
    internal static class JoystickResponse
    {
        public const float Travel = 110f, DeadZone = .15f, Ease = 1.4f;

        /// <summary>Maps the thumb offset (canvas units from the stick centre) to a move input of length 0..1.</summary>
        public static Vector2 FromOffset(Vector2 offset) => Shape(offset / Travel);

        public static Vector2 Shape(Vector2 raw)
        {
            float m = Mathf.Clamp01(raw.magnitude);
            if (m <= DeadZone) return Vector2.zero;
            float shaped = Mathf.Pow((m - DeadZone) / (1f - DeadZone), Ease);
            return raw.normalized * shaped;
        }
    }

    internal sealed class MobileGardenJoystick : MonoBehaviour, IPointerDownHandler, IDragHandler, IPointerUpHandler
    {
        static readonly System.Collections.Generic.List<RaycastResult> uiHits = new System.Collections.Generic.List<RaycastResult>();
        /// <summary>
        /// True when a screen point is over UI other than the stick's touch zone (the zone is invisible and covers part of the
        /// garden, so tapping the pet there must still work).
        /// </summary>
        internal static bool OverUiExceptStick(Vector2 screenPosition)
        {
            var es = EventSystem.current;
            if (es == null) return false;
            uiHits.Clear();
            es.RaycastAll(new PointerEventData(es) { position = screenPosition }, uiHits);
            foreach (var hit in uiHits)
                if (hit.gameObject != null && hit.gameObject.GetComponent<MobileGardenJoystick>() == null) return true;
            return false;
        }

        /// <summary>Share of the safe area's width that starts the stick, and the resting spot's height in it.</summary>
        public const float ZoneWidth = .4f, RestHeight = .22f;
        private GardenCompanionDemo owner;
        private RectTransform zone, baseRect, thumb;
        private CanvasGroup look;
        private Rect lastSafeArea;
        private Vector2 lastScreen, lastZoneSize;
        private int pointerId = int.MinValue;

        public void Initialize(GardenCompanionDemo demo, RectTransform touchZone, RectTransform joystickBase, RectTransform joystickThumb, CanvasGroup baseGroup)
        {
            owner = demo; zone = touchZone; baseRect = joystickBase; thumb = joystickThumb; look = baseGroup;
            FitSafeArea(); Rest();
        }

        private void Update()
        {
            if (Screen.safeArea != lastSafeArea || new Vector2(Screen.width, Screen.height) != lastScreen) FitSafeArea();
            if (zone != null && zone.rect.size != lastZoneSize && pointerId == int.MinValue) { lastZoneSize = zone.rect.size; Rest(); }
            if (look != null) look.alpha = Mathf.MoveTowards(look.alpha, pointerId != int.MinValue ? 1f : .55f, Time.unscaledDeltaTime * 4f);
        }

        // The zone covers the left 40% of the safe area, full height.
        private void FitSafeArea()
        {
            lastSafeArea = Screen.safeArea; lastScreen = new Vector2(Screen.width, Screen.height);
            if (zone == null || Screen.width <= 0 || Screen.height <= 0) return;
            Rect safe = Screen.safeArea;
            var min = new Vector2(safe.xMin / Screen.width, safe.yMin / Screen.height);
            var max = new Vector2((safe.xMin + safe.width * ZoneWidth) / Screen.width, safe.yMax / Screen.height);
            zone.anchorMin = min; zone.anchorMax = max; zone.offsetMin = zone.offsetMax = Vector2.zero;
        }

        private void Rest()
        {
            if (zone == null || baseRect == null) return;
            Vector2 size = zone.rect.size;
            float x = Mathf.Min(200f, size.x * .5f);
            baseRect.anchoredPosition = new Vector2(Mathf.Max(x, JoystickResponse.Travel), Mathf.Max(size.y * RestHeight, JoystickResponse.Travel));
            if (thumb != null) thumb.anchoredPosition = Vector2.zero;
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            if (pointerId != int.MinValue || zone == null) return;
            if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(zone, eventData.position, eventData.pressEventCamera, out Vector2 local)) return;
            pointerId = eventData.pointerId;
            // Centre the stick under the thumb, kept far enough from the edges that the full travel fits.
            Vector2 size = zone.rect.size;
            float m = JoystickResponse.Travel;
            baseRect.anchoredPosition = new Vector2(Mathf.Clamp(local.x, m, Mathf.Max(m, size.x - m * .5f)), Mathf.Clamp(local.y, m, Mathf.Max(m, size.y - m)));
            UpdateThumb(eventData);
        }

        public void OnDrag(PointerEventData eventData) { if (eventData.pointerId == pointerId) UpdateThumb(eventData); }
        public void OnPointerUp(PointerEventData eventData) { if (eventData.pointerId == pointerId) Release(); }
        private void OnDisable() => Release();

        private void UpdateThumb(PointerEventData eventData)
        {
            if (baseRect == null || thumb == null || owner == null) return;
            if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(baseRect, eventData.position, eventData.pressEventCamera, out Vector2 local)) return;
            Vector2 offset = Vector2.ClampMagnitude(local, JoystickResponse.Travel);
            thumb.anchoredPosition = offset;
            owner.SetMove(JoystickResponse.FromOffset(offset));
        }

        private void Release()
        {
            pointerId = int.MinValue;
            Rest();
            owner?.SetMove(Vector2.zero);
        }
    }

    /// <summary>One reload at a time, with at most one queued follow-up (F4). Pure, so it is tested.</summary>
    internal sealed class ReloadGate
    {
        public bool Running { get; private set; }
        public bool Queued { get; private set; }

        /// <summary>Returns true when the caller should start a reload now; otherwise one follow-up is queued.</summary>
        public bool Request()
        {
            if (Running) { Queued = true; return false; }
            Running = true; return true;
        }

        /// <summary>Call when a reload ends. Returns true when the queued follow-up should run now (still Running).</summary>
        public bool Finished()
        {
            if (Queued) { Queued = false; return true; }
            Running = false; return false;
        }
    }
}
