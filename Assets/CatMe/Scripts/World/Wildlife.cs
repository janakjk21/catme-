using System.Collections.Generic;
using CatMe.Garden;
using CatMe.Session;
using UnityEngine;

namespace CatMe.World
{
    /// <summary>
    /// Background animals across the river and deep in the side woods. They stay scenery: mostly idle or grazing, with short slow walks near their home spot,
    /// never entering the route. Each session shows a random subset (deer more at dusk; horse and chickens in daylight, gone in rain). The animals are
    /// placed under this component (named Deer_*, Horse_*, Chicken_*) by PlaceSceneBuilder and start inactive.
    /// </summary>
    public sealed class Wildlife : MonoBehaviour
    {
        sealed class Animal { public Transform t; public Animator a; public string kind; public Vector3 home; public Vector3 target; public bool walking; public float nextAt; public float speed = .5f; public bool shown; }
        readonly List<Animal> animals = new List<Animal>();
        SessionDirector director;
        readonly List<Animal> chosen = new List<Animal>();

        void Start()
        {
            director = SessionDirector.Instance;
            foreach (var a in GetComponentsInChildren<Animator>(true))
            {
                string n = a.transform.root == a.transform ? a.name : a.transform.parent != null && a.transform.parent.parent == transform ? a.transform.parent.name : a.name;
                Transform top = a.transform; while (top.parent != null && top.parent != transform) top = top.parent;
                string kind = top.name.StartsWith("Deer") ? "Deer" : top.name.StartsWith("Horse") ? "Horse" : top.name.StartsWith("Chicken") ? "Chicken" : null;
                if (kind == null) continue;
                animals.Add(new Animal { t = top, a = a, kind = kind, home = top.position, nextAt = Time.time + Random.Range(1f, 6f) });
                top.gameObject.SetActive(false);
            }
            // Random cast for this session
            Pick("Deer", Random.Range(2, 4)); Pick("Horse", Random.value < .6f ? 1 : 0); Pick("Chicken", Random.Range(3, 5));
        }

        void Pick(string kind, int count)
        {
            var pool = animals.FindAll(x => x.kind == kind);
            for (int i = 0; i < count && pool.Count > 0; i++) { int k = Random.Range(0, pool.Count); chosen.Add(pool[k]); pool.RemoveAt(k); }
        }

        void Update()
        {
            var day = director != null ? director.Day : null;
            float rain = director != null && director.WeatherController != null ? director.WeatherController.Intensity : 0f;
            bool night = day != null && day.Night, indoors = day != null && day.Suspended;
            foreach (var an in chosen)
            {
                bool want = !indoors && (an.kind == "Deer" || (!night && rain < .4f));
                if (want != an.shown) { an.shown = want; an.t.gameObject.SetActive(want); }
                if (!an.shown) continue;
                if (an.walking)
                {
                    Vector3 to = an.target - an.t.position; to.y = 0;
                    if (to.magnitude < .3f) { an.walking = false; an.nextAt = Time.time + Random.Range(5f, 12f); SetMove(an, false); }
                    else
                    {
                        an.t.position += to.normalized * an.speed * Time.deltaTime;
                        an.t.rotation = Quaternion.Slerp(an.t.rotation, Quaternion.LookRotation(to.normalized), 2f * Time.deltaTime);
                        Vector3 p = an.t.position; p.y = GroundProbe.Height(p, p.y); an.t.position = p;
                    }
                }
                else if (Time.time > an.nextAt)
                {
                    // Mostly keep still (idle / look around); every so often a short slow walk near home
                    if (Random.value < .45f)
                    {
                        Vector2 r = Random.insideUnitCircle * 3.5f; an.target = an.home + new Vector3(r.x, 0, r.y);
                        an.walking = true; SetMove(an, true);
                    }
                    else { an.nextAt = Time.time + Random.Range(5f, 12f); an.a.SetFloat("State", Random.value < .35f ? 1f : 0f); }
                }
            }
        }

        static void SetMove(Animal an, bool walk) { an.a.SetFloat("Vert", walk ? 1f : 0f); an.a.SetFloat("State", 0f); }
    }
}
