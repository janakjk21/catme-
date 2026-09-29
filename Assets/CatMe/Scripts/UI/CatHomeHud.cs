using System.Collections.Generic;
using CatMe.CameraSystem;
using UnityEngine;
using UnityEngine.UI;

namespace CatMe.UI
{
    /// <summary>Arranges the existing runtime controls without taking ownership of their actions.</summary>
    [DisallowMultipleComponent]
    public sealed class CatHomeHud : MonoBehaviour
    {
        private static readonly Color Ink = new Color(0.26f, 0.17f, 0.12f, 1f);
        private static readonly Color Cream = new Color(1f, 0.96f, 0.88f, 0.94f);
        private static readonly Color Dock = new Color(0.91f, 0.79f, 0.65f, 0.78f);

        private readonly Dictionary<string, RectTransform> controls = new Dictionary<string, RectTransform>();
        private readonly HashSet<string> hookedMenuControls = new HashSet<string>();
        private RectTransform safeArea;
        private RectTransform actionDock;
        private RectTransform playTray;
        private RectTransform moreTray;
        private Button playToggle;
        private Button moreToggle;
        private Button settingsButton;
        private Button helpButton;
        private Button soundButton;
        private Button hintDismiss;
        private Button diagnosticsButton;
        private RectTransform hintPanel;
        private Text hint;
        private RoomOrbitCamera roomCamera;
        private bool playOpen;
        private bool moreOpen;
        private bool diagnosticsOpen;
        private bool helpOpen;
        private float hintUntil;
        private float lastWidth = -1f;
        private float lastHeight = -1f;
        private float nextControlScan;
        private bool soundEnabled;
        private Rect lastSafePixels;
        private Image screenCornerMask;
        private Texture2D screenMaskTexture;
        private Sprite screenMaskSprite;
        private int lastMaskScreenWidth;
        private int lastMaskScreenHeight;
        private Texture2D roundedTexture;
        private Sprite roundedSprite;

        private void Awake()
        {
            safeArea = (RectTransform)transform;
            roomCamera = FindAnyObjectByType<RoomOrbitCamera>();
            CreateScreenCornerMask();
            CreateRoundedSprite();
            actionDock = CreateTray("HomeActionDock", Dock);
            playTray = CreateTray("PlayTray", new Color(0.19f, 0.13f, 0.10f, 0.82f));
            moreTray = CreateTray("MoreTray", new Color(0.19f, 0.13f, 0.10f, 0.96f));
            playToggle = CreateButton("PlayMenuButton", "Play", Cream, () =>
            {
                playOpen = !playOpen;
                if (playOpen) moreOpen = false;
                UpdateTrayVisibility();
            });
            moreToggle = CreateButton("MoreMenuButton", "More", Cream, () =>
            {
                moreOpen = !moreOpen;
                if (moreOpen) playOpen = false;
                UpdateTrayVisibility();
            });
            settingsButton = CreateButton("SettingsButton", "Settings", Cream, () =>
            {
                moreOpen = !moreOpen;
                if (moreOpen) playOpen = false;
                UpdateTrayVisibility();
            });
            helpButton = CreateButton("HelpButton", "How to play", Cream, () =>
            {
                helpOpen = !helpOpen;
                moreOpen = false;
                UpdateTrayVisibility();
                RefreshHint();
            });
            soundEnabled = PlayerPrefs.GetInt("catme.sound.enabled", 1) != 0;
            AudioListener.volume = soundEnabled ? 1f : 0f;
            soundButton = CreateButton("SoundButton", soundEnabled ? "Sound on" : "Sound off", Cream, () =>
            {
                soundEnabled = !soundEnabled;
                AudioListener.volume = soundEnabled ? 1f : 0f;
                PlayerPrefs.SetInt("catme.sound.enabled", soundEnabled ? 1 : 0);
                PlayerPrefs.Save();
                Text label = soundButton.GetComponentInChildren<Text>();
                if (label != null) label.text = soundEnabled ? "Sound on" : "Sound off";
            });
            if (Debug.isDebugBuild)
            {
                diagnosticsButton = CreateButton("DiagnosticsButton", "Diagnostics", Cream, () =>
                {
                    diagnosticsOpen = !diagnosticsOpen;
                    RefreshDiagnostics();
                });
            }
            hintPanel = CreateTray("HomeHintPanel", Cream);
            hintPanel.GetComponent<Image>().raycastTarget = false;
            hint = CreateText("HomeHint", 30);
            hint.transform.SetParent(hintPanel, false);
            RectTransform hintTextRect = hint.rectTransform;
            hintTextRect.anchorMin = Vector2.zero;
            hintTextRect.anchorMax = Vector2.one;
            hintTextRect.offsetMin = new Vector2(12f, 4f);
            hintTextRect.offsetMax = new Vector2(-12f, -4f);
            hintDismiss = CreateButton("HintDismissButton", "Got it", Cream, () =>
            {
                hintUntil = 0f;
                helpOpen = false;
                RefreshHint();
            });
            Outline hintOutline = hint.gameObject.AddComponent<Outline>();
            hintOutline.effectColor = new Color(0.08f, 0.05f, 0.035f, 0.90f);
            hintOutline.effectDistance = new Vector2(2f, -2f);
            hintUntil = Time.unscaledTime + 4f;
            UpdateTrayVisibility();
            RefreshHint();
        }

        private void LateUpdate()
        {
            if (safeArea == null) return;
            Rect safePixels = Screen.safeArea;
            RefreshScreenCornerMask();
            if (safePixels != lastSafePixels && Screen.width > 0 && Screen.height > 0)
            {
                lastSafePixels = safePixels;
                safeArea.anchorMin = new Vector2(safePixels.xMin / Screen.width, safePixels.yMin / Screen.height);
                safeArea.anchorMax = new Vector2(safePixels.xMax / Screen.width, safePixels.yMax / Screen.height);
            }
            if (roomCamera == null) roomCamera = FindAnyObjectByType<RoomOrbitCamera>();
            if (Time.unscaledTime >= nextControlScan)
            {
                nextControlScan = Time.unscaledTime + 0.5f;
                FindAndArrangeControls();
            }
            if (Mathf.Abs(lastWidth - safeArea.rect.width) > 0.5f || Mathf.Abs(lastHeight - safeArea.rect.height) > 0.5f)
            {
                lastWidth = safeArea.rect.width;
                lastHeight = safeArea.rect.height;
                Layout();
            }
            RefreshHint();
        }

        private void CreateScreenCornerMask()
        {
            Canvas canvas = GetComponentInParent<Canvas>();
            if (canvas == null) return;

            GameObject maskObject = new GameObject("RoundedScreenEdges", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            RectTransform maskRect = maskObject.GetComponent<RectTransform>();
            maskRect.SetParent(canvas.transform, false);
            maskRect.anchorMin = Vector2.zero;
            maskRect.anchorMax = Vector2.one;
            maskRect.offsetMin = Vector2.zero;
            maskRect.offsetMax = Vector2.zero;
            screenCornerMask = maskObject.GetComponent<Image>();
            screenCornerMask.color = Color.white;
            screenCornerMask.raycastTarget = false;
            RefreshScreenCornerMask(true);
            maskRect.SetAsLastSibling();
        }

        private void CreateRoundedSprite()
        {
            const int size = 64;
            const float radius = 16f;
            var pixels = new Color32[size * size];
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float px = Mathf.Abs(x + .5f - size * .5f) - (size * .5f - radius);
                    float py = Mathf.Abs(y + .5f - size * .5f) - (size * .5f - radius);
                    float outside = Mathf.Sqrt(Mathf.Max(px, 0f) * Mathf.Max(px, 0f) +
                                              Mathf.Max(py, 0f) * Mathf.Max(py, 0f));
                    float distance = outside + Mathf.Min(Mathf.Max(px, py), 0f) - radius;
                    byte alpha = (byte)Mathf.RoundToInt((1f - Mathf.Clamp01(distance + .5f)) * 255f);
                    pixels[y * size + x] = new Color32(255, 255, 255, alpha);
                }
            }
            roundedTexture = new Texture2D(size, size, TextureFormat.RGBA32, false)
            {
                name = "CatHomeRoundedControls",
                filterMode = FilterMode.Bilinear,
                wrapMode = TextureWrapMode.Clamp
            };
            roundedTexture.SetPixels32(pixels);
            roundedTexture.Apply(false, false);
            roundedSprite = Sprite.Create(roundedTexture, new Rect(0f, 0f, size, size),
                new Vector2(.5f, .5f), size, 0, SpriteMeshType.FullRect, new Vector4(16f, 16f, 16f, 16f));
        }

        private void RefreshScreenCornerMask(bool force = false)
        {
            if (screenCornerMask == null || Screen.width <= 0 || Screen.height <= 0) return;
            if (!force && lastMaskScreenWidth == Screen.width && lastMaskScreenHeight == Screen.height) return;

            const int resolution = 512;
            float width = Screen.width;
            float height = Screen.height;
            float radius = Mathf.Min(width, height) * 0.045f;
            float halfWidth = width * 0.5f;
            float halfHeight = height * 0.5f;
            float cornerX = halfWidth - radius;
            float cornerY = halfHeight - radius;
            var pixels = new Color32[resolution * resolution];
            for (int y = 0; y < resolution; y++)
            {
                float py = Mathf.Abs(((y + 0.5f) / resolution - 0.5f) * height);
                for (int x = 0; x < resolution; x++)
                {
                    float px = Mathf.Abs(((x + 0.5f) / resolution - 0.5f) * width);
                    float qx = px - cornerX;
                    float qy = py - cornerY;
                    float outsideX = Mathf.Max(qx, 0f);
                    float outsideY = Mathf.Max(qy, 0f);
                    float distance = Mathf.Sqrt(outsideX * outsideX + outsideY * outsideY) +
                                     Mathf.Min(Mathf.Max(qx, qy), 0f) - radius;
                    byte alpha = (byte)Mathf.RoundToInt(Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(distance + 0.5f)) * 255f);
                    pixels[y * resolution + x] = new Color32(0, 0, 0, alpha);
                }
            }

            if (screenMaskTexture != null) Destroy(screenMaskTexture);
            if (screenMaskSprite != null) Destroy(screenMaskSprite);
            screenMaskTexture = new Texture2D(resolution, resolution, TextureFormat.RGBA32, false)
            {
                name = "RoundedScreenEdgesTexture",
                filterMode = FilterMode.Bilinear,
                wrapMode = TextureWrapMode.Clamp
            };
            screenMaskTexture.SetPixels32(pixels);
            screenMaskTexture.Apply(false, false);
            screenMaskSprite = Sprite.Create(screenMaskTexture, new Rect(0, 0, resolution, resolution), new Vector2(0.5f, 0.5f), 100f);
            screenCornerMask.sprite = screenMaskSprite;
            screenCornerMask.type = Image.Type.Simple;
            screenCornerMask.preserveAspect = false;
            lastMaskScreenWidth = Screen.width;
            lastMaskScreenHeight = Screen.height;
            screenCornerMask.transform.SetAsLastSibling();
        }

        private void FindAndArrangeControls()
        {
            Capture("CallButton");
            Capture("FeedButton");
            Capture("SleepButton");
            Capture("BallPlayButton");
            Capture("EnergyMeter");
            Capture("PhotoButton");
            Capture("CatGenerationToggle");
            Capture("HomeViewButton");
            Capture("LaserButton");
            Capture("BallRecallButton");
            Capture("RecentButton");
            Capture("MemoriesButton");
            Capture("SeatedCallStatus");
            Capture("BallPlayHint");
            foreach (string name in new[] { "CallButton", "BallPlayButton", "FeedButton", "SleepButton" })
                MoveIntoTray(name, actionDock);
            foreach (string name in new[] { "PhotoButton", "CatGenerationToggle" })
                MoveIntoTray(name, safeArea);
            foreach (string name in new[] { "LaserButton", "BallRecallButton", "RecentButton", "MemoriesButton", "HomeViewButton" })
                Hide(name);
            playToggle.gameObject.SetActive(false);
            moreToggle.gameObject.SetActive(false);
            playTray.gameObject.SetActive(false);
            HookMenuControl("CatGenerationToggle");
            ApplySharedButtonTheme();
            ThemeEnergyMeter();
            if (!controls.ContainsKey("CatActivityReadout")) Capture("CatActivityReadout");
            RefreshDiagnostics();
            Layout();
        }

        private void Capture(string name)
        {
            if (controls.ContainsKey(name)) return;
            RectTransform found = FindDescendant(name);
            if (found != null) controls.Add(name, found);
        }

        private void MoveIntoTray(string name, RectTransform tray)
        {
            Capture(name);
            if (!controls.TryGetValue(name, out RectTransform control) || control == null) return;
            if (control.parent != tray) control.SetParent(tray, false);
        }

        private void ApplySharedButtonTheme()
        {
            foreach (KeyValuePair<string, RectTransform> pair in controls)
            {
                if (pair.Value == null) continue;
                if (pair.Value.GetComponent<Button>() == null) continue;
                Image image = pair.Value.GetComponent<Image>();
                if (image != null)
                {
                    image.color = Cream;
                    image.sprite = roundedSprite;
                    image.type = Image.Type.Sliced;
                }
                Text label = pair.Value.GetComponentInChildren<Text>(true);
                if (label != null) label.color = Ink;
            }
        }

        private void Hide(string name)
        {
            if (controls.TryGetValue(name, out RectTransform control) && control != null)
                control.gameObject.SetActive(false);
        }

        private void ThemeEnergyMeter()
        {
            if (!controls.TryGetValue("EnergyMeter", out RectTransform meter) || meter == null) return;
            Image background = meter.GetComponent<Image>();
            if (background != null)
            {
                background.color = Cream;
                background.sprite = roundedSprite;
                background.type = Image.Type.Sliced;
            }
            Text label = meter.GetComponentInChildren<Text>(true);
            if (label != null) label.color = Ink;
        }

        private RectTransform FindDescendant(string name)
        {
            RectTransform[] descendants = safeArea.GetComponentsInChildren<RectTransform>(true);
            foreach (RectTransform descendant in descendants)
                if (descendant.name == name) return descendant;
            return null;
        }

        private void Layout()
        {
            float width = Mathf.Max(1f, safeArea.rect.width);
            float height = Mathf.Max(1f, safeArea.rect.height);
            bool landscape = width >= height;
            if (landscape)
            {
                const float margin = 14f;
                float dockWidth = Mathf.Min(width * .72f, 760f);
                float dockHeight = Mathf.Clamp(height * .15f, 76f, 100f);
                SetRect(actionDock, safeArea, new Vector2(.5f, 0f), new Vector2(0f, 14f),
                    new Vector2(dockWidth, dockHeight));
                float gap = 9f;
                float buttonWidth = (dockWidth - gap * 5f) / 4f;
                string[] actions = { "CallButton", "BallPlayButton", "FeedButton", "SleepButton" };
                for (int i = 0; i < actions.Length; i++)
                {
                    Place(actions[i], actionDock, new Vector2(0f, 0f),
                        new Vector2(gap + i * (buttonWidth + gap), gap),
                        new Vector2(buttonWidth, dockHeight - gap * 2f));
                    ResizeLabel(actions[i], buttonWidth * .22f);
                }
                Place("BallPlayHint", safeArea, new Vector2(.5f, 0f),
                    new Vector2(0f, dockHeight + 24f), new Vector2(Mathf.Min(width * .5f, 460f), 52f));

                SetRect(settingsButton.GetComponent<RectTransform>(), safeArea, new Vector2(0f, 1f),
                    new Vector2(margin, -margin), new Vector2(108f, 60f));
                Place("EnergyMeter", safeArea, new Vector2(.5f, 1f),
                    new Vector2(0f, -margin), new Vector2(Mathf.Min(width * .34f, 350f), 60f));
                Place("PhotoButton", safeArea, new Vector2(1f, 1f),
                    new Vector2(-margin, -margin), new Vector2(108f, 60f));
                Place("CatGenerationToggle", safeArea, new Vector2(1f, 1f),
                    new Vector2(-margin - 120f, -margin), new Vector2(108f, 60f));

                float landscapeMenuWidth = 330f;
                SetRect(moreTray, safeArea, new Vector2(0f, 1f),
                    new Vector2(margin, -82f), new Vector2(landscapeMenuWidth, Debug.isDebugBuild ? 150f : 84f));
                ArrangeMoreItems(landscapeMenuWidth);
                Place("SeatedCallStatus", safeArea, new Vector2(.5f, 0f),
                    new Vector2(0f, dockHeight + 32f), new Vector2(Mathf.Min(width * .4f, 400f), 52f));
                SetRect(hintPanel, safeArea, new Vector2(.5f, 1f),
                    new Vector2(0f, -86f), new Vector2(Mathf.Min(width * .36f, 310f), 50f));
                SetRect(hintDismiss.GetComponent<RectTransform>(), safeArea, new Vector2(.5f, 1f),
                    new Vector2(0f, -140f), new Vector2(112f, 42f));
                ResizeLabel("PhotoButton", 20f);
                ResizeLabel("CatGenerationToggle", 20f);
                ResizeLabel("SettingsButton", 18f);
                return;
            }

            float actionWidth = Mathf.Clamp(width * 0.23f, 70f, 178f);
            // Keep the lower-left movement stick clear on narrow portrait screens.
            float leftAction = width * 0.63f - width * 0.5f;
            float rightAction = width * 0.87f - width * 0.5f;
            Place("CallButton", safeArea, new Vector2(0.5f, 0f), new Vector2(leftAction, 66f), new Vector2(actionWidth, 74f));
            Place("FeedButton", safeArea, new Vector2(0.5f, 0f), new Vector2(rightAction, 66f), new Vector2(actionWidth, 74f));
            Place("SleepButton", safeArea, new Vector2(0.5f, 0f), new Vector2(leftAction, 154f), new Vector2(actionWidth, 72f));
            SetRect(playToggle.GetComponent<RectTransform>(), safeArea, new Vector2(0.5f, 0f), new Vector2(rightAction, 154f), new Vector2(actionWidth, 72f));

            float trayWidth = Mathf.Min(width * 0.59f, 620f);
            SetRect(playTray, safeArea, new Vector2(1f, 0f), new Vector2(-16f, 244f),
                new Vector2(trayWidth, 94f));
            float toyWidth = (trayWidth - 34f) / 3f;
            Place("BallPlayButton", playTray, new Vector2(0f, 0f),
                new Vector2(8f, 8f), new Vector2(toyWidth, 72f));
            float hintY = Mathf.Min(350f, Mathf.Max(190f, height - 140f));
            Place("BallPlayHint", safeArea, new Vector2(0.5f, 0f), new Vector2(0f, hintY), new Vector2(Mathf.Min(width - 36f, 520f), 58f));

            Place("EnergyMeter", safeArea, new Vector2(0f, 1f), new Vector2(16f, -20f), new Vector2(Mathf.Min(240f, width * 0.38f), 42f));
            float topButtonWidth = Mathf.Min(146f, width * 0.29f);
            Place("SettingsButton", safeArea, new Vector2(0f, 1f), new Vector2(16f, -72f), new Vector2(118f, 48f));
            Place("HomeViewButton", safeArea, new Vector2(1f, 1f), new Vector2(-16f, -18f), new Vector2(topButtonWidth, 60f));
            SetRect(moreToggle.GetComponent<RectTransform>(), safeArea, new Vector2(1f, 1f), new Vector2(-16f, -90f), new Vector2(topButtonWidth, 54f));
            float statusY = Mathf.Min(418f, Mathf.Max(245f, height - 85f));
            Place("SeatedCallStatus", safeArea, new Vector2(0.5f, 0f), new Vector2(0f, statusY),
                new Vector2(Mathf.Min(width - 36f, 520f), 58f));

            float menuWidth = Mathf.Max(1f, Mathf.Min(width - 24f, 440f));
            int rows = Debug.isDebugBuild ? 4 : 3;
            SetRect(moreTray, safeArea, new Vector2(1f, 1f), new Vector2(-18f, -164f),
                new Vector2(menuWidth, rows * 66f + 20f));
            ArrangeMoreItems(menuWidth);
            SetRect(hintPanel, safeArea, new Vector2(0.5f, 1f),
                new Vector2(0f, -164f), new Vector2(Mathf.Min(width - 32f, 620f), 78f));
            SetRect(hintDismiss.GetComponent<RectTransform>(), safeArea, new Vector2(0.5f, 1f),
                new Vector2(0f, -246f), new Vector2(112f, 42f));
            ResizeLabel("CallButton", actionWidth * 0.28f);
            ResizeLabel("FeedButton", actionWidth * 0.28f);
            ResizeLabel("SleepButton", actionWidth * 0.28f);
            ResizeLabel("BallPlayButton", toyWidth * 0.23f);
            ResizeLabel("HomeViewButton", topButtonWidth * 0.18f);
        }

        private void ArrangeMoreItems(float menuWidth)
        {
            int item = 0;
            SetMenuItem(helpButton.GetComponent<RectTransform>(), item++, menuWidth);
            SetMenuItem(soundButton.GetComponent<RectTransform>(), item++, menuWidth);
            if (diagnosticsButton != null) SetMenuItem(diagnosticsButton.GetComponent<RectTransform>(), item, menuWidth);
        }

        private void PlaceMenuItem(string name, int index, float menuWidth)
        {
            if (controls.TryGetValue(name, out RectTransform control) && control != null)
                SetMenuItem(control, index, menuWidth);
        }

        private void SetMenuItem(RectTransform control, int index, float menuWidth)
        {
            float itemWidth = (menuWidth - 30f) * 0.5f;
            float x = 10f + (index % 2) * (itemWidth + 10f);
            float y = -10f - (index / 2) * 66f;
            SetRect(control, moreTray, new Vector2(0f, 1f), new Vector2(x, y), new Vector2(itemWidth, 56f));
        }

        private void Place(string name, Transform parent, Vector2 anchor, Vector2 position, Vector2 size)
        {
            if (controls.TryGetValue(name, out RectTransform control) && control != null)
                SetRect(control, parent, anchor, position, size);
        }

        private static void SetRect(RectTransform rect, Transform parent, Vector2 anchor, Vector2 position, Vector2 size)
        {
            if (rect.parent != parent) rect.SetParent(parent, false);
            rect.anchorMin = rect.anchorMax = anchor;
            rect.pivot = anchor;
            rect.anchoredPosition = position;
            rect.sizeDelta = size;
        }

        private RectTransform CreateTray(string name, Color color)
        {
            GameObject root = new GameObject(name, typeof(RectTransform), typeof(Image));
            root.transform.SetParent(transform, false);
            Image background = root.GetComponent<Image>();
            background.color = color;
            background.sprite = roundedSprite;
            background.type = Image.Type.Sliced;
            return root.GetComponent<RectTransform>();
        }

        private Button CreateButton(string name, string caption, Color color, UnityEngine.Events.UnityAction action)
        {
            GameObject root = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button));
            root.transform.SetParent(transform, false);
            Image image = root.GetComponent<Image>();
            image.color = color;
            image.sprite = roundedSprite;
            image.type = Image.Type.Sliced;
            Button button = root.GetComponent<Button>();
            button.targetGraphic = image;
            button.onClick.AddListener(action);
            Text label = CreateText("Label", 22);
            label.transform.SetParent(root.transform, false);
            RectTransform rect = label.rectTransform;
            rect.anchorMin = Vector2.zero; rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero; rect.offsetMax = Vector2.zero;
            label.text = caption;
            return button;
        }

        private Text CreateText(string name, int fontSize)
        {
            GameObject root = new GameObject(name, typeof(RectTransform), typeof(Text));
            root.transform.SetParent(transform, false);
            Text label = root.GetComponent<Text>();
            label.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            label.fontSize = fontSize;
            label.fontStyle = FontStyle.Bold;
            label.alignment = TextAnchor.MiddleCenter;
            label.color = Ink;
            label.raycastTarget = false;
            return label;
        }

        private void UpdateTrayVisibility()
        {
            playTray.gameObject.SetActive(playOpen);
            moreTray.gameObject.SetActive(moreOpen);
            Text playLabel = playToggle.GetComponentInChildren<Text>();
            Text moreLabel = moreToggle.GetComponentInChildren<Text>();
            if (playLabel != null) playLabel.text = playOpen ? "Close toys" : "Play";
            if (moreLabel != null) moreLabel.text = moreOpen ? "Close" : "More";
        }

        private void RefreshHint()
        {
            if (hint == null) return;
            bool visible = helpOpen || Time.unscaledTime < hintUntil;
            bool callStatusShowing = false;
            if (controls.TryGetValue("SeatedCallStatus", out RectTransform status) && status != null)
            {
                Text statusText = status.GetComponent<Text>();
                callStatusShowing = statusText != null && statusText.enabled && !string.IsNullOrEmpty(statusText.text);
            }
            bool showHint = visible && !moreOpen && !callStatusShowing;
            hintPanel.gameObject.SetActive(showHint);
            if (hintDismiss != null) hintDismiss.gameObject.SetActive(showHint && helpOpen);
            if (!visible) return;
            bool firstPerson = roomCamera != null && roomCamera.IsSeatedView;
            hint.text = firstPerson
                ? "Stroke the cat to pet"
                : "Tap the cat to pet";
        }

        private void ResizeLabel(string name, float maximum)
        {
            if (!controls.TryGetValue(name, out RectTransform control) || control == null) return;
            Text label = control.GetComponentInChildren<Text>(true);
            if (label != null) label.fontSize = Mathf.Clamp(Mathf.RoundToInt(maximum), 15, 30);
        }

        private void RefreshDiagnostics()
        {
            if (controls.TryGetValue("CatActivityReadout", out RectTransform readout) && readout != null)
                readout.gameObject.SetActive(Debug.isDebugBuild && diagnosticsOpen);
        }

        private void HookMenuControl(string name)
        {
            if (!controls.TryGetValue(name, out RectTransform control) || control == null || hookedMenuControls.Contains(name)) return;
            Button button = control.GetComponent<Button>();
            if (button == null) return;
            if (name == "CatGenerationToggle")
            {
                Text label = button.GetComponentInChildren<Text>(true);
                if (label != null) label.text = "My Cat";
            }
            button.onClick.AddListener(() => { moreOpen = false; UpdateTrayVisibility(); });
            hookedMenuControls.Add(name);
        }

        private void HookPlayControl(string name)
        {
            if (!controls.TryGetValue(name, out RectTransform control) || control == null || hookedMenuControls.Contains(name)) return;
            Button button = control.GetComponent<Button>();
            if (button == null) return;
            button.onClick.AddListener(() => { playOpen = false; UpdateTrayVisibility(); });
            hookedMenuControls.Add(name);
        }

        private void OnDestroy()
        {
            if (roundedSprite != null) Destroy(roundedSprite);
            if (roundedTexture != null) Destroy(roundedTexture);
            if (screenMaskSprite != null) Destroy(screenMaskSprite);
            if (screenMaskTexture != null) Destroy(screenMaskTexture);
        }
    }
}
