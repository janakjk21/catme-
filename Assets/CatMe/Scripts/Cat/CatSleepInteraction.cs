using System.Collections;
using System;
using CatMe.CameraSystem;
using CatMe.GameInput;
using UnityEngine;
using UnityEngine.UI;

namespace CatMe.Cat
{
    /// <summary>Manual, occluded cat-house sleep loop for the HomeRoom prototype.</summary>
    [DisallowMultipleComponent]
    public sealed class CatSleepInteraction : MonoBehaviour
    {
        public enum SleepState { AwakeIdle, WalkingToHouse, EnteringHouse, SleepingHidden, ExitingHouse }

        private const float TransitionSeconds = 0.45f;
        private CatMotor motor;
        private RoomOrbitCamera roomCamera;
        private LocalCatAssetLoader loader;
        private Transform catModelRoot;
        private Transform approachPoint;
        private Transform insidePoint;
        private Renderer[] renderers;
        private bool[] rendererStates;
        private Button sleepButton;
        private Text sleepLabel;
        private Text zzzCue;
        private Transform catHouse;
        private float cueElapsed;
        private float maximumProtectedLocalDrift;
        private Vector3 initialModelPosition;
        private Vector3 initialAxisPosition;
        private Vector3 initialLoadedPosition;
        private bool initialized;
        private bool restoredRendererStates;
        private float wakePositionError = float.PositiveInfinity;

        public SleepState CurrentState { get; private set; } = SleepState.AwakeIdle;
        public int SleepRequestCount { get; private set; }
        public int SuccessfulEntryCount { get; private set; }
        public int SuccessfulWakeCount { get; private set; }
        public bool HiddenActivityLockActive => motor != null && motor.IsActivityLocked;
        public bool AllCachedRendererStatesRestored => restoredRendererStates;
        public float MaximumProtectedLocalDrift => maximumProtectedLocalDrift;
        public float WakePositionError => wakePositionError;
        public bool IsZzzCueActive => zzzCue != null && zzzCue.gameObject.activeSelf;
        public event Action<SleepState> StateChanged;

        public bool RequestSleepFromEnergy()
        {
            if (CurrentState != SleepState.AwakeIdle) return false;
            OnSleepPressed();
            return CurrentState == SleepState.WalkingToHouse;
        }

        public bool RequestWakeFromEnergy()
        {
            if (CurrentState != SleepState.SleepingHidden) return false;
            OnSleepPressed();
            return CurrentState == SleepState.ExitingHouse;
        }

        public bool RequestSleepForValidation()
        {
            if (CurrentState != SleepState.AwakeIdle)
            {
                return false;
            }
            OnSleepPressed();
            return CurrentState == SleepState.WalkingToHouse;
        }

        public bool RequestWakeForValidation()
        {
            if (CurrentState != SleepState.SleepingHidden)
            {
                return false;
            }
            OnSleepPressed();
            return CurrentState == SleepState.ExitingHouse;
        }

        public void Initialize(LocalCatAssetLoader readyLoader, Transform modelRoot, CatMotor readyMotor)
        {
            if (initialized || readyLoader == null || modelRoot == null || readyMotor == null)
            {
                return;
            }

            initialized = true;
            loader = readyLoader;
            catModelRoot = modelRoot;
            motor = readyMotor;
            roomCamera = FindAnyObjectByType<RoomOrbitCamera>();
            approachPoint = FindPoint("SleepApproach");
            insidePoint = FindPoint("SleepInside");
            catHouse = GameObject.Find("CatHouse")?.transform;
            renderers = loader.LoadedRoot.GetComponentsInChildren<Renderer>(true);
            rendererStates = new bool[renderers.Length];
            initialModelPosition = catModelRoot.localPosition;
            Transform axis = catModelRoot.Find("AxisCorrection");
            initialAxisPosition = axis == null ? Vector3.zero : axis.localPosition;
            initialLoadedPosition = loader.LoadedRoot.localPosition;
            CreateControl();
            CreateCue();
        }

        private void Update()
        {
            if (!initialized)
            {
                return;
            }

            TrackProtectedDrift();
            if (CurrentState == SleepState.WalkingToHouse && motor.HasFullyArrived)
            {
                StartCoroutine(EnterHouse());
            }

            if (zzzCue != null && zzzCue.gameObject.activeSelf)
            {
                cueElapsed += Time.unscaledDeltaTime;
                UpdateCuePosition();
            }

            if (sleepButton != null)
            {
                sleepButton.interactable = motor.IsReady &&
                    (CurrentState == SleepState.AwakeIdle || CurrentState == SleepState.SleepingHidden);
            }
        }

        private void OnSleepPressed()
        {
            if (CurrentState == SleepState.AwakeIdle)
            {
                if (approachPoint == null || insidePoint == null || !motor.PrepareForPlayerAction())
                {
                    return;
                }

                SleepRequestCount++;
                if (motor.TryMoveTo(approachPoint, "Sleep"))
                {
                    SetState(SleepState.WalkingToHouse);
                    SetButtonText("Sleep");
                    roomCamera?.FocusOnActivity(catModelRoot, 3.0f, -16f, 28f);
                }
                return;
            }

            if (CurrentState == SleepState.SleepingHidden)
            {
                StartCoroutine(ExitHouse());
            }
        }

        private IEnumerator EnterHouse()
        {
            SetState(SleepState.EnteringHouse);
            motor.SetHiddenActivityLock(true);
            roomCamera?.FocusOnActivity(catHouse, 2.8f, 18f, 30f);
            yield return new WaitForSecondsRealtime(TransitionSeconds);
            CacheRendererStates();
            SetRenderersEnabled(false);
            if (!motor.PlaceAtHiddenPoint(insidePoint))
            {
                motor.SetHiddenActivityLock(false);
                SetRenderersEnabled(true);
                SetState(SleepState.AwakeIdle);
                yield break;
            }

            SuccessfulEntryCount++;
            SetState(SleepState.SleepingHidden);
            SetCueActive(true);
            SetButtonText("Wake");
        }

        private IEnumerator ExitHouse()
        {
            SetState(SleepState.ExitingHouse);
            roomCamera?.FocusOnActivity(catHouse, 2.8f, 18f, 30f);
            yield return new WaitForSecondsRealtime(TransitionSeconds);
            if (!motor.RestoreAt(approachPoint, out wakePositionError))
            {
                SetState(SleepState.SleepingHidden);
                yield break;
            }

            SetRenderersEnabledFromCache();
            restoredRendererStates = true;
            SetCueActive(false);
            motor.SetHiddenActivityLock(false);
            SuccessfulWakeCount++;
            SetState(SleepState.AwakeIdle);
            SetButtonText("Sleep");
            roomCamera?.ReturnToRoomView();
        }

        private void CacheRendererStates()
        {
            for (int i = 0; i < renderers.Length; i++)
            {
                rendererStates[i] = renderers[i] != null && renderers[i].enabled;
            }
            restoredRendererStates = false;
        }

        private void SetRenderersEnabled(bool enabled)
        {
            foreach (Renderer renderer in renderers)
            {
                if (renderer != null)
                {
                    renderer.enabled = enabled;
                }
            }
        }

        private void SetRenderersEnabledFromCache()
        {
            for (int i = 0; i < renderers.Length; i++)
            {
                if (renderers[i] != null)
                {
                    renderers[i].enabled = rendererStates[i];
                }
            }
        }

        private void CreateControl()
        {
            Canvas canvas = FindFirstObjectByType<Canvas>();
            if (canvas == null)
            {
                GameObject canvasObject = new GameObject("CatSleepValidationCanvas");
                canvas = canvasObject.AddComponent<Canvas>();
                canvas.renderMode = RenderMode.ScreenSpaceOverlay;
                canvasObject.AddComponent<CanvasScaler>();
                canvasObject.AddComponent<GraphicRaycaster>();
            }

            GameObject buttonObject = new GameObject("SleepButton", typeof(RectTransform), typeof(Image), typeof(Button));
            buttonObject.transform.SetParent(canvas.transform.Find("CallSafeArea") ?? canvas.transform, false);
            RectTransform rect = buttonObject.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 0f);
            rect.anchorMax = new Vector2(0.5f, 0f);
            rect.pivot = new Vector2(0.5f, 0f);
            rect.anchoredPosition = new Vector2(120f, 160f);
            rect.sizeDelta = new Vector2(220f, 96f);
            Image image = buttonObject.GetComponent<Image>();
            image.color = new Color(0.08f, 0.24f, 0.16f, 0.96f);
            sleepButton = buttonObject.GetComponent<Button>();
            sleepButton.targetGraphic = image;
            sleepButton.onClick.AddListener(OnSleepPressed);

            GameObject labelObject = new GameObject("Label", typeof(RectTransform), typeof(Text));
            labelObject.transform.SetParent(buttonObject.transform, false);
            RectTransform labelRect = labelObject.GetComponent<RectTransform>();
            labelRect.anchorMin = Vector2.zero;
            labelRect.anchorMax = Vector2.one;
            labelRect.offsetMin = Vector2.zero;
            labelRect.offsetMax = Vector2.zero;
            sleepLabel = labelObject.GetComponent<Text>();
            sleepLabel.alignment = TextAnchor.MiddleCenter;
            sleepLabel.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            sleepLabel.fontSize = 34;
            sleepLabel.fontStyle = FontStyle.Bold;
            sleepLabel.color = Color.white;
            SetButtonText("Sleep");
        }

        private void CreateCue()
        {
            Canvas canvas = FindFirstObjectByType<Canvas>();
            if (canvas == null)
            {
                return;
            }

            GameObject cueObject = new GameObject("SleepZzzCue", typeof(RectTransform), typeof(Text));
            cueObject.transform.SetParent(canvas.transform, false);
            zzzCue = cueObject.GetComponent<Text>();
            zzzCue.alignment = TextAnchor.MiddleCenter;
            zzzCue.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            zzzCue.fontSize = 42;
            zzzCue.fontStyle = FontStyle.Bold;
            zzzCue.color = new Color(0.85f, 0.95f, 1f, 0.95f);
            zzzCue.raycastTarget = false;
            SetCueActive(false);
        }

        private void SetCueActive(bool active)
        {
            if (zzzCue == null)
            {
                return;
            }

            zzzCue.gameObject.SetActive(active);
            if (active)
            {
                cueElapsed = 0f;
                zzzCue.text = "Zzz";
                UpdateCuePosition();
            }
        }

        private void UpdateCuePosition()
        {
            if (catHouse == null || zzzCue == null || Camera.main == null)
            {
                return;
            }

            Vector3 screen = Camera.main.WorldToScreenPoint(catHouse.position + Vector3.up * 1.05f);
            screen.y += Mathf.Sin(cueElapsed * 1.8f) * 8f;
            zzzCue.rectTransform.position = screen;
        }

        private void SetButtonText(string value)
        {
            if (sleepLabel != null)
            {
                sleepLabel.text = value;
            }
        }

        private void SetState(SleepState state)
        {
            if (CurrentState == state) return;
            CurrentState = state;
            StateChanged?.Invoke(state);
        }

        private void TrackProtectedDrift()
        {
            if (catModelRoot == null || loader == null || loader.LoadedRoot == null)
            {
                return;
            }

            maximumProtectedLocalDrift = Mathf.Max(maximumProtectedLocalDrift,
                Vector3.Distance(catModelRoot.localPosition, initialModelPosition));
            Transform axis = catModelRoot.Find("AxisCorrection");
            if (axis != null)
            {
                maximumProtectedLocalDrift = Mathf.Max(maximumProtectedLocalDrift,
                    Vector3.Distance(axis.localPosition, initialAxisPosition));
            }
            maximumProtectedLocalDrift = Mathf.Max(maximumProtectedLocalDrift,
                Vector3.Distance(loader.LoadedRoot.localPosition, initialLoadedPosition));
        }

        private static Transform FindPoint(string name)
        {
            GameObject point = GameObject.Find(name);
            return point == null ? null : point.transform;
        }

        private void OnDestroy()
        {
            if (SleepRequestCount == 0 && SuccessfulEntryCount == 0 && SuccessfulWakeCount == 0)
            {
                Debug.Log("[CatMe][Sleep] validation summary | NOT_RUN | sleep interaction was not exercised in this regression.");
                return;
            }

            bool pass = SuccessfulEntryCount >= 2 && SuccessfulWakeCount >= 2 &&
                        !HiddenActivityLockActive && AllCachedRendererStatesRestored &&
                        !IsZzzCueActive && maximumProtectedLocalDrift <= 0.02f &&
                        wakePositionError <= 0.10f;
            Debug.Log($"[CatMe][Sleep] validation summary | {(pass ? "PASS" : "FAIL")} | state={CurrentState} | " +
                      $"requests={SleepRequestCount} | entries={SuccessfulEntryCount} | wakes={SuccessfulWakeCount} | " +
                      $"activityLock={HiddenActivityLockActive} | rendererRestore={AllCachedRendererStatesRestored} | " +
                      $"maxProtectedLocalDrift={maximumProtectedLocalDrift:0.######}m | wakePositionError={wakePositionError:0.####}m | " +
                      $"zzzActive={IsZzzCueActive}");
        }
    }
}
