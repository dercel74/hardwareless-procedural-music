using UnityEngine;
using Hardwareless.Audio;

namespace Hardwareless.Audio
{
    [RequireComponent(typeof(CharacterController))]
    public class FootstepSfx : MonoBehaviour
    {
        public string stepKey = "footstep-default";
        [Tooltip("Meters/second threshold before footsteps play")]
        public float minSpeed = 0.2f;
        [Tooltip("Base interval in seconds between steps at walking speed")]
        public float baseInterval = 0.55f;
        [Tooltip("Interval at running speed (~6 m/s)")]
        public float runInterval = 0.32f;
        public float volumeScale = 1f;

        private CharacterController _cc;
        private float _timer;

        private void Awake()
        {
            _cc = GetComponent<CharacterController>();
        }

        private void Update()
        {
            if (_cc == null)
            {
                return;
            }
            float speed = _cc.velocity.magnitude;
            bool grounded = _cc.isGrounded;
            if (!grounded || speed < minSpeed)
            {
                _timer = 0f;
                return;
            }

            float t = Mathf.Clamp01(speed / 6f);
            float interval = Mathf.Lerp(baseInterval, runInterval, t);
            _timer += Time.deltaTime;
            if (_timer >= interval)
            {
                _timer = 0f;
                // Play step at character's foot position (approximate via transform.position)
                AudioSystem.PlayOneShot(stepKey, transform.position);
            }
        }
    }
}
