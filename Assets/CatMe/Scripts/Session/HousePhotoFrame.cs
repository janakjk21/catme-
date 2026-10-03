using System;
using System.IO;
using CatMe.Garden;
using UnityEngine;
using CatMe.Onboarding;
using System.Threading.Tasks;

namespace CatMe.Session
{
    /// <summary>An account-saved, replaceable photograph in the existing house room.</summary>
    public sealed class HousePhotoFrame : MonoBehaviour
    {
        public Renderer photoSurface;
        public GameObject placeholder;
        public Vector2 openingSize = new Vector2(2.16f, 1.41f);
        Texture2D photo;
        Material material;
        bool picking, syncing;
        string userId;
        IHouseFrameApi api;
        int revision;
#if UNITY_EDITOR
        public static IHouseFrameApi EditorApi;
        public static string EditorPhotoPath; // isolated Play Mode validation never replaces the player's frame photo
#endif
        public static string SavedPhotoPath
        {
            get
            {
#if UNITY_EDITOR
                if (!string.IsNullOrEmpty(EditorPhotoPath)) return EditorPhotoPath;
#endif
                string uid = FindAnyObjectByType<DeviceBridge>()?.AccountService?.CurrentUser?.UserId;
                return Path.Combine(Application.persistentDataPath, "HousePhotos", string.IsNullOrEmpty(uid) ? "offline" : uid, "frame.jpg");
            }
        }
        public bool HasPhoto => photo != null;
        public bool CanInteract => !picking && !syncing;
        bool NeedsSave => File.Exists(SavedPhotoPath + ".unsynced");
        public string ButtonLabel => NeedsSave ? "Save photo" : HasPhoto ? "Change photo" : "Add a photo";

        void Awake()
        {
            if (photoSurface == null) return;
            material = new Material(photoSurface.sharedMaterial);
            photoSurface.sharedMaterial = material;
            RefreshAccount();
        }

        void Update() { RefreshAccount(); }

        string AccountUid()
        {
            string uid = FindAnyObjectByType<DeviceBridge>()?.AccountService?.CurrentUser?.UserId ?? "offline";
#if UNITY_EDITOR
            if (EditorApi != null) uid = "frame-test";
#endif
            return uid;
        }

        void RefreshAccount()
        {
            var device = FindAnyObjectByType<DeviceBridge>();
            string uid = AccountUid();
            if (uid == userId) return;
            revision++; userId = uid; api = null;
            picking = syncing = false;
            ClearDisplayedPhoto(); RestorePhoto();
#if UNITY_EDITOR
            if (EditorApi != null) api = EditorApi;
#endif
            if (api == null && uid != "offline")
            {
                var config = Resources.Load<MobileApiConfig>("MobileApiConfig");
                if (config != null) api = new MobileApi(config, new MobileIdentityService(device.AccountService));
            }
            if (api != null && !NeedsSave) _ = RestoreAccountPhotoAsync(revision);
        }

        async Task RestoreAccountPhotoAsync(int version)
        {
            try
            {
                byte[] bytes = await api.GetHouseFramePhotoAsync();
                if (this == null || version != revision || userId != AccountUid() || NeedsSave) return;
                if (bytes == null)
                {
                    if (File.Exists(SavedPhotoPath)) File.Delete(SavedPhotoPath);
                    ClearDisplayedPhoto(); return;
                }
                string temp = SavedPhotoPath + ".download";
                Directory.CreateDirectory(Path.GetDirectoryName(temp));
                try { File.WriteAllBytes(temp, bytes); SavePhoto(temp, out _, false); }
                finally { if (File.Exists(temp)) File.Delete(temp); }
            }
            catch (Exception) { /* Offline: keep this account's cached photo. */ }
        }

        public void Interact()
        {
            if (!CanInteract) return;
            if (NeedsSave) _ = SaveToAccountAsync(); else ChoosePhoto();
        }

        async Task SaveToAccountAsync()
        {
            if (syncing) return;
            if (api == null) { FindAnyObjectByType<GardenCompanionDemo>()?.SetAppStatus("Sign in to save this photo to your account."); return; }
            syncing = true;
            int version = revision;
            string path = SavedPhotoPath;
            var service = api;
            try
            {
                await service.SaveHouseFramePhotoAsync(File.ReadAllBytes(path));
                if (this == null || version != revision || userId != AccountUid()) return;
                if (File.Exists(path + ".unsynced")) File.Delete(path + ".unsynced");
                FindAnyObjectByType<GardenCompanionDemo>()?.SetAppStatus("Your photo is saved in your account.");
            }
            catch (Exception)
            {
                if (this != null && version == revision && userId == AccountUid()) FindAnyObjectByType<GardenCompanionDemo>()?.SetAppStatus("Saved on this phone. Use Save photo at the frame to try again.");
            }
            finally { if (this != null && version == revision && userId == AccountUid()) syncing = false; }
        }

        public void ChoosePhoto()
        {
            if (!CanInteract) return;
            var device = FindAnyObjectByType<DeviceBridge>();
            if (device == null) return;
            picking = true;
            int version = revision;
            device.RequestPhoto(false, (path, error) =>
            {
                if (this == null || version != revision || userId != AccountUid()) return;
                picking = false;
                var garden = FindAnyObjectByType<GardenCompanionDemo>();
                if (!string.IsNullOrEmpty(path))
                {
                    if (SavePhoto(path, out error)) { garden?.SetAppStatus("Your photo is in the frame. Saving to your account…"); _ = SaveToAccountAsync(); }
                }
                if (!string.IsNullOrEmpty(error)) garden?.SetAppStatus(error);
            }, "Choose a photo for the house frame");
        }

        public bool SavePhoto(string source, out string error, bool markUnsynced = true)
        {
            error = null;
            Texture2D decoded = null;
            try
            {
                byte[] bytes = File.ReadAllBytes(source);
                if (bytes.Length > 9 * 1024 * 1024 || !CatMe.Onboarding.PhotoDimensions.IsSafe(bytes)) throw new IOException("Invalid photo size.");
                decoded = new Texture2D(2, 2, TextureFormat.RGB24, false);
                if (!decoded.LoadImage(bytes)) throw new IOException("Invalid photo.");
                // Keep display memory bounded, including for large photos picked in the editor.
                int maxSide = Mathf.Max(decoded.width, decoded.height);
                if (maxSide > 1536)
                {
                    float scale = 1536f / maxSide;
                    var rt = RenderTexture.GetTemporary(Mathf.RoundToInt(decoded.width * scale), Mathf.RoundToInt(decoded.height * scale));
                    var previous = RenderTexture.active;
                    try
                    {
                        Graphics.Blit(decoded, rt); RenderTexture.active = rt;
                        var small = new Texture2D(rt.width, rt.height, TextureFormat.RGB24, false);
                        small.ReadPixels(new Rect(0, 0, rt.width, rt.height), 0, 0); small.Apply();
                        Destroy(decoded); decoded = small;
                    }
                    finally { RenderTexture.active = previous; RenderTexture.ReleaseTemporary(rt); }
                }
                Directory.CreateDirectory(Path.GetDirectoryName(SavedPhotoPath));
                string pending = SavedPhotoPath + ".pending";
                File.WriteAllBytes(pending, decoded.EncodeToJPG(90));
                if (File.Exists(SavedPhotoPath)) File.Replace(pending, SavedPhotoPath, null); else File.Move(pending, SavedPhotoPath);
                if (markUnsynced) { File.WriteAllText(SavedPhotoPath + ".unsynced", "pending"); revision++; }
                ApplyPhoto(decoded); decoded = null;
                return true;
            }
            catch (Exception e) when (e is IOException || e is UnauthorizedAccessException || e is ArgumentException)
            { error = "That photo could not be saved. Choose another photo or free some space."; return false; }
            finally { if (decoded != null) Destroy(decoded); }
        }

        public void RestorePhoto()
        {
            if (!File.Exists(SavedPhotoPath)) return;
            Texture2D restored = new Texture2D(2, 2, TextureFormat.RGB24, false);
            try { if (restored.LoadImage(File.ReadAllBytes(SavedPhotoPath))) { ApplyPhoto(restored); return; } }
            catch (IOException) { }
            catch (UnauthorizedAccessException) { }
            Destroy(restored);
        }

        void ApplyPhoto(Texture2D texture)
        {
            if (photo != null) Destroy(photo);
            photo = texture;
            material.mainTexture = photo; material.color = Color.white;
            float scale = Mathf.Min(openingSize.x / photo.width, openingSize.y / photo.height);
            photoSurface.transform.localScale = new Vector3(photo.width * scale, photo.height * scale, 1f);
            if (placeholder != null) placeholder.SetActive(false);
        }

        public void ClearDisplayedPhoto()
        {
            revision++; picking = syncing = false;
            if (photo != null) Destroy(photo); photo = null;
            if (material != null) { material.mainTexture = null; material.color = new Color(.94f, .90f, .83f); }
            if (photoSurface != null) photoSurface.transform.localScale = new Vector3(openingSize.x, openingSize.y, 1f);
            if (placeholder != null) placeholder.SetActive(true);
        }

        void OnDestroy() { revision++; if (photo != null) Destroy(photo); if (material != null) Destroy(material); }
    }
}
