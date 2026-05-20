using System;
using System.Collections.Generic;
using UnityEngine;

namespace ForestFriendsQuest
{
    public enum AchievementCategory
    {
        Explorer,
        Bonding,
        PuzzleMastery,
        Seasonal,
        Secret
    }

    [Serializable]
    public class Achievement
    {
        public string              id;
        public string              title;
        public string              description;
        public string              secretHint;        // shown before unlock for Secret tier
        public AchievementCategory category;
        public bool                isSecret;
        public bool                isUnlocked;
        public int                 rewardTreats;
    }

    /// <summary>
    /// Achievement system tracking 40+ forest-themed milestones across all tiers.
    /// Persists via SaveSystem. Emits OnAchievementUnlocked for UI/VFX integration.
    /// </summary>
    public class AchievementSystem : MonoBehaviour
    {
        private SaveSystem _saveSystem;

        public event Action<Achievement> OnAchievementUnlocked;

        // ─── Catalog ──────────────────────────────────────────────────────────────

        private readonly List<Achievement> _catalog = new List<Achievement>();

        // ─── Lifecycle ───────────────────────────────────────────────────────────

        public void Initialize(SaveSystem saveSystem)
        {
            _saveSystem = saveSystem;
            BuildCatalog();
            LoadUnlockState();
        }

        // ─── Public API ───────────────────────────────────────────────────────────

        public IReadOnlyList<Achievement> GetAll()           => _catalog;
        public IReadOnlyList<Achievement> GetUnlocked()
        {
            var result = new List<Achievement>();
            foreach (var a in _catalog) { if (a.isUnlocked) result.Add(a); }
            return result;
        }

        public IReadOnlyList<Achievement> GetByCategory(AchievementCategory cat)
        {
            var result = new List<Achievement>();
            foreach (var a in _catalog) { if (a.category == cat) result.Add(a); }
            return result;
        }

        public int GetUnlockedCount() => GetUnlocked().Count;
        public int GetTotalCount()    => _catalog.Count;

        /// <summary>Attempt to unlock an achievement by ID. Safe to call repeatedly.</summary>
        public bool TryUnlock(string achievementId, ForestSaveData saveData = null)
        {
            Achievement target = null;
            foreach (var a in _catalog)
            {
                if (a.id == achievementId) { target = a; break; }
            }

            if (target == null || target.isUnlocked) return false;

            target.isUnlocked = true;

            if (_saveSystem != null)
            {
                _saveSystem.SetAchievementUnlocked(achievementId, true);
            }

            if (saveData != null)
            {
                saveData.forestTreats += target.rewardTreats;
            }

            OnAchievementUnlocked?.Invoke(target);
            Debug.Log($"[Achievement] Unlocked: {target.title}");
            return true;
        }

        public bool IsUnlocked(string id)
        {
            foreach (var a in _catalog) { if (a.id == id) return a.isUnlocked; }
            return false;
        }

        // ─── State Persistence ────────────────────────────────────────────────────

        private void LoadUnlockState()
        {
            if (_saveSystem == null) return;
            foreach (var a in _catalog)
            {
                if (_saveSystem.IsAchievementUnlocked(a.id))
                {
                    a.isUnlocked = true;
                }
            }
        }

        // ─── Catalog Definition ───────────────────────────────────────────────────

        private void BuildCatalog()
        {
            // ── Explorer Category ─────────────────────────────────────────────────
            Add("exp_first_steps",   "First Steps",        "Take your first walk through the Whispering Meadow.",      AchievementCategory.Explorer, false, 1);
            Add("exp_5_zones",       "Zone Wanderer",      "Visit 5 different forest zones.",                          AchievementCategory.Explorer, false, 3);
            Add("exp_all_zones",     "World Traverser",    "Set foot in every zone of the forest.",                    AchievementCategory.Explorer, false, 8);
            Add("exp_night_walk",    "Moonlit Explorer",   "Explore the forest at night.",                             AchievementCategory.Explorer, false, 2);
            Add("exp_meteor",        "Star Catcher",       "Witness a meteor shower night event.",                     AchievementCategory.Explorer, false, 5);
            Add("exp_eclipse",       "Eclipse Witness",    "Be present during a solar eclipse in the forest.",         AchievementCategory.Explorer, false, 6);
            Add("exp_secret_path",   "Path Seeker",        "Discover a hidden path in the forest.",                    AchievementCategory.Explorer, false, 4);
            Add("exp_ruins",         "Ruin Walker",        "Explore the Forgotten Ruins.",                             AchievementCategory.Explorer, false, 3);
            Add("exp_observatory",   "Sky Gazer",          "Reach the Ancient Observatory.",                           AchievementCategory.Explorer, false, 5);
            Add("exp_dream_forest",  "Dream Walker",       "Enter the Endless Dream Forest.",                          AchievementCategory.Explorer, false, 10);

            // ── Bonding Category ──────────────────────────────────────────────────
            Add("bond_pip_1",        "Pip's Friend",       "Reach Bond Level 2 with Pip the Fox.",                     AchievementCategory.Bonding, false, 2);
            Add("bond_pip_5",        "Pip's Best Pal",     "Reach Bond Level 5 with Pip the Fox.",                     AchievementCategory.Bonding, false, 8);
            Add("bond_mimi_1",       "Mimi's Listener",    "Reach Bond Level 2 with Mimi the Bird.",                   AchievementCategory.Bonding, false, 2);
            Add("bond_tomo_1",       "Tomo's Companion",   "Reach Bond Level 2 with Tomo the Turtle.",                 AchievementCategory.Bonding, false, 2);
            Add("bond_luma_1",       "Luma's Light",       "Reach Bond Level 2 with Luma the Firefly.",                AchievementCategory.Bonding, false, 2);
            Add("bond_nori_1",       "Nori's Friend",      "Reach Bond Level 2 with Nori the Deer.",                   AchievementCategory.Bonding, false, 2);
            Add("bond_sol_1",        "Sol's Apprentice",   "Reach Bond Level 2 with Sol the Owl.",                     AchievementCategory.Bonding, false, 2);
            Add("bond_all_max",      "Forest Family",      "Reach Bond Level 3 with all six forest friends.",          AchievementCategory.Bonding, false, 20);
            Add("bond_feed_10",      "Generous Explorer",  "Feed forest friends 10 times.",                            AchievementCategory.Bonding, false, 3);

            // ── Puzzle Mastery ────────────────────────────────────────────────────
            Add("puz_first_solve",   "First Solution",     "Solve your very first forest puzzle.",                     AchievementCategory.PuzzleMastery, false, 1);
            Add("puz_no_hints_5",    "Unaided Mind",       "Solve 5 puzzles in a row without using a hint.",           AchievementCategory.PuzzleMastery, false, 5);
            Add("puz_memory_5",      "Memory Keeper",      "Complete 5 memory trail puzzles.",                         AchievementCategory.PuzzleMastery, false, 4);
            Add("puz_rune_decode",   "Rune Reader",        "Decode your first rune sequence.",                         AchievementCategory.PuzzleMastery, false, 3);
            Add("puz_mirror_3",      "Mirror Master",      "Solve 3 logic mirror puzzles.",                            AchievementCategory.PuzzleMastery, false, 5);
            Add("puz_music_3",       "Forest Musician",    "Complete 3 music pattern puzzles.",                        AchievementCategory.PuzzleMastery, false, 4);
            Add("puz_cipher_5",      "Cipher Sage",        "Complete 5 symbol cipher rituals.",                        AchievementCategory.PuzzleMastery, false, 8);
            Add("puz_flawless_10",   "Perfect Mind",       "Achieve a flawless score on 10 puzzles (no mistakes).",    AchievementCategory.PuzzleMastery, false, 10);
            Add("puz_speed_3",       "Quick Thinker",      "Solve 3 puzzles under 30 seconds each.",                   AchievementCategory.PuzzleMastery, false, 5);

            // ── Seasonal ──────────────────────────────────────────────────────────
            Add("sea_spring_bloom",  "Spring Awakening",   "Witness the spring bloom in the Whispering Meadow.",       AchievementCategory.Seasonal, false, 5);
            Add("sea_summer_glow",   "Summer Glow",        "Collect 5 sun crystals during summer.",                    AchievementCategory.Seasonal, false, 5);
            Add("sea_autumn_relic",  "Autumn Harvest",     "Find a hidden relic during autumn.",                       AchievementCategory.Seasonal, false, 5);
            Add("sea_winter_dream",  "Winter Dream",       "Enter the Dream Forest during winter season.",             AchievementCategory.Seasonal, false, 5);
            Add("sea_daily_7",       "Week of Wonder",     "Complete 7 consecutive daily rituals.",                    AchievementCategory.Seasonal, false, 15);
            Add("sea_daily_30",      "Month of Magic",     "Complete 30 daily rituals total.",                         AchievementCategory.Seasonal, false, 40);

            // ── Secret ────────────────────────────────────────────────────────────
            AddSecret("sec_midnight_sol",  "Midnight Scholar",    "Something waits for those who visit Sol at midnight.",     "Meet Sol at the stroke of midnight.",          8);
            AddSecret("sec_rain_dance",    "Rain Dancer",         "A hidden interaction exists in the rain.",                 "Dance in the rain near Moonlit Creek.",        6);
            AddSecret("sec_ancient_boss",  "Ancient Challenger",  "A legendary creature guards the Observatory.",             "Defeat the Ancient Forest Guardian.",         25);
            AddSecret("sec_dream_unlocked","Dream Weaver",        "The Endless Dream Forest holds one final secret.",         "Discover all Dream Forest hidden paths.",      30);
            AddSecret("sec_lore_complete", "Lore Keeper",         "Every piece of forest lore has been discovered.",          "Collect all lore pages.",                      20);
        }

        private void Add(string id, string title, string desc,
            AchievementCategory cat, bool secret, int treats)
        {
            _catalog.Add(new Achievement
            {
                id = id, title = title, description = desc,
                category = cat, isSecret = secret, rewardTreats = treats
            });
        }

        private void AddSecret(string id, string title, string desc,
            string hint, int treats)
        {
            _catalog.Add(new Achievement
            {
                id = id, title = title, description = desc,
                secretHint = hint,
                category = AchievementCategory.Secret, isSecret = true,
                rewardTreats = treats
            });
        }
    }
}
