using System;
using UnityEngine;
using UnityEngine.UI;

namespace CatMe.Garden
{
    /// <summary>Confirmation dialog: dimmed scrim, one card, the safe choice on the left and the consequence on the right.</summary>
    internal sealed class ConfirmDialog
    {
        readonly GameObject root; readonly RectTransform card; Text title, body; Button safe, act, cancel; Action onSafe, onAct;
        HoldToConfirm hold;

        public ConfirmDialog(Transform canvasRoot)
        {
            var screen = UiKit.Screen(canvasRoot, "Confirm dialog", new Color(.12f, .1f, .09f, .55f)); root = screen.root;
            card = (RectTransform)CatMeUIStyle.Card(screen.safe, "Dialog card", new Vector2(760f, 420f), CatMeUIStyle.Paper, CatMeUIStyle.Elevation.Floating).transform;
            title = UiKit.Label(card, "", 46, CatMeUIStyle.Ink, new Vector2(0f, 120f), new Vector2(680f, 70f), FontStyle.Bold, TextAnchor.MiddleLeft, "Title");
            body = UiKit.Label(card, "", 30, CatMeUIStyle.BodyInk, new Vector2(0f, 20f), new Vector2(680f, 120f), FontStyle.Normal, TextAnchor.UpperLeft, "Body");
            safe = UiKit.Button(card, "Keep it", new Vector2(-170f, -140f), new Vector2(320f, 90f), CatMeUIStyle.Tile, false, () => { Hide(); onSafe?.Invoke(); }, 30, "Safe choice");
            safe.GetComponentInChildren<Text>().color = CatMeUIStyle.SageDeep;
            act = UiKit.Button(card, "Delete", new Vector2(170f, -140f), new Vector2(320f, 90f), CatMeUIStyle.Danger, true, () => { if (hold != null && hold.enabled) return; Hide(); onAct?.Invoke(); }, 30, "Consequence");
            hold = act.gameObject.AddComponent<HoldToConfirm>(); hold.enabled = false;
            hold.Completed = () => { Hide(); onAct?.Invoke(); };
            cancel = UiKit.Link(card, "Cancel", new Vector2(0f, -218f), new Vector2(300f, 64f), Hide, 28, TextAnchor.MiddleCenter);
            cancel.gameObject.SetActive(false);
            root.SetActive(false);
        }

        /// <summary>
        /// Three choices (main, consequence, Cancel), for example "Save with Google" · "Sign out anyway" · "Cancel".
        /// With <paramref name="holdSeconds"/> the consequence must be pressed and held (account deletion).
        /// </summary>
        public void ShowWithCancel(string heading, string message, string mainLabel, string actLabel, Action mainAction, Action action, bool destructive = true, float holdSeconds = 0f)
        {
            Show(heading, message, mainLabel, actLabel, mainAction, action, destructive, holdSeconds);
            card.sizeDelta = new Vector2(760f, 500f);
            cancel.gameObject.SetActive(true);
        }

        public bool IsOpen => root.activeSelf;

        /// <param name="destructive">Red consequence button for deleting things; pine for ordinary confirmations.</param>
        public void Show(string heading, string message, string safeLabel, string actLabel, Action safeAction, Action action, bool destructive = true, float holdSeconds = 0f)
        {
            title.text = heading; body.text = message; onSafe = safeAction; onAct = action;
            card.sizeDelta = new Vector2(760f, 420f); cancel.gameObject.SetActive(false);
            hold.Seconds = holdSeconds; hold.enabled = holdSeconds > 0f;
            safe.GetComponentInChildren<Text>().text = safeLabel; act.GetComponentInChildren<Text>().text = actLabel;
            CatMeUIStyle.StyleButton(act, destructive ? CatMeUIStyle.Danger : CatMeUIStyle.SageDeep, true);
            root.SetActive(true); root.transform.SetAsLastSibling();
        }

        public void Hide() => root.SetActive(false);
    }

    /// <summary>Short notice that drops in from the top and leaves by itself ("Mochi is ready").</summary>
    internal sealed class Toast : MonoBehaviour
    {
        RectTransform card; Text label; Button action; Action onAction; float until; bool visible;
        const float Hidden = 170f, Shown = -70f;

        public static Toast Create(Transform canvasRoot)
        {
            var host = new GameObject("Toast host", typeof(RectTransform)); host.transform.SetParent(canvasRoot, false);
            var r = (RectTransform)host.transform; r.anchorMin = Vector2.zero; r.anchorMax = Vector2.one; r.offsetMin = r.offsetMax = Vector2.zero;
            var toast = host.AddComponent<Toast>(); toast.Build(); return toast;
        }

        void Build()
        {
            card = (RectTransform)CatMeUIStyle.Card(transform, "Toast card", new Vector2(860f, 130f), CatMeUIStyle.PaperGlass, CatMeUIStyle.Elevation.Floating).transform;
            card.anchorMin = card.anchorMax = new Vector2(.5f, 1f); card.anchoredPosition = new Vector2(0f, Hidden);
            label = UiKit.Label(card, "", 28, CatMeUIStyle.Pine, new Vector2(-130f, 0f), new Vector2(520f, 110f), FontStyle.Bold, TextAnchor.MiddleLeft, "Message");
            action = UiKit.Button(card, "Open", new Vector2(285f, 0f), new Vector2(240f, 72f), CatMeUIStyle.SageDeep, true, () => { Dismiss(); onAction?.Invoke(); }, 28, "Toast action");
            gameObject.SetActive(false);
        }

        public void Show(string message, string actionLabel, Action onTap, float seconds = 6f)
        {
            label.text = message; onAction = onTap;
            bool hasAction = !string.IsNullOrEmpty(actionLabel);   // without a button the message gets the whole card
            label.rectTransform.sizeDelta = new Vector2(hasAction ? 520f : 780f, 110f);
            label.rectTransform.anchoredPosition = new Vector2(hasAction ? -130f : 0f, 0f);
            action.gameObject.SetActive(!string.IsNullOrEmpty(actionLabel)); if (!string.IsNullOrEmpty(actionLabel)) action.GetComponentInChildren<Text>().text = actionLabel;
            gameObject.SetActive(true); transform.SetAsLastSibling(); visible = true; until = Time.unscaledTime + seconds;
            CatMe.World.Haptics.Soft();
        }

        public void Dismiss() { visible = false; until = 0f; }
        /// <summary>Places the card in its shown position at once (screenshots and reduced motion).</summary>
        public void SnapShown() { var p = card.anchoredPosition; p.y = Shown; card.anchoredPosition = p; }

        void Update()
        {
            if (visible && Time.unscaledTime > until) visible = false;
            bool reduce = PlayerPrefs.GetInt("CatMe.ReduceMotion", 0) == 1;
            float target = visible ? Shown : Hidden;
            var p = card.anchoredPosition; p.y = reduce ? target : Mathf.Lerp(p.y, target, 1f - Mathf.Exp(-9f * Time.unscaledDeltaTime)); card.anchoredPosition = p;
            if (!visible && Mathf.Abs(p.y - Hidden) < 2f) gameObject.SetActive(false);
        }
    }

    /// <summary>Full-screen states: loading, offline and "could not make your pet". Plain words say what happened and what to do.</summary>
    internal sealed class ScreenState
    {
        readonly GameObject root; readonly RectTransform safe;
        public ScreenState(Transform canvasRoot)
        {
            var s = UiKit.Screen(canvasRoot, "Screen state", CatMeUIStyle.Mist); root = s.root; safe = s.safe; root.SetActive(false);
        }

        void Begin(string heading, string message)
        {
            foreach (Transform c in safe) { c.gameObject.SetActive(false); UnityEngine.Object.Destroy(c.gameObject); }
            UiKit.Label(safe, heading, 72, CatMeUIStyle.Pine, new Vector2(0f, 120f), new Vector2(1100f, 130f), FontStyle.Bold, TextAnchor.MiddleCenter, "Heading");
            UiKit.Label(safe, message, 34, CatMeUIStyle.BodyInk, new Vector2(0f, 0f), new Vector2(980f, 140f), FontStyle.Normal, TextAnchor.UpperCenter, "Message");
            root.SetActive(true); root.transform.SetAsLastSibling();
        }

        public void ShowLoading(string tip)
        {
            Begin("Getting the garden ready", tip);
            UiKit.Pic(safe, Art.Load("PawGlowBright"), new Vector2(0f, 330f), new Vector2(240f, 240f), true, "Glow");
        }

        public void ShowOffline(Action retry, Action continueOffline)
        {
            Begin("No internet", "Check your connection and try again.");
            UiKit.NestedButton(safe, "Try again", new Vector2(0f, -150f), 460f, CatMeUIStyle.SageDeep, () => { Hide(); retry?.Invoke(); }, 34, "Retry");
            UiKit.Link(safe, "Continue offline", new Vector2(0f, -270f), new Vector2(360f, 70f), () => { Hide(); continueOffline?.Invoke(); }, 28, TextAnchor.MiddleCenter);
        }

        public void ShowFailed(string petName, Action retry, Action choosePhoto)
        {
            Begin("We couldn't make " + petName + " this time", "That was our side, not your photo. Nothing has been used from your free pets.");
            UiKit.NestedButton(safe, "Try again", new Vector2(0f, -150f), 460f, CatMeUIStyle.SageDeep, () => { Hide(); retry?.Invoke(); }, 34, "Retry");
            UiKit.Link(safe, "Choose another photo", new Vector2(0f, -270f), new Vector2(420f, 70f), () => { Hide(); choosePhoto?.Invoke(); }, 28, TextAnchor.MiddleCenter);
        }

        public void Hide() => root.SetActive(false);
    }

    /// <summary>Press and hold a button to confirm (account deletion): a fill grows across it and the action runs when it is full.</summary>
    internal sealed class HoldToConfirm : MonoBehaviour, UnityEngine.EventSystems.IPointerDownHandler, UnityEngine.EventSystems.IPointerUpHandler, UnityEngine.EventSystems.IPointerExitHandler
    {
        public float Seconds = 3f;
        public Action Completed;
        Image fill; float heldFor; bool holding;

        void Awake()
        {
            var go = new GameObject("Hold fill", typeof(RectTransform), typeof(Image)); go.transform.SetParent(transform, false); go.transform.SetAsFirstSibling();
            var r = (RectTransform)go.transform; r.anchorMin = Vector2.zero; r.anchorMax = Vector2.one; r.offsetMin = r.offsetMax = Vector2.zero;
            fill = go.GetComponent<Image>(); fill.color = new Color(1f, 1f, 1f, .28f); fill.raycastTarget = false;
            fill.type = Image.Type.Filled; fill.fillMethod = Image.FillMethod.Horizontal; fill.fillAmount = 0f;
        }

        public void OnPointerDown(UnityEngine.EventSystems.PointerEventData e) { if (enabled) { holding = true; heldFor = 0f; } }
        public void OnPointerUp(UnityEngine.EventSystems.PointerEventData e) => Release();
        public void OnPointerExit(UnityEngine.EventSystems.PointerEventData e) => Release();
        void Release() { holding = false; heldFor = 0f; if (fill != null) fill.fillAmount = 0f; }
        void OnDisable() => Release();

        void Update()
        {
            if (!holding) return;
            heldFor += Time.unscaledDeltaTime;   // dialogs are shown while the game is paused
            fill.fillAmount = Mathf.Clamp01(heldFor / Mathf.Max(.1f, Seconds));
            if (heldFor >= Seconds) { Release(); CatMe.World.Haptics.Tap(); Completed?.Invoke(); }
        }
    }
}
