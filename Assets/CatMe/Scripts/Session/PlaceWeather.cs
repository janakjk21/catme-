using UnityEngine;

namespace CatMe.Session
{
    public enum WeatherState { Dry, RainStarting, Raining, Clearing }

    /// <summary>Local rain for the active place. Rain collides with roofs, follows the view, and never counts paused time as shelter.</summary>
    public sealed class PlaceWeather : MonoBehaviour
    {
        const int MaxParticles = 420;
        // Phone budget for the Rain Particles pack: drops, and splashes/rings spawned where drops die.
        const int PackMaxDrops = 600, PackMaxSplashes = 300;
        const float PackFullRate = 300f;
        ParticleSystem rain;
        ParticleSystem packRain;
        Transform view;
        float intensity, targetIntensity, wetness;
        float shelteredFor;
        float clearingFor;
        PlaceRoot place;
        public WeatherState State { get; private set; } = WeatherState.Dry;
        public event System.Action<WeatherState> StateChanged;
        public event System.Action ShelterCleared;

        public void Initialize(Transform camera)
        {
            view = camera;
            var go = new GameObject("Place rain");
            go.transform.SetParent(transform, false);
            rain = go.AddComponent<ParticleSystem>();
            var main = rain.main; main.loop = true; main.playOnAwake = false; main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.maxParticles = MaxParticles; main.startLifetime = 1.15f; main.startSpeed = 14f; main.startSize = .035f;
            main.startColor = new Color(.78f, .84f, .91f, .55f); main.gravityModifier = .08f;
            var emission = rain.emission; emission.rateOverTime = 0f;
            var shape = rain.shape; shape.shapeType = ParticleSystemShapeType.Box; shape.scale = new Vector3(20f, 1f, 22f);
            // Drops die on roofs, ground and bodies, so rain stops at the shelter roof.
            var collision = rain.collision; collision.enabled = true; collision.type = ParticleSystemCollisionType.World;
            collision.mode = ParticleSystemCollisionMode.Collision3D; collision.quality = ParticleSystemCollisionQuality.Low;
            collision.lifetimeLoss = 1f; collision.bounce = 0f; collision.dampen = 1f; collision.radiusScale = .5f; collision.maxCollisionShapes = 64;
            var renderer = go.GetComponent<ParticleSystemRenderer>();
            Shader shader = Shader.Find("Universal Render Pipeline/Particles/Unlit") ?? Shader.Find("Sprites/Default");
            if (shader != null) { renderer.material = new Material(shader); renderer.material.color = new Color(.74f, .81f, .9f, .5f); }
            renderer.renderMode = ParticleSystemRenderMode.Stretch; renderer.velocityScale = .025f;
        }

        public void EnterPlace(PlaceRoot root)
        {
            place = root; shelteredFor = clearingFor = 0f;
            SetUpPackRain(root.rainEffect);
            switch (root.startWeather)
            {
                case "Raining": Set(WeatherState.Raining, 1f); break;
                case "RainRamp": Set(WeatherState.RainStarting, 0f); break;
                default: Set(WeatherState.Dry, 0f); break;
            }
        }

        public void Restore(string saved)
        {
            if (System.Enum.TryParse(saved, out WeatherState state) && state != WeatherState.Dry && place != null && place.startWeather == "Raining")
                Set(state, state == WeatherState.Dry ? 0f : state == WeatherState.Clearing ? .5f : 1f);
        }

        void Set(WeatherState state, float target)
        {
            bool changed = State != state;
            State = state; targetIntensity = target;
            if (changed) StateChanged?.Invoke(state);
        }

        /// <summary>Advance weather. Uses unscaled-pause-safe delta, so a paused game never earns shelter time.</summary>
        public void Tick(Vector3 owner, Vector3 cat)
        {
            if (rain == null || place == null || view == null) return;
            float dt = Time.deltaTime; // zero while paused (timeScale 0)
            bool reduced = PlayerPrefs.GetInt("CatMe.ReduceMotion", 0) == 1;

            if (State == WeatherState.RainStarting && place.rainRampToZ > place.rainRampFromZ)
            {
                targetIntensity = Mathf.InverseLerp(place.rainRampFromZ, place.rainRampToZ, owner.z);
                if (targetIntensity > .98f) Set(WeatherState.Raining, 1f);
            }
            if (State == WeatherState.Raining && place.shelterVolume != null)
            {
                bool both = place.shelterVolume.bounds.Contains(owner) && place.shelterVolume.bounds.Contains(cat);
                shelteredFor = both ? shelteredFor + dt : 0f;
                if (shelteredFor >= place.shelterHoldSeconds) { Set(WeatherState.Clearing, 0f); clearingFor = 0f; ShelterCleared?.Invoke(); }
            }
            if (State == WeatherState.Clearing)
            {
                clearingFor += dt;
                if (clearingFor > 20f) Set(WeatherState.Dry, 0f);
            }

            intensity = Mathf.MoveTowards(intensity, Mathf.Max(targetIntensity, DayRain), dt / 6f);
            wetness = Mathf.MoveTowards(wetness, State == WeatherState.Dry ? Mathf.Min(wetness, .4f) : intensity, dt / 20f);
            if (State == WeatherState.Dry) wetness = Mathf.MoveTowards(wetness, 0f, dt / 40f);
            Shader.SetGlobalFloat("_CatMeWetness", wetness);

            float strength = reduced || Suppressed ? 0f : intensity;
            if (packRain != null)
            {
                Drive(rain, 0f);
                Drive(packRain, PackFullRate * strength);
            }
            else Drive(rain, 220f * strength);
        }

        // Emits at the given rate above the view; stops emitting (letting falling drops finish) when the rate is near zero.
        void Drive(ParticleSystem system, float rate)
        {
            if (system == null) return;
            if (rate > 1f && !system.gameObject.activeSelf) system.gameObject.SetActive(true);
            var emission = system.emission;
            emission.rateOverTime = rate;
            if (rate > 1f && !system.isPlaying) system.Play();
            else if (rate <= 1f && system.isPlaying) system.Stop(true, ParticleSystemStopBehavior.StopEmitting);
            system.transform.position = view.position + Vector3.up * 7f;
        }

        // The pack's emitter shoots along its local -Z; turn it so the drops fall. Drops collide with the world (roofs stop them),
        // and its two child systems are death sub-emitters (splash and ring where a drop hits).
        void SetUpPackRain(ParticleSystem pack)
        {
            if (packRain != null && packRain != pack) packRain.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            packRain = pack;
            if (packRain == null) return;
            packRain.transform.rotation = Quaternion.FromToRotation(Vector3.forward, Vector3.up);
            var main = packRain.main; main.playOnAwake = false; main.simulationSpace = ParticleSystemSimulationSpace.World; main.maxParticles = PackMaxDrops;
            // A drop dies where it hits, so its splash and ring appear on the ground or roof, never in the air.
            var collision = packRain.collision; collision.enabled = true; collision.type = ParticleSystemCollisionType.World;
            collision.mode = ParticleSystemCollisionMode.Collision3D; collision.quality = ParticleSystemCollisionQuality.Low;
            collision.lifetimeLoss = 1f; collision.bounce = 0f; collision.maxCollisionShapes = 64;
            foreach (var child in packRain.GetComponentsInChildren<ParticleSystem>(true))
            {
                if (child == packRain) continue;
                var childMain = child.main; childMain.maxParticles = PackMaxSplashes; childMain.simulationSpace = ParticleSystemSimulationSpace.World;
            }
            var emission = packRain.emission; emission.rateOverTime = 0f;
        }

        public float Intensity => intensity;
        public bool Suppressed { get; set; }
        /// <summary>Rain from the random day type (0..1), merged with the story rain near the tent.</summary>
        public float DayRain { get; set; }
        /// <summary>Stops the rain for good in this place (used when the tent shower passes).</summary>
        public void EndRain() { Set(WeatherState.Clearing, 0f); clearingFor = 0f; }
    }
}
