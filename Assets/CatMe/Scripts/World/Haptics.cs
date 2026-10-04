using System.Runtime.InteropServices;
using UnityEngine;

namespace CatMe.World
{
    /// <summary>Light, calm haptics. iOS uses UIImpactFeedbackGenerator through CatMeHaptics.mm; the editor and other platforms do nothing.</summary>
    public static class Haptics
    {
        const string Key = "CatMe.Haptics";
        public static bool Enabled { get => PlayerPrefs.GetInt(Key, 1) == 1; set { PlayerPrefs.SetInt(Key, value ? 1 : 0); PlayerPrefs.Save(); } }
#if UNITY_IOS && !UNITY_EDITOR
        [DllImport("__Internal")] static extern void CatMeHapticImpact(int style);
        [DllImport("__Internal")] static extern void CatMeHapticPrepare();
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Warm() => CatMeHapticPrepare();   // generators exist and are ready before the first tap
#endif
        /// <summary>0 = soft, 1 = light, 2 = medium.</summary>
        static void Impact(int style)
        {
            LastStyle = style;
            if (!Enabled) return;
            Count++;
#if UNITY_IOS && !UNITY_EDITOR
            CatMeHapticImpact(style);
#endif
        }
        public static void Tap() => Impact(1);
        public static void Soft() => Impact(0);
        public static void Bump() => Impact(2);
        /// <summary>For tests and the dev console: haptics requested while enabled, and the last style asked for.</summary>
        public static int Count { get; private set; }
        public static int LastStyle { get; private set; } = -1;
    }
}
