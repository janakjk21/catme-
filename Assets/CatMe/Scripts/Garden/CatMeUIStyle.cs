using UnityEngine;
using UnityEngine.UI;
using CatMe.World;

namespace CatMe.Garden
{
    /// <summary>
    /// Shared CatMe look: warm botanical palette, soft raised surfaces with a gentle bevel and blurred drop shadow, three elevation tiers,
    /// one rounded system font. Every button and card in the game goes through this class, so changing it restyles the whole app.
    /// </summary>
    internal static class CatMeUIStyle
    {
        internal static readonly Color Ink = new Color(39f/255, 35f/255, 33f/255, 1f);                 // Worn Ink #272321
        internal static readonly Color MutedInk = new Color(118f/255, 110f/255, 104f/255, 1f);        // Muted Ink #766E68
        internal static readonly Color CreamGlass = new Color(255f/255, 252f/255, 247f/255, 1f);      // Paper White #FFFCF7
        internal static readonly Color CreamSoft = new Color(234f/255, 227f/255, 215f/255, 1f);
        internal static readonly Color Olive = new Color(95f/255, 110f/255, 97f/255, 1f);           // = SageDeep (was near-black)
        internal static readonly Color OliveSoft = new Color(135f/255, 148f/255, 135f/255, 1f);       // Quiet Sage #879487
        internal static readonly Color Hairline = new Color(215f/255, 206f/255, 194f/255, 1f);        // Stone #D7CEC2
        // Onboarding palette (docs/production/07): sage mist ground, pine actions, sunset only for the current step and active choice.
        internal static readonly Color Mist = new Color(243f/255, 238f/255, 229f/255, 1f);           // Warm Bone #F3EEE5
        internal static readonly Color Pine = new Color(39f/255, 35f/255, 33f/255, 1f);              // Worn Ink #272321 (primary actions and text)
        internal static readonly Color PineSoft = new Color(79f/255, 72f/255, 67f/255, 1f);          // Body Ink #4F4843
        internal static readonly Color Paper = new Color(255f/255, 252f/255, 247f/255, 1f);
        internal static readonly Color PaperGlass = new Color(255f/255, 252f/255, 247f/255, .97f);
        internal static readonly Color Danger = new Color(179f/255, 69f/255, 44f/255, 1f);
        internal static readonly Color Tile = new Color(234f/255, 227f/255, 215f/255, 1f);
        internal static readonly Color Sunset = new Color(197f/255, 162f/255, 97f/255, 1f);           // Old Brass #C5A261 (current step, active choice)
        internal static readonly Color BodyInk = new Color(79f/255, 72f/255, 67f/255, 1f);
        internal static readonly Color Peach = new Color(232f/255, 186f/255, 156f/255, 1f);
        // Menus, account and settings (docs/MENUS_AND_ACCOUNT.md section 1): sage is the only accent; Danger only for destructive actions.
        internal static readonly Color Stone = new Color(215f/255, 206f/255, 194f/255, 1f);           // #D7CEC2 dividers, toggle off
        internal static readonly Color SageDeep = new Color(95f/255, 110f/255, 97f/255, 1f);          // #5F6E61 main buttons, active toggle
        internal static readonly Color Sage = new Color(135f/255, 148f/255, 135f/255, 1f);            // #879487 secondary icons, progress
        internal static readonly Color SageTint = new Color(228f/255, 233f/255, 225f/255, 1f);        // #E4E9E1 selected cards, chips

        /// <summary>0 = resting (flat), 1 = raised (buttons, cards), 2 = floating (sheets, popups).</summary>
        internal enum Elevation { Resting = 0, Raised = 1, Floating = 2 }

        private static Sprite roundedSurface, shadowSprite;
        private static Font uiFont;

        /// <summary>A rounded system sans: SF Rounded / Avenir Next on iPhone, falling back to the built-in font where none is installed.</summary>
        private static Font bodyFont, displayFont;
        /// <summary>Titles: Fraunces SemiBold, falling back to the body font.</summary>
        internal static Font DisplayFont { get { if (displayFont == null) displayFont = Resources.Load<Font>("Fonts/Fraunces-SemiBold"); return displayFont != null ? displayFont : UiFont; } }
        /// <summary>The bundled fonts are already semibold/bold, so asking Unity to embolden them again smudges the glyphs.</summary>
        internal static FontStyle Weight(FontStyle wanted) { var f = UiFont; return f != null && f == bodyFont ? FontStyle.Normal : wanted; }
        private static Font boldFont;
        /// <summary>DM Sans Bold for buttons and labels, so bold text is a real weight instead of an embolden pass.</summary>
        internal static Font BoldFont { get { if (boldFont == null) boldFont = Resources.Load<Font>("Fonts/DMSans-Bold"); return boldFont != null ? boldFont : UiFont; } }
        internal static Font FontFor(FontStyle style, int size) => style == FontStyle.Bold ? (size >= 44 ? DisplayFont : BoldFont) : UiFont;
        internal static Font UiFont
        {
            get
            {
                if (uiFont != null) return uiFont;
                if (bodyFont == null) bodyFont = Resources.Load<Font>("Fonts/DMSans-Regular");
                if (bodyFont != null) { uiFont = bodyFont; return uiFont; }
                try { uiFont = Font.CreateDynamicFontFromOSFont(new[] { "SF Pro Rounded", "Avenir Next Rounded", "Avenir Next", "Helvetica Neue", "Arial" }, 28); }
                catch { uiFont = null; }
                if (uiFont == null) uiFont = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
                return uiFont;
            }
        }

        /// <summary>
        /// Flat rounded surface from the UI kit: a pill when the control is under ~140 pt tall (radius = half the height), otherwise a 48 pt card.
        /// One white sprite is tinted per use; no bevel or baked gloss.
        /// </summary>
        internal static void ApplySurface(Image image, Color tint, bool sliced = true)
        {
            if (image == null) return;
            image.sprite = FlatSurface;
            image.type = sliced ? Image.Type.Sliced : Image.Type.Simple;
            image.color = tint;
            image.preserveAspect = false;
            float h = image.rectTransform.rect.height; if (h < 1f) h = image.rectTransform.sizeDelta.y;
            float radius = h > 1f && h <= 140f ? h * .5f : 48f;
            if (h > 1f) radius = Mathf.Min(radius, h * .5f);
            image.pixelsPerUnitMultiplier = FlatBorder / Mathf.Max(8f, radius);
        }

        private const float FlatBorder = 104f;
        private static Sprite flatSurface;
        private static Sprite FlatSurface
        {
            get
            {
                if (flatSurface != null) return flatSurface;
                const int size = 256; const float radius = 100f, half = size * .5f;
                var texture = new Texture2D(size, size, TextureFormat.RGBA32, false) { name = "CatMe flat surface", filterMode = FilterMode.Bilinear, wrapMode = TextureWrapMode.Clamp, hideFlags = HideFlags.HideAndDontSave };
                var pixels = new Color32[size * size];
                for (int y = 0; y < size; y++)
                for (int x = 0; x < size; x++)
                {
                    float px = Mathf.Abs(x + .5f - half) - (half - radius);
                    float py = Mathf.Abs(y + .5f - half) - (half - radius);
                    float outside = Mathf.Sqrt(Mathf.Max(px, 0f) * Mathf.Max(px, 0f) + Mathf.Max(py, 0f) * Mathf.Max(py, 0f));
                    float distance = outside + Mathf.Min(Mathf.Max(px, py), 0f) - radius;
                    pixels[y * size + x] = new Color(1f, 1f, 1f, 1f - Edge(-1f, 1f, distance));
                }
                texture.SetPixels32(pixels); texture.Apply(false, true);
                flatSurface = Sprite.Create(texture, new Rect(0, 0, size, size), new Vector2(.5f, .5f), 100f, 0, SpriteMeshType.FullRect, new Vector4(FlatBorder, FlatBorder, FlatBorder, FlatBorder));
                flatSurface.name = "CatMe flat 9-slice"; flatSurface.hideFlags = HideFlags.HideAndDontSave;
                return flatSurface;
            }
        }

        /// <summary>
        /// Soft shadow behind a surface. The shadow is a sibling placed just before the surface (a child would draw on top of the fill) and
        /// follows its position, size and scale. Idempotent: re-applying changes the tier.
        /// </summary>
        internal static void SetElevation(RectTransform surface, Elevation tier)
        {
            if (surface == null || surface.parent == null) return;
            var follow = surface.GetComponent<ShadowFollow>();
            if (tier == Elevation.Resting) { if (follow != null) follow.SetVisible(false); return; }
            bool floating = tier == Elevation.Floating;
            float h = surface.rect.height; if (h < 1f) h = surface.sizeDelta.y;
            float r = h > 1f && h <= 140f ? h * .5f : 48f; if (h > 1f) r = Mathf.Min(r, h * .5f);
            float s = Mathf.Max(8f, r) / 100f;                 // the shadow texture is built for a 100 px corner and a 40 px blur
            if (follow == null) follow = surface.gameObject.AddComponent<ShadowFollow>();
            follow.Configure(ShadowSprite, 100f / Mathf.Max(8f, r), 40f * s, (floating ? 14f : 6f) * Mathf.Clamp(s * 2f, .6f, 1.2f), new Color(.153f, .137f, .129f, floating ? .26f : .18f));
            follow.SetVisible(true);
            var motion = surface.GetComponent<CatMeButtonMotion>();
            if (motion != null) motion.RememberShadow(follow);
        }

        internal static void StyleButton(Button button, Color tint, bool primary)
        {
            if (button == null) return;
            var image = button.GetComponent<Image>();
            ApplySurface(image, tint);
            SetElevation(button.transform as RectTransform, Elevation.Raised);
            button.targetGraphic = image;
            button.transition = Selectable.Transition.None;   // press feedback is scale + shadow, handled by CatMeButtonMotion
            var motion = button.GetComponent<CatMeButtonMotion>() ?? button.gameObject.AddComponent<CatMeButtonMotion>();
            motion.Initialize(button.transform as RectTransform);
            motion.RememberShadow(button.GetComponent<ShadowFollow>());
            motion.Tint(image, tint);
            // Every styled button gets the same soft tick and light haptic.
            if (button.GetComponent<CatMeButtonFeedback>() == null) { button.gameObject.AddComponent<CatMeButtonFeedback>(); button.onClick.AddListener(() => { UiSounds.Click(); Haptics.Tap(); }); }

            Text label = button.GetComponentInChildren<Text>();
            if (label != null)
            {
                label.font = BoldFont;
                label.color = primary ? Color.white : Ink;
                label.fontStyle = Weight(FontStyle.Bold);
            }
        }

        /// <summary>Rounded raised card with the shared surface, bevel and shadow.</summary>
        internal static GameObject Card(Transform parent, string name, Vector2 size, Color color, Elevation tier = Elevation.Raised)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Image)); go.transform.SetParent(parent, false);
            var rect = go.GetComponent<RectTransform>(); rect.anchorMin = rect.anchorMax = new Vector2(.5f, .5f); rect.pivot = new Vector2(.5f, .5f); rect.sizeDelta = size;
            ApplySurface(go.GetComponent<Image>(), color);
            SetElevation(rect, tier);
            return go;
        }

        private static Sprite RoundedSurface
        {
            get
            {
                if (roundedSurface != null) return roundedSurface;
                const int size = 96;
                const float radius = 18f, half = size * .5f;
                var texture = new Texture2D(size, size, TextureFormat.RGBA32, false) { name = "CatMe raised surface", filterMode = FilterMode.Bilinear, wrapMode = TextureWrapMode.Clamp, hideFlags = HideFlags.HideAndDontSave };
                var pixels = new Color32[size * size];
                for (int y = 0; y < size; y++)
                for (int x = 0; x < size; x++)
                {
                    float px = Mathf.Abs(x + .5f - half) - (half - radius);
                    float py = Mathf.Abs(y + .5f - half) - (half - radius);
                    float outside = Mathf.Sqrt(Mathf.Max(px, 0f) * Mathf.Max(px, 0f) + Mathf.Max(py, 0f) * Mathf.Max(py, 0f));
                    float distance = outside + Mathf.Min(Mathf.Max(px, py), 0f) - radius;
                    float alpha = 1f - Edge(-.7f, .7f, distance);
                    // Gentle bevel: slightly brighter along the top edge, slightly deeper toward the bottom, thin inner rim highlight.
                    float v = y / (float)(size - 1);
                    float shade = Mathf.Lerp(.925f, 1f, Edge(0f, 1f, v));
                    float rim = Mathf.Exp(-Mathf.Abs(distance + 1.4f) * 1.1f) * Edge(.35f, 1f, v) * .35f;
                    float g = Mathf.Clamp01(shade + rim);
                    pixels[y * size + x] = new Color(g, g, g, alpha);
                }
                texture.SetPixels32(pixels); texture.Apply(false, true);
                roundedSurface = Sprite.Create(texture, new Rect(0, 0, size, size), new Vector2(.5f, .5f), 100f, 0, SpriteMeshType.FullRect, new Vector4(32, 32, 32, 32));
                roundedSurface.name = "CatMe raised 9-slice"; roundedSurface.hideFlags = HideFlags.HideAndDontSave;
                return roundedSurface;
            }
        }

        /// <summary>GLSL-style smoothstep (Mathf.SmoothStep interpolates between its first two arguments instead).</summary>
        internal static float Edge(float a, float b, float x) { float t = Mathf.Clamp01((x - a) / (b - a)); return t * t * (3f - 2f * t); }

        private static Sprite ShadowSprite
        {
            get
            {
                if (shadowSprite != null) return shadowSprite;
                const int size = 256; const float radius = 100f, half = size * .5f, blur = 40f;
                var texture = new Texture2D(size, size, TextureFormat.RGBA32, false) { name = "CatMe soft shadow", filterMode = FilterMode.Bilinear, wrapMode = TextureWrapMode.Clamp, hideFlags = HideFlags.HideAndDontSave };
                var pixels = new Color32[size * size];
                for (int y = 0; y < size; y++)
                for (int x = 0; x < size; x++)
                {
                    float px = Mathf.Abs(x + .5f - half) - (half - blur - radius);
                    float py = Mathf.Abs(y + .5f - half) - (half - blur - radius);
                    float outside = Mathf.Sqrt(Mathf.Max(px, 0f) * Mathf.Max(px, 0f) + Mathf.Max(py, 0f) * Mathf.Max(py, 0f));
                    float distance = outside + Mathf.Min(Mathf.Max(px, py), 0f) - radius;
                    float a = 1f - Edge(-blur, blur, distance);
                    pixels[y * size + x] = new Color(1f, 1f, 1f, a * a);
                }
                texture.SetPixels32(pixels); texture.Apply(false, true);
                shadowSprite = Sprite.Create(texture, new Rect(0, 0, size, size), new Vector2(.5f, .5f), 100f, 0, SpriteMeshType.FullRect, new Vector4(140, 140, 140, 140));
                shadowSprite.name = "CatMe shadow 9-slice"; shadowSprite.hideFlags = HideFlags.HideAndDontSave;
                return shadowSprite;
            }
        }
    }

    /// <summary>Marker so a button is only wired to the shared tick and haptic once.</summary>
    internal sealed class CatMeButtonFeedback : MonoBehaviour { }

    /// <summary>Keeps a shadow image (a sibling drawn behind its surface) matched to the surface's rect, scale and visibility.</summary>
    internal sealed class ShadowFollow : MonoBehaviour
    {
        RectTransform shadow, surface; Image image; float spread, drop; Color color; internal float alphaScale = 1f; bool visible = true;

        internal void Configure(Sprite sprite, float multiplier, float spreadPt, float dropPt, Color tint)
        {
            surface = (RectTransform)transform; spread = spreadPt; drop = dropPt; color = tint;
            if (shadow == null)
            {
                var go = new GameObject(name + " shadow", typeof(RectTransform), typeof(Image), typeof(LayoutElement));
                go.GetComponent<LayoutElement>().ignoreLayout = true;
                go.transform.SetParent(surface.parent, false);
                shadow = (RectTransform)go.transform; image = go.GetComponent<Image>(); image.raycastTarget = false; image.type = Image.Type.Sliced;
            }
            image.sprite = sprite; image.pixelsPerUnitMultiplier = multiplier; shadow.SetSiblingIndex(Mathf.Max(0, surface.GetSiblingIndex()));
            Sync();
        }

        internal void SetVisible(bool value) { visible = value; if (shadow != null) shadow.gameObject.SetActive(value && isActiveAndEnabled); }
        void OnEnable() { if (shadow != null) shadow.gameObject.SetActive(visible); }
        void OnDisable() { if (shadow != null) shadow.gameObject.SetActive(false); }
        void OnDestroy() { if (shadow != null) Destroy(shadow.gameObject); }
        void LateUpdate() { Sync(); }

        internal void SyncNow() { Sync(); }
        void Sync()
        {
            if (shadow == null || surface == null) return;
            if (shadow.parent != surface.parent) shadow.SetParent(surface.parent, false);
            shadow.anchorMin = surface.anchorMin; shadow.anchorMax = surface.anchorMax; shadow.pivot = surface.pivot;
            shadow.sizeDelta = surface.sizeDelta + new Vector2(spread * 2f, spread * 2f);
            shadow.anchoredPosition = surface.anchoredPosition + new Vector2(0f, -drop);
            shadow.localScale = surface.localScale; shadow.localRotation = surface.localRotation;
            var c = color; c.a *= alphaScale; if (image != null) image.color = c;
        }
    }
}
