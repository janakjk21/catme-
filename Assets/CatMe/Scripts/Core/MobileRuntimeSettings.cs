using UnityEngine;

namespace CatMe.Core
{
    /// <summary>
    /// Applies the small set of runtime defaults shared by every CatMe scene.
    /// </summary>
    public sealed class MobileRuntimeSettings : MonoBehaviour
    {
        private const int TargetFrameRate = 60;

        private void Awake()
        {
            Application.targetFrameRate = TargetFrameRate;
            Screen.sleepTimeout = SleepTimeout.NeverSleep;
            Input.multiTouchEnabled = true;
        }
    }
}
