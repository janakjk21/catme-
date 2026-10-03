using System;
using UnityEngine;
using System.Collections.Generic;
using System.Threading.Tasks;
using Firebase;
using Firebase.Auth;
using Firebase.Firestore;
using Firebase.Storage;

namespace CatMe.Onboarding
{
    // Firebase owns remote identity. Local garden play never depends on this service.
    public sealed class FirebaseAccountService
    {
        FirebaseAuth auth;
        FirebaseFirestore firestore;
        FirebaseStorage storage;

        public bool IsReady { get; private set; }
        public bool ProfileReady { get; private set; }
        public FirebaseUser CurrentUser => IsReady ? auth.CurrentUser : null;
        public string LastError { get; private set; }

        Task initializing;

        /// <summary>Starts Firebase once; later calls return the same task. After a failure, call RetryAsync.</summary>
        public Task InitializeAsync() => initializing ?? (initializing = InitializeCoreAsync());

        /// <summary>Tries again after a failed start (for example when the phone was offline at launch).</summary>
        public Task RetryAsync() { if (!IsReady) initializing = null; return InitializeAsync(); }

        async Task InitializeCoreAsync()
        {
            try
            {
                DependencyStatus status = await FirebaseApp.CheckAndFixDependenciesAsync();
                if (status != DependencyStatus.Available)
                {
                    LastError = "Account services are unavailable on this device (" + status + ").";
                    Debug.LogWarning("[WornTag][Firebase] dependencies: " + status);
                    return;
                }
                FirebaseApp app = FirebaseApp.DefaultInstance;
                auth = FirebaseAuth.GetAuth(app);
                IsReady = true;          // sign-in only needs Auth; Firestore and Storage are optional extras
                LastError = null;
                try { firestore = FirebaseFirestore.DefaultInstance; } catch (Exception e) { Debug.LogWarning("[WornTag][Firebase] Firestore unavailable: " + e.Message); }
                try { storage = FirebaseStorage.GetInstance(app); } catch (Exception e) { Debug.LogWarning("[WornTag][Firebase] Storage unavailable: " + e.Message); }
                if (auth.CurrentUser != null) await TryEnsureProfileAsync();
            }
            catch (Exception e)
            {
                Debug.LogWarning("[WornTag][Firebase] start failed: " + e.GetType().Name + ": " + e.Message);
                LastError = "Account services could not start (" + e.GetType().Name + "). " + e.Message;
            }
        }

        public async Task SignInWithEmailAsync(string email, string password)
        {
            RequireReady();
            await auth.SignInWithEmailAndPasswordAsync(email.Trim(), password);
            await TryEnsureProfileAsync();
        }

        public async Task SignInWithAppleAsync(string idToken, string rawNonce, string authorizationCode)
        {
            RequireReady();
            if (string.IsNullOrWhiteSpace(idToken) || string.IsNullOrWhiteSpace(rawNonce))
                throw new ArgumentException("Apple did not return a usable sign-in credential.");
            Credential credential = OAuthProvider.GetCredential("apple.com", idToken, rawNonce, authorizationCode);
            await SignInWithCredentialAsync(credential);
        }

        public async Task SignInWithGameCenterAsync()
        {
            RequireReady();
#if UNITY_IOS && !UNITY_EDITOR
            if (!GameCenterAuthProvider.IsPlayerAuthenticated())
                throw new InvalidOperationException("Sign in to Game Center in iPhone Settings first.");
            Credential credential = await GameCenterAuthProvider.GetCredentialAsync();
            await SignInWithCredentialAsync(credential);
#else
            throw new PlatformNotSupportedException("Game Center is only available on iPhone.");
#endif
        }

        // The Google provider needs a native Google Sign-In client to obtain these tokens.
        // Do not accept user-entered tokens or expose this as a button until that client is installed.
        public async Task SignInWithGoogleTokensAsync(string idToken, string accessToken)
        {
            RequireReady();
            if (string.IsNullOrWhiteSpace(idToken)) throw new ArgumentException("Google ID token is missing.");
            await SignInWithCredentialAsync(GoogleAuthProvider.GetCredential(idToken, accessToken));
        }

        async Task SignInWithCredentialAsync(Credential credential)
        {
            // Never silently merge a signed-in account with a different provider identity.
            if (auth.CurrentUser != null && !auth.CurrentUser.IsAnonymous)
                throw new InvalidOperationException("Sign out before using a different sign-in method.");
            if (auth.CurrentUser != null && auth.CurrentUser.IsAnonymous)
                await auth.CurrentUser.LinkWithCredentialAsync(credential);
            else
                await auth.SignInWithCredentialAsync(credential);
            await TryEnsureProfileAsync();
        }

        public async Task CreateOrLinkEmailAsync(string email, string password)
        {
            RequireReady();
            FirebaseUser existing = auth.CurrentUser;
            if (existing != null && existing.IsAnonymous)
                await existing.LinkWithCredentialAsync(EmailAuthProvider.GetCredential(email.Trim(), password));
            else
                await auth.CreateUserWithEmailAndPasswordAsync(email.Trim(), password);
            await TryEnsureProfileAsync();
        }

        public async Task SignInAnonymouslyAsync()
        {
            RequireReady();
            if (auth.CurrentUser == null) await auth.SignInAnonymouslyAsync();
            await TryEnsureProfileAsync();
        }

        public void SignOut()
        {
            RequireReady();
            auth.SignOut();
            ProfileReady = false;
        }

        public DocumentReference UserDocument()
        {
            RequireReady();
            FirebaseUser user = auth.CurrentUser ?? throw new InvalidOperationException("Sign in first.");
            return firestore.Collection("users").Document(user.UserId);
        }

        public StorageReference UserStorageRoot()
        {
            RequireReady();
            FirebaseUser user = auth.CurrentUser ?? throw new InvalidOperationException("Sign in first.");
            return storage.RootReference.Child("users").Child(user.UserId);
        }

        Task TryEnsureProfileAsync()
        {
            // /v1/session creates the server-owned profile. The app must not write it.
            ProfileReady = auth?.CurrentUser != null;
            return Task.CompletedTask;
        }

        void RequireReady()
        {
            if (!IsReady) throw new InvalidOperationException(LastError ?? "Account services are still starting.");
        }
    }
}
