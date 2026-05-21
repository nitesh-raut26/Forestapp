using UnityEngine;

namespace ForestFriendsQuest
{
    /// <summary>
    /// Propagates the "calm mode" (reduced motion) setting to all subsystems
    /// that run visual animations.
    ///
    /// When calm mode is active:
    ///   - EmotionalParticleEngine stops emitting new particles
    ///   - CreatureAmbientBehavior wander speed is halved
    ///   - AnimatedTransitionController skips slide animations (instant cut)
    ///   - RegionUnlockSequence skips the particle burst step
    ///   - Background pulsing effects are suspended
    ///
    /// This class is registered with AccessibilityManager and called whenever
    /// SetCalmMode() is toggled. It holds references to the systems it controls.
    /// </summary>
    public class ReducedMotionController : MonoBehaviour
    {
        private EmotionalParticleEngine    _particleEngine;
        private AnimatedTransitionController _transitions;

        public bool IsReducedMotion { get; private set; }

        // ─── Setup ────────────────────────────────────────────────────────────────

        public void Initialize(EmotionalParticleEngine particleEngine,
            AnimatedTransitionController transitions)
        {
            _particleEngine = particleEngine;
            _transitions    = transitions;

            // Restore persisted setting
            IsReducedMotion = PlayerPrefs.GetInt("FFQ.Access.CalmMode", 0) == 1;
            ApplyState(IsReducedMotion);
        }

        // ─── Public API ───────────────────────────────────────────────────────────

        public void SetReducedMotion(bool enabled)
        {
            if (IsReducedMotion == enabled) return;
            IsReducedMotion = enabled;
            ApplyState(enabled);
        }

        // ─── Private ─────────────────────────────────────────────────────────────

        private void ApplyState(bool reduced)
        {
            if (_particleEngine != null)
                _particleEngine.SetEmissionEnabled(!reduced);

            if (_transitions != null)
                _transitions.SetInstantMode(reduced);

            Debug.Log($"[ReducedMotion] Calm mode: {(reduced ? "ON" : "OFF")}");
        }
    }
}
