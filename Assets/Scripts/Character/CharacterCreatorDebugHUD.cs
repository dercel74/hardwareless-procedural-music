using UnityEngine;

namespace Hardwareless.Character
{
    /// <summary>
    /// Debug HUD for Metahuman character creation system.
    /// Press F10 to toggle the character creator debug interface.
    /// Provides runtime controls for character customization and preset management.
    /// </summary>
    [AddComponentMenu("Hardwareless/Character/Character Creator Debug HUD")]
    public class CharacterCreatorDebugHUD : MonoBehaviour
    {
        [Tooltip("Reference to the MetahumanCharacterCreator.")]
        public MetahumanCharacterCreator characterCreator;
        
        [Tooltip("Key to toggle the HUD.")]
        public KeyCode toggleKey = KeyCode.F10;
        
        [Tooltip("Show HUD on startup.")]
        public bool showOnStart = false;
        
        private bool _showHUD = false;
        private Vector2 _scrollPosition = Vector2.zero;
        private int _selectedTab = 0;
        private string[] _tabNames = { "Body", "Face", "Colors", "Presets" };
        
        // Cached character data reference for faster access
        private CharacterData _currentData;
        
        private void Start()
        {
            _showHUD = showOnStart;
        }
        
        private void Update()
        {
            if (Input.GetKeyDown(toggleKey))
            {
                _showHUD = !_showHUD;
            }
            
            // Cache current character data
            if (characterCreator != null && characterCreator.characterCustomization != null)
            {
                _currentData = characterCreator.characterCustomization.currentCharacter;
            }
        }
        
        private void OnGUI()
        {
            if (!_showHUD) return;
            
            GUILayout.BeginArea(new Rect(10, 10, 500, Screen.height - 20));
            GUILayout.BeginVertical("box");
            
            // Header
            GUILayout.Label("METAHUMAN CHARACTER CREATOR", GUI.skin.box);
            GUILayout.Label($"Press {toggleKey} to close", GUI.skin.label);
            
            if (characterCreator == null)
            {
                GUILayout.Label("ERROR: MetahumanCharacterCreator reference missing!", GUI.skin.box);
                GUILayout.EndVertical();
                GUILayout.EndArea();
                return;
            }
            
            if (_currentData == null)
            {
                GUILayout.Label("No character data available.", GUI.skin.box);
                GUILayout.EndVertical();
                GUILayout.EndArea();
                return;
            }
            
            // Character name
            GUILayout.BeginHorizontal();
            GUILayout.Label("Character Name:", GUILayout.Width(120));
            _currentData.characterName = GUILayout.TextField(_currentData.characterName, GUILayout.Width(200));
            GUILayout.EndHorizontal();
            
            GUILayout.Space(10);
            
            // Quick actions
            GUILayout.BeginHorizontal();
            if (GUILayout.Button("Randomize", GUILayout.Height(30)))
            {
                characterCreator.CreateRandomCharacter();
            }
            if (GUILayout.Button("Reset", GUILayout.Height(30)))
            {
                characterCreator.ResetCharacter();
            }
            GUILayout.EndHorizontal();
            
            GUILayout.Space(10);
            
            // Tab selection
            _selectedTab = GUILayout.Toolbar(_selectedTab, _tabNames);
            
            GUILayout.Space(5);
            
            // Scrollable content area
            _scrollPosition = GUILayout.BeginScrollView(_scrollPosition, GUILayout.Height(Screen.height - 250));
            
            switch (_selectedTab)
            {
                case 0:
                    DrawBodyTab();
                    break;
                case 1:
                    DrawFaceTab();
                    break;
                case 2:
                    DrawColorsTab();
                    break;
                case 3:
                    DrawPresetsTab();
                    break;
            }
            
            GUILayout.EndScrollView();
            
            GUILayout.EndVertical();
            GUILayout.EndArea();
        }
        
        private void DrawBodyTab()
        {
            GUILayout.Label("BODY CUSTOMIZATION", GUI.skin.box);
            
            _currentData.height = DrawSlider("Height", _currentData.height);
            _currentData.build = DrawSlider("Build", _currentData.build);
            _currentData.bodyWeight = DrawSlider("Body Weight", _currentData.bodyWeight);
            
            GUILayout.Space(10);
            GUILayout.Label("HEAD", GUI.skin.box);
            _currentData.headSize = DrawSlider("Head Size", _currentData.headSize);
            _currentData.faceWidth = DrawSlider("Face Width", _currentData.faceWidth);
            
            ApplyChanges();
        }
        
        private void DrawFaceTab()
        {
            GUILayout.Label("FACIAL FEATURES", GUI.skin.box);
            
            GUILayout.Label("EYES", GUI.skin.box);
            _currentData.eyeSize = DrawSlider("Eye Size", _currentData.eyeSize);
            _currentData.eyeDistance = DrawSlider("Eye Distance", _currentData.eyeDistance);
            _currentData.eyeHeight = DrawSlider("Eye Height", _currentData.eyeHeight);
            
            GUILayout.Space(10);
            GUILayout.Label("NOSE", GUI.skin.box);
            _currentData.noseSize = DrawSlider("Nose Size", _currentData.noseSize);
            _currentData.noseWidth = DrawSlider("Nose Width", _currentData.noseWidth);
            _currentData.noseBridge = DrawSlider("Nose Bridge", _currentData.noseBridge);
            
            GUILayout.Space(10);
            GUILayout.Label("MOUTH", GUI.skin.box);
            _currentData.mouthSize = DrawSlider("Mouth Size", _currentData.mouthSize);
            _currentData.lipThickness = DrawSlider("Lip Thickness", _currentData.lipThickness);
            _currentData.mouthWidth = DrawSlider("Mouth Width", _currentData.mouthWidth);
            
            GUILayout.Space(10);
            GUILayout.Label("EARS & JAW", GUI.skin.box);
            _currentData.earSize = DrawSlider("Ear Size", _currentData.earSize);
            _currentData.earRotation = DrawSlider("Ear Rotation", _currentData.earRotation);
            _currentData.jawWidth = DrawSlider("Jaw Width", _currentData.jawWidth);
            _currentData.chinSize = DrawSlider("Chin Size", _currentData.chinSize);
            _currentData.cheekboneHeight = DrawSlider("Cheekbone Height", _currentData.cheekboneHeight);
            
            ApplyChanges();
        }
        
        private void DrawColorsTab()
        {
            GUILayout.Label("COLORS & APPEARANCE", GUI.skin.box);
            
            GUILayout.Label("SKIN TONE", GUI.skin.box);
            _currentData.skinTone = DrawColorPicker("Skin Tone", _currentData.skinTone);
            
            GUILayout.Space(10);
            GUILayout.Label("EYES", GUI.skin.box);
            _currentData.eyeColor = DrawColorPicker("Eye Color", _currentData.eyeColor);
            
            GUILayout.Space(10);
            GUILayout.Label("HAIR", GUI.skin.box);
            _currentData.hairColor = DrawColorPicker("Hair Color", _currentData.hairColor);
            
            GUILayout.BeginHorizontal();
            GUILayout.Label("Hair Style:", GUILayout.Width(120));
            _currentData.hairStyle = (int)GUILayout.HorizontalSlider(_currentData.hairStyle, 0, 10, GUILayout.Width(200));
            GUILayout.Label(_currentData.hairStyle.ToString(), GUILayout.Width(50));
            GUILayout.EndHorizontal();
            
            GUILayout.Space(10);
            GUILayout.Label("OUTFIT", GUI.skin.box);
            GUILayout.BeginHorizontal();
            GUILayout.Label("Outfit Index:", GUILayout.Width(120));
            _currentData.outfitIndex = (int)GUILayout.HorizontalSlider(_currentData.outfitIndex, 0, 10, GUILayout.Width(200));
            GUILayout.Label(_currentData.outfitIndex.ToString(), GUILayout.Width(50));
            GUILayout.EndHorizontal();
            
            GUILayout.BeginHorizontal();
            GUILayout.Label("Accessory Index:", GUILayout.Width(120));
            _currentData.accessoryIndex = (int)GUILayout.HorizontalSlider(_currentData.accessoryIndex, 0, 10, GUILayout.Width(200));
            GUILayout.Label(_currentData.accessoryIndex.ToString(), GUILayout.Width(50));
            GUILayout.EndHorizontal();
            
            ApplyChanges();
        }
        
        private void DrawPresetsTab()
        {
            GUILayout.Label("PRESET MANAGEMENT", GUI.skin.box);
            
            if (characterCreator.HasUnsavedChanges())
            {
                GUILayout.Label("⚠ UNSAVED CHANGES", GUI.skin.box);
            }
            
            GUILayout.Space(5);
            
            var slots = characterCreator.GetPresetSlots();
            
            for (int i = 0; i < slots.Length; i++)
            {
                GUILayout.BeginHorizontal("box");
                
                string slotLabel = $"Slot {i}";
                if (i == characterCreator.currentPresetSlot)
                {
                    slotLabel += " (Active)";
                }
                
                GUILayout.Label(slotLabel, GUILayout.Width(80));
                
                if (slots[i].isOccupied)
                {
                    GUILayout.Label(slots[i].characterName, GUILayout.Width(150));
                    
                    if (GUILayout.Button("Load", GUILayout.Width(60)))
                    {
                        characterCreator.LoadFromPreset(i);
                    }
                    
                    if (GUILayout.Button("Overwrite", GUILayout.Width(80)))
                    {
                        characterCreator.SaveToPreset(i);
                    }
                    
                    if (GUILayout.Button("Delete", GUILayout.Width(60)))
                    {
                        CharacterPresetManager.DeletePreset(i);
                    }
                }
                else
                {
                    GUILayout.Label("(Empty)", GUILayout.Width(150));
                    
                    if (GUILayout.Button("Save", GUILayout.Width(60)))
                    {
                        characterCreator.SaveToPreset(i);
                    }
                }
                
                GUILayout.EndHorizontal();
            }
            
            GUILayout.Space(10);
            
            if (GUILayout.Button("Clear All Presets", GUILayout.Height(30)))
            {
                // Simple confirmation - click twice to confirm
                CharacterPresetManager.ClearAllPresets();
            }
        }
        
        private float DrawSlider(string label, float value)
        {
            GUILayout.BeginHorizontal();
            GUILayout.Label(label, GUILayout.Width(120));
            float newValue = GUILayout.HorizontalSlider(value, 0f, 1f, GUILayout.Width(200));
            GUILayout.Label(newValue.ToString("F2"), GUILayout.Width(50));
            GUILayout.EndHorizontal();
            return newValue;
        }
        
        private Color DrawColorPicker(string label, Color color)
        {
            GUILayout.BeginHorizontal();
            GUILayout.Label(label, GUILayout.Width(120));
            
            // Simple RGB sliders for color picking
            GUILayout.BeginVertical();
            
            GUILayout.BeginHorizontal();
            GUILayout.Label("R:", GUILayout.Width(20));
            float r = GUILayout.HorizontalSlider(color.r, 0f, 1f, GUILayout.Width(150));
            GUILayout.EndHorizontal();
            
            GUILayout.BeginHorizontal();
            GUILayout.Label("G:", GUILayout.Width(20));
            float g = GUILayout.HorizontalSlider(color.g, 0f, 1f, GUILayout.Width(150));
            GUILayout.EndHorizontal();
            
            GUILayout.BeginHorizontal();
            GUILayout.Label("B:", GUILayout.Width(20));
            float b = GUILayout.HorizontalSlider(color.b, 0f, 1f, GUILayout.Width(150));
            GUILayout.EndHorizontal();
            
            GUILayout.EndVertical();
            
            GUILayout.EndHorizontal();
            
            return new Color(r, g, b, color.a);
        }
        
        private void ApplyChanges()
        {
            if (characterCreator != null && characterCreator.characterCustomization != null && _currentData != null)
            {
                characterCreator.characterCustomization.ApplyCustomization(_currentData, false);
            }
        }
    }
}
