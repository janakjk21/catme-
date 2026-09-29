using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;
using UnityEngine;

namespace CatMe.Cat
{
    [Serializable]
    public sealed class CatCompanionProfileData
    {
        public int schemaVersion = 1;
        public string catIdentity = string.Empty;
        public float playfulness = 0.62f;
        public float curiosity = 0.58f;
        public float affection = 0.54f;
        public float confidence = 0.50f;
        public string[] discoveries = Array.Empty<string>();
    }

    /// <summary>Stores neutral local traits and one-time discoveries without reading the cat photo.</summary>
    [DisallowMultipleComponent]
    public sealed class CatCompanionJournal : MonoBehaviour
    {
        private const int SchemaVersion = 2;
        private CatCompanionProfileData profile;
        private CatSleepInteraction sleep;
        private string savePath;
        private string catIdentity;

        public float Playfulness => profile == null ? 0.62f : profile.playfulness;
        public float Curiosity => profile == null ? 0.58f : profile.curiosity;
        public float Affection => profile == null ? 0.54f : profile.affection;
        public float Confidence => profile == null ? 0.50f : profile.confidence;
        public IReadOnlyList<string> Discoveries => profile == null ? Array.Empty<string>() : profile.discoveries;
        public string SavePath => savePath;

        public void Initialize(CatSleepInteraction readySleep = null, string catAssetPath = null)
        {
            if (readySleep != null && sleep == null)
            {
                sleep = readySleep;
                sleep.StateChanged += OnSleepStateChanged;
            }
            if (profile != null) return;
            catIdentity = CreateCatIdentity(catAssetPath);
            savePath = Path.Combine(Application.persistentDataPath, "catme-companion-" + catIdentity + ".json");
            try
            {
                if (File.Exists(savePath))
                {
                    profile = JsonUtility.FromJson<CatCompanionProfileData>(File.ReadAllText(savePath));
                }
            }
            catch (Exception exception)
            {
                Debug.LogWarning("[CatMe][Journal] Profile load failed safely: " + exception.GetType().Name);
            }

            if (profile == null || profile.schemaVersion != SchemaVersion || profile.catIdentity != catIdentity)
            {
                // Neutral defaults are explicit. Traits are not inferred from the uploaded image.
                profile = new CatCompanionProfileData { schemaVersion = SchemaVersion, catIdentity = catIdentity };
                Save();
            }
            else
            {
                profile.playfulness = ClampTrait(profile.playfulness, 0.62f);
                profile.curiosity = ClampTrait(profile.curiosity, 0.58f);
                profile.affection = ClampTrait(profile.affection, 0.54f);
                profile.confidence = ClampTrait(profile.confidence, 0.50f);
                if (profile.discoveries == null) profile.discoveries = Array.Empty<string>();
            }
        }

        public bool RecordDiscovery(string key)
        {
            if (string.IsNullOrWhiteSpace(key)) return false;
            if (profile == null) Initialize();
            foreach (string existing in profile.discoveries)
            {
                if (string.Equals(existing, key, StringComparison.Ordinal)) return false;
            }
            var discoveries = new List<string>(profile.discoveries) { key };
            if (discoveries.Count > 24) discoveries.RemoveAt(0);
            profile.discoveries = discoveries.ToArray();
            Save();
            Debug.Log("[CatMe][Journal] Discovery recorded | " + key);
            return true;
        }

        public bool Save()
        {
            if (profile == null || string.IsNullOrEmpty(savePath)) return false;
            string temporary = savePath + ".tmp";
            try
            {
                string directory = Path.GetDirectoryName(savePath);
                if (!string.IsNullOrEmpty(directory)) Directory.CreateDirectory(directory);
                File.WriteAllText(temporary, JsonUtility.ToJson(profile, true));
                if (File.Exists(savePath)) File.Replace(temporary, savePath, null);
                else File.Move(temporary, savePath);
                return true;
            }
            catch (Exception exception)
            {
                Debug.LogWarning("[CatMe][Journal] Profile save failed safely: " + exception.GetType().Name);
                return false;
            }
        }

        private void OnApplicationPause(bool paused) { if (paused) Save(); }
        private void OnApplicationQuit() { Save(); }

        private void OnSleepStateChanged(CatSleepInteraction.SleepState state)
        {
            if (state == CatSleepInteraction.SleepState.SleepingHidden) RecordDiscovery("first_home_rest");
        }

        private void OnDestroy()
        {
            if (sleep != null) sleep.StateChanged -= OnSleepStateChanged;
        }

        private static float ClampTrait(float value, float fallback)
        {
            return float.IsNaN(value) || float.IsInfinity(value) ? fallback : Mathf.Clamp01(value);
        }

        private static string CreateCatIdentity(string assetPath)
        {
            if (!string.IsNullOrEmpty(assetPath) && File.Exists(assetPath))
            {
                try
                {
                    using (SHA256 hash = SHA256.Create())
                    using (FileStream stream = File.OpenRead(assetPath))
                    {
                        byte[] digest = hash.ComputeHash(stream);
                        return BitConverter.ToString(digest, 0, 8).Replace("-", string.Empty).ToLowerInvariant();
                    }
                }
                catch (Exception exception)
                {
                    Debug.LogWarning("[CatMe][Journal] Cat identity hash failed safely: " + exception.GetType().Name);
                }
            }

            string fallback = Path.GetFileNameWithoutExtension(assetPath ?? "local-cat").ToLowerInvariant();
            string identity = string.Empty;
            foreach (char character in fallback)
                if (char.IsLetterOrDigit(character)) identity += character;
            return string.IsNullOrEmpty(identity) ? "local-cat" : identity;
        }
    }
}
