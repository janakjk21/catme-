using System;
using System.Collections;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace CatMe.Garden
{
    /// <summary>Local prototype onboarding and photo-to-character flows for the Garden scene.</summary>
    [DisallowMultipleComponent]
    public sealed class CompanionCreationPanel : MonoBehaviour
    {
        private const long MaxPhotoBytes = 9L * 1024L * 1024L;
        private const long MaxModelBytes = 200L * 1024L * 1024L;
        private static string ProfileDirectory => Path.Combine(Application.persistentDataPath, "Companion");
        public static string CatPhotoPath => Path.Combine(ProfileDirectory, "cat-photo.jpg");
        public static string HumanPhotoPath => Path.Combine(ProfileDirectory, "person-photo.jpg");
        public static string CatModelPath => Path.Combine(ProfileDirectory, "cat-walking.glb");
        public static string HumanModelPath => Path.Combine(ProfileDirectory, "person-walking.glb");

        [Serializable]
        private sealed class Job
        {
            public string id;
            public string phase;
            public string status;
            public string error;
            public int progress;
        }

        private Transform parent;
        private GameObject panel, overlay;
        private Text catPhotoState, humanPhotoState, catJobState, humanJobState;
        private RawImage catPreview, humanPreview;
        private Button catPick, humanPick, catCreate, humanCreate;
        private bool running;
        private bool pickerForHuman;
        private string selectedCatPath, selectedHumanPath;
        private Coroutine generation;
        private const string ReceiverName = "CatMeCompanionCreationPanel";

        public void Initialize(Transform canvasRoot)
        {
            parent = canvasRoot;
            gameObject.name = ReceiverName;
            Build();
            selectedCatPath = ExistingOrNull(CatPhotoPath);
            selectedHumanPath = ExistingOrNull(HumanPhotoPath);
            RefreshPhotoCard(false);
            RefreshPhotoCard(true);
            overlay.SetActive(false);
        }

        public void Toggle() { if (overlay != null) overlay.SetActive(!overlay.activeSelf); }
        public void Close() { if (overlay != null) overlay.SetActive(false); }

        private void Build()
        {
            GameObject shade = new GameObject("CompanionSetupOverlay", typeof(RectTransform), typeof(Image));
            overlay = shade;
            shade.transform.SetParent(parent, false);
            RectTransform shadeRect = shade.GetComponent<RectTransform>();
            shadeRect.anchorMin = Vector2.zero; shadeRect.anchorMax = Vector2.one; shadeRect.offsetMin = Vector2.zero; shadeRect.offsetMax = Vector2.zero;
            shade.GetComponent<Image>().color = new Color(0.035f, 0.065f, 0.05f, 0.78f);

            panel = new GameObject("CompanionSetupCard", typeof(RectTransform), typeof(Image));
            panel.transform.SetParent(shade.transform, false);
            RectTransform card = panel.GetComponent<RectTransform>();
            card.anchorMin = new Vector2(0.5f, 0.5f); card.anchorMax = new Vector2(0.5f, 0.5f); card.pivot = new Vector2(0.5f, 0.5f);
            card.sizeDelta = new Vector2(1300f, 820f);
            panel.GetComponent<Image>().color = new Color(0.965f, 0.946f, 0.89f, 1f);

            Label(panel.transform, "Eyebrow", "CATME  /  YOUR COMPANIONS", 22, new Color(0.3f, 0.42f, 0.29f), new Vector2(0f, 340f), new Vector2(1120f, 42f), FontStyle.Bold, TextAnchor.MiddleLeft);
            Label(panel.transform, "Title", "Make this garden yours", 48, new Color(0.12f, 0.19f, 0.15f), new Vector2(0f, 284f), new Vector2(1120f, 70f), FontStyle.Bold, TextAnchor.MiddleLeft);
            Label(panel.transform, "Intro", "Add your person and your pet. Their walking characters stay saved on this phone.", 25, new Color(0.32f, 0.34f, 0.3f), new Vector2(0f, 232f), new Vector2(1120f, 48f), FontStyle.Normal, TextAnchor.MiddleLeft);
            MakeButton(panel.transform, "Close", "×", new Vector2(586f, 342f), new Vector2(58f, 58f), new Color(0.86f, 0.84f, 0.77f), Close, 34);

            BuildProfileCard(false, -288f);
            BuildProfileCard(true, 288f);

            Label(panel.transform, "ProviderMode", "DIRECT PROVIDER CONNECTION  ·  LOCAL PROTOTYPE", 16, new Color(0.36f, 0.4f, 0.34f), new Vector2(-345f, -346f), new Vector2(470f, 30f), FontStyle.Bold, TextAnchor.MiddleLeft);
            Label(panel.transform, "PipelineNote", "Cat: Tripo  ·  Person: Meshy  ·  Photos and models stay on this phone", 17, new Color(0.38f, 0.39f, 0.35f), new Vector2(-115f, -393f), new Vector2(850f, 26f), FontStyle.Normal, TextAnchor.MiddleCenter);

            Button done = MakeButton(panel.transform, "Done", "Back to garden", new Vector2(472f, -345f), new Vector2(230f, 62f), new Color(0.18f, 0.33f, 0.24f), Close, 22);
            done.GetComponentInChildren<Text>().fontStyle = FontStyle.Bold;
        }

        private void BuildProfileCard(bool human, float x)
        {
            string title = human ? "Your person" : "Your cat";
            string name = human ? "Person" : "Cat";
            GameObject card = new GameObject(name + "ProfileCard", typeof(RectTransform), typeof(Image)); card.transform.SetParent(panel.transform, false);
            RectTransform rect = card.GetComponent<RectTransform>(); rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f); rect.sizeDelta = new Vector2(550f, 490f); rect.anchoredPosition = new Vector2(x, -30f);
            card.GetComponent<Image>().color = Color.white;
            Label(card.transform, "Title", title, 34, new Color(0.14f, 0.2f, 0.16f), new Vector2(0f, 196f), new Vector2(480f, 54f), FontStyle.Bold, TextAnchor.MiddleLeft);
            Label(card.transform, "Provider", human ? "MESHY  /  STANDING FULL-BODY PHOTO" : "TRIPO  /  FULL-BODY CAT PHOTO", 16, new Color(0.39f, 0.47f, 0.37f), new Vector2(0f, 157f), new Vector2(480f, 28f), FontStyle.Bold, TextAnchor.MiddleLeft);
            GameObject preview = new GameObject("PhotoPreview", typeof(RectTransform), typeof(RawImage)); preview.transform.SetParent(card.transform, false);
            RectTransform previewRect = preview.GetComponent<RectTransform>(); previewRect.anchorMin = previewRect.anchorMax = new Vector2(0.5f, 0.5f); previewRect.sizeDelta = new Vector2(250f, 205f); previewRect.anchoredPosition = new Vector2(-120f, 8f);
            preview.GetComponent<RawImage>().color = new Color(0.92f, 0.92f, 0.88f);
            if (human) humanPreview = preview.GetComponent<RawImage>(); else catPreview = preview.GetComponent<RawImage>();
            Text photoState = Label(card.transform, "PhotoState", "No photo selected", 19, new Color(0.34f, 0.37f, 0.33f), new Vector2(114f, 8f), new Vector2(190f, 88f), FontStyle.Normal, TextAnchor.MiddleLeft);
            Text jobState = Label(card.transform, "JobState", "Ready when you are", 16, new Color(0.39f, 0.43f, 0.37f), new Vector2(0f, -121f), new Vector2(480f, 52f), FontStyle.Normal, TextAnchor.MiddleCenter);
            if (human) { humanPhotoState = photoState; humanJobState = jobState; }
            else { catPhotoState = photoState; catJobState = jobState; }

            Button choose = MakeButton(card.transform, "Choose", "Choose photo", new Vector2(-126f, -183f), new Vector2(220f, 60f), new Color(0.87f, 0.88f, 0.82f), () => PickPhoto(human), 20);
            choose.GetComponentInChildren<Text>().color = new Color(0.16f, 0.24f, 0.18f);
            Button create = MakeButton(card.transform, "Create", human ? "Create person" : "Create cat", new Vector2(124f, -183f), new Vector2(220f, 60f), new Color(0.19f, 0.36f, 0.26f), () => BeginGeneration(human), 20);
            create.GetComponentInChildren<Text>().fontStyle = FontStyle.Bold;
            if (human) { humanPick = choose; humanCreate = create; } else { catPick = choose; catCreate = create; }
        }

        private void PickPhoto(bool human)
        {
            if (running) return;
            pickerForHuman = human;
#if UNITY_EDITOR
            string photo = EditorUtility.OpenFilePanel(human ? "Choose a full-body person photo" : "Choose your cat photo", "", "png,jpg,jpeg");
            OnPhotoPicked(photo);
#elif UNITY_IOS && !UNITY_EDITOR
            if (human) CatMePhotoPicker.OpenHuman(gameObject.name); else CatMePhotoPicker.Open(gameObject.name);
#else
            SetJobStatus(human, "Photo picker is available in the iOS app and Unity Editor.");
#endif
        }

        public void OnCatPhotoPicked(string path) { pickerForHuman = false; OnPhotoPicked(path); }
        public void OnHumanPhotoPicked(string path) { pickerForHuman = true; OnPhotoPicked(path); }

        private void OnPhotoPicked(string path)
        {
            bool human = pickerForHuman;
            if (string.IsNullOrWhiteSpace(path) || !File.Exists(path)) return;
            FileInfo info = new FileInfo(path);
            if (info.Length <= 0 || info.Length > MaxPhotoBytes) { SetJobStatus(human, "Choose a photo smaller than 9 MB for direct provider upload."); return; }
            try
            {
                Directory.CreateDirectory(ProfileDirectory);
                string destination = human ? HumanPhotoPath : CatPhotoPath;
                byte[] source = File.ReadAllBytes(path);
                Texture2D decoded = new Texture2D(2, 2, TextureFormat.RGBA32, false);
                if (!decoded.LoadImage(source)) { Destroy(decoded); SetJobStatus(human, "That image could not be opened. Choose a JPG or PNG photo."); return; }
                byte[] normalized = decoded.EncodeToJPG(88);
                Destroy(decoded);
                if (normalized == null || normalized.LongLength <= 0 || normalized.LongLength > MaxPhotoBytes)
                { SetJobStatus(human, "Choose a photo that compresses below 9 MB."); return; }
                File.WriteAllBytes(destination, normalized);
                if (human) selectedHumanPath = destination; else selectedCatPath = destination;
                RefreshPhotoCard(human);
                SetJobStatus(human, "Photo saved on this phone. Ready to create the walking character.");
            }
            catch (Exception exception) { SetJobStatus(human, "Could not save this photo: " + exception.Message); }
        }

        private void RefreshPhotoCard(bool human)
        {
            string path = human ? selectedHumanPath : selectedCatPath;
            Text state = human ? humanPhotoState : catPhotoState;
            RawImage preview = human ? humanPreview : catPreview;
            Button create = human ? humanCreate : catCreate;
            if (state != null) state.text = string.IsNullOrEmpty(path) ? "Add a clear full-body photo" : "Photo saved\non this phone";
            if (create != null) create.interactable = !string.IsNullOrEmpty(path);
            if (preview == null || string.IsNullOrEmpty(path) || !File.Exists(path)) return;
            try
            {
                byte[] bytes = File.ReadAllBytes(path); Texture2D image = new Texture2D(2, 2, TextureFormat.RGBA32, false);
                if (image.LoadImage(bytes)) { preview.texture = image; preview.color = Color.white; }
                else Destroy(image);
            }
            catch { }
        }

        private void BeginGeneration(bool human)
        {
            if (running) return;
            string photo = human ? selectedHumanPath : selectedCatPath;
            if (string.IsNullOrEmpty(photo) || !File.Exists(photo)) { SetJobStatus(human, "Choose a photo first."); return; }
            if (!CompanionProviderApi.IsConfigured(human, out string reason))
            { SetJobStatus(human, reason); return; }
            generation = StartCoroutine(Generate(human, photo));
        }

        private IEnumerator Generate(bool human, string photoPath)
        {
            running = true; SetButtons(false);
            byte[] photo;
            try { photo = File.ReadAllBytes(photoPath); }
            catch (Exception exception) { SetJobStatus(human, "Photo could not be read: " + exception.Message); Finish(); yield break; }
            SetJobStatus(human, human ? "Sending your photo directly to Meshy…" : "Sending your photo directly to Tripo…");
            Task<byte[]> task = human
                ? CompanionProviderApi.CreateWalkingHuman(photo, phase => SetJobStatus(true, phase))
                : CompanionProviderApi.CreateWalkingCat(photo, phase => SetJobStatus(false, phase));
            yield return new WaitUntil(() => task.IsCompleted);
            if (task.IsFaulted || task.IsCanceled)
            {
                string message = task.Exception?.GetBaseException().Message ?? "Provider request was cancelled.";
                SetJobStatus(human, "Character creation failed: " + message); Finish(); yield break;
            }
            byte[] bytes = task.Result;
            if (!IsGlb(bytes)) { SetJobStatus(human, "The provider returned an invalid walking character file."); Finish(); yield break; }
            string destination = human ? HumanModelPath : CatModelPath;
            try
            {
                string pending = destination + ".pending"; File.WriteAllBytes(pending, bytes);
                if (File.Exists(destination)) File.Delete(destination);
                File.Move(pending, destination);
            }
            catch (Exception exception) { SetJobStatus(human, "Could not save the character on this phone: " + exception.Message); Finish(); yield break; }
            SetJobStatus(human, "Walking character saved on this phone. Updating the garden…");
            yield return new WaitForSecondsRealtime(0.5f);
            GardenCompanionDemo garden = GetComponent<GardenCompanionDemo>();
            if (garden != null) yield return StartCoroutine(garden.ReloadCharactersFromPhoneStorage());
            SetJobStatus(human, "Your character is ready in the garden and saved on this phone.");
            Finish();
        }

        private void SetJobStatus(bool human, string message) { Text target = human ? humanJobState : catJobState; if (target != null) target.text = message; }
        private void SetButtons(bool enabledState) { if (catPick != null) catPick.interactable = enabledState; if (humanPick != null) humanPick.interactable = enabledState; if (catCreate != null) catCreate.interactable = enabledState && !string.IsNullOrEmpty(selectedCatPath); if (humanCreate != null) humanCreate.interactable = enabledState && !string.IsNullOrEmpty(selectedHumanPath); }
        private void Finish() { running = false; SetButtons(true); generation = null; }
        private static string ExistingOrNull(string path) => File.Exists(path) ? path : null;
        private static bool IsGlb(byte[] data) => data != null && data.Length >= 20 && data.LongLength <= MaxModelBytes && data[0] == 0x67 && data[1] == 0x6c && data[2] == 0x54 && data[3] == 0x46 && BitConverter.ToUInt32(data, 8) == (uint)data.Length;

        private static Text Label(Transform root, string name, string value, int size, Color color, Vector2 position, Vector2 dimensions, FontStyle style, TextAnchor alignment)
        {
            GameObject go = new GameObject(name, typeof(RectTransform), typeof(Text)); go.transform.SetParent(root, false);
            RectTransform rect = go.GetComponent<RectTransform>(); rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f); rect.sizeDelta = dimensions; rect.anchoredPosition = position;
            Text label = go.GetComponent<Text>(); label.text = value; label.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf"); label.fontSize = size; label.fontStyle = style; label.color = color; label.alignment = alignment; label.horizontalOverflow = HorizontalWrapMode.Wrap; label.verticalOverflow = VerticalWrapMode.Truncate; label.raycastTarget = false; return label;
        }

        private static Button MakeButton(Transform root, string name, string label, Vector2 position, Vector2 dimensions, Color color, Action action, int size)
        {
            GameObject go = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button)); go.transform.SetParent(root, false);
            RectTransform rect = go.GetComponent<RectTransform>(); rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f); rect.sizeDelta = dimensions; rect.anchoredPosition = position;
            Image image = go.GetComponent<Image>(); image.color = color; Button button = go.GetComponent<Button>(); button.targetGraphic = image; button.onClick.AddListener(() => action());
            Label(go.transform, "Label", label, size, Color.white, Vector2.zero, dimensions - new Vector2(12f, 8f), FontStyle.Normal, TextAnchor.MiddleCenter);
            ColorBlock colors = button.colors; colors.highlightedColor = Color.Lerp(color, Color.white, 0.12f); colors.pressedColor = Color.Lerp(color, Color.black, 0.12f); button.colors = colors;
            return button;
        }

        private void OnDestroy() { if (generation != null) StopCoroutine(generation); }

        internal static class CatMePhotoPicker
        {
#if UNITY_IOS && !UNITY_EDITOR
            [DllImport("__Internal")] private static extern void CatMePhotoPicker_Open(string gameObjectName);
            [DllImport("__Internal")] private static extern void CatMePhotoPicker_OpenForHuman(string gameObjectName);
#endif
            public static void Open(string gameObjectName)
            {
#if UNITY_IOS && !UNITY_EDITOR
                CatMePhotoPicker_Open(gameObjectName);
#endif
            }
            public static void OpenHuman(string gameObjectName)
            {
#if UNITY_IOS && !UNITY_EDITOR
                CatMePhotoPicker_OpenForHuman(gameObjectName);
#endif
            }
        }
    }
}
