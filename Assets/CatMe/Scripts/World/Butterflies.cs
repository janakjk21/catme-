using System.Collections.Generic;
using CatMe.Garden;
using CatMe.Session;
using UnityEngine;

namespace CatMe.World
{
    /// <summary>
    /// A few butterflies drift around the player through the flowers, land briefly, scatter when the owner walks close, and disappear at dusk and in rain.
    /// The butterfly objects are placed under this component by PlaceSceneBuilder. Their Animator (AI cartoon butterflies pack) has an
    /// integer "State": 0 folds the wings (sitting), 1 flaps; it is set here whenever a butterfly lands or takes off.
    /// </summary>
    public sealed class Butterflies : MonoBehaviour
    {
        sealed class Flier { public Transform t; public Animator anim; public Vector3 target; public float waitUntil, fleeUntil, speed = 1f, phase; public bool landed, flapping, flapSet; }
        static readonly int StateParam = Animator.StringToHash("State");
        readonly List<Flier> fliers = new List<Flier>();
        GardenCompanionDemo garden; SessionDirector director;
        bool hidden;

        /// <summary>The nearest flying butterfly within range, for the companion to chase for a few steps.</summary>
        public bool NearestAirborne(Vector3 from, float range, out Vector3 position)
        {
            position = default; float best = range; bool found = false;
            foreach (var f in fliers) { if (f.landed || !f.t.gameObject.activeInHierarchy) continue; float d = Vector3.Distance(f.t.position, from); if (d < best) { best = d; position = f.t.position; found = true; } }
            return found;
        }

        void Start()
        {
            garden = FindAnyObjectByType<GardenCompanionDemo>(); director = SessionDirector.Instance;
            foreach (Transform child in transform) fliers.Add(new Flier { t = child, anim = WingAnimator(child), phase = Random.value * 10f });
        }

        void Update()
        {
            if (garden == null || garden.OwnerTransform == null) { garden = FindAnyObjectByType<GardenCompanionDemo>(); return; }
            Vector3 owner = garden.OwnerTransform.position;
            bool away = director != null && director.Day != null && (director.Day.Night || director.Day.Suspended || (director.WeatherController != null && director.WeatherController.Intensity > .25f));
            // A re-enabled Animator starts again in its default (sitting) state, so the wing state is set again after showing.
            if (away != hidden) { hidden = away; foreach (var f in fliers) { f.t.gameObject.SetActive(!hidden); f.flapSet = false; } }
            if (hidden) return;
            foreach (var f in fliers)
            {
                float dist = Vector3.Distance(f.t.position, owner);
                if (dist < 2.1f && Time.time > f.fleeUntil) { f.fleeUntil = Time.time + 2.2f; f.landed = false; f.speed = 2.6f; Vector3 away2 = (f.t.position - owner); away2.y = 0; f.target = f.t.position + away2.normalized * 6f + Vector3.up * 1.6f; }
                Flap(f, !f.landed);
                if (f.landed) { if (Time.time > f.waitUntil) PickTarget(f, owner); else continue; }
                Vector3 to = f.target - f.t.position;
                if (to.magnitude < .2f)
                {
                    if (f.target.y - GroundProbe.Height(f.target) < .75f && Time.time > f.fleeUntil) { f.landed = true; f.waitUntil = Time.time + Random.Range(2f, 4.5f); }
                    else PickTarget(f, owner);
                    continue;
                }
                if (Time.time > f.fleeUntil) f.speed = Mathf.MoveTowards(f.speed, 1f, Time.deltaTime);
                Vector3 dir = to.normalized;
                Vector3 wobble = new Vector3(Mathf.Sin(Time.time * 3.1f + f.phase), Mathf.Sin(Time.time * 5.3f + f.phase) * .6f, Mathf.Cos(Time.time * 2.7f + f.phase)) * .35f;
                f.t.position += (dir + wobble * .5f).normalized * f.speed * Time.deltaTime;
                Vector3 flat = new Vector3(dir.x, 0, dir.z); if (flat.sqrMagnitude > .001f) f.t.rotation = Quaternion.Slerp(f.t.rotation, Quaternion.LookRotation(flat), 4f * Time.deltaTime);
            }
        }

        static Animator WingAnimator(Transform butterfly)
        {
            var anim = butterfly.GetComponentInChildren<Animator>(true);
            return anim != null && anim.runtimeAnimatorController != null ? anim : null;
        }

        static void Flap(Flier f, bool flapping)
        {
            if (f.anim == null || (f.flapSet && f.flapping == flapping) || !f.anim.isActiveAndEnabled) return;
            f.flapping = flapping; f.flapSet = true;
            f.anim.SetInteger(StateParam, flapping ? 1 : 0);
        }

        void PickTarget(Flier f, Vector3 owner)
        {
            float angle = Random.value * Mathf.PI * 2f, radius = Random.Range(4f, 15f);
            Vector3 p = owner + new Vector3(Mathf.Cos(angle) * radius, 0, Mathf.Sin(angle) * radius);
            float ground = GroundProbe.Height(p, owner.y);
            p.y = ground + (Random.value < .45f ? Random.Range(.25f, .5f) : Random.Range(.9f, 1.8f)); // sometimes a flower, sometimes in the air
            f.target = p; f.landed = false;
        }
    }
}
