using System;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Networking;

namespace CatMe.Onboarding
{
    [Serializable] public sealed class UploadTicket { public string photoPath; public string uploadUrl; public int expiresInSeconds; }
    [Serializable] public sealed class CreationStatus
    {
        public string id; public string name; public string species; public string status; public string stage; public string rejectReason; public string errorCode; public string modelUrl;
        public long modelBytes; public string modelMd5; public bool isFixture; public string fixtureKey; public string petId; public bool duplicate;
    }

    [Serializable] public sealed class MobileSession
    {
        public CreationStatus[] pets;
        public CreationStatus owner;
    }

    public interface IMobileApi
    {
        Task<MobileSession> OpenSessionAsync();
        Task AcceptConsentAsync(string version);
        Task<UploadTicket> RequestUploadAsync(string kind, int bytes);
        Task UploadPhotoAsync(UploadTicket ticket, byte[] jpeg, Action<float> progress);
        Task<string> CreatePetAsync(string name, string species, string photoPath, string idempotencyKey);
        Task CreateOwnerAsync(string photoPath, string idempotencyKey);
        Task<CreationStatus> GetPetAsync(string petId);
        Task<CreationStatus> GetOwnerAsync();
        Task DownloadFileAsync(string url, string destinationPath, Action<float> progress);
    }

    public sealed class MobileApiException : Exception
    {
        public long StatusCode { get; }
        public MobileApiException(string message, long statusCode) : base(message)
        {
            StatusCode = statusCode;
        }
    }

    public interface IPrivacyApi
    {
        Task DeletePhotosAsync();
        Task DeleteAccountAsync();
    }

    public interface IHouseFrameApi
    {
        Task<byte[]> GetHouseFramePhotoAsync();
        Task SaveHouseFramePhotoAsync(byte[] jpeg);
    }

    public sealed class MobileApi : IMobileApi, IPrivacyApi, IHouseFrameApi
    {
        readonly MobileApiConfig config;
        readonly IIdentityService identity;

        public MobileApi(MobileApiConfig config, IIdentityService identity)
        {
            this.config = config ?? throw new ArgumentNullException(nameof(config));
            this.identity = identity ?? throw new ArgumentNullException(nameof(identity));
        }

        Uri BaseUri()
        {
            if (string.IsNullOrWhiteSpace(config.BaseUrl) ||
                !Uri.TryCreate(config.BaseUrl, UriKind.Absolute, out Uri baseUri) ||
                baseUri.Scheme != Uri.UriSchemeHttps)
                throw new InvalidOperationException("The account server is not configured.");
            return baseUri;
        }

        /// <summary>Sends an authenticated JSON request; refreshes the token once on 401 and retries transient failures twice.</summary>
        async Task<string> SendAsync(string method, string path, string jsonBody)
        {
            Uri baseUri = BaseUri();
            if (string.IsNullOrEmpty(identity.UserId)) throw new InvalidOperationException("Sign in first.");

            bool refresh = false;
            int transientRetries = 0;
            while (true)
            {
                string token = await identity.GetIdTokenAsync(refresh);
                using (var request = new UnityWebRequest(new Uri(baseUri, path).ToString(), method))
                {
                    if (jsonBody != null) request.uploadHandler = new UploadHandlerRaw(Encoding.UTF8.GetBytes(jsonBody));
                    request.downloadHandler = new DownloadHandlerBuffer();
                    request.SetRequestHeader("Content-Type", "application/json");
                    request.SetRequestHeader("Authorization", "Bearer " + token);
                    request.timeout = 20;
                    var operation = request.SendWebRequest();
                    while (!operation.isDone) await Task.Yield();
                    if (request.responseCode == 401 && !refresh) { refresh = true; continue; }
                    bool transient = request.responseCode == 429 || request.responseCode >= 500 ||
                                     request.result == UnityWebRequest.Result.ConnectionError;
                    if (transient && transientRetries < 2)
                    {
                        await Task.Delay(300 * (1 << transientRetries));
                        transientRetries++;
                        continue;
                    }
                    if (request.result != UnityWebRequest.Result.Success)
                        throw new MobileApiException(ErrorMessage(request), request.responseCode);
                    return request.downloadHandler.text;
                }
            }
        }

        [Serializable] sealed class ApiErrorEnvelope { public ApiError error; }
        [Serializable] sealed class ApiError { public string code; public string message; }
        static string ErrorMessage(UnityWebRequest request)
            => ErrorMessage(request.responseCode, request.downloadHandler?.text);

        /// <summary>Neutral: there may be no pet yet, so nothing claims one is saved.</summary>
        public const string OfflineMessage = "No internet connection. Try again when you're back online.";

        public static string ErrorMessage(long statusCode, string responseBody)
        {
            try
            {
                var error = JsonUtility.FromJson<ApiErrorEnvelope>(responseBody)?.error;
                if (error?.code == "DAILY_CAP_REACHED") return "We are making a lot of pets today. Try again tomorrow.";
                if (!string.IsNullOrEmpty(error?.code) && !string.IsNullOrEmpty(error.message)) return error.message;
            }
            catch (ArgumentException) { }
            if (statusCode == 0) return OfflineMessage;
            if (statusCode == 429) return "Too many tries. Wait a minute.";
            if (statusCode >= 500) return "The WornTag server is having trouble. Try again shortly.";
            switch (statusCode)
            {
                case 401: return "Please sign in again.";
                case 403: return "Accept the photo consent first.";
                case 409: return "This can't be made right now. Try again later.";
                case 400: return "That photo could not be used. Choose a JPEG under 8 MB.";
                default: return "Account service is unavailable. Try again shortly.";
            }
        }

        [Serializable] sealed class ConsentBody { public string version; }
        [Serializable] sealed class UploadBody { public string kind; public string contentType = "image/jpeg"; public int bytes; }
        [Serializable] sealed class PetBody { public string name; public string species; public string photoPath; public string idempotencyKey; }
        [Serializable] sealed class OwnerBody { public string photoPath; public string idempotencyKey; }

        [Serializable] sealed class FrameRecord { public bool exists; public string photoUrl; }
        [Serializable] sealed class FrameBody { public string photoPath; }
        public async Task<byte[]> GetHouseFramePhotoAsync()
        {
            var record = JsonUtility.FromJson<FrameRecord>(await SendAsync("GET", "/v1/house/frame", null));
            if (record == null || !record.exists) return null;
            if (!Uri.TryCreate(record.photoUrl, UriKind.Absolute, out var url) || url.Scheme != "https")
                throw new InvalidOperationException("Photo download link missing.");
            using (var request = UnityWebRequest.Get(record.photoUrl))
            {
                request.timeout = 30;
                var operation = request.SendWebRequest();
                while (!operation.isDone) await Task.Yield();
                if (request.result != UnityWebRequest.Result.Success) throw new IOException("Photo download failed.");
                var bytes = request.downloadHandler.data;
                if (bytes.Length > 8 * 1024 * 1024) throw new IOException("Photo too large.");
                return bytes;
            }
        }
        public async Task SaveHouseFramePhotoAsync(byte[] jpeg)
        {
            string uid = identity.UserId;
            // Choosing a photo explicitly consents to storing it in this account, without generation.
            await AcceptConsentAsync("house-frame-v1");
            if (identity.UserId != uid) throw new InvalidOperationException("Account changed.");
            var ticket = await RequestUploadAsync("frame", jpeg.Length);
            if (identity.UserId != uid) throw new InvalidOperationException("Account changed.");
            await UploadPhotoAsync(ticket, jpeg, null);
            if (identity.UserId != uid) throw new InvalidOperationException("Account changed.");
            await SendAsync("PUT", "/v1/house/frame", JsonUtility.ToJson(new FrameBody { photoPath = ticket.photoPath }));
        }

        public async Task DeletePhotosAsync() => await SendAsync("DELETE", "/v1/photos", null);
        public async Task DeleteAccountAsync() => await SendAsync("DELETE", "/v1/account", null);

        public async Task<MobileSession> OpenSessionAsync() =>
            JsonUtility.FromJson<MobileSession>(await SendAsync("POST", "/v1/session", "{}")) ?? new MobileSession();
        public async Task AcceptConsentAsync(string version) => await SendAsync("POST", "/v1/consent", JsonUtility.ToJson(new ConsentBody { version = version }));
        public async Task<UploadTicket> RequestUploadAsync(string kind, int bytes) =>
            JsonUtility.FromJson<UploadTicket>(await SendAsync("POST", "/v1/uploads", JsonUtility.ToJson(new UploadBody { kind = kind, bytes = bytes })));
        public async Task<string> CreatePetAsync(string name, string species, string photoPath, string idempotencyKey) =>
            JsonUtility.FromJson<CreationStatus>(await SendAsync("POST", "/v1/pets", JsonUtility.ToJson(new PetBody { name = name, species = species, photoPath = photoPath, idempotencyKey = idempotencyKey }))).petId;
        public async Task CreateOwnerAsync(string photoPath, string idempotencyKey) =>
            await SendAsync("POST", "/v1/owner", JsonUtility.ToJson(new OwnerBody { photoPath = photoPath, idempotencyKey = idempotencyKey }));
        public async Task<CreationStatus> GetPetAsync(string petId) =>
            JsonUtility.FromJson<CreationStatus>(await SendAsync("GET", "/v1/pets/" + Uri.EscapeDataString(petId), null));
        public async Task<CreationStatus> GetOwnerAsync() =>
            JsonUtility.FromJson<CreationStatus>(await SendAsync("GET", "/v1/owner", null));

        /// <summary>PUTs the JPEG to the short-lived signed URL (a Google Storage address, not our server, so no bearer token).</summary>
        public async Task UploadPhotoAsync(UploadTicket ticket, byte[] jpeg, Action<float> progress)
        {
            if (ticket == null || string.IsNullOrEmpty(ticket.uploadUrl)) throw new InvalidOperationException("Upload link missing.");
            for (int attempt = 0; ; attempt++)
            {
                using (var request = new UnityWebRequest(ticket.uploadUrl, "PUT"))
                {
                    request.uploadHandler = new UploadHandlerRaw(jpeg);
                    request.downloadHandler = new DownloadHandlerBuffer();
                    request.SetRequestHeader("Content-Type", "image/jpeg");
                    request.timeout = 120;
                    var operation = request.SendWebRequest();
                    while (!operation.isDone) { progress?.Invoke(request.uploadProgress); await Task.Yield(); }
                    if (request.result == UnityWebRequest.Result.Success) { progress?.Invoke(1f); return; }
                    if (attempt >= 2) throw new MobileApiException("The photo could not be uploaded. Check your connection.", request.responseCode);
                    await Task.Delay(600 * (attempt + 1));
                }
            }
        }

        public async Task DownloadFileAsync(string url, string destinationPath, Action<float> progress)
        {
            string temp = destinationPath + ".part";
            Directory.CreateDirectory(Path.GetDirectoryName(destinationPath));
            using (var request = new UnityWebRequest(url, "GET"))
            {
                request.downloadHandler = new DownloadHandlerFile(temp) { removeFileOnAbort = true };
                request.timeout = 300;
                var operation = request.SendWebRequest();
                while (!operation.isDone) { progress?.Invoke(request.downloadProgress); await Task.Yield(); }
                if (request.result != UnityWebRequest.Result.Success) throw new MobileApiException("The model could not be downloaded. Check your connection.", request.responseCode);
            }
            if (File.Exists(destinationPath)) File.Delete(destinationPath);
            File.Move(temp, destinationPath);
            progress?.Invoke(1f);
        }
    }
}
