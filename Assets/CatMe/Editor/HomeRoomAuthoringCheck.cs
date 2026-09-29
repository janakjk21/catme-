using System.Collections.Generic;
using Unity.AI.Navigation;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.AI;

namespace CatMe.Editor
{
    /// <summary>
    /// Read-only anchor and navigation check for the currently open HomeRoom.
    /// Room authors can move named anchors and rerun this check from the CatMe menu.
    /// </summary>
    public static class HomeRoomAuthoringCheck
    {
        private static readonly string[] RequiredAnchors =
        {
            "CatSpawn", "CallDestination", "WindowApproach", "SleepApproach",
            "FeedingApproach", "ToyApproach", "CameraPivot"
        };

        [MenuItem("CatMe/Room Authoring/Open HomeRoom and Check")]
        public static void OpenAndCheckHomeRoom()
        {
            EditorSceneManager.OpenScene("Assets/CatMe/Scenes/HomeRoom.unity", OpenSceneMode.Single);
            ValidateOpenHomeRoom();
        }

        [MenuItem("CatMe/Room Authoring/Check Anchors and Navigation")]
        public static void ValidateOpenHomeRoom()
        {
            if (EditorSceneManager.GetActiveScene().name != "HomeRoom")
            {
                Debug.LogError("[CatMe][RoomCheck] Open HomeRoom before running the anchor check.");
                return;
            }

            List<string> errors = new List<string>();
            Dictionary<string, Transform> anchors = new Dictionary<string, Transform>();
            foreach (string name in RequiredAnchors)
            {
                GameObject anchor = GameObject.Find(name);
                if (anchor == null)
                {
                    errors.Add("Missing required anchor: " + name);
                    continue;
                }
                anchors[name] = anchor.transform;
            }

            NavMeshSurface surface = Object.FindAnyObjectByType<NavMeshSurface>();
            if (surface == null) errors.Add("Missing NavMeshSurface.");
            if (anchors.TryGetValue("CatSpawn", out Transform spawn) && surface != null)
            {
                if (!NavMesh.SamplePosition(spawn.position, out NavMeshHit spawnHit, 0.5f, NavMesh.AllAreas))
                {
                    errors.Add("CatSpawn is not near the baked NavMesh.");
                }
                else
                {
                    foreach (string destinationName in new[] { "CallDestination", "WindowApproach", "SleepApproach", "FeedingApproach", "ToyApproach" })
                    {
                        if (!anchors.TryGetValue(destinationName, out Transform destination)) continue;
                        if (!NavMesh.SamplePosition(destination.position, out NavMeshHit targetHit, 0.5f, NavMesh.AllAreas))
                        {
                            errors.Add(destinationName + " is not near the baked NavMesh.");
                            continue;
                        }
                        NavMeshPath path = new NavMeshPath();
                        if (!NavMesh.CalculatePath(spawnHit.position, targetHit.position, NavMesh.AllAreas, path) ||
                            path.status != NavMeshPathStatus.PathComplete)
                            errors.Add("No complete NavMesh route from CatSpawn to " + destinationName + ".");
                    }
                }
            }

            Camera camera = Camera.main;
            if (camera == null) errors.Add("Missing MainCamera tag/reference.");
            if (errors.Count == 0)
            {
                Debug.Log("[CatMe][RoomCheck] PASS | required anchors present | spawn, activity destinations, and camera valid | NavMesh routes complete.");
            }
            else
            {
                foreach (string error in errors) Debug.LogError("[CatMe][RoomCheck] " + error);
                Debug.LogError($"[CatMe][RoomCheck] FAIL | {errors.Count} issue(s).");
            }

            if (Application.isBatchMode) EditorApplication.Exit(errors.Count == 0 ? 0 : 1);
        }
    }
}
