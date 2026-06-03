using System;
using UnityEngine;

namespace ForestFriendsQuest
{
    /// <summary>
    /// Seasonal sanctuary visual director — transforms the sanctuary's
    /// look and feel with each season change.
    ///
    /// Spring: Soft greens, blossom petals, warm morning light
    /// Summer: Bright yellows, firefly glow, long afternoon light
    /// Autumn: Golden oranges, falling leaves, soft amber tones
    /// Winter: Cool blues, frost shimmer, cozy warm candle glow
    ///
    /// Drives:
    ///   - Background color palette transitions
    ///   - Particle type changes (petals / fireflies / leaves / frost)
    ///   - Ambient light temperature
    ///   - Campfire warmth multiplier
    /// </summary>
    public class SanctuarySeasonalVisuals : MonoBehaviour
    {
        // ─── Events ──────────────────────────────────────────────────────────────

        public event Action<SeasonalVisualProfile> OnSeasonVisualApplied;

        // ─── Dependencies ─────────────────────────────────────────────────────────

        private DynamicSeasonManager       _seasonManager;
        private SanctuaryDecorationSystem  _decorSystem;
        private EmotionalParticleEngine    _particles;
        private DayNightWeatherController  _timeController;
        private ReducedMotionController    _reducedMotion;

        private SeasonalVisualProfile _currentProfile;

        // ─── Lifecycle ───────────────────────────────────────────────────────────

        public void Initialize(
            DynamicSeasonManager       seasons,
            SanctuaryDecorationSystem  decor,
            EmotionalParticleEngine    particles,
            DayNightWeatherController  time,
            ReducedMotionController    reducedMotion)
        {
            _seasonManager = seasons;
            _decorSystem   = decor;
            _particles     = particles;
            _timeController= time;
            _reducedMotion = reducedMotion;

            if (_seasonManager != null)
                _seasonManager.OnSeasonChanged += (prev, next) => ApplySeason(next.ToString().ToLower());

            // Apply current season immediately
            ApplySeason(_seasonManager != null
                ? _seasonManager.CurrentSeason.ToString().ToLower()
                : "spring");
        }

        // ─── Public API ───────────────────────────────────────────────────────────

        public SeasonalVisualProfile GetCurrentProfile() => _currentProfile;

        public void ApplySeason(string season)
        {
            _currentProfile = BuildProfile(season.ToLower());

            // Apply to decoration system
            _decorSystem?.ApplySeasonTheme(season);

            // Apply ambient light through time controller
            _timeController?.SetBiomeTint(_currentProfile.fogColor, _currentProfile.ambientColor);

            // Apply particle theme
            if (_reducedMotion?.IsReducedMotion != true && _particles != null)
            {
                // Trigger a gentle ambient particle burst for the season
                _particles.BurstAt(Vector2.zero, 15, _currentProfile.particleColor);
            }

            OnSeasonVisualApplied?.Invoke(_currentProfile);
            Debug.Log($"[SanctuarySeasonalVisuals] Season applied: {season}");
        }

        // ─── Profile Definitions ──────────────────────────────────────────────────

        private static SeasonalVisualProfile BuildProfile(string season) => season switch
        {
            "spring" => new SeasonalVisualProfile
            {
                season          = "spring",
                displayName     = "Blossom Spring",
                skyColor        = new Color(0.65f, 0.88f, 1.00f),
                ambientColor    = new Color(0.95f, 1.00f, 0.85f),
                fogColor        = new Color(0.85f, 0.95f, 0.80f, 0.15f),
                groundColor     = new Color(0.30f, 0.72f, 0.32f),
                particleColor   = new Color(1.00f, 0.75f, 0.85f),  // pink petals
                campfireMultiplier = 0.9f,
                particleEmoji   = "🌸",
            },
            "summer" => new SeasonalVisualProfile
            {
                season          = "summer",
                displayName     = "Radiant Summer",
                skyColor        = new Color(0.60f, 0.82f, 1.00f),
                ambientColor    = new Color(1.00f, 0.98f, 0.80f),
                fogColor        = new Color(0.80f, 0.90f, 0.70f, 0.10f),
                groundColor     = new Color(0.28f, 0.65f, 0.25f),
                particleColor   = new Color(1.00f, 0.95f, 0.30f),  // golden fireflies
                campfireMultiplier = 0.6f,
                particleEmoji   = "✨",
            },
            "autumn" => new SeasonalVisualProfile
            {
                season          = "autumn",
                displayName     = "Golden Autumn",
                skyColor        = new Color(0.75f, 0.65f, 0.50f),
                ambientColor    = new Color(1.00f, 0.88f, 0.60f),
                fogColor        = new Color(0.75f, 0.60f, 0.40f, 0.25f),
                groundColor     = new Color(0.60f, 0.42f, 0.18f),
                particleColor   = new Color(0.90f, 0.55f, 0.15f),  // amber leaves
                campfireMultiplier = 1.2f,
                particleEmoji   = "🍂",
            },
            "winter" => new SeasonalVisualProfile
            {
                season          = "winter",
                displayName     = "Frosty Winter",
                skyColor        = new Color(0.75f, 0.85f, 1.00f),
                ambientColor    = new Color(0.80f, 0.88f, 1.00f),
                fogColor        = new Color(0.70f, 0.80f, 1.00f, 0.35f),
                groundColor     = new Color(0.85f, 0.90f, 0.95f),
                particleColor   = new Color(0.90f, 0.95f, 1.00f),  // frost sparkles
                campfireMultiplier = 1.5f,
                particleEmoji   = "❄️",
            },
            _ => BuildProfile("spring")
        };
    }

    // ─── Data Types ───────────────────────────────────────────────────────────────

    [Serializable]
    public class SeasonalVisualProfile
    {
        public string season;
        public string displayName;
        public Color  skyColor;
        public Color  ambientColor;
        public Color  fogColor;
        public Color  groundColor;
        public Color  particleColor;
        public float  campfireMultiplier;
        public string particleEmoji;
    }
}
