#if UNITY_EDITOR
using CatMe.Garden;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using System;

namespace CatMe.Editor
{
    [InitializeOnLoad]
    public static class GardenCompanionSmokeCheck
    {
        private const string ScenePath = "Assets/CatMe/Scenes/GardenCompanion.unity";
        private const string ActiveKey = "CatMe.GardenSmoke.Active";
        private const string StageKey = "CatMe.GardenSmoke.Stage";
        private const string TimeKey = "CatMe.GardenSmoke.Time";

        static GardenCompanionSmokeCheck()
        {
            EditorApplication.update += Tick;
            EditorApplication.playModeStateChanged += OnPlayModeChanged;
        }

        [MenuItem("CatMe/Garden/Run Garden Smoke Check")]
        public static void Run()
        {
            EditorSceneManager.OpenScene(ScenePath);
            EditorPrefs.SetBool(ActiveKey, true);
            EditorPrefs.SetInt(StageKey, 0);
            SetTime(EditorApplication.timeSinceStartup);
            EditorApplication.isPlaying = true;
        }

        private static void OnPlayModeChanged(PlayModeStateChange state)
        {
            if (state == PlayModeStateChange.EnteredPlayMode) SetTime(EditorApplication.timeSinceStartup);
        }

        private static void Tick()
        {
            if (!EditorPrefs.GetBool(ActiveKey, false) || !EditorApplication.isPlaying) return;
            double now = EditorApplication.timeSinceStartup;
            double started = GetTime();
            GardenCompanionDemo demo = UnityEngine.Object.FindAnyObjectByType<GardenCompanionDemo>();
            if (demo == null || !demo.IsReady)
            {
                if (now - started > 90) Finish(false, "Timed out waiting for the three GLB characters and animation clips.");
                return;
            }

            int stage = EditorPrefs.GetInt(StageKey, 0);
            if (stage == 0)
            {
                EditorPrefs.SetInt(StageKey, 1); SetTime(now);
                demo.SetMove(Vector2.right);
                return;
            }
            if (stage == 1 && now - started > 1.2)
            {
                demo.SetMove(Vector2.zero);
                if (demo.HumanPosition.x < 0.8f) { Finish(false, "Touch movement did not move the human player."); return; }
                demo.ToggleFollow(); EditorPrefs.SetInt(StageKey, 2); SetTime(now);
                return;
            }
            if (stage == 2 && now - started > 2.0)
            {
                if (demo.CatDistanceToHuman >= 3.5f) { Finish(false, "The cat did not close the distance in Follow mode."); return; }
                demo.CallCat(); EditorPrefs.SetInt(StageKey, 3); SetTime(now);
                return;
            }
            if (stage == 3 && now - started > System.Math.Max(3.0, demo.CallSitDuration + 0.4))
            {
                if (!demo.DidCallGesturePlay) { Finish(false, "The human did not play the call gesture after sitting."); return; }
                if (!demo.DidMeow) { Finish(false, "The cat did not reach the caller and trigger the meow."); return; }
                demo.ThrowBall(); EditorPrefs.SetInt(StageKey, 4); SetTime(now);
                return;
            }
            if (stage == 4 && now - started > 2.5)
            {
                if (!demo.DidThrowBall || !demo.DidCatChaseBall) { Finish(false, "The thrown ball did not trigger cat chase behavior."); return; }
                Finish(true, "Human movement, cat follow, call animation, meow path and ball throw ran in Play Mode.");
            }
        }

        private static void Finish(bool passed, string message)
        {
            EditorPrefs.SetBool(ActiveKey, false);
            if (passed) Debug.Log("[CatMe][GardenSmoke] PASS | " + message);
            else Debug.LogError("[CatMe][GardenSmoke] FAIL | " + message);
            EditorApplication.Exit(passed ? 0 : 1);
        }

        private static void SetTime(double value) => EditorPrefs.SetString(TimeKey, value.ToString("R", System.Globalization.CultureInfo.InvariantCulture));
        private static double GetTime() => double.TryParse(EditorPrefs.GetString(TimeKey, "0"), System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out double value) ? value : 0;
    }
}
#endif
