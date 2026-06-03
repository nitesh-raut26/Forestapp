using UnityEngine;

namespace ForestFriendsQuest
{
    /// <summary>
    /// Camera feel controller providing spring-based 2D follow, discovery zoom,
    /// soft screen shake on puzzle wins, and per-biome lens configurations.
    /// Works purely via transform manipulation — no Cinemachine dependency required.
    /// </summary>
    public class CameraFeelController : MonoBehaviour
    {
        [Header("Follow Settings")]
        public Transform followTarget;
        public float followSmoothTime   = 0.3f;
        public float followMaxSpeed     = 25f;
        public Vector3 followOffset     = new Vector3(0f, 0f, -10f);

        [Header("Zoom")]
        public float defaultOrthographicSize = 5f;
        public float discoveryZoomSize       = 3.5f;
        public float zoomSmoothTime          = 0.25f;

        [Header("Shake")]
        public float shakeDecay = 8f;

        // ─── Internal State ──────────────────────────────────────────────────────

        private Camera _camera;
        private Vector3 _followVelocity;
        private float   _zoomVelocity;
        private float   _targetOrthoSize;

        private float   _shakeMagnitude;
        private float   _shakeDuration;
        private float   _shakeTimer;
        private Vector3 _shakeOffset;

        private bool    _initialized;

        // ─── Lifecycle ───────────────────────────────────────────────────────────

        private void Awake()
        {
            _camera = GetComponent<Camera>();
            if (_camera == null)
            {
                _camera = Camera.main;
            }

            _targetOrthoSize = defaultOrthographicSize;

            if (_camera != null && _camera.orthographic)
            {
                _camera.orthographicSize = defaultOrthographicSize;
            }

            _initialized = true;
        }

        private void LateUpdate()
        {
            if (!_initialized) return;

            // 1. Spring-follow target
            if (followTarget != null)
            {
                var desiredPos = followTarget.position + followOffset;
                transform.position = Vector3.SmoothDamp(
                    transform.position,
                    desiredPos,
                    ref _followVelocity,
                    followSmoothTime,
                    followMaxSpeed
                );
            }

            // 2. Smooth zoom
            if (_camera != null && _camera.orthographic)
            {
                _camera.orthographicSize = Mathf.SmoothDamp(
                    _camera.orthographicSize,
                    _targetOrthoSize,
                    ref _zoomVelocity,
                    zoomSmoothTime
                );
            }

            // 3. Screen shake
            if (_shakeTimer > 0f)
            {
                _shakeTimer -= Time.deltaTime;
                var progress  = 1f - Mathf.Clamp01(_shakeTimer / _shakeDuration);
                var fade      = Mathf.Lerp(_shakeMagnitude, 0f, progress);
                _shakeOffset  = Random.insideUnitSphere * fade;
                _shakeOffset.z = 0f;
                transform.position += _shakeOffset;
            }
        }

        // ─── Public API ───────────────────────────────────────────────────────────

        /// <summary>Tiny camera pulse used for reward moments.</summary>
        public void TriggerMicroPulse(float magnitude) => Shake(magnitude, 0.2f);

        /// <summary>Screen shake alias used by NintendoFeelSystem.</summary>
        public void TriggerScreenShake(float magnitude, float duration) => Shake(magnitude, duration);

        /// <summary>Trigger a gentle screen shake on puzzle win or rare discovery.</summary>
        public void Shake(float magnitude = 0.12f, float duration = 0.35f)
        {
            _shakeMagnitude = magnitude;
            _shakeDuration  = duration;
            _shakeTimer     = duration;
        }

        /// <summary>Zoom in for discovery moments (creature cutscene, rare item, etc.).</summary>
        public void ZoomToDiscovery()
        {
            _targetOrthoSize = discoveryZoomSize;
        }

        /// <summary>Return to the default world zoom level.</summary>
        public void ZoomToDefault()
        {
            _targetOrthoSize = defaultOrthographicSize;
        }

        /// <summary>Apply a biome-specific lens preset.</summary>
        public void ApplyBiomeLens(string biomeId)
        {
            switch (biomeId)
            {
                case "crystal_caverns":
                    _targetOrthoSize = 4.0f;
                    break;
                case "skyroot_canopy":
                    _targetOrthoSize = 6.5f;
                    break;
                case "ancient_observatory":
                    _targetOrthoSize = 5.5f;
                    break;
                default:
                    _targetOrthoSize = defaultOrthographicSize;
                    break;
            }
        }

        /// <summary>Immediately snap to a world position (no spring easing).</summary>
        public void SnapTo(Vector3 worldPosition)
        {
            transform.position = worldPosition + followOffset;
            _followVelocity = Vector3.zero;
        }
    }
}
