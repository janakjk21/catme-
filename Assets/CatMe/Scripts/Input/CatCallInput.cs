using CatMe.Cat;
using CatMe.CameraSystem;
using CatMe.UI;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;

namespace CatMe.GameInput
{
    /// <summary>
    /// Temporary Phase 5 validation input. It owns one Call button and forwards
    /// the authored destination request to CatMotor.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class CatCallInput : MonoBehaviour
    {
        private const float DebounceSeconds = 0.35f;
        private CatMotor motor;
        private Transform callDestination;
        private Button callButton;
        private Text callLabel;
        private Text statusLabel;
        private float nextAllowedPress;
        private RoomOrbitCamera roomCamera;
        private CatRoomAudio roomAudio;
        private Transform seatedCallDestination;
        private bool seatedCallPending;
        private float seatedCallStartedAt;
        private float statusClearAt;

        public void Initialize(CatMotor readyMotor, CatRoomAudio audioFeedback = null)
        {
            motor = readyMotor;
            roomAudio = audioFeedback;
            roomCamera = FindAnyObjectByType<RoomOrbitCamera>();
            callDestination = FindDestination();
            CreateControl();
            callButton.interactable = motor != null && motor.IsReady && callDestination != null;
        }

        private void CreateControl()
        {
            Canvas canvas = FindFirstObjectByType<Canvas>();
            if (canvas == null)
            {
                GameObject canvasObject = new GameObject("CatCallValidationCanvas");
                canvas = canvasObject.AddComponent<Canvas>();
                canvas.renderMode = RenderMode.ScreenSpaceOverlay;
                canvasObject.AddComponent<CanvasScaler>();
                canvasObject.AddComponent<GraphicRaycaster>();
            }

            CanvasScaler scaler = canvas.GetComponent<CanvasScaler>();
            if (scaler != null)
            {
                scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
                scaler.referenceResolution = new Vector2(1080f, 1920f);
                scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
                scaler.matchWidthOrHeight = 0.5f;
            }

            EventSystem eventSystem = FindFirstObjectByType<EventSystem>();
            if (eventSystem == null)
            {
                GameObject eventObject = new GameObject("EventSystem");
                eventSystem = eventObject.AddComponent<EventSystem>();
            }
            StandaloneInputModule legacyModule = eventSystem.GetComponent<StandaloneInputModule>();
            if (legacyModule != null)
            {
                legacyModule.enabled = false;
                Destroy(legacyModule);
            }
            if (eventSystem.GetComponent<InputSystemUIInputModule>() == null)
                eventSystem.gameObject.AddComponent<InputSystemUIInputModule>();

            Transform existingSafeArea = canvas.transform.Find("CallSafeArea");
            RectTransform safeArea;
            if (existingSafeArea == null)
            {
                GameObject safeAreaObject = new GameObject("CallSafeArea", typeof(RectTransform));
                safeArea = safeAreaObject.GetComponent<RectTransform>();
                safeArea.SetParent(canvas.transform, false);
            }
            else safeArea = existingSafeArea.GetComponent<RectTransform>();
            safeArea.anchorMin = Vector2.zero;
            safeArea.anchorMax = Vector2.one;
            safeArea.offsetMin = Vector2.zero;
            safeArea.offsetMax = Vector2.zero;
            ApplySafeArea(safeArea);
            if (safeArea.GetComponent<CatHomeHud>() == null) safeArea.gameObject.AddComponent<CatHomeHud>();

            GameObject buttonObject = new GameObject("CallButton", typeof(RectTransform), typeof(Image), typeof(Button));
            buttonObject.transform.SetParent(safeArea, false);
            RectTransform buttonRect = buttonObject.GetComponent<RectTransform>();
            buttonRect.anchorMin = new Vector2(0.5f, 0f);
            buttonRect.anchorMax = new Vector2(0.5f, 0f);
            buttonRect.pivot = new Vector2(0.5f, 0f);
            buttonRect.anchoredPosition = new Vector2(-120f, 160f);
            buttonRect.sizeDelta = new Vector2(220f, 96f);
            Image image = buttonObject.GetComponent<Image>();
            image.color = new Color(0.08f, 0.16f, 0.24f, 0.96f);
            callButton = buttonObject.GetComponent<Button>();
            callButton.targetGraphic = image;
            callButton.onClick.AddListener(OnCallPressed);

            GameObject labelObject = new GameObject("Label", typeof(RectTransform), typeof(Text));
            labelObject.transform.SetParent(buttonObject.transform, false);
            RectTransform labelRect = labelObject.GetComponent<RectTransform>();
            labelRect.anchorMin = Vector2.zero;
            labelRect.anchorMax = Vector2.one;
            labelRect.offsetMin = Vector2.zero;
            labelRect.offsetMax = Vector2.zero;
            callLabel = labelObject.GetComponent<Text>();
            callLabel.text = "Call";
            callLabel.alignment = TextAnchor.MiddleCenter;
            callLabel.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            callLabel.fontSize = 34;
            callLabel.fontStyle = FontStyle.Bold;
            callLabel.color = Color.white;

            GameObject statusObject = new GameObject("SeatedCallStatus", typeof(RectTransform), typeof(Text));
            statusObject.transform.SetParent(safeArea, false);
            RectTransform statusRect = statusObject.GetComponent<RectTransform>();
            statusRect.anchorMin = new Vector2(0.5f, 1f);
            statusRect.anchorMax = new Vector2(0.5f, 1f);
            statusRect.pivot = new Vector2(0.5f, 1f);
            statusRect.anchoredPosition = new Vector2(0f, -142f);
            statusRect.sizeDelta = new Vector2(520f, 76f);
            statusLabel = statusObject.GetComponent<Text>();
            statusLabel.alignment = TextAnchor.MiddleCenter;
            statusLabel.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            statusLabel.fontSize = 30;
            statusLabel.fontStyle = FontStyle.Bold;
            statusLabel.color = Color.white;
            statusLabel.text = string.Empty;
            statusLabel.raycastTarget = false;
            Outline statusOutline = statusLabel.gameObject.AddComponent<Outline>();
            statusOutline.effectColor = new Color(0.05f, 0.07f, 0.08f, 0.92f);
            statusOutline.effectDistance = new Vector2(2f, -2f);
        }

        private void Update()
        {
            if (callButton != null)
            {
                callButton.interactable = motor != null && motor.IsReady && callDestination != null && !motor.IsActivityLocked;
            }

            if (statusLabel != null)
            {
                statusLabel.enabled = seatedCallPending || Time.unscaledTime < statusClearAt;

                if (seatedCallPending && motor != null)
                {
                    if (Time.unscaledTime >= statusClearAt)
                    {
                        seatedCallPending = false;
                        statusLabel.text = string.Empty;
                    }
                    else if (motor.HasFullyArrived)
                    {
                        statusLabel.text = "Here with you";
                        seatedCallPending = false;
                        statusClearAt = Time.unscaledTime + 1.6f;
                    }
                    else if (Time.unscaledTime >= seatedCallStartedAt + 0.4f && motor.IsMoving)
                    {
                        statusLabel.text = "Coming over";
                    }
                }
                else if (Time.unscaledTime >= statusClearAt)
                {
                    statusLabel.text = string.Empty;
                }
            }
        }

        private void OnCallPressed()
        {
            if (Time.unscaledTime < nextAllowedPress || motor == null || callDestination == null || motor.IsActivityLocked)
            {
                return;
            }

            nextAllowedPress = Time.unscaledTime + DebounceSeconds;
            Transform destination = callDestination;
            bool seatedCall = roomCamera != null && roomCamera.IsSeatedView;
            if (seatedCall && roomCamera.TryGetSeatedCallPose(out Vector3 approachPosition, out Quaternion approachRotation))
            {
                if (seatedCallDestination == null)
                {
                    seatedCallDestination = new GameObject("SeatedCallApproachRuntime").transform;
                }
                seatedCallDestination.SetPositionAndRotation(approachPosition, approachRotation);
                destination = seatedCallDestination;
            }

            if (motor.TryMoveTo(destination, seatedCall ? "Seated call" : "Call"))
            {
                roomAudio?.PlayCallMeow();
                seatedCallPending = true;
                seatedCallStartedAt = Time.unscaledTime;
                statusClearAt = seatedCallStartedAt + 14f;
                if (statusLabel != null) statusLabel.text = "Cat heard you";
                if (!seatedCall) roomCamera?.FocusOnCat(motor.transform);
            }
        }

        private static Transform FindDestination()
        {
            GameObject destination = GameObject.Find("CallDestination");
            return destination == null ? null : destination.transform;
        }

        private static void ApplySafeArea(RectTransform safeArea)
        {
            Rect safe = Screen.safeArea;
            safeArea.anchorMin = new Vector2(safe.xMin / Screen.width, safe.yMin / Screen.height);
            safeArea.anchorMax = new Vector2(safe.xMax / Screen.width, safe.yMax / Screen.height);
        }
    }
}
