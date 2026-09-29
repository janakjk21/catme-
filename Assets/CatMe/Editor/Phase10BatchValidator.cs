using CatMe.Cat;
using CatMe.Save;
using System.IO;
using CatMe.Toys;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace CatMe.Editor
{
    /// <summary>Focused editor smoke check for the combined energy and rest loop.</summary>
    [InitializeOnLoad]
    public static class Phase10BatchValidator
    {
        private const string ScenePath = "Assets/CatMe/Scenes/HomeRoom.unity";
        private const string StageKey = "CatMe.Phase10BatchValidator.Stage";
        private const string StartedKey = "CatMe.Phase10BatchValidator.Started";
        private const string FailureKey = "CatMe.Phase10BatchValidator.Failure";
        private enum Stage { None, Start, Wait, Play, Rest, Finish, Failed }

        static Phase10BatchValidator() { EditorApplication.update -= Tick; EditorApplication.update += Tick; }

        [MenuItem("CatMe/Validate Phase 10 Energy Rest (Batch)")]
        public static void Run()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode) { Debug.LogError("[CatMe][Phase10Batch] Stop Play Mode first."); return; }
            CatLocalSave.ValidationPathOverride = Path.Combine(Application.temporaryCachePath, "CatMePhase10-11-save-test.json");
            SessionState.SetString(FailureKey, string.Empty); SetStage(Stage.Start); 
        }

        private static void Tick()
        {
            switch (GetStage())
            {
                case Stage.Start: StartScene(); break;
                case Stage.Wait: WaitForReady(); break;
                case Stage.Play: RunPlay(); break;
                case Stage.Rest: RunRest(); break;
                case Stage.Finish: Finish(0, "PASS"); break;
                case Stage.Failed: Finish(1, "FAIL: " + SessionState.GetString(FailureKey, "unknown")); break;
            }
        }

        private static void StartScene()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode) return;
            EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single); SetStage(Stage.Wait); EditorApplication.isPlaying = true;
        }

        private static void WaitForReady()
        {
            HomeRoomCatIntegration integration = Object.FindAnyObjectByType<HomeRoomCatIntegration>();
            CatEnergy energy = Object.FindAnyObjectByType<CatEnergy>();
            LaserToyInteraction laser = Object.FindAnyObjectByType<LaserToyInteraction>();
            if (integration == null || !integration.IsStationaryAcceptanceComplete || energy == null || laser == null) { Timeout(90, "components did not become ready"); return; }
            if (!integration.DidStationaryAcceptancePass || !laser.BeginSessionForValidation(true)) { Fail("stationary acceptance or energy laser setup failed"); return; }
            energy.SetEnergyForValidation(40f); SetStage(Stage.Play);
        }

        private static void RunPlay()
        {
            LaserToyInteraction laser = Object.FindAnyObjectByType<LaserToyInteraction>();
            CatEnergy energy = Object.FindAnyObjectByType<CatEnergy>();
            if (laser == null || energy == null) { Fail("energy or laser was destroyed"); return; }
            if (laser.CurrentState == LaserToyInteraction.LaserState.Aiming && laser.AcceptedTargetCount < 3)
            {
                Vector3[] points = { new Vector3(-0.9f, 0f, -0.55f), new Vector3(0f, 0f, 0.55f), new Vector3(0.9f, 0f, -0.55f) };
                laser.PlaceTargetForValidation(points[Mathf.Min(laser.AcceptedTargetCount, points.Length - 1)]);
            }
            if (energy.CurrentEnergy <= CatEnergy.LowEnergyThreshold && laser.CurrentState == LaserToyInteraction.LaserState.Idle) SetStage(Stage.Rest);
            else Timeout(60, "low energy session did not finish");
        }

        private static void RunRest()
        {
            CatEnergy energy = Object.FindAnyObjectByType<CatEnergy>();
            CatSleepInteraction sleep = Object.FindAnyObjectByType<CatSleepInteraction>();
            if (energy == null || sleep == null) { Fail("rest components missing"); return; }
            if (sleep.CurrentState == CatSleepInteraction.SleepState.AwakeIdle && energy.CurrentEnergy >= CatEnergy.AutomaticWakeThreshold)
            {
                bool pass = energy.ProcessedCatchCount > 0 && energy.AutomaticSleepRequestCount == 1 && energy.AutomaticWakeRequestCount == 1 && energy.EnergyMeterInstanceCount == 1;
                if (!pass) { Fail("energy/rest counters failed"); return; }
                EditorApplication.isPlaying = false; SetStage(Stage.Finish); return;
            }
            Timeout(30, "rest did not recover and wake");
        }

        private static void Timeout(double seconds, string message) { if (EditorApplication.timeSinceStartup - SessionState.GetFloat(StartedKey, 0f) >= seconds) Fail(message); }
        private static void Fail(string message) { SessionState.SetString(FailureKey, message); if (EditorApplication.isPlayingOrWillChangePlaymode) EditorApplication.isPlaying = false; SetStage(Stage.Failed); }
        private static void Finish(int code, string result) { CatLocalSave.ValidationPathOverride = null; Debug.Log("[CatMe][Phase10Batch] " + result); SessionState.SetString(StageKey, string.Empty); EditorApplication.Exit(code); }
        private static void SetStage(Stage stage) { SessionState.SetInt(StageKey, (int)stage); SessionState.SetFloat(StartedKey, (float)EditorApplication.timeSinceStartup); }
        private static Stage GetStage() => (Stage)SessionState.GetInt(StageKey, 0);
    }
}
