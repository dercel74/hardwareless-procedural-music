using UnityEngine;

namespace Hardwareless.Audio
{
    /// <summary>
    /// Subscribes to MusicEvents and triggers procedural music reactions (stingers, progression shifts).
    /// Attach alongside a ProceduralMusicManager. Zero config beyond tags for gameplay scripts.
    /// </summary>
    [AddComponentMenu("Hardwareless/Audio/Combat Music Reactive Driver")]
    public class CombatMusicReactiveDriver : MonoBehaviour
    {
        public ProceduralMusicManager musicManager;
        [Tooltip("Enemy kills needed before advancing harmonic progression.")] public int killsPerProgression = 3;
        [Tooltip("Cap on progression advances per session (-1 = unlimited).")]
        public int maxProgressionAdvances = -1;
        [Tooltip("Minimum seconds between damage stingers to avoid spam.")] public float damageStingerCooldown = 1.0f;
        [Tooltip("Health fraction threshold for urgent 'hit' stinger pitch (lower = more aggressive).")]
        [Range(0f,1f)] public float lowHealthThreshold = 0.3f;

        private int _killCount;
        private int _progressionAdvances;
        private float _nextDamageAllowed;

        private void Awake()
        {
            if (musicManager == null) { musicManager = FindObjectOfType<ProceduralMusicManager>(); }
        }

        private void OnEnable()
        {
            MusicEvents.PlayerDamaged += OnPlayerDamaged;
            MusicEvents.EnemyKilled += OnEnemyKilled;
            MusicEvents.ObjectiveCaptured += OnObjectiveCaptured;
        }
        private void OnDisable()
        {
            MusicEvents.PlayerDamaged -= OnPlayerDamaged;
            MusicEvents.EnemyKilled -= OnEnemyKilled;
            MusicEvents.ObjectiveCaptured -= OnObjectiveCaptured;
        }

        private void OnPlayerDamaged(int dmg, float healthFrac)
        {
            if (musicManager == null) { return; }
            if (Time.time < _nextDamageAllowed) { return; }
            _nextDamageAllowed = Time.time + damageStingerCooldown;
            // Choose stinger type based on health severity
            string type = healthFrac < lowHealthThreshold ? "hit" : "rise";
            musicManager.PlayStinger(type);
            // Slight immediate intensity bump (clamped)
            float bump = Mathf.Lerp(0.05f, 0.15f, 1f - healthFrac);
            musicManager.SetIntensitySmooth(Mathf.Clamp01(musicManager.intensity + bump), 0.6f);
        }

        private void OnEnemyKilled(string enemyType)
        {
            if (musicManager == null) { return; }
            _killCount++;
            // Subtle rise stinger every kill
            musicManager.PlayStinger("rise");
            // Progression advance every configured kills
            if (_killCount % Mathf.Max(1, killsPerProgression) == 0)
            {
                if (maxProgressionAdvances < 0 || _progressionAdvances < maxProgressionAdvances)
                {
                    musicManager.AdvanceProgression(1);
                    _progressionAdvances++;
                }
            }
            // Gradually push intensity upward toward action
            float target = Mathf.Clamp01(musicManager.intensity + 0.07f);
            musicManager.SetIntensitySmooth(target, 1.2f);
        }

        private void OnObjectiveCaptured(string objectiveName)
        {
            if (musicManager == null) { return; }
            // Big celebratory stinger & harmonic shift
            musicManager.PlayStinger("rise");
            musicManager.AdvanceProgression(1);
            // Boost intensity more dramatically
            musicManager.SetIntensitySmooth(Mathf.Clamp01(musicManager.intensity + 0.25f), 2f);
        }
    }
}
