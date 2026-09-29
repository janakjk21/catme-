using UnityEngine;

namespace CatMe.Toys
{
    /// <summary>Plays restrained, spatial toy sounds for the reusable ball.</summary>
    [DisallowMultipleComponent]
    public sealed class CatBallImpactAudio : MonoBehaviour
    {
        private AudioSource source;
        private AudioClip[] clips;
        private AudioLowPassFilter surfaceFilter;
        private bool onWovenSurface;
        private float nextImpactAt;

        public void Initialize()
        {
            source = GetComponent<AudioSource>();
            if (source == null) source = gameObject.AddComponent<AudioSource>();
            source.playOnAwake = false;
            source.loop = false;
            source.spatialBlend = 0.28f;
            source.rolloffMode = AudioRolloffMode.Linear;
            source.minDistance = 1f;
            source.maxDistance = 6f;
            source.dopplerLevel = 0f;
            surfaceFilter = GetComponent<AudioLowPassFilter>();
            if (surfaceFilter == null) surfaceFilter = gameObject.AddComponent<AudioLowPassFilter>();
            surfaceFilter.cutoffFrequency = 4800f;
            clips = Resources.LoadAll<AudioClip>("CatMeAudio/BallToy");
        }

        public void SetOnWovenSurface(bool value)
        {
            onWovenSurface = value;
            if (surfaceFilter != null) surfaceFilter.cutoffFrequency = value ? 1350f : 4800f;
        }

        public void PlayBat(float strength = 1.1f)
        {
            PlayImpact(strength);
        }

        private void OnCollisionEnter(Collision collision)
        {
            if (collision == null || collision.contactCount == 0) return;
            float normalSpeed = Mathf.Abs(Vector3.Dot(collision.relativeVelocity, collision.GetContact(0).normal));
            PlayImpact(normalSpeed);
        }

        private void PlayImpact(float strength)
        {
            if (source == null || clips == null || clips.Length == 0 || strength < 0.25f || Time.time < nextImpactAt) return;
            nextImpactAt = Time.time + 0.12f;
            source.pitch = Random.Range(0.94f, 1.1f);
            // Use the existing clips with a quieter, softened rug response until
            // distinct licensed wood and fabric impact recordings are supplied.
            float volume = Mathf.Clamp(strength / 4f, 0.12f, 0.48f) * (onWovenSurface ? 0.48f : 1f);
            source.PlayOneShot(clips[Random.Range(0, clips.Length)], volume);
        }
    }
}
