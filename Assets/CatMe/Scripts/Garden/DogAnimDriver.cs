using UnityEngine;

namespace CatMe.Garden
{
    /// <summary>Drives a Dogs Kit companion's Animator from how fast its parent actually moves: breathing, walking, then running; occasional tail wag when still.</summary>
    public sealed class DogAnimDriver : MonoBehaviour
    {
        Animator animator; Vector3 last; float speed, nextWagAt, wagUntil;
        void Start() { animator = GetComponentInChildren<Animator>(true); last = transform.parent != null ? transform.parent.position : transform.position; nextWagAt = Time.time + Random.Range(3f, 7f); }
        void Update()
        {
            if (animator == null) return;
            Vector3 p = transform.parent != null ? transform.parent.position : transform.position;
            float instant = Time.deltaTime > 0f ? Vector3.ProjectOnPlane(p - last, Vector3.up).magnitude / Time.deltaTime : 0f;
            last = p; speed = Mathf.Lerp(speed, instant, 1f - Mathf.Exp(-8f * Time.deltaTime));
            int id = speed > 1.7f ? 4 : speed > .15f ? 3 : 0;
            if (id == 0) { if (Time.time > nextWagAt) { wagUntil = Time.time + 2f; nextWagAt = Time.time + Random.Range(5f, 10f); } if (Time.time < wagUntil) id = 1; }
            animator.SetInteger("AnimationID", id);
            animator.speed = id == 3 ? Mathf.Clamp(speed / .9f, .8f, 1.5f) : 1f;
        }
    }
}
