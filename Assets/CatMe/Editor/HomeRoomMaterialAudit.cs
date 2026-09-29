using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace CatMe.Editor
{
    /// <summary>Read-only report of the materials actually assigned to HomeRoom renderers.</summary>
    public static class HomeRoomMaterialAudit
    {
        [MenuItem("CatMe/Room Authoring/Report HomeRoom Materials")]
        public static void Report()
        {
            EditorSceneManager.OpenScene("Assets/CatMe/Scenes/HomeRoom.unity", OpenSceneMode.Single);
            Renderer[] renderers = Object.FindObjectsByType<Renderer>();
            StringBuilder report = new StringBuilder("[CatMe][MaterialAudit] HomeRoom assigned materials:");
            foreach (Renderer renderer in renderers)
            {
                if (renderer == null || renderer.sharedMaterials == null) continue;
                foreach (Material material in renderer.sharedMaterials)
                {
                    if (material == null) continue;
                    Color color = material.HasProperty("_BaseColor") ? material.GetColor("_BaseColor") :
                        material.HasProperty("_Color") ? material.GetColor("_Color") : Color.white;
                    report.Append("\n").Append(renderer.name).Append(" -> ").Append(material.name)
                        .Append(" | ").Append(AssetDatabase.GetAssetPath(material))
                        .Append(" | tint=").Append(color);
                }
            }
            Debug.Log(report.ToString());
            if (Application.isBatchMode) EditorApplication.Exit(0);
        }
    }
}
