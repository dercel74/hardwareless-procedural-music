using System.Collections.Generic;
using UnityEngine;

namespace Hardwareless.Audio
{
    [CreateAssetMenu(menuName = "Hardwareless/SFX Library", fileName = "SFXLibrary")]
    public class SFXLibrary : ScriptableObject
    {
        [System.Serializable]
        public class Entry
        {
            public string key;
            public AudioClip clip;
            [Range(0f, 1f)] public float volume = 1f;
        }

        public List<Entry> entries = new List<Entry>();

        private static SFXLibrary _cached;

        public static AudioClip FindClip(string key, out float volume)
        {
            volume = 1f;
            var lib = Get();
            if (!lib || string.IsNullOrEmpty(key)) return null;
            foreach (var e in lib.entries)
            {
                if (e != null && e.clip != null && e.key == key)
                {
                    volume = e.volume;
                    return e.clip;
                }
            }
            return null;
        }

        private static SFXLibrary Get()
        {
            if (_cached) return _cached;
            _cached = Resources.Load<SFXLibrary>("SFX/SFXLibrary");
            return _cached;
        }
    }
}
