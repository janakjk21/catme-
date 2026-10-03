using System.Collections;
using System.Collections.Generic;
using CatMe.Garden;
using CatMe.Session;
using UnityEngine;

namespace CatMe.World
{
    /// <summary>
    /// A neighbour's dog occasionally walks the riverside path toward the pair, stops a little way off, wags and sits, lets the cat look at it, and walks on.
    /// Dogs (Dogs Kit) are placed under this component, inactive, by PlaceSceneBuilder. Waypoints come from the sibling object "DogPath". Animator
    /// AnimationID: 0 breathing, 1 wag, 2/3 walk, 4 run, 5 eat, 6 angry, 7 sit.
    /// </summary>
    public sealed class DogVisitor : MonoBehaviour
    {
        readonly List<Animator> dogs = new List<Animator>();
        readonly List<Transform> path = new List<Transform>();
        GardenCompanionDemo garden; SessionDirector director;
        float nextVisitAt; bool visiting;

        void Start()
        {
            garden = FindAnyObjectByType<GardenCompanionDemo>(); director = SessionDirector.Instance;
            foreach (Transform c in transform) { var a = c.GetComponentInChildren<Animator>(true); if (a != null) { dogs.Add(a); c.gameObject.SetActive(false); } }
            var pathRoot = GameObject.Find("DogPath");
            if (pathRoot != null) foreach (Transform c in pathRoot.transform) path.Add(c);
            nextVisitAt = Time.time + Random.Range(90f, 200f);
        }

        void Update()
        {
            if (visiting || dogs.Count == 0 || path.Count < 6 || garden == null || garden.OwnerTransform == null) return;
            var day = director != null ? director.Day : null;
            bool rain = director != null && director.WeatherController != null && director.WeatherController.Intensity > .3f;
            if (Time.time < nextVisitAt || (day != null && (day.Suspended || day.Night)) || rain) return;
            if (Random.value > .55f) { nextVisitAt = Time.time + Random.Range(60f, 140f); return; } // not every time
            StartCoroutine(Visit());
        }

        int NearestIndex(Vector3 p) { int best = 0; float bd = float.MaxValue; for (int i = 0; i < path.Count; i++) { float d = (path[i].position - p).sqrMagnitude; if (d < bd) { bd = d; best = i; } } return best; }

        IEnumerator Visit()
        {
            visiting = true;
            Transform owner = garden.OwnerTransform;
            int here = NearestIndex(owner.position);
            Vector3 along = path[Mathf.Min(here + 1, path.Count - 1)].position - path[Mathf.Max(here - 1, 0)].position;
            int dir = Vector3.Dot(owner.forward, along) >= 0f ? 1 : -1;              // the way the owner is heading along the path
            int start = Mathf.Clamp(here + dir * 4, 0, path.Count - 1);
            if (Mathf.Abs(start - here) < 3) { visiting = false; nextVisitAt = Time.time + 40f; yield break; } // too close to the end of the path
            int step = -dir;                                                          // the dog walks toward the owner, then on past
            var dog = dogs[Random.Range(0, dogs.Count)];
            Transform dt = dog.transform.parent == transform ? dog.transform : dog.transform.parent;
            dt.gameObject.SetActive(true);
            // Keep to one side of the path so the dog walks beside, not through, the pair.
            Vector3 Offset(int idx) { Vector3 tan = (path[Mathf.Clamp(idx + 1, 0, path.Count - 1)].position - path[Mathf.Clamp(idx - 1, 0, path.Count - 1)].position).normalized * step; return Vector3.Cross(Vector3.up, tan) * 1.2f; }
            dt.position = Ground(path[start].position + Offset(start));
            SetId(dog, 3);
            int idxNow = start; bool met = false, noticed = false; float startedAt = Time.time;
            while (Time.time - startedAt < 90f)
            {
                float ownerDist = Vector3.Distance(dt.position, owner.position);
                if (!noticed && ownerDist < garden.Behavior.SeesVisitorAt + 2f) { noticed = true; garden.SetCatLookAt(dt.position, 5f); garden.Voice?.Say(CatMe.Pets.Moment.VisitorNear, 2.8f); }
                if (!met && ownerDist < 4.8f)
                {
                    met = true; Haptics.Tap();
                    SetId(dog, 1); garden.SetCatLookAt(dt.position, 8f);   // wag while the cat looks over
                    yield return new WaitForSeconds(3.5f);
                    SetId(dog, 7); yield return new WaitForSeconds(3.2f);   // sit for a moment
                    SetId(dog, 3);
                    continue;
                }
                if (met && Mathf.Abs(idxNow - here) >= 4 && Vector3.Distance(dt.position, owner.position) > 14f) break; // far enough behind: done
                int ti = Mathf.Clamp(idxNow + step, 0, path.Count - 1);
                Vector3 goal = Ground(path[ti].position + Offset(ti)); Vector3 to = goal - dt.position; to.y = 0;
                if (to.magnitude < .4f) { if (ti == idxNow) break; idxNow = ti; yield return null; continue; }
                dt.position = Ground(dt.position + to.normalized * .95f * Time.deltaTime);
                dt.rotation = Quaternion.Slerp(dt.rotation, Quaternion.LookRotation(to.normalized), 4f * Time.deltaTime);
                yield return null;
            }
            dt.gameObject.SetActive(false);
            nextVisitAt = Time.time + Random.Range(150f, 300f);
            visiting = false;
        }

        static Vector3 Ground(Vector3 p) { p.y = GroundProbe.Height(p, p.y); return p; }
        static void SetId(Animator a, int id) { a.SetInteger("AnimationID", id); }
    }
}
