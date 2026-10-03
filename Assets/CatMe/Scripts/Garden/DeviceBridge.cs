using System;
using System.IO;
using System.Runtime.InteropServices;
using CatMe.Onboarding;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace CatMe.Garden
{
    /// <summary>
    /// The phone's native pieces in one place: the photo picker, Sign in with Apple and the Firebase account.
    /// It has no screens of its own; the onboarding flow owns every screen. Native plugins call back by this GameObject's name.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class DeviceBridge : MonoBehaviour
    {
        static string Drafts => Path.Combine(Application.persistentDataPath, "Profiles", "local-guest", "drafts");
        static string Models => Path.Combine(Application.persistentDataPath, "Companion");
        /// <summary>The player's downloaded models, loaded by the garden in place of the bundled ones.</summary>
        public static string CatModelPath => Path.Combine(Models, "cat-walking.glb");
        public static string HumanModelPath => Path.Combine(Models, "person-walking.glb");

        public FirebaseAccountService AccountService { get; private set; }
#if UNITY_EDITOR
        public static string EditorAutoPickPet, EditorAutoPickHuman;   // editor-only test hook; see docs/QUALITY.md
#endif

        bool picking, pickingHuman;
        Action<string, string> photoDone;      // (saved path or null, message)
        Action<string> appleDone;              // error message, or null on success

        public void Initialize()
        {
            gameObject.name = "WornTagDevice";
            AccountService = new FirebaseAccountService();
            _ = AccountService.InitializeAsync();
        }

        // ---- photos -------------------------------------------------------------------------------------------------------

        /// <summary>Opens the photo picker (editor: a file dialog) and reports the saved JPEG path, or null with a short reason.</summary>
        public void RequestPhoto(bool human, Action<string, string> done, string editorTitle = null)
        {
            if (picking) return;
            picking = true; pickingHuman = human; photoDone = done;
#if UNITY_EDITOR
            // Automated editor runs set these instead of answering the file dialog.
            string auto = human ? EditorAutoPickHuman : EditorAutoPickPet;
            if (!string.IsNullOrEmpty(auto)) { OnPhotoPicked(auto); return; }
            OnPhotoPicked(EditorUtility.OpenFilePanel(editorTitle ?? (human ? "Choose a photo of you" : "Choose a photo of your pet"), "", "png,jpg,jpeg"));
#elif UNITY_IOS
            if (human) CatMePhotoPicker_OpenForHuman(gameObject.name); else CatMePhotoPicker_Open(gameObject.name);
#else
            Finish(null, "Photos can be chosen on iPhone or in the Unity editor.");
#endif
        }

        // Called by the native picker.
        public void OnCatPhotoPicked(string path) { pickingHuman = false; OnPhotoPicked(path); }
        public void OnHumanPhotoPicked(string path) { pickingHuman = true; OnPhotoPicked(path); }

        void OnPhotoPicked(string path)
        {
            if (string.IsNullOrWhiteSpace(path)) { Finish(null, null); return; }
            Texture2D decoded = null;
            try
            {
                var file = new FileInfo(path);
                if (!file.Exists || file.Length == 0 || file.Length > 9 * 1024 * 1024) { Finish(null, "Choose a JPG or PNG smaller than 9 MB."); return; }
                byte[] bytes = File.ReadAllBytes(path);
                if (!PhotoDimensions.IsSafe(bytes)) { Finish(null, "Choose a photo up to 4096 pixels on each side."); return; }
                decoded = new Texture2D(2, 2, TextureFormat.RGBA32, false);
                if (!decoded.LoadImage(bytes)) { Finish(null, "This photo could not be read. Choose another one."); return; }
                Directory.CreateDirectory(Drafts);
                string destination = Path.Combine(Drafts, pickingHuman ? "person-photo.jpg" : "pet-photo-" + DateTime.UtcNow.Ticks + ".jpg");
                File.WriteAllBytes(destination, decoded.EncodeToJPG(88));
                Finish(destination, null);
            }
            catch (Exception e) when (e is IOException || e is UnauthorizedAccessException || e is ArgumentException)
            {
                Finish(null, "The photo could not be saved. Check free space and try again.");
            }
            finally
            {
                if (decoded != null) Destroy(decoded);
#if UNITY_IOS && !UNITY_EDITOR
                if (Path.GetFileName(path).StartsWith("catme-photo-") && Path.GetFullPath(path).StartsWith(Path.GetFullPath(Application.temporaryCachePath))) { try { File.Delete(path); } catch (IOException) { } }
#endif
            }
        }

        void Finish(string path, string message)
        {
            picking = false;
            var done = photoDone; photoDone = null;
            done?.Invoke(path, message);
        }

        // ---- Sign in with Apple ---------------------------------------------------------------------------------------------

        /// <summary>Shows Apple's sign-in sheet; reports null on success or a short reason.</summary>
        public void SignInWithApple(Action<string> done)
        {
            if (AccountService == null || !AccountService.IsReady) { done?.Invoke(AccountService?.LastError ?? "Accounts are still starting. Try again in a moment."); return; }
#if UNITY_IOS && !UNITY_EDITOR
            appleDone = done;
            CatMeAppleSignIn_Open(gameObject.name);
#else
            done?.Invoke("Apple sign-in works on iPhone.");
#endif
        }

        [Serializable] sealed class ApplePayload { public string idToken; public string rawNonce; public string authorizationCode; }

        // Called by the native Apple plugin.
        public async void OnAppleAuthorization(string json)
        {
            string error = null;
            try
            {
                var payload = JsonUtility.FromJson<ApplePayload>(json);
                if (payload == null) throw new InvalidOperationException("Apple sign-in was incomplete.");
                await AccountService.SignInWithAppleAsync(payload.idToken, payload.rawNonce, payload.authorizationCode);
            }
            catch (Exception ex) { Debug.LogWarning("[WornTag][Apple] " + ex); error = "Apple sign-in failed: " + ex.Message; }
            var done = appleDone; appleDone = null; done?.Invoke(error);
        }

        public void OnAppleAuthorizationFailed(string message)
        {
            var done = appleDone; appleDone = null;
            done?.Invoke(string.IsNullOrWhiteSpace(message) ? "Apple sign-in was not completed." : message);
        }

        // ---- Google sign-in ---------------------------------------------------------------------------------------------------

        Action<string> googleDone;             // error message, or null on success

        /// <summary>Shows Google's sign-in sheet and signs in to Firebase with the result; reports null on success or a short reason.</summary>
        public void SignInWithGoogle(Action<string> done)
        {
            if (AccountService == null || !AccountService.IsReady) { done?.Invoke(AccountService?.LastError ?? "Accounts are still starting. Try again in a moment."); return; }
#if UNITY_IOS && !UNITY_EDITOR
            googleDone = done;
            CatMeGoogleSignIn_Open(gameObject.name);
#else
            done?.Invoke("Google sign-in works on iPhone.");
#endif
        }

        [Serializable] sealed class GooglePayload { public string idToken; public string accessToken; }

        // Called by the native Google plugin.
        public async void OnGoogleAuthorization(string json)
        {
            string error = null;
            try
            {
                var payload = JsonUtility.FromJson<GooglePayload>(json);
                if (payload == null || string.IsNullOrEmpty(payload.idToken)) throw new InvalidOperationException("Google sign-in was incomplete.");
                await AccountService.SignInWithGoogleTokensAsync(payload.idToken, payload.accessToken);
            }
            catch (Exception ex) { Debug.LogWarning("[WornTag][Google] " + ex); error = "Google sign-in failed: " + ex.Message; }
            var done = googleDone; googleDone = null; done?.Invoke(error);
        }

        public void OnGoogleAuthorizationFailed(string message)
        {
            var done = googleDone; googleDone = null;
            done?.Invoke(string.IsNullOrWhiteSpace(message) ? "Google sign-in was not completed." : message);
        }

#if UNITY_IOS && !UNITY_EDITOR
        [DllImport("__Internal")] static extern void CatMeGoogleSignIn_Open(string target);
        [DllImport("__Internal")] static extern void CatMePhotoPicker_Open(string target);
        [DllImport("__Internal")] static extern void CatMePhotoPicker_OpenForHuman(string target);
        [DllImport("__Internal")] static extern void CatMeAppleSignIn_Open(string target);
#endif
    }

    /// <summary>One shared size for every generated character, so a provider's model units never change the pair's proportions.</summary>
    internal static class CompanionScaleStandards
    {
        public const float OwnerHeightMeters = 1.70f;
        public const float CatHeightMeters = 0.46f;

        public static float ScaleForMeasuredHeight(float measuredHeight, float targetHeight)
        {
            if (float.IsNaN(measuredHeight) || float.IsInfinity(measuredHeight) || measuredHeight <= 0.001f)
                throw new ArgumentOutOfRangeException(nameof(measuredHeight), "A generated character needs visible, non-zero model bounds.");
            return targetHeight / measuredHeight;
        }
    }
}
