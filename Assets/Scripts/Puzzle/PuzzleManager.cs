using System;
using System.Collections.Generic;
using UnityEngine;

namespace ForestFriendsQuest
{
    public enum PuzzleType
    {
        MemoryTrail,
        RuneSequence,
        LogicMirror,
        MusicPattern,
        SymbolCipher,
        ForestRouting,
        PressureGate,
        LightReflection,
        RotatingPath,
        TimeMemory
    }

    public enum PuzzleResult
    {
        InProgress,
        Solved,
        Failed
    }

    [Serializable]
    public class PuzzleAttemptRecord
    {
        public string  puzzleType;
        public bool    success;
        public int     mistakes;
        public bool    hintUsed;
        public float   timeSeconds;
        public int     stars;
    }

    /// <summary>
    /// Central puzzle orchestrator. Routes to specific puzzle implementations,
    /// feeds results to CognitiveAnalyticsSystem and DynamicDifficultySystem,
    /// triggers VFX on solve/fail, and manages hint timing.
    /// </summary>
    public class PuzzleManager : MonoBehaviour
    {
        // ─── System Links ─────────────────────────────────────────────────────────

        private CognitiveAnalyticsSystem  _analytics;
        private DynamicDifficultySystem   _difficulty;
        private ProceduralAudioSystem     _audio;
        private VFXManager                _vfx;
        private QuestEngine               _quests;

        // ─── Active Puzzle State ─────────────────────────────────────────────────

        private PuzzleType    _activePuzzleType;
        private string        _currentTier;
        private bool          _puzzleActive;
        private float         _puzzleStartTime;
        private int           _currentMistakes;
        private bool          _hintUsed;

        private float         _hintCooldown;
        private bool          _hintAvailable;

        // ─── Events ───────────────────────────────────────────────────────────────

        public event Action<PuzzleAttemptRecord> OnPuzzleCompleted;
        public event Action                      OnPuzzleFailed;
        public event Action                      OnHintAvailable;

        // ─── Lifecycle ───────────────────────────────────────────────────────────

        public void Initialize(
            CognitiveAnalyticsSystem  analytics,
            DynamicDifficultySystem   difficulty,
            ProceduralAudioSystem     audio,
            VFXManager                vfx,
            QuestEngine               quests)
        {
            _analytics  = analytics;
            _difficulty = difficulty;
            _audio      = audio;
            _vfx        = vfx;
            _quests     = quests;
        }

        private void Update()
        {
            if (!_puzzleActive) return;

            // Countdown hint availability
            if (!_hintAvailable)
            {
                _hintCooldown -= Time.deltaTime;
                if (_hintCooldown <= 0f)
                {
                    _hintAvailable = true;
                    OnHintAvailable?.Invoke();
                }
            }
        }

        // ─── Public API ───────────────────────────────────────────────────────────

        /// <summary>Begin a new puzzle session of the given type.</summary>
        public void StartPuzzle(PuzzleType type, string tier)
        {
            _activePuzzleType = type;
            _currentTier      = tier;
            _puzzleActive     = true;
            _puzzleStartTime  = Time.time;
            _currentMistakes  = 0;
            _hintUsed         = false;
            _hintAvailable    = false;
            _hintCooldown     = _difficulty?.GetHintDelay() ?? 10f;

            Debug.Log($"[PuzzleManager] Started {type} puzzle — tier: {tier}");
        }

        /// <summary>Record a correct step in the current puzzle.</summary>
        public void RecordCorrectStep(Vector2 vfxPos = default)
        {
            if (!_puzzleActive) return;
            _vfx?.OnPuzzleNodeSelect(vfxPos);
            _audio?.PlayTapCue();
        }

        /// <summary>Record a mistake. Soft feedback — not punishing.</summary>
        public void RecordMistake(Vector2 vfxPos = default)
        {
            if (!_puzzleActive) return;
            _currentMistakes++;
            _vfx?.OnPuzzleError(vfxPos);

            // Adaptive: report to analytics each mistake
            _analytics?.RecordPuzzleAttempt(
                _activePuzzleType.ToString(), false,
                _currentMistakes, _hintUsed,
                Time.time - _puzzleStartTime
            );
        }

        /// <summary>Use the available hint.</summary>
        public bool UseHint()
        {
            if (!_hintAvailable) return false;
            _hintUsed      = true;
            _hintAvailable = false;
            return true;
        }

        /// <summary>Call this when the puzzle is successfully solved.</summary>
        public void SolvePuzzle(Vector2 vfxPos = default)
        {
            if (!_puzzleActive) return;
            _puzzleActive = false;

            var elapsed = Time.time - _puzzleStartTime;
            var stars   = CalculateStars(elapsed);

            var record = new PuzzleAttemptRecord
            {
                puzzleType  = _activePuzzleType.ToString(),
                success     = true,
                mistakes    = _currentMistakes,
                hintUsed    = _hintUsed,
                timeSeconds = elapsed,
                stars       = stars
            };

            _analytics?.RecordPuzzleAttempt(
                record.puzzleType, true,
                _currentMistakes, _hintUsed, elapsed
            );

            _vfx?.OnPuzzleSolved(vfxPos);
            _audio?.PlayContextChord(true);

            // Progress relevant quest objectives
            ProgressQuestObjectives();

            OnPuzzleCompleted?.Invoke(record);
            Debug.Log($"[PuzzleManager] Solved {_activePuzzleType} — {stars} stars, {elapsed:F1}s, {_currentMistakes} mistakes");
        }

        /// <summary>Force-fail the current puzzle (e.g. time limit reached).</summary>
        public void FailPuzzle()
        {
            if (!_puzzleActive) return;
            _puzzleActive = false;

            _analytics?.RecordPuzzleAttempt(
                _activePuzzleType.ToString(), false,
                _currentMistakes, _hintUsed,
                Time.time - _puzzleStartTime
            );

            _audio?.PlayContextChord(false);
            OnPuzzleFailed?.Invoke();
        }

        // ─── Adaptive Query API ───────────────────────────────────────────────────

        public int GetAdaptedMemoryLength(int baseLength)
        {
            return _difficulty?.GetAdaptedMemoryLength(baseLength, _currentTier) ?? baseLength;
        }

        public Vector2Int GetAdaptedGridSize(int cols, int rows)
        {
            return _difficulty?.GetAdaptedGridDimensions(cols, rows, _currentTier)
                ?? new Vector2Int(cols, rows);
        }

        public bool ShouldShowGuides()
        {
            return _difficulty?.ShouldShowVisualGuides(_currentTier) ?? false;
        }

        public bool IsActive     => _puzzleActive;
        public bool HintReady    => _hintAvailable;
        public int  MistakeCount => _currentMistakes;

        // ─── Private Helpers ──────────────────────────────────────────────────────

        private int CalculateStars(float elapsed)
        {
            if (_currentMistakes == 0 && !_hintUsed && elapsed < 20f) return 3;
            if (_currentMistakes <= 1 && elapsed < 40f)               return 2;
            return 1;
        }

        private void ProgressQuestObjectives()
        {
            if (_quests == null) return;

            switch (_activePuzzleType)
            {
                case PuzzleType.MemoryTrail:
                    _quests.ProgressObjective("memory_trail_complete");
                    break;
                case PuzzleType.LogicMirror:
                    _quests.ProgressObjective("mirror_puzzle_solved");
                    break;
                case PuzzleType.RuneSequence:
                    _quests.ProgressObjective("rune_puzzle_solved");
                    break;
                case PuzzleType.MusicPattern:
                    _quests.ProgressObjective("music_pattern_complete");
                    break;
                case PuzzleType.SymbolCipher:
                    _quests.ProgressObjective("symbol_cipher_decoded");
                    break;
            }
        }
    }
}
