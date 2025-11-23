using UnityEngine;

namespace Hardwareless.Character
{
    /// <summary>
    /// Manages character preset saving and loading using PlayerPrefs and JSON.
    /// Similar to the procedural music preset system, provides slots for quick save/load.
    /// </summary>
    public static class CharacterPresetManager
    {
        private const string PRESET_KEY_PREFIX = "CharacterPreset_";
        private const string LAST_USED_KEY = "Character_LastUsed";
        private const int MAX_PRESET_SLOTS = 10;
        
        /// <summary>
        /// Save character data to a preset slot (0-9).
        /// </summary>
        public static void SavePreset(CharacterData data, int slotIndex)
        {
            if (data == null)
            {
                Debug.LogWarning("Cannot save null character data.");
                return;
            }
            
            if (slotIndex < 0 || slotIndex >= MAX_PRESET_SLOTS)
            {
                Debug.LogWarning($"Invalid preset slot: {slotIndex}. Must be 0-{MAX_PRESET_SLOTS - 1}.");
                return;
            }
            
            string key = PRESET_KEY_PREFIX + slotIndex;
            string json = data.ToJson();
            PlayerPrefs.SetString(key, json);
            PlayerPrefs.Save();
            
            Debug.Log($"Character preset saved to slot {slotIndex}: {data.characterName}");
        }
        
        /// <summary>
        /// Load character data from a preset slot (0-9).
        /// Returns null if slot is empty or invalid.
        /// </summary>
        public static CharacterData LoadPreset(int slotIndex)
        {
            if (slotIndex < 0 || slotIndex >= MAX_PRESET_SLOTS)
            {
                Debug.LogWarning($"Invalid preset slot: {slotIndex}. Must be 0-{MAX_PRESET_SLOTS - 1}.");
                return null;
            }
            
            string key = PRESET_KEY_PREFIX + slotIndex;
            if (!PlayerPrefs.HasKey(key))
            {
                Debug.Log($"Preset slot {slotIndex} is empty.");
                return null;
            }
            
            string json = PlayerPrefs.GetString(key);
            try
            {
                CharacterData data = CharacterData.FromJson(json);
                Debug.Log($"Character preset loaded from slot {slotIndex}: {data.characterName}");
                return data;
            }
            catch (System.Exception e)
            {
                Debug.LogError($"Failed to load preset from slot {slotIndex}: {e.Message}");
                return null;
            }
        }
        
        /// <summary>
        /// Check if a preset slot has saved data.
        /// </summary>
        public static bool HasPreset(int slotIndex)
        {
            if (slotIndex < 0 || slotIndex >= MAX_PRESET_SLOTS)
                return false;
            
            string key = PRESET_KEY_PREFIX + slotIndex;
            return PlayerPrefs.HasKey(key);
        }
        
        /// <summary>
        /// Delete a character preset from a slot.
        /// </summary>
        public static void DeletePreset(int slotIndex)
        {
            if (slotIndex < 0 || slotIndex >= MAX_PRESET_SLOTS)
            {
                Debug.LogWarning($"Invalid preset slot: {slotIndex}.");
                return;
            }
            
            string key = PRESET_KEY_PREFIX + slotIndex;
            if (PlayerPrefs.HasKey(key))
            {
                PlayerPrefs.DeleteKey(key);
                PlayerPrefs.Save();
                Debug.Log($"Deleted character preset from slot {slotIndex}.");
            }
        }
        
        /// <summary>
        /// Clear all character presets.
        /// </summary>
        public static void ClearAllPresets()
        {
            for (int i = 0; i < MAX_PRESET_SLOTS; i++)
            {
                string key = PRESET_KEY_PREFIX + i;
                if (PlayerPrefs.HasKey(key))
                {
                    PlayerPrefs.DeleteKey(key);
                }
            }
            PlayerPrefs.Save();
            Debug.Log("All character presets cleared.");
        }
        
        /// <summary>
        /// Save the last used character data.
        /// </summary>
        public static void SaveLastUsed(CharacterData data)
        {
            if (data == null) return;
            
            string json = data.ToJson();
            PlayerPrefs.SetString(LAST_USED_KEY, json);
            PlayerPrefs.Save();
        }
        
        /// <summary>
        /// Load the last used character data.
        /// Returns a default character if no last used data exists.
        /// </summary>
        public static CharacterData LoadLastUsed()
        {
            if (!PlayerPrefs.HasKey(LAST_USED_KEY))
            {
                return CharacterData.CreateDefault();
            }
            
            string json = PlayerPrefs.GetString(LAST_USED_KEY);
            try
            {
                return CharacterData.FromJson(json);
            }
            catch (System.Exception e)
            {
                Debug.LogError($"Failed to load last used character: {e.Message}");
                return CharacterData.CreateDefault();
            }
        }
        
        /// <summary>
        /// Export character data to a JSON file.
        /// </summary>
        public static void ExportToFile(CharacterData data, string filePath)
        {
            if (data == null)
            {
                Debug.LogWarning("Cannot export null character data.");
                return;
            }
            
            try
            {
                string json = data.ToJson();
                System.IO.File.WriteAllText(filePath, json);
                Debug.Log($"Character exported to: {filePath}");
            }
            catch (System.Exception e)
            {
                Debug.LogError($"Failed to export character: {e.Message}");
            }
        }
        
        /// <summary>
        /// Import character data from a JSON file.
        /// </summary>
        public static CharacterData ImportFromFile(string filePath)
        {
            if (!System.IO.File.Exists(filePath))
            {
                Debug.LogWarning($"File not found: {filePath}");
                return null;
            }
            
            try
            {
                string json = System.IO.File.ReadAllText(filePath);
                CharacterData data = CharacterData.FromJson(json);
                Debug.Log($"Character imported from: {filePath}");
                return data;
            }
            catch (System.Exception e)
            {
                Debug.LogError($"Failed to import character: {e.Message}");
                return null;
            }
        }
        
        /// <summary>
        /// Get an array of preset slot information (name and whether slot is occupied).
        /// </summary>
        public static PresetSlotInfo[] GetPresetSlots()
        {
            PresetSlotInfo[] slots = new PresetSlotInfo[MAX_PRESET_SLOTS];
            
            for (int i = 0; i < MAX_PRESET_SLOTS; i++)
            {
                slots[i] = new PresetSlotInfo
                {
                    slotIndex = i,
                    isOccupied = HasPreset(i),
                    characterName = ""
                };
                
                if (slots[i].isOccupied)
                {
                    CharacterData data = LoadPreset(i);
                    if (data != null)
                    {
                        slots[i].characterName = data.characterName;
                    }
                }
            }
            
            return slots;
        }
    }
    
    /// <summary>
    /// Information about a character preset slot.
    /// </summary>
    [System.Serializable]
    public struct PresetSlotInfo
    {
        public int slotIndex;
        public bool isOccupied;
        public string characterName;
    }
}
