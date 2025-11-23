using UnityEngine;

namespace Hardwareless.Character
{
    /// <summary>
    /// Manages character customization and appearance.
    /// Attach to a GameObject with a character model to apply customization.
    /// Supports runtime customization and morphing between character presets.
    /// </summary>
    [AddComponentMenu("Hardwareless/Character/Character Customization")]
    public class CharacterCustomization : MonoBehaviour
    {
        [Tooltip("Current character data defining appearance.")]
        public CharacterData currentCharacter;
        
        [Tooltip("Apply customization automatically on Start.")]
        public bool applyOnStart = true;
        
        [Tooltip("Enable smooth transitions when changing attributes.")]
        public bool enableSmoothTransitions = true;
        
        [Tooltip("Transition duration in seconds.")]
        public float transitionDuration = 0.5f;
        
        [Header("Visual Components")]
        [Tooltip("Root transform of the character body (for scaling).")]
        public Transform bodyRoot;
        
        [Tooltip("Head bone transform.")]
        public Transform headBone;
        
        [Tooltip("Renderer for skin material.")]
        public Renderer skinRenderer;
        
        [Tooltip("Renderer for hair material.")]
        public Renderer hairRenderer;
        
        [Tooltip("Renderer for eyes material.")]
        public Renderer eyesRenderer;
        
        [Tooltip("Hair style GameObjects (enabled based on hairStyle index).")]
        public GameObject[] hairStyles;
        
        [Tooltip("Outfit GameObjects (enabled based on outfitIndex).")]
        public GameObject[] outfits;
        
        private CharacterData _targetCharacter;
        private float _transitionProgress = 1f;
        
        // Cache material instances to avoid memory leaks
        private Material _skinMaterialInstance;
        private Material _hairMaterialInstance;
        private Material _eyesMaterialInstance;
        private bool _materialsInitialized = false;
        
        private void Start()
        {
            if (currentCharacter == null)
            {
                currentCharacter = CharacterData.CreateDefault();
            }
            
            if (applyOnStart)
            {
                ApplyCustomization(currentCharacter, false);
            }
        }
        
        private void Update()
        {
            if (_transitionProgress < 1f && enableSmoothTransitions)
            {
                _transitionProgress += Time.deltaTime / transitionDuration;
                _transitionProgress = Mathf.Clamp01(_transitionProgress);
                
                // Interpolate between current and target
                if (_targetCharacter != null && currentCharacter != null)
                {
                    LerpCharacterData(currentCharacter, _targetCharacter, _transitionProgress);
                    ApplyVisualsImmediate();
                }
            }
        }
        
        /// <summary>
        /// Apply character customization with optional smooth transition.
        /// </summary>
        public void ApplyCustomization(CharacterData data, bool smooth = true)
        {
            if (data == null) return;
            
            if (smooth && enableSmoothTransitions)
            {
                _targetCharacter = data.Clone();
                _transitionProgress = 0f;
            }
            else
            {
                currentCharacter = data.Clone();
                ApplyVisualsImmediate();
            }
        }
        
        /// <summary>
        /// Apply visual changes immediately based on current character data.
        /// </summary>
        private void ApplyVisualsImmediate()
        {
            if (currentCharacter == null) return;
            
            // Initialize material instances on first use
            if (!_materialsInitialized)
            {
                InitializeMaterialInstances();
                _materialsInitialized = true;
            }
            
            // Apply body scaling
            if (bodyRoot != null)
            {
                float heightScale = Mathf.Lerp(0.8f, 1.2f, currentCharacter.height);
                float widthScale = Mathf.Lerp(0.85f, 1.15f, currentCharacter.build);
                bodyRoot.localScale = new Vector3(widthScale, heightScale, widthScale);
            }
            
            // Apply head scaling
            if (headBone != null)
            {
                float headScale = Mathf.Lerp(0.9f, 1.1f, currentCharacter.headSize);
                float faceWidth = Mathf.Lerp(0.95f, 1.05f, currentCharacter.faceWidth);
                headBone.localScale = new Vector3(faceWidth, headScale, headScale);
            }
            
            // Apply skin tone
            if (_skinMaterialInstance != null && _skinMaterialInstance.HasProperty("_Color"))
            {
                _skinMaterialInstance.color = currentCharacter.skinTone;
            }
            
            // Apply hair color
            if (_hairMaterialInstance != null && _hairMaterialInstance.HasProperty("_Color"))
            {
                _hairMaterialInstance.color = currentCharacter.hairColor;
            }
            
            // Apply eye color
            if (_eyesMaterialInstance != null && _eyesMaterialInstance.HasProperty("_Color"))
            {
                _eyesMaterialInstance.color = currentCharacter.eyeColor;
            }
            
            // Apply hair style
            if (hairStyles != null && hairStyles.Length > 0)
            {
                for (int i = 0; i < hairStyles.Length; i++)
                {
                    if (hairStyles[i] != null)
                    {
                        hairStyles[i].SetActive(i == currentCharacter.hairStyle);
                    }
                }
            }
            
            // Apply outfit
            if (outfits != null && outfits.Length > 0)
            {
                for (int i = 0; i < outfits.Length; i++)
                {
                    if (outfits[i] != null)
                    {
                        outfits[i].SetActive(i == currentCharacter.outfitIndex);
                    }
                }
            }
        }
        
        /// <summary>
        /// Linearly interpolate character data for smooth transitions.
        /// </summary>
        private void LerpCharacterData(CharacterData current, CharacterData target, float t)
        {
            // Body
            current.height = Mathf.Lerp(current.height, target.height, t);
            current.build = Mathf.Lerp(current.build, target.build, t);
            current.bodyWeight = Mathf.Lerp(current.bodyWeight, target.bodyWeight, t);
            
            // Head & Face
            current.headSize = Mathf.Lerp(current.headSize, target.headSize, t);
            current.faceWidth = Mathf.Lerp(current.faceWidth, target.faceWidth, t);
            current.jawWidth = Mathf.Lerp(current.jawWidth, target.jawWidth, t);
            current.chinSize = Mathf.Lerp(current.chinSize, target.chinSize, t);
            current.cheekboneHeight = Mathf.Lerp(current.cheekboneHeight, target.cheekboneHeight, t);
            
            // Eyes
            current.eyeSize = Mathf.Lerp(current.eyeSize, target.eyeSize, t);
            current.eyeDistance = Mathf.Lerp(current.eyeDistance, target.eyeDistance, t);
            current.eyeHeight = Mathf.Lerp(current.eyeHeight, target.eyeHeight, t);
            current.eyeColor = Color.Lerp(current.eyeColor, target.eyeColor, t);
            
            // Nose
            current.noseSize = Mathf.Lerp(current.noseSize, target.noseSize, t);
            current.noseWidth = Mathf.Lerp(current.noseWidth, target.noseWidth, t);
            current.noseBridge = Mathf.Lerp(current.noseBridge, target.noseBridge, t);
            
            // Mouth
            current.mouthSize = Mathf.Lerp(current.mouthSize, target.mouthSize, t);
            current.lipThickness = Mathf.Lerp(current.lipThickness, target.lipThickness, t);
            current.mouthWidth = Mathf.Lerp(current.mouthWidth, target.mouthWidth, t);
            
            // Ears
            current.earSize = Mathf.Lerp(current.earSize, target.earSize, t);
            current.earRotation = Mathf.Lerp(current.earRotation, target.earRotation, t);
            
            // Appearance
            current.hairColor = Color.Lerp(current.hairColor, target.hairColor, t);
            current.skinTone = Color.Lerp(current.skinTone, target.skinTone, t);
            
            // Discrete values (switch at midpoint)
            if (t >= 0.5f)
            {
                current.hairStyle = target.hairStyle;
                current.outfitIndex = target.outfitIndex;
                current.accessoryIndex = target.accessoryIndex;
            }
        }
        
        /// <summary>
        /// Randomize the current character's appearance.
        /// </summary>
        public void RandomizeCharacter()
        {
            ApplyCustomization(CharacterData.CreateRandom(), enableSmoothTransitions);
        }
        
        /// <summary>
        /// Reset to default character appearance.
        /// </summary>
        public void ResetToDefault()
        {
            ApplyCustomization(CharacterData.CreateDefault(), enableSmoothTransitions);
        }
        
        /// <summary>
        /// Get a copy of the current character data.
        /// </summary>
        public CharacterData GetCharacterData()
        {
            return currentCharacter?.Clone();
        }
        
        /// <summary>
        /// Initialize material instances to avoid memory leaks from accessing .material property.
        /// </summary>
        private void InitializeMaterialInstances()
        {
            if (skinRenderer != null && skinRenderer.sharedMaterial != null)
            {
                _skinMaterialInstance = new Material(skinRenderer.sharedMaterial);
                skinRenderer.material = _skinMaterialInstance;
            }
            
            if (hairRenderer != null && hairRenderer.sharedMaterial != null)
            {
                _hairMaterialInstance = new Material(hairRenderer.sharedMaterial);
                hairRenderer.material = _hairMaterialInstance;
            }
            
            if (eyesRenderer != null && eyesRenderer.sharedMaterial != null)
            {
                _eyesMaterialInstance = new Material(eyesRenderer.sharedMaterial);
                eyesRenderer.material = _eyesMaterialInstance;
            }
        }
        
        private void OnDestroy()
        {
            // Clean up material instances
            if (_skinMaterialInstance != null)
            {
                Destroy(_skinMaterialInstance);
            }
            if (_hairMaterialInstance != null)
            {
                Destroy(_hairMaterialInstance);
            }
            if (_eyesMaterialInstance != null)
            {
                Destroy(_eyesMaterialInstance);
            }
        }
    }
}
