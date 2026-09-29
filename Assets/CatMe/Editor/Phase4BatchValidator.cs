using System;
using CatMe.Cat;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace CatMe.Editor
{
    /// <summary>
    /// Runs the Phase 4 scene checks in batch mode without needing the editor UI.
    /// It is invoked explicitly with -executeMethod and never runs in normal play.
    /// </summary>
    [InitializeOnLoad]
    public static class Phase4BatchValidator
    {
        private const string StageKey = "CatMe.Phase4BatchValidator.Stage";
        private const string StartedAtKey = "CatMe.Phase4BatchValidator.StartedAt";
        private const string RouteStartedAtKey = "CatMe.Phase4BatchValidator.RouteStartedAt";
        private const string FailureKey = "CatMe.Phase4BatchValidator.Failure";
        private const string ModelTestScene = "Assets/CatMe/Scenes/ModelTest.unity";
        private const string HomeRoomScene = "Assets/CatMe/Scenes/HomeRoom.unity";
        private const double ModelLoadTimeoutSeconds = 45d;
        private const double HomeLoadTimeoutSeconds = 90d;
        private const double RouteTimeoutSeconds = 150d;

        private enum Stage
        {
            None,
            StartModelTest,
            ObserveModelTest,
            StartHomeRoom,
            ObserveHomeRoom,
            Complete,
            Failed,
        }

        static Phase4BatchValidator()
        {
            EditorApplication.update -= Tick;
            EditorApplication.update += Tick;
        }

        [MenuItem("CatMe/Validate Phase 4 Locomotion (Batch)")]
        public static void Run()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
            {
                Debug.LogError("[CatMe][Phase4Batch] Stop Play Mode before starting the batch validator.");
                return;
            }

            SetStage(Stage.StartModelTest);
            SessionState.SetString(FailureKey, string.Empty);
            SessionState.SetFloat(RouteStartedAtKey, 0f);
            Debug.Log("[CatMe][Phase4Batch] Starting ModelTest and HomeRoom locomotion validation.");
        }

        private static void Tick()
        {
            Stage stage = GetStage();
            if (stage == Stage.None)
            {
                return;
            }

            switch (stage)
            {
                case Stage.StartModelTest:
                    StartScene(ModelTestScene, Stage.ObserveModelTest);
                    break;
                case Stage.ObserveModelTest:
                    ObserveModelTest();
                    break;
                case Stage.StartHomeRoom:
                    StartScene(HomeRoomScene, Stage.ObserveHomeRoom);
                    break;
                case Stage.ObserveHomeRoom:
                    ObserveHomeRoom();
                    break;
                case Stage.Complete:
                    Finish(0, "PASS");
                    break;
                case Stage.Failed:
                    Finish(1, "FAIL: " + SessionState.GetString(FailureKey, "Unknown failure"));
                    break;
            }
        }

        private static void StartScene(string scenePath, Stage observeStage)
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
            {
                return;
            }

            EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);
            SetStage(observeStage);
            EditorApplication.isPlaying = true;
        }

        private static void ObserveModelTest()
        {
            if (!EditorApplication.isPlaying)
            {
                return;
            }

            LocalCatAssetLoader loader = UnityEngine.Object.FindFirstObjectByType<LocalCatAssetLoader>();
            if (loader == null)
            {
                FailWhenTimedOut(ModelLoadTimeoutSeconds, "ModelTest did not create LocalCatAssetLoader.");
                return;
            }

            if (loader.HasFailed)
            {
                Fail("ModelTest fixture failed to load.");
                return;
            }

            if (!loader.IsLoaded)
            {
                FailWhenTimedOut(ModelLoadTimeoutSeconds, "ModelTest fixture did not load in time.");
                return;
            }

            if (loader.LoadedRoot == null || loader.ImportedAnimation == null || loader.SelectedWalkClip == null ||
                loader.LoadedRoot.GetComponentsInChildren<SkinnedMeshRenderer>(true).Length == 0 ||
                Mathf.Abs(loader.FinalBounds.size.y - loader.TargetHeight) > 0.02f ||
                Mathf.Abs(loader.FinalBounds.min.y) > 0.01f)
            {
                Fail("ModelTest loaded but did not meet model, animation, scale, or floor-contact acceptance.");
                return;
            }

            if (ElapsedSeconds < 12d)
            {
                return;
            }

            Debug.Log($"[CatMe][Phase4Batch] ModelTest PASS | bytes={loader.FixtureByteSize} | " +
                      $"clip={loader.SelectedWalkClip.name} | height={loader.FinalBounds.size.y:0.###}m.");
            EditorApplication.isPlaying = false;
            SetStage(Stage.StartHomeRoom);
        }

        private static void ObserveHomeRoom()
        {
            if (!EditorApplication.isPlaying)
            {
                return;
            }

            LocalCatAssetLoader loader = UnityEngine.Object.FindFirstObjectByType<LocalCatAssetLoader>();
            if (loader == null)
            {
                FailWhenTimedOut(HomeLoadTimeoutSeconds, "HomeRoom did not create LocalCatAssetLoader.");
                return;
            }

            if (loader.HasFailed)
            {
                Fail("HomeRoom fixture failed to load.");
                return;
            }

            if (!loader.IsLoaded)
            {
                FailWhenTimedOut(HomeLoadTimeoutSeconds, "HomeRoom fixture did not load in time.");
                return;
            }

            HomeRoomCatIntegration integration = UnityEngine.Object.FindFirstObjectByType<HomeRoomCatIntegration>();
            if (integration == null)
            {
                FailWhenTimedOut(HomeLoadTimeoutSeconds, "HomeRoom did not create HomeRoomCatIntegration.");
                return;
            }

            if (!integration.IsStationaryAcceptanceComplete)
            {
                FailWhenTimedOut(HomeLoadTimeoutSeconds, "HomeRoom stationary acceptance did not complete in time.");
                return;
            }

            if (!integration.DidStationaryAcceptancePass)
            {
                Fail("HomeRoom stationary acceptance failed.");
                return;
            }

            CatMotor motor = integration.CatMotor;
            if (motor == null)
            {
                Fail("HomeRoom stationary acceptance passed but CatMotor was not created.");
                return;
            }

            if (motor.HasDiagnosticFailed)
            {
                Fail("CatMotor reported a route failure.");
                return;
            }

            float routeStartedAt = SessionState.GetFloat(RouteStartedAtKey, 0f);
            if (routeStartedAt <= 0f)
            {
                // Normal HomeRoom play waits for player Call. The Phase 4
                // validator is the explicit owner of the diagnostic route.
                motor.BeginDiagnosticRoute();
                SessionState.SetFloat(RouteStartedAtKey, (float)EditorApplication.timeSinceStartup);
                routeStartedAt = (float)EditorApplication.timeSinceStartup;
            }

            if (!motor.IsDiagnosticFinished)
            {
                if (EditorApplication.timeSinceStartup - routeStartedAt >= RouteTimeoutSeconds)
                {
                    Fail("CatMotor did not finish the diagnostic route in time.");
                }
                return;
            }

            bool pass = motor.CompletedLegs >= 10 && motor.FailedCount == 0 && motor.PartialCount == 0 &&
                        motor.OffNavMeshCount == 0 && motor.StuckCount == 0 && motor.MaxArrivalError <= 0.10f &&
                        motor.MinForwardVelocityDot >= 0.95f && motor.MaxLocalDrift <= 0.02f;
            if (!pass)
            {
                Fail("CatMotor completed but did not meet locomotion acceptance.");
                return;
            }

            Debug.Log($"[CatMe][Phase4Batch] HomeRoom PASS | legs={motor.CompletedLegs} | " +
                      $"arrivalError={motor.MaxArrivalError:0.####}m | forwardDot={motor.MinForwardVelocityDot:0.###} | " +
                      $"localDrift={motor.MaxLocalDrift:0.####}m.");
            EditorApplication.isPlaying = false;
            SetStage(Stage.Complete);
        }

        private static void FailWhenTimedOut(double timeoutSeconds, string message)
        {
            if (ElapsedSeconds >= timeoutSeconds)
            {
                Fail(message);
            }
        }

        private static void Fail(string message)
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
            {
                EditorApplication.isPlaying = false;
            }

            SessionState.SetString(FailureKey, message);
            SetStage(Stage.Failed);
        }

        private static void Finish(int exitCode, string result)
        {
            Debug.Log($"[CatMe][Phase4Batch] {result}");
            SessionState.EraseInt(StageKey);
            SessionState.EraseFloat(StartedAtKey);
            SessionState.EraseFloat(RouteStartedAtKey);
            SessionState.EraseString(FailureKey);
            if (Application.isBatchMode)
            {
                EditorApplication.Exit(exitCode);
            }
            else
            {
                EditorApplication.isPlaying = false;
            }
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
