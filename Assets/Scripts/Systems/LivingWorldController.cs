using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace ForestFriendsQuest
{
    /// <summary>
    /// Orchestrates the "living world" feel of the game by coordinating:
    ///   - Season background tints on map/sanctuary canvases
    ///   - Ambient story snippet rotation
    ///   - World event banner display
    ///   - Daily day-tick advancement
    ///
    /// Called by ForestSystemsContainer after all sub-systems are ready.
    /// Exposes ApplySeasonToCanvas() for use by WorldMapController and
    /// SanctuaryBuilderManager.
    /// </summary>
    public class LivingWorldController : MonoBehaviour
    {
        private DynamicSeasonManager     _seasons;
        private RareWorldEventSystem     _events;
        private EnvironmentalStorySystem _stories;
        private ForestMusicDirector      _music;

        private string _lastSessionDate;

        // ─── Lifecycle ────────────────────────────────────────────────────────────

        public void Initialize(DynamicSeasonManager seasons, RareWorldEventSystem events,
            EnvironmentalStorySystem stories, ForestMusicDirector music)
        {
            _seasons = seasons;
            _events  = events;
            _stories = stories;
            _music   = music;

            // Wire season change to music director time-of-day
            if (_seasons != null)
                _seasons.OnSeasonChanged += (prev, next) => OnSeasonChanged(next);

            // Wire world events
            if (_events != null)
            {
                _events.OnEventStarted += e => Debug.Log($"[LivingWorld] Event started: {e.displayName}");
                _events.OnEventEnded   += e => Debug.Log($"[LivingWorld] Event ended: {e.displayName}");
            }

            // Check daily tick
            TickDayIfNewSession();
        }

        // ─── Public API ───────────────────────────────────────────────────────────

        /// <summary>Apply the current season's background tint to a canvas background Image.</summary>
        public void ApplySeasonToCanvas(Image backgroundImage)
        {
            if (backgroundImage == null || _seasons == null) return;
            StartCoroutine(TintTransition(backgroundImage,
                backgroundImage.color, _seasons.GetSeasonBackground(), 1.5f));
        }

        /// <summary>Get a UI-ready ambient story line.</summary>
        public string GetAmbientStory() => _stories?.GetAmbientSnippet() ?? string.Empty;

        /// <summary>Get the current season's accent color for UI elements.</summary>
        public Color GetSeasonAccent() => _seasons?.GetSeasonAccentColor()
            ?? new Color32(120, 220, 140, 255);

        public string GetSeasonName() => _seasons?.GetSeasonDisplayName() ?? "Spring";

        public WorldEvent GetActiveWorldEvent() => _events?.GetActiveEvent();

        public string GetRegionUnlockStory(string regionId) =>
            _stories?.GetRegionUnlockNarrative(regionId) ?? "A new path opens before you.";

        // ─── Private ─────────────────────────────────────────────────────────────

        private void TickDayIfNewSession()
        {
            var today = System.DateTime.Today.ToString("yyyyMMdd");
            var lastDate = PlayerPrefs.GetString("FFQ.LastSessionDate", "");

            if (today != lastDate)
            {
                PlayerPrefs.SetString("FFQ.LastSessionDate", today);
                _seasons?.TickDay();
                _events?.OnDayTick();
                Debug.Log($"[LivingWorld] New day tick: {today}");
            }

            _lastSessionDate = today;
        }

        private void OnSeasonChanged(Season newSeason)
        {
            // Update music director time-of-day approximation based on season
            var tod = newSeason switch
            {
                Season.Winter => 0.9f,  // feels like dusk/night
                Season.Autumn => 0.7f,
                Season.Summer => 0.45f, // midday
                _             => 0.25f  // spring morning
            };
            _music?.SetTimeOfDay(tod);
        }

        private static IEnumerator TintTransition(Image img, Color from, Color to, float duration)
        {
            var elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                img.color = Color.Lerp(from, to, Mathf.Clamp01(elapsed / duration));
                yield return null;
            }
            img.color = to;
        }
    }
}
