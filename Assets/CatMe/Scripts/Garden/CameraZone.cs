using UnityEngine;

namespace CatMe.Garden
{
    /// <summary>Pushes a camera profile while the owner stands inside this trigger volume. Polled by SessionDirector.</summary>
    public sealed class CameraZone : MonoBehaviour
    {
        public string profileName = "Default";
        public float blendIn = 1.5f;
        CameraProfile profile;
        public CameraProfile Profile
        {
            get
            {
                if (profile == null) { profile = CameraProfile.Named(profileName); profile.blendIn = blendIn; }
                return profile;
            }
        }
        public bool Contains(Vector3 point)
        {
            var box = GetComponent<Collider>();
            return box != null && box.bounds.Contains(point);
        }
    }
}
