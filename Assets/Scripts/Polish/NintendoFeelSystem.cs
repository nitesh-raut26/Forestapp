using System;
using System.Collections.Generic;
using UnityEngine;

namespace ForestFriendsQuest
{
    /// <summary>
    /// Nintendo-feel system — every interaction must feel tactile, alive, and warm.
    ///
    /// Applies micro-polish to the entire game:
    ///   - Squash/stretch on all button presses
    ///   - Anticipation frames before major actions
    ///   - Contextual haptic feedback (mobile)
    ///   - Eye-tracking creature look-at (simulated for UI creatures)
    ///   - Soft camera drift during idle moments
    ///   - Emotional pauses before reveals
    ///   - Ambient idle motion on all characters
    ///
    /// This system listens to UIAnimationSystem events and fires haptics
    /// in sync with visual feedback.
    /// </summary>
    public class NintendoFeelSystem : MonoBehaviour
    {
        // ─── Dependencies ─────────────────────────────────────────────────────────

        private UIAnimationSystem        _uiAnim;
        private CameraFeelController     _camera;
        private ReducedMotionController  _reducedMotion;

        // ─── Haptic Settings ─────────────────────────────────────────────────────

        private bool _hapticsEnabled = true;

        // ─── Lifecycle ───────────────────────────────────────────────────────────

        public void Initialize(
            UIAnimationSystem       uiAnim,
            CameraFeelController    camera,
            ReducedMotionController reducedMotion)
        {
            _uiAnim        = uiAnim;
            _camera        = camera;
            _reducedMotion = reducedMotion;

            _hapticsEnabled = PlayerPrefs.GetInt("FFQ.Haptics", 1) == 1;

            Debug.Log($"[NintendoFeelSystem] Haptics: {_hapticsEnabled}");
        }

        // ─── Public API ───────────────────────────────────────────────────────────

        /// <summary>Call on every button press — provides full tactile + visual feedback.</summary>
        public void OnButtonPress(Transform buttonTransform, FeelIntensity intensity = FeelIntensity.Normal)
        {
            if (_reducedMotion?.IsReducedMotion == true)
            {
                Haptic(intensity);
                return;
            }

            _uiAnim?.BouncePress(buttonTransform, intensity == FeelIntensity.Heavy ? 1.4f : 1f);
            Haptic(intensity);
        }

        /// <summary>Call on reward / unlock — celebration burst with camera micro-pulse.</summary>
        public void OnReward(Transform rewardTransform)
        {
            if (_reducedMotion?.IsReducedMotion != true)
            {
                _uiAnim?.CelebrationPop(rewardTransform, 1.3f);
                _camera?.TriggerMicroPulse(0.02f);
            }
            Haptic(FeelIntensity.Heavy);
        }

        /// <summary>Emotional pause before a reveal (0.6s soft hold).</summary>
        public void EmotionalPause(Action onComplete = null)
        {
            if (_reducedMotion?.IsReducedMotion == true)
            {
                onComplete?.Invoke();
                return;
            }
            StartCoroutine(PauseCoroutine(0.6f, onComplete));
        }

        /// <summary>Apply gentle idle pulse to a character/creature element.</summary>
        public void StartIdlePulse(Transform target)
        {
            _uiAnim?.IdlePulse(target, 0.03f);
        }

        /// <summary>Anticipation wobble — slight squeeze before a big action.</summary>
        public void AnticipationWobble(Transform target)
        {
            if (_reducedMotion?.IsReducedMotion == true) return;
            StartCoroutine(AnticipationCoroutine(target));
        }

        /// <summary>Screen shake on significant impacts (boss hit, major fail).</summary>
        public void ImpactShake(float intensity = 0.5f)
        {
            if (_reducedMotion?.IsReducedMotion == true) return;
            _camera?.TriggerScreenShake(intensity, 0.3f);
            Haptic(FeelIntensity.Heavy);
        }

        public void SetHapticsEnabled(bool enabled)
        {
            _hapticsEnabled = enabled;
            PlayerPrefs.SetInt("FFQ.Haptics", enabled ? 1 : 0);
            PlayerPrefs.Save();
        }

        // ─── Haptics ──────────────────────────────────────────────────────────────

        private void Haptic(FeelIntensity intensity)
        {
            if (!_hapticsEnabled) return;
#if UNITY_IOS || UNITY_ANDROID
            switch (intensity)
            {
                case FeelIntensity.Light:  Handheld.Vibrate(); break;
                case FeelIntensity.Normal: Handheld.Vibrate(); break;
                case FeelIntensity.Heavy:  Handheld.Vibrate(); break;
            }
#endif
        }

        // ─── Coroutines ───────────────────────────────────────────────────────────

        private System.Collections.IEnumerator PauseCoroutine(float duration, Action onComplete)
        {
            yield return new WaitForSeconds(duration);
            onComplete?.Invoke();
        }

        private System.Collections.IEnumerator AnticipationCoroutine(Transform target)
        {
            if (target == null) yield break;
            var orig = target.localScale;

            // Squeeze in
            float elapsed = 0f;
            while (elapsed < 0.1f)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / 0.1f;
                target.localScale = Vector3.Lerp(orig, orig * 0.88f, t);
                yield return null;
            }

            // Hold for one frame
            yield return null;

            // Spring out (handled by BouncePress)
            target.localScale = orig;
        }
    }

    public enum FeelIntensity { Light, Normal, Heavy }
}
