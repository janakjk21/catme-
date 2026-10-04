using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.UI;

namespace CatMe.Garden
{
    /// <summary>Generated onboarding and icon art in Resources/Onboarding/Generated, cached as sprites.</summary>
    internal static class Art
    {
        static readonly Dictionary<string, Sprite> cache = new Dictionary<string, Sprite>();
        internal static Sprite Load(string name)
        {
            if (cache.TryGetValue(name, out var s) && s != null) return s;
            var tex = Resources.Load<Texture2D>("Onboarding/Generated/" + name);
            if (tex == null) { cache[name] = null; return null; }
            s = Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), new Vector2(.5f, .5f), 100f, 0, SpriteMeshType.FullRect);
            s.name = name; cache[name] = s; return s;
        }

        /// <summary>Loads a photo from disk as a sprite (caller owns it). Returns null if it cannot be read.</summary>
        internal static Sprite FromFile(string path)
        {
            try
            {
                if (string.IsNullOrEmpty(path) || !File.Exists(path)) return null;
                var tex = new Texture2D(2, 2, TextureFormat.RGBA32, false);
                if (!tex.LoadImage(File.ReadAllBytes(path))) { UnityEngine.Object.Destroy(tex); return null; }
                return Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), new Vector2(.5f, .5f), 100f, 0, SpriteMeshType.FullRect);
            }
            catch { return null; }
        }
    }

    /// <summary>Small layout helpers for the new full-screen and panel screens. All sizes are in the 1920x1080 reference space.</summary>
    internal static class UiKit
    {
        static Sprite circle;
        internal static Sprite Circle
        {
            get
            {
                if (circle != null) return circle;
                const int n = 128; var t = new Texture2D(n, n, TextureFormat.RGBA32, false) { filterMode = FilterMode.Bilinear, hideFlags = HideFlags.HideAndDontSave };
                var px = new Color32[n * n];
                for (int y = 0; y < n; y++) for (int x = 0; x < n; x++)
                {
                    float d = Vector2.Distance(new Vector2(x + .5f, y + .5f), new Vector2(n / 2f, n / 2f)) - (n / 2f - 1.2f);
                    px[y * n + x] = new Color(1, 1, 1, 1f - CatMeUIStyle.Edge(-.8f, .8f, d));
                }
                t.SetPixels32(px); t.Apply(false, true);
                circle = Sprite.Create(t, new Rect(0, 0, n, n), new Vector2(.5f, .5f), 100f); circle.hideFlags = HideFlags.HideAndDontSave;
                return circle;
            }
        }

        internal static RectTransform Rect(Transform parent, string name, Vector2 pos, Vector2 size, Vector2? anchor = null)
        {
            var go = new GameObject(name, typeof(RectTransform)); go.transform.SetParent(parent, false);
            var r = (RectTransform)go.transform; var a = anchor ?? new Vector2(.5f, .5f);
            r.anchorMin = r.anchorMax = a; r.pivot = new Vector2(.5f, .5f); r.anchoredPosition = pos; r.sizeDelta = size;
            return r;
        }

        /// <summary>Full-screen container (dark scrim or solid), with a child sized to the phone's safe area for content.</summary>
        internal static (GameObject root, RectTransform safe) Screen(Transform canvasRoot, string name, Color color)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Image)); go.transform.SetParent(canvasRoot, false);
            var r = (RectTransform)go.transform; r.anchorMin = Vector2.zero; r.anchorMax = Vector2.one; r.offsetMin = r.offsetMax = Vector2.zero;
            go.GetComponent<Image>().color = color;
            var safe = new GameObject("Safe area", typeof(RectTransform)); safe.transform.SetParent(go.transform, false);
            var sr = (RectTransform)safe.transform; sr.anchorMin = Vector2.zero; sr.anchorMax = Vector2.one; sr.offsetMin = sr.offsetMax = Vector2.zero;
            var area = UnityEngine.Screen.safeArea;
            if (UnityEngine.Screen.width > 0 && UnityEngine.Screen.height > 0)
            {
                sr.anchorMin = new Vector2(area.xMin / UnityEngine.Screen.width, area.yMin / UnityEngine.Screen.height);
                sr.anchorMax = new Vector2(area.xMax / UnityEngine.Screen.width, area.yMax / UnityEngine.Screen.height);
            }
            return (go, sr);
        }

        internal static Text Label(Transform parent, string text, int size, Color color, Vector2 pos, Vector2 dims, FontStyle style = FontStyle.Normal, TextAnchor align = TextAnchor.MiddleCenter, string name = null)
        {
            var go = new GameObject(name ?? "Label", typeof(RectTransform), typeof(Text)); go.transform.SetParent(parent, false);
            var r = (RectTransform)go.transform; r.anchorMin = r.anchorMax = new Vector2(.5f, .5f); r.pivot = new Vector2(.5f, .5f); r.anchoredPosition = pos; r.sizeDelta = dims;
            var t = go.GetComponent<Text>(); t.font = CatMeUIStyle.FontFor(style, size); t.text = text; t.fontSize = size; t.fontStyle = CatMeUIStyle.Weight(style); t.color = color; t.alignment = align;
            t.horizontalOverflow = HorizontalWrapMode.Wrap; t.verticalOverflow = VerticalWrapMode.Truncate; t.raycastTarget = false;
            return t;
        }

        internal static Button Button(Transform parent, string label, Vector2 pos, Vector2 dims, Color tint, bool primary, Action action, int fontSize = 26, string name = null)
        {
            var go = new GameObject(name ?? label, typeof(RectTransform), typeof(Image), typeof(Button)); go.transform.SetParent(parent, false);
            var r = (RectTransform)go.transform; r.anchorMin = r.anchorMax = new Vector2(.5f, .5f); r.pivot = new Vector2(.5f, .5f); r.anchoredPosition = pos; r.sizeDelta = dims;
            var b = go.GetComponent<Button>(); b.onClick.AddListener(() => action());
            var t = Label(go.transform, label, fontSize, primary ? Color.white : CatMeUIStyle.Ink, Vector2.zero, dims - new Vector2(28, 10), FontStyle.Bold);
            CatMeUIStyle.StyleButton(b, tint, primary);
            return b;
        }

        internal static void SetLabel(Button b, string text) { var t = b.GetComponentInChildren<Text>(); if (t != null) t.text = text; }

        internal static Image Pic(Transform parent, Sprite sprite, Vector2 pos, Vector2 size, bool preserve = true, string name = "Art")
        {
            var r = Rect(parent, name, pos, size);
            var img = r.gameObject.AddComponent<Image>(); img.sprite = sprite; img.preserveAspect = preserve; img.raycastTarget = false; img.color = sprite == null ? new Color(1, 1, 1, 0) : Color.white;
            return img;
        }

        /// <summary>Picture that fills a rounded frame like a cover photo (cropped to fit), clipped by the frame.</summary>
        internal static RectTransform CoverFrame(Transform parent, Sprite sprite, Vector2 pos, Vector2 size, float corner = 1f, string name = "Cover", float focusY = .5f, float focusX = .5f)
        {
            var frame = Rect(parent, name, pos, size);
            var fi = frame.gameObject.AddComponent<Image>(); CatMeUIStyle.ApplySurface(fi, Color.white); fi.raycastTarget = false;
            frame.gameObject.AddComponent<Mask>().showMaskGraphic = false;
            if (sprite != null) FillCover(frame, sprite, focusY, focusX);
            return frame;
        }

        internal static void FillCover(RectTransform frame, Sprite sprite, float focusY = .5f, float focusX = .5f)
        {
            var old = frame.Find("Picture"); if (old != null) UnityEngine.Object.Destroy(old.gameObject);
            if (sprite == null) return;
            var pic = Rect(frame, "Picture", Vector2.zero, frame.sizeDelta);
            var img = pic.gameObject.AddComponent<Image>(); img.sprite = sprite; img.raycastTarget = false;
            float sw = sprite.rect.width, sh = sprite.rect.height;
            float scale = Mathf.Max(frame.sizeDelta.x / sw, frame.sizeDelta.y / sh);
            pic.sizeDelta = new Vector2(sw * scale, sh * scale);
            pic.anchoredPosition = new Vector2((pic.sizeDelta.x - frame.sizeDelta.x) * .5f * (1f - 2f * focusX), -(pic.sizeDelta.y - frame.sizeDelta.y) * .5f * (1f - 2f * focusY));   // 0 keeps the top (faces), 1 keeps the bottom
        }

        /// <summary>Round picture (pet or owner photo), clipped to a circle with a soft ring.</summary>
        internal static RectTransform RoundPhoto(Transform parent, Sprite sprite, Vector2 pos, float diameter, Color ring, string name = "Photo")
        {
            var outer = Rect(parent, name, pos, new Vector2(diameter + 24f, diameter + 24f));
            var ringImg = outer.gameObject.AddComponent<Image>(); ringImg.sprite = Circle; ringImg.color = ring; ringImg.raycastTarget = false;
            var inner = Rect(outer, "Mask", Vector2.zero, new Vector2(diameter, diameter));
            var mi = inner.gameObject.AddComponent<Image>(); mi.sprite = Circle; mi.color = Color.white; mi.raycastTarget = false;
            inner.gameObject.AddComponent<Mask>().showMaskGraphic = true;
            if (sprite != null) FillCover(inner, sprite);
            return outer;
        }

        internal static void SetRoundPhoto(RectTransform outer, Sprite sprite) { FillCover(outer.Find("Mask") as RectTransform, sprite); }

        internal static GameObject Chip(Transform parent, string text, Color bg, Color fg, Vector2 pos, Vector2 dims, int size = 20)
        {
            var go = CatMeUIStyle.Card(parent, "Chip " + text, dims, bg, CatMeUIStyle.Elevation.Resting);
            ((RectTransform)go.transform).anchoredPosition = pos;
            Label(go.transform, text, size, fg, Vector2.zero, dims - new Vector2(16, 6), FontStyle.Bold);
            return go;
        }

        /// <summary>Horizontal scroller whose content grows with its children. Returns the content rect.</summary>
        internal static RectTransform HScroll(Transform parent, Vector2 pos, Vector2 size, float spacing, int pad)
        {
            var view = Rect(parent, "Scroll", pos, size);
            var sr = view.gameObject.AddComponent<ScrollRect>(); sr.horizontal = true; sr.vertical = false; sr.movementType = ScrollRect.MovementType.Elastic; sr.scrollSensitivity = 30f;
            var viewport = Rect(view, "Viewport", Vector2.zero, size);
            viewport.anchorMin = Vector2.zero; viewport.anchorMax = Vector2.one; viewport.offsetMin = viewport.offsetMax = Vector2.zero;
            viewport.gameObject.AddComponent<RectMask2D>();
            var vi = viewport.gameObject.AddComponent<Image>(); vi.color = new Color(1, 1, 1, 0);
            var content = Rect(viewport, "Content", Vector2.zero, new Vector2(size.x, size.y));
            content.anchorMin = new Vector2(0, 0); content.anchorMax = new Vector2(0, 1); content.pivot = new Vector2(0, .5f); content.anchoredPosition = Vector2.zero; content.sizeDelta = new Vector2(size.x, 0);
            var h = content.gameObject.AddComponent<HorizontalLayoutGroup>(); h.spacing = spacing; h.padding = new RectOffset(pad, pad, 30, 40);
            h.childAlignment = TextAnchor.MiddleLeft; h.childControlWidth = false; h.childControlHeight = false; h.childForceExpandWidth = false; h.childForceExpandHeight = false;
            var fit = content.gameObject.AddComponent<ContentSizeFitter>(); fit.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
            sr.viewport = viewport; sr.content = content;
            return content;
        }

        /// <summary>A card of fixed size to place inside a layout group.</summary>
        internal static GameObject LayoutCard(Transform content, string name, Vector2 size, Color color)
        {
            var go = CatMeUIStyle.Card(content, name, size, color);
            var le = go.AddComponent<LayoutElement>(); le.minWidth = size.x; le.minHeight = size.y; le.preferredWidth = size.x; le.preferredHeight = size.y;
            return go;
        }

        internal static void Dots(Transform parent, int count, int active, Vector2 pos)
        {
            var row = Rect(parent, "Progress", pos, new Vector2(count * 34f, 24f));
            for (int i = 0; i < count; i++)
            {
                var d = Rect(row, "Dot", new Vector2((i - (count - 1) / 2f) * 30f, 0), new Vector2(i == active ? 22f : 12f, 12f));
                var img = d.gameObject.AddComponent<Image>(); img.sprite = Circle; img.color = i == active ? CatMeUIStyle.Olive : CatMeUIStyle.Hairline; img.raycastTarget = false;
            }
        }

        /// <summary>Pill action with the label on the left and a small arrow disc on the right (the "island" button from the onboarding design).</summary>
        internal static Button NestedButton(Transform parent, string label, Vector2 pos, float width, Color tint, Action action, int fontSize = 34, string name = null, bool arrow = true)
        {
            const float h = 104f;
            var b = Button(parent, label, pos, new Vector2(width, h), tint, true, action, fontSize, name ?? label);
            var t = b.GetComponentInChildren<Text>(); var tr = t.rectTransform;
            float textW = width - (arrow ? 160f : 60f);
            tr.sizeDelta = new Vector2(textW, h); tr.anchoredPosition = new Vector2(-width / 2f + (arrow ? 54f : 30f) + textW / 2f, 0f);
            t.alignment = arrow ? TextAnchor.MiddleLeft : TextAnchor.MiddleCenter;
            if (!arrow) { tr.anchoredPosition = Vector2.zero; tr.sizeDelta = new Vector2(width - 40f, h); return b; }
            var disc = Rect(b.transform, "Arrow disc", new Vector2(width / 2f - 58f, 0f), new Vector2(72f, 72f));
            var di = disc.gameObject.AddComponent<Image>(); di.sprite = Circle; di.color = new Color(.55f, .5f, .45f, .5f); di.raycastTarget = false;
            Icon(disc, "arrow", Vector2.zero, 40f, Color.white);
            return b;
        }

        /// <summary>Quiet text action (Skip, Sign in). Same tick sound as the buttons.</summary>
        internal static Button Link(Transform parent, string text, Vector2 pos, Vector2 dims, Action action, int fontSize = 28, TextAnchor align = TextAnchor.MiddleLeft)
        {
            var go = new GameObject("Link " + text, typeof(RectTransform), typeof(Image), typeof(Button)); go.transform.SetParent(parent, false);
            var r = (RectTransform)go.transform; r.anchorMin = r.anchorMax = new Vector2(.5f, .5f); r.pivot = new Vector2(.5f, .5f); r.anchoredPosition = pos; r.sizeDelta = dims;
            go.GetComponent<Image>().color = new Color(0, 0, 0, 0);
            var b = go.GetComponent<Button>(); b.onClick.AddListener(() => { CatMe.World.UiSounds.Click(); CatMe.World.Haptics.Soft(); action(); });
            Label(go.transform, text, fontSize, CatMeUIStyle.SageDeep, Vector2.zero, dims, FontStyle.Bold, align);
            return b;
        }

        /// <summary>Round icon button (back, close) on a translucent paper disc.</summary>
        internal static Button IconButton(Transform parent, string glyph, Vector2 pos, Action action, string name)
        {
            var icon = string.IsNullOrEmpty(glyph) ? null : UiIcons.Get(glyph);
            var b = Button(parent, icon != null ? "" : glyph, pos, new Vector2(88f, 88f), CatMeUIStyle.Paper, false, action, 44, name);
            var img = b.GetComponent<Image>(); img.sprite = Circle; img.type = Image.Type.Simple; img.color = new Color(.98f, .99f, .98f, .88f);
            b.GetComponentInChildren<Text>().color = CatMeUIStyle.SageDeep;
            if (icon != null) Icon(b.transform, glyph, Vector2.zero, 46f, CatMeUIStyle.SageDeep);
            return b;
        }

        /// <summary>A tinted line icon from UiIcons (falls back to nothing if the name is unknown).</summary>
        internal static Image Icon(Transform parent, string icon, Vector2 pos, float size, Color color)
        {
            var r = Rect(parent, "Icon " + icon, pos, new Vector2(size, size));
            var img = r.gameObject.AddComponent<Image>(); img.sprite = UiIcons.Get(icon); img.color = img.sprite == null ? new Color(0, 0, 0, 0) : color; img.raycastTarget = false; img.preserveAspect = true;
            return img;
        }

        /// <summary>Square tile with an icon over a short label (pause menu, shortcuts).</summary>
        internal static Button IconTile(Transform parent, string icon, string label, Vector2 pos, Vector2 dims, Action action, string name = null)
        {
            var b = Button(parent, label, pos, dims, CatMeUIStyle.Tile, false, action, 30, name ?? label);
            var t = b.GetComponentInChildren<Text>(); t.color = CatMeUIStyle.Pine; t.alignment = TextAnchor.LowerCenter;
            t.rectTransform.sizeDelta = new Vector2(dims.x - 12f, 56f); t.rectTransform.anchoredPosition = new Vector2(0f, -dims.y / 2f + 40f);
            Icon(b.transform, icon, new Vector2(0f, dims.y / 2f - 62f), 64f, CatMeUIStyle.Pine);
            return b;
        }

        /// <summary>Progress rail: one segment size everywhere; done in sage, the current one in deep sage, the rest faint.</summary>
        internal static void Rail(Transform parent, int count, int active, Vector2 pos)
        {
            var row = Rect(parent, "Progress", pos, new Vector2(count * 90f, 20f));
            float x = 0f, total = count * 50f - 10f;
            x = -total / 2f;
            for (int i = 0; i < count; i++)
            {
                float w = 40f;
                var seg = Rect(row, "Segment", new Vector2(x + w / 2f, 0f), new Vector2(w, 8f)); x += w + 10f;
                var img = seg.gameObject.AddComponent<Image>(); img.sprite = Circle; img.raycastTarget = false;
                img.color = i == active ? CatMeUIStyle.SageDeep : i < active ? CatMeUIStyle.Sage : new Color(.153f, .137f, .129f, .16f);
            }
        }

        /// <summary>Soft double frame: an outer tray with a paper card inside. Returns the inner area to place content in.</summary>
        internal static RectTransform Bezel(Transform parent, string name, Vector2 pos, Vector2 size)
        {
            var outer = CatMeUIStyle.Card(parent, name, size, CatMeUIStyle.Tile, CatMeUIStyle.Elevation.Resting);
            ((RectTransform)outer.transform).anchoredPosition = pos;
            var inner = CatMeUIStyle.Card(outer.transform, "Core", size - new Vector2(24f, 24f), CatMeUIStyle.Paper, CatMeUIStyle.Elevation.Raised);
            return (RectTransform)inner.transform;
        }
    }
}
