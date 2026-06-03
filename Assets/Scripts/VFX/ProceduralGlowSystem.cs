using UnityEngine;
using UnityEngine.UI;

namespace ForestFriendsQuest
{
    /// <summary>
    /// GPU-driven procedural glow system using Material property blocks and
    /// runtime emissive color pulses. Drives creature auras, puzzle highlights,
    /// discovery rings, and screen-space flash events — entirely without emoji.
    /// </summary>
    public class ProceduralGlowSystem : MonoBehaviour
    {
        [Header("Glow Canvas")]
        public RectTransform glowCanvas;

        // ─── Glow Ring Pool ───────────────────────────────────────────────────────

        private class GlowRing
        {
            public RectTransform rect;
            public Image         image;
            public float         lifetime;
            public float         maxLifetime;
            public Color         color;
            public float         expandSpeed;
            public bool          isActive;
        }

        private const int MaxRings = 16;
        private readonly GlowRing[] _rings = new GlowRing[MaxRings];

        // ─── Screen Flash State ───────────────────────────────────────────────────

        private Image  _screenFlash;
        private float  _flashTimer;
        private float  _flashDuration;
        private Color  _flashColor;

        // ─── Creature Pulse State ─────────────────────────────────────────────────

        private class CreaturePulse
        {
            public RectTransform target;
            public RectTransform rect;
            public Image         glowImage;
            public float         timer;
            public float         duration;
            public Color         color;
            public bool          isActive;
        }

        private const int MaxPulses = 8;
        private readonly CreaturePulse[] _pulses = new CreaturePulse[MaxPulses];

        // ─── Lifecycle ───────────────────────────────────────────────────────────

        private void Awake()
        {
            // Pre-allocate glow rings
            for (var i = 0; i < MaxRings; i++)
            {
                _rings[i] = new GlowRing();
            }

            // Pre-allocate creature pulse overlays
            for (var i = 0; i < MaxPulses; i++)
            {
                _pulses[i] = new CreaturePulse();
            }

            // Screen flash overlay
            BuildScreenFlash();
        }

        private void Update()
        {
            UpdateGlowRings();
            UpdateCreaturePulses();
            UpdateScreenFlash();
        }

        // ─── Public API ───────────────────────────────────────────────────────────

        /// <summary>Pulse a soft aura glow around a creature's RectTransform.</summary>
        public void PulseGlow(RectTransform target, Color color, float duration = 0.8f)
        {
            CreaturePulse pulse = null;
            for (var i = 0; i < MaxPulses; i++)
            {
                if (!_pulses[i].isActive)
                {
                    pulse = _pulses[i];
                    break;
                }
            }

            if (pulse == null) return;

            // Reuse or create glow image
            if (pulse.glowImage == null)
            {
                var go      = new GameObject("GlowPulse");
                go.transform.SetParent(glowCanvas != null ? glowCanvas : transform as RectTransform, false);
                var rt      = go.AddComponent<RectTransform>();
                rt.sizeDelta = new Vector2(160f, 160f);
                var img     = go.AddComponent<Image>();
                img.sprite  = CreateSoftCircleSprite();
                img.raycastTarget = false;
                pulse.glowImage = img;
                pulse.rect      = rt;
            }

            pulse.target      = target;
            pulse.timer       = 0f;
            pulse.duration    = duration;
            pulse.color       = color;
            pulse.glowImage.gameObject.SetActive(true);
            pulse.isActive    = true;
        }

        /// <summary>Spawn an expanding discovery ring (light circle expand effect).</summary>
        public void SpawnDiscoveryRing(Vector2 canvasPos)
        {
            SpawnGlowRing(canvasPos, new Color(0.60f, 0.95f, 1.00f, 0.70f), 0.80f, 280f);
        }

        /// <summary>Crystal explosion burst — multiple expanding rings in sequence.</summary>
        public void SpawnCrystalBurst(Vector2 canvasPos)
        {
            SpawnGlowRing(canvasPos, new Color(0.90f, 0.80f, 1.00f, 0.80f), 0.55f, 360f);
            SpawnGlowRing(canvasPos, new Color(0.70f, 0.95f, 1.00f, 0.60f), 0.75f, 220f);
            SpawnGlowRing(canvasPos, new Color(1.00f, 0.90f, 0.80f, 0.50f), 0.95f, 180f);
        }

        /// <summary>Full screen color flash (soft — used on puzzle solve).</summary>
        public void PulseScreen(Color color, float duration = 0.5f)
        {
            if (_screenFlash == null) return;
            _flashColor    = new Color(color.r, color.g, color.b, 0.18f);
            _flashDuration = duration;
            _flashTimer    = duration;
            _screenFlash.color = _flashColor;
            _screenFlash.gameObject.SetActive(true);
        }

        // ─── Update Methods ───────────────────────────────────────────────────────

        private void UpdateGlowRings()
        {
            foreach (var ring in _rings)
            {
                if (!ring.isActive) continue;

                ring.lifetime -= Time.deltaTime;
                if (ring.lifetime <= 0f)
                {
                    ring.isActive = false;
                    if (ring.rect != null) ring.rect.gameObject.SetActive(false);
                    continue;
                }

                var t     = 1f - ring.lifetime / ring.maxLifetime;
                var scale = Mathf.Lerp(0.2f, 1.0f, t);
                ring.rect.localScale = Vector3.one * scale;

                var alpha = Mathf.Lerp(ring.color.a, 0f, t * t);
                ring.image.color = new Color(ring.color.r, ring.color.g, ring.color.b, alpha);
            }
        }

        private void UpdateCreaturePulses()
        {
            foreach (var pulse in _pulses)
            {
                if (!pulse.isActive) continue;

                pulse.timer += Time.deltaTime;
                var t = pulse.timer / pulse.duration;

                if (t >= 1f)
                {
                    pulse.isActive = false;
                    if (pulse.glowImage != null) pulse.glowImage.gameObject.SetActive(false);
                    continue;
                }

                // Follow creature position
                if (pulse.target != null && pulse.rect != null)
                {
                    pulse.rect.anchoredPosition = pulse.target.anchoredPosition;
                }

                // Pulse in/out
                var alpha = Mathf.Sin(t * Mathf.PI) * 0.7f;
                var scale = 1f + Mathf.Sin(t * Mathf.PI * 2f) * 0.15f;
                pulse.glowImage.color = new Color(pulse.color.r, pulse.color.g, pulse.color.b, alpha);
                pulse.rect.localScale = Vector3.one * scale;
            }
        }

        private void UpdateScreenFlash()
        {
            if (_flashTimer <= 0f || _screenFlash == null) return;

            _flashTimer -= Time.deltaTime;
            var alpha = Mathf.Clamp01(_flashTimer / _flashDuration) * _flashColor.a;
            _screenFlash.color = new Color(_flashColor.r, _flashColor.g, _flashColor.b, alpha);

            if (_flashTimer <= 0f)
            {
                _screenFlash.gameObject.SetActive(false);
            }
        }

        // ─── Private Helpers ──────────────────────────────────────────────────────

        private void SpawnGlowRing(Vector2 canvasPos, Color color, float lifetime, float targetSize)
        {
            GlowRing ring = null;
            for (var i = 0; i < MaxRings; i++)
            {
                if (!_rings[i].isActive)
                {
                    ring = _rings[i];
                    break;
                }
            }

            if (ring == null) return;

            if (ring.rect == null)
            {
                var go  = new GameObject("GlowRing");
                go.transform.SetParent(glowCanvas != null ? glowCanvas : transform as RectTransform, false);
                ring.rect  = go.AddComponent<RectTransform>();
                ring.image = go.AddComponent<Image>();
                ring.image.sprite = CreateSoftRingSprite();
                ring.image.raycastTarget = false;
            }

            ring.rect.gameObject.SetActive(true);
            ring.rect.anchoredPosition = canvasPos;
            ring.rect.sizeDelta        = new Vector2(targetSize, targetSize);
            ring.rect.localScale       = Vector3.one * 0.2f;
            ring.color                 = color;
            ring.image.color           = color;
            ring.lifetime              = lifetime;
            ring.maxLifetime           = lifetime;
            ring.isActive              = true;
        }

        private void BuildScreenFlash()
        {
            var root   = glowCanvas != null ? glowCanvas : transform as RectTransform;
            var go     = new GameObject("ScreenFlash");
            go.transform.SetParent(root, false);
            var rt     = go.AddComponent<RectTransform>();
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.sizeDelta = Vector2.zero;
            _screenFlash = go.AddComponent<Image>();
            _screenFlash.color = Color.clear;
            _screenFlash.raycastTarget = false;
            go.SetActive(false);
        }

        private static Sprite CreateSoftCircleSprite()
        {
            const int size = 64;
            var tex    = new Texture2D(size, size, TextureFormat.RGBA32, false);
            var center = new Vector2(size / 2f, size / 2f);
            var pixels = new Color[size * size];

            for (var y = 0; y < size; y++)
            {
                for (var x = 0; x < size; x++)
                {
                    var dist  = Vector2.Distance(new Vector2(x + 0.5f, y + 0.5f), center);
                    var alpha = 1f - Mathf.Clamp01(dist / (size * 0.45f));
                    alpha     = alpha * alpha; // softer falloff
                    pixels[y * size + x] = new Color(1f, 1f, 1f, alpha);
                }
            }

            tex.SetPixels(pixels);
            tex.Apply();
            return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), size);
        }

        private static Sprite CreateSoftRingSprite()
        {
            const int size     = 64;
            const float ringR  = 0.45f;
            const float ringW  = 0.12f;
            var tex    = new Texture2D(size, size, TextureFormat.RGBA32, false);
            var center = new Vector2(size / 2f, size / 2f);
            var pixels = new Color[size * size];

            for (var y = 0; y < size; y++)
            {
                for (var x = 0; x < size; x++)
                {
                    var dist    = Vector2.Distance(new Vector2(x + 0.5f, y + 0.5f), center) / size;
                    var onRing  = 1f - Mathf.Clamp01(Mathf.Abs(dist - ringR) / ringW);
                    pixels[y * size + x] = new Color(1f, 1f, 1f, onRing);
                }
            }

            tex.SetPixels(pixels);
            tex.Apply();
            return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), size);
        }
    }
}
