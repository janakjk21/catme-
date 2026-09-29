using System.Collections;
using CatMe.GameInput;
using CatMe.Toys;
using CatMe.Save;
using CatMe.UI;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;

namespace CatMe.Cat
{
    /// <summary>
    /// Owns the local room cat presentation and makes the ready motor available
    /// to the temporary player Call control.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class HomeRoomCatIntegration : MonoBehaviour
    {
        [SerializeField] private Transform catSpawn;
        [SerializeField] private Transform catModelRoot;
        [SerializeField] private Transform axisCorrection;
        [SerializeField] private Transform scaleReference;
        [SerializeField] private AudioClip purrClip;
        [SerializeField] private float targetHeight = 0.35f;
        [SerializeField] private float presentationScale = 2.688f;
        [SerializeField] private float roomFacingYawDegrees = 180f;

        private LocalCatAssetLoader assetLoader;
        private CatMotor catMotor;
        private CatRoomAudio roomAudio;
        private RectTransform loadFailureSafeArea;
        private RectTransform loadFailurePanel;
        private Text loadFailureMessage;
        private Button loadFailureRetryButton;
        private float failureSafeWidth = -1f;
        private float failureSafeHeight = -1f;
        private bool started;

        public bool IsStationaryAcceptanceComplete { get; private set; }
        public bool DidStationaryAcceptancePass { get; private set; }
        public CatMotor CatMotor => catMotor;

        private void Start()
        {
            if (started)
            {
                return;
            }

            started = true;
            // The open HomeRoom scene may still contain the earlier 2.24x
            // serialized value. Enforce the requested 20% presentation bump
            // even before the scene is resaved by the editor tuning command.
            presentationScale = Mathf.Max(presentationScale, 2.688f);
            roomAudio = GetComponent<CatRoomAudio>();
            if (roomAudio == null) roomAudio = gameObject.AddComponent<CatRoomAudio>();
            roomAudio.Initialize(transform);
            if (!ResolveRoomReferences())
            {
                return;
            }

            transform.name = "CatRuntime";
            transform.SetPositionAndRotation(catSpawn.position, Quaternion.Euler(0f, roomFacingYawDegrees, 0f));
            catModelRoot = FindOrCreateChild(transform, "CatModelRoot");
            axisCorrection = FindOrCreateChild(catModelRoot, "AxisCorrection");
            catModelRoot.localPosition = Vector3.zero;
            catModelRoot.localRotation = Quaternion.identity;
            catModelRoot.localScale = Vector3.one;
            axisCorrection.localPosition = Vector3.zero;
            axisCorrection.localRotation = Quaternion.identity;
            axisCorrection.localScale = Vector3.one;

            assetLoader = gameObject.AddComponent<LocalCatAssetLoader>();
            assetLoader.Loaded += OnCatLoaded;
            assetLoader.Failed += OnCatLoadFailed;
            assetLoader.Initialize(
                catModelRoot,
                axisCorrection,
                targetHeight,
                assetForwardYaw: 0f,
                loopWalkInPlace: true,
                logLabel: "HomeRoom",
                preferCachedGeneratedCat: true);

            if (Debug.isDebugBuild)
            {
                CatGenerationPanel generationPanel = gameObject.AddComponent<CatGenerationPanel>();
                generationPanel.Initialize();
            }
        }

        private bool ResolveRoomReferences()
        {
            if (catSpawn == null)
            {
                GameObject spawnObject = GameObject.Find("CatSpawn");
                catSpawn = spawnObject == null ? null : spawnObject.transform;
            }

            if (catSpawn == null)
            {
                Debug.LogError("[CatMe][HomeRoom] CatSpawn is missing; HomeRoom cat integration was not started.");
                return false;
            }

            if (scaleReference == null)
            {
                GameObject referenceObject = GameObject.Find("CatScaleReference");
                scaleReference = referenceObject == null ? null : referenceObject.transform;
            }

            return true;
        }

        private void OnCatLoaded(LocalCatAssetLoader loader)
        {
            if (loadFailurePanel != null) loadFailurePanel.gameObject.SetActive(false);

            // Keep runtime-imported renderers visible in the HomeRoom preview.
            // Some GLB files arrive with their renderer flag disabled even
            // though the loader can still measure their bounds.
            foreach (Renderer renderer in loader.LoadedRoot.GetComponentsInChildren<Renderer>(true))
            {
                renderer.gameObject.SetActive(true);
                renderer.enabled = true;
            }

            // Keep the validated 0.35 m asset normalization, while giving the
            // room presentation a little more readable presence on screen.
            catModelRoot.localScale = Vector3.one * Mathf.Max(1f, presentationScale);
            bool scaleReferenceHidden = scaleReference != null;
            if (scaleReference != null)
            {
                foreach (Renderer renderer in scaleReference.GetComponentsInChildren<Renderer>(true))
                {
                    renderer.enabled = false;
                }

                scaleReferenceHidden = !HasEnabledRenderer(scaleReference);
            }

            Debug.Log(
                $"[CatMe][HomeRoom] Integration summary | file={loader.FixturePath} | bytes={loader.FixtureByteSize} | " +
                $"loadMs={loader.LoadDurationMilliseconds} | renderers={loader.LoadedRoot.GetComponentsInChildren<Renderer>(true).Length} | " +
                $"skinned={loader.LoadedRoot.GetComponentsInChildren<SkinnedMeshRenderer>(true).Length} | " +
                $"materials={CountMaterials(loader.LoadedRoot)} | clips={FormatClips(loader)} | selected={loader.SelectedWalkClip.name} | " +
                $"normalizedHeight={loader.FinalBounds.size.y:0.###}m | floorDistance={Mathf.Abs(loader.FinalBounds.min.y):0.####}m | " +
                $"assetForwardYaw={loader.ForwardYawDegrees:0.###}deg | roomFacingYaw={roomFacingYawDegrees:0.###}deg | " +
                $"catSpawn=({catSpawn.position.x:0.###},{catSpawn.position.y:0.###},{catSpawn.position.z:0.###}) | " +
                $"scaleReferenceHidden={scaleReferenceHidden}");
            StartCoroutine(ValidateStationaryWalkThenStartLocomotion(loader));
        }

        private void OnCatLoadFailed(LocalCatAssetLoader loader)
        {
            Debug.LogError("[CatMe][HomeRoom] Cat integration stopped because the local asset failed to load.");
            ShowCatLoadFailure();
        }

        private void ShowCatLoadFailure()
        {
            EnsureCatLoadFailureUi();
            if (loadFailurePanel != null) loadFailurePanel.gameObject.SetActive(true);
            if (loadFailureMessage != null) loadFailureMessage.text = "Your cat couldn't load. Please try again.";
            if (loadFailureRetryButton != null) loadFailureRetryButton.interactable = true;
        }

        private void EnsureCatLoadFailureUi()
        {
            if (loadFailurePanel != null) return;

            Canvas canvas = FindFirstObjectByType<Canvas>();
            if (canvas == null)
            {
                GameObject canvasObject = new GameObject("CatLoadFailureCanvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
                canvas = canvasObject.GetComponent<Canvas>();
                canvas.renderMode = RenderMode.ScreenSpaceOverlay;
                CanvasScaler scaler = canvasObject.GetComponent<CanvasScaler>();
                scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
                scaler.referenceResolution = new Vector2(1080f, 1920f);
                scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
                scaler.matchWidthOrHeight = 0.5f;
            }
            else
            {
                if (canvas.GetComponent<CanvasScaler>() == null) canvas.gameObject.AddComponent<CanvasScaler>();
                if (canvas.GetComponent<GraphicRaycaster>() == null) canvas.gameObject.AddComponent<GraphicRaycaster>();
            }

            EventSystem eventSystem = FindFirstObjectByType<EventSystem>();
            if (eventSystem == null)
            {
                GameObject eventObject = new GameObject("EventSystem");
                eventSystem = eventObject.AddComponent<EventSystem>();
            }
            StandaloneInputModule legacyModule = eventSystem.GetComponent<StandaloneInputModule>();
            if (legacyModule != null)
            {
                legacyModule.enabled = false;
                Destroy(legacyModule);
            }
            if (eventSystem.GetComponent<InputSystemUIInputModule>() == null)
                eventSystem.gameObject.AddComponent<InputSystemUIInputModule>();

            GameObject safeAreaObject = new GameObject("CatLoadFailureSafeArea", typeof(RectTransform));
            loadFailureSafeArea = safeAreaObject.GetComponent<RectTransform>();
            loadFailureSafeArea.SetParent(canvas.transform, false);
            loadFailureSafeArea.anchorMin = Vector2.zero;
            loadFailureSafeArea.anchorMax = Vector2.one;
            loadFailureSafeArea.offsetMin = Vector2.zero;
            loadFailureSafeArea.offsetMax = Vector2.zero;
            ApplyFailureSafeArea();

            GameObject panelObject = new GameObject("CatLoadFailurePanel", typeof(RectTransform), typeof(Image));
            loadFailurePanel = panelObject.GetComponent<RectTransform>();
            loadFailurePanel.SetParent(loadFailureSafeArea, false);
            panelObject.GetComponent<Image>().color = new Color(0.08f, 0.12f, 0.14f, 0.94f);

            GameObject messageObject = new GameObject("Message", typeof(RectTransform), typeof(Text));
            RectTransform messageRect = messageObject.GetComponent<RectTransform>();
            messageRect.SetParent(loadFailurePanel, false);
            messageRect.anchorMin = new Vector2(0.08f, 0.38f);
            messageRect.anchorMax = new Vector2(0.92f, 0.92f);
            messageRect.offsetMin = Vector2.zero;
            messageRect.offsetMax = Vector2.zero;
            loadFailureMessage = messageObject.GetComponent<Text>();
            loadFailureMessage.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            loadFailureMessage.fontSize = 30;
            loadFailureMessage.fontStyle = FontStyle.Bold;
            loadFailureMessage.color = Color.white;
            loadFailureMessage.alignment = TextAnchor.MiddleCenter;
            loadFailureMessage.raycastTarget = false;
            loadFailureMessage.text = "Your cat couldn't load. Please try again.";

            GameObject buttonObject = new GameObject("RetryCatLoad", typeof(RectTransform), typeof(Image), typeof(Button));
            RectTransform buttonRect = buttonObject.GetComponent<RectTransform>();
            buttonRect.SetParent(loadFailurePanel, false);
            buttonRect.anchorMin = new Vector2(0.5f, 0.14f);
            buttonRect.anchorMax = new Vector2(0.5f, 0.14f);
            buttonRect.pivot = new Vector2(0.5f, 0.5f);
            buttonRect.sizeDelta = new Vector2(340f, 74f);
            Image buttonImage = buttonObject.GetComponent<Image>();
            buttonImage.color = new Color(0.76f, 0.37f, 0.20f, 1f);
            loadFailureRetryButton = buttonObject.GetComponent<Button>();
            loadFailureRetryButton.targetGraphic = buttonImage;
            loadFailureRetryButton.onClick.AddListener(RetryCatLoad);

            GameObject buttonLabelObject = new GameObject("Label", typeof(RectTransform), typeof(Text));
            RectTransform buttonLabelRect = buttonLabelObject.GetComponent<RectTransform>();
            buttonLabelRect.SetParent(buttonRect, false);
            buttonLabelRect.anchorMin = Vector2.zero;
            buttonLabelRect.anchorMax = Vector2.one;
            buttonLabelRect.offsetMin = Vector2.zero;
            buttonLabelRect.offsetMax = Vector2.zero;
            Text buttonLabel = buttonLabelObject.GetComponent<Text>();
            buttonLabel.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            buttonLabel.fontSize = 26;
            buttonLabel.fontStyle = FontStyle.Bold;
            buttonLabel.color = Color.white;
            buttonLabel.alignment = TextAnchor.MiddleCenter;
            buttonLabel.text = "Retry cat load";
            buttonLabel.raycastTarget = false;

            loadFailurePanel.gameObject.SetActive(false);
            LayoutCatLoadFailureUi();
        }

        private void RetryCatLoad()
        {
            if (assetLoader == null || !assetLoader.HasFailed) return;
            if (loadFailureMessage != null) loadFailureMessage.text = "Retrying cat load…";
            if (loadFailureRetryButton != null) loadFailureRetryButton.interactable = false;
            assetLoader.RetryKnownGoodFixture();
        }

        private void ApplyFailureSafeArea()
        {
            if (loadFailureSafeArea == null || Screen.width <= 0 || Screen.height <= 0) return;
            Rect safe = Screen.safeArea;
            loadFailureSafeArea.anchorMin = new Vector2(safe.xMin / Screen.width, safe.yMin / Screen.height);
            loadFailureSafeArea.anchorMax = new Vector2(safe.xMax / Screen.width, safe.yMax / Screen.height);
        }

        private void LayoutCatLoadFailureUi()
        {
            if (loadFailureSafeArea == null || loadFailurePanel == null) return;
            float width = loadFailureSafeArea.rect.width;
            float height = loadFailureSafeArea.rect.height;
            if (width < 1f || height < 1f) return;
            failureSafeWidth = width;
            failureSafeHeight = height;
            loadFailurePanel.anchorMin = loadFailurePanel.anchorMax = new Vector2(0.5f, 0.5f);
            loadFailurePanel.pivot = new Vector2(0.5f, 0.5f);
            loadFailurePanel.anchoredPosition = Vector2.zero;
            loadFailurePanel.sizeDelta = new Vector2(Mathf.Min(860f, width - 32f), Mathf.Min(300f, height - 32f));
        }

        private void LateUpdate()
        {
            if (loadFailureSafeArea == null) return;
            Rect safe = Screen.safeArea;
            if (Screen.width > 0 && Screen.height > 0 &&
                (Mathf.Abs(safe.xMin / Screen.width - loadFailureSafeArea.anchorMin.x) > 0.001f ||
                 Mathf.Abs(safe.yMin / Screen.height - loadFailureSafeArea.anchorMin.y) > 0.001f ||
                 Mathf.Abs(safe.xMax / Screen.width - loadFailureSafeArea.anchorMax.x) > 0.001f ||
                 Mathf.Abs(safe.yMax / Screen.height - loadFailureSafeArea.anchorMax.y) > 0.001f))
                ApplyFailureSafeArea();

            float width = loadFailureSafeArea.rect.width;
            float height = loadFailureSafeArea.rect.height;
            if (Mathf.Abs(width - failureSafeWidth) > 0.5f || Mathf.Abs(height - failureSafeHeight) > 0.5f)
                LayoutCatLoadFailureUi();
        }

        private static bool TryGetLoadedBounds(LocalCatAssetLoader loader, out Bounds bounds)
        {
            Renderer[] renderers = loader == null || loader.LoadedRoot == null
                ? System.Array.Empty<Renderer>()
                : loader.LoadedRoot.GetComponentsInChildren<Renderer>(true);
            if (renderers.Length == 0)
            {
                bounds = default;
                return false;
            }

            bounds = renderers[0].bounds;
            for (int index = 1; index < renderers.Length; index++)
            {
                bounds.Encapsulate(renderers[index].bounds);
            }

            return true;
        }

        private IEnumerator ValidateStationaryWalkThenStartLocomotion(LocalCatAssetLoader loader)
        {
            // Some imported clips apply a scale curve on a child bone after
            // the GLB starts playing. Correct that at the presentation root so
            // the cat remains visibly sized in the room.
            yield return null;
            if (TryGetLoadedBounds(loader, out Bounds liveBounds) && liveBounds.size.y > 0.001f)
            {
                float desiredHeight = targetHeight * Mathf.Max(1f, presentationScale);
                float correction = desiredHeight / liveBounds.size.y;
                if (Mathf.Abs(correction - 1f) > 0.05f)
                {
                    catModelRoot.localScale *= correction;
                    Debug.Log($"[CatMe][HomeRoom] Applied live presentation correction | renderedHeight={liveBounds.size.y:0.###}m | " +
                              $"targetHeight={desiredHeight:0.###}m | correction={correction:0.###}x");
                }
            }

            Vector3 startPosition = transform.position;
            float maximumHorizontalDrift = 0f;
            float elapsed = 0f;
            while (elapsed < 10f)
            {
                yield return null;
                elapsed += Time.deltaTime;
                Vector3 offset = transform.position - startPosition;
                maximumHorizontalDrift = Mathf.Max(
                    maximumHorizontalDrift,
                    new Vector2(offset.x, offset.z).magnitude);
            }

            float floorDistance = Mathf.Abs(loader.FinalBounds.min.y);
            bool stationary = maximumHorizontalDrift <= 0.02f;
            bool correctHeight = Mathf.Abs(loader.FinalBounds.size.y - targetHeight) <= 0.02f;
            bool onFloor = floorDistance <= 0.01f;
            bool pass = stationary && correctHeight && onFloor;
            IsStationaryAcceptanceComplete = true;
            DidStationaryAcceptancePass = pass;
            string message = $"[CatMe][HomeRoom] 10-second stationary walk acceptance: {(pass ? "PASS" : "FAIL")} | " +
                             $"maxRootDrift={maximumHorizontalDrift:0.####}m | height={loader.FinalBounds.size.y:0.###}m | " +
                             $"floorDistance={floorDistance:0.####}m";
            if (pass)
            {
                Debug.Log(message);
                catMotor = gameObject.AddComponent<CatMotor>();
                catMotor.Initialize(loader);
                CatCallInput callInput = gameObject.AddComponent<CatCallInput>();
                callInput.Initialize(catMotor, roomAudio);
                CatPetReaction petReaction = gameObject.AddComponent<CatPetReaction>();
                petReaction.Initialize(loader, catModelRoot, catMotor, purrClip);
                CatPetInput petInput = gameObject.AddComponent<CatPetInput>();
                petInput.Initialize(petReaction, catMotor);
                CatSleepInteraction sleepInteraction = gameObject.AddComponent<CatSleepInteraction>();
                sleepInteraction.Initialize(loader, catModelRoot, catMotor);
                LaserToyInteraction laserInteraction = gameObject.AddComponent<LaserToyInteraction>();
                laserInteraction.Initialize(catMotor, catModelRoot);
                CatEnergy energy = gameObject.AddComponent<CatEnergy>();
                energy.Initialize(sleepInteraction, laserInteraction);
                CatLocalSave save = gameObject.AddComponent<CatLocalSave>();
                save.Initialize(energy, sleepInteraction, CatLocalSave.ValidationPathOverride);
                CatFeedingInteraction feeding = gameObject.AddComponent<CatFeedingInteraction>();
                feeding.Initialize(catMotor, catModelRoot, roomAudio);
                CatCompanionJournal journal = gameObject.AddComponent<CatCompanionJournal>();
                journal.Initialize(sleepInteraction, loader.FixturePath);
                CatPhotoCapture photoCapture = gameObject.AddComponent<CatPhotoCapture>();
                photoCapture.Initialize(journal);
                CatBallInteraction ballInteraction = gameObject.AddComponent<CatBallInteraction>();
                ballInteraction.Initialize(catMotor);
                CatAmbientBehavior ambientBehavior = gameObject.AddComponent<CatAmbientBehavior>();
                ambientBehavior.Initialize(catMotor);
            }
            else
            {
                Debug.LogError(message);
            }
        }

        private static Transform FindOrCreateChild(Transform parent, string childName)
        {
            Transform child = parent.Find(childName);
            if (child != null)
            {
                return child;
            }

            GameObject childObject = new GameObject(childName);
            childObject.transform.SetParent(parent, false);
            return childObject.transform;
        }

        private static int CountMaterials(Transform root)
        {
            var materials = new System.Collections.Generic.HashSet<Material>();
            foreach (Renderer renderer in root.GetComponentsInChildren<Renderer>(true))
            {
                foreach (Material material in renderer.sharedMaterials)
                {
                    if (material != null)
                    {
                        materials.Add(material);
                    }
                }
            }

            return materials.Count;
        }

        private static bool HasEnabledRenderer(Transform root)
        {
            foreach (Renderer renderer in root.GetComponentsInChildren<Renderer>(true))
            {
                if (renderer.enabled)
                {
                    return true;
                }
            }

            return false;
        }

        private static string FormatClips(LocalCatAssetLoader loader)
        {
            if (loader.DiscoveredClips == null || loader.DiscoveredClips.Length == 0)
            {
                return "none";
            }

            return string.Join(", ", System.Array.ConvertAll(
                loader.DiscoveredClips,
                clip => $"{clip.name} ({clip.length:0.###}s)"));
        }
    }
}
