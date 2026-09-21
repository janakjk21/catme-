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
    /// Loads the local ModelTest fixture and reports the values that gameplay will
    /// eventually need. This is deliberately isolated from HomeRoom and providers.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class LocalCatModelLoader : MonoBehaviour
    {
        private const string FixtureRelativePath = "LocalFixtures/Cats/mochi-tripo-walk.glb";

        [SerializeField] private Transform catModelRoot;
        [SerializeField] private Transform axisCorrection;
        [SerializeField] private Transform forwardMarker;
        [SerializeField] private Transform cameraRig;
        [SerializeField] private float targetHeight = 0.35f;
        [SerializeField] private float forwardYawDegrees;

        private GltfImport gltfImport;
        private Transform loadedSceneTransform;
        private Animation legacyAnimation;
        private Vector3 stableScenePosition;
        private Quaternion stableSceneRotation;
        private Vector3 stableSceneScale;
        private Transform[] stableRootChildren = Array.Empty<Transform>();
        private Vector3[] stableChildPositions = Array.Empty<Vector3>();
        private Quaternion[] stableChildRotations = Array.Empty<Quaternion>();
        private Vector3[] stableChildScales = Array.Empty<Vector3>();
        private Transform stationaryRootBone;
        private Vector3 stableRootBonePosition;
        private Quaternion stableRootBoneRotation;
        private Vector3 stableRootBoneScale;
        private bool keepRootStationary;
        private bool loadStarted;

        private async void Start()
        {
            if (loadStarted)
            {
                return;
            }

            loadStarted = true;
            EnsureDiagnosticStructure();
            await LoadFixtureAsync();
        }

        private void LateUpdate()
        {
            if (!keepRootStationary || loadedSceneTransform == null)
            {
                return;
            }

            // Legacy Animation can apply root translation curves. Keep the
            // diagnostic model in place while retaining all supplied bone motion.
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

        private async Task LoadFixtureAsync()
        {
            string filePath = Path.GetFullPath(Path.Combine(Application.dataPath, "..", FixtureRelativePath));
            if (!File.Exists(filePath))
            {
                Debug.LogError($"[CatMe][ModelTest] Local cat fixture is missing: {filePath}");
                return;
            }

            long byteSize = new FileInfo(filePath).Length;
            Stopwatch stopwatch = Stopwatch.StartNew();
            string uri = new Uri(filePath).AbsoluteUri;
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
                loaded = await gltfImport.Load(uri, importSettings);
            }
            catch (Exception exception)
            {
                Debug.LogError($"[CatMe][ModelTest] GLB load threw an exception: {exception.Message}");
                return;
            }

            if (!loaded)
            {
                Debug.LogError($"[CatMe][ModelTest] glTFast could not load the local fixture: {filePath}");
                return;
            }

            AnimationClip[] clips = gltfImport.GetAnimationClips() ?? Array.Empty<AnimationClip>();
            AnimationClip selectedClip = SelectWalkClip(clips);
            LogClipDiagnostics(clips, selectedClip);

            Transform loadedContainer = new GameObject("LoadedCat").transform;
            loadedContainer.SetParent(axisCorrection, false);

            InstantiationSettings instantiationSettings = new InstantiationSettings
            {
                SceneObjectCreation = SceneObjectCreation.Always,
                SkinUpdateWhenOffscreen = true,
            };
            GameObjectInstantiator instantiator = new GameObjectInstantiator(
                gltfImport,
                loadedContainer,
                settings: instantiationSettings);

            bool instantiated;
            try
            {
                instantiated = await gltfImport.InstantiateMainSceneAsync(instantiator);
            }
            catch (Exception exception)
            {
                Debug.LogError($"[CatMe][ModelTest] GLB instantiation threw an exception: {exception.Message}");
                return;
            }

            if (!instantiated)
            {
                Debug.LogError("[CatMe][ModelTest] glTFast loaded the fixture but could not instantiate its main scene.");
                return;
            }

            loadedSceneTransform = instantiator.SceneTransform;
            if (loadedSceneTransform == null)
            {
                Debug.LogError("[CatMe][ModelTest] glTFast returned no scene transform for the loaded cat.");
                return;
            }

            await Task.Yield();
            Physics.SyncTransforms();

            if (!TryGetBounds(out Bounds rawBounds))
            {
                Debug.LogError("[CatMe][ModelTest] Loaded cat has no active renderers.");
                return;
            }

            float rawHeight = rawBounds.size.y;
            if (rawHeight <= Mathf.Epsilon)
            {
                Debug.LogError($"[CatMe][ModelTest] Loaded cat has an invalid raw height: {rawHeight}.");
                return;
            }

            float appliedScale = targetHeight / rawHeight;
            catModelRoot.localScale = Vector3.one * appliedScale;
            await Task.Yield();
            Physics.SyncTransforms();

            if (!TryGetBounds(out Bounds normalizedBounds))
            {
                Debug.LogError("[CatMe][ModelTest] Cat bounds disappeared after normalization.");
                return;
            }

            Vector3 centerCorrection = new Vector3(-normalizedBounds.center.x, -normalizedBounds.min.y, -normalizedBounds.center.z);
            catModelRoot.position += centerCorrection;
            await Task.Yield();
            Physics.SyncTransforms();

            TryGetBounds(out Bounds finalBounds);
            ConfigureAnimation(selectedClip);
            ConfigureCamera(finalBounds);
            ConfigureForwardMarker();

            stopwatch.Stop();
            LogModelDiagnostics(filePath, byteSize, stopwatch.ElapsedMilliseconds, rawBounds, finalBounds, appliedScale);
            StartCoroutine(ValidateStationaryWalk(finalBounds));
            Debug.Log("[CatMe][ModelTest] Model validation loaded successfully. Observe the looping walk for at least 10 seconds.");
        }

        private IEnumerator ValidateStationaryWalk(Bounds normalizedBounds)
        {
            Vector3 startPosition = catModelRoot.position;
            float maximumHorizontalDrift = 0f;
            float elapsed = 0f;
            while (elapsed < 10f)
            {
                yield return new WaitForEndOfFrame();
                elapsed += Time.deltaTime;
                Vector3 offset = catModelRoot.position - startPosition;
                maximumHorizontalDrift = Mathf.Max(
                    maximumHorizontalDrift,
                    new Vector2(offset.x, offset.z).magnitude);
            }

            float floorDistance = Mathf.Abs(normalizedBounds.min.y);
            bool stationary = maximumHorizontalDrift <= 0.02f;
            bool correctHeight = Mathf.Abs(normalizedBounds.size.y - targetHeight) <= 0.02f;
            bool onFloor = floorDistance <= 0.01f;
            string result = stationary && correctHeight && onFloor ? "PASS" : "FAIL";
            if (result == "PASS")
            {
                Debug.Log($"[CatMe][ModelTest] 10-second walk acceptance: PASS | maxRootDrift={maximumHorizontalDrift:0.####}m | height={normalizedBounds.size.y:0.###}m | floorDistance={floorDistance:0.####}m");
            }
            else
            {
                Debug.LogError($"[CatMe][ModelTest] 10-second walk acceptance: FAIL | maxRootDrift={maximumHorizontalDrift:0.####}m | height={normalizedBounds.size.y:0.###}m | floorDistance={floorDistance:0.####}m");
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
                    Debug.LogWarning($"[CatMe][ModelTest] No clip name contains 'walk'; using '{selected.name}'.");
                }
            }

            if (selected == null)
            {
                Debug.LogError("[CatMe][ModelTest] The GLB contains no usable animation clip.");
            }

            return selected;
        }

        private void ConfigureAnimation(AnimationClip selectedClip)
        {
            legacyAnimation = loadedSceneTransform.GetComponent<Animation>() ?? loadedSceneTransform.GetComponentInChildren<Animation>(true);
            SkinnedMeshRenderer[] skinnedMeshes = loadedSceneTransform.GetComponentsInChildren<SkinnedMeshRenderer>(true);
            if (skinnedMeshes.Length == 0)
            {
                Debug.LogError("[CatMe][ModelTest] Loaded cat has no SkinnedMeshRenderer.");
            }

            if (legacyAnimation == null || selectedClip == null)
            {
                return;
            }

            foreach (AnimationState state in legacyAnimation)
            {
                state.wrapMode = WrapMode.Loop;
            }

            AnimationState selectedState = legacyAnimation[selectedClip.name];
            if (selectedState == null)
            {
                Debug.LogError($"[CatMe][ModelTest] Selected clip '{selectedClip.name}' was not attached to the imported Animation component.");
                return;
            }

            selectedState.wrapMode = WrapMode.Loop;
            legacyAnimation.clip = selectedClip;
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

            legacyAnimation.Play(selectedClip.name, PlayMode.StopAll);
            keepRootStationary = true;
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

        private void EnsureDiagnosticStructure()
        {
            gameObject.name = "ModelTestRuntime";
            catModelRoot = FindOrCreateChild(transform, "CatModelRoot");
            axisCorrection = FindOrCreateChild(catModelRoot, "AxisCorrection");
            Transform ground = FindOrCreateChild(transform, "Ground");
            forwardMarker = FindOrCreateChild(transform, "ForwardMarker");
            cameraRig = FindOrCreateChild(transform, "CameraRig");
            FindOrCreateChild(transform, "Lighting");

            catModelRoot.localScale = Vector3.one;
            axisCorrection.localRotation = Quaternion.Euler(0f, forwardYawDegrees, 0f);
            CreateGroundIfNeeded(ground);
            CreateForwardMarkerIfNeeded(forwardMarker);

            Camera mainCamera = Camera.main;
            if (mainCamera == null)
            {
                GameObject cameraObject = new GameObject("Main Camera");
                cameraObject.tag = "MainCamera";
                mainCamera = cameraObject.AddComponent<Camera>();
                cameraObject.AddComponent<AudioListener>();
            }

            mainCamera.transform.SetParent(cameraRig, false);
            mainCamera.clearFlags = CameraClearFlags.SolidColor;
            mainCamera.backgroundColor = new Color(0.82f, 0.84f, 0.87f);
            mainCamera.nearClipPlane = 0.01f;
            mainCamera.farClipPlane = 10f;
            mainCamera.fieldOfView = 38f;
        }

        private void ConfigureCamera(Bounds finalBounds)
        {
            Camera mainCamera = Camera.main;
            if (mainCamera == null)
            {
                return;
            }

            Vector3 target = new Vector3(finalBounds.center.x, finalBounds.min.y + finalBounds.size.y * 0.48f, finalBounds.center.z);
            mainCamera.transform.position = target + new Vector3(0.48f, 0.2f, 1.15f);
            mainCamera.transform.rotation = Quaternion.LookRotation(target - mainCamera.transform.position, Vector3.up);
        }

        private void ConfigureForwardMarker()
        {
            if (forwardMarker != null)
            {
                forwardMarker.localRotation = Quaternion.identity;
            }

            axisCorrection.localRotation = Quaternion.Euler(0f, forwardYawDegrees, 0f);
            Debug.Log($"[CatMe][ModelTest] Forward axis: world +Z; applied yaw correction: {forwardYawDegrees:0.###} degrees.");
        }

        private void LogClipDiagnostics(AnimationClip[] clips, AnimationClip selectedClip)
        {
            string clipSummary = clips.Length == 0
                ? "none"
                : string.Join(", ", clips.Select(clip => $"{clip.name} ({clip.length:0.###}s)"));
            Debug.Log($"[CatMe][ModelTest] Embedded clips: {clipSummary}; selected: {(selectedClip == null ? "none" : selectedClip.name)}.");
        }

        private void LogModelDiagnostics(string filePath, long byteSize, long loadMilliseconds, Bounds rawBounds, Bounds finalBounds, float appliedScale)
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

            Debug.Log(
                $"[CatMe][ModelTest] Validation summary | file={filePath} | bytes={byteSize} | loadMs={loadMilliseconds} | " +
                $"renderers={renderers.Length} | skinned={skinnedMeshes.Length} | materials={materials.Count} | " +
                $"rawBounds={FormatBounds(rawBounds)} | normalizedBounds={FormatBounds(finalBounds)} | " +
                $"normalizedHeight={finalBounds.size.y:0.###}m | scale={appliedScale:0.######} | floorOffset={catModelRoot.position.y:0.######}m | " +
                $"forwardYaw={forwardYawDegrees:0.###}deg");
        }

        private static string FormatBounds(Bounds bounds)
        {
            return $"min({bounds.min.x:0.###},{bounds.min.y:0.###},{bounds.min.z:0.###}) max({bounds.max.x:0.###},{bounds.max.y:0.###},{bounds.max.z:0.###})";
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

        private static void CreateGroundIfNeeded(Transform ground)
        {
            if (ground == null || ground.GetComponentInChildren<MeshRenderer>() != null)
            {
                return;
            }

            GameObject plane = GameObject.CreatePrimitive(PrimitiveType.Plane);
            plane.name = "GroundSurface";
            plane.transform.SetParent(ground, false);
            plane.transform.localScale = Vector3.one * 0.35f;
            plane.GetComponent<Renderer>().sharedMaterial = CreateDiagnosticMaterial(
                "CatMe_ModelTest_Ground",
                new Color(0.68f, 0.7f, 0.73f));
        }

        private static void CreateForwardMarkerIfNeeded(Transform marker)
        {
            if (marker == null || marker.GetComponentInChildren<MeshRenderer>() != null)
            {
                return;
            }

            GameObject arrow = GameObject.CreatePrimitive(PrimitiveType.Cube);
            arrow.name = "ForwardArrow";
            arrow.transform.SetParent(marker, false);
            arrow.transform.localPosition = new Vector3(0f, 0.008f, 0.11f);
            arrow.transform.localScale = new Vector3(0.018f, 0.012f, 0.22f);
            arrow.GetComponent<Renderer>().sharedMaterial = CreateDiagnosticMaterial(
                "CatMe_ModelTest_Forward",
                new Color(0.2f, 0.65f, 0.9f));
            Collider collider = arrow.GetComponent<Collider>();
            if (collider != null)
            {
                UnityEngine.Object.Destroy(collider);
            }
        }

        private static Material CreateDiagnosticMaterial(string materialName, Color color)
        {
            Shader shader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
            Material material = new Material(shader)
            {
                name = materialName,
                color = color,
            };
            return material;
        }
    }
}
