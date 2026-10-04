#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.Build;
using UnityEngine;

namespace CatMe.Editor
{
    public static class TextureBudget
    {
        [MenuItem("CatMe/Optimise/Apply Texture Budget")]
        public static void ApplyTextures()
        {
            var paths = new HashSet<string>();
            foreach (var guid in AssetDatabase.FindAssets("t:Texture2D", new[] { "Assets" }))
                paths.Add(AssetDatabase.GUIDToAssetPath(guid));
            foreach (var guid in AssetDatabase.FindAssets("t:Cubemap", new[] { "Assets" }))
                paths.Add(AssetDatabase.GUIDToAssetPath(guid));

            int changed = 0;
            AssetDatabase.StartAssetEditing();
            try
            {
                foreach (var path in paths)
                {
                    var importer = AssetImporter.GetAtPath(path) as TextureImporter;
                    if (importer == null) continue;
                    string name = path.ToLowerInvariant();
                    bool ui = importer.textureType == TextureImporterType.Sprite ||
                              name.Contains("/ui/") || name.Contains("/onboarding/") || name.Contains("/icons/");
                    bool normal = importer.textureType == TextureImporterType.NormalMap ||
                                  name.Contains("normal") || name.Contains("_nrm") || name.EndsWith("_n.png");
                    bool sky = name.Contains("skybox") || name.Contains("/sky/");
                    bool hero = name.Contains("country house") || name.Contains("torii") || name.Contains("path");
                    bool foliage = name.Contains("rock") || name.Contains("tree") || name.Contains("foliage") || name.Contains("grass");
                    int size = ui ? importer.maxTextureSize : sky ? 2048 : normal || hero || foliage ? 1024 : 512;
                    var format = ui ? TextureImporterFormat.ASTC_4x4 :
                                 size == 512 ? TextureImporterFormat.ASTC_8x8 : TextureImporterFormat.ASTC_6x6;
                    var settings = importer.GetPlatformTextureSettings("iPhone");
                    bool changeType = normal && !ui && importer.textureType != TextureImporterType.NormalMap;
                    if (settings.overridden && settings.maxTextureSize == size && settings.format == format &&
                        !importer.isReadable && importer.mipmapEnabled == !ui &&
                        (ui || importer.streamingMipmaps) && !changeType) continue;
                    if (changeType) importer.textureType = TextureImporterType.NormalMap;
                    settings.name = "iPhone";
                    settings.overridden = true;
                    settings.maxTextureSize = size;
                    settings.format = format;
                    importer.SetPlatformTextureSettings(settings);
                    importer.isReadable = false;
                    importer.mipmapEnabled = !ui;
                    importer.streamingMipmaps = !ui;
                    importer.SaveAndReimport();
                    changed++;
                }
            }
            finally { AssetDatabase.StopAssetEditing(); }
            QualitySettings.streamingMipmapsMemoryBudget = 256f;
            Debug.Log($"[CatMe][Optimise] Updated {changed} texture importers; iPhone streaming budget 256 MB.");
        }

        [MenuItem("CatMe/Optimise/Apply Model Budget")]
        public static void ApplyModels()
        {
            int changed = 0;
            foreach (var guid in AssetDatabase.FindAssets("t:Model", new[] { "Assets" }))
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                if (!path.EndsWith(".fbx", StringComparison.OrdinalIgnoreCase) &&
                    !path.EndsWith(".obj", StringComparison.OrdinalIgnoreCase)) continue;
                var importer = AssetImporter.GetAtPath(path) as ModelImporter;
                if (importer == null) continue;
                bool staticArt = path.Contains("/JTS_Shrine_") || path.Contains("/ALP_Assets/") ||
                                 path.Contains("/Waldemarst/") || path.Contains("/Anime Trees/") ||
                                 path.Contains("/Free/Stones/") || path.Contains("/LowPolyLivingRoomPack/") ||
                                 path.Contains("/3D set of stylized nature");
                bool dirty = importer.isReadable || importer.meshCompression != ModelImporterMeshCompression.Medium ||
                             !importer.optimizeMeshPolygons || !importer.optimizeMeshVertices ||
                             importer.importCameras || importer.importLights ||
                             (staticArt && importer.importBlendShapes) ||
                             importer.animationCompression != ModelImporterAnimationCompression.Optimal;
                if (dirty)
                {
                    importer.isReadable = false;
                    importer.meshCompression = ModelImporterMeshCompression.Medium;
                    importer.optimizeMeshPolygons = true;
                    importer.optimizeMeshVertices = true;
                    importer.importCameras = false;
                    importer.importLights = false;
                    if (staticArt) importer.importBlendShapes = false;
                    importer.animationCompression = ModelImporterAnimationCompression.Optimal;
                    importer.SaveAndReimport();
                    changed++;
                }
                foreach (var asset in AssetDatabase.LoadAllAssetsAtPath(path))
                {
                    if (!(asset is Mesh mesh)) continue;
                    ulong triangles = 0;
                    for (int submesh = 0; submesh < mesh.subMeshCount; submesh++)
                        if (mesh.GetTopology(submesh) == MeshTopology.Triangles)
                            triangles += mesh.GetIndexCount(submesh) / 3;
                    if (triangles > 50000)
                        Debug.LogWarning($"[CatMe][Optimise] {path}: {mesh.name} has {triangles} triangles.");
                }
            }
            Debug.Log($"[CatMe][Optimise] Updated {changed} model importers. Animation clips were retained for Play Mode review.");
        }

        [MenuItem("CatMe/Optimise/Apply Audio Budget")]
        public static void ApplyAudio()
        {
            int changed = 0;
            foreach (var guid in AssetDatabase.FindAssets("t:AudioClip", new[] { "Assets" }))
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                var importer = AssetImporter.GetAtPath(path) as AudioImporter;
                var clip = AssetDatabase.LoadAssetAtPath<AudioClip>(path);
                if (importer == null || clip == null) continue;
                string lower = path.ToLowerInvariant();
                bool ambience = clip.length > 5f || lower.Contains("ambient") || lower.Contains("amb_") ||
                                lower.Contains("wind-") || lower.Contains("rain-") || lower.Contains("river-") ||
                                lower.Contains("music");
                bool stereo = lower.Contains("music") || lower.Contains("day-ambient") || lower.Contains("amb_morning");
                var settings = importer.defaultSampleSettings;
                settings.loadType = ambience ? AudioClipLoadType.Streaming : AudioClipLoadType.DecompressOnLoad;
                settings.compressionFormat = ambience ? AudioCompressionFormat.Vorbis : AudioCompressionFormat.ADPCM;
                settings.quality = ambience ? .5f : 1f;
                settings.sampleRateSetting = AudioSampleRateSetting.OverrideSampleRate;
                settings.sampleRateOverride = (uint)Math.Min(clip.frequency, 44100);
                if (settings.loadType == importer.defaultSampleSettings.loadType &&
                    settings.compressionFormat == importer.defaultSampleSettings.compressionFormat &&
                    settings.sampleRateOverride == importer.defaultSampleSettings.sampleRateOverride &&
                    importer.forceToMono == !stereo) continue;
                importer.forceToMono = !stereo;
                importer.defaultSampleSettings = settings;
                importer.SaveAndReimport();
                changed++;
            }
            Debug.Log($"[CatMe][Optimise] Updated {changed} audio importers.");
        }

        [MenuItem("CatMe/Optimise/Apply iPhone Build Budget")]
        public static void ApplyBuild()
        {
            PlayerSettings.SetScriptingBackend(NamedBuildTarget.iOS, ScriptingImplementation.IL2CPP);
            PlayerSettings.SetManagedStrippingLevel(NamedBuildTarget.iOS, ManagedStrippingLevel.High);
            PlayerSettings.stripEngineCode = true;
            Debug.Log("[CatMe][Optimise] iPhone: IL2CPP, High managed stripping, engine code stripping on. Build and test all flows before shipping.");
        }
    }
}
#endif
