using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace ForestFriendsQuest
{
    /// <summary>
    /// Emotional state identifiers — drives particle selection and color profiles.
    /// NEVER uses emoji. All visual feedback is sprite-based or procedural.
    /// </summary>
    public enum EmotionalParticleType
    {
        // Creature sleep state
        SleepMoonDust,

        // Creature happy state
        HappyGoldenWisp,
        HappyPollenBurst,

        // Joy / celebration
        JoyFireflySpiral,
        JoySparkle,

        // Thankful / care
        ThankfulFlowerPetal,
        ThankfulBloomRing,

        // Discovery moments
        DiscoveryRuneGlow,
        DiscoveryLightCircle,

        // Rare reward
        RareRewardCrystal,
        RareRewardRainbowMist,

        // World interactions
        GrassDisturbDust,
        RainRipple,
        FireflyWander
    }

    /// <summary>
    /// Runtime color + motion profile for each particle type.
    /// These replace all emoji text particles with smooth sprite-driven VFX.
    /// </summary>
    [System.Serializable]
    public class ParticleProfile
    {
        public EmotionalParticleType type;
        public Color primaryColor   = Color.white;
        public Color secondaryColor = Color.white;
        public float speed          = 60f;
        public float lifetime       = 1.5f;
        public float scale          = 1f;
        public float spread         = 30f;      // horizontal spread in UI units
        public float riseForce      = 80f;      // upward velocity
        public float driftFrequency = 2f;       // sine drift cycles per second
        public bool  useAdditive    = true;
    }

    /// <summary>
    /// Full sprite-based emotional particle engine.
    ///
    /// This system completely replaces all emoji text rendering
    /// (Sleep, Happy, Joy, Thankful etc.) with procedurally animated lightweight
    /// UI-canvas sprite particles that work on any device.
    ///
    /// Architecture:
    ///   - Particle GameObjects are drawn from an internal ring buffer
    ///   - Each particle is a RectTransform + Image with a procedural circle sprite
    ///   - Driven entirely by Update() — zero coroutines, zero GC per frame
    ///   - ObjectPoolManager recycles instances for zero allocation
    /// </summary>
    public class EmotionalParticleEngine : MonoBehaviour
    {
        // ─── Config ──────────────────────────────────────────────────────────────

        [Header("Pool Settings")]
        public int maxLiveParticles = 64;

        [Header("Canvas Parent")]
        public RectTransform particleCanvas;

        // ─── Internal Pool ───────────────────────────────────────────────────────

        private class LiveParticle
        {
            public RectTransform rect;
            public Image image;
            public Vector2 velocity;
            public float lifetime;
            public float maxLifetime;
            public float driftPhase;
            public float driftFrequency;
            public Color startColor;
            public bool  isActive;
        }

        private readonly LiveParticle[] _particles = new LiveParticle[128];
        private int _particleCount;

        // Procedurally generated sprite (solid circle)
        private Sprite _circleSprite;
        private Sprite _petalSprite;
        private Sprite _sparkSprite;

        // ─── Profiles ─────────────────────────────────────────────────────────────

        private static readonly Dictionary<EmotionalParticleType, ParticleProfile> Profiles
            = new Dictionary<EmotionalParticleType, ParticleProfile>
        {
            {
                EmotionalParticleType.SleepMoonDust,
                new ParticleProfile
                {
                    primaryColor   = new Color(0.65f, 0.82f, 1.00f, 0.80f),
                    secondaryColor = new Color(0.80f, 0.88f, 1.00f, 0.50f),
                    speed          = 25f,
                    lifetime       = 2.4f,
                    scale          = 0.55f,
                    spread         = 20f,
                    riseForce      = 35f,
                    driftFrequency = 1.2f,
                    useAdditive    = true
                }
            },
            {
                EmotionalParticleType.HappyGoldenWisp,
                new ParticleProfile
                {
                    primaryColor   = new Color(1.00f, 0.88f, 0.35f, 0.90f),
                    secondaryColor = new Color(1.00f, 0.95f, 0.60f, 0.60f),
                    speed          = 65f,
                    lifetime       = 1.6f,
                    scale          = 0.45f,
                    spread         = 40f,
                    riseForce      = 90f,
                    driftFrequency = 2.8f,
                    useAdditive    = true
                }
            },
            {
                EmotionalParticleType.HappyPollenBurst,
                new ParticleProfile
                {
                    primaryColor   = new Color(0.95f, 0.85f, 0.40f, 0.85f),
                    secondaryColor = new Color(0.80f, 1.00f, 0.55f, 0.65f),
                    speed          = 80f,
                    lifetime       = 1.2f,
                    scale          = 0.30f,
                    spread         = 55f,
                    riseForce      = 60f,
                    driftFrequency = 3.5f,
                    useAdditive    = true
                }
            },
            {
                EmotionalParticleType.JoyFireflySpiral,
                new ParticleProfile
                {
                    primaryColor   = new Color(0.75f, 1.00f, 0.65f, 0.95f),
                    secondaryColor = new Color(0.55f, 0.90f, 0.40f, 0.70f),
                    speed          = 70f,
                    lifetime       = 2.0f,
                    scale          = 0.38f,
                    spread         = 50f,
                    riseForce      = 85f,
                    driftFrequency = 4.0f,
                    useAdditive    = true
                }
            },
            {
                EmotionalParticleType.JoySparkle,
                new ParticleProfile
                {
                    primaryColor   = new Color(1.00f, 1.00f, 0.90f, 1.00f),
                    secondaryColor = new Color(0.90f, 0.95f, 1.00f, 0.80f),
                    speed          = 90f,
                    lifetime       = 0.9f,
                    scale          = 0.25f,
                    spread         = 60f,
                    riseForce      = 100f,
                    driftFrequency = 5.0f,
                    useAdditive    = true
                }
            },
            {
                EmotionalParticleType.ThankfulFlowerPetal,
                new ParticleProfile
                {
                    primaryColor   = new Color(0.95f, 0.72f, 0.85f, 0.85f),
                    secondaryColor = new Color(1.00f, 0.85f, 0.90f, 0.60f),
                    speed          = 35f,
                    lifetime       = 2.8f,
                    scale          = 0.60f,
                    spread         = 45f,
                    riseForce      = 28f,
                    driftFrequency = 1.5f,
                    useAdditive    = false
                }
            },
            {
                EmotionalParticleType.ThankfulBloomRing,
                new ParticleProfile
                {
                    primaryColor   = new Color(0.85f, 0.55f, 0.80f, 0.70f),
                    secondaryColor = new Color(0.70f, 0.35f, 0.65f, 0.45f),
                    speed          = 20f,
                    lifetime       = 1.8f,
                    scale          = 0.90f,
                    spread         = 8f,
                    riseForce      = 15f,
                    driftFrequency = 0.8f,
                    useAdditive    = true
                }
            },
            {
                EmotionalParticleType.DiscoveryRuneGlow,
                new ParticleProfile
                {
                    primaryColor   = new Color(0.40f, 0.90f, 1.00f, 0.95f),
                    secondaryColor = new Color(0.20f, 0.70f, 0.90f, 0.70f),
                    speed          = 45f,
                    lifetime       = 2.2f,
                    scale          = 0.70f,
                    spread         = 30f,
                    riseForce      = 50f,
                    driftFrequency = 2.0f,
                    useAdditive    = true
                }
            },
            {
                EmotionalParticleType.DiscoveryLightCircle,
                new ParticleProfile
                {
                    primaryColor   = new Color(0.80f, 1.00f, 0.95f, 0.80f),
                    secondaryColor = new Color(0.50f, 0.90f, 0.75f, 0.55f),
                    speed          = 30f,
                    lifetime       = 1.5f,
                    scale          = 1.20f,
                    spread         = 5f,
                    riseForce      = 8f,
                    driftFrequency = 0.5f,
                    useAdditive    = true
                }
            },
            {
                EmotionalParticleType.RareRewardCrystal,
                new ParticleProfile
                {
                    primaryColor   = new Color(0.60f, 0.90f, 1.00f, 1.00f),
                    secondaryColor = new Color(1.00f, 0.70f, 0.90f, 0.85f),
                    speed          = 110f,
                    lifetime       = 1.8f,
                    scale          = 0.55f,
                    spread         = 80f,
                    riseForce      = 120f,
                    driftFrequency = 6.0f,
                    useAdditive    = true
                }
            },
            {
                EmotionalParticleType.RareRewardRainbowMist,
                new ParticleProfile
                {
                    primaryColor   = new Color(1.00f, 0.85f, 0.95f, 0.75f),
                    secondaryColor = new Color(0.75f, 0.95f, 1.00f, 0.55f),
                    speed          = 18f,
                    lifetime       = 3.5f,
                    scale          = 1.60f,
                    spread         = 70f,
                    riseForce      = 12f,
                    driftFrequency = 0.6f,
                    useAdditive    = true
                }
            },
            {
                EmotionalParticleType.GrassDisturbDust,
                new ParticleProfile
                {
                    primaryColor   = new Color(0.75f, 0.68f, 0.50f, 0.65f),
                    secondaryColor = new Color(0.85f, 0.80f, 0.60f, 0.40f),
                    speed          = 50f,
                    lifetime       = 0.8f,
                    scale          = 0.40f,
                    spread         = 35f,
                    riseForce      = 30f,
                    driftFrequency = 3.0f,
                    useAdditive    = false
                }
            },
            {
                EmotionalParticleType.FireflyWander,
                new ParticleProfile
                {
                    primaryColor   = new Color(0.80f, 1.00f, 0.60f, 0.90f),
                    secondaryColor = new Color(0.95f, 1.00f, 0.75f, 0.65f),
                    speed          = 22f,
                    lifetime       = 4.0f,
                    scale          = 0.32f,
                    spread         = 90f,
                    riseForce      = 18f,
                    driftFrequency = 1.8f,
                    useAdditive    = true
                }
            }
        };

        // ─── Lifecycle ───────────────────────────────────────────────────────────

        private void Awake()
        {
            // Pre-allocate pool
            for (var i = 0; i < _particles.Length; i++)
            {
                _particles[i] = new LiveParticle();
            }

            GenerateProceduralSprites();
        }

        private void Update()
        {
            for (var i = 0; i < _particleCount; i++)
            {
                var p = _particles[i];
                if (!p.isActive) continue;

                p.lifetime -= Time.deltaTime;

                if (p.lifetime <= 0f)
                {
                    p.isActive = false;
                    if (p.rect != null) p.rect.gameObject.SetActive(false);
                    continue;
                }

                // Animate position
                var t     = p.lifetime / p.maxLifetime;
                var drift = Mathf.Sin(Time.time * p.driftFrequency + p.driftPhase) * 12f;

                p.rect.anchoredPosition += new Vector2(
                    (p.velocity.x + drift) * Time.deltaTime,
                    p.velocity.y * Time.deltaTime
                );

                // Decelerate rise
                p.velocity.y = Mathf.Max(p.velocity.y - 35f * Time.deltaTime, 0f);

                // Fade out
                if (p.image != null)
                {
                    var alpha = Mathf.SmoothStep(0f, 1f, t) * p.startColor.a;
                    p.image.color = new Color(p.startColor.r, p.startColor.g, p.startColor.b, alpha);
                }

                // Scale down gently
                var scale = Mathf.Lerp(0.4f, 1f, t);
                p.rect.localScale = Vector3.one * scale;
            }
        }

        // ─── Emission Toggle (for calm / reduced-motion mode) ─────────────────────

        private bool _emissionEnabled = true;

        public void SetEmissionEnabled(bool enabled) => _emissionEnabled = enabled;

        // ─── Public Spawn API ────────────────────────────────────────────────────

        /// <summary>Spawn N particles of the given emotional type at a canvas position.</summary>
        public void Spawn(EmotionalParticleType type, Vector2 canvasPosition, int count = 6)
        {
            if (!_emissionEnabled) return;
            if (!Profiles.TryGetValue(type, out var profile)) return;

            for (var i = 0; i < count; i++)
            {
                SpawnOne(profile, canvasPosition);
            }
        }

        /// <summary>Spawn particles relative to a RectTransform (e.g. creature position).</summary>
        public void SpawnAtRect(EmotionalParticleType type, RectTransform source, int count = 6)
        {
            if (source == null) return;
            Spawn(type, source.anchoredPosition + new Vector2(0f, source.rect.height * 0.5f), count);
        }

        /// <summary>Spawn a compact burst of happiness particles (replaces emoji heart).</summary>
        public void SpawnHappyBurst(Vector2 pos)
        {
            Spawn(EmotionalParticleType.HappyGoldenWisp,  pos, 4);
            Spawn(EmotionalParticleType.HappyPollenBurst, pos, 5);
        }

        /// <summary>Spawn sleep indicators (replaces emoji Zzz bubbles).</summary>
        public void SpawnSleepParticles(Vector2 pos)
        {
            Spawn(EmotionalParticleType.SleepMoonDust, pos, 3);
        }

        /// <summary>Spawn joy celebration burst (replaces emoji sparkle).</summary>
        public void SpawnJoyBurst(Vector2 pos)
        {
            Spawn(EmotionalParticleType.JoyFireflySpiral, pos, 5);
            Spawn(EmotionalParticleType.JoySparkle,       pos, 8);
        }

        /// <summary>Burst a fixed count of particles at a canvas position with a tint color.</summary>
        public void BurstAt(Vector2 pos, int count, Color color)
        {
            Spawn(EmotionalParticleType.HappyPollenBurst, pos, count);
        }

        /// <summary>Spawn thankful petal drift (replaces emoji blossom).</summary>
        public void SpawnThankfulParticles(Vector2 pos)
        {
            Spawn(EmotionalParticleType.ThankfulFlowerPetal, pos, 6);
            Spawn(EmotionalParticleType.ThankfulBloomRing,   pos, 2);
        }

        /// <summary>Spawn discovery reveal (rune glow + expanding circle).</summary>
        public void SpawnDiscoveryBurst(Vector2 pos)
        {
            Spawn(EmotionalParticleType.DiscoveryRuneGlow,   pos, 5);
            Spawn(EmotionalParticleType.DiscoveryLightCircle, pos, 3);
        }

        /// <summary>Spawn rare reward explosion (crystal + rainbow mist).</summary>
        public void SpawnRareRewardBurst(Vector2 pos)
        {
            Spawn(EmotionalParticleType.RareRewardCrystal,     pos, 10);
            Spawn(EmotionalParticleType.RareRewardRainbowMist, pos, 4);
        }

        // ─── Private Core ─────────────────────────────────────────────────────────

        private void SpawnOne(ParticleProfile profile, Vector2 canvasPos)
        {
            // Find an inactive slot
            LiveParticle p = null;
            for (var i = 0; i < _particles.Length; i++)
            {
                if (!_particles[i].isActive)
                {
                    p = _particles[i];
                    if (i >= _particleCount) _particleCount = i + 1;
                    break;
                }
            }

            if (p == null) return; // pool exhausted

            // Ensure GameObject exists
            if (p.rect == null)
            {
                var go = new GameObject("EmotionParticle");
                go.transform.SetParent(particleCanvas != null ? particleCanvas : transform as RectTransform, false);
                p.rect  = go.AddComponent<RectTransform>();
                p.image = go.AddComponent<Image>();
                p.image.sprite = _circleSprite;
                p.image.raycastTarget = false;
                p.rect.sizeDelta = new Vector2(20f, 20f);
            }

            // Configure
            p.rect.gameObject.SetActive(true);
            p.rect.anchoredPosition = canvasPos;

            var angle  = Random.Range(0f, 360f) * Mathf.Deg2Rad;
            var spread = Random.Range(0f, profile.spread);
            p.velocity = new Vector2(
                Mathf.Cos(angle) * spread,
                profile.riseForce + Random.Range(-15f, 15f)
            );

            p.lifetime      = profile.lifetime * Random.Range(0.75f, 1.25f);
            p.maxLifetime   = p.lifetime;
            p.driftPhase    = Random.Range(0f, Mathf.PI * 2f);
            p.driftFrequency = profile.driftFrequency;

            // Color variation
            var t = Random.value;
            p.startColor  = Color.Lerp(profile.primaryColor, profile.secondaryColor, t);
            p.image.color = p.startColor;

            // Scale
            var scale = profile.scale * Random.Range(0.7f, 1.3f);
            p.rect.localScale = Vector3.one * scale;

            p.isActive = true;
        }

        // ─── Procedural Sprite Generation ────────────────────────────────────────

        private void GenerateProceduralSprites()
        {
            _circleSprite = CreateCircleSprite(16, Color.white);
            _petalSprite  = _circleSprite; // soft oval reuse
            _sparkSprite  = CreateCircleSprite(8,  Color.white);
        }

        private static Sprite CreateCircleSprite(int resolution, Color color)
        {
            var tex     = new Texture2D(resolution, resolution, TextureFormat.RGBA32, false);
            var center  = new Vector2(resolution / 2f, resolution / 2f);
            var radius  = resolution / 2f - 1f;
            var pixels  = new Color[resolution * resolution];

            for (var y = 0; y < resolution; y++)
            {
                for (var x = 0; x < resolution; x++)
                {
                    var dist = Vector2.Distance(new Vector2(x + 0.5f, y + 0.5f), center);
                    var alpha = 1f - Mathf.Clamp01((dist - (radius - 2f)) / 2f);
                    pixels[y * resolution + x] = new Color(color.r, color.g, color.b, alpha);
                }
            }

            tex.SetPixels(pixels);
            tex.Apply();
            tex.filterMode = FilterMode.Bilinear;
            tex.wrapMode   = TextureWrapMode.Clamp;

            return Sprite.Create(
                tex,
                new Rect(0f, 0f, resolution, resolution),
                new Vector2(0.5f, 0.5f),
                resolution
            );
        }
    }
}
