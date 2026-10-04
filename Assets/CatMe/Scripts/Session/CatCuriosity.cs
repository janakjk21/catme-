using CatMe.Garden;
using UnityEngine;

namespace CatMe.Session
{
    /// <summary>
    /// While following, the cat occasionally trots to a nearby curiosity point and stops facing it. When the owner then
    /// walks up, a keepsake is collected. Walking-only: no clips beyond the existing walk are required.
    /// </summary>
    public sealed class CatCuriosity : MonoBehaviour
    {
        const float NoticeRange = 5.5f, Cooldown = 25f, DwellSeconds = 4f, CollectRange = 2.2f;
        GardenCompanionDemo garden;
        CuriosityPoint active;
        float nextAllowedAt = 8f, arrivedAt = -1f, activatedAt, playUntil, nextPlayAt = 30f;
        World.Butterflies butterflies;

        public void Initialize(GardenCompanionDemo demo) { garden = demo; }

        void Update()
        {
            if (garden == null || garden.OwnerTransform == null || garden.CatTransform == null || SessionDirector.Instance == null || SessionDirector.Instance.IsBusy) return;
            Vector3 owner = garden.OwnerTransform.position, cat = garden.CatTransform.position;

            if (playUntil > 0f)
            {
                if (Time.time > playUntil || Vector3.Distance(cat, owner) > garden.Behavior.CuriosityRange + 1f) { garden.SetCuriosity(Vector3.zero, 0f); playUntil = 0f; nextAllowedAt = Time.time + Cooldown; }
                return;
            }
            if (active != null)
            {
                float catDistance = Vector3.Distance(cat, active.transform.position);
                if (catDistance < 1f && arrivedAt < 0f) arrivedAt = Time.time;
                if (arrivedAt > 0f && Vector3.Distance(owner, active.transform.position) < CollectRange) Collect();
                else if (arrivedAt > 0f && Time.time - arrivedAt > DwellSeconds + 6f) Release();
                else if (Time.time - activatedAt > 30f || Vector3.Distance(cat, owner) > 5.5f) Release();
                return;
            }
            if (Time.time < nextAllowedAt || !garden.CatIsFollowing) return;
            if (Time.time > nextPlayAt)
            {
                nextPlayAt = Time.time + Random.Range(45f, 90f);
                if (butterflies == null) butterflies = FindAnyObjectByType<World.Butterflies>();
                if (butterflies != null && butterflies.NearestAirborne(cat, 6f, out Vector3 fly) && Vector3.Distance(fly, owner) < garden.Behavior.CuriosityRange)
                {
                    playUntil = Time.time + 6f; garden.SetCuriosity(fly, 6f, true); garden.Voice?.Say(Pets.Moment.Butterfly, 2.4f);
                    return;
                }
            }
            CuriosityPoint best = null; float bestDistance = NoticeRange;
            foreach (var point in FindObjectsByType<CuriosityPoint>(FindObjectsSortMode.None))
            {
                if (point.found) continue;
                if (Vector3.Distance(owner, point.transform.position) > garden.Behavior.CuriosityRange) continue; // trips stay close to the owner
                float d = Vector3.Distance(cat, point.transform.position);
                string n = point.name.ToLowerInvariant(); if (n.Contains("water") || n.Contains("river") || n.Contains("pond") || n.Contains("bank")) d *= garden.Behavior.WaterPull;
                if (d < bestDistance) { best = point; bestDistance = d; }
            }
            if (best == null) return;
            active = best; arrivedAt = -1f; activatedAt = Time.time;
            garden.SetCuriosity(best.transform.position, 20f);
        }

        void Collect()
        {
            active.found = true;
            if (!string.IsNullOrEmpty(active.keepsakeId)) SessionDirector.Instance.AddKeepsake(active.keepsakeId);
            if (active.keepsakeId == "lookout-keepsake")
            {
                garden.Voice?.Say(Pets.Moment.GoHome, 3.4f, true);
                foreach (var point in SessionDirector.Instance.CurrentInteractions())
                    if (point != null && point.kind == InteractionKind.EnterHouse) { garden.SetLead(point.transform.position, false); break; }
            }
            if (!string.IsNullOrEmpty(active.foundLine) && PlayerPrefs.GetInt("CatMe.Journey.StoryEnabled", 0) == 1) garden.SetAppStatus(active.foundLine);
            active.gameObject.SetActive(false);
            Release();
        }

        void Release()
        {
            garden.SetCuriosity(Vector3.zero, 0f);
            active = null; arrivedAt = -1f; nextAllowedAt = Time.time + Cooldown;
        }
    }
}
