using UnityEngine;

namespace CatMe.Session
{
    /// <summary>Marks a trigger volume that ends a place. Polled by SessionDirector (characters have no colliders).</summary>
    public sealed class PlaceExit : MonoBehaviour
    {
        public string targetPlace;
        public string targetEntry = "Start";
        public string label;
        public bool Contains(Vector3 point)
        {
            var c = GetComponent<Collider>();
            return c != null && c.bounds.Contains(point);
        }
    }
}
