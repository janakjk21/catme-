using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using GLTFast;
using UnityEngine;
using Stopwatch = System.Diagnostics.Stopwatch;

namespace CatMe.Cat
{
    /// <summary>
    /// Loads and normalizes the known-good local cat without owning any room or
    /// gameplay presentation. ModelTest and HomeRoom provide those concerns.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class LocalCatAssetLoader : MonoBehaviour
    {
        public const string FixtureRelativePath = "LocalFixtures/Cats/master-walking-cat.glb";
        public const string GeneratedCatCacheFileName = "active-cat.glb";

        [SerializeField] private Transform catModelRoot;
        [SerializeField] private Transform axisCorrection;
        [SerializeField] private float targetHeight = 0.35f;
        [SerializeField] private float forwardYawDegrees;
        [SerializeField] private bool playWalkInPlace = true;
        [SerializeField] private string diagnosticLabel = "CatAsset";

        private GltfImport gltfImport;
        private Transform loadedSceneTransform;
        private Animation legacyAnimation;
        private Transform[] stableRootChildren = Array.Empty<Transform>();
        private Vector3[] stableChildPositions = Array.Empty<Vector3>();
        private Quaternion[] stableChildRotations = Array.Empty<Quaternion>();
        private Vector3[] stableChildScales = Array.Empty<Vector3>();
        private Transform stationaryRootBone;
        private Vector3 stableRootBonePosition;
        private Quaternion stableRootBoneRotation;
        private Vector3 stableRootBoneScale;
        private Vector3 stableScenePosition;
        private Quaternion stableSceneRotation;
        private Vector3 stableSceneScale;
        private bool keepRootStationary;
        private bool loadStarted;
        private bool preferGeneratedCache;
        private bool loadingGeneratedCache;
        private bool generatedCacheFallbackStarted;

        public event Action<LocalCatAssetLoader> Loaded;
        public event Action<LocalCatAssetLoader> Failed;

        public Transform LoadedRoot => loadedSceneTransform;
        public Animation ImportedAnimation => legacyAnimation;
        public AnimationClip SelectedWalkClip { get; private set; }
        public AnimationClip SelectedIdleClip { get; private set; }
        public AnimationClip[] DiscoveredClips { get; private set; } = Array.Empty<AnimationClip>();
        public Bounds FinalBounds { get; private set; }
        public Bounds RawBounds { get; private set; }
        public long FixtureByteSize { get; private set; }
        public long LoadDurationMilliseconds { get; private set; }
        public float AppliedScale { get; private set; }
        public float TargetHeight => targetHeight;
        public float ForwardYawDegrees => forwardYawDegrees;
        public string FixturePath { get; private set; }
        public bool LoadedGeneratedCat => IsLoaded && loadingGeneratedCache;
        public bool IsLoaded { get; private set; }
        public bool HasFailed { get; private set; }

        public void Initialize(
            Transform modelRoot,
            Transform correctionRoot,
            float normalizedHeight,
            float assetForwardYaw,
            bool loopWalkInPlace,
            string logLabel,
            bool preferCachedGeneratedCat = false)
        {
            if (loadStarted)
            {
                return;
            }

            catModelRoot = modelRoot;
            axisCorrection = correctionRoot;
            targetHeight = normalizedHeight;
            forwardYawDegrees = assetForwardYaw;
            playWalkInPlace = loopWalkInPlace;
            diagnosticLabel = string.IsNullOrWhiteSpace(logLabel) ? "CatAsset" : logLabel;
            preferGeneratedCache = preferCachedGeneratedCat;
            BeginLoad();
        }

        /// <summary>Retries only the packaged known-good fixture after a terminal load failure.</summary>
        public void RetryKnownGoodFixture()
        {
            if (!HasFailed || catModelRoot == null || axisCorrection == null)
            {
                return;
            }

            for (int index = axisCorrection.childCount - 1; index >= 0; index--)
            {
                Destroy(axisCorrection.GetChild(index).gameObject);
            }

            catModelRoot.localScale = Vector3.one;
            loadedSceneTransform = null;
            legacyAnimation = null;
            SelectedWalkClip = null;
            SelectedIdleClip = null;
            DiscoveredClips = Array.Empty<AnimationClip>();
            IsLoaded = false;
            HasFailed = false;
            loadingGeneratedCache = false;
            keepRootStationary = false;
            stableRootChildren = Array.Empty<Transform>();
            stableChildPositions = Array.Empty<Vector3>();
            stableChildRotations = Array.Empty<Quaternion>();
            stableChildScales = Array.Empty<Vector3>();
            stationaryRootBone = null;
            loadStarted = true;
            _ = LoadFixtureAsync(forceKnownGoodFixture: true);
        }

        private void Start()
        {
            // Serialized use is supported for isolated diagnostics, while
            // gameplay callers can configure the roots explicitly via Initialize.
            BeginLoad();
        }

        private void LateUpdate()
        {
            if (!keepRootStationary || loadedSceneTransform == null)
            {
                return;
            }

            // Some legacy clips contain translation curves on the imported root.
            // Freeze only those container transforms and the skeleton root while
            // retaining the supplied bone animation.
            loadedSceneTransform.localPosition = stableScenePosition;
            loadedSceneTransform.localRotation = stableSceneRotation;
            loadedSceneTransform.localScale = stableSceneScale;

            for (int index = 0; index < stableRootChildren.Length; index++)
            {
                Transform child = stableRootChildren[index];
                if (child == null)
                {
                    continue;
                }

                child.localPosition = stableChildPositions[index];
                child.localRotation = stableChildRotations[index];
                child.localScale = stableChildScales[index];
            }

            if (stationaryRootBone != null)
            {
                stationaryRootBone.localPosition = stableRootBonePosition;
                stationaryRootBone.localRotation = stableRootBoneRotation;
                stationaryRootBone.localScale = stableRootBoneScale;
            }
        }

        private void BeginLoad()
        {
            if (loadStarted)
            {
                return;
            }

            loadStarted = true;
            if (catModelRoot == null || axisCorrection == null)
            {
                Fail("Cat model root and axis correction must be assigned before loading.");
                return;
            }

            _ = LoadFixtureAsync();
        }

        private async Task LoadFixtureAsync(bool forceKnownGoodFixture = false)
        {
            try
            {
                string generatedCatPath = Path.Combine(Application.persistentDataPath, "Cats", GeneratedCatCacheFileName);
                FixturePath = !forceKnownGoodFixture && preferGeneratedCache && File.Exists(generatedCatPath)
                    ? generatedCatPath
                    : GetKnownGoodFixturePath();
                loadingGeneratedCache = FixturePath == generatedCatPath;
                if (!File.Exists(FixturePath) && !FixturePath.Contains("://"))
                {
                    Fail($"Local cat fixture is missing from the app package: {FixturePath}");
                    return;
                }

                FixtureByteSize = File.Exists(FixturePath) ? new FileInfo(FixturePath).Length : 0;
                string fixtureUri = FixturePath.Contains("://") ? FixturePath : new Uri(FixturePath).AbsoluteUri;
                Stopwatch stopwatch = Stopwatch.StartNew();
                gltfImport = new GltfImport();
                ImportSettings importSettings = new ImportSettings
                {
                    AnimationMethod = AnimationMethod.Legacy,
                    GenerateMipMaps = true,
                    AnisotropicFilterLevel = 2,
                };

                bool loaded;
                try
                {
                    loaded = await gltfImport.Load(fixtureUri, importSettings);
                }
                catch (Exception exception)
                {
                    Fail($"glTF load threw an exception: {exception.Message}");
                    return;
                }

                if (!loaded)
                {
                    Fail($"glTFast could not load the local fixture: {FixturePath}");
                    return;
                }

                DiscoveredClips = gltfImport.GetAnimationClips() ?? Array.Empty<AnimationClip>();
                SelectedWalkClip = SelectWalkClip(DiscoveredClips);
                SelectedIdleClip = DiscoveredClips.FirstOrDefault(clip =>
                    clip != null && clip.length > 0f && clip.name.IndexOf("idle", StringComparison.OrdinalIgnoreCase) >= 0);
                if (SelectedWalkClip == null)
                {
                    Fail("The GLB contains no usable animation clip.");
                    return;
                }

                Transform loadedContainer = new GameObject("LoadedCat").transform;
                loadedContainer.SetParent(axisCorrection, false);
                GameObjectInstantiator instantiator = new GameObjectInstantiator(
                    gltfImport,
                    loadedContainer,
                    settings: new InstantiationSettings
                    {
                        SceneObjectCreation = SceneObjectCreation.Always,
                        SkinUpdateWhenOffscreen = true,
                    });

                bool instantiated;
                try
                {
                    instantiated = await gltfImport.InstantiateMainSceneAsync(instantiator);
                }
                catch (Exception exception)
                {
                    Fail($"glTF instantiation threw an exception: {exception}");
                    return;
                }

                if (!instantiated || instantiator.SceneTransform == null)
                {
                    Fail("glTFast loaded the fixture but could not instantiate its main scene.");
                    return;
                }

                loadedSceneTransform = instantiator.SceneTransform;
                await Task.Yield();
                Physics.SyncTransforms();

                if (loadingGeneratedCache)
                {
                    if (!TryDiscoverGeneratedForwardYaw(out float discoveredYaw))
                    {
                        Fail("Generated cat is missing the Tripo spine markers used to discover its facing direction.");
                        return;
                    }
                    forwardYawDegrees = discoveredYaw;
                }

                if (!TryGetBounds(out Bounds rawBounds))
                {
                    Fail("Loaded cat has no active renderers.");
                    return;
                }

                RawBounds = rawBounds;

                float rawHeight = RawBounds.size.y;
                if (rawHeight <= Mathf.Epsilon)
                {
                    Fail($"Loaded cat has an invalid raw height: {rawHeight}.");
                    return;
                }

                AppliedScale = targetHeight / rawHeight;
                catModelRoot.localScale = Vector3.one * AppliedScale;
                await Task.Yield();
                Physics.SyncTransforms();

                if (!TryGetBounds(out Bounds normalizedBounds))
                {
                    Fail("Cat bounds disappeared after normalization.");
                    return;
                }

                // Keep CatModelRoot at its authored spawn. Offset only the
                // imported scene under AxisCorrection so a room spawn is stable.
                Vector3 anchorPosition = catModelRoot.position;
                Vector3 centerCorrection = new Vector3(
                    anchorPosition.x - normalizedBounds.center.x,
                    anchorPosition.y - normalizedBounds.min.y,
                    anchorPosition.z - normalizedBounds.center.z);
                loadedSceneTransform.position += centerCorrection;
                await Task.Yield();
                Physics.SyncTransforms();

                if (!TryGetBounds(out Bounds finalBounds))
                {
                    Fail("Cat bounds disappeared after centering on its spawn.");
                    return;
                }

                FinalBounds = finalBounds;

                if (!ConfigureAnimation(SelectedWalkClip))
                {
                    return;
                }

                axisCorrection.localRotation = Quaternion.Euler(0f, forwardYawDegrees, 0f);
                stopwatch.Stop();
                LoadDurationMilliseconds = stopwatch.ElapsedMilliseconds;
                IsLoaded = true;
                LogSummary();
                Loaded?.Invoke(this);
            }
            catch (Exception exception)
            {
                Fail($"Unexpected local cat loading error: {exception.Message}");
            }
        }

        private AnimationClip SelectWalkClip(AnimationClip[] clips)
        {
            AnimationClip selected = clips.FirstOrDefault(clip =>
                clip != null && clip.length > 0f && clip.name.IndexOf("walk", StringComparison.OrdinalIgnoreCase) >= 0);

            if (selected == null)
            {
                selected = clips.FirstOrDefault(clip => clip != null && clip.length > 0f);
                if (selected != null)
                {
                    LogWarning($"No clip name contains 'walk'; using '{selected.name}'.");
                }
            }

            return selected;
        }

        private bool ConfigureAnimation(AnimationClip selectedClip)
        {
            legacyAnimation = loadedSceneTransform.GetComponent<Animation>() ?? loadedSceneTransform.GetComponentInChildren<Animation>(true);
            SkinnedMeshRenderer[] skinnedMeshes = loadedSceneTransform.GetComponentsInChildren<SkinnedMeshRenderer>(true);
            if (skinnedMeshes.Length == 0)
            {
                Fail("Loaded cat has no SkinnedMeshRenderer.");
                return false;
            }

            if (legacyAnimation == null || selectedClip == null)
            {
                Fail("Loaded cat has no usable legacy Animation component.");
                return false;
            }

            foreach (AnimationState state in legacyAnimation)
            {
                state.wrapMode = WrapMode.Loop;
            }

            AnimationState selectedState = legacyAnimation[selectedClip.name];
            if (selectedState == null)
            {
                Fail($"Selected clip '{selectedClip.name}' was not attached to the imported Animation component.");
                return false;
            }

            selectedState.wrapMode = WrapMode.Loop;
            legacyAnimation.clip = selectedClip;
            if (playWalkInPlace)
            {
                stableScenePosition = loadedSceneTransform.localPosition;
                stableSceneRotation = loadedSceneTransform.localRotation;
                stableSceneScale = loadedSceneTransform.localScale;
                stableRootChildren = loadedSceneTransform.Cast<Transform>().ToArray();
                stableChildPositions = stableRootChildren.Select(child => child.localPosition).ToArray();
                stableChildRotations = stableRootChildren.Select(child => child.localRotation).ToArray();
                stableChildScales = stableRootChildren.Select(child => child.localScale).ToArray();

                SkinnedMeshRenderer skinnedMesh = loadedSceneTransform.GetComponentInChildren<SkinnedMeshRenderer>(true);
                stationaryRootBone = skinnedMesh == null ? null : skinnedMesh.rootBone;
                if (stationaryRootBone != null)
                {
                    stableRootBonePosition = stationaryRootBone.localPosition;
                    stableRootBoneRotation = stationaryRootBone.localRotation;
                    stableRootBoneScale = stationaryRootBone.localScale;
                }

                keepRootStationary = true;
            }

            legacyAnimation.Play(selectedClip.name, PlayMode.StopAll);
            return true;
        }

        private bool TryGetBounds(out Bounds bounds)
        {
            Renderer[] renderers = loadedSceneTransform == null
                ? Array.Empty<Renderer>()
                : loadedSceneTransform.GetComponentsInChildren<Renderer>(true);

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

        private bool TryDiscoverGeneratedForwardYaw(out float yawDegrees)
        {
            yawDegrees = forwardYawDegrees;
            Transform rear = FindChildByName(loadedSceneTransform, "tripoSpine_0") ??
                             FindChildByName(loadedSceneTransform, "tripo::Spine_0");
            Transform front = FindChildByName(loadedSceneTransform, "tripoSpine_1") ??
                              FindChildByName(loadedSceneTransform, "tripo::Spine_1");
            if (rear == null || front == null)
            {
                return false;
            }

            Vector3 localForward = axisCorrection.InverseTransformDirection(front.position - rear.position);
            if (new Vector2(localForward.x, localForward.z).sqrMagnitude < 0.000001f)
            {
                return false;
            }

            yawDegrees = -Mathf.Atan2(localForward.x, localForward.z) * Mathf.Rad2Deg;
            Log($"Discovered Tripo forward direction from spine markers | assetForwardYaw={yawDegrees:0.###}deg");
            return true;
        }

        private static Transform FindChildByName(Transform root, string expectedName)
        {
            if (root == null) return null;
            foreach (Transform child in root.GetComponentsInChildren<Transform>(true))
            {
                if (string.Equals(child.name, expectedName, StringComparison.OrdinalIgnoreCase)) return child;
            }
            return null;
        }

        private void LogSummary()
        {
            Renderer[] renderers = loadedSceneTransform.GetComponentsInChildren<Renderer>(true);
            SkinnedMeshRenderer[] skinnedMeshes = loadedSceneTransform.GetComponentsInChildren<SkinnedMeshRenderer>(true);
            HashSet<Material> materials = new HashSet<Material>();
            foreach (Renderer renderer in renderers)
            {
                foreach (Material material in renderer.sharedMaterials)
                {
                    if (material != null)
                    {
                        materials.Add(material);
                    }
                }
            }

            string clips = DiscoveredClips.Length == 0
                ? "none"
                : string.Join(", ", DiscoveredClips.Select(clip => $"{clip.name} ({clip.length:0.###}s)"));
            Log($"Asset summary | file={FixturePath} | bytes={FixtureByteSize} | loadMs={LoadDurationMilliseconds} | " +
                $"renderers={renderers.Length} | skinned={skinnedMeshes.Length} | materials={materials.Count} | " +
                $"clips={clips} | selected={SelectedWalkClip.name} | normalizedHeight={FinalBounds.size.y:0.###}m | " +
                $"floorDistance={Mathf.Abs(FinalBounds.min.y - catModelRoot.position.y):0.####}m | " +
                $"assetForwardYaw={forwardYawDegrees:0.###}deg");
        }

        private void Fail(string message)
        {
            if (HasFailed)
            {
                return;
            }

            if (loadingGeneratedCache && !generatedCacheFallbackStarted)
            {
                generatedCacheFallbackStarted = true;
                LogWarning($"Cached generated cat could not be loaded ({message}); retaining it and falling back to the packaged cat.");
                for (int index = axisCorrection.childCount - 1; index >= 0; index--)
                {
                    Destroy(axisCorrection.GetChild(index).gameObject);
                }
                catModelRoot.localScale = Vector3.one;
                loadedSceneTransform = null;
                legacyAnimation = null;
                SelectedWalkClip = null;
                SelectedIdleClip = null;
                DiscoveredClips = Array.Empty<AnimationClip>();
                loadingGeneratedCache = false;
                _ = LoadFixtureAsync(forceKnownGoodFixture: true);
                return;
            }

            HasFailed = true;
            LogError(message);
            Failed?.Invoke(this);
        }

        private static string GetKnownGoodFixturePath()
        {
            string editorFixturePath = Path.GetFullPath(Path.Combine(Application.dataPath, "..", FixtureRelativePath));
            string packagedFixturePath = Path.Combine(Application.streamingAssetsPath, "Cats", "master-walking-cat.glb");
#if UNITY_EDITOR
            // Keep the existing local authoring workflow. Builds load the staged StreamingAssets copy.
            return File.Exists(editorFixturePath) ? editorFixturePath : packagedFixturePath;
#else
            return packagedFixturePath;
#endif
        }

        private void Log(string message) => Debug.Log($"[CatMe][{diagnosticLabel}] {message}");
        private void LogWarning(string message) => Debug.LogWarning($"[CatMe][{diagnosticLabel}] {message}");
        private void LogError(string message) => Debug.LogError($"[CatMe][{diagnosticLabel}] {message}");
    }
}
