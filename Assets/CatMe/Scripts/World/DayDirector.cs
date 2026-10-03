using CatMe.Session;
using UnityEngine;

namespace CatMe.World
{
    public enum DayType { Clear, PartlyCloudy, MistyMorning, Showery, Rainy }

    /// <summary>
    /// Picks a day type per session and moves the world through time and weather gradually: afternoon light slides to sunset and blue hour,
    /// clouds come and go, showers pass, wind rises before rain, lanterns and windows light at dusk. Saved so a relaunch resumes the same day.
    /// Needs the existing PlaceLighting (look) and PlaceWeather (rain). See docs/production/05_LIVING_WORLD_AND_PREMIUM_FEEL.md R-WX.
    /// </summary>
    public sealed class DayDirector : MonoBehaviour
    {
        const string TypeKey = "CatMe.World.DayType", TimeKey = "CatMe.World.Time";
        const float CycleSeconds = 1080f; // afternoon -> blue hour over about 18 minutes of play

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        static void DefaultWind() { Shader.SetGlobalFloat("_CatMeWindScale", 1f); }

        SessionDirector director; PlaceLighting lighting; PlaceWeather weather;
        AudioSource windAudio; AudioClip windClip;
        public DayType Type { get; private set; }
        public float TimeOfDay { get; private set; }        // 0 afternoon, .5 sunset, 1 blue hour
        public float Cloudiness { get; private set; }
        public bool Night => TimeOfDay > .72f;
        public bool Suspended { get; set; }                  // true inside rooms, which set their own lighting
        float cloudTarget, showerAt, showerUntil, applyAt, saveAt, driftSeed;
        bool nightApplied;
        ParticleSystem fireflies;

        public void Initialize(SessionDirector sessionDirector, PlaceLighting placeLighting, PlaceWeather placeWeather)
        {
            director = sessionDirector; lighting = placeLighting; weather = placeWeather;
            driftSeed = Random.value * 100f;
            if (PlayerPrefs.HasKey(TypeKey) && PlayerPrefs.HasKey(TimeKey)) { Type = (DayType)Mathf.Clamp(PlayerPrefs.GetInt(TypeKey), 0, 4); TimeOfDay = Mathf.Clamp01(PlayerPrefs.GetFloat(TimeKey)); }
            else NewDay();
            windClip = Resources.Load<AudioClip>("CatMeAudio/wind-low");
            if (windClip != null) { windAudio = gameObject.AddComponent<AudioSource>(); windAudio.clip = windClip; windAudio.loop = true; windAudio.spatialBlend = 0f; windAudio.volume = 0f; windAudio.Play(); }
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
            float r = Random.value;
            Type = r < .40f ? DayType.Clear : r < .65f ? DayType.PartlyCloudy : r < .75f ? DayType.MistyMorning : r < .90f ? DayType.Showery : DayType.Rainy;
            TimeOfDay = Random.Range(0f, .4f);
            Save();
            Debug.Log("[CatMe][World] New day: " + Type + ", starting at time " + TimeOfDay.ToString("F2"));
        }

        void Save() { PlayerPrefs.SetInt(TypeKey, (int)Type); PlayerPrefs.SetFloat(TimeKey, TimeOfDay); PlayerPrefs.Save(); }

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
            if (!Suspended) TimeOfDay = Mathf.Min(1f, TimeOfDay + dt / CycleSeconds);

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
            if (Time.time >= saveAt) { saveAt = Time.time + 20f; Save(); }
        }

        PlaceLightingPreset Compose()
        {
            var day = PlaceLightingPreset.Get("GoldenAfternoon"); var sunset = PlaceLightingPreset.Get("Sunset"); var blue = PlaceLightingPreset.Get("BlueHour");
            PlaceLightingPreset basePreset = TimeOfDay < .5f
                ? PlaceLightingPreset.Lerp(day, sunset, Mathf.SmoothStep(0f, 1f, TimeOfDay / .5f))
                : PlaceLightingPreset.Lerp(sunset, blue, Mathf.SmoothStep(0f, 1f, (TimeOfDay - .5f) / .5f));
            var mixed = PlaceLightingPreset.Lerp(basePreset, PlaceLightingPreset.Get("Overcast"), Cloudiness * .75f);
            if (Type == DayType.MistyMorning) { mixed.fogDensity += .012f * (1f - TimeOfDay); mixed.fogColor = Color.Lerp(mixed.fogColor, new Color(.82f, .85f, .86f), .5f * (1f - TimeOfDay)); }
            return mixed;
        }
    }
}
