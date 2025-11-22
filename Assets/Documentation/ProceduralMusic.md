# Procedural Music System (Hardwareless)

This project includes a fully runtime procedural music system with adaptive layers, beat alignment, responsive stingers/fills, ducking, caching, and convenient runtime controls.

## Quick Start

- Add `ProceduralMusicManager` to a scene (or use the existing one).
- Press `F9` in Play Mode to open the HUD.
- Adjust Intensity, BPM, and mixer controls in real-time.

Note: If no manager is present in a scene, a default one is auto-created on load (can be disabled via the Editor menu).

## Key Features

- Adaptive layers: pad, bass, drums, arp with complexity/richness and crossfades.
- Beat alignment: progression changes and stingers/fills can align to beat/subdivisions.
- Event stingers and percussive fills.
- Sidechain-like ducking on stingers/fills (configurable envelope and per-layer amounts).
- Runtime mixer: per-layer mute, solo, and trim.
- Per-layer RMS meters for quick visibility.
- LRU cache for generated clips with adjustable size/clip limits.
- HUD title badges: shows active preset, AutoProg state, and countdown to next auto-progression.
- Save toast: transient "[Saved]" indicator after manual or debounced saves.
- Optional AutoProg progress bar (toggle in HUD) to visualize the countdown.

## Persistence & Presets

- PlayerPrefs persistence: enable "Persist runtime settings" in the HUD to auto-load on start.
- Auto-save: enable on-quit or on-change (throttled) via HUD toggles.
- Save/Load/Clear buttons to manage PlayerPrefs stored settings.
- Clipboard presets: Copy/Paste JSON to share or version settings.
- Disk presets: Save/Load preset slots A/B/C stored under `Application.persistentDataPath/music_presets`.
- HUD shows the last loaded/saved preset slot and truncated path.
- Immediate saves: Cache limit presets, BPM steppers, JSON paste, disk preset Save/Load, and Reset Defaults trigger immediate saves when persistence is enabled.

## Recommended Defaults

Use the "Reset Defaults" button to restore:

- Align progression to chord: on (beats/chord = 4).
- Align fills to beat: on; align stingers: off.
- Auto progression: on.
- Pad filter dynamics: on.
- Ducking: on with tuned A/H/R.
- Mixer: all layers unmuted, trims at 1.0, no solos.

## Tips

- Use BPM steppers and Tap Tempo to find a groove; Regenerate to apply tempo changes across layers.
- Lock drum/bass complexity or pad richness for focused testing.
- Use cache limits (MB/clips) presets to control memory usage; Clear Cache to reset.
- Watch the HUD title for "AutoProg: ON, Next: Xs" to anticipate upcoming progression changes.
- A brief "[Saved]" toast confirms settings were stored; continuous slider tweaks are auto-saved with a short debounce.
- Toggle "Show AutoProg Bar" in the HUD if you prefer a minimal header.

### Editor Helpers

- Menu: `Hardwareless/Audio/Add Procedural Music Manager` adds a manager to the scene.
- Menu: `Hardwareless/Audio/Toggle Music Bootstrap` enables/disables auto-creation via PlayerPrefs.

## Files & Components

- `Assets/Scripts/Audio/ProceduralMusic.cs`: synthesis + LRU cache.
- `Assets/Scripts/Audio/ProceduralMusicManager.cs`: orchestration, adaptation, alignment, ducking, presets.
- `Assets/Scripts/Audio/ProceduralMusicDebugHUD.cs`: runtime controls, meters, persistence/presets UI.

