using System;
using UnityEngine;

namespace Hardwareless.Audio
{
    /// <summary>
    /// Central lightweight static event hub for reactive music cues.
    /// Gameplay systems call the static Invoke* methods; music drivers subscribe.
    /// </summary>
    public static class MusicEvents
    {
        /// <summary>Fired when player takes damage (int = damage amount, float = health fraction after damage).</summary>
        public static event Action<int, float> PlayerDamaged;
        /// <summary>Fired when an enemy is killed (string = enemy type id or null).</summary>
        public static event Action<string> EnemyKilled;
        /// <summary>Fired when an objective is captured/secured (string = objective name).</summary>
        public static event Action<string> ObjectiveCaptured;

        public static void InvokePlayerDamaged(int damage, float healthFraction)
        {
            try { PlayerDamaged?.Invoke(damage, Mathf.Clamp01(healthFraction)); } catch (Exception ex) { Debug.LogError("MusicEvents PlayerDamaged handler threw: " + ex); }
        }
        public static void InvokeEnemyKilled(string enemyType = null)
        {
            try { EnemyKilled?.Invoke(enemyType); } catch (Exception ex) { Debug.LogError("MusicEvents EnemyKilled handler threw: " + ex); }
        }
        public static void InvokeObjectiveCaptured(string objectiveName = null)
        {
            try { ObjectiveCaptured?.Invoke(objectiveName); } catch (Exception ex) { Debug.LogError("MusicEvents ObjectiveCaptured handler threw: " + ex); }
        }
    }
}
