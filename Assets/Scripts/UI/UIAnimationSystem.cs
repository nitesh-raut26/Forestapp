using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace ForestFriendsQuest
{
    /// <summary>
    /// Squash/stretch, button bounce, ambient motion, and Nintendo-level feel.
    ///
    /// Every tap must feel tactile. Every reward must feel earned.
    /// Every transition must feel alive.
    ///
    /// This system applies micro-animations to UI elements at runtime
    /// without requiring pre-authored Animation clips.
    ///
    /// Respects ReducedMotionController — all animations can be disabled.
    /// </summary>
    public class UIAnimationSystem : MonoBehaviour
    {
        // ─── Dependencies ─────────────────────────────────────────────────────────

        private ReducedMotionController _reducedMotion;

        // ─── Active Animations ────────────────────────────────────────────────────

        private readonly List<UITween> _activeTweens = new();

        // ─── Lifecycle ───────────────────────────────────────────────────────────

        public void Initialize(ReducedMotionController reducedMotion)
        {
            _reducedMotion = reducedMotion;
        }

        private void Update()
        {
            if (_reducedMotion?.IsReducedMotion == true) return;

            for (int i = _activeTweens.Count - 1; i >= 0; i--)
            {
                var tween = _activeTweens[i];
                tween.elapsed += Time.deltaTime;

                float t = Mathf.Clamp01(tween.elapsed / tween.duration);
                float easedT = EaseOutBack(t);

                if (tween.transform != null)
                    tween.transform.localScale = Vector3.LerpUnclamped(tween.fromScale, tween.toScale, easedT);

                if (t >= 1f)
                {
                    if (tween.transform != null)
                        tween.transform.localScale = tween.toScale;
                    _activeTweens.RemoveAt(i);
                }
            }
        }

        // ─── Public API ───────────────────────────────────────────────────────────

        /// <summary>Bounce-press effect — squash down then spring back.</summary>
        public void BouncePress(Transform target, float intensity = 1f)
        {
            if (_reducedMotion?.IsReducedMotion == true) return;
            if (target == null) return;

            // Quick squash then return to normal
            var squash = Vector3.one * (1f - 0.12f * intensity);
            ScheduleTween(target, squash, duration: 0.08f, then: () =>
                ScheduleTween(target, Vector3.one, duration: 0.25f));
        }

        /// <summary>Celebration pop — scale up then settle.</summary>
        public void CelebrationPop(Transform target, float scale = 1.25f)
        {
            if (_reducedMotion?.IsReducedMotion == true) return;
            if (target == null) return;

            var big = Vector3.one * scale;
            ScheduleTween(target, big, duration: 0.15f, then: () =>
                ScheduleTween(target, Vector3.one, duration: 0.3f));
        }

        /// <summary>Gentle idle pulse for ambient motion.</summary>
        public void IdlePulse(Transform target, float amplitude = 0.04f)
        {
            if (_reducedMotion?.IsReducedMotion == true) return;
            StartCoroutine(IdlePulseCoroutine(target, amplitude));
        }

        /// <summary>Fade-in a CanvasGroup from 0 to 1.</summary>
        public void FadeIn(CanvasGroup group, float duration = 0.4f, Action onComplete = null)
        {
            if (group == null) return;
            if (_reducedMotion?.IsReducedMotion == true)
            {
                group.alpha = 1f;
                onComplete?.Invoke();
                return;
            }
            StartCoroutine(FadeCoroutine(group, 0f, 1f, duration, onComplete));
        }

        /// <summary>Fade-out a CanvasGroup from 1 to 0.</summary>
        public void FadeOut(CanvasGroup group, float duration = 0.3f, Action onComplete = null)
        {
            if (group == null) return;
            if (_reducedMotion?.IsReducedMotion == true)
            {
                group.alpha = 0f;
                onComplete?.Invoke();
                return;
            }
            StartCoroutine(FadeCoroutine(group, 1f, 0f, duration, onComplete));
        }

        /// <summary>Slide a RectTransform in from an offset position.</summary>
        public void SlideIn(RectTransform rt, Vector2 fromOffset, float duration = 0.35f, Action onComplete = null)
        {
            if (rt == null) return;
            if (_reducedMotion?.IsReducedMotion == true)
            {
                onComplete?.Invoke();
                return;
            }
            var targetPos = rt.anchoredPosition;
            rt.anchoredPosition = targetPos + fromOffset;
            StartCoroutine(SlideCoroutine(rt, targetPos, duration, onComplete));
        }

        /// <summary>Color flash — quickly tint an Image then return to original.</summary>
        public void ColorFlash(Image image, Color flashColor, float duration = 0.2f)
        {
            if (image == null) return;
            StartCoroutine(ColorFlashCoroutine(image, flashColor, duration));
        }

        // ─── Tween Queue ──────────────────────────────────────────────────────────

        private void ScheduleTween(Transform target, Vector3 toScale, float duration, Action then = null)
        {
            var tween = new UITween
            {
                transform  = target,
                fromScale  = target.localScale,
                toScale    = toScale,
                duration   = duration,
                elapsed    = 0f,
                onComplete = then
            };
            _activeTweens.Add(tween);
        }

        // ─── Coroutines ───────────────────────────────────────────────────────────

        private System.Collections.IEnumerator IdlePulseCoroutine(Transform target, float amplitude)
        {
            while (target != null && _reducedMotion?.IsReducedMotion != true)
            {
                float t = Mathf.Sin(Time.time * 1.5f) * amplitude;
                target.localScale = Vector3.one * (1f + t);
                yield return null;
            }
            if (target != null) target.localScale = Vector3.one;
        }

        private System.Collections.IEnumerator FadeCoroutine(CanvasGroup g, float from, float to, float dur, Action onComplete)
        {
            float elapsed = 0f;
            g.alpha = from;
            while (elapsed < dur)
            {
                elapsed += Time.deltaTime;
                g.alpha = Mathf.Lerp(from, to, elapsed / dur);
                yield return null;
            }
            g.alpha = to;
            onComplete?.Invoke();
        }

        private System.Collections.IEnumerator SlideCoroutine(RectTransform rt, Vector2 target, float dur, Action onComplete)
        {
            float elapsed = 0f;
            var start = rt.anchoredPosition;
            while (elapsed < dur)
            {
                elapsed += Time.deltaTime;
                rt.anchoredPosition = Vector2.Lerp(start, target, EaseOutCubic(elapsed / dur));
                yield return null;
            }
            rt.anchoredPosition = target;
            onComplete?.Invoke();
        }

        private System.Collections.IEnumerator ColorFlashCoroutine(Image img, Color flash, float dur)
        {
            var original = img.color;
            img.color = flash;
            yield return new WaitForSeconds(dur * 0.5f);
            float elapsed = 0f;
            while (elapsed < dur * 0.5f)
            {
                elapsed += Time.deltaTime;
                img.color = Color.Lerp(flash, original, elapsed / (dur * 0.5f));
                yield return null;
            }
            img.color = original;
        }

        // ─── Easing Functions ─────────────────────────────────────────────────────

        private static float EaseOutBack(float t)
        {
            const float c1 = 1.70158f;
            const float c3 = c1 + 1f;
            return 1f + c3 * Mathf.Pow(t - 1f, 3f) + c1 * Mathf.Pow(t - 1f, 2f);
        }

        private static float EaseOutCubic(float t) => 1f - Mathf.Pow(1f - t, 3f);

        // ─── Data Types ───────────────────────────────────────────────────────────

        private class UITween
        {
            public Transform transform;
            public Vector3   fromScale;
            public Vector3   toScale;
            public float     duration;
            public float     elapsed;
            public Action    onComplete;
        }
    }
}
