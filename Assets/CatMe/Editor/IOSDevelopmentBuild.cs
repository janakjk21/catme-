using System.IO;
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
        private const string GardenCompanionPath = "Assets/CatMe/Scenes/GardenCompanion.unity";
        private const string HomeGardenPath = "Assets/CatMe/Scenes/Places/Place_HomeGarden_r2.unity";
        private const string OutputPath = "Builds/iOS/CatMeConnectedJourney";
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
            // The Apple button is compiled in only for paid-team builds, matching the capabilities added after export.
            {
                var target = UnityEditor.Build.NamedBuildTarget.iOS;
                PlayerSettings.GetScriptingDefineSymbols(target, out string[] defines);
                var list = new System.Collections.Generic.List<string>(defines); list.Remove("WORNTAG_PAID_TEAM");
                if (System.Environment.GetEnvironmentVariable("WORNTAG_PAID_TEAM") == "1") list.Add("WORNTAG_PAID_TEAM");
                PlayerSettings.SetScriptingDefineSymbols(target, list.ToArray());
            }
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

            if (!PrepareRuntimeGltfShaders()) return;

            Scene scene = SceneManager.GetSceneByPath(GardenCompanionPath);
            if (!scene.IsValid() || !scene.isLoaded)
                scene = EditorSceneManager.OpenScene(GardenCompanionPath, OpenSceneMode.Single);
            if (!scene.IsValid())
            {
                Debug.LogError($"Could not open startup scene {GardenCompanionPath}.");
                return;
            }

            if (!File.Exists(HomeGardenPath))
            {
                Debug.LogError($"Current garden place scene is missing: {HomeGardenPath}");
                return;
            }

            // The shell loads the current garden additively. Omitting it silently starts the legacy route.
            string[] buildScenes = { GardenCompanionPath, HomeGardenPath };
            EditorBuildSettings.scenes = new[]
            {
                new EditorBuildSettingsScene(GardenCompanionPath, true),
                new EditorBuildSettingsScene(HomeGardenPath, true),
            };
            SetLandscapeOrientation();
            PlayerSettings.companyName = "CatMe";
            PlayerSettings.productName = "WornTag";
            PlayerSettings.SetApplicationIdentifier(NamedBuildTarget.iOS, "com.catme.home");
            // Signing: the Xcode project is regenerated on every build, so the team must come from the environment, not from a click in Xcode.
            // WORNTAG_APPLE_TEAM_ID is the 10-character team id (developer.apple.com > Membership details).
            string teamId = System.Environment.GetEnvironmentVariable("WORNTAG_APPLE_TEAM_ID");
            if (!string.IsNullOrWhiteSpace(teamId))
            {
                PlayerSettings.iOS.appleDeveloperTeamID = teamId.Trim();
                PlayerSettings.iOS.appleEnableAutomaticSigning = true;
            }
            else Debug.LogWarning("[WornTag][iOS] WORNTAG_APPLE_TEAM_ID is not set: choose the team in Xcode (Signing & Capabilities) before running on a phone.");
            PlayerSettings.SetScriptingBackend(NamedBuildTarget.iOS, ScriptingImplementation.IL2CPP);
            PlayerSettings.stripEngineCode = true;
            // Runtime imported animation and materials must remain available to glTFast on IL2CPP.
            PlayerSettings.SetManagedStrippingLevel(NamedBuildTarget.iOS, ManagedStrippingLevel.Low);
            PlayerSettings.SetIl2CppCompilerConfiguration(NamedBuildTarget.iOS, Il2CppCompilerConfiguration.Release);
            if (!int.TryParse(PlayerSettings.iOS.buildNumber, out int previousBuild)) previousBuild = 0;
            PlayerSettings.iOS.buildNumber = System.Math.Max(10, previousBuild + 1).ToString();
            Directory.CreateDirectory(Path.GetDirectoryName(outputPath));
            AssetDatabase.SaveAssets();

            BuildPlayerOptions options = new BuildPlayerOptions
            {
                scenes = buildScenes,
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

            Debug.Log($"[CatMe][iOS] Xcode development project exported | scenes=GardenCompanion,HomeGarden | build={PlayerSettings.iOS.buildNumber} | " +
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
            root.SetString("CFBundleDisplayName", "WornTag");
            // The selfie screen opens the front camera (CatMePhotoPicker.mm).
            root.SetString("NSCameraUsageDescription", "Take a selfie so we can make a little 3D you to walk with your pet.");
            // Google sign-in: the client id, and the reversed client id as the URL scheme Google returns to (from GoogleService-Info.plist).
            string firebasePlistPath = Path.Combine(Application.dataPath, "GoogleService-Info.plist");
            if (File.Exists(firebasePlistPath))
            {
                var firebasePlist = new PlistDocument(); firebasePlist.ReadFromFile(firebasePlistPath);
                string clientId = firebasePlist.root["CLIENT_ID"]?.AsString(), reversed = firebasePlist.root["REVERSED_CLIENT_ID"]?.AsString();
                if (!string.IsNullOrEmpty(clientId) && !string.IsNullOrEmpty(reversed))
                {
                    root.SetString("GIDClientID", clientId);
                    PlistElementArray urlTypes = root["CFBundleURLTypes"]?.AsArray() ?? root.CreateArray("CFBundleURLTypes");
                    bool present = false;   // Firebase's own build step usually adds it already
                    foreach (var type in urlTypes.values)
                        foreach (var scheme in type.AsDict()["CFBundleURLSchemes"]?.AsArray()?.values ?? new System.Collections.Generic.List<PlistElement>())
                            if (scheme.AsString() == reversed) present = true;
                    if (!present)
                    {
                        PlistElementDict googleType = urlTypes.AddDict();
                        googleType.SetString("CFBundleURLName", "google-sign-in");
                        googleType.CreateArray("CFBundleURLSchemes").AddString(reversed);
                    }
                }
                else Debug.LogWarning("[WornTag][iOS] GoogleService-Info.plist has no CLIENT_ID: Google sign-in will not work. Download it again from Firebase after enabling Google.");
            }
            // The WornTag API is HTTPS, so plain-HTTP access is only allowed for builds that talk to a local prototype server.
            if (System.Environment.GetEnvironmentVariable("WORNTAG_LOCAL_HTTP") == "1")
            {
                PlistElementDict transport = root.CreateDict("NSAppTransportSecurity");
                transport.SetBoolean("NSAllowsArbitraryLoads", true);
                root.SetString("NSLocalNetworkUsageDescription", "Connect to the WornTag prototype service on your local Wi-Fi.");
            }
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
            project.AddFrameworkToProject(target, "Vision.framework", false);
            project.AddFrameworkToProject(target, "AVFoundation.framework", false);
            // Unity already adds Assets/Plugins/iOS/CatMeAppleSignIn.mm to UnityFramework.
            // Adding a second copy here produces duplicate native symbols at link time.
            project.AddFrameworkToProject(target, "AuthenticationServices.framework", false);
            project.AddFrameworkToProject(target, "GameKit.framework", false);
            project.WriteToFile(projectPath);

            string mainTarget = project.GetUnityMainTargetGuid();
            string entitlements = project.GetBuildPropertyForAnyConfig(mainTarget, "CODE_SIGN_ENTITLEMENTS");
            if (string.IsNullOrWhiteSpace(entitlements)) entitlements = "CatMeHome.entitlements";
            // Sign in with Apple and Game Center need an App ID with those capabilities, which only a paid Apple Developer Program team can have.
            // A free personal team cannot provision them, and the build would fail to install if they were requested.
            if (System.Environment.GetEnvironmentVariable("WORNTAG_PAID_TEAM") == "1")
            {
                var capabilities = new ProjectCapabilityManager(projectPath, entitlements, targetGuid: mainTarget);
                capabilities.AddSignInWithApple();
                capabilities.AddGameCenter();
                capabilities.WriteToFile();
            }
            else Debug.LogWarning("[WornTag][iOS] WORNTAG_PAID_TEAM is not 1: Sign in with Apple and Game Center capabilities were NOT added. Email and guest sign-in still work.");
        }
    }
}
