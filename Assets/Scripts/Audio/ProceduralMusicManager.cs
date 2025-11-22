using UnityEngine;

namespace Hardwareless.Audio
{
    /// <summary>
    /// Runtime manager that layers procedurally generated loops (pad, bass, drums, arp) based on intensity.
    /// Attach to a GameObject with multiple child AudioSources or let it create them.
    /// </summary>
    [AddComponentMenu("Hardwareless/Audio/Procedural Music Manager")]
    public class ProceduralMusicManager : MonoBehaviour
    {
        [Range(0f,1f)] public float intensity = 0f; // 0 = calm, 1 = full action
    [Tooltip("Generation seed; change for different musical textures.")]
        public int seed = 12345;
        [Tooltip("Beats per minute used for procedural generation.")]
        public float bpm = 90f;
        [Tooltip("Loop length in seconds (all layers share length).")]
        public float loopLengthSeconds = 12f;
        [Tooltip("Auto generate on Start.")]
    public bool autoGenerate = true;
    /// <summary>Automatically advance harmonic progression over time.</summary>
    [Tooltip("Automatically advance harmonic progression over time.")]
    public bool enableAutoProgression = true;
    /// <summary>Base seconds between automatic progression advances.</summary>
    [Tooltip("Base seconds between automatic progression advances.")]
    public float autoProgressionInterval = 60f;
    /// <summary>Random +/- jitter applied to interval (seconds).</summary>
    [Tooltip("Random +/- jitter applied to interval (seconds).")]
    public float autoProgressionJitter = 10f;
    /// <summary>Enable dynamic low-pass filtering of pad based on intensity (darker when calm).</summary>
    [Tooltip("Enable dynamic low-pass filtering of pad based on intensity (darker when calm).")]
    public bool enablePadFilterDynamics = true;
    /// <summary>Pad low-pass cutoff at minimum intensity.</summary>
    [Tooltip("Pad low-pass cutoff at minimum intensity.")]
    public float padMinCutoff = 900f;
    /// <summary>Pad low-pass cutoff at maximum intensity.</summary>
    [Tooltip("Pad low-pass cutoff at maximum intensity.")]
    public float padMaxCutoff = 4800f;
    [Tooltip("Enable adaptive regeneration & crossfade of drum loop complexity based on intensity.")]
    public bool adaptiveDrumComplexity = true;
    [Tooltip("Seconds used for crossfading when drum complexity changes.")] public float drumComplexityCrossfadeSeconds = 2f;
    [Tooltip("Intensity threshold separating low/medium complexity (0->1).")]
    [Range(0f,1f)] public float drumMediumThreshold = 0.35f;
    [Tooltip("Intensity threshold separating medium/high complexity (1->2).")]
    [Range(0f,1f)] public float drumHighThreshold = 0.7f;

    [Tooltip("Enable adaptive regeneration & crossfade of bass loop complexity based on intensity.")]
    public bool adaptiveBassComplexity = true;
    [Tooltip("Seconds used for crossfading when bass complexity changes.")] public float bassComplexityCrossfadeSeconds = 2f;
    /// <summary>Intensity threshold separating low/medium bass complexity (0->1).</summary>
    [Tooltip("Intensity threshold separating low/medium bass complexity (0->1).")]
    [Range(0f,1f)] public float bassMediumThreshold = 0.4f;
    /// <summary>Intensity threshold separating medium/high bass complexity (1->2).</summary>
    [Tooltip("Intensity threshold separating medium/high bass complexity (1->2).")]
    [Range(0f,1f)] public float bassHighThreshold = 0.75f;
    /// <summary>Enable adaptive rich harmonic pad variants (adds chord extensions & shimmer at higher intensity).</summary>
    [Tooltip("Enable adaptive rich harmonic pad variants (adds extensions & shimmer at higher intensity).")]
    public bool adaptivePadRichness = true;
    /// <summary>Intensity threshold separating base/rich pad (0->1).</summary>
    [Tooltip("Intensity threshold separating base/rich pad (0->1).")]
    [Range(0f,1f)] public float padRichnessMediumThreshold = 0.45f;
    /// <summary>Intensity threshold separating rich/very rich pad (1->2).</summary>
    [Tooltip("Intensity threshold separating rich/very rich pad (1->2).")]
    [Range(0f,1f)] public float padRichnessHighThreshold = 0.8f;
    /// <summary>Seconds used for crossfading when pad richness changes.</summary>
    [Tooltip("Seconds used for crossfading when pad richness changes.")]
    public float padRichnessCrossfadeSeconds = 3f;
    /// <summary>Link bass root transposition to harmonic progression variant shifts.</summary>
    [Tooltip("Link bass root transposition to harmonic progression variant shifts.")]
    public bool linkBassToProgression = true;
    /// <summary>Base bass root frequency (A1 ≈ 55Hz). Used as starting point for transposition when progression advances.</summary>
    [Tooltip("Base bass root frequency (A1 ≈ 55Hz). Starting point for transposition.")]
    public float bassBaseRootHz = 55f;
    /// <summary>Seconds used for bass crossfade when progression changes (if linked). Defaults to progression crossfade if <= 0.</summary>
    [Tooltip("Seconds used for bass crossfade when progression changes (if linked). Use 0 to reuse progression crossfade.")]
    public float bassProgressionCrossfadeSeconds = 2.5f;
    /// <summary>Align progression changes to the next chord boundary for cleaner musical timing.</summary>
    [Tooltip("Align progression changes to the next chord boundary for cleaner timing.")]
    public bool alignProgressionToChord = true;
    /// <summary>Beats per chord used for alignment (pad/arp use 4 by default).</summary>
    [Tooltip("Beats per chord used for alignment (pad/arp default = 4).")]
    public int beatsPerChord = 4;

    [Header("Manual Overrides")]
    [Tooltip("Lock drum complexity to a fixed level (0..2), overriding intensity adaptation.")]
    public bool lockDrumComplexity = false;
    [Range(0,2)] public int lockedDrumComplexity = 0;
    [Tooltip("Lock bass complexity to a fixed level (0..2), overriding intensity adaptation.")]
    public bool lockBassComplexity = false;
    [Range(0,2)] public int lockedBassComplexity = 0;
    [Tooltip("Lock pad richness to a fixed level (0..2), overriding intensity adaptation.")]
    public bool lockPadRichness = false;
    [Range(0,2)] public int lockedPadRichness = 0;

    private AudioSource padSource;
    private AudioSource bassSource;
    private AudioSource drumSource;
    private AudioSource arpSource;
    private AudioSource stingerSource; // one-shot reactive accents
    private AudioSource percFillSource; // one-shot percussive fills
    [Tooltip("Play a short percussive fill automatically when progression changes.")]
    public bool playFillOnProgressionChange = true;
    /// <summary>Optionally align reactive stingers to the next rhythmic subdivision for musical tightness.</summary>
    [Tooltip("Align reactive stingers to the next rhythmic subdivision.")]
    public bool alignStingersToBeat = false;
    /// <summary>Subdivision for stinger alignment: 1=beat, 2=eighth, 4=sixteenth.</summary>
    [Tooltip("Subdivision for stinger alignment: 1=beat, 2=eighth, 4=sixteenth.")]
    [Range(1,4)] public int stingerSubdivision = 1;
    /// <summary>Optionally align percussive fills to the next rhythmic subdivision.</summary>
    [Tooltip("Align percussive fills to the next rhythmic subdivision.")]
    public bool alignFillsToBeat = true;
    /// <summary>Subdivision for fill alignment: 1=beat, 2=eighth, 4=sixteenth.</summary>
    [Tooltip("Subdivision for fill alignment: 1=beat, 2=eighth, 4=sixteenth.")]
    [Range(1,4)] public int fillSubdivision = 1;
        [Header("Mixer")]
        [Tooltip("Mute individual layers.")]
        public bool mutePad = false;
        public bool muteBass = false;
        public bool muteDrums = false;
        public bool muteArp = false;
        [Tooltip("Per-layer trim (linear multiplier). 1 = unity.")]
        [Range(0f,2f)] public float trimPad = 1f;
        [Range(0f,2f)] public float trimBass = 1f;
        [Range(0f,2f)] public float trimDrums = 1f;
        [Range(0f,2f)] public float trimArp = 1f;
        [Tooltip("Solo switches (if any solo is on, only soloed layers are heard).")]
        public bool soloPad = false;
        public bool soloBass = false;
        public bool soloDrums = false;
        public bool soloArp = false;
        [Header("Persistence")]
        [Tooltip("Persist runtime settings via PlayerPrefs and auto-load on start.")]
        public bool persistRuntimeSettings = true;
        [Tooltip("Automatically save settings on application quit.")]
        public bool autoSaveOnQuit = true;
        [Tooltip("Automatically save when settings change (throttled).")]
        public bool autoSaveOnChange = false;
        private int _lastSettingsSig;
        private float _lastAutoSaveTime;
        private const float AutoSaveMinInterval = 1f;
    [Header("Event Ducking")]
    [Tooltip("Ducks pad/arp briefly when stingers or fills play for clarity.")]
    public bool enableDucking = true;
    [Tooltip("Pad volume reduction at full duck (0..1).")]
    [Range(0f,1f)] public float duckPadAmount = 0.5f;
    [Tooltip("Arp volume reduction at full duck (0..1).")]
    [Range(0f,1f)] public float duckArpAmount = 0.6f;
    [Tooltip("Duck attack seconds (fade-in of ducking).")]
    public float duckAttackSeconds = 0.02f;
    [Tooltip("Duck hold seconds at full reduction.")]
    public float duckHoldSeconds = 0.08f;
    [Tooltip("Duck release seconds (recover back to normal).")]
    public float duckReleaseSeconds = 0.35f;

    private AudioClip padClip;
    private AudioClip bassClip;
    private AudioClip drumClip;
    private AudioClip arpClip;
    private AudioClip stingerClip;

        private float _lastIntensity;
        private Coroutine _intensityRoutine; // active smoothing coroutine
    private int _progressionIndex; // current harmonic variant index
    [Tooltip("Seconds used for crossfading when advancing progression.")]
    /// <summary>Seconds used for crossfading when advancing harmonic progression.</summary>
    public float progressionCrossfadeSeconds = 3f;
    private int _drumComplexity; // current drum complexity (0,1,2)
    private int _bassComplexity; // current bass complexity (0,1,2)
    private float _currentBassRootHz; // current transposed bass root
        private float _nextAutoProgressionTime;
        private AudioLowPassFilter _padLpf;
        private int _padRichness; // current pad richness level (0..2)
        private Coroutine _pendingProgressionRoutine;
        private Coroutine _duckRoutine;
        private float _duckLevel; // 0..1

        /// <summary>Current progression variant index.</summary>
        public int CurrentProgressionIndex => _progressionIndex;
        /// <summary>Current drum complexity (0..2).</summary>
        public int CurrentDrumComplexity => _drumComplexity;
        /// <summary>Current bass complexity (0..2).</summary>
        public int CurrentBassComplexity => _bassComplexity;
        /// <summary>Current pad richness (0..2).</summary>
        public int CurrentPadRichness => _padRichness;
        /// <summary>Current bass root frequency in Hz (after transposition if linked).</summary>
        public float CurrentBassRootHz => _currentBassRootHz;
        /// <summary>Current pad low-pass cutoff; -1 if filter not present.</summary>
        public float CurrentPadCutoff => _padLpf != null ? _padLpf.cutoffFrequency : -1f;
        /// <summary>Seconds remaining until the next auto progression (or -1 if disabled/not scheduled).</summary>
        public float NextAutoProgressionInSeconds
        {
            get
            {
                if (!enableAutoProgression) return -1f;
                float t = _nextAutoProgressionTime - Time.time;
                return t > 0f ? t : 0f;
            }
        }
        /// <summary>Read-only accessors for HUD/testing.</summary>
        public AudioSource PadSource => padSource;
        public AudioSource BassSource => bassSource;
        public AudioSource DrumSource => drumSource;
        public AudioSource ArpSource => arpSource;

        private void Awake()
        {
            EnsureSources();
            if (persistRuntimeSettings)
            {
                LoadRuntimeSettings(false);
            }
            _lastSettingsSig = ComputeSettingsSignature();
            _lastAutoSaveTime = Time.unscaledTime;
        }

        private void Start()
        {
            if (autoGenerate)
            {
                GenerateAll();
                PlayAll();
                ApplyIntensityImmediate();
            }
            ScheduleNextProgression();
        }

        /// <summary>Regenerates every layer (pad, bass, drums, arp) with base (non-variant) progression.</summary>
        public void GenerateAll()
        {
            // Determine initial desired states based on overrides or thresholds
            int desiredDrum = lockDrumComplexity ? Mathf.Clamp(lockedDrumComplexity, 0, 2) : (adaptiveDrumComplexity ? 0 : 0);
            if (!lockDrumComplexity && adaptiveDrumComplexity)
            {
                if (intensity >= drumHighThreshold) desiredDrum = 2; else if (intensity >= drumMediumThreshold) desiredDrum = 1; else desiredDrum = 0;
            }
            int desiredBass = lockBassComplexity ? Mathf.Clamp(lockedBassComplexity, 0, 2) : (adaptiveBassComplexity ? 0 : 0);
            if (!lockBassComplexity && adaptiveBassComplexity)
            {
                if (intensity >= bassHighThreshold) desiredBass = 2; else if (intensity >= bassMediumThreshold) desiredBass = 1; else desiredBass = 0;
            }
            int desiredPadRich = lockPadRichness ? Mathf.Clamp(lockedPadRichness, 0, 2) : (adaptivePadRichness ? 0 : 0);
            if (!lockPadRichness && adaptivePadRichness)
            {
                if (intensity >= padRichnessHighThreshold) desiredPadRich = 2; else if (intensity >= padRichnessMediumThreshold) desiredPadRich = 1; else desiredPadRich = 0;
            }

            // Build pad according to current progression and desired richness
            if (adaptivePadRichness || lockPadRichness)
            {
                padClip = ProceduralMusic.GetPadLoopVariantRich(seed, bpm, loopLengthSeconds, _progressionIndex, desiredPadRich);
                _padRichness = desiredPadRich;
            }
            else
            {
                padClip = ProceduralMusic.GetPadLoop(seed, bpm, loopLengthSeconds);
                _padRichness = 0;
            }
            _currentBassRootHz = linkBassToProgression ? ComputeBassRootForProgression(_progressionIndex) : bassBaseRootHz;
            bassClip = (adaptiveBassComplexity || lockBassComplexity)
                ? ProceduralMusic.GetBassLoopVariant(seed + 11, bpm, loopLengthSeconds, _currentBassRootHz, desiredBass)
                : ProceduralMusic.GetBassLoop(seed + 11, bpm, loopLengthSeconds, _currentBassRootHz);
            drumClip = (adaptiveDrumComplexity || lockDrumComplexity)
                ? ProceduralMusic.GetDrumLoopVariant(seed + 29, bpm, loopLengthSeconds, desiredDrum)
                : ProceduralMusic.GetDrumLoop(seed + 29, bpm, loopLengthSeconds);
            arpClip = ProceduralMusic.GetArpLoop(seed + 53, bpm, loopLengthSeconds);
            AssignClip(padSource, padClip, true);
            AssignClip(bassSource, bassClip, true);
            AssignClip(drumSource, drumClip, true);
            AssignClip(arpSource, arpClip, true);
            _drumComplexity = (adaptiveDrumComplexity || lockDrumComplexity) ? desiredDrum : 0;
            _bassComplexity = (adaptiveBassComplexity || lockBassComplexity) ? desiredBass : 0;
        }

        /// <summary>
        /// Advances harmonic progression variant and crossfades pad + arp layers to new clips.
        /// Uses small semitone shifts for gentle modulation; bass & drums remain unchanged.
        /// </summary>
        /// <param name="steps">Variant steps forward (can be negative). Each step shifts progression by 2 semitones (clamped).</param>
        public void AdvanceProgression(int steps = 1)
        {
            _progressionIndex += steps;
            var newPad = ProceduralMusic.GetPadLoopVariant(seed, bpm, loopLengthSeconds, _progressionIndex);
            var newArp = ProceduralMusic.GetArpLoopVariant(seed + 53, bpm, loopLengthSeconds, _progressionIndex);
            CrossfadeSource(ref padSource, ref padClip, newPad, progressionCrossfadeSeconds);
            CrossfadeSource(ref arpSource, ref arpClip, newArp, progressionCrossfadeSeconds);
            if (linkBassToProgression)
            {
                RebuildBassClipForProgression();
            }
            if (playFillOnProgressionChange)
            {
                PlayDrumFill();
            }
        }

        public void PlayAll()
        {
            if (padClip != null && !padSource.isPlaying)
            {
                padSource.Play();
            }
            if (bassClip != null && !bassSource.isPlaying)
            {
                bassSource.Play();
            }
            if (drumClip != null && !drumSource.isPlaying)
            {
                drumSource.Play();
            }
            if (arpClip != null && !arpSource.isPlaying)
            {
                arpSource.Play();
            }
        }

        public void StopAll()
        {
            padSource.Stop();
            bassSource.Stop();
            drumSource.Stop();
            arpSource.Stop();
        }

        /// <summary>
        /// Update intensity and smoothly fade volumes.
        /// </summary>
        public void SetIntensity(float value)
        {
            intensity = Mathf.Clamp01(value);
            ApplyIntensityImmediate();
        }

        /// <summary>
        /// Smoothly ramps intensity to target over given seconds using linear interpolation and per-frame volume updates.
        /// </summary>
        /// <param name="target">Destination intensity 0..1.</param>
        /// <param name="seconds">Fade duration in seconds (clamped >= 0.01).</param>
        public void SetIntensitySmooth(float target, float seconds)
        {
            target = Mathf.Clamp01(target);
            seconds = Mathf.Max(0.01f, seconds);
            if (_intensityRoutine != null)
            {
                StopCoroutine(_intensityRoutine);
            }
            _intensityRoutine = StartCoroutine(SmoothIntensityRoutine(target, seconds));
        }

        private void Update()
        {
            if (Mathf.Abs(intensity - _lastIntensity) > 0.001f)
            {
                ApplyIntensitySmooth();
                _lastIntensity = intensity;
                if (adaptiveDrumComplexity)
                {
                    EvaluateDrumComplexity();
                }
                if (adaptiveBassComplexity)
                {
                    EvaluateBassComplexity();
                }
                if (adaptivePadRichness)
                {
                    EvaluatePadRichness();
                }
            }
            if (enableAutoProgression && Time.time >= _nextAutoProgressionTime)
            {
                if (alignProgressionToChord && padSource != null && padSource.clip != null)
                {
                    QueueAlignedProgression(1);
                }
                else
                {
                    AdvanceProgression(1);
                }
                ScheduleNextProgression();
            }
            // Auto-save on change (throttled)
            if (persistRuntimeSettings)
            {
                int sig = ComputeSettingsSignature();
                if (sig != _lastSettingsSig)
                {
                    _lastSettingsSig = sig;
                    if (autoSaveOnChange && (Time.unscaledTime - _lastAutoSaveTime) > AutoSaveMinInterval)
                    {
                        SaveRuntimeSettings();
                        _lastAutoSaveTime = Time.unscaledTime;
                    }
                }
            }
        }

        private int ComputeSettingsSignature()
        {
            int h = 17;
            // helper local
            int Q(float v) => Mathf.RoundToInt(v * 1000f);
            void Mix(int v) { unchecked { h = h * 31 + v; } }
            // Core
            Mix(Q(intensity));
            Mix(Q(bpm));
            Mix(enableAutoProgression ? 1 : 0);
            Mix(Q(autoProgressionInterval));
            Mix(Q(autoProgressionJitter));
            Mix(alignProgressionToChord ? 1 : 0);
            Mix(beatsPerChord);
            Mix(alignStingersToBeat ? 1 : 0);
            Mix(stingerSubdivision);
            Mix(alignFillsToBeat ? 1 : 0);
            Mix(fillSubdivision);
            Mix(enablePadFilterDynamics ? 1 : 0);
            Mix(linkBassToProgression ? 1 : 0);
            // Overrides
            Mix(lockDrumComplexity ? 1 : 0);
            Mix(lockedDrumComplexity);
            Mix(lockBassComplexity ? 1 : 0);
            Mix(lockedBassComplexity);
            Mix(lockPadRichness ? 1 : 0);
            Mix(lockedPadRichness);
            // Ducking
            Mix(enableDucking ? 1 : 0);
            Mix(Q(duckPadAmount));
            Mix(Q(duckArpAmount));
            Mix(Q(duckAttackSeconds));
            Mix(Q(duckHoldSeconds));
            Mix(Q(duckReleaseSeconds));
            // Mixer
            Mix(mutePad ? 1 : 0); Mix(muteBass ? 1 : 0); Mix(muteDrums ? 1 : 0); Mix(muteArp ? 1 : 0);
            Mix(soloPad ? 1 : 0); Mix(soloBass ? 1 : 0); Mix(soloDrums ? 1 : 0); Mix(soloArp ? 1 : 0);
            Mix(Q(trimPad)); Mix(Q(trimBass)); Mix(Q(trimDrums)); Mix(Q(trimArp));
            // Cache limits
            float limMB; int limClips; ProceduralMusic.GetCacheLimits(out limMB, out limClips);
            Mix(Q(limMB));
            Mix(limClips);
            // Persistence toggles
            Mix(persistRuntimeSettings ? 1 : 0);
            Mix(autoSaveOnQuit ? 1 : 0);
            Mix(autoSaveOnChange ? 1 : 0);
            return h;
        }

        private void OnApplicationQuit()
        {
            if (persistRuntimeSettings && autoSaveOnQuit)
            {
                SaveRuntimeSettings();
            }
        }

        [System.Serializable]
        public class RuntimeSettingsData
        {
            public float intensity;
            public float bpm;
            public bool enableAutoProgression;
            public float autoProgressionInterval;
            public float autoProgressionJitter;
            public bool alignProgressionToChord;
            public int beatsPerChord;
            public bool alignStingersToBeat;
            public int stingerSubdivision;
            public bool alignFillsToBeat;
            public int fillSubdivision;
            public bool enablePadFilterDynamics;
            public bool linkBassToProgression;
            // Overrides
            public bool lockDrumComplexity; public int lockedDrumComplexity;
            public bool lockBassComplexity; public int lockedBassComplexity;
            public bool lockPadRichness; public int lockedPadRichness;
            // Ducking
            public bool enableDucking; public float duckPadAmount; public float duckArpAmount;
            public float duckAttackSeconds; public float duckHoldSeconds; public float duckReleaseSeconds;
            // Mixer
            public bool mutePad; public bool muteBass; public bool muteDrums; public bool muteArp;
            public bool soloPad; public bool soloBass; public bool soloDrums; public bool soloArp;
            public float trimPad; public float trimBass; public float trimDrums; public float trimArp;
            // Cache
            public float cacheMB; public int cacheClips;
            // Persistence prefs
            public bool persistRuntimeSettings; public bool autoSaveOnQuit; public bool autoSaveOnChange;
            // Last preset info (for HUD convenience)
            public string lastPresetSlot; public string lastPresetPath;
        }

        public RuntimeSettingsData CaptureRuntimeSettings()
        {
            var d = new RuntimeSettingsData();
            d.intensity = intensity; d.bpm = bpm; d.enableAutoProgression = enableAutoProgression;
            d.autoProgressionInterval = autoProgressionInterval; d.autoProgressionJitter = autoProgressionJitter;
            d.alignProgressionToChord = alignProgressionToChord; d.beatsPerChord = beatsPerChord;
            d.alignStingersToBeat = alignStingersToBeat; d.stingerSubdivision = stingerSubdivision;
            d.alignFillsToBeat = alignFillsToBeat; d.fillSubdivision = fillSubdivision;
            d.enablePadFilterDynamics = enablePadFilterDynamics; d.linkBassToProgression = linkBassToProgression;
            d.lockDrumComplexity = lockDrumComplexity; d.lockedDrumComplexity = lockedDrumComplexity;
            d.lockBassComplexity = lockBassComplexity; d.lockedBassComplexity = lockedBassComplexity;
            d.lockPadRichness = lockPadRichness; d.lockedPadRichness = lockedPadRichness;
            d.enableDucking = enableDucking; d.duckPadAmount = duckPadAmount; d.duckArpAmount = duckArpAmount;
            d.duckAttackSeconds = duckAttackSeconds; d.duckHoldSeconds = duckHoldSeconds; d.duckReleaseSeconds = duckReleaseSeconds;
            d.mutePad = mutePad; d.muteBass = muteBass; d.muteDrums = muteDrums; d.muteArp = muteArp;
            d.soloPad = soloPad; d.soloBass = soloBass; d.soloDrums = soloDrums; d.soloArp = soloArp;
            d.trimPad = trimPad; d.trimBass = trimBass; d.trimDrums = trimDrums; d.trimArp = trimArp;
            float limMB; int limClips; ProceduralMusic.GetCacheLimits(out limMB, out limClips);
            d.cacheMB = limMB; d.cacheClips = limClips;
            d.persistRuntimeSettings = persistRuntimeSettings; d.autoSaveOnQuit = autoSaveOnQuit; d.autoSaveOnChange = autoSaveOnChange;
            d.lastPresetSlot = lastPresetSlot; d.lastPresetPath = lastPresetPath;
            return d;
        }

        public void ApplyRuntimeSettings(RuntimeSettingsData d, bool regenerate)
        {
            if (d == null) { return; }
            intensity = Mathf.Clamp01(d.intensity);
            bpm = d.bpm;
            enableAutoProgression = d.enableAutoProgression;
            autoProgressionInterval = d.autoProgressionInterval;
            autoProgressionJitter = d.autoProgressionJitter;
            alignProgressionToChord = d.alignProgressionToChord;
            beatsPerChord = d.beatsPerChord;
            alignStingersToBeat = d.alignStingersToBeat;
            stingerSubdivision = Mathf.Clamp(d.stingerSubdivision, 1, 4);
            alignFillsToBeat = d.alignFillsToBeat;
            fillSubdivision = Mathf.Clamp(d.fillSubdivision, 1, 4);
            enablePadFilterDynamics = d.enablePadFilterDynamics;
            linkBassToProgression = d.linkBassToProgression;
            lockDrumComplexity = d.lockDrumComplexity; lockedDrumComplexity = Mathf.Clamp(d.lockedDrumComplexity, 0, 2);
            lockBassComplexity = d.lockBassComplexity; lockedBassComplexity = Mathf.Clamp(d.lockedBassComplexity, 0, 2);
            lockPadRichness = d.lockPadRichness; lockedPadRichness = Mathf.Clamp(d.lockedPadRichness, 0, 2);
            enableDucking = d.enableDucking; duckPadAmount = Mathf.Clamp01(d.duckPadAmount); duckArpAmount = Mathf.Clamp01(d.duckArpAmount);
            duckAttackSeconds = Mathf.Max(0.005f, d.duckAttackSeconds); duckHoldSeconds = Mathf.Max(0f, d.duckHoldSeconds); duckReleaseSeconds = Mathf.Max(0.05f, d.duckReleaseSeconds);
            mutePad = d.mutePad; muteBass = d.muteBass; muteDrums = d.muteDrums; muteArp = d.muteArp;
            soloPad = d.soloPad; soloBass = d.soloBass; soloDrums = d.soloDrums; soloArp = d.soloArp;
            trimPad = Mathf.Clamp(d.trimPad, 0f, 2f); trimBass = Mathf.Clamp(d.trimBass, 0f, 2f); trimDrums = Mathf.Clamp(d.trimDrums, 0f, 2f); trimArp = Mathf.Clamp(d.trimArp, 0f, 2f);
            ProceduralMusic.SetCacheLimits(Mathf.Max(1f, d.cacheMB), d.cacheClips > 0 ? d.cacheClips : -1);
            persistRuntimeSettings = d.persistRuntimeSettings; autoSaveOnQuit = d.autoSaveOnQuit; autoSaveOnChange = d.autoSaveOnChange;
            lastPresetSlot = d.lastPresetSlot ?? string.Empty; lastPresetPath = d.lastPresetPath ?? string.Empty;
            if (regenerate)
            {
                GenerateAll();
                PlayAll();
            }
            ReapplyAdaptiveState();
            RescheduleAutoProgression();
        }

        public string ExportRuntimeSettingsJson(bool prettyPrint = true)
        {
            var data = CaptureRuntimeSettings();
            return JsonUtility.ToJson(data, prettyPrint);
        }

        public bool ImportRuntimeSettingsJson(string json, bool regenerate)
        {
            if (string.IsNullOrEmpty(json)) { Debug.LogWarning("ImportRuntimeSettingsJson: empty JSON"); return false; }
            try
            {
                var data = JsonUtility.FromJson<RuntimeSettingsData>(json);
                if (data == null) { Debug.LogWarning("ImportRuntimeSettingsJson: parse failed"); return false; }
                ApplyRuntimeSettings(data, regenerate);
                return true;
            }
            catch (System.Exception ex)
            {
                Debug.LogWarning("ImportRuntimeSettingsJson exception: " + ex.Message);
                return false;
            }
        }

        // Preset slots to disk (A/B/C) using JSON under persistentDataPath/music_presets
        private static string GetPresetDir()
        {
            string dir = System.IO.Path.Combine(Application.persistentDataPath, "music_presets");
            return dir;
        }

        private static string GetPresetPath(char slot)
        {
            slot = char.ToUpperInvariant(slot);
            if (slot != 'A' && slot != 'B' && slot != 'C') slot = 'A';
            return System.IO.Path.Combine(GetPresetDir(), $"preset_{slot}.json");
        }

        public bool SavePresetSlot(char slot)
        {
            try
            {
                string dir = GetPresetDir();
                if (!System.IO.Directory.Exists(dir)) System.IO.Directory.CreateDirectory(dir);
                string path = GetPresetPath(slot);
                string json = ExportRuntimeSettingsJson(true);
                System.IO.File.WriteAllText(path, json);
                Debug.Log($"Saved music preset {char.ToUpperInvariant(slot)} to {path}");
                lastPresetSlot = char.ToUpperInvariant(slot).ToString();
                lastPresetPath = path;
                UnityEngine.PlayerPrefs.SetString(PrefPrefix + "LastPresetSlot", lastPresetSlot);
                UnityEngine.PlayerPrefs.SetString(PrefPrefix + "LastPresetPath", lastPresetPath);
                UnityEngine.PlayerPrefs.Save();
                return true;
            }
            catch (System.Exception ex)
            {
                Debug.LogWarning("SavePresetSlot failed: " + ex.Message);
                return false;
            }
        }

        public bool LoadPresetSlot(char slot, bool regenerate)
        {
            try
            {
                string path = GetPresetPath(slot);
                if (!System.IO.File.Exists(path))
                {
                    Debug.LogWarning($"Preset {char.ToUpperInvariant(slot)} not found: {path}");
                    return false;
                }
                string json = System.IO.File.ReadAllText(path);
                bool ok = ImportRuntimeSettingsJson(json, regenerate);
                if (ok)
                {
                    lastPresetSlot = char.ToUpperInvariant(slot).ToString();
                    lastPresetPath = path;
                    UnityEngine.PlayerPrefs.SetString(PrefPrefix + "LastPresetSlot", lastPresetSlot);
                    UnityEngine.PlayerPrefs.SetString(PrefPrefix + "LastPresetPath", lastPresetPath);
                    UnityEngine.PlayerPrefs.Save();
                }
                return ok;
            }
            catch (System.Exception ex)
            {
                Debug.LogWarning("LoadPresetSlot failed: " + ex.Message);
                return false;
            }
        }

        private void ApplyIntensityImmediate()
        {
            float i = intensity;
            float padVol = Mathf.Lerp(0.25f, 0.4f, i * 0.5f);
            float bassVol = Mathf.Lerp(0f, 0.45f, Mathf.SmoothStep(0f,1f,i));
            float drumVol = Mathf.Lerp(0f, 0.6f, Mathf.Pow(i,0.8f));
            float arpVol = Mathf.Lerp(0f, 0.35f, Mathf.Clamp01((i - 0.3f)/0.7f));
            // trims
            padVol *= trimPad;
            bassVol *= trimBass;
            drumVol *= trimDrums;
            arpVol *= trimArp;
            // ducking on pad/arp
            if (enableDucking && (_duckLevel > 0f))
            {
                padVol *= (1f - duckPadAmount * _duckLevel);
                arpVol *= (1f - duckArpAmount * _duckLevel);
            }
            // mutes/solos
            bool anySolo = soloPad || soloBass || soloDrums || soloArp;
            if (anySolo)
            {
                if (!soloPad) { padVol = 0f; }
                if (!soloBass) { bassVol = 0f; }
                if (!soloDrums) { drumVol = 0f; }
                if (!soloArp) { arpVol = 0f; }
            }
            else
            {
                if (mutePad) { padVol = 0f; }
                if (muteBass) { bassVol = 0f; }
                if (muteDrums) { drumVol = 0f; }
                if (muteArp) { arpVol = 0f; }
            }
            padSource.volume = padVol;
            bassSource.volume = bassVol;
            drumSource.volume = drumVol;
            arpSource.volume = arpVol;
            if (enableDucking && (_duckLevel > 0f))
            {
                padSource.volume *= (1f - duckPadAmount * _duckLevel);
                arpSource.volume *= (1f - duckArpAmount * _duckLevel);
            }
            if (enablePadFilterDynamics)
            {
                EnsurePadFilter();
                float cutoff = Mathf.Lerp(padMinCutoff, padMaxCutoff, Mathf.Pow(i, 0.8f));
                _padLpf.cutoffFrequency = cutoff;
            }
        }

        private void ApplyIntensitySmooth()
        {
            // Legacy per-frame hook retained; now defers to immediate calculation (coroutine handles gradual changes)
            ApplyIntensityImmediate();
        }

        private System.Collections.IEnumerator SmoothIntensityRoutine(float target, float seconds)
        {
            float start = intensity;
            float t = 0f;
            while (t < seconds)
            {
                t += Time.deltaTime;
                float u = Mathf.Clamp01(t / seconds);
                // ease (smoothstep) for a softer curve
                float eased = u * u * (3f - 2f * u);
                intensity = Mathf.Lerp(start, target, eased);
                ApplyIntensityImmediate();
                yield return null;
            }
            intensity = target;
            ApplyIntensityImmediate();
            _intensityRoutine = null;
        }

        private void EnsureSources()
        {
            if (padSource == null)
            {
                padSource = CreateChildSource("Pad");
            }
            if (bassSource == null)
            {
                bassSource = CreateChildSource("Bass");
            }
            if (drumSource == null)
            {
                drumSource = CreateChildSource("Drums");
            }
            if (arpSource == null)
            {
                arpSource = CreateChildSource("Arp");
            }
            if (stingerSource == null)
            {
                stingerSource = CreateChildSource("Stinger");
                stingerSource.loop = false; // one-shot
            }
            if (percFillSource == null)
            {
                percFillSource = CreateChildSource("PercFill");
                percFillSource.loop = false;
            }
            if (enablePadFilterDynamics)
            {
                EnsurePadFilter();
            }
        }

        private AudioSource CreateChildSource(string name)
        {
            var go = new GameObject("Music_" + name);
            go.transform.SetParent(transform);
            var src = go.AddComponent<AudioSource>();
            src.loop = true;
            src.playOnAwake = false;
            src.spatialBlend = 0f;
            src.rolloffMode = AudioRolloffMode.Linear;
            return src;
        }

        private void AssignClip(AudioSource src, AudioClip clip, bool resetTime)
        {
            if (src == null || clip == null)
            {
                return;
            }
            src.clip = clip;
            if (resetTime)
            {
                src.time = 0f;
            }
        }

        private void CrossfadeSource(ref AudioSource currentSrc, ref AudioClip currentClip, AudioClip newClip, float seconds)
        {
            if (currentSrc == null || newClip == null)
            {
                return;
            }
            seconds = Mathf.Max(0.1f, seconds);
            float oldVol = currentSrc.volume;
            // Create temporary new source
            var newGo = new GameObject(currentSrc.gameObject.name + "_XFade");
            newGo.transform.SetParent(transform);
            var newSrc = newGo.AddComponent<AudioSource>();
            newSrc.loop = true;
            newSrc.playOnAwake = false;
            newSrc.spatialBlend = 0f;
            newSrc.rolloffMode = AudioRolloffMode.Linear;
            newSrc.clip = newClip;
            newSrc.volume = 0f;
            newSrc.Play();
            StartCoroutine(CrossfadeRoutine(currentSrc, newSrc, oldVol, seconds, () =>
            {
                // After fade complete: replace current source
                currentSrc.Stop();
                Destroy(currentSrc.gameObject);
                currentSrc = newSrc;
                currentClip = newClip;
                ApplyIntensityImmediate(); // re-apply intensity mapping to the new source volumes
            }));
        }

        private System.Collections.IEnumerator CrossfadeRoutine(AudioSource oldSrc, AudioSource newSrc, float targetVolume, float seconds, System.Action onDone)
        {
            float t = 0f;
            while (t < seconds)
            {
                t += Time.deltaTime;
                float u = Mathf.Clamp01(t / seconds);
                float eased = u * u * (3f - 2f * u); // smoothstep easing
                newSrc.volume = targetVolume * eased;
                oldSrc.volume = targetVolume * (1f - eased);
                yield return null;
            }
            newSrc.volume = targetVolume;
            oldSrc.volume = 0f;
            onDone?.Invoke();
        }

        /// <summary>
        /// Plays a reactive musical stinger (type: "rise" or "hit"). Fetches from procedural cache.
        /// Volume scaled by current intensity for contextual feel.
        /// </summary>
        public void PlayStinger(string type = "rise")
        {
            EnsureSources();
            if (alignStingersToBeat && bpm > 0f)
            {
                float spb = 60f / Mathf.Max(1f, bpm);
                int subdiv = Mathf.Clamp(stingerSubdivision, 1, 4);
                float grid = spb / subdiv;
                float t = 0f;
                if (padSource != null && padSource.clip != null)
                {
                    t = padSource.time % grid;
                }
                float delay = grid - t;
                if (delay < 0.02f) { delay = 0.02f; }
                StartCoroutine(PlayStingerRoutine(type, delay));
            }
            else
            {
                StartCoroutine(PlayStingerRoutine(type, 0f));
            }
        }

        private System.Collections.IEnumerator PlayStingerRoutine(string type, float delay)
        {
            float t = 0f;
            while (t < delay)
            {
                t += Time.deltaTime;
                yield return null;
            }
            stingerClip = ProceduralMusic.GetStingerClip(seed + 91, type, 2f);
            if (stingerClip == null)
            {
                yield break;
            }
            stingerSource.clip = stingerClip;
            stingerSource.time = 0f;
            stingerSource.volume = Mathf.Lerp(0.3f, 0.9f, intensity);
            stingerSource.loop = false;
            stingerSource.Play();
            if (enableDucking)
            {
                StartDuck();
            }
        }

        /// <summary>Plays a short one-shot percussive fill overlay to accent transitions.</summary>
        public void PlayDrumFill()
        {
            EnsureSources();
            if (alignFillsToBeat && bpm > 0f)
            {
                float spb = 60f / Mathf.Max(1f, bpm);
                int subdiv = Mathf.Clamp(fillSubdivision, 1, 4);
                float grid = spb / subdiv;
                float t = 0f;
                if (padSource != null && padSource.clip != null)
                {
                    t = padSource.time % grid;
                }
                float delay = grid - t;
                if (delay < 0.02f) { delay = 0.02f; }
                StartCoroutine(PlayDrumFillRoutine(delay));
            }
            else
            {
                StartCoroutine(PlayDrumFillRoutine(0f));
            }
        }

        private System.Collections.IEnumerator PlayDrumFillRoutine(float delay)
        {
            float t = 0f;
            while (t < delay)
            {
                t += Time.deltaTime;
                yield return null;
            }
            var beatLen = 60f / Mathf.Max(1f, bpm);
            var fill = ProceduralMusic.GetDrumFillClip(seed + 131, bpm, beatLen);
            if (fill == null)
            {
                yield break;
            }
            percFillSource.clip = fill;
            percFillSource.time = 0f;
            percFillSource.volume = Mathf.Lerp(0.25f, 0.8f, intensity);
            percFillSource.loop = false;
            percFillSource.Play();
            if (enableDucking)
            {
                StartDuck();
            }
        }

        private void StartDuck()
        {
            if (_duckRoutine != null)
            {
                StopCoroutine(_duckRoutine);
            }
            _duckRoutine = StartCoroutine(DuckRoutine());
        }

        private System.Collections.IEnumerator DuckRoutine()
        {
            float attack = Mathf.Max(0.005f, duckAttackSeconds);
            float hold = Mathf.Max(0f, duckHoldSeconds);
            float release = Mathf.Max(0.05f, duckReleaseSeconds);
            float t = 0f;
            // attack
            while (t < attack)
            {
                t += Time.deltaTime;
                _duckLevel = Mathf.Clamp01(t / attack);
                ApplyIntensityImmediate();
                yield return null;
            }
            _duckLevel = 1f;
            ApplyIntensityImmediate();
            // hold
            float h = 0f;
            while (h < hold)
            {
                h += Time.deltaTime;
                yield return null;
            }
            // release
            float r = 0f;
            while (r < release)
            {
                r += Time.deltaTime;
                float u = Mathf.Clamp01(r / release);
                // ease out
                _duckLevel = 1f - (u * u * (3f - 2f * u));
                ApplyIntensityImmediate();
                yield return null;
            }
            _duckLevel = 0f;
            ApplyIntensityImmediate();
            _duckRoutine = null;
        }

    /// <summary>Evaluates desired drum complexity from current intensity and crossfades if changed.</summary>
        private void EvaluateDrumComplexity()
        {
            int desired = 0;
            if (lockDrumComplexity)
            {
                desired = Mathf.Clamp(lockedDrumComplexity, 0, 2);
            }
            else
            {
                // adaptation by intensity
                if (intensity >= drumHighThreshold) { desired = 2; }
                else if (intensity >= drumMediumThreshold) { desired = 1; }
            }
            if (desired == _drumComplexity) { return; }
            var newDrums = ProceduralMusic.GetDrumLoopVariant(seed + 29, bpm, loopLengthSeconds, desired);
            CrossfadeSource(ref drumSource, ref drumClip, newDrums, drumComplexityCrossfadeSeconds);
            _drumComplexity = desired;
        }

        /// <summary>Evaluates desired bass complexity from current intensity and crossfades if changed.</summary>
        private void EvaluateBassComplexity()
        {
            int desired = 0;
            if (lockBassComplexity)
            {
                desired = Mathf.Clamp(lockedBassComplexity, 0, 2);
            }
            else
            {
                if (intensity >= bassHighThreshold) { desired = 2; }
                else if (intensity >= bassMediumThreshold) { desired = 1; }
            }
            if (desired == _bassComplexity) { return; }
            _currentBassRootHz = linkBassToProgression ? ComputeBassRootForProgression(_progressionIndex) : bassBaseRootHz;
            var newBass = ProceduralMusic.GetBassLoopVariant(seed + 11, bpm, loopLengthSeconds, _currentBassRootHz, desired);
            CrossfadeSource(ref bassSource, ref bassClip, newBass, bassComplexityCrossfadeSeconds);
            _bassComplexity = desired;
        }

        /// <summary>Evaluates desired pad richness (0 base, 1 rich, 2 very rich) from current intensity and crossfades if changed.</summary>
        private void EvaluatePadRichness()
        {
            int desired = 0;
            if (lockPadRichness)
            {
                desired = Mathf.Clamp(lockedPadRichness, 0, 2);
            }
            else
            {
                if (intensity >= padRichnessHighThreshold) { desired = 2; }
                else if (intensity >= padRichnessMediumThreshold) { desired = 1; }
            }
            if (desired == _padRichness) { return; }
            var newPad = ProceduralMusic.GetPadLoopVariantRich(seed, bpm, loopLengthSeconds, _progressionIndex, desired);
            CrossfadeSource(ref padSource, ref padClip, newPad, padRichnessCrossfadeSeconds);
            _padRichness = desired;
        }

        /// <summary>Rebuilds bass clip according to current progression index (and complexity) and crossfades.</summary>
        private void RebuildBassClipForProgression()
        {
            _currentBassRootHz = ComputeBassRootForProgression(_progressionIndex);
            int complexity = adaptiveBassComplexity ? _bassComplexity : 0;
            AudioClip newBass = adaptiveBassComplexity
                ? ProceduralMusic.GetBassLoopVariant(seed + 11, bpm, loopLengthSeconds, _currentBassRootHz, complexity)
                : ProceduralMusic.GetBassLoop(seed + 11, bpm, loopLengthSeconds, _currentBassRootHz);
            float seconds = bassProgressionCrossfadeSeconds > 0f ? bassProgressionCrossfadeSeconds : progressionCrossfadeSeconds;
            CrossfadeSource(ref bassSource, ref bassClip, newBass, seconds);
        }

        /// <summary>Computes transposed bass root based on progression index (2 semitones per step, clamped [-6,+6]).</summary>
        private float ComputeBassRootForProgression(int progIndex)
        {
            int rawShift = progIndex * 2;
            int shift = Mathf.Clamp(rawShift, -6, 6);
            return bassBaseRootHz * Mathf.Pow(2f, shift / 12f);
        }

        private void ScheduleNextProgression()
        {
            if (!enableAutoProgression)
            {
                return;
            }
            float jitter = 0f;
            if (autoProgressionJitter > 0f)
            {
                jitter = Random.Range(-autoProgressionJitter, autoProgressionJitter);
            }
            _nextAutoProgressionTime = Time.time + Mathf.Max(5f, autoProgressionInterval + jitter);
        }

        /// <summary>Public helper to (re)schedule the next automatic progression based on current settings.</summary>
        public void RescheduleAutoProgression()
        {
            ScheduleNextProgression();
        }

        private void EnsurePadFilter()
        {
            if (_padLpf == null && padSource != null)
            {
                _padLpf = padSource.GetComponent<AudioLowPassFilter>();
                if (_padLpf == null)
                {
                    _padLpf = padSource.gameObject.AddComponent<AudioLowPassFilter>();
                    _padLpf.cutoffFrequency = padMaxCutoff;
                }
            }
        }

        /// <summary>Queues a progression advance on the next chord boundary (based on BPM and beatsPerChord).</summary>
        private void QueueAlignedProgression(int steps)
        {
            if (padSource == null || padSource.clip == null)
            {
                AdvanceProgression(steps);
                return;
            }
            if (_pendingProgressionRoutine != null)
            {
                StopCoroutine(_pendingProgressionRoutine);
                _pendingProgressionRoutine = null;
            }
            float spb = 60f / Mathf.Max(1f, bpm);
            int bpc = Mathf.Max(1, beatsPerChord);
            float chordSec = spb * bpc;
            float t = padSource.time % chordSec;
            float delay = chordSec - t;
            if (delay < 0.02f) { delay = 0.02f; }
            _pendingProgressionRoutine = StartCoroutine(DoAdvanceAfterDelay(steps, delay));
        }

        /// <summary>
        /// Advances progression immediately or queues it to the next chord boundary if alignment is enabled.
        /// </summary>
        public void AdvanceProgressionSmart(int steps = 1)
        {
            if (alignProgressionToChord && padSource != null && padSource.clip != null)
            {
                QueueAlignedProgression(steps);
            }
            else
            {
                AdvanceProgression(steps);
            }
        }

        /// <summary>Re-applies adaptive decisions immediately (useful after changing overrides in HUD).</summary>
        public void ReapplyAdaptiveState()
        {
            if (adaptiveDrumComplexity || lockDrumComplexity) { EvaluateDrumComplexity(); }
            if (adaptiveBassComplexity || lockBassComplexity) { EvaluateBassComplexity(); }
            if (adaptivePadRichness || lockPadRichness) { EvaluatePadRichness(); }
            ApplyIntensityImmediate();
        }

        /// <summary>
        /// Resets common runtime toggles to recommended defaults and optionally regenerates all clips.
        /// Does not change serialized thresholds or seed.
        /// </summary>
        public void ResetToDefaults(bool regenerate = true)
        {
            // Alignment
            alignProgressionToChord = true;
            beatsPerChord = 4;
            alignStingersToBeat = false;
            stingerSubdivision = 1;
            alignFillsToBeat = true;
            fillSubdivision = 1;
            // Auto progression
            enableAutoProgression = true;
            // Pad dynamics
            enablePadFilterDynamics = true;
            // Overrides
            lockDrumComplexity = false; lockedDrumComplexity = 0;
            lockBassComplexity = false; lockedBassComplexity = 0;
            lockPadRichness = false; lockedPadRichness = 0;
            // Ducking defaults
            enableDucking = true;
            duckPadAmount = 0.5f; duckArpAmount = 0.6f;
            duckAttackSeconds = 0.02f; duckHoldSeconds = 0.08f; duckReleaseSeconds = 0.35f;
            // Mixer defaults
            mutePad = muteBass = muteDrums = muteArp = false;
            trimPad = trimBass = trimDrums = trimArp = 1f;
            soloPad = soloBass = soloDrums = soloArp = false;

            if (regenerate)
            {
                GenerateAll();
                PlayAll();
                ReapplyAdaptiveState();
                RescheduleAutoProgression();
            }
            else
            {
                ReapplyAdaptiveState();
                RescheduleAutoProgression();
            }
        }

        private const string PrefPrefix = "HWL_Music_";
        /// <summary>Saves relevant runtime settings to PlayerPrefs.</summary>
        public void SaveRuntimeSettings()
        {
            UnityEngine.PlayerPrefs.SetFloat(PrefPrefix + nameof(intensity), intensity);
            UnityEngine.PlayerPrefs.SetFloat(PrefPrefix + nameof(bpm), bpm);
            UnityEngine.PlayerPrefs.SetInt(PrefPrefix + nameof(enableAutoProgression), enableAutoProgression ? 1 : 0);
            UnityEngine.PlayerPrefs.SetFloat(PrefPrefix + nameof(autoProgressionInterval), autoProgressionInterval);
            UnityEngine.PlayerPrefs.SetFloat(PrefPrefix + nameof(autoProgressionJitter), autoProgressionJitter);
            UnityEngine.PlayerPrefs.SetInt(PrefPrefix + nameof(alignProgressionToChord), alignProgressionToChord ? 1 : 0);
            UnityEngine.PlayerPrefs.SetInt(PrefPrefix + nameof(beatsPerChord), beatsPerChord);
            UnityEngine.PlayerPrefs.SetInt(PrefPrefix + nameof(alignStingersToBeat), alignStingersToBeat ? 1 : 0);
            UnityEngine.PlayerPrefs.SetInt(PrefPrefix + nameof(stingerSubdivision), stingerSubdivision);
            UnityEngine.PlayerPrefs.SetInt(PrefPrefix + nameof(alignFillsToBeat), alignFillsToBeat ? 1 : 0);
            UnityEngine.PlayerPrefs.SetInt(PrefPrefix + nameof(fillSubdivision), fillSubdivision);
            UnityEngine.PlayerPrefs.SetInt(PrefPrefix + nameof(enablePadFilterDynamics), enablePadFilterDynamics ? 1 : 0);
            UnityEngine.PlayerPrefs.SetInt(PrefPrefix + nameof(linkBassToProgression), linkBassToProgression ? 1 : 0);
            // Overrides
            UnityEngine.PlayerPrefs.SetInt(PrefPrefix + nameof(lockDrumComplexity), lockDrumComplexity ? 1 : 0);
            UnityEngine.PlayerPrefs.SetInt(PrefPrefix + nameof(lockedDrumComplexity), lockedDrumComplexity);
            UnityEngine.PlayerPrefs.SetInt(PrefPrefix + nameof(lockBassComplexity), lockBassComplexity ? 1 : 0);
            UnityEngine.PlayerPrefs.SetInt(PrefPrefix + nameof(lockedBassComplexity), lockedBassComplexity);
            UnityEngine.PlayerPrefs.SetInt(PrefPrefix + nameof(lockPadRichness), lockPadRichness ? 1 : 0);
            UnityEngine.PlayerPrefs.SetInt(PrefPrefix + nameof(lockedPadRichness), lockedPadRichness);
            // Ducking
            UnityEngine.PlayerPrefs.SetInt(PrefPrefix + nameof(enableDucking), enableDucking ? 1 : 0);
            UnityEngine.PlayerPrefs.SetFloat(PrefPrefix + nameof(duckPadAmount), duckPadAmount);
            UnityEngine.PlayerPrefs.SetFloat(PrefPrefix + nameof(duckArpAmount), duckArpAmount);
            UnityEngine.PlayerPrefs.SetFloat(PrefPrefix + nameof(duckAttackSeconds), duckAttackSeconds);
            UnityEngine.PlayerPrefs.SetFloat(PrefPrefix + nameof(duckHoldSeconds), duckHoldSeconds);
            UnityEngine.PlayerPrefs.SetFloat(PrefPrefix + nameof(duckReleaseSeconds), duckReleaseSeconds);
            // Mixer
            UnityEngine.PlayerPrefs.SetInt(PrefPrefix + nameof(mutePad), mutePad ? 1 : 0);
            UnityEngine.PlayerPrefs.SetInt(PrefPrefix + nameof(muteBass), muteBass ? 1 : 0);
            UnityEngine.PlayerPrefs.SetInt(PrefPrefix + nameof(muteDrums), muteDrums ? 1 : 0);
            UnityEngine.PlayerPrefs.SetInt(PrefPrefix + nameof(muteArp), muteArp ? 1 : 0);
            UnityEngine.PlayerPrefs.SetInt(PrefPrefix + nameof(soloPad), soloPad ? 1 : 0);
            UnityEngine.PlayerPrefs.SetInt(PrefPrefix + nameof(soloBass), soloBass ? 1 : 0);
            UnityEngine.PlayerPrefs.SetInt(PrefPrefix + nameof(soloDrums), soloDrums ? 1 : 0);
            UnityEngine.PlayerPrefs.SetInt(PrefPrefix + nameof(soloArp), soloArp ? 1 : 0);
            UnityEngine.PlayerPrefs.SetFloat(PrefPrefix + nameof(trimPad), trimPad);
            UnityEngine.PlayerPrefs.SetFloat(PrefPrefix + nameof(trimBass), trimBass);
            UnityEngine.PlayerPrefs.SetFloat(PrefPrefix + nameof(trimDrums), trimDrums);
            UnityEngine.PlayerPrefs.SetFloat(PrefPrefix + nameof(trimArp), trimArp);
            // Cache limits
            float limMB; int limClips; ProceduralMusic.GetCacheLimits(out limMB, out limClips);
            UnityEngine.PlayerPrefs.SetFloat(PrefPrefix + "CacheMB", limMB);
            UnityEngine.PlayerPrefs.SetInt(PrefPrefix + "CacheClips", limClips);
            // Last preset info
            UnityEngine.PlayerPrefs.SetString(PrefPrefix + "LastPresetSlot", lastPresetSlot ?? string.Empty);
            UnityEngine.PlayerPrefs.SetString(PrefPrefix + "LastPresetPath", lastPresetPath ?? string.Empty);
            UnityEngine.PlayerPrefs.Save();
        }

        /// <summary>Loads runtime settings from PlayerPrefs; optionally regenerates content.</summary>
        public void LoadRuntimeSettings(bool regenerate)
        {
            // Use current values as defaults
            intensity = UnityEngine.PlayerPrefs.GetFloat(PrefPrefix + nameof(intensity), intensity);
            bpm = UnityEngine.PlayerPrefs.GetFloat(PrefPrefix + nameof(bpm), bpm);
            enableAutoProgression = UnityEngine.PlayerPrefs.GetInt(PrefPrefix + nameof(enableAutoProgression), enableAutoProgression ? 1 : 0) != 0;
            autoProgressionInterval = UnityEngine.PlayerPrefs.GetFloat(PrefPrefix + nameof(autoProgressionInterval), autoProgressionInterval);
            autoProgressionJitter = UnityEngine.PlayerPrefs.GetFloat(PrefPrefix + nameof(autoProgressionJitter), autoProgressionJitter);
            alignProgressionToChord = UnityEngine.PlayerPrefs.GetInt(PrefPrefix + nameof(alignProgressionToChord), alignProgressionToChord ? 1 : 0) != 0;
            beatsPerChord = UnityEngine.PlayerPrefs.GetInt(PrefPrefix + nameof(beatsPerChord), beatsPerChord);
            alignStingersToBeat = UnityEngine.PlayerPrefs.GetInt(PrefPrefix + nameof(alignStingersToBeat), alignStingersToBeat ? 1 : 0) != 0;
            stingerSubdivision = UnityEngine.PlayerPrefs.GetInt(PrefPrefix + nameof(stingerSubdivision), stingerSubdivision);
            alignFillsToBeat = UnityEngine.PlayerPrefs.GetInt(PrefPrefix + nameof(alignFillsToBeat), alignFillsToBeat ? 1 : 0) != 0;
            fillSubdivision = UnityEngine.PlayerPrefs.GetInt(PrefPrefix + nameof(fillSubdivision), fillSubdivision);
            enablePadFilterDynamics = UnityEngine.PlayerPrefs.GetInt(PrefPrefix + nameof(enablePadFilterDynamics), enablePadFilterDynamics ? 1 : 0) != 0;
            linkBassToProgression = UnityEngine.PlayerPrefs.GetInt(PrefPrefix + nameof(linkBassToProgression), linkBassToProgression ? 1 : 0) != 0;
            // Overrides
            lockDrumComplexity = UnityEngine.PlayerPrefs.GetInt(PrefPrefix + nameof(lockDrumComplexity), lockDrumComplexity ? 1 : 0) != 0;
            lockedDrumComplexity = UnityEngine.PlayerPrefs.GetInt(PrefPrefix + nameof(lockedDrumComplexity), lockedDrumComplexity);
            lockBassComplexity = UnityEngine.PlayerPrefs.GetInt(PrefPrefix + nameof(lockBassComplexity), lockBassComplexity ? 1 : 0) != 0;
            lockedBassComplexity = UnityEngine.PlayerPrefs.GetInt(PrefPrefix + nameof(lockedBassComplexity), lockedBassComplexity);
            lockPadRichness = UnityEngine.PlayerPrefs.GetInt(PrefPrefix + nameof(lockPadRichness), lockPadRichness ? 1 : 0) != 0;
            lockedPadRichness = UnityEngine.PlayerPrefs.GetInt(PrefPrefix + nameof(lockedPadRichness), lockedPadRichness);
            // Ducking
            enableDucking = UnityEngine.PlayerPrefs.GetInt(PrefPrefix + nameof(enableDucking), enableDucking ? 1 : 0) != 0;
            duckPadAmount = UnityEngine.PlayerPrefs.GetFloat(PrefPrefix + nameof(duckPadAmount), duckPadAmount);
            duckArpAmount = UnityEngine.PlayerPrefs.GetFloat(PrefPrefix + nameof(duckArpAmount), duckArpAmount);
            duckAttackSeconds = UnityEngine.PlayerPrefs.GetFloat(PrefPrefix + nameof(duckAttackSeconds), duckAttackSeconds);
            duckHoldSeconds = UnityEngine.PlayerPrefs.GetFloat(PrefPrefix + nameof(duckHoldSeconds), duckHoldSeconds);
            duckReleaseSeconds = UnityEngine.PlayerPrefs.GetFloat(PrefPrefix + nameof(duckReleaseSeconds), duckReleaseSeconds);
            // Mixer
            mutePad = UnityEngine.PlayerPrefs.GetInt(PrefPrefix + nameof(mutePad), mutePad ? 1 : 0) != 0;
            muteBass = UnityEngine.PlayerPrefs.GetInt(PrefPrefix + nameof(muteBass), muteBass ? 1 : 0) != 0;
            muteDrums = UnityEngine.PlayerPrefs.GetInt(PrefPrefix + nameof(muteDrums), muteDrums ? 1 : 0) != 0;
            muteArp = UnityEngine.PlayerPrefs.GetInt(PrefPrefix + nameof(muteArp), muteArp ? 1 : 0) != 0;
            soloPad = UnityEngine.PlayerPrefs.GetInt(PrefPrefix + nameof(soloPad), soloPad ? 1 : 0) != 0;
            soloBass = UnityEngine.PlayerPrefs.GetInt(PrefPrefix + nameof(soloBass), soloBass ? 1 : 0) != 0;
            soloDrums = UnityEngine.PlayerPrefs.GetInt(PrefPrefix + nameof(soloDrums), soloDrums ? 1 : 0) != 0;
            soloArp = UnityEngine.PlayerPrefs.GetInt(PrefPrefix + nameof(soloArp), soloArp ? 1 : 0) != 0;
            trimPad = UnityEngine.PlayerPrefs.GetFloat(PrefPrefix + nameof(trimPad), trimPad);
            trimBass = UnityEngine.PlayerPrefs.GetFloat(PrefPrefix + nameof(trimBass), trimBass);
            trimDrums = UnityEngine.PlayerPrefs.GetFloat(PrefPrefix + nameof(trimDrums), trimDrums);
            trimArp = UnityEngine.PlayerPrefs.GetFloat(PrefPrefix + nameof(trimArp), trimArp);
            // Cache limits
            float curMB; int curClips; ProceduralMusic.GetCacheLimits(out curMB, out curClips);
            float loadMB = UnityEngine.PlayerPrefs.GetFloat(PrefPrefix + "CacheMB", curMB);
            int loadClips = UnityEngine.PlayerPrefs.GetInt(PrefPrefix + "CacheClips", curClips);
            ProceduralMusic.SetCacheLimits(loadMB, loadClips);
            // Last preset info
            lastPresetSlot = UnityEngine.PlayerPrefs.GetString(PrefPrefix + "LastPresetSlot", lastPresetSlot ?? string.Empty);
            lastPresetPath = UnityEngine.PlayerPrefs.GetString(PrefPrefix + "LastPresetPath", lastPresetPath ?? string.Empty);

            if (regenerate)
            {
                GenerateAll();
                PlayAll();
            }
            ReapplyAdaptiveState();
            RescheduleAutoProgression();
        }

        /// <summary>Clears saved PlayerPrefs entries for runtime settings.</summary>
        public void ClearSavedRuntimeSettings()
        {
            string[] keys = new string[]
            {
                nameof(intensity), nameof(bpm), nameof(enableAutoProgression), nameof(autoProgressionInterval), nameof(autoProgressionJitter),
                nameof(alignProgressionToChord), nameof(beatsPerChord), nameof(alignStingersToBeat), nameof(stingerSubdivision),
                nameof(alignFillsToBeat), nameof(fillSubdivision), nameof(enablePadFilterDynamics), nameof(linkBassToProgression),
                nameof(lockDrumComplexity), nameof(lockedDrumComplexity), nameof(lockBassComplexity), nameof(lockedBassComplexity),
                nameof(lockPadRichness), nameof(lockedPadRichness), nameof(enableDucking), nameof(duckPadAmount), nameof(duckArpAmount),
                nameof(duckAttackSeconds), nameof(duckHoldSeconds), nameof(duckReleaseSeconds), nameof(mutePad), nameof(muteBass),
                nameof(muteDrums), nameof(muteArp), nameof(soloPad), nameof(soloBass), nameof(soloDrums), nameof(soloArp),
                nameof(trimPad), nameof(trimBass), nameof(trimDrums), nameof(trimArp)
            };
            foreach (var k in keys)
            {
                UnityEngine.PlayerPrefs.DeleteKey(PrefPrefix + k);
            }
            UnityEngine.PlayerPrefs.DeleteKey(PrefPrefix + "CacheMB");
            UnityEngine.PlayerPrefs.DeleteKey(PrefPrefix + "CacheClips");
            UnityEngine.PlayerPrefs.DeleteKey(PrefPrefix + "LastPresetSlot");
            UnityEngine.PlayerPrefs.DeleteKey(PrefPrefix + "LastPresetPath");
            UnityEngine.PlayerPrefs.Save();
        }

        private System.Collections.IEnumerator DoAdvanceAfterDelay(int steps, float delay)
        {
            float t = 0f;
            while (t < delay)
            {
                t += Time.deltaTime;
                yield return null;
            }
            AdvanceProgression(steps);
            _pendingProgressionRoutine = null;
        }
    }
}
