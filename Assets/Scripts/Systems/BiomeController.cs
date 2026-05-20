using System;
using System.Collections.Generic;
using UnityEngine;

namespace ForestFriendsQuest
{
    [Serializable]
    public class BiomeProfile
    {
        public string    regionId;
        public string    displayName;

        // Ambient audio
        public string    ambientTrackId;       // key into ProceduralAudioSystem
        public float     musicTempo;           // BPM hint for procedural layers
        public string[]  creatureSoundIds;

        // Color palette (RGB 0-1)
        public Color fogColor;
        public Color ambientLightColor;
        public Color groundTintColor;
        public Color skyTintColor;

        // Particle density (0-1 scale)
        public float pollenDensity;
        public float fireflydensity;
        public float mistDensity;
        public float leafDensity;

        // Resident creatures
        public string[] residentCreatureIds;

        // Weather tendencies (0-1 probability weights)
        public float weatherSunnyWeight;
        public float weatherMistyWeight;
        public float weatherWindyWeight;
        public float weatherSnowyWeight;
        public float weatherStormyWeight;
    }

    /// <summary>
    /// Owns BiomeProfile definitions for all 10 regions and provides the
    /// current biome's settings to other systems on zone transitions.
    ///
    /// Driven by WorldStateManager.OnRegionUnlocked and ForestQuestApp zone tap.
    /// Pushes settings to DayNightWeatherController and ProceduralAudioSystem.
    /// </summary>
    public class BiomeController : MonoBehaviour
    {
        private DayNightWeatherController _weather;
        private ProceduralAudioSystem     _audio;

        public event Action<BiomeProfile> OnBiomeEntered;

        private readonly Dictionary<string, BiomeProfile> _biomes =
            new Dictionary<string, BiomeProfile>();

        private BiomeProfile _currentBiome;

        // ─── Lifecycle ───────────────────────────────────────────────────────────

        public void Initialize(
            DayNightWeatherController weather,
            ProceduralAudioSystem audio)
        {
            _weather = weather;
            _audio   = audio;
            BuildBiomeProfiles();
        }

        // ─── Public API ───────────────────────────────────────────────────────────

        public BiomeProfile GetCurrentBiome() => _currentBiome;

        public BiomeProfile GetBiome(string regionId)
        {
            _biomes.TryGetValue(regionId, out var p);
            return p;
        }

        /// <summary>Called when player enters a new zone.</summary>
        public void EnterBiome(string regionId)
        {
            if (!_biomes.TryGetValue(regionId, out var profile)) return;
            if (_currentBiome?.regionId == regionId) return;

            _currentBiome = profile;

            _weather?.SetBiomeTint(profile.fogColor, profile.ambientLightColor);
            _audio?.SetBiomeTrack(profile.ambientTrackId, profile.musicTempo);

            OnBiomeEntered?.Invoke(profile);
            Debug.Log($"[BiomeController] Entered biome: {profile.displayName}");
        }

        /// <summary>Sample a weather state based on this biome's tendency weights.</summary>
        public WeatherState SampleWeather(string regionId)
        {
            if (!_biomes.TryGetValue(regionId, out var p))
                return WeatherState.Sunny;

            var roll = UnityEngine.Random.value;
            var cum  = 0f;

            cum += p.weatherSunnyWeight;  if (roll <= cum) return WeatherState.Sunny;
            cum += p.weatherMistyWeight;  if (roll <= cum) return WeatherState.Misty;
            cum += p.weatherWindyWeight;  if (roll <= cum) return WeatherState.Windy;
            cum += p.weatherSnowyWeight;  if (roll <= cum) return WeatherState.Snowy;
            return WeatherState.Stormy;
        }

        // ─── Biome Profile Definitions ────────────────────────────────────────────

        private void BuildBiomeProfiles()
        {
            Add(new BiomeProfile
            {
                regionId            = "fern-trail",
                displayName         = "Whispering Meadow",
                ambientTrackId      = "meadow_morning",
                musicTempo          = 72f,
                creatureSoundIds    = new[] { "pip_chirp", "bird_warble", "bee_buzz" },
                fogColor            = new Color(0.85f, 0.95f, 0.80f, 0.15f),
                ambientLightColor   = new Color(0.95f, 1.00f, 0.85f, 1.00f),
                groundTintColor     = new Color(0.30f, 0.62f, 0.28f, 1.00f),
                skyTintColor        = new Color(0.65f, 0.85f, 1.00f, 1.00f),
                pollenDensity       = 0.7f,
                fireflydensity      = 0.1f,
                mistDensity         = 0.2f,
                leafDensity         = 0.3f,
                residentCreatureIds = new[] { "pip", "mimi" },
                weatherSunnyWeight  = 0.70f,
                weatherMistyWeight  = 0.20f,
                weatherWindyWeight  = 0.08f,
                weatherSnowyWeight  = 0.00f,
                weatherStormyWeight = 0.02f
            });

            Add(new BiomeProfile
            {
                regionId            = "firefly-hollow",
                displayName         = "Firefly Hollow",
                ambientTrackId      = "hollow_dusk",
                musicTempo          = 60f,
                creatureSoundIds    = new[] { "firefly_glow", "frog_croak" },
                fogColor            = new Color(0.45f, 0.62f, 0.35f, 0.25f),
                ambientLightColor   = new Color(0.75f, 0.90f, 0.60f, 1.00f),
                groundTintColor     = new Color(0.20f, 0.45f, 0.22f, 1.00f),
                skyTintColor        = new Color(0.25f, 0.40f, 0.55f, 1.00f),
                pollenDensity       = 0.4f,
                fireflydensity      = 0.8f,
                mistDensity         = 0.5f,
                leafDensity         = 0.4f,
                residentCreatureIds = new[] { "luma" },
                weatherSunnyWeight  = 0.40f,
                weatherMistyWeight  = 0.45f,
                weatherWindyWeight  = 0.10f,
                weatherSnowyWeight  = 0.00f,
                weatherStormyWeight = 0.05f
            });

            Add(new BiomeProfile
            {
                regionId            = "river-bend",
                displayName         = "River Bend",
                ambientTrackId      = "river_flow",
                musicTempo          = 68f,
                creatureSoundIds    = new[] { "tomo_splash", "water_trickle" },
                fogColor            = new Color(0.70f, 0.85f, 0.90f, 0.20f),
                ambientLightColor   = new Color(0.85f, 0.92f, 1.00f, 1.00f),
                groundTintColor     = new Color(0.22f, 0.55f, 0.55f, 1.00f),
                skyTintColor        = new Color(0.60f, 0.78f, 1.00f, 1.00f),
                pollenDensity       = 0.3f,
                fireflydensity      = 0.2f,
                mistDensity         = 0.6f,
                leafDensity         = 0.2f,
                residentCreatureIds = new[] { "tomo", "pip" },
                weatherSunnyWeight  = 0.50f,
                weatherMistyWeight  = 0.35f,
                weatherWindyWeight  = 0.12f,
                weatherSnowyWeight  = 0.00f,
                weatherStormyWeight = 0.03f
            });

            Add(new BiomeProfile
            {
                regionId            = "moonlit-creek",
                displayName         = "Moonlit Creek",
                ambientTrackId      = "moonlit_ripple",
                musicTempo          = 55f,
                creatureSoundIds    = new[] { "night_cricket", "owl_distant" },
                fogColor            = new Color(0.50f, 0.55f, 0.80f, 0.35f),
                ambientLightColor   = new Color(0.70f, 0.75f, 1.00f, 1.00f),
                groundTintColor     = new Color(0.18f, 0.30f, 0.50f, 1.00f),
                skyTintColor        = new Color(0.15f, 0.20f, 0.45f, 1.00f),
                pollenDensity       = 0.2f,
                fireflydensity      = 0.6f,
                mistDensity         = 0.7f,
                leafDensity         = 0.1f,
                residentCreatureIds = new[] { "luma", "tomo" },
                weatherSunnyWeight  = 0.15f,
                weatherMistyWeight  = 0.60f,
                weatherWindyWeight  = 0.15f,
                weatherSnowyWeight  = 0.05f,
                weatherStormyWeight = 0.05f
            });

            Add(new BiomeProfile
            {
                regionId            = "elderwood-grove",
                displayName         = "Elderwood Grove",
                ambientTrackId      = "elderwood_deep",
                musicTempo          = 50f,
                creatureSoundIds    = new[] { "nori_step", "branch_creak" },
                fogColor            = new Color(0.55f, 0.72f, 0.45f, 0.30f),
                ambientLightColor   = new Color(0.80f, 0.88f, 0.65f, 1.00f),
                groundTintColor     = new Color(0.28f, 0.50f, 0.20f, 1.00f),
                skyTintColor        = new Color(0.35f, 0.55f, 0.30f, 1.00f),
                pollenDensity       = 0.6f,
                fireflydensity      = 0.3f,
                mistDensity         = 0.4f,
                leafDensity         = 0.9f,
                residentCreatureIds = new[] { "nori", "mimi" },
                weatherSunnyWeight  = 0.30f,
                weatherMistyWeight  = 0.30f,
                weatherWindyWeight  = 0.30f,
                weatherSnowyWeight  = 0.05f,
                weatherStormyWeight = 0.05f
            });

            Add(new BiomeProfile
            {
                regionId            = "crystal-caverns",
                displayName         = "Crystal Caverns",
                ambientTrackId      = "cavern_resonance",
                musicTempo          = 45f,
                creatureSoundIds    = new[] { "crystal_ping", "echo_drip" },
                fogColor            = new Color(0.60f, 0.80f, 1.00f, 0.40f),
                ambientLightColor   = new Color(0.65f, 0.85f, 1.00f, 1.00f),
                groundTintColor     = new Color(0.30f, 0.45f, 0.70f, 1.00f),
                skyTintColor        = new Color(0.20f, 0.30f, 0.60f, 1.00f),
                pollenDensity       = 0.0f,
                fireflydensity      = 0.4f,
                mistDensity         = 0.3f,
                leafDensity         = 0.0f,
                residentCreatureIds = new[] { "sol" },
                weatherSunnyWeight  = 0.00f,
                weatherMistyWeight  = 0.60f,
                weatherWindyWeight  = 0.10f,
                weatherSnowyWeight  = 0.20f,
                weatherStormyWeight = 0.10f
            });

            Add(new BiomeProfile
            {
                regionId            = "forgotten-ruins",
                displayName         = "Forgotten Ruins",
                ambientTrackId      = "ruins_ancient",
                musicTempo          = 40f,
                creatureSoundIds    = new[] { "rune_hum", "stone_shift" },
                fogColor            = new Color(0.70f, 0.60f, 0.40f, 0.35f),
                ambientLightColor   = new Color(0.88f, 0.80f, 0.60f, 1.00f),
                groundTintColor     = new Color(0.55f, 0.45f, 0.25f, 1.00f),
                skyTintColor        = new Color(0.45f, 0.38f, 0.22f, 1.00f),
                pollenDensity       = 0.1f,
                fireflydensity      = 0.2f,
                mistDensity         = 0.5f,
                leafDensity         = 0.1f,
                residentCreatureIds = new[] { "sol", "nori" },
                weatherSunnyWeight  = 0.20f,
                weatherMistyWeight  = 0.40f,
                weatherWindyWeight  = 0.25f,
                weatherSnowyWeight  = 0.10f,
                weatherStormyWeight = 0.05f
            });

            Add(new BiomeProfile
            {
                regionId            = "firefly-marsh",
                displayName         = "Firefly Marsh",
                ambientTrackId      = "marsh_night",
                musicTempo          = 52f,
                creatureSoundIds    = new[] { "marsh_croak", "reed_whistle" },
                fogColor            = new Color(0.40f, 0.60f, 0.40f, 0.40f),
                ambientLightColor   = new Color(0.70f, 0.85f, 0.65f, 1.00f),
                groundTintColor     = new Color(0.15f, 0.35f, 0.20f, 1.00f),
                skyTintColor        = new Color(0.20f, 0.35f, 0.25f, 1.00f),
                pollenDensity       = 0.2f,
                fireflydensity      = 1.0f,
                mistDensity         = 0.8f,
                leafDensity         = 0.2f,
                residentCreatureIds = new[] { "luma" },
                weatherSunnyWeight  = 0.10f,
                weatherMistyWeight  = 0.70f,
                weatherWindyWeight  = 0.10f,
                weatherSnowyWeight  = 0.00f,
                weatherStormyWeight = 0.10f
            });

            Add(new BiomeProfile
            {
                regionId            = "ancient-observatory",
                displayName         = "Ancient Observatory",
                ambientTrackId      = "observatory_cosmic",
                musicTempo          = 48f,
                creatureSoundIds    = new[] { "sol_hoot", "star_chime" },
                fogColor            = new Color(0.20f, 0.20f, 0.50f, 0.50f),
                ambientLightColor   = new Color(0.60f, 0.65f, 1.00f, 1.00f),
                groundTintColor     = new Color(0.22f, 0.22f, 0.45f, 1.00f),
                skyTintColor        = new Color(0.05f, 0.05f, 0.25f, 1.00f),
                pollenDensity       = 0.0f,
                fireflydensity      = 0.9f,
                mistDensity         = 0.2f,
                leafDensity         = 0.0f,
                residentCreatureIds = new[] { "sol" },
                weatherSunnyWeight  = 0.00f,
                weatherMistyWeight  = 0.20f,
                weatherWindyWeight  = 0.20f,
                weatherSnowyWeight  = 0.40f,
                weatherStormyWeight = 0.20f
            });

            Add(new BiomeProfile
            {
                regionId            = "skyroot-canopy",
                displayName         = "Skyroot Canopy",
                ambientTrackId      = "canopy_wind",
                musicTempo          = 76f,
                creatureSoundIds    = new[] { "wind_leaves", "mimi_song" },
                fogColor            = new Color(0.75f, 0.90f, 0.70f, 0.20f),
                ambientLightColor   = new Color(0.95f, 1.00f, 0.80f, 1.00f),
                groundTintColor     = new Color(0.30f, 0.65f, 0.25f, 1.00f),
                skyTintColor        = new Color(0.55f, 0.80f, 1.00f, 1.00f),
                pollenDensity       = 0.9f,
                fireflydensity      = 0.1f,
                mistDensity         = 0.1f,
                leafDensity         = 1.0f,
                residentCreatureIds = new[] { "mimi", "pip" },
                weatherSunnyWeight  = 0.60f,
                weatherMistyWeight  = 0.10f,
                weatherWindyWeight  = 0.25f,
                weatherSnowyWeight  = 0.03f,
                weatherStormyWeight = 0.02f
            });
        }

        private void Add(BiomeProfile profile) => _biomes[profile.regionId] = profile;
    }
}
