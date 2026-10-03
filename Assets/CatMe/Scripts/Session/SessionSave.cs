using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace CatMe.Session
{
    /// <summary>Versioned local save for the place-based session. Written atomically; v1 journey snapshots migrate once.</summary>
    [Serializable]
    public sealed class SessionSaveData
    {
        public int schema = 2;
        public string placeId = "HomeGarden";
        public string entryId = "Start";
        public int sessionStep;
        public string weather = "Dry";
        public bool homecoming;
        public int completedSessions;
        public List<string> keepsakes = new List<string>();
    }

    public static class SessionSave
    {
        [Serializable] sealed class V1Snapshot { public int schema, mapRevision; public string checkpointId; public bool active, completed, rainStarted, sheltered, retreatReached; }

        static string Folder => Path.Combine(Application.persistentDataPath, "CatMeSession");
        static string FilePath => Path.Combine(Folder, "session-v2.json");
        static string LegacyPath => Path.Combine(Application.persistentDataPath, "CatMeJourney", "checkpoint-v1.json");

        public static SessionSaveData Load()
        {
            SessionSaveData data = Read(FilePath) ?? Read(FilePath + ".bak");
            if (data != null && data.schema == 2) return Sanitize(data);
            if (data != null && data.schema > 2) { Debug.LogWarning("[CatMe][Session] Save is from a newer version; starting at home without overwriting it."); return new SessionSaveData(); }
            return MigrateV1() ?? new SessionSaveData();
        }

        public static void Write(SessionSaveData data)
        {
            try
            {
                Directory.CreateDirectory(Folder);
                string pending = FilePath + ".pending", backup = FilePath + ".bak";
                File.WriteAllText(pending, JsonUtility.ToJson(data));
                if (File.Exists(FilePath)) File.Replace(pending, FilePath, backup);
                else File.Move(pending, FilePath);
            }
            catch (Exception error) { Debug.LogWarning("[CatMe][Session] Save failed: " + error.Message); }
        }

        static SessionSaveData Read(string path)
        {
            if (!File.Exists(path)) return null;
            try { return JsonUtility.FromJson<SessionSaveData>(File.ReadAllText(path)); }
            catch (Exception error) { Debug.LogWarning("[CatMe][Session] Invalid save: " + error.Message); return null; }
        }

        // Old journey checkpoints map to the nearest place; the v1 file is left in place.
        static SessionSaveData MigrateV1()
        {
            if (!File.Exists(LegacyPath)) return null;
            try
            {
                var old = JsonUtility.FromJson<V1Snapshot>(File.ReadAllText(LegacyPath));
                if (old == null || old.schema != 1) return null;
                var data = new SessionSaveData();
                switch (old.checkpointId)
                {
                    case "woodland": data.placeId = "Woodland"; data.entryId = "FromRiverside"; data.sessionStep = 2; break;
                    case "tent": case "shelter": data.placeId = "TentAndShelter"; data.entryId = "FromWoodland"; data.sessionStep = 3; break;
                    case "retreat": data.placeId = "MountainRetreat"; data.entryId = "FromShelter"; data.sessionStep = 4; break;
                    default: data.placeId = "HomeGarden"; data.entryId = "Start"; break;
                }
                if (old.completed) { data.placeId = "HomeGarden"; data.entryId = "Return"; data.homecoming = true; data.completedSessions = 1; data.sessionStep = 5; }
                return data;
            }
            catch (Exception error) { Debug.LogWarning("[CatMe][Session] Could not migrate journey snapshot: " + error.Message); return null; }
        }

        static SessionSaveData Sanitize(SessionSaveData data)
        {
            if (string.IsNullOrEmpty(data.placeId)) data.placeId = "HomeGarden";
            if (string.IsNullOrEmpty(data.entryId)) data.entryId = "Start";
            if (data.keepsakes == null) data.keepsakes = new List<string>();
            return data;
        }
    }
}
