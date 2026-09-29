using CatMe.Cat;
using CatMe.Toys;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace CatMe.Editor
{
    /// <summary>Focused editor validation for two three-catch laser sessions.</summary>
    [InitializeOnLoad]
    public static class Phase9BatchValidator
    {
        private const string StageKey = "CatMe.Phase9BatchValidator.Stage";
        private const string StartedAtKey = "CatMe.Phase9BatchValidator.StartedAt";
        private const string FailureKey = "CatMe.Phase9BatchValidator.Failure";
        private const string HomeRoomScene = "Assets/CatMe/Scenes/HomeRoom.unity";
        private const double SetupTimeoutSeconds = 90d;
        private const double SessionTimeoutSeconds = 60d;
        private static readonly Vector3[] Targets =
        {
            new Vector3(-0.9f, 0f, -0.55f), new Vector3(0f, 0f, 0.55f), new Vector3(0.9f, 0f, -0.55f)
        };

        private enum Stage { None, StartHomeRoom, WaitForReady, SessionOne, SessionTwo, Complete, Failed }

        static Phase9BatchValidator()
        {
            EditorApplication.update -= Tick;
            EditorApplication.update += Tick;
        }

        [MenuItem("CatMe/Validate Phase 9 Laser (Batch)")]
        public static void Run()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
            {
                Debug.LogError("[CatMe][Phase9Batch] Stop Play Mode before starting the batch validator.");
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
                case Stage.SessionOne: RunSession(1); return;
                case Stage.SessionTwo: RunSession(2); return;
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
            LaserToyInteraction laser = Object.FindAnyObjectByType<LaserToyInteraction>();
            if (integration == null || laser == null || !integration.IsStationaryAcceptanceComplete)
            {
                FailWhenTimedOut(SetupTimeoutSeconds, "HomeRoom laser components were not ready in time.");
                return;
            }

            if (!integration.DidStationaryAcceptancePass || integration.CatMotor == null || !laser.BeginSessionForValidation())
            {
                Fail("HomeRoom stationary acceptance or first laser session setup failed.");
                return;
            }

            SetStage(Stage.SessionOne);
        }

        private static void RunSession(int session)
        {
            if (!EditorApplication.isPlaying) return;
            LaserToyInteraction laser = Object.FindAnyObjectByType<LaserToyInteraction>();
            if (laser == null)
            {
                Fail("Laser interaction was destroyed during validation.");
                return;
            }

            if (laser.CurrentState == LaserToyInteraction.LaserState.Aiming && laser.SuccessfulCatchCount < 3)
            {
                int next = laser.AcceptedTargetCount;
                if (next < Targets.Length && !laser.PlaceTargetForValidation(Targets[next]))
                {
                    Fail($"Session {session} rejected its valid target {next + 1}.");
                    return;
                }
            }

            if (laser.CurrentState != LaserToyInteraction.LaserState.Idle)
            {
                FailWhenTimedOut(SessionTimeoutSeconds, $"Laser session {session} did not finish three catches.");
                return;
            }

            if (laser.SuccessfulCatchCount != 3 || laser.LaserDotInstanceCount != 1 || laser.IsActivityLockActive ||
                laser.MaximumProtectedLocalPositionDrift > 0.02f || laser.MaximumRotationRestoreError > 0.001f ||
                laser.MaximumScaleRestoreError > 0.001f)
            {
                Fail($"Laser session {session} failed catch, lock, dot, or neutral restore acceptance.");
                return;
            }

            if (session == 1)
            {
                if (!laser.BeginSessionForValidation())
                {
                    Fail("Second laser session could not start cleanly.");
                    return;
                }
                SetStage(Stage.SessionTwo);
            }
            else
            {
                Debug.Log($"[CatMe][Phase9Batch] Laser PASS | sessions=2 | catches={laser.SuccessfulCatchCount} | " +
                    $"accepted={laser.AcceptedTargetCount} | rejected={laser.RejectedTargetCount} | " +
                    $"dotInstances={laser.LaserDotInstanceCount} | cameraDelta={laser.MaximumCameraDelta:0.###} | " +
                    $"protectedLocalDrift={laser.MaximumProtectedLocalPositionDrift:0.####}m | haptics={laser.HapticRequestCount}");
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
            Debug.Log("[CatMe][Phase9Batch] " + result);
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
