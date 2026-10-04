using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace CatMe.EditorTools
{
    /// <summary>
    /// F8: the owner drops a crouch / pour animation (for example Mixamo "Kneeling", "Picking Up" or "Pouring", FBX "without skin")
    /// into Assets/CatMe/Art/Animations/. The garden's person is a glTF model played with Legacy Animation, so the clip is imported
    /// as Legacy and copied to Resources/CatMeAnimations/OwnerPour.anim, which GardenCompanionDemo.PlayOwnerPour loads.
    /// It only moves the bones if the clip's bone names match the bundled person (a Mixamo rig); otherwise nothing plays.
    /// </summary>
    public sealed class OwnerAnimationImport : AssetPostprocessor
    {
        const string Folder = "Assets/CatMe/Art/Animations/";
        const string Output = "Assets/CatMe/Resources/CatMeAnimations/OwnerPour.anim";

        void OnPreprocessModel()
        {
            if (!assetPath.Replace('\\', '/').StartsWith(Folder)) return;
            var importer = (ModelImporter)assetImporter;
            importer.animationType = ModelImporterAnimationType.Legacy;
            importer.importAnimation = true;
        }

        static void OnPostprocessAllAssets(string[] imported, string[] deleted, string[] moved, string[] movedFrom)
        {
            foreach (string path in imported)
            {
                if (!path.StartsWith(Folder) || !path.ToLowerInvariant().EndsWith(".fbx")) continue;
                var clip = AssetDatabase.LoadAllAssetsAtPath(path).OfType<AnimationClip>().FirstOrDefault(c => !c.name.StartsWith("__preview__"));
                if (clip == null) continue;
                Directory.CreateDirectory(Path.GetDirectoryName(Output));
                var copy = Object.Instantiate(clip); copy.name = "OwnerPour"; copy.legacy = true;
                AssetDatabase.DeleteAsset(Output);
                AssetDatabase.CreateAsset(copy, Output);
                Debug.Log("[CatMe][Animations] Owner pour clip made from " + path + " (" + clip.length.ToString("F2") + " s).");
            }
        }
    }
}
