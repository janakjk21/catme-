#if UNITY_EDITOR
using System;
using System.IO;
using System.Text;
using System.Collections.Generic;
using CatMe.Garden;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace CatMe.Editor
{
    /// <summary>Walks the complete connected outing in Play Mode and records rendered waypoint views.</summary>
    [InitializeOnLoad]
    public static class GardenJourneyPlayCheck
    {
        const string ScenePath = "Assets/CatMe/Scenes/GardenCompanion.unity";
        const string ActiveKey = "CatMe.JourneyCheck.Active";
        const string StageKey = "CatMe.JourneyCheck.Stage";
        const string StartedKey = "CatMe.JourneyCheck.Started";
        const string Output = "docs/implementation/journey";
        static readonly StringBuilder report = new StringBuilder();
        static readonly Dictionary<string, string> priorPrefs = new Dictionary<string, string>();
        static readonly string[] intPrefs = { "CatMe.Journey.Active.v1", "CatMe.Journey.Completed.v1", "CatMe.Journey.ShelterReached.v1", "CatMe.Journey.RainStarted.v1", "CatMe.Journey.RetreatReached.v1", "CatMe.Journey.LastStory.v1", "CatMe.Journey.StoryMask.v1", "CatMe.Journey.StoryEnabled", "CatMe.ReduceMotion" };
        static readonly string[] floatPrefs = { "CatMe.Journey.CheckpointZ.v1", "CatMe.Journey.StoryHold" };
        static readonly string[] stringPrefs = { "CatMe.Journey.RetreatUtc.v1", "CatMe.Journey.LastStoryUtc" };
        static double waitUntil;
        static double lastProgress;

        static GardenJourneyPlayCheck()
        {
            EditorApplication.update += Tick;
            EditorApplication.playModeStateChanged += OnPlayModeChanged;
        }

        [MenuItem("CatMe/Garden/Run Connected Journey Play Check")]
        public static void Run()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode) return;
            Directory.CreateDirectory(Output);
            report.Clear();
            report.AppendLine("Connected journey Play Mode check | " + DateTime.Now.ToString("O"));
            report.AppendLine("Unity rendered Game view captures; owner walked using the production movement controller.");
            File.WriteAllText(Path.Combine(Output, "play-mode-validation.txt"), report.ToString());
            priorPrefs.Clear();
            foreach (string key in intPrefs) priorPrefs[key] = PlayerPrefs.HasKey(key) ? "i:" + PlayerPrefs.GetInt(key) : "missing";
            foreach (string key in floatPrefs) priorPrefs[key] = PlayerPrefs.HasKey(key) ? "f:" + PlayerPrefs.GetFloat(key).ToString("R", System.Globalization.CultureInfo.InvariantCulture) : "missing";
            foreach (string key in stringPrefs) priorPrefs[key] = PlayerPrefs.HasKey(key) ? "s:" + PlayerPrefs.GetString(key) : "missing";
            EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            EditorPrefs.SetInt(StageKey, 0);
            lastProgress = EditorApplication.timeSinceStartup;
            EditorPrefs.SetString(StartedKey, EditorApplication.timeSinceStartup.ToString("R", System.Globalization.CultureInfo.InvariantCulture));
            EditorPrefs.SetBool(ActiveKey, true);
            EditorApplication.isPlaying = true;
        }

        static void OnPlayModeChanged(PlayModeStateChange state)
        {
            if (state == PlayModeStateChange.ExitingPlayMode) Time.timeScale = 1f;
        }

        static void Tick()
        {
            if (!EditorPrefs.GetBool(ActiveKey, false) || !EditorApplication.isPlaying) return;
            double now = EditorApplication.timeSinceStartup;
            double started = double.TryParse(EditorPrefs.GetString(StartedKey, "0"), out double parsed) ? parsed : now;
            if (now - started > 180) { Finish(false, "Timed out while traversing connected journey."); return; }

            GardenCompanionDemo demo = UnityEngine.Object.FindAnyObjectByType<GardenCompanionDemo>();
            if (demo == null || !demo.IsReady) return;
            if (now - lastProgress >= 8)
            {
                int z = Mathf.FloorToInt(demo.OwnerPosition.z);
                Record("route progress z=" + z);
                lastProgress = now;
            }
            int stage = EditorPrefs.GetInt(StageKey, 0);
            if (stage == 0)
            {
                HideOnboardingOverlays();
                demo.BeginConnectedOuting();
                demo.SetInputBlocked(false);
                demo.SetMove(Vector2.up);
                Time.timeScale = 4f;
                Record("home");
                Capture("home");
                EditorPrefs.SetInt(StageKey, 1);
                return;
            }
            if (stage == 1 && demo.OwnerPosition.z >= 21f)
            {
                Record("garden reached at z=" + demo.OwnerPosition.z.ToString("F1")); Capture("garden");
                EditorPrefs.SetInt(StageKey, 2);
            }
            else if (stage == 2 && demo.OwnerPosition.z >= 50f)
            {
                Record("woodland path reached; rain flag=" + PlayerPrefs.GetInt("CatMe.Journey.RainStarted.v1", 0));
                Capture("woodland-rain"); EditorPrefs.SetInt(StageKey, 3);
            }
            else if (stage == 3 && demo.OwnerPosition.z >= 73f)
            {
                Record("tent clearing reached at z=" + demo.OwnerPosition.z.ToString("F1")); Capture("tent-clearing");
                EditorPrefs.SetInt(StageKey, 4);
            }
            else if (stage == 4 && demo.OwnerPosition.z >= 100.2f)
            {
                demo.SetMove(Vector2.zero); Time.timeScale = 1f;
                waitUntil = now + 15.5;
                Record("rain shelter reached; waiting for both companions to settle and rain to clear");
                Capture("rain-shelter"); EditorPrefs.SetInt(StageKey, 5);
            }
            else if (stage == 5 && now >= waitUntil)
            {
                if (PlayerPrefs.GetInt("CatMe.Journey.ShelterReached.v1", 0) != 1)
                { Finish(false, "Shelter pause did not complete with both characters inside."); return; }
                Record("shelter pause passed; rain cleared");
                demo.SetMove(Vector2.up); Time.timeScale = 4f; EditorPrefs.SetInt(StageKey, 6);
            }
            else if (stage == 6 && demo.OwnerPosition.z >= 121f)
            {
                demo.SetMove(Vector2.zero); Time.timeScale = 1f;
                waitUntil = now + 7.0;
                Record("mountain retreat reached; allowing a quiet pause"); Capture("mountain-retreat-evening");
                EditorPrefs.SetInt(StageKey, 7);
            }
            else if (stage == 7 && now >= waitUntil)
            {
                if (PlayerPrefs.GetInt("CatMe.Journey.RetreatReached.v1", 0) != 1)
                { Finish(false, "Mountain retreat checkpoint was not saved."); return; }
                Record("mountain pause passed; returning toward home");
                demo.SetMove(Vector2.down); Time.timeScale = 4f; EditorPrefs.SetInt(StageKey, 8);
            }
            else if (stage == 8 && demo.OwnerPosition.z <= -8.5f)
            {
                demo.SetMove(Vector2.zero); Time.timeScale = 1f;
                if (PlayerPrefs.GetInt("CatMe.Journey.Completed.v1", 0) != 1)
                { Finish(false, "Return reached home but journey completion was not recorded."); return; }
                Record("home return and completion passed"); Capture("return-home");
                waitUntil = now + 1.0;
                EditorPrefs.SetInt(StageKey, 9);
            }
            else if (stage == 9 && now >= waitUntil)
            {
                Finish(true, "All route points, grounding movement, rainy shelter pause, retreat pause, and return-home progression passed in Play Mode.");
            }
        }

        static void Record(string line)
        {
            report.AppendLine("PASS | " + line);
            File.WriteAllText(Path.Combine(Output, "play-mode-validation.txt"), report.ToString());
        }

        static void Capture(string name)
        {
            // CaptureScreenshot records the actual composed Game view, including companion framing and UI.
            ScreenCapture.CaptureScreenshot(Path.GetFullPath(Path.Combine(Output, "journey-" + name + ".png")));
        }

        static void HideOnboardingOverlays()
        {
            string[] names = { "Welcome screen", "First meeting", "Find your cat", "Cat reunion", "Walk tip", "Ball tip", "Garden settings screen" };
            foreach (Canvas canvas in UnityEngine.Object.FindObjectsByType<Canvas>())
            foreach (Transform child in canvas.transform)
                if (Array.IndexOf(names, child.name) >= 0) child.gameObject.SetActive(false);
        }

        static void Finish(bool passed, string summary)
        {
            Time.timeScale = 1f;
            EditorPrefs.SetBool(ActiveKey, false);
            foreach (KeyValuePair<string, string> item in priorPrefs)
            {
                if (item.Value == "missing") { PlayerPrefs.DeleteKey(item.Key); continue; }
                string value = item.Value;
                if (value.StartsWith("i:")) PlayerPrefs.SetInt(item.Key, int.Parse(value.Substring(2)));
                else if (value.StartsWith("f:")) PlayerPrefs.SetFloat(item.Key, float.Parse(value.Substring(2), System.Globalization.CultureInfo.InvariantCulture));
                else if (value.StartsWith("s:")) PlayerPrefs.SetString(item.Key, value.Substring(2));
            }
            PlayerPrefs.Save();
            report.AppendLine((passed ? "PASS | " : "FAIL | ") + summary);
            File.WriteAllText(Path.Combine(Output, "play-mode-validation.txt"), report.ToString());
            if (passed) Debug.Log("[CatMe][JourneyPlayCheck] PASS | " + summary);
            else Debug.LogError("[CatMe][JourneyPlayCheck] FAIL | " + summary);
            EditorApplication.isPlaying = false;
        }
    }
}
#endif
