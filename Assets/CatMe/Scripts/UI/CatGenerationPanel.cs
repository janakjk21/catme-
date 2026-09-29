using System;
using System.Collections;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using CatMe.Cat;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace CatMe.UI
{
    /// <summary>Development-only photo and saved-cat controls for CatMe's Tripo service.</summary>
    [DisallowMultipleComponent]
    public sealed class CatGenerationPanel : MonoBehaviour
    {
        private const long MaxGeneratedModelBytes = 200L * 1024L * 1024L;
        private const long MaxPhotoBytes = 12L * 1024L * 1024L;
        private const string ServerPreferenceKey = "catme.dev.generation-server";
        private const string DefaultServerUrl = "http://192.168.0.75:3001";

        [Serializable] private sealed class JobState
        {
            public string phase;
            public string status;
            public string id;
            public string error;
            public int progress;
        }

        [Serializable] private sealed class ModelEntry { public string id; public string label; }
        [Serializable] private sealed class ModelList { public ModelEntry[] items; }

        private InputField serverInput;
        private Text statusLabel;
        private Button createCatButton;
        private Button savedCatButton;
        private Button choosePhotoButton;
        private RectTransform safeAreaRect;
        private RectTransform panelRect;
        private RectTransform urlRect;
        private RectTransform statusRect;
        private RectTransform closeRect;
        private float lastPanelWidth = -1f;
        private float lastPanelHeight = -1f;
        private string selectedPhotoPath;
        private string selectedPhotoName;
        private bool selectedPhotoIsTemporary;
        private bool busy;
        private Coroutine jobRoutine;

        public void Initialize()
        {
            if (!Debug.isDebugBuild)
            {
                enabled = false;
                return;
            }
            if (FindAnyObjectByType<Canvas>() != null) CreateControls();
            else StartCoroutine(WaitForCanvas());
        }

        private IEnumerator WaitForCanvas()
        {
            while (FindAnyObjectByType<Canvas>() == null) yield return null;
            CreateControls();
        }

        private void CreateControls()
        {
            Canvas canvas = FindAnyObjectByType<Canvas>();
            if (canvas == null) return;
            Transform parent = canvas.transform.Find("CallSafeArea") ?? canvas.transform;
            safeAreaRect = parent as RectTransform;

            GameObject toggleObject = CreateObject("CatGenerationToggle", parent);
            RectTransform toggleRect = toggleObject.GetComponent<RectTransform>();
            SetTopRect(toggleRect, new Vector2(0.5f, 1f), new Vector2(310f, -72f), new Vector2(250f, 72f));
            Image toggleImage = toggleObject.AddComponent<Image>();
            toggleImage.color = new Color(0.29f, 0.36f, 0.24f, 0.96f);
            Button toggle = toggleObject.AddComponent<Button>();
            toggle.targetGraphic = toggleImage;
            CreateLabel(toggleObject.transform, "Make your cat", 24, Color.white);

            GameObject panel = CreateObject("CatGenerationPanel", parent);
            panelRect = panel.GetComponent<RectTransform>();
            SetTopRect(panelRect, new Vector2(0.5f, 1f), new Vector2(0f, -158f), new Vector2(780f, 475f));
            Image panelImage = panel.AddComponent<Image>();
            panelImage.color = new Color(0.96f, 0.92f, 0.83f, 0.98f);

            GameObject titleObject = CreateObject("GenerationTitle", panel.transform);
            SetPanelRect(titleObject.GetComponent<RectTransform>(), new Vector2(0f, 1f), new Vector2(16f, -10f), new Vector2(250f, 42f));
            Text title = titleObject.AddComponent<Text>();
            title.text = "Your cat";
            title.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            title.fontSize = 32;
            title.fontStyle = FontStyle.Bold;
            title.color = new Color(0.16f, 0.17f, 0.15f);
            title.alignment = TextAnchor.MiddleLeft;
            title.raycastTarget = false;
            Button closeButton = CreateButton("CloseGenerationPanel", panel.transform, "Close", Vector2.zero, new Vector2(100f, 42f));
            closeButton.GetComponentInChildren<Text>().fontSize = 24;
            closeRect = closeButton.GetComponent<RectTransform>();
            closeButton.onClick.AddListener(() => panel.SetActive(false));

            GameObject urlObject = CreateObject("GenerationServerUrl", panel.transform);
            urlRect = urlObject.GetComponent<RectTransform>();
            SetPanelRect(urlRect, new Vector2(0f, 1f), new Vector2(14f, -14f), new Vector2(752f, 62f));
            urlObject.AddComponent<Image>().color = Color.white;
            serverInput = urlObject.AddComponent<InputField>();
            serverInput.text = PlayerPrefs.GetString(ServerPreferenceKey, DefaultServerUrl);
            serverInput.lineType = InputField.LineType.SingleLine;
            serverInput.targetGraphic = urlObject.GetComponent<Image>();
            Text urlText = CreateLabel(urlObject.transform, serverInput.text, 26, new Color(0.16f, 0.17f, 0.15f));
            RectTransform urlTextRect = urlText.rectTransform;
            urlTextRect.anchorMin = Vector2.zero; urlTextRect.anchorMax = Vector2.one;
            urlTextRect.offsetMin = new Vector2(14f, 4f); urlTextRect.offsetMax = new Vector2(-14f, -4f);
            serverInput.textComponent = urlText;
            serverInput.onValueChanged.AddListener(value => { PlayerPrefs.SetString(ServerPreferenceKey, value); PlayerPrefs.Save(); });

            choosePhotoButton = CreateButton("ChoosePhoto", panel.transform, "Choose photo", new Vector2(14f, -88f), new Vector2(238f, 66f));
            choosePhotoButton.GetComponentInChildren<Text>().fontSize = 28;
            choosePhotoButton.onClick.AddListener(OpenPhotoPicker);
            createCatButton = CreateButton("SubmitPhoto", panel.transform, "Create cat", new Vector2(265f, -88f), new Vector2(238f, 66f));
            createCatButton.GetComponentInChildren<Text>().fontSize = 28;
            createCatButton.onClick.AddListener(BeginPhotoGeneration);
            savedCatButton = CreateButton("LoadSavedCat", panel.transform, "Load saved cat", new Vector2(516f, -88f), new Vector2(250f, 66f));
            savedCatButton.GetComponentInChildren<Text>().fontSize = 28;
            savedCatButton.onClick.AddListener(BeginLoadSavedCat);

            GameObject statusObject = CreateObject("GenerationStatus", panel.transform);
            statusRect = statusObject.GetComponent<RectTransform>();
            SetPanelRect(statusRect, new Vector2(0f, 1f), new Vector2(20f, -172f), new Vector2(740f, 270f));
            statusLabel = statusObject.AddComponent<Text>();
            statusLabel.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            statusLabel.fontSize = 28;
            statusLabel.color = new Color(0.16f, 0.17f, 0.15f);
            statusLabel.alignment = TextAnchor.UpperLeft;
            statusLabel.horizontalOverflow = HorizontalWrapMode.Wrap;
            statusLabel.verticalOverflow = VerticalWrapMode.Truncate;
            statusLabel.text = "Choose a cat photo, then create its Tripo rigged walking companion. Use the Mac's Wi-Fi address while testing on iPhone.";

            LayoutPanel();
            panel.SetActive(false);
            toggle.onClick.AddListener(() =>
            {
                panel.SetActive(!panel.activeSelf);
                if (panel.activeSelf && !busy) StartCoroutine(ResumeExistingJob());
            });
        }

        private void Update()
        {
            if (panelRect == null || safeAreaRect == null) return;
            float width = safeAreaRect.rect.width;
            float height = safeAreaRect.rect.height;
            if (Mathf.Abs(width - lastPanelWidth) < 0.5f && Mathf.Abs(height - lastPanelHeight) < 0.5f) return;
            LayoutPanel();
        }

        private void LayoutPanel()
        {
            if (panelRect == null || safeAreaRect == null) return;
            float safeWidth = safeAreaRect.rect.width;
            float safeHeight = safeAreaRect.rect.height;
            if (safeWidth < 1f || safeHeight < 1f) return;
            lastPanelWidth = safeWidth;
            lastPanelHeight = safeHeight;
            float width = Mathf.Min(780f, Mathf.Max(1f, safeWidth - 24f));
            bool stacked = width < 650f;
            float panelHeight = stacked ? 500f : 390f;
            float top = Mathf.Min(158f, Mathf.Max(16f, safeHeight - panelHeight - 16f));
            SetTopRect(panelRect, new Vector2(0.5f, 1f), new Vector2(0f, -top), new Vector2(width, panelHeight));
            SetPanelRect(closeRect, new Vector2(0f, 1f), new Vector2(width - 112f, -10f), new Vector2(100f, 42f));
            SetPanelRect(urlRect, new Vector2(0f, 1f), new Vector2(14f, -58f), new Vector2(width - 28f, 60f));
            RectTransform chooseRect = choosePhotoButton.GetComponent<RectTransform>();
            RectTransform createRect = createCatButton.GetComponent<RectTransform>();
            RectTransform savedRect = savedCatButton.GetComponent<RectTransform>();
            if (stacked)
            {
                float buttonWidth = width - 28f;
                SetPanelRect(chooseRect, new Vector2(0f, 1f), new Vector2(14f, -132f), new Vector2(buttonWidth, 54f));
                SetPanelRect(createRect, new Vector2(0f, 1f), new Vector2(14f, -196f), new Vector2(buttonWidth, 54f));
                SetPanelRect(savedRect, new Vector2(0f, 1f), new Vector2(14f, -260f), new Vector2(buttonWidth, 54f));
                SetPanelRect(statusRect, new Vector2(0f, 1f), new Vector2(20f, -330f), new Vector2(width - 40f, 150f));
            }
            else
            {
                float buttonWidth = (width - 46f) / 3f;
                SetPanelRect(chooseRect, new Vector2(0f, 1f), new Vector2(14f, -132f), new Vector2(buttonWidth, 60f));
                SetPanelRect(createRect, new Vector2(0f, 1f), new Vector2(23f + buttonWidth, -132f), new Vector2(buttonWidth, 60f));
                SetPanelRect(savedRect, new Vector2(0f, 1f), new Vector2(32f + buttonWidth * 2f, -132f), new Vector2(buttonWidth, 60f));
                SetPanelRect(statusRect, new Vector2(0f, 1f), new Vector2(20f, -212f), new Vector2(width - 40f, 160f));
            }
            statusLabel.fontSize = stacked ? 26 : 28;
        }

        private static GameObject CreateObject(string objectName, Transform parent)
        {
            GameObject result = new GameObject(objectName, typeof(RectTransform));
            result.transform.SetParent(parent, false);
            return result;
        }

        private static void SetTopRect(RectTransform rect, Vector2 anchor, Vector2 position, Vector2 size)
        {
            rect.anchorMin = anchor; rect.anchorMax = anchor; rect.pivot = new Vector2(0.5f, 1f);
            rect.anchoredPosition = position; rect.sizeDelta = size;
        }

        private static void SetPanelRect(RectTransform rect, Vector2 anchor, Vector2 position, Vector2 size)
        {
            rect.anchorMin = anchor; rect.anchorMax = anchor; rect.pivot = new Vector2(0f, 1f);
            rect.anchoredPosition = position; rect.sizeDelta = size;
        }

        private static Text CreateLabel(Transform parent, string value, int size, Color color)
        {
            GameObject labelObject = CreateObject("Label", parent);
            RectTransform rect = labelObject.GetComponent<RectTransform>();
            rect.anchorMin = Vector2.zero; rect.anchorMax = Vector2.one; rect.offsetMin = Vector2.zero; rect.offsetMax = Vector2.zero;
            Text text = labelObject.AddComponent<Text>();
            text.text = value;
            text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            text.fontSize = size; text.fontStyle = FontStyle.Bold; text.alignment = TextAnchor.MiddleCenter; text.color = color;
            return text;
        }

        private static Button CreateButton(string objectName, Transform parent, string label, Vector2 position, Vector2 size)
        {
            GameObject buttonObject = CreateObject(objectName, parent);
            RectTransform rect = buttonObject.GetComponent<RectTransform>();
            SetPanelRect(rect, new Vector2(0f, 1f), position, size);
            Image image = buttonObject.AddComponent<Image>();
            image.color = new Color(0.35f, 0.43f, 0.29f, 1f);
            Button button = buttonObject.AddComponent<Button>();
            button.targetGraphic = image;
            CreateLabel(buttonObject.transform, label, 20, Color.white);
            return button;
        }

        private void OpenPhotoPicker()
        {
            if (busy) return;
#if UNITY_EDITOR
            string path = EditorUtility.OpenFilePanel("Choose a cat photo", "", "png,jpg,jpeg");
            OnCatPhotoPicked(path);
#elif UNITY_IOS && !UNITY_EDITOR
            CatMePhotoPicker.Open(gameObject.name);
#else
            SetStatus("Photo selection is available in the Unity Editor and iOS development build.");
#endif
        }

        public void OnCatPhotoPicked(string path)
        {
            if (string.IsNullOrWhiteSpace(path) || !File.Exists(path))
            {
                if (!string.IsNullOrWhiteSpace(path)) SetStatus("Could not read the selected photo. Choose another image.");
                return;
            }
            FileInfo info = new FileInfo(path);
            if (info.Length <= 0 || info.Length > MaxPhotoBytes)
            {
                SetStatus("Choose an image smaller than 12 MB.");
                return;
            }
            ClearTemporaryPhoto();
            selectedPhotoPath = path;
            selectedPhotoName = info.Name;
            selectedPhotoIsTemporary = info.Name.StartsWith("catme-photo-", StringComparison.OrdinalIgnoreCase) ||
                                       path.StartsWith(Path.GetTempPath(), StringComparison.OrdinalIgnoreCase);
            SetStatus($"Photo selected: {selectedPhotoName}\nTap Create cat to upload it and start rigging.");
        }

        private void BeginPhotoGeneration()
        {
            if (busy) return;
            if (!ValidateServerAddress()) return;
            if (string.IsNullOrEmpty(selectedPhotoPath) || !File.Exists(selectedPhotoPath))
            {
                SetStatus("Choose a cat photo first.");
                return;
            }
            jobRoutine = StartCoroutine(CreateCatFromPhoto());
        }

        private void BeginLoadSavedCat()
        {
            if (!busy && ValidateServerAddress()) jobRoutine = StartCoroutine(LoadSavedCat());
        }

        private bool ValidateServerAddress()
        {
            string value = ServerUrl;
            if (!Uri.TryCreate(value, UriKind.Absolute, out Uri uri) ||
                (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps))
            {
                SetStatus("Enter the local CatMe service address, such as http://192.168.0.75:3001.");
                return false;
            }
#if UNITY_IOS && !UNITY_EDITOR
            if (uri.IsLoopback)
            {
                SetStatus("Use your Mac's Wi-Fi address here. The phone cannot reach a server through localhost.");
                return false;
            }
#endif
            return true;
        }

        private IEnumerator LoadSavedCat()
        {
            SetBusy(true);
            SetStatus("Checking saved rigged cats on the local server…");
            string body;
            using (UnityWebRequest request = UnityWebRequest.Get(ServerUrl + "/api/catme-generation/models"))
            {
                request.timeout = 30;
                yield return request.SendWebRequest();
                if (request.result != UnityWebRequest.Result.Success)
                {
                    SetStatus($"Could not reach the generation server: {request.error}");
                    SetBusy(false);
                    yield break;
                }
                body = request.downloadHandler.text;
            }

            ModelList models;
            try { models = JsonUtility.FromJson<ModelList>($"{{\"items\":{body}}}"); }
            catch (Exception exception) { SetStatus($"Server returned an invalid model list: {exception.Message}"); SetBusy(false); yield break; }
            if (models == null || models.items == null || models.items.Length == 0)
            {
                SetStatus("No saved rigged cats are available yet. Choose a photo to create one.");
                SetBusy(false);
                yield break;
            }

            ModelEntry selected = models.items[models.items.Length - 1];
            yield return DownloadAndActivate(selected.id);
            SetBusy(false);
        }

        private IEnumerator CreateCatFromPhoto()
        {
            SetBusy(true);
            byte[] photo;
            try { photo = File.ReadAllBytes(selectedPhotoPath); }
            catch (Exception exception) { SetStatus($"Could not read selected photo: {exception.Message}"); SetBusy(false); yield break; }

            SetStatus("Uploading photo to CatMe server…");
            JobState job;
            string uploadFailure = null;
            using (UnityWebRequest request = new UnityWebRequest(ServerUrl + "/api/catme-generation", "POST"))
            {
                WWWForm form = new WWWForm();
                string mimeType = selectedPhotoPath.EndsWith(".png", StringComparison.OrdinalIgnoreCase) ? "image/png" : "image/jpeg";
                form.AddBinaryData("photo", photo, selectedPhotoName, mimeType);
                request.uploadHandler = new UploadHandlerRaw(form.data);
                request.uploadHandler.contentType = form.headers["Content-Type"];
                request.downloadHandler = new DownloadHandlerBuffer();
                request.timeout = 120;
                yield return request.SendWebRequest();
                if (request.result != UnityWebRequest.Result.Success && request.responseCode != 409)
                {
                    uploadFailure = ReadError(request);
                    job = null;
                }
                else job = ParseJob(request.downloadHandler.text);
            }
            ClearTemporaryPhoto();
            if (uploadFailure != null)
            {
                SetStatus($"Could not start cat creation: {uploadFailure}");
                SetBusy(false);
                yield break;
            }

            if (job == null)
            {
                SetStatus("The server did not return a generation job.");
                SetBusy(false);
                yield break;
            }
            yield return ContinueGeneration(job);
            SetBusy(false);
        }

        private IEnumerator ResumeExistingJob()
        {
            if (busy) yield break;
            SetBusy(true);
            yield return new WaitForSecondsRealtime(1.5f);
            JobState job = null;
            bool failed = false;
            using (UnityWebRequest request = UnityWebRequest.Get(ServerUrl + "/api/catme-generation/status"))
            {
                request.timeout = 30;
                yield return request.SendWebRequest();
                if (request.result != UnityWebRequest.Result.Success || string.IsNullOrWhiteSpace(request.downloadHandler.text) || request.downloadHandler.text == "null")
                {
                    failed = true;
                }
                else job = ParseJob(request.downloadHandler.text);
            }
            if (failed || job == null || (job.status != "PENDING" && job.phase != "READY"))
            {
                SetBusy(false);
                yield break;
            }
            SetStatus("Resuming the generation job already started on this local server…");
            yield return ContinueGeneration(job);
            SetBusy(false);
        }

        private IEnumerator ContinueGeneration(JobState job)
        {
            if (job.phase != "READY" || job.status != "SUCCEEDED")
            {
                yield return PollJob("/api/catme-generation/status", job, "Making and preparing your cat");
                if (job.status != "SUCCEEDED" || job.phase != "READY") yield break;
            }
            if (string.IsNullOrEmpty(job.id))
            {
                SetStatus("The server finished but did not return a saved cat ID.");
                yield break;
            }
            yield return DownloadAndActivate(job.id);
        }

        private IEnumerator PollJob(string route, JobState initial, string label)
        {
            JobState current = initial;
            while (current != null && current.status == "PENDING")
            {
                SetStatus($"{label}: {HumanPhase(current.phase)} ({Mathf.Clamp(current.progress, 0, 100)}%)\nThe existing cat stays playable while the server works.");
                yield return new WaitForSecondsRealtime(1.5f);
                using (UnityWebRequest request = UnityWebRequest.Get(ServerUrl + route))
                {
                    request.timeout = 60;
                    yield return request.SendWebRequest();
                    if (request.result != UnityWebRequest.Result.Success)
                    {
                        SetStatus($"Generation status check failed: {request.error}. Reopen this panel to resume.");
                        yield break;
                    }
                    current = ParseJob(request.downloadHandler.text);
                }
                if (current != null && current.status == "FAILED")
                {
                    SetStatus($"Cat creation stopped: {current.error ?? "the server reported an error"}. Your current cat is still playable.");
                    yield break;
                }
            }
            if (current != null)
            {
                initial.phase = current.phase; initial.status = current.status; initial.id = current.id;
                initial.progress = current.progress; initial.error = current.error;
            }
        }

        private IEnumerator DownloadAndActivate(string version)
        {
            if (string.IsNullOrEmpty(version))
            {
                SetStatus("The server did not identify the saved animated cat.");
                yield break;
            }
            string url = ServerUrl + "/api/catme-generation/model?version=" + UnityWebRequest.EscapeURL(version);
            SetStatus("Downloading and checking the rigged cat…");
            using (UnityWebRequest request = UnityWebRequest.Get(url))
            {
                request.downloadHandler = new DownloadHandlerBuffer();
                request.timeout = 180;
                yield return request.SendWebRequest();
                if (request.result != UnityWebRequest.Result.Success)
                {
                    SetStatus($"Could not download the rigged cat: {request.error}. Your current cat is still playable.");
                    yield break;
                }
                byte[] bytes = request.downloadHandler.data;
                if (!IsPlausibleGlb(bytes))
                {
                    SetStatus("The downloaded file is not a valid GLB. Your current cat is still playable.");
                    yield break;
                }
                string directory = Path.Combine(Application.persistentDataPath, "Cats");
                string destination = Path.Combine(directory, LocalCatAssetLoader.GeneratedCatCacheFileName);
                string pending = destination + ".pending";
                string backup = destination + ".previous";
                string cacheError = null;
                try
                {
                    Directory.CreateDirectory(directory);
                    File.WriteAllBytes(pending, bytes);
                    if (File.Exists(destination))
                    {
                        File.Copy(destination, backup, true);
                        File.Delete(destination);
                    }
                    File.Move(pending, destination);
                }
                catch (Exception exception)
                {
                    try { if (File.Exists(pending)) File.Delete(pending); } catch { }
                    try { if (File.Exists(backup) && !File.Exists(destination)) File.Copy(backup, destination, true); } catch { }
                    cacheError = exception.Message;
                }
                if (cacheError != null)
                {
                    SetStatus($"Could not cache the new cat: {cacheError}. Your current cat is still playable.");
                    yield break;
                }
                SetStatus("Rigged cat saved. Restarting the room with your new companion…");
                yield return new WaitForSecondsRealtime(0.6f);
                SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
                yield break;
            }
        }

        private static bool IsPlausibleGlb(byte[] bytes)
        {
            if (bytes == null || bytes.Length < 20 || bytes.LongLength > MaxGeneratedModelBytes) return false;
            return bytes[0] == (byte)'g' && bytes[1] == (byte)'l' && bytes[2] == (byte)'T' && bytes[3] == (byte)'F'
                   && BitConverter.ToUInt32(bytes, 8) == (uint)bytes.Length;
        }

        private static JobState ParseJob(string json)
        {
            if (string.IsNullOrWhiteSpace(json) || json == "null") return null;
            try { return JsonUtility.FromJson<JobState>(json); }
            catch { return null; }
        }

        private static string ReadError(UnityWebRequest request)
        {
            JobState job = ParseJob(request.downloadHandler == null ? null : request.downloadHandler.text);
            return job != null && !string.IsNullOrEmpty(job.error) ? job.error : request.error;
        }

        private string ServerUrl
        {
            get
            {
                string value = serverInput == null ? PlayerPrefs.GetString(ServerPreferenceKey, DefaultServerUrl) : serverInput.text;
                return (string.IsNullOrWhiteSpace(value) ? DefaultServerUrl : value.Trim()).TrimEnd('/');
            }
        }

        private static string HumanPhase(string phase)
        {
            if (string.IsNullOrEmpty(phase)) return "waiting";
            return phase.Replace('_', ' ').ToLowerInvariant();
        }

        private void SetBusy(bool value)
        {
            busy = value;
            if (createCatButton != null) createCatButton.interactable = !value;
            if (savedCatButton != null) savedCatButton.interactable = !value;
            if (choosePhotoButton != null) choosePhotoButton.interactable = !value;
        }

        private void SetStatus(string value)
        {
            if (statusLabel != null) statusLabel.text = value;
            else Debug.Log($"[CatMe][Generation] {value}");
        }

        private void ClearTemporaryPhoto()
        {
            if (!selectedPhotoIsTemporary) return;
            try { if (File.Exists(selectedPhotoPath)) File.Delete(selectedPhotoPath); } catch { }
            selectedPhotoPath = null;
            selectedPhotoIsTemporary = false;
        }

        private void OnDestroy()
        {
            if (jobRoutine != null) StopCoroutine(jobRoutine);
        }
    }

    internal static class CatMePhotoPicker
    {
#if UNITY_IOS && !UNITY_EDITOR
        [DllImport("__Internal")] private static extern void CatMePhotoPicker_Open(string gameObjectName);
#endif

        public static void Open(string gameObjectName)
        {
#if UNITY_IOS && !UNITY_EDITOR
            CatMePhotoPicker_Open(gameObjectName);
#endif
        }
    }
}
