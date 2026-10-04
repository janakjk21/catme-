using System.Runtime.InteropServices;
using UnityEngine;

namespace CatMe.World
{
    /// <summary>
    /// One local notification, for a pet still being made when the player leaves the app (launch task L5). iOS only
    /// (Assets/Plugins/iOS/CatMeNotifications.mm); does nothing in the Editor and on other platforms.
    /// </summary>
    public static class LocalNotifications
    {
        public const string PetReadyId = "worntag.pet.making";
        /// <summary>How long after leaving the app the reminder fires; a typical pet takes about three minutes.</summary>
        public const int ReminderSeconds = 240;

#if UNITY_IOS && !UNITY_EDITOR
        [DllImport("__Internal")] static extern void CatMeNotificationsRequest();
        [DllImport("__Internal")] static extern void CatMeNotificationsSchedule(string id, string title, string body, int seconds);
        [DllImport("__Internal")] static extern void CatMeNotificationsCancel(string id);
#endif

        /// <summary>Asks once for permission to show alerts (iOS shows its own prompt the first time only).</summary>
        public static void RequestPermission()
        {
#if UNITY_IOS && !UNITY_EDITOR
            CatMeNotificationsRequest();
#endif
        }

        public static void Schedule(string id, string title, string body, int seconds)
        {
#if UNITY_IOS && !UNITY_EDITOR
            CatMeNotificationsSchedule(id, title, body, Mathf.Max(1, seconds));
#else
            Debug.Log("[WornTag][Notify] (" + seconds + " s) " + title + ": " + body);
#endif
        }

        public static void Cancel(string id)
        {
#if UNITY_IOS && !UNITY_EDITOR
            CatMeNotificationsCancel(id);
#endif
        }

        /// <summary>The words, kept honest: the phone can't know the pet is finished while the app is closed.</summary>
        public static string ReminderBody(string petName) => petName + " should be ready soon. Come and say hello.";
    }
}
