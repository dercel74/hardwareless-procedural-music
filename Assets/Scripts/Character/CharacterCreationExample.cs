using UnityEngine;
using Hardwareless.Character;

/// <summary>
/// Example script demonstrating how to use the Metahuman Character Creation System.
/// Attach this to a GameObject in your scene to see the system in action.
/// </summary>
public class CharacterCreationExample : MonoBehaviour
{
    [Header("References")]
    [Tooltip("Reference to the character creator manager.")]
    public MetahumanCharacterCreator characterCreator;
    
    [Tooltip("Reference to the character customization component.")]
    public CharacterCustomization characterCustomization;
    
    [Header("Demo Settings")]
    [Tooltip("Auto-create a random character on start.")]
    public bool createRandomOnStart = true;
    
    [Tooltip("Cycle through random characters every X seconds (0 = disabled).")]
    public float autoCycleInterval = 5f;
    
    private float _cycleTimer = 0f;
    
    void Start()
    {
        // Example 1: Create a random character
        if (createRandomOnStart && characterCreator != null)
        {
            characterCreator.CreateRandomCharacter();
            Debug.Log("Created random character on start!");
        }
        
        // Example 2: Create a specific character programmatically
        CreateCustomCharacter();
        
        // Example 3: Load from preset if available
        LoadPresetExample();
    }
    
    void Update()
    {
        // Auto-cycle through random characters
        if (autoCycleInterval > 0f)
        {
            _cycleTimer += Time.deltaTime;
            if (_cycleTimer >= autoCycleInterval)
            {
                _cycleTimer = 0f;
                if (characterCreator != null)
                {
                    characterCreator.CreateRandomCharacter();
                }
            }
        }
        
        // Keyboard shortcuts for testing
        HandleKeyboardInput();
    }
    
    /// <summary>
    /// Create a custom character with specific attributes.
    /// </summary>
    void CreateCustomCharacter()
    {
        if (characterCustomization == null) return;
        
        // Create character data
        CharacterData customChar = new CharacterData();
        customChar.characterName = "Custom Hero";
        
        // Set body attributes
        customChar.height = 0.7f;  // Tall
        customChar.build = 0.6f;   // Athletic
        
        // Set facial features
        customChar.eyeSize = 0.55f;
        customChar.eyeColor = new Color(0.2f, 0.4f, 0.8f); // Blue eyes
        
        // Set appearance
        customChar.hairStyle = 3;
        customChar.hairColor = new Color(0.2f, 0.15f, 0.1f); // Dark brown
        customChar.skinTone = new Color(0.85f, 0.75f, 0.65f); // Light skin
        
        // Apply to character (with smooth transition)
        characterCustomization.ApplyCustomization(customChar, smoothTransition: true);
        
        Debug.Log("Custom character created: " + customChar.characterName);
    }
    
    /// <summary>
    /// Example of loading a preset character.
    /// </summary>
    void LoadPresetExample()
    {
        if (characterCreator == null) return;
        
        // Check if preset slot 0 has a saved character
        if (CharacterPresetManager.HasPreset(0))
        {
            characterCreator.LoadFromPreset(0);
            Debug.Log("Loaded character from preset slot 0");
        }
        else
        {
            Debug.Log("No character saved in preset slot 0");
        }
    }
    
    /// <summary>
    /// Handle keyboard input for testing.
    /// </summary>
    void HandleKeyboardInput()
    {
        if (characterCreator == null) return;
        
        // Press R to randomize
        if (Input.GetKeyDown(KeyCode.R))
        {
            characterCreator.CreateRandomCharacter();
            Debug.Log("Randomized character (R key)");
        }
        
        // Press 1-5 to save to preset slots
        if (Input.GetKeyDown(KeyCode.Alpha1))
        {
            characterCreator.SaveToPreset(0);
            Debug.Log("Saved to preset slot 0 (1 key)");
        }
        if (Input.GetKeyDown(KeyCode.Alpha2))
        {
            characterCreator.SaveToPreset(1);
            Debug.Log("Saved to preset slot 1 (2 key)");
        }
        if (Input.GetKeyDown(KeyCode.Alpha3))
        {
            characterCreator.SaveToPreset(2);
            Debug.Log("Saved to preset slot 2 (3 key)");
        }
        
        // Press L to load from preset slot 0
        if (Input.GetKeyDown(KeyCode.L))
        {
            if (CharacterPresetManager.HasPreset(0))
            {
                characterCreator.LoadFromPreset(0);
                Debug.Log("Loaded from preset slot 0 (L key)");
            }
        }
        
        // Press E to export to file
        if (Input.GetKeyDown(KeyCode.E))
        {
            string filePath = Application.persistentDataPath + "/character_export.json";
            characterCreator.ExportCharacter(filePath);
            Debug.Log($"Exported character to: {filePath}");
        }
        
        // Press I to import from file
        if (Input.GetKeyDown(KeyCode.I))
        {
            string filePath = Application.persistentDataPath + "/character_export.json";
            if (System.IO.File.Exists(filePath))
            {
                characterCreator.ImportCharacter(filePath);
                Debug.Log($"Imported character from: {filePath}");
            }
            else
            {
                Debug.Log("No export file found. Press E to export first.");
            }
        }
    }
    
    /// <summary>
    /// Example: Modify specific attributes at runtime.
    /// </summary>
    public void ModifyCharacterHeight(float newHeight)
    {
        if (characterCreator != null)
        {
            characterCreator.UpdateAttribute("height", newHeight);
        }
    }
    
    /// <summary>
    /// Example: Change eye color at runtime.
    /// </summary>
    public void ChangeEyeColor(Color newColor)
    {
        if (characterCreator != null)
        {
            characterCreator.UpdateColorAttribute("eyeColor", newColor);
        }
    }
    
    /// <summary>
    /// Example: Change hair style at runtime.
    /// </summary>
    public void ChangeHairStyle(int styleIndex)
    {
        if (characterCreator != null)
        {
            characterCreator.UpdateIntAttribute("hairStyle", styleIndex);
        }
    }
}
