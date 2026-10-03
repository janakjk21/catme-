using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace CatMe.Garden
{
    /// <summary>Shared press feedback: the button sinks to 95 %, its shadow tightens, and a faint darken sells the press. Honors reduced motion.</summary>
    [DisallowMultipleComponent]
    public sealed class CatMeButtonMotion : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IPointerExitHandler, ISelectHandler, IDeselectHandler
    {
        private RectTransform target; private ShadowFollow shadow;
        private Vector3 restingScale = Vector3.one;
        private Coroutine transition;
        private Image surface; private Color restingTint = Color.white;

        internal void Initialize(RectTransform rect)
        {
            target = rect;
            if (target != null) restingScale = target.localScale;
        }
        internal void Tint(Image image, Color tint) { surface = image; restingTint = tint; }
        internal void RememberShadow(ShadowFollow follow) { shadow = follow; }

        private bool Reduced => PlayerPrefs.GetInt("CatMe.ReduceMotion", 0) == 1;
        public void OnPointerDown(PointerEventData eventData) => Animate(.95f, 1f, 0.08f);
        public void OnPointerUp(PointerEventData eventData) => Animate(1f, 0f, 0.18f);
        public void OnPointerExit(PointerEventData eventData) => Animate(1f, 0f, 0.18f);
        public void OnSelect(BaseEventData eventData) => Animate(1.015f, 0f, 0.12f);
        public void OnDeselect(BaseEventData eventData) => Animate(1f, 0f, 0.16f);

        private void Animate(float scale, float press, float duration)
        {
            if (target == null) return;
            if (transition != null) StopCoroutine(transition);
            transition = StartCoroutine(Run(restingScale * (Reduced ? 1f : scale), press, Reduced ? .01f : duration));
        }

        private IEnumerator Run(Vector3 destination, float press, float duration)
        {
            Vector3 start = target.localScale; float startPress = surface != null ? (restingTint.r - surface.color.r) * 0f : 0f;
            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(elapsed / duration); t = 1f - Mathf.Pow(1f - t, 3f);
                target.localScale = Vector3.LerpUnclamped(start, destination, t);
                Apply(Mathf.Lerp(startPress, press, t));
                yield return null;
            }
            target.localScale = destination; Apply(press);
            transition = null;
        }

        // press 0..1: shadow pulls in toward the button and softens, surface darkens slightly.
        private void Apply(float press)
        {
            if (shadow != null) shadow.alphaScale = 1f - .4f * press;   // the shadow softens while pressed
            if (surface != null) surface.color = Color.Lerp(restingTint, restingTint * new Color(.86f, .86f, .84f, 1f), press);
        }

        private void OnDisable() { if (target != null) target.localScale = restingScale; Apply(0f); }
    }
}
