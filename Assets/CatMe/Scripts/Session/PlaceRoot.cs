using System.Collections.Generic;
using System.Linq;
using CatMe.Garden;
using UnityEngine;

namespace CatMe.Session
{
    /// <summary>Root of a place scene. Holds the data SessionDirector needs; contains no gameplay logic.</summary>
    public sealed class PlaceRoot : MonoBehaviour
    {
        public string placeId = "HomeGarden";
        public string displayName = "";
        public string lightingPreset = "Morning";
        public string cameraProfile = "Default";
        public string ambienceResource = "";
        public float ambienceVolume = .3f;
        [Tooltip("Dry, Raining, or RainRamp (intensity grows between the two Z values).")]
        public string startWeather = "Dry";
        public float rainRampFromZ, rainRampToZ = 1f;
        public Collider shelterVolume;
        [Tooltip("Rain Particles pack instance (inactive in the scene); PlaceWeather drives it. Without it the code-built rain is used.")]
        public ParticleSystem rainEffect;
        public string clearedLightingPreset = "Sunset";
        public float shelterHoldSeconds = 10f;

        List<PlaceExit> exits;
        List<CameraZone> zones;

        public IReadOnlyList<PlaceExit> Exits => exits ??= GetComponentsInChildren<PlaceExit>(true).ToList();
        public IReadOnlyList<CameraZone> Zones => zones ??= GetComponentsInChildren<CameraZone>(true).ToList();

        public Transform FindEntry(string entryId)
        {
            Transform found = transform.Cast<Transform>().Select(t => t).FirstOrDefault(t => t.name == "Entry_" + entryId);
            if (found == null) found = GetComponentsInChildren<Transform>(true).FirstOrDefault(t => t.name == "Entry_" + entryId);
            if (found == null) found = GetComponentsInChildren<Transform>(true).FirstOrDefault(t => t.name.StartsWith("Entry_"));
            return found;
        }

        public Transform FindRecovery(Vector3 near)
        {
            return GetComponentsInChildren<Transform>(true).Where(t => t.name.StartsWith("Recovery_")).OrderBy(t => (t.position - near).sqrMagnitude).FirstOrDefault();
        }
    }
}
