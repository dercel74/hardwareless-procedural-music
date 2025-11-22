using UnityEngine;

namespace Hardwareless.Audio
{
    public static class AudioSystem
    {
        public static void PlayOneShot(string key, Vector3 position)
        {
            float vol;
            var clip = SFXLibrary.FindClip(key, out vol);
            if (!clip)
            {
                clip = GenerateFallback(key);
                vol = 0.6f;
            }
            if (clip)
            {
                AudioSource.PlayClipAtPoint(clip, position, Mathf.Clamp01(vol));
            }
        }

        public static void PlayOneShot2D(string key)
        {
            float vol;
            var clip = SFXLibrary.FindClip(key, out vol);
            if (!clip)
            {
                clip = GenerateFallback(key);
                vol = 0.6f;
            }
            if (!clip)
            {
                return;
            }
            var go = new GameObject("OneShot2D_Audio");
            var src = go.AddComponent<AudioSource>();
            src.spatialBlend = 0f;
            src.playOnAwake = false;
            src.clip = clip;
            src.volume = Mathf.Clamp01(vol);
            src.Play();
            Object.Destroy(go, clip.length + 0.1f);
        }

        private static AudioClip GenerateFallback(string key)
        {
            // very small procedural sfx: sine for shot, click for empty, noise burst for hit
            int sampleRate = 22050;
            float duration = key == "gun-empty" ? 0.08f : (key == "gun-reload" ? 0.2f : 0.12f);
            int samples = Mathf.CeilToInt(sampleRate * duration);
            var clip = AudioClip.Create($"fbk_{key}", samples, 1, sampleRate, false);
            var data = new float[samples];

            if (key == "gun-shot")
            {
                float freq = 450f;
                for (int i = 0; i < samples; i++)
                {
                    float t = i / (float)sampleRate;
                    float env = Mathf.Exp(-20f * t);
                    data[i] = Mathf.Sin(2 * Mathf.PI * freq * t) * env;
                }
            }
            else if (key == "gun-empty")
            {
                // quiet click
                for (int i = 0; i < samples; i++)
                {
                    float t = i / (float)sampleRate;
                    float env = Mathf.Exp(-50f * t);
                    data[i] = (i == 0 ? 1f : 0f) * env;
                }
            }
            else if (key == "hit-default")
            {
                var rand = new System.Random(1337);
                for (int i = 0; i < samples; i++)
                {
                    float t = i / (float)sampleRate;
                    float env = Mathf.Exp(-30f * t);
                    data[i] = ((float)rand.NextDouble() * 2f - 1f) * env * 0.5f;
                }
            }
            else if (key == "gun-reload")
            {
                // short up-chirp
                for (int i = 0; i < samples; i++)
                {
                    float t = i / (float)sampleRate;
                    float freq = Mathf.Lerp(300f, 600f, t);
                    float env = Mathf.Exp(-6f * t);
                    data[i] = Mathf.Sin(2 * Mathf.PI * freq * t) * env * 0.5f;
                }
            }
            else if (key == "footstep-default")
            {
                // short noise tap for footsteps
                var rand = new System.Random(4242);
                for (int i = 0; i < samples; i++)
                {
                    float t = i / (float)sampleRate;
                    float env = Mathf.Exp(-60f * t);
                    // slight lowpass by averaging
                    float n = ((float)rand.NextDouble() * 2f - 1f);
                    float n2 = ((float)rand.NextDouble() * 2f - 1f);
                    data[i] = ((n * 0.6f + n2 * 0.4f) * 0.4f) * env;
                }
            }
            else
            {
                return null;
            }

            clip.SetData(data, 0);
            return clip;
        }
    }
}
