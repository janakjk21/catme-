using System.IO;
using CatMe.Cat;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEditor.SceneManagement;
using UnityEditor.iOS.Xcode;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace CatMe.Editor
{
    /// <summary>Stages the local development cat and exports an unsigned iOS Xcode project.</summary>
    public static class IOSDevelopmentBuild
    {
        private const string FixtureRelativePath = "LocalFixtures/Cats/master-walking-cat.glb";
        private const string PackagedCatRelativePath = "Assets/StreamingAssets/Cats/master-walking-cat.glb";
        private const string HomeRoomPath = "Assets/CatMe/Scenes/HomeRoom.unity";
        private const string OutputPath = "Builds/iOS/CatMeHomeOptimized";
        private const string GltfShaderFolder = "Packages/com.unity.cloud.gltfast/Runtime/Shader/";
        private const string GltfMaterialFolder = "Assets/CatMe/Resources/GltfRuntimeShaders";

        [MenuItem("CatMe/Set Landscape Orientation")]
        public static void SetLandscapeOrientation()
        {
            PlayerSettings.defaultInterfaceOrientation = UIOrientation.LandscapeLeft;
            PlayerSettings.allowedAutorotateToPortrait = false;
            PlayerSettings.allowedAutorotateToPortraitUpsideDown = false;
            PlayerSettings.allowedAutorotateToLandscapeLeft = true;
            PlayerSettings.allowedAutorotateToLandscapeRight = true;
            AssetDatabase.SaveAssets();
        }

        [MenuItem("CatMe/Prepare Offline iOS Cat Asset")]
        public static void PrepareOfflineCat()
        {
            string fixture = Path.GetFullPath(Path.Combine(Application.dataPath, "..", FixtureRelativePath));
            if (!File.Exists(fixture))
            {
                Debug.LogError($"Known-good local cat fixture is missing: {fixture}");
                return;
            }

            string destination = Path.GetFullPath(Path.Combine(Application.dataPath, "..", PackagedCatRelativePath));
            Directory.CreateDirectory(Path.GetDirectoryName(destination));
            if (!File.Exists(destination) || new FileInfo(fixture).Length != new FileInfo(destination).Length ||
                File.GetLastWriteTimeUtc(fixture) > File.GetLastWriteTimeUtc(destination))
            {
                File.Copy(fixture, destination, true);
                Debug.Log($"[CatMe][iOS] Staged local cat for offline development build: {destination}");
            }
            else
            {
                Debug.Log("[CatMe][iOS] Offline cat package already matches the local fixture.");
            }
            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
        }

        private static bool PrepareRuntimeGltfShaders()
        {
            // glTFast resolves these by Shader.Find in a player. Without a serialized
            // reference Unity strips them and an otherwise valid GLB cannot instantiate.
            Directory.CreateDirectory(GltfMaterialFolder);
            string[] names =
            {
                "glTF-pbrMetallicRoughness",
                "glTF-pbrSpecularGlossiness",
                "glTF-unlit",
            };
            foreach (string name in names)
            {
                Shader shader = AssetDatabase.LoadAssetAtPath<Shader>(GltfShaderFolder + name + ".shadergraph");
                if (shader == null)
                {
                    Debug.LogError($"[CatMe][iOS] Required glTF shader is missing: {name}");
                    return false;
                }

                string materialPath = GltfMaterialFolder + "/" + name + ".mat";
                Material material = AssetDatabase.LoadAssetAtPath<Material>(materialPath);
                if (material == null)
                {
                    material = new Material(shader) { name = name };
                    AssetDatabase.CreateAsset(material, materialPath);
                }
                else if (material.shader != shader)
                {
                    material.shader = shader;
                    EditorUtility.SetDirty(material);
                }
            }
            AssetDatabase.SaveAssets();
            return true;
        }

        [MenuItem("CatMe/Build iOS Development Xcode Project")]
        public static void Build()
        {
            string requestedOutput = System.Environment.GetEnvironmentVariable("CATME_IOS_EXPORT_PATH");
            string outputPath = string.IsNullOrWhiteSpace(requestedOutput) ? OutputPath : requestedOutput;
            if (EditorApplication.isPlayingOrWillChangePlaymode)
            {
                Debug.LogError("Stop Play Mode before building the iOS Xcode project.");
                return;
            }
            if (!BuildPipeline.IsBuildTargetSupported(BuildTargetGroup.iOS, BuildTarget.iOS))
            {
                Debug.LogError("Unity iOS Build Support is not installed. Add the iOS module to Unity 6000.6.2f1 in Unity Hub.");
                return;
            }

            PrepareOfflineCat();
            if (!File.Exists(PackagedCatRelativePath)) return;
            if (!PrepareRuntimeGltfShaders()) return;

            Scene scene = SceneManager.GetSceneByPath(HomeRoomPath);
            if (!scene.IsValid() || !scene.isLoaded)
                scene = EditorSceneManager.OpenScene(HomeRoomPath, OpenSceneMode.Single);
            if (!scene.IsValid())
            {
                Debug.LogError($"Could not open startup scene {HomeRoomPath}.");
                return;
            }

            // Launch directly into the playable room; the current Bootstrap scene has no scene loader.
            EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(HomeRoomPath, true) };
            SetLandscapeOrientation();
            PlayerSettings.companyName = "CatMe";
            PlayerSettings.productName = "CatMe Home";
            PlayerSettings.SetApplicationIdentifier(NamedBuildTarget.iOS, "com.catme.home");
            PlayerSettings.SetScriptingBackend(NamedBuildTarget.iOS, ScriptingImplementation.IL2CPP);
            PlayerSettings.stripEngineCode = true;
            // Runtime imported animation and materials must remain available to glTFast on IL2CPP.
            PlayerSettings.SetManagedStrippingLevel(NamedBuildTarget.iOS, ManagedStrippingLevel.Low);
            PlayerSettings.SetIl2CppCompilerConfiguration(NamedBuildTarget.iOS, Il2CppCompilerConfiguration.Release);
            PlayerSettings.iOS.buildNumber = "1";
            Directory.CreateDirectory(Path.GetDirectoryName(outputPath));
            AssetDatabase.SaveAssets();

            BuildPlayerOptions options = new BuildPlayerOptions
            {
                scenes = new[] { HomeRoomPath },
                locationPathName = outputPath,
                target = BuildTarget.iOS,
                // Development keeps the in-app local generation panel available, while Release
                // IL2CPP and no debugger attachment keep the test binary compact enough to build.
                options = BuildOptions.Development,
            };
            BuildReport report = BuildPipeline.BuildPlayer(options);
            BuildSummary summary = report.summary;
            if (summary.result != BuildResult.Succeeded)
            {
                Debug.LogError($"[CatMe][iOS] Xcode export {summary.result} | errors={summary.totalErrors} | path={outputPath}");
                return;
            }

            Debug.Log($"[CatMe][iOS] Xcode development project exported | scenes=HomeRoom | " +
                $"bytes={summary.totalSize} | elapsed={summary.totalTime} | path={Path.GetFullPath(outputPath)}. " +
                "Open this project in Xcode, select a signing team, and install with the iPhone connected.");
            if (!Application.isBatchMode) EditorUtility.RevealInFinder(Path.GetFullPath(outputPath));
        }
    }

    /// <summary>Allows the local development app to reach the developer's LAN server over HTTP.</summary>
    public sealed class IOSDevelopmentNetworkPostprocessor : IPostprocessBuildWithReport
    {
        public int callbackOrder => 999;

        public void OnPostprocessBuild(BuildReport report)
        {
            if (report.summary.platform != BuildTarget.iOS || (report.summary.options & BuildOptions.Development) == 0)
            {
                return;
            }

            string plistPath = Path.Combine(report.summary.outputPath, "Info.plist");
            PlistDocument plist = new PlistDocument();
            plist.ReadFromFile(plistPath);
            PlistElementDict root = plist.root;
            PlistElementArray supportedOrientations = root.CreateArray("UISupportedInterfaceOrientations");
            supportedOrientations.AddString("UIInterfaceOrientationLandscapeLeft");
            supportedOrientations.AddString("UIInterfaceOrientationLandscapeRight");
            PlistElementDict transport = root.CreateDict("NSAppTransportSecurity");
            transport.SetBoolean("NSAllowsArbitraryLoads", true);
            root.SetString("NSLocalNetworkUsageDescription", "Connect to the CatMe companion service on your local Wi-Fi to create your cat and person for this prototype.");
            plist.WriteToFile(plistPath);

            string nativePickerSource = Path.Combine(Application.dataPath, "Plugins", "iOS", "CatMePhotoPicker.mm");
            string nativePickerRelativePath = "Classes/Native/CatMePhotoPicker.mm";
            string nativePickerDestination = Path.Combine(report.summary.outputPath, nativePickerRelativePath);
            Directory.CreateDirectory(Path.GetDirectoryName(nativePickerDestination));
            File.Copy(nativePickerSource, nativePickerDestination, true);

            string projectPath = PBXProject.GetPBXProjectPath(report.summary.outputPath);
            PBXProject project = new PBXProject();
            project.ReadFromFile(projectPath);
            string target = project.GetUnityFrameworkTargetGuid();
            string pickerFile = project.AddFile(nativePickerRelativePath, nativePickerRelativePath, PBXSourceTree.Source);
            project.AddFileToBuild(target, pickerFile);
            project.AddFrameworkToProject(target, "PhotosUI.framework", false);
            project.AddFrameworkToProject(target, "Photos.framework", false);
            project.WriteToFile(projectPath);
        }
    }
}
