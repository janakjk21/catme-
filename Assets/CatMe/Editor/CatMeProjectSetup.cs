using System.IO;
using CatMe.Core;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace CatMe.Editor
{
    public static class CatMeProjectSetup
    {
        private const string ScenesFolder = "Assets/CatMe/Scenes";
        private const string BootstrapScene = ScenesFolder + "/Bootstrap.unity";
        private const string ModelTestScene = ScenesFolder + "/ModelTest.unity";
        private const string HomeRoomScene = ScenesFolder + "/HomeRoom.unity";
        private const string RenderingFolder = "Assets/CatMe/Settings";
        private const string RendererAssetPath = RenderingFolder + "/CatMeMobileRenderer.asset";
        private const string PipelineAssetPath = RenderingFolder + "/CatMeMobilePipeline.asset";

        [MenuItem("CatMe/Configure Project")]
        public static void Configure()
        {
            ConfigurePlayer();
            ConfigureRendering();
            Directory.CreateDirectory(ScenesFolder);

            CreateScene(BootstrapScene, SceneKind.Bootstrap);
            CreateScene(ModelTestScene, SceneKind.ModelTest);
            CreateScene(HomeRoomScene, SceneKind.HomeRoom);

            EditorBuildSettings.scenes = new[]
            {
                new EditorBuildSettingsScene(BootstrapScene, true),
                new EditorBuildSettingsScene(HomeRoomScene, true),
                new EditorBuildSettingsScene(ModelTestScene, false),
            };

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            EditorSceneManager.OpenScene(ModelTestScene);
            Debug.Log("CatMe Home project configured successfully.");
        }

        // Entry point used by the command-line verification step.
        public static void ConfigureFromCommandLine()
        {
            Configure();
        }

        private static void ConfigurePlayer()
        {
            PlayerSettings.companyName = "CatMe";
            PlayerSettings.productName = "CatMe Home";
            PlayerSettings.bundleVersion = "0.1.0";
            PlayerSettings.colorSpace = ColorSpace.Linear;
            PlayerSettings.defaultInterfaceOrientation = UIOrientation.Portrait;
            PlayerSettings.allowedAutorotateToPortrait = false;
            PlayerSettings.allowedAutorotateToPortraitUpsideDown = false;
            PlayerSettings.allowedAutorotateToLandscapeLeft = false;
            PlayerSettings.allowedAutorotateToLandscapeRight = false;
            PlayerSettings.SetApplicationIdentifier(NamedBuildTarget.Android, "com.catme.home");
            PlayerSettings.SetApplicationIdentifier(NamedBuildTarget.iOS, "com.catme.home");
            PlayerSettings.Android.bundleVersionCode = 1;
            PlayerSettings.iOS.buildNumber = "1";

            QualitySettings.vSyncCount = 0;
        }

        private static void ConfigureRendering()
        {
            Directory.CreateDirectory(RenderingFolder);

            UniversalRendererData rendererData = AssetDatabase.LoadAssetAtPath<UniversalRendererData>(RendererAssetPath);
            if (rendererData == null)
            {
                rendererData = ScriptableObject.CreateInstance<UniversalRendererData>();
                AssetDatabase.CreateAsset(rendererData, RendererAssetPath);
            }

            UniversalRenderPipelineAsset pipeline = AssetDatabase.LoadAssetAtPath<UniversalRenderPipelineAsset>(PipelineAssetPath);
            if (pipeline == null)
            {
                pipeline = UniversalRenderPipelineAsset.Create(rendererData);
                AssetDatabase.CreateAsset(pipeline, PipelineAssetPath);
            }

            GraphicsSettings.defaultRenderPipeline = pipeline;
            QualitySettings.renderPipeline = pipeline;
        }

        private static void CreateScene(string path, SceneKind kind)
        {
            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            scene.name = Path.GetFileNameWithoutExtension(path);

            GameObject systems = new GameObject("[CatMe] Systems");
            systems.AddComponent<MobileRuntimeSettings>();

            if (kind != SceneKind.Bootstrap)
            {
                CreateCamera();
                CreateLighting();

                GameObject environment = new GameObject("Environment");
                GameObject catSpawn = new GameObject("CatSpawn");
                catSpawn.transform.SetPositionAndRotation(Vector3.zero, Quaternion.identity);

                GameObject interactionPoints = new GameObject("InteractionPoints");
                interactionPoints.transform.SetParent(environment.transform);
            }

            if (kind == SceneKind.HomeRoom)
            {
                new GameObject("RoomRuntime");
            }
            else if (kind == SceneKind.ModelTest)
            {
                new GameObject("ModelUnderTest");
            }

            EditorSceneManager.SaveScene(scene, path);
        }

        private static void CreateCamera()
        {
            GameObject cameraObject = new GameObject("Main Camera");
            Camera camera = cameraObject.AddComponent<Camera>();
            camera.tag = "MainCamera";
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(0.82f, 0.84f, 0.87f);
            camera.nearClipPlane = 0.05f;
            camera.farClipPlane = 100f;
            camera.fieldOfView = 42f;
            cameraObject.transform.SetPositionAndRotation(
                new Vector3(0f, 1.8f, -4.5f),
                Quaternion.Euler(14f, 0f, 0f));
            cameraObject.AddComponent<AudioListener>();
        }

        private static void CreateLighting()
        {
            GameObject lightObject = new GameObject("Sun");
            Light light = lightObject.AddComponent<Light>();
            light.type = LightType.Directional;
            light.intensity = 1.1f;
            light.color = new Color(1f, 0.94f, 0.86f);
            light.shadows = LightShadows.Soft;
            lightObject.transform.rotation = Quaternion.Euler(48f, -32f, 0f);
        }

        private enum SceneKind
        {
            Bootstrap,
            ModelTest,
            HomeRoom,
        }
    }
}
