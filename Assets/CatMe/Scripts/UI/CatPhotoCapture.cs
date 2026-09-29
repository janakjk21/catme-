using System;
using System.Collections;
using System.IO;
using CatMe.Cat;
using UnityEngine;
using UnityEngine.UI;

namespace CatMe.UI
{
    /// <summary>Captures the room camera without overlay controls and shows the latest local photo or discoveries.</summary>
    [DisallowMultipleComponent]
    public sealed class CatPhotoCapture : MonoBehaviour
    {
        private const int PhotoWidth = 1600;
        private const int PhotoHeight = 900;
        private Camera roomCamera;
        private CatCompanionJournal journal;
        private Button photoButton;
        private Text photoLabel;
        private GameObject overlay;
        private Texture2D previewTexture;
        private string photoPath;
        private bool captureInProgress;
        public bool LastCaptureSucceeded { get; private set; }
        public bool IsOverlayOpen => overlay != null;

        public bool HasRecentPhoto => !string.IsNullOrEmpty(photoPath) && File.Exists(photoPath);
        public string PhotoPath => photoPath;

        public void Initialize(CatCompanionJournal readyJournal)
        {
            if (photoButton != null) return;
            roomCamera = Camera.main;
            journal = readyJournal;
            photoPath = Path.Combine(Application.persistentDataPath, "CatPhotos", "recent.png");
            if (roomCamera == null)
            {
                Debug.LogError("[CatMe][Photo] Main Camera was not found; room photos are unavailable.");
                return;
            }
            CreateControls();
        }

        private void CreateControls()
        {
            Canvas canvas = FindAnyObjectByType<Canvas>();
            if (canvas == null) return;
            Transform parent = canvas.transform.Find("CallSafeArea") ?? canvas.transform;
            photoButton = CreateButton(parent, "Photo", new Vector2(-34f, -122f), CaptureRecentPhoto);
            photoLabel = photoButton.GetComponentInChildren<Text>();
            CreateButton(parent, "Recent", new Vector2(-34f, -194f), ShowRecentPhoto);
            CreateButton(parent, "Memories", new Vector2(-34f, -266f), ShowMemories);
        }

        private static Button CreateButton(Transform parent, string title, Vector2 position, UnityEngine.Events.UnityAction callback)
        {
            GameObject root = new GameObject(title.Replace(" ", string.Empty) + "Button", typeof(RectTransform), typeof(Image), typeof(Button));
            root.transform.SetParent(parent, false);
            RectTransform rect = root.GetComponent<RectTransform>();
            rect.anchorMin = rect.anchorMax = new Vector2(1f, 1f);
            rect.pivot = new Vector2(1f, 1f);
            rect.anchoredPosition = position;
            rect.sizeDelta = new Vector2(160f, 62f);
            Image image = root.GetComponent<Image>();
            image.color = new Color(0.20f, 0.24f, 0.28f, 0.94f);
            Button button = root.GetComponent<Button>();
            button.targetGraphic = image;
            button.onClick.AddListener(callback);
            GameObject label = new GameObject("Label", typeof(RectTransform), typeof(Text));
            label.transform.SetParent(root.transform, false);
            RectTransform labelRect = label.GetComponent<RectTransform>();
            labelRect.anchorMin = Vector2.zero; labelRect.anchorMax = Vector2.one;
            labelRect.offsetMin = labelRect.offsetMax = Vector2.zero;
            Text text = label.GetComponent<Text>();
            text.text = title;
            text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            text.fontSize = 20; text.fontStyle = FontStyle.Bold;
            text.color = Color.white; text.alignment = TextAnchor.MiddleCenter; text.raycastTarget = false;
            return button;
        }

        private void CaptureRecentPhoto()
        {
            if (!captureInProgress) StartCoroutine(CaptureAfterUiFrame(false));
        }

        private IEnumerator CaptureAfterUiFrame(bool validationCapture)
        {
            captureInProgress = true;
            if (photoButton != null) photoButton.interactable = false;
            if (!validationCapture) yield return new WaitForEndOfFrame();
            RenderTexture previousTarget = roomCamera.targetTexture;
            float previousAspect = roomCamera.aspect;
            RenderTexture target = new RenderTexture(PhotoWidth, PhotoHeight, 24, RenderTextureFormat.ARGB32);
            Texture2D image = new Texture2D(PhotoWidth, PhotoHeight, TextureFormat.RGB24, false);
            RenderTexture previousActive = RenderTexture.active;
            try
            {
                roomCamera.targetTexture = target;
                roomCamera.aspect = (float)PhotoWidth / PhotoHeight;
                roomCamera.Render();
                RenderTexture.active = target;
                image.ReadPixels(new Rect(0f, 0f, PhotoWidth, PhotoHeight), 0, 0);
                image.Apply(false, false);
                Directory.CreateDirectory(Path.GetDirectoryName(photoPath));
                string temporary = photoPath + ".tmp";
                File.WriteAllBytes(temporary, image.EncodeToPNG());
                if (File.Exists(photoPath)) File.Replace(temporary, photoPath, null);
                else File.Move(temporary, photoPath);
                LastCaptureSucceeded = true;
                journal?.RecordDiscovery("first_room_photo");
                if (photoLabel != null) photoLabel.text = "Saved!";
                Debug.Log("[CatMe][Photo] Saved recent room photo | size=1600x900");
            }
            catch (Exception exception)
            {
                LastCaptureSucceeded = false;
                if (photoLabel != null) photoLabel.text = "Photo failed";
                Debug.LogWarning("[CatMe][Photo] Capture failed safely: " + exception.GetType().Name);
            }
            finally
            {
                roomCamera.targetTexture = previousTarget;
                roomCamera.aspect = previousAspect;
                RenderTexture.active = previousActive;
                target.Release();
                Destroy(target);
                Destroy(image);
                captureInProgress = false;
                if (photoButton != null) photoButton.interactable = true;
            }
            yield return new WaitForSecondsRealtime(1.5f);
            if (photoLabel != null) photoLabel.text = "Photo";
        }

        private void ShowMemories()
        {
            CloseOverlay();
            Canvas canvas = FindAnyObjectByType<Canvas>();
            if (canvas == null) return;
            Transform parent = canvas.transform.Find("CallSafeArea") ?? canvas.transform;
            overlay = CreateOverlay(parent, "Cat memories");
            string body = journal == null || journal.Discoveries.Count == 0
                ? "New moments will appear here as you play."
                : FormatDiscoveries(journal.Discoveries);
            AddOverlayText(overlay.transform, body, new Vector2(0.08f, 0.28f), new Vector2(0.92f, 0.84f));
            CreateButton(overlay.transform, "Close", new Vector2(-20f, -20f), CloseOverlay);
        }

        private void ShowRecentPhoto()
        {
            if (!HasRecentPhoto)
            {
                ShowMemoriesOrPhoto();
                return;
            }
            CloseOverlay();
            if (previewTexture != null) Destroy(previewTexture);
            previewTexture = new Texture2D(2, 2, TextureFormat.RGB24, false);
            try
            {
                if (!previewTexture.LoadImage(File.ReadAllBytes(photoPath))) throw new InvalidDataException("photo data");
            }
            catch (Exception exception)
            {
                Destroy(previewTexture);
                previewTexture = null;
                Debug.LogWarning("[CatMe][Photo] Recent photo could not be opened: " + exception.GetType().Name);
                ShowMemoriesOrPhoto();
                return;
            }
            Canvas canvas = FindAnyObjectByType<Canvas>();
            if (canvas == null) return;
            Transform parent = canvas.transform.Find("CallSafeArea") ?? canvas.transform;
            overlay = CreateOverlay(parent, "Recent photo");
            GameObject preview = new GameObject("Photo", typeof(RectTransform), typeof(RawImage));
            preview.transform.SetParent(overlay.transform, false);
            RectTransform rect = preview.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.10f, 0.16f); rect.anchorMax = new Vector2(0.90f, 0.86f);
            rect.offsetMin = rect.offsetMax = Vector2.zero;
            preview.GetComponent<RawImage>().texture = previewTexture;
            CreateButton(overlay.transform, "Close", new Vector2(-20f, -20f), CloseOverlay);
        }

        private void ShowMemoriesOrPhoto()
        {
            if (HasRecentPhoto) ShowRecentPhoto();
            else ShowMemories();
        }

        private GameObject CreateOverlay(Transform parent, string title)
        {
            if (overlay != null) Destroy(overlay);
            GameObject root = new GameObject("CatPhotoOverlay", typeof(RectTransform), typeof(Image));
            root.transform.SetParent(parent, false);
            RectTransform rect = root.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.04f, 0.04f); rect.anchorMax = new Vector2(0.96f, 0.96f);
            rect.offsetMin = rect.offsetMax = Vector2.zero;
            root.GetComponent<Image>().color = new Color(0.10f, 0.12f, 0.15f, 0.97f);
            AddOverlayText(root.transform, title, new Vector2(0.08f, 0.88f), new Vector2(0.92f, 0.96f));
            return root;
        }

        private static void AddOverlayText(Transform parent, string value, Vector2 anchorMin, Vector2 anchorMax)
        {
            GameObject label = new GameObject("Text", typeof(RectTransform), typeof(Text));
            label.transform.SetParent(parent, false);
            RectTransform rect = label.GetComponent<RectTransform>();
            rect.anchorMin = anchorMin; rect.anchorMax = anchorMax;
            rect.offsetMin = rect.offsetMax = Vector2.zero;
            Text text = label.GetComponent<Text>();
            text.text = value; text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            text.fontSize = 25; text.color = Color.white; text.alignment = TextAnchor.UpperLeft;
            text.horizontalOverflow = HorizontalWrapMode.Wrap; text.verticalOverflow = VerticalWrapMode.Overflow;
            text.raycastTarget = false;
        }

        private static string FormatDiscoveries(System.Collections.Generic.IReadOnlyList<string> discoveries)
        {
            string result = string.Empty;
            for (int index = discoveries.Count - 1; index >= 0; index--)
            {
                string label = discoveries[index].Replace('_', ' ');
                if (label.Length > 0) label = char.ToUpperInvariant(label[0]) + label.Substring(1);
                result += "• " + label + "\n";
            }
            return result.TrimEnd();
        }

        private void CloseOverlay()
        {
            if (overlay != null) Destroy(overlay);
            overlay = null;
            if (previewTexture != null) Destroy(previewTexture);
            previewTexture = null;
        }

        private void OnDestroy() { CloseOverlay(); }

#if UNITY_EDITOR
        public void CaptureForValidation(string isolatedPath)
        {
            photoPath = isolatedPath;
            LastCaptureSucceeded = false;
            if (!captureInProgress) StartCoroutine(CaptureAfterUiFrame(true));
        }

        public void OpenRecentPhotoForValidation() => ShowRecentPhoto();
        public void CloseOverlayForValidation() => CloseOverlay();
#endif

    }
}
