using System.Collections.Generic;
using UnityEngine;

namespace CatMe.Garden
{
    /// <summary>
    /// A camera profile for while the owner stands inside this trigger volume. Polled by SessionDirector, which uses one zone at
    /// a time (<see cref="CameraZonePicker"/>): the highest <see cref="priority"/>, then the smallest box, with a 1 m margin so
    /// walking along an edge doesn't flip the camera back and forth.
    /// </summary>
    public sealed class CameraZone : MonoBehaviour
    {
        public string profileName = "Default";
        public float blendIn = 1.5f;
        [Tooltip("Higher wins where zones overlap. Equal priority: the smaller zone wins.")]
        public int priority;
        CameraProfile profile;
        public CameraProfile Profile
        {
            get
            {
                if (profile == null) { profile = CameraProfile.Named(profileName); profile.blendIn = blendIn; }
                return profile;
            }
        }
        public bool Contains(Vector3 point) => Contains(point, 0f);

        /// <summary>Inside the box grown (positive margin) or shrunk (negative margin) by this many metres on every side.</summary>
        public bool Contains(Vector3 point, float margin)
        {
            var box = GetComponent<Collider>();
            if (box == null) return false;
            Bounds b = box.bounds;
            Vector3 size = b.size + Vector3.one * (2f * margin);
            if (size.x <= 0f || size.z <= 0f) return false;
            size.y = Mathf.Max(size.y, .01f);
            return new Bounds(b.center, size).Contains(point);
        }

        public float Volume { get { var box = GetComponent<Collider>(); if (box == null) return float.MaxValue; var s = box.bounds.size; return s.x * s.y * s.z; } }

        public CameraZonePicker.Candidate CandidateFor(Vector3 owner) => new CameraZonePicker.Candidate
        {
            priority = priority, volume = Volume,
            inside = Contains(owner), insideWithMargin = Contains(owner, CameraZonePicker.Margin), insideShrunk = Contains(owner, -CameraZonePicker.Margin),
        };
    }

    /// <summary>Chooses the one camera zone in use. Pure logic, so it is covered by EditMode tests.</summary>
    public static class CameraZonePicker
    {
        public const float Margin = 1f;

        public struct Candidate
        {
            public int priority;
            public float volume;
            public bool inside, insideWithMargin, insideShrunk;
        }

        static bool Better(Candidate a, Candidate b) => a.priority != b.priority ? a.priority > b.priority : a.volume < b.volume;

        /// <summary>
        /// Returns the index of the zone to use, or -1 for none. <paramref name="current"/> is the zone in use now (-1 for none).
        /// The current zone is kept until the owner is more than <see cref="Margin"/> outside it, unless a better zone has the
        /// owner more than <see cref="Margin"/> inside it.
        /// </summary>
        public static int Pick(IReadOnlyList<Candidate> zones, int current)
        {
            int best = -1;
            for (int i = 0; i < zones.Count; i++)
                if (zones[i].inside && (best < 0 || Better(zones[i], zones[best]))) best = i;
            bool keep = current >= 0 && current < zones.Count && zones[current].insideWithMargin;
            if (!keep) return best;
            if (best < 0 || best == current) return current;
            return Better(zones[best], zones[current]) && zones[best].insideShrunk ? best : current;
        }
    }
}
