using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace ForestFriendsQuest
{
    /// <summary>
    /// Manages all ambient world VFX: fireflies at night, floating pollen spores,
    /// dust motes, ambient mist, rain ripples. Reacts to day/night and weather
    /// changes from DayNightWeatherController. Zero emoji — all sprite-based.
    /// </summary>
    public class AmbientVFXController : MonoBehaviour
    {
        [Header("Canvas Parent")]
        public RectTransform ambientCanvas;

        [Header("Ambient Rates")]
        public float fireflySpawnRate   = 0.6f;   // fireflies per second at night
        public float pollenSpawnRate    = 0.4f;   // pollen spores per second
        public float dustMoteSpawnRate  = 0.25f;  // dust motes per second

        // ─── Internal State ──────────────────────────────────────────────────────

        private TimeOfDay    _currentTime    = TimeOfDay.Afternoon;  // TimeOfDay.Day doesn't exist; Afternoon is the default daytime state
        private WeatherState _currentWeather = WeatherState.Clear;

        private float _fireflyTimer;
        private float _pollenTimer;
        private float _dustTimer;

        private Sprite _softCircleSprite;
        private Sprite _glowDotSprite;

        // ─── Live Ambient Particles ───────────────────────────────────────────────

        private class AmbientParticle
        {
            public RectTransform rect;
            public Image         image;
            public Vector2       velocity;
            public float         lifetime;
            public float         maxLifetime;
            public float         driftPhase;
            public float         driftAmp;
            public float         driftFreq;
            public bool          isActive;
        }

        private const int MaxAmbient = 80;
        private readonly AmbientParticle[] _ambient = new AmbientParticle[MaxAmbient];
        private int _activeCount;

        // ─── Rain Ripples ─────────────────────────────────────────────────────────

        private class RainRipple
        {
            public RectTransform rect;
            public Image         image;
            public float         lifetime;
            public float         maxLifetime;
            public bool          isActive;
        }

        private const int MaxRipples = 20;
        private readonly RainRipple[] _ripples = new RainRipple[MaxRipples];

        // ─── Lifecycle ───────────────────────────────────────────────────────────

        private void Awake()
        {
            _softCircleSprite = CreateCircleSprite(24);
            _glowDotSprite    = CreateCircleSprite(12);

            for (var i = 0; i < MaxAmbient; i++) _ambient[i]  = new AmbientParticle();
            for (var i = 0; i < MaxRipples; i++) _ripples[i]  = new RainRipple();
        }

        private void Update()
        {
            UpdateAmbientParticles();
            UpdateRainRipples();
            SpawnAmbientOverTime();
        }

        // ─── Public API ───────────────────────────────────────────────────────────

        public void UpdateAmbientState(TimeOfDay time, WeatherState weather)
        {
            _currentTime    = time;
            _currentWeather = weather;
        }

        // ─── Spawning Logic ───────────────────────────────────────────────────────

        private void SpawnAmbientOverTime()
        {
            // Fireflies only appear at dusk/night
            if (_currentTime == TimeOfDay.Night || _currentTime == TimeOfDay.Dusk)
            {
                _fireflyTimer += Time.deltaTime;
                if (_fireflyTimer >= 1f / fireflySpawnRate)
                {
                    _fireflyTimer = 0f;
                    SpawnFirefly();
                }
            }

            // Pollen and dust in clear/sunny weather
            if (_currentWeather == WeatherState.Clear)
            {
                _pollenTimer += Time.deltaTime;
                if (_pollenTimer >= 1f / pollenSpawnRate)
                {
                    _pollenTimer = 0f;
                    SpawnPollen();
                }

                _dustTimer += Time.deltaTime;
                if (_dustTimer >= 1f / dustMoteSpawnRate)
                {
                    _dustTimer = 0f;
                    SpawnDustMote();
                }
            }

            // Rain ripples during rain
            if (_currentWeather == WeatherState.Rainy)
            {
                _dustTimer += Time.deltaTime;
                if (_dustTimer >= 0.15f)
                {
                    _dustTimer = 0f;
                    SpawnRainRipple();
                }
            }
        }

        private void SpawnFirefly()
        {
            var p = GetFreeSlot();
            if (p == null) return;

            EnsureParticleGO(p);

            var xPos = Random.Range(-500f, 500f);
            var yPos = Random.Range(-300f, 300f);
            p.rect.anchoredPosition = new Vector2(xPos, yPos);

            // Soft warm green-yellow glow
            var brightness = Random.Range(0.7f, 1.0f);
            p.image.color  = new Color(0.65f * brightness, 1.0f * brightness, 0.45f * brightness, 0.85f);
            p.image.sprite = _glowDotSprite;

            var scale = Random.Range(0.25f, 0.45f);
            p.rect.sizeDelta  = new Vector2(20f, 20f);
            p.rect.localScale = Vector3.one * scale;

            p.velocity      = new Vector2(Random.Range(-8f, 8f), Random.Range(3f, 12f));
            p.lifetime      = Random.Range(3.0f, 6.0f);
            p.maxLifetime   = p.lifetime;
            p.driftPhase    = Random.Range(0f, Mathf.PI * 2f);
            p.driftAmp      = Random.Range(15f, 40f);
            p.driftFreq     = Random.Range(1.0f, 2.5f);
            p.isActive      = true;

            p.rect.gameObject.SetActive(true);
        }

        private void SpawnPollen()
        {
            var p = GetFreeSlot();
            if (p == null) return;

            EnsureParticleGO(p);

            p.rect.anchoredPosition = new Vector2(
                Random.Range(-600f, 600f),
                Random.Range(-400f, -100f) // spawn near bottom, rises
            );

            p.image.color  = new Color(0.95f, 0.88f, 0.45f, Random.Range(0.35f, 0.65f));
            p.image.sprite = _softCircleSprite;

            var scale = Random.Range(0.15f, 0.30f);
            p.rect.sizeDelta  = new Vector2(20f, 20f);
            p.rect.localScale = Vector3.one * scale;

            p.velocity      = new Vector2(Random.Range(-6f, 6f), Random.Range(12f, 28f));
            p.lifetime      = Random.Range(4.0f, 8.0f);
            p.maxLifetime   = p.lifetime;
            p.driftPhase    = Random.Range(0f, Mathf.PI * 2f);
            p.driftAmp      = Random.Range(10f, 25f);
            p.driftFreq     = Random.Range(0.5f, 1.5f);
            p.isActive      = true;

            p.rect.gameObject.SetActive(true);
        }

        private void SpawnDustMote()
        {
            var p = GetFreeSlot();
            if (p == null) return;

            EnsureParticleGO(p);

            p.rect.anchoredPosition = new Vector2(Random.Range(-700f, 700f), Random.Range(-500f, 500f));

            var warmth = Random.Range(0.75f, 0.95f);
            p.image.color  = new Color(warmth, warmth * 0.85f, warmth * 0.65f, Random.Range(0.20f, 0.45f));
            p.image.sprite = _softCircleSprite;

            var scale = Random.Range(0.08f, 0.20f);
            p.rect.sizeDelta  = new Vector2(20f, 20f);
            p.rect.localScale = Vector3.one * scale;

            p.velocity      = new Vector2(Random.Range(-3f, 3f), Random.Range(2f, 8f));
            p.lifetime      = Random.Range(5.0f, 10.0f);
            p.maxLifetime   = p.lifetime;
            p.driftPhase    = Random.Range(0f, Mathf.PI * 2f);
            p.driftAmp      = Random.Range(5f, 15f);
            p.driftFreq     = Random.Range(0.3f, 0.8f);
            p.isActive      = true;

            p.rect.gameObject.SetActive(true);
        }

        private void SpawnRainRipple()
        {
            RainRipple ripple = null;
            for (var i = 0; i < MaxRipples; i++)
            {
                if (!_ripples[i].isActive) { ripple = _ripples[i]; break; }
            }

            if (ripple == null) return;

            if (ripple.rect == null)
            {
                var go    = new GameObject("RainRipple");
                go.transform.SetParent(ambientCanvas != null ? ambientCanvas : transform as RectTransform, false);
                ripple.rect  = go.AddComponent<RectTransform>();
                ripple.image = go.AddComponent<Image>();
                ripple.image.sprite = CreateRingSprite();
                ripple.image.raycastTarget = false;
            }

            ripple.rect.gameObject.SetActive(true);
            ripple.rect.anchoredPosition = new Vector2(Random.Range(-600f, 600f), Random.Range(-500f, -100f));
            ripple.rect.sizeDelta        = new Vector2(40f, 20f);
            ripple.rect.localScale       = Vector3.one * 0.15f;
            ripple.image.color           = new Color(0.65f, 0.80f, 0.95f, 0.55f);
            ripple.lifetime              = 0.7f;
            ripple.maxLifetime           = 0.7f;
            ripple.isActive              = true;
        }

        // ─── Update Methods ───────────────────────────────────────────────────────

        private void UpdateAmbientParticles()
        {
            for (var i = 0; i < MaxAmbient; i++)
            {
                var p = _ambient[i];
                if (!p.isActive) continue;

                p.lifetime -= Time.deltaTime;
                if (p.lifetime <= 0f)
                {
                    p.isActive = false;
                    if (p.rect != null) p.rect.gameObject.SetActive(false);
                    continue;
                }

                var t     = p.lifetime / p.maxLifetime;
                var drift = Mathf.Sin(Time.time * p.driftFreq + p.driftPhase) * p.driftAmp;

                p.rect.anchoredPosition += new Vector2(
                    (p.velocity.x + drift) * Time.deltaTime,
                    p.velocity.y * Time.deltaTime
                );

                // Gentle blink for fireflies
                var alpha = p.image.color.a;
                var blink = 1f + Mathf.Sin(Time.time * 3.5f + p.driftPhase) * 0.25f;
                var fadeT = Mathf.Min(t, 1f - t) * 2f; // fade in/out
                var c     = p.image.color;
                p.image.color = new Color(c.r, c.g, c.b, c.a * Mathf.Clamp01(fadeT));
            }
        }

        private void UpdateRainRipples()
        {
            foreach (var r in _ripples)
            {
                if (!r.isActive) continue;

                r.lifetime -= Time.deltaTime;
                if (r.lifetime <= 0f)
                {
                    r.isActive = false;
                    if (r.rect != null) r.rect.gameObject.SetActive(false);
                    continue;
                }

                var t     = 1f - r.lifetime / r.maxLifetime;
                var scale = Mathf.Lerp(0.15f, 1.0f, t);
                r.rect.localScale = new Vector3(scale, scale * 0.5f, 1f);

                var alpha = Mathf.Lerp(0.55f, 0f, t);
                r.image.color = new Color(0.65f, 0.80f, 0.95f, alpha);
            }
        }

        // ─── Helpers ──────────────────────────────────────────────────────────────

        private AmbientParticle GetFreeSlot()
        {
            for (var i = 0; i < MaxAmbient; i++)
            {
                if (!_ambient[i].isActive) return _ambient[i];
            }
            return null;
        }

        private void EnsureParticleGO(AmbientParticle p)
        {
            if (p.rect != null) return;

            var go  = new GameObject("AmbientParticle");
            go.transform.SetParent(ambientCanvas != null ? ambientCanvas : transform as RectTransform, false);
            p.rect  = go.AddComponent<RectTransform>();
            p.image = go.AddComponent<Image>();
            p.image.raycastTarget = false;
        }

        private static Sprite CreateCircleSprite(int size)
        {
            var tex    = new Texture2D(size, size, TextureFormat.RGBA32, false);
            var center = new Vector2(size / 2f, size / 2f);
            var pixels = new Color[size * size];

            for (var y = 0; y < size; y++)
            {
                for (var x = 0; x < size; x++)
                {
                    var dist  = Vector2.Distance(new Vector2(x + 0.5f, y + 0.5f), center);
                    var alpha = 1f - Mathf.Clamp01(dist / (size * 0.48f));
                    alpha     = alpha * alpha;
                    pixels[y * size + x] = new Color(1f, 1f, 1f, alpha);
                }
            }

            tex.SetPixels(pixels);
            tex.Apply();
            return Sprite.Create(tex, new Rect(0, 0, size, size), Vector2.one * 0.5f, size);
        }

        private static Sprite CreateRingSprite()
        {
            const int size = 32;
            var tex    = new Texture2D(size, size, TextureFormat.RGBA32, false);
            var center = new Vector2(size / 2f, size / 2f);
            var pixels = new Color[size * size];

            for (var y = 0; y < size; y++)
            {
                for (var x = 0; x < size; x++)
                {
                    var dist   = Vector2.Distance(new Vector2(x + 0.5f, y + 0.5f), center) / size;
                    var onRing = 1f - Mathf.Clamp01(Mathf.Abs(dist - 0.42f) / 0.10f);
                    pixels[y * size + x] = new Color(1f, 1f, 1f, onRing);
                }
            }

            tex.SetPixels(pixels);
            tex.Apply();
            return Sprite.Create(tex, new Rect(0, 0, size, size), Vector2.one * 0.5f, size);
        }
    }
}
