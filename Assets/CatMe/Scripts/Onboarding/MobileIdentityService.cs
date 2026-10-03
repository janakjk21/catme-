using System;
using System.Threading.Tasks;
using Firebase.Auth;

namespace CatMe.Onboarding
{
    public interface IIdentityService
    {
        string UserId { get; }
        bool IsGuest { get; }
        Task InitializeAsync();
        Task SignInWithEmailAsync(string email, string password);
        Task CreateEmailAsync(string email, string password);
        Task ContinueAsGuestAsync();
        Task SignInWithGameCenterAsync();
        Task SignInWithAppleCredentialAsync(string idToken, string rawNonce, string authorizationCode);
        Task SendPasswordResetAsync(string email);
        Task<string> GetIdTokenAsync(bool forceRefresh = false);
        void SignOut();
    }

    public sealed class MobileIdentityService : IIdentityService
    {
        readonly FirebaseAccountService account;
        public MobileIdentityService(FirebaseAccountService account)
        {
            this.account = account ?? throw new ArgumentNullException(nameof(account));
        }

        public string UserId => account.CurrentUser?.UserId;
        public bool IsGuest => account.CurrentUser?.IsAnonymous == true;
        public Task InitializeAsync() => account.InitializeAsync();
        public Task SignInWithEmailAsync(string email, string password) => account.SignInWithEmailAsync(email, password);
        public Task CreateEmailAsync(string email, string password) => account.CreateOrLinkEmailAsync(email, password);
        public Task ContinueAsGuestAsync() => account.SignInAnonymouslyAsync();
        public Task SignInWithGameCenterAsync() => account.SignInWithGameCenterAsync();
        public Task SignInWithAppleCredentialAsync(string idToken, string rawNonce, string authorizationCode) =>
            account.SignInWithAppleAsync(idToken, rawNonce, authorizationCode);

        public Task SendPasswordResetAsync(string email)
        {
            if (!account.IsReady) throw new InvalidOperationException("Account service is still starting.");
            if (string.IsNullOrWhiteSpace(email)) throw new ArgumentException("Enter your email address.");
            return FirebaseAuth.DefaultInstance.SendPasswordResetEmailAsync(email.Trim());
        }

        public Task<string> GetIdTokenAsync(bool forceRefresh = false)
        {
            FirebaseUser user = account.CurrentUser ?? throw new InvalidOperationException("Sign in first.");
            return user.TokenAsync(forceRefresh);
        }

        public void SignOut() => account.SignOut();
    }
}
