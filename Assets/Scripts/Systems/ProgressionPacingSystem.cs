using System;
using System.Collections.Generic;
using UnityEngine;

namespace ForestFriendsQuest
{
    /// <summary>
    /// Manages 200+ puzzle progression pacing to ensure:
    ///   - Correct difficulty curve (easy → medium → hard → boss)
    ///   - Variety of puzzle types throughout the journey
    ///   - Surprise moments (rare puzzles, hidden challenges, boss gates)
    ///   - Age-tier adaptation (Sprout / Scout / Druid paths)
    ///   - Replayability through procedurally varied parameters
    ///
    /// Works alongside DynamicDifficultySystem (real-time tuning) but focuses
    /// on the long-term content map, not individual puzzle tuning.
    /// </summary>
    public class ProgressionPacingSystem : MonoBehaviour
    {
        // ─── Events ──────────────────────────────────────────────────────────────

        public event Action<int>    OnMilestoneReached;     // puzzle count milestone
        public event Action<string> OnSurpriseMomentReady;  // surprise id
        public event Action<string> OnBossGateReached;      // zone id

        // ─── State ───────────────────────────────────────────────────────────────

        private SaveSystem              _save;
        private DynamicDifficultySystem _difficulty;
        private WorldStateManager       _world;

        private int   _totalPuzzlesCompleted;
        private int   _consecutivePerfectClears;
        private const int MilestoneInterval = 10;

        // ─── Milestone Data ───────────────────────────────────────────────────────

        private readonly Dictionary<int, string> _milestoneMessages = new()
        {
            {  10, "First ten puzzles — the forest stirs!" },
            {  25, "25 puzzles — you're a true Scout!" },
            {  50, "50 puzzles — the Elder Oak notices you." },
            { 100, "100 puzzles — a legendary Forest Guardian!" },
            { 200, "200 puzzles — you have mastered the ancient forest." },
        };

        // Boss gate zones
        private readonly Dictionary<string, int> _bossGates = new()
        {
            { "firefly-hollow",      5  },
            { "river-bend",          12 },
            { "moonlit-creek",       20 },
            { "elderwood-grove",     30 },
            { "crystal-caverns",     45 },
            { "forgotten-ruins",     62 },
            { "firefly-marsh",       75 },
            { "ancient-observatory", 90 },
            { "skyroot-canopy",      110 },
        };

        // ─── Lifecycle ───────────────────────────────────────────────────────────

        public void Initialize(SaveSystem save, DynamicDifficultySystem difficulty, WorldStateManager world)
        {
            _save       = save;
            _difficulty = difficulty;
            _world      = world;

            _totalPuzzlesCompleted = save?.ActiveData?.totalLevelsCleared ?? 0;
            Debug.Log($"[ProgressionPacingSystem] Starting at puzzle #{_totalPuzzlesCompleted}");
        }

        // ─── Public API ───────────────────────────────────────────────────────────

        public void RecordPuzzleCompleted(string zoneId, bool isPerfect, int stars)
        {
            _totalPuzzlesCompleted++;
            if (isPerfect) _consecutivePerfectClears++;
            else           _consecutivePerfectClears = 0;

            // Milestone check
            if (_totalPuzzlesCompleted % MilestoneInterval == 0)
                OnMilestoneReached?.Invoke(_totalPuzzlesCompleted);

            // Named milestone check
            if (_milestoneMessages.TryGetValue(_totalPuzzlesCompleted, out var msg))
                OnSurpriseMomentReady?.Invoke(msg);

            // Boss gate check
            if (_bossGates.TryGetValue(zoneId ?? "", out var required))
            {
                if (_totalPuzzlesCompleted >= required)
                    OnBossGateReached?.Invoke(zoneId);
            }

            // World unlock check
            _world?.OnLevelCleared(_totalPuzzlesCompleted);

            // Inform difficulty system of mastery
            if (_consecutivePerfectClears >= 3)
            {
                _difficulty?.RegisterPerfectRun();
                _consecutivePerfectClears = 0; // reset and escalate
            }

            Debug.Log($"[ProgressionPacingSystem] Puzzle #{_totalPuzzlesCompleted}, ★{stars}, perfect streak: {_consecutivePerfectClears}");
        }

        /// <summary>Returns the recommended puzzle mode for this stage of progression.</summary>
        public string GetRecommendedPuzzleMode(int puzzleIndex)
        {
            // Variety rotation based on index modulo
            var pool = puzzleIndex switch
            {
                < 10  => new[] { "choice", "choice", "memory" },
                < 25  => new[] { "choice", "memory", "path" },
                < 50  => new[] { "memory", "path", "choice", "light_reflection" },
                < 100 => new[] { "path", "light_reflection", "pressure_gate", "rotating_path", "rune_sequence" },
                _     => new[] { "rotating_path", "time_memory", "rune_sequence", "boss_cipher", "music_pattern" }
            };
            return pool[puzzleIndex % pool.Length];
        }

        /// <summary>Get a hint for how many puzzles until the next major milestone.</summary>
        public int PuzzlesUntilNextMilestone()
        {
            int next = ((_totalPuzzlesCompleted / MilestoneInterval) + 1) * MilestoneInterval;
            return next - _totalPuzzlesCompleted;
        }

        public int TotalPuzzlesCompleted => _totalPuzzlesCompleted;

        /// <summary>Returns true if a surprise hidden puzzle should appear now.</summary>
        public bool ShouldTriggerHiddenPuzzle()
            => _totalPuzzlesCompleted > 5 && UnityEngine.Random.value < 0.05f; // 5% chance
    }
}
