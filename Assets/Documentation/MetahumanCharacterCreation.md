# Metahuman Character Creation System

## Overview

The Metahuman Character Creation System provides a comprehensive framework for creating, customizing, and managing character appearances in Unity. The system offers real-time character customization with smooth transitions, preset management, and seamless integration with the existing Hardwareless audio system.

## Features

### 🎭 Character Customization
- **Body Attributes** - Height, build, body weight
- **Facial Features** - Head size, face width, jaw, chin, cheekbones
- **Eyes** - Size, distance, height, color
- **Nose** - Size, width, bridge shape
- **Mouth** - Size, lip thickness, width
- **Ears** - Size and rotation
- **Appearance** - Hair style/color, skin tone, outfits, accessories

### 💾 Preset Management
- **10 Preset Slots** - Save/load character configurations
- **Last Used Character** - Auto-save and restore on restart
- **JSON Export/Import** - Share characters via files
- **Preset Slot Info** - View all saved characters at a glance

### 🎨 Visual Features
- **Smooth Transitions** - Morphing between character configurations
- **Real-time Updates** - Instant visual feedback
- **Color Customization** - RGB control for eyes, hair, and skin
- **Style Variations** - Multiple hair styles and outfits

### 🔧 Debug Interface
- **F10 Hotkey** - Toggle character creator HUD
- **Tabbed Interface** - Organized controls (Body, Face, Colors, Presets)
- **Live Editing** - All parameters adjustable at runtime
- **Quick Actions** - Randomize, reset, save, load

## Components

### CharacterData
Serializable data structure storing all character attributes. Supports:
- JSON serialization/deserialization
- Random character generation
- Default character creation
- Deep cloning

**Example Usage:**
```csharp
// Create a random character
CharacterData randomChar = CharacterData.CreateRandom();

// Create default character
CharacterData defaultChar = CharacterData.CreateDefault();

// Serialize to JSON
string json = character.ToJson();

// Deserialize from JSON
CharacterData loaded = CharacterData.FromJson(json);
```

### CharacterCustomization
MonoBehaviour that applies character data to visual components. Features:
- Automatic customization on Start
- Smooth transitions between configurations
- Support for multiple visual components (body, head, skin, hair, eyes)
- Hair style and outfit switching

**Setup:**
1. Attach to GameObject with character model
2. Assign visual component references:
   - `bodyRoot` - Root transform for body scaling
   - `headBone` - Head bone for head scaling
   - `skinRenderer` - Renderer for skin material
   - `hairRenderer` - Renderer for hair material
   - `eyesRenderer` - Renderer for eyes material
   - `hairStyles` - Array of hair style GameObjects
   - `outfits` - Array of outfit GameObjects
3. Configure `currentCharacter` or let it auto-create default

**Example Usage:**
```csharp
// Get reference
CharacterCustomization customization = GetComponent<CharacterCustomization>();

// Apply new character
customization.ApplyCustomization(characterData, smoothTransition: true);

// Randomize
customization.RandomizeCharacter();

// Reset to default
customization.ResetToDefault();

// Get current data
CharacterData current = customization.GetCharacterData();
```

### CharacterPresetManager
Static utility class for preset management using PlayerPrefs and JSON. Features:
- 10 preset slots (0-9)
- Last used character auto-save
- File import/export
- Preset slot information queries

**Example Usage:**
```csharp
// Save to preset slot
CharacterPresetManager.SavePreset(characterData, slotIndex: 0);

// Load from preset slot
CharacterData loaded = CharacterPresetManager.LoadPreset(slotIndex: 0);

// Check if slot has data
bool hasData = CharacterPresetManager.HasPreset(slotIndex: 0);

// Delete preset
CharacterPresetManager.DeletePreset(slotIndex: 0);

// Export to file
CharacterPresetManager.ExportToFile(characterData, "character.json");

// Import from file
CharacterData imported = CharacterPresetManager.ImportFromFile("character.json");

// Get all preset slots
PresetSlotInfo[] slots = CharacterPresetManager.GetPresetSlots();
```

### MetahumanCharacterCreator
Main manager component that orchestrates the character creation workflow. Features:
- Auto-save with configurable delay
- Audio integration (save/load/randomize sounds)
- Preset management integration
- Runtime attribute updates
- Unsaved changes tracking

**Setup:**
1. Attach to GameObject in scene
2. Assign `characterCustomization` reference
3. Configure audio settings (optional)
4. Set `autoSaveOnChange` for automatic persistence

**Example Usage:**
```csharp
// Get reference
MetahumanCharacterCreator creator = GetComponent<MetahumanCharacterCreator>();

// Create random character
creator.CreateRandomCharacter();

// Reset character
creator.ResetCharacter();

// Save to preset
creator.SaveToPreset(slotIndex: 0);

// Load from preset
creator.LoadFromPreset(slotIndex: 0);

// Update specific attribute
creator.UpdateAttribute("height", 0.8f);
creator.UpdateColorAttribute("eyeColor", Color.blue);
creator.UpdateIntAttribute("hairStyle", 3);

// Check for unsaved changes
bool unsaved = creator.HasUnsavedChanges();
```

### CharacterCreatorDebugHUD
Runtime debug interface for character creation. Features:
- Tabbed interface (Body, Face, Colors, Presets)
- Sliders for all numeric attributes
- RGB color pickers
- Preset management UI
- Unsaved changes indicator

**Setup:**
1. Attach to GameObject in scene (same as or separate from MetahumanCharacterCreator)
2. Assign `characterCreator` reference
3. Configure `toggleKey` (default: F10)
4. Set `showOnStart` if desired

**Usage:**
- Press **F10** to toggle HUD
- Navigate tabs to access different customization categories
- Use sliders to adjust values in real-time
- Click "Randomize" for instant random character
- Click "Reset" to return to default
- Manage presets in the Presets tab

## Integration with Audio System

The character creation system integrates seamlessly with the Hardwareless audio system:

```csharp
// Audio events triggered by character creator:
// - "ui-click" on randomize
// - "ui-save" on save operations
// - "ui-load" on load operations
```

Enable/disable audio feedback via `MetahumanCharacterCreator` settings:
- `playSaveSound`
- `playLoadSound`
- `playRandomizeSound`

## Workflow Examples

### Basic Character Creation
```csharp
// 1. Create character creator manager
GameObject creatorObj = new GameObject("CharacterCreator");
MetahumanCharacterCreator creator = creatorObj.AddComponent<MetahumanCharacterCreator>();

// 2. Create character with customization
GameObject characterObj = new GameObject("Character");
CharacterCustomization customization = characterObj.AddComponent<CharacterCustomization>();

// 3. Link them
creator.characterCustomization = customization;

// 4. Create random character
creator.CreateRandomCharacter();

// 5. Save to preset
creator.SaveToPreset(0);
```

### Runtime Customization
```csharp
// Access the character data
CharacterData data = customization.currentCharacter;

// Modify attributes
data.height = 0.9f;
data.eyeColor = Color.green;
data.hairStyle = 5;

// Apply changes
customization.ApplyCustomization(data, smoothTransition: true);
```

### Preset Workflow
```csharp
// Create several characters and save them
for (int i = 0; i < 5; i++)
{
    creator.CreateRandomCharacter();
    creator.SaveToPreset(i);
}

// Load specific preset
creator.LoadFromPreset(2);

// Export to share
creator.ExportCharacter("my_character.json");

// Import shared character
creator.ImportCharacter("shared_character.json");
```

## Technical Details

### Data Persistence
- Uses Unity's `PlayerPrefs` for preset storage
- JSON serialization via `JsonUtility`
- Preset keys: `CharacterPreset_0` through `CharacterPreset_9`
- Last used key: `Character_LastUsed`

### Performance
- Smooth transitions use `Time.deltaTime` for frame-rate independence
- Lerp operations for smooth attribute interpolation
- Minimal garbage allocation (reuses data structures)
- Efficient material property updates

### Extensibility
- Add new attributes to `CharacterData` class
- Extend `CharacterCustomization` to support new visual components
- Add custom tabs to `CharacterCreatorDebugHUD`
- Implement custom preset storage backends

## Architecture

The system follows the same architectural patterns as the Procedural Music System:
- **Namespace**: `Hardwareless.Character`
- **Separation of Concerns**: Data, customization, management, UI
- **Manager Pattern**: Central orchestration via `MetahumanCharacterCreator`
- **Debug Tools**: Runtime HUD for development and testing

## Best Practices

1. **Always assign visual component references** in `CharacterCustomization` for proper visual updates
2. **Use smooth transitions** for better user experience (enabled by default)
3. **Enable auto-save** to prevent data loss
4. **Use preset slots** for character variations in your game
5. **Leverage the debug HUD** during development for rapid iteration

## Future Enhancements

Potential additions:
- Blend shapes support for more detailed facial features
- Animation integration for character expressions
- Clothing layering system
- Procedural texture generation
- Body pose customization
- Voice modulation integration
- Character stats and attributes
- Network synchronization for multiplayer

## Troubleshooting

**Character doesn't update visually:**
- Check that visual component references are assigned in `CharacterCustomization`
- Ensure materials have `_Color` property
- Verify GameObject references in `hairStyles` and `outfits` arrays

**Presets don't save:**
- Check console for error messages
- Ensure `PlayerPrefs` is supported on your platform
- Verify write permissions

**Debug HUD doesn't appear:**
- Press F10 to toggle
- Check that `CharacterCreatorDebugHUD` component is enabled
- Verify `characterCreator` reference is assigned

**Audio doesn't play:**
- Check that `AudioSystem` is properly configured
- Verify SFX clips exist for "ui-click", "ui-save", "ui-load"
- Enable audio flags in `MetahumanCharacterCreator`

---

**Ready to create amazing Metahuman characters!** 🎭✨
