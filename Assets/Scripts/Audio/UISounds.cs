using UnityEngine;

namespace Hardwareless.Audio
{
    public static class UISounds
    {
        private static GameObject _host;
        private static AudioSource _src;

        private static void Ensure()
        {
            if (_host != null && _src != null)
            {
                return;
            }
            _host = new GameObject("UISounds");
            Object.DontDestroyOnLoad(_host);
            _src = _host.AddComponent<AudioSource>();
            _src.playOnAwake = false;
            _src.loop = false;
            _src.volume = 0.6f;
        }

        public static void PlayClick()
        {
            Ensure();
            var clip = Resources.Load<AudioClip>("UI/Click");
            if (clip == null)
            {
                clip = CreateClick();
            }
            _src.pitch = 1f;
            _src.PlayOneShot(clip, 1f);
        }

        // Lightweight synthesized click if no asset exists
        private static AudioClip CreateClick()
        {
            int sampleRate = AudioSettings.outputSampleRate > 0 ? AudioSettings.outputSampleRate : 44100;
            int samples = Mathf.CeilToInt(0.08f * sampleRate); // 80 ms
            var data = new float[samples];
            float f0 = 1200f;
            for (int i = 0; i < samples; i++)
            {
                float t = i / (float)sampleRate;
                // short exponential decay envelope
                float env = Mathf.Exp(-t * 40f);
                float v = Mathf.Sin(2 * Mathf.PI * f0 * t) * env;
                data[i] = v * 0.35f;
            }
            var clip = AudioClip.Create("ui_click_gen", samples, 1, sampleRate, false);
            clip.SetData(data, 0);
            return clip;
        }
    }
}
