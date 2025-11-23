using UnityEngine;
using System;

namespace Hardwareless.Character
{
    /// <summary>
    /// Serializable data structure for storing Metahuman character attributes.
    /// Includes appearance customization, body proportions, and facial features.
    /// </summary>
    [Serializable]
    public class CharacterData
    {
        [Header("Basic Information")]
        public string characterName = "New Character";
        public string characterID = "";
        
        [Header("Body Customization")]
        [Range(0f, 1f)] public float height = 0.5f;
        [Range(0f, 1f)] public float build = 0.5f; // 0 = slim, 1 = muscular
        [Range(0f, 1f)] public float bodyWeight = 0.5f;
        
        [Header("Head & Face")]
        [Range(0f, 1f)] public float headSize = 0.5f;
        [Range(0f, 1f)] public float faceWidth = 0.5f;
        [Range(0f, 1f)] public float jawWidth = 0.5f;
        [Range(0f, 1f)] public float chinSize = 0.5f;
        [Range(0f, 1f)] public float cheekboneHeight = 0.5f;
        
        [Header("Eyes")]
        [Range(0f, 1f)] public float eyeSize = 0.5f;
        [Range(0f, 1f)] public float eyeDistance = 0.5f;
        [Range(0f, 1f)] public float eyeHeight = 0.5f;
        public Color eyeColor = Color.blue;
        
        [Header("Nose")]
        [Range(0f, 1f)] public float noseSize = 0.5f;
        [Range(0f, 1f)] public float noseWidth = 0.5f;
        [Range(0f, 1f)] public float noseBridge = 0.5f;
        
        [Header("Mouth")]
        [Range(0f, 1f)] public float mouthSize = 0.5f;
        [Range(0f, 1f)] public float lipThickness = 0.5f;
        [Range(0f, 1f)] public float mouthWidth = 0.5f;
        
        [Header("Ears")]
        [Range(0f, 1f)] public float earSize = 0.5f;
        [Range(0f, 1f)] public float earRotation = 0.5f;
        
        [Header("Hair & Skin")]
        public int hairStyle = 0; // Index into available hairstyles
        public Color hairColor = new Color(0.3f, 0.2f, 0.1f);
        public Color skinTone = new Color(0.9f, 0.8f, 0.7f);
        
        [Header("Clothing & Accessories")]
        public int outfitIndex = 0;
        public int accessoryIndex = 0;
        
        /// <summary>
        /// Creates a new character with random attributes.
        /// </summary>
        public static CharacterData CreateRandom()
        {
            var data = new CharacterData();
            data.characterID = System.Guid.NewGuid().ToString();
            data.characterName = "Random Character";
            
            // Randomize body
            data.height = UnityEngine.Random.Range(0.3f, 0.7f);
            data.build = UnityEngine.Random.Range(0.2f, 0.8f);
            data.bodyWeight = UnityEngine.Random.Range(0.3f, 0.7f);
            
            // Randomize face
            data.headSize = UnityEngine.Random.Range(0.4f, 0.6f);
            data.faceWidth = UnityEngine.Random.Range(0.3f, 0.7f);
            data.jawWidth = UnityEngine.Random.Range(0.3f, 0.7f);
            data.chinSize = UnityEngine.Random.Range(0.3f, 0.7f);
            data.cheekboneHeight = UnityEngine.Random.Range(0.3f, 0.7f);
            
            // Randomize eyes
            data.eyeSize = UnityEngine.Random.Range(0.4f, 0.6f);
            data.eyeDistance = UnityEngine.Random.Range(0.4f, 0.6f);
            data.eyeHeight = UnityEngine.Random.Range(0.4f, 0.6f);
            data.eyeColor = new Color(
                UnityEngine.Random.value,
                UnityEngine.Random.value,
                UnityEngine.Random.value
            );
            
            // Randomize nose
            data.noseSize = UnityEngine.Random.Range(0.3f, 0.7f);
            data.noseWidth = UnityEngine.Random.Range(0.3f, 0.7f);
            data.noseBridge = UnityEngine.Random.Range(0.3f, 0.7f);
            
            // Randomize mouth
            data.mouthSize = UnityEngine.Random.Range(0.4f, 0.6f);
            data.lipThickness = UnityEngine.Random.Range(0.3f, 0.7f);
            data.mouthWidth = UnityEngine.Random.Range(0.4f, 0.6f);
            
            // Randomize ears
            data.earSize = UnityEngine.Random.Range(0.4f, 0.6f);
            data.earRotation = UnityEngine.Random.Range(0.4f, 0.6f);
            
            // Randomize appearance
            data.hairStyle = UnityEngine.Random.Range(0, 10);
            data.hairColor = new Color(
                UnityEngine.Random.Range(0.1f, 0.9f),
                UnityEngine.Random.Range(0.1f, 0.5f),
                UnityEngine.Random.Range(0.05f, 0.3f)
            );
            data.skinTone = new Color(
                UnityEngine.Random.Range(0.6f, 1.0f),
                UnityEngine.Random.Range(0.5f, 0.9f),
                UnityEngine.Random.Range(0.4f, 0.8f)
            );
            
            return data;
        }
        
        /// <summary>
        /// Creates a default character with neutral/average attributes.
        /// </summary>
        public static CharacterData CreateDefault()
        {
            var data = new CharacterData();
            data.characterID = System.Guid.NewGuid().ToString();
            data.characterName = "Default Character";
            // All values already default to 0.5f
            return data;
        }
        
        /// <summary>
        /// Clones this character data.
        /// </summary>
        public CharacterData Clone()
        {
            return (CharacterData)this.MemberwiseClone();
        }
        
        /// <summary>
        /// Serializes character data to JSON string.
        /// </summary>
        public string ToJson()
        {
            return JsonUtility.ToJson(this, true);
        }
        
        /// <summary>
        /// Deserializes character data from JSON string.
        /// </summary>
        public static CharacterData FromJson(string json)
        {
            return JsonUtility.FromJson<CharacterData>(json);
        }
    }
}
