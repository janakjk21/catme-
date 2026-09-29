using System;

namespace CatMe.Save
{
    [Serializable]
    public sealed class CatSaveData
    {
        public int schemaVersion = 1;
        public float energy = 100f;
        public string stableState = "Awake";
        public long savedAtUtc;
    }
}
