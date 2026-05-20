using System;
using System.Collections.Generic;
using UnityEngine;

namespace ForestFriendsQuest
{
    [Serializable]
    public class CognitiveMetrics
    {
        public float spatialScore = 50f;
        public float patternScore = 50f;
        public float logicScore = 50f;
    }

    [Serializable]
    public class CognitiveReport
    {
        public int totalAttempts;
        public int totalClears;
        public int totalMistakes;
        public int totalHints;
        public float avgCompletionTime;
        public float spatialScore;
        public float patternScore;
        public float logicScore;
        public float frustrationScore;
        public float boredomScore;
    }

    public class CognitiveAnalyticsSystem : MonoBehaviour
    {
        private CognitiveMetrics _metrics = new CognitiveMetrics();
        private float _frustrationScore = 0f;
        private float _boredomScore = 0f;

        private int _totalAttempts;
        private int _totalClears;
        private int _totalMistakes;
        private int _totalHints;
        private float _totalTimeSpent;
        private int _levelPlaysCount;

        private int _consecutiveFailures;
        private int _consecutiveFlawlessClears;

        public float FrustrationScore => _frustrationScore;
        public float BoredomScore => _boredomScore;
        public CognitiveMetrics Metrics => _metrics;

        public void RecordPuzzleAttempt(string puzzleType, bool success, int mistakes, bool hintUsed, float timeSeconds)
        {
            _totalAttempts++;
            _totalTimeSpent += timeSeconds;
            _levelPlaysCount++;

            if (success)
            {
                _totalClears++;
                _totalMistakes += mistakes;
                if (hintUsed) _totalHints++;

                // Adjust consecutive streams
                if (mistakes == 0 && !hintUsed)
                {
                    _consecutiveFlawlessClears++;
                    _consecutiveFailures = 0;
                }
                else
                {
                    _consecutiveFlawlessClears = 0;
                    _consecutiveFailures = 0;
                }

                // Adjust specific metrics
                UpdateMetricsOnSuccess(puzzleType, mistakes, hintUsed, timeSeconds);
            }
            else
            {
                _totalMistakes++;
                _consecutiveFailures++;
                _consecutiveFlawlessClears = 0;

                UpdateMetricsOnFailure(puzzleType);
            }

            // Calculate frustration and boredom dynamic scores (clamped between 0 and 1)
            CalculateEcosystemFeel();
        }

        private void UpdateMetricsOnSuccess(string puzzleType, int mistakes, bool hintUsed, float timeSeconds)
        {
            float reward = 5f;
            if (mistakes > 0) reward -= 1.5f * mistakes;
            if (hintUsed) reward -= 2f;
            if (timeSeconds > 40f) reward -= 1f;

            reward = Mathf.Max(0.5f, reward);

            switch (puzzleType?.ToLower())
            {
                case "memory":
                case "pattern":
                    _metrics.patternScore = Mathf.Clamp(_metrics.patternScore + reward, 0f, 100f);
                    break;
                case "path":
                case "spatial":
                    _metrics.spatialScore = Mathf.Clamp(_metrics.spatialScore + reward, 0f, 100f);
                    break;
                default: // choice or general logic
                    _metrics.logicScore = Mathf.Clamp(_metrics.logicScore + reward, 0f, 100f);
                    break;
            }
        }

        private void UpdateMetricsOnFailure(string puzzleType)
        {
            float penalty = 2f;
            switch (puzzleType?.ToLower())
            {
                case "memory":
                case "pattern":
                    _metrics.patternScore = Mathf.Clamp(_metrics.patternScore - penalty, 0f, 100f);
                    break;
                case "path":
                case "spatial":
                    _metrics.spatialScore = Mathf.Clamp(_metrics.spatialScore - penalty, 0f, 100f);
                    break;
                default:
                    _metrics.logicScore = Mathf.Clamp(_metrics.logicScore - penalty, 0f, 100f);
                    break;
            }
        }

        private void CalculateEcosystemFeel()
        {
            // Consecutive failures trigger frustration
            _frustrationScore = Mathf.Clamp01(_consecutiveFailures * 0.25f + (_totalHints > 4 ? 0.1f : 0f));
            
            // Consecutive perfect clears trigger boredom (app needs higher difficulty scaling)
            _boredomScore = Mathf.Clamp01(_consecutiveFlawlessClears * 0.2f - (_frustrationScore * 0.5f));
        }

        public CognitiveReport GetReport()
        {
            return new CognitiveReport
            {
                totalAttempts = _totalAttempts,
                totalClears = _totalClears,
                totalMistakes = _totalMistakes,
                totalHints = _totalHints,
                avgCompletionTime = _levelPlaysCount > 0 ? _totalTimeSpent / _levelPlaysCount : 0f,
                spatialScore = _metrics.spatialScore,
                patternScore = _metrics.patternScore,
                logicScore = _metrics.logicScore,
                frustrationScore = _frustrationScore,
                boredomScore = _boredomScore
            };
        }

        public void HydrateFromSave(int attempts, int mistakes, int hints)
        {
            _totalAttempts = attempts;
            _totalMistakes = mistakes;
            _totalHints = hints;
        }

        public void ResetAnalytics()
        {
            _metrics = new CognitiveMetrics();
            _frustrationScore = 0f;
            _boredomScore = 0f;
            _totalAttempts = 0;
            _totalClears = 0;
            _totalMistakes = 0;
            _totalHints = 0;
            _totalTimeSpent = 0f;
            _levelPlaysCount = 0;
            _consecutiveFailures = 0;
            _consecutiveFlawlessClears = 0;
        }
    }
}
