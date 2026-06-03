using System;
using System.Collections.Generic;
using UnityEngine;

namespace ForestFriendsQuest
{
    /// <summary>
    /// Describes a single daily ritual event.
    /// </summary>
    [Serializable]
    public class DailyRitual
    {
        public string id;
        public string title;
        public string description;
        public string rewardDescription;
        public RitualType type;
        public int      rewardTreats;
    }

    public enum RitualType
    {
        ForestMystery,
        CipherChallenge,
        RareCreatureMigration,
        MoonBlossomBloom,
        RelicRecovery,
        WeatherAnomaly,
        SpiritGuideQuest,
        MeteorShowerNight,
        EclipseRitual
    }

    /// <summary>
    /// Ethical daily engagement system. Uses the current date as a deterministic
    /// seed so every player sees the same daily ritual on the same day — creating
    /// a sense of shared forest magic — without any server dependency.
    ///
    /// Design principles:
    ///   - Rewards feel magical, never manipulative
    ///   - Missing a day has zero penalty
    ///   - Rituals are completable in under 3 minutes
    ///   - Content rotates meaningfully (9 ritual types over ~30-day cycle)
    /// </summary>
    public class DailyRitualSystem : MonoBehaviour
    {
        private SaveSystem _saveSystem;

        // ─── All possible ritual definitions ─────────────────────────────────────

        private static readonly List<DailyRitual> AllRituals = new List<DailyRitual>
        {
            new DailyRitual
            {
                id = "forest_mystery_01", title = "The Whispering Oak",
                description = "The ancient oak near the Elderwood has awakened. Listen carefully to its bark patterns and decode the morning message.",
                rewardDescription = "3 Amber Acorns + Pip bond trust",
                type = RitualType.ForestMystery, rewardTreats = 3
            },
            new DailyRitual
            {
                id = "cipher_01", title = "The Shifting Rune Cipher",
                description = "A new rune pattern has appeared in the Crystal Caverns. Decode the three-symbol sequence to reveal today's forest secret.",
                rewardDescription = "5 Alchemical Fragments + rune progress",
                type = RitualType.CipherChallenge, rewardTreats = 5
            },
            new DailyRitual
            {
                id = "migration_01", title = "Rare Creature Migration",
                description = "A rare Silverback Moth has been spotted crossing the Firefly Marsh. Reach the marsh before sunset to witness it.",
                rewardDescription = "Rare encounter log entry + 2 Forest Treats",
                type = RitualType.RareCreatureMigration, rewardTreats = 2
            },
            new DailyRitual
            {
                id = "moon_blossom_01", title = "Moon Blossom Bloom",
                description = "The Moon Blossoms only open once every few days. Water three of them before the day cycle turns to dusk.",
                rewardDescription = "Moon Petal collection + 4 Treats",
                type = RitualType.MoonBlossomBloom, rewardTreats = 4
            },
            new DailyRitual
            {
                id = "relic_01", title = "Lost Relic Recovery",
                description = "Tomo the Turtle found a buried relic near Moonlit Creek. Help recover it by solving the path memory puzzle.",
                rewardDescription = "Ancient Relic fragment + 3 Treats",
                type = RitualType.RelicRecovery, rewardTreats = 3
            },
            new DailyRitual
            {
                id = "weather_01", title = "Rainbow Storm Anomaly",
                description = "A rare prismatic storm is passing through the Skyroot Canopy. Navigate the storm safely to collect scattered light crystals.",
                rewardDescription = "Light Crystal shards + 4 Treats",
                type = RitualType.WeatherAnomaly, rewardTreats = 4
            },
            new DailyRitual
            {
                id = "spirit_01", title = "Sol's Midnight Teaching",
                description = "Sol the Owl has a wisdom lesson available tonight. Solve the logic mirror puzzle she presents.",
                rewardDescription = "Sol bond +20 trust + ancient tome page",
                type = RitualType.SpiritGuideQuest, rewardTreats = 2
            },
            new DailyRitual
            {
                id = "meteor_01", title = "Meteor Shower Ritual",
                description = "A meteor shower lights up the forest sky. Catch 5 falling stars in the Ancient Observatory by memory trail.",
                rewardDescription = "Stardust collection + 6 Treats",
                type = RitualType.MeteorShowerNight, rewardTreats = 6
            },
            new DailyRitual
            {
                id = "eclipse_01", title = "Eclipse Cipher Night",
                description = "The rare forest eclipse has triggered hidden rune pillars in the Forgotten Ruins. Decode them all before dawn.",
                rewardDescription = "Eclipse rune set + 8 Treats + lore page",
                type = RitualType.EclipseRitual, rewardTreats = 8
            }
        };

        // ─── Lifecycle ───────────────────────────────────────────────────────────

        public void Initialize(SaveSystem saveSystem)
        {
            _saveSystem = saveSystem;
        }

        // ─── Public API ───────────────────────────────────────────────────────────

        /// <summary>
        /// Get today's ritual. Deterministic per calendar date — same ritual for
        /// every player on the same day, seeded by day-of-year.
        /// </summary>
        public DailyRitual GetTodaysRitual()
        {
            var dayOfYear = DateTime.Today.DayOfYear;
            var index     = dayOfYear % AllRituals.Count;
            return AllRituals[index];
        }

        /// <summary>Returns true if today's ritual has already been completed.</summary>
        public bool IsTodaysRitualComplete()
        {
            if (_saveSystem == null)
            {
                // Fallback to PlayerPrefs directly
                var dateKey = $"FFQ.DailyRitual.{DateTime.Today:yyyyMMdd}";
                return PlayerPrefs.GetInt(dateKey, 0) == 1;
            }

            var ritual = GetTodaysRitual();
            return _saveSystem.GetDailyRitualComplete(ritual.id);
        }

        /// <summary>Mark today's ritual as complete and grant its rewards.</summary>
        public int CompleteRitual(ForestSaveData saveData)
        {
            var ritual = GetTodaysRitual();

            if (_saveSystem != null)
            {
                _saveSystem.SetDailyRitualComplete(ritual.id, true);
            }
            else
            {
                var dateKey = $"FFQ.DailyRitual.{DateTime.Today:yyyyMMdd}";
                PlayerPrefs.SetInt(dateKey, 1);
                PlayerPrefs.Save();
            }

            // Grant treat reward to save data
            if (saveData != null)
            {
                saveData.forestTreats += ritual.rewardTreats;
            }

            Debug.Log($"[DailyRitual] Completed '{ritual.title}' — granted {ritual.rewardTreats} treats.");
            return ritual.rewardTreats;
        }

        /// <summary>Preview the next N days of rituals (helps create anticipation).</summary>
        public List<DailyRitual> GetUpcomingRituals(int days = 3)
        {
            var result    = new List<DailyRitual>(days);
            var dayOfYear = DateTime.Today.DayOfYear;

            for (var i = 1; i <= days; i++)
            {
                var index = (dayOfYear + i) % AllRituals.Count;
                result.Add(AllRituals[index]);
            }

            return result;
        }

        /// <summary>Get the ritual type for the current day (for weather/creature override).</summary>
        public RitualType GetTodaysRitualType()
        {
            return GetTodaysRitual().type;
        }

        /// <summary>Number of consecutive days the player has completed a daily ritual.</summary>
        public int CurrentStreak
        {
            get
            {
                int streak = 0;
                for (int i = 0; i < 365; i++)
                {
                    var date   = DateTime.Today.AddDays(-i);
                    var ritual = AllRituals[date.DayOfYear % AllRituals.Count];
                    bool done;
                    if (_saveSystem != null)
                        done = _saveSystem.GetDailyRitualComplete(ritual.id);
                    else
                        done = PlayerPrefs.GetInt($"FFQ.DailyRitual.{date:yyyyMMdd}", 0) == 1;
                    if (done) streak++;
                    else break;
                }
                return streak;
            }
        }

        /// <summary>Inject a ritual from LiveContentPipeline — added to pool immediately.</summary>
        public void RegisterLiveRitual(DailyRitual ritual)
        {
            if (ritual == null || string.IsNullOrEmpty(ritual.id)) return;
            // Avoid duplicate injection
            if (AllRituals.Exists(r => r.id == ritual.id)) return;
            AllRituals.Add(ritual);
            Debug.Log($"[DailyRitualSystem] Live ritual registered: {ritual.id}");
        }
    }
}
