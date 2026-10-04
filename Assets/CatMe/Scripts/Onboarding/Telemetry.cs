using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;

namespace CatMe.Onboarding
{
    /// <summary>
    /// Crash reports (Firebase Crashlytics) and a few funnel events (Google Analytics for Firebase), launch task L8.
    /// The two Firebase SDKs are found by reflection, so this compiles and does nothing until the owner imports
    /// FirebaseCrashlytics.unitypackage and FirebaseAnalytics.unitypackage (same version as the other Firebase packages).
    /// No user id, photo, name or email is ever sent. The player can turn it off in Settings.
    /// </summary>
    public static class Telemetry
    {
        public const string EnabledKey = "CatMe.Telemetry.Enabled";
        const int MaxRecent = 30;

        static bool resolved, crashlyticsReady;
        static Type analytics, crashlytics;
        static readonly List<string> recent = new List<string>();

        /// <summary>The last events, newest last ("name param=value"). For tests and the development console.</summary>
        public static IReadOnlyList<string> Recent => recent;

        /// <summary>On unless the player switched it off in Settings.</summary>
        public static bool Enabled
        {
            get => PlayerPrefs.GetInt(EnabledKey, 1) == 1;
            set { PlayerPrefs.SetInt(EnabledKey, value ? 1 : 0); PlayerPrefs.Save(); ApplyCollection(); }
        }

        /// <summary>True when the Firebase Analytics SDK is in the build.</summary>
        public static bool AnalyticsAvailable { get { Resolve(); return analytics != null; } }
        public static bool CrashlyticsAvailable { get { Resolve(); return crashlytics != null; } }

        /// <summary>Call once Firebase has started (FirebaseApp.CheckAndFixDependenciesAsync succeeded).</summary>
        public static void FirebaseStarted()
        {
            Resolve();
            if (crashlytics != null && !crashlyticsReady)
            {
                crashlyticsReady = true;
                TrySet(crashlytics, "ReportUncaughtExceptionsAsFatal", true);
            }
            ApplyCollection();
        }

        static void ApplyCollection()
        {
            Resolve();
            bool on = Enabled;
            if (crashlytics != null) TrySet(crashlytics, "IsCrashlyticsCollectionEnabled", on);
            if (analytics != null) TryCall(analytics, "SetAnalyticsCollectionEnabled", new object[] { on });
        }

        /// <summary>Firebase event names: lower case letters, digits and underscores, starting with a letter, at most 40 characters.</summary>
        public static string EventName(string name)
        {
            if (string.IsNullOrWhiteSpace(name)) return "event";
            var chars = name.Trim().ToLowerInvariant().Select(c => char.IsLetterOrDigit(c) && c < 128 ? c : '_').ToArray();
            string clean = new string(chars);
            while (clean.Contains("__")) clean = clean.Replace("__", "_");
            clean = clean.Trim('_');
            if (clean.Length == 0 || !char.IsLetter(clean[0])) clean = "e_" + clean;
            return clean.Length > 40 ? clean.Substring(0, 40) : clean;
        }

        /// <summary>Parameter values are short codes, never names or free text from the player.</summary>
        public static string Value(string value)
        {
            if (string.IsNullOrEmpty(value)) return "none";
            string v = EventName(value);
            return v.Length > 36 ? v.Substring(0, 36) : v;
        }

        public static void Track(string name) => Record(EventName(name), null, null);
        public static void Track(string name, string parameter, string value) => Record(EventName(name), EventName(parameter), Value(value));
        public static void Track(string name, string parameter, long value) => Record(EventName(name), EventName(parameter), value);

        static void Record(string name, string parameter, object value)
        {
            string line = parameter == null ? name : name + " " + parameter + "=" + value;
            recent.Add(line); if (recent.Count > MaxRecent) recent.RemoveAt(0);
            if (!Enabled) return;
            Resolve();
            if (analytics == null) { if (Debug.isDebugBuild) Debug.Log("[WornTag][Telemetry] " + line); return; }
            if (parameter == null) TryCall(analytics, "LogEvent", new object[] { name });
            else TryCall(analytics, "LogEvent", new object[] { name, parameter, value });
            if (crashlytics != null) TryCall(crashlytics, "Log", new object[] { line });   // breadcrumbs for crash reports
        }

        /// <summary>A caught error worth seeing in Crashlytics (not shown to the player).</summary>
        public static void Report(Exception error)
        {
            if (error == null || !Enabled) return;
            Resolve();
            if (crashlytics != null) TryCall(crashlytics, "LogException", new object[] { error });
        }

        static void Resolve()
        {
            if (resolved) return;
            resolved = true;
            foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                analytics ??= assembly.GetType("Firebase.Analytics.FirebaseAnalytics", false);
                crashlytics ??= assembly.GetType("Firebase.Crashlytics.Crashlytics", false);
            }
        }

        static void TrySet(Type type, string property, object value)
        {
            try { type.GetProperty(property, BindingFlags.Public | BindingFlags.Static)?.SetValue(null, value); }
            catch (Exception e) { Debug.LogWarning("[WornTag][Telemetry] " + property + ": " + e.Message); }
        }

        static void TryCall(Type type, string method, object[] args)
        {
            try
            {
                var types = args.Select(a => a.GetType()).ToArray();
                MethodInfo m = type.GetMethod(method, BindingFlags.Public | BindingFlags.Static, null, types, null);
                if (m == null && types.Length == 3 && types[2] == typeof(long))
                    m = type.GetMethod(method, BindingFlags.Public | BindingFlags.Static, null, new[] { typeof(string), typeof(string), typeof(long) }, null);
                if (m == null && args.Length == 1 && args[0] is Exception)
                    m = type.GetMethod(method, BindingFlags.Public | BindingFlags.Static, null, new[] { typeof(Exception) }, null);
                m?.Invoke(null, args);
            }
            catch (Exception e) { Debug.LogWarning("[WornTag][Telemetry] " + method + ": " + e.Message); }
        }

        /// <summary>Tests: forget recorded events.</summary>
        public static void ClearRecentForTests() => recent.Clear();
    }
}
