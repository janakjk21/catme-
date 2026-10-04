using System.Collections.Generic;

namespace CatMe.Pets
{
    /// <summary>
    /// Decides whether a pet vocalises and with which clip (docs/PET_AUDIO.md sections 3 to 5). No Unity calls, so it is tested
    /// with a seeded random and a fake clock.
    /// Rules: shuffle bag with no back-to-back repeats (and not the last two with 4+ clips); per-cue cooldown; at most one
    /// vocalisation every 6 s and 6 a minute for a cat, 4 for a dog; a chance per cue; pitch within the species base ±6%,
    /// volume ±12%, meows start 0 to 120 ms late. A forced (high-priority) cue skips chance, cooldown and budget.
    /// </summary>
    public sealed class PetVoicePicker
    {
        public struct Shot { public bool play; public int index; public float pitch, volume, delay; }

        public const float MinGap = 6f;
        public readonly Species species;
        public float BasePitch { get; set; }
        public int PerMinute => species == Species.Dog ? 4 : 6;

        readonly System.Random rng;
        readonly Dictionary<PetCue, float> lastCue = new Dictionary<PetCue, float>();
        readonly List<float> recent = new List<float>();
        readonly Dictionary<int, List<int>> bags = new Dictionary<int, List<int>>();
        readonly List<int> lastPlayed = new List<int>();
        float lastAny = float.NegativeInfinity;

        public PetVoicePicker(Species species, int seed = 0)
        {
            this.species = species;
            rng = seed == 0 ? new System.Random() : new System.Random(seed);
            BasePitch = species == Species.Dog ? 1.12f : .92f;   // medium dog; adult cat (the meows are a kitten pack)
        }

        public static float Cooldown(PetCue cue)
        {
            switch (cue)
            {
                case PetCue.Chirp: return 4f;
                case PetCue.Excited: return 15f;
                case PetCue.Hello: return 20f;
                case PetCue.Plead: return 25f;
                case PetCue.Ask: return 30f;
                default: return 0f;
            }
        }

        public static float Chance(PetCue cue)
        {
            switch (cue)
            {
                case PetCue.Hello: return .8f;
                case PetCue.Excited: return .6f;
                default: return 1f;
            }
        }

        public Shot Next(PetCue cue, float now, int clipCount, bool force = false)
        {
            var shot = new Shot();
            if (clipCount <= 0) return shot;
            if (!force)
            {
                if (now - lastAny < MinGap) return shot;
                if (lastCue.TryGetValue(cue, out float last) && now - last < Cooldown(cue)) return shot;
                recent.RemoveAll(t => now - t >= 60f);
                if (recent.Count >= PerMinute) return shot;
                if (rng.NextDouble() >= Chance(cue)) return shot;
            }
            lastAny = now; lastCue[cue] = now; recent.Add(now);
            shot.play = true;
            shot.index = Pick(clipCount);
            shot.pitch = BasePitch * (1f + Range(-.06f, .06f));
            shot.volume = 1f + Range(-.12f, .12f);
            shot.delay = species == Species.Cat ? Range(0f, .12f) : 0f;
            return shot;
        }

        float Range(float a, float b) => a + (b - a) * (float)rng.NextDouble();

        int Pick(int count)
        {
            if (count == 1) return 0;
            int avoid = count >= 4 ? 2 : 1;
            if (!bags.TryGetValue(count, out var bag) || bag.Count == 0)
            {
                bag = new List<int>(); for (int i = 0; i < count; i++) bag.Add(i);
                for (int i = bag.Count - 1; i > 0; i--) { int j = rng.Next(i + 1); (bag[i], bag[j]) = (bag[j], bag[i]); }
                bags[count] = bag;
            }
            // Take the first clip in the bag that wasn't among the last ones played.
            int at = 0;
            for (int k = 0; k < bag.Count; k++) if (!lastPlayed.Contains(bag[k]) || k == bag.Count - 1) { at = k; break; }
            if (lastPlayed.Count > 0 && bag[at] == lastPlayed[lastPlayed.Count - 1])
                for (int k = 0; k < bag.Count; k++) if (bag[k] != lastPlayed[lastPlayed.Count - 1]) { at = k; break; }
            int pick = bag[at]; bag.RemoveAt(at);
            lastPlayed.Add(pick); while (lastPlayed.Count > avoid) lastPlayed.RemoveAt(0);
            return pick;
        }
    }
}
