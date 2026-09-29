using CatMe.CameraSystem;
using CatMe.Cat;
using Unity.AI.Navigation;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;

namespace CatMe.Editor
{
    /// <summary>Applies the approved sparse room without rebuilding gameplay objects.</summary>
    public static class ApprovedHomeRoomPhase
    {
        private const string ScenePath = "Assets/CatMe/Scenes/HomeRoom.unity";
        private const string MaterialFolder = "Assets/CatMe/Art/Materials/";
        private const string GroupName = "ApprovedHomeRoom";

        [MenuItem("CatMe/Apply Approved Home Room")]
        public static void Apply()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
            {
                Debug.LogError("Leave Play Mode before authoring the approved HomeRoom.");
                return;
            }

            Scene scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            Transform room = GameObject.Find("Room_Blockout")?.transform;
            Transform architecture = room?.Find("Architecture");
            Transform props = room?.Find("Props_Blockout");
            Transform navigation = room?.Find("Navigation");
            Transform catRuntime = GameObject.Find("CatRuntime")?.transform;
            Camera camera = room?.Find("CameraRig/Main Camera")?.GetComponent<Camera>();
            if (!scene.IsValid() || room == null || architecture == null || props == null ||
                navigation == null || catRuntime == null || camera == null)
            {
                Debug.LogError("The existing HomeRoom gameplay hierarchy is incomplete; no room edit was saved.");
                return;
            }

            Material wall = Material("ApprovedRoom_WarmWall", new Color(.91f, .83f, .73f), .08f);
            Material floor = Material("ApprovedRoom_OakFloor", new Color(.78f, .59f, .40f), .12f);
            Material wood = Material("ApprovedRoom_PaleWood", new Color(.68f, .43f, .23f), .16f);
            Material trim = Material("ApprovedRoom_SoftTrim", new Color(.62f, .47f, .34f), .10f);
            Material rug = Material("ApprovedRoom_WovenRug", new Color(.77f, .66f, .49f), .04f);
            Material red = Material("ApprovedRoom_BallRed", new Color(.78f, .17f, .13f), .28f);
            Material bowl = Material("ApprovedRoom_Bowl", new Color(.85f, .78f, .69f), .18f);
            Material lampShade = Material("ApprovedRoom_LampShade", new Color(.98f, .84f, .59f), .07f);
            Material daylight = Material("ApprovedRoom_Daylight", new Color(.76f, .87f, .90f), .02f, true);
            Material shadow = Material("ApprovedRoom_CoveShadow", new Color(.77f, .66f, .55f), .05f);
            SetTexture(floor, "Assets/CatMe/Art/Textures/RoomReference_OakGrain.png", new Vector2(1.4f, 1.4f));
            SetTexture(rug, "Assets/CatMe/Art/Textures/RoomReference_WovenRug.png", new Vector2(1.6f, 1.6f));

            Disable(room.Find("ReferenceInterior"));
            Disable(room.Find("ReferenceRoomDetails"));
            Disable(room.Find("ImportedRoomAssets"));
            Transform previous = room.Find(GroupName);
            if (previous != null) Object.DestroyImmediate(previous.gameObject);
            Transform group = new GameObject(GroupName).transform;
            group.SetParent(room, false);

            SetRenderer(architecture.Find("BackWall"), false, null);
            SetRenderer(architecture.Find("Floor"), true, floor);
            SetRenderer(architecture.Find("LeftWall"), true, wall);
            SetRenderer(architecture.Find("RightWall"), true, wall);
            BuildRoomShell(group, wall, floor, wood, trim, shadow, daylight);
            BuildRug(group, rug, trim);
            BuildLamp(group, wood, lampShade);
            BuildBallTable(group, wood, trim, red);
            BuildFoodArea(group, bowl, wood);
            SimplifyExistingProps(props);
            FrameCatAndRoom(room, camera, catRuntime);

            NavMeshSurface surface = navigation.GetComponent<NavMeshSurface>();
            if (surface != null) surface.BuildNavMesh();
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            AssetDatabase.SaveAssets();
            Debug.Log("[CatMe][ApprovedRoom] Sparse rounded room applied; gameplay hierarchy and navigation retained.");
        }

        public static void ApplyFromCommandLine()
        {
            Apply();
            EditorApplication.Exit(0);
        }

        private static void BuildRoomShell(Transform parent, Material wall, Material floor,
            Material wood, Material trim, Material shadow, Material daylight)
        {
            // This shallow cove and its shaded reveal replace the visible beam ceiling.
            Cube("Ceiling", parent, new Vector3(0f, 2.78f, 0f), new Vector3(5.72f, .12f, 5.0f), wall);
            Cove("LeftRoundedCeiling", parent, -1f, -2.5f, 2.5f, shadow);
            Cove("RightRoundedCeiling", parent, 1f, -2.5f, 2.5f, shadow);
            BackCove(parent, shadow);
            Cube("BackFloorTrim", parent, new Vector3(0f, .09f, 2.37f),
                new Vector3(6.35f, .16f, .10f), trim);

            // The authored opaque back wall stays non-rendering so the window reads as an opening.
            Cube("BackLeft", parent, new Vector3(-2.08f, 1.37f, 2.50f), new Vector3(2.34f, 2.74f, .20f), wall);
            Cube("BackRight", parent, new Vector3(2.08f, 1.37f, 2.50f), new Vector3(2.34f, 2.74f, .20f), wall);
            Cube("BackBelowWindow", parent, new Vector3(0f, .46f, 2.50f), new Vector3(1.82f, .92f, .20f), wall);
            Cube("BackAboveWindow", parent, new Vector3(0f, 2.42f, 2.50f), new Vector3(1.82f, .64f, .20f), wall);
            Cube("WindowDaylight", parent, new Vector3(0f, 1.53f, 2.62f),
                new Vector3(1.74f, 1.82f, .02f), daylight);
            Cube("WindowSill", parent, new Vector3(0f, .88f, 2.30f), new Vector3(2.01f, .10f, .37f), wood);
            Cube("WindowLeft", parent, new Vector3(-.92f, 1.53f, 2.31f), new Vector3(.11f, 1.89f, .15f), wood);
            Cube("WindowRight", parent, new Vector3(.92f, 1.53f, 2.31f), new Vector3(.11f, 1.89f, .15f), wood);
            Cube("WindowTop", parent, new Vector3(0f, 2.48f, 2.31f), new Vector3(1.95f, .11f, .15f), wood);
            Cube("WindowCross", parent, new Vector3(0f, 1.55f, 2.29f), new Vector3(1.83f, .06f, .10f), wood);
            Cube("WindowMiddle", parent, new Vector3(0f, 1.53f, 2.29f), new Vector3(.06f, 1.85f, .10f), wood);

            // Readable floor length without the old high-contrast plank grid.
            for (int i = 0; i < 7; i++)
            {
                float x = -2.7f + i * .9f;
                Cube("FloorJoint" + i, parent, new Vector3(x, .002f, -.02f),
                    new Vector3(.003f, .002f, 4.8f), trim, false);
            }
        }

        private static void BuildRug(Transform parent, Material rug, Material edge)
        {
            Cylinder("OvalCarpet", parent, new Vector3(0f, .018f, -.25f),
                new Vector3(3.42f, .018f, 2.05f), rug, false);
            Cylinder("CarpetEdge", parent, new Vector3(0f, .014f, -.25f),
                new Vector3(3.52f, .013f, 2.15f), edge, false);
            parent.Find("OvalCarpet").SetAsLastSibling();
        }

        private static void BuildLamp(Transform parent, Material wood, Material shade)
        {
            Transform lamp = new GameObject("SmallWarmLamp").transform;
            lamp.SetParent(parent, false);
            lamp.localPosition = new Vector3(2.32f, 0f, 1.72f);
            Cylinder("LampBase", lamp, new Vector3(0f, .12f, 0f), new Vector3(.17f, .11f, .17f), wood, false);
            Cylinder("LampStem", lamp, new Vector3(0f, .42f, 0f), new Vector3(.045f, .23f, .045f), wood, false);
            Cylinder("LampShade", lamp, new Vector3(0f, .70f, 0f), new Vector3(.31f, .18f, .31f), shade, false);
            GameObject lightObject = new GameObject("LampGlow");
            lightObject.transform.SetParent(lamp, false);
            lightObject.transform.localPosition = new Vector3(0f, .69f, 0f);
            Light light = lightObject.AddComponent<Light>();
            light.type = LightType.Point;
            light.color = new Color(1f, .77f, .49f);
            light.intensity = .72f;
            light.range = 2.7f;
            light.shadows = LightShadows.None;
        }

        private static void BuildBallTable(Transform parent, Material wood, Material trim, Material red)
        {
            Transform table = new GameObject("BallTable").transform;
            table.SetParent(parent, false);
            table.localPosition = new Vector3(2.30f, 0f, -.58f);
            Cylinder("TableTop", table, new Vector3(0f, .48f, 0f), new Vector3(.83f, .055f, .68f), wood);
            Cylinder("TableEdge", table, new Vector3(0f, .435f, 0f), new Vector3(.81f, .025f, .66f), trim, false);
            foreach (float x in new[] { -.28f, .28f })
                foreach (float z in new[] { -.20f, .20f })
                    Cylinder("TableLeg", table, new Vector3(x, .22f, z), new Vector3(.052f, .22f, .052f), wood);
            // The visible ball is a room-stage placeholder; the later ball phase must
            // make the reusable gameplay ball occupy this point instead of duplicating it.
            Sphere("BallStageMarker", table, new Vector3(0f, .61f, 0f), .15f, red, false);
        }

        private static void BuildFoodArea(Transform parent, Material bowl, Material wood)
        {
            Transform food = new GameObject("FoodAreaFinish").transform;
            food.SetParent(parent, false);
            food.localPosition = new Vector3(-2.35f, 0f, 1.60f);
            Cylinder("FoodBox", food, new Vector3(-.25f, .28f, .17f),
                new Vector3(.22f, .27f, .19f), wood, false);
            Cylinder("FoodBoxLid", food, new Vector3(-.25f, .56f, .17f),
                new Vector3(.24f, .045f, .21f), wood, false);
            // The existing feeding bowl remains the real interaction target.
            Transform feedingNook = GameObject.Find("Room_Blockout")?.transform.Find("Props_Blockout/FeedingNook");
            if (feedingNook == null) return;
            feedingNook.localPosition = new Vector3(-2.04f, 0f, 1.53f);
            Transform liveBowl = feedingNook.Find("Bowl");
            foreach (Renderer renderer in feedingNook.GetComponentsInChildren<Renderer>(true))
            {
                renderer.enabled = renderer.transform == liveBowl || renderer.transform.IsChildOf(liveBowl);
                if (renderer.enabled) renderer.sharedMaterial = bowl;
            }
            foreach (Collider collider in feedingNook.GetComponentsInChildren<Collider>(true))
                collider.enabled = collider.transform == liveBowl || collider.transform.IsChildOf(liveBowl);
            Transform room = GameObject.Find("Room_Blockout")?.transform;
            Transform approach = room?.Find("InteractionPoints/FeedingApproach");
            if (approach != null) approach.localPosition = new Vector3(-1.42f, 0f, 1.45f);
            Transform zone = room?.Find("Zones/FeedingZone");
            if (zone != null) zone.localPosition = new Vector3(-2.04f, 0f, 1.53f);
        }

        private static void SimplifyExistingProps(Transform props)
        {
            foreach (string name in new[] { "Sofa", "WindowPerch", "Rug", "ShopTablet", "MemoryFrame", "FoodPacket" })
                Disable(props.Find(name));
            Transform house = props.Find("CatHouse");
            if (house != null)
            {
                house.localPosition = new Vector3(2.35f, 0f, 1.35f);
                foreach (Renderer renderer in house.GetComponentsInChildren<Renderer>(true))
                    renderer.enabled = false;
            }
            Transform basket = props.Find("ToyBasket");
            if (basket != null)
                foreach (Renderer renderer in basket.GetComponentsInChildren<Renderer>(true)) renderer.enabled = false;
            Transform room = GameObject.Find("Room_Blockout")?.transform;
            Transform sleepApproach = room?.Find("InteractionPoints/SleepApproach");
            if (sleepApproach != null) sleepApproach.localPosition = new Vector3(1.6f, 0f, 1.3f);
            Transform sleepInside = room?.Find("InteractionPoints/SleepInside");
            if (sleepInside != null) sleepInside.localPosition = new Vector3(2.35f, .2f, 1.35f);
        }

        private static void FrameCatAndRoom(Transform room, Camera camera, Transform catRuntime)
        {
            Transform pivot = room.Find("CameraRig/CameraPivot");
            if (pivot != null) pivot.localPosition = new Vector3(0f, 1.05f, .25f);
            camera.transform.position = room.TransformPoint(new Vector3(0f, 2.0f, -4.0f));
            camera.transform.LookAt(room.TransformPoint(new Vector3(0f, 1.05f, .25f)));
            camera.fieldOfView = 50f;
            camera.backgroundColor = new Color(.83f, .73f, .62f);
            RoomOrbitCamera orbit = camera.GetComponent<RoomOrbitCamera>();
            if (orbit == null) orbit = camera.gameObject.AddComponent<RoomOrbitCamera>();
            SerializedObject cameraSettings = new SerializedObject(orbit);
            cameraSettings.FindProperty("fixedRoomView").boolValue = true;
            cameraSettings.FindProperty("minimumDistance").floatValue = 3.8f;
            cameraSettings.FindProperty("maximumDistance").floatValue = 5.0f;
            cameraSettings.FindProperty("roomViewDistance").floatValue = 4.35f;
            cameraSettings.ApplyModifiedPropertiesWithoutUndo();

            HomeRoomCatIntegration integration = catRuntime.GetComponent<HomeRoomCatIntegration>();
            if (integration != null)
            {
                SerializedObject catSettings = new SerializedObject(integration);
                catSettings.FindProperty("presentationScale").floatValue = 4.0f;
                catSettings.ApplyModifiedPropertiesWithoutUndo();
            }
        }

        private static void Cove(string name, Transform parent, float side, float front, float back, Material material)
        {
            const int steps = 18;
            const float radius = .43f;
            Mesh mesh = new Mesh { name = name + "Mesh" };
            Vector3[] vertices = new Vector3[(steps + 1) * 2];
            int[] triangles = new int[steps * 6];
            for (int i = 0; i <= steps; i++)
            {
                float angle = i * Mathf.PI * .5f / steps;
                float x = side * (2.81f + radius * Mathf.Cos(angle));
                float y = 2.31f + radius * Mathf.Sin(angle);
                vertices[i * 2] = new Vector3(x, y, front);
                vertices[i * 2 + 1] = new Vector3(x, y, back);
            }
            for (int i = 0; i < steps; i++)
            {
                int at = i * 6;
                int a = i * 2;
                int b = a + 1;
                int c = a + 2;
                int d = a + 3;
                if (side > 0)
                {
                    triangles[at] = a; triangles[at + 1] = b; triangles[at + 2] = c;
                    triangles[at + 3] = c; triangles[at + 4] = b; triangles[at + 5] = d;
                }
                else
                {
                    triangles[at] = a; triangles[at + 1] = c; triangles[at + 2] = b;
                    triangles[at + 3] = c; triangles[at + 4] = d; triangles[at + 5] = b;
                }
            }
            mesh.vertices = vertices;
            mesh.triangles = triangles;
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
            GameObject objectWithMesh = new GameObject(name, typeof(MeshFilter), typeof(MeshRenderer));
            objectWithMesh.transform.SetParent(parent, false);
            objectWithMesh.GetComponent<MeshFilter>().sharedMesh = mesh;
            objectWithMesh.GetComponent<MeshRenderer>().sharedMaterial = material;
        }

        private static void BackCove(Transform parent, Material material)
        {
            const int steps = 18;
            const float radius = .43f;
            Mesh mesh = new Mesh { name = "BackRoundedCeilingMesh" };
            Vector3[] vertices = new Vector3[(steps + 1) * 2];
            int[] triangles = new int[steps * 6];
            for (int i = 0; i <= steps; i++)
            {
                float angle = i * Mathf.PI * .5f / steps;
                float z = 2.07f + radius * Mathf.Cos(angle);
                float y = 2.31f + radius * Mathf.Sin(angle);
                vertices[i * 2] = new Vector3(-2.82f, y, z);
                vertices[i * 2 + 1] = new Vector3(2.82f, y, z);
            }
            for (int i = 0; i < steps; i++)
            {
                int at = i * 6;
                int a = i * 2;
                int b = a + 1;
                int c = a + 2;
                int d = a + 3;
                triangles[at] = a; triangles[at + 1] = c; triangles[at + 2] = b;
                triangles[at + 3] = c; triangles[at + 4] = d; triangles[at + 5] = b;
            }
            mesh.vertices = vertices;
            mesh.triangles = triangles;
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
            GameObject objectWithMesh = new GameObject("BackRoundedCeiling", typeof(MeshFilter), typeof(MeshRenderer));
            objectWithMesh.transform.SetParent(parent, false);
            objectWithMesh.GetComponent<MeshFilter>().sharedMesh = mesh;
            objectWithMesh.GetComponent<MeshRenderer>().sharedMaterial = material;
        }

        private static Material Material(string name, Color color, float smoothness, bool unlit = false)
        {
            string path = MaterialFolder + name + ".mat";
            Material material = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (material == null)
            {
                Shader shader = Shader.Find(unlit ? "Universal Render Pipeline/Unlit" : "Universal Render Pipeline/Lit");
                material = new Material(shader) { name = name };
                AssetDatabase.CreateAsset(material, path);
            }
            if (material.HasProperty("_BaseColor")) material.SetColor("_BaseColor", color);
            if (material.HasProperty("_Color")) material.SetColor("_Color", color);
            if (material.HasProperty("_Smoothness")) material.SetFloat("_Smoothness", smoothness);
            if (material.HasProperty("_Metallic")) material.SetFloat("_Metallic", 0f);
            EditorUtility.SetDirty(material);
            return material;
        }

        private static void SetTexture(Material material, string path, Vector2 tiling)
        {
            Texture2D texture = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
            if (texture == null) return;
            material.SetTexture("_BaseMap", texture);
            material.SetTextureScale("_BaseMap", tiling);
            EditorUtility.SetDirty(material);
        }

        private static GameObject Cube(string name, Transform parent, Vector3 position, Vector3 scale,
            Material material, bool collider = false)
        {
            GameObject go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            go.name = name;
            go.transform.SetParent(parent, false);
            go.transform.localPosition = position;
            go.transform.localScale = scale;
            go.GetComponent<Renderer>().sharedMaterial = material;
            if (!collider) Object.DestroyImmediate(go.GetComponent<Collider>());
            return go;
        }

        private static GameObject Cylinder(string name, Transform parent, Vector3 position, Vector3 scale,
            Material material, bool collider = false)
        {
            GameObject go = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            go.name = name;
            go.transform.SetParent(parent, false);
            go.transform.localPosition = position;
            go.transform.localScale = scale;
            go.GetComponent<Renderer>().sharedMaterial = material;
            if (!collider) Object.DestroyImmediate(go.GetComponent<Collider>());
            return go;
        }

        private static void Sphere(string name, Transform parent, Vector3 position, float diameter,
            Material material, bool collider)
        {
            GameObject go = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            go.name = name;
            go.transform.SetParent(parent, false);
            go.transform.localPosition = position;
            go.transform.localScale = Vector3.one * diameter;
            go.GetComponent<Renderer>().sharedMaterial = material;
            if (!collider) Object.DestroyImmediate(go.GetComponent<Collider>());
        }

        private static void Disable(Transform target)
        {
            if (target != null) target.gameObject.SetActive(false);
        }

        private static void SetRenderer(Transform target, bool visible, Material material)
        {
            Renderer renderer = target?.GetComponent<Renderer>();
            if (renderer == null) return;
            renderer.enabled = visible;
            if (material != null) renderer.sharedMaterial = material;
        }
    }
}
