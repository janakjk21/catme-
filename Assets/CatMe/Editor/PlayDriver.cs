#if UNITY_EDITOR
using System;
using System.IO;
using System.Text;
using CatMe.Garden;
using CatMe.Session;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

namespace CatMe.Editor
{
    /// <summary>
    /// Batch-mode Play Mode smoke test: starts GardenCompanion, walks to the house door, checks the prompt appears,
    /// presses it, checks first person, walks inside, and writes what happened (plus a screenshot) to a report.
    /// Run: Unity -batchMode -projectPath ... -executeMethod CatMe.Editor.PlayDriver.Run
    /// </summary>
    public static class PlayDriver
    {
        static readonly StringBuilder report = new StringBuilder();
        static string OutDir => Environment.GetEnvironmentVariable("CATME_PLAY_OUT") ?? "/private/tmp/catme-play";
        static int stage; static double stageAt, startedAt, lastWaitingLog;
        static GardenCompanionDemo garden; static Vector3 ownerBefore, catBefore;

        static void Log(string line) { report.AppendLine(line); Debug.Log("[CatMe][PlayDriver] " + line); }

        public static void Run()
        {
            Directory.CreateDirectory(OutDir);
            ShaderUtil.allowAsyncCompilation = false;
            // Use the checked-in scene in the test clone. Rebuilding here changes the
            // approved layout and can consume several gigabytes before Play Mode starts.
            EditorSceneManager.OpenScene("Assets/CatMe/Scenes/GardenCompanion.unity");
            EditorSettings.enterPlayModeOptionsEnabled = true;
            EditorSettings.enterPlayModeOptions = EnterPlayModeOptions.DisableDomainReload | EnterPlayModeOptions.DisableSceneReload;
            stage = 0; startedAt = EditorApplication.timeSinceStartup; stageAt = startedAt; lastWaitingLog = startedAt;
            Log("Starting room check from the saved GardenCompanion scene.");
            EditorApplication.update += Tick;
            EditorApplication.isPlaying = true;
        }

        static void Finish(int code)
        {
            File.WriteAllText(Path.Combine(OutDir, "report.txt"), report.ToString());
            EditorApplication.update -= Tick;
            EditorApplication.Exit(code);
        }

        static bool Wait(double seconds) => EditorApplication.timeSinceStartup - stageAt >= seconds;
        static void Next() { stage++; stageAt = EditorApplication.timeSinceStartup; }

        static Button FindButton(string name)
        {
            foreach (var b in Resources.FindObjectsOfTypeAll<Button>()) if (b.gameObject.scene.IsValid() && b.name == name) return b;
            return null;
        }

        static void Shot(string name)
        {
            var cam = Camera.main; if (cam == null) return;
            var rt = new RenderTexture(1280, 576, 24); var old = cam.targetTexture; cam.targetTexture = rt; cam.Render();
            RenderTexture.active = rt; var tex = new Texture2D(1280, 576, TextureFormat.RGB24, false); tex.ReadPixels(new Rect(0, 0, 1280, 576), 0, 0); tex.Apply();
            File.WriteAllBytes(Path.Combine(OutDir, name + ".png"), tex.EncodeToPNG());
            RenderTexture.active = null; cam.targetTexture = old; UnityEngine.Object.DestroyImmediate(rt); UnityEngine.Object.DestroyImmediate(tex);
        }

        static void Tick()
        {
            try
            {
                if (EditorApplication.timeSinceStartup - startedAt > 480) { Log("TIMEOUT at stage " + stage); Finish(2); return; }
                if (!EditorApplication.isPlaying) return;
                switch (stage)
                {
                    case 0:
                        if (EditorApplication.timeSinceStartup - lastWaitingLog > 10)
                        {
                            lastWaitingLog = EditorApplication.timeSinceStartup;
                            Log("Waiting for HomeGarden: place=" + (SessionDirector.Instance != null ? SessionDirector.Instance.CurrentPlaceId : "no director")
                                + ", busy=" + (SessionDirector.Instance != null && SessionDirector.Instance.IsBusy)
                                + ", owner=" + (garden != null && garden.OwnerTransform != null)
                                + ", cat=" + (garden != null && garden.CatTransform != null)
                                + ", timeScale=" + Time.timeScale + ", frame=" + Time.frameCount);
                        }
                        if (SessionDirector.Instance != null && SessionDirector.Instance.CurrentPlaceId == "HomeGarden" && !SessionDirector.Instance.IsBusy)
                        {
                            garden = UnityEngine.Object.FindAnyObjectByType<GardenCompanionDemo>();
                            if (garden != null && garden.OwnerTransform != null && garden.CatTransform != null && Wait(3))
                            { Log("Place loaded. Owner at " + garden.OwnerTransform.position + ", cat at " + garden.CatTransform.position + ", input blocked: " + garden.IsInputBlocked); Next(); }
                        }
                        break;
                    case 1:
                        {
                            var points = SessionDirector.Instance.CurrentInteractions();
                            Log("Interaction points in scene: " + points.Length);
                            foreach (var p in points) Log("  - " + p.kind + " '" + p.label + "' at " + p.transform.position);
                            InteractionPoint house = null; foreach (var p in points) if (p.kind == InteractionKind.EnterHouse) house = p;
                            if (house == null) { Log("FAIL: no EnterHouse point"); Finish(1); return; }
                            garden.SetInputBlocked(false);
                            garden.PlaceOwnerOnly(house.transform.position + Vector3.forward * 1.5f, 180f);
                            Next(); break;
                        }
                    case 2:
                        if (Wait(1.0))
                        {
                            var btn = FindButton("Interaction button");
                            Log("Interaction button exists: " + (btn != null) + ", active: " + (btn != null && btn.gameObject.activeInHierarchy) + ", label: " + (btn != null ? btn.GetComponentInChildren<Text>(true).text : "-"));
                            Shot("1-at-house-door");
                            if (btn == null || !btn.gameObject.activeInHierarchy) { Log("FAIL: house prompt not visible"); Finish(1); return; }
                            btn.onClick.Invoke(); Next();
                        }
                        break;
                    case 3:
                        if (Wait(4.0))
                        {
                            Log("After pressing: owner at " + garden.OwnerTransform.position + ", first person: " + garden.CameraRig.IsFirstPerson + ", input blocked: " + garden.IsInputBlocked + ", camera at " + Camera.main.transform.position);
                            ownerBefore = garden.OwnerTransform.position; catBefore = garden.CatTransform.position;
                            Shot("2-inside-house");
                            garden.SetMove(new Vector2(0f, 1f)); Next();
                        }
                        break;
                    case 4:
                        if (Wait(1.6))
                        {
                            garden.SetMove(Vector2.zero);
                            Log("Walked forward 1.6 s: owner moved " + Vector3.Distance(ownerBefore, garden.OwnerTransform.position).ToString("F2") + " m, cat moved " + Vector3.Distance(catBefore, garden.CatTransform.position).ToString("F2") + " m, camera at " + Camera.main.transform.position);
                            Shot("3-after-walk");
                            Log(Vector3.Distance(ownerBefore, garden.OwnerTransform.position) > .3f ? "PASS: owner walks inside the house" : "FAIL: owner did not move inside the house");
                            Finish(0);
                        }
                        break;
                }
            }
            catch (Exception e) { Log("EXCEPTION: " + e); Finish(3); }
        }
    }
}
#endif
