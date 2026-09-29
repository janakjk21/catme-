using System.IO;
using CatMe.Cat;
using CatMe.UI;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace CatMe.Editor
{
    /// <summary>Checks a camera-only portrait capture and recent-photo reopen using isolated temporary storage.</summary>
    [InitializeOnLoad]
    public static class CatPhotoBatchValidator
    {
        private const string PhotoPath = "/tmp/catme-photo-validation.png";
        private const string StageKey = "CatMe.CatPhotoBatch.Stage";
        private const string StartedKey = "CatMe.CatPhotoBatch.Started";
        private const string FailureKey = "CatMe.CatPhotoBatch.Failure";
        private enum Stage { None, Start, WaitForCat, Capture, WaitForCapture, OpenPhoto, Finish, Failed }

        static CatPhotoBatchValidator()
        {
            EditorApplication.update -= Tick;
            EditorApplication.update += Tick;
        }

        [MenuItem("CatMe/Validate Photo Capture (Batch)")]
        public static void Run()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
            {
                Debug.LogError("[CatMe][PhotoBatch] Stop Play Mode before validation.");
                return;
            }
            SessionState.SetString(FailureKey, string.Empty);
            SetStage(Stage.Start);
        }

        private static void Tick()
        {
            switch ((Stage)SessionState.GetInt(StageKey, 0))
            {
                case Stage.None: return;
                case Stage.Start:
                    if (File.Exists(PhotoPath)) File.Delete(PhotoPath);
                    EditorSceneManager.OpenScene("Assets/CatMe/Scenes/HomeRoom.unity", OpenSceneMode.Single);
                    SetStage(Stage.WaitForCat);
                    EditorApplication.isPlaying = true;
                    return;
                case Stage.WaitForCat:
                    HomeRoomCatIntegration integration = Object.FindAnyObjectByType<HomeRoomCatIntegration>();
                    CatPhotoCapture photo = Object.FindAnyObjectByType<CatPhotoCapture>();
                    if (integration != null && integration.IsStationaryAcceptanceComplete && photo != null)
                    {
                        photo.CaptureForValidation(PhotoPath);
                        SetStage(Stage.WaitForCapture);
                    }
                    else if (Expired(80f)) Fail("HomeRoom cat/photo controls did not initialize.");
                    return;
                case Stage.WaitForCapture:
                    photo = Object.FindAnyObjectByType<CatPhotoCapture>();
                    if (photo != null && photo.LastCaptureSucceeded && File.Exists(PhotoPath) && new FileInfo(PhotoPath).Length > 1024)
                    {
                        photo.OpenRecentPhotoForValidation();
                        SetStage(photo.IsOverlayOpen ? Stage.Finish : Stage.Failed);
                        if (!photo.IsOverlayOpen) SessionState.SetString(FailureKey, "Recent-photo overlay did not open.");
                    }
                    else if (Expired(15f)) Fail("Photo capture did not produce a readable PNG.");
                    return;
                case Stage.Finish:
                    photo = Object.FindAnyObjectByType<CatPhotoCapture>();
                    photo?.CloseOverlayForValidation();
                    Finish(0, "PASS | room-only screenshot saved and recent-photo overlay opened");
                    return;
                case Stage.Failed:
                    Finish(1, "FAIL: " + SessionState.GetString(FailureKey, "unknown"));
                    return;
            }
        }

        private static bool Expired(float timeout) => EditorApplication.timeSinceStartup - SessionState.GetFloat(StartedKey, 0f) >= timeout;

        private static void Fail(string message)
        {
            SessionState.SetString(FailureKey, message);
            if (EditorApplication.isPlayingOrWillChangePlaymode) EditorApplication.isPlaying = false;
            SetStage(Stage.Failed);
        }

        private static void Finish(int exitCode, string result)
        {
            Debug.Log("[CatMe][PhotoBatch] " + result);
            if (File.Exists(PhotoPath)) File.Delete(PhotoPath);
            SessionState.EraseInt(StageKey);
            SessionState.EraseFloat(StartedKey);
            SessionState.EraseString(FailureKey);
            EditorApplication.Exit(exitCode);
        }

        private static void SetStage(Stage stage)
        {
            SessionState.SetInt(StageKey, (int)stage);
            SessionState.SetFloat(StartedKey, (float)EditorApplication.timeSinceStartup);
        }
    }
}
