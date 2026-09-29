using System;
using System.IO;
using CatMe.Cat;
using UnityEngine;

namespace CatMe.Save
{
    /// <summary>Versioned atomic local save for stable CatMe return state.</summary>
    [DisallowMultipleComponent]
    public sealed class CatLocalSave : MonoBehaviour
    {
        private const int CurrentSchemaVersion = 1;
        private const float OrdinaryWriteDebounceSeconds = 1f;
        private CatEnergy energy;
        private CatSleepInteraction sleep;
        private string savePath;
        private string temporaryPath;
        private float debounce;
        private float lastSavedEnergy = -1f;
        private string lastSavedState;
        private bool initialized;

        /// <summary>Editor validators may inject isolated storage without changing the player path.</summary>
        public static string ValidationPathOverride { get; set; }

        public bool SaveWasLoaded { get; private set; }
        public bool SaveDefaulted { get; private set; }
        public int SuccessfulSaveCount { get; private set; }
        public int SuccessfulLoadCount { get; private set; }
        public int CorruptSaveFallbackCount { get; private set; }
        public float OfflineEnergyRecovered { get; private set; }
        public string RestoredStableState { get; private set; } = "Awake";
        public string SavePath => savePath;

        public void Initialize(CatEnergy readyEnergy, CatSleepInteraction readySleep, string isolatedPath = null)
        {
            if (initialized || readyEnergy == null || readySleep == null) return;
            initialized = true;
            energy = readyEnergy;
            sleep = readySleep;
            savePath = string.IsNullOrEmpty(isolatedPath) ? Path.Combine(Application.persistentDataPath, "catme-save.json") : isolatedPath;
            temporaryPath = savePath + ".tmp";
            Load();
            sleep.StateChanged += OnSleepStateChanged;
        }

        private void Update()
        {
            if (!initialized) return;
            debounce += Time.unscaledDeltaTime;
            string state = GetStableState();
            if (debounce >= OrdinaryWriteDebounceSeconds && (Mathf.Abs(lastSavedEnergy - energy.CurrentEnergy) > 0.01f || state != lastSavedState))
                Save();
        }

        private void OnSleepStateChanged(CatSleepInteraction.SleepState state)
        {
            if (state == CatSleepInteraction.SleepState.SleepingHidden || state == CatSleepInteraction.SleepState.AwakeIdle) Save();
        }

        public bool Save()
        {
            if (!initialized || string.IsNullOrEmpty(savePath)) return false;
            CatSaveData data = new CatSaveData
            {
                schemaVersion = CurrentSchemaVersion,
                energy = Mathf.Clamp(energy.CurrentEnergy, 0f, CatEnergy.MaximumEnergy),
                stableState = GetStableState(),
                savedAtUtc = DateTimeOffset.UtcNow.ToUnixTimeSeconds()
            };
            try
            {
                string json = JsonUtility.ToJson(data, true);
                string directory = Path.GetDirectoryName(savePath);
                if (!string.IsNullOrEmpty(directory)) Directory.CreateDirectory(directory);
                File.WriteAllText(temporaryPath, json);
                if (File.Exists(savePath)) File.Replace(temporaryPath, savePath, null);
                else File.Move(temporaryPath, savePath);
                lastSavedEnergy = data.energy; lastSavedState = data.stableState; debounce = 0f; SuccessfulSaveCount++;
                return true;
            }
            catch (Exception exception)
            {
                Debug.LogWarning($"[CatMe][Save] Save failed safely: {exception.GetType().Name}");
                return false;
            }
        }

        private void Load()
        {
            CatSaveData data = null;
            try
            {
                if (!File.Exists(savePath)) { SaveDefaulted = true; ApplyDefault(); return; }
                data = JsonUtility.FromJson<CatSaveData>(File.ReadAllText(savePath));
                if (!IsValid(data)) throw new InvalidDataException("invalid save");
            }
            catch (Exception exception)
            {
                CorruptSaveFallbackCount++;
                SaveDefaulted = true;
                Debug.LogWarning($"[CatMe][Save] Falling back to default save: {exception.GetType().Name}");
                ApplyDefault();
                return;
            }

            SaveWasLoaded = true; SuccessfulLoadCount++;
            long now = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            float offline = 0f;
            if (data.stableState == "Sleeping")
            {
                long elapsed = Math.Max(0L, now - data.savedAtUtc);
                offline = Mathf.Min(50f, elapsed * CatEnergy.SleepRecoveryPerSecond);
                data.energy = Mathf.Clamp(data.energy + offline, 0f, CatEnergy.MaximumEnergy);
            }
            OfflineEnergyRecovered = offline;
            RestoredStableState = data.stableState;
            energy.ApplyLoadedState(data.energy, data.stableState == "Sleeping");
            lastSavedEnergy = energy.CurrentEnergy; lastSavedState = GetStableState();
        }

        private void ApplyDefault()
        {
            RestoredStableState = "Awake";
            energy.ApplyLoadedState(CatEnergy.StartingEnergy, false);
            lastSavedEnergy = energy.CurrentEnergy; lastSavedState = "Awake";
        }

        private string GetStableState()
        {
            // Persist only stable checkpoints. A house walk or entry transition
            // must return as Awake after relaunch rather than restoring a
            // half-completed transition.
            return sleep != null && sleep.CurrentState == CatSleepInteraction.SleepState.SleepingHidden
                ? "Sleeping"
                : "Awake";
        }

        private static bool IsValid(CatSaveData data)
        {
            return data != null && data.schemaVersion == CurrentSchemaVersion &&
                !float.IsNaN(data.energy) && !float.IsInfinity(data.energy) && data.energy >= 0f && data.energy <= CatEnergy.MaximumEnergy &&
                (data.stableState == "Awake" || data.stableState == "Sleeping") && data.savedAtUtc >= 0;
        }

        private void OnApplicationPause(bool paused) { if (paused) Save(); }
        private void OnApplicationQuit() { Save(); }

        private void OnDestroy()
        {
            if (sleep != null) sleep.StateChanged -= OnSleepStateChanged;
        }
    }
}
