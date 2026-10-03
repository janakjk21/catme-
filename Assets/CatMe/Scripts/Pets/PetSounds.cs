using System.Collections.Generic;
using UnityEngine;

namespace CatMe.Pets
{
    /// <summary>What a pet sound is for. Each species answers with its own sound (a cat meows and purrs, a dog barks and pants).</summary>
    public enum PetCue { Hello, Excited, Happy, Eat, Settle }

    /// <summary>
    /// One place that picks the right sound for the active species. Recordings in Resources/CatMeAudio win; until a dog recording
    /// is added, a soft generated stand-in plays so a dog is never silent or meowing. Respects the Sound setting.
    ///
    /// Files it looks for (first found wins):
    ///   cat: Hello cat-meow, Excited cat-meow / cat_mewfood, Happy cat-purr, Eat cat-eat / cat_mewfood, Settle cat-purr
    ///   dog: Hello dog-bark, Excited dog-bark-excited / dog-bark, Happy dog-pant, Eat dog-eat, Settle dog-sigh
    /// </summary>
    public static class PetSounds
    {
        static readonly Dictionary<string, AudioClip> cache = new Dictionary<string, AudioClip>();

        public static bool Enabled => PlayerPrefs.GetInt("CatMe.SoundEnabled", 1) == 1;

        public static AudioClip Clip(Species species, PetCue cue)
        {
            string key = species + "/" + cue;
            if (cache.TryGetValue(key, out var cached) && cached != null) return cached;
            AudioClip clip = species == Species.Dog ? DogClip(cue) : CatClip(cue);
            cache[key] = clip;
            return clip;
        }

        /// <summary>Plays once through the pet's own audio source (follows the pet).</summary>
        public static void Play(AudioSource source, Species species, PetCue cue, float volume = .7f)
        {
            if (!Enabled || source == null || source.mute) return;
            var clip = Clip(species, cue);
            if (clip != null) source.PlayOneShot(clip, volume);
        }

        /// <summary>Plays once at a world position (when there is no source to hand).</summary>
        public static void PlayAt(Vector3 position, Species species, PetCue cue, float volume = .7f)
        {
            if (!Enabled) return;
            var clip = Clip(species, cue);
            if (clip != null) AudioSource.PlayClipAtPoint(clip, position, volume);
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
                case PetCue.Excited: return Load("cat-meow", "cat_mewfood");
                default: return Load("cat-meow", "cat_mewfood");
            }
        }

        static AudioClip DogClip(PetCue cue)
        {
            switch (cue)
            {
                case PetCue.Happy: return Load("dog-pant") ?? Pant();
                case PetCue.Settle: return Load("dog-sigh") ?? Sigh();
                case PetCue.Eat: return Load("dog-eat") ?? Chomp();
                case PetCue.Excited: return Load("dog-bark-excited", "dog-bark") ?? Woof(2);
                default: return Load("dog-bark") ?? Woof(1);
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
