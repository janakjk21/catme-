using CatMe.Cat;
using CatMe.GameInput;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace CatMe.Editor
{
    /// <summary>
    /// Exercises the local pet response after HomeRoom has completed its
    /// stationary asset acceptance. It is invoked explicitly in batch mode.
    /// </summary>
    [InitializeOnLoad]
    public static class Phase7BatchValidator
    {
        private const string StageKey = "CatMe.Phase7BatchValidator.Stage";
        private const string StartedAtKey = "CatMe.Phase7BatchValidator.StartedAt";
        private const string FailureKey = "CatMe.Phase7BatchValidator.Failure";
        private const string HomeRoomScene = "Assets/CatMe/Scenes/HomeRoom.unity";
        private const double SetupTimeoutSeconds = 90d;
        private const double ResponseTimeoutSeconds = 4d;

        private enum Stage
        {
            None,
            StartHomeRoom,
            WaitForReadyCat,
            ObserveReactionStart,
            VerifyNeutralRestore,
            Complete,
            Failed,
        }

        private static Transform catModelRoot;
        private static Quaternion neutralRotation;
        private static Vector3 neutralScale;

        static Phase7BatchValidator()
        {
            EditorApplication.update -= Tick;
            EditorApplication.update += Tick;
        }

        [MenuItem("CatMe/Validate Phase 7 Petting (Batch)")]
        public static void Run()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
            {
                Debug.LogError("[CatMe][Phase7Batch] Stop Play Mode before starting the batch validator.");
                return;
            }

            SetStage(Stage.StartHomeRoom);
            SessionState.SetString(FailureKey, string.Empty);
            Debug.Log("[CatMe][Phase7Batch] Starting HomeRoom petting validation.");
        }

        private static void Tick()
        {
            switch (GetStage())
            {
                case Stage.None:
                    return;
                case Stage.StartHomeRoom:
                    StartHomeRoom();
                    return;
                case Stage.WaitForReadyCat:
                    WaitForReadyCat();
                    return;
                case Stage.ObserveReactionStart:
                    ObserveReactionStart();
                    return;
                case Stage.VerifyNeutralRestore:
                    VerifyNeutralRestore();
                    return;
                case Stage.Complete:
                    Finish(0, "PASS");
                    return;
                case Stage.Failed:
                    Finish(1, "FAIL: " + SessionState.GetString(FailureKey, "Unknown failure"));
                    return;
            }
        }

        private static void StartHomeRoom()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
            {
                return;
            }

            EditorSceneManager.OpenScene(HomeRoomScene, OpenSceneMode.Single);
            SetStage(Stage.WaitForReadyCat);
            EditorApplication.isPlaying = true;
        }

        private static void WaitForReadyCat()
        {
            if (!EditorApplication.isPlaying)
            {
                return;
            }

            HomeRoomCatIntegration integration = Object.FindAnyObjectByType<HomeRoomCatIntegration>();
            CatPetReaction reaction = Object.FindAnyObjectByType<CatPetReaction>();
            CatPetInput input = Object.FindAnyObjectByType<CatPetInput>();
            if (integration == null || reaction == null || input == null || !integration.IsStationaryAcceptanceComplete)
            {
                FailWhenTimedOut(SetupTimeoutSeconds, "HomeRoom petting components were not ready in time.");
                return;
            }

            if (!integration.DidStationaryAcceptancePass || integration.CatMotor == null || integration.CatMotor.IsMoving)
            {
                Fail("HomeRoom was not stationary before petting validation.");
                return;
            }

            catModelRoot = integration.transform.Find("CatModelRoot");
            if (reaction.PetTarget == null || catModelRoot == null)
            {
                Fail("Pet target or CatModelRoot is missing.");
                return;
            }

            neutralRotation = catModelRoot.localRotation;
            neutralScale = catModelRoot.localScale;
            if (!reaction.TryReact(1f))
            {
                Fail("A valid stationary pet reaction was rejected.");
                return;
            }

            SetStage(Stage.ObserveReactionStart);
        }

        private static void ObserveReactionStart()
        {
            if (!EditorApplication.isPlaying)
            {
                return;
            }

            CatPetReaction reaction = Object.FindAnyObjectByType<CatPetReaction>();
            if (reaction == null || !reaction.HasPurrClip || reaction.PurrStartCount != 1 ||
                reaction.ReactionCount != 1 || reaction.ValidStrokeCount != 1)
            {
                FailWhenTimedOut(1d, "Pet reaction did not queue one purr source from the serialized local clip.");
                return;
            }

            SetStage(Stage.VerifyNeutralRestore);
        }

        private static void VerifyNeutralRestore()
        {
            if (!EditorApplication.isPlaying)
            {
                return;
            }

            if (ElapsedSeconds < 1.2d)
            {
                return;
            }

            CatPetReaction reaction = Object.FindAnyObjectByType<CatPetReaction>();
            if (reaction == null || catModelRoot == null)
            {
                Fail("Pet reaction was lost before restoration validation.");
                return;
            }

            float rotationError = Quaternion.Angle(catModelRoot.localRotation, neutralRotation);
            float scaleError = Vector3.Distance(catModelRoot.localScale, neutralScale);
            bool pass = !reaction.IsPurrPlaying && !Object.FindAnyObjectByType<CatPetInput>().IsPointerCaptured &&
                        reaction.MaximumProtectedPositionDrift <= 0.0001f && rotationError <= 0.05f && scaleError <= 0.001f;
            if (!pass)
            {
                Fail("Pet response did not stop cleanly or restore CatModelRoot to neutral.");
                return;
            }

            Debug.Log($"[CatMe][Phase7Batch] HomeRoom PASS | target={reaction.PetTarget.GetType().Name} | " +
                      $"size={reaction.PetTarget.bounds.size} | purrPlayed=YES | reactions={reaction.ReactionCount} | " +
                      $"protectedDrift={reaction.MaximumProtectedPositionDrift:0.######}m | " +
                      $"rotationRestoreError={rotationError:0.######}deg | scaleRestoreError={scaleError:0.######}.");
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
            Debug.Log("[CatMe][Phase7Batch] " + result);
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
