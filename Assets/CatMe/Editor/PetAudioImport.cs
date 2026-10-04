using UnityEditor;
using UnityEngine;

namespace CatMe.EditorTools
{
    /// <summary>Import settings for pet sounds (docs/PET_AUDIO.md section 2), so nobody sets them by hand.</summary>
    public sealed class PetAudioImport : AssetPostprocessor
    {
        void OnPreprocessAudio()
        {
            string path = assetPath.Replace('\\', '/');
            var importer = (AudioImporter)assetImporter;
            if (path.Contains("/CatMeAudio/Cat/") || path.Contains("/CatMeAudio/Dog/"))
            {
                importer.forceToMono = true;
                importer.loadInBackground = false;
                var settings = importer.defaultSampleSettings;
                settings.loadType = AudioClipLoadType.DecompressOnLoad;
                settings.compressionFormat = AudioCompressionFormat.ADPCM;
                settings.preloadAudioData = true;
                importer.defaultSampleSettings = settings;
            }
            else if (path.EndsWith("/CatMeAudio/cat-purr.mp3"))
            {
                var settings = importer.defaultSampleSettings;
                settings.loadType = AudioClipLoadType.CompressedInMemory;
                settings.compressionFormat = AudioCompressionFormat.Vorbis;
                settings.quality = .6f;
                importer.defaultSampleSettings = settings;
            }
        }
    }
}
