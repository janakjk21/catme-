#if UNITY_EDITOR
using System;
using System.IO;
using CatMe.Session;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace CatMe.Editor
{
    /// <summary>One-off scene edits applied to the saved place scene without a full rebuild (a rebuild would re-scatter the approved layout).</summary>
    public static class PlaceFix
    {
        /// <summary>Removes the bench and its Sit prompt inside the garden walls (near 4.8, 5).</summary>
        public static void RemoveGardenBench()
        {
            try
            {
                string path = SessionDirector.ScenePath("HomeGarden");
                var scene = EditorSceneManager.OpenScene(path, OpenSceneMode.Single);
                var centre = new Vector3(4.8f, 0f, 5f); int removed = 0;
                foreach (var root in scene.GetRootGameObjects())
                    foreach (var t in root.GetComponentsInChildren<Transform>(true))
                    {
                        if (t == null || (t.name != "Bench" && t.name != "Interact_Bench")) continue;
                        Vector3 p = t.position; p.y = 0f;
                        if (Vector3.Distance(p, centre) > 2.5f) continue;
                        UnityEngine.Object.DestroyImmediate(t.gameObject); removed++;
                    }
                Debug.Log("[CatMe][PlaceFix] Removed " + removed + " garden bench objects.");
                EditorSceneManager.MarkSceneDirty(scene); EditorSceneManager.SaveScene(scene);
                EditorApplication.Exit(removed > 0 ? 0 : 2);
            }
            catch (Exception e) { File.WriteAllText("/tmp/catme-capture-error.txt", e.ToString()); EditorApplication.Exit(1); }
        }
    }
}
#endif
