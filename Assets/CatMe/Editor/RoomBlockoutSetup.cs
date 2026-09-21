using System.IO;
using Unity.AI.Navigation;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.SceneManagement;

namespace CatMe.Editor
{
    public static class RoomBlockoutSetup
    {
        private const string ScenePath = "Assets/CatMe/Scenes/HomeRoom.unity";
        private const string MaterialFolder = "Assets/CatMe/Art/Materials";

        [MenuItem("CatMe/Build Room Phase 1")]
        public static void BuildRoomPhase1()
        {
            EnsureMaterialFolder();

            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            scene.name = "HomeRoom";

            Material wall = GetMaterial("RoomBlockout_Wall", new Color(0.82f, 0.77f, 0.68f), 0.7f);
            Material floor = GetMaterial("RoomBlockout_Floor", new Color(0.34f, 0.20f, 0.12f), 0.6f);
            Material rug = GetMaterial("RoomBlockout_Rug", new Color(0.46f, 0.34f, 0.25f), 0.85f);
            Material wood = GetMaterial("RoomBlockout_Wood", new Color(0.28f, 0.15f, 0.08f), 0.5f);
            Material fabric = GetMaterial("RoomBlockout_Fabric", new Color(0.47f, 0.29f, 0.21f), 0.95f);
            Material accent = GetMaterial("RoomBlockout_Accent", new Color(0.76f, 0.53f, 0.24f), 0.55f);
            Material glass = GetMaterial("RoomBlockout_Glass", new Color(0.36f, 0.60f, 0.66f), 0.25f);
            Material reference = GetMaterial("RoomBlockout_CatReference", new Color(0.22f, 0.55f, 0.52f), 0.8f);

            GameObject room = Empty("Room_Blockout", null, Vector3.zero);
            GameObject architecture = Empty("Architecture", room.transform, Vector3.zero);
            Primitive("Floor", PrimitiveType.Cube, architecture.transform, new Vector3(0f, -0.1f, 0f), new Vector3(6.5f, 0.2f, 5f), floor);
            Primitive("BackWall", PrimitiveType.Cube, architecture.transform, new Vector3(0f, 1.4f, 2.5f), new Vector3(6.5f, 2.8f, 0.2f), wall);
            Primitive("LeftWall", PrimitiveType.Cube, architecture.transform, new Vector3(-3.25f, 1.4f, 0f), new Vector3(0.2f, 2.8f, 5f), wall);
            Primitive("RightWall", PrimitiveType.Cube, architecture.transform, new Vector3(3.25f, 1.4f, 0f), new Vector3(0.2f, 2.8f, 5f), wall);

            GameObject zones = Empty("Zones", room.transform, Vector3.zero);
            Zone("OpenPlayZone", zones.transform, new Vector3(0f, 0f, 0f), new Vector3(3.2f, 0.02f, 2.4f));
            Zone("WindowZone", zones.transform, new Vector3(-1.8f, 0f, 2.15f), new Vector3(2f, 1.6f, 0.7f));
            Zone("SleepZone", zones.transform, new Vector3(2.15f, 0f, 1.75f), new Vector3(1.3f, 1.2f, 1.3f));
            Zone("FeedingZone", zones.transform, new Vector3(-2.9f, 0f, -1.25f), new Vector3(0.8f, 1f, 1.2f));
            Zone("ToyZone", zones.transform, new Vector3(2.35f, 0f, -1.55f), new Vector3(0.9f, 0.7f, 0.9f));
            Zone("ShopZone", zones.transform, new Vector3(2.85f, 0.8f, -0.25f), new Vector3(0.5f, 1.2f, 0.6f));

            GameObject props = Empty("Props_Blockout", room.transform, Vector3.zero);
            Primitive("Rug", PrimitiveType.Cube, props.transform, new Vector3(0f, 0.01f, -0.1f), new Vector3(3.2f, 0.02f, 2.4f), rug, false);
            BuildSofa(props.transform, new Vector3(2.55f, 0f, 0.65f), fabric);
            BuildWindowPerch(props.transform, new Vector3(-1.8f, 0f, 2.15f), wood, glass);
            BuildCatHouse(props.transform, new Vector3(2.15f, 0f, 1.75f), wood, fabric);
            BuildFeedingNook(props.transform, new Vector3(-2.9f, 0f, -1.25f), wood, accent);
            Primitive("FoodPacket", PrimitiveType.Cube, props.transform, new Vector3(-2.4f, 0.7f, -1.25f), new Vector3(0.25f, 0.45f, 0.08f), accent);
            BuildToyBasket(props.transform, new Vector3(2.35f, 0f, -1.55f), wood, accent);
            BuildShopTablet(props.transform, new Vector3(2.85f, 0.8f, -0.25f), wood, glass);
            Primitive("MemoryFrame", PrimitiveType.Cube, props.transform, new Vector3(-2.4f, 1.45f, 2.38f), new Vector3(0.85f, 0.7f, 0.05f), accent);

            GameObject points = Empty("InteractionPoints", room.transform, Vector3.zero);
            Point("CallDestination", points.transform, new Vector3(0f, 0f, -1.35f), Quaternion.identity);
            Point("WindowApproach", points.transform, new Vector3(-1.8f, 0f, 1.2f), Quaternion.Euler(0f, 180f, 0f));
            Point("SleepApproach", points.transform, new Vector3(1.4f, 0f, 1.0f), Quaternion.identity);
            Point("SleepInside", points.transform, new Vector3(2.15f, 0.2f, 1.75f), Quaternion.identity);
            Point("FeedingApproach", points.transform, new Vector3(-2.25f, 0f, -1.25f), Quaternion.Euler(0f, -90f, 0f));
            Point("ToyApproach", points.transform, new Vector3(1.65f, 0f, -1.55f), Quaternion.Euler(0f, 90f, 0f));
            Point("ShopViewPoint", points.transform, new Vector3(2.35f, 0.9f, -0.25f), Quaternion.Euler(0f, 90f, 0f));

            GameObject cameraRig = Empty("CameraRig", room.transform, Vector3.zero);
            Point("CameraPivot", cameraRig.transform, new Vector3(0f, 0.75f, 0f), Quaternion.identity);
            Transform home = Point("HomeAnchor", cameraRig.transform, new Vector3(0f, 3.4f, -6.8f), Quaternion.identity);
            Transform left = Point("LeftAnchor", cameraRig.transform, new Vector3(-5f, 3.0f, -3.2f), Quaternion.identity);
            Transform right = Point("RightAnchor", cameraRig.transform, new Vector3(5f, 3.0f, -3.2f), Quaternion.identity);
            LookAt(home, new Vector3(0f, 0.7f, 0.2f));
            LookAt(left, new Vector3(-0.25f, 0.7f, 0.35f));
            LookAt(right, new Vector3(0.25f, 0.7f, 0.35f));

            GameObject cameraObject = new GameObject("Main Camera");
            cameraObject.transform.SetPositionAndRotation(home.position, home.rotation);
            cameraObject.transform.SetParent(cameraRig.transform, true);
            Camera camera = cameraObject.AddComponent<Camera>();
            camera.tag = "MainCamera";
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(0.72f, 0.75f, 0.77f);
            camera.nearClipPlane = 0.05f;
            camera.farClipPlane = 100f;
            camera.fieldOfView = 42f;
            cameraObject.AddComponent<AudioListener>();

            GameObject referenceRoot = Empty("ScaleReference", room.transform, Vector3.zero);
            GameObject catReference = Primitive("CatScaleReference", PrimitiveType.Capsule, referenceRoot.transform, new Vector3(0f, 0.175f, -0.5f), new Vector3(0.1f, 0.175f, 0.1f), reference);
            Collider referenceCollider = catReference.GetComponent<Collider>();
            if (referenceCollider != null)
            {
                Object.DestroyImmediate(referenceCollider);
            }

            GameObject navigation = Empty("Navigation", room.transform, Vector3.zero);
            NavMeshSurface surface = navigation.AddComponent<NavMeshSurface>();
            surface.collectObjects = CollectObjects.All;
            surface.useGeometry = NavMeshCollectGeometry.PhysicsColliders;
            surface.layerMask = ~0;

            GameObject lighting = Empty("Lighting", room.transform, Vector3.zero);
            CreateLight("WarmKey", lighting.transform, LightType.Directional, new Vector3(0f, 3f, -2f), new Color(1f, 0.88f, 0.72f), 1.15f, Quaternion.Euler(48f, -28f, 0f));
            CreateLight("SoftFill", lighting.transform, LightType.Point, new Vector3(-1.5f, 2.5f, -1.5f), new Color(0.62f, 0.75f, 1f), 0.8f, Quaternion.identity, 8f);
            RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
            RenderSettings.ambientLight = new Color(0.24f, 0.25f, 0.27f);
            RenderSettings.fog = false;

            EditorSceneManager.SaveScene(scene, ScenePath);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            surface.BuildNavMesh();
            ValidateNavigation(points.transform);
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene, ScenePath);
            Debug.Log("CatMe Room Phase 1 blockout built and NavMesh baked.");
        }

        public static void BuildRoomPhase1FromCommandLine()
        {
            BuildRoomPhase1();
        }

        private static GameObject Empty(string name, Transform parent, Vector3 position)
        {
            GameObject objectRoot = new GameObject(name);
            objectRoot.transform.SetParent(parent, false);
            objectRoot.transform.localPosition = position;
            return objectRoot;
        }

        private static GameObject Primitive(string name, PrimitiveType type, Transform parent, Vector3 position, Vector3 scale, Material material, bool collider = true)
        {
            GameObject item = GameObject.CreatePrimitive(type);
            item.name = name;
            item.transform.SetParent(parent, false);
            item.transform.localPosition = position;
            item.transform.localScale = scale;
            item.isStatic = true;
            Renderer renderer = item.GetComponent<Renderer>();
            if (renderer != null)
            {
                renderer.sharedMaterial = material;
            }

            if (!collider)
            {
                Collider itemCollider = item.GetComponent<Collider>();
                if (itemCollider != null)
                {
                    Object.DestroyImmediate(itemCollider);
                }
            }

            return item;
        }

        private static void Zone(string name, Transform parent, Vector3 position, Vector3 size)
        {
            GameObject zone = Empty(name, parent, position);
            zone.transform.localScale = size;
        }

        private static Transform Point(string name, Transform parent, Vector3 position, Quaternion rotation)
        {
            GameObject point = Empty(name, parent, position);
            point.transform.localRotation = rotation;
            return point.transform;
        }

        private static void BuildSofa(Transform parent, Vector3 position, Material material)
        {
            GameObject root = Empty("Sofa", parent, position);
            Primitive("Seat", PrimitiveType.Cube, root.transform, new Vector3(0f, 0.45f, 0f), new Vector3(1.5f, 0.35f, 0.9f), material);
            Primitive("Back", PrimitiveType.Cube, root.transform, new Vector3(0f, 1.05f, 0.35f), new Vector3(1.5f, 0.9f, 0.25f), material);
            Primitive("ArmLeft", PrimitiveType.Cube, root.transform, new Vector3(-0.68f, 0.72f, 0f), new Vector3(0.18f, 0.55f, 0.9f), material);
            Primitive("ArmRight", PrimitiveType.Cube, root.transform, new Vector3(0.68f, 0.72f, 0f), new Vector3(0.18f, 0.55f, 0.9f), material);
        }

        private static void BuildWindowPerch(Transform parent, Vector3 position, Material wood, Material glass)
        {
            GameObject root = Empty("WindowPerch", parent, position);
            Primitive("Glass", PrimitiveType.Cube, root.transform, new Vector3(0f, 1.3f, 0.28f), new Vector3(1.8f, 1.2f, 0.05f), glass);
            Primitive("Shelf", PrimitiveType.Cube, root.transform, new Vector3(0f, 0.72f, 0f), new Vector3(1.8f, 0.12f, 0.65f), wood);
            Primitive("LeftFrame", PrimitiveType.Cube, root.transform, new Vector3(-0.88f, 1.3f, 0.2f), new Vector3(0.08f, 1.25f, 0.1f), wood);
            Primitive("RightFrame", PrimitiveType.Cube, root.transform, new Vector3(0.88f, 1.3f, 0.2f), new Vector3(0.08f, 1.25f, 0.1f), wood);
        }

        private static void BuildCatHouse(Transform parent, Vector3 position, Material wood, Material fabric)
        {
            GameObject root = Empty("CatHouse", parent, position);
            Primitive("Floor", PrimitiveType.Cube, root.transform, new Vector3(0f, 0.08f, 0f), new Vector3(1.1f, 0.16f, 1f), fabric);
            Primitive("Back", PrimitiveType.Cube, root.transform, new Vector3(0f, 0.55f, 0.45f), new Vector3(1.1f, 1f, 0.12f), wood);
            Primitive("LeftSide", PrimitiveType.Cube, root.transform, new Vector3(-0.49f, 0.55f, 0f), new Vector3(0.12f, 1f, 1f), wood);
            Primitive("RightSide", PrimitiveType.Cube, root.transform, new Vector3(0.49f, 0.55f, 0f), new Vector3(0.12f, 1f, 1f), wood);
            Primitive("Roof", PrimitiveType.Cube, root.transform, new Vector3(0f, 1.08f, 0f), new Vector3(1.25f, 0.16f, 1.1f), wood);
        }

        private static void BuildFeedingNook(Transform parent, Vector3 position, Material wood, Material accent)
        {
            GameObject root = Empty("FeedingNook", parent, position);
            Primitive("Back", PrimitiveType.Cube, root.transform, new Vector3(-0.2f, 0.45f, 0f), new Vector3(0.12f, 0.9f, 1.1f), wood);
            Primitive("Top", PrimitiveType.Cube, root.transform, new Vector3(-0.02f, 0.95f, 0f), new Vector3(0.5f, 0.12f, 1.1f), wood);
            Primitive("LeftSide", PrimitiveType.Cube, root.transform, new Vector3(0.05f, 0.45f, -0.5f), new Vector3(0.4f, 0.9f, 0.12f), wood);
            Primitive("RightSide", PrimitiveType.Cube, root.transform, new Vector3(0.05f, 0.45f, 0.5f), new Vector3(0.4f, 0.9f, 0.12f), wood);
            Primitive("Bowl", PrimitiveType.Cylinder, root.transform, new Vector3(-0.03f, 0.18f, 0f), new Vector3(0.28f, 0.08f, 0.28f), accent);
        }

        private static void BuildToyBasket(Transform parent, Vector3 position, Material wood, Material accent)
        {
            GameObject root = Empty("ToyBasket", parent, position);
            Primitive("Basket", PrimitiveType.Cylinder, root.transform, new Vector3(0f, 0.3f, 0f), new Vector3(0.45f, 0.3f, 0.45f), wood);
            Primitive("Toy", PrimitiveType.Sphere, root.transform, new Vector3(0f, 0.7f, 0f), new Vector3(0.18f, 0.18f, 0.18f), accent);
        }

        private static void BuildShopTablet(Transform parent, Vector3 position, Material wood, Material glass)
        {
            GameObject root = Empty("ShopTablet", parent, position);
            Primitive("Stand", PrimitiveType.Cube, root.transform, new Vector3(0f, -0.35f, 0f), new Vector3(0.12f, 0.7f, 0.25f), wood);
            Primitive("Screen", PrimitiveType.Cube, root.transform, new Vector3(0f, 0.25f, 0f), new Vector3(0.08f, 0.65f, 0.45f), glass);
        }

        private static void CreateLight(string name, Transform parent, LightType type, Vector3 position, Color color, float intensity, Quaternion rotation, float range = 10f)
        {
            GameObject lightObject = new GameObject(name);
            lightObject.transform.SetParent(parent, false);
            lightObject.transform.localPosition = position;
            lightObject.transform.localRotation = rotation;
            Light light = lightObject.AddComponent<Light>();
            light.type = type;
            light.color = color;
            light.intensity = intensity;
            light.range = range;
            light.shadows = type == LightType.Directional ? LightShadows.Soft : LightShadows.None;
        }

        private static void LookAt(Transform transform, Vector3 target)
        {
            transform.rotation = Quaternion.LookRotation(target - transform.position, Vector3.up);
        }

        private static void ValidateNavigation(Transform points)
        {
            Vector3 start = new Vector3(0f, 0.05f, 0f);
            string[] requiredPoints = { "CallDestination", "WindowApproach", "SleepApproach", "FeedingApproach", "ToyApproach" };
            NavMeshPath path = new NavMeshPath();
            bool allReachable = true;

            if (!NavMesh.SamplePosition(start, out NavMeshHit startHit, 0.5f, NavMesh.AllAreas))
            {
                Debug.LogError("Room Phase 1 NavMesh validation failed: open play center is not walkable.");
                return;
            }

            foreach (string pointName in requiredPoints)
            {
                Transform point = points.Find(pointName);
                bool reachable = point != null
                    && NavMesh.SamplePosition(point.position, out NavMeshHit targetHit, 0.5f, NavMesh.AllAreas)
                    && NavMesh.CalculatePath(startHit.position, targetHit.position, NavMesh.AllAreas, path)
                    && path.status == NavMeshPathStatus.PathComplete;

                if (reachable)
                {
                    Debug.Log("Room Phase 1 NavMesh path complete: " + pointName);
                }
                else
                {
                    allReachable = false;
                    Debug.LogError("Room Phase 1 NavMesh path missing: " + pointName);
                }
            }

            if (allReachable)
            {
                Debug.Log("Room Phase 1 NavMesh validation passed for all required approach points.");
            }
        }

        private static Material GetMaterial(string name, Color color, float smoothness)
        {
            string path = Path.Combine(MaterialFolder, name + ".mat").Replace("\\", "/");
            Material material = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (material == null)
            {
                Shader shader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
                material = new Material(shader) { name = name };
                AssetDatabase.CreateAsset(material, path);
            }

            if (material.HasProperty("_BaseColor"))
            {
                material.SetColor("_BaseColor", color);
            }
            else if (material.HasProperty("_Color"))
            {
                material.SetColor("_Color", color);
            }

            if (material.HasProperty("_Smoothness"))
            {
                material.SetFloat("_Smoothness", smoothness);
            }

            return material;
        }

        private static void EnsureMaterialFolder()
        {
            if (!AssetDatabase.IsValidFolder("Assets/CatMe/Art/Materials"))
            {
                AssetDatabase.CreateFolder("Assets/CatMe/Art", "Materials");
            }
        }
    }
}
