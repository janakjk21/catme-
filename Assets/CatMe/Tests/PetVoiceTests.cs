using System;
using System.Linq;
using System.Reflection;
using NUnit.Framework;

namespace CatMe.Tests
{
    /// <summary>F1 / docs/PET_AUDIO.md section 7: no repeats, cooldowns, budget, pitch range, and clip set names.</summary>
    public sealed class PetVoiceTests
    {
        static Assembly App => AppDomain.CurrentDomain.GetAssemblies().First(a => a.GetName().Name == "Assembly-CSharp");
        static Type Picker => App.GetType("CatMe.Pets.PetVoicePicker", true);
        static Type Species => App.GetType("CatMe.Pets.Species", true);
        static Type Cue => App.GetType("CatMe.Pets.PetCue", true);
        static object Make(string species, int seed) => Activator.CreateInstance(Picker, Enum.Parse(Species, species), seed);
        static object Next(object p, string cue, float now, int count, bool force = false) => Picker.GetMethod("Next").Invoke(p, new object[] { Enum.Parse(Cue, cue), now, count, force });
        static T F<T>(object shot, string field) => (T)shot.GetType().GetField(field).GetValue(shot);

        [Test]
        public void TwoHundredPicksNeverRepeatAndUseEveryClip()
        {
            var p = Make("Cat", 7);
            var counts = new int[5]; int last = -1;
            for (int i = 0; i < 200; i++)
            {
                var shot = Next(p, "Hello", i * 100f, 5, true);
                int index = F<int>(shot, "index");
                Assert.That(index, Is.Not.EqualTo(last), "back-to-back repeat at " + i);
                counts[index]++; last = index;
            }
            Assert.That(counts.All(c => c >= 30), Is.True, string.Join(",", counts));
        }

        [Test]
        public void CooldownAndBudgetCapBursts()
        {
            var p = Make("Cat", 3);
            int played = 0;
            for (int i = 0; i < 20; i++) if (F<bool>(Next(p, "Chirp", i * .5f, 3), "play")) played++;
            Assert.That(played, Is.EqualTo(2), "one every 6 s: at 0 s and 6 s within 10 s");
            var q = Make("Cat", 3);
            Assert.That(F<bool>(Next(q, "Ask", 0f, 3), "play"), Is.True);
            Assert.That(F<bool>(Next(q, "Ask", 2f, 3), "play"), Is.False, "two triggers 2 s apart: one sound");
            var dog = Make("Dog", 5); played = 0;
            for (int i = 0; i < 20; i++) if (F<bool>(Next(dog, "Ask", i * 6.5f, 2), "play")) played++;
            Assert.That(played, Is.LessThanOrEqualTo(4 * 3), "at most 4 a minute for a dog");
        }

        [Test]
        public void PitchStaysWithinSixPercentOfTheBase()
        {
            foreach (string species in new[] { "Cat", "Dog" })
            {
                var p = Make(species, 11);
                float basePitch = (float)Picker.GetProperty("BasePitch").GetValue(p);
                for (int i = 0; i < 100; i++)
                {
                    float pitch = F<float>(Next(p, "Hello", i * 100f, 3, true), "pitch");
                    Assert.That(pitch, Is.InRange(basePitch * .94f - 1e-4f, basePitch * 1.06f + 1e-4f));
                }
            }
        }

        [Test]
        public void ClipNamesGroupIntoSets()
        {
            var setName = App.GetType("CatMe.Pets.PetSounds", true).GetMethod("SetName");
            Assert.That(setName.Invoke(null, new object[] { "cat-hello-2" }), Is.EqualTo("cat-hello"));
            Assert.That(setName.Invoke(null, new object[] { "dog-barkdouble-1" }), Is.EqualTo("dog-barkdouble"));
            Assert.That(setName.Invoke(null, new object[] { "cat-purr" }), Is.EqualTo("cat-purr"));
        }
    }
}
