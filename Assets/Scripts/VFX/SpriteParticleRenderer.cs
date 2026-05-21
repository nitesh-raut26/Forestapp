using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace ForestFriendsQuest
{
    public enum SpriteParticleShape { Dot, Star, Leaf, Snowflake, Petal }

    /// <summary>
    /// General-purpose sprite particle renderer for one-shot burst effects.
    ///
    /// Unlike EmotionalParticleEngine (which has fixed profiles), this renderer
    /// takes explicit parameters at spawn time, making it suitable for unique
    /// visual moments: region unlock, level complete, seasonal transition.
    ///
    /// All particles are from a shared pool — no Destroy() during steady state.
    /// Pool size is scaled by PerformanceManager.
    /// </summary>
    public class SpriteParticleRenderer : MonoBehaviour
    {
        private RectTransform      _canvas;
        private PerformanceManager _perf;

        private readonly List<SpriteParticle> _pool = new List<SpriteParticle>();
        private int _nextFree;

        // ─── Setup ────────────────────────────────────────────────────────────────

        public void Initialize(RectTransform canvas, PerformanceManager perf)
        {
            _canvas = canvas;
            _perf   = perf;

            var poolSize = perf.ScaleParticles(32);
            for (var i = 0; i < poolSize; i++)
                _pool.Add(CreateParticle(i));
        }

        // ─── Public API ───────────────────────────────────────────────────────────

        /// <summary>Burst count particles from pos in all directions.</summary>
        public void Burst(Vector2 pos, int count, Color color,
            SpriteParticleShape shape = SpriteParticleShape.Dot,
            float radius = 80f, float lifetime = 0.8f)
        {
            count = _perf.ScaleParticles(count);

            for (var i = 0; i < count; i++)
            {
                var p = GetFree();
                if (p == null) break;

                p.Rect.anchoredPosition = pos;
                p.Image.color           = color;
                p.Image.gameObject.SetActive(true);

                var angle    = i * (360f / count) * Mathf.Deg2Rad;
                var speed    = Random.Range(radius * 0.5f, radius);
                var velocity = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * speed;

                StartCoroutine(AnimateParticle(p, velocity, lifetime));
            }
        }

        /// <summary>Scatter count particles randomly within a rect.</summary>
        public void Scatter(Rect area, int count, Color color,
            SpriteParticleShape shape = SpriteParticleShape.Leaf, float lifetime = 1.5f)
        {
            count = _perf.ScaleParticles(count);

            for (var i = 0; i < count; i++)
            {
                var p = GetFree();
                if (p == null) break;

                var startX = Random.Range(area.xMin, area.xMax);
                var startY = area.yMax + Random.Range(0f, 30f);
                p.Rect.anchoredPosition = new Vector2(startX, startY);
                p.Image.color = color;
                p.Image.gameObject.SetActive(true);

                var vel = new Vector2(Random.Range(-15f, 15f), -Random.Range(20f, 50f));
                StartCoroutine(AnimateParticle(p, vel, lifetime));
            }
        }

        // ─── Private ─────────────────────────────────────────────────────────────

        private SpriteParticle GetFree()
        {
            for (var i = 0; i < _pool.Count; i++)
            {
                var idx = (_nextFree + i) % _pool.Count;
                if (!_pool[idx].Image.gameObject.activeSelf)
                {
                    _nextFree = (idx + 1) % _pool.Count;
                    return _pool[idx];
                }
            }
            return null;
        }

        private static IEnumerator AnimateParticle(SpriteParticle p, Vector2 velocity, float lifetime)
        {
            var elapsed = 0f;

            while (elapsed < lifetime)
            {
                elapsed += Time.deltaTime;
                var t = elapsed / lifetime;

                velocity.y -= 200f * Time.deltaTime; // gravity
                p.Rect.anchoredPosition += velocity * Time.deltaTime;
                p.Rect.localScale = Vector3.one * Mathf.Lerp(1f, 0.2f, t);

                var c = p.Image.color;
                p.Image.color = new Color(c.r, c.g, c.b, Mathf.Lerp(1f, 0f, t * t));

                yield return null;
            }

            p.Image.gameObject.SetActive(false);
        }

        private SpriteParticle CreateParticle(int index)
        {
            var go = new GameObject($"SP_{index}");
            go.transform.SetParent(_canvas, false);
            var rt = go.AddComponent<RectTransform>();
            rt.sizeDelta = new Vector2(10f, 10f);
            rt.pivot     = new Vector2(0.5f, 0.5f);

            var img = go.AddComponent<Image>();
            img.raycastTarget = false;
            go.SetActive(false);

            return new SpriteParticle { Rect = rt, Image = img };
        }

        private class SpriteParticle
        {
            public RectTransform Rect;
            public Image         Image;
        }
    }
}
