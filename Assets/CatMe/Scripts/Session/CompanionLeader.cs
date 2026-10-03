using CatMe.Garden;
using CatMe.Pets;
using UnityEngine;

namespace CatMe.Session
{
    /// <summary>
    /// Makes the companion initiate moments instead of waiting to be asked: it leads to the tent when it rains, invites a rest at benches,
    /// and mentions heading home or being hungry late in the outing. Hunger is only ever a line, never a meter.
    /// </summary>
    public sealed class CompanionLeader : MonoBehaviour
    {
        enum Phase { Idle, Leading, Waiting, Done }
        GardenCompanionDemo garden; SessionDirector director;
        Phase phase = Phase.Idle; Vector3 leadTo; float phaseAt, waitSaid, nextRainLeadAt = 12f, nextBenchAt = 40f, benchLeadUntil;
        float startedAt, nextHomeAt = 480f; bool homeSaid, hungrySaid, benchLeading;
        float nextVisitorLookAt;

        public void Initialize(GardenCompanionDemo demo, SessionDirector sessionDirector) { garden = demo; director = sessionDirector; startedAt = Time.time; }

        InteractionPoint Nearest(InteractionKind kind, Vector3 from, float within)
        {
            InteractionPoint best = null; float bd = within;
            var points = director.CurrentInteractions();
            if (points == null) return null;
            foreach (var p in points) { if (p == null || p.kind != kind || !p.gameObject.activeInHierarchy) continue; float d = Vector3.Distance(p.transform.position, from); if (d < bd) { bd = d; best = p; } }
            return best;
        }

        void Update()
        {
            if (garden == null || director == null || garden.Voice == null || garden.OwnerTransform == null || garden.CatTransform == null) return;
            if (director.IsBusy || garden.IsInputBlocked || (director.Day != null && director.Day.Suspended) || Time.timeScale == 0f) { if (phase != Phase.Idle && phase != Phase.Done) Stop(); return; }
            Vector3 owner = garden.OwnerTransform.position, pet = garden.CatTransform.position;
            var weather = director.WeatherController;
            float rain = weather != null ? weather.Intensity : 0f;

            switch (phase)
            {
                case Phase.Idle: TickIdle(owner, rain); break;
                case Phase.Leading:
                case Phase.Waiting: TickLead(owner, pet, rain); break;
                case Phase.Done: if (rain < .05f) { phase = Phase.Idle; nextRainLeadAt = Time.time + 20f; } break;
            }
            if (phase == Phase.Idle) TickMoments(owner, rain);
        }

        void TickIdle(Vector3 owner, float rain)
        {
            if (!garden.CatIsFollowing && !garden.LeadActive) return;
            if (rain > .3f && Time.time > nextRainLeadAt)
            {
                var tent = Nearest(InteractionKind.EnterTent, owner, 70f);
                if (tent == null) { nextRainLeadAt = Time.time + 30f; return; }
                leadTo = tent.transform.position;
                if (Vector3.Distance(owner, leadTo) < tent.radius + .5f) { phase = Phase.Done; return; } // already at the tent
                garden.Voice.Say(Moment.Rain, 3.2f, true);
                garden.SetLead(leadTo, false); phase = Phase.Leading; phaseAt = Time.time; waitSaid = 0f; benchLeading = false;
            }
        }

        void TickLead(Vector3 owner, Vector3 pet, float rain)
        {
            float petToOwner = Vector3.Distance(pet, owner), ownerToGoal = Vector3.Distance(owner, leadTo), petToGoal = Vector3.Distance(pet, leadTo);
            bool timeout = Time.time - phaseAt > (benchLeading ? 14f : 70f);
            bool rainOver = !benchLeading && rain < .08f;
            if (timeout || rainOver || ownerToGoal < 2.4f) { Finish(ownerToGoal < 2.4f && !rainOver && !benchLeading); return; }
            if (phase == Phase.Leading && petToOwner > 5.5f) { garden.SetLead(leadTo, true); phase = Phase.Waiting; waitSaid = Time.time; }
            else if (phase == Phase.Waiting)
            {
                if (petToOwner < 3.2f) { garden.SetLead(leadTo, false); phase = Phase.Leading; }
                else if (Time.time - waitSaid > 5f) { garden.Voice.Say(Moment.WaitUp, 2.8f); waitSaid = Time.time; }
            }
            if (petToGoal < 1.2f && phase == Phase.Leading) { garden.SetLead(garden.CatTransform.position, true); } // arrived: stands and waits for the owner
        }

        void Finish(bool reachedTent)
        {
            garden.ClearLead();
            if (reachedTent) garden.Voice.Say(Moment.ReachedTent, 3f, true);
            phase = Phase.Done; nextRainLeadAt = Time.time + 90f;
            if (benchLeading) { phase = Phase.Idle; benchLeading = false; }
        }
        void Stop() { garden.ClearLead(); phase = Phase.Idle; benchLeading = false; nextRainLeadAt = Time.time + 25f; }

        void TickMoments(Vector3 owner, float rain)
        {
            if (!garden.CatIsFollowing) return;
            float t = Time.time - startedAt;
            var bench = Nearest(InteractionKind.Sit, owner, 7f);
            if (bench != null && Time.time > nextBenchAt && rain < .3f && Random.value < .5f)
            {
                nextBenchAt = Time.time + 100f;
                if (garden.Voice.Say(Moment.Bench, 3.4f)) { leadTo = bench.transform.position; garden.SetLead(leadTo, false); phase = Phase.Leading; phaseAt = Time.time; benchLeading = true; }
                return;
            }
            var day = director.Day;
            bool late = day != null && day.TimeOfDay > .8f;
            if (!homeSaid && (late || t > nextHomeAt)) { if (garden.Voice.Say(Moment.GoHome, 3.4f)) { homeSaid = true; nextHomeAt = t + 600f; } }
            else if (homeSaid && !hungrySaid && t > nextHomeAt - 120f) { if (garden.Voice.Say(Moment.Hungry, 3.4f)) { hungrySaid = true; HungryHint(); } }
        }

        void HungryHint()
        {
            var food = StoreCatalog.FirstOwned(StoreCategory.Food, PetRoster.Active.Kind);
            if (food != null) garden.SetAppStatus(food.name + " is in your bag. Feed " + PetRoster.Active.name + " in the house.");
        }
    }
}
