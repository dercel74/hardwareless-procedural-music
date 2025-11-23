# Character Creation System Scripts

This folder contains the Metahuman Character Creation System for Unity.

## Scripts Overview

### Core System
- **CharacterData.cs** - Data model for character attributes (body, face, colors)
- **CharacterCustomization.cs** - Component for applying character appearance to visual components
- **CharacterPresetManager.cs** - Preset management system with PlayerPrefs and JSON
- **MetahumanCharacterCreator.cs** - Main manager orchestrating the creation workflow

### UI & Debug
- **CharacterCreatorDebugHUD.cs** - Runtime debug interface (press F10 to toggle)

### Examples
- **CharacterCreationExample.cs** - Example script showing how to use the system

## Quick Start

1. **Add to Scene:**
   ```
   - Create GameObject "CharacterCreator"
   - Add MetahumanCharacterCreator component
   - Add CharacterCreatorDebugHUD component
   ```

2. **Setup Character:**
   ```
   - Create GameObject "Character"
   - Add CharacterCustomization component
   - Assign visual component references (body, head, renderers)
   - Link to CharacterCreator
   ```

3. **Test:**
   ```
   - Press Play
   - Press F10 to open debug HUD
   - Click "Randomize" or adjust sliders
   ```

## Keyboard Shortcuts (Example Script)

When using CharacterCreationExample.cs:
- **R** - Randomize character
- **1/2/3** - Save to preset slots 0/1/2
- **L** - Load from preset slot 0
- **E** - Export to JSON file
- **I** - Import from JSON file
- **F10** - Toggle debug HUD

## Documentation

Full documentation available at: `Assets/Documentation/MetahumanCharacterCreation.md`

## Architecture

All scripts use the `Hardwareless.Character` namespace and follow Unity best practices:
- MonoBehaviour components for scene objects
- Static manager for preset operations
- Serializable data for persistence
- Material instance management to prevent memory leaks
- Optimized preset loading with cached names

## Integration

The system integrates with:
- **Audio System** - UI sound effects via AudioSystem.PlayOneShot2D()
- **PlayerPrefs** - Persistent storage for presets
- **JSON** - Import/export functionality

## Performance

- Material instances properly managed to avoid memory leaks
- Optimized preset slot queries (caches character names)
- Efficient attribute updates using reflection
- Smooth transitions using Time.deltaTime for frame-rate independence
