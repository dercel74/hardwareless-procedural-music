using UnityEngine;

namespace Hardwareless.Audio
{
    [AddComponentMenu("Hardwareless/Audio/Procedural Music Debug HUD")]
    public class ProceduralMusicDebugHUD : MonoBehaviour
    {
        [Tooltip("Target music manager; if null, will auto-find in scene.")]
        public ProceduralMusicManager manager;

        [Tooltip("Toggle HUD visibility key.")]
        public KeyCode toggleKey = KeyCode.F9;

        [Tooltip("Start with HUD visible.")]
        public bool visible = true;

        [Tooltip("Show AutoProg countdown progress bar in HUD.")]
        public bool showAutoProgBar = true;

        [Tooltip("Drain and show last profiling entries (if profiling enabled).")]
        public bool showProfiling = false;

        private string[] _recentProfile = System.Array.Empty<string>();
        private float _lastProfilePollTime;
        private const float ProfilePollInterval = 2f;
        // Tap tempo state
        private readonly float[] _tapTimes = new float[8];
        private int _tapCount = 0;
        // Meter state
        private float _padLevel, _bassLevel, _drumLevel, _arpLevel;
        private float _lastMeterTime;
        private const float MeterInterval = 0.1f;
        private float[] _meterBuf = new float[256];
        // Debounced persistence
        private bool _settingsDirty = false;
        private float _lastSaveTime = 0f;
        private const float SaveDebounce = 0.5f;
        // Save toast indicator
        private float _saveToastUntil = 0f;
        private const float SaveToastDuration = 1.5f;
        // Auto progression ETA tracking for progress bar
        private float _etaMax = 0f;

        private void Awake()
        {
            if (manager == null)
            {
                manager = FindObjectOfType<ProceduralMusicManager>();
            }
        }

        private void Update()
        {
            if (Input.GetKeyDown(toggleKey))
            {
                visible = !visible;
            }
            if (showProfiling && ProceduralMusic.EnableProfiling && Time.unscaledTime - _lastProfilePollTime > ProfilePollInterval)
            {
                _recentProfile = ProceduralMusic.GetAndClearProfileLog();
                _lastProfilePollTime = Time.unscaledTime;
            }
            // Light metering at interval
            if (manager != null && Time.unscaledTime - _lastMeterTime > MeterInterval)
            {
                _padLevel = ComputeRms(manager.PadSource);
                _bassLevel = ComputeRms(manager.BassSource);
                _drumLevel = ComputeRms(manager.DrumSource);
                _arpLevel = ComputeRms(manager.ArpSource);
                _lastMeterTime = Time.unscaledTime;
            }
            // Debounced auto-save for small adjustments (sliders etc.)
            if (manager != null && manager.persistRuntimeSettings && _settingsDirty && (Time.unscaledTime - _lastSaveTime) > SaveDebounce)
            {
                manager.SaveRuntimeSettings();
                _settingsDirty = false;
                _lastSaveTime = Time.unscaledTime;
                _saveToastUntil = Time.unscaledTime + SaveToastDuration;
            }
        }

        private void OnGUI()
        {
            if (!visible) { return; }
            var prevColor = GUI.color;
            GUI.color = Color.black * 0.6f;
            GUILayout.BeginArea(new Rect(10, 10, 640, 540), GUI.skin.box);
            GUI.color = Color.white;
            string title = "Procedural Music Debug";
            if (manager != null)
            {
                if (!string.IsNullOrEmpty(manager.lastPresetSlot))
                {
                    title += "  [Preset: " + manager.lastPresetSlot + "]";
                }
                if (manager.enableAutoProgression)
                {
                    float eta = manager.NextAutoProgressionInSeconds;
                    title += "  [AutoProg: ON" + (eta > 0.5f ? $", Next: {eta:F0}s" : "") + "]";
                }
                else
                {
                    title += "  [AutoProg: OFF]";
                }
            }
            if (Time.unscaledTime < _saveToastUntil)
            {
                title += "  [Saved]";
            }
            GUILayout.Label(title);
            // AutoProg progress bar
            if (manager != null && manager.enableAutoProgression && showAutoProgBar)
            {
                float eta = manager.NextAutoProgressionInSeconds;
                if (eta > 0.5f)
                {
                    if (eta > _etaMax - 0.25f) { _etaMax = eta; } // new schedule or extended
                    float denom = Mathf.Max(0.0001f, _etaMax);
                    float frac = Mathf.Clamp01(1f - (eta / denom));
                    DrawSimpleProgress("Next Progression", frac, Color.green);
                }
                else
                {
                    _etaMax = 0f;
                }
            }
            if (manager == null)
            {
                GUILayout.Label("Manager: <none>");
            }
            else
            {
                GUILayout.Label($"Intensity: {manager.intensity:F2}  BPM: {manager.bpm:F0}  Loop: {manager.loopLengthSeconds:F1}s");
                GUILayout.Label($"Progression: {manager.CurrentProgressionIndex}  AutoProg: {manager.enableAutoProgression}");
                GUILayout.Label($"Drums: Lvl {manager.CurrentDrumComplexity}  Bass: Lvl {manager.CurrentBassComplexity}  PadRich: {manager.CurrentPadRichness}");
                GUILayout.Label($"Bass Root Hz: {manager.CurrentBassRootHz:F2}  Pad LPF: {(manager.CurrentPadCutoff > 0 ? manager.CurrentPadCutoff.ToString("F0") + " Hz" : "n/a")}");

                // Runtime toggles for timing alignment
                bool ap = GUILayout.Toggle(manager.alignProgressionToChord, "Align Progression To Chord");
                if (ap != manager.alignProgressionToChord) { manager.alignProgressionToChord = ap; _settingsDirty = true; }
                bool asb = GUILayout.Toggle(manager.alignStingersToBeat, "Align Stingers To Beat/Subdivision");
                if (asb != manager.alignStingersToBeat) { manager.alignStingersToBeat = asb; _settingsDirty = true; }
                bool afb = GUILayout.Toggle(manager.alignFillsToBeat, "Align Perc Fills To Beat/Subdivision");
                if (afb != manager.alignFillsToBeat) { manager.alignFillsToBeat = afb; _settingsDirty = true; }

                GUILayout.BeginHorizontal();
                GUILayout.Label("Stinger Subdiv:", GUILayout.Width(120));
                if (GUILayout.Button("1")) { manager.stingerSubdivision = 1; _settingsDirty = true; }
                if (GUILayout.Button("2")) { manager.stingerSubdivision = 2; _settingsDirty = true; }
                if (GUILayout.Button("4")) { manager.stingerSubdivision = 4; _settingsDirty = true; }
                GUILayout.Label($"Current: {manager.stingerSubdivision}x");
                GUILayout.EndHorizontal();

                GUILayout.BeginHorizontal();
                GUILayout.Label("Fill Subdiv:", GUILayout.Width(120));
                if (GUILayout.Button("1")) { manager.fillSubdivision = 1; _settingsDirty = true; }
                if (GUILayout.Button("2")) { manager.fillSubdivision = 2; _settingsDirty = true; }
                if (GUILayout.Button("4")) { manager.fillSubdivision = 4; _settingsDirty = true; }
                GUILayout.Label($"Current: {manager.fillSubdivision}x");
                GUILayout.EndHorizontal();

                // Cache controls
                float limMB; int limClips; ProceduralMusic.GetCacheLimits(out limMB, out limClips);
                GUILayout.Label($"Cache Limits: {limMB:F0} MB, {limClips} clips");
                GUILayout.BeginHorizontal();
                GUILayout.Label("Set MB:", GUILayout.Width(80));
                if (GUILayout.Button("16MB")) { ProceduralMusic.SetCacheLimits(16f, -1); _settingsDirty = true; SaveNowIfPersistent(); }
                if (GUILayout.Button("32MB")) { ProceduralMusic.SetCacheLimits(32f, -1); _settingsDirty = true; SaveNowIfPersistent(); }
                if (GUILayout.Button("64MB")) { ProceduralMusic.SetCacheLimits(64f, -1); _settingsDirty = true; SaveNowIfPersistent(); }
                if (GUILayout.Button("128MB")) { ProceduralMusic.SetCacheLimits(128f, -1); _settingsDirty = true; SaveNowIfPersistent(); }
                GUILayout.EndHorizontal();
                GUILayout.BeginHorizontal();
                GUILayout.Label("Set Clips:", GUILayout.Width(80));
                if (GUILayout.Button("64")) { ProceduralMusic.SetCacheLimits(-1f, 64); _settingsDirty = true; SaveNowIfPersistent(); }
                if (GUILayout.Button("128")) { ProceduralMusic.SetCacheLimits(-1f, 128); _settingsDirty = true; SaveNowIfPersistent(); }
                if (GUILayout.Button("256")) { ProceduralMusic.SetCacheLimits(-1f, 256); _settingsDirty = true; SaveNowIfPersistent(); }
                GUILayout.EndHorizontal();
                GUILayout.BeginHorizontal();
                if (GUILayout.Button("Clear Cache")) ProceduralMusic.ClearCache();
                GUILayout.EndHorizontal();

                // Test controls
                GUILayout.Space(4);
                GUILayout.Label("Test & Control:");
                GUILayout.BeginHorizontal();
                if (GUILayout.Button("Stinger Rise")) manager.PlayStinger("rise");
                if (GUILayout.Button("Stinger Hit")) manager.PlayStinger("hit");
                if (GUILayout.Button("Perc Fill")) manager.PlayDrumFill();
                if (GUILayout.Button("Advance Prog")) { manager.AdvanceProgressionSmart(1); }
                bool newAuto = GUILayout.Toggle(manager.enableAutoProgression, "AutoProg");
                if (newAuto != manager.enableAutoProgression) { manager.enableAutoProgression = newAuto; manager.RescheduleAutoProgression(); _settingsDirty = true; }
                bool showBar = GUILayout.Toggle(showAutoProgBar, "Show AutoProg Bar");
                if (showBar != showAutoProgBar) { showAutoProgBar = showBar; }
                GUILayout.EndHorizontal();

                // Intensity control
                GUILayout.BeginHorizontal();
                GUILayout.Label($"Intensity: {manager.intensity:F2}", GUILayout.Width(120));
                float newInt = GUILayout.HorizontalSlider(manager.intensity, 0f, 1f, GUILayout.Width(300));
                if (Mathf.Abs(newInt - manager.intensity) > 0.001f) { manager.SetIntensity(newInt); _settingsDirty = true; }
                if (GUILayout.Button("Reset Defaults", GUILayout.Width(140))) { manager.ResetToDefaults(true); _settingsDirty = true; SaveNowIfPersistent(); }
                GUILayout.EndHorizontal();

                // Mixer controls
                GUILayout.Space(4);
                GUILayout.Label("Mixer:");
                GUILayout.BeginHorizontal();
                bool mp = GUILayout.Toggle(manager.mutePad, "Mute Pad", GUILayout.Width(90)); if (mp != manager.mutePad) { manager.mutePad = mp; _settingsDirty = true; }
                bool sp = GUILayout.Toggle(manager.soloPad, "Solo", GUILayout.Width(60)); if (sp != manager.soloPad) { manager.soloPad = sp; _settingsDirty = true; }
                GUILayout.Label($"Pad {manager.trimPad:F2}", GUILayout.Width(80));
                float npad = Mathf.Clamp(GUILayout.HorizontalSlider(manager.trimPad, 0f, 2f, GUILayout.Width(180)), 0f, 2f);
                if (Mathf.Abs(npad - manager.trimPad) > 0.0001f) { manager.trimPad = npad; _settingsDirty = true; }
                bool mbb = GUILayout.Toggle(manager.muteBass, "Mute Bass", GUILayout.Width(90)); if (mbb != manager.muteBass) { manager.muteBass = mbb; _settingsDirty = true; }
                bool sbb = GUILayout.Toggle(manager.soloBass, "Solo", GUILayout.Width(60)); if (sbb != manager.soloBass) { manager.soloBass = sbb; _settingsDirty = true; }
                GUILayout.Label($"Bass {manager.trimBass:F2}", GUILayout.Width(80));
                float nb = Mathf.Clamp(GUILayout.HorizontalSlider(manager.trimBass, 0f, 2f, GUILayout.Width(180)), 0f, 2f);
                if (Mathf.Abs(nb - manager.trimBass) > 0.0001f) { manager.trimBass = nb; _settingsDirty = true; }
                GUILayout.EndHorizontal();
                GUILayout.BeginHorizontal();
                bool md = GUILayout.Toggle(manager.muteDrums, "Mute Drums", GUILayout.Width(90)); if (md != manager.muteDrums) { manager.muteDrums = md; _settingsDirty = true; }
                bool sd = GUILayout.Toggle(manager.soloDrums, "Solo", GUILayout.Width(60)); if (sd != manager.soloDrums) { manager.soloDrums = sd; _settingsDirty = true; }
                GUILayout.Label($"Drums {manager.trimDrums:F2}", GUILayout.Width(80));
                float nd = Mathf.Clamp(GUILayout.HorizontalSlider(manager.trimDrums, 0f, 2f, GUILayout.Width(180)), 0f, 2f);
                if (Mathf.Abs(nd - manager.trimDrums) > 0.0001f) { manager.trimDrums = nd; _settingsDirty = true; }
                bool ma = GUILayout.Toggle(manager.muteArp, "Mute Arp", GUILayout.Width(90)); if (ma != manager.muteArp) { manager.muteArp = ma; _settingsDirty = true; }
                bool sa = GUILayout.Toggle(manager.soloArp, "Solo", GUILayout.Width(60)); if (sa != manager.soloArp) { manager.soloArp = sa; _settingsDirty = true; }
                GUILayout.Label($"Arp {manager.trimArp:F2}", GUILayout.Width(80));
                float na = Mathf.Clamp(GUILayout.HorizontalSlider(manager.trimArp, 0f, 2f, GUILayout.Width(180)), 0f, 2f);
                if (Mathf.Abs(na - manager.trimArp) > 0.0001f) { manager.trimArp = na; _settingsDirty = true; }
                GUILayout.EndHorizontal();

                // Level meters
                GUILayout.Space(2);
                GUILayout.Label("Meters (RMS)");
                DrawMeterRow("Pad", _padLevel, Color.cyan);
                DrawMeterRow("Bass", _bassLevel, Color.green);
                DrawMeterRow("Drums", _drumLevel, Color.yellow);
                DrawMeterRow("Arp", _arpLevel, Color.magenta);

                // Persistence controls
                GUILayout.Space(4);
                GUILayout.Label("Persistence:");
                bool persist = GUILayout.Toggle(manager.persistRuntimeSettings, "Persist runtime settings (auto-load on start)");
                if (persist != manager.persistRuntimeSettings) { manager.persistRuntimeSettings = persist; _settingsDirty = true; SaveNowIfPersistent(); }
                GUILayout.BeginHorizontal();
                bool asq = GUILayout.Toggle(manager.autoSaveOnQuit, "Auto-save on quit", GUILayout.Width(160));
                if (asq != manager.autoSaveOnQuit) { manager.autoSaveOnQuit = asq; _settingsDirty = true; SaveNowIfPersistent(); }
                bool asc = GUILayout.Toggle(manager.autoSaveOnChange, "Auto-save on change", GUILayout.Width(180));
                if (asc != manager.autoSaveOnChange) { manager.autoSaveOnChange = asc; _settingsDirty = true; SaveNowIfPersistent(); }
                GUILayout.EndHorizontal();
                if (!string.IsNullOrEmpty(manager.lastPresetSlot))
                {
                    string path = manager.lastPresetPath;
                    if (!string.IsNullOrEmpty(path) && path.Length > 60) path = "…" + path.Substring(path.Length - 60);
                    GUILayout.Label($"Last preset: {manager.lastPresetSlot}  {path}");
                }
                GUILayout.BeginHorizontal();
                if (GUILayout.Button("Save Settings", GUILayout.Width(120))) { SaveNow(); }
                if (GUILayout.Button("Load Settings", GUILayout.Width(120))) { manager.LoadRuntimeSettings(true); }
                if (GUILayout.Button("Clear Saved", GUILayout.Width(120))) { manager.ClearSavedRuntimeSettings(); }
                GUILayout.EndHorizontal();

                // JSON preset import/export
                GUILayout.BeginHorizontal();
                if (GUILayout.Button("Copy JSON", GUILayout.Width(120)))
                {
                    string json = manager.ExportRuntimeSettingsJson(true);
                    GUIUtility.systemCopyBuffer = json;
                }
                if (GUILayout.Button("Paste JSON", GUILayout.Width(120)))
                {
                    string json = GUIUtility.systemCopyBuffer;
                    if (manager.ImportRuntimeSettingsJson(json, true) && manager.persistRuntimeSettings)
                    {
                        SaveNow();
                    }
                }
                GUILayout.EndHorizontal();

                // Disk preset slots A/B/C
                GUILayout.BeginHorizontal();
                GUILayout.Label("Presets:", GUILayout.Width(60));
                if (GUILayout.Button("Save A", GUILayout.Width(70))) { if (manager.SavePresetSlot('A') && manager.persistRuntimeSettings) SaveNow(); }
                if (GUILayout.Button("Load A", GUILayout.Width(70))) { if (manager.LoadPresetSlot('A', true) && manager.persistRuntimeSettings) SaveNow(); }
                if (GUILayout.Button("Save B", GUILayout.Width(70))) { if (manager.SavePresetSlot('B') && manager.persistRuntimeSettings) SaveNow(); }
                if (GUILayout.Button("Load B", GUILayout.Width(70))) { if (manager.LoadPresetSlot('B', true) && manager.persistRuntimeSettings) SaveNow(); }
                if (GUILayout.Button("Save C", GUILayout.Width(70))) { if (manager.SavePresetSlot('C') && manager.persistRuntimeSettings) SaveNow(); }
                if (GUILayout.Button("Load C", GUILayout.Width(70))) { if (manager.LoadPresetSlot('C', true) && manager.persistRuntimeSettings) SaveNow(); }
                GUILayout.EndHorizontal();

                // Overrides for complexity/richness
                GUILayout.Space(4);
                GUILayout.Label("Overrides:");
                GUILayout.BeginHorizontal();
                bool ldc = GUILayout.Toggle(manager.lockDrumComplexity, "Lock Drum");
                if (ldc != manager.lockDrumComplexity) { manager.lockDrumComplexity = ldc; manager.ReapplyAdaptiveState(); _settingsDirty = true; }
                GUILayout.Label("Lvl:", GUILayout.Width(32));
                if (GUILayout.Button("0")) { manager.lockedDrumComplexity = 0; manager.lockDrumComplexity = true; manager.ReapplyAdaptiveState(); _settingsDirty = true; }
                if (GUILayout.Button("1")) { manager.lockedDrumComplexity = 1; manager.lockDrumComplexity = true; manager.ReapplyAdaptiveState(); _settingsDirty = true; }
                if (GUILayout.Button("2")) { manager.lockedDrumComplexity = 2; manager.lockDrumComplexity = true; manager.ReapplyAdaptiveState(); _settingsDirty = true; }
                GUILayout.Label($"Current: {manager.CurrentDrumComplexity}");
                GUILayout.EndHorizontal();

                GUILayout.BeginHorizontal();
                bool lbc = GUILayout.Toggle(manager.lockBassComplexity, "Lock Bass");
                if (lbc != manager.lockBassComplexity) { manager.lockBassComplexity = lbc; manager.ReapplyAdaptiveState(); _settingsDirty = true; }
                GUILayout.Label("Lvl:", GUILayout.Width(32));
                if (GUILayout.Button("0")) { manager.lockedBassComplexity = 0; manager.lockBassComplexity = true; manager.ReapplyAdaptiveState(); _settingsDirty = true; }
                if (GUILayout.Button("1")) { manager.lockedBassComplexity = 1; manager.lockBassComplexity = true; manager.ReapplyAdaptiveState(); _settingsDirty = true; }
                if (GUILayout.Button("2")) { manager.lockedBassComplexity = 2; manager.lockBassComplexity = true; manager.ReapplyAdaptiveState(); _settingsDirty = true; }
                GUILayout.Label($"Current: {manager.CurrentBassComplexity}");
                GUILayout.EndHorizontal();

                GUILayout.BeginHorizontal();
                bool lpr = GUILayout.Toggle(manager.lockPadRichness, "Lock Pad");
                if (lpr != manager.lockPadRichness) { manager.lockPadRichness = lpr; manager.ReapplyAdaptiveState(); _settingsDirty = true; }
                GUILayout.Label("Lvl:", GUILayout.Width(32));
                if (GUILayout.Button("0")) { manager.lockedPadRichness = 0; manager.lockPadRichness = true; manager.ReapplyAdaptiveState(); _settingsDirty = true; }
                if (GUILayout.Button("1")) { manager.lockedPadRichness = 1; manager.lockPadRichness = true; manager.ReapplyAdaptiveState(); _settingsDirty = true; }
                if (GUILayout.Button("2")) { manager.lockedPadRichness = 2; manager.lockPadRichness = true; manager.ReapplyAdaptiveState(); _settingsDirty = true; }
                GUILayout.Label($"Current: {manager.CurrentPadRichness}");
                GUILayout.EndHorizontal();

                // BPM controls
                GUILayout.BeginHorizontal();
                GUILayout.Label("BPM:", GUILayout.Width(40));
                if (GUILayout.Button("-5", GUILayout.Width(40))) { AdjustBpm(manager, -5f); _settingsDirty = true; SaveNowIfPersistent(); }
                if (GUILayout.Button("-1", GUILayout.Width(40))) { AdjustBpm(manager, -1f); _settingsDirty = true; SaveNowIfPersistent(); }
                GUILayout.Label($"{manager.bpm:F0}", GUILayout.Width(60));
                if (GUILayout.Button("+1", GUILayout.Width(40))) { AdjustBpm(manager, +1f); _settingsDirty = true; SaveNowIfPersistent(); }
                if (GUILayout.Button("+5", GUILayout.Width(40))) { AdjustBpm(manager, +5f); _settingsDirty = true; SaveNowIfPersistent(); }
                if (GUILayout.Button("Tap", GUILayout.Width(60))) { TapTempo(manager); _settingsDirty = true; SaveNowIfPersistent(); }
                if (GUILayout.Button("Regenerate", GUILayout.Width(100))) { RegenerateAll(manager); _settingsDirty = true; SaveNowIfPersistent(); }
                GUILayout.EndHorizontal();

                // Ducking controls
                GUILayout.Space(4);
                GUILayout.Label("Ducking:");
                bool ed = GUILayout.Toggle(manager.enableDucking, "Enable Ducking");
                if (ed != manager.enableDucking) { manager.enableDucking = ed; _settingsDirty = true; }
                GUILayout.BeginHorizontal();
                GUILayout.Label($"Pad Amt: {manager.duckPadAmount:F2}", GUILayout.Width(120));
                float dpa = Mathf.Clamp01(GUILayout.HorizontalSlider(manager.duckPadAmount, 0f, 1f, GUILayout.Width(200)));
                if (Mathf.Abs(dpa - manager.duckPadAmount) > 0.0001f) { manager.duckPadAmount = dpa; _settingsDirty = true; }
                GUILayout.Label($"Arp Amt: {manager.duckArpAmount:F2}", GUILayout.Width(120));
                float daa = Mathf.Clamp01(GUILayout.HorizontalSlider(manager.duckArpAmount, 0f, 1f, GUILayout.Width(200)));
                if (Mathf.Abs(daa - manager.duckArpAmount) > 0.0001f) { manager.duckArpAmount = daa; _settingsDirty = true; }
                GUILayout.EndHorizontal();
                GUILayout.BeginHorizontal();
                GUILayout.Label($"Atk: {manager.duckAttackSeconds:F2}s", GUILayout.Width(120));
                float das = Mathf.Clamp(GUILayout.HorizontalSlider(manager.duckAttackSeconds, 0.005f, 0.2f, GUILayout.Width(200)), 0.005f, 1f);
                if (Mathf.Abs(das - manager.duckAttackSeconds) > 0.0001f) { manager.duckAttackSeconds = das; _settingsDirty = true; }
                GUILayout.Label($"Hold: {manager.duckHoldSeconds:F2}s", GUILayout.Width(120));
                float dhs = Mathf.Clamp(GUILayout.HorizontalSlider(manager.duckHoldSeconds, 0f, 0.5f, GUILayout.Width(200)), 0f, 2f);
                if (Mathf.Abs(dhs - manager.duckHoldSeconds) > 0.0001f) { manager.duckHoldSeconds = dhs; _settingsDirty = true; }
                GUILayout.EndHorizontal();
                GUILayout.BeginHorizontal();
                GUILayout.Label($"Rel: {manager.duckReleaseSeconds:F2}s", GUILayout.Width(120));
                float drs = Mathf.Clamp(GUILayout.HorizontalSlider(manager.duckReleaseSeconds, 0.05f, 1.0f, GUILayout.Width(200)), 0.05f, 3f);
                if (Mathf.Abs(drs - manager.duckReleaseSeconds) > 0.0001f) { manager.duckReleaseSeconds = drs; _settingsDirty = true; }
                if (GUILayout.Button("Test Duck", GUILayout.Width(100))) { manager.SendMessage("StartDuck", SendMessageOptions.DontRequireReceiver); }
                GUILayout.EndHorizontal();
            }
            var stats = ProceduralMusic.GetCacheStats();
            float mb = stats.TotalBytesApprox / (1024f * 1024f);
            GUILayout.Label($"Cache: clips={stats.ClipCount} ~{stats.TotalSecondsApprox:F1}s ~{mb:F1}MB");
            if (showProfiling && _recentProfile != null && _recentProfile.Length > 0)
            {
                GUILayout.Label("Profiling (recent):");
                int max = Mathf.Min(5, _recentProfile.Length);
                for (int i = _recentProfile.Length - max; i < _recentProfile.Length; i++)
                {
                    if (i >= 0) GUILayout.Label(_recentProfile[i]);
                }
            }
            GUILayout.EndArea();
            GUI.color = prevColor;
        }

        private void AdjustBpm(ProceduralMusicManager m, float delta)
        {
            m.bpm = Mathf.Max(40f, m.bpm + delta);
            RegenerateAll(m);
        }

        private void TapTempo(ProceduralMusicManager m)
        {
            float now = Time.unscaledTime;
            if (_tapCount < _tapTimes.Length)
            {
                _tapTimes[_tapCount++] = now;
            }
            else
            {
                // shift left
                for (int i = 1; i < _tapTimes.Length; i++) _tapTimes[i - 1] = _tapTimes[i];
                _tapTimes[_tapTimes.Length - 1] = now;
            }
            int count = Mathf.Min(_tapCount, _tapTimes.Length);
            if (count >= 2)
            {
                int start = Mathf.Max(0, count - 5); // up to last 5 intervals
                int n = 0;
                float sum = 0f;
                for (int i = start + 1; i < count; i++)
                {
                    float dt = _tapTimes[i] - _tapTimes[i - 1];
                    if (dt > 0.08f && dt < 2.0f) { sum += dt; n++; }
                }
                if (n > 0)
                {
                    float avg = sum / n;
                    float bpm = Mathf.Clamp(60f / avg, 40f, 200f);
                    m.bpm = bpm;
                    RegenerateAll(m);
                }
            }
        }

        private void RegenerateAll(ProceduralMusicManager m)
        {
            m.GenerateAll();
            m.PlayAll();
            m.RescheduleAutoProgression();
        }

        private float ComputeRms(AudioSource src)
        {
            if (src == null || src.clip == null || !_meterBufIsValid(src.clip))
            {
                return 0f;
            }
            var clip = src.clip;
            int window = _meterBuf.Length;
            int total = clip.samples;
            if (total <= 0) return 0f;
            int pos = src.timeSamples % total;
            if (pos < 0) pos += total;
            int remaining = total - pos;
            if (remaining >= window)
            {
                clip.GetData(_meterBuf, pos);
            }
            else
            {
                // wrap-around read
                float[] tmp = _meterBuf;
                clip.GetData(tmp, pos);
                // tmp currently holds up to 'remaining' valid samples from pos; need fill rest from start
                if (remaining < window)
                {
                    // shift leftover to front to keep contiguous buffer
                    // but simpler: compute RMS in two parts
                    float sum = 0f; int n = 0;
                    for (int i = 0; i < remaining; i++) { float v = tmp[i]; sum += v * v; n++; }
                    int need = window - remaining;
                    int take = Mathf.Min(need, total);
                    // reuse tmp for second part
                    clip.GetData(tmp, 0);
                    for (int i = 0; i < take; i++) { float v = tmp[i]; sum += v * v; n++; }
                    return n > 0 ? Mathf.Sqrt(sum / n) : 0f;
                }
            }
            // contiguous case
            float s = 0f; int count = 0; var buf = _meterBuf; int len = Mathf.Min(window, buf.Length);
            for (int i = 0; i < len; i++) { float v = buf[i]; s += v * v; count++; }
            return count > 0 ? Mathf.Sqrt(s / count) : 0f;
        }

        private bool _meterBufIsValid(AudioClip clip)
        {
            int need = Mathf.Min(256, clip.samples);
            if (_meterBuf == null || _meterBuf.Length != need)
            {
                _meterBuf = new float[need];
            }
            return _meterBuf.Length > 0;
        }

        private void DrawMeterRow(string label, float value, Color color)
        {
            value = Mathf.Clamp01(value * 4f); // scale up for visibility
            GUILayout.BeginHorizontal();
            GUILayout.Label(label, GUILayout.Width(50));
            Rect r = GUILayoutUtility.GetRect(1, 12, GUILayout.ExpandWidth(true));
            // background
            Color prev = GUI.color;
            GUI.color = Color.black * 0.5f;
            GUI.DrawTexture(r, Texture2D.whiteTexture);
            // fill
            GUI.color = color;
            Rect fill = new Rect(r.x, r.y, r.width * value, r.height);
            GUI.DrawTexture(fill, Texture2D.whiteTexture);
            GUI.color = prev;
            GUILayout.Label(string.Format("{0:F2}", value), GUILayout.Width(40));
            GUILayout.EndHorizontal();
        }

        private void SaveNow()
        {
            if (manager == null) return;
            manager.SaveRuntimeSettings();
            _settingsDirty = false;
            _lastSaveTime = Time.unscaledTime;
            _saveToastUntil = Time.unscaledTime + SaveToastDuration;
        }

        private void SaveNowIfPersistent()
        {
            if (manager != null && manager.persistRuntimeSettings)
            {
                SaveNow();
            }
        }

        private void DrawSimpleProgress(string label, float fraction, Color color)
        {
            GUILayout.BeginHorizontal();
            GUILayout.Label(label + ":", GUILayout.Width(120));
            Rect r = GUILayoutUtility.GetRect(1, 10, GUILayout.ExpandWidth(true));
            // background
            Color prev = GUI.color;
            GUI.color = Color.black * 0.4f;
            GUI.DrawTexture(r, Texture2D.whiteTexture);
            // fill
            GUI.color = color;
            Rect fill = new Rect(r.x, r.y, r.width * Mathf.Clamp01(fraction), r.height);
            GUI.DrawTexture(fill, Texture2D.whiteTexture);
            GUI.color = prev;
            GUILayout.Label(string.Format("{0,3:P0}", Mathf.Clamp01(fraction)), GUILayout.Width(40));
            GUILayout.EndHorizontal();
        }
    }
}
