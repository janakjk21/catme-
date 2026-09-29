using CatMe.Cat;
using CatMe.CameraSystem;
using CatMe.Toys;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace CatMe.Editor
{
    /// <summary>Exercises repeated physics throws and the cat chase/bat loop in HomeRoom.</summary>
    [InitializeOnLoad]
    public static class CatBallBatchValidator
    {
        private const string ScenePath = "Assets/CatMe/Scenes/HomeRoom.unity";
        private const string StageKey = "CatMe.CatBallBatch.Stage";
        private const string StartedKey = "CatMe.CatBallBatch.Started";
        private const string FailureKey = "CatMe.CatBallBatch.Failure";
        private const float TimeoutSeconds = 45f;
        private static Vector3 cameraStartPosition;
        private static Quaternion cameraStartRotation;
        private static Vector3 pivotStartPosition;
        private static float cameraStartFov;
        private static float cameraStartDistance;
        private static RoomOrbitCamera checkedCamera;

        private enum Stage { None, Start, WaitForReady, ThrowOne, WaitOne, ThrowTwo, WaitTwo, Finish, Failed }

        static CatBallBatchValidator()
        {
            EditorApplication.update -= Tick;
            EditorApplication.update += Tick;
        }

        [MenuItem("CatMe/Validate Ball Play (Batch)")]
        public static void Run()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
            {
                Debug.LogError("[CatMe][BallBatch] Stop Play Mode before validation.");
                return;
            }
            SessionState.SetString(FailureKey, string.Empty);
            SetStage(Stage.Start);
        }

        private static void Tick()
        {
            switch (GetStage())
            {
                case Stage.None: return;
                case Stage.Start: StartScene(); return;
                case Stage.WaitForReady: WaitForReady(); return;
                case Stage.ThrowOne: Throw(1); return;
                case Stage.WaitOne: WaitForThrow(1); return;
                case Stage.ThrowTwo: Throw(2); return;
                case Stage.WaitTwo: WaitForThrow(2); return;
                case Stage.Finish: Finish(0, "PASS"); return;
                case Stage.Failed: Finish(1, "FAIL: " + SessionState.GetString(FailureKey, "unknown")); return;
            }
        }

        private static void StartScene()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode) return;
            EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            SetStage(Stage.WaitForReady);
            EditorApplication.isPlaying = true;
        }

        private static void WaitForReady()
        {
            HomeRoomCatIntegration integration = Object.FindAnyObjectByType<HomeRoomCatIntegration>();
            CatBallInteraction ball = Object.FindAnyObjectByType<CatBallInteraction>();
            if (integration == null || ball == null || !integration.IsStationaryAcceptanceComplete)
            {
                Timeout("HomeRoom and ball play did not initialize.");
                return;
            }
            if (!integration.DidStationaryAcceptancePass || integration.CatMotor == null || !ball.HasSingleBall)
            {
                Fail("Cat load, stationary acceptance, motor, or single-ball setup failed.");
                return;
            }
            checkedCamera = Object.FindAnyObjectByType<RoomOrbitCamera>();
            Camera camera = Camera.main;
            if (checkedCamera == null || camera == null || !checkedCamera.IsFixedRoomView || checkedCamera.AuthoredPivot == null)
            {
                Fail("Fixed room camera or authored pivot is missing.");
                return;
            }
            cameraStartPosition = camera.transform.position;
            cameraStartRotation = camera.transform.rotation;
            pivotStartPosition = checkedCamera.AuthoredPivot.position;
            cameraStartFov = camera.fieldOfView;
            cameraStartDistance = checkedCamera.CurrentDistance;
            checkedCamera.FocusOnCat(integration.CatMotor.transform);
            checkedCamera.FocusOnActivity(integration.CatMotor.transform, 2.0f, 40f, 50f);
            checkedCamera.SetSeatedView(true);
            checkedCamera.ReturnToRoomView();
            SetStage(Stage.ThrowOne);
        }

        private static void Throw(int number)
        {
            CatBallInteraction ball = Object.FindAnyObjectByType<CatBallInteraction>();
            if (ball == null || !ball.ThrowForValidation(Vector3.forward, 1.25f))
            {
                Fail($"Throw {number} could not start.");
                return;
            }
            SetStage(number == 1 ? Stage.WaitOne : Stage.WaitTwo);
        }

        private static void WaitForThrow(int number)
        {
            CatBallInteraction ball = Object.FindAnyObjectByType<CatBallInteraction>();
            if (ball == null) { Fail("Ball component was destroyed during play."); return; }
            if (!FixedCameraUnchanged()) { Fail("Ball activity reframed or rotated the fixed room camera."); return; }
            if (ball.CurrentState == CatBallInteraction.BallState.Idle && !ball.IsActivityLockActive)
            {
                if (!ball.HasSingleBall || ball.ThrowCount != number || ball.BatCount < 1)
                {
                    Fail($"Throw {number} ended without one reusable ball and a successful cat bat.");
                    return;
                }
                Debug.Log($"[CatMe][BallBatch] Throw {number} PASS | bats={ball.BatCount} | failedPaths={ball.FailedPathCount}");
                SetStage(number == 1 ? Stage.ThrowTwo : Stage.Finish);
                return;
            }
            Timeout($"Throw {number} did not finish cleanly.");
        }

        private static bool FixedCameraUnchanged()
        {
            Camera camera = Camera.main;
            return checkedCamera != null && camera != null && checkedCamera.AuthoredPivot != null &&
                Vector3.Distance(camera.transform.position, cameraStartPosition) <= 0.005f &&
                Quaternion.Angle(camera.transform.rotation, cameraStartRotation) <= 0.05f &&
                Vector3.Distance(checkedCamera.AuthoredPivot.position, pivotStartPosition) <= 0.005f &&
                Mathf.Abs(camera.fieldOfView - cameraStartFov) <= 0.01f &&
                Mathf.Abs(checkedCamera.CurrentDistance - cameraStartDistance) <= 0.005f;
        }

        private static void Timeout(string message)
        {
            if (EditorApplication.timeSinceStartup - SessionState.GetFloat(StartedKey, 0f) >= TimeoutSeconds)
                Fail(message);
        }

        private static void Fail(string message)
        {
            SessionState.SetString(FailureKey, message);
            if (EditorApplication.isPlayingOrWillChangePlaymode) EditorApplication.isPlaying = false;
            SetStage(Stage.Failed);
        }

        private static void Finish(int exitCode, string result)
        {
            Debug.Log($"[CatMe][BallBatch] {result} | {SessionState.GetString(FailureKey, string.Empty)}");
            SessionState.EraseInt(StageKey);
            SessionState.EraseFloat(StartedKey);
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
            SessionState.SetFloat(StartedKey, (float)EditorApplication.timeSinceStartup);
        }

        private static Stage GetStage() => (Stage)SessionState.GetInt(StageKey, (int)Stage.None);
    }
}
