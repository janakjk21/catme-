using System.Runtime.InteropServices;
using UnityEngine;

namespace CatMe.World
{
    /// <summary>
    /// Launch blocker F1 (no sound on the phone): sets the iOS audio session to Playback + MixWithOthers at startup and again on
    /// every return from the background, keeps exactly one enabled AudioListener on the main camera, and never leaves audio
    /// paused or at zero volume.
    /// </summary>
    public sealed class AudioSession : MonoBehaviour
    {
#if UNITY_IOS && !UNITY_EDITOR
        [DllImport("__Internal")] static extern void CatMeAudioSessionActivate();
#endif
        static AudioSession instance;
        float nextListenerCheck;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Boot()
        {
            if (instance != null) return;
            var go = new GameObject("Audio session"); DontDestroyOnLoad(go);
            instance = go.AddComponent<AudioSession>();
            Activate();
        }

        /// <summary>Plays even with the ring/silent switch on silent; other apps' music keeps playing.</summary>
        public static void Activate()
        {
#if UNITY_IOS && !UNITY_EDITOR
            CatMeAudioSessionActivate();
#endif
            AudioListener.pause = false;
            if (AudioListener.volume <= 0f) AudioListener.volume = 1f;
        }

        void OnApplicationPause(bool paused) { if (!paused) Activate(); }
        void OnApplicationFocus(bool focused) { if (focused) Activate(); }

        void Update()
        {
            if (Time.unscaledTime < nextListenerCheck) return;
            nextListenerCheck = Time.unscaledTime + 1f;
            EnsureOneListener();
        }

        /// <summary>Exactly one enabled listener, on the main camera when there is one.</summary>
        public static void EnsureOneListener()
        {
            var cam = Camera.main;
            var listeners = FindObjectsByType<AudioListener>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
            AudioListener keep = cam != null ? cam.GetComponent<AudioListener>() : null;
            if (keep == null && cam != null) keep = cam.gameObject.AddComponent<AudioListener>();
            if (keep == null && listeners.Length > 0) keep = listeners[0];
            if (keep != null && !keep.enabled) keep.enabled = true;
            foreach (var l in listeners) if (l != keep && l.enabled) l.enabled = false;
        }
    }
}
