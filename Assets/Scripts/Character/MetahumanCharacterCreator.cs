using UnityEngine;
using Hardwareless.Audio;

namespace Hardwareless.Character
{
    /// <summary>
    /// Main manager for the Metahuman character creation system.
    /// Handles character creation workflow, preset management, and UI integration.
    /// Attach to a GameObject in the scene to enable character creation features.
    /// </summary>
    [AddComponentMenu("Hardwareless/Character/Character Creation Manager")]
    public class MetahumanCharacterCreator : MonoBehaviour
    {
        [Header("Character Setup")]
        [Tooltip("Reference to the CharacterCustomization component controlling the character.")]
        public CharacterCustomization characterCustomization;
        
        [Tooltip("Auto-save character changes to 'Last Used' slot.")]
        public bool autoSaveOnChange = true;
        
        [Tooltip("Delay in seconds before auto-saving after a change.")]
        public float autoSaveDelay = 2f;
        
        [Header("Audio Integration")]
        [Tooltip("Play UI sound when saving character.")]
        public bool playSaveSound = true;
        
        [Tooltip("Play UI sound when loading character.")]
        public bool playLoadSound = true;
        
        [Tooltip("Play UI sound when randomizing character.")]
        public bool playRandomizeSound = true;
        
        [Header("Preset Management")]
        [Tooltip("Current active preset slot (-1 if not using preset).")]
        public int currentPresetSlot = -1;
        
        private float _autoSaveTimer = 0f;
        private bool _pendingAutoSave = false;
        private CharacterData _lastSavedData;
        
        private void Start()
        {
            // Load last used character if available
            if (characterCustomization != null)
            {
                CharacterData lastUsed = CharacterPresetManager.LoadLastUsed();
                characterCustomization.ApplyCustomization(lastUsed, false);
                _lastSavedData = lastUsed.Clone();
            }
        }
        
        private void Update()
        {
            // Handle auto-save timer
            if (_pendingAutoSave && autoSaveOnChange)
            {
                _autoSaveTimer -= Time.deltaTime;
                if (_autoSaveTimer <= 0f)
                {
                    _pendingAutoSave = false;
                    AutoSaveLastUsed();
                }
            }
        }
        
        /// <summary>
        /// Create a new random character.
        /// </summary>
        public void CreateRandomCharacter()
        {
            if (characterCustomization == null)
            {
                Debug.LogWarning("CharacterCustomization reference is missing.");
                return;
            }
            
            CharacterData randomData = CharacterData.CreateRandom();
            characterCustomization.ApplyCustomization(randomData, true);
            
            if (playRandomizeSound)
            {
                AudioSystem.PlayOneShot2D("ui-click");
            }
            
            TriggerAutoSave();
            Debug.Log("Random character created.");
        }
        
        /// <summary>
        /// Reset character to default appearance.
        /// </summary>
        public void ResetCharacter()
        {
            if (characterCustomization == null) return;
            
            characterCustomization.ResetToDefault();
            TriggerAutoSave();
            Debug.Log("Character reset to default.");
        }
        
        /// <summary>
        /// Save current character to a preset slot.
        /// </summary>
        public void SaveToPreset(int slotIndex)
        {
            if (characterCustomization == null)
            {
                Debug.LogWarning("CharacterCustomization reference is missing.");
                return;
            }
            
            CharacterData data = characterCustomization.GetCharacterData();
            if (data != null)
            {
                CharacterPresetManager.SavePreset(data, slotIndex);
                currentPresetSlot = slotIndex;
                _lastSavedData = data.Clone();
                
                if (playSaveSound)
                {
                    AudioSystem.PlayOneShot2D("ui-save");
                }
            }
        }
        
        /// <summary>
        /// Load character from a preset slot.
        /// </summary>
        public void LoadFromPreset(int slotIndex)
        {
            if (characterCustomization == null)
            {
                Debug.LogWarning("CharacterCustomization reference is missing.");
                return;
            }
            
            CharacterData data = CharacterPresetManager.LoadPreset(slotIndex);
            if (data != null)
            {
                characterCustomization.ApplyCustomization(data, true);
                currentPresetSlot = slotIndex;
                _lastSavedData = data.Clone();
                
                if (playLoadSound)
                {
                    AudioSystem.PlayOneShot2D("ui-load");
                }
            }
        }
        
        /// <summary>
        /// Export current character to a JSON file.
        /// </summary>
        public void ExportCharacter(string filePath)
        {
            if (characterCustomization == null) return;
            
            CharacterData data = characterCustomization.GetCharacterData();
            if (data != null)
            {
                CharacterPresetManager.ExportToFile(data, filePath);
                
                if (playSaveSound)
                {
                    AudioSystem.PlayOneShot2D("ui-save");
                }
            }
        }
        
        /// <summary>
        /// Import character from a JSON file.
        /// </summary>
        public void ImportCharacter(string filePath)
        {
            if (characterCustomization == null) return;
            
            CharacterData data = CharacterPresetManager.ImportFromFile(filePath);
            if (data != null)
            {
                characterCustomization.ApplyCustomization(data, true);
                currentPresetSlot = -1;
                
                if (playLoadSound)
                {
                    AudioSystem.PlayOneShot2D("ui-load");
                }
                
                TriggerAutoSave();
            }
        }
        
        /// <summary>
        /// Update a specific character attribute.
        /// </summary>
        public void UpdateAttribute(string attributeName, float value)
        {
            if (characterCustomization == null || characterCustomization.currentCharacter == null)
                return;
            
            var data = characterCustomization.currentCharacter;
            
            // Use reflection to set the field value
            var field = typeof(CharacterData).GetField(attributeName);
            if (field != null && field.FieldType == typeof(float))
            {
                field.SetValue(data, value);
                characterCustomization.ApplyCustomization(data, false);
                TriggerAutoSave();
            }
            else
            {
                Debug.LogWarning($"Attribute '{attributeName}' not found or not a float.");
            }
        }
        
        /// <summary>
        /// Update a color attribute (eyeColor, hairColor, skinTone).
        /// </summary>
        public void UpdateColorAttribute(string attributeName, Color color)
        {
            if (characterCustomization == null || characterCustomization.currentCharacter == null)
                return;
            
            var data = characterCustomization.currentCharacter;
            
            var field = typeof(CharacterData).GetField(attributeName);
            if (field != null && field.FieldType == typeof(Color))
            {
                field.SetValue(data, color);
                characterCustomization.ApplyCustomization(data, false);
                TriggerAutoSave();
            }
            else
            {
                Debug.LogWarning($"Color attribute '{attributeName}' not found.");
            }
        }
        
        /// <summary>
        /// Update an integer attribute (hairStyle, outfitIndex, accessoryIndex).
        /// </summary>
        public void UpdateIntAttribute(string attributeName, int value)
        {
            if (characterCustomization == null || characterCustomization.currentCharacter == null)
                return;
            
            var data = characterCustomization.currentCharacter;
            
            var field = typeof(CharacterData).GetField(attributeName);
            if (field != null && field.FieldType == typeof(int))
            {
                field.SetValue(data, value);
                characterCustomization.ApplyCustomization(data, false);
                TriggerAutoSave();
            }
            else
            {
                Debug.LogWarning($"Integer attribute '{attributeName}' not found.");
            }
        }
        
        /// <summary>
        /// Trigger the auto-save timer.
        /// </summary>
        private void TriggerAutoSave()
        {
            if (autoSaveOnChange)
            {
                _pendingAutoSave = true;
                _autoSaveTimer = autoSaveDelay;
            }
        }
        
        /// <summary>
        /// Auto-save to last used slot.
        /// </summary>
        private void AutoSaveLastUsed()
        {
            if (characterCustomization == null) return;
            
            CharacterData currentData = characterCustomization.GetCharacterData();
            if (currentData != null)
            {
                CharacterPresetManager.SaveLastUsed(currentData);
                _lastSavedData = currentData.Clone();
                Debug.Log("Character auto-saved to 'Last Used' slot.");
            }
        }
        
        /// <summary>
        /// Get array of all preset slots with their info.
        /// </summary>
        public PresetSlotInfo[] GetPresetSlots()
        {
            return CharacterPresetManager.GetPresetSlots();
        }
        
        /// <summary>
        /// Check if current character has unsaved changes.
        /// </summary>
        public bool HasUnsavedChanges()
        {
            if (characterCustomization == null || _lastSavedData == null)
                return false;
            
            CharacterData currentData = characterCustomization.GetCharacterData();
            if (currentData == null)
                return false;
            
            // Compare JSON representations for simplicity
            return currentData.ToJson() != _lastSavedData.ToJson();
        }
        
        private void OnApplicationQuit()
        {
            // Save on quit if auto-save is enabled
            if (autoSaveOnChange && _pendingAutoSave)
            {
                AutoSaveLastUsed();
            }
        }
    }
}
