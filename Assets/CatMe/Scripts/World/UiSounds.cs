using UnityEngine;

namespace CatMe.World
{
    /// <summary>Very soft generated tick for buttons, so no extra audio asset is required. Honors the sound setting.</summary>
    public static class UiSounds
    {
        static AudioClip click; static AudioSource source;
        public static void Click()
        {
            if (PlayerPrefs.GetInt("CatMe.SoundEnabled", 1) == 0) return;
            if (click == null)
            {
                const int rate = 22050; var data = new float[rate / 12];
                for (int i = 0; i < data.Length; i++) { float t = i / (float)rate; data[i] = Mathf.Sin(2f * Mathf.PI * 520f * t) * Mathf.Exp(-t * 55f) * .35f; }
                click = AudioClip.Create("ui-soft-tick", data.Length, 1, rate, false); click.SetData(data, 0);
            }
            if (source == null) { var go = new GameObject("UiSounds"); Object.DontDestroyOnLoad(go); source = go.AddComponent<AudioSource>(); source.spatialBlend = 0f; source.volume = .5f; }
            source.PlayOneShot(click);
        }
    }
}
