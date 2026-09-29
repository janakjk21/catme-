using System.Collections;
using UnityEngine;

namespace CatMe.Cat
{
    /// <summary>Small, reusable ambience and interaction sounds for HomeRoom.</summary>
    [DisallowMultipleComponent]
    public sealed class CatRoomAudio : MonoBehaviour
    {
        private const string ResourceRoot = "CatMeAudio/";
        private const float AmbientVolume = 0.075f;

        private AudioSource ambienceSource;
        private AudioSource voiceSource;
        private AudioClip callMeow;
        private AudioClip foodMeow;
        private Transform catAnchor;
        private bool initialized;

        public bool HasRoomAmbience => ambienceSource != null && ambienceSource.clip != null;
        public bool HasCallMeow => callMeow != null;
        public bool HasFoodCue => foodMeow != null;

        public void Initialize(Transform anchor)
        {
            if (initialized) return;
            initialized = true;
            catAnchor = anchor == null ? transform : anchor;

            AudioClip roomLoop = Resources.Load<AudioClip>(ResourceRoot + "amb_morning");
            callMeow = Resources.Load<AudioClip>(ResourceRoot + "cat-meow");
            foodMeow = Resources.Load<AudioClip>(ResourceRoot + "cat_mewfood");

            ambienceSource = CreateSource("RoomAmbienceAudio", transform, false, 0f, 0f, 0f);
            ambienceSource.loop = true;
            ambienceSource.clip = roomLoop;
            if (roomLoop != null) StartCoroutine(FadeInRoomAmbience());

            voiceSource = CreateSource("CatVoiceAudio", transform, true, 0.32f, 1.1f, 7f);
            Debug.Log($"[CatMe][Audio] HomeRoom audio ready | ambience={roomLoop != null} | call={callMeow != null} | " +
                      $"food={foodMeow != null}");
        }

        public void PlayCallMeow()
        {
            if (callMeow == null || voiceSource == null) return;
            voiceSource.transform.position = catAnchor.position + Vector3.up * 0.22f;
            voiceSource.pitch = Random.Range(0.96f, 1.04f);
            voiceSource.PlayOneShot(callMeow, 0.62f);
        }

        public void PlayFoodCue()
        {
            if (foodMeow == null || voiceSource == null) return;
            voiceSource.transform.position = catAnchor.position + Vector3.up * 0.22f;
            voiceSource.pitch = Random.Range(0.96f, 1.03f);
            voiceSource.PlayOneShot(foodMeow, 0.46f);
        }

        private IEnumerator FadeInRoomAmbience()
        {
            ambienceSource.volume = 0f;
            ambienceSource.spatialBlend = 0f;
            ambienceSource.playOnAwake = false;
            ambienceSource.loop = true;
            ambienceSource.Play();
            const float fadeSeconds = 2.2f;
            float elapsed = 0f;
            while (elapsed < fadeSeconds && ambienceSource != null)
            {
                elapsed += Time.unscaledDeltaTime;
                ambienceSource.volume = Mathf.Lerp(0f, AmbientVolume, Mathf.Clamp01(elapsed / fadeSeconds));
                yield return null;
            }
            if (ambienceSource != null) ambienceSource.volume = AmbientVolume;
        }

        private static AudioSource CreateSource(string sourceName, Transform parent, bool spatial, float volume,
            float minimumDistance, float maximumDistance)
        {
            GameObject sourceObject = new GameObject(sourceName);
            sourceObject.transform.SetParent(parent, false);
            AudioSource source = sourceObject.AddComponent<AudioSource>();
            source.playOnAwake = false;
            source.loop = false;
            source.volume = volume;
            source.spatialBlend = spatial ? 0.48f : 0f;
            source.rolloffMode = AudioRolloffMode.Linear;
            source.minDistance = minimumDistance;
            source.maxDistance = maximumDistance;
            source.dopplerLevel = 0f;
            source.priority = 96;
            return source;
        }
    }
}
