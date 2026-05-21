using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace ForestFriendsQuest
{
    /// <summary>
    /// Spawns ambient firefly-like wisps that drift slowly across the UI canvas.
    ///
    /// Used in the Sanctuary and Firefly Hollow region.
    /// Each firefly follows a gentle curved path with occasional direction changes.
    /// Count and brightness are scaled by PerformanceManager tier.
    ///
    /// All particles are pooled — no Destroy() in steady state.
    /// </summary>
    public class FireflyTrailSystem : MonoBehaviour
    {
        private RectTransform    _canvas;
        private PerformanceManager _perf;

        private readonly List<FireflyAgent> _pool = new List<FireflyAgent>();
        private bool _active;

        // ─── Setup ────────────────────────────────────────────────────────────────

        public void Initialize(RectTransform canvas, PerformanceManager perf)
        {
            _canvas = canvas;
            _perf   = perf;

            var count = perf.ScaleParticles(16);
            for (var i = 0; i < count; i++)
                _pool.Add(CreateFirefly(i));

            _active = perf.AmbientVFXEnabled;
            if (_active)
                StartCoroutine(SpawnLoop());
        }

        public void SetActive(bool active)
        {
            _active = active;
            foreach (var f in _pool)
                f.Image.gameObject.SetActive(active);
        }

        // ─── Firefly Loop ─────────────────────────────────────────────────────────

        private IEnumerator SpawnLoop()
        {
            foreach (var fly in _pool)
            {
                StartCoroutine(FireflyLife(fly));
                yield return new WaitForSeconds(Random.Range(0.3f, 1.2f));
            }
        }

        private IEnumerator FireflyLife(FireflyAgent fly)
        {
            while (_active)
            {
                // Respawn at random edge position
                var startX = Random.Range(_canvas.rect.xMin + 20f, _canvas.rect.xMax - 20f);
                var startY = Random.Range(_canvas.rect.yMin + 40f, _canvas.rect.yMax * 0.6f);
                fly.Rect.anchoredPosition = new Vector2(startX, startY);
                fly.Image.color = new Color(0.7f, 1f, 0.55f, 0f);
                fly.Image.gameObject.SetActive(true);

                var lifetime = Random.Range(4f, 9f);
                var elapsed  = 0f;
                var phase    = Random.Range(0f, Mathf.PI * 2f);
                var driftX   = Random.Range(-15f, 15f);
                var driftY   = Random.Range(8f, 25f);

                while (elapsed < lifetime)
                {
                    elapsed += Time.deltaTime;
                    var t = elapsed / lifetime;

                    // Sine-wave drift
                    var sway = Mathf.Sin(elapsed * 1.2f + phase) * 18f;
                    fly.Rect.anchoredPosition += new Vector2(
                        (driftX + sway) * Time.deltaTime,
                        driftY * Time.deltaTime);

                    // Alpha envelope: fade in → glow → fade out
                    var alpha = Mathf.Sin(t * Mathf.PI) * 0.85f;
                    // Pulse brightness
                    var pulse = 0.65f + Mathf.Sin(elapsed * 3.5f + phase) * 0.2f;
                    fly.Image.color = new Color(0.7f, 1f, 0.5f, alpha * pulse);

                    // Scale pulse
                    fly.Rect.localScale = Vector3.one * (0.8f + Mathf.Sin(elapsed * 2f) * 0.15f);

                    yield return null;
                }

                fly.Image.gameObject.SetActive(false);
                yield return new WaitForSeconds(Random.Range(0.5f, 3f));
            }
        }

        // ─── Factory ─────────────────────────────────────────────────────────────

        private FireflyAgent CreateFirefly(int index)
        {
            var go = new GameObject($"Firefly_{index}");
            go.transform.SetParent(_canvas, false);
            var rt = go.AddComponent<RectTransform>();
            rt.sizeDelta = new Vector2(6f, 6f);
            rt.pivot     = new Vector2(0.5f, 0.5f);

            var img = go.AddComponent<Image>();
            img.color = new Color(0.7f, 1f, 0.55f, 0f);
            img.raycastTarget = false;

            go.SetActive(false);
            return new FireflyAgent { Rect = rt, Image = img };
        }

        private class FireflyAgent
        {
            public RectTransform Rect;
            public Image         Image;
        }
    }
}
