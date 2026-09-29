using CatMe.CameraSystem;
using UnityEngine;
using UnityEngine.UI;

namespace CatMe.Cat
{
    /// <summary>
    /// Gives the room cat occasional purposeful walks between authored points.
    /// Player actions can cancel these low-priority moves through CatMotor.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class CatAmbientBehavior : MonoBehaviour
    {
        [SerializeField] private float minimumPauseSeconds = 28f;
        [SerializeField] private float maximumPauseSeconds = 46f;
        [SerializeField] private float minimumRestSeconds = 16f;
        [SerializeField] private float maximumRestSeconds = 29f;
        [SerializeField] private float cameraFollowDistance = 4.6f;

        private CatMotor motor;
        private CatCompanionJournal journal;
        private CatEnergy energy;
        private RoomOrbitCamera roomCamera;
        private Transform[] destinations;
        private float nextDecisionAt;
        private Transform currentDestination;
        private Text activityReadout;
        private bool initialized;

        public string CurrentActivity { get; private set; } = "Settling";
        public string CurrentReason { get; private set; } = "The cat is taking a quiet moment.";

        public void Initialize(CatMotor readyMotor)
        {
            if (initialized || readyMotor == null) return;
            motor = readyMotor;
            journal = GetComponent<CatCompanionJournal>();
            energy = GetComponent<CatEnergy>();
            roomCamera = FindAnyObjectByType<RoomOrbitCamera>();
            destinations = new[]
            {
                FindPoint("WindowApproach"),
                FindPoint("ToyApproach"),
                FindPoint("CallDestination"),
                FindPoint("CatSpawn")
            };
            initialized = true;
            ScheduleNextActivity(minimumPauseSeconds, maximumPauseSeconds);
            if (Debug.isDebugBuild) CreateDevelopmentReadout();
            Debug.Log("[CatMe][Ambient] Ready | authored destinations=" + CountDestinations());
        }

        private void Update()
        {
            if (!initialized || motor == null) return;
            RefreshDevelopmentReadout();

            if (motor.IsAmbientCommand)
            {
                if (motor.IsAmbientMoving)
                {
                    if (!CurrentActivity.StartsWith("Walking"))
                    {
                        CurrentActivity = "Walking to " + (currentDestination == null ? "a room spot" : currentDestination.name);
                        CurrentReason = "The cat is choosing a place to look around.";
                    }
                    return;
                }

                if (motor.HasFullyArrived)
                {
                    CurrentActivity = "Looking around";
                    CurrentReason = "The cat has reached a familiar room spot.";
                    roomCamera?.ReturnToRoomView();
                    if (currentDestination != null && currentDestination.name == "WindowApproach")
                        journal?.RecordDiscovery("first_window_watch");
                    else if (currentDestination != null && currentDestination.name == "CallDestination")
                        journal?.RecordDiscovery("first_voluntary_approach");
                    motor.TryClearAmbientCommand();
                    ScheduleNextActivity(minimumRestSeconds, maximumRestSeconds);
                    Debug.Log("[CatMe][Ambient] Arrived | spot=" + (currentDestination == null ? "unknown" : currentDestination.name));
                    return;
                }

                return;
            }

            if (motor.IsMoving || motor.IsActivityLocked)
            {
                if (!motor.IsMoving) ScheduleNextActivity(20f, 30f);
                CurrentActivity = motor.IsActivityLocked ? "Player interaction" : "Following the player";
                CurrentReason = motor.IsActivityLocked ? "The player is using a toy or room activity." : "The cat is responding to the player.";
                return;
            }

            if (Time.unscaledTime < nextDecisionAt) return;
            TryStartActivity();
        }

        private void TryStartActivity()
        {
            if (destinations == null || destinations.Length == 0)
            {
                ScheduleNextActivity(minimumPauseSeconds, maximumPauseSeconds);
                return;
            }

            Transform destination = null;
            float totalWeight = 0f;
            float[] weights = new float[destinations.Length];
            for (int index = 0; index < destinations.Length; index++)
            {
                Transform candidate = destinations[index];
                if (candidate == null || Vector3.Distance(candidate.position, motor.transform.position) < 0.8f) continue;
                float playfulness = journal == null ? 0.62f : journal.Playfulness;
                float curiosity = journal == null ? 0.58f : journal.Curiosity;
                float affection = journal == null ? 0.54f : journal.Affection;
                float confidence = journal == null ? 0.50f : journal.Confidence;
                float energyRatio = energy == null ? 0.8f : energy.CurrentEnergy / Mathf.Max(1f, energy.Maximum);
                weights[index] = candidate.name == "ToyApproach" ? 0.2f + playfulness * Mathf.Lerp(0.25f, 1.6f, energyRatio) :
                    candidate.name == "WindowApproach" ? 0.45f + curiosity * 1.25f :
                    candidate.name == "CallDestination" ? 0.25f + affection * 1.2f + confidence * 0.5f :
                    0.35f + confidence * 0.45f;
                if (candidate == currentDestination) weights[index] *= 0.35f;
                totalWeight += weights[index];
            }

            float choice = Random.value * totalWeight;
            for (int index = 0; index < destinations.Length; index++)
            {
                if (weights[index] <= 0f) continue;
                choice -= weights[index];
                if (choice <= 0f) { destination = destinations[index]; break; }
            }

            if (destination == null)
            {
                CurrentActivity = "Resting";
                CurrentReason = "The cat is comfortable where it is.";
                ScheduleNextActivity(minimumPauseSeconds, maximumPauseSeconds);
                return;
            }

            currentDestination = destination;
            if (motor.TryMoveToAmbient(destination, "Ambient room exploration"))
            {
                CurrentActivity = "Walking to " + destination.name;
                CurrentReason = destination.name == "WindowApproach"
                    ? "The cat is going to watch the window."
                    : "The cat is exploring a familiar part of the room.";
                roomCamera?.FocusOnActivity(motor.transform, cameraFollowDistance);
                Debug.Log($"[CatMe][Ambient] Started | activity={CurrentActivity} | reason={CurrentReason}");
            }
            ScheduleNextActivity(minimumRestSeconds, maximumRestSeconds);
        }

        private void ScheduleNextActivity(float minimum, float maximum)
        {
            nextDecisionAt = Time.unscaledTime + Random.Range(minimum, maximum);
        }

        private int CountDestinations()
        {
            if (destinations == null) return 0;
            int count = 0;
            foreach (Transform destination in destinations) if (destination != null) count++;
            return count;
        }

        private void CreateDevelopmentReadout()
        {
            Canvas canvas = FindAnyObjectByType<Canvas>();
            if (canvas == null) return;
            Transform parent = canvas.transform.Find("CallSafeArea") ?? canvas.transform;
            GameObject panel = new GameObject("CatActivityReadout", typeof(RectTransform), typeof(Image));
            panel.transform.SetParent(parent, false);
            RectTransform panelRect = panel.GetComponent<RectTransform>();
            panelRect.anchorMin = new Vector2(0f, 1f);
            panelRect.anchorMax = new Vector2(0f, 1f);
            panelRect.pivot = new Vector2(0f, 1f);
            panelRect.anchoredPosition = new Vector2(24f, -78f);
            panelRect.sizeDelta = new Vector2(280f, 72f);
            panel.GetComponent<Image>().color = new Color(0.08f, 0.10f, 0.13f, 0.78f);
            GameObject label = new GameObject("Activity", typeof(RectTransform), typeof(Text));
            label.transform.SetParent(panel.transform, false);
            RectTransform labelRect = label.GetComponent<RectTransform>();
            labelRect.anchorMin = Vector2.zero;
            labelRect.anchorMax = Vector2.one;
            labelRect.offsetMin = new Vector2(10f, 5f);
            labelRect.offsetMax = new Vector2(-10f, -5f);
            activityReadout = label.GetComponent<Text>();
            activityReadout.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            activityReadout.fontSize = 15;
            activityReadout.color = Color.white;
            activityReadout.alignment = TextAnchor.MiddleLeft;
            activityReadout.horizontalOverflow = HorizontalWrapMode.Wrap;
            activityReadout.verticalOverflow = VerticalWrapMode.Truncate;
            activityReadout.raycastTarget = false;
        }

        private void RefreshDevelopmentReadout()
        {
            if (activityReadout == null) return;
            activityReadout.text = $"{CurrentActivity}\n{CurrentReason}";
        }

        private static Transform FindPoint(string name)
        {
            GameObject point = GameObject.Find(name);
            return point == null ? null : point.transform;
        }
    }
}
