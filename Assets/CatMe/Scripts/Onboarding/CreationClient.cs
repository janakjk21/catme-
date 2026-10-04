using System;
using System.IO;
using System.Threading.Tasks;
using UnityEngine;

namespace CatMe.Onboarding
{
    public sealed class CreationRequest
    {
        public string petName, species, petPhotoPath, ownerPhotoPath;      // species: "cat" or "dog"
        public string petModelDestination, ownerModelDestination;          // where downloaded GLBs are saved
        public Task<string> ownerPhotoTask;                                // selfie path (or null for none), supplied after the pet job has started
    }

    public sealed class CreationResult
    {
        public bool ok;
        public string petId, error, rejectReason, fixtureKey;
        public bool petModelSaved, ownerModelSaved, isFixture;
    }

    /// <summary>
    /// Runs one creation from the phone: consent, resize, private upload, create the pet (and optionally the owner), follow the server's stages,
    /// and download the finished models. All provider work happens on the server; this only talks to the WornTag API.
    /// </summary>
    public sealed class CreationClient
    {
        public const string ConsentVersion = "2026-10-04";   // raise when the consent screen, privacy policy or terms change
        const string ConsentKey = "CatMe.Consent.Version";
        public const string PrivacyUrl = "https://worntag.com/privacy.html", TermsUrl = "https://worntag.com/terms.html";

        /// <summary>True once the player has tapped "I agree" on the consent screen for the current version.</summary>
        public static bool ConsentGiven => PlayerPrefs.GetString(ConsentKey, "") == ConsentVersion;
        public static void RecordConsent() { PlayerPrefs.SetString(ConsentKey, ConsentVersion); PlayerPrefs.Save(); }
        public const string PendingKey = "CatMe.Creation.Pending.v1";
        const int MaxSide = 1536;

        readonly IMobileApi api;
        readonly Func<int, Task> wait;
        public CreationClient(IMobileApi api) : this(api, Task.Delay) { }
        public CreationClient(IMobileApi api, Func<int, Task> wait)
        {
            this.api = api ?? throw new ArgumentNullException(nameof(api));
            this.wait = wait ?? throw new ArgumentNullException(nameof(wait));
        }

        /// <summary>Key used by builds before 2026-10; moved to <see cref="PendingKey"/> once.</summary>
        public const string LegacyPendingKey = "WornTag.Creation.Pending";

        // species and name are kept so a resumed creation shows the right animal and name before the server answers.
        [Serializable] sealed class Pending { public string photo, petKey, ownerKey, petId, species, name; }

        /// <summary>Moves a pending creation saved under the old key to the current key, once. Returns true if the old key existed.</summary>
        public static bool MigrateLegacyPending()
        {
            if (!PlayerPrefs.HasKey(LegacyPendingKey)) return false;
            string old = PlayerPrefs.GetString(LegacyPendingKey, "");
            PlayerPrefs.DeleteKey(LegacyPendingKey);
            if (!PlayerPrefs.HasKey(PendingKey) && !string.IsNullOrEmpty(old)) PlayerPrefs.SetString(PendingKey, old);
            PlayerPrefs.Save();
            return true;
        }

        static Pending ReadPending()
        {
            MigrateLegacyPending();
            try { return JsonUtility.FromJson<Pending>(PlayerPrefs.GetString(PendingKey, "")); }
            catch { return null; }
        }

        public static string PendingPetId => ReadPending()?.petId;
        /// <summary>"cat" or "dog" for the creation in progress, or null when unknown (very old pending records).</summary>
        public static string PendingSpecies { get { string s = ReadPending()?.species; return s == "cat" || s == "dog" ? s : null; } }
        public static string PendingName { get { string n = ReadPending()?.name; return string.IsNullOrWhiteSpace(n) ? null : n; } }

        /// <summary>Continues an existing server job. This path never uploads a photo or starts another creation.</summary>
        public async Task<CreationResult> ResumeAsync(string petId, string petModelDestination, string ownerModelDestination, Action<string, string> report)
        {
            var result = new CreationResult { petId = petId };
            if (string.IsNullOrEmpty(petId)) { result.error = "We couldn't find the pet being made. Try again."; return result; }
            try
            {
                CreationStatus pet = await PollPetAsync(petId, report);
                if (!FinishPetStatus(pet, result)) return result;
                await DownloadPetAsync(pet, petModelDestination, result, report);
                CreationStatus owner = await SafeOwnerAsync();
                if (owner != null && owner.status != "failed" && owner.status != "rejected")
                {
                    for (int i = 0; i < 900 && owner.status != "ready"; i++) { await wait(2000); owner = await SafeOwnerAsync(); }
                    if (owner?.status == "ready" && !string.IsNullOrEmpty(owner.modelUrl) && !string.IsNullOrEmpty(ownerModelDestination))
                    { await api.DownloadFileAsync(owner.modelUrl, ownerModelDestination, null); result.ownerModelSaved = true; }
                }
                result.ok = true; ClearPending(); return result;
            }
            catch (Exception e)
            {
                Debug.LogWarning("[WornTag][Creation] resume failed: " + e.GetType().Name + ": " + e.Message);
                result.error = e is MobileApiException || e is InvalidOperationException ? e.Message : "Something went wrong. Check your connection and try again.";
                return result;
            }
        }

        /// <param name="report">Called with the server stage name (checking, queued, generating, ready) and a short message.</param>
        public async Task<CreationResult> RunAsync(CreationRequest request, Action<string, string> report)
        {
            var result = new CreationResult();
            // Nothing is uploaded until the player has agreed on the consent screen (OnboardingFlow.ShowConsent).
            if (!ConsentGiven) { result.error = "Please agree to the photo terms before we make your pet."; return result; }
            try
            {
                report?.Invoke("checking", "Preparing your photo");
                byte[] petJpeg = PrepareJpeg(request.petPhotoPath);

                // Reuse the same keys if this exact photo was already submitted (app killed or connection lost): the server then returns the existing job.
                var pending = LoadPending(request.petPhotoPath) ?? new Pending { photo = request.petPhotoPath, petKey = Guid.NewGuid().ToString(), ownerKey = Guid.NewGuid().ToString() };
                pending.species = request.species; pending.name = request.petName;
                SavePending(pending);

                await api.AcceptConsentAsync(CreationClient.ConsentVersion);
                var ticket = await api.RequestUploadAsync("pet", petJpeg.Length);
                await api.UploadPhotoAsync(ticket, petJpeg, null);
                pending.petId = await api.CreatePetAsync(request.petName, request.species, ticket.photoPath, pending.petKey);
                SavePending(pending);
                result.petId = pending.petId;

                // The pet is already being made on the server; the selfie may arrive later (it is asked for after the pet photo).
                string ownerPath = request.ownerPhotoPath;
                if (string.IsNullOrEmpty(ownerPath) && request.ownerPhotoTask != null) ownerPath = await request.ownerPhotoTask;
                byte[] ownerJpeg = string.IsNullOrEmpty(ownerPath) ? null : PrepareJpeg(ownerPath);
                if (ownerJpeg != null)
                {
                    var ownerTicket = await api.RequestUploadAsync("owner", ownerJpeg.Length);
                    await api.UploadPhotoAsync(ownerTicket, ownerJpeg, null);
                    try { await api.CreateOwnerAsync(ownerTicket.photoPath, pending.ownerKey); }
                    catch (MobileApiException e) when (e.StatusCode == 409) { /* an owner avatar already exists or is running: keep going */ }
                }

                CreationStatus pet = await PollPetAsync(pending.petId, report), owner = null;
                if (!FinishPetStatus(pet, result)) return result;
                await DownloadPetAsync(pet, request.petModelDestination, result, report);
                if (ownerJpeg != null)
                {
                    for (int i = 0; i < 900; i++)   // up to 30 minutes, every 2 seconds
                    {
                        owner = await SafeOwnerAsync();
                        if (owner != null && (owner.status == "ready" || owner.status == "failed" || owner.status == "rejected")) break;
                        await wait(2000);
                    }
                    if (owner != null && owner.status == "ready" && !string.IsNullOrEmpty(owner.modelUrl) && !string.IsNullOrEmpty(request.ownerModelDestination))
                    {
                        await api.DownloadFileAsync(owner.modelUrl, request.ownerModelDestination, null);
                        result.ownerModelSaved = true;
                    }
                }
                result.ok = true;
                ClearPending();
                return result;
            }
            catch (Exception e)
            {
                Debug.LogWarning("[WornTag][Creation] failed: " + e.GetType().Name + ": " + e.Message);
                result.error = e is MobileApiException || e is InvalidOperationException ? e.Message : "Something went wrong. Check your connection and try again.";
                return result;
            }
        }

        async Task<CreationStatus> SafeOwnerAsync()
        {
            try { return await api.GetOwnerAsync(); }
            catch (MobileApiException) { return null; }
        }

        async Task<CreationStatus> PollPetAsync(string petId, Action<string, string> report)
        {
            var deadline = DateTime.UtcNow.AddMinutes(30);
            CreationStatus pet = null;
            while (DateTime.UtcNow < deadline)
            {
                pet = await api.GetPetAsync(petId);
                if (pet != null && !string.IsNullOrEmpty(pet.stage)) report?.Invoke(pet.stage, null);
                if (pet != null && (pet.status == "ready" || pet.status == "failed" || pet.status == "rejected")) break;
                await wait(2000);
            }
            return pet;
        }

        static bool FinishPetStatus(CreationStatus pet, CreationResult result)
        {
            if (pet == null || pet.status == "failed") { result.error = FailedMessage(pet?.errorCode); ClearPending(); return false; }
            if (pet.status == "rejected") { result.rejectReason = pet.rejectReason; result.error = RejectMessage(pet.rejectReason); ClearPending(); return false; }
            if (pet.status != "ready") { result.error = "This is taking longer than expected. Your pet will keep being made; open the app again soon."; return false; }
            result.isFixture = pet.isFixture; result.fixtureKey = pet.fixtureKey;
            return true;
        }

        async Task DownloadPetAsync(CreationStatus pet, string destination, CreationResult result, Action<string, string> report)
        {
            if (string.IsNullOrEmpty(pet.modelUrl) || string.IsNullOrEmpty(destination)) return;
            report?.Invoke("downloading", "Bringing them home");
            await api.DownloadFileAsync(pet.modelUrl, destination, null);
            result.petModelSaved = true;
        }

        public const string CheckUnavailableMessage = "We couldn't check your photo just now. Try again in a minute.";

        /// <summary>The line for a failed (not rejected) creation. The photo check being down is not the player's fault, and no credit was used.</summary>
        public static string FailedMessage(string errorCode)
            => errorCode == "CHECK_UNAVAILABLE" ? CheckUnavailableMessage : "We couldn't make your pet this time. Try again.";

        public static string RejectMessage(string reason)
        {
            switch (reason)
            {
                case "not_a_cat": return "We can't see a cat in this photo. Try another one.";
                case "not_a_dog": return "We can't see a dog in this photo. Try another one.";
                case "multiple_subjects": return "There is more than one in this photo. Try one with only your pet.";
                case "too_dark": return "It's too dark to see them. Try again near a window.";
                case "unsafe": return "This photo can't be used. Please choose a different one.";
                case "too_blurry": return "That photo is blurry. A sharp, well-lit photo works best.";
                case "too_small": return "That photo is too small. Pick one at least 512 pixels wide.";
                case "pet_not_riggable": return "We could not build a walking body from that photo. Use a clear photo that shows the whole animal.";
                case "pet_generate_failed": return "We could not make a 3D model from that photo. Try one with better light.";
                case "owner_reference_failed": return "We could not use that selfie. Use one clear photo of one person in good light.";
                case "not_an_image": case "photo_missing": return "That photo could not be read. Choose another.";
                default: return "That photo could not be used. Try another.";
            }
        }

        // Decode, shrink to at most 1536 px on the long side and re-encode, so uploads stay far below the 8 MB limit on any phone.
        static byte[] PrepareJpeg(string path)
        {
            if (string.IsNullOrEmpty(path) || !File.Exists(path)) throw new InvalidOperationException("The photo could not be found on this phone.");
            var source = new Texture2D(2, 2, TextureFormat.RGB24, false);
            try
            {
                if (!source.LoadImage(File.ReadAllBytes(path))) throw new InvalidOperationException("That photo could not be read.");
                int w = source.width, h = source.height;
                float scale = Mathf.Min(1f, MaxSide / (float)Mathf.Max(w, h));
                if (scale >= 1f) return source.EncodeToJPG(88);
                int nw = Mathf.Max(1, Mathf.RoundToInt(w * scale)), nh = Mathf.Max(1, Mathf.RoundToInt(h * scale));
                var rt = RenderTexture.GetTemporary(nw, nh, 0, RenderTextureFormat.ARGB32);
                var previous = RenderTexture.active;
                Graphics.Blit(source, rt);
                RenderTexture.active = rt;
                var small = new Texture2D(nw, nh, TextureFormat.RGB24, false);
                small.ReadPixels(new Rect(0, 0, nw, nh), 0, 0); small.Apply();
                RenderTexture.active = previous; RenderTexture.ReleaseTemporary(rt);
                byte[] jpeg = small.EncodeToJPG(88);
                UnityEngine.Object.Destroy(small);
                return jpeg;
            }
            finally { UnityEngine.Object.Destroy(source); }
        }

        static Pending LoadPending(string photo)
        {
            var p = ReadPending();
            return p != null && p.photo == photo && !string.IsNullOrEmpty(p.petKey) ? p : null;
        }
        static void SavePending(Pending p) { PlayerPrefs.SetString(PendingKey, JsonUtility.ToJson(p)); PlayerPrefs.Save(); }
        static void ClearPending() { PlayerPrefs.DeleteKey(PendingKey); PlayerPrefs.Save(); }
    }
}
