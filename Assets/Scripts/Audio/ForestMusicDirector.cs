using UnityEngine;

namespace ForestFriendsQuest
{
    /// <summary>
    /// Central music director for Forest Friends Quest.
    ///
    /// Responsibilities:
    ///   - Maps UIState / active zone → appropriate music context
    ///   - Drives AdaptiveMusicTransition for smooth crossfades
    ///   - Coordinates DynamicAmbientMixer biome transitions
    ///   - Responds to world events (boss entrance, region unlock, puzzle complete)
    ///   - Respects master volume and music-enabled settings
    ///
    /// Architecture: single MonoBehaviour, receives events via public methods.
    /// UIStateController should call SetContext() whenever state changes.
    /// WorldStateManager.OnRegionUnlocked should call OnRegionUnlocked().
    /// </summary>
    public class ForestMusicDirector : MonoBehaviour
    {
        private AdaptiveMusicTransition _transition;
        private DynamicAmbientMixer     _ambient;
        private AudioAssetLibrary       _library;

        private bool   _musicEnabled = true;
        private float  _masterVolume = 0.7f;
        private string _currentContext;

        // ─── Setup ────────────────────────────────────────────────────────────────

        public void Initialize(AudioAssetLibrary library)
        {
            _library = library;

            _transition = gameObject.AddComponent<AdaptiveMusicTransition>();
            _transition.Initialize();

            _ambient = gameObject.AddComponent<DynamicAmbientMixer>();
            _ambient.Initialize(library);

            // Default: start in Meadow context
            SetContext("explore_meadow");
        }

        // ─── Context Changes ──────────────────────────────────────────────────────

        /// <summary>
        /// Called by UIStateController / ForestUIRouter when scene changes.
        /// contextId examples: "explore_meadow", "zone_firefly-hollow", "puzzle", "boss_mirewick",
        ///                     "sanctuary", "worldmap", "ritual", "parents"
        /// </summary>
        public void SetContext(string contextId)
        {
            if (contextId == _currentContext || !_musicEnabled) return;
            _currentContext = contextId;

            var theme = GetThemeForContext(contextId);
            if (theme == null) return;

            _transition.CrossFade(theme, 1.5f, _masterVolume);

            // Sync ambient to biome
            var biome = ExtractBiomeFromContext(contextId);
            if (biome != null)
                _ambient.SetBiome(biome, 2f);
        }

        public void SetZone(string zoneId)
        {
            SetContext($"zone_{zoneId}");
        }

        // ─── Event Responses ──────────────────────────────────────────────────────

        public void OnPuzzleStart()
        {
            // Soften ambient during puzzle focus
            _ambient.SetMasterVolume(_masterVolume * 0.35f);
        }

        public void OnPuzzleEnd(bool success)
        {
            _ambient.SetMasterVolume(_masterVolume);
            if (success)
            {
                var sting = _library?.GetSFX("puzzle_complete");
                _transition.PlaySting(sting, _masterVolume);
            }
        }

        public void OnRegionUnlocked(string regionId)
        {
            var sting = _library?.GetSFX("region_unlock");
            _transition.PlaySting(sting, _masterVolume);
        }

        public void OnBossEncounterStart(string bossId)
        {
            // Hard cut to boss theme (no graceful fade — dramatic)
            var bossTheme = _library?.GetTheme("boss");
            if (bossTheme != null)
                _transition.HardCut(bossTheme, _masterVolume);
            _ambient.SetMasterVolume(0f);
        }

        public void OnBossDefeated()
        {
            _ambient.SetMasterVolume(_masterVolume);
            // Return to zone music
            if (_currentContext != null)
                SetContext(_currentContext);
        }

        public void OnRitualStart()
        {
            // Ambient swells, music softens
            _transition.SetVolume(_masterVolume * 0.4f);
            _ambient.SetMasterVolume(_masterVolume * 0.8f);
        }

        // ─── Settings ─────────────────────────────────────────────────────────────

        public void SetMusicEnabled(bool enabled)
        {
            _musicEnabled = enabled;
            if (!enabled)
                _transition.FadeOut(1f);
            else
                SetContext(_currentContext ?? "explore_meadow");
        }

        public void SetMasterVolume(float volume)
        {
            _masterVolume = Mathf.Clamp01(volume);
            _transition.SetVolume(_masterVolume);
            _ambient.SetMasterVolume(_masterVolume * 0.6f);
        }

        public void SetTimeOfDay(float normalizedTime)
        {
            _ambient.SetTimeOfDay(normalizedTime);
        }

        // ─── Helpers ─────────────────────────────────────────────────────────────

        private AudioClip GetThemeForContext(string contextId)
        {
            if (contextId.StartsWith("zone_"))
                return _library?.GetTheme(contextId.Substring(5));

            return contextId switch
            {
                "sanctuary"      => _library?.GetTheme("fern-trail"),
                "worldmap"       => _library?.GetTheme("elderwood-grove"),
                "ritual"         => _library?.GetTheme("moonlit-creek"),
                "puzzle"         => _library?.GetTheme("firefly-hollow"),
                "parents"        => _library?.GetTheme("fern-trail"),
                "explore_meadow" => _library?.GetTheme("fern-trail"),
                _                => _library?.GetTheme("fern-trail")
            };
        }

        private static string ExtractBiomeFromContext(string contextId)
        {
            if (contextId.StartsWith("zone_"))
                return contextId.Substring(5);
            if (contextId == "sanctuary" || contextId == "explore_meadow")
                return "fern-trail";
            if (contextId == "worldmap")
                return "elderwood-grove";
            if (contextId == "ritual")
                return "moonlit-creek";
            return null;
        }
    }
}
