using System.IO;
using CatMe.Cat;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace CatMe.Editor
{
    /// <summary>Applies the warm, lived-in reference look to the existing playable room.</summary>
    public static class RoomReferenceInterior
    {
        private const string ScenePath = "Assets/CatMe/Scenes/HomeRoom.unity";
        private const string TexturePath = "Assets/CatMe/Art/Textures";
        private const string DetailsName = "ReferenceInterior";

        [MenuItem("CatMe/Apply Landscape Reference Room")]
        public static void ApplyLandscapeRoomFromCommandLine()
        {
            IOSDevelopmentBuild.SetLandscapeOrientation();
            RoomPresentationTuning.Apply();
            Apply();
            Debug.Log("[CatMe][Landscape] Landscape orientation and reference room have been applied to HomeRoom.");
            EditorApplication.Exit(0);
        }

        [MenuItem("CatMe/Make Home Room Cat Bigger")]
        public static void MakeCatBigger()
        {
            Scene scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            GameObject catRuntime = GameObject.Find("CatRuntime");
            HomeRoomCatIntegration integration = catRuntime == null
                ? null
                : catRuntime.GetComponent<HomeRoomCatIntegration>();
            if (!scene.IsValid() || integration == null)
            {
                Debug.LogError("Could not find the HomeRoom CatRuntime integration.");
                return;
            }

            SerializedObject serialized = new SerializedObject(integration);
            SerializedProperty scale = serialized.FindProperty("presentationScale");
            if (scale == null)
            {
                Debug.LogError("HomeRoom cat presentation scale is missing.");
                return;
            }
            scale.floatValue = 2.688f;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            AssetDatabase.SaveAssets();
            Debug.Log("HomeRoom cat presentation scale set to 2.688x (+20%).");
        }

        public static void Apply()
        {
            Scene scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            GameObject room = GameObject.Find("Room_Blockout");
            if (!scene.IsValid() || room == null)
            {
                Debug.LogError("Could not find HomeRoom or Room_Blockout.");
                return;
            }

            Transform old = room.transform.Find(DetailsName);
            if (old != null) Object.DestroyImmediate(old.gameObject);

            // This shared wall material also styles the back-wall pieces that form
            // the existing true window aperture.
            Material wall = Material("RoomBlockout_Wall", new Color(0.78f, 0.72f, 0.63f), 0.08f);
            Material floor = Material("RoomBlockout_Floor", new Color(0.77f, 0.63f, 0.45f), 0.22f);
            Material wood = Material("RoomBlockout_Wood", new Color(0.35f, 0.17f, 0.07f), 0.24f);
            Material paleWood = Material("RoomReference_PaleOak", new Color(0.73f, 0.46f, 0.22f), 0.24f);
            Material rug = Material("RoomBlockout_Rug", new Color(0.80f, 0.68f, 0.50f), 0.08f);
            Material fabric = Material("RoomBlockout_Fabric", new Color(0.78f, 0.68f, 0.56f), 0.12f);
            Material plant = Material("RoomReference_Leaf", new Color(0.22f, 0.29f, 0.12f), 0.34f);
            Material pot = Material("RoomReference_Terracotta", new Color(0.49f, 0.27f, 0.14f), 0.18f);
            Material glass = Material("RoomBlockout_Glass", new Color(0.96f, 0.85f, 0.67f), 0.18f);
            Material daylight = Material("RoomReference_Daylight", new Color(1f, 0.94f, 0.82f), 0.08f);
            Material tape = Material("RoomReference_Tape", new Color(0.78f, 0.55f, 0.28f), 0.12f);
            Material seam = Material("RoomReference_CardboardSeam", new Color(0.39f, 0.19f, 0.09f), 0.18f);
            Material floorSeam = Material("RoomReference_PlankSeam", new Color(0.46f, 0.27f, 0.14f), 0.18f);
            Material roof = Material("RoomReference_CardboardRoof", new Color(0.86f, 0.65f, 0.42f), 0.14f);

            ApplyFloorAndWallMaterials(wall, floor);
            SetActive("Room_Blockout/ReferenceRoomDetails/ForegroundLeftWallFinish", false);
            SetActive("Room_Blockout/ReferenceRoomDetails/ForegroundRightWallFinish", false);
            Texture2D oak = CreateTiledTexture("RoomReference_OakGrain", false);
            Texture2D weave = CreateTiledTexture("RoomReference_WovenRug", true);
            Texture2D corrugation = CreateTiledTexture("RoomReference_CorrugatedCardboard", false);
            SetTexture(floor, oak, new Vector2(3f, 3f));
            SetTexture(rug, weave, new Vector2(1.8f, 1.8f));
            SetTexture(floorSeam, corrugation, new Vector2(1f, 0.3f));
            SetTexture(roof, corrugation, new Vector2(5f, 1.2f));
            Shader roofShader = Shader.Find("Universal Render Pipeline/Unlit");
            if (roofShader != null)
            {
                roof.shader = roofShader;
                roof.SetColor("_BaseColor", new Color(0.58f, 0.30f, 0.12f));
                roof.SetTexture("_BaseMap", corrugation);
                roof.SetTextureScale("_BaseMap", new Vector2(5f, 1.2f));
            }
            if (roof.HasProperty("_EmissionColor"))
            {
                roof.EnableKeyword("_EMISSION");
                roof.SetColor("_EmissionColor", new Color(0.30f, 0.11f, 0.035f));
            }
            if (daylight.HasProperty("_EmissionColor"))
            {
                daylight.EnableKeyword("_EMISSION");
                daylight.SetColor("_EmissionColor", new Color(1f, 0.45f, 0.09f) * 1.4f);
            }
            if (glass.HasProperty("_EmissionColor"))
            {
                glass.EnableKeyword("_EMISSION");
                glass.SetColor("_EmissionColor", new Color(1f, 0.42f, 0.08f) * 0.7f);
            }

            GameObject details = Empty(DetailsName, room.transform, Vector3.zero);
            SetActive("Room_Blockout/ReferenceRoomDetails/ForegroundLeftWallFinish", false);
            SetActive("Room_Blockout/ReferenceRoomDetails/ForegroundRightWallFinish", false);
            SetActive("Room_Blockout/ReferenceRoomDetails/PersianPlayRug", false);
            Renderer roofRenderer = GameObject.Find("Room_Blockout/ReferenceRoomDetails/Ceiling")?.GetComponent<Renderer>();
            if (roofRenderer != null) roofRenderer.sharedMaterial = roof;
            BuildCeiling(details.transform, wood, paleWood, wall);
            BuildCardboardSeamsAndRepairs(details.transform, seam, tape, paleWood);
            BuildOvalRug(details.transform, rug, paleWood);
            BuildCatTree(details.transform, wood, paleWood, fabric);
            BuildCardboardHideaway(details.transform, seam, tape);
            BuildPendant(details.transform, wood, paleWood);
            RestyleExistingProps(wood, paleWood, fabric);

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("Reference-inspired room materials and cozy interior details applied.");
        }

        private static void ApplyFloorAndWallMaterials(Material wall, Material floor)
        {
            Renderer[] renderers = GameObject.Find("Room_Blockout/Architecture")?.GetComponentsInChildren<Renderer>(true);
            if (renderers == null) return;
            foreach (Renderer renderer in renderers)
            {
                if (renderer.name == "Floor") renderer.sharedMaterial = floor;
                else if (renderer.name.Contains("Wall")) renderer.sharedMaterial = wall;
            }
        }

        private static void RestyleExistingProps(Material wood, Material paleWood, Material fabric)
        {
            SetActive("Room_Blockout/Props_Blockout/Sofa", true);
            SetActive("Room_Blockout/Props_Blockout/WindowPerch", true);
            GameObject sofa = GameObject.Find("Room_Blockout/Props_Blockout/Sofa");
            if (sofa != null) sofa.transform.localPosition = new Vector3(-2.15f, 0f, 0.2f);
            GameObject props = GameObject.Find("Room_Blockout/Props_Blockout");
            if (props == null) return;
            foreach (Renderer renderer in props.GetComponentsInChildren<Renderer>(true))
            {
                string name = renderer.gameObject.name;
                if (name.Contains("Frame") || name == "Shelf" || name == "Roof") renderer.sharedMaterial = paleWood;
                else if (name.Contains("Sofa") || name == "Seat" || name == "Back" || name.StartsWith("Arm")) renderer.sharedMaterial = fabric;
                else if (name == "Glass") renderer.sharedMaterial = Material("RoomBlockout_Glass", new Color(0.73f, 0.81f, 0.79f), 0.24f);
                else if (name == "Back" || name == "LeftSide" || name == "RightSide") renderer.sharedMaterial = wood;
            }
        }

        private static void BuildCeiling(Transform parent, Material beam, Material trim, Material plaster)
        {
            // Leave the roof open to the front camera; the beams still frame the room.
            for (int i = 0; i < 3; i++)
            {
                float z = -1.8f + i * 1.8f;
                Cube("OakCeilingBeam_" + i, parent, new Vector3(0f, 2.67f, z), new Vector3(6.35f, 0.24f, 0.3f), beam, false);
                Cube("BeamLowerTrim_" + i, parent, new Vector3(0f, 2.53f, z), new Vector3(6.18f, 0.045f, 0.25f), trim, false);
            }
            Cube("BackCrownMoulding", parent, new Vector3(0f, 2.70f, 2.35f), new Vector3(6.42f, 0.12f, 0.18f), trim, false);
            Cube("LeftCrownMoulding", parent, new Vector3(-3.1f, 2.56f, 0f), new Vector3(0.16f, 0.12f, 4.7f), trim, false);
            Cube("RightCrownMoulding", parent, new Vector3(3.1f, 2.56f, 0f), new Vector3(0.16f, 0.12f, 4.7f), trim, false);
        }

        private static void BuildWindowDetails(Transform parent, Material wood, Material glass)
        {
            const float x = 0f;
            const float z = 2.32f;
            Cube("WindowGlassWarmTint", parent, new Vector3(x, 1.28f, z), new Vector3(1.54f, 1.3f, 0.04f), glass, false);
            Cube("WindowLeftJamb", parent, new Vector3(x - 0.85f, 1.16f, z - 0.07f), new Vector3(0.14f, 1.48f, 0.16f), wood, false);
            Cube("WindowRightJamb", parent, new Vector3(x + 0.85f, 1.16f, z - 0.07f), new Vector3(0.14f, 1.48f, 0.16f), wood, false);
            Cube("WindowSill", parent, new Vector3(x, 0.43f, z - 0.12f), new Vector3(1.86f, 0.13f, 0.35f), wood, false);
            Cube("WindowCentreMuntin", parent, new Vector3(x, 1.32f, z - 0.08f), new Vector3(0.075f, 1.33f, 0.08f), wood, false);
            Cube("WindowCrossMuntin", parent, new Vector3(x, 1.12f, z - 0.09f), new Vector3(1.62f, 0.07f, 0.08f), wood, false);
            // A segmented half-round frame gives the top of the window its arched profile.
            for (int i = 0; i < 17; i++)
            {
                float a0 = Mathf.PI * i / 17f;
                float a1 = Mathf.PI * (i + 1) / 17f;
                Vector3 p0 = new Vector3(x + Mathf.Cos(a0) * 0.85f, 1.78f + Mathf.Sin(a0) * 0.72f, z - 0.07f);
                Vector3 p1 = new Vector3(x + Mathf.Cos(a1) * 0.85f, 1.78f + Mathf.Sin(a1) * 0.72f, z - 0.07f);
                Vector3 delta = p1 - p0;
                GameObject segment = Cube("WindowArch", parent, (p0 + p1) * 0.5f, new Vector3(0.14f, delta.magnitude + 0.035f, 0.16f), wood, false);
                segment.transform.rotation = Quaternion.LookRotation(Vector3.forward, delta.normalized);
            }
        }

        private static void BuildCardboardSeamsAndRepairs(Transform parent, Material seam, Material tape, Material trim)
        {
            const float back = 2.365f;
            foreach (float x in new[] { -2.75f, -1.85f, 1.85f, 2.75f })
                Cube("BackWallFold", parent, new Vector3(x, 1.4f, back), new Vector3(0.025f, 2.64f, 0.018f), seam, false);

            Cube("BackWallHorizontalFold", parent, new Vector3(0f, 0.69f, back), new Vector3(6.3f, 0.025f, 0.018f), seam, false);
            Cube("BackWallTopFold", parent, new Vector3(0f, 2.62f, back), new Vector3(6.3f, 0.035f, 0.025f), trim, false);
            Cube("CardboardFloorLip", parent, new Vector3(0f, 0.09f, 2.35f), new Vector3(6.35f, 0.18f, 0.08f), trim, false);

            // Small taped patches make the box construction visible without making the room look damaged.
            Cube("RepairPatch_BackLeft", parent, new Vector3(-2.75f, 1.95f, 2.34f), new Vector3(0.5f, 0.26f, 0.035f), tape, false)
                .transform.localRotation = Quaternion.Euler(0f, 0f, -11f);
            Cube("RepairPatch_BackRight", parent, new Vector3(2.62f, 1.05f, 2.34f), new Vector3(0.42f, 0.22f, 0.035f), tape, false)
                .transform.localRotation = Quaternion.Euler(0f, 0f, 8f);
            Cube("TapeStrip_BackLeft", parent, new Vector3(-2.75f, 1.95f, 2.315f), new Vector3(0.055f, 0.36f, 0.025f), trim, false)
                .transform.localRotation = Quaternion.Euler(0f, 0f, -11f);
            Cube("TapeStrip_BackRight", parent, new Vector3(2.62f, 1.05f, 2.315f), new Vector3(0.05f, 0.31f, 0.025f), trim, false)
                .transform.localRotation = Quaternion.Euler(0f, 0f, 8f);

            for (int i = 0; i < 3; i++)
            {
                float z = -1.7f + i * 1.65f;
                Cube("LeftWallFold", parent, new Vector3(-3.10f, 1.4f, z), new Vector3(0.025f, 2.64f, 0.025f), seam, false);
                Cube("RightWallFold", parent, new Vector3(3.10f, 1.4f, z), new Vector3(0.025f, 2.64f, 0.025f), seam, false);
            }
        }

        private static void BuildCardboardHideaway(Transform parent, Material cardboardEdge, Material tape)
        {
            Vector3 center = new Vector3(2.52f, 0f, -0.85f);
            const float width = 0.92f;
            const float height = 0.66f;
            const float depth = 0.82f;
            Cube("CardboardDenFloor", parent, center + new Vector3(0f, 0.08f, 0f), new Vector3(width, 0.16f, depth), tape, false);
            Cube("CardboardDenRoof", parent, center + new Vector3(0f, height - 0.06f, 0f), new Vector3(width, 0.12f, depth), tape, false);
            Cube("CardboardDenBack", parent, center + new Vector3(0f, height * 0.5f, 0.34f), new Vector3(width, height, 0.12f), cardboardEdge, false);
            Cube("CardboardDenLeft", parent, center + new Vector3(-0.40f, height * 0.5f, 0f), new Vector3(0.12f, height, depth), cardboardEdge, false);
            Cube("CardboardDenRight", parent, center + new Vector3(0.40f, height * 0.5f, 0f), new Vector3(0.12f, height, depth), cardboardEdge, false);
            Cube("CardboardDenOpening", parent, center + new Vector3(0f, 0.30f, -0.405f), new Vector3(0.62f, 0.46f, 0.018f),
                Material("RoomReference_DenInterior", new Color(0.12f, 0.055f, 0.025f), 0.05f), false);
            // Leave a low, wide entrance by adding short front flaps rather than a solid front wall.
            Cube("CardboardDenFrontLeft", parent, center + new Vector3(-0.32f, 0.33f, -0.40f), new Vector3(0.16f, height, 0.06f), cardboardEdge, false);
            Cube("CardboardDenFrontRight", parent, center + new Vector3(0.32f, 0.33f, -0.40f), new Vector3(0.16f, height, 0.06f), cardboardEdge, false);
            Cube("CardboardDenFrontTop", parent, center + new Vector3(0f, 0.61f, -0.40f), new Vector3(0.50f, 0.10f, 0.06f), cardboardEdge, false);
        }

        private static void BuildOvalRug(Transform parent, Material rug, Material edge)
        {
            GameObject root = Empty("WovenOvalRug", parent, new Vector3(0f, 0.025f, -0.12f));
            Mesh mesh = DiscMesh(1.58f, 1.05f, 64);
            MeshFilter filter = root.AddComponent<MeshFilter>();
            filter.sharedMesh = mesh;
            MeshRenderer renderer = root.AddComponent<MeshRenderer>();
            renderer.sharedMaterial = rug;
            GameObject border = Empty("RugBorder", root.transform, new Vector3(0f, 0.003f, 0f));
            MeshFilter borderFilter = border.AddComponent<MeshFilter>();
            borderFilter.sharedMesh = RingMesh(1.62f, 1.09f, 1.56f, 1.03f, 64);
            border.AddComponent<MeshRenderer>().sharedMaterial = edge;
        }

        private static void BuildCatTree(Transform parent, Material wood, Material sisal, Material cushion)
        {
            Vector3 p = new Vector3(2.38f, 0f, 0.35f);
            float[] heights = { 0.55f, 1.38f, 2.14f };
            for (int i = 0; i < heights.Length; i++)
            {
                float x = i == 1 ? -0.2f : 0.2f;
                float h = heights[i];
                Cylinder("CatTreeSisalPost_" + i, parent, p + new Vector3(x, h * 0.5f, 0f), new Vector3(0.13f, h * 0.5f, 0.13f), sisal, false);
                Cube("CatTreePlatform_" + i, parent, p + new Vector3(x, h, 0f), new Vector3(0.85f, 0.12f, 0.7f), wood, false);
                Cube("CatTreeCushion_" + i, parent, p + new Vector3(x, h + 0.07f, -0.02f), new Vector3(0.72f, 0.06f, 0.56f), cushion, false);
            }
            Cylinder("CatTreeFrontPost", parent, p + new Vector3(0.26f, 0.47f, -0.34f), new Vector3(0.12f, 0.47f, 0.12f), sisal, false);
            Cube("CatTreeBase", parent, p + new Vector3(0f, 0.1f, 0f), new Vector3(0.95f, 0.2f, 0.82f), wood, false);
            Cube("CatTreeTopBed", parent, p + new Vector3(0.2f, 2.25f, 0f), new Vector3(0.95f, 0.16f, 0.8f), cushion, false);
        }

        private static void BuildPlant(Transform parent, Vector3 position, float scale, Material pot, Material leaf)
        {
            GameObject root = Empty("PottedGreenery", parent, position);
            Cylinder("CeramicPlanter", root.transform, new Vector3(0f, 0.18f, 0f), new Vector3(0.22f, 0.18f, 0.22f), pot, false).transform.localScale *= scale;
            Cylinder("Soil", root.transform, new Vector3(0f, 0.34f, 0f), new Vector3(0.18f, 0.025f, 0.18f), Material("RoomReference_Soil", new Color(0.16f, 0.105f, 0.055f), 1f), false).transform.localScale *= scale;
            for (int i = 0; i < 9; i++)
            {
                float angle = i * 2.39996f;
                float height = (0.45f + (i % 4) * 0.13f) * scale;
                Vector3 direction = new Vector3(Mathf.Cos(angle), 0.65f, Mathf.Sin(angle));
                Vector3 start = new Vector3(0f, 0.34f * scale, 0f);
                Vector3 end = start + direction.normalized * height;
                GameObject stem = Cylinder("LeafStem", root.transform, (start + end) * 0.5f, new Vector3(0.018f, height * 0.5f, 0.018f), leaf, false);
                stem.transform.rotation = Quaternion.FromToRotation(Vector3.up, direction);
                GameObject blade = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                blade.name = "Leaf";
                blade.transform.SetParent(root.transform, false);
                blade.transform.localPosition = end;
                blade.transform.localRotation = Quaternion.Euler(0f, -angle * Mathf.Rad2Deg, 22f);
                blade.transform.localScale = new Vector3(0.11f, 0.035f, 0.2f) * scale;
                blade.GetComponent<Renderer>().sharedMaterial = leaf;
                Collider c = blade.GetComponent<Collider>();
                if (c != null) Object.DestroyImmediate(c);
            }
        }

        private static void BuildPendant(Transform parent, Material wood, Material shade)
        {
            Cylinder("PendantCeilingCap", parent, new Vector3(0.75f, 2.58f, -0.75f), new Vector3(0.12f, 0.045f, 0.12f), wood, false);
            Cylinder("PendantCord", parent, new Vector3(0.75f, 2.42f, -0.75f), new Vector3(0.012f, 0.15f, 0.012f), wood, false);
            GameObject lamp = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            lamp.name = "WarmPendantShade";
            lamp.transform.SetParent(parent, false);
            lamp.transform.localPosition = new Vector3(0.75f, 2.30f, -0.75f);
            lamp.transform.localScale = new Vector3(0.34f, 0.18f, 0.34f);
            lamp.GetComponent<Renderer>().sharedMaterial = shade;
            Collider c = lamp.GetComponent<Collider>();
            if (c != null) Object.DestroyImmediate(c);
            GameObject lightObject = new GameObject("PendantWarmGlow");
            lightObject.transform.SetParent(parent, false);
            lightObject.transform.localPosition = new Vector3(0.75f, 2.18f, -0.75f);
            Light light = lightObject.AddComponent<Light>();
            light.type = LightType.Point;
            light.color = new Color(1f, 0.77f, 0.48f);
            light.intensity = 0.65f;
            light.range = 4f;
            light.shadows = LightShadows.None;
        }

        private static Texture2D CreateTiledTexture(string name, bool woven, bool forceRegenerate = false)
        {
            if (!AssetDatabase.IsValidFolder("Assets/CatMe/Art")) AssetDatabase.CreateFolder("Assets/CatMe", "Art");
            if (!AssetDatabase.IsValidFolder(TexturePath)) AssetDatabase.CreateFolder("Assets/CatMe/Art", "Textures");
            string path = TexturePath + "/" + name + ".png";
            if (File.Exists(path) && !forceRegenerate) return AssetDatabase.LoadAssetAtPath<Texture2D>(path);
            const int size = 512;
            Texture2D texture = new Texture2D(size, size, TextureFormat.RGB24, true) { name = name, wrapMode = TextureWrapMode.Repeat, filterMode = FilterMode.Trilinear };
            bool cardboard = name.Contains("CorrugatedCardboard");
            Color[] pixels = new Color[size * size];
            for (int y = 0; y < size; y++)
            for (int x = 0; x < size; x++)
            {
                float noise = Mathf.PerlinNoise(x * (woven ? 0.09f : cardboard ? 0.025f : 0.012f),
                    y * (woven ? 0.09f : cardboard ? 0.032f : 0.018f));
                float fibers = woven
                    ? ((x + y) % 6 < 3 ? 0.075f : -0.06f) + Mathf.Sin((x - y) * 0.08f) * 0.012f
                    : cardboard
                        ? Mathf.Sin(x * 0.78f + Mathf.Sin(y * 0.017f) * 1.4f) * 0.035f + Mathf.Sin(y * 0.12f) * 0.012f
                        : Mathf.Sin(y * 0.34f + Mathf.Sin(x * 0.018f) * 3f) * 0.025f;
                if (!woven && y % 128 < 2) fibers -= 0.08f;
                float noiseContrast = woven ? 0.18f : 0.12f;
                float shadeValue = Mathf.Clamp(0.92f + (noise - 0.5f) * noiseContrast + fibers, 0.62f, 1.08f);
                pixels[y * size + x] = new Color(shadeValue, shadeValue, shadeValue);
            }
            texture.SetPixels(pixels);
            texture.Apply(true, false);
            File.WriteAllBytes(path, texture.EncodeToPNG());
            Object.DestroyImmediate(texture);
            AssetDatabase.ImportAsset(path);
            TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter;
            if (importer != null)
            {
                importer.wrapMode = TextureWrapMode.Repeat;
                importer.filterMode = FilterMode.Trilinear;
                importer.SaveAndReimport();
            }
            return AssetDatabase.LoadAssetAtPath<Texture2D>(path);
        }

        private static Mesh DiscMesh(float rx, float rz, int steps)
        {
            Vector3[] vertices = new Vector3[steps + 1];
            Vector2[] uv = new Vector2[vertices.Length];
            int[] triangles = new int[steps * 3];
            vertices[0] = Vector3.zero;
            uv[0] = new Vector2(0.5f, 0.5f);
            for (int i = 0; i < steps; i++)
            {
                float a = 2f * Mathf.PI * i / steps;
                vertices[i + 1] = new Vector3(Mathf.Cos(a) * rx, 0f, Mathf.Sin(a) * rz);
                uv[i + 1] = new Vector2(0.5f + Mathf.Cos(a) * 0.5f, 0.5f + Mathf.Sin(a) * 0.5f);
                triangles[i * 3] = 0;
                triangles[i * 3 + 1] = (i + 1) % steps + 1;
                triangles[i * 3 + 2] = i + 1;
            }
            Mesh mesh = new Mesh { name = "WovenOvalMesh", vertices = vertices, uv = uv, triangles = triangles };
            mesh.RecalculateNormals();
            return mesh;
        }

        private static Mesh RingMesh(float outerX, float outerZ, float innerX, float innerZ, int steps)
        {
            Vector3[] vertices = new Vector3[steps * 2];
            Vector2[] uv = new Vector2[vertices.Length];
            int[] triangles = new int[steps * 6];
            for (int i = 0; i < steps; i++)
            {
                float a = 2f * Mathf.PI * i / steps;
                float c = Mathf.Cos(a), s = Mathf.Sin(a);
                vertices[i * 2] = new Vector3(c * outerX, 0f, s * outerZ);
                vertices[i * 2 + 1] = new Vector3(c * innerX, 0f, s * innerZ);
                uv[i * 2] = new Vector2(c * 0.5f + 0.5f, s * 0.5f + 0.5f);
                uv[i * 2 + 1] = uv[i * 2];
                int next = (i + 1) % steps * 2;
                int t = i * 6;
                triangles[t] = i * 2; triangles[t + 1] = i * 2 + 1; triangles[t + 2] = next;
                triangles[t + 3] = i * 2 + 1; triangles[t + 4] = next + 1; triangles[t + 5] = next;
            }
            Mesh mesh = new Mesh { name = "OvalRugBorderMesh", vertices = vertices, uv = uv, triangles = triangles };
            mesh.RecalculateNormals();
            return mesh;
        }

        private static Material Material(string name, Color color, float smoothness)
        {
            string path = "Assets/CatMe/Art/Materials/" + name + ".mat";
            Material material = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (material == null)
            {
                Shader shader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
                material = new Material(shader) { name = name };
                AssetDatabase.CreateAsset(material, path);
            }
            if (material.HasProperty("_BaseColor")) material.SetColor("_BaseColor", color);
            else if (material.HasProperty("_Color")) material.SetColor("_Color", color);
            if (material.HasProperty("_Smoothness")) material.SetFloat("_Smoothness", smoothness);
            return material;
        }

        [MenuItem("CatMe/Art/Regenerate Woven Rug Texture Only")]
        public static void RegenerateWovenRugTextureOnly()
        {
            CreateTiledTexture("RoomReference_WovenRug", true, true);
            AssetDatabase.SaveAssets();
            Debug.Log("Regenerated only the woven-rug texture; HomeRoom was not rebuilt.");
            if (Application.isBatchMode) EditorApplication.Exit(0);
        }

        private static void SetTexture(Material material, Texture texture, Vector2 tiling)
        {
            if (material == null || texture == null) return;
            if (material.HasProperty("_BaseMap")) material.SetTexture("_BaseMap", texture);
            else if (material.HasProperty("_MainTex")) material.SetTexture("_MainTex", texture);
            material.mainTextureScale = tiling;
        }

        private static GameObject Empty(string name, Transform parent, Vector3 position)
        {
            GameObject go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.localPosition = position;
            return go;
        }

        private static GameObject Cube(string name, Transform parent, Vector3 position, Vector3 size, Material material, bool collider)
        {
            GameObject go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            go.name = name;
            go.transform.SetParent(parent, false);
            go.transform.localPosition = position;
            go.transform.localScale = size;
            go.GetComponent<Renderer>().sharedMaterial = material;
            if (!collider) Object.DestroyImmediate(go.GetComponent<Collider>());
            return go;
        }

        private static GameObject Cylinder(string name, Transform parent, Vector3 position, Vector3 scale, Material material, bool collider)
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

        private static void SetActive(string path, bool active)
        {
            GameObject go = GameObject.Find(path);
            if (go != null) go.SetActive(active);
        }
    }
}
