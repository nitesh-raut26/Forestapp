using System;
using System.Collections.Generic;
using UnityEngine;

namespace ForestFriendsQuest
{
    public enum WorldEventType
    {
        MeteorShower,
        FireflyFestival,
        HarvestMoon,
        SnowStorm,
        SpringBloom,
        CreatureMigration,
        AncientTreeAwakening,
        RainbowAfterStorm
    }

    [Serializable]
    public class WorldEvent
    {
        public string        eventId;
        public WorldEventType type;
        public string        displayName;
        public string        description;
        public int           durationDays;
        public bool          isActive;
        public float         rewardMultiplier = 1.5f;
    }

    /// <summary>
    /// Schedules and tracks rare world events that appear unexpectedly,
    /// driving re-engagement and wonder.
    ///
    /// Events are seeded from real date + save data hash so they feel
    /// "real" (same date = same event for all players) but not predictable.
    ///
    /// Active events surface through LivingWorldController for UI display
    /// and through ForestMusicDirector for audio theming.
    /// </summary>
    public class RareWorldEventSystem : MonoBehaviour
    {
        private DynamicSeasonManager _seasons;
        private SaveSystem           _saveSystem;

        private WorldEvent _activeEvent;
        private int        _activatedOnDay = -1;

        public event Action<WorldEvent> OnEventStarted;
        public event Action<WorldEvent> OnEventEnded;

        private static readonly List<WorldEvent> EventPool = new List<WorldEvent>
        {
            new WorldEvent { eventId = "meteor",     type = WorldEventType.MeteorShower,
                displayName = "Meteor Shower",        description = "Stars fall through the canopy tonight.",
                durationDays = 2, rewardMultiplier = 2f },
            new WorldEvent { eventId = "firefly",    type = WorldEventType.FireflyFestival,
                displayName = "Firefly Festival",     description = "The hollow glows with a thousand lights.",
                durationDays = 3, rewardMultiplier = 1.8f },
            new WorldEvent { eventId = "harvest",    type = WorldEventType.HarvestMoon,
                displayName = "Harvest Moon",         description = "The great orange moon rises over the river.",
                durationDays = 1, rewardMultiplier = 1.5f },
            new WorldEvent { eventId = "snowstorm",  type = WorldEventType.SnowStorm,
                displayName = "First Snowfall",       description = "Soft white silence blankets the forest.",
                durationDays = 2, rewardMultiplier = 1.3f },
            new WorldEvent { eventId = "bloom",      type = WorldEventType.SpringBloom,
                displayName = "Great Spring Bloom",   description = "A thousand flowers open at once.",
                durationDays = 3, rewardMultiplier = 1.6f },
            new WorldEvent { eventId = "migration",  type = WorldEventType.CreatureMigration,
                displayName = "Creature Migration",   description = "New friends pass through the forest.",
                durationDays = 4, rewardMultiplier = 1.4f },
            new WorldEvent { eventId = "tree",       type = WorldEventType.AncientTreeAwakening,
                displayName = "Ancient Tree Awakens", description = "A deep rumble from the elder roots.",
                durationDays = 1, rewardMultiplier = 3f },
            new WorldEvent { eventId = "rainbow",    type = WorldEventType.RainbowAfterStorm,
                displayName = "Rainbow Bridge",       description = "After the rain, a path of colour appears.",
                durationDays = 1, rewardMultiplier = 1.5f },
        };

        // ─── Lifecycle ────────────────────────────────────────────────────────────

        public void Initialize(DynamicSeasonManager seasons, SaveSystem saveSystem)
        {
            _seasons     = seasons;
            _saveSystem  = saveSystem;
            CheckForEventTransition();
        }

        // ─── Public API ───────────────────────────────────────────────────────────

        public WorldEvent GetActiveEvent() => _activeEvent?.isActive == true ? _activeEvent : null;

        public bool HasActiveEvent() => _activeEvent?.isActive == true;

        /// <summary>Called by DynamicSeasonManager.OnSeasonChanged and on daily tick.</summary>
        public void OnDayTick()
        {
            CheckForEventTransition();
        }

        // ─── Private ─────────────────────────────────────────────────────────────

        private void CheckForEventTransition()
        {
            var today = _saveSystem?.ActiveData?.totalInGameDays ?? 0;

            // End active event if duration expired
            if (_activeEvent != null && _activeEvent.isActive)
            {
                if (today - _activatedOnDay >= _activeEvent.durationDays)
                {
                    _activeEvent.isActive = false;
                    OnEventEnded?.Invoke(_activeEvent);
                    _activeEvent = null;
                    Debug.Log("[RareWorldEvents] Event ended.");
                }
                return;
            }

            // Roll for new event — ~15% chance per day
            var roll = GetDeterministicRoll(today);
            if (roll > 0.85f)
            {
                var eventIndex = Mathf.Abs(GetSeededInt(today)) % EventPool.Count;
                var candidate  = EventPool[eventIndex];

                // Season filter: snowstorm only in winter, bloom only in spring
                if (!IsEventValidForSeason(candidate)) return;

                _activeEvent       = candidate;
                _activeEvent.isActive = true;
                _activatedOnDay    = today;

                Debug.Log($"[RareWorldEvents] Event started: {_activeEvent.displayName}");
                OnEventStarted?.Invoke(_activeEvent);
            }
        }

        private bool IsEventValidForSeason(WorldEvent e)
        {
            var season = _seasons?.CurrentSeason ?? Season.Spring;
            return e.type switch
            {
                WorldEventType.SnowStorm       => season == Season.Winter,
                WorldEventType.SpringBloom     => season == Season.Spring,
                WorldEventType.HarvestMoon     => season == Season.Autumn,
                WorldEventType.FireflyFestival => season == Season.Summer,
                _                              => true
            };
        }

        private static float GetDeterministicRoll(int day)
        {
            // Deterministic pseudo-random from day seed
            var x = Mathf.Sin(day * 127.1f + 311.7f) * 43758.5453f;
            return x - Mathf.Floor(x);
        }

        private static int GetSeededInt(int day)
        {
            return (int)(GetDeterministicRoll(day * 31 + 7) * 1000f);
        }
    }
}
