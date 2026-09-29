using System.Collections;
using UnityEngine;

namespace CatMe.Cat
{
    /// <summary>
    /// ModelTest-only presentation and acceptance wrapper. The reusable local
    /// GLB loading is owned by LocalCatAssetLoader.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class LocalCatModelLoader : MonoBehaviour
    {
        [SerializeField] private Transform catModelRoot;
        [SerializeField] private Transform axisCorrection;
        [SerializeField] private Transform forwardMarker;
        [SerializeField] private Transform cameraRig;
        [SerializeField] private float targetHeight = 0.35f;
        [SerializeField] private float forwardYawDegrees;

        private LocalCatAssetLoader assetLoader;
        private bool loadStarted;

        private void Start()
        {
            if (loadStarted)
            {
                return;
            }

            loadStarted = true;
            EnsureDiagnosticStructure();
            assetLoader = gameObject.AddComponent<LocalCatAssetLoader>();
            assetLoader.Loaded += OnAssetLoaded;
            assetLoader.Failed += OnAssetFailed;
            assetLoader.Initialize(
                catModelRoot,
                axisCorrection,
                targetHeight,
                forwardYawDegrees,
                loopWalkInPlace: true,
                logLabel: "ModelTest");
        }

        private void OnAssetLoaded(LocalCatAssetLoader loader)
        {
            ConfigureCamera(loader.FinalBounds);
            ConfigureForwardMarker();
            StartCoroutine(ValidateStationaryWalk(loader));
            Debug.Log("[CatMe][ModelTest] Model validation loaded successfully. Observe the looping walk for at least 10 seconds.");
        }

        private void OnAssetFailed(LocalCatAssetLoader loader)
        {
            Debug.LogError("[CatMe][ModelTest] Model validation stopped because the local asset failed to load.");
        }

        private IEnumerator ValidateStationaryWalk(LocalCatAssetLoader loader)
        {
            Vector3 startPosition = catModelRoot.position;
            float maximumHorizontalDrift = 0f;
            float elapsed = 0f;
            while (elapsed < 10f)
            {
                yield return null;
                elapsed += Time.deltaTime;
                Vector3 offset = catModelRoot.position - startPosition;
                maximumHorizontalDrift = Mathf.Max(
                    maximumHorizontalDrift,
                    new Vector2(offset.x, offset.z).magnitude);
            }

            float floorDistance = Mathf.Abs(loader.FinalBounds.min.y);
            bool stationary = maximumHorizontalDrift <= 0.02f;
            bool correctHeight = Mathf.Abs(loader.FinalBounds.size.y - targetHeight) <= 0.02f;
            bool onFloor = floorDistance <= 0.01f;
            string result = stationary && correctHeight && onFloor ? "PASS" : "FAIL";
            string message = $"[CatMe][ModelTest] 10-second walk acceptance: {result} | " +
                             $"maxRootDrift={maximumHorizontalDrift:0.####}m | height={loader.FinalBounds.size.y:0.###}m | " +
                             $"floorDistance={floorDistance:0.####}m";
            if (result == "PASS")
            {
                Debug.Log(message);
            }
            else
            {
                Debug.LogError(message);
            }
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
            return new Material(shader)
            {
                name = materialName,
                color = color,
            };
        }
    }
}
