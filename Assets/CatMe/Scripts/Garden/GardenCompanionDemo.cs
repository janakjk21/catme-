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
        private readonly Vector3 gardenLimit = new Vector3(3.7f, 0f, 8f);
        private Transform humanRoot, catRoot, ball;
        private Transform gardenCamera;
        private Transform humanHand;
        private GameObject walkingHuman, callingHuman;
        private CompanionCreationPanel creationPanel;
        private Animation humanWalkAnimation, humanCallAnimation, catAnimation;
        private AnimationState humanWalkState, sitState, beckonState, catWalkState;
        private AudioSource audioSource;
        private AudioClip meow;
        private Text status;
        private bool follow, calling, fetching;
        private Vector2 moveInput, touchMoveInput;
        private Vector3 catTarget, ballVelocity;
        private float wanderAt, callStarted;
        private bool ballInFlight;
        public bool DidCallGesturePlay { get; private set; }
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
                audioSource = gameObject.AddComponent<AudioSource>();
                meow = Resources.Load<AudioClip>("CatMeAudio/cat_mewfood");
                humanRoot = new GameObject("HumanPlayer").transform;
                humanRoot.position = new Vector3(0f, 0f, -2.15f);
                catRoot = new GameObject("CatCompanion").transform;
                catRoot.position = new Vector3(1f, 0f, -1.7f);
                ball = GameObject.CreatePrimitive(PrimitiveType.Sphere).transform;
                ball.name = "FetchBall";
                ball.localScale = Vector3.one * 0.17f;
                ball.GetComponent<Renderer>().material = MakeMaterial(new Color(0.94f, 0.28f, 0.17f), 0.55f);
                ball.GetComponent<Collider>().enabled = false;
                ball.gameObject.SetActive(false);
                if (creationPanel == null) creationPanel = GetComponent<CompanionCreationPanel>();
                await LoadCharacters();
            }
            catch (Exception exception)
            {
                Debug.LogException(exception, this);
                SetStatus("Garden setup failed: " + exception.Message);
            }
        }

        private void Update()
        {
            ReadMovement();
            if (humanRoot != null)
            {
                Vector2 combinedInput = Vector2.ClampMagnitude(moveInput + touchMoveInput, 1f);
                Vector3 movement = new Vector3(combinedInput.x, 0f, combinedInput.y);
                if (movement.sqrMagnitude > 1f) movement.Normalize();
                humanRoot.position += movement * (2.0f * Time.deltaTime);
                humanRoot.position = ClampGarden(humanRoot.position, 0.28f);
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
                if (humanWalkState != null) humanWalkState.enabled = !calling;
            }

            if (catRoot != null) UpdateCat();
            if (ballInFlight) UpdateBall();
        }

        private void LateUpdate()
        {
            if (gardenCamera == null || humanRoot == null) return;

            Vector3 focus = humanRoot.position;
            float separation = 0f;
            if (catRoot != null)
            {
                Vector3 catOffset = catRoot.position - humanRoot.position;
                separation = catOffset.magnitude;
                // Keep the player weighted at center while letting the cat remain in frame.
                focus += Vector3.ClampMagnitude(catOffset * 0.28f, 1.15f);
            }
            focus.y = 0.96f;

            // Stay close enough for the human and cat to feel like companions.
            // The garden remains larger than the camera view and is discovered by moving.
            float framing = 1f + Mathf.Clamp(separation - 2.2f, 0f, 7f) * 0.008f;
            Vector3 desiredPosition = focus + new Vector3(0f, 2.45f, -4.05f) * framing;
            float blend = 1f - Mathf.Exp(-5.6f * Time.deltaTime);
            gardenCamera.position = Vector3.Lerp(gardenCamera.position, desiredPosition, blend);
            Quaternion look = Quaternion.LookRotation(focus + Vector3.up * 0.15f - gardenCamera.position);
            gardenCamera.rotation = Quaternion.Slerp(gardenCamera.rotation, look, blend);
        }

        private void ReadMovement()
        {
            if (Keyboard.current == null) return;
            float x = (Keyboard.current.dKey.isPressed || Keyboard.current.rightArrowKey.isPressed ? 1f : 0f)
                    - (Keyboard.current.aKey.isPressed || Keyboard.current.leftArrowKey.isPressed ? 1f : 0f);
            float z = (Keyboard.current.fKey.isPressed || Keyboard.current.upArrowKey.isPressed ? 1f : 0f)
                    - (Keyboard.current.sKey.isPressed || Keyboard.current.downArrowKey.isPressed ? 1f : 0f);
            moveInput = new Vector2(x, z);
        }

        private async Task LoadCharacters()
        {
            string folder = Path.Combine(Application.streamingAssetsPath, GardenAssets);
            string savedCat = CompanionCreationPanel.CatModelPath;
            string savedHuman = CompanionCreationPanel.HumanModelPath;
            string catPath = File.Exists(savedCat) ? savedCat : Path.Combine(folder, "cat-walking.glb");
            string humanPath = File.Exists(savedHuman) ? savedHuman : Path.Combine(folder, "human-walking.glb");
            string callPath = File.Exists(savedHuman) ? savedHuman : Path.Combine(folder, "human-call.glb");
            // Normalize the full GLB to a natural pet size beside a 1.7 m human avatar.
            catAnimation = await LoadCharacter(catPath, catRoot, 0.46f, true);
            catWalkState = FirstState(catAnimation);
            walkingHuman = new GameObject("WalkingHuman");
            walkingHuman.transform.SetParent(humanRoot, false);
            humanWalkAnimation = await LoadCharacter(humanPath, walkingHuman.transform, 1.7f, true);
            humanWalkState = FirstState(humanWalkAnimation);
            humanHand = FindHumanHand(walkingHuman.transform);
            callingHuman = new GameObject("CallingHuman");
            callingHuman.transform.SetParent(humanRoot, false);
            humanCallAnimation = await LoadCharacter(callPath, callingHuman.transform, 1.7f, false);
            sitState = FindState(humanCallAnimation, "Chair_Sit_Idle_F");
            beckonState = FindState(humanCallAnimation, "Call_Gesture");
            if (sitState != null) sitState.wrapMode = WrapMode.Loop;
            callingHuman.SetActive(false);
            if (humanWalkState != null) { humanWalkState.wrapMode = WrapMode.Loop; humanWalkAnimation.Play(humanWalkState.name); }
            if (catWalkState != null) catWalkState.wrapMode = WrapMode.Loop;
            IsReady = catWalkState != null && humanWalkState != null;
            SetStatus("Move with the joystick. Your cat is exploring.");
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
            loadedRoot.transform.localScale = Vector3.one * targetHeight / bounds.size.y;
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
            if (calling && Time.time >= callStarted + 9f) calling = false;
            if (fetching && !ball.gameObject.activeSelf) fetching = false;
            if (follow && !calling && !fetching)
            {
                Vector3 behind = humanRoot.position - humanRoot.forward * 0.65f;
                catTarget = ClampGarden(behind, 0.4f);
            }
            else if (fetching && ball.gameObject.activeSelf)
            {
                DidCatChaseBall = true;
                catTarget = ClampGarden(ball.position, 0.25f);
                if (Vector3.Distance(catRoot.position, catTarget) < 0.3f && ball.position.y < 0.34f)
                {
                    ball.gameObject.SetActive(false); fetching = false; ballInFlight = false;
                    SetStatus("Your cat brought the ball back.");
                }
            }
            else if (calling)
            {
                catTarget = humanRoot.position - humanRoot.forward * 0.5f;
                if (Vector3.Distance(catRoot.position, catTarget) < 0.34f && Time.time > callStarted + 2f)
                {
                    calling = false;
                    if (meow != null) { audioSource.PlayOneShot(meow, 0.75f); DidMeow = true; }
                    SetStatus("Your cat came when you called.");
                }
            }
            else if (Time.time > wanderAt)
            {
                catTarget = new Vector3(UnityEngine.Random.Range(-gardenLimit.x + 0.5f, gardenLimit.x - 0.5f), 0f, UnityEngine.Random.Range(-gardenLimit.z + 0.5f, gardenLimit.z - 0.5f));
                wanderAt = Time.time + UnityEngine.Random.Range(2f, 4f);
            }

            Vector3 delta = catTarget - catRoot.position;
            delta.y = 0f;
            if (delta.magnitude > 0.06f)
            {
                catRoot.position += delta.normalized * (fetching ? 1.2f : follow || calling ? 0.85f : 0.48f) * Time.deltaTime;
                catRoot.position = ClampGarden(catRoot.position, 0.25f);
                catRoot.rotation = Quaternion.Slerp(catRoot.rotation, Quaternion.LookRotation(delta), 8f * Time.deltaTime);
                if (catWalkState != null) catWalkState.enabled = true;
            }
            else if (catWalkState != null) catWalkState.enabled = false;
        }

        private void UpdateBall()
        {
            ballVelocity.y -= 9.8f * Time.deltaTime;
            ball.position += ballVelocity * Time.deltaTime;
            if (ball.position.y < 0.12f) { ball.position = new Vector3(ball.position.x, 0.12f, ball.position.z); ballVelocity.y = Mathf.Abs(ballVelocity.y) > 0.7f ? ballVelocity.y * -0.27f : 0f; ballVelocity.x *= 0.84f; ballVelocity.z *= 0.84f; }
            if (Mathf.Abs(ball.position.x) > gardenLimit.x - 0.15f) { ball.position = new Vector3(Mathf.Clamp(ball.position.x, -gardenLimit.x + 0.15f, gardenLimit.x - 0.15f), ball.position.y, ball.position.z); ballVelocity.x = 0f; }
            if (Mathf.Abs(ball.position.z) > gardenLimit.z - 0.15f) { ball.position = new Vector3(ball.position.x, ball.position.y, Mathf.Clamp(ball.position.z, -gardenLimit.z + 0.15f, gardenLimit.z - 0.15f)); ballVelocity.z = 0f; }
            if (ball.position.y <= 0.12f && ballVelocity.magnitude < 0.45f) { ballInFlight = false; ballVelocity = Vector3.zero; }
        }

        public void ToggleFollow()
        {
            follow = !follow; fetching = false; calling = false;
            SetStatus(follow ? "Your cat is following you." : "Your cat is exploring the garden.");
        }

        public void CallCat()
        {
            follow = false; fetching = false; calling = true; callStarted = Time.time;
            DidCallGesturePlay = false;
            catTarget = humanRoot.position;
            if (walkingHuman != null) walkingHuman.SetActive(false);
            if (callingHuman != null) callingHuman.SetActive(true);
            if (humanCallAnimation != null && sitState != null) humanCallAnimation.Play(sitState.name);
            StartCoroutine(PlayCallGesture());
            SetStatus("You call your cat.");
        }

        private IEnumerator PlayCallGesture()
        {
            yield return new WaitForSeconds(sitState == null ? 1.5f : sitState.length);
            if (callingHuman != null && callingHuman.activeSelf && humanCallAnimation != null && beckonState != null)
            {
                beckonState.wrapMode = WrapMode.Once;
                humanCallAnimation.Play(beckonState.name);
                DidCallGesturePlay = true;
            }
        }

        public void ThrowBall()
        {
            if (humanRoot == null) return;
            calling = false; fetching = true;
            DidThrowBall = true;
            if (callingHuman != null) callingHuman.SetActive(false);
            if (walkingHuman != null) walkingHuman.SetActive(true);
            ball.position = humanHand != null
                ? humanHand.position + humanRoot.forward * 0.12f
                : humanRoot.position + Vector3.up * 1.15f + humanRoot.forward * 0.38f;
            ball.gameObject.SetActive(true);
            ballVelocity = humanRoot.forward * 3.3f + Vector3.up * 3.3f;
            ballInFlight = true;
            SetStatus("You throw the ball. Your cat is chasing it.");
        }

        public void SetMove(Vector2 direction) => touchMoveInput = direction;
        public bool IsReady { get; private set; }
        public float CallSitDuration => sitState == null ? 0f : sitState.length;
        public float CatDistanceToHuman => catRoot == null || humanRoot == null ? float.MaxValue : Vector3.Distance(catRoot.position, humanRoot.position);
        public Vector3 HumanPosition => humanRoot == null ? Vector3.zero : humanRoot.position;
        public Vector3 CatPosition => catRoot == null ? Vector3.zero : catRoot.position;

        private void CreateGarden()
        {
            if (GameObject.Find("GardenEnvironment") != null) return;
            var prefab = Resources.Load<GameObject>("CompanionGarden/GardenEnvironment");
            if (prefab == null) throw new InvalidOperationException("Build the first garden using CatMe > Garden > Build First Garden.");
            Instantiate(prefab).name = "GardenEnvironment";
        }

        private void CreateCameraAndLight()
        {
            Camera camera = Camera.main;
            if (camera == null) { var go = new GameObject("Main Camera", typeof(Camera), typeof(AudioListener)); camera = go.GetComponent<Camera>(); go.tag = "MainCamera"; }
            camera.transform.position = new Vector3(0f, 3.35f, -6.2f);
            camera.transform.rotation = Quaternion.Euler(21f, 0f, 0f);
            camera.fieldOfView = 40f;
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(0.66f, 0.82f, 0.91f);
            camera.allowHDR = true;
            gardenCamera = camera.transform;
            RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Trilight;
            RenderSettings.ambientSkyColor = new Color(0.79f, 0.87f, 0.96f);
            RenderSettings.ambientEquatorColor = new Color(0.84f, 0.83f, 0.76f);
            RenderSettings.ambientGroundColor = new Color(0.50f, 0.58f, 0.43f);
            RenderSettings.ambientIntensity = 1.15f;
            RenderSettings.ambientLight = new Color(0.82f, 0.85f, 0.78f);
            var sun = new GameObject("Garden sunlight", typeof(Light)).GetComponent<Light>();
            sun.type = LightType.Directional; sun.color = new Color(1f, 0.96f, 0.86f); sun.intensity = 1.15f;
            sun.shadows = LightShadows.Soft; sun.shadowStrength = 0.55f; sun.shadowResolution = UnityEngine.Rendering.LightShadowResolution.High;
            sun.shadowBias = 0.06f; sun.shadowNormalBias = 0.32f;
            sun.transform.rotation = Quaternion.Euler(48f, -32f, 0f);
            var fill = new GameObject("Garden sky fill", typeof(Light)).GetComponent<Light>();
            fill.type = LightType.Directional; fill.color = new Color(0.78f, 0.86f, 1f); fill.intensity = 0.42f;
            fill.shadows = LightShadows.None; fill.transform.rotation = Quaternion.Euler(30f, 145f, 0f);
        }

        private void CreateHud()
        {
            var eventSystem = FindAnyObjectByType<EventSystem>();
            if (eventSystem == null) { eventSystem = new GameObject("EventSystem", typeof(EventSystem), typeof(InputSystemUIInputModule)).GetComponent<EventSystem>(); }
            else if (eventSystem.GetComponent<InputSystemUIInputModule>() == null) eventSystem.gameObject.AddComponent<InputSystemUIInputModule>();
            var canvasObject = new GameObject("Garden HUD", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            Canvas canvas = canvasObject.GetComponent<Canvas>(); canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            CanvasScaler scaler = canvasObject.GetComponent<CanvasScaler>(); scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize; scaler.referenceResolution = new Vector2(1920, 1080);
            status = MakeLabel(canvas.transform, "GardenStatus", "Loading cat and human…", new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -42f), new Vector2(880f, 72f), 30);
            CreateMoveJoystick(canvas.transform);
            Button("Follow", "Follow", canvas.transform, new Vector2(1f, 0f), new Vector2(-355f, 180f), ToggleFollow, 26);
            Button("Call cat", "Call", canvas.transform, new Vector2(1f, 0f), new Vector2(-220f, 180f), CallCat, 26);
            Button("Throw ball", "Ball", canvas.transform, new Vector2(1f, 0f), new Vector2(-85f, 180f), ThrowBall, 26);
            Button("Upload companions", "Upload", canvas.transform, new Vector2(1f, 1f), new Vector2(-105f, -52f), () => creationPanel?.Toggle(), 24, new Vector2(170f, 72f));
            creationPanel = gameObject.GetComponent<CompanionCreationPanel>() ?? gameObject.AddComponent<CompanionCreationPanel>();
            creationPanel.Initialize(canvas.transform);
        }

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
            Image image = go.GetComponent<Image>(); image.color = new Color(0.1f, 0.19f, 0.15f, 0.92f);
            Button button = go.GetComponent<Button>(); button.targetGraphic = image; button.onClick.AddListener(() => action());
            MakeLabel(go.transform, "Label", label, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero, fontSize);
            return button;
        }

        private static Text MakeLabel(Transform parent, string name, string value, Vector2 amin, Vector2 amax, Vector2 pos, Vector2 size, int fontSize)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Text)); go.transform.SetParent(parent, false);
            var rect = go.GetComponent<RectTransform>(); rect.anchorMin = amin; rect.anchorMax = amax; rect.pivot = new Vector2(0.5f, 0.5f); rect.anchoredPosition = pos; rect.sizeDelta = size;
            Text text = go.GetComponent<Text>(); text.text = value; text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf"); text.fontSize = fontSize; text.color = Color.white; text.alignment = TextAnchor.MiddleCenter; text.horizontalOverflow = HorizontalWrapMode.Wrap; text.raycastTarget = false; return text;
        }
        private void SetStatus(string value) { if (status != null) status.text = value; }
        private static AnimationState FirstState(Animation animation) => animation == null ? null : animation.Cast<AnimationState>().FirstOrDefault();
        private static AnimationState FindState(Animation animation, string name) => animation == null ? null : animation[name];
        private static Transform FindHumanHand(Transform root)
        {
            string[] names = { "hand_r", "right hand", "r_hand", "hand.right", "mixamorig:right hand", "mixamorig:hand_r" };
            return root.GetComponentsInChildren<Transform>(true).FirstOrDefault(item => names.Any(name => item.name.IndexOf(name, StringComparison.OrdinalIgnoreCase) >= 0));
        }
        private static Bounds RenderBounds(Transform root) { var renderers = root.GetComponentsInChildren<Renderer>(true); if (renderers.Length == 0) return new Bounds(root.position, Vector3.zero); Bounds result = renderers[0].bounds; for (int i = 1; i < renderers.Length; i++) result.Encapsulate(renderers[i].bounds); return result; }
        private Vector3 ClampGarden(Vector3 value, float inset) { value.x = Mathf.Clamp(value.x, -gardenLimit.x + inset, gardenLimit.x - inset); value.z = Mathf.Clamp(value.z, -gardenLimit.z + inset, gardenLimit.z - inset); value.y = 0f; return value; }
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
