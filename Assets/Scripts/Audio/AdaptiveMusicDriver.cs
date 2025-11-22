using UnityEngine;

namespace Hardwareless.Audio
{
    /// <summary>
    /// Polls simple gameplay heuristics (enemy count + player movement) and drives
    /// procedural music intensity smoothly. Designed to work without extra setup:
    /// - Counts GameObjects tagged "Enemy".
    /// - Finds a GameObject tagged "Player" and reads Rigidbody velocity (if present).
    /// Falls back gracefully if tags are missing.
    /// </summary>
    [AddComponentMenu("Hardwareless/Audio/Adaptive Music Driver")]
    [DisallowMultipleComponent]
    public class AdaptiveMusicDriver : MonoBehaviour
    {
        [Tooltip("Target music manager to control; auto-find if null.")] public ProceduralMusicManager musicManager;
        [Tooltip("Seconds between polls (lower = more responsive, higher = cheaper).")]
        public float pollInterval = 0.5f;
        [Tooltip("Enemy count that corresponds to full intensity (1.0).")]
        public int enemyFullIntensityCount = 8;
        [Tooltip("Weight of enemy presence contribution (0-1). Remaining weight used by movement.")]
        [Range(0f,1f)] public float enemyWeight = 0.6f;
        [Tooltip("Smoothing factor for exponential moving average (larger = faster response).")]
        [Range(0.01f,1f)] public float smoothing = 0.15f;
        [Tooltip("Duration in seconds for intensity ramp transitions.")] public float rampSeconds = 1.0f;

        private float _ema; // exponential moving average of raw threat
        private float _nextPoll;
        private Rigidbody _playerRb;

        private void Awake()
        {
            AutoLocateReferences();
        }

        private void AutoLocateReferences()
        {
            if (musicManager == null)
            {
                musicManager = FindObjectOfType<ProceduralMusicManager>();
            }
            var player = GameObject.FindGameObjectWithTag("Player");
            if (player != null)
            {
                _playerRb = player.GetComponent<Rigidbody>();
            }
        }

        private void Update()
        {
            if (musicManager == null) { return; }
            if (Time.time < _nextPoll) { return; }
            _nextPoll = Time.time + pollInterval;

            float enemyContribution = 0f;
            var enemies = GameObject.FindGameObjectsWithTag("Enemy");
            if (enemies != null && enemies.Length > 0 && enemyFullIntensityCount > 0)
            {
                enemyContribution = Mathf.Clamp01(enemies.Length / (float)enemyFullIntensityCount);
            }

            float movementContribution = 0f;
            if (_playerRb != null)
            {
                movementContribution = Mathf.Clamp01(_playerRb.velocity.magnitude / 5f); // assume 5 m/s ~ full
            }

            float rawThreat = enemyContribution * enemyWeight + movementContribution * Mathf.Clamp01(1f - enemyWeight);

            // Exponential moving average smoothing
            float alpha = 1f - Mathf.Exp(-smoothing);
            _ema = Mathf.Lerp(_ema, rawThreat, alpha);

            musicManager.SetIntensitySmooth(_ema, rampSeconds);
        }
    }
}
