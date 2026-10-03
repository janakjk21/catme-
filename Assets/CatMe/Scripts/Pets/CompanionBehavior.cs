using UnityEngine;

namespace CatMe.Pets
{
    /// <summary>Things a companion can say or react to. Each behaviour owns its own phrasing so a dog does not read as a re-skinned cat.</summary>
    public enum Moment { Rain, FollowMe, WaitUp, ReachedTent, Bench, GoHome, Hungry, VisitorNear, Butterfly, Petted, Arrived }

    /// <summary>
    /// How a species moves with its owner and what it says. The garden reads these numbers each frame; leaders and visitors ask it for lines and sounds.
    /// </summary>
    public abstract class CompanionBehavior
    {
        public abstract Species Species { get; }
        /// <summary>Sideways offset from the owner while following, in metres.</summary>
        public abstract float Lateral { get; }
        /// <summary>How far ahead of the owner it likes to walk.</summary>
        public abstract float Ahead { get; }
        /// <summary>Multiplies walking speed when closing the gap.</summary>
        public abstract float Pace { get; }
        /// <summary>Distance from the owner at which it starts hurrying back.</summary>
        public abstract float Leash { get; }
        /// <summary>How far from the owner it will go to look at something.</summary>
        public abstract float CuriosityRange { get; }
        /// <summary>1 = no preference; below 1 makes riverside and pond points look closer than they are.</summary>
        public abstract float WaterPull { get; }
        /// <summary>Radius of the little circle it runs around a butterfly.</summary>
        public abstract float PlayRadius { get; }
        public abstract float SeesVisitorAt { get; }
        protected abstract string[] Pool(Moment m);

        string[] lastPool; int lastIndex = -1;
        /// <summary>Returns a line for the moment that differs from the previous line said for it.</summary>
        public string Line(Moment m)
        {
            var pool = Pool(m);
            if (pool == null || pool.Length == 0) return null;
            int i = Random.Range(0, pool.Length);
            if (pool.Length > 1 && ReferenceEquals(pool, lastPool) && i == lastIndex) i = (i + 1 + Random.Range(0, pool.Length - 1)) % pool.Length;
            lastPool = pool; lastIndex = i;
            return pool[i];
        }

        /// <summary>Species sound cue; plays nothing if the clip is not in the project.</summary>
        public void Sound(AudioSource source, string cue, float volume = .6f)
        {
            if (source == null || source.mute) return;
            var clip = Resources.Load<AudioClip>("CatMeAudio/" + (Species == Species.Dog ? "dog-" : "cat-") + cue);
            if (clip != null) source.PlayOneShot(clip, volume);
        }

        public static CompanionBehavior For(Species s) => s == Species.Dog ? (CompanionBehavior)new DogBehavior() : new CatBehavior();
    }

    public sealed class CatBehavior : CompanionBehavior
    {
        public override Species Species => Species.Cat;
        public override float Lateral => .85f;
        public override float Ahead => 1.5f;
        public override float Pace => 1f;
        public override float Leash => 3f;
        public override float CuriosityRange => 4.5f;
        public override float WaterPull => 1f;
        public override float PlayRadius => .9f;
        public override float SeesVisitorAt => 4.8f;
        static readonly string[] rain = { "It's raining. This way.", "Rain! Come on.", "Let's get out of this." };
        static readonly string[] follow = { "Follow me.", "This way.", "Over here." };
        static readonly string[] wait = { "Are you coming?", "Hurry up.", "I'm waiting." };
        static readonly string[] tent = { "Dry at last.", "Here. It's cosy.", "In we go." };
        static readonly string[] bench = { "Can we rest here?", "A nice spot to sit.", "Shall we sit a while?" };
        static readonly string[] home = { "Shall we head home?", "It's getting late.", "Home soon?" };
        static readonly string[] hungry = { "I'm getting hungry.", "Is it dinner time?", "I could eat something." };
        static readonly string[] visitor = { "Who's that?", "Hm. A dog.", "Someone's coming." };
        static readonly string[] fly = { "Oooh.", "Look at that.", "Mine." };
        static readonly string[] pet = { "Purr.", "Mmm.", "More." };
        static readonly string[] arrive = { "Here.", "Hello.", "Yes?" };
        protected override string[] Pool(Moment m)
        {
            switch (m)
            {
                case Moment.Rain: return rain; case Moment.FollowMe: return follow; case Moment.WaitUp: return wait; case Moment.ReachedTent: return tent;
                case Moment.Bench: return bench; case Moment.GoHome: return home; case Moment.Hungry: return hungry; case Moment.VisitorNear: return visitor;
                case Moment.Butterfly: return fly; case Moment.Petted: return pet; default: return arrive;
            }
        }
    }

    /// <summary>Sticks closer, is drawn to the water, notices visitors earlier and sounds more eager.</summary>
    public sealed class DogBehavior : CompanionBehavior
    {
        public override Species Species => Species.Dog;
        public override float Lateral => .6f;
        public override float Ahead => 1.0f;
        public override float Pace => 1.25f;
        public override float Leash => 2.4f;
        public override float CuriosityRange => 5.5f;
        public override float WaterPull => .6f;
        public override float PlayRadius => 1.4f;
        public override float SeesVisitorAt => 9f;
        static readonly string[] rain = { "Rain! Quick, this way!", "Come on, come on!", "Wet! Let's go!" };
        static readonly string[] follow = { "This way! This way!", "Follow me!", "Come see!" };
        static readonly string[] wait = { "Hurry! Are you coming?", "Come on, come on!", "Over here!" };
        static readonly string[] tent = { "Made it!", "In here! In here!", "Dry! Good!" };
        static readonly string[] bench = { "Can we sit? Please?", "Rest here? Okay?", "Sit time!" };
        static readonly string[] home = { "Home? Dinner?", "Can we go home now?", "Home! Home!" };
        static readonly string[] hungry = { "I'm so hungry!", "Is it food time?", "Snacks? Please?" };
        static readonly string[] visitor = { "Who's that? Who's that?", "A friend!", "Ooh, a dog!" };
        static readonly string[] fly = { "Wheee!", "Wait for me!", "Got it! Almost!" };
        static readonly string[] pet = { "Yes! Again!", "Best day!", "More, more!" };
        static readonly string[] arrive = { "Here!", "Coming!", "Right here!" };
        protected override string[] Pool(Moment m)
        {
            switch (m)
            {
                case Moment.Rain: return rain; case Moment.FollowMe: return follow; case Moment.WaitUp: return wait; case Moment.ReachedTent: return tent;
                case Moment.Bench: return bench; case Moment.GoHome: return home; case Moment.Hungry: return hungry; case Moment.VisitorNear: return visitor;
                case Moment.Butterfly: return fly; case Moment.Petted: return pet; default: return arrive;
            }
        }
    }
}
