#if UNITY_EDITOR
using System.IO;
using CatMe.Garden;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace CatMe.Editor
{
    public static class GardenCompanionSceneSetup
    {
        private const string ScenePath = "Assets/CatMe/Scenes/GardenCompanion.unity";

        [MenuItem("CatMe/Garden/Create Garden Demo Scene")]
        public static void CreateScene()
        {
            Directory.CreateDirectory(Path.GetDirectoryName(ScenePath));
            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            var root = new GameObject("GardenCompanionDemo");
            root.AddComponent<GardenCompanionDemo>();
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene, ScenePath);
            AssetDatabase.Refresh();
            Debug.Log("[CatMe][Garden] Saved standalone mobile garden scene at " + ScenePath);
        }

        [MenuItem("CatMe/Garden/Build Android APK")]
        public static void BuildAndroid()
        {
            CreateScene();
            BuildTarget target = BuildTarget.Android;
            EditorUserBuildSettings.SwitchActiveBuildTarget(BuildTargetGroup.Android, target);
            var options = new BuildPlayerOptions
            {
                scenes = new[] { ScenePath },
                locationPathName = "/tmp/CatMeGardenMobile.apk",
                target = target,
                options = BuildOptions.None,
            };
            BuildReport report = BuildPipeline.BuildPlayer(options);
            if (report.summary.result != BuildResult.Succeeded)
                throw new BuildFailedException("Garden Android build failed: " + report.summary.result);
            Debug.Log("[CatMe][Garden] Android APK ready at /tmp/CatMeGardenMobile.apk; size=" + report.summary.totalSize + " bytes.");
        }

        [MenuItem("CatMe/Garden/Export iOS Xcode Project")]
        public static void ExportIos()
        {
            CreateScene();
            BuildTarget target = BuildTarget.iOS;
            EditorUserBuildSettings.SwitchActiveBuildTarget(BuildTargetGroup.iOS, target);
            var options = new BuildPlayerOptions
            {
                scenes = new[] { ScenePath },
                locationPathName = "/tmp/CatMeGardenUploadFixiOS",
                target = target,
                options = BuildOptions.Development,
            };
            PlayerSettings.companyName = "CatMe";
            PlayerSettings.productName = "CatMe Home";
            PlayerSettings.SetApplicationIdentifier(UnityEditor.Build.NamedBuildTarget.iOS, "com.catme.home");
            PlayerSettings.iOS.buildNumber = "8";
            BuildReport report = BuildPipeline.BuildPlayer(options);
            if (report.summary.result != BuildResult.Succeeded)
                throw new BuildFailedException("Garden iOS export failed: " + report.summary.result);
            Debug.Log("[CatMe][Garden] iOS Xcode project exported at /tmp/CatMeGardenUploadFixiOS; size=" + report.summary.totalSize + " bytes.");
        }
    }
}
#endif
