using CatMe.Session;
using UnityEngine;

namespace CatMe.World
{
    public enum DayType { Clear, PartlyCloudy, MistyMorning, Showery, Rainy }

    /// <summary>
    /// Picks a day type per session and moves the world through dawn, morning, midday, golden hour, sunset and night,
    /// clouds come and go, showers pass, wind rises before rain, lanterns and windows light at dusk. Saved so a relaunch resumes the same day.
    /// Needs the existing PlaceLighting (look) and PlaceWeather (rain). See docs/production/05_LIVING_WORLD_AND_PREMIUM_FEEL.md R-WX.
    /// </summary>
    public sealed class DayDirector : MonoBehaviour
    {
        const string TypeKey = "CatMe.World.DayType", TimeKey = "CatMe.World.Time", SavedAtKey = "CatMe.World.SavedAt";
        const float CycleSeconds = 1080f;
        const double NewDayAfterHours = 3.0;  // coming back after a break starts a fresh day

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        static void DefaultWind() { Shader.SetGlobalFloat("_CatMeWindScale", 1f); }

        SessionDirector director; PlaceLighting lighting; PlaceWeather weather;
        AudioSource windAudio, riverAudio; AudioClip windClip;
        public DayType Type { get; private set; }
        public float TimeOfDay { get; private set; }        // 0 dawn, .12 morning, .3 midday, .48 golden hour, .66 sunset, .8 night, 1 dawn
        public float Cloudiness { get; private set; }
        public bool Night => TimeOfDay >= .78f && TimeOfDay < .97f;
        public bool Suspended { get; set; }                  // true inside rooms, which set their own lighting
        float cloudTarget, showerAt, showerUntil, applyAt, saveAt, driftSeed;
        bool nightApplied, announcePending;
        float announceAt;
        ParticleSystem fireflies;

        public void Initialize(SessionDirector sessionDirector, PlaceLighting placeLighting, PlaceWeather placeWeather)
        {
            director = sessionDirector; lighting = placeLighting; weather = placeWeather;
            driftSeed = Random.value * 100f;
            if (PlayerPrefs.HasKey(TypeKey) && PlayerPrefs.HasKey(TimeKey) && !SavedLongAgo() && PlayerPrefs.GetFloat(TimeKey) < .97f)
            { Type = (DayType)Mathf.Clamp(PlayerPrefs.GetInt(TypeKey), 0, 4); TimeOfDay = Mathf.Clamp01(PlayerPrefs.GetFloat(TimeKey)); }
            else NewDay();
            windClip = Resources.Load<AudioClip>("CatMeAudio/wind-low");
            if (windClip != null) { windAudio = gameObject.AddComponent<AudioSource>(); windAudio.clip = windClip; windAudio.loop = true; windAudio.spatialBlend = 0f; windAudio.volume = 0f; windAudio.Play(); }
            riverAudio = GameObject.Find("River bank sound")?.GetComponent<AudioSource>();
            Cloudiness = cloudTarget = TargetCloud();
            ScheduleShower();
            BuildFireflies();
        }

        // Soft glowing motes near the owner at dusk (R-LIFE-05); emission follows how dark it is and stops in rain.
        void BuildFireflies()
        {
            var go = new GameObject("Fireflies"); go.transform.SetParent(transform, false);
            fireflies = go.AddComponent<ParticleSystem>();
            var main = fireflies.main; main.loop = true; main.simulationSpace = ParticleSystemSimulationSpace.World; main.startLifetime = 6f; main.startSpeed = .12f; main.startSize = .07f;
            main.maxParticles = 60; main.startColor = new Color(1f, .93f, .5f, 1f); main.gravityModifier = -.005f;
            var emission = fireflies.emission; emission.rateOverTime = 0f;
            var shape = fireflies.shape; shape.shapeType = ParticleSystemShapeType.Box; shape.scale = new Vector3(18f, 2.5f, 18f);
            var noise = fireflies.noise; noise.enabled = true; noise.strength = .6f; noise.frequency = .4f;
            var col = fireflies.colorOverLifetime; col.enabled = true;
            var g = new Gradient(); g.SetKeys(new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) }, new[] { new GradientAlphaKey(0f, 0f), new GradientAlphaKey(1f, .25f), new GradientAlphaKey(1f, .7f), new GradientAlphaKey(0f, 1f) });
            col.color = g;
            var renderer = go.GetComponent<ParticleSystemRenderer>();
            Shader shader = Shader.Find("Universal Render Pipeline/Particles/Unlit") ?? Shader.Find("Sprites/Default");
            var mat = new Material(shader);
            const int n = 32; var tex = new Texture2D(n, n, TextureFormat.RGBA32, false);
            for (int y = 0; y < n; y++) for (int x = 0; x < n; x++) { float d = Vector2.Distance(new Vector2(x, y), new Vector2(n / 2f, n / 2f)) / (n / 2f); tex.SetPixel(x, y, new Color(1, 1, 1, Mathf.Clamp01(1f - d) * Mathf.Clamp01(1f - d))); }
            tex.Apply(); mat.mainTexture = tex; if (mat.HasProperty("_BaseMap")) mat.SetTexture("_BaseMap", tex);
            if (mat.HasProperty("_Surface")) { mat.SetFloat("_Surface", 1f); mat.SetFloat("_Blend", 2f); mat.EnableKeyword("_SURFACE_TYPE_TRANSPARENT"); mat.renderQueue = 3000; }
            renderer.material = mat;
            fireflies.Play();
        }

        /// <summary>Weighted toward fair weather; never chaotic.</summary>
        public void NewDay()
        {
            bool hadDay = PlayerPrefs.HasKey(TypeKey);
            var previous = (DayType)PlayerPrefs.GetInt(TypeKey, -1);
            // Never the same weather two days running, so returning players notice the garden change.
            do
            {
                float r = Random.value;
                Type = r < .40f ? DayType.Clear : r < .65f ? DayType.PartlyCloudy : r < .75f ? DayType.MistyMorning : r < .90f ? DayType.Showery : DayType.Rainy;
            }
            while (hadDay && Type == previous);
            TimeOfDay = Type == DayType.MistyMorning ? .12f : 0f;
            announcePending = true; announceAt = Time.time + 6f;
            Save();
            Debug.Log("[CatMe][World] New day: " + Type + ", starting at time " + TimeOfDay.ToString("F2"));
        }

        void Save() { PlayerPrefs.SetInt(TypeKey, (int)Type); PlayerPrefs.SetFloat(TimeKey, TimeOfDay); PlayerPrefs.SetString(SavedAtKey, System.DateTime.UtcNow.Ticks.ToString()); PlayerPrefs.Save(); }

        static bool SavedLongAgo()
        {
            if (!long.TryParse(PlayerPrefs.GetString(SavedAtKey, ""), out long ticks)) return false;
            return (System.DateTime.UtcNow - new System.DateTime(ticks, System.DateTimeKind.Utc)).TotalHours > NewDayAfterHours;
        }

        // One quiet line so the player notices what kind of day it is.
        static string DayLine(DayType type)
        {
            switch (type)
            {
                case DayType.Clear: return "A clear, sunny day.";
                case DayType.PartlyCloudy: return "Soft clouds drifting over.";
                case DayType.MistyMorning: return "A misty start to the day.";
                case DayType.Showery: return "Showers might pass by today.";
                default: return "A rainy day. The tent is cosy.";
            }
        }

        float TargetCloud()
        {
            switch (Type)
            {
                case DayType.Clear: return .05f;
                case DayType.PartlyCloudy: return .3f + .15f * Mathf.Sin(Time.time * .03f + driftSeed);
                case DayType.MistyMorning: return .3f;
                case DayType.Showery: return Time.time < showerUntil && Time.time > showerAt ? .85f : .4f;
                default: return .9f;
            }
        }

        void ScheduleShower() { showerAt = Time.time + Random.Range(70f, 140f); showerUntil = showerAt + Random.Range(40f, 75f); }

        void Update()
        {
            if (director == null || lighting == null) return;
            float dt = Time.deltaTime;
            if (!Suspended)
            {
                TimeOfDay += dt / CycleSeconds;
                if (TimeOfDay >= 1f)
                {
                    NewDay(); Cloudiness = cloudTarget = TargetCloud(); ScheduleShower();
                    announceAt = Time.time + 2f;
                }
            }

            // Cloud and rain targets
            if (Type == DayType.Showery && Time.time > showerUntil) ScheduleShower();
            cloudTarget = TargetCloud();
            float rain = Type == DayType.Rainy ? .8f : Type == DayType.Showery && Time.time > showerAt && Time.time < showerUntil ? .7f : 0f;
            if (weather != null) weather.DayRain = Suspended ? 0f : rain;
            float weatherIntensity = weather != null ? weather.Intensity : 0f;
            Cloudiness = Mathf.MoveTowards(Cloudiness, Mathf.Max(cloudTarget, weatherIntensity), dt / 25f);

            // Wind rises before and during rain, calms afterwards
            Shader.SetGlobalFloat("_CatMeWindScale", 1f + 1.1f * Cloudiness + .8f * weatherIntensity);
            if (windAudio != null) windAudio.volume = Mathf.MoveTowards(windAudio.volume, PlayerPrefs.GetInt("CatMe.SoundEnabled", 1) == 0 ? 0f : .02f + .1f * Cloudiness, dt * .05f);
            if (riverAudio == null) riverAudio = GameObject.Find("River bank sound")?.GetComponent<AudioSource>();
            if (riverAudio != null) riverAudio.mute = PlayerPrefs.GetInt("CatMe.SoundEnabled", 1) == 0;

            if (fireflies != null && director.CameraRigTransform != null)
            {
                var em = fireflies.emission; float dusk = Mathf.InverseLerp(.62f, .85f, TimeOfDay);
                em.rateOverTime = Suspended || weatherIntensity > .2f ? 0f : 7f * dusk;
                fireflies.transform.position = director.OwnerPositionForWorld + Vector3.up * 1.3f;
            }
            if (!Suspended && Time.time >= applyAt)
            {
                applyAt = Time.time + (PlayerPrefs.GetInt("CatMe.ReduceMotion", 0) == 1 ? 4f : 1.5f);
                lighting.ApplyPreset(Compose());
            }
            if (!Suspended && Night != nightApplied) { nightApplied = Night; director.SetNightObjects(Night); }
            if (announcePending && Time.time >= announceAt && !Suspended)
            {
                var garden = FindAnyObjectByType<CatMe.Garden.GardenCompanionDemo>();
                if (garden != null && garden.IsReady) { announcePending = false; garden.SetAppStatus(DayLine(Type)); }
            }
            if (Time.time >= saveAt) { saveAt = Time.time + 20f; Save(); }
        }

        PlaceLightingPreset Compose()
        {
            var morning = PlaceLightingPreset.Get("Morning"); var golden = PlaceLightingPreset.Get("GoldenAfternoon");
            var sunset = PlaceLightingPreset.Get("Sunset"); var blue = PlaceLightingPreset.Get("BlueHour");
            var midday = PlaceLightingPreset.Lerp(morning, golden, .45f); midday.name = "Midday";
            var dawn = PlaceLightingPreset.Lerp(blue, morning, .55f); dawn.name = "Dawn";
            var night = PlaceLightingPreset.Lerp(blue, blue, 0f); night.name = "Night";
            night.ambientSky *= .42f; night.ambientEquator *= .42f; night.ambientGround *= .42f;
            night.fogColor *= .5f; night.skyExposure = .14f; night.fillIntensity = .08f;
            float t = TimeOfDay;
            PlaceLightingPreset basePreset = t < .12f ? Blend(dawn, morning, t / .12f)
                : t < .30f ? Blend(morning, midday, (t - .12f) / .18f)
                : t < .48f ? Blend(midday, golden, (t - .30f) / .18f)
                : t < .66f ? Blend(golden, sunset, (t - .48f) / .18f)
                : t < .78f ? Blend(sunset, blue, (t - .66f) / .12f)
                : t < .94f ? Blend(blue, night, (t - .78f) / .16f)
                : Blend(night, dawn, (t - .94f) / .06f);
            var mixed = PlaceLightingPreset.Lerp(basePreset, PlaceLightingPreset.Get("Overcast"), Cloudiness * .75f);
            if (Type == DayType.MistyMorning) { float mist = 1f - Mathf.InverseLerp(.12f, .4f, TimeOfDay); mixed.fogDensity += .012f * mist; mixed.fogColor = Color.Lerp(mixed.fogColor, new Color(.82f, .85f, .86f), .5f * mist); }
            TintMountains(basePreset);
            return mixed;
        }

        static PlaceLightingPreset Blend(PlaceLightingPreset a, PlaceLightingPreset b, float t) => PlaceLightingPreset.Lerp(a, b, Mathf.SmoothStep(0f, 1f, t));

        void TintMountains(PlaceLightingPreset light)
        {
            var root = FindAnyObjectByType<PlaceRoot>();
            if (root == null) return;
            var tint = Color.Lerp(light.fogColor, light.ambientSky, .55f);
            var block = new MaterialPropertyBlock();
            foreach (var renderer in root.GetComponentsInChildren<MeshRenderer>(true))
            {
                if (!renderer.name.StartsWith("Mountain silhouette")) continue;
                renderer.GetPropertyBlock(block);
                block.SetColor("_BaseColor", tint);
                renderer.SetPropertyBlock(block);
            }
        }
    }
}
