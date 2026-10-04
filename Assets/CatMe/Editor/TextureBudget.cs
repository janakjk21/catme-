#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using UnityEditor;
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
                    int size = ui ? 2048 : sky ? 2048 : normal || hero || foliage ? 1024 : 512;
                    var format = ui ? TextureImporterFormat.ASTC_4x4 :
                                 size == 512 ? TextureImporterFormat.ASTC_8x8 : TextureImporterFormat.ASTC_6x6;
                    var settings = importer.GetPlatformTextureSettings("iPhone");
                    if (settings.overridden && settings.maxTextureSize == size && settings.format == format &&
                        !importer.isReadable && importer.mipmapEnabled == !ui &&
                        (ui || importer.streamingMipmaps)) continue;
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
    }
}
#endif
