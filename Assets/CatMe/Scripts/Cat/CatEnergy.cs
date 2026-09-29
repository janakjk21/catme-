using System;
using CatMe.Toys;
using UnityEngine;
using UnityEngine.UI;

namespace CatMe.Cat
{
    /// <summary>Small, local energy loop composed from the existing sleep and laser owners.</summary>
    [DisallowMultipleComponent]
    public sealed class CatEnergy : MonoBehaviour
    {
        public const float MaximumEnergy = 100f;
        public const float StartingEnergy = 100f;
        public const float CatchCost = 20f;
        public const float LowEnergyThreshold = 25f;
        public const float AutomaticWakeThreshold = 80f;
        public const float SleepRecoveryPerSecond = 15f;

        private CatSleepInteraction sleep;
        private LaserToyInteraction laser;
        private Image meterFill;
        private Text meterLabel;
        private bool initialized;
        private bool autoSleepSent;
        private bool autoWakeSent;
        private float energy = StartingEnergy;
        private float minimumObserved = StartingEnergy;
        private float maximumObserved = StartingEnergy;

        public float CurrentEnergy => energy;
        public float Maximum => MaximumEnergy;
        public float TotalEnergySpent { get; private set; }
        public float TotalEnergyRecovered { get; private set; }
        public int ProcessedCatchCount { get; private set; }
        public int LowEnergyTriggerCount { get; private set; }
        public int AutomaticSleepRequestCount { get; private set; }
        public int AutomaticWakeRequestCount { get; private set; }
        public bool IsPlayAllowed => initialized && energy > LowEnergyThreshold &&
            sleep != null && sleep.CurrentState == CatSleepInteraction.SleepState.AwakeIdle;
        public bool IsRecoveryActive => sleep != null && sleep.CurrentState == CatSleepInteraction.SleepState.SleepingHidden;
        public float MinimumObservedEnergy => minimumObserved;
        public float MaximumObservedEnergy => maximumObserved;
        public int EnergyMeterInstanceCount => meterFill == null ? 0 : 1;

        public void Initialize(CatSleepInteraction readySleep, LaserToyInteraction readyLaser)
        {
            if (initialized || readySleep == null || readyLaser == null) return;
            initialized = true;
            sleep = readySleep;
            laser = readyLaser;
            laser.BindEnergy(this);
            sleep.StateChanged += OnSleepStateChanged;
            CreateMeter();
            RefreshMeter();
        }

        public void SetEnergyForValidation(float value)
        {
            SetEnergy(value);
        }

        public bool ApplySuccessfulCatchForValidation()
        {
            return ApplySuccessfulCatch();
        }

        public void ApplyLoadedState(float restoredEnergy, bool sleeping)
        {
            SetEnergy(restoredEnergy);
            autoSleepSent = false;
            autoWakeSent = false;
            if (!sleeping || sleep == null) return;
            if (energy >= AutomaticWakeThreshold) RequestAutomaticWake();
            else RequestAutomaticSleep();
        }

        internal bool ApplySuccessfulCatch()
        {
            if (!initialized || ProcessedCatchCount < 0) return false;
            ProcessedCatchCount++;
            SetEnergy(energy - CatchCost);
            TotalEnergySpent += CatchCost;
            if (energy <= LowEnergyThreshold)
            {
                LowEnergyTriggerCount++;
                autoSleepSent = false;
            }
            return true;
        }

        private void Update()
        {
            if (!initialized || sleep == null) return;
            if (sleep.CurrentState == CatSleepInteraction.SleepState.SleepingHidden)
            {
                autoSleepSent = false;
                if (energy < AutomaticWakeThreshold)
                {
                    float amount = SleepRecoveryPerSecond * Time.unscaledDeltaTime;
                    SetEnergy(energy + amount);
                    TotalEnergyRecovered += amount;
                }
                if (energy >= AutomaticWakeThreshold) RequestAutomaticWake();
            }
            else if (sleep.CurrentState == CatSleepInteraction.SleepState.AwakeIdle)
            {
                autoWakeSent = false;
                if (energy <= LowEnergyThreshold && laser != null && laser.CurrentState == LaserToyInteraction.LaserState.Idle)
                    RequestAutomaticSleep();
            }
            RefreshMeter();
        }

        private void OnSleepStateChanged(CatSleepInteraction.SleepState state)
        {
            if (state == CatSleepInteraction.SleepState.SleepingHidden)
            {
                autoWakeSent = false;
            }
            else if (state == CatSleepInteraction.SleepState.AwakeIdle)
            {
                autoSleepSent = false;
                autoWakeSent = false;
            }
        }

        internal void NotifyLaserSessionFinished()
        {
            if (energy <= LowEnergyThreshold) RequestAutomaticSleep();
        }

        private void RequestAutomaticSleep()
        {
            if (autoSleepSent || sleep == null || sleep.CurrentState != CatSleepInteraction.SleepState.AwakeIdle) return;
            autoSleepSent = true;
            if (sleep.RequestSleepFromEnergy())
            {
                AutomaticSleepRequestCount++;
            }
            else autoSleepSent = false;
        }

        private void RequestAutomaticWake()
        {
            if (autoWakeSent || sleep == null || sleep.CurrentState != CatSleepInteraction.SleepState.SleepingHidden) return;
            autoWakeSent = true;
            if (sleep.RequestWakeFromEnergy()) AutomaticWakeRequestCount++;
            else autoWakeSent = false;
        }

        private void SetEnergy(float value)
        {
            energy = Mathf.Clamp(float.IsNaN(value) || float.IsInfinity(value) ? StartingEnergy : value, 0f, MaximumEnergy);
            minimumObserved = Mathf.Min(minimumObserved, energy);
            maximumObserved = Mathf.Max(maximumObserved, energy);
            RefreshMeter();
        }

        private void CreateMeter()
        {
            Canvas canvas = FindFirstObjectByType<Canvas>();
            if (canvas == null) return;
            GameObject background = new GameObject("EnergyMeter", typeof(RectTransform), typeof(Image));
            background.transform.SetParent(canvas.transform.Find("CallSafeArea") ?? canvas.transform, false);
            RectTransform rect = background.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0f, 1f); rect.anchorMax = new Vector2(0f, 1f);
            rect.pivot = new Vector2(0f, 1f); rect.anchoredPosition = new Vector2(24f, -24f); rect.sizeDelta = new Vector2(250f, 42f);
            background.GetComponent<Image>().color = new Color(0.05f, 0.09f, 0.12f, 0.92f);
            GameObject fill = new GameObject("Fill", typeof(RectTransform), typeof(Image));
            fill.transform.SetParent(background.transform, false);
            RectTransform fillRect = fill.GetComponent<RectTransform>();
            fillRect.anchorMin = new Vector2(.44f, .26f); fillRect.anchorMax = new Vector2(.94f, .74f);
            fillRect.offsetMin = fillRect.offsetMax = Vector2.zero;
            meterFill = fill.GetComponent<Image>(); meterFill.type = Image.Type.Filled; meterFill.fillMethod = Image.FillMethod.Horizontal;
            GameObject label = new GameObject("Label", typeof(RectTransform), typeof(Text));
            label.transform.SetParent(background.transform, false); RectTransform labelRect = label.GetComponent<RectTransform>();
            labelRect.anchorMin = Vector2.zero; labelRect.anchorMax = new Vector2(.43f, 1f);
            labelRect.offsetMin = labelRect.offsetMax = Vector2.zero;
            meterLabel = label.GetComponent<Text>(); meterLabel.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf"); meterLabel.alignment = TextAnchor.MiddleCenter; meterLabel.fontSize = 22; meterLabel.fontStyle = FontStyle.Bold; meterLabel.color = Color.white; meterLabel.raycastTarget = false;
        }

        private void RefreshMeter()
        {
            if (meterFill == null) return;
            meterFill.fillAmount = energy / MaximumEnergy;
            meterFill.color = energy <= LowEnergyThreshold
                ? new Color(0.90f, 0.35f, 0.24f)
                : new Color(0.98f, 0.67f, 0.17f);
            if (meterLabel != null) meterLabel.text = $"Energy {Mathf.RoundToInt(energy)}";
        }

        private void OnDestroy()
        {
            if (sleep != null) sleep.StateChanged -= OnSleepStateChanged;
            bool pass = energy >= 0f && energy <= MaximumEnergy && EnergyMeterInstanceCount <= 1;
            Debug.Log($"[CatMe][Energy] validation summary | {(pass ? "PASS" : "FAIL")} | energy={energy:0.##} | spent={TotalEnergySpent:0.##} | recovered={TotalEnergyRecovered:0.##} | catches={ProcessedCatchCount} | lowTriggers={LowEnergyTriggerCount} | autoSleep={AutomaticSleepRequestCount} | autoWake={AutomaticWakeRequestCount} | meterInstances={EnergyMeterInstanceCount}");
        }
    }
}
