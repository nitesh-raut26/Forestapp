using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace ForestFriendsQuest
{
    /// <summary>
    /// Provides smooth animated transitions between panels.
    ///
    /// Supports:
    ///   - CrossFade:   outgoing fades out, incoming fades in
    ///   - SlideLeft:   incoming slides in from right, outgoing slides out left
    ///   - SlideUp:     incoming slides up from bottom (modal style)
    ///   - ScalePop:    incoming scales from 0.85 to 1.0 (achievement / reward)
    ///
    /// All transitions use coroutines and LeanTween-style cubic easing baked inline
    /// (no external dependency required).
    /// </summary>
    public class AnimatedTransitionController : MonoBehaviour
    {
        public enum TransitionType { CrossFade, SlideLeft, SlideUp, ScalePop }

        [Header("Timing")]
        [SerializeField] private float _fadeDuration  = 0.22f;
        [SerializeField] private float _slideDuration = 0.28f;
        [SerializeField] private float _popDuration   = 0.20f;

        private bool _running;
        private bool _instantMode;

        /// <summary>When true, all transitions are instantaneous (for calm / reduced-motion mode).</summary>
        public void SetInstantMode(bool instant) => _instantMode = instant;

        // ─── Public API ───────────────────────────────────────────────────────────

        /// <summary>
        /// Cross-fade between two panels. Calls onMidpoint when outgoing is fully
        /// invisible (safe to swap data), then calls onComplete when incoming is
        /// fully visible.
        /// </summary>
        public void CrossFade(
            RectTransform outgoing,
            RectTransform incoming,
            Action onMidpoint  = null,
            Action onComplete  = null)
        {
            StartCoroutine(DoCrossFade(outgoing, incoming, onMidpoint, onComplete));
        }

        public void SlideIn(RectTransform incoming, TransitionType direction = TransitionType.SlideLeft, Action onComplete = null)
        {
            StartCoroutine(DoSlideIn(incoming, direction, onComplete));
        }

        public void SlideOut(RectTransform outgoing, TransitionType direction = TransitionType.SlideLeft, Action onComplete = null)
        {
            StartCoroutine(DoSlideOut(outgoing, direction, onComplete));
        }

        public void ScalePop(RectTransform target, Action onComplete = null)
        {
            StartCoroutine(DoScalePop(target, onComplete));
        }

        // ─── Coroutine Implementations ────────────────────────────────────────────

        private IEnumerator DoCrossFade(
            RectTransform outgoing,
            RectTransform incoming,
            Action onMidpoint,
            Action onComplete)
        {
            var outGroup = GetOrAddCanvasGroup(outgoing);
            var inGroup  = GetOrAddCanvasGroup(incoming);

            // Ensure incoming is hidden and active
            if (incoming != null)
            {
                incoming.gameObject.SetActive(true);
                inGroup.alpha          = 0f;
                inGroup.blocksRaycasts = false;
            }

            if (_instantMode)
            {
                if (outgoing != null) { outGroup.alpha = 0f; outgoing.gameObject.SetActive(false); }
                onMidpoint?.Invoke();
                if (incoming != null) { inGroup.alpha = 1f; inGroup.blocksRaycasts = true; }
                onComplete?.Invoke();
                yield break;
            }

            // Fade out
            if (outgoing != null)
            {
                yield return FadeGroup(outGroup, 1f, 0f, _fadeDuration);
                outGroup.blocksRaycasts = false;
                outgoing.gameObject.SetActive(false);
            }

            onMidpoint?.Invoke();

            // Fade in
            if (incoming != null)
            {
                inGroup.blocksRaycasts = true;
                yield return FadeGroup(inGroup, 0f, 1f, _fadeDuration);
            }

            onComplete?.Invoke();
        }

        private IEnumerator DoSlideIn(RectTransform incoming, TransitionType direction, Action onComplete)
        {
            if (incoming == null) yield break;

            var group = GetOrAddCanvasGroup(incoming);
            incoming.gameObject.SetActive(true);
            group.blocksRaycasts = false;

            var screenW = Screen.width;
            var screenH = Screen.height;

            var startPos = direction == TransitionType.SlideLeft
                ? new Vector2(screenW * 0.4f, 0f)
                : new Vector2(0f, -screenH * 0.3f);

            incoming.anchoredPosition = startPos;

            var elapsed = 0f;
            while (elapsed < _slideDuration)
            {
                elapsed += Time.deltaTime;
                var t = CubicEaseOut(Mathf.Clamp01(elapsed / _slideDuration));
                incoming.anchoredPosition = Vector2.Lerp(startPos, Vector2.zero, t);
                group.alpha = t;
                yield return null;
            }

            incoming.anchoredPosition  = Vector2.zero;
            group.alpha                = 1f;
            group.blocksRaycasts       = true;
            onComplete?.Invoke();
        }

        private IEnumerator DoSlideOut(RectTransform outgoing, TransitionType direction, Action onComplete)
        {
            if (outgoing == null) yield break;

            var group = GetOrAddCanvasGroup(outgoing);
            var screenW = Screen.width;
            var endPos = direction == TransitionType.SlideLeft
                ? new Vector2(-screenW * 0.4f, 0f)
                : new Vector2(0f, -Screen.height * 0.3f);

            group.blocksRaycasts = false;

            var elapsed = 0f;
            while (elapsed < _slideDuration)
            {
                elapsed += Time.deltaTime;
                var t = CubicEaseIn(Mathf.Clamp01(elapsed / _slideDuration));
                outgoing.anchoredPosition = Vector2.Lerp(Vector2.zero, endPos, t);
                group.alpha = 1f - t;
                yield return null;
            }

            outgoing.gameObject.SetActive(false);
            outgoing.anchoredPosition = Vector2.zero;
            onComplete?.Invoke();
        }

        private IEnumerator DoScalePop(RectTransform target, Action onComplete)
        {
            if (target == null) yield break;

            var group = GetOrAddCanvasGroup(target);
            target.gameObject.SetActive(true);
            group.blocksRaycasts = false;

            var elapsed = 0f;
            while (elapsed < _popDuration)
            {
                elapsed += Time.deltaTime;
                var t = ElasticEaseOut(Mathf.Clamp01(elapsed / _popDuration));
                target.localScale = Vector3.one * Mathf.Lerp(0.85f, 1f, t);
                group.alpha = Mathf.Clamp01(elapsed / (_popDuration * 0.4f));
                yield return null;
            }

            target.localScale        = Vector3.one;
            group.alpha              = 1f;
            group.blocksRaycasts     = true;
            onComplete?.Invoke();
        }

        private IEnumerator FadeGroup(CanvasGroup group, float from, float to, float duration)
        {
            var elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                group.alpha = Mathf.Lerp(from, to, Mathf.Clamp01(elapsed / duration));
                yield return null;
            }
            group.alpha = to;
        }

        // ─── Easing Functions ─────────────────────────────────────────────────────

        private static float CubicEaseOut(float t)  => 1f - Mathf.Pow(1f - t, 3f);
        private static float CubicEaseIn(float t)   => t * t * t;
        private static float ElasticEaseOut(float t)
        {
            if (t <= 0f) return 0f;
            if (t >= 1f) return 1f;
            return Mathf.Pow(2f, -10f * t) * Mathf.Sin((t * 10f - 0.75f) * (2f * Mathf.PI / 3f)) + 1f;
        }

        // ─── Helpers ──────────────────────────────────────────────────────────────

        private static CanvasGroup GetOrAddCanvasGroup(RectTransform rt)
        {
            if (rt == null) return null;
            return rt.gameObject.GetComponent<CanvasGroup>()
                ?? rt.gameObject.AddComponent<CanvasGroup>();
        }
    }
}
