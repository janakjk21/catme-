using System.Collections.Generic;
using UnityEngine;

namespace CatMe.Pets
{
    /// <summary>What a pet sound is for. Each species answers with its own sound (a cat meows and purrs, a dog barks and pants).</summary>
    public enum PetCue { Hello, Excited, Happy, Eat, Settle, Chirp, Ask, Plead }

    /// <summary>
    /// The pet's voice (docs/PET_AUDIO.md). Recordings come in sets loaded from Resources/CatMeAudio/Cat and /Dog and grouped by
    /// the name before the last '-' (cat-hello-1, cat-hello-2 ... make the set "cat-hello"). Every vocal play goes through a
    /// <see cref="PetVoicePicker"/>: shuffle bag with no repeats, per-cue cooldowns, a per-pet budget, chance and pitch variation.
    /// Loops (purr, panting, eating) are not budgeted and come from <see cref="Clip"/>. Until a dog recording exists for a cue, a
    /// soft generated stand-in plays so a dog never meows. Respects the Sound setting.
    /// </summary>
    public static class PetSounds
    {
        static readonly Dictionary<string, AudioClip> cache = new Dictionary<string, AudioClip>();
        static Dictionary<string, List<AudioClip>> sets;
        static readonly PetVoicePicker catVoice = new PetVoicePicker(Species.Cat), dogVoice = new PetVoicePicker(Species.Dog);
        static AudioSource worldVoice;

        public static bool Enabled => PlayerPrefs.GetInt("CatMe.SoundEnabled", 1) == 1;
        /// <summary>True when a vocalisation should be quieter (inside a room, or in rain); set by the rooms and the weather.</summary>
        public static bool Indoors { get; set; }
        public static bool Raining { get; set; }

        /// <summary>Clip sets by prefix, loaded once ("cat-hello" holds cat-hello-1..3).</summary>
        public static IReadOnlyDictionary<string, List<AudioClip>> Sets
        {
            get
            {
                if (sets != null) return sets;
                sets = new Dictionary<string, List<AudioClip>>();
                foreach (string folder in new[] { "CatMeAudio/Cat", "CatMeAudio/Dog" })
                    foreach (var clip in Resources.LoadAll<AudioClip>(folder))
                    {
                        string key = SetName(clip.name);
                        if (!sets.TryGetValue(key, out var list)) sets[key] = list = new List<AudioClip>();
                        list.Add(clip);
                    }
                if (Debug.isDebugBuild)
                {
                    var parts = new List<string>(); foreach (var kv in sets) parts.Add(kv.Key + "=" + kv.Value.Count);
                    Debug.Log("[WornTag][Audio] pet sound sets: " + (parts.Count == 0 ? "none found in Resources/CatMeAudio/Cat or /Dog" : string.Join(", ", parts)));
                }
                return sets;
            }
        }

        /// <summary>"cat-hello-2" becomes "cat-hello"; a name without a number stays as it is.</summary>
        public static string SetName(string clipName)
        {
            if (string.IsNullOrEmpty(clipName)) return "";
            int dash = clipName.LastIndexOf('-');
            if (dash <= 0) return clipName;
            string tail = clipName.Substring(dash + 1);
            foreach (char c in tail) if (!char.IsDigit(c)) return clipName;
            return tail.Length == 0 ? clipName : clipName.Substring(0, dash);
        }

        /// <summary>The set a vocal cue draws from, first non-empty wins.</summary>
        public static string[] SetsFor(Species species, PetCue cue)
        {
            if (species == Species.Dog)
                switch (cue)
                {
                    case PetCue.Excited: case PetCue.Ask: return new[] { "dog-barkdouble", "dog-bark" };
                    case PetCue.Chirp: return new string[0];                 // a dog doesn't chirp; the tap gets hearts only
                    default: return new[] { "dog-bark" };
                }
            switch (cue)
            {
                case PetCue.Chirp: return new[] { "cat-chirp", "cat-helloshort" };
                case PetCue.Ask: case PetCue.Excited: return new[] { "cat-ask", "cat-hello" };
                case PetCue.Plead: return new[] { "cat-plead", "cat-ask" };
                default: return new[] { "cat-hello", "cat-helloshort" };
            }
        }

        /// <summary>Loops and one-off non-vocal sounds (purr, panting, eating, sighs); also the fallback for a vocal cue with no set.</summary>
        public static AudioClip Clip(Species species, PetCue cue)
        {
            string key = species + "/" + cue;
            if (cache.TryGetValue(key, out var cached) && cached != null) return cached;
            AudioClip clip = species == Species.Dog ? DogClip(cue) : CatClip(cue);
            cache[key] = clip;
            return clip;
        }

        static bool IsVocal(PetCue cue) => cue == PetCue.Hello || cue == PetCue.Excited || cue == PetCue.Chirp || cue == PetCue.Ask || cue == PetCue.Plead;

        /// <summary>Plays through the pet's own audio source (follows the pet). Vocal cues may be skipped by the cooldown rules.</summary>
        public static void Play(AudioSource source, Species species, PetCue cue, float volume = .7f) => Say(source, species, cue, volume, false);

        /// <summary>Plays at a world position through one shared pet voice (moved there), never a new object per sound.</summary>
        public static void PlayAt(Vector3 position, Species species, PetCue cue, float volume = .7f) => PlayAt(position, species, cue, volume, false);

        /// <param name="force">High priority (the reveal, the call answered): ignores chance, cooldown and budget, never the Sound setting.</param>
        public static void PlayAt(Vector3 position, Species species, PetCue cue, float volume, bool force)
        {
            if (!Enabled) return;
            if (worldVoice == null)
            {
                var go = new GameObject("Pet voice"); Object.DontDestroyOnLoad(go);
                worldVoice = go.AddComponent<AudioSource>(); worldVoice.playOnAwake = false;
                worldVoice.spatialBlend = 1f; worldVoice.minDistance = 1.5f; worldVoice.maxDistance = 20f; worldVoice.rolloffMode = AudioRolloffMode.Logarithmic;
            }
            worldVoice.transform.position = position;
            Say(worldVoice, species, cue, volume, force);
        }

        static void Say(AudioSource source, Species species, PetCue cue, float volume, bool force)
        {
            if (!Enabled || source == null || source.mute) return;
            if (!IsVocal(cue))
            {
                var loop = Clip(species, cue);
                if (loop != null) source.PlayOneShot(loop, volume * (Indoors ? .85f : 1f));
                return;
            }
            if (Raining && cue == PetCue.Chirp) return;
            var picker = species == Species.Dog ? dogVoice : catVoice;
            List<AudioClip> set = null;
            foreach (string name in SetsFor(species, cue)) if (Sets.TryGetValue(name, out var found) && found.Count > 0) { set = found; break; }
            int count = set != null ? set.Count : 1;
            if (set == null && species == Species.Cat && cue == PetCue.Chirp) return;   // no chirp recorded: stay quiet rather than meow
            if (set == null && species == Species.Dog && cue == PetCue.Chirp) return;
            var shot = picker.Next(cue, Time.unscaledTime, count, force);
            if (!shot.play) return;
            AudioClip clip = set != null ? set[shot.index] : Clip(species, cue);
            if (clip == null) return;
            source.pitch = shot.pitch;
            float v = volume * shot.volume * (Indoors ? .85f : 1f) * (Raining ? .8f : 1f);
            if (shot.delay > 0f && source.gameObject.activeInHierarchy) { source.clip = clip; source.volume = Mathf.Clamp01(v); source.PlayDelayed(shot.delay); }
            else source.PlayOneShot(clip, v);
        }

        static AudioClip Load(params string[] names)
        {
            foreach (var n in names) { var c = Resources.Load<AudioClip>("CatMeAudio/" + n); if (c != null) return c; }
            return null;
        }

        static AudioClip CatClip(PetCue cue)
        {
            switch (cue)
            {
                case PetCue.Happy: case PetCue.Settle: return Load("cat-purr");
                case PetCue.Eat: return Load("cat-eat", "cat_mewfood", "cat-meow");
                default: return Load("Cat/cat-hello-1", "cat-meow");
            }
        }

        static AudioClip DogClip(PetCue cue)
        {
            switch (cue)
            {
                case PetCue.Happy: return Load("dog-pant") ?? Pant();
                case PetCue.Settle: return Load("dog-sigh") ?? Sigh();
                case PetCue.Eat: return Load("dog-eat") ?? Chomp();
                case PetCue.Excited: case PetCue.Ask: return Load("Dog/dog-barkdouble-1", "dog-bark-excited", "dog-bark") ?? Woof(2);
                default: return Load("Dog/dog-bark-1", "dog-bark") ?? Woof(1);
            }
        }

        // ---- generated stand-ins (replace with recordings in Resources/CatMeAudio) ----------------------------------------

        const int Rate = 22050;

        // A short, soft "woof": a falling voiced tone with a breathy edge; two for an excited bark.
        static AudioClip Woof(int count)
        {
            float one = .2f, gap = .09f;
            var data = new float[(int)(Rate * (count * (one + gap) + .05f))];
            var rng = new System.Random(5);
            for (int b = 0; b < count; b++)
            {
                int start = (int)(Rate * b * (one + gap)); float phase = 0f;
                for (int i = 0; i < Rate * one && start + i < data.Length; i++)
                {
                    float t = i / (float)Rate, p = t / one;
                    float pitch = Mathf.Lerp(330f, 180f, p) * (b == 1 ? 1.08f : 1f);
                    phase += 2f * Mathf.PI * pitch / Rate;
                    float voice = Mathf.Sin(phase) * .6f + Mathf.Sin(phase * 2f) * .25f + Mathf.Sin(phase * 3f) * .12f;
                    float breath = (float)(rng.NextDouble() * 2.0 - 1.0) * .18f;
                    float env = Mathf.Min(1f, p * 25f) * Mathf.Pow(1f - p, 1.6f);
                    data[start + i] += (voice + breath) * env * .55f;
                }
            }
            return Make("dog-woof-generated", data);
        }

        // Happy panting: soft breathy pulses, about three a second, loopable.
        static AudioClip Pant()
        {
            var data = new float[Rate * 2]; var rng = new System.Random(9); float low = 0f;
            for (int i = 0; i < data.Length; i++)
            {
                float t = i / (float)Rate;
                float pulse = Mathf.Pow(Mathf.Max(0f, Mathf.Sin(t * Mathf.PI * 2f * 3f)), 3f);
                float n = (float)(rng.NextDouble() * 2.0 - 1.0); low += (n - low) * .3f;
                data[i] = low * pulse * .35f;
            }
            return Make("dog-pant-generated", data);
        }

        // A content sigh when settling down: one long, falling breath.
        static AudioClip Sigh()
        {
            var data = new float[(int)(Rate * .9f)]; var rng = new System.Random(4); float low = 0f;
            for (int i = 0; i < data.Length; i++)
            {
                float p = i / (float)data.Length;
                float n = (float)(rng.NextDouble() * 2.0 - 1.0); low += (n - low) * Mathf.Lerp(.25f, .06f, p);
                data[i] = low * Mathf.Sin(p * Mathf.PI) * .4f;
            }
            return Make("dog-sigh-generated", data);
        }

        // Eating: quick crunchy clicks.
        static AudioClip Chomp()
        {
            var data = new float[(int)(Rate * 1.2f)]; var rng = new System.Random(7);
            for (int c = 0; c < 7; c++)
            {
                int at = (int)(Rate * (.05f + c * .16f + (float)rng.NextDouble() * .04f));
                for (int k = 0; k < 500 && at + k < data.Length; k++) data[at + k] += (float)(rng.NextDouble() * 2.0 - 1.0) * .4f * Mathf.Exp(-k / 90f);
            }
            return Make("dog-eat-generated", data);
        }

        static AudioClip Make(string name, float[] data)
        {
            var clip = AudioClip.Create(name, data.Length, 1, Rate, false); clip.SetData(data, 0); return clip;
        }
    }
}
