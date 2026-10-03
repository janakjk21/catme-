using CatMe.Pets;
using UnityEngine;
using UnityEngine.UI;

namespace CatMe.Garden
{
    /// <summary>A small speech bubble that follows the companion on screen, fades in and out, and never says two things at once.</summary>
    public sealed class CompanionVoice : MonoBehaviour
    {
        GardenCompanionDemo garden; RectTransform canvas, bubble; Text text; CanvasGroup group;
        float shownAt = -99f, until, cooldownUntil;
        const float FadeSeconds = .25f;

        public void Initialize(GardenCompanionDemo demo, Transform canvasRoot)
        {
            garden = demo; canvas = canvasRoot as RectTransform;
            var card = CatMeUIStyle.Card(canvasRoot, "Companion speech", new Vector2(420f, 78f), CatMeUIStyle.CreamGlass, CatMeUIStyle.Elevation.Raised);
            bubble = (RectTransform)card.transform;
            bubble.anchorMin = bubble.anchorMax = Vector2.zero; bubble.pivot = new Vector2(.5f, 0f);
            card.GetComponent<Image>().raycastTarget = false;
            text = UiKit.Label(bubble, "", 30, CatMeUIStyle.Ink, Vector2.zero, new Vector2(390f, 70f), FontStyle.Bold);
            group = card.AddComponent<CanvasGroup>(); group.alpha = 0f; group.blocksRaycasts = false; group.interactable = false;
            card.SetActive(false);
        }

        public bool Busy => Time.unscaledTime < until || Time.unscaledTime < cooldownUntil;

        /// <summary>Says a line for the moment if nothing else was said recently. Returns whether it was shown.</summary>
        public bool Say(Moment moment, float seconds = 3.2f, bool force = false)
        {
            if (garden == null || garden.Behavior == null || bubble == null) return false;
            if (!force && Busy) return false;
            string line = garden.Behavior.Line(moment);
            if (string.IsNullOrEmpty(line)) return false;
            text.text = line; shownAt = Time.unscaledTime; until = shownAt + seconds; cooldownUntil = until + 5f;
            bubble.gameObject.SetActive(true);
            garden.Behavior.Sound(garden.CompanionAudio, moment == Moment.Petted ? "purr" : "call", .5f);
            CatMe.World.Haptics.Soft();
            return true;
        }

        void LateUpdate()
        {
            if (bubble == null || !bubble.gameObject.activeSelf) return;
            float t = Time.unscaledTime;
            if (t > until + FadeSeconds || garden == null || garden.CatTransform == null) { bubble.gameObject.SetActive(false); return; }
            bool reduce = PlayerPrefs.GetInt("CatMe.ReduceMotion", 0) == 1;
            float fade = Mathf.Clamp01(Mathf.Min((t - shownAt) / FadeSeconds, (until + FadeSeconds - t) / FadeSeconds));
            group.alpha = reduce ? (t < until ? 1f : 0f) : fade;
            var cam = Camera.main; if (cam == null || canvas == null) return;
            Vector3 screen = cam.WorldToScreenPoint(garden.CatTransform.position + Vector3.up * .75f);
            bool visible = screen.z > 0f; group.alpha *= visible ? 1f : 0f;
            float scale = canvas.localScale.x > 0f ? canvas.localScale.x : 1f;
            float rise = reduce ? 0f : Mathf.Min(1f, (t - shownAt) / .4f) * 14f;
            bubble.position = new Vector3(Mathf.Clamp(screen.x, 230f * scale, Screen.width - 230f * scale), Mathf.Clamp(screen.y + rise, 40f, Screen.height - 120f * scale), 0f);
        }
    }
}
