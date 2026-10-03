using UnityEngine;

namespace CatMe.Session
{
    /// <summary>Something small the cat walks to and looks at (feather, mushroom, flower). Optionally becomes a keepsake.</summary>
    public sealed class CuriosityPoint : MonoBehaviour
    {
        public string keepsakeId = "";
        public string foundLine = "";
        public bool found;
    }
}
