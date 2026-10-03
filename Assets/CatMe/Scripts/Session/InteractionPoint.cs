using UnityEngine;

namespace CatMe.Session
{
    /// <summary>A spot in a place scene with a contextual action ("Sit", "Get in"). Facing follows the object's forward axis.</summary>
    public sealed class InteractionPoint : MonoBehaviour
    {
        public InteractionKind kind;
        public string label = "Sit";
        public float radius = 2.2f;
    }
}
