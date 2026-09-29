using Unity.AI.Navigation;
using CatMe.Cat;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace CatMe.Editor
{
    /// <summary>
    /// Applies the temporary prototype composition: a deeper, narrower room
    /// with only the interaction props needed for validation.
    /// </summary>
    public static class RoomPresentationTuning
    {
        private const string ScenePath = "Assets/CatMe/Scenes/HomeRoom.unity";

        [MenuItem("CatMe/Tune Prototype Room Presentation")]
        public static void Apply()
        {
            Scene scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            if (!scene.IsValid())
            {
                Debug.LogError($"Could not open {ScenePath}.");
                return;
            }

            GameObject room = GameObject.Find("Room_Blockout");
            if (room == null)
            {
                Debug.LogError("Room_Blockout is missing.");
                return;
            }

            // Keep the authored 6.5 m × 5 m room wide enough to read as a
            // complete, stage-like landscape scene on a phone.
            room.transform.localScale = Vector3.one;

            GameObject catRuntime = GameObject.Find("CatRuntime");
            HomeRoomCatIntegration integration = catRuntime == null
                ? null
                : catRuntime.GetComponent<HomeRoomCatIntegration>();
            if (integration != null)
            {
                SerializedObject serializedIntegration = new SerializedObject(integration);
                SerializedProperty presentationScale = serializedIntegration.FindProperty("presentationScale");
                if (presentationScale != null)
                {
                    presentationScale.floatValue = 2.688f;
                    serializedIntegration.ApplyModifiedPropertiesWithoutUndo();
                }
            }

            SetActive("Room_Blockout/Props_Blockout/Sofa", false);
            SetActive("Room_Blockout/Props_Blockout/WindowPerch", false);
            SetActive("Room_Blockout/Props_Blockout/ShopTablet", false);
            SetActive("Room_Blockout/Props_Blockout/MemoryFrame", false);

            // Place the live feeding bowl in the foreground-right like the reference,
            // while hiding the old tall serving nook around it.
            Transform feedingNook = room.transform.Find("Props_Blockout/FeedingNook");
            Transform bowl = feedingNook == null ? null : feedingNook.Find("Bowl");
            if (feedingNook != null && bowl != null)
            {
                feedingNook.localPosition = new Vector3(2.42f, 0f, -1.68f);
                foreach (Renderer renderer in feedingNook.GetComponentsInChildren<Renderer>(true))
                    if (renderer.transform != bowl && !renderer.transform.IsChildOf(bowl)) renderer.enabled = false;
                foreach (Collider collider in feedingNook.GetComponentsInChildren<Collider>(true))
                    if (collider.transform != bowl && !collider.transform.IsChildOf(bowl)) collider.enabled = false;
            }
            Transform feedingZone = room.transform.Find("Zones/FeedingZone");
            if (feedingZone != null) feedingZone.localPosition = new Vector3(2.42f, 0f, -1.68f);
            Transform feedingApproach = room.transform.Find("InteractionPoints/FeedingApproach");
            if (feedingApproach != null) feedingApproach.localPosition = new Vector3(1.55f, 0f, -1.62f);

            GameObject navigation = GameObject.Find("Room_Blockout/Navigation");
            NavMeshSurface surface = navigation == null ? null : navigation.GetComponent<NavMeshSurface>();
            if (surface != null)
            {
                surface.BuildNavMesh();
            }

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("CatMe prototype room tuned: narrower, deeper, and cleared to interaction essentials.");
        }

        public static void ApplyFromCommandLine()
        {
            Apply();
        }

        public static void SetCatPresentationScaleFromCommandLine()
        {
            Scene scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            GameObject catRuntime = GameObject.Find("CatRuntime");
            HomeRoomCatIntegration integration = catRuntime == null
                ? null
                : catRuntime.GetComponent<HomeRoomCatIntegration>();
            if (!scene.IsValid() || integration == null)
            {
                Debug.LogError("Could not find HomeRoom or its CatRuntime integration.");
                EditorApplication.Exit(1);
                return;
            }

            SerializedObject serializedIntegration = new SerializedObject(integration);
            SerializedProperty presentationScale = serializedIntegration.FindProperty("presentationScale");
            if (presentationScale == null)
            {
                Debug.LogError("The cat presentation scale field is missing.");
                EditorApplication.Exit(1);
                return;
            }

            presentationScale.floatValue = 2.688f;
            serializedIntegration.ApplyModifiedPropertiesWithoutUndo();
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            AssetDatabase.SaveAssets();
            Debug.Log("[CatMe][Presentation] HomeRoom serialized cat presentation scale set to 2.688x (+20%).");
            EditorApplication.Exit(0);
        }

        private static void SetActive(string path, bool active)
        {
            GameObject item = GameObject.Find(path);
            if (item != null)
            {
                item.SetActive(active);
            }
        }
    }
}
