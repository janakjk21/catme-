using UnityEngine;

namespace CatMe.Onboarding
{
    // The URL is configured per build; it is never guessed from Firebase project metadata.
    [CreateAssetMenu(menuName = "CatMe/Mobile API Config")]
    public sealed class MobileApiConfig : ScriptableObject
    {
        [SerializeField] string baseUrl;
        public string BaseUrl => baseUrl;
    }
}
