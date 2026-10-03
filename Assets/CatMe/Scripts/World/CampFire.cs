using UnityEngine;

namespace CatMe.World
{
    /// <summary>
    /// A small fire inside the tent: flames and rising sparks (code-built particles), a warm flickering light and a soft crackle.
    /// Placed by PlaceSceneBuilder on the "Tent fire" object; needs no art assets. Respects Sound and Reduce motion.
    /// </summary>
    public sealed class CampFire : MonoBehaviour
    {
        public float lightIntensity = 1.6f, lightRange = 5.5f;
        Light glow; AudioSource crackle; ParticleSystem flames, sparks;
        float seed;

        void Start()
        {
            seed = Random.value * 50f;
            var lightGo = new GameObject("Fire light"); lightGo.transform.SetParent(transform, false); lightGo.transform.localPosition = Vector3.up * .45f;
            glow = lightGo.AddComponent<Light>(); glow.type = LightType.Point; glow.range = lightRange; glow.intensity = lightIntensity;
            glow.color = new Color(1f, .58f, .26f); glow.shadows = LightShadows.None;
            var glowTexture = Glow();
            flames = Particles("Flames", glowTexture, 26, new Color(1f, .62f, .22f, .9f), new Color(1f, .25f, .05f, 0f), .9f, .55f, .32f, .12f);
            sparks = Particles("Sparks", glowTexture, 14, new Color(1f, .82f, .45f, 1f), new Color(1f, .4f, .1f, 0f), 1.6f, 1.1f, .05f, .2f);
            crackle = gameObject.AddComponent<AudioSource>();
            crackle.clip = CrackleClip(); crackle.loop = true; crackle.spatialBlend = 1f; crackle.minDistance = 1.2f; crackle.maxDistance = 9f;
            crackle.rolloffMode = AudioRolloffMode.Linear; crackle.volume = 0f; crackle.Play();
        }

        void Update()
        {
            bool reduced = PlayerPrefs.GetInt("CatMe.ReduceMotion", 0) == 1;
            float flicker = reduced ? 1f : .82f + .18f * Mathf.PerlinNoise(Time.time * 6f, seed) + .06f * Mathf.Sin(Time.time * 23f + seed);
            if (glow != null) glow.intensity = lightIntensity * flicker;
            bool sound = PlayerPrefs.GetInt("CatMe.SoundEnabled", 1) == 1;
            if (crackle != null) crackle.volume = Mathf.MoveTowards(crackle.volume, sound ? .32f : 0f, Time.unscaledDeltaTime * .5f);
            if (sparks != null) { var emission = sparks.emission; emission.rateOverTime = reduced ? 3f : 14f; }
        }

        ParticleSystem Particles(string name, Texture2D texture, float rate, Color start, Color end, float lifetime, float speed, float size, float spread)
        {
            var go = new GameObject(name); go.transform.SetParent(transform, false); go.transform.localPosition = Vector3.up * .08f;
            var ps = go.AddComponent<ParticleSystem>();
            var main = ps.main; main.loop = true; main.startLifetime = lifetime; main.startSpeed = speed; main.startSize = new ParticleSystem.MinMaxCurve(size * .6f, size);
            main.maxParticles = 60; main.simulationSpace = ParticleSystemSimulationSpace.World; main.gravityModifier = -.05f; main.startColor = start;
            var emission = ps.emission; emission.rateOverTime = rate;
            var shape = ps.shape; shape.shapeType = ParticleSystemShapeType.Cone; shape.angle = 12f; shape.radius = spread;
            var colour = ps.colorOverLifetime; colour.enabled = true;
            var gradient = new Gradient();
            gradient.SetKeys(new[] { new GradientColorKey(new Color(start.r, start.g, start.b), 0f), new GradientColorKey(new Color(end.r, end.g, end.b), 1f) },
                             new[] { new GradientAlphaKey(0f, 0f), new GradientAlphaKey(start.a, .15f), new GradientAlphaKey(0f, 1f) });
            colour.color = gradient;
            var sizeOverLife = ps.sizeOverLifetime; sizeOverLife.enabled = true; sizeOverLife.size = new ParticleSystem.MinMaxCurve(1f, AnimationCurve.Linear(0f, 1f, 1f, .2f));
            var noise = ps.noise; noise.enabled = true; noise.strength = .25f; noise.frequency = 1.4f;
            var renderer = go.GetComponent<ParticleSystemRenderer>();
            Shader shader = Shader.Find("Universal Render Pipeline/Particles/Unlit") ?? Shader.Find("Sprites/Default");
            var material = new Material(shader); material.mainTexture = texture;
            if (material.HasProperty("_BaseMap")) material.SetTexture("_BaseMap", texture);
            // Additive blending, like the fireflies: bright where flames overlap.
            if (material.HasProperty("_Surface")) { material.SetFloat("_Surface", 1f); material.SetFloat("_Blend", 2f); material.EnableKeyword("_SURFACE_TYPE_TRANSPARENT"); material.renderQueue = 3000; }
            renderer.material = material;
            ps.Play();
            return ps;
        }

        static Texture2D Glow()
        {
            const int n = 32; var texture = new Texture2D(n, n, TextureFormat.RGBA32, false);
            for (int y = 0; y < n; y++) for (int x = 0; x < n; x++)
            {
                float d = Vector2.Distance(new Vector2(x, y), new Vector2(n / 2f, n / 2f)) / (n / 2f);
                float a = Mathf.Clamp01(1f - d); texture.SetPixel(x, y, new Color(1f, 1f, 1f, a * a));
            }
            texture.Apply(); return texture;
        }

        // A soft bed of low noise with random small pops, looped without a click. Stand-in until a licensed recording is added.
        static AudioClip CrackleClip()
        {
            const int rate = 22050, seconds = 5; var data = new float[rate * seconds]; var rng = new System.Random(3); float low = 0f;
            for (int i = 0; i < data.Length; i++)
            {
                float n = (float)(rng.NextDouble() * 2.0 - 1.0);
                low += (n - low) * .04f;
                data[i] = low * .35f;
            }
            for (int pop = 0; pop < 70; pop++)
            {
                int at = rng.Next(0, data.Length - 400); float amp = .25f + (float)rng.NextDouble() * .5f; int length = 60 + rng.Next(0, 260);
                for (int k = 0; k < length; k++) data[at + k] += (float)(rng.NextDouble() * 2.0 - 1.0) * amp * Mathf.Exp(-k / (length * .25f));
            }
            int fade = rate / 10;
            for (int i = 0; i < fade; i++) { float a = i / (float)fade; data[i] = data[i] * a + data[data.Length - fade + i] * (1f - a); }
            var clip = AudioClip.Create("fire-crackle", data.Length, 1, rate, false); clip.SetData(data, 0); return clip;
        }
    }
}
