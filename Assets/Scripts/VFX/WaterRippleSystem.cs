using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace ForestFriendsQuest
{
    /// <summary>
    /// Simulates concentric ripple rings for water-adjacent zones
    /// (River Bend, Moonlit Creek, Firefly Marsh).
    ///
    /// Each ripple is an expanding Image ring (scale 1→2.5) that fades out.
    /// Rings spawn at configurable anchor points (creek center, pond edge etc.)
    ///
    /// Pooled — no Destroy(). Active count limited by PerformanceManager tier.
    /// </summary>
    public class WaterRippleSystem : MonoBehaviour
    {
        private RectTransform      _canvas;
        private PerformanceManager _perf;

        private readonly List<RippleRing> _pool      = new List<RippleRing>();
        private readonly List<Vector2>    _dropPoints = new List<Vector2>();
        private bool _active;

        private static readonly Color RippleColor = new Color(0.4f, 0.75f, 0.9f, 0.6f);

        // ─── Setup ────────────────────────────────────────────────────────────────

        public void Initialize(RectTransform canvas, PerformanceManager perf)
        {
            _canvas = canvas;
            _perf   = perf;

            var poolSize = perf.ScaleParticles(8);
            for (var i = 0; i < poolSize; i++)
                _pool.Add(CreateRing(i));

            _active = perf.AmbientVFXEnabled;
        }

        /// <summary>Register a point where ripples should originate.</summary>
        public void AddDropPoint(Vector2 canvasPos)
        {
            _dropPoints.Add(canvasPos);
            if (_active && _dropPoints.Count == 1)
                StartCoroutine(RippleLoop());
        }

        public void SetActive(bool active) => _active = active;

        // ─── Ripple Loop ──────────────────────────────────────────────────────────

        private IEnumerator RippleLoop()
        {
            var poolIndex = 0;
            while (_active && _dropPoints.Count > 0)
            {
                var point = _dropPoints[Random.Range(0, _dropPoints.Count)];
                // Offset slightly for natural feel
                point += new Vector2(Random.Range(-20f, 20f), Random.Range(-10f, 10f));

                var ring = _pool[poolIndex % _pool.Count];
                poolIndex++;

                StartCoroutine(AnimateRing(ring, point));
                yield return new WaitForSeconds(Random.Range(0.8f, 2.5f));
            }
        }

        private static IEnumerator AnimateRing(RippleRing ring, Vector2 pos)
        {
            ring.Rect.anchoredPosition = pos;
            ring.Rect.localScale       = Vector3.one * 0.3f;
            ring.Image.gameObject.SetActive(true);
            ring.Image.color = RippleColor;

            var elapsed  = 0f;
            var duration = Random.Range(1.2f, 2f);

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                var t = Mathf.Clamp01(elapsed / duration);

                ring.Rect.localScale = Vector3.one * Mathf.Lerp(0.3f, 2.8f, t);
                ring.Image.color = new Color(
                    RippleColor.r, RippleColor.g, RippleColor.b,
                    Mathf.Lerp(0.6f, 0f, t));

                yield return null;
            }

            ring.Image.gameObject.SetActive(false);
        }

        // ─── Factory ─────────────────────────────────────────────────────────────

        private RippleRing CreateRing(int index)
        {
            var go = new GameObject($"Ripple_{index}");
            go.transform.SetParent(_canvas, false);
            var rt = go.AddComponent<RectTransform>();
            rt.sizeDelta = new Vector2(40f, 20f); // ellipse approximation
            rt.pivot     = new Vector2(0.5f, 0.5f);

            var img = go.AddComponent<Image>();
            img.color = new Color(RippleColor.r, RippleColor.g, RippleColor.b, 0f);
            img.raycastTarget = false;

            go.SetActive(false);
            return new RippleRing { Rect = rt, Image = img };
        }

        private class RippleRing
        {
            public RectTransform Rect;
            public Image         Image;
        }
    }
}
