using System;
using System.Collections.Generic;
using UnityEngine;

namespace ForestFriendsQuest
{
    public enum QuestStatus
    {
        Locked,
        Available,
        Active,
        Completed
    }

    public enum QuestTier
    {
        Sprout,
        Scout,
        Druid
    }

    [Serializable]
    public class QuestObjective
    {
        public string id;
        public string description;
        public int    targetCount;
        public int    currentCount;
        public bool   IsComplete => currentCount >= targetCount;
    }

    [Serializable]
    public class QuestDefinition
    {
        public string          id;
        public string          title;
        public string          description;
        public string          creatureId;      // which guide creature gives this quest
        public QuestTier       tier;
        public QuestStatus     status;
        public QuestObjective[] objectives;
        public string          rewardDescription;
        public int             rewardTreats;
        public int             rewardBondPoints;
        public string[]        prerequisiteQuestIds;
    }

    /// <summary>
    /// Full quest engine managing active quests per tier, objective tracking,
    /// completion rewards, and prerequisite chains.
    ///
    /// Sprout quests: 1–2 objectives, simple care/tap interactions
    /// Scout quests:  2–4 objectives, exploration + collection chains
    /// Druid quests:  4–6 objectives, multi-stage cipher + crafting
    /// </summary>
    public class QuestEngine : MonoBehaviour
    {
        // ─── System Links ─────────────────────────────────────────────────────────

        private EmotionalBondingEngine _bondingEngine;
        private SaveSystem             _saveSystem;

        // ─── Quest Catalog ────────────────────────────────────────────────────────

        private readonly List<QuestDefinition> _allQuests = new List<QuestDefinition>();
        private readonly HashSet<string>        _completedIds = new HashSet<string>();

        // ─── Events ───────────────────────────────────────────────────────────────

        public event Action<QuestDefinition>  OnQuestStarted;
        public event Action<QuestDefinition>  OnQuestCompleted;
        public event Action<QuestObjective>   OnObjectiveProgressed;

        // ─── Lifecycle ───────────────────────────────────────────────────────────

        public void Initialize(EmotionalBondingEngine bondingEngine, SaveSystem saveSystem)
        {
            _bondingEngine = bondingEngine;
            _saveSystem    = saveSystem;

            BuildQuestCatalog();
            LoadCompletionState();
            RefreshAvailability();
        }

        // ─── Public API ───────────────────────────────────────────────────────────

        public IReadOnlyList<QuestDefinition> GetQuestsByTier(QuestTier tier)
        {
            var result = new List<QuestDefinition>();
            foreach (var q in _allQuests)
            {
                if (q.tier == tier) result.Add(q);
            }
            return result;
        }

        public IReadOnlyList<QuestDefinition> GetActiveQuests()
        {
            var result = new List<QuestDefinition>();
            foreach (var q in _allQuests)
            {
                if (q.status == QuestStatus.Active) result.Add(q);
            }
            return result;
        }

        public QuestDefinition GetQuest(string id)
        {
            foreach (var q in _allQuests)
            {
                if (q.id == id) return q;
            }
            return null;
        }

        /// <summary>Start a quest — moves it from Available to Active.</summary>
        public bool StartQuest(string questId)
        {
            var quest = GetQuest(questId);
            if (quest == null || quest.status != QuestStatus.Available) return false;

            quest.status = QuestStatus.Active;
            OnQuestStarted?.Invoke(quest);
            return true;
        }

        /// <summary>Progress an objective by ID within any active quest.</summary>
        public void ProgressObjective(string objectiveId, int amount = 1)
        {
            foreach (var quest in _allQuests)
            {
                if (quest.status != QuestStatus.Active) continue;

                foreach (var obj in quest.objectives)
                {
                    if (obj.id != objectiveId) continue;
                    obj.currentCount = Mathf.Min(obj.currentCount + amount, obj.targetCount);
                    OnObjectiveProgressed?.Invoke(obj);

                    // Check if all objectives are now complete
                    if (AllObjectivesComplete(quest))
                    {
                        CompleteQuest(quest);
                    }
                    return;
                }
            }
        }

        public bool IsQuestComplete(string questId) => _completedIds.Contains(questId);

        // ─── Private Quest Completion ────────────────────────────────────────────

        private void CompleteQuest(QuestDefinition quest)
        {
            quest.status = QuestStatus.Completed;
            _completedIds.Add(quest.id);

            // Grant bond trust to associated creature
            if (!string.IsNullOrEmpty(quest.creatureId) && _bondingEngine != null)
            {
                _bondingEngine.AddTrust(quest.creatureId, quest.rewardBondPoints);
            }

            // Persist completion
            if (_saveSystem != null)
            {
                _saveSystem.SetAchievementUnlocked($"Quest.{quest.id}", true);
            }

            OnQuestCompleted?.Invoke(quest);
            RefreshAvailability();

            Debug.Log($"[QuestEngine] Completed: {quest.title} — reward: {quest.rewardTreats} treats");
        }

        private static bool AllObjectivesComplete(QuestDefinition quest)
        {
            if (quest.objectives == null) return true;
            foreach (var obj in quest.objectives)
            {
                if (!obj.IsComplete) return false;
            }
            return true;
        }

        private void RefreshAvailability()
        {
            foreach (var quest in _allQuests)
            {
                if (quest.status != QuestStatus.Locked) continue;

                if (PrerequisitesMet(quest))
                {
                    quest.status = QuestStatus.Available;
                }
            }
        }

        private bool PrerequisitesMet(QuestDefinition quest)
        {
            if (quest.prerequisiteQuestIds == null || quest.prerequisiteQuestIds.Length == 0)
                return true;

            foreach (var prereqId in quest.prerequisiteQuestIds)
            {
                if (!_completedIds.Contains(prereqId)) return false;
            }
            return true;
        }

        private void LoadCompletionState()
        {
            foreach (var quest in _allQuests)
            {
                if (_saveSystem != null && _saveSystem.IsAchievementUnlocked($"Quest.{quest.id}"))
                {
                    quest.status = QuestStatus.Completed;
                    _completedIds.Add(quest.id);
                }
            }
        }

        // ─── Quest Catalog ────────────────────────────────────────────────────────

        private void BuildQuestCatalog()
        {
            // ── Sprout Tier Quests ────────────────────────────────────────────────
            _allQuests.Add(new QuestDefinition
            {
                id = "sprout_q01", title = "First Petal Touch",
                description = "Pip wants to show you something magical. Tap three flowers in the Whispering Meadow.",
                creatureId = "pip", tier = QuestTier.Sprout, status = QuestStatus.Available,
                objectives = new[]
                {
                    new QuestObjective { id = "tap_flower", description = "Tap magical flowers", targetCount = 3 }
                },
                rewardDescription = "3 Sunberries + Pip trust", rewardTreats = 3, rewardBondPoints = 15,
                prerequisiteQuestIds = Array.Empty<string>()
            });

            _allQuests.Add(new QuestDefinition
            {
                id = "sprout_q02", title = "Water the Dream Seeds",
                description = "Mimi found tiny glowing seeds. Water them so they grow into magical plants.",
                creatureId = "mimi", tier = QuestTier.Sprout, status = QuestStatus.Locked,
                objectives = new[]
                {
                    new QuestObjective { id = "water_plant", description = "Water magical plants", targetCount = 2 }
                },
                rewardDescription = "Moon Petals + Mimi trust", rewardTreats = 2, rewardBondPoints = 10,
                prerequisiteQuestIds = new[] { "sprout_q01" }
            });

            // ── Scout Tier Quests ─────────────────────────────────────────────────
            _allQuests.Add(new QuestDefinition
            {
                id = "scout_q01", title = "The Hidden Creek Trail",
                description = "Tomo knows a secret path through Moonlit Creek. Follow his memory trail through 4 checkpoints.",
                creatureId = "tomo", tier = QuestTier.Scout, status = QuestStatus.Available,
                objectives = new[]
                {
                    new QuestObjective { id = "memory_trail_complete", description = "Complete memory trail", targetCount = 1 },
                    new QuestObjective { id = "collectible_found", description = "Find hidden collectibles", targetCount = 2 }
                },
                rewardDescription = "River Crystal + Tomo trust", rewardTreats = 4, rewardBondPoints = 20,
                prerequisiteQuestIds = Array.Empty<string>()
            });

            _allQuests.Add(new QuestDefinition
            {
                id = "scout_q02", title = "Luma's Firefly Census",
                description = "Luma is counting the firefly colonies in Firefly Marsh. Help her record 5 colonies.",
                creatureId = "luma", tier = QuestTier.Scout, status = QuestStatus.Locked,
                objectives = new[]
                {
                    new QuestObjective { id = "firefly_zone_visited", description = "Visit firefly zones", targetCount = 5 },
                    new QuestObjective { id = "night_exploration", description = "Explore at night", targetCount = 1 }
                },
                rewardDescription = "Glowing Sap + Luma trust", rewardTreats = 5, rewardBondPoints = 25,
                prerequisiteQuestIds = new[] { "scout_q01" }
            });

            _allQuests.Add(new QuestDefinition
            {
                id = "scout_q03", title = "The Puzzle Cave Expedition",
                description = "A cave with crystal mirrors has been discovered near Crystal Caverns. Solve all three mirror puzzles inside.",
                creatureId = "pip", tier = QuestTier.Scout, status = QuestStatus.Locked,
                objectives = new[]
                {
                    new QuestObjective { id = "mirror_puzzle_solved", description = "Solve mirror puzzles", targetCount = 3 }
                },
                rewardDescription = "Crystal Lens + rare item", rewardTreats = 6, rewardBondPoints = 30,
                prerequisiteQuestIds = new[] { "scout_q02" }
            });

            // ── Druid Tier Quests ─────────────────────────────────────────────────
            _allQuests.Add(new QuestDefinition
            {
                id = "druid_q01", title = "The Ancient Cipher Chamber",
                description = "Sol has found a hidden chamber in the Forgotten Ruins. Decode the 5-step rune cipher inscribed on its walls.",
                creatureId = "sol", tier = QuestTier.Druid, status = QuestStatus.Available,
                objectives = new[]
                {
                    new QuestObjective { id = "rune_puzzle_solved", description = "Solve the rune cipher", targetCount = 1 },
                    new QuestObjective { id = "lore_page_collected", description = "Collect lore pages", targetCount = 3 }
                },
                rewardDescription = "Ancient tome unlock + 8 treats", rewardTreats = 8, rewardBondPoints = 40,
                prerequisiteQuestIds = Array.Empty<string>()
            });

            _allQuests.Add(new QuestDefinition
            {
                id = "druid_q02", title = "The Alchemical Observatory",
                description = "The Ancient Observatory holds a rare telescope puzzle. Align star patterns by solving a logic gate sequence.",
                creatureId = "sol", tier = QuestTier.Druid, status = QuestStatus.Locked,
                objectives = new[]
                {
                    new QuestObjective { id = "logic_gate_solved", description = "Solve logic gate puzzles", targetCount = 2 },
                    new QuestObjective { id = "star_pattern_aligned", description = "Align star patterns", targetCount = 1 },
                    new QuestObjective { id = "alchemical_item_crafted", description = "Craft alchemical item", targetCount = 1 }
                },
                rewardDescription = "Star chart + rare alchemical relic + 12 treats",
                rewardTreats = 12, rewardBondPoints = 60,
                prerequisiteQuestIds = new[] { "druid_q01" }
            });

            _allQuests.Add(new QuestDefinition
            {
                id = "druid_q03", title = "The Hidden Druid Sanctuary",
                description = "A legendary hidden sanctuary awaits those who can decode the Dream Forest's deepest secrets.",
                creatureId = "sol", tier = QuestTier.Druid, status = QuestStatus.Locked,
                objectives = new[]
                {
                    new QuestObjective { id = "reverse_memory_ritual", description = "Complete reverse memory ritual", targetCount = 1 },
                    new QuestObjective { id = "symbol_cipher_decoded", description = "Decode symbol ciphers", targetCount = 4 },
                    new QuestObjective { id = "sanctuary_built", description = "Build Druid sanctuary items", targetCount = 3 }
                },
                rewardDescription = "Endless Dream Forest access + legendary relic",
                rewardTreats = 20, rewardBondPoints = 100,
                prerequisiteQuestIds = new[] { "druid_q02" }
            });
        }
    }
}
