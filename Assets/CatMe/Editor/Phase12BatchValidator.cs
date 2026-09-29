using CatMe.Cat;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace CatMe.Editor
{
    /// <summary>Two-cycle validation for the physical packet-to-bowl feeding loop.</summary>
    [InitializeOnLoad]
    public static class Phase12BatchValidator
    {
        private const string ScenePath = "Assets/CatMe/Scenes/HomeRoom.unity";
        private const string StageKey = "CatMe.Phase12BatchValidator.Stage";
        private const string StartedKey = "CatMe.Phase12BatchValidator.Started";
        private const string FailureKey = "CatMe.Phase12BatchValidator.Failure";
        private enum Stage { None, Start, Wait, CycleOne, CycleTwo, Finish, Failed }

        static Phase12BatchValidator() { EditorApplication.update -= Tick; EditorApplication.update += Tick; }

        [MenuItem("CatMe/Validate Phase 12 Feeding (Batch)")]
        public static void Run()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode) { Debug.LogError("[CatMe][Phase12Batch] Stop Play Mode first."); return; }
            SessionState.SetString(FailureKey, string.Empty);
            SetStage(Stage.Start);
        }

        private static void Tick()
        {
            switch (GetStage())
            {
                case Stage.Start: StartScene(); break;
                case Stage.Wait: WaitForReady(); break;
                case Stage.CycleOne: RunCycle(1); break;
                case Stage.CycleTwo: RunCycle(2); break;
                case Stage.Finish: Finish(0, "PASS"); break;
                case Stage.Failed: Finish(1, "FAIL: " + SessionState.GetString(FailureKey, "unknown")); break;
            }
        }

        private static void StartScene()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode) return;
            EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            SetStage(Stage.Wait);
            EditorApplication.isPlaying = true;
        }

        private static void WaitForReady()
        {
            HomeRoomCatIntegration integration = Object.FindAnyObjectByType<HomeRoomCatIntegration>();
            CatFeedingInteraction feeding = Object.FindAnyObjectByType<CatFeedingInteraction>();
            if (integration == null || !integration.IsStationaryAcceptanceComplete || feeding == null)
            {
                Timeout(90, "components did not become ready");
                return;
            }

            if (!integration.DidStationaryAcceptancePass || feeding.PacketInstanceCount != 1 || feeding.FoodVisualInstanceCount != 1 || feeding.PacketColliderInstanceCount != 1)
            {
                Fail("stationary acceptance or reusable feeding props failed");
                return;
            }

            if (!feeding.RejectDropForValidation() || feeding.RejectedDropCount != 1 || feeding.IsActivityLockActive)
            {
                Fail("invalid drop did not cancel cleanly");
                return;
            }

            SetStage(Stage.CycleOne);
        }

        private static void RunCycle(int cycle)
        {
            CatFeedingInteraction feeding = Object.FindAnyObjectByType<CatFeedingInteraction>();
            if (feeding == null) { Fail("feeding component was destroyed"); return; }
            if (feeding.CurrentState == CatFeedingInteraction.FeedingState.Idle && feeding.CompletedCycleCount == cycle - 1)
            {
                if (!feeding.BeginValidDropForValidation()) { Fail("valid feeding drop was rejected"); return; }
            }

            if (feeding.CompletedCycleCount >= cycle && feeding.CurrentState == CatFeedingInteraction.FeedingState.Idle)
            {
                if (feeding.PacketInstanceCount != 1 || feeding.FoodVisualInstanceCount != 1 || feeding.PacketColliderInstanceCount != 1 || feeding.IsActivityLockActive || feeding.ArrivalError > 0.10f || feeding.MaximumProtectedLocalDrift > 0.02f || feeding.NeutralRotationRestoreError > 0.01f || feeding.NeutralScaleRestoreError > 0.0001f || feeding.MaximumCameraDelta > 0.01f)
                {
                    Fail("feeding cycle did not restore its protected state");
                    return;
                }
                SetStage(cycle == 1 ? Stage.CycleTwo : Stage.Finish);
            }
            else Timeout(60, "feeding cycle did not complete");
        }

        private static void Timeout(double seconds, string message)
        {
            if (EditorApplication.timeSinceStartup - SessionState.GetFloat(StartedKey, 0f) >= seconds) Fail(message);
        }

        private static void Fail(string message)
        {
            SessionState.SetString(FailureKey, message);
            if (EditorApplication.isPlayingOrWillChangePlaymode) EditorApplication.isPlaying = false;
            SetStage(Stage.Failed);
        }

        private static void Finish(int code, string result)
        {
            Debug.Log("[CatMe][Phase12Batch] " + result);
            SessionState.SetString(StageKey, string.Empty);
            EditorApplication.Exit(code);
        }

        private static void SetStage(Stage stage)
        {
            SessionState.SetInt(StageKey, (int)stage);
            SessionState.SetFloat(StartedKey, (float)EditorApplication.timeSinceStartup);
        }

        private static Stage GetStage() => (Stage)SessionState.GetInt(StageKey, 0);
    }
}
