using System.Collections;
using UnityEngine;

namespace CatMe.Session
{
    /// <summary>One lighting look for a place: sun, ambient, fog, sky. Values follow docs/production/04 §6.6.</summary>
    public sealed class PlaceLightingPreset
    {
        public string name;
        public float sunPitch, sunYaw, sunIntensity;
        public Color sunColor, ambientSky, ambientEquator, ambientGround, fogColor;
        public float ambientIntensity = 1f, fogDensity, skyExposure = 1f, fillIntensity = .08f;
        public Color skyTint = Color.white;
        public string skyResource = "Skyboxes/Sky_Sunset";

        /// <summary>Blend two presets into a new one (sky resource comes from the first).</summary>
        public static PlaceLightingPreset Lerp(PlaceLightingPreset a, PlaceLightingPreset b, float t)
        {
            return new PlaceLightingPreset
            {
                name = t < .5f ? a.name : b.name, skyResource = a.skyResource,
                sunPitch = Mathf.LerpAngle(a.sunPitch, b.sunPitch, t), sunYaw = Mathf.LerpAngle(a.sunYaw, b.sunYaw, t), sunIntensity = Mathf.Lerp(a.sunIntensity, b.sunIntensity, t),
                sunColor = Color.Lerp(a.sunColor, b.sunColor, t), ambientSky = Color.Lerp(a.ambientSky, b.ambientSky, t), ambientEquator = Color.Lerp(a.ambientEquator, b.ambientEquator, t),
                ambientGround = Color.Lerp(a.ambientGround, b.ambientGround, t), fogColor = Color.Lerp(a.fogColor, b.fogColor, t), ambientIntensity = Mathf.Lerp(a.ambientIntensity, b.ambientIntensity, t),
                fogDensity = Mathf.Lerp(a.fogDensity, b.fogDensity, t), skyExposure = Mathf.Lerp(a.skyExposure, b.skyExposure, t), fillIntensity = Mathf.Lerp(a.fillIntensity, b.fillIntensity, t),
                skyTint = Color.Lerp(a.skyTint, b.skyTint, t)
            };
        }

        static Color Hex(string hex) { ColorUtility.TryParseHtmlString(hex, out Color c); return c; }

        public static PlaceLightingPreset Get(string preset)
        {
            switch (preset)
            {
                case "GoldenAfternoon":
                    return new PlaceLightingPreset { name = preset, skyResource = "Skyboxes/Sky_Day", skyTint = Hex("#9DB8E4"), sunPitch = 27, sunYaw = -55, sunIntensity = 1.7f, sunColor = Hex("#FFB86B"), ambientSky = Hex("#B9C4DC"), ambientEquator = Hex("#C9A07A"), ambientGround = Hex("#7A5F40"), fogColor = Hex("#EBC99A"), fogDensity = .0075f, skyExposure = 1.2f, ambientIntensity = 1f, fillIntensity = .1f };
                case "DappledWood":
                    return new PlaceLightingPreset { name = preset, skyResource = "Skyboxes/Sky_Day", skyTint = Hex("#78A6E8"), sunPitch = 48, sunYaw = -30, sunIntensity = 1.35f, sunColor = Hex("#FFE6C0"), ambientSky = Hex("#8FA690"), ambientEquator = Hex("#7C8C6C"), ambientGround = Hex("#4F5A3F"), fogColor = Hex("#C4D2B8"), fogDensity = .010f, skyExposure = 1.1f, ambientIntensity = .9f };
                case "Overcast":
                    return new PlaceLightingPreset { name = preset, sunPitch = 50, sunYaw = 0, sunIntensity = .55f, sunColor = Hex("#C8D2DC"), ambientSky = Hex("#8C99A6"), ambientEquator = Hex("#7B8791"), ambientGround = Hex("#4C535A"), fogColor = Hex("#9AA4AD"), fogDensity = .02f, skyExposure = .55f, skyTint = Hex("#8A97A6"), ambientIntensity = .95f, fillIntensity = .12f };
                case "Sunset":
                    return new PlaceLightingPreset { name = preset, sunPitch = 6, sunYaw = -80, sunIntensity = 1.1f, sunColor = Hex("#FF9E5E"), ambientSky = Hex("#7F8FB8"), ambientEquator = Hex("#9A7E7A"), ambientGround = Hex("#5A4A3E"), fogColor = Hex("#E79B6E"), fogDensity = .01f, skyExposure = 1f, ambientIntensity = .85f, fillIntensity = .12f };
                case "BlueHour":
                    return new PlaceLightingPreset { name = preset, sunPitch = -4, sunYaw = -80, sunIntensity = 0f, sunColor = Hex("#9DB4E0"), ambientSky = Hex("#4A5F86"), ambientEquator = Hex("#3F4A66"), ambientGround = Hex("#2C2A30"), fogColor = Hex("#3E4D6B"), fogDensity = .014f, skyExposure = .35f, skyTint = Hex("#6F86B8"), ambientIntensity = .9f, fillIntensity = .22f };
                case "HouseInterior":
                    return new PlaceLightingPreset { name = preset, skyResource = "Skyboxes/Sky_Day", skyTint = Hex("#9DB8E4"), sunPitch = 50, sunYaw = 0, sunIntensity = 0f, sunColor = Color.black, ambientSky = Hex("#6B5640"), ambientEquator = Hex("#7A6048"), ambientGround = Hex("#4A3A2A"), fogColor = Hex("#C9D6DE"), fogDensity = .0015f, skyExposure = 1.1f, ambientIntensity = 1f, fillIntensity = 0f };
                case "TentInterior":
                    return new PlaceLightingPreset { name = preset, skyResource = "Skyboxes/Sky_Sunset", sunPitch = 50, sunYaw = 0, sunIntensity = 0f, sunColor = Color.black, ambientSky = Hex("#1C2128"), ambientEquator = Hex("#1A1E24"), ambientGround = Hex("#101216"), fogColor = Hex("#14171C"), fogDensity = .03f, skyExposure = .1f, ambientIntensity = .5f, fillIntensity = 0f };
                case "Morning":
                default:
                    return new PlaceLightingPreset { name = "Morning", skyResource = "Skyboxes/Sky_Day", skyTint = Hex("#78A6E8"), sunPitch = 42, sunYaw = -40, sunIntensity = 1.5f, sunColor = Hex("#FFE2B8"), ambientSky = Hex("#BFD3E6"), ambientEquator = Hex("#B59A80"), ambientGround = Hex("#7A6A4E"), fogColor = Hex("#E6DCC8"), fogDensity = .005f, skyExposure = 1.25f, ambientIntensity = 1f };
            }
        }
    }

    /// <summary>Applies a preset to the shell's sun and RenderSettings, optionally blended. Only writer of those values in session mode.</summary>
    public sealed class PlaceLighting : MonoBehaviour
    {
        Light sun, fill;
        Material sky;
        PlaceLightingPreset current;
        Coroutine blend;

        void Awake() { Resolve(); }

        void Resolve()
        {
            if (sun == null) sun = GameObject.Find("Garden sunlight")?.GetComponent<Light>();
            if (fill == null) fill = GameObject.Find("Garden sky fill")?.GetComponent<Light>();
            if (sky == null)
            {
                Material source = RenderSettings.skybox != null ? RenderSettings.skybox : Resources.Load<Material>("Skyboxes/Sky_Sunset");
                if (source != null) { sky = new Material(source); RenderSettings.skybox = sky; }
            }
        }

        public string CurrentName => current?.name;

        string skyResource;

        void UseSky(string resource)
        {
            if (resource == skyResource && sky != null) return;
            var source = Resources.Load<Material>(resource) ?? Resources.Load<Material>("Skyboxes/Sky_Sunset");
            if (source == null) return;
            skyResource = resource; sky = new Material(source); RenderSettings.skybox = sky;
        }

        /// <summary>Applies an already-mixed preset immediately (used by the day director every few seconds).</summary>
        public void ApplyPreset(PlaceLightingPreset preset)
        {
            Resolve();
            UseSky(preset.skyResource);
            if (sun != null) RenderSettings.sun = sun;
            if (blend != null) { StopCoroutine(blend); blend = null; }
            Set(preset, preset, 1f); current = preset;
        }

        public void Apply(string presetName, float seconds)
        {
            Resolve();
            var target = PlaceLightingPreset.Get(presetName);
            UseSky(target.skyResource);
            if (sun != null) RenderSettings.sun = sun;
            if (blend != null) StopCoroutine(blend);
            if (seconds <= 0.01f || current == null || PlayerPrefs.GetInt("CatMe.ReduceMotion", 0) == 1 && seconds < 2f) { Set(target, target, 1f); current = target; return; }
            blend = StartCoroutine(Blend(current, target, seconds));
        }

        IEnumerator Blend(PlaceLightingPreset from, PlaceLightingPreset to, float seconds)
        {
            for (float t = 0f; t < seconds; t += Time.unscaledDeltaTime)
            {
                Set(from, to, Mathf.SmoothStep(0f, 1f, t / seconds));
                yield return null;
            }
            Set(to, to, 1f); current = to; blend = null;
        }

        void Set(PlaceLightingPreset a, PlaceLightingPreset b, float t)
        {
            if (sun != null)
            {
                sun.transform.rotation = Quaternion.Euler(Mathf.LerpAngle(a.sunPitch, b.sunPitch, t), Mathf.LerpAngle(a.sunYaw, b.sunYaw, t), 0f);
                sun.color = Color.Lerp(a.sunColor, b.sunColor, t);
                sun.intensity = Mathf.Lerp(a.sunIntensity, b.sunIntensity, t);
                sun.enabled = sun.intensity > .01f;
            }
            if (fill != null) fill.intensity = Mathf.Lerp(a.fillIntensity, b.fillIntensity, t);
            RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Trilight;
            RenderSettings.ambientSkyColor = Color.Lerp(a.ambientSky, b.ambientSky, t);
            RenderSettings.ambientEquatorColor = Color.Lerp(a.ambientEquator, b.ambientEquator, t);
            RenderSettings.ambientGroundColor = Color.Lerp(a.ambientGround, b.ambientGround, t);
            RenderSettings.ambientIntensity = Mathf.Lerp(a.ambientIntensity, b.ambientIntensity, t);
            // Full-strength sky reflections washed the terrain with a mint sheen in the first renders.
            RenderSettings.reflectionIntensity = .2f;
            RenderSettings.fog = true;
            RenderSettings.fogMode = FogMode.ExponentialSquared;
            RenderSettings.fogColor = Color.Lerp(a.fogColor, b.fogColor, t);
            RenderSettings.fogDensity = Mathf.Lerp(a.fogDensity, b.fogDensity, t);
            if (sky != null)
            {
                if (sky.HasProperty("_Exposure")) sky.SetFloat("_Exposure", Mathf.Lerp(a.skyExposure, b.skyExposure, t));
                if (sky.HasProperty("_Tint")) sky.SetColor("_Tint", Color.Lerp(a.skyTint, b.skyTint, t));
                if (sky.HasProperty("_SkyTint")) sky.SetColor("_SkyTint", Color.Lerp(a.skyTint, b.skyTint, t));
            }
        }
    }
}
