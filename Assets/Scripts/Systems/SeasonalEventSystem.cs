using System;
using System.Collections.Generic;
using UnityEngine;

namespace ForestFriendsQuest
{
    public enum Season { Spring, Summer, Autumn, Winter }

    public enum SpecialEventType
    {
        SpringBloom,
        SummerFireflyFestival,
        AutumnHarvest,
        WinterDreamFrost,
        MeteorShower,
        SolarEclipse,
        LunarBloom,
        AncientRitualNight
    }

    [Serializable]
    public class SeasonalEvent
    {
        public string          id;
        public string          title;
        public string          description;
        public SpecialEventType type;
        public Season          season;
        public int             durationDays;
        public string          rewardDescription;
        public int             rewardTreats;
        public string          achievementId;     // unlock on first attendance
    }

    /// <summary>
    /// Manages the four-season cycle and special celestial/ecological events.
    ///
    /// Season length: 30 in-game days each (real calendar-date seeded).
    /// Special events layer on top of seasons at specific day offsets and
    /// persist for their durationDays.
    ///
    /// Integration:
    ///   - DayNightWeatherController listens to OnSeasonChanged to blend biome tones
    ///   - AchievementSystem.TryUnlock called on first event attendance
    ///   - VFXManager.SetAmbientState feeds TimeOfDay + WeatherState per season
    /// </summary>
    public class SeasonalEventSystem : MonoBehaviour
    {
        private SaveSystem      _saveSystem;
        private AchievementSystem _achievements;

        public event Action<Season>        OnSeasonChanged;
        public event Action<SeasonalEvent> OnSpecialEventStarted;
        public event Action<SeasonalEvent> OnSpecialEventEnded;
        /// <summary>Fired when the player attends (participates in) a seasonal event.</summary>
        public event Action<SeasonalEvent> OnEventAttended;

        // ─── Season State ─────────────────────────────────────────────────────────

        private Season _currentSeason = Season.Spring;
        private int    _dayOfSeason   = 0;         // 0–29 within current season
        private int    _totalDays     = 0;          // absolute day count from epoch

        private const int SeasonLengthDays = 30;

        // ─── Event Catalog ────────────────────────────────────────────────────────

        private readonly List<SeasonalEvent>   _allEvents     = new List<SeasonalEvent>();
        private readonly List<SeasonalEvent>   _activeEvents  = new List<SeasonalEvent>();
        private readonly HashSet<string>        _attendedIds   = new HashSet<string>();

        // ─── Lifecycle ───────────────────────────────────────────────────────────

        public void Initialize(SaveSystem saveSystem, AchievementSystem achievements)
        {
            _saveSystem   = saveSystem;
            _achievements = achievements;

            BuildEventCatalog();
            SyncToRealDate();
            LoadAttendedState();
        }

        // ─── Public API ───────────────────────────────────────────────────────────

        public Season CurrentSeason   => _currentSeason;
        public int    DayOfSeason     => _dayOfSeason;

        public IReadOnlyList<SeasonalEvent> GetActiveEvents()  => _activeEvents;
        public IReadOnlyList<SeasonalEvent> GetAllEvents()     => _allEvents;

        /// <summary>
        /// Mark player as having attended an event (unlocks achievement, persists).
        /// Safe to call multiple times.
        /// </summary>
        public void AttendEvent(string eventId, ForestSaveData saveData = null)
        {
            if (_attendedIds.Contains(eventId)) return;

            SeasonalEvent ev = null;
            foreach (var e in _allEvents)
            {
                if (e.id == eventId) { ev = e; break; }
            }
            if (ev == null) return;

            _attendedIds.Add(eventId);

            if (saveData != null)
                saveData.forestTreats += ev.rewardTreats;

            if (!string.IsNullOrEmpty(ev.achievementId))
                _achievements?.TryUnlock(ev.achievementId, saveData);

            if (_saveSystem != null)
                _saveSystem.SetAchievementUnlocked($"SeasonEvent.{eventId}", true);

            Debug.Log($"[SeasonalEventSystem] Attended: {ev.title}");
            OnEventAttended?.Invoke(ev);
        }

        public bool HasAttended(string eventId) => _attendedIds.Contains(eventId);

        /// <summary>
        /// Advance by one in-game day. Should be called by TimeController or
        /// manually from ForestSystemsContainer on a real-time cadence.
        /// </summary>
        public void AdvanceDay()
        {
            _totalDays++;
            _dayOfSeason = _totalDays % SeasonLengthDays;

            var newSeason = (Season)((_totalDays / SeasonLengthDays) % 4);
            if (newSeason != _currentSeason)
            {
                _currentSeason = newSeason;
                OnSeasonChanged?.Invoke(_currentSeason);
                Debug.Log($"[SeasonalEventSystem] Season changed to {_currentSeason}");
            }

            RefreshActiveEvents();
        }

        // ─── Season Tone Helpers ──────────────────────────────────────────────────

        /// <summary>Returns a WeatherState appropriate for the current season.</summary>
        public WeatherState GetSeasonWeather()
        {
            switch (_currentSeason)
            {
                case Season.Spring:  return WeatherState.Misty;
                case Season.Summer:  return WeatherState.Sunny;
                case Season.Autumn:  return WeatherState.Windy;
                case Season.Winter:  return WeatherState.Snowy;
                default:             return WeatherState.Sunny;
            }
        }

        // ─── Private Helpers ──────────────────────────────────────────────────────

        private void RefreshActiveEvents()
        {
            var toEnd = new List<SeasonalEvent>();
            foreach (var ev in _activeEvents)
            {
                // Simple: events last their durationDays from their start day
                // (we treat dayOfSeason as a trigger window)
                if (_dayOfSeason > ev.durationDays)
                {
                    toEnd.Add(ev);
                    OnSpecialEventEnded?.Invoke(ev);
                }
            }
            foreach (var ev in toEnd) _activeEvents.Remove(ev);

            // Check if any new event should start today
            foreach (var ev in _allEvents)
            {
                if (ev.season != _currentSeason) continue;
                if (_activeEvents.Contains(ev)) continue;

                // Trigger window: events in first half of season at specific days
                int triggerDay = EventTriggerDay(ev.type);
                if (_dayOfSeason == triggerDay)
                {
                    _activeEvents.Add(ev);
                    OnSpecialEventStarted?.Invoke(ev);
                    Debug.Log($"[SeasonalEventSystem] Event started: {ev.title}");
                }
            }
        }

        private static int EventTriggerDay(SpecialEventType type)
        {
            switch (type)
            {
                case SpecialEventType.SpringBloom:          return 5;
                case SpecialEventType.SummerFireflyFestival: return 8;
                case SpecialEventType.AutumnHarvest:        return 6;
                case SpecialEventType.WinterDreamFrost:     return 3;
                case SpecialEventType.MeteorShower:         return 14;
                case SpecialEventType.SolarEclipse:         return 20;
                case SpecialEventType.LunarBloom:           return 15;
                case SpecialEventType.AncientRitualNight:   return 25;
                default:                                    return 0;
            }
        }

        private void SyncToRealDate()
        {
            var now         = DateTime.Now;
            var epoch       = new DateTime(2024, 3, 20);        // spring equinox epoch
            _totalDays      = (int)(now - epoch).TotalDays % (SeasonLengthDays * 4);
            _dayOfSeason    = _totalDays % SeasonLengthDays;
            _currentSeason  = (Season)((_totalDays / SeasonLengthDays) % 4);
            RefreshActiveEvents();
        }

        private void LoadAttendedState()
        {
            if (_saveSystem == null) return;
            foreach (var ev in _allEvents)
            {
                if (_saveSystem.IsAchievementUnlocked($"SeasonEvent.{ev.id}"))
                    _attendedIds.Add(ev.id);
            }
        }

        // ─── Event Catalog ────────────────────────────────────────────────────────

        private void BuildEventCatalog()
        {
            _allEvents.Add(new SeasonalEvent
            {
                id = "spring_bloom", title = "Great Spring Bloom",
                description = "The Whispering Meadow erupts in luminous blossoms. Pip knows a hidden grove that only appears now.",
                type = SpecialEventType.SpringBloom, season = Season.Spring,
                durationDays = 5, rewardTreats = 8,
                rewardDescription = "8 Moon Petals + Spring Bloom achievement",
                achievementId = "sea_spring_bloom"
            });

            _allEvents.Add(new SeasonalEvent
            {
                id = "summer_firefly", title = "Firefly Festival",
                description = "Firefly Marsh lights up with thousands of colonies. Luma leads a night parade through the reeds.",
                type = SpecialEventType.SummerFireflyFestival, season = Season.Summer,
                durationDays = 4, rewardTreats = 5,
                rewardDescription = "5 Sun Crystals + Summer Glow achievement",
                achievementId = "sea_summer_glow"
            });

            _allEvents.Add(new SeasonalEvent
            {
                id = "autumn_harvest", title = "Autumn Harvest",
                description = "Golden leaves carry hidden relics. Nori guards the secret path to the amber grove.",
                type = SpecialEventType.AutumnHarvest, season = Season.Autumn,
                durationDays = 6, rewardTreats = 6,
                rewardDescription = "Autumn Relic + Autumn Harvest achievement",
                achievementId = "sea_autumn_relic"
            });

            _allEvents.Add(new SeasonalEvent
            {
                id = "winter_dream", title = "Winter Dream Frost",
                description = "The Endless Dream Forest crystallises into a frost world. Sol speaks of ancient ice runes.",
                type = SpecialEventType.WinterDreamFrost, season = Season.Winter,
                durationDays = 7, rewardTreats = 10,
                rewardDescription = "Dream Crystal + Winter Dream achievement",
                achievementId = "sea_winter_dream"
            });

            _allEvents.Add(new SeasonalEvent
            {
                id = "meteor_shower", title = "Meteor Shower Night",
                description = "Streaks of light cross the Skyroot Canopy. Stand under the ancient tree to make a wish.",
                type = SpecialEventType.MeteorShower, season = Season.Summer,
                durationDays = 2, rewardTreats = 5,
                rewardDescription = "Star Shard + Star Catcher achievement",
                achievementId = "exp_meteor"
            });

            _allEvents.Add(new SeasonalEvent
            {
                id = "solar_eclipse", title = "Solar Eclipse",
                description = "The forest falls silent as daylight dims. Ancient stone circles begin to glow.",
                type = SpecialEventType.SolarEclipse, season = Season.Autumn,
                durationDays = 1, rewardTreats = 6,
                rewardDescription = "Eclipse Stone + Eclipse Witness achievement",
                achievementId = "exp_eclipse"
            });

            _allEvents.Add(new SeasonalEvent
            {
                id = "lunar_bloom", title = "Lunar Bloom",
                description = "Moon flowers bloom only when the full moon aligns with the Ancient Observatory telescope.",
                type = SpecialEventType.LunarBloom, season = Season.Spring,
                durationDays = 3, rewardTreats = 4,
                rewardDescription = "3 Moon Petals + Sol lore entry"
            });

            _allEvents.Add(new SeasonalEvent
            {
                id = "ancient_ritual_night", title = "Ancient Ritual Night",
                description = "Sol has deciphered the forgotten druid calendar. This night the Forgotten Ruins become fully accessible.",
                type = SpecialEventType.AncientRitualNight, season = Season.Winter,
                durationDays = 1, rewardTreats = 12,
                rewardDescription = "Ancient Tome page + 12 treats"
            });
        }
    }
}
