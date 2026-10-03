using System;
using System.Collections.Generic;
using UnityEngine;

namespace CatMe.Garden
{
    /// <summary>
    /// Line icons drawn in code (24 x 24 grid, round caps) and cached as white sprites, so any Image can tint them.
    /// Matches the icon set in the approved prototypes. Replace a name by adding a PNG of the same name under Resources/UI/Icons.
    /// </summary>
    internal static class UiIcons
    {
        const int Size = 128; const float Grid = 24f, Stroke = 1.75f;
        static readonly Dictionary<string, Sprite> cache = new Dictionary<string, Sprite>();

        internal static Sprite Get(string name)
        {
            if (cache.TryGetValue(name, out var s) && s != null) return s;
            var custom = Resources.Load<Texture2D>("UI/Icons/" + name);
            if (custom != null) s = Sprite.Create(custom, new Rect(0, 0, custom.width, custom.height), new Vector2(.5f, .5f), 100f);
            else
            {
                var segs = new List<Vector4>();
                if (!Build(name, segs)) { cache[name] = null; return null; }
                s = Rasterize(segs, name);
            }
            cache[name] = s; return s;
        }

        // ---- shape helpers (y grows downward, like SVG) ------------------------------------------------------------
        static void Line(List<Vector4> o, params float[] p) { for (int i = 0; i + 3 < p.Length; i += 4) o.Add(new Vector4(p[i], p[i + 1], p[i + 2], p[i + 3])); }
        static void Poly(List<Vector4> o, bool close, params float[] p) { for (int i = 0; i + 3 < p.Length; i += 2) o.Add(new Vector4(p[i], p[i + 1], p[i + 2], p[i + 3])); if (close) o.Add(new Vector4(p[p.Length - 2], p[p.Length - 1], p[0], p[1])); }
        static void Arc(List<Vector4> o, float cx, float cy, float rx, float ry, float a0, float a1)
        {
            int n = Mathf.Max(6, Mathf.CeilToInt(Mathf.Abs(a1 - a0) / 12f)); Vector2 prev = default;
            for (int i = 0; i <= n; i++)
            {
                float a = Mathf.Deg2Rad * Mathf.Lerp(a0, a1, i / (float)n); var pt = new Vector2(cx + Mathf.Cos(a) * rx, cy + Mathf.Sin(a) * ry);
                if (i > 0) o.Add(new Vector4(prev.x, prev.y, pt.x, pt.y)); prev = pt;
            }
        }
        static void Circle(List<Vector4> o, float cx, float cy, float r) => Arc(o, cx, cy, r, r, 0, 360);

        static bool Build(string n, List<Vector4> o)
        {
            switch (n)
            {
                case "paw": Circle(o, 7f, 9.5f, 1.8f); Circle(o, 12f, 6.5f, 1.8f); Circle(o, 17f, 9.5f, 1.8f); Arc(o, 12f, 15.6f, 4.8f, 3.6f, 0, 360); return true;
                case "pause": Line(o, 9f, 6.5f, 9f, 17.5f, 15f, 6.5f, 15f, 17.5f); return true;
                case "call": Circle(o, 12f, 14f, 1.4f); Arc(o, 12f, 14f, 4.6f, 4.6f, 215, 325); Arc(o, 12f, 14f, 8.2f, 8.2f, 215, 325); Line(o, 12f, 15.5f, 12f, 20f); return true;
                case "walk": Circle(o, 13f, 5f, 1.8f); Poly(o, false, 12f, 8.5f, 9.5f, 12.5f, 12f, 14.8f, 12f, 20f); Poly(o, false, 12f, 8.5f, 15f, 12f, 17.5f, 12.6f); Line(o, 9.5f, 12.5f, 7f, 15.5f); return true;
                case "ball": Circle(o, 12f, 12f, 7.6f); Poly(o, false, 4.6f, 10.3f, 8f, 11.3f, 12f, 11.6f, 16f, 11.3f, 19.4f, 10.3f); Poly(o, false, 4.6f, 14f, 8f, 13f, 12f, 12.7f, 16f, 13f, 19.4f, 14f); return true;
                case "sit": Poly(o, true, 5f, 11f, 19f, 11f, 19f, 15f, 5f, 15f); Line(o, 7f, 15f, 7f, 19f, 17f, 15f, 17f, 19f); Poly(o, false, 6f, 11f, 6f, 6f, 18f, 6f, 18f, 11f); return true;
                case "arrow": Line(o, 5f, 12f, 19f, 12f); Poly(o, false, 13f, 6f, 19f, 12f, 13f, 18f); return true;
                case "camera": Poly(o, true, 4f, 8f, 7f, 8f, 8.6f, 5.8f, 15.4f, 5.8f, 17f, 8f, 20f, 8f, 20f, 19f, 4f, 19f); Circle(o, 12f, 13.2f, 3.4f); return true;
                case "album": Poly(o, true, 4f, 5.5f, 20f, 5.5f, 20f, 18.5f, 4f, 18.5f); Poly(o, false, 4f, 15.5f, 8.5f, 11f, 12.5f, 15f, 15.5f, 12f, 20f, 16.5f); Circle(o, 15.5f, 9f, 1.3f); return true;
                case "gear": Circle(o, 12f, 12f, 3.2f); for (int i = 0; i < 8; i++) { float a = i * Mathf.PI / 4f; Line(o, 12f + Mathf.Cos(a) * 6.2f, 12f + Mathf.Sin(a) * 6.2f, 12f + Mathf.Cos(a) * 8.6f, 12f + Mathf.Sin(a) * 8.6f); } Circle(o, 12f, 12f, 6.2f); return true;
                case "store": Poly(o, true, 5f, 9f, 19f, 9f, 18f, 19.5f, 6f, 19.5f); Arc(o, 12f, 9f, 3.6f, 3.6f, 180, 360); return true;
                case "pets": Circle(o, 9f, 8.5f, 3f); Arc(o, 9f, 19f, 5.6f, 5.2f, 180, 360); Circle(o, 17.5f, 10f, 2.2f); Arc(o, 17.5f, 19f, 3.8f, 3.6f, 200, 340); return true;
                case "account": Circle(o, 12f, 8.5f, 3.6f); Arc(o, 12f, 20f, 7.2f, 6.5f, 180, 360); return true;
                case "bowl": Line(o, 4f, 11f, 20f, 11f); Arc(o, 12f, 11f, 8f, 7.5f, 0, 180); Poly(o, false, 9f, 8f, 10f, 6.5f, 9f, 5f); Poly(o, false, 14f, 8f, 15f, 6.5f, 14f, 5f); return true;
                case "bed": Line(o, 3f, 6.5f, 3f, 18.5f, 3f, 14f, 21f, 14f, 21f, 14f, 21f, 18.5f, 11f, 14f, 11f, 9.5f); Poly(o, false, 11f, 9.5f, 19f, 9.5f, 21f, 12f, 21f, 14f); Circle(o, 7f, 11f, 1.8f); return true;
                case "door": Poly(o, false, 10f, 5f, 5f, 5f, 5f, 19f, 10f, 19f); Poly(o, false, 14f, 8f, 18f, 12f, 14f, 16f); Line(o, 18f, 12f, 9f, 12f); return true;
                case "up": Poly(o, false, 6f, 15f, 12f, 9f, 18f, 15f); return true;
                case "back": Poly(o, false, 15f, 5f, 8f, 12f, 15f, 19f); return true;
                case "close": Line(o, 6f, 6f, 18f, 18f, 18f, 6f, 6f, 18f); return true;
                case "plus": Line(o, 12f, 5f, 12f, 19f, 5f, 12f, 19f, 12f); return true;
                case "check": Poly(o, false, 5f, 12.5f, 9.5f, 17f, 19f, 7.5f); return true;
                case "trash": Line(o, 5f, 7f, 19f, 7f, 10f, 7f, 10f, 5f, 10f, 5f, 14f, 5f, 14f, 5f, 14f, 7f); Poly(o, false, 7f, 7f, 8f, 19f, 16f, 19f, 17f, 7f); return true;
                case "share": Line(o, 12f, 15f, 12f, 4.5f); Poly(o, false, 8f, 8f, 12f, 4f, 16f, 8f); Poly(o, false, 5f, 13f, 5f, 19f, 19f, 19f, 19f, 13f); return true;
                case "apple": Arc(o, 12f, 14f, 6.2f, 6.4f, 0, 360); Poly(o, false, 12f, 8f, 12.4f, 5.6f, 14.2f, 4.2f); return true;
                case "google": Arc(o, 12f, 12f, 7.4f, 7.4f, 40, 320); Line(o, 12f, 12f, 19.4f, 12f); return true;
                case "gamecenter": Circle(o, 9.4f, 9.4f, 4.4f); Circle(o, 14.6f, 9.4f, 4.4f); Circle(o, 9.4f, 14.6f, 4.4f); Circle(o, 14.6f, 14.6f, 4.4f); return true;
                case "mail": Poly(o, true, 4f, 6.5f, 20f, 6.5f, 20f, 17.5f, 4f, 17.5f); Poly(o, false, 4.5f, 7.5f, 12f, 13f, 19.5f, 7.5f); return true;
                case "lock": Poly(o, true, 6f, 11f, 18f, 11f, 18f, 19f, 6f, 19f); Arc(o, 12f, 11f, 4f, 4.5f, 180, 360); return true;
                case "speaker": Poly(o, true, 4f, 9.5f, 8f, 9.5f, 12.5f, 5.5f, 12.5f, 18.5f, 8f, 14.5f, 4f, 14.5f); Arc(o, 12.5f, 12f, 4.5f, 4.5f, -50, 50); Arc(o, 12.5f, 12f, 8f, 8f, -50, 50); return true;
                case "vibrate": Poly(o, true, 8f, 4.5f, 16f, 4.5f, 16f, 19.5f, 8f, 19.5f); Line(o, 4.5f, 9f, 4.5f, 15f, 19.5f, 9f, 19.5f, 15f, 2.5f, 11f, 2.5f, 13f, 21.5f, 11f, 21.5f, 13f); return true;
                case "bulb": Arc(o, 12f, 10f, 5.6f, 5.6f, 150, 390); Line(o, 9.2f, 14.5f, 9.6f, 17f, 14.8f, 14.5f, 14.4f, 17f, 9.8f, 19.5f, 14.2f, 19.5f); return true;
                case "book": Poly(o, false, 12f, 7f, 7.5f, 5.5f, 4f, 6f, 4f, 18f, 7.5f, 17.5f, 12f, 19f); Poly(o, false, 12f, 7f, 16.5f, 5.5f, 20f, 6f, 20f, 18f, 16.5f, 17.5f, 12f, 19f); Line(o, 12f, 7f, 12f, 19f); return true;
                case "clock": Circle(o, 12f, 12f, 8f); Poly(o, false, 12f, 7f, 12f, 12f, 15.5f, 14f); return true;
                case "motion": Poly(o, false, 4f, 8f, 8f, 6f, 12f, 8f, 16f, 6f, 20f, 8f); Poly(o, false, 4f, 12f, 8f, 10f, 12f, 12f, 16f, 10f, 20f, 12f); Poly(o, false, 4f, 16f, 8f, 14f, 12f, 16f, 16f, 14f, 20f, 16f); return true;
                case "heart": Arc(o, 8.5f, 9f, 3.6f, 3.6f, 135, 360); Arc(o, 15.5f, 9f, 3.6f, 3.6f, 180, 405); Poly(o, false, 5.1f, 11.6f, 12f, 19f, 18.9f, 11.6f); return true;
            }
            return false;
        }

        static Sprite Rasterize(List<Vector4> segs, string name)
        {
            var tex = new Texture2D(Size, Size, TextureFormat.RGBA32, false) { name = "icon " + name, filterMode = FilterMode.Bilinear, wrapMode = TextureWrapMode.Clamp, hideFlags = HideFlags.HideAndDontSave };
            var px = new Color32[Size * Size]; float scale = Size / Grid, half = Stroke * .5f, aa = 1.1f / scale;
            for (int y = 0; y < Size; y++)
                for (int x = 0; x < Size; x++)
                {
                    float gx = (x + .5f) / scale, gy = Grid - (y + .5f) / scale, best = 99f;
                    for (int i = 0; i < segs.Count; i++)
                    {
                        var s = segs[i]; Vector2 a = new Vector2(s.x, s.y), b = new Vector2(s.z, s.w), p = new Vector2(gx, gy), ab = b - a;
                        float t = ab.sqrMagnitude < 1e-6f ? 0f : Mathf.Clamp01(Vector2.Dot(p - a, ab) / ab.sqrMagnitude);
                        float d = (p - (a + ab * t)).magnitude; if (d < best) best = d;
                    }
                    float alpha = 1f - Mathf.Clamp01((best - half + aa * .5f) / aa);
                    px[y * Size + x] = new Color(1f, 1f, 1f, alpha);
                }
            tex.SetPixels32(px); tex.Apply(false, true);
            var sprite = Sprite.Create(tex, new Rect(0, 0, Size, Size), new Vector2(.5f, .5f), 100f, 0, SpriteMeshType.FullRect); sprite.hideFlags = HideFlags.HideAndDontSave; sprite.name = name;
            return sprite;
        }
    }
}
