using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace ForestFriendsQuest
{
    /// <summary>
    /// Adaptive visual density scaler. Monitors frame rate and cognitive load
    /// signals (puzzle active, NPC count, ambient particles) and dynamically
    /// adjusts the density of visual effects to maintain smooth performance
    /// and prevent sensory overwhelm — critical for children ages 4-8.
    ///
    /// Two modes:
    ///   Performance Mode — reduces particle counts, glow, and ambient effects
    ///                      when FPS drops below threshold
    ///   Calm Mode        — reduces visual density during high cognitive load
    ///                      (puzzle active, post-mistake recovery)
    ///
    /// All adjustments are gradual (lerped over 2 seconds) to avoid jarring
    /// visual transitions.
    /// </summary>
    public class AdaptiveVisualDensityScaler : MonoBehaviour
    {
        // ─── Config ──────────────────────────────────────────────────────────────

        [Header("Performance Targets")]
        public float targetFPS          = 55f;
        public float criticalFPS        = 30f;
        public float measureInterval    = 1.5f;

        [Header("Density Levels (0=minimal, 1=full)")]
        public float fullDensity    = 1.0f;
        public float reducedDensity = 0.5f;
        public float minimalDensity = 0.20f;

        [Header("Cognitive Calm Mode")]
        public bool  enableCalmModeOnPuzzle = true;
        public float calmModeDensity        = 0.40f;
        public float calmTransitionSpeed    = 0.5f;   // per second

        // ─── System Links ─────────────────────────────────────────────────────────

        private AmbientVFXController    _ambientVFX;
        private EmotionalParticleEngine _particles;
        private PuzzleManager           _puzzleManager;
        private CanvasGroup             _worldCanvasGroup;
        private CanvasGroup             _vfxCanvasGroup;

        // ─── State ───────────────────────────────────────────────────────────────

        private float _currentDensity = 1.0f;
        private float _targetDensity  = 1.0f;

        private float _frameTimer;
        private int   _frameCount;
        private float _measuredFPS;

        private bool  _isCalmMode;
        private bool  _isPerformanceMode;

        // ─── Performance Window ───────────────────────────────────────────────────

        private const int FpsWindowSize = 10;
        private readonly float[] _fpsWindow = new float[FpsWindowSize];
        private int _fpsWindowIndex;

        // ─── Lifecycle ───────────────────────────────────────────────────────────

        public void Initialize(
            AmbientVFXController    ambientVFX,
            EmotionalParticleEngine particles,
            PuzzleManager           puzzleManager,
            CanvasGroup             worldCanvasGroup = null,
            CanvasGroup             vfxCanvasGroup   = null)
        {
            _ambientVFX      = ambientVFX;
            _particles       = particles;
            _puzzleManager   = puzzleManager;
            _worldCanvasGroup = worldCanvasGroup;
            _vfxCanvasGroup   = vfxCanvasGroup;
        }

        private void Update()
        {
            MeasureFPS();
            DetectCalmMode();
            UpdateDensity();
        }

        // ─── Public API ───────────────────────────────────────────────────────────

        public float CurrentDensity    => _currentDensity;
        public float MeasuredFPS       => _measuredFPS;
        public bool  IsPerformanceMode => _isPerformanceMode;
        public bool  IsCalmMode        => _isCalmMode;

        /// <summary>Manually override density (e.g. settings screen).</summary>
        public void SetDensityOverride(float density)
        {
            _targetDensity = Mathf.Clamp01(density);
        }

        /// <summary>Re-enable automatic density management.</summary>
        public void ClearDensityOverride()
        {
            _isPerformanceMode = false;
            _isCalmMode        = false;
        }

        // ─── FPS Measurement ──────────────────────────────────────────────────────

        private void MeasureFPS()
        {
            _frameCount++;
            _frameTimer += Time.unscaledDeltaTime;

            if (_frameTimer >= measureInterval)
            {
                var fps = _frameCount / _frameTimer;
                _fpsWindow[_fpsWindowIndex % FpsWindowSize] = fps;
                _fpsWindowIndex++;

                // Rolling average
                var sum   = 0f;
                var count = Mathf.Min(_fpsWindowIndex, FpsWindowSize);
                for (var i = 0; i < count; i++) sum += _fpsWindow[i];
                _measuredFPS = sum / count;

                _frameCount = 0;
                _frameTimer = 0f;

                EvaluatePerformance();
            }
        }

        private void EvaluatePerformance()
        {
            if (_measuredFPS < criticalFPS)
            {
                _isPerformanceMode = true;
                _targetDensity     = minimalDensity;
            }
            else if (_measuredFPS < targetFPS)
            {
                _isPerformanceMode = true;
                _targetDensity     = reducedDensity;
            }
            else if (_isPerformanceMode && _measuredFPS >= targetFPS + 5f)
            {
                // Recovered — restore density
                _isPerformanceMode = false;
                if (!_isCalmMode) _targetDensity = fullDensity;
            }
        }

        // ─── Calm Mode ────────────────────────────────────────────────────────────

        private void DetectCalmMode()
        {
            if (!enableCalmModeOnPuzzle || _puzzleManager == null) return;

            var shouldBeCalm = _puzzleManager.IsActive;

            if (shouldBeCalm != _isCalmMode)
            {
                _isCalmMode    = shouldBeCalm;
                _targetDensity = _isCalmMode ? calmModeDensity : fullDensity;
            }
        }

        // ─── Density Application ──────────────────────────────────────────────────

        private void UpdateDensity()
        {
            if (Mathf.Approximately(_currentDensity, _targetDensity)) return;

            _currentDensity = Mathf.MoveTowards(
                _currentDensity,
                _targetDensity,
                calmTransitionSpeed * Time.deltaTime
            );

            ApplyDensity(_currentDensity);
        }

        private void ApplyDensity(float density)
        {
            // Ambient particle rates — scale with density
            if (_ambientVFX != null)
            {
                _ambientVFX.fireflySpawnRate  = Mathf.Lerp(0f, 0.6f, density);
                _ambientVFX.pollenSpawnRate   = Mathf.Lerp(0f, 0.4f, density);
                _ambientVFX.dustMoteSpawnRate = Mathf.Lerp(0f, 0.25f, density);
            }

            // VFX canvas group alpha (soft fade)
            if (_vfxCanvasGroup != null)
            {
                _vfxCanvasGroup.alpha = Mathf.Lerp(0.3f, 1.0f, density);
            }

            // World canvas gets slightly dimmed in minimal mode for clarity
            if (_worldCanvasGroup != null)
            {
                _worldCanvasGroup.alpha = Mathf.Lerp(0.85f, 1.0f, density);
            }
        }

        // ─── Debug Info ───────────────────────────────────────────────────────────

        public string GetDebugStatus()
        {
            return $"FPS:{_measuredFPS:F0} Density:{_currentDensity:F2} " +
                   $"Perf:{_isPerformanceMode} Calm:{_isCalmMode}";
        }
    }
}
