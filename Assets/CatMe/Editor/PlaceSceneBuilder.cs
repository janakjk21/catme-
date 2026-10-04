#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using CatMe.Garden;
using CatMe.Session;
using CatMe.World;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

namespace CatMe.Editor
{
    /// <summary>
    /// Generates the five place scenes described in docs/production/04 as terrain + blockout + dressing from the
    /// existing garden assets. It writes only Assets/CatMe/Scenes/Places and Assets/CatMe/Art/Places, never the
    /// legacy GardenEnvironment prefab. Re-running replaces generated scenes, so keep manual edits in copies.
    /// </summary>
    public static class PlaceSceneBuilder
    {
        const string SceneFolder = "Assets/CatMe/Scenes/Places";
        const string ArtFolder = "Assets/CatMe/Art/Places";
        const string LegacyPrefab = "Assets/CatMe/Resources/CompanionGarden/GardenEnvironment.prefab";
        const string ShellScene = "Assets/CatMe/Scenes/GardenCompanion.unity";
        const int Res = 257;
        const string Ghibli = "Assets/3D set of stylized nature - GHIBLI style/Art/Prefabs/";
        const string ToriiFolder = "Assets/JTS_Shrine_Torii_Gates/Prefabs/";
        const string AnimeFolder = "Assets/Anime Trees/Prefabs/";
        const string WaterMaterialPath = "Assets/PolyOne/Water URP/Materials/M_Shader Water.mat";
        const string BricksMaterialPath = "Assets/TileableBricksWall/Materials/Bricks_01_mat.mat";
        static readonly string[] GhibliRocks = { "Rock_01", "Rock_02", "Rock_03", "Rock_04", "Rock_05", "Rock_06", "Rock_07", "Rock_08", "Rock_09" };
        static readonly string[] GhibliShrubs = { "Shrubs_01", "Shrubs_02", "Shrubs_03" };
        static readonly string[] GhibliGrass = { "Gras_01", "Gras_02", "Gras_03" };
        static readonly string[] GhibliFlowers = { "Flower_01_1", "Flower_02_1", "Flower_03_1", "Flower_04_1", "Flower_05_1", "Flower_06_1", "Flower_07_1", "Flower_08_1", "Flower_09_1", "Flower_10", "Flower_11" };
        static readonly string[] GhibliTrees = { "Tree_01", "Tree_02", "Tree_03", "Tree_04", "Tree_05" };
        static readonly HashSet<string> warned = new HashSet<string>();

        static System.Random rng;
        static int baseSeed;
        static readonly List<Vector3> clearZones = new List<Vector3>();
        // Keeps benches open and visible: no trees within 6 m of a bench, and nothing at all within 1.6 m of it.
        static bool NearClearZone(Vector3 p, float radius) { foreach (var z in clearZones) { float dx = p.x - z.x, dz = p.z - z.z; if (dx * dx + dz * dz < radius * radius) return true; } return false; }
        static Transform root, dressing, blockers, structures;
        static Terrain terrain;
        static PlaceSpec spec;
        static List<Transform> treeSources, rockSources, shrubSources, fernSources, flowerSources, benchSources, stepSources;
        static readonly Dictionary<string, Material> materials = new Dictionary<string, Material>();

        sealed class PlaceSpec
        {
            public string id, display, lighting, camera, ambience = "", weather = "Dry";
            public float x0, x1, z0, z1, maxHeight = 30f;
            public Func<float, float> pathX = z => 0f;
            public Func<float, float, float> height = (x, z) => 0f;
            public float pathHalf = 1.6f;
            public Action build;
        }

        [MenuItem("CatMe/Places/Build All Places")]
        public static void BuildAll()
        {
            if (EditorApplication.isPlaying) throw new InvalidOperationException("Stop Play Mode before building places.");
            if (!EditorUtility.DisplayDialog("Build place scenes?", "This creates or replaces the five scenes in Assets/CatMe/Scenes/Places and their terrain assets, and adds them to Build Settings. The generated legacy route is not touched.", "Build", "Cancel")) return;
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            BuildAllCore();
        }

        [MenuItem("CatMe/Places/Rebuild With Last Seed (same layout)")]
        public static void RebuildSameSeed()
        {
            if (EditorApplication.isPlaying) throw new InvalidOperationException("Stop Play Mode before building places.");
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            BuildAllCore(EditorPrefs.GetInt("CatMe.Places.LastSeed", 2909));
        }

        /// <summary>Same as the menu item without prompts; used by the batch-mode capture.</summary>
        public static void BuildAllCore(int? seed = null)
        {
            // Each rebuild scatters trees, rocks, plants and props differently. Set CATME_SEED to pin a layout, or use the
            // "Rebuild With Last Seed" menu item to reproduce the previous one. The seed is logged and stored.
            string pinned = Environment.GetEnvironmentVariable("CATME_SEED");
            baseSeed = seed ?? (int.TryParse(pinned, out int parsed) ? parsed : (int)(DateTime.UtcNow.Ticks & 0x7fffffff));
            EditorPrefs.SetInt("CatMe.Places.LastSeed", baseSeed);
            Debug.Log("[CatMe][Places] Layout seed " + baseSeed + " (use Rebuild With Last Seed to repeat it).");
            Directory.CreateDirectory(SceneFolder); Directory.CreateDirectory(ArtFolder + "/Materials"); Directory.CreateDirectory(ArtFolder + "/Terrain");
            AssetDatabase.Refresh();
            EnsureWalkableLayer();
            ConvertImportedMaterials();
            EnsureDaySky();
            EnsurePostProfile();
            LoadSources();
            var specs = new List<PlaceSpec> { HomeGarden() };
            try
            {
                foreach (var s in specs) BuildPlace(s);
                RemoveRetiredPlaces();
                RegisterBuildScenes(specs.Select(s => SceneFolder + "/Place_" + s.id + SessionDirector.SceneRevision + ".unity"));
                EditorSceneManager.OpenScene(ShellScene);
                Debug.Log("[CatMe][Places] Built " + specs.Count + " place scenes and added them to Build Settings.");
                File.WriteAllText("/tmp/catme-places-build-result.txt", "SUCCESS " + DateTime.Now.ToString("O"));
            }
            finally { AssetDatabase.SaveAssets(); }
        }

        // ---- place definitions (coordinates are scene-local metres; every place is built around the origin) ------------

        static PlaceSpec HomeGarden()
        {
            // One place: a compact walled garden whose gate opens onto the riverside walk. The river exists only beyond the gate,
            // and thick planting outside the walls keeps it out of sight until the player steps through.
            const float G = 11f, L = 95f;
            var p = new PlaceSpec { id = "HomeGarden", display = "Home", lighting = "Sunset", camera = "GardenClose", ambience = "CatMeAudio/day-ambient", x0 = -45, x1 = 40, z0 = -25, z1 = G + L + 16f };
            p.pathX = z => z <= G + 3f ? 0f : 4f * Mathf.Sin((z - (G + 3f)) * .065f);
            p.pathHalf = 1.1f;
            p.height = (x, z) =>
            {
                float rx = p.pathX(z) - 8.5f, d = Mathf.Abs(x - rx);
                float riverMask = Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(G + 5f, G + 16f, z)); // no river until past the gate
                float carve = -1.1f * (1f - Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(2.8f, 5.2f, d))) * riverMask;
                return Hills(x, z, p.pathX(z), z < G + 3f ? 9f : 11f, 2.2f, 8f, 18f) + carve;
            };
            p.build = () =>
            {
                Entry("Start", new Vector3(0, 0, 1f), 0f); Entry("Return", new Vector3(0, 0, 6.5f), 180f); Entry("FromRiverside", new Vector3(0, 0, 8.5f), 180f);
                Recovery("A", new Vector3(0, 0, 4f)); Recovery("B", new Vector3(0, 0, 9f));

                // Stone-brick side walls and a rubble ring of boulders outside them; timber fence flanks the gate.
                Wall("Courtyard wall left", new Vector3(-7f, 0, 3f), new Vector3(.5f, 1.2f, 16f));
                Wall("Courtyard wall right", new Vector3(7f, 0, 3f), new Vector3(.5f, 1.2f, 16f));
                Wall("Courtyard wall back left", new Vector3(-4.6f, 0, -5f), new Vector3(4.8f, 1.2f, .5f));
                Wall("Courtyard wall back right", new Vector3(4.6f, 0, -5f), new Vector3(4.8f, 1.2f, .5f));
                if (AssetDatabase.LoadAssetAtPath<GameObject>(Ghibli + "Fence_01.prefab") != null)
                {
                    PlaceLine(Ghibli + "Fence_01.prefab", new Vector3(-7f, 0, 11f), new Vector3(-3.1f, 0, 11f), 1.25f, Blocker.Box);
                    PlaceLine(Ghibli + "Fence_01.prefab", new Vector3(3.1f, 0, 11f), new Vector3(7f, 0, 11f), 1.25f, Blocker.Box);
                }
                else
                {
                    Wall("Courtyard wall far left", new Vector3(-5f, 0, 11f), new Vector3(4f, 1.2f, .5f));
                    Wall("Courtyard wall far right", new Vector3(5f, 0, 11f), new Vector3(4f, 1.2f, .5f));
                }
                for (float z = -4.5f; z <= 10.5f; z += 1.7f)
                {
                    PlaceProp(Ghibli + Pick(GhibliRocks) + ".prefab", dressing, new Vector3(-7.9f + Next(-.3f, .3f), 0, z + Next(-.4f, .4f)), Next(0, 360), Next(.55f, 1.15f), Blocker.None);
                    PlaceProp(Ghibli + Pick(GhibliRocks) + ".prefab", dressing, new Vector3(7.9f + Next(-.3f, .3f), 0, z + Next(-.4f, .4f)), Next(0, 360), Next(.55f, 1.15f), Blocker.None);
                }
                Gate(new Vector3(0, 0, 11f), 3.4f);
                PlaceProp(Ghibli + "Sign.prefab", structures, new Vector3(4.6f, 0, 14.5f), -25f, 1.7f, Blocker.Capsule);
                HouseWithRoom(new Vector3(0, 0, -10f));
                Pond(new Vector3(-4f, 0, 6.5f), 1.7f);
                Lantern("Gate lantern L", new Vector3(-3.5f, 0, 10.3f)); Lantern("Gate lantern R", new Vector3(3.5f, 0, 10.3f));

                // Inside: beds of shrubs, ferns and flowers against every wall, boulders, stepping stones and two cherry trees.
                // Open lawn with flower beds along the walls and path rather than thick planting.
                for (int i = 0; i < 14; i++)
                {
                    float side = i % 2 == 0 ? -1f : 1f;
                    PlaceFrom(shrubSources, new Vector3(side * Next(5.6f, 6.5f), 0, Next(-3.5f, 10f)), Next(0, 360), Next(.7f, 1.0f), dressing);
                    PlaceFrom(fernSources, new Vector3(side * Next(4.4f, 6.4f), 0, Next(-3.5f, 10f)), Next(0, 360), Next(.8f, 1.2f), dressing);
                }
                for (int i = 0; i < 190; i++) PlaceProp(Ghibli + Pick(GhibliFlowers) + ".prefab", dressing, new Vector3((i % 2 == 0 ? -1f : 1f) * Next(1.9f, 6.5f), 0, Next(-3.5f, 10.2f)), Next(0, 360), -Next(.35f, .7f), Blocker.None);
                for (int i = 0; i < 36; i++) PlaceFrom(flowerSources, new Vector3((i % 2 == 0 ? -1f : 1f) * Next(2.2f, 6.4f), 0, Next(-3.5f, 10f)), Next(0, 360), 1f, dressing);
                for (int i = 0; i < 8; i++) PlaceFrom(stepSources, new Vector3(Mathf.Sin(i) * .3f, 0, -2 + i * 1.5f), Next(-20, 20), 1f, dressing);
                PlaceTree(new Vector3(-5.2f, 0, 0f), 1.15f); PlaceTree(new Vector3(5.4f, 0, 8.2f), 1.05f);
                DressProps(rocks: 0, shrubs: 3, flowers: 70, grass: 24, trees: 0, xRange: 6f, zFrom: -3.5f, zTo: 10f, avoidHalf: 1.7f);   // no loose boulders on the lawn
                EdgeDressing(p, -3f, 10f, 1.4f, 1.25f);
                ClearAround(OnGround(new Vector3(-4f, 0, 6.5f)), 2.4f, null);   // planting scattered after the pond must not land in it

                // Surrounding woodland: a thick ring of cherry, pine and round trees outside the walls and beside the path beyond the gate.
                for (int side = -1; side <= 1; side += 2)
                {
                    for (int i = 0; i < 38; i++)
                    {
                        Vector3 at = new Vector3(side * Next(8.8f, 18f), 0, Next(-9f, 16f));
                        if (rng.Next(3) == 0) PlaceProp(PickAnimeTree(), dressing, at, Next(0, 360), Next(6f, 10f), Blocker.Capsule);
                        else PlaceTree(at, Next(1.2f, 1.8f));
                    }
                    // A solid hedge of shrubs, ferns and grass right outside the walls, so the garden reads as a secret space.
                    for (int i = 0; i < 150; i++) PlaceProp((rng.Next(2) == 0 ? PickBush() : Ghibli + Pick(GhibliGrass) + ".prefab"), dressing, new Vector3(side * Next(8.3f, 14f), 0, Next(-9f, 17f)), Next(0, 360), rng.Next(2) == 0 ? Next(1f, 1.6f) : -Next(.9f, 1.4f), Blocker.None);
                    for (int i = 0; i < 60; i++) PlaceFrom(fernSources, new Vector3(side * Next(8.3f, 15f), 0, Next(-9f, 17f)), Next(0, 360), Next(1.1f, 1.8f), dressing);
                }
                for (int i = 0; i < 22; i++) PlaceTree(new Vector3(Next(-16f, 16f), 0, Next(-22f, -16f)), Next(1.3f, 1.8f)); // behind the house
                // Screen between the garden and the river: a thick hedge and trees on the left, so no water shows from inside.
                Undergrowth(p, -1, 6f, 17f, 120, 3f, 8f);
                for (int i = 0; i < 10; i++) PlaceTree(new Vector3(Next(-12f, -8f), 0, Next(8f, 17f)), Next(1.2f, 1.7f));

                // The riverside walk beyond the gate.
                River(p, -8.5f, 6.5f, -.55f, fromZ: G + 6f, toZ: G + L + 8f);
                for (float z = G + 7f; z < G + L; z += 8.7f)
                {
                    float zEnd = Mathf.Min(z + 8.7f, G + L);
                    if (AssetDatabase.LoadAssetAtPath<GameObject>(Ghibli + "Fence_02.prefab") != null) PlaceLine(Ghibli + "Fence_02.prefab", new Vector3(p.pathX(z) - 3.4f, 0, z), new Vector3(p.pathX(zEnd) - 3.4f, 0, zEnd), 1.0f);
                    else for (float zz = z; zz < zEnd; zz += 3f) Fence(new Vector3(p.pathX(zz) - 3.4f, 0, zz), new Vector3(p.pathX(zz + 3f) - 3.4f, 0, zz + 3f));
                }
                BankBlocker(p, -3.5f, G + 1f, G + L + 5f); // exactly on the fence line: nobody can step past the fence toward the water
                Bench(new Vector3(p.pathX(G + 14f) + 3.2f, 0, G + 14f), 90f);
                Bench(new Vector3(p.pathX(G + L * .22f) + 3.2f, 0, G + L * .22f), 90f);
                Lookout(p, G + L + 4f);
                // Tent in the middle of the walk, on the forest side, with its prompt on the path edge. Rain begins on the way to it.
                float tentZ = G + L * .5f;
                TentWithRoom(new Vector3(p.pathX(tentZ) + 5.2f, 0, tentZ), p.pathX(tentZ) + 2.2f);
                var pr = root.GetComponent<PlaceRoot>(); pr.startWeather = "RainRamp"; pr.rainRampFromZ = tentZ - 34f; pr.rainRampToZ = tentZ - 4f;
                BuildLivingWorld(p, G, L);
                EdgeDressing(p, G + 1f, G + L + 3f, 2.0f, .95f);
                RiverDressing(p, -8.5f, G + 8f, G + L + 6f, 36, 110);
                // Outside density: forest crowding the right of the path, thick undergrowth, and a far bank beyond the river.
                TreeClusters(p, 1, G + 2f, G + L + 8f, 44, 4.5f, 10f);
                Undergrowth(p, 1, G, G + L + 8f, 420, 1.9f, 11f);
                TreeClusters(p, -1, G + 12f, G + L + 10f, 20, 13f, 24f);
                Undergrowth(p, -1, G + 12f, G + L + 8f, 140, 11f, 22f);
                // Trees stand on the SIDES of the route only; the end stays an open clearing, closed by invisible walls.
                for (int side = -1; side <= 1; side += 2)
                    for (int i = 0; i < 10; i++) PlaceTree(new Vector3(p.pathX(G + L) + side * Next(6f, 12f), 0, G + L + Next(-2f, 12f)), Next(1.3f, 1.8f));
                // Invisible boundaries: nobody can leave the route. The river side is already fenced; add the forest side, the gate approach and the far end.
                BankBlocker(p, 7.5f, G - 1f, G + L + 12f);
                for (float x = -7f; x <= 7.5f; x += 3f)
                {
                    var wall = Box("End blocker", blockers, new Vector3(p.pathX(G + L + 12f) + x, GroundAt(p.pathX(G + L + 12f) + x, G + L + 12f) + .7f, G + L + 12f), new Vector3(3.2f, 1.6f, .5f), Stone, true);
                    wall.GetComponent<Renderer>().enabled = false;
                }
                Curiosity("Smooth river stone", new Vector3(p.pathX(G + 20f) - 3f, 0, G + 20f), "river-stone", "A good skipping stone.", PrimitiveType.Sphere, new Vector3(.22f, .1f, .16f), new Color(.55f, .55f, .52f));
                Recovery("C", new Vector3(p.pathX(G + 30f), 0, G + 30f)); Recovery("D", new Vector3(p.pathX(G + 70f), 0, G + 70f));
                Zone("RiversideZone", "Riverside", new Vector3(p.pathX(G + L * .5f), 2, G + L * .5f), new Vector3(30, 8, L - 5f));
                Curiosity("Fallen petal", new Vector3(3.5f, 0, 3.5f), "garden-petal", "Mochi found something.", PrimitiveType.Cube, new Vector3(.18f, .02f, .12f), new Color(.95f, .6f, .7f));
                Zone("GardenCloseZone", "GardenClose", new Vector3(0, 2, 3.5f), new Vector3(14, 8, 16), 1);
                Zone("GateRevealZone", "GateReveal", new Vector3(0, 2, 8.7f), new Vector3(8, 8, 5), 2);
                Zone("BeyondGateZone", "Default", new Vector3(0, 2, 22f), new Vector3(30, 8, 12));
                MountainSilhouettes(G + L + 20f);
                NightSky(G + L + 100f);
            };
            return p;
        }

        // ---- shared terrain functions ------------------------------------------------------------------------------

        static float Hills(float x, float z, float corridorX, float corridorHalf, float amp, float blend, float scale)
        {
            float d = Mathf.Abs(x - corridorX);
            float m = Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(corridorHalf, corridorHalf + blend * 2f, d));
            return (Mathf.PerlinNoise(x / scale + 40f, z / scale + 40f) - .35f) * amp * 2f * m;
        }

        // ---- build pipeline -----------------------------------------------------------------------------------------

        static void BuildPlace(PlaceSpec s)
        {
            clearZones.Clear();
            spec = s; rng = new System.Random(unchecked(baseSeed * 31 + StableHash(s.id)));
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            root = new GameObject("PlaceRoot_" + s.id).transform;
            dressing = Child("Dressing"); blockers = Child("Blockers"); structures = Child("Structures");
            var pr = root.gameObject.AddComponent<PlaceRoot>();
            pr.ambienceVolume = .16f; pr.placeId = s.id; pr.displayName = s.display; pr.lightingPreset = s.lighting; pr.cameraProfile = s.camera;
            pr.ambienceResource = s.ambience; pr.startWeather = s.weather;
            if (s.weather == "RainRamp") { pr.rainRampFromZ = 16f; pr.rainRampToZ = 40f; }
            // Rain streaks with ground splashes and rings (Rain Particles pack). Kept inactive; PlaceWeather turns it on when it rains.
            var rainFx = Spawn(RainPrefab, root, "Rain effect");
            if (rainFx != null) { rainFx.SetActive(false); pr.rainEffect = rainFx.GetComponent<ParticleSystem>(); }
            MakeTerrain(s);
            PathRibbon(s);
            AddPostVolume();
            s.build();
            string path = SceneFolder + "/Place_" + s.id + SessionDirector.SceneRevision + ".unity";
            EditorSceneManager.SaveScene(scene, path);
            Debug.Log("[CatMe][Places] Saved " + path);
        }

        static int StableHash(string text) { int h = 17; foreach (char c in text) h = unchecked(h * 31 + c); return h; }

        // Procedural blue sky for the daytime places. The sun disc follows RenderSettings.sun, set by PlaceLighting.
        static void EnsureDaySky()
        {
            Directory.CreateDirectory("Assets/Resources/Skyboxes");
            string path = "Assets/Resources/Skyboxes/Sky_Day.mat";
            var shader = Shader.Find("Skybox/Procedural");
            if (shader == null) return;
            var m = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (m == null) { m = new Material(shader); AssetDatabase.CreateAsset(m, path); }
            m.shader = shader;
            m.SetFloat("_SunSize", .045f); m.SetFloat("_SunSizeConvergence", 5f); m.SetFloat("_AtmosphereThickness", .85f);
            m.SetFloat("_Exposure", 1.25f); m.SetColor("_SkyTint", new Color(.62f, .77f, .94f)); m.SetColor("_GroundColor", new Color(.72f, .78f, .62f));
            EditorUtility.SetDirty(m);
        }

        static VolumeProfile postProfile;

        // Warm, slightly saturated grade with ACES tone mapping, soft bloom and a light vignette.
        static void EnsurePostProfile()
        {
            string path = ArtFolder + "/PlacePost.asset";
            AssetDatabase.DeleteAsset(path);
            var profile = ScriptableObject.CreateInstance<VolumeProfile>();
            var tone = profile.Add<Tonemapping>(true); tone.mode.Override(TonemappingMode.ACES);
            var bloom = profile.Add<Bloom>(true); bloom.threshold.Override(.95f); bloom.intensity.Override(.38f); bloom.scatter.Override(.68f); bloom.tint.Override(new Color(1f, .93f, .82f));
            var grade = profile.Add<ColorAdjustments>(true);
            grade.postExposure.Override(.15f); grade.contrast.Override(14f); grade.saturation.Override(16f); grade.colorFilter.Override(new Color(1f, .955f, .89f));
            var vignette = profile.Add<Vignette>(true); vignette.intensity.Override(.2f); vignette.smoothness.Override(.45f);
            var smh = profile.Add<ShadowsMidtonesHighlights>(true);
            smh.shadows.Override(new Vector4(.92f, .96f, 1.06f, 0f)); smh.highlights.Override(new Vector4(1.06f, 1.02f, .94f, 0f));
            AssetDatabase.CreateAsset(profile, path);
            foreach (var component in profile.components) AssetDatabase.AddObjectToAsset(component, profile);
            AssetDatabase.SaveAssets();
            postProfile = AssetDatabase.LoadAssetAtPath<VolumeProfile>(path);
        }

        static void AddPostVolume()
        {
            if (postProfile == null) return;
            var go = new GameObject("Post volume"); go.transform.SetParent(root, false);
            var volume = go.AddComponent<Volume>(); volume.isGlobal = true; volume.priority = 5; volume.sharedProfile = postProfile;
        }

        static Transform Child(string name) { var t = new GameObject(name).transform; t.SetParent(root, false); return t; }

        static void MakeTerrain(PlaceSpec s)
        {
            float w = s.x1 - s.x0, l = s.z1 - s.z0;
            var td = new TerrainData { heightmapResolution = Res, size = new Vector3(w, s.maxHeight, l) };
            var h = new float[Res, Res];
            float minY = 0f;
            for (int iz = 0; iz < Res; iz++) for (int ix = 0; ix < Res; ix++)
            {
                float x = s.x0 + ix / (float)(Res - 1) * w, z = s.z0 + iz / (float)(Res - 1) * l;
                float y = s.height(x, z); minY = Mathf.Min(minY, y); h[iz, ix] = y;
            }
            // Terrain heights cannot be negative; lift everything so the lowest point is 0 and offset the object down.
            float lift = Mathf.Max(0f, -minY) + .01f;
            for (int iz = 0; iz < Res; iz++) for (int ix = 0; ix < Res; ix++) h[iz, ix] = Mathf.Clamp01((h[iz, ix] + lift) / s.maxHeight);
            td.SetHeights(0, 0, h);
            var layers = MakeLayers();
            td.terrainLayers = layers;
            td.alphamapResolution = 256;
            var alpha = new float[256, 256, 3];
            for (int iz = 0; iz < 256; iz++) for (int ix = 0; ix < 256; ix++)
            {
                float x = s.x0 + ix / 255f * w, z = s.z0 + iz / 255f * l;
                float dPath = Mathf.Abs(x - s.pathX(z));
                float path = 1f - Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(s.pathHalf, s.pathHalf + 1.2f, dPath));
                if (spec.id == "HomeGarden" && z < 17f && Mathf.Abs(x) < 10.5f) path = Mathf.Max(path, Mathf.Abs(x) < 3f ? 1f : 0f);
                float steep = Mathf.InverseLerp(.35f, .6f, td.GetSteepness(ix / 255f, iz / 255f) / 45f);
                float rock = Mathf.Clamp01(steep) * (1f - path);
                float grass = Mathf.Clamp01(1f - path - rock);
                alpha[iz, ix, 0] = grass; alpha[iz, ix, 1] = path; alpha[iz, ix, 2] = rock;
            }
            td.SetAlphamaps(0, 0, alpha);
            string assetPath = ArtFolder + "/Terrain/" + s.id + "_Terrain.asset";
            AssetDatabase.DeleteAsset(assetPath);
            AssetDatabase.CreateAsset(td, assetPath);
            var go = Terrain.CreateTerrainGameObject(td);
            go.name = "Terrain"; go.transform.SetParent(root, false);
            go.transform.position = new Vector3(s.x0, -lift, s.z0);
            go.layer = LayerMask.NameToLayer(GroundProbe.WalkableLayerName);
            terrain = go.GetComponent<Terrain>();
            var terrainShader = Shader.Find("Universal Render Pipeline/Terrain/Lit");
            if (terrainShader != null)
            {
                string matPath = ArtFolder + "/Materials/Terrain matte.mat";
                var tm = AssetDatabase.LoadAssetAtPath<Material>(matPath);
                if (tm == null) { tm = new Material(terrainShader); AssetDatabase.CreateAsset(tm, matPath); }
                tm.EnableKeyword("_ENVIRONMENTREFLECTIONS_OFF"); tm.EnableKeyword("_SPECULARHIGHLIGHTS_OFF");
                if (tm.HasProperty("_EnvironmentReflections")) tm.SetFloat("_EnvironmentReflections", 0f);
                if (tm.HasProperty("_SpecularHighlights")) tm.SetFloat("_SpecularHighlights", 0f);
                terrain.materialTemplate = tm;
            }
            var probeAlpha = td.GetAlphamaps(128, 128, 1, 1);
            Debug.Log("[CatMe][Places] " + s.id + " terrain layers=" + td.terrainLayers.Length + " alpha centre g/p/r=" + probeAlpha[0, 0, 0].ToString("F2") + "/" + probeAlpha[0, 0, 1].ToString("F2") + "/" + probeAlpha[0, 0, 2].ToString("F2") + " diffuse0=" + (td.terrainLayers[0].diffuseTexture != null) + " diffuse1=" + (td.terrainLayers[1].diffuseTexture != null));
            terrain.drawInstanced = true; terrain.heightmapPixelError = 8; terrain.basemapDistance = 40; terrain.detailObjectDistance = 35;
        }

        // A gravel strip draped on the terrain along the walking route. Visual only: the terrain collider provides the ground.
        static void PathRibbon(PlaceSpec s)
        {
            float zFrom = s.id == "HomeGarden" ? -4f : -3f, zTo = s.z1 - 12f;
            float hw = s.id == "HomeGarden" ? 1.4f : 0.95f;
            var verts = new List<Vector3>(); var uvs = new List<Vector2>(); var tris = new List<int>();
            int steps = Mathf.CeilToInt((zTo - zFrom) / .75f);
            for (int i = 0; i <= steps; i++)
            {
                float z = zFrom + i * (zTo - zFrom) / steps, cx = s.pathX(z);
                // Slight width wobble so the edge reads as hand-laid gravel rather than a ruler line.
                float w = hw * (1f + .08f * Mathf.Sin(z * .9f));
                verts.Add(new Vector3(cx - w, GroundAt(cx - w, z) + .035f, z)); verts.Add(new Vector3(cx + w, GroundAt(cx + w, z) + .035f, z));
                uvs.Add(new Vector2(0, z / 2.5f)); uvs.Add(new Vector2(w * 2f / 2.5f, z / 2.5f));
                if (i > 0) { int a = (i - 1) * 2; tris.AddRange(new[] { a, a + 2, a + 1, a + 1, a + 2, a + 3 }); }
            }
            var mesh = new Mesh { name = "Gravel path" };
            mesh.SetVertices(verts); mesh.SetUVs(0, uvs); mesh.SetTriangles(tris, 0); mesh.RecalculateNormals(); mesh.RecalculateBounds();
            string meshPath = ArtFolder + "/Terrain/" + s.id + "_Path.asset";
            AssetDatabase.DeleteAsset(meshPath); AssetDatabase.CreateAsset(mesh, meshPath);
            string matPath = ArtFolder + "/Materials/Gravel path.mat";
            var mat = AssetDatabase.LoadAssetAtPath<Material>(matPath);
            if (mat == null) { mat = new Material(Shader.Find("Universal Render Pipeline/Lit")); AssetDatabase.CreateAsset(mat, matPath); }
            mat.SetTexture("_BaseMap", AssetDatabase.LoadAssetAtPath<Texture2D>(ArtFolder + "/Terrain/Path.png"));
            mat.SetColor("_BaseColor", new Color(1.3f, 1.14f, .96f)); mat.SetFloat("_Smoothness", 0f); mat.SetFloat("_Metallic", 0f);
            mat.EnableKeyword("_SPECULARHIGHLIGHTS_OFF"); mat.EnableKeyword("_ENVIRONMENTREFLECTIONS_OFF");
            EditorUtility.SetDirty(mat);
            var go = new GameObject("Gravel path", typeof(MeshFilter), typeof(MeshRenderer)); go.transform.SetParent(root, false);
            go.GetComponent<MeshFilter>().sharedMesh = mesh; go.GetComponent<MeshRenderer>().sharedMaterial = mat;
        }

        static TerrainLayer[] MakeLayers()
        {
            return new[] { Layer("Grass", new Color(.38f, .50f, .26f), .08f), Layer("Path", new Color(.62f, .50f, .36f), .06f), Layer("Rock", new Color(.45f, .43f, .40f), .12f) };
        }

        static TerrainLayer Layer(string name, Color color, float noise)
        {
            string texPath = ArtFolder + "/Terrain/" + name + ".png", layerPath = ArtFolder + "/Terrain/" + name + ".terrainlayer";
            if (!File.Exists(texPath))
            {
                var t = new Texture2D(256, 256, TextureFormat.RGB24, true);
                for (int y = 0; y < 256; y++) for (int x = 0; x < 256; x++)
                {
                    float n = Mathf.PerlinNoise(x * .06f, y * .06f) * .6f + Mathf.PerlinNoise(x * .3f, y * .3f) * .4f;
                    t.SetPixel(x, y, color * (1f + (n - .5f) * noise * 4f));
                }
                File.WriteAllBytes(texPath, t.EncodeToPNG()); Object.DestroyImmediate(t);
                AssetDatabase.ImportAsset(texPath);
            }
            var existing = AssetDatabase.LoadAssetAtPath<TerrainLayer>(layerPath);
            if (existing != null) return existing;
            var layer = new TerrainLayer { diffuseTexture = AssetDatabase.LoadAssetAtPath<Texture2D>(texPath), tileSize = new Vector2(5, 5), smoothness = 0f };
            AssetDatabase.CreateAsset(layer, layerPath);
            return layer;
        }

        // ---- primitives ---------------------------------------------------------------------------------------------

        static Material Mat(string name, Color color, float smoothness = 0f, Color? emission = null)
        {
            if (materials.TryGetValue(name, out var cached) && cached != null) return cached;
            string path = ArtFolder + "/Materials/" + name + ".mat";
            var m = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (m == null) { m = new Material(Shader.Find("Universal Render Pipeline/Lit")); AssetDatabase.CreateAsset(m, path); }
            m.SetColor("_BaseColor", color); m.SetFloat("_Smoothness", smoothness);
            if (emission.HasValue) { m.EnableKeyword("_EMISSION"); m.SetColor("_EmissionColor", emission.Value); m.globalIlluminationFlags = MaterialGlobalIlluminationFlags.None; }
            EditorUtility.SetDirty(m);
            materials[name] = m; return m;
        }
        static Material Stone => Mat("Stone wall", new Color(.62f, .58f, .50f));
        static Material Timber => Mat("Timber", new Color(.36f, .25f, .17f));
        static Material Plaster => Mat("Plaster", new Color(.86f, .80f, .70f));
        static Material Roof => Mat("Roof", new Color(.30f, .25f, .21f));
        static Material Canvas => Mat("Canvas", new Color(.78f, .68f, .50f));
        static Material Water
        {
            get
            {
                // Imported water Shader Graph rendered as grey noise; use the in-house stylised river shader (flat tint if it is missing).
                var shader = Shader.Find("CatMe/River");
                if (shader == null) return Mat("River water", new Color(.17f, .42f, .50f), .85f);
                string path = ArtFolder + "/Materials/River CatMe.mat";
                var m = AssetDatabase.LoadAssetAtPath<Material>(path);
                if (m == null) { m = new Material(shader); AssetDatabase.CreateAsset(m, path); }
                m.shader = shader; EditorUtility.SetDirty(m);
                return m;
            }
        }

        static GameObject Box(string name, Transform parent, Vector3 centre, Vector3 size, Material mat, bool collider, float yaw = 0f)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            go.name = name; go.transform.SetParent(parent, false);
            go.transform.position = centre; go.transform.rotation = Quaternion.Euler(0, yaw, 0); go.transform.localScale = size;
            go.GetComponent<Renderer>().sharedMaterial = mat;
            if (!collider) Object.DestroyImmediate(go.GetComponent<Collider>());
            return go;
        }

        static float GroundAt(float x, float z) => terrain.SampleHeight(new Vector3(x, 0, z)) + terrain.transform.position.y;
        static Vector3 OnGround(Vector3 p) { p.y = GroundAt(p.x, p.z); return p; }

        static void Entry(string id, Vector3 p, float yaw) { var t = new GameObject("Entry_" + id).transform; t.SetParent(root, false); t.position = OnGround(p); t.rotation = Quaternion.Euler(0, yaw, 0); }
        static void Recovery(string id, Vector3 p) { var t = new GameObject("Recovery_" + id).transform; t.SetParent(root, false); t.position = p.y > 0.01f ? p : OnGround(p); }

        static void Exit(string name, string target, string entry, Vector3 p, Vector3 size, float yaw)
        {
            var go = new GameObject(name); go.transform.SetParent(root, false);
            go.transform.position = new Vector3(p.x, GroundAt(p.x, p.z) + p.y - (p.y > 4f ? p.y : 0f), p.z);
            if (p.y > 4f) go.transform.position = p; // already absolute (deck)
            go.transform.rotation = Quaternion.Euler(0, yaw, 0);
            var c = go.AddComponent<BoxCollider>(); c.isTrigger = true; c.size = size;
            var e = go.AddComponent<PlaceExit>(); e.targetPlace = target; e.targetEntry = entry; e.label = name;
        }

        static void Zone(string name, string profile, Vector3 centre, Vector3 size, int priority = 0)
        {
            var go = new GameObject(name); go.transform.SetParent(root, false);
            go.transform.position = new Vector3(centre.x, GroundAt(centre.x, centre.z) + centre.y, centre.z);
            var c = go.AddComponent<BoxCollider>(); c.isTrigger = true; c.size = size;
            var z = go.AddComponent<CameraZone>(); z.profileName = profile; z.priority = priority;
        }

        // Brick texture from the imported pack, tiled to the wall's real size so bricks keep a constant scale.
        static Material BrickWall(float length, float height)
        {
            // The imported brick material rendered as flat cream; use weathered grey-brown stone with a mossy tint instead.
            if (true) return Mat("Mossy stone wall", new Color(.42f, .43f, .36f));
            var source = AssetDatabase.LoadAssetAtPath<Material>(BricksMaterialPath);
            if (source == null) return Mat("Stone wall", new Color(.62f, .58f, .50f));
            string key = "Bricks " + Mathf.RoundToInt(length) + "x" + Mathf.RoundToInt(height * 10);
            string path = ArtFolder + "/Materials/" + key + ".mat";
            var m = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (m == null) { m = new Material(source); AssetDatabase.CreateAsset(m, path); }
            m.SetTextureScale("_BaseMap", new Vector2(length / 1.5f, height / 1.5f));
            m.SetTextureScale("_MainTex", new Vector2(length / 1.5f, height / 1.5f));
            EditorUtility.SetDirty(m); return m;
        }

        static void Wall(string name, Vector3 baseCentre, Vector3 size)
        {
            Vector3 p = OnGround(baseCentre); p.y += size.y * .5f - .1f;
            Box(name, structures, p, size + Vector3.up * .1f, BrickWall(Mathf.Max(size.x, size.z), size.y), true);
            Box(name + " cap", structures, p + Vector3.up * (size.y * .5f), new Vector3(size.x + (size.x > size.z ? 0f : .15f), .1f, size.z + (size.x > size.z ? .15f : 0f)), Mat("Stone cap", new Color(.52f, .52f, .45f)), false);
        }

        static void Gate(Vector3 centre, float opening)
        {
            var torii = Spawn(ToriiFolder + "Torii_Kuroki_Med1_LOD.prefab", structures, "Torii gate");
            if (torii != null)
            {
                // Scale to the opening, orient so the gap runs along X, and stand it on the ground.
                var b = Bounds(torii);
                if (b.size.z > b.size.x) torii.transform.rotation = Quaternion.Euler(0, 90f, 0);
                b = Bounds(torii);
                float targetWidth = opening + 1.8f;
                torii.transform.localScale *= targetWidth / Mathf.Max(b.size.x, .01f);
                PlaceOnGround(torii, centre, torii.transform.eulerAngles.y);
                b = Bounds(torii);
                // Pillars get solid blockers; the opening between them stays clear.
                float pillarX = b.size.x * .5f - .35f;
                for (int side = -1; side <= 1; side += 2)
                {
                    var blocker = new GameObject("Torii pillar blocker"); blocker.transform.SetParent(blockers, false);
                    blocker.transform.position = new Vector3(centre.x + side * pillarX, GroundAt(centre.x, centre.z) + 1.2f, centre.z);
                    blocker.AddComponent<BoxCollider>().size = new Vector3(.6f, 2.4f, .6f);
                }
                return;
            }
            float half = opening * .5f;
            for (int s = -1; s <= 1; s += 2)
            {
                Box("Gate pillar", structures, OnGround(centre + new Vector3(s * (half + .3f), 0, 0)) + Vector3.up * .95f, new Vector3(.6f, 1.9f, .6f), Stone, true);
                // Leaves stand open against the wall line so the opening stays clear.
                Box("Gate leaf (open)", structures, OnGround(centre + new Vector3(s * (half + 1f), 0, -.5f)) + Vector3.up * .7f, new Vector3(.08f, 1.3f, 1.4f), Timber, false, s * 80f);
            }
            Box("Gate lintel hint", structures, OnGround(centre) + Vector3.up * 2.05f, new Vector3(opening + 1.5f, .18f, .3f), Timber, false);
        }

        static void Fence(Vector3 a, Vector3 b)
        {
            Vector3 pa = OnGround(a), pb = OnGround(b);
            Box("Fence post", structures, pa + Vector3.up * .55f, new Vector3(.12f, 1.1f, .12f), Timber, false);
            Vector3 mid = (pa + pb) * .5f + Vector3.up * .8f; Vector3 dir = pb - pa;
            float yaw = Mathf.Atan2(dir.x, dir.z) * Mathf.Rad2Deg;
            Box("Fence rail", structures, mid, new Vector3(.07f, .07f, dir.magnitude), Timber, false, yaw);
            Box("Fence rail low", structures, mid - Vector3.up * .3f, new Vector3(.07f, .07f, dir.magnitude), Timber, false, yaw);
        }
        static void Railing(Vector3 a, Vector3 b) { Fence(new Vector3(a.x, a.y, a.z), new Vector3(b.x, b.y, b.z)); }

        static void BankBlocker(PlaceSpec s, float offsetX, float z0, float z1)
        {
            for (float z = z0; z < z1; z += 3f)
            {
                Vector3 a = new Vector3(s.pathX(z) + offsetX, 0, z), b = new Vector3(s.pathX(z + 3f) + offsetX, 0, z + 3f);
                Vector3 mid = (a + b) * .5f; mid.y = GroundAt(mid.x, mid.z) + .6f;
                float yaw = Mathf.Atan2(b.x - a.x, b.z - a.z) * Mathf.Rad2Deg;
                Box("Bank blocker", blockers, mid, new Vector3(.8f, 2.2f, (b - a).magnitude + .6f), Stone, true, yaw).GetComponent<Renderer>().enabled = false;
            }
        }

        static void River(PlaceSpec s, float offsetX, float width, float surfaceY, float? fixedX = null, float fromZ = -10f, float toZ = 90f)
        {
            const float step = 1.5f;
            int count = Mathf.CeilToInt((toZ - fromZ) / step) + 1;
            var centres = new Vector3[count];
            for (int i = 0; i < count; i++)
            {
                float z = Mathf.Min(toZ, fromZ + i * step);
                centres[i] = new Vector3(fixedX ?? s.pathX(z) + offsetX, surfaceY, z);
            }
            var vertices = new Vector3[count * 2]; var uvs = new Vector2[count * 2]; var triangles = new int[(count - 1) * 6];
            float distance = 0f;
            for (int i = 0; i < count; i++)
            {
                Vector3 tangent = centres[Mathf.Min(i + 1, count - 1)] - centres[Mathf.Max(0, i - 1)]; tangent.y = 0f; tangent.Normalize();
                Vector3 across = new Vector3(tangent.z, 0f, -tangent.x) * (width * .5f);
                if (i > 0) distance += Vector3.Distance(centres[i - 1], centres[i]);
                vertices[i * 2] = centres[i] - across; vertices[i * 2 + 1] = centres[i] + across;
                uvs[i * 2] = new Vector2(0f, distance / 4f); uvs[i * 2 + 1] = new Vector2(1f, distance / 4f);
                if (i == count - 1) continue;
                int v = i * 2, t = i * 6;
                triangles[t] = v; triangles[t + 1] = v + 2; triangles[t + 2] = v + 1;
                triangles[t + 3] = v + 1; triangles[t + 4] = v + 2; triangles[t + 5] = v + 3;
            }
            var mesh = new Mesh { name = s.id + " river spline" };
            mesh.vertices = vertices; mesh.uv = uvs; mesh.triangles = triangles; mesh.RecalculateNormals(); mesh.RecalculateBounds();
            string meshPath = ArtFolder + "/Terrain/" + s.id + "_River.asset";
            AssetDatabase.DeleteAsset(meshPath); AssetDatabase.CreateAsset(mesh, meshPath);
            var river = new GameObject("River spline", typeof(MeshFilter), typeof(MeshRenderer)); river.transform.SetParent(structures, false);
            river.GetComponent<MeshFilter>().sharedMesh = AssetDatabase.LoadAssetAtPath<Mesh>(meshPath);
            river.GetComponent<MeshRenderer>().sharedMaterial = Water;

            float middleZ = (fromZ + toZ) * .5f;
            var sound = new GameObject("River bank sound"); sound.transform.SetParent(structures, false);
            sound.transform.position = new Vector3(s.pathX(middleZ) - 4.2f, GroundAt(s.pathX(middleZ) - 4.2f, middleZ) + .8f, middleZ);
            var audio = sound.AddComponent<AudioSource>(); audio.clip = Resources.Load<AudioClip>("CatMeAudio/river-loop"); audio.loop = true; audio.playOnAwake = true;
            audio.spatialBlend = .7f; audio.volume = .22f; audio.rolloffMode = AudioRolloffMode.Linear; audio.minDistance = 5f; audio.maxDistance = 75f;
        }

        // Imported country house, scaled to a target width and grounded, with a solid footprint and warm night lights.
        static bool CountryHouse(string prefabName, Vector3 centre, float width, float yaw, string nightPrefix = "Night_House")
        {
            var go = Spawn("Assets/ALP_Assets/country house01/Prefabs/" + prefabName, structures, "Country house");
            if (go == null) return false;
            go.transform.rotation = Quaternion.Euler(0, yaw, 0);
            var b = Bounds(go);
            float extent = Mathf.Max(b.size.x, b.size.z);
            if (extent > .01f) go.transform.localScale *= width / extent;
            PlaceOnGround(go, centre, yaw);
            AddBlocker(go, Blocker.Box);
            b = Bounds(go);
            // Warm window glow for the blue-hour homecoming; the Night_ prefix lets SessionDirector switch it on.
            foreach (float side in new[] { -1f, 1f })
            {
                var light = new GameObject(nightPrefix + " window light"); light.transform.SetParent(structures, false);
                light.transform.position = new Vector3(b.center.x + side * b.size.x * .22f, b.min.y + b.size.y * .4f, b.max.z + 1.2f);
                var l = light.AddComponent<Light>(); l.type = LightType.Point; l.range = 8f; l.intensity = 1.6f; l.color = new Color(1f, .72f, .42f); l.shadows = LightShadows.None;
            }
            return true;
        }

        // "Get in" prompt at the front door, plus a simple furnished room far outside the playable area for the first-person visit:
        // a ball to throw, a food bowl and a bed for the cat.
        static void HouseWithRoom(Vector3 houseCentre)
        {
            House(houseCentre);
            var o = new GameObject("Interact_House"); o.transform.SetParent(structures, false);
            o.transform.position = OnGround(new Vector3(0, 0, -3.2f)); o.transform.rotation = Quaternion.identity;
            var ip = o.AddComponent<InteractionPoint>(); ip.kind = InteractionKind.EnterHouse; ip.label = "Get in the house"; ip.radius = 2.8f;
            // The room is its own prefab (Resources/Rooms/HouseRoom_r2), loaded by the game when you get in, so it never depends on the big place scene.
            var roomRoot = new GameObject("HouseRoom").transform;
            var savedStructures = structures; var savedBlockers = blockers; structures = roomRoot; blockers = roomRoot;
            Vector3 c = new Vector3(220f, 0f, 0f);
            if ((currentRoomRoot = roomRoot) != null && BuildHomeKitRoom(roomRoot, c, savedStructures, savedBlockers)) { structures = savedStructures; blockers = savedBlockers; return; }
            var wall = Mat("House room wall", new Color(.78f, .70f, .58f));
            var wood = Mat("House room floor", new Color(.42f, .30f, .20f));
            var floor = Box("House room floor", structures, c + Vector3.up * -.05f, new Vector3(6.4f, .1f, 5.4f), wood, true);
            floor.layer = LayerMask.NameToLayer(GroundProbe.WalkableLayerName);
            Box("House wall N", structures, c + new Vector3(0, 1.3f, 2.7f), new Vector3(6.4f, 2.6f, .12f), wall, true);
            Box("House wall S", structures, c + new Vector3(0, 1.3f, -2.7f), new Vector3(6.4f, 2.6f, .12f), wall, true);
            Box("House wall E", structures, c + new Vector3(3.2f, 1.3f, 0), new Vector3(.12f, 2.6f, 5.4f), wall, true);
            Box("House wall W", structures, c + new Vector3(-3.2f, 1.3f, 0), new Vector3(.12f, 2.6f, 5.4f), wall, true);
            Box("House roof", structures, c + new Vector3(0, 2.65f, 0), new Vector3(6.4f, .12f, 5.4f), Mat("House room ceiling", new Color(.6f, .52f, .42f)), true);
            Box("House rug", structures, c + new Vector3(0, .01f, 0), new Vector3(2.6f, .02f, 1.8f), Mat("House rug", new Color(.55f, .25f, .22f)), false);
            Box("Window glow", structures, c + new Vector3(0, 1.6f, 2.62f), new Vector3(1.4f, 1f, .05f), Mat("Room window", new Color(.9f, .75f, .45f), 0f, new Color(1.4f, 1f, .5f)), false);
            // Furniture from the Low Poly Living Room pack, scaled to real sizes; solid blockers so the owner and cat walk around them.
            const string LR = "Assets/LowPolyLivingRoomPack/Prefabs/";
            RoomProp(LR + "Sofa_2Seat.prefab", c + new Vector3(-1.6f, 0, 2.15f), 180f, 0.9f, true);
            RoomProp(LR + "Coffee_table_1.prefab", c + new Vector3(-1.6f, 0, 0.95f), 0f, 0.45f, true);
            RoomProp(LR + "Armchair_Classic.prefab", c + new Vector3(2.3f, 0, -0.9f), 235f, 0.95f, true);
            RoomProp(LR + "Bookshelf_Tall.prefab", c + new Vector3(3.05f, 0, 0.1f), 270f, 1.9f, true);
            RoomProp(LR + "PottedPlant_Tall_1.prefab", c + new Vector3(-2.8f, 0, -2.2f), 0f, 1.15f, false);
            RoomProp(LR + "PottedPlant_Tall_1.prefab", c + new Vector3(2.75f, 0, 2.2f), 90f, 1.1f, false);
            RoomProp(LR + "Lamp_Tall.prefab", c + new Vector3(-2.8f, 0, 2.2f), 0f, 1.5f, false);
            RoomProp(LR + "Night_stand_1.prefab", c + new Vector3(-0.2f, 0, 2.3f), 180f, 0.55f, true);
            RoomProp(LR + "Carpet_1.prefab", c + new Vector3(-1.6f, 0.01f, 1.0f), 0f, -2.4f, false);
            RoomProp(LR + "Painting_Modern_1.prefab", c + new Vector3(-1.6f, 1.55f, 2.6f), 180f, 0.8f, false, true);
            // Cat bed (low box + cushion) and food bowl, both with named anchors the visit logic looks up.
            Box("Cat bed frame", structures, c + new Vector3(2.3f, .12f, 1.5f), new Vector3(1.3f, .24f, .9f), Mat("Cat bed frame", new Color(.4f, .27f, .17f)), false);
            Box("Cat bed cushion", structures, c + new Vector3(2.3f, .27f, 1.5f), new Vector3(1.1f, .12f, .72f), Mat("Cat bed cushion", new Color(.82f, .62f, .5f)), false);
            var bed = new GameObject("Cat_Bed"); bed.transform.SetParent(structures, false); bed.transform.position = c + new Vector3(2.3f, 0f, 1.5f); bed.transform.rotation = Quaternion.Euler(0, 200, 0);
            var bowlMesh = GameObject.CreatePrimitive(PrimitiveType.Cylinder); bowlMesh.name = "Food bowl"; bowlMesh.transform.SetParent(structures, false);
            bowlMesh.transform.position = c + new Vector3(0.6f, .05f, 2.2f); bowlMesh.transform.localScale = new Vector3(.34f, .05f, .34f);
            bowlMesh.GetComponent<Renderer>().sharedMaterial = Mat("Food bowl", new Color(.75f, .35f, .25f)); Object.DestroyImmediate(bowlMesh.GetComponent<Collider>());
            var bowl = new GameObject("Cat_Bowl"); bowl.transform.SetParent(structures, false); bowl.transform.position = c + new Vector3(0.6f, 0f, 2.2f); bowl.transform.rotation = Quaternion.Euler(0, 180, 0);
            var spot = new GameObject("House_Spot"); spot.transform.SetParent(structures, false); spot.transform.position = c + new Vector3(0, 0, -1.7f);
            var lamp = new GameObject("House room light"); lamp.transform.SetParent(structures, false); lamp.transform.position = c + new Vector3(0, 2.3f, 0);
            var l = lamp.AddComponent<Light>(); l.type = LightType.Point; l.range = 10f; l.intensity = 1.5f; l.color = new Color(1f, .78f, .5f); l.shadows = LightShadows.None;
            structures = savedStructures; blockers = savedBlockers;
            HouseRoomPersonalization.Add(roomRoot, c);
            Directory.CreateDirectory("Assets/CatMe/Resources/Rooms");
            string roomPath = "Assets/CatMe/Resources/Rooms/HouseRoom_r2.prefab";
            AssetDatabase.DeleteAsset(roomPath);
            PrefabUtility.SaveAsPrefabAsset(roomRoot.gameObject, roomPath);
            Object.DestroyImmediate(roomRoot.gameObject);
            Debug.Log("[CatMe][Places] Saved house room prefab " + roomPath);
        }

        // Places an imported prop in the house room (off the terrain) at a measured height (negative = widest extent), with an optional box blocker.
        static void RoomProp(string path, Vector3 position, float yaw, float size, bool solid, bool wall = false)
        {
            var go = Spawn(path, structures, null);
            if (go == null) return;
            go.transform.rotation = Quaternion.Euler(0, yaw, 0);
            var b = Bounds(go);
            if (size > 0f && b.size.y > .001f) go.transform.localScale *= size / b.size.y;
            else if (size < 0f) { float e = Mathf.Max(b.size.x, b.size.z); if (e > .001f) go.transform.localScale *= -size / e; }
            b = Bounds(go);
            // Stand it on the room floor (y of `position`), or hang it at that height when it is a wall piece.
            float baseY = wall ? position.y - b.size.y * .5f : position.y;
            go.transform.position += new Vector3(position.x - b.center.x, baseY - b.min.y, position.z - b.center.z);
            if (solid) { var holder = new GameObject(go.name + " blocker"); holder.transform.SetParent(blockers, false); var bb = Bounds(go); holder.transform.position = bb.center; holder.AddComponent<BoxCollider>().size = new Vector3(Mathf.Max(bb.size.x * .85f, .2f), bb.size.y, Mathf.Max(bb.size.z * .85f, .2f)); }
        }

        /// <summary>Home Kit textures as shown in its Textured sample scene: wood floor, cream walls and ceiling, raw wood for structure, glowing ceiling lights.</summary>
        public static void ApplyHomeKitMaterials(GameObject root)
        {
            Material M(string n) => AssetDatabase.LoadAssetAtPath<Material>("Assets/VRChat/HomeKit/Materials/" + n + ".mat") ?? AssetDatabase.LoadAssetAtPath<Material>("Assets/VRChat/HomeKit/Models/Materials/" + n + ".mat");
            var floor = M("HomeKit_WoodFloor_Material"); var wall = M("HomeKit_SolidColor_Material"); var wood = M("HomeKit_WoodRaw_Material"); var glow = M("emissive_CelingLight"); var metal = M("HomeKit_Metal_Material");
            foreach (var r in root.GetComponentsInChildren<Renderer>(true))
            {
                string n = r.name; Material m;
                if (n.StartsWith("floor")) m = floor;
                else if (n.StartsWith("interiorWalls") || n.StartsWith("exteriorWalls") || n.StartsWith("ceiling")) m = wall;
                else if (n.Contains("Light")) m = glow;
                else if (n.StartsWith("balcony") || n.Contains("support")) m = metal ?? wood;
                else m = wood;
                if (m != null) r.sharedMaterials = Enumerable.Repeat(m, Mathf.Max(1, r.sharedMaterials.Length)).ToArray();
            }
        }

        // The house room, built from the owner's Home Kit (VRChat template) shell with its own textured materials, furnished with the
        // realistic GLB props (sofa, lamp, plants, rug) and the kit's table and chairs.
        static bool BuildHomeKitRoom(Transform roomRoot, Vector3 c, Transform savedStructures, Transform savedBlockers)
        {
            var shellSrc = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/VRChat/HomeKit/Prefabs/Home Geometry.prefab");
            if (shellSrc == null) { Debug.LogWarning("[CatMe][Places] Home Kit not found; using the simple room."); return false; }
            var shell = (GameObject)Object.Instantiate(shellSrc, roomRoot); shell.name = "HomeKitShell";
            shell.transform.position = c;
            StripScripts(shell);
            foreach (var l in shell.GetComponentsInChildren<Light>(true)) Object.DestroyImmediate(l);
            ApplyHomeKitMaterials(shell);
            int walkable = LayerMask.NameToLayer(GroundProbe.WalkableLayerName);
            foreach (var t in shell.GetComponentsInChildren<Transform>(true))
            {
                if (t.name == "portalDoor" || t.name == "portalDecoration1") t.gameObject.SetActive(false);   // VRChat portal pieces
                if (t.name.StartsWith("floor")) t.gameObject.layer = walkable;
            }
            // Furniture. y = 0 is the kit's floor; x runs across the room, z along it (door side is +z, back wall -z).
            const string R = "Assets/";
            Vector3 P(float x, float z) => c + new Vector3(x, 0, z);
            HomeProp(R + "carpet_free.glb", P(-0.9f, -2.6f), 0f, -3.4f, false, true);
            HomeProp("Assets/VRChat/HomeKit/Prefabs/couch.prefab", P(-0.9f, -4.5f), 0f, 0.9f, true);   // the kit's own couch fits its shell; the downloaded sofa models came in on their side
            HomeProp("Assets/VRChat/HomeKit/Prefabs/table.prefab", P(-0.9f, -2.6f), 0f, 0.5f, true);
            HomeProp("Assets/VRChat/HomeKit/Prefabs/Low_Chair.prefab", P(1.6f, -2.2f), 250f, 0.75f, true);
            HomeProp(R + "monstera_deliciosa_potted_mid-century_plant.glb", P(2.4f, -4.7f), 0f, 1.5f, false);
            HomeProp(R + "bonsaitree.glb", P(-0.9f, -2.6f) + new Vector3(0.1f, .45f, 0f), 0f, 0.5f, false);
            // Cat corner with its bed and bowl, by the sofa.
            Box("Cat bed frame", roomRoot, c + new Vector3(1.8f, .1f, -0.2f), new Vector3(1.1f, .2f, .8f), Mat("Cat bed frame", new Color(.4f, .27f, .17f)), false);
            Box("Cat bed cushion", roomRoot, c + new Vector3(1.8f, .24f, -0.2f), new Vector3(.95f, .12f, .65f), Mat("Cat bed cushion", new Color(.82f, .62f, .5f)), false);
            var bed = new GameObject("Cat_Bed"); bed.transform.SetParent(roomRoot, false); bed.transform.position = c + new Vector3(1.8f, 0f, -0.2f); bed.transform.rotation = Quaternion.Euler(0, 200, 0);
            var bowlMesh = GameObject.CreatePrimitive(PrimitiveType.Cylinder); bowlMesh.name = "Food bowl"; bowlMesh.transform.SetParent(roomRoot, false);
            bowlMesh.transform.position = c + new Vector3(0.2f, .05f, 0.9f); bowlMesh.transform.localScale = new Vector3(.3f, .05f, .3f);
            bowlMesh.GetComponent<Renderer>().sharedMaterial = Mat("Food bowl", new Color(.75f, .35f, .25f)); Object.DestroyImmediate(bowlMesh.GetComponent<Collider>());
            var bowl = new GameObject("Cat_Bowl"); bowl.transform.SetParent(roomRoot, false); bowl.transform.position = c + new Vector3(0.2f, 0f, 0.9f); bowl.transform.rotation = Quaternion.Euler(0, 180, 0);
            var spot = new GameObject("House_Spot"); spot.transform.SetParent(roomRoot, false); spot.transform.position = c + new Vector3(-0.9f, 0f, 3.6f); spot.transform.rotation = Quaternion.Euler(0, 180, 0);
            // Warm light so the evening room feels lived-in.
            foreach (var lp in new[] { new Vector3(-0.9f, 2.4f, -2.2f), new Vector3(-0.9f, 2.4f, 1.8f) })
            {
                var lamp = new GameObject("House room light"); lamp.transform.SetParent(roomRoot, false); lamp.transform.position = c + lp;
                var l = lamp.AddComponent<Light>(); l.type = LightType.Point; l.range = 9f; l.intensity = 1.1f; l.color = new Color(1f, .8f, .55f); l.shadows = LightShadows.None;
            }
            HouseRoomPersonalization.Add(roomRoot, c);
            Directory.CreateDirectory("Assets/CatMe/Resources/Rooms");
            string roomPath = "Assets/CatMe/Resources/Rooms/HouseRoom_r3.prefab";
            AssetDatabase.DeleteAsset(roomPath);
            var saved = PrefabUtility.SaveAsPrefabAsset(roomRoot.gameObject, roomPath);
            if (saved == null) throw new System.InvalidOperationException("House room prefab could not be saved: " + roomPath);
            AssetDatabase.DeleteAsset("Assets/CatMe/Resources/Rooms/HouseRoom_r2.prefab");
            Object.DestroyImmediate(roomRoot.gameObject);
            Debug.Log("[CatMe][Places] Saved Home Kit house room prefab " + roomPath);
            return true;
        }

        // size > 0: height in metres; size < 0: widest horizontal extent. Props stand on y = position.y; solid ones get a box blocker.
        static void HomeProp(string path, Vector3 position, float yaw, float size, bool solid, bool flat = false)
        {
            var src = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (src == null) { Debug.LogWarning("[CatMe][Places] Room prop missing: " + path); return; }
            var go = (GameObject)Object.Instantiate(src, GetRoomRoot()); go.name = Path.GetFileNameWithoutExtension(path);
            StripScripts(go);
            foreach (var col in go.GetComponentsInChildren<Collider>(true)) Object.DestroyImmediate(col);
            go.transform.rotation = Quaternion.Euler(0, yaw, 0);
            var b = Bounds(go);
            Debug.Log("[CatMe][Places] Room prop " + go.name + " raw size " + b.size.ToString("F2"));
            if (flat)
            {
                // A rug must lie flat: stand its thinnest axis up. (Some downloaded models are authored on their side.)
                var sz = b.size; if (sz.y > Mathf.Min(sz.x, sz.z) * 1.2f) { go.transform.rotation = Quaternion.Euler(sz.x < sz.z ? 0f : -90f, yaw, sz.x < sz.z ? 90f : 0f) * go.transform.rotation; b = Bounds(go); if (b.size.y > Mathf.Min(b.size.x, b.size.z) * 1.2f) { go.transform.rotation = Quaternion.Euler(-90f, 0f, 0f) * go.transform.rotation; b = Bounds(go); } }
            }
            if (size > 0f && b.size.y > .001f) go.transform.localScale *= size / b.size.y;
            else if (size < 0f) { float e = Mathf.Max(b.size.x, b.size.z); if (e > .001f) go.transform.localScale *= -size / e; }
            b = Bounds(go);
            go.transform.position += new Vector3(position.x - b.center.x, position.y + (flat ? .012f : 0f) - b.min.y, position.z - b.center.z);
            if (solid)
            {
                var bb = Bounds(go); var holder = new GameObject(go.name + " blocker"); holder.transform.SetParent(GetRoomRoot(), false); holder.transform.position = bb.center;
                holder.AddComponent<BoxCollider>().size = new Vector3(Mathf.Max(bb.size.x * .85f, .2f), bb.size.y, Mathf.Max(bb.size.z * .85f, .2f));
            }
        }
        // The kit ships VRChat SDK scripts that are not installed here; they would block saving the prefab, so remove every script and missing script.
        static void StripScripts(GameObject root)
        {
            foreach (var t in root.GetComponentsInChildren<Transform>(true)) GameObjectUtility.RemoveMonoBehavioursWithMissingScript(t.gameObject);
            foreach (var m in root.GetComponentsInChildren<MonoBehaviour>(true)) if (m != null) Object.DestroyImmediate(m);
        }

        static Transform currentRoomRoot;
        static Transform GetRoomRoot() => currentRoomRoot;

        static void House(Vector3 centre)
        {
            if (CountryHouse("House_Prefab.prefab", centre, 13f, 0f)) return;
            var g = new GameObject("TEMP_Replaceable_House").transform; g.SetParent(structures, false);
            Vector3 b = OnGround(centre);
            Box("House front", g, b + new Vector3(0, 2.2f, 0), new Vector3(14, 4.4f, .5f), Plaster, true);
            Box("House roof", g, b + new Vector3(0, 4.7f, -1.2f), new Vector3(15, .4f, 4.5f), Roof, false);
            Box("House porch", g, b + new Vector3(0, .06f, 1.8f), new Vector3(7, .12f, 3.6f), Timber, false);
            for (int s = -1; s <= 1; s += 2) Box("Porch post", g, b + new Vector3(s * 3.2f, 1.5f, 3.4f), new Vector3(.25f, 3f, .25f), Timber, true);
            Box("Porch roof", g, b + new Vector3(0, 3.1f, 2f), new Vector3(7.4f, .25f, 4f), Roof, false);
            for (int s = -1; s <= 1; s += 2)
            {
                var window = Box("Night_Window", g, b + new Vector3(s * 3.8f, 2.2f, .3f), new Vector3(1.6f, 1.3f, .1f), Mat("Window glow", new Color(1f, .75f, .4f), 0f, new Color(2.2f, 1.3f, .5f)), false);
                var lightGo = new GameObject("Night_Window light"); lightGo.transform.SetParent(g, false); lightGo.transform.position = window.transform.position + Vector3.forward * 1.2f;
                var l = lightGo.AddComponent<Light>(); l.type = LightType.Point; l.range = 7f; l.intensity = 1.4f; l.color = new Color(1f, .72f, .42f);
            }
            Box("Day window", g, b + new Vector3(0, 2.2f, .3f), new Vector3(1.6f, 1.3f, .1f), Mat("Window day", new Color(.35f, .45f, .5f), .7f), false);
        }

        // A small garden pond: a dark bed for depth, the stylised river water just below the rim, and a ring of the garden's own rocks.
        static void Pond(Vector3 centre, float radius)
        {
            Vector3 p = OnGround(centre);
            var pond = new GameObject("Garden pond").transform; pond.SetParent(structures, false); pond.position = p;
            var bed = GameObject.CreatePrimitive(PrimitiveType.Cylinder); bed.name = "Pond bed"; bed.transform.SetParent(pond, false);
            bed.transform.position = p + Vector3.up * .01f; bed.transform.localScale = new Vector3(radius * 2.05f, .01f, radius * 2.05f);
            bed.GetComponent<Renderer>().sharedMaterial = Mat("Pond bed", new Color(.14f, .19f, .15f)); Object.DestroyImmediate(bed.GetComponent<Collider>());
            var water = GameObject.CreatePrimitive(PrimitiveType.Cylinder); water.name = "Pond water"; water.transform.SetParent(pond, false);
            water.transform.position = p + Vector3.up * .05f; water.transform.localScale = new Vector3(radius * 1.95f, .01f, radius * 1.95f);
            water.GetComponent<Renderer>().sharedMaterial = Water; Object.DestroyImmediate(water.GetComponent<Collider>());
            ClearAround(p, radius + .7f, pond);   // no flowers, grass or bushes planted in the water or on the rim
            int count = 18;
            for (int i = 0; i < count; i++)
            {
                float a = (i + Next(-.25f, .25f)) / count * Mathf.PI * 2f;
                Vector3 q = p + new Vector3(Mathf.Cos(a), 0, Mathf.Sin(a)) * (radius + Next(.02f, .14f));
                PondRock(pond, q, Next(.14f, .26f));
            }
            for (int i = 0; i < 2; i++)   // two stones standing in the water
            {
                float a = Next(0f, Mathf.PI * 2f);
                PondRock(pond, p + new Vector3(Mathf.Cos(a), 0, Mathf.Sin(a)) * radius * Next(.4f, .6f), Next(.12f, .18f));
            }
            var blocker = new GameObject("Pond blocker"); blocker.transform.SetParent(blockers, false); blocker.transform.position = p + Vector3.up * .5f;
            var cc = blocker.AddComponent<CapsuleCollider>(); cc.radius = radius + .2f; cc.height = 1f;
        }

        static void ClearAround(Vector3 centre, float radius, Transform keep)
        {
            if (dressing == null) return;
            var doomed = new List<GameObject>();
            foreach (Transform child in dressing)
            {
                var b = Bounds(child.gameObject);
                bool tall = b.size.y > 3f;   // a tree is cleared only when its trunk stands in the pond; low planting also off the rim
                Vector3 at = tall ? child.position : b.center;
                float r = tall ? radius - .5f : radius;
                float dx = at.x - centre.x, dz = at.z - centre.z;
                if (dx * dx + dz * dz < r * r) doomed.Add(child.gameObject);
            }
            foreach (var go in doomed) Object.DestroyImmediate(go);
        }

        static void PondRock(Transform parent, Vector3 at, float height)
        {
            var go = Spawn(Ghibli + Pick(GhibliRocks) + ".prefab", parent, "Pond rock");
            if (go == null) { Box("Pond rock", parent, OnGround(at) + Vector3.up * height * .5f, new Vector3(.6f, height, .5f), Mat("Pond stone", new Color(.5f, .48f, .44f)), false); return; }
            go.transform.rotation = Quaternion.Euler(0, Next(0, 360), 0);
            var b = Bounds(go); if (b.size.y > .001f) go.transform.localScale *= height / b.size.y;
            b = Bounds(go); float wide = Mathf.Max(b.size.x, b.size.z); if (wide > height * 3.2f) go.transform.localScale *= height * 3.2f / wide;   // no flat slabs
            PlaceOnGround(go, at, go.transform.eulerAngles.y);
            go.transform.position += Vector3.down * height * .25f;   // bedded in, not perched
        }

        /// <summary>Applies the pond and lawn-rock change to the saved garden without re-scattering it: removes loose lawn boulders and the old pond, builds the new pond.</summary>
        public static string ApplyGardenPondFix(UnityEngine.SceneManagement.Scene scene)
        {
            Transform placeRoot = null;
            foreach (var r in scene.GetRootGameObjects()) if (r.GetComponentInChildren<Terrain>(true) != null) { placeRoot = r.transform; break; }
            if (placeRoot == null) return "no place root with a terrain";
            terrain = placeRoot.GetComponentInChildren<Terrain>(true);
            structures = FindChild(placeRoot, "Structures"); blockers = FindChild(placeRoot, "Blockers"); dressing = FindChild(placeRoot, "Dressing");
            if (structures == null || blockers == null) return "Structures or Blockers not found";
            rng = new System.Random(4242);
            int rocks = 0, pondParts = 0;
            foreach (var t in placeRoot.GetComponentsInChildren<Transform>(true))
            {
                if (t == null) continue;
                Vector3 c = t.position;
                bool lawn = Mathf.Abs(c.x) < 7f && c.z > -5f && c.z < 10.5f;
                if (lawn && t.name.StartsWith("Rock_") && t.parent != null && !t.parent.name.StartsWith("Rock_")) { Object.DestroyImmediate(t.gameObject); rocks++; continue; }
                if (t.name == "Pond water" || t.name == "Pond stone" || t.name == "Pond blocker" || t.name == "Garden pond") { Object.DestroyImmediate(t.gameObject); pondParts++; }
            }
            Pond(new Vector3(-4f, 0, 6.5f), 1.7f);
            return "removed " + rocks + " lawn rocks and " + pondParts + " old pond parts; new pond built";
        }

        static Transform FindChild(Transform root, string name)
        {
            foreach (var t in root.GetComponentsInChildren<Transform>(true)) if (t.name == name) return t;
            return null;
        }

        static void Bench(Vector3 p, float yaw)
        {
            var src = benchSources.FirstOrDefault();
            if (src == null) { Box("Bench", structures, OnGround(p) + Vector3.up * .25f, new Vector3(1.6f, .5f, .5f), Timber, true, yaw); AddSitPoint(p); clearZones.Add(p); return; }
            var go = Object.Instantiate(src.gameObject, structures); go.name = "Bench"; PlaceOnGround(go, p, yaw);
            AddBoxCollider(go);
            AddSitPoint(p);
            clearZones.Add(p);
        }

        static void Lookout(PlaceSpec p, float z)
        {
            float pathX = p.pathX(z);
            Vector3 bench = new Vector3(pathX + 3.1f, 0f, z);
            Bench(bench, 90f);
            var lastBench = structures.GetComponentsInChildren<Transform>(true).LastOrDefault(t => t.name == "Bench");
            if (lastBench != null) lastBench.name = "Lookout bench";

            // A small stone fan gives the camera a deliberate foreground without blocking the walking line.
            Vector3 photo = new Vector3(pathX - .25f, 0f, z + 1.8f);
            for (int i = -2; i <= 2; i++)
                Box("Lookout photo marker", structures, OnGround(photo + new Vector3(i * .32f, 0f, 0f)) + Vector3.up * .025f,
                    new Vector3(.25f, .05f, .42f), Mat("Lookout marker", new Color(.52f, .50f, .43f)), false, i * 5f);
            Zone("LookoutPhotoZone", "Overlook", new Vector3(pathX, 2f, z + 1f), new Vector3(10f, 7f, 9f), 3);

            Curiosity("Lookout keepsake", new Vector3(pathX + .8f, 0f, z + 1.1f), "lookout-keepsake",
                "One last look. Then let's walk home.", PrimitiveType.Sphere, new Vector3(.18f, .09f, .18f), new Color(.68f, .76f, .72f));
            clearZones.Add(new Vector3(pathX, 0f, z));
        }

        // ---- Phase 5: butterflies, background animals across the river and in the side woods, and the visiting dog -------------------------

        const string AnimalsFolder = "Assets/ithappy/Animals_FREE/Prefabs/";
        const string ButterflyPrefabs = "Assets/AI cartoon butterflies/AI cartoon butterflies/Models/Butterflies/Prefabs/";
        const string RainPrefab = "Assets/Rain Particles/Prefabs/Rain Particles.prefab";
        const string DogsFolder = "Assets/Bublisher/3D Stylized Animated Dogs Kit/Prefabs/";

        static void BuildLivingWorld(PlaceSpec p, float G, float L)
        {
            // Meadows: clear spots on the far bank and deep in the woods, kept free of trees and rocks so animals read clearly from the path.
            var meadows = new (string id, float z, float x)[] { ("A", G + 24f, -18f), ("B", G + 52f, -19f), ("C", G + 80f, -17f), ("W1", G + 38f, 20f), ("W2", G + 66f, 22f) };
            var wildlife = new GameObject("Wildlife"); wildlife.transform.SetParent(root, false); wildlife.AddComponent<Wildlife>();
            foreach (var m in meadows)
            {
                var spot = new Vector3(p.pathX(m.z) + m.x, 0, m.z); clearZones.Add(spot);
                var anchor = new GameObject("Meadow_" + m.id).transform; anchor.SetParent(wildlife.transform, false); anchor.position = OnGround(spot);
                switch (m.id)
                {
                    case "A": for (int i = 0; i < 3; i++) Animal(AnimalsFolder + "Deer_001.prefab", anchor, "Deer_A" + i, spot, 1.35f); break;
                    case "B": Animal(AnimalsFolder + "Deer_001.prefab", anchor, "Deer_B0", spot, 1.35f); for (int i = 0; i < 4; i++) Animal(AnimalsFolder + "Chicken_001.prefab", anchor, "Chicken_B" + i, spot + new Vector3(Next(-3f, 3f), 0, 6f + Next(-1.5f, 1.5f)), 0.38f); break;
                    case "C": Animal(AnimalsFolder + "Horse_001.prefab", anchor, "Horse_C0", spot, 1.6f); for (int i = 0; i < 2; i++) Animal(AnimalsFolder + "Deer_001.prefab", anchor, "Deer_C" + i, spot + new Vector3(Next(-4f, 4f), 0, Next(-6f, -3f)), 1.35f); break;
                    default: Animal(AnimalsFolder + "Deer_001.prefab", anchor, "Deer_" + m.id, spot, 1.35f); break;
                }
            }
            // Butterflies drifting around the garden and path (positions are re-chosen at runtime around the owner).
            var flies = new GameObject("Butterflies"); flies.transform.SetParent(root, false); flies.AddComponent<Butterflies>();
            for (int i = 0; i < 8; i++)
            {
                // A different species for each butterfly (the pack has Butterfly and Butterfly_2 to Butterfly_10).
                var bf = Spawn(ButterflyPrefabs + (i == 0 ? "Butterfly" : "Butterfly_" + (i + 1)) + ".prefab", flies.transform, "Butterfly_" + i);
                if (bf == null) break;
                var b = Bounds(bf); float e = Mathf.Max(b.size.x, b.size.z); if (e > .001f) bf.transform.localScale *= .22f / e;
                bf.transform.position = OnGround(new Vector3(Next(-5f, 5f), 0, Next(0f, 9f))) + Vector3.up * Next(.4f, 1.4f);
            }
            // The visiting dog: five breeds, inactive until a visit, plus waypoints along the walking route.
            var dogs = new GameObject("DogVisitor"); dogs.transform.SetParent(root, false); dogs.AddComponent<DogVisitor>();
            foreach (var d in new (string file, float h)[] { ("corgi", .42f), ("pug", .38f), ("cur", .5f), ("germanshepherd", .68f), ("chihuahua", .26f) })
            {
                var dog = Spawn(DogsFolder + d.file + ".prefab", dogs.transform, "Dog_" + d.file);
                if (dog == null) continue;
                var b = Bounds(dog); if (b.size.y > .001f) dog.transform.localScale *= d.h / b.size.y;
                foreach (var a in dog.GetComponentsInChildren<Animator>(true)) a.applyRootMotion = false;
                dog.SetActive(false);
            }
            var wps = new GameObject("DogPath").transform; wps.SetParent(root, false);
            int n = 0; for (float z = G + 6f; z <= G + L + 6f; z += 6f) { var w = new GameObject("wp_" + (n++)).transform; w.SetParent(wps, false); w.position = OnGround(new Vector3(p.pathX(z), 0, z)); }
        }

        static void Animal(string path, Transform parent, string name, Vector3 world, float height)
        {
            var go = Spawn(path, parent, name);
            if (go == null) return;
            go.transform.rotation = Quaternion.Euler(0, Next(0, 360), 0);
            var b = Bounds(go); if (b.size.y > .001f) go.transform.localScale *= height / b.size.y;
            PlaceOnGround(go, world, go.transform.eulerAngles.y);
            foreach (var a in go.GetComponentsInChildren<Animator>(true)) a.applyRootMotion = false;
            go.SetActive(false);
        }

        // "Sit" prompt at a bench; the sitter faces toward the walking route (x = 0 side).
        static void AddSitPoint(Vector3 p)
        {
            var o = new GameObject("Interact_Bench"); o.transform.SetParent(structures, false);
            o.transform.position = OnGround(p) + Vector3.up * .45f;
            o.transform.rotation = Quaternion.Euler(0, p.x > spec.pathX(p.z) ? 270f : 90f, 0);
            var ip = o.AddComponent<InteractionPoint>(); ip.kind = InteractionKind.Sit; ip.label = "Sit"; ip.radius = 2.4f;
        }

        // Tent beside the path with a "Get in" prompt, plus a cosy room far outside the playable area that the prompt switches to.
        static void TentWithRoom(Vector3 tentCentre, float pathEdgeX)
        {
            Tent(tentCentre);
            var o = new GameObject("Interact_Tent"); o.transform.SetParent(structures, false);
            o.transform.position = OnGround(new Vector3(pathEdgeX, 0, tentCentre.z));
            var ip = o.AddComponent<InteractionPoint>(); ip.kind = InteractionKind.EnterTent; ip.label = "Get in"; ip.radius = 3.2f;
            TentRoom(new Vector3(160f, 0f, 0f));
        }

        // Room: 5 x 5 m, 2.8 m high, warm canvas walls, a small fire in a stone ring, rug, sleeping bag, a cushion for the pet,
        // a crate and a hanging lantern. Floor is Walkable so the ground probe works. Room_Spot faces the fire.
        static void TentRoom(Vector3 c)
        {
            const float half = 2.5f, height = 2.8f;
            var walls = Mat("Tent room canvas warm", new Color(.34f, .27f, .18f));
            var floor = Box("Tent room floor", structures, c + Vector3.up * -.05f, new Vector3(half * 2f, .1f, half * 2f), Mat("Tent room floor", new Color(.24f, .19f, .14f)), true);
            floor.layer = LayerMask.NameToLayer(GroundProbe.WalkableLayerName);
            Box("Tent room wall N", structures, c + new Vector3(0, height / 2f, half), new Vector3(half * 2f, height, .1f), walls, true);
            Box("Tent room wall S", structures, c + new Vector3(0, height / 2f, -half), new Vector3(half * 2f, height, .1f), walls, true);
            Box("Tent room wall E", structures, c + new Vector3(half, height / 2f, 0), new Vector3(.1f, height, half * 2f), walls, true);
            Box("Tent room wall W", structures, c + new Vector3(-half, height / 2f, 0), new Vector3(.1f, height, half * 2f), walls, true);
            Box("Tent room roof", structures, c + new Vector3(0, height + .05f, 0), new Vector3(half * 2f, .1f, half * 2f), walls, true);
            // Poles: a ridge along the middle and one at each corner, so it reads as a tent rather than a box.
            Box("Tent ridge pole", structures, c + new Vector3(0, height - .12f, 0), new Vector3(.08f, .08f, half * 2f), Timber, false);
            for (int x = -1; x <= 1; x += 2) for (int z = -1; z <= 1; z += 2)
                Box("Tent corner pole", structures, c + new Vector3(x * (half - .12f), height / 2f, z * (half - .12f)), new Vector3(.08f, height, .08f), Timber, false);

            // Fire in a ring of stones, with crossed logs; CampFire adds flames, sparks, light and crackle at runtime.
            Vector3 fire = c + new Vector3(0f, 0f, 1.1f);
            for (int i = 0; i < 9; i++)
            {
                float a = i / 9f * Mathf.PI * 2f;
                Box("Fire stone", structures, fire + new Vector3(Mathf.Cos(a) * .42f, .07f, Mathf.Sin(a) * .42f), new Vector3(.2f, .14f, .17f), Stone, false, a * Mathf.Rad2Deg);
            }
            Box("Fire log", structures, fire + new Vector3(0, .07f, 0), new Vector3(.55f, .09f, .09f), Timber, false, 30f);
            Box("Fire log", structures, fire + new Vector3(0, .1f, 0), new Vector3(.55f, .09f, .09f), Timber, false, -40f);
            var fireGo = new GameObject("Tent fire"); fireGo.transform.SetParent(structures, false); fireGo.transform.position = fire + Vector3.up * .08f;
            fireGo.AddComponent<CampFire>();
            var blocker = Box("Tent fire blocker", structures, fire + Vector3.up * .3f, new Vector3(1f, .6f, 1f), Stone, true);
            Object.DestroyImmediate(blocker.GetComponent<Renderer>()); Object.DestroyImmediate(blocker.GetComponent<MeshFilter>());

            Box("Tent rug", structures, c + new Vector3(0f, .015f, -.2f), new Vector3(2.4f, .03f, 1.7f), Mat("Tent rug", new Color(.58f, .27f, .2f)), false);
            Box("Sleeping bag", structures, c + new Vector3(-1.75f, .07f, .1f), new Vector3(.8f, .14f, 1.9f), Mat("Sleeping bag", new Color(.25f, .38f, .52f)), false);
            Box("Pillow", structures, c + new Vector3(-1.75f, .16f, .9f), new Vector3(.55f, .12f, .32f), Mat("Pillow", new Color(.88f, .84f, .74f)), false);
            Box("Blanket", structures, c + new Vector3(-1.7f, .15f, -.35f), new Vector3(.82f, .05f, .7f), Mat("Blanket", new Color(.66f, .55f, .32f)), false);
            Box("Pet cushion", structures, c + new Vector3(1f, .05f, .55f), new Vector3(.62f, .1f, .62f), Mat("Pet cushion", new Color(.74f, .52f, .31f)), false);
            Box("Crate", structures, c + new Vector3(1.85f, .25f, 1.85f), new Vector3(.5f, .5f, .5f), Timber, true, 12f);
            Box("Mug", structures, c + new Vector3(1.8f, .55f, 1.8f), new Vector3(.09f, .11f, .09f), Mat("Mug", new Color(.85f, .9f, .92f)), false);
            Box("Hanging lantern", structures, c + new Vector3(-1.6f, 2.05f, -1.8f), new Vector3(.18f, .26f, .18f), Mat("Lantern glow", new Color(1f, .8f, .5f), 0f, new Color(1.6f, 1f, .5f)), false);
            var lg = new GameObject("Tent room lantern"); lg.transform.SetParent(structures, false); lg.transform.position = c + new Vector3(-1.6f, 1.85f, -1.8f);
            var l = lg.AddComponent<Light>(); l.type = LightType.Point; l.range = 4f; l.intensity = .35f; l.color = new Color(1f, .62f, .32f); l.shadows = LightShadows.None;

            var spot = new GameObject("Room_Spot"); spot.transform.SetParent(structures, false); spot.transform.position = c + new Vector3(0, 0, -1.5f); spot.transform.rotation = Quaternion.identity;
            // Where the pet curls up: on its cushion, facing the fire.
            var pet = new GameObject("Tent_CatSpot"); pet.transform.SetParent(structures, false); pet.transform.position = c + new Vector3(1f, .1f, .55f);
            pet.transform.rotation = Quaternion.LookRotation(new Vector3(fire.x - pet.transform.position.x, 0f, fire.z - pet.transform.position.z));
        }

        static void Lantern(string name, Vector3 p)
        {
            Vector3 b = OnGround(p);
            var lamp = PlaceProp(Ghibli + "Street_Lamp.prefab", structures, p, 0f, 2.3f, Blocker.Capsule);
            if (lamp != null)
            {
                var light = new GameObject(name + " light"); light.transform.SetParent(structures, false); light.transform.position = b + Vector3.up * 2.0f;
                var lampLight = light.AddComponent<Light>(); lampLight.type = LightType.Point; lampLight.range = 6f; lampLight.intensity = .8f; lampLight.color = new Color(1f, .72f, .42f); lampLight.shadows = LightShadows.None;
                return;
            }
            Box(name + " post", structures, b + Vector3.up * .6f, new Vector3(.2f, 1.2f, .2f), Stone, true);
            Box(name + " lamp", structures, b + Vector3.up * 1.35f, new Vector3(.35f, .4f, .35f), Mat("Lantern glow", new Color(1f, .8f, .5f), 0f, new Color(1.6f, 1f, .5f)), false);
            var lg = new GameObject(name + " light"); lg.transform.SetParent(structures, false); lg.transform.position = b + Vector3.up * 1.5f;
            var l = lg.AddComponent<Light>(); l.type = LightType.Point; l.range = 6f; l.intensity = .8f; l.color = new Color(1f, .72f, .42f); l.shadows = LightShadows.None;
        }

        static void Tent(Vector3 centre)
        {
            var g = new GameObject("TEMP_Replaceable_Tent").transform; g.SetParent(structures, false);
            Vector3 b = OnGround(centre);
            // Two leaning panels form an A-frame (about 3.4 m wide, 2.7 m tall, 4.4 m deep) so it reads as a place to shelter from the path.
            Box("Tent panel L", g, b + new Vector3(-1.05f, 1.3f, 0), new Vector3(.06f, 3.0f, 4.4f), Canvas, true, 0f).transform.rotation = Quaternion.Euler(0, 0, -32f);
            Box("Tent panel R", g, b + new Vector3(1.05f, 1.3f, 0), new Vector3(.06f, 3.0f, 4.4f), Canvas, true, 0f).transform.rotation = Quaternion.Euler(0, 0, 32f);
            Box("Tent ridge pole", g, b + new Vector3(0f, 2.62f, 0f), new Vector3(.09f, .09f, 4.8f), Timber, false);
            Box("Tent front pole", g, b + new Vector3(0f, 1.3f, 2.25f), new Vector3(.08f, 2.6f, .08f), Timber, false);
            Box("Tent back pole", g, b + new Vector3(0f, 1.3f, -2.25f), new Vector3(.08f, 2.6f, .08f), Timber, false);
            Box("Tent floor", g, b + Vector3.up * .03f, new Vector3(3.2f, .05f, 4.4f), Mat("Tent floor", new Color(.45f, .40f, .30f)), false);
            Box("Tent doormat", g, b + new Vector3(0f, .04f, 2.6f), new Vector3(1.1f, .03f, .7f), Mat("Tent rug", new Color(.58f, .27f, .2f)), false);
            // A lantern hanging at the entrance: it glows at dusk and marks the tent from along the path.
            Box("Tent entrance lantern", g, b + new Vector3(0f, 2.15f, 2.3f), new Vector3(.18f, .26f, .18f), Mat("Lantern glow", new Color(1f, .8f, .5f), 0f, new Color(1.6f, 1f, .5f)), false);
            var lg = new GameObject("Night_Tent entrance light"); lg.transform.SetParent(g, false); lg.transform.position = b + new Vector3(0f, 1.95f, 2.6f);
            var l = lg.AddComponent<Light>(); l.type = LightType.Point; l.range = 5f; l.intensity = .7f; l.color = new Color(1f, .7f, .38f); l.shadows = LightShadows.None;
        }

        static BoxCollider Shelter(Vector3 centre)
        {
            var g = new GameObject("TEMP_Replaceable_RainShelter").transform; g.SetParent(structures, false);
            Vector3 b = OnGround(centre);
            Box("Shelter roof", g, b + Vector3.up * 3.2f, new Vector3(7.2f, .3f, 6.2f), Roof, true);
            for (int x = -1; x <= 1; x += 2) for (int z = -1; z <= 1; z += 2) Box("Shelter post", g, b + new Vector3(x * 3.2f, 1.5f, z * 2.7f), new Vector3(.25f, 3f, .25f), Timber, true);
            Box("Shelter floor", g, b + Vector3.up * .04f, new Vector3(6.6f, .08f, 5.6f), Mat("Shelter floor", new Color(.50f, .46f, .40f)), false).layer = LayerMask.NameToLayer(GroundProbe.WalkableLayerName);
            Box("Shelter half wall", g, b + new Vector3(3.1f, .4f, 0), new Vector3(.4f, .8f, 5.2f), Stone, true);
            Bench(centre + new Vector3(-1.2f, 0, -.6f), 270f);
            var lg = new GameObject("Shelter lantern"); lg.transform.SetParent(g, false); lg.transform.position = b + new Vector3(0, 2.7f, 0);
            var l = lg.AddComponent<Light>(); l.type = LightType.Point; l.range = 9f; l.intensity = 1.7f; l.color = new Color(1f, .70f, .38f); l.shadows = LightShadows.None;
            Box("Lantern lamp", g, b + new Vector3(0, 2.7f, 0), new Vector3(.3f, .4f, .3f), Mat("Lantern glow", new Color(1f, .8f, .5f), 0f, new Color(1.6f, 1f, .5f)), false);
            var vol = new GameObject("ShelterVolume"); vol.transform.SetParent(root, false); vol.transform.position = b + Vector3.up * 1.5f;
            var c = vol.AddComponent<BoxCollider>(); c.isTrigger = true; c.size = new Vector3(6.4f, 3f, 5.4f);
            return c;
        }

        static void Cabin(Vector3 centre)
        {
            if (CountryHouse("House_Green_Prefab.prefab", centre, 8f, 90f, "Night_Cabin")) return;
            var g = new GameObject("TEMP_Replaceable_Cabin").transform; g.SetParent(structures, false);
            Vector3 b = centre;
            Box("Cabin walls", g, b + new Vector3(0, 1.5f, 0), new Vector3(5.5f, 3f, 4f), Timber, true);
            Box("Cabin roof", g, b + new Vector3(0, 3.3f, 0), new Vector3(6.3f, .4f, 4.8f), Roof, false);
            Box("Night_Cabin window", g, b + new Vector3(2.8f, 1.6f, 0), new Vector3(.08f, 1.1f, 1.8f), Mat("Window glow", new Color(1f, .75f, .4f), 0f, new Color(2.2f, 1.3f, .5f)), false);
            Box("Cabin window", g, b + new Vector3(2.8f, 1.6f, 0), new Vector3(.06f, 1.1f, 1.8f), Mat("Window day", new Color(.35f, .45f, .5f), .7f), false);
            var lg = new GameObject("Cabin light"); lg.transform.SetParent(g, false); lg.transform.position = b + new Vector3(3.8f, 2f, 0);
            var l = lg.AddComponent<Light>(); l.type = LightType.Point; l.range = 8f; l.intensity = 1.2f; l.color = new Color(1f, .72f, .42f); l.shadows = LightShadows.None;
        }

        static void Deck(Vector3 centre, Vector3 size)
        {
            var d = Box("Overlook deck", structures, centre + Vector3.up * -.06f, size, Mat("Deck planks", new Color(.48f, .36f, .26f)), true);
            d.layer = LayerMask.NameToLayer(GroundProbe.WalkableLayerName);
        }

        enum Blocker { None, Box, Capsule }

        static GameObject Spawn(string path, Transform parent, string name)
        {
            var src = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (src == null) { if (warned.Add(path)) Debug.LogWarning("[CatMe][Places] Missing asset, using blockout instead: " + path); return null; }
            var go = Object.Instantiate(src, parent);
            go.name = name ?? src.name;
            foreach (var c in go.GetComponentsInChildren<Collider>(true)) Object.DestroyImmediate(c); // our own blockers replace imported colliders
            foreach (var m in go.GetComponentsInChildren<MonoBehaviour>(true)) Object.DestroyImmediate(m);
            return go;
        }

        // Instantiates an imported prefab, scales it to a target height, grounds it and adds a simple blocker.
        static GameObject PlaceProp(string path, Transform parent, Vector3 p, float yaw, float targetHeight, Blocker blocker)
        {
            bool tall = blocker == Blocker.Capsule, hard = blocker == Blocker.Box || path.Contains("Shrub") || path.Contains("Rock") || path.Contains("Stone");
            if (NearClearZone(p, tall ? 6f : hard ? 3.8f : 1.0f)) return null; // no trees, stones or bushes around a bench; low flowers and grass are fine
            var go = Spawn(path, parent, null);
            if (go == null) return null;
            if (path.Contains("Anime Trees/Prefabs/Bush")) targetHeight *= 1.5f; // the pack's bushes are drawn small relative to their prefab size
            go.transform.rotation = Quaternion.Euler(0, yaw, 0);
            var b = Bounds(go);
            // Positive target = height in metres; negative target = widest horizontal extent in metres (flowers, grass).
            if (targetHeight > 0f && b.size.y > .001f) go.transform.localScale *= targetHeight / b.size.y;
            else if (targetHeight < 0f)
            {
                float extent = Mathf.Max(b.size.x, b.size.z); if (extent > .001f) go.transform.localScale *= -targetHeight / extent;
                b = Bounds(go); if (b.size.y > .9f) go.transform.localScale *= .9f / b.size.y; // keep spiky flowers knee-height
            }
            PlaceOnGround(go, p, yaw);
            if (blocker != Blocker.None) AddBlocker(go, blocker);
            return go;
        }

        static void AddBlocker(GameObject go, Blocker kind)
        {
            var b = Bounds(go);
            var holder = new GameObject(go.name + " blocker"); holder.transform.SetParent(blockers, false);
            holder.transform.position = b.center;
            if (kind == Blocker.Box) holder.AddComponent<BoxCollider>().size = new Vector3(Mathf.Max(b.size.x * .8f, .2f), b.size.y, Mathf.Max(b.size.z * .8f, .2f));
            else { var c = holder.AddComponent<CapsuleCollider>(); c.radius = Mathf.Clamp(Mathf.Min(b.size.x, b.size.z) * .3f, .1f, .5f); c.height = b.size.y; }
        }

        // Tiles an imported fence-like prefab between two points; its long horizontal axis sets the spacing.
        static void PlaceLine(string path, Vector3 a, Vector3 b, float targetHeight, Blocker blocker = Blocker.None)
        {
            var probe = Spawn(path, structures, "Line probe");
            if (probe == null) return;
            var pb = Bounds(probe);
            float scale = targetHeight > 0f && pb.size.y > .001f ? targetHeight / pb.size.y : 1f;
            bool alongX = pb.size.x >= pb.size.z;
            float segment = (alongX ? pb.size.x : pb.size.z) * scale;
            Object.DestroyImmediate(probe);
            if (segment < .2f) return;
            Vector3 dir = b - a; float length = dir.magnitude; dir /= Mathf.Max(length, .001f);
            float yaw = Mathf.Atan2(dir.x, dir.z) * Mathf.Rad2Deg - (alongX ? 90f : 0f);
            int count = Mathf.Max(1, Mathf.RoundToInt(length / segment));
            for (int i = 0; i < count; i++)
            {
                Vector3 centre = a + dir * ((i + .5f) * length / count);
                var piece = Spawn(path, structures, "Fence piece");
                piece.transform.localScale *= scale * (length / count) / segment;
                piece.transform.rotation = Quaternion.Euler(0, yaw, 0);
                PlaceOnGround(piece, centre, yaw);
                if (blocker != Blocker.None) AddBlocker(piece, blocker);
            }
        }

        // Lush edge along the path: grass tufts, flowers, ferns and small rocks hugging both sides, like a worn trail through a meadow.
        static void EdgeDressing(PlaceSpec s, float zFrom, float zTo, float perMetre, float halfWidth)
        {
            int count = Mathf.RoundToInt((zTo - zFrom) * perMetre * 2f);
            for (int i = 0; i < count; i++)
            {
                float z = Next(zFrom, zTo), side = rng.Next(2) == 0 ? -1f : 1f;
                float x = s.pathX(z) + side * (halfWidth + Next(.1f, 1.6f));
                var at = new Vector3(x, 0, z);
                int kind = rng.Next(10);
                if (kind < 4) PlaceProp(Ghibli + Pick(GhibliGrass) + ".prefab", dressing, at, Next(0, 360), -Next(.6f, 1.1f), Blocker.None);
                else if (kind < 7) PlaceProp(Ghibli + Pick(GhibliFlowers) + ".prefab", dressing, at, Next(0, 360), -Next(.35f, .65f), Blocker.None);
                else if (kind < 9) PlaceFrom(fernSources, at, Next(0, 360), Next(.9f, 1.4f), dressing);
                else PlaceProp(Ghibli + Pick(GhibliRocks) + ".prefab", dressing, at, Next(0, 360), Next(.25f, .5f), Blocker.None);
            }
        }

        // Rocks standing in the water and a green, flowery bank, like the reference shot.
        static void RiverDressing(PlaceSpec s, float offsetX, float zFrom, float zTo, int waterRocks, int bankItems)
        {
            for (int i = 0; i < waterRocks; i++)
            {
                float z = Next(zFrom, zTo);
                PlaceProp(Ghibli + Pick(GhibliRocks) + ".prefab", dressing, new Vector3(s.pathX(z) + offsetX + Next(-2.6f, 2.6f), 0, z), Next(0, 360), Next(.5f, 1.3f), Blocker.None);
            }
            for (int i = 0; i < bankItems; i++)
            {
                float z = Next(zFrom, zTo), side = rng.Next(2) == 0 ? 1f : -1f;
                var at = new Vector3(s.pathX(z) + offsetX + side * (3.5f + Next(0f, .9f)), 0, z);
                if (rng.Next(3) == 0) PlaceFrom(fernSources, at, Next(0, 360), Next(1f, 1.6f), dressing);
                else if (rng.Next(2) == 0) PlaceProp(Ghibli + Pick(GhibliGrass) + ".prefab", dressing, at, Next(0, 360), -Next(.8f, 1.4f), Blocker.None);
                else PlaceProp(Ghibli + Pick(GhibliFlowers) + ".prefab", dressing, at, Next(0, 360), -Next(.4f, .7f), Blocker.None);
            }
        }

        // Layered tree clusters beside the route: pines, cherry and round trees at mixed sizes, set back from the path.
        static void TreeClusters(PlaceSpec s, int side, float zFrom, float zTo, int clusters, float minDist, float maxDist)
        {
            for (int c = 0; c < clusters; c++)
            {
                float z = Next(zFrom, zTo), baseDist = Next(minDist, maxDist);
                int members = 2 + rng.Next(3);
                for (int m = 0; m < members; m++)
                {
                    float zz = z + Next(-3.5f, 3.5f), d = baseDist + Next(-1.5f, 3.5f);
                    var at = new Vector3(s.pathX(zz) + side * d, 0, zz);
                    if (rng.Next(3) == 0) PlaceProp(PickAnimeTree(), dressing, at, Next(0, 360), Next(5.5f, 9f), Blocker.Capsule);
                    else PlaceTree(at, Next(1.15f, 1.7f));
                }
            }
        }

        // Dense ground cover for a forest side: shrubs, ferns and tall grass at random distances from the path.
        static void Undergrowth(PlaceSpec s, int side, float zFrom, float zTo, int count, float minDist, float maxDist)
        {
            for (int i = 0; i < count; i++)
            {
                float z = Next(zFrom, zTo), d = Next(minDist, maxDist);
                var at = new Vector3(s.pathX(z) + side * d, 0, z);
                int kind = rng.Next(10);
                if (kind < 3) PlaceProp(PickBush(), dressing, at, Next(0, 360), Next(.6f, 1.05f), Blocker.None);   // fewer, smaller bushes
                else if (kind < 5) PlaceFrom(fernSources, at, Next(0, 360), Next(1f, 1.7f), dressing);
                else if (kind < 7) PlaceProp(Ghibli + Pick(GhibliGrass) + ".prefab", dressing, at, Next(0, 360), -Next(.9f, 1.5f), Blocker.None);
                else if (kind < 9) PlaceProp(Ghibli + Pick(GhibliFlowers) + ".prefab", dressing, at, Next(0, 360), -Next(.45f, .8f), Blocker.None);
                else PlaceProp(PickAnimeTree(), dressing, at, Next(0, 360), Next(2.6f, 4.2f), Blocker.None);              // slim young trees
            }
        }

        // Natural-looking painterly shrubs and multi-stem trees from the Anime Nature pack replace the puffy teal bushes and round trees.
        static string PickBush() => "Assets/Anime Trees/Prefabs/Bush_0" + (1 + rng.Next(4)) + ".prefab";
        static string PickAnimeTree() => "Assets/Anime Trees/Prefabs/Tree_0" + (1 + rng.Next(8)) + ".prefab";

        static string Pick(string[] names) => names[rng.Next(names.Length)];

        // Imported rocks, shrubs, flowers, grass and filler trees scattered like Dress(), kept off the path.
        static void DressProps(int rocks, int shrubs, int flowers, int grass, int trees, float xRange, float zFrom, float zTo, float avoidHalf, int side = 0)
        {
            for (int i = 0; i < rocks; i++) PlaceProp(Ghibli + Pick(GhibliRocks) + ".prefab", dressing, Scatter(xRange * .8f, zFrom, zTo, avoidHalf * .9f, side), Next(0, 360), Next(.5f, 1.3f), Blocker.Box);
            for (int i = 0; i < shrubs; i++) PlaceProp(PickBush(), dressing, Scatter(xRange * .7f, zFrom, zTo, avoidHalf, side), Next(0, 360), Next(.6f, 1.1f), Blocker.None);
            for (int i = 0; i < flowers; i++) PlaceProp(Ghibli + Pick(GhibliFlowers) + ".prefab", dressing, Scatter(xRange * .5f, zFrom, zTo, avoidHalf + .3f, side), Next(0, 360), -Next(.35f, .7f), Blocker.None);
            for (int i = 0; i < grass; i++) PlaceProp(Ghibli + Pick(GhibliGrass) + ".prefab", dressing, Scatter(xRange * .6f, zFrom, zTo, avoidHalf + .2f, side), Next(0, 360), -Next(.5f, .9f), Blocker.None);
            for (int i = 0; i < trees; i++) PlaceProp(PickAnimeTree(), dressing, Scatter(xRange, zFrom, zTo, avoidHalf + 3f, side), Next(0, 360), Next(5f, 8f), Blocker.Capsule);
        }

        [MenuItem("CatMe/Places/Convert Imported Materials To URP")]
        public static void ConvertImportedMaterialsMenu() => ConvertImportedMaterials();

        // Standard-shader materials (imported from older packs) turn pink in URP; convert them in place.
        static void ConvertImportedMaterials()
        {
            string[] folders = { "Assets/JTS_Shrine_Torii_Gates", "Assets/Free/Stones", "Assets/TileableBricksWall", "Assets/AI cartoon butterflies/AI cartoon butterflies/Models/Butterflies/Materials", "Assets/ALP_Assets/country house01", "Assets/LowPolyLivingRoomPack", "Assets/ithappy", "Assets/Bublisher", "Assets/VRChat/HomeKit" };
            var urp = Shader.Find("Universal Render Pipeline/Lit");
            int converted = 0;
            foreach (string guid in AssetDatabase.FindAssets("t:Material", folders.Where(AssetDatabase.IsValidFolder).ToArray()))
            {
                var m = AssetDatabase.LoadAssetAtPath<Material>(AssetDatabase.GUIDToAssetPath(guid));
                if (m == null || m.shader == null || urp == null) continue;
                string name = m.shader.name;
                // Convert anything that is not already a URP / Shader Graph / in-house shader (older packs use many built-in names).
                if (name.StartsWith("Universal Render Pipeline") || name.StartsWith("Shader Graphs/") || name.StartsWith("CatMe/")) continue;
                Debug.Log("[CatMe][Places] Converting " + m.name + " (" + name + ")");
                Texture main = m.HasProperty("_MainTex") ? m.GetTexture("_MainTex") : null;
                Color color = m.HasProperty("_Color") ? m.GetColor("_Color") : Color.white;
                Texture normal = m.HasProperty("_BumpMap") ? m.GetTexture("_BumpMap") : null;
                float cutoff = m.HasProperty("_Mode") ? m.GetFloat("_Mode") : 0f;
                Vector2 tiling = m.HasProperty("_MainTex") ? m.GetTextureScale("_MainTex") : Vector2.one;
                m.shader = urp;
                m.SetTexture("_BaseMap", main); m.SetColor("_BaseColor", color); m.SetTextureScale("_BaseMap", tiling);
                if (normal != null) { m.SetTexture("_BumpMap", normal); m.EnableKeyword("_NORMALMAP"); }
                m.SetFloat("_Smoothness", .15f); m.SetFloat("_Metallic", 0f);
                if (cutoff == 1f) { m.SetFloat("_AlphaClip", 1f); m.EnableKeyword("_ALPHATEST_ON"); m.SetFloat("_Cutoff", .5f); }
                if (m.name.ToLowerInvariant().Contains("glass"))
                {
                    m.SetFloat("_Surface", 1f); m.SetFloat("_Blend", 0f); m.SetOverrideTag("RenderType", "Transparent");
                    m.EnableKeyword("_SURFACE_TYPE_TRANSPARENT"); m.renderQueue = 3000; m.SetFloat("_ZWrite", 0f);
                    m.SetColor("_BaseColor", new Color(.75f, .88f, .95f, .25f)); m.SetFloat("_Smoothness", .9f);
                }
                EditorUtility.SetDirty(m); converted++;
            }
            converted += ConvertParticleMaterials("Assets/Rain Particles/Materials");
            AssetDatabase.SaveAssets();
            Debug.Log("[CatMe][Places] Converted " + converted + " imported materials to URP.");
        }

        // Built-in particle materials (Rain Particles pack) -> URP Particles/Unlit, alpha blended, keeping texture and tint.
        static int ConvertParticleMaterials(string folder)
        {
            var unlit = Shader.Find("Universal Render Pipeline/Particles/Unlit");
            if (unlit == null || !AssetDatabase.IsValidFolder(folder)) return 0;
            int converted = 0;
            foreach (string guid in AssetDatabase.FindAssets("t:Material", new[] { folder }))
            {
                var m = AssetDatabase.LoadAssetAtPath<Material>(AssetDatabase.GUIDToAssetPath(guid));
                if (m == null || m.shader == null || m.shader.name.StartsWith("Universal Render Pipeline") || m.name.StartsWith("Demo-")) continue;
                Debug.Log("[CatMe][Places] Converting particle material " + m.name + " (" + m.shader.name + ")");
                Texture main = m.HasProperty("_MainTex") ? m.GetTexture("_MainTex") : null;
                Color color = m.HasProperty("_Color") ? m.GetColor("_Color") : m.HasProperty("_TintColor") ? m.GetColor("_TintColor") : Color.white;
                m.shader = unlit;
                m.SetTexture("_BaseMap", main); m.SetColor("_BaseColor", color);
                m.SetFloat("_Surface", 1f); m.SetFloat("_Blend", 0f); m.SetOverrideTag("RenderType", "Transparent");
                m.SetFloat("_SrcBlend", (float)UnityEngine.Rendering.BlendMode.SrcAlpha); m.SetFloat("_DstBlend", (float)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
                m.SetFloat("_ZWrite", 0f); m.EnableKeyword("_SURFACE_TYPE_TRANSPARENT"); m.renderQueue = 3000;
                EditorUtility.SetDirty(m); converted++;
            }
            return converted;
        }

        static void Curiosity(string name, Vector3 p, string keepsake, string line, PrimitiveType shape, Vector3 size, Color color)
        {
            var go = GameObject.CreatePrimitive(shape); go.name = "Curiosity_" + name; go.transform.SetParent(dressing, false);
            Vector3 g = OnGround(p); go.transform.position = g + Vector3.up * (size.y * .5f + .02f); go.transform.localScale = size;
            go.GetComponent<Renderer>().sharedMaterial = Mat("Curiosity " + name, color);
            Object.DestroyImmediate(go.GetComponent<Collider>());
            var c = go.AddComponent<CuriosityPoint>(); c.keepsakeId = keepsake; c.foundLine = line;
        }

        static void GardenBeds()
        {
            // Flower and shrub beds hugging the walls inside the courtyard.
            for (int i = 0; i < 26; i++)
            {
                float side = i % 2 == 0 ? -1 : 1;
                PlaceFrom(shrubSources, new Vector3(side * Next(8.5f, 10.2f), 0, Next(-3f, 15f)), Next(0, 360), Next(.8f, 1.2f), dressing);
            }
            for (int i = 0; i < 36; i++)
            {
                float side = i % 2 == 0 ? -1 : 1;
                PlaceFrom(flowerSources, new Vector3(side * Next(7.2f, 10.2f), 0, Next(-3f, 15f)), Next(0, 360), 1f, dressing);
            }
            for (int i = 0; i < 6; i++) PlaceFrom(rockSources, new Vector3((i % 2 == 0 ? -1 : 1) * Next(7.5f, 9.5f), 0, Next(0, 14)), Next(0, 360), Next(.8f, 1.2f), dressing);
            for (int i = 0; i < 8; i++) PlaceFrom(stepSources, new Vector3(Mathf.Sin(i) * .4f, 0, 3 + i * 1.3f), Next(-20, 20), 1f, dressing);
            PlaceTree(new Vector3(-8.5f, 0, 1f), 1.1f); PlaceTree(new Vector3(8.5f, 0, 13f), 1f);
        }

        static void Dress(int trees, int rocks, int shrubs, int flowers, float xRange, float zFrom, float zTo, float avoidHalf, int side = 0, float? reedsX = null, int ferns = 0)
        {
            for (int i = 0; i < trees; i++) PlaceTree(Scatter(xRange, zFrom, zTo, avoidHalf, side), Next(.9f, 1.3f));
            for (int i = 0; i < rocks; i++) PlaceFrom(rockSources, Scatter(xRange * .8f, zFrom, zTo, avoidHalf * .8f, side), Next(0, 360), Next(.7f, 1.5f), dressing);
            for (int i = 0; i < shrubs; i++) PlaceFrom(shrubSources, Scatter(xRange * .7f, zFrom, zTo, avoidHalf, side), Next(0, 360), Next(.7f, 1.2f), dressing);
            for (int i = 0; i < ferns; i++) PlaceFrom(fernSources, Scatter(xRange * .7f, zFrom, zTo, avoidHalf, side), Next(0, 360), Next(.8f, 1.2f), dressing);
            for (int i = 0; i < flowers; i++) PlaceFrom(flowerSources, Scatter(xRange * .5f, zFrom, zTo, avoidHalf + .4f, side), Next(0, 360), 1f, dressing);
            if (reedsX.HasValue) for (int i = 0; i < 24; i++) PlaceFrom(fernSources, new Vector3(reedsX.Value + Next(-1f, 1f), 0, Next(zFrom, zTo)), Next(0, 360), Next(1f, 1.6f), dressing);
        }

        static Vector3 Scatter(float xRange, float zFrom, float zTo, float avoidHalf, int side)
        {
            for (int tries = 0; tries < 12; tries++)
            {
                float z = Next(zFrom, zTo), x = Next(-xRange, xRange);
                if (side != 0 && Mathf.Sign(x) != Mathf.Sign(side)) x = -x;
                if (Mathf.Abs(x - spec.pathX(z)) > avoidHalf) return new Vector3(x, 0, z);
            }
            return new Vector3(spec.x1 - 3f, 0, Next(zFrom, zTo));
        }

        static void MountainSilhouettes(float z)
        {
            const int samples = 33;
            for (int layer = 0; layer < 3; layer++)
            {
                var vertices = new Vector3[samples * 2]; var triangles = new int[(samples - 1) * 6];
                float depth = z + 14f + layer * 18f;
                for (int i = 0; i < samples; i++)
                {
                    float x = Mathf.Lerp(-95f, 95f, i / (float)(samples - 1));
                    float broad = Mathf.PerlinNoise(i * .16f + layer * 7.1f, layer * 3.7f);
                    float detail = Mathf.PerlinNoise(i * .61f + layer * 11f, 3.2f);
                    float crest = 8f + broad * (layer + 1) * 5f + detail * 3f;
                    vertices[i * 2] = new Vector3(x, -8f, depth);
                    vertices[i * 2 + 1] = new Vector3(x, crest, depth);
                    if (i == samples - 1) continue;
                    int v = i * 2, t = i * 6;
                    triangles[t] = v; triangles[t + 1] = v + 1; triangles[t + 2] = v + 3;
                    triangles[t + 3] = v; triangles[t + 4] = v + 3; triangles[t + 5] = v + 2;
                }
                var mesh = new Mesh { name = "Mountain silhouette " + layer };
                mesh.vertices = vertices; mesh.triangles = triangles; mesh.RecalculateNormals(); mesh.RecalculateBounds();
                string path = ArtFolder + "/Terrain/" + spec.id + "_Mountain" + layer + ".asset";
                AssetDatabase.DeleteAsset(path); AssetDatabase.CreateAsset(mesh, path);
                var go = new GameObject("Mountain silhouette " + layer, typeof(MeshFilter), typeof(MeshRenderer));
                go.transform.SetParent(dressing, false);
                go.GetComponent<MeshFilter>().sharedMesh = AssetDatabase.LoadAssetAtPath<Mesh>(path);
                go.GetComponent<MeshRenderer>().sharedMaterial = Mat("Mountain layer " + layer, Color.Lerp(new Color(.27f, .39f, .39f), new Color(.48f, .58f, .62f), layer / 2f));
            }
        }

        static void NightSky(float z)
        {
            var sky = new GameObject("Night_Starfield"); sky.transform.SetParent(dressing, false);
            const int stars = 48;
            var vertices = new Vector3[stars * 4]; var triangles = new int[stars * 6];
            var colours = new Color[stars * 4];
            var random = new System.Random(9157);
            for (int i = 0; i < stars; i++)
            {
                float x = (float)(random.NextDouble() * 180f - 90f);
                float y = (float)(random.NextDouble() * 28f + 23f);
                float radius = i % 9 == 0 ? .28f : .14f;
                int v = i * 4, t = i * 6;
                vertices[v] = new Vector3(x - radius, y - radius, z); vertices[v + 1] = new Vector3(x + radius, y - radius, z);
                vertices[v + 2] = new Vector3(x - radius, y + radius, z); vertices[v + 3] = new Vector3(x + radius, y + radius, z);
                triangles[t] = v; triangles[t + 1] = v + 2; triangles[t + 2] = v + 1;
                triangles[t + 3] = v + 1; triangles[t + 4] = v + 2; triangles[t + 5] = v + 3;
                for (int c = 0; c < 4; c++) colours[v + c] = new Color(.85f, .9f, 1f);
            }
            var mesh = new Mesh { name = "Night stars" }; mesh.vertices = vertices; mesh.triangles = triangles; mesh.colors = colours; mesh.RecalculateBounds();
            string path = ArtFolder + "/Terrain/" + spec.id + "_Stars.asset";
            AssetDatabase.DeleteAsset(path); AssetDatabase.CreateAsset(mesh, path);
            var filter = sky.AddComponent<MeshFilter>(); filter.sharedMesh = AssetDatabase.LoadAssetAtPath<Mesh>(path);
            sky.AddComponent<MeshRenderer>().sharedMaterial = Mat("Night stars", new Color(.8f, .88f, 1f), 0f, new Color(1.4f, 1.55f, 1.8f));
            var moon = GameObject.CreatePrimitive(PrimitiveType.Sphere); moon.name = "Night_Moon"; moon.transform.SetParent(dressing, false);
            moon.transform.position = new Vector3(-29f, 37f, z + 2f); moon.transform.localScale = Vector3.one * 4f;
            moon.GetComponent<Renderer>().sharedMaterial = Mat("Night moon", new Color(.92f, .94f, .84f), 0f, new Color(.8f, .9f, .8f));
            Object.DestroyImmediate(moon.GetComponent<Collider>());
        }

        // ---- scattering from the legacy garden prefab (materials are already URP-converted there) -------------------

        static void LoadSources()
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(LegacyPrefab);
            if (prefab == null) throw new FileNotFoundException("Build the legacy garden first (CatMe > Garden > Build First Garden): " + LegacyPrefab);
            var all = prefab.GetComponentsInChildren<Transform>(true);
            List<Transform> By(params string[] names) => all.Where(t => names.Contains(t.name)).ToList();
            treeSources = By("Grove canopy", "Journey grove", "TEMP Woodland approach tree");
            rockSources = By("Weathered garden rock"); shrubSources = By("Layered boxwood"); fernSources = By("Fern understory");
            flowerSources = By("Wildflower drift"); benchSources = By("Shaded resting bench", "Shelter resting bench"); stepSources = By("Inset stepping stone");
            foreach (var kv in new Dictionary<string, List<Transform>> { { "trees", treeSources }, { "rocks", rockSources }, { "shrubs", shrubSources }, { "ferns", fernSources }, { "flowers", flowerSources }, { "benches", benchSources } })
                if (kv.Value.Count == 0) Debug.LogWarning("[CatMe][Places] No source objects named for " + kv.Key + " in the legacy prefab; that dressing will be skipped.");
        }

        static void PlaceTree(Vector3 p, float scale) => PlaceFrom(treeSources, p, Next(0, 360), scale, dressing, solidTrunk: true);

        static void PlaceFrom(List<Transform> sources, Vector3 p, float yaw, float scale, Transform parent, bool solidTrunk = false)
        {
            if (sources == null || sources.Count == 0) return;
            if (ReferenceEquals(sources, shrubSources)) { PlaceProp(PickBush(), parent, p, yaw, scale * 1.1f, Blocker.None); return; }
            var src = sources[rng.Next(sources.Count)];
            bool hard = src.name.Contains("rock") || src.name.Contains("boxwood") || src.name.Contains("stepping");
            if (NearClearZone(p, solidTrunk ? 6f : hard ? 3.8f : 1.0f)) return;
            var go = Object.Instantiate(src.gameObject, parent);
            go.name = src.name; go.transform.localScale = src.localScale * scale;
            PlaceOnGround(go, p, yaw);
            if (solidTrunk)
            {
                var b = Bounds(go);
                var blocker = new GameObject("Trunk blocker"); blocker.transform.SetParent(blockers, false);
                blocker.transform.position = new Vector3(b.center.x, b.min.y + .8f, b.center.z);
                var c = blocker.AddComponent<CapsuleCollider>(); c.radius = .35f; c.height = 1.6f;
            }
        }

        static void PlaceOnGround(GameObject go, Vector3 p, float yaw)
        {
            go.transform.rotation = Quaternion.Euler(0, yaw, 0);
            var b = Bounds(go);
            float y = GroundAt(p.x, p.z);
            go.transform.position += new Vector3(p.x - b.center.x, y - b.min.y, p.z - b.center.z);
        }

        static Bounds Bounds(GameObject go)
        {
            var rs = go.GetComponentsInChildren<Renderer>(true).Where(r => r.enabled).ToArray();
            if (rs.Length == 0) return new Bounds(go.transform.position, Vector3.one * .01f);
            var b = rs[0].bounds; foreach (var r in rs.Skip(1)) b.Encapsulate(r.bounds); return b;
        }
        static void AddBoxCollider(GameObject go) { var b = Bounds(go); var c = go.AddComponent<BoxCollider>(); c.center = go.transform.InverseTransformPoint(b.center); c.size = new Vector3(b.size.x / Mathf.Max(go.transform.lossyScale.x, .001f), b.size.y / Mathf.Max(go.transform.lossyScale.y, .001f), b.size.z / Mathf.Max(go.transform.lossyScale.z, .001f)); }

        static float Next(float a, float b) => (float)(a + (b - a) * rng.NextDouble());

        // ---- project setup ------------------------------------------------------------------------------------------

        static void EnsureWalkableLayer()
        {
            var tagManager = new SerializedObject(AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/TagManager.asset")[0]);
            var layers = tagManager.FindProperty("layers");
            for (int i = 8; i < layers.arraySize; i++)
            {
                if (layers.GetArrayElementAtIndex(i).stringValue == GroundProbe.WalkableLayerName) return;
            }
            for (int i = 8; i < layers.arraySize; i++)
            {
                if (string.IsNullOrEmpty(layers.GetArrayElementAtIndex(i).stringValue))
                {
                    layers.GetArrayElementAtIndex(i).stringValue = GroundProbe.WalkableLayerName;
                    tagManager.ApplyModifiedProperties();
                    return;
                }
            }
            throw new InvalidOperationException("No free user layer for Walkable.");
        }

        // Riverside, Woodland, Tent and Retreat were merged into the single garden-and-river place; remove their generated assets.
        static void RemoveRetiredPlaces()
        {
            // Earlier revisions of the merged scene (and the retired places) are deleted so only the current file exists.
            AssetDatabase.DeleteAsset(SceneFolder + "/Place_HomeGarden.unity");
            foreach (string id in new[] { "Riverside", "Woodland", "TentAndShelter", "MountainRetreat" })
            {
                AssetDatabase.DeleteAsset(SceneFolder + "/Place_" + id + ".unity");
                AssetDatabase.DeleteAsset(ArtFolder + "/Terrain/" + id + "_Terrain.asset");
                AssetDatabase.DeleteAsset(ArtFolder + "/Terrain/" + id + "_Path.asset");
            }
        }

        static void RegisterBuildScenes(IEnumerable<string> placePaths)
        {
            // Drop any place scene that no longer exists, then make sure the current ones are registered.
            var keep = new HashSet<string>(placePaths);
            var list = EditorBuildSettings.scenes.Where(sc => !sc.path.StartsWith(SceneFolder) || keep.Contains(sc.path)).ToList();
            foreach (var path in placePaths)
                if (!list.Any(s => s.path == path)) list.Add(new EditorBuildSettingsScene(path, true));
            EditorBuildSettings.scenes = list.ToArray();
        }
    }
}
#endif
