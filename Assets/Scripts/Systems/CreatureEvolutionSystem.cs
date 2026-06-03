using System;
using System.Collections.Generic;
using UnityEngine;

namespace ForestFriendsQuest
{
    [Serializable]
    public class EvolutionStage
    {
        public int    bondLevel;            // minimum bond level for this stage
        public string stageId;              // e.g. "pip_stage2"
        public string stageName;            // e.g. "Blazing Fox"
        public string description;          // what changes visually/behaviourally
        public string spriteVariantId;      // key used to swap the creature sprite
        public string[] newGestures;        // unlocked gesture ids
        public string[] newDialogueLineIds; // unlocked dialogue lines
        public int    treatBonus;           // bonus treats when at this stage
    }

    [Serializable]
    public class CreatureEvolutionPath
    {
        public string          creatureId;
        public EvolutionStage[] stages;
    }

    /// <summary>
    /// Tracks and applies creature visual/behavioural evolution as bond levels rise.
    ///
    /// Each creature has 3 stages:
    ///   Stage 1 (bond 1-2): Default appearance, basic gestures
    ///   Stage 2 (bond 3-4): Glow accent added, 2 new gestures, bonus dialogue
    ///   Stage 3 (bond 5+):  Full radiant form, rare gestures, max treat bonus
    ///
    /// Persists current stage per creature via SaveSystem.
    /// Emits OnStageEvolved for UI/VFX celebrations.
    /// </summary>
    public class CreatureEvolutionSystem : MonoBehaviour
    {
        private EmotionalBondingEngine _bonding;
        private SaveSystem             _saveSystem;
        private AchievementSystem      _achievements;

        public event Action<string, EvolutionStage> OnStageEvolved;   // (creatureId, newStage)

        private readonly Dictionary<string, CreatureEvolutionPath> _paths =
            new Dictionary<string, CreatureEvolutionPath>();

        private readonly Dictionary<string, int> _currentStageIndex =
            new Dictionary<string, int>();

        // ─── Lifecycle ───────────────────────────────────────────────────────────

        public void Initialize(
            EmotionalBondingEngine bonding,
            SaveSystem saveSystem,
            AchievementSystem achievements)
        {
            _bonding      = bonding;
            _saveSystem   = saveSystem;
            _achievements = achievements;

            BuildEvolutionPaths();
            LoadStageState();

            // React to bond level changes
            if (_bonding != null)
                _bonding.OnBondLevelUp += OnCreatureBondLevelUp;
        }

        // ─── Public API ───────────────────────────────────────────────────────────

        /// <summary>Returns the creature's current active evolution stage.</summary>
        public EvolutionStage GetCurrentStage(string creatureId)
        {
            if (!_paths.TryGetValue(creatureId, out var path)) return null;
            var idx = _currentStageIndex.TryGetValue(creatureId, out var i) ? i : 0;
            return idx < path.stages.Length ? path.stages[idx] : path.stages[path.stages.Length - 1];
        }

        public string GetSpriteVariantId(string creatureId)
        {
            var stage = GetCurrentStage(creatureId);
            return stage?.spriteVariantId ?? creatureId;
        }

        public bool HasGestureUnlocked(string creatureId, string gestureId)
        {
            var stage = GetCurrentStage(creatureId);
            if (stage?.newGestures == null) return false;
            foreach (var g in stage.newGestures)
                if (g == gestureId) return true;
            return false;
        }

        /// <summary>Debug/QA: immediately advance a creature to its next evolution stage.</summary>
        public void ForceEvolve(string creatureId)
        {
            if (!_paths.TryGetValue(creatureId, out var path)) return;
            var idx     = _currentStageIndex.TryGetValue(creatureId, out var i) ? i : 0;
            var nextIdx = Mathf.Min(idx + 1, path.stages.Length - 1);
            _currentStageIndex[creatureId] = nextIdx;
            PersistStage(creatureId, nextIdx);
            OnStageEvolved?.Invoke(creatureId, path.stages[nextIdx]);
            Debug.Log($"[CreatureEvolution] Force-evolved {creatureId} to stage {nextIdx}");
        }

        // ─── Bond Level Reaction ─────────────────────────────────────────────────

        private void OnCreatureBondLevelUp(string creatureId, int newBondLevel)
        {
            if (!_paths.TryGetValue(creatureId, out var path)) return;

            var currentIdx = _currentStageIndex.TryGetValue(creatureId, out var i) ? i : 0;

            // Find the highest stage this bond level qualifies for
            var targetIdx = currentIdx;
            for (var s = 0; s < path.stages.Length; s++)
            {
                if (newBondLevel >= path.stages[s].bondLevel)
                    targetIdx = s;
            }

            if (targetIdx > currentIdx)
            {
                _currentStageIndex[creatureId] = targetIdx;
                PersistStage(creatureId, targetIdx);

                var newStage = path.stages[targetIdx];
                OnStageEvolved?.Invoke(creatureId, newStage);

                // Unlock bonding achievements on stage 2
                if (targetIdx == 1)
                    _achievements?.TryUnlock($"bond_{creatureId}_1");

                // Unlock max achievement on stage 3
                if (targetIdx == 2)
                    _achievements?.TryUnlock($"bond_{creatureId}_5");

                Debug.Log($"[CreatureEvolution] {creatureId} evolved to stage {targetIdx}: {newStage.stageName}");
            }
        }

        // ─── Persistence ─────────────────────────────────────────────────────────

        private void PersistStage(string creatureId, int stageIndex)
        {
            if (_saveSystem != null)
                _saveSystem.SetAchievementUnlocked($"Evolution.{creatureId}.{stageIndex}", true);
        }

        private void LoadStageState()
        {
            if (_saveSystem == null) return;
            foreach (var kv in _paths)
            {
                var id    = kv.Key;
                var path  = kv.Value;
                var stage = 0;
                for (var s = path.stages.Length - 1; s >= 0; s--)
                {
                    if (_saveSystem.IsAchievementUnlocked($"Evolution.{id}.{s}"))
                    {
                        stage = s;
                        break;
                    }
                }
                _currentStageIndex[id] = stage;
            }
        }

        // ─── Evolution Path Definitions ───────────────────────────────────────────

        private void BuildEvolutionPaths()
        {
            // ── Pip the Fox ───────────────────────────────────────────────────────
            AddPath("pip", new[]
            {
                new EvolutionStage
                {
                    bondLevel       = 1,
                    stageId         = "pip_stage1",
                    stageName       = "Forest Fox",
                    description     = "Pip has a warm russet coat. Curious and playful.",
                    spriteVariantId = "pip_v1",
                    newGestures     = Array.Empty<string>(),
                    newDialogueLineIds = new[] { "pip_greeting_1", "pip_greeting_2" },
                    treatBonus      = 0
                },
                new EvolutionStage
                {
                    bondLevel       = 3,
                    stageId         = "pip_stage2",
                    stageName       = "Ember Fox",
                    description     = "Pip's tail tip glows amber at dusk. A new spin dance unlocked.",
                    spriteVariantId = "pip_v2",
                    newGestures     = new[] { "spin_dance", "tail_glow" },
                    newDialogueLineIds = new[] { "pip_bond3_1", "pip_bond3_2" },
                    treatBonus      = 1
                },
                new EvolutionStage
                {
                    bondLevel       = 5,
                    stageId         = "pip_stage3",
                    stageName       = "Radiant Fox",
                    description     = "Pip radiates golden light. Friends across the meadow can see the glow.",
                    spriteVariantId = "pip_v3",
                    newGestures     = new[] { "radiant_leap", "pollen_burst_dance" },
                    newDialogueLineIds = new[] { "pip_bond5_1", "pip_bond5_2", "pip_bond5_3" },
                    treatBonus      = 3
                }
            });

            // ── Mimi the Bird ─────────────────────────────────────────────────────
            AddPath("mimi", new[]
            {
                new EvolutionStage
                {
                    bondLevel       = 1,
                    stageId         = "mimi_stage1",
                    stageName       = "Song Sparrow",
                    description     = "Mimi hops and chirps. Soft blue feathers.",
                    spriteVariantId = "mimi_v1",
                    newGestures     = Array.Empty<string>(),
                    newDialogueLineIds = new[] { "mimi_greeting_1" },
                    treatBonus      = 0
                },
                new EvolutionStage
                {
                    bondLevel       = 3,
                    stageId         = "mimi_stage2",
                    stageName       = "Melody Bird",
                    description     = "Mimi's songs now unlock music puzzle hints. Tail feathers shimmer.",
                    spriteVariantId = "mimi_v2",
                    newGestures     = new[] { "melody_dance", "feather_shower" },
                    newDialogueLineIds = new[] { "mimi_bond3_1" },
                    treatBonus      = 1
                },
                new EvolutionStage
                {
                    bondLevel       = 5,
                    stageId         = "mimi_stage3",
                    stageName       = "Aurora Bird",
                    description     = "Mimi's feathers reflect all forest colors. Her song is heard across every zone.",
                    spriteVariantId = "mimi_v3",
                    newGestures     = new[] { "aurora_flight" },
                    newDialogueLineIds = new[] { "mimi_bond5_1", "mimi_bond5_2" },
                    treatBonus      = 3
                }
            });

            // ── Tomo the Turtle ───────────────────────────────────────────────────
            AddPath("tomo", new[]
            {
                new EvolutionStage
                {
                    bondLevel       = 1, stageId = "tomo_stage1", stageName = "River Turtle",
                    description     = "Tomo is slow, steady, and wise. Mossy shell.",
                    spriteVariantId = "tomo_v1",
                    newGestures     = Array.Empty<string>(),
                    newDialogueLineIds = new[] { "tomo_greeting_1" },
                    treatBonus      = 0
                },
                new EvolutionStage
                {
                    bondLevel       = 3, stageId = "tomo_stage2", stageName = "Crystal Turtle",
                    description     = "Tomo's shell has grown small crystal formations on the edges.",
                    spriteVariantId = "tomo_v2",
                    newGestures     = new[] { "shell_spin", "water_ripple" },
                    newDialogueLineIds = new[] { "tomo_bond3_1" },
                    treatBonus      = 1
                },
                new EvolutionStage
                {
                    bondLevel       = 5, stageId = "tomo_stage3", stageName = "Ancient Turtle",
                    description     = "Tomo's shell glows with ancient rune carvings. A living map.",
                    spriteVariantId = "tomo_v3",
                    newGestures     = new[] { "rune_map_reveal" },
                    newDialogueLineIds = new[] { "tomo_bond5_1" },
                    treatBonus      = 3
                }
            });

            // ── Luma the Firefly ──────────────────────────────────────────────────
            AddPath("luma", new[]
            {
                new EvolutionStage
                {
                    bondLevel       = 1, stageId = "luma_stage1", stageName = "Glow Firefly",
                    description     = "Luma flickers softly. A guiding light in the dark.",
                    spriteVariantId = "luma_v1",
                    newGestures     = Array.Empty<string>(),
                    newDialogueLineIds = new[] { "luma_greeting_1" },
                    treatBonus      = 0
                },
                new EvolutionStage
                {
                    bondLevel       = 3, stageId = "luma_stage2", stageName = "Lantern Firefly",
                    description     = "Luma's glow now illuminates hidden marsh paths at night.",
                    spriteVariantId = "luma_v2",
                    newGestures     = new[] { "trail_light", "colony_call" },
                    newDialogueLineIds = new[] { "luma_bond3_1" },
                    treatBonus      = 1
                },
                new EvolutionStage
                {
                    bondLevel       = 5, stageId = "luma_stage3", stageName = "Star Firefly",
                    description     = "Luma glows like a tiny star. Firefly colonies follow her in formation.",
                    spriteVariantId = "luma_v3",
                    newGestures     = new[] { "star_formation_dance" },
                    newDialogueLineIds = new[] { "luma_bond5_1" },
                    treatBonus      = 3
                }
            });

            // ── Nori the Deer ─────────────────────────────────────────────────────
            AddPath("nori", new[]
            {
                new EvolutionStage
                {
                    bondLevel       = 1, stageId = "nori_stage1", stageName = "Forest Deer",
                    description     = "Nori is graceful and protective of Elderwood Grove.",
                    spriteVariantId = "nori_v1",
                    newGestures     = Array.Empty<string>(),
                    newDialogueLineIds = new[] { "nori_greeting_1" },
                    treatBonus      = 0
                },
                new EvolutionStage
                {
                    bondLevel       = 3, stageId = "nori_stage2", stageName = "Grove Guardian",
                    description     = "Nori's antlers sprout tiny flowers. Her presence calms forest creatures.",
                    spriteVariantId = "nori_v2",
                    newGestures     = new[] { "flower_antler_sway", "guardian_stomp" },
                    newDialogueLineIds = new[] { "nori_bond3_1" },
                    treatBonus      = 1
                },
                new EvolutionStage
                {
                    bondLevel       = 5, stageId = "nori_stage3", stageName = "Sacred Deer",
                    description     = "Nori's antlers glow with ancient forest light. She reveals hidden groves.",
                    spriteVariantId = "nori_v3",
                    newGestures     = new[] { "sacred_path_reveal" },
                    newDialogueLineIds = new[] { "nori_bond5_1" },
                    treatBonus      = 3
                }
            });

            // ── Sol the Owl ───────────────────────────────────────────────────────
            AddPath("sol", new[]
            {
                new EvolutionStage
                {
                    bondLevel       = 1, stageId = "sol_stage1", stageName = "Dusk Owl",
                    description     = "Sol watches from high branches with amber eyes.",
                    spriteVariantId = "sol_v1",
                    newGestures     = Array.Empty<string>(),
                    newDialogueLineIds = new[] { "sol_greeting_1" },
                    treatBonus      = 0
                },
                new EvolutionStage
                {
                    bondLevel       = 3, stageId = "sol_stage2", stageName = "Rune Owl",
                    description     = "Sol's feathers carry glowing rune symbols. Hints cipher solutions.",
                    spriteVariantId = "sol_v2",
                    newGestures     = new[] { "rune_trace", "wisdom_hoot" },
                    newDialogueLineIds = new[] { "sol_bond3_1", "sol_bond3_2" },
                    treatBonus      = 2
                },
                new EvolutionStage
                {
                    bondLevel       = 5, stageId = "sol_stage3", stageName = "Arch-Druid Owl",
                    description     = "Sol shines with cosmic starlight. Opening the Endless Dream Forest.",
                    spriteVariantId = "sol_v3",
                    newGestures     = new[] { "starfield_wings", "dream_forest_key" },
                    newDialogueLineIds = new[] { "sol_bond5_1", "sol_bond5_2", "sol_bond5_3" },
                    treatBonus      = 5
                }
            });
        }

        private void AddPath(string creatureId, EvolutionStage[] stages)
        {
            _paths[creatureId] = new CreatureEvolutionPath
            {
                creatureId = creatureId,
                stages     = stages
            };
            _currentStageIndex[creatureId] = 0;
        }
    }
}
