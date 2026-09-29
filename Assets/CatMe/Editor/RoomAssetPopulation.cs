using System.IO;
using Unity.AI.Navigation;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace CatMe.Editor
{
    /// <summary>Restores the playable HomeRoom and populates it with the imported cozy-room props.</summary>
    public static class RoomAssetPopulation
    {
        private const string ScenePath = "Assets/CatMe/Scenes/HomeRoom.unity";
        private const string GroupName = "ReferenceRoomDetails";
        private const string OldGroupName = "ImportedRoomAssets";
        private const string SofaPath = "Assets/LowPolyLivingRoomPack/Prefabs/Sofa_2Seat.prefab";
        private const string TallPlantPath = "Assets/LowPolyLivingRoomPack/Prefabs/PottedPlant_Tall_2.prefab";
        private const string SmallPlantPath = "Assets/LowPolyLivingRoomPack/Prefabs/PottedPlant_Small_1.prefab";
        private const string CarpetPath = "Assets/Persian_Carpet_URP/Prefab/Persian_Carpet_URP_024 Variant.prefab";
        private const string ArmchairPath = "Assets/LowPolyLivingRoomPack/Prefabs/Armchair_Classic.prefab";
        private const string SideTablePath = "Assets/LowPolyLivingRoomPack/Prefabs/Side_table_1.prefab";
        private const string TableLampPath = "Assets/LowPolyLivingRoomPack/Prefabs/Night_light_1.prefab";
        private const string PaintingPath = "Assets/LowPolyLivingRoomPack/Prefabs/Painting_Modern_1.prefab";
        private const string ClockPath = "Assets/LowPolyLivingRoomPack/Prefabs/Clock_WallRound.prefab";
        private const string CabinetPath = "Assets/Furniture_ges1/tumba_fur/tumba_fur.FBX";
        private const string BowlPath = "Assets/Low-Poly_Objects_Pack/Prefabs/URP/bowl.prefab";
        private const string WateringCanPath = "Assets/Low-Poly_Objects_Pack/Prefabs/URP/watering_can.prefab";
        private const string CatToyPath = "Assets/gray_cat_toy.glb";

        [MenuItem("CatMe/Restore Reference Home Room")]
        [MenuItem("CatMe/Populate Cozy Room With Imported Props")]
        public static void Apply()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
            {
                Debug.LogError("Exit Play Mode before changing HomeRoom.");
                return;
            }

            // Complete the asset and scene preflight before removing any existing room objects.
            GameObject sofaSource = AssetDatabase.LoadAssetAtPath<GameObject>(SofaPath);
            GameObject tallSource = AssetDatabase.LoadAssetAtPath<GameObject>(TallPlantPath);
            GameObject smallSource = AssetDatabase.LoadAssetAtPath<GameObject>(SmallPlantPath);
            GameObject carpetSource = AssetDatabase.LoadAssetAtPath<GameObject>(CarpetPath);
            GameObject armchairSource = AssetDatabase.LoadAssetAtPath<GameObject>(ArmchairPath);
            GameObject sideTableSource = AssetDatabase.LoadAssetAtPath<GameObject>(SideTablePath);
            GameObject tableLampSource = AssetDatabase.LoadAssetAtPath<GameObject>(TableLampPath);
            GameObject paintingSource = AssetDatabase.LoadAssetAtPath<GameObject>(PaintingPath);
            GameObject clockSource = AssetDatabase.LoadAssetAtPath<GameObject>(ClockPath);
            GameObject cabinetSource = AssetDatabase.LoadAssetAtPath<GameObject>(CabinetPath);
            GameObject bowlSource = AssetDatabase.LoadAssetAtPath<GameObject>(BowlPath);
            GameObject wateringCanSource = AssetDatabase.LoadAssetAtPath<GameObject>(WateringCanPath);
            GameObject catToySource = AssetDatabase.LoadAssetAtPath<GameObject>(CatToyPath);
            if (!HasMesh(sofaSource) || !HasMesh(tallSource) || !HasMesh(smallSource) ||
                !HasMesh(carpetSource) || !HasMesh(armchairSource) || !HasMesh(sideTableSource) ||
                !HasMesh(tableLampSource) || !HasMesh(paintingSource) || !HasMesh(clockSource) ||
                !HasMesh(cabinetSource) || !HasMesh(bowlSource) || !HasMesh(wateringCanSource) || !HasMesh(catToySource))
            {
                Debug.LogError("Cozy room needs the imported living-room, Persian carpet, furniture, bowl, watering-can and cat-toy assets.");
                return;
            }
            Material cream = LoadMaterial("RoomBlockout_Wall");
            Material oak = LoadMaterial("RoomReference_PaleOak") ?? LoadMaterial("RoomBlockout_Wood");
            Material floor = LoadMaterial("RoomBlockout_Floor");
            Material fabric = LoadMaterial("RoomBlockout_Fabric");
            Material daylight = LoadOrCreateDaylightMaterial();
            Material seam = LoadOrCreatePlankSeamMaterial();
            if (cream == null || oak == null || floor == null || fabric == null ||
                daylight == null || seam == null || LoadMaterial("RoomBlockout_Wood") == null || LoadMaterial("RoomBlockout_Accent") == null)
            {
                Debug.LogError("HomeRoom material assets are missing. No scene changes were made.");
                return;
            }

            Scene scene = EditorSceneManager.GetActiveScene();
            if (!scene.IsValid() || scene.path != ScenePath)
                scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            Transform room = GameObject.Find("Room_Blockout")?.transform;
            Transform architecture = room?.Find("Architecture");
            Transform props = room?.Find("Props_Blockout");
            NavMeshSurface nav = room?.Find("Navigation")?.GetComponent<NavMeshSurface>();
            if (room == null || architecture == null || props == null || nav == null ||
                architecture.Find("Floor") == null || architecture.Find("BackWall") == null ||
                props.Find("CatHouse") == null || props.Find("FeedingNook/Bowl") == null ||
                props.Find("ToyBasket/Toy") == null)
            {
                Debug.LogError("HomeRoom is missing its authored architecture, functional props or navigation surface.");
                return;
            }

            DestroyChild(room, OldGroupName);
            DestroyChild(room, "ReferenceInterior");
            DestroyChild(room, GroupName);
            RestoreArchitecture(architecture);
            RestoreFunctionalProps(props);
            RemoveOldDecoration(props);

            // The existing floor texture tinted the scene orange. Keep the imported
            // geometry, but use quiet pale-oak and cream colours from the reference.
            SetColor(cream, new Color(.97f, .93f, .86f));
            SetColor(oak, new Color(.83f, .70f, .53f));
            SetColor(floor, new Color(.95f, .86f, .73f));
            SetColor(fabric, new Color(.88f, .81f, .71f));
            foreach (Material matte in new[] { cream, oak, floor, fabric })
            {
                matte.SetFloat("_Smoothness", .12f);
                matte.SetFloat("_Metallic", 0f);
            }
            if (floor.HasProperty("_BaseMap")) floor.SetTexture("_BaseMap", null);
            if (floor.HasProperty("_MainTex")) floor.SetTexture("_MainTex", null);

            Transform group = NewGroup(GroupName, room);
            SetMaterial(architecture.Find("Floor"), floor);
            SetMaterial(architecture.Find("LeftWall"), cream);
            SetMaterial(architecture.Find("RightWall"), cream);
            BuildBackWallAndWindow(group, cream, oak, daylight);
            BuildCeiling(group, cream, oak);
            BuildFloorSeams(group, seam);
            // Continue the room finish beneath the overview camera; the playable floor
            // collider and authored play zone remain the navigation boundary.
            Cube("ForegroundFloorFinish", group, new Vector3(0, -.1f, -3.75f), new Vector3(6.5f, .2f, 2.5f), floor, false);
            Cube("ForegroundLeftWallFinish", group, new Vector3(-3.25f, 1.4f, -3.75f), new Vector3(.2f, 2.8f, 2.5f), cream, false);
            Cube("ForegroundRightWallFinish", group, new Vector3(3.25f, 1.4f, -3.75f), new Vector3(.2f, 2.8f, 2.5f), cream, false);
            // Keep the physical laser control visible along the right edge of portrait play.
            props.Find("ToyBasket").localPosition = new Vector3(1.9f, 0, -.65f);
            Transform toyApproach = room.Find("InteractionPoints/ToyApproach");
            if (toyApproach != null) toyApproach.localPosition = new Vector3(1.25f, 0, -.65f);
            RestyleFunctionalProps(props, oak, fabric);
            AddSofaAndPlants(group, sofaSource, tallSource, smallSource);
            AddDownloadedProps(group, props, carpetSource, armchairSource, sideTableSource, tableLampSource,
                paintingSource, clockSource, cabinetSource, bowlSource, wateringCanSource, catToySource);
            SetWarmLighting(room);
            FrameCamera(room);

            nav.BuildNavMesh();
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            AssetDatabase.SaveAssets();
            Debug.Log("Reference HomeRoom restored with the downloaded carpet, floor lamp, bonsai and interactive floor toy. Functional props and navigation were rebuilt.");
        }

        [MenuItem("CatMe/Capture Reference Home Room Preview")]
        public static void CapturePreview()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
            {
                Debug.LogError("Capture the edit-mode room preview after leaving Play Mode.");
                return;
            }
            Scene scene = EditorSceneManager.GetActiveScene();
            if (!scene.IsValid() || scene.path != ScenePath)
                scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            Camera camera = GameObject.Find("Room_Blockout/CameraRig/Main Camera")?.GetComponent<Camera>();
            if (camera == null)
            {
                Debug.LogError("HomeRoom Main Camera is missing.");
                return;
            }
            const int width = 768;
            const int height = 1280;
            RenderTexture target = RenderTexture.GetTemporary(width, height, 24);
            RenderTexture oldTarget = camera.targetTexture;
            RenderTexture oldActive = RenderTexture.active;
            Texture2D output = new Texture2D(width, height, TextureFormat.RGB24, false);
            try
            {
                camera.targetTexture = target;
                camera.Render();
                RenderTexture.active = target;
                output.ReadPixels(new Rect(0, 0, width, height), 0, 0);
                output.Apply();
                string path = Path.GetFullPath("Temp/HomeRoomReferencePreview.png");
                Directory.CreateDirectory(Path.GetDirectoryName(path));
                File.WriteAllBytes(path, output.EncodeToPNG());
                Debug.Log("HomeRoom preview: " + path);
            }
            finally
            {
                camera.targetTexture = oldTarget;
                RenderTexture.active = oldActive;
                RenderTexture.ReleaseTemporary(target);
                Object.DestroyImmediate(output);
            }
        }

        private static bool HasMesh(GameObject source) => source != null && source.GetComponentsInChildren<Renderer>(true).Length > 0;
        private static Material LoadMaterial(string name) => AssetDatabase.LoadAssetAtPath<Material>("Assets/CatMe/Art/Materials/" + name + ".mat");

        private static void SetColor(Material material, Color color)
        {
            if (material.HasProperty("_BaseColor")) material.SetColor("_BaseColor", color);
            if (material.HasProperty("_Color")) material.SetColor("_Color", color);
            EditorUtility.SetDirty(material);
        }

        private static Material LoadOrCreateDaylightMaterial()
        {
            const string path = "Assets/CatMe/Art/Materials/RoomReference_Daylight.mat";
            Material material = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (material != null) return material;
            Shader shader = Shader.Find("Universal Render Pipeline/Unlit") ?? Shader.Find("Unlit/Color");
            if (shader == null) return null;
            material = new Material(shader) { name = "RoomReference_Daylight" };
            if (material.HasProperty("_BaseColor")) material.SetColor("_BaseColor", new Color(1f, .98f, .9f));
            else if (material.HasProperty("_Color")) material.SetColor("_Color", new Color(1f, .98f, .9f));
            AssetDatabase.CreateAsset(material, path);
            return material;
        }

        private static Material LoadOrCreatePlankSeamMaterial()
        {
            const string path = "Assets/CatMe/Art/Materials/RoomReference_PlankSeam.mat";
            Material material = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (material != null) return material;
            Shader shader = Shader.Find("Universal Render Pipeline/Unlit") ?? Shader.Find("Unlit/Color");
            if (shader == null) return null;
            material = new Material(shader) { name = "RoomReference_PlankSeam" };
            if (material.HasProperty("_BaseColor")) material.SetColor("_BaseColor", new Color(.66f, .56f, .44f));
            else if (material.HasProperty("_Color")) material.SetColor("_Color", new Color(.66f, .56f, .44f));
            AssetDatabase.CreateAsset(material, path);
            return material;
        }

        private static void RestyleFunctionalProps(Transform props, Material oak, Material fabric)
        {
            Transform house = props.Find("CatHouse");
            if (house == null) return;
            foreach (Renderer renderer in house.GetComponentsInChildren<Renderer>(true))
                renderer.sharedMaterial = renderer.name == "Floor" ? fabric : oak;
            Transform nook = props.Find("FeedingNook");
            if (nook != null)
                foreach (Renderer renderer in nook.GetComponentsInChildren<Renderer>(true))
                    if (renderer.name != "Bowl") renderer.sharedMaterial = oak;
            Transform basket = props.Find("ToyBasket/Basket");
            SetMaterial(basket, oak);
        }

        private static void RestoreArchitecture(Transform architecture)
        {
            foreach (Renderer renderer in architecture.GetComponentsInChildren<Renderer>(true)) renderer.enabled = renderer.name != "BackWall";
            // The opaque authored back wall blocks the actual window aperture, so its collider is disabled too.
            Transform back = architecture.Find("BackWall");
            if (back != null)
            {
                Renderer renderer = back.GetComponent<Renderer>();
                if (renderer != null) renderer.enabled = false;
                Collider collider = back.GetComponent<Collider>();
                if (collider != null) collider.enabled = false;
            }
            Transform floor = architecture.Find("Floor");
            if (floor != null && floor.GetComponent<Collider>() is Collider floorCollider) floorCollider.enabled = true;
        }

        private static void RestoreFunctionalProps(Transform props)
        {
            Material wood = LoadMaterial("RoomBlockout_Wood");
            Material fabric = LoadMaterial("RoomBlockout_Fabric");
            Material accent = LoadMaterial("RoomBlockout_Accent");
            Transform house = props.Find("CatHouse");
            if (house != null && house.Find("Roof") == null)
            {
                for (int i = house.childCount - 1; i >= 0; i--) Object.DestroyImmediate(house.GetChild(i).gameObject);
                Cube("Floor", house, new Vector3(0, .08f, 0), new Vector3(1.1f, .16f, 1), fabric);
                Cube("Back", house, new Vector3(0, .55f, .45f), new Vector3(1.1f, 1, .12f), wood);
                Cube("LeftSide", house, new Vector3(-.49f, .55f, 0), new Vector3(.12f, 1, 1), wood);
                Cube("RightSide", house, new Vector3(.49f, .55f, 0), new Vector3(.12f, 1, 1), wood);
                Cube("Roof", house, new Vector3(0, 1.08f, 0), new Vector3(1.25f, .16f, 1.1f), wood);
            }
            Transform nook = props.Find("FeedingNook");
            if (nook != null && nook.Find("Back") == null)
            {
                Cube("Back", nook, new Vector3(-.2f, .45f, 0), new Vector3(.12f, .9f, 1.1f), wood);
                Cube("Top", nook, new Vector3(-.02f, .95f, 0), new Vector3(.5f, .12f, 1.1f), wood);
                Cube("LeftSide", nook, new Vector3(.05f, .45f, -.5f), new Vector3(.4f, .9f, .12f), wood);
                Cube("RightSide", nook, new Vector3(.05f, .45f, .5f), new Vector3(.4f, .9f, .12f), wood);
            }
            Transform basket = props.Find("ToyBasket");
            if (basket != null && basket.Find("Basket") == null)
            {
                GameObject bowl = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                bowl.name = "Basket";
                bowl.transform.SetParent(basket, false);
                bowl.transform.localPosition = new Vector3(0, .3f, 0);
                bowl.transform.localScale = new Vector3(.45f, .3f, .45f);
                bowl.GetComponent<Renderer>().sharedMaterial = wood;
            }
            Transform toy = props.Find("ToyBasket/Toy");
            if (toy != null)
            {
                DestroyChild(toy, "ImportedGrayCatToy");
                toy.localPosition = new Vector3(0, .7f, 0);
                toy.localScale = new Vector3(.18f, .18f, .18f);
                Renderer renderer = toy.GetComponent<Renderer>();
                if (renderer != null) { renderer.enabled = true; renderer.sharedMaterial = accent; }
                if (toy.GetComponent<Collider>() == null) toy.gameObject.AddComponent<SphereCollider>();
            }
        }

        private static void RemoveOldDecoration(Transform props)
        {
            DestroyChild(props, "Sofa");
            DestroyChild(props, "WindowPerch");
            DestroyChild(props, "Rug");
            DestroyChild(props, "ShopTablet");
            DestroyChild(props, "MemoryFrame");
            // Feeding owns its runtime packet; remove only the obsolete authored blockout packet.
            DestroyChild(props, "FoodPacket");
        }

        private static void BuildBackWallAndWindow(Transform parent, Material cream, Material oak, Material daylightMaterial)
        {
            // Back wall at z=2.5. The central 1.65 m opening is real empty geometry.
            const float z = 2.5f;
            const float radius = .83f;
            const float archBase = 1.65f;
            Cube("BackWallLeft", parent, new Vector3(-2.04f, 1.4f, z), new Vector3(2.42f, 2.8f, .2f), cream);
            Cube("BackWallRight", parent, new Vector3(2.04f, 1.4f, z), new Vector3(2.42f, 2.8f, .2f), cream);
            Cube("BackWallBelowWindow", parent, new Vector3(0, .28f, z), new Vector3(1.66f, .56f, .2f), cream);
            Cube("BackWallAboveWindow", parent, new Vector3(0, 2.64f, z), new Vector3(1.66f, .32f, .2f), cream);
            // Narrow masonry wedges follow the arch, leaving its curved interior unfilled.
            for (int i = 0; i < 12; i++)
            {
                float x = (i + .5f) * radius / 12f;
                float y = archBase + Mathf.Sqrt(radius * radius - x * x);
                float h = 2.48f - y;
                if (h <= .015f) continue;
                Cube("ArchSpandrelL" + i, parent, new Vector3(-x, y + h / 2, z), new Vector3(radius / 12f + .008f, h, .2f), cream);
                Cube("ArchSpandrelR" + i, parent, new Vector3(x, y + h / 2, z), new Vector3(radius / 12f + .008f, h, .2f), cream);
            }
            // A bright exterior surface sits beyond the opening, separated from the wall.
            GameObject daylight = Cube("WindowDaylightBeyond", parent, new Vector3(0, 1.42f, 3.3f), new Vector3(2.0f, 2.3f, .025f), daylightMaterial, false);
            Renderer daylightRenderer = daylight.GetComponent<Renderer>();
            daylightRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            daylightRenderer.receiveShadows = false;
            daylight.transform.localRotation = Quaternion.Euler(0, 180, 0);
            Cube("WindowSill", parent, new Vector3(0, .56f, 2.34f), new Vector3(1.95f, .12f, .38f), oak, false);
            Cube("WindowLeftJamb", parent, new Vector3(-.86f, 1.11f, 2.36f), new Vector3(.12f, 1.1f, .18f), oak, false);
            Cube("WindowRightJamb", parent, new Vector3(.86f, 1.11f, 2.36f), new Vector3(.12f, 1.1f, .18f), oak, false);
            for (int i = 0; i < 20; i++)
            {
                float a0 = Mathf.PI * i / 20;
                float a1 = Mathf.PI * (i + 1) / 20;
                Vector3 p0 = new Vector3(Mathf.Cos(a0) * radius, archBase + Mathf.Sin(a0) * radius, 2.35f);
                Vector3 p1 = new Vector3(Mathf.Cos(a1) * radius, archBase + Mathf.Sin(a1) * radius, 2.35f);
                Vector3 delta = p1 - p0;
                GameObject piece = Cube("TimberArch" + i, parent, (p0 + p1) / 2, new Vector3(.12f, delta.magnitude + .015f, .18f), oak, false);
                piece.transform.localRotation = Quaternion.LookRotation(Vector3.forward, delta.normalized);
            }
            Cube("WindowMuntinVertical", parent, new Vector3(0, 1.52f, 2.34f), new Vector3(.065f, 1.92f, .08f), oak, false);
            Cube("WindowMuntinCross", parent, new Vector3(0, 1.65f, 2.34f), new Vector3(1.68f, .065f, .08f), oak, false);
            Cube("WindowLowerCross", parent, new Vector3(0, 1.05f, 2.34f), new Vector3(1.68f, .05f, .08f), oak, false);
            for (int side = -1; side <= 1; side += 2)
            {
                Vector3 start = new Vector3(0, 1.65f, 2.34f);
                Vector3 end = new Vector3(side * .59f, 2.23f, 2.34f);
                GameObject spoke = Cube("WindowFan" + side, parent, (start + end) * .5f, new Vector3(.055f, Vector3.Distance(start, end), .08f), oak, false);
                spoke.transform.localRotation = Quaternion.LookRotation(Vector3.forward, (end - start).normalized);
            }
        }

        private static void BuildCeiling(Transform parent, Material cream, Material oak)
        {
            Cube("Ceiling", parent, new Vector3(0, 2.84f, -1.25f), new Vector3(6.5f, .1f, 7.5f), cream, false);
            for (int i = 0; i < 3; i++)
                Cube("OakBeam" + i, parent, new Vector3(0, 2.75f, -.3f + i * 1.18f), new Vector3(6.25f, .12f, .16f), oak, false);
        }

        private static void BuildFloorSeams(Transform parent, Material seam)
        {
            // Flat, narrow inlays imply lengthwise oak boards without a repeated image texture.
            for (int i = 0; i < 14; i++)
            {
                float x = -3.05f + i * .47f;
                GameObject line = Cube("PlankSeam" + i, parent, new Vector3(x, .003f, -1.25f), new Vector3(.006f, .002f, 7.5f), seam, false);
                line.GetComponent<Renderer>().shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            }
            for (int i = 0; i < 11; i++)
            {
                float x = -2.82f + i * .47f;
                float z = i % 3 == 0 ? -1.2f : i % 3 == 1 ? .55f : 1.8f;
                GameObject joint = Cube("PlankJoint" + i, parent, new Vector3(x, .003f, z), new Vector3(.45f, .002f, .007f), seam, false);
                joint.GetComponent<Renderer>().shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            }
        }

        private static void AddSofaAndPlants(Transform parent, GameObject sofaSource, GameObject tallSource, GameObject smallSource)
        {
            GameObject sofa = Spawn(sofaSource, "Sofa", parent, new Vector3(-2.4f, 0, .35f), Quaternion.Euler(0, 90, 0));
            FitLongestHorizontalAndGround(sofa, 2.05f);
            CenterOn(sofa, new Vector3(-2.4f, 0, .35f));
            AddFootprintCollider(sofa, .85f, 1.95f);
            GameObject tall = Spawn(tallSource, "TallPlant", parent, new Vector3(-2.65f, 0, 1.72f), Quaternion.identity);
            FitHeightAndGround(tall, 1.17f);
            CenterOn(tall, new Vector3(-2.65f, 0, 1.72f));
            AddFootprintCollider(tall, .46f, .46f);
            GameObject small = Spawn(smallSource, "SmallPlant", parent, new Vector3(-1.75f, 0, 1.96f), Quaternion.identity);
            FitHeightAndGround(small, .76f);
            CenterOn(small, new Vector3(-1.75f, 0, 1.96f));
            AddFootprintCollider(small, .38f, .38f);
        }

        private static void AddDownloadedProps(Transform parent, Transform props, GameObject carpetSource,
            GameObject armchairSource, GameObject sideTableSource, GameObject tableLampSource,
            GameObject paintingSource, GameObject clockSource, GameObject cabinetSource,
            GameObject bowlSource, GameObject wateringCanSource, GameObject catToySource)
        {
            // A patterned Persian rug anchors the room while staying collider-free
            // so it never interrupts the cat's open play routes.
            GameObject carpet = Spawn(carpetSource, "PersianPlayRug", parent, new Vector3(0, 0, -.55f), Quaternion.identity);
            FitLongestHorizontalAndGround(carpet, 2.75f);
            CenterOn(carpet, new Vector3(0, 0, -.55f));

            // A matching armchair balances the sofa and frames a broad, walkable centre.
            GameObject armchair = Spawn(armchairSource, "AccentArmchair", parent, new Vector3(2.25f, 0, .35f), Quaternion.Euler(0, -90f, 0));
            FitLongestHorizontalAndGround(armchair, .92f);
            CenterOn(armchair, new Vector3(2.25f, 0, .35f));
            AddFootprintCollider(armchair, .82f, .82f);

            // Side table and table lamp make a small warm reading corner beside the sofa.
            GameObject sideTable = Spawn(sideTableSource, "ReadingSideTable", parent, new Vector3(-1.55f, 0, -.72f), Quaternion.identity);
            FitHeightAndGround(sideTable, .52f);
            CenterOn(sideTable, new Vector3(-1.55f, 0, -.72f));
            AddFootprintCollider(sideTable, .55f, .48f);
            GameObject tableLamp = Spawn(tableLampSource, "TableLamp", parent, new Vector3(-1.55f, .52f, -.72f), Quaternion.identity);
            FitHeightAndGround(tableLamp, .34f);
            tableLamp.transform.localPosition = new Vector3(-1.55f, .52f, -.72f);
            GameObject lampGlow = new GameObject("TableLampGlow");
            lampGlow.transform.SetParent(parent, false);
            lampGlow.transform.localPosition = new Vector3(-1.55f, .79f, -.72f);
            Light glow = lampGlow.AddComponent<Light>();
            glow.type = LightType.Point;
            glow.color = new Color(1f, .73f, .43f);
            glow.intensity = .38f;
            glow.range = 1.7f;
            glow.shadows = LightShadows.None;

            // A low cabinet occupies the back-right edge, and wall art balances the
            // window without taking floor space away from movement and toys.
            GameObject cabinet = Spawn(cabinetSource, "WoodStorageCabinet", parent, new Vector3(2.35f, 0, 1.63f), Quaternion.Euler(0, 180f, 0));
            FitHeightAndGround(cabinet, .78f);
            CenterOn(cabinet, new Vector3(2.35f, 0, 1.63f));
            AddFootprintCollider(cabinet, .85f, .55f);
            GameObject painting = Spawn(paintingSource, "BackWallPainting", parent, new Vector3(-2.27f, 1.94f, 2.30f), Quaternion.Euler(0, 180f, 0));
            FitHeightAndGround(painting, .74f);
            painting.transform.localPosition = new Vector3(-2.27f, 1.58f, 2.30f);
            GameObject clock = Spawn(clockSource, "BackWallClock", parent, new Vector3(2.20f, 2.06f, 2.30f), Quaternion.Euler(0, 180f, 0));
            FitHeightAndGround(clock, .48f);
            clock.transform.localPosition = new Vector3(2.20f, 1.95f, 2.30f);

            // Use the pack bowl model inside the authored bowl anchor; its root keeps
            // the original collider and interaction contract used by feeding.
            Transform bowlAnchor = props.Find("FeedingNook/Bowl");
            if (bowlAnchor != null)
            {
                foreach (Renderer renderer in bowlAnchor.GetComponents<Renderer>()) renderer.enabled = false;
                GameObject bowl = Spawn(bowlSource, "ImportedLowPolyBowl", bowlAnchor, Vector3.zero, Quaternion.identity);
                FitLongestHorizontalAndGround(bowl, .42f);
                CenterOn(bowl, Vector3.zero);
            }

            // A watering can by the existing plants makes the room feel used, not staged.
            GameObject wateringCan = Spawn(wateringCanSource, "PlantWateringCan", parent, new Vector3(-2.08f, 0, 1.12f), Quaternion.Euler(0, -24f, 0));
            FitLongestHorizontalAndGround(wateringCan, .34f);
            CenterOn(wateringCan, new Vector3(-2.08f, 0, 1.12f));

            // Keep the little mouse toy on the rug as a clear touch target. Tapping it
            // starts the existing physics-ball chase and bat loop.
            GameObject floorToy = Spawn(catToySource, "FloorMouseToy", parent, new Vector3(-.82f, 0, -.78f), Quaternion.Euler(0, 32f, 0));
            FitLongestHorizontalAndGround(floorToy, .30f);
            CenterOn(floorToy, new Vector3(-.82f, 0, -.78f));
            BoxCollider toyTarget = floorToy.AddComponent<BoxCollider>();
            toyTarget.center = new Vector3(0, .13f, 0);
            toyTarget.size = new Vector3(.56f, .30f, .48f);
            floorToy.AddComponent<CatMe.Toys.CatFloorToyInput>();
        }

        private static GameObject Spawn(GameObject source, string name, Transform parent, Vector3 position, Quaternion rotation)
        {
            GameObject wrapper = new GameObject(name);
            wrapper.transform.SetParent(parent, false);
            wrapper.transform.localPosition = position;
            wrapper.transform.localRotation = rotation;
            GameObject imported = PrefabUtility.InstantiatePrefab(source) as GameObject;
            imported.transform.SetParent(wrapper.transform, false);
            // glTF importers may put a required axis and unit correction on the root.
            // Keep the prefab's local rotation and scale; fit only the wrapper.
            foreach (Collider collider in imported.GetComponentsInChildren<Collider>(true)) Object.DestroyImmediate(collider);
            return wrapper;
        }

        private static void FitLongestHorizontalAndGround(GameObject go, float length)
        {
            Bounds bounds = RenderBounds(go);
            float longest = Mathf.Max(bounds.size.x, bounds.size.z);
            if (longest > .001f) go.transform.localScale *= length / longest;
            Ground(go);
        }

        private static void FitHeightAndGround(GameObject go, float height)
        {
            Bounds bounds = RenderBounds(go);
            if (bounds.size.y > .001f) go.transform.localScale *= height / bounds.size.y;
            Ground(go);
        }

        private static void Ground(GameObject go) => go.transform.position += Vector3.up * -RenderBounds(go).min.y;

        private static void CenterOn(GameObject go, Vector3 roomLocalPosition)
        {
            Bounds bounds = RenderBounds(go);
            Vector3 target = go.transform.parent.TransformPoint(roomLocalPosition);
            go.transform.position += new Vector3(target.x - bounds.center.x, 0, target.z - bounds.center.z);
        }

        private static Bounds RenderBounds(GameObject go)
        {
            Renderer[] renderers = go.GetComponentsInChildren<Renderer>(true);
            Bounds bounds = renderers[0].bounds;
            for (int i = 1; i < renderers.Length; i++) bounds.Encapsulate(renderers[i].bounds);
            return bounds;
        }

        private static void AddFootprintCollider(GameObject model, float maxWidth, float maxDepth)
        {
            Bounds b = RenderBounds(model);
            Transform room = model.transform.parent;
            GameObject blocker = new GameObject("NavigationFootprint");
            blocker.transform.SetParent(room, false);
            blocker.transform.position = new Vector3(b.center.x, b.min.y, b.center.z);
            BoxCollider collider = blocker.AddComponent<BoxCollider>();
            collider.center = new Vector3(0, .4f, 0);
            collider.size = new Vector3(
                Mathf.Min(maxWidth, b.size.x / room.lossyScale.x),
                .8f,
                Mathf.Min(maxDepth, b.size.z / room.lossyScale.z));
        }

        private static void SetWarmLighting(Transform room)
        {
            Transform lighting = room.Find("Lighting");
            if (lighting == null) return;
            Light key = lighting.Find("WarmKey")?.GetComponent<Light>();
            if (key != null)
            {
                key.color = new Color(1f, .94f, .84f);
                key.intensity = 1.3f;
                key.shadows = LightShadows.Soft;
                key.shadowBias = .03f;
                key.transform.localRotation = Quaternion.Euler(35f, 155f, 0f);
            }
            Light fill = lighting.Find("SoftFill")?.GetComponent<Light>();
            if (fill != null) { fill.color = new Color(1f, .97f, .9f); fill.intensity = .95f; }
            RenderSettings.ambientLight = new Color(.78f, .74f, .68f);
        }

        private static void FrameCamera(Transform room)
        {
            Camera camera = room.Find("CameraRig/Main Camera")?.GetComponent<Camera>();
            if (camera == null) return;
            camera.transform.position = room.TransformPoint(new Vector3(0, 2.45f, -4.83f));
            camera.transform.LookAt(room.TransformPoint(new Vector3(0, .85f, .3f)));
            camera.fieldOfView = 57f;
            camera.backgroundColor = new Color(.96f, .92f, .85f);
        }

        private static Transform NewGroup(string name, Transform parent)
        {
            GameObject go = new GameObject(name);
            go.transform.SetParent(parent, false);
            return go.transform;
        }

        private static GameObject Cube(string name, Transform parent, Vector3 position, Vector3 scale, Material material, bool collider = true)
        {
            GameObject go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            go.name = name;
            go.transform.SetParent(parent, false);
            go.transform.localPosition = position;
            go.transform.localScale = scale;
            if (material != null) go.GetComponent<Renderer>().sharedMaterial = material;
            if (!collider) Object.DestroyImmediate(go.GetComponent<Collider>());
            return go;
        }

        private static void SetMaterial(Transform target, Material material)
        {
            Renderer renderer = target?.GetComponent<Renderer>();
            if (renderer != null) renderer.sharedMaterial = material;
        }

        private static void DestroyChild(Transform parent, string path)
        {
            Transform child = parent?.Find(path);
            if (child != null) Object.DestroyImmediate(child.gameObject);
        }
    }
}
