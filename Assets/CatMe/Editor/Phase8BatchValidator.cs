using CatMe.Cat;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace CatMe.Editor
{
    /// <summary>Focused editor validation for two complete manual sleep cycles.</summary>
    [InitializeOnLoad]
    public static class Phase8BatchValidator
    {
        private const string StageKey = "CatMe.Phase8BatchValidator.Stage";
        private const string StartedAtKey = "CatMe.Phase8BatchValidator.StartedAt";
        private const string FailureKey = "CatMe.Phase8BatchValidator.Failure";
        private const string HomeRoomScene = "Assets/CatMe/Scenes/HomeRoom.unity";
        private const double SetupTimeoutSeconds = 90d;
        private const double CycleTimeoutSeconds = 45d;

        private enum Stage { None, StartHomeRoom, WaitForReady, SleepOne, WakeOne, SleepTwo, WakeTwo, Complete, Failed }

        static Phase8BatchValidator()
        {
            EditorApplication.update -= Tick;
            EditorApplication.update += Tick;
        }

        [MenuItem("CatMe/Validate Phase 8 Sleep (Batch)")]
        public static void Run()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
            {
                Debug.LogError("[CatMe][Phase8Batch] Stop Play Mode before starting the batch validator.");
                return;
            }

            SessionState.SetString(FailureKey, string.Empty);
            SetStage(Stage.StartHomeRoom);
        }

        private static void Tick()
        {
            switch (GetStage())
            {
                case Stage.None: return;
                case Stage.StartHomeRoom: StartHomeRoom(); return;
                case Stage.WaitForReady: WaitForReady(); return;
                case Stage.SleepOne: RunSleep(1); return;
                case Stage.WakeOne: RunWake(1); return;
                case Stage.SleepTwo: RunSleep(2); return;
                case Stage.WakeTwo: RunWake(2); return;
                case Stage.Complete: Finish(0, "PASS"); return;
                case Stage.Failed: Finish(1, "FAIL: " + SessionState.GetString(FailureKey, "Unknown failure")); return;
            }
        }

        private static void StartHomeRoom()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode) return;
            EditorSceneManager.OpenScene(HomeRoomScene, OpenSceneMode.Single);
            SetStage(Stage.WaitForReady);
            EditorApplication.isPlaying = true;
        }

        private static void WaitForReady()
        {
            if (!EditorApplication.isPlaying) return;
            HomeRoomCatIntegration integration = Object.FindAnyObjectByType<HomeRoomCatIntegration>();
            CatSleepInteraction sleep = Object.FindAnyObjectByType<CatSleepInteraction>();
            if (integration == null || sleep == null || !integration.IsStationaryAcceptanceComplete)
            {
                FailWhenTimedOut(SetupTimeoutSeconds, "HomeRoom sleep components were not ready in time.");
                return;
            }

            if (!integration.DidStationaryAcceptancePass || integration.CatMotor == null)
            {
                Fail("HomeRoom stationary acceptance failed before sleep validation.");
                return;
            }

            if (!sleep.RequestSleepForValidation() || sleep.SleepRequestCount != 1)
            {
                Fail("The first Sleep request was rejected or duplicated.");
                return;
            }

            if (sleep.RequestSleepForValidation() || sleep.SleepRequestCount != 1)
            {
                Fail("A repeated Sleep request restarted the first transition.");
                return;
            }
            SetStage(Stage.SleepOne);
        }

        private static void RunSleep(int cycle)
        {
            if (!EditorApplication.isPlaying) return;
            CatSleepInteraction sleep = Object.FindAnyObjectByType<CatSleepInteraction>();
            if (sleep == null) { Fail("Sleep interaction was destroyed during entry."); return; }
            if (sleep.CurrentState != CatSleepInteraction.SleepState.SleepingHidden)
            {
                FailWhenTimedOut(CycleTimeoutSeconds, $"Sleep cycle {cycle} did not reach SleepingHidden.");
                return;
            }

            if (sleep.SuccessfulEntryCount != cycle || !sleep.HiddenActivityLockActive || !sleep.IsZzzCueActive)
            {
                Fail($"Sleep cycle {cycle} did not establish one hidden lock and one Zzz cue.");
                return;
            }

            if (cycle == 1)
            {
                if (!sleep.RequestWakeForValidation()) { Fail("The first Wake request was rejected."); return; }
                SetStage(Stage.WakeOne);
            }
            else
            {
                if (!sleep.RequestWakeForValidation()) { Fail("The second Wake request was rejected."); return; }
                SetStage(Stage.WakeTwo);
            }
        }

        private static void RunWake(int cycle)
        {
            if (!EditorApplication.isPlaying) return;
            CatSleepInteraction sleep = Object.FindAnyObjectByType<CatSleepInteraction>();
            if (sleep == null) { Fail("Sleep interaction was destroyed during wake."); return; }
            if (sleep.CurrentState != CatSleepInteraction.SleepState.AwakeIdle)
            {
                FailWhenTimedOut(CycleTimeoutSeconds, $"Wake cycle {cycle} did not reach AwakeIdle.");
                return;
            }

            bool pass = sleep.SuccessfulWakeCount == cycle && !sleep.HiddenActivityLockActive &&
                        !sleep.IsZzzCueActive && sleep.AllCachedRendererStatesRestored &&
                        sleep.WakePositionError <= 0.10f && sleep.MaximumProtectedLocalDrift <= 0.02f;
            if (!pass) { Fail($"Wake cycle {cycle} failed renderer, lock, cue, position, or drift acceptance."); return; }

            if (cycle == 1)
            {
                if (!sleep.RequestSleepForValidation()) { Fail("The second Sleep request was rejected."); return; }
                SetStage(Stage.SleepTwo);
            }
            else
            {
                Debug.Log($"[CatMe][Phase8Batch] Sleep PASS | cycles=2 | entries={sleep.SuccessfulEntryCount} | " +
                          $"wakes={sleep.SuccessfulWakeCount} | requests={sleep.SleepRequestCount} | " +
                          $"wakePositionError={sleep.WakePositionError:0.####}m | " +
                          $"maxProtectedLocalDrift={sleep.MaximumProtectedLocalDrift:0.######}m | zzz=single-active-cue");
                EditorApplication.isPlaying = false;
                SetStage(Stage.Complete);
            }
        }

        private static void FailWhenTimedOut(double timeout, string message)
        {
            if (ElapsedSeconds >= timeout) Fail(message);
        }

        private static void Fail(string message)
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode) EditorApplication.isPlaying = false;
            SessionState.SetString(FailureKey, message);
            SetStage(Stage.Failed);
        }

        private static void Finish(int exitCode, string result)
        {
            Debug.Log("[CatMe][Phase8Batch] " + result);
            SessionState.EraseString(StageKey);
            SessionState.EraseFloat(StartedAtKey);
            SessionState.EraseString(FailureKey);
            EditorApplication.Exit(exitCode);
        }

        private static void SetStage(Stage stage)
        {
            SessionState.SetInt(StageKey, (int)stage);
            SessionState.SetFloat(StartedAtKey, (float)EditorApplication.timeSinceStartup);
        }

        private static Stage GetStage() => (Stage)SessionState.GetInt(StageKey, (int)Stage.None);
        private static double ElapsedSeconds => EditorApplication.timeSinceStartup - SessionState.GetFloat(StartedAtKey, 0f);
    }
}
