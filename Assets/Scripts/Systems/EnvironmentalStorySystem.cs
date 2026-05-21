using System.Collections.Generic;
using UnityEngine;

namespace ForestFriendsQuest
{
    /// <summary>
    /// Generates short contextual narrative snippets that surface based on
    /// player state, season, and world events.
    ///
    /// These snippets appear as ambient "whisper" text on the world map,
    /// in the sanctuary, and during ritual transitions — adding the sense
    /// that the forest is alive and watching.
    ///
    /// No external database — all stories are baked in to ensure offline play.
    /// </summary>
    public class EnvironmentalStorySystem : MonoBehaviour
    {
        private DynamicSeasonManager  _seasons;
        private WorldStateManager     _world;
        private RareWorldEventSystem  _events;

        private int _lastSnippetIndex = -1;

        // ─── Lifecycle ────────────────────────────────────────────────────────────

        public void Initialize(DynamicSeasonManager seasons, WorldStateManager world,
            RareWorldEventSystem events)
        {
            _seasons = seasons;
            _world   = world;
            _events  = events;
        }

        // ─── Public API ───────────────────────────────────────────────────────────

        /// <summary>Returns a contextual ambient story snippet for the current world state.</summary>
        public string GetAmbientSnippet()
        {
            var pool = BuildSnippetPool();
            if (pool.Count == 0) return string.Empty;

            // Avoid repeating the last snippet
            int idx;
            do { idx = Random.Range(0, pool.Count); }
            while (pool.Count > 1 && idx == _lastSnippetIndex);

            _lastSnippetIndex = idx;
            return pool[idx];
        }

        /// <summary>Returns the narrative text to show when a region is unlocked.</summary>
        public string GetRegionUnlockNarrative(string regionId) => regionId switch
        {
            "firefly-hollow"      => "The hollow stirs. Ancient lights remember you.",
            "river-bend"          => "Water carries all secrets downstream.",
            "moonlit-creek"       => "The moon bows to those who reach this far.",
            "elderwood-grove"     => "The elders have waited long for you.",
            "crystal-caverns"     => "Below the roots, the world holds its breath.",
            "firefly-marsh"       => "Mist and magic — not so different after all.",
            "forgotten-ruins"     => "What was lost was never truly gone.",
            "ancient-observatory" => "The stars have been watching your journey.",
            "skyroot-canopy"      => "You have reached the crown of the world.",
            _                     => "A new path opens before you."
        };

        /// <summary>Returns a world event narrative for display in the living world banner.</summary>
        public string GetEventNarrative(WorldEvent worldEvent)
        {
            if (worldEvent == null) return string.Empty;
            return worldEvent.type switch
            {
                WorldEventType.MeteorShower       =>
                    "Pip spotted something streaking across the sky. Gather quickly!",
                WorldEventType.FireflyFestival    =>
                    "The hollow is alive with thousand tiny lights. Come see!",
                WorldEventType.HarvestMoon        =>
                    "Tonight the moon is closer. Rituals bring double rewards.",
                WorldEventType.SnowStorm          =>
                    "The forest hushed. White silence. Winter has come.",
                WorldEventType.SpringBloom        =>
                    "Everything is waking up! The forest smells of new beginnings.",
                WorldEventType.CreatureMigration  =>
                    "Strangers pass through the forest paths. Wave hello!",
                WorldEventType.AncientTreeAwakening =>
                    "Deep below the roots, something very old has opened one eye.",
                WorldEventType.RainbowAfterStorm  =>
                    "After the rain — a bridge of colour leads somewhere new.",
                _                                 => worldEvent.description
            };
        }

        // ─── Private ─────────────────────────────────────────────────────────────

        private List<string> BuildSnippetPool()
        {
            var pool = new List<string>();

            // Season-specific
            if (_seasons != null)
            {
                var seasonSnippets = GetSeasonSnippets(_seasons.CurrentSeason);
                pool.AddRange(seasonSnippets);
            }

            // World-event specific
            var activeEvent = _events?.GetActiveEvent();
            if (activeEvent != null)
                pool.Add(GetEventNarrative(activeEvent));

            // Region-based (most recently unlocked)
            if (_world != null)
            {
                foreach (var region in _world.GetAllRegions())
                {
                    if (region.unlockState == RegionUnlockState.Discovered)
                        pool.Add($"The {region.displayName} holds secrets you haven't found yet.");
                }
            }

            // Fallback universal snippets
            pool.AddRange(new[]
            {
                "The forest remembers every step you take.",
                "Somewhere, a leaf falls and a creature looks up.",
                "The stars above the canopy spell out your name.",
                "Between the roots, old things dream of being found.",
                "Your creatures grow stronger every time you return."
            });

            return pool;
        }

        private static string[] GetSeasonSnippets(Season season) => season switch
        {
            Season.Spring => new[]
            {
                "New buds everywhere — the forest is holding its breath.",
                "The first fireflies have returned to the hollow.",
                "Everything smells like rain and possibility."
            },
            Season.Summer => new[]
            {
                "Long golden afternoons settle over the elderwood.",
                "The river runs fast and cold under the summer sun.",
                "Creatures linger longer when the days are warm."
            },
            Season.Autumn => new[]
            {
                "Copper leaves drift past the crystal caverns.",
                "The forest is letting go, gently, of everything.",
                "Harvest time — treats and crafting resources are more plentiful."
            },
            Season.Winter => new[]
            {
                "Ice rimes the observatory windows. Something ancient stirs below.",
                "The forest sleeps. But you are awake. That matters.",
                "Moonlit Creek has frozen over. Stars reflect in the ice."
            },
            _ => new[] { "The forest breathes slowly around you." }
        };
    }
}
