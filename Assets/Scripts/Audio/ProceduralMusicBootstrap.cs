using UnityEngine;

namespace Hardwareless.Audio
{
    public static class ProceduralMusicBootstrap
    {
        private const string DisableKey = "HWL_Music_DisableBootstrap";

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Init()
        {
            if (PlayerPrefs.GetInt(DisableKey, 0) != 0)
            {
                return;
            }
            if (Object.FindObjectOfType<ProceduralMusicManager>() != null)
            {
                return;
            }
            var go = new GameObject("ProceduralMusicManager");
            go.AddComponent<ProceduralMusicManager>();
            Object.DontDestroyOnLoad(go);
        }
    }
}
