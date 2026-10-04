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
        // All per account (AccountStorage: Profiles/{uid}/).
        static string Drafts => AccountStorage.Drafts;
        /// <summary>The player's downloaded models, loaded by the garden in place of the bundled ones.</summary>
        public static string CatModelPath => AccountStorage.CatModelPath;
        /// <summary>The one place the person model lives (creation, restore and retry all write here).</summary>
        public static string HumanModelPath => AccountStorage.PersonModelPath;

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

        /// <summary>Opens the photo picker (editor: a file dialog) and reports the saved JPEG path, or null with a short reason.
        /// On iPhone a person photo must show exactly one face and is cropped to the head and shoulders before it is saved;
        /// <paramref name="selfieCamera"/> opens the front camera instead of the library.</summary>
        public void RequestPhoto(bool human, Action<string, string> done, string editorTitle = null, bool selfieCamera = false, bool petCheck = false)
        {
            if (picking) return;
            picking = true; pickingHuman = human; photoDone = done; LastPetCheck = null;
#if UNITY_EDITOR
            // Automated editor runs set these instead of answering the file dialog.
            string auto = human ? EditorAutoPickHuman : EditorAutoPickPet;
            if (!string.IsNullOrEmpty(auto)) { OnPhotoPicked(auto); return; }
            OnPhotoPicked(EditorUtility.OpenFilePanel(editorTitle ?? (human ? "Choose a photo of you" : "Choose a photo of your pet"), "", "png,jpg,jpeg"));
#elif UNITY_IOS
            if (human && selfieCamera) CatMePhotoPicker_TakeSelfie(gameObject.name);
            else if (human) CatMePhotoPicker_OpenForHuman(gameObject.name);
            else if (petCheck) CatMePhotoPicker_OpenForPet(gameObject.name);
            else CatMePhotoPicker_Open(gameObject.name);
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
            if (path.StartsWith("error:", StringComparison.Ordinal)) { Finish(null, PersonPhotoMessage(path.Substring(6))); return; }
            // Pet photos arrive as "pet:<cat|dog|many|none>|<path>" from the on-device animal check.
            if (path.StartsWith("pet:", StringComparison.Ordinal))
            {
                int bar = path.IndexOf('|');
                if (bar > 4) { LastPetCheck = path.Substring(4, bar - 4); path = path.Substring(bar + 1); }
            }
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

        /// <summary>Copies a saved album photo to the phone's Photos library. Editor: shows the file instead. Returns false where unsupported.</summary>
        public static bool SaveToPhotos(string path)
        {
            if (string.IsNullOrEmpty(path) || !File.Exists(path)) return false;
#if UNITY_EDITOR
            EditorUtility.RevealInFinder(path); return true;
#elif UNITY_IOS
            CatMePhotoPicker_SaveToPhotos(path); return true;
#else
            return false;
#endif
        }

        public const string CameraDeniedMessage = "Allow camera access to take your selfie.";

        /// <summary>What the phone saw in the last pet photo: "cat", "dog", "many", "none", or null when not checked (editor, house frame).</summary>
        public string LastPetCheck { get; private set; }

        /// <summary>Opens this app's page in the iPhone Settings app (to turn the camera back on).</summary>
        public static void OpenAppSettings()
        {
#if UNITY_IOS && !UNITY_EDITOR
            CatMePhotoPicker_OpenSettings();
#endif
        }

        // Words for the native face check (COPY.md, Messages).
        static string PersonPhotoMessage(string reason)
        {
            switch (reason)
            {
                case "no_face": return "We can't see your face in this photo. Try a selfie in good light.";
                case "many_faces": return "There is more than one in this photo. Try one with only you.";
                case "face_small": return "Your face is too small in this photo. Take a closer selfie.";
                case "camera_denied": return CameraDeniedMessage;
                default: return "This photo could not be read. Choose another one.";
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
        /// <summary>Reported instead of a message when a guest tries to save to a Google account that already has WornTag pets.</summary>
        public const string GoogleAccountInUse = "GOOGLE_ACCOUNT_IN_USE";

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
            catch (Exception ex)
            {
                Debug.LogWarning("[WornTag][Google] " + ex);
                error = FirebaseAccountService.IsCredentialInUse(ex) ? GoogleAccountInUse : "Google sign-in failed: " + ex.Message;
            }
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
        [DllImport("__Internal")] static extern void CatMePhotoPicker_TakeSelfie(string target);
        [DllImport("__Internal")] static extern void CatMePhotoPicker_OpenForPet(string target);
        [DllImport("__Internal")] static extern void CatMePhotoPicker_OpenSettings();
        [DllImport("__Internal")] static extern void CatMePhotoPicker_SaveToPhotos(string path);
        [DllImport("__Internal")] static extern void CatMeAppleSignIn_Open(string target);
#endif
    }

    /// <summary>One shared size for every generated character, so a provider's model units never change the pair's proportions.</summary>
    /// <summary>Which model walks beside the owner (launch task L7). Pure, so it is tested.</summary>
    internal static class PetModelChoice
    {
        /// <summary>
        /// The pet's own downloaded model when it exists (cats and dogs alike); otherwise null, meaning the bundled stand-in:
        /// the bundled cat for a cat, the corgi for a dog.
        /// </summary>
        public static string OwnModel(string modelPath, bool modelExists) => !string.IsNullOrEmpty(modelPath) && modelExists ? modelPath : null;

        /// <summary>True when a dog has no model of its own yet, so the corgi stands in.</summary>
        public static bool UsesCorgi(CatMe.Pets.Species kind, string modelPath, bool modelExists) => kind == CatMe.Pets.Species.Dog && OwnModel(modelPath, modelExists) == null;
    }

    internal static class CompanionScaleStandards
    {
        public const float OwnerHeightMeters = 1.70f;
        // Real ears-to-paws is ~0.46 m, but at that size the pet is hard to spot from the follow camera; ~1.4x reads as "my cat".
        public const float CatHeightMeters = 0.65f;
        /// <summary>The cat size the walk speed, clip rate and collision radius were tuned at; scale those by CatScale.</summary>
        public const float TunedCatHeightMeters = 0.46f;
        public static float CatScale => CatHeightMeters / TunedCatHeightMeters;
        /// <summary>A dog's own model is shown a little taller than the cat, like real dogs; the corgi stand-in keeps its own size.</summary>
        public const float DogHeightMeters = 0.72f;
        public static float PetHeightFor(CatMe.Pets.Species kind) => kind == CatMe.Pets.Species.Dog ? DogHeightMeters : CatHeightMeters;
        /// <summary>A dog's height follows its size (small, medium, large); a cat is always the cat height.</summary>
        public static float PetHeightFor(CatMe.Pets.PetProfile pet) => pet != null && pet.Kind == CatMe.Pets.Species.Dog ? CatMe.Pets.PetSize.DogHeight(pet.size) : CatHeightMeters;

        public static float ScaleForMeasuredHeight(float measuredHeight, float targetHeight)
        {
            if (float.IsNaN(measuredHeight) || float.IsInfinity(measuredHeight) || measuredHeight <= 0.001f)
                throw new ArgumentOutOfRangeException(nameof(measuredHeight), "A generated character needs visible, non-zero model bounds.");
            return targetHeight / measuredHeight;
        }
    }
}
