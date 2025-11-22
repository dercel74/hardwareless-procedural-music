using UnityEngine;
using System.Diagnostics;

namespace Hardwareless.Audio
{
    /// <summary>
    /// Static procedural music generation helpers. Produces short seamless loops (pad, bass, drums, arp)
    /// deterministically from a seed. Intended for lightweight ambience without external assets.
    /// </summary>
    public static class ProceduralMusic
    {
        #region Cache
        // Simple in-memory cache of generated clips to prevent re-synthesis when same parameters are requested.
        // Key format: type|seed|bpm|len|extraParams
        private static readonly System.Collections.Generic.Dictionary<string, AudioClip> _cache = new System.Collections.Generic.Dictionary<string, AudioClip>();
        // LRU tracking structures and limits
        private static readonly System.Collections.Generic.LinkedList<string> _lru = new System.Collections.Generic.LinkedList<string>();
        private static readonly System.Collections.Generic.Dictionary<string, System.Collections.Generic.LinkedListNode<string>> _lruNodes = new System.Collections.Generic.Dictionary<string, System.Collections.Generic.LinkedListNode<string>>();
        private static readonly System.Collections.Generic.Dictionary<string, long> _cacheBytes = new System.Collections.Generic.Dictionary<string, long>();
        private static long _totalBytes;
        private static int _maxClips = 128;
        private static long _maxBytes = 64L * 1024L * 1024L; // ~64 MB default
        // Optional profiling log entries (bounded) capturing generation durations.
        private static readonly System.Collections.Generic.List<string> _profileLog = new System.Collections.Generic.List<string>(64);
    /// <summary>Toggle to record generation durations of procedural clips (stored in an internal bounded log).</summary>
    public static bool EnableProfiling = false;

        /// <summary>Removes all cached procedural clips.</summary>
        public static void ClearCache()
        {
            _cache.Clear();
            _lru.Clear();
            _lruNodes.Clear();
            _cacheBytes.Clear();
            _totalBytes = 0;
        }
        /// <summary>Set cache limits (MB and clip count). Eviction uses LRU.</summary>
        public static void SetCacheLimits(float maxMegabytes, int maxClips)
        {
            if (maxMegabytes > 0f) { _maxBytes = (long)(maxMegabytes * 1024f * 1024f); }
            if (maxClips > 0) { _maxClips = maxClips; }
            EnforceCacheLimits();
        }
        /// <summary>Get current cache limits (MB and clip count).</summary>
        public static void GetCacheLimits(out float maxMB, out int maxClips)
        {
            maxMB = _maxBytes / (1024f * 1024f);
            maxClips = _maxClips;
        }
        /// <summary>Returns and clears any profiling log entries recorded when EnableProfiling = true.</summary>
        public static string[] GetAndClearProfileLog()
        {
            var arr = _profileLog.ToArray();
            _profileLog.Clear();
            return arr;
        }

        /// <summary>Lightweight statistics about the procedural clip cache.</summary>
        public struct CacheStats
        {
            public int ClipCount;
            public int TotalSamples;
            public float TotalSecondsApprox;
            public long TotalBytesApprox;
        }

        /// <summary>Number of cached clips currently stored (non-null).</summary>
        public static int CacheCount
        {
            get
            {
                int count = 0;
                foreach (var kv in _cache)
                {
                    if (kv.Value != null)
                    {
                        count++;
                    }
                }
                return count;
            }
        }

        /// <summary>Returns aggregate cache stats (approx seconds and bytes assume uncompressed PCM floats).</summary>
        public static CacheStats GetCacheStats()
        {
            CacheStats s = new CacheStats();
            foreach (var kv in _cache)
            {
                var clip = kv.Value;
                if (clip == null)
                {
                    continue;
                }
                int samples = clip.samples;
                int channels = clip.channels > 0 ? clip.channels : 1;
                int freq = clip.frequency > 0 ? clip.frequency : ResolveSampleRate();
                s.ClipCount++;
                s.TotalSamples += samples * channels;
                s.TotalSecondsApprox += (freq > 0) ? (samples / (float)freq) : 0f;
                s.TotalBytesApprox += (long)samples * channels * sizeof(float);
            }
            return s;
        }

        /// <summary>Returns a concise one-line summary of cache size for quick logging.</summary>
        public static string GetCacheDebugSummary()
        {
            var s = GetCacheStats();
            float mb = s.TotalBytesApprox / (1024f * 1024f);
            return $"cache clips={s.ClipCount} ~{s.TotalSecondsApprox:F1}s ~{mb:F1}MB";
        }

        private static string BuildKey(string type, int seed, float bpm, float lengthSeconds, string extra)
        {
            return string.Concat(type, "|", seed, "|", bpm.ToString("F2"), "|", lengthSeconds.ToString("F2"), "|", extra);
        }
        private static void Touch(string key)
        {
            if (_lruNodes.TryGetValue(key, out var node))
            {
                _lru.Remove(node);
                _lru.AddLast(node);
            }
        }
        private static void TrackAdd(string key, AudioClip clip)
        {
            long bytes = 0;
            if (clip != null)
            {
                int samples = clip.samples;
                int channels = clip.channels > 0 ? clip.channels : 1;
                bytes = (long)samples * channels * sizeof(float);
            }
            if (_lruNodes.TryGetValue(key, out var existing))
            {
                _lru.Remove(existing);
                _lru.AddLast(existing);
                long prev = 0; _cacheBytes.TryGetValue(key, out prev);
                _totalBytes -= prev;
                _cacheBytes[key] = bytes;
                _totalBytes += bytes;
            }
            else
            {
                var node = _lru.AddLast(key);
                _lruNodes[key] = node;
                _cacheBytes[key] = bytes;
                _totalBytes += bytes;
            }
            EnforceCacheLimits();
        }
        private static void EnforceCacheLimits()
        {
            while ((_maxClips > 0 && _cache.Count > _maxClips) || (_maxBytes > 0 && _totalBytes > _maxBytes))
            {
                var node = _lru.First;
                if (node == null)
                {
                    break;
                }
                string evict = node.Value;
                _lru.RemoveFirst();
                _lruNodes.Remove(evict);
                if (_cache.ContainsKey(evict))
                {
                    _cache.Remove(evict);
                }
                if (_cacheBytes.TryGetValue(evict, out var b))
                {
                    _totalBytes -= b;
                    _cacheBytes.Remove(evict);
                }
            }
        }
        #endregion
        #region Public API
        /// <summary>
        /// Generates a transposed chord progression variant for pad/arp loops.
        /// Progression index shifts the base progression by a small semitone offset for gentle modulation.
        /// </summary>
        /// <param name="progressionIndex">Variant index (can be negative); each step shifts by +2 semitones wrapped in range [-6,+6].</param>
        /// <param name="baseRootHz">Base root frequency (defaults to A3 ≈ 220 Hz).</param>
        public static float[] GetChordProgressionVariant(int progressionIndex, float baseRootHz = 220f)
        {
            // Base minor-like progression offsets (A, F, C, G) relative to A: 0, -5, -9, -2 semitones
            int[] baseOffsets = { 0, -5, -9, -2 };
            // shift by 2 semitones per index, clamp into musical window [-6,+6] to avoid extreme jumps
            int rawShift = progressionIndex * 2;
            int shift = Mathf.Clamp(rawShift, -6, 6);
            float[] roots = new float[baseOffsets.Length];
            for (int i = 0; i < baseOffsets.Length; i++)
            {
                int semi = baseOffsets[i] + shift;
                roots[i] = baseRootHz * Mathf.Pow(2f, semi / 12f);
            }
            return roots;
        }
        /// <summary>
        /// Creates a mellow pad loop. Chord progression is given as array of chord roots (Hz) or will default to A minor sequence.
        /// </summary>
        /// <param name="seed">Deterministic seed.</param>
        /// <param name="bpm">Beats per minute (used for chord step length).</param>
        /// <param name="lengthSeconds">Total loop length (will truncate/extend last chord to fit).</param>
        /// <param name="chordRoots">Optional chord root frequencies; if null uses default Am F C G.</param>
        public static AudioClip CreatePadLoop(int seed, float bpm, float lengthSeconds, float[] chordRoots = null)
        {
            Stopwatch sw = null; if (EnableProfiling) { sw = Stopwatch.StartNew(); }
            int sampleRate = ResolveSampleRate();
            int totalSamples = Mathf.Max(8, Mathf.CeilToInt(lengthSeconds * sampleRate));
            float[] data = new float[totalSamples];

            System.Random rng = new System.Random(seed);
            var roots = chordRoots == null || chordRoots.Length == 0
                ? new[] { 220.0f, 174.61f, 130.81f, 196.00f } // Am, F, C, G
                : chordRoots;

            // derive chord duration from bpm (1 chord per 4 beats)
            float beatsPerChord = 4f;
            float chordSeconds = beatsPerChord * 60f / Mathf.Max(1f, bpm);
            int chordSamples = Mathf.Max(1, Mathf.CeilToInt(chordSeconds * sampleRate));
            int chordsCount = Mathf.CeilToInt(totalSamples / (float)chordSamples);

            for (int c = 0; c < chordsCount; c++)
            {
                float root = roots[c % roots.Length];
                float third = root * 1.5f; // just minor/major ambiguous 3rd (perfect fifth relation for simplicity)
                float octave = root * 2f;
                float detune = 1f + (float)(rng.NextDouble() * 0.004 - 0.002);
                int start = c * chordSamples;
                int end = Mathf.Min(totalSamples, start + chordSamples);
                int len = end - start;
                for (int i = 0; i < len; i++)
                {
                    int idx = start + i;
                    float t = idx / (float)sampleRate;
                    float env = PadEnvelope(i, len);
                    float v = 0.55f * Mathf.Sin(TAU * root * detune * t)
                            + 0.30f * Mathf.Sin(TAU * third * t)
                            + 0.20f * Mathf.Sin(TAU * octave * t);
                    // slow chorus LFO
                    float chorus = 1f + 0.02f * Mathf.Sin(TAU * 0.1f * t);
                    v *= env * 0.18f * chorus;
                    data[idx] += v;
                }
            }
            NormaliseInPlace(data, 0.95f);
            var clip = AudioClip.Create("ProcPad" + seed, totalSamples, 1, sampleRate, false);
            clip.SetData(data, 0);
            if (sw != null)
            {
                sw.Stop();
                _profileLog.Add($"Pad seed={seed} bpm={bpm:F1} len={lengthSeconds:F1}s ms={sw.ElapsedMilliseconds}");
            }
            return clip;
        }

        /// <summary>Returns a cached pad loop if available, otherwise generates and stores it.</summary>
        public static AudioClip GetPadLoop(int seed, float bpm, float lengthSeconds, float[] chordRoots = null, bool forceRegenerate = false)
        {
            string extra = chordRoots == null ? "default" : string.Join(",", System.Array.ConvertAll(chordRoots, r => r.ToString("F2")));
            string key = BuildKey("pad", seed, bpm, lengthSeconds, extra);
            if (!forceRegenerate && _cache.TryGetValue(key, out var cached) && cached != null)
            {
                Touch(key);
                return cached;
            }
            var clip = CreatePadLoop(seed, bpm, lengthSeconds, chordRoots);
            _cache[key] = clip;
            TrackAdd(key, clip);
            return clip;
        }

    /// <summary>
    /// Returns a cached pad loop variant for a given progression index (gentle harmonic modulation); generates if missing.
    /// </summary>
        public static AudioClip GetPadLoopVariant(int seed, float bpm, float lengthSeconds, int progressionIndex, bool forceRegenerate = false)
        {
            float[] roots = GetChordProgressionVariant(progressionIndex);
            string extra = "var:" + progressionIndex;
            string key = BuildKey("pad", seed, bpm, lengthSeconds, extra);
            if (!forceRegenerate && _cache.TryGetValue(key, out var cached) && cached != null)
            {
                Touch(key);
                return cached;
            }
            var clip = CreatePadLoop(seed, bpm, lengthSeconds, roots);
            _cache[key] = clip;
            TrackAdd(key, clip);
            return clip;
        }

        /// <summary>
        /// Creates a richer pad loop variant with harmonic extensions based on a richness level.
        /// Richness levels:
        /// 0 = base pad (root, fifth-ish third, octave) identical to CreatePadLoop.
        /// 1 = adds 9th (add2) shimmer partial + slow amplitude LFO for subtle movement.
        /// 2 = adds 9th + 11th extensions, high airy shimmer and micro detune pairs for broader texture.
        /// </summary>
        /// <param name="seed">Deterministic seed.</param>
        /// <param name="bpm">Beats per minute (controls chord duration).</param>
        /// <param name="lengthSeconds">Total loop length.</param>
        /// <param name="richnessLevel">0..2 richness tier (values outside clamped).</param>
        /// <param name="chordRoots">Optional chord root frequencies; defaults to Am F C G if null.</param>
        public static AudioClip CreatePadLoopRich(int seed, float bpm, float lengthSeconds, int richnessLevel, float[] chordRoots = null)
        {
            Stopwatch sw = null; if (EnableProfiling) { sw = Stopwatch.StartNew(); }
            richnessLevel = Mathf.Clamp(richnessLevel, 0, 2);
            int sampleRate = ResolveSampleRate();
            int totalSamples = Mathf.Max(8, Mathf.CeilToInt(lengthSeconds * sampleRate));
            float[] data = new float[totalSamples];
            System.Random rng = new System.Random(seed * 73 + richnessLevel * 997);
            var roots = chordRoots == null || chordRoots.Length == 0
                ? new[] { 220.0f, 174.61f, 130.81f, 196.00f }
                : chordRoots;
            float beatsPerChord = 4f;
            float chordSeconds = beatsPerChord * 60f / Mathf.Max(1f, bpm);
            int chordSamples = Mathf.Max(1, Mathf.CeilToInt(chordSeconds * sampleRate));
            int chordsCount = Mathf.CeilToInt(totalSamples / (float)chordSamples);

            for (int c = 0; c < chordsCount; c++)
            {
                float root = roots[c % roots.Length];
                float fifth = root * 1.5f;
                float octave = root * 2f;
                float ninth = root * Mathf.Pow(2f, 14f/12f);   // add9
                float eleventh = root * Mathf.Pow(2f, 17f/12f); // add11
                int start = c * chordSamples;
                int end = Mathf.Min(totalSamples, start + chordSamples);
                int len = end - start;
                for (int i = 0; i < len; i++)
                {
                    int idx = start + i;
                    float t = idx / (float)sampleRate;
                    float env = PadEnvelope(i, len);
                    float detuneA = 1f + (float)(rng.NextDouble() * 0.004 - 0.002);
                    float detuneB = 1f + (float)(rng.NextDouble() * 0.004 - 0.002);
                    float basePad = 0.55f * Mathf.Sin(TAU * root * detuneA * t)
                                  + 0.30f * Mathf.Sin(TAU * fifth * t)
                                  + 0.20f * Mathf.Sin(TAU * octave * detuneB * t);
                    float value = basePad;
                    if (richnessLevel >= 1)
                    {
                        // 9th shimmer + slow amplitude LFO
                        float lfo = 1f + 0.03f * Mathf.Sin(TAU * 0.07f * t);
                        value += 0.18f * Mathf.Sin(TAU * ninth * t) * lfo;
                    }
                    if (richnessLevel >= 2)
                    {
                        // add 11th airy and high shimmer partials (quiet)
                        float shimmer = Mathf.Sin(TAU * eleventh * t) * 0.12f;
                        shimmer += 0.07f * Mathf.Sin(TAU * (eleventh * 1.01f) * t);
                        // subtle high noise layer filtered by envelope for air
                        float noise = (float)(rng.NextDouble() * 2 - 1) * env * 0.02f;
                        value += shimmer + noise;
                    }
                    // gentle chorus movement common to all levels
                    float chorus = 1f + 0.02f * Mathf.Sin(TAU * 0.1f * t);
                    value *= env * 0.18f * chorus;
                    data[idx] += value;
                }
            }
            NormaliseInPlace(data, 0.95f);
            var clip = AudioClip.Create("ProcPadRich" + richnessLevel + "_" + seed, totalSamples, 1, sampleRate, false);
            clip.SetData(data, 0);
            if (sw != null)
            {
                sw.Stop();
                _profileLog.Add($"PadRich seed={seed} bpm={bpm:F1} rl={richnessLevel} len={lengthSeconds:F1}s ms={sw.ElapsedMilliseconds}");
            }
            return clip;
        }

        /// <summary>
        /// Returns a cached pad loop variant for a progression index and richness level; generates if missing.
        /// </summary>
        public static AudioClip GetPadLoopVariantRich(int seed, float bpm, float lengthSeconds, int progressionIndex, int richnessLevel, bool forceRegenerate = false)
        {
            float[] roots = GetChordProgressionVariant(progressionIndex);
            string extra = "var:" + progressionIndex + "|r:" + Mathf.Clamp(richnessLevel,0,2);
            string key = BuildKey("pad", seed, bpm, lengthSeconds, extra);
            if (!forceRegenerate && _cache.TryGetValue(key, out var cached) && cached != null)
            {
                Touch(key);
                return cached;
            }
            var clip = CreatePadLoopRich(seed, bpm, lengthSeconds, richnessLevel, roots);
            _cache[key] = clip;
            TrackAdd(key, clip);
            return clip;
        }

        /// <summary>
        /// Simple bass loop using root + fifth pattern per beat.
        /// </summary>
        public static AudioClip CreateBassLoop(int seed, float bpm, float lengthSeconds, float rootHz = 55f)
        {
            Stopwatch sw = null; if (EnableProfiling) { sw = Stopwatch.StartNew(); }
            int sampleRate = ResolveSampleRate();
            int totalSamples = Mathf.Max(8, Mathf.CeilToInt(lengthSeconds * sampleRate));
            float[] data = new float[totalSamples];
            System.Random rng = new System.Random(seed);

            float secondsPerBeat = 60f / Mathf.Max(1f, bpm);
            int beatSamples = Mathf.Max(1, Mathf.CeilToInt(secondsPerBeat * sampleRate));
            int beats = Mathf.CeilToInt(totalSamples / (float)beatSamples);

            for (int b = 0; b < beats; b++)
            {
                int start = b * beatSamples;
                int end = Mathf.Min(totalSamples, start + beatSamples);
                float baseF = (b % 4 == 2) ? rootHz * 1.12246f : rootHz; // slight variation (≈ +2 semitones)
                float fifth = baseF * 1.5f;
                float detune = 1f + (float)(rng.NextDouble() * 0.006 - 0.003);
                int len = end - start;
                for (int i = 0; i < len; i++)
                {
                    int idx = start + i;
                    float t = idx / (float)sampleRate;
                    float env = PercEnvelope(i, len, 0.02f, 0.15f);
                    float v = 0.7f * Mathf.Sin(TAU * baseF * detune * t)
                            + 0.3f * Mathf.Sin(TAU * fifth * t * 0.5f);
                    v *= env * 0.5f;
                    data[idx] += v;
                }
            }
            LowPassInPlace(data, sampleRate, 200f);
            NormaliseInPlace(data, 0.9f);
            var clip = AudioClip.Create("ProcBass" + seed, totalSamples, 1, sampleRate, false);
            clip.SetData(data, 0);
            if (sw != null)
            {
                sw.Stop();
                _profileLog.Add($"Bass seed={seed} bpm={bpm:F1} root={rootHz:F1} ms={sw.ElapsedMilliseconds}");
            }
            return clip;
        }

        /// <summary>Returns a cached bass loop if available, otherwise generates and stores it.</summary>
        public static AudioClip GetBassLoop(int seed, float bpm, float lengthSeconds, float rootHz = 55f, bool forceRegenerate = false)
        {
            string key = BuildKey("bass", seed, bpm, lengthSeconds, rootHz.ToString("F2"));
            if (!forceRegenerate && _cache.TryGetValue(key, out var cached) && cached != null)
            {
                Touch(key);
                return cached;
            }
            var clip = CreateBassLoop(seed, bpm, lengthSeconds, rootHz);
            _cache[key] = clip;
            TrackAdd(key, clip);
            return clip;
        }

        /// <summary>
        /// Creates a bass loop with adjustable complexity. Levels:
        /// 0 = basic root/fifth pulses per beat.
        /// 1 = adds syncopated off-beat notes & occasional fifth inversion.
        /// 2 = adds 16th-note passing tones, octave jumps, and rare chromatic approach tones for more urgency.
        /// </summary>
        /// <param name="seed">Deterministic seed for variation.</param>
        /// <param name="bpm">Beats per minute controlling rhythmic spacing.</param>
        /// <param name="lengthSeconds">Total loop length (will truncate/extend last beat).</param>
        /// <param name="rootHz">Base root frequency (default A1 ≈ 55 Hz).</param>
        /// <param name="complexity">Complexity level (0..2 recommended; values outside are clamped).</param>
        public static AudioClip CreateBassLoopComplex(int seed, float bpm, float lengthSeconds, float rootHz, int complexity)
        {
            Stopwatch sw = null; if (EnableProfiling) { sw = Stopwatch.StartNew(); }
            int sampleRate = ResolveSampleRate();
            int totalSamples = Mathf.Max(8, Mathf.CeilToInt(lengthSeconds * sampleRate));
            float[] data = new float[totalSamples];
            complexity = Mathf.Clamp(complexity, 0, 2);
            System.Random rng = new System.Random(seed * 37 + complexity * 131);

            float secondsPerBeat = 60f / Mathf.Max(1f, bpm);
            int beatSamples = Mathf.Max(1, Mathf.CeilToInt(secondsPerBeat * sampleRate));
            int beats = Mathf.CeilToInt(totalSamples / (float)beatSamples);

            // Helper local function to synth a single bass note into buffer
            void SynthBassNote(float freq, int start, int noteLenSamples, float amp, bool shortEnvelope)
            {
                int len = Mathf.Min(data.Length - start, noteLenSamples);
                for (int i = 0; i < len; i++)
                {
                    int idx = start + i;
                    float t = i / (float)sampleRate;
                    float env = shortEnvelope ? PercEnvelope(i, len, 0.01f, 0.20f) : PercEnvelope(i, len, 0.02f, 0.35f);
                    // slight detune for thickness
                    float det = 1f + (float)(rng.NextDouble() * 0.004 - 0.002);
                    float v = 0.75f * Mathf.Sin(TAU * freq * det * t) + 0.25f * Mathf.Sin(TAU * (freq * 2f) * t * 0.25f);
                    // gentle saturation style soft clip
                    v = (float)System.Math.Tanh(v * 1.4f);
                    data[idx] += v * env * amp;
                }
            }

            for (int b = 0; b < beats; b++)
            {
                int beatStart = b * beatSamples;
                float baseFreq = rootHz;
                // subtle progression mimic: every 4th beat shift root (like pad progression) using a small table
                int progStep = (b / 4) % 4;
                switch (progStep)
                {
                    case 1: baseFreq = rootHz * Mathf.Pow(2f, -5f/12f); break; // F
                    case 2: baseFreq = rootHz * Mathf.Pow(2f, -9f/12f); break; // C
                    case 3: baseFreq = rootHz * Mathf.Pow(2f, -2f/12f); break; // G
                }
                // primary pulse (root or fifth on some beats)
                float primary = (complexity >= 1 && (b % 8 == 5)) ? baseFreq * 1.5f : baseFreq; // occasional fifth inversion
                SynthBassNote(primary, beatStart, beatSamples, 0.55f, false);

                if (complexity >= 1)
                {
                    // syncopated off-beat (the "and") using fifth or octave
                    int offStart = beatStart + beatSamples / 2;
                    if (offStart < totalSamples)
                    {
                        float offFreq = (b % 6 == 3) ? baseFreq * 2f : baseFreq * 1.5f;
                        SynthBassNote(offFreq, offStart, beatSamples / 2, 0.45f, true);
                    }
                }
                if (complexity >= 2)
                {
                    // 16th-note passing tone pattern (two quick notes before next beat)
                    int sixteenth = beatSamples / 4;
                    int passStart1 = beatStart + (int)(sixteenth * 0.75f);
                    int passStart2 = beatStart + (int)(sixteenth * 1.5f);
                    if (passStart2 < beatStart + beatSamples)
                    {
                        // occasional chromatic approach from below
                        float approach = baseFreq * ((b % 7 == 4) ? Mathf.Pow(2f, -1f/12f) : 1f);
                        SynthBassNote(approach, passStart1, sixteenth, 0.38f, true);
                        SynthBassNote(baseFreq * 2f, passStart2, sixteenth, 0.40f, true); // octave pop
                    }
                    // rare extra octave jump on downbeat for energy
                    if (b % 16 == 12)
                    {
                        SynthBassNote(baseFreq * 2f, beatStart, beatSamples / 2, 0.60f, true);
                    }
                }
            }

            // Tone shaping: mild low-pass to thicken fundamentals then normalise
            LowPassInPlace(data, sampleRate, 250f);
            NormaliseInPlace(data, 0.92f);
            var clip = AudioClip.Create("ProcBassC" + complexity + "_" + seed, totalSamples, 1, sampleRate, false);
            clip.SetData(data, 0);
            if (sw != null)
            {
                sw.Stop();
                _profileLog.Add($"BassComplex seed={seed} bpm={bpm:F1} root={rootHz:F1} lvl={complexity} len={lengthSeconds:F1}s ms={sw.ElapsedMilliseconds}");
            }
            return clip;
        }

        /// <summary>
        /// Returns a cached bass loop variant keyed by complexity (0..2) and root; generates if missing.
        /// </summary>
        public static AudioClip GetBassLoopVariant(int seed, float bpm, float lengthSeconds, float rootHz, int complexity, bool forceRegenerate = false)
        {
            string extra = "c:" + Mathf.Clamp(complexity,0,2) + "|root:" + rootHz.ToString("F2");
            string key = BuildKey("bass", seed, bpm, lengthSeconds, extra);
            if (!forceRegenerate && _cache.TryGetValue(key, out var cached) && cached != null)
            {
                Touch(key);
                return cached;
            }
            var clip = CreateBassLoopComplex(seed, bpm, lengthSeconds, rootHz, complexity);
            _cache[key] = clip;
            TrackAdd(key, clip);
            return clip;
        }

        /// <summary>
        /// Creates a very lightweight drum loop (kick/snare/hat) using simple synthesized waveforms.
        /// </summary>
        public static AudioClip CreateDrumLoop(int seed, float bpm, float lengthSeconds)
        {
            Stopwatch sw = null; if (EnableProfiling) { sw = Stopwatch.StartNew(); }
            int sampleRate = ResolveSampleRate();
            int totalSamples = Mathf.Max(8, Mathf.CeilToInt(lengthSeconds * sampleRate));
            float[] data = new float[totalSamples];
            System.Random rng = new System.Random(seed);
            float spb = 60f / Mathf.Max(1f, bpm);
            int beatSamples = Mathf.Max(1, Mathf.CeilToInt(spb * sampleRate));
            int beats = Mathf.CeilToInt(totalSamples / (float)beatSamples);

            for (int b = 0; b < beats; b++)
            {
                int beatStart = b * beatSamples;
                // Kick on beats 0 & 2
                if (b % 4 == 0 || b % 4 == 2)
                {
                    SynthesizeKick(data, beatStart, sampleRate, 0.9f);
                }
                // Snare on beats 2
                if (b % 4 == 2)
                {
                    SynthesizeSnare(data, beatStart, sampleRate, 0.6f);
                }
                // Hats eighth notes
                for (int h = 0; h < 2; h++)
                {
                    int hatStart = beatStart + (h * beatSamples / 2);
                    SynthesizeHat(data, hatStart, sampleRate, 0.15f + 0.05f * (float)rng.NextDouble());
                }
            }
            NormaliseInPlace(data, 0.85f);
            var clip = AudioClip.Create("ProcDrums" + seed, totalSamples, 1, sampleRate, false);
            clip.SetData(data, 0);
            if (sw != null)
            {
                sw.Stop();
                _profileLog.Add($"Drums seed={seed} bpm={bpm:F1} len={lengthSeconds:F1}s ms={sw.ElapsedMilliseconds}");
            }
            return clip;
        }

        /// <summary>Returns a cached drum loop if available, otherwise generates and stores it.</summary>
        public static AudioClip GetDrumLoop(int seed, float bpm, float lengthSeconds, bool forceRegenerate = false)
        {
            string key = BuildKey("drums", seed, bpm, lengthSeconds, "-");
            if (!forceRegenerate && _cache.TryGetValue(key, out var cached) && cached != null)
            {
                Touch(key);
                return cached;
            }
            var clip = CreateDrumLoop(seed, bpm, lengthSeconds);
            _cache[key] = clip;
            TrackAdd(key, clip);
            return clip;
        }

        /// <summary>
        /// Creates a drum loop with adjustable complexity: 0 minimal, 1 adds extra hats & ghost snare, 2 adds fills & offbeat kicks.
        /// </summary>
    /// <summary>
    /// Creates a drum loop with adjustable complexity. Complexity levels:
    /// 0 = minimal (basic kick/snare + hats), 1 = adds ghost snare & 16th hats, 2 = adds offbeat kicks + periodic fills.
    /// </summary>
    /// <param name="seed">Deterministic seed for variation.</param>
    /// <param name="bpm">Beats per minute driving pattern timing.</param>
    /// <param name="lengthSeconds">Total loop length.</param>
    /// <param name="complexity">Integer complexity level (0..2 recommended).</param>
    public static AudioClip CreateDrumLoopComplex(int seed, float bpm, float lengthSeconds, int complexity)
        {
            Stopwatch sw = null; if (EnableProfiling) { sw = Stopwatch.StartNew(); }
            int sampleRate = ResolveSampleRate();
            int totalSamples = Mathf.Max(8, Mathf.CeilToInt(lengthSeconds * sampleRate));
            float[] data = new float[totalSamples];
            System.Random rng = new System.Random(seed * 17 + complexity);
            float spb = 60f / Mathf.Max(1f, bpm);
            int beatSamples = Mathf.Max(1, Mathf.CeilToInt(spb * sampleRate));
            int beats = Mathf.CeilToInt(totalSamples / (float)beatSamples);
            // base pattern similar to CreateDrumLoop, augmented by complexity
            for (int b = 0; b < beats; b++)
            {
                int beatStart = b * beatSamples;
                bool kickBeat = (b % 4 == 0 || b % 4 == 2);
                if (kickBeat)
                {
                    SynthesizeKick(data, beatStart, sampleRate, 0.9f);
                }
                if (b % 4 == 2)
                {
                    SynthesizeSnare(data, beatStart, sampleRate, 0.6f);
                }
                // Complexity-based additions
                if (complexity >= 1)
                {
                    // Ghost snare on beat 3 (index 2) midway
                    if (b % 4 == 2)
                    {
                        SynthesizeSnare(data, beatStart + beatSamples / 2, sampleRate, 0.25f);
                    }
                }
                if (complexity >= 2)
                {
                    // Offbeat kick on the 'and' of beat (except where main kick already present)
                    int offStart = beatStart + beatSamples / 2;
                    if (!kickBeat)
                    {
                        SynthesizeKick(data, offStart, sampleRate, 0.55f);
                    }
                    // Simple fill every 8 beats: rapid hats
                    if (b % 8 == 7)
                    {
                        int fillCount = 6;
                        int fillLen = beatSamples / 3;
                        for (int f = 0; f < fillCount; f++)
                        {
                            int hatStart = beatStart + (f * fillLen / fillCount);
                            SynthesizeHat(data, hatStart, sampleRate, 0.18f + 0.05f * (float)rng.NextDouble());
                        }
                    }
                }
                // Hats: base eighth notes
                for (int h = 0; h < 2; h++)
                {
                    int hatStart = beatStart + (h * beatSamples / 2);
                    float amp = 0.15f + 0.05f * (float)rng.NextDouble();
                    SynthesizeHat(data, hatStart, sampleRate, amp);
                    // Complexity 1+: add 16th hat in between
                    if (complexity >= 1)
                    {
                        int mid = hatStart + beatSamples / 4;
                        if (mid < beatStart + beatSamples)
                        {
                            SynthesizeHat(data, mid, sampleRate, amp * 0.7f);
                        }
                    }
                }
            }
            NormaliseInPlace(data, 0.85f);
            var clip = AudioClip.Create("ProcDrumsC" + complexity + "_" + seed, totalSamples, 1, sampleRate, false);
            clip.SetData(data, 0);
            if (sw != null)
            {
                sw.Stop();
                _profileLog.Add($"DrumsComplex seed={seed} bpm={bpm:F1} lvl={complexity} len={lengthSeconds:F1}s ms={sw.ElapsedMilliseconds}");
            }
            return clip;
        }

        /// <summary>Returns a cached drum loop variant keyed by complexity; generates if missing.</summary>
    /// <summary>
    /// Returns a cached drum loop variant keyed by complexity (0..2) or generates if absent.
    /// </summary>
    public static AudioClip GetDrumLoopVariant(int seed, float bpm, float lengthSeconds, int complexity, bool forceRegenerate = false)
        {
            string extra = "c:" + complexity;
            string key = BuildKey("drums", seed, bpm, lengthSeconds, extra);
            if (!forceRegenerate && _cache.TryGetValue(key, out var cached) && cached != null)
            {
                Touch(key);
                return cached;
            }
            var clip = CreateDrumLoopComplex(seed, bpm, lengthSeconds, complexity);
            _cache[key] = clip;
            TrackAdd(key, clip);
            return clip;
        }

        /// <summary>
        /// Creates a simple arpeggio loop across provided chord roots.
        /// </summary>
        public static AudioClip CreateArpLoop(int seed, float bpm, float lengthSeconds, float[] chordRoots = null)
        {
            Stopwatch sw = null; if (EnableProfiling) { sw = Stopwatch.StartNew(); }
            int sampleRate = ResolveSampleRate();
            int totalSamples = Mathf.Max(8, Mathf.CeilToInt(lengthSeconds * sampleRate));
            float[] data = new float[totalSamples];
            System.Random rng = new System.Random(seed);
            var roots = chordRoots == null || chordRoots.Length == 0 ? new[] { 220f, 174.61f, 130.81f, 196f } : chordRoots;
            float spb = 60f / Mathf.Max(1f, bpm);
            int noteSamples = Mathf.Max(1, Mathf.CeilToInt(spb / 2f * sampleRate)); // 8th notes
            int notes = Mathf.CeilToInt(totalSamples / (float)noteSamples);
            for (int n = 0; n < notes; n++)
            {
                int start = n * noteSamples;
                int end = Mathf.Min(totalSamples, start + noteSamples);
                float root = roots[(n / 4) % roots.Length];
                // pattern degrees
                float[] degrees = { 1f, 1.5f, 2f, 1.5f }; // root, fifth, octave, fifth
                float chosen = root * degrees[n % degrees.Length];
                int len = end - start;
                for (int i = 0; i < len; i++)
                {
                    int idx = start + i;
                    float t = idx / (float)sampleRate;
                    float env = PercEnvelope(i, len, 0.005f, 0.25f);
                    float v = Mathf.Sin(TAU * chosen * t) * env * 0.4f;
                    // slight vibrato
                    v *= 1f + 0.01f * Mathf.Sin(TAU * 6f * t);
                    data[idx] += v;
                }
            }
            HighPassInPlace(data, sampleRate, 300f);
            NormaliseInPlace(data, 0.9f);
            var clip = AudioClip.Create("ProcArp" + seed, totalSamples, 1, sampleRate, false);
            clip.SetData(data, 0);
            if (sw != null)
            {
                sw.Stop();
                _profileLog.Add($"Arp seed={seed} bpm={bpm:F1} len={lengthSeconds:F1}s ms={sw.ElapsedMilliseconds}");
            }
            return clip;
        }

        /// <summary>
        /// Creates a short one-shot stinger clip for reactive musical accents.
        /// Type 'rise' = upward pitch sweep pad; 'hit' = percussive impact.
        /// </summary>
    public static AudioClip CreateStingerClip(int seed, string type = "rise", float lengthSeconds = 2f)
        {
            Stopwatch sw = null; if (EnableProfiling) { sw = Stopwatch.StartNew(); }
            int sampleRate = ResolveSampleRate();
            int totalSamples = Mathf.Max(8, Mathf.CeilToInt(lengthSeconds * sampleRate));
            float[] data = new float[totalSamples];
            System.Random rng = new System.Random(seed);
            if (type == null) { type = "rise"; }
            switch (type)
            {
                case "hit":
                {
                    for (int i = 0; i < totalSamples; i++)
                    {
                        float t = i / (float)sampleRate;
                        float noise = (float)(rng.NextDouble() * 2 - 1);
                        float tone = Mathf.Sin(TAU * Mathf.Lerp(220f, 110f, t) * t);
                        float env = Mathf.Exp(-6f * t);
                        data[i] += (tone * 0.4f + noise * 0.6f) * env;
                    }
                    break;
                }
                case "rise":
                default:
                {
                    for (int i = 0; i < totalSamples; i++)
                    {
                        float t = i / (float)sampleRate;
                        float freq = Mathf.Lerp(180f, 360f, Mathf.Pow(t, 0.85f));
                        float env = Mathf.Sin(Mathf.Clamp01(t / lengthSeconds) * Mathf.PI * 0.5f) * Mathf.Exp(-2f * Mathf.Max(0f, t - lengthSeconds * 0.7f));
                        float v = Mathf.Sin(TAU * freq * t) * env;
                        // subtle shimmer
                        v += 0.25f * Mathf.Sin(TAU * (freq * 1.01f) * t) * env * 0.5f;
                        data[i] += v;
                    }
                    break;
                }
            }
            NormaliseInPlace(data, 0.95f);
            var clip = AudioClip.Create("ProcStinger_" + type + "_" + seed, totalSamples, 1, sampleRate, false);
            clip.SetData(data, 0);
            if (sw != null)
            {
                sw.Stop();
                _profileLog.Add($"Stinger type={type} seed={seed} len={lengthSeconds:F1}s ms={sw.ElapsedMilliseconds}");
            }
            return clip;
        }

        /// <summary>Returns a cached stinger clip; generates if missing.</summary>
    public static AudioClip GetStingerClip(int seed, string type = "rise", float lengthSeconds = 2f, bool forceRegenerate = false)
        {
            string key = BuildKey("stinger", seed, 0f, lengthSeconds, type);
            if (!forceRegenerate && _cache.TryGetValue(key, out var cached) && cached != null)
            {
                Touch(key);
                return cached;
            }
            var clip = CreateStingerClip(seed, type, lengthSeconds);
            _cache[key] = clip;
            TrackAdd(key, clip);
            return clip;
        }

        /// <summary>
        /// Creates a short percussive fill (snare roll + hats + kick pickup). Intended as a one-shot overlay (~1 beat).
        /// </summary>
        public static AudioClip CreateDrumFillClip(int seed, float bpm, float lengthSeconds = 0.8f)
        {
            Stopwatch sw = null; if (EnableProfiling) { sw = Stopwatch.StartNew(); }
            int sampleRate = ResolveSampleRate();
            float spb = 60f / Mathf.Max(1f, bpm);
            if (lengthSeconds <= 0f) { lengthSeconds = spb; }
            int totalSamples = Mathf.Max(8, Mathf.CeilToInt(lengthSeconds * sampleRate));
            float[] data = new float[totalSamples];
            System.Random rng = new System.Random(seed * 911);

            // quick kick pickup at start
            SynthesizeKick(data, 0, sampleRate, 0.7f);
            // snare roll: multiple staggered quiet snares
            int hits = 6;
            for (int h = 1; h <= hits; h++)
            {
                int pos = (int)((h / (float)(hits + 1)) * totalSamples);
                float amp = Mathf.Lerp(0.25f, 0.6f, h / (float)hits);
                SynthesizeSnare(data, pos, sampleRate, amp);
            }
            // rapid hats sprinkled throughout
            int hatCount = 8;
            for (int i = 0; i < hatCount; i++)
            {
                int pos = (int)((i / (float)hatCount) * totalSamples);
                SynthesizeHat(data, pos, sampleRate, 0.18f + 0.06f * (float)rng.NextDouble());
            }
            NormaliseInPlace(data, 0.95f);
            var clip = AudioClip.Create("ProcDrumFill_" + seed, totalSamples, 1, sampleRate, false);
            clip.SetData(data, 0);
            if (sw != null)
            {
                sw.Stop();
                _profileLog.Add($"DrumFill seed={seed} bpm={bpm:F1} len={lengthSeconds:F2}s ms={sw.ElapsedMilliseconds}");
            }
            return clip;
        }

        /// <summary>Returns a cached drum fill clip; generates if missing.</summary>
        public static AudioClip GetDrumFillClip(int seed, float bpm, float lengthSeconds = 0.8f, bool forceRegenerate = false)
        {
            string key = BuildKey("drumfill", seed, bpm, lengthSeconds, "-");
            if (!forceRegenerate && _cache.TryGetValue(key, out var cached) && cached != null)
            {
                Touch(key);
                return cached;
            }
            var clip = CreateDrumFillClip(seed, bpm, lengthSeconds);
            _cache[key] = clip;
            TrackAdd(key, clip);
            return clip;
        }

        /// <summary>Returns a cached arpeggio loop if available, otherwise generates and stores it.</summary>
        public static AudioClip GetArpLoop(int seed, float bpm, float lengthSeconds, float[] chordRoots = null, bool forceRegenerate = false)
        {
            string extra = chordRoots == null ? "default" : string.Join(",", System.Array.ConvertAll(chordRoots, r => r.ToString("F2")));
            string key = BuildKey("arp", seed, bpm, lengthSeconds, extra);
            if (!forceRegenerate && _cache.TryGetValue(key, out var cached) && cached != null)
            {
                Touch(key);
                return cached;
            }
            var clip = CreateArpLoop(seed, bpm, lengthSeconds, chordRoots);
            _cache[key] = clip;
            TrackAdd(key, clip);
            return clip;
        }

    /// <summary>
    /// Returns a cached arpeggio loop variant for a given progression index; generates if missing.
    /// </summary>
        public static AudioClip GetArpLoopVariant(int seed, float bpm, float lengthSeconds, int progressionIndex, bool forceRegenerate = false)
        {
            float[] roots = GetChordProgressionVariant(progressionIndex);
            string extra = "var:" + progressionIndex;
            string key = BuildKey("arp", seed, bpm, lengthSeconds, extra);
            if (!forceRegenerate && _cache.TryGetValue(key, out var cached) && cached != null)
            {
                Touch(key);
                return cached;
            }
            var clip = CreateArpLoop(seed, bpm, lengthSeconds, roots);
            _cache[key] = clip;
            TrackAdd(key, clip);
            return clip;
        }
        #endregion

        #region Internal DSP Helpers
        private const float TAU = Mathf.PI * 2f;

        private static int ResolveSampleRate() => AudioSettings.outputSampleRate > 0 ? AudioSettings.outputSampleRate : 44100;

        private static float PadEnvelope(int i, int len)
        {
            float attack = Mathf.Clamp01(i / (len * 0.15f));
            float release = 1f - Mathf.Clamp01((i - len * 0.75f) / (len * 0.25f));
            return Mathf.Min(attack, release);
        }

        private static float PercEnvelope(int i, int len, float attackFrac, float decayFrac)
        {
            float aEnd = len * attackFrac;
            float dEnd = len * decayFrac;
            if (i < aEnd)
            {
                return i / Mathf.Max(1f, aEnd);
            }
            float d = 1f - (i - aEnd) / Mathf.Max(1f, (dEnd - aEnd));
            return Mathf.Clamp01(d);
        }

        private static void NormaliseInPlace(float[] data, float targetPeak)
        {
            float peak = 0f;
            for (int i = 0; i < data.Length; i++)
            {
                float a = Mathf.Abs(data[i]);
                if (a > peak)
                {
                    peak = a;
                }
            }
            if (peak < 1e-6f)
            {
                return;
            }
            float mul = targetPeak / peak;
            for (int i = 0; i < data.Length; i++)
            {
                data[i] *= mul;
            }
        }

        // Simple one-pole low pass
        private static void LowPassInPlace(float[] data, int sampleRate, float cutoff)
        {
            float rc = 1f / (cutoff * TAU);
            float dt = 1f / sampleRate;
            float alpha = dt / (rc + dt);
            float prev = 0f;
            for (int i = 0; i < data.Length; i++)
            {
                prev = prev + alpha * (data[i] - prev);
                data[i] = prev;
            }
        }

        private static void HighPassInPlace(float[] data, int sampleRate, float cutoff)
        {
            float rc = 1f / (cutoff * TAU);
            float dt = 1f / sampleRate;
            float alpha = rc / (rc + dt);
            float prev = data[0];
            float hp = 0f;
            for (int i = 0; i < data.Length; i++)
            {
                hp = alpha * (hp + data[i] - prev);
                prev = data[i];
                data[i] = hp;
            }
        }

        private static void SynthesizeKick(float[] data, int start, int sampleRate, float amp)
        {
            int len = Mathf.Min(data.Length - start, sampleRate / 8); // 125ms
            for (int i = 0; i < len; i++)
            {
                int idx = start + i;
                float t = i / (float)sampleRate;
                float freq = Mathf.Lerp(80f, 40f, i / (float)len); // downward sweep
                float env = Mathf.Exp(-6f * t);
                float v = Mathf.Sin(TAU * freq * t) * env * amp;
                data[idx] += v;
            }
        }

        private static void SynthesizeSnare(float[] data, int start, int sampleRate, float amp)
        {
            int len = Mathf.Min(data.Length - start, sampleRate / 6); // ~166ms
            System.Random rng = new System.Random(start + data.Length);
            for (int i = 0; i < len; i++)
            {
                int idx = start + i;
                float t = i / (float)sampleRate;
                float noise = (float)(rng.NextDouble() * 2 - 1);
                float tone = Mathf.Sin(TAU * 180f * t) * Mathf.Exp(-10f * t);
                float env = Mathf.Exp(-12f * t);
                data[idx] += (tone * 0.3f + noise * 0.7f) * env * amp;
            }
        }

        private static void SynthesizeHat(float[] data, int start, int sampleRate, float amp)
        {
            int len = Mathf.Min(data.Length - start, sampleRate / 16); // ~62ms
            System.Random rng = new System.Random(start * 17 + data.Length);
            for (int i = 0; i < len; i++)
            {
                int idx = start + i;
                float t = i / (float)sampleRate;
                float noise = (float)(rng.NextDouble() * 2 - 1);
                float env = Mathf.Exp(-35f * t);
                data[idx] += noise * env * amp;
            }
        }
        #endregion
    }
}
