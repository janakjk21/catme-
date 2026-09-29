using System.IO;
using CatMe.Cat;
using CatMe.CameraSystem;
using CatMe.Toys;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace CatMe.Editor
{
    /// <summary>Captures the actual HomeRoom camera and local cat for art review.</summary>
    [InitializeOnLoad]
    public static class HomeRoomPreviewCapture
    {
        private const string Key = "CatMe.RoomPreview.Pending";
        private const string CompanionViewKey = "CatMe.RoomPreview.Companion";
        private const string CallCaptureKey = "CatMe.RoomPreview.CallCapture";
        private const string CallCaptureSeatedKey = "CatMe.RoomPreview.CallCaptureSeated";
        private const string CallCaptureWalkKey = "CatMe.RoomPreview.CallCaptureWalk";
        private const string CallCaptureStartedKey = "CatMe.RoomPreview.CallCaptureStarted";
        private const string CallCaptureWaitKey = "CatMe.RoomPreview.CallCaptureWaitUntil";
        private const string BallCaptureKey = "CatMe.RoomPreview.BallCapture";
        private const string BallCaptureStartedKey = "CatMe.RoomPreview.BallCaptureStarted";
        private const string BallCaptureTimeoutKey = "CatMe.RoomPreview.BallCaptureTimeout";
        static HomeRoomPreviewCapture() { EditorApplication.update += Tick; }
        public static void ApplyAndRun()
        {
            RoomAssetPopulation.Apply();
            Run();
        }

        [MenuItem("CatMe/Capture Runtime Room Preview")]
        public static void Run()
        {
            StartCapture(false);
        }

        [MenuItem("CatMe/Capture Companion Camera Preview")]
        public static void RunCompanionView()
        {
            StartCapture(true);
        }

        [MenuItem("CatMe/Capture Call Comparison - Third Person")]
        public static void RunThirdPersonCall()
        {
            StartCallCapture(false);
        }

        [MenuItem("CatMe/Capture Call Comparison - Seated First Person")]
        public static void RunSeatedCall()
        {
            StartCallCapture(true);
        }

        [MenuItem("CatMe/Capture Walk-and-Call First Person Preview")]
        public static void RunWalkingFirstPersonCall()
        {
            StartCallCapture(true);
            SessionState.SetBool(CallCaptureWalkKey, true);
        }

        [MenuItem("CatMe/Capture Seated Toy Chase Preview")]
        public static void RunSeatedToyChase()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
            {
                Debug.LogWarning("Stop Play Mode before capturing a seated toy chase.");
                return;
            }
            EditorSceneManager.OpenScene("Assets/CatMe/Scenes/HomeRoom.unity");
            SessionState.SetBool(CompanionViewKey, false);
            SessionState.SetBool(CallCaptureKey, false);
            SessionState.SetBool(CallCaptureWalkKey, false);
            SessionState.SetBool(BallCaptureKey, true);
            SessionState.SetBool(BallCaptureStartedKey, false);
            SessionState.EraseFloat(CallCaptureWaitKey);
            SessionState.SetFloat(BallCaptureTimeoutKey, (float)EditorApplication.timeSinceStartup + 30f);
            SessionState.SetBool(Key, true);
            EditorApplication.isPlaying = true;
        }

        private static void StartCapture(bool companionView)
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
            {
                Debug.LogWarning("Stop Play Mode before capturing the runtime room preview.");
                return;
            }
            EditorSceneManager.OpenScene("Assets/CatMe/Scenes/HomeRoom.unity");
            SessionState.SetBool(CompanionViewKey, companionView);
            SessionState.SetBool(CallCaptureKey, false);
            SessionState.SetBool(Key, true);
            EditorApplication.isPlaying = true;
        }

        private static void StartCallCapture(bool seated)
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
            {
                Debug.LogWarning("Stop Play Mode before capturing a call comparison.");
                return;
            }
            EditorSceneManager.OpenScene("Assets/CatMe/Scenes/HomeRoom.unity");
            SessionState.SetBool(CompanionViewKey, false);
            SessionState.SetBool(CallCaptureKey, true);
            SessionState.SetBool(CallCaptureSeatedKey, seated);
            SessionState.SetBool(CallCaptureWalkKey, false);
            SessionState.SetBool(CallCaptureStartedKey, false);
            SessionState.EraseFloat(CallCaptureWaitKey);
            SessionState.SetBool(Key, true);
            EditorApplication.isPlaying = true;
        }
        private static void Tick()
        {
            if (!SessionState.GetBool(Key, false) || !EditorApplication.isPlaying) return;
            var cat = Object.FindAnyObjectByType<HomeRoomCatIntegration>();
            if (cat == null || !cat.IsStationaryAcceptanceComplete)
            {
                if (Time.timeSinceLevelLoad > 90f)
                {
                    SessionState.SetBool(Key, false);
                    Debug.LogError("Room preview could not load the local cat within 90 seconds.");
                    EditorApplication.isPlaying = false;
                    if (Application.isBatchMode) EditorApplication.delayCall += () => EditorApplication.Exit(1);
                }
                return;
            }
            if (SessionState.GetBool(CallCaptureKey, false))
            {
                if (!TryPrepareCallCapture(cat)) return;
                if (EditorApplication.timeSinceStartup < SessionState.GetFloat(CallCaptureWaitKey, 0f)) return;
            }
            if (SessionState.GetBool(BallCaptureKey, false))
            {
                if (!TryPrepareBallCapture(cat)) return;
                if (EditorApplication.timeSinceStartup < SessionState.GetFloat(CallCaptureWaitKey, 0f)) return;
            }
            if (SessionState.GetBool(CompanionViewKey, false))
            {
                RoomOrbitCamera roomCamera = Object.FindAnyObjectByType<RoomOrbitCamera>();
                if (roomCamera == null)
                {
                    SessionState.SetBool(Key, false);
                    Debug.LogError("Room camera was unavailable for companion view capture.");
                    EditorApplication.isPlaying = false;
                    if (Application.isBatchMode) EditorApplication.delayCall += () => EditorApplication.Exit(1);
                    return;
                }
                roomCamera.FocusOnCat(cat.transform);
                float companionWait = SessionState.GetFloat(CallCaptureWaitKey, 0f);
                if (companionWait <= 0f)
                {
                    SessionState.SetFloat(CallCaptureWaitKey, (float)EditorApplication.timeSinceStartup + 2f);
                    return;
                }
                if (EditorApplication.timeSinceStartup < companionWait) return;
            }
            SessionState.SetBool(Key, false);
            var camera = Camera.main;
            if (SessionState.GetBool(CallCaptureKey, false))
            {
                Vector3 toCamera = (camera.transform.position - cat.transform.position).normalized;
                float facingCameraDot = Vector3.Dot(cat.transform.forward, toCamera);
                Debug.Log($"[CatMe][RoomPreview] Call comparison | seated={SessionState.GetBool(CallCaptureSeatedKey, false)} | " +
                          $"catPosition={cat.transform.position} | cameraPosition={camera.transform.position} | " +
                          $"catForward={cat.transform.forward} | facingCameraDot={facingCameraDot:0.###}");
            }
            if (SessionState.GetBool(CompanionViewKey, false))
            {
                RoomOrbitCamera roomCamera = camera.GetComponent<RoomOrbitCamera>();
                Vector3 toCamera = (camera.transform.position - cat.transform.position).normalized;
                Debug.Log($"[CatMe][RoomPreview] Companion framing | cameraPosition={camera.transform.position} | " +
                    $"catForward={cat.transform.forward} | cameraSideDot={Vector3.Dot(cat.transform.forward, toCamera):0.###} | " +
                    $"distance={roomCamera.CurrentDistance:0.###} | collisionShortened={roomCamera.CollisionShortened}");
            }
            var previous = camera.targetTexture;
            float previousAspect = camera.aspect;
            var active = RenderTexture.active;
            const int previewWidth = 1600;
            const int previewHeight = 900;
            var texture = new RenderTexture(previewWidth, previewHeight, 24);
            var image = new Texture2D(previewWidth, previewHeight, TextureFormat.RGB24, false);
            camera.targetTexture = texture;
            camera.aspect = (float)previewWidth / previewHeight;
            camera.Render();
            RenderTexture.active = texture;
            image.ReadPixels(new Rect(0, 0, previewWidth, previewHeight), 0, 0);
            image.Apply();
            string outputPath;
            if (SessionState.GetBool(CallCaptureKey, false))
            {
                string suffix = SessionState.GetBool(CallCaptureWalkKey, false)
                    ? "walk-first-person"
                    : SessionState.GetBool(CallCaptureSeatedKey, false) ? "seated-first-person" : "third-person";
                const string directory = "docs/research/catme/assets";
                Directory.CreateDirectory(directory);
                outputPath = Path.Combine(directory, "call-view-" + suffix + ".png");
            }
            else if (SessionState.GetBool(BallCaptureKey, false))
            {
                outputPath = "docs/research/catme/assets/toy-view-seated-first-person.png";
            }
            else
            {
                outputPath = SessionState.GetBool(CompanionViewKey, false)
                    ? "/tmp/catme-room-companion.png"
                    : "/tmp/catme-room-runtime.png";
            }
            File.WriteAllBytes(outputPath, image.EncodeToPNG());
            camera.targetTexture = previous;
            camera.aspect = previousAspect;
            RenderTexture.active = active;
            Object.DestroyImmediate(image);
            texture.Release();
            Object.DestroyImmediate(texture);
            Debug.Log($"[CatMe][RoomPreview] Captured {outputPath}");
            SessionState.SetBool(CompanionViewKey, false);
            SessionState.SetBool(CallCaptureKey, false);
            SessionState.SetBool(CallCaptureWalkKey, false);
            SessionState.SetBool(BallCaptureKey, false);
            EditorApplication.isPlaying = false;
            if (Application.isBatchMode) EditorApplication.delayCall += () => EditorApplication.Exit(0);
        }

        private static bool TryPrepareCallCapture(HomeRoomCatIntegration cat)
        {
            RoomOrbitCamera roomCamera = Object.FindAnyObjectByType<RoomOrbitCamera>();
            if (roomCamera == null || cat.CatMotor == null || !cat.CatMotor.IsReady)
            {
                if (Time.timeSinceLevelLoad > 90f)
                {
                    FailCapture("Camera or cat motor was not ready for the call comparison.");
                }
                return false;
            }

            bool seated = SessionState.GetBool(CallCaptureSeatedKey, false);
            roomCamera.SetSeatedView(seated);
            if (!SessionState.GetBool(CallCaptureStartedKey, false))
            {
                Transform destination = GameObject.Find("CallDestination")?.transform;
                if (seated)
                {
                    if (SessionState.GetBool(CallCaptureWalkKey, false))
                    {
                        Vector3 beforeWalk = roomCamera.SeatedPosition;
                        bool moved = roomCamera.MoveForEditorValidation(Vector2.up, 0.35f);
                        float walkedMeters = Vector3.Distance(beforeWalk, roomCamera.SeatedPosition);
                        Debug.Log($"[CatMe][RoomPreview] First-person move check | moved={moved} | delta={walkedMeters:0.###}m | " +
                                  $"newPlayerPosition={roomCamera.SeatedPosition}");
                    }
                    if (!roomCamera.TryGetSeatedCallPose(out Vector3 position, out Quaternion rotation))
                    {
                        FailCapture("Seated call approach could not be created.");
                        return false;
                    }
                    destination = new GameObject("PreviewSeatedCallApproach").transform;
                    destination.SetPositionAndRotation(position, rotation);
                }
                if (destination == null || !cat.CatMotor.TryMoveTo(destination, "Preview call comparison"))
                {
                    FailCapture("Call comparison could not start the cat approach.");
                    return false;
                }
                if (!seated) roomCamera.FocusOnCat(cat.transform);
                SessionState.SetBool(CallCaptureStartedKey, true);
                return false;
            }

            if (!cat.CatMotor.HasFullyArrived) return false;
            if (SessionState.GetFloat(CallCaptureWaitKey, 0f) <= 0f)
            {
                SessionState.SetFloat(CallCaptureWaitKey, (float)EditorApplication.timeSinceStartup + 1.0f);
                return false;
            }
            return true;
        }

        private static bool TryPrepareBallCapture(HomeRoomCatIntegration cat)
        {
            RoomOrbitCamera roomCamera = Object.FindAnyObjectByType<RoomOrbitCamera>();
            CatBallInteraction ball = Object.FindAnyObjectByType<CatBallInteraction>();
            if (roomCamera == null || cat.CatMotor == null || ball == null || !cat.CatMotor.IsReady)
            {
                if (Time.timeSinceLevelLoad > 90f) FailCapture("Camera, cat motor, or ball interaction was unavailable.");
                return false;
            }

            roomCamera.SetSeatedView(true);
            if (!SessionState.GetBool(BallCaptureStartedKey, false))
            {
                // A cross-room throw keeps the cat in front of the seated
                // player instead of sending it directly away from the camera.
                if (!ball.ThrowForValidation(Vector3.right, 2.8f))
                {
                    FailCapture("Seated toy chase could not start.");
                    return false;
                }
                SessionState.SetBool(BallCaptureStartedKey, true);
                return false;
            }

            if (EditorApplication.timeSinceStartup > SessionState.GetFloat(BallCaptureTimeoutKey, 0f))
            {
                FailCapture("Seated toy chase did not produce a readable cat-toy interaction in 30 seconds.");
                return false;
            }
            if (ball.CurrentState != CatBallInteraction.BallState.Watching || ball.BatCount < 1) return false;
            if (SessionState.GetFloat(CallCaptureWaitKey, 0f) <= 0f)
            {
                SessionState.SetFloat(CallCaptureWaitKey, (float)EditorApplication.timeSinceStartup + 0.25f);
                return false;
            }
            return true;
        }

        private static void FailCapture(string message)
        {
            SessionState.SetBool(Key, false);
            SessionState.SetBool(CallCaptureKey, false);
            SessionState.SetBool(BallCaptureKey, false);
            Debug.LogError("[CatMe][RoomPreview] " + message);
            EditorApplication.isPlaying = false;
            if (Application.isBatchMode) EditorApplication.delayCall += () => EditorApplication.Exit(1);
        }
    }
}
