using System.Collections;
using UnityEngine;

namespace Hardwareless.Audio
{
    [DisallowMultipleComponent]
    public class MusicManager : MonoBehaviour
    {
        private static MusicManager _instance;
        public static MusicManager Instance
        {
            get
            {
                if (_instance == null)
                {
                    var go = new GameObject("MusicManager");
                    _instance = go.AddComponent<MusicManager>();
                    DontDestroyOnLoad(go);
                }
                return _instance;
            }
        }

        public AudioSource a;
        public AudioSource b;
        public float crossfadeSeconds = 0.8f;

        private void Awake()
        {
            if (_instance && _instance != this)
            {
                Destroy(gameObject);
                return;
            }
            _instance = this;
            DontDestroyOnLoad(gameObject);
            if (a == null)
            {
                a = gameObject.AddComponent<AudioSource>();
            }
            if (b == null)
            {
                b = gameObject.AddComponent<AudioSource>();
            }
            a.loop = true; b.loop = true;
            a.playOnAwake = false; b.playOnAwake = false;
            a.volume = 0f; b.volume = 0f;
        }

        public static void PlayTitleMusic()
        {
            Instance.PlayNamedOrProcedural("Music/TitleLoop", seed: 1234, tempo: 92f);
        }

        public static void PlayGameplayMusic()
        {
            Instance.PlayNamedOrProcedural("Music/GameplayLoop", seed: 5678, tempo: 108f);
        }

        private void PlayNamedOrProcedural(string resourcePath, int seed, float tempo)
        {
            var clip = Resources.Load<AudioClip>(resourcePath);
            if (clip == null)
            {
                clip = ProceduralMusic.CreatePadLoop(seed, tempo, 8f); // 8s loop
            }
            CrossfadeTo(clip);
        }

        private void CrossfadeTo(AudioClip clip)
        {
            if (clip == null)
            {
                return;
            }
            // swap roles: ensure one is playing next clip
            var from = a.isPlaying && a.volume > b.volume ? a : b;
            var to = from == a ? b : a;
            to.clip = clip;
            to.volume = 0f;
            to.Play();
            StopAllCoroutines();
            StartCoroutine(CrossfadeRoutine(from, to, crossfadeSeconds));
        }

        private IEnumerator CrossfadeRoutine(AudioSource from, AudioSource to, float dur)
        {
            float t = 0f;
            while (t < dur)
            {
                t += Time.unscaledDeltaTime;
                float k = Mathf.Clamp01(t / dur);
                to.volume = Mathf.Lerp(0f, 0.6f, k);
                if (from != null)
                {
                    from.volume = Mathf.Lerp(0.6f, 0f, k);
                }
                yield return null;
            }
            to.volume = 0.6f;
            if (from != null)
            {
                from.volume = 0f;
                from.Stop();
            }
        }
    }
}
