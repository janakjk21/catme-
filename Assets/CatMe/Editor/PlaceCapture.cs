#if UNITY_EDITOR
using System;
using System.IO;
using System.Linq;
using CatMe.Session;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace CatMe.Editor
{
    /// <summary>
    /// Builds the place scenes (optional) and renders gameplay-camera stills of each place without Play Mode.
    /// Run in batch mode: Unity -batchMode -projectPath ... -executeMethod CatMe.Editor.PlaceCapture.Run
    /// Stand-in capsules mark the owner (1.70 m) and cat (0.46 m); real characters are not loaded.
    /// </summary>
    public static class PlaceCapture
    {
        const int Width = 1920, Height = 864;
        static string OutDir => Environment.GetEnvironmentVariable("CATME_RENDER_OUT") ?? Path.GetFullPath("docs/implementation/production/phase-4/renders");

        sealed class Shot { public float yawOffset; public bool showWildlife, showDog; public bool hasAbs; public Vector3 absPos; public float absYaw; public bool sunOff; public float reflection = .2f; public string place, name, entry; public float along, yaw, dist = 4.2f, height = 1.9f, pitch = 9f, fov = 50f, offsetX; public bool fromExit; public string lighting; }

        public static void Run()
        {
            try
            {
                ShaderUtil.allowAsyncCompilation = false; // otherwise the first renders show fallback shaders
                if (Environment.GetEnvironmentVariable("CATME_SKIP_BUILD") != "1") PlaceSceneBuilder.BuildAllCore();
                Directory.CreateDirectory(OutDir);
                var shots = new[]
                {
                    new Shot { place = "HomeGarden", name = "01-garden-start", entry = "Start", along = 0f, dist = 3.6f, height = 1.7f, pitch = 10f, fov = 48f },
                    new Shot { place = "HomeGarden", name = "02-garden-gate", entry = "Start", along = 5.5f, dist = 4.6f, height = 1.5f, pitch = 4f, fov = 52f },
                    new Shot { place = "HomeGarden", name = "03-garden-overview", entry = "Start", along = 3f, dist = 11f, height = 7f, pitch = 24f, fov = 55f },
                    new Shot { place = "HomeGarden", name = "03d-house-view", entry = "Return", along = 0f, dist = 4.5f, height = 2.2f, pitch = 8f, fov = 55f },
                    new Shot { place = "HomeGarden", name = "04-just-past-gate", entry = "Start", along = 15f, dist = 3.2f, height = 1.7f, pitch = 8f, fov = 52f, offsetX = .24f },
                    new Shot { place = "HomeGarden", name = "05-riverside-walk", entry = "Start", along = 40f, dist = 3.2f, height = 1.7f, pitch = 8f, fov = 52f, offsetX = .24f },
                    new Shot { place = "HomeGarden", name = "06-riverside-far", entry = "Start", along = 70f, dist = 3.2f, height = 1.7f, pitch = 8f, fov = 52f, offsetX = .24f },
                    new Shot { place = "HomeGarden", name = "07-end-of-path", entry = "Start", along = 100f, dist = 3.2f, height = 1.7f, pitch = 8f, fov = 52f, offsetX = .24f },
                    new Shot { place = "HomeGarden", name = "10-bench", entry = "Start", along = 27f, dist = 3.2f, height = 1.7f, pitch = 8f, fov = 52f, offsetX = -.2f },
                    new Shot { place = "HomeGarden", name = "11-room-a", entry = "Start", hasAbs = true, absPos = new Vector3(219.1f, 1.55f, 3.6f), absYaw = 180f, pitch = 8f, fov = 70f },
                    new Shot { place = "HomeGarden", name = "12b-room-c", entry = "Start", hasAbs = true, absPos = new Vector3(219.1f, 2.2f, 4.8f), absYaw = 180f, pitch = 18f, fov = 85f },
                    new Shot { place = "HomeGarden", name = "12-room-b", entry = "Start", hasAbs = true, absPos = new Vector3(219.1f, 1.55f, 0.2f), absYaw = 215f, pitch = 8f, fov = 70f },
                    new Shot { place = "HomeGarden", name = "13-far-bank-a", entry = "Start", along = 33f, yawOffset = -62f, dist = 3.2f, height = 1.7f, pitch = 4f, fov = 55f, showWildlife = true },
                    new Shot { place = "HomeGarden", name = "14-far-bank-b", entry = "Start", along = 62f, yawOffset = -62f, dist = 3.2f, height = 1.7f, pitch = 4f, fov = 55f, showWildlife = true },
                    new Shot { place = "HomeGarden", name = "15-dog-visit", entry = "Start", along = 40f, dist = 3.0f, height = 1.6f, pitch = 8f, fov = 52f, showDog = true },
                    new Shot { place = "HomeGarden", name = "16-butterflies-garden", entry = "Start", along = 2f, dist = 3.0f, height = 1.6f, pitch = 8f, fov = 52f },
                    new Shot { place = "HomeGarden", name = "09-tent", entry = "Start", along = 52f, dist = 3.2f, height = 1.7f, pitch = 8f, fov = 52f, offsetX = -.2f },
                    new Shot { place = "HomeGarden", name = "08-from-garden-no-river", entry = "Start", along = 2f, dist = 3.6f, height = 1.8f, pitch = 6f, fov = 60f },
                };
                string current = null;
                foreach (var shot in shots)
                {
                    string path = SessionDirector.ScenePath(shot.place);
                    if (current != shot.place || shot.lighting != null) { EditorSceneManager.OpenScene(path, OpenSceneMode.Single); current = shot.place; }
                    Render(shot);
                }
                Debug.Log("[CatMe][Capture] Rendered " + shots.Length + " stills to " + OutDir);
                File.WriteAllText(Path.Combine(OutDir, "capture-result.txt"), "SUCCESS " + DateTime.Now.ToString("O"));
                EditorApplication.Exit(0);
            }
            catch (Exception e)
            {
                Debug.LogError("[CatMe][Capture] " + e);
                File.WriteAllText("/tmp/catme-capture-error.txt", e.ToString());
                EditorApplication.Exit(1);
            }
        }

        /// <summary>Renders a labelled-by-order lineup of candidate plants and trees side by side, to choose realistic-looking ones.</summary>
        public static void Lineup()
        {
            try
            {
                ShaderUtil.allowAsyncCompilation = false;
                Directory.CreateDirectory(OutDir);
                EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
                var temp = new GameObject("LINEUP");
                ApplyLighting(temp.transform, PlaceLightingPreset.Get("Sunset"));
                var legacy = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/CatMe/Resources/CompanionGarden/GardenEnvironment.prefab");
                var rows = new (string path, float height)[][]
                {
                    new[] { ("Assets/ithappy/Animals_FREE/Prefabs/Deer_001.prefab", 1.35f), ("Assets/ithappy/Animals_FREE/Prefabs/Horse_001.prefab", 1.6f), ("Assets/ithappy/Animals_FREE/Prefabs/Chicken_001.prefab", .38f), ("Assets/Butterfly (Animated)/Prefab/Butterfly.prefab", .22f) },
                    new[] { ("Assets/Bublisher/3D Stylized Animated Dogs Kit/Prefabs/corgi.prefab", .42f), ("Assets/Bublisher/3D Stylized Animated Dogs Kit/Prefabs/pug.prefab", .38f), ("Assets/Bublisher/3D Stylized Animated Dogs Kit/Prefabs/cur.prefab", .5f), ("Assets/Bublisher/3D Stylized Animated Dogs Kit/Prefabs/germanshepherd.prefab", .68f), ("Assets/Bublisher/3D Stylized Animated Dogs Kit/Prefabs/chihuahua.prefab", .26f) },
                };
                var ground = GameObject.CreatePrimitive(PrimitiveType.Plane); ground.transform.SetParent(temp.transform); ground.transform.localScale = new Vector3(8, 1, 8); ground.GetComponent<Renderer>().sharedMaterial = Flat(new Color(.35f, .45f, .25f));
                for (int r = 0; r < rows.Length; r++)
                    for (int i = 0; i < rows[r].Length; i++)
                    {
                        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(rows[r][i].path);
                        if (prefab == null) { Debug.LogWarning("Lineup missing " + rows[r][i].path); continue; }
                        var go = (GameObject)UnityEngine.Object.Instantiate(prefab, temp.transform);
                        foreach (var col in go.GetComponentsInChildren<Collider>(true)) UnityEngine.Object.DestroyImmediate(col);
                        var rs = go.GetComponentsInChildren<Renderer>(); if (rs.Length == 0) continue;
                        Bounds bb = rs[0].bounds; foreach (var rr in rs) bb.Encapsulate(rr.bounds);
                        float target = rows[r][i].path.Contains("Butterfly") ? Mathf.Max(bb.size.x, bb.size.z) : bb.size.y;
                        go.transform.localScale *= rows[r][i].height / Mathf.Max(target, .001f);
                        rs = go.GetComponentsInChildren<Renderer>(); bb = rs[0].bounds; foreach (var rr in rs) bb.Encapsulate(rr.bounds);
                        go.transform.position = new Vector3(-6f + i * 3f, 0, r * 4f) + (go.transform.position - new Vector3(bb.center.x, bb.min.y, bb.center.z));
                    }
                // Legacy-converted Waldemarst shrubs and ferns for comparison (third row).
                if (legacy != null)
                {
                    int i = 0;
                    foreach (var t in legacy.GetComponentsInChildren<Transform>(true))
                    {
                        if (t.name != "Layered boxwood" && t.name != "Fern understory") continue;
                        var go = (GameObject)UnityEngine.Object.Instantiate(t.gameObject, temp.transform);
                        go.transform.position = new Vector3(-13.5f + i * 3f, 0, 14f); if (++i >= 4) break;
                    }
                }
                var camGo = new GameObject("LINEUP_CAM"); camGo.transform.SetParent(temp.transform);
                var cam = camGo.AddComponent<Camera>(); cam.fieldOfView = 55; cam.clearFlags = CameraClearFlags.Skybox;
                cam.GetUniversalAdditionalCameraData().renderPostProcessing = false;
                for (int pass = 0; pass < 3; pass++)
                {
                    camGo.transform.position = new Vector3(0, 4.2f, -7f); camGo.transform.rotation = Quaternion.Euler(18f, 0f, 0f);
                }
                var rt = new RenderTexture(Width, Height, 24); cam.targetTexture = rt; cam.Render(); cam.Render();
                RenderTexture.active = rt; var tex = new Texture2D(Width, Height, TextureFormat.RGB24, false); tex.ReadPixels(new Rect(0, 0, Width, Height), 0, 0); tex.Apply();
                File.WriteAllBytes(Path.Combine(OutDir, "lineup.png"), tex.EncodeToPNG());
                File.WriteAllText(Path.Combine(OutDir, "capture-result.txt"), "SUCCESS");
                EditorApplication.Exit(0);
            }
            catch (Exception e) { Debug.LogError("[CatMe][Lineup] " + e); EditorApplication.Exit(1); }
        }

        /// <summary>Renders the Home Kit house exterior and interior from a few angles, to judge it for the in-game house room.</summary>
        public static void HomeKitLook()
        {
            try
            {
                ShaderUtil.allowAsyncCompilation = false;
                Directory.CreateDirectory(OutDir);
                PlaceSceneBuilder.ConvertImportedMaterialsMenu();
                var hgPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/VRChat/HomeKit/Prefabs/Home Geometry.prefab");
                EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
                var temp = new GameObject("HK");
                ApplyLighting(temp.transform, PlaceLightingPreset.Get("HouseInterior"));
                var home = (GameObject)UnityEngine.Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/VRChat/HomeKit/Prefabs/Home Geometry.prefab"), temp.transform);
                PlaceSceneBuilder.ApplyHomeKitMaterials(home);
                foreach (var l in home.GetComponentsInChildren<Light>(true)) l.enabled = true;
                var rs = home.GetComponentsInChildren<Renderer>(); Bounds b = rs[0].bounds; foreach (var r in rs) b.Encapsulate(r.bounds);
                File.WriteAllText(Path.Combine(OutDir, "bounds.txt"), "center " + b.center + " size " + b.size + " renderers " + rs.Length);
                foreach (var name in new[] { "couch", "table", "Low_Chair" })
                {
                    var pf = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/VRChat/HomeKit/Prefabs/" + name + ".prefab");
                    if (pf == null) continue;
                    var g = (GameObject)UnityEngine.Object.Instantiate(pf, temp.transform); g.transform.position = b.center + new Vector3(0, -b.size.y * .5f + .02f, 0);
                }
                var sun = new GameObject("fill"); sun.transform.SetParent(temp.transform); var pl = sun.AddComponent<Light>(); pl.type = LightType.Point; pl.range = 30; pl.intensity = 3f; pl.color = new Color(1f, .85f, .6f); sun.transform.position = b.center + Vector3.up * 1.8f;
                foreach (var r in home.GetComponentsInChildren<Renderer>(true)) if (r.name.StartsWith("ceiling")) r.enabled = false;
                File.WriteAllText(Path.Combine(OutDir, "parts.txt"), string.Join("\n", System.Linq.Enumerable.Select(home.GetComponentsInChildren<Renderer>(true), r => r.name + " " + r.bounds.center.ToString("F1") + " " + r.bounds.size.ToString("F1"))));
                var views = new (string name, Vector3 pos, Vector3 look, float fov)[]
                {
                    ("hk-1-exterior", b.center + new Vector3(0, b.size.y * .3f, -b.size.z * 1.4f), b.center, 60f),
                    ("hk-2-interior-a", b.center + new Vector3(-b.size.x * .3f, -b.size.y * .35f, -b.size.z * .3f), b.center + new Vector3(b.size.x * .2f, -b.size.y * .35f, b.size.z * .2f), 75f),
                    ("hk-3-interior-b", b.center + new Vector3(b.size.x * .3f, -b.size.y * .35f, b.size.z * .3f), b.center + new Vector3(-b.size.x * .2f, -b.size.y * .35f, -b.size.z * .2f), 75f),
                    ("hk-4-top", b.center + new Vector3(0, b.size.y * 2.2f, -b.size.z * .6f), b.center, 55f),
                };
                foreach (var v in views)
                {
                    var camGo = new GameObject("cam"); camGo.transform.SetParent(temp.transform);
                    var cam = camGo.AddComponent<Camera>(); cam.fieldOfView = v.fov; cam.nearClipPlane = .05f; cam.farClipPlane = 300f; cam.clearFlags = CameraClearFlags.SolidColor; cam.backgroundColor = new Color(.35f, .45f, .6f);
                    camGo.transform.position = v.pos; camGo.transform.rotation = Quaternion.LookRotation(v.look - v.pos);
                    var rt = new RenderTexture(Width, Height, 24); cam.targetTexture = rt; cam.Render(); cam.Render();
                    RenderTexture.active = rt; var tex = new Texture2D(Width, Height, TextureFormat.RGB24, false); tex.ReadPixels(new Rect(0, 0, Width, Height), 0, 0); tex.Apply();
                    File.WriteAllBytes(Path.Combine(OutDir, v.name + ".png"), tex.EncodeToPNG());
                    RenderTexture.active = null; cam.targetTexture = null; UnityEngine.Object.DestroyImmediate(rt); UnityEngine.Object.DestroyImmediate(tex); UnityEngine.Object.DestroyImmediate(camGo);
                }
                File.WriteAllText(Path.Combine(OutDir, "capture-result.txt"), "SUCCESS");
                EditorApplication.Exit(0);
            }
            catch (Exception e) { Debug.LogError("[CatMe][HomeKit] " + e); EditorApplication.Exit(1); }
        }

        static void Render(Shot shot)
        {
            var root = GameObject.FindFirstObjectByType<PlaceRoot>();
            if (root == null) throw new InvalidOperationException("No PlaceRoot in " + shot.place);
            var entry = root.FindEntry(shot.entry) ?? throw new InvalidOperationException("No entry " + shot.entry);
            var temp = new GameObject("CAPTURE_TEMP");
            var preset = PlaceLightingPreset.Get(shot.lighting ?? root.lightingPreset);
            ApplyLighting(temp.transform, preset);
            RenderSettings.reflectionIntensity = shot.reflection;
            if (shot.sunOff) { var sunObj = GameObject.Find("CAPTURE_SUN"); if (sunObj != null) sunObj.GetComponent<Light>().enabled = false; }
            if (shot.lighting == "BlueHour") foreach (var t in root.GetComponentsInChildren<Transform>(true)) if (t.name.StartsWith("Night_")) t.gameObject.SetActive(true);

            if (shot.hasAbs)
            {
                var roomPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/CatMe/Resources/Rooms/HouseRoom_r3.prefab");
                if (roomPrefab != null) UnityEngine.Object.Instantiate(roomPrefab, temp.transform);
                var absCam = new GameObject("CAPTURE_CAM"); absCam.transform.SetParent(temp.transform);
                var ac = absCam.AddComponent<Camera>(); ac.fieldOfView = shot.fov; ac.nearClipPlane = .05f; ac.farClipPlane = 100f; ac.clearFlags = CameraClearFlags.SolidColor; ac.backgroundColor = Color.black;
                ac.GetUniversalAdditionalCameraData().renderPostProcessing = true;
                absCam.transform.position = shot.absPos; absCam.transform.rotation = Quaternion.Euler(shot.pitch, shot.absYaw, 0f);
                var art = new RenderTexture(Width, Height, 24); ac.targetTexture = art; ac.Render(); ac.Render();
                RenderTexture.active = art; var atex = new Texture2D(Width, Height, TextureFormat.RGB24, false); atex.ReadPixels(new Rect(0, 0, Width, Height), 0, 0); atex.Apply();
                File.WriteAllBytes(Path.Combine(OutDir, shot.name + ".png"), atex.EncodeToPNG());
                RenderTexture.active = null; ac.targetTexture = null; UnityEngine.Object.DestroyImmediate(art); UnityEngine.Object.DestroyImmediate(atex); UnityEngine.Object.DestroyImmediate(temp);
                return;
            }
            if (shot.showWildlife)
                foreach (var t in root.GetComponentsInChildren<Transform>(true))
                    if (t.name.StartsWith("Deer_") || t.name.StartsWith("Horse_") || t.name.StartsWith("Chicken_")) t.gameObject.SetActive(true);
            Vector3 forward = entry.forward; forward.y = 0; forward.Normalize();
            Vector3 viewForward = Quaternion.Euler(0, shot.yawOffset, 0) * forward; // only the camera turns; the owner stays on the route
            Vector3 ownerPos = entry.position + forward * shot.along;
            ownerPos.y = GroundY(ownerPos);
            var owner = GameObject.CreatePrimitive(PrimitiveType.Capsule); owner.name = "owner"; owner.transform.SetParent(temp.transform);
            owner.transform.localScale = new Vector3(.45f, .85f, .3f); owner.transform.position = ownerPos + Vector3.up * .85f;
            owner.GetComponent<Renderer>().sharedMaterial = Flat(new Color(.16f, .17f, .19f)); UnityEngine.Object.DestroyImmediate(owner.GetComponent<Collider>());
            Vector3 catPos = ownerPos + Quaternion.LookRotation(forward) * new Vector3(-.8f, 0, -.9f); catPos.y = GroundY(catPos);
            var cat = GameObject.CreatePrimitive(PrimitiveType.Capsule); cat.name = "cat"; cat.transform.SetParent(temp.transform);
            cat.transform.localScale = new Vector3(.2f, .11f, .3f); cat.transform.position = catPos + Vector3.up * .23f; cat.transform.rotation = Quaternion.LookRotation(forward) * Quaternion.Euler(90, 0, 0);
            cat.GetComponent<Renderer>().sharedMaterial = Flat(new Color(.55f, .5f, .45f)); UnityEngine.Object.DestroyImmediate(cat.GetComponent<Collider>());

            if (shot.showDog)
                foreach (var t in root.GetComponentsInChildren<Transform>(true))
                    if (t.name == "Dog_corgi") { t.gameObject.SetActive(true); Vector3 dp = ownerPos + Quaternion.LookRotation(forward) * new Vector3(1.4f, 0, 3.2f); dp.y = GroundY(dp); t.position = dp; t.rotation = Quaternion.LookRotation(-forward); }
            Vector3 focus = ownerPos + Vector3.up * 1.05f;
            Vector3 right = Vector3.Cross(Vector3.up, viewForward);
            Vector3 camPos = focus - viewForward * shot.dist; camPos.y = ownerPos.y + shot.height; camPos -= right * shot.offsetX * shot.dist * .55f;
            Vector3 aim = focus - right * shot.offsetX * shot.dist * .55f;
            var camGo = new GameObject("CAPTURE_CAM"); camGo.transform.SetParent(temp.transform);
            var cam = camGo.AddComponent<Camera>(); cam.fieldOfView = shot.fov; cam.nearClipPlane = .1f; cam.farClipPlane = 400f;
            cam.clearFlags = CameraClearFlags.Skybox; cam.allowHDR = true;
            cam.GetUniversalAdditionalCameraData().renderPostProcessing = true;
            camGo.transform.position = camPos; camGo.transform.rotation = Quaternion.LookRotation(aim - camPos);

            var rt = new RenderTexture(Width, Height, 24, RenderTextureFormat.ARGB32); rt.antiAliasing = 4;
            cam.targetTexture = rt; cam.Render(); cam.Render(); // first pass warms up shader variants
            RenderTexture.active = rt;
            var tex = new Texture2D(Width, Height, TextureFormat.RGB24, false);
            tex.ReadPixels(new Rect(0, 0, Width, Height), 0, 0); tex.Apply();
            File.WriteAllBytes(Path.Combine(OutDir, shot.name + ".png"), tex.EncodeToPNG());
            RenderTexture.active = null; cam.targetTexture = null;
            UnityEngine.Object.DestroyImmediate(rt); UnityEngine.Object.DestroyImmediate(tex); UnityEngine.Object.DestroyImmediate(temp);
            Debug.Log("[CatMe][Capture] " + shot.name);
        }

        static float GroundY(Vector3 p)
        {
            int layer = LayerMask.NameToLayer("Walkable");
            int mask = layer >= 0 ? 1 << layer : ~0;
            return Physics.Raycast(new Vector3(p.x, p.y + 50f, p.z), Vector3.down, out RaycastHit hit, 200f, mask) ? hit.point.y : p.y;
        }

        static Material Flat(Color c)
        {
            var m = new Material(Shader.Find("Universal Render Pipeline/Lit")); m.SetColor("_BaseColor", c); m.SetFloat("_Smoothness", .2f); return m;
        }

        // Mirrors PlaceLighting.Set for a single preset so renders match what the game applies.
        static void ApplyLighting(Transform parent, PlaceLightingPreset a)
        {
            var sunGo = new GameObject("CAPTURE_SUN"); sunGo.transform.SetParent(parent);
            var sun = sunGo.AddComponent<Light>(); sun.type = LightType.Directional; sun.shadows = LightShadows.Soft; sun.shadowStrength = .8f;
            sun.transform.rotation = Quaternion.Euler(a.sunPitch, a.sunYaw, 0); sun.color = a.sunColor; sun.intensity = a.sunIntensity; sun.enabled = a.sunIntensity > .01f;
            var fillGo = new GameObject("CAPTURE_FILL"); fillGo.transform.SetParent(parent);
            var fill = fillGo.AddComponent<Light>(); fill.type = LightType.Directional; fill.color = new Color(.68f, .76f, 1f); fill.intensity = a.fillIntensity; fill.shadows = LightShadows.None; fill.transform.rotation = Quaternion.Euler(30, 145, 0);
            RenderSettings.sun = sun;
            RenderSettings.ambientMode = AmbientMode.Trilight;
            RenderSettings.ambientSkyColor = a.ambientSky; RenderSettings.ambientEquatorColor = a.ambientEquator; RenderSettings.ambientGroundColor = a.ambientGround;
            RenderSettings.ambientIntensity = a.ambientIntensity;
            RenderSettings.fog = true; RenderSettings.fogMode = FogMode.ExponentialSquared; RenderSettings.fogColor = a.fogColor; RenderSettings.fogDensity = a.fogDensity;
            var skySource = Resources.Load<Material>(a.skyResource) ?? Resources.Load<Material>("Skyboxes/Sky_Sunset");
            if (skySource != null)
            {
                var sky = new Material(skySource);
                if (sky.HasProperty("_Exposure")) sky.SetFloat("_Exposure", a.skyExposure);
                if (sky.HasProperty("_Tint")) sky.SetColor("_Tint", a.skyTint);
                if (sky.HasProperty("_SkyTint")) sky.SetColor("_SkyTint", a.skyTint);
                RenderSettings.skybox = sky;
            }
            DynamicGI.UpdateEnvironment();
        }
    }
}
#endif
