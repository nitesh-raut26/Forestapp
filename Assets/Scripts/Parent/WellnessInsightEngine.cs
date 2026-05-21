using System;
using System.Collections.Generic;
using UnityEngine;

namespace ForestFriendsQuest
{
    /// <summary>
    /// Emotional wellness insight engine for the Parent Dashboard.
    ///
    /// Analyses play session patterns to surface:
    ///   - Engagement quality (not just quantity)
    ///   - Emotional state signals (frustration vs flow vs boredom)
    ///   - Healthy play time monitoring
    ///   - Positive learning momentum indicators
    ///
    /// All insights are framed in warm, positive, trust-building language.
    /// This is NOT a surveillance tool — it's a parent partnership tool.
    /// </summary>
    public class WellnessInsightEngine : MonoBehaviour
    {
        // ─── Events ──────────────────────────────────────────────────────────────

        public event Action<WellnessInsight> OnInsightGenerated;

        // ─── Dependencies ─────────────────────────────────────────────────────────

        private CognitiveAnalyticsSystem _analytics;
        private DynamicDifficultySystem  _difficulty;
        private EmotionalBondingEngine   _bonding;
        private RetentionPacingSystem    _retention;

        // ─── Insight Cache ────────────────────────────────────────────────────────

        private readonly List<WellnessInsight> _recentInsights = new();
        private float _lastInsightTime;
        private const float InsightCooldown = 300f; // 5 min minimum between insights

        // ─── Lifecycle ───────────────────────────────────────────────────────────

        public void Initialize(
            CognitiveAnalyticsSystem analytics,
            DynamicDifficultySystem  difficulty,
            EmotionalBondingEngine   bonding,
            RetentionPacingSystem    retention)
        {
            _analytics  = analytics;
            _difficulty = difficulty;
            _bonding    = bonding;
            _retention  = retention;
        }

        private void Update()
        {
            if (Time.time - _lastInsightTime > InsightCooldown)
            {
                _lastInsightTime = Time.time;
                EvaluateAndEmit();
            }
        }

        // ─── Public API ───────────────────────────────────────────────────────────

        /// <summary>Generate a full wellness snapshot on demand (e.g., parent opens dashboard).</summary>
        public WellnessSnapshot GetCurrentSnapshot()
        {
            return new WellnessSnapshot
            {
                engagementLevel    = GetEngagementLevel(),
                emotionalState     = GetEmotionalStateLabel(),
                learningMomentum   = GetLearningMomentum(),
                sessionHealthLabel = GetSessionHealthLabel(),
                sessionMinutes     = _retention?.GetSessionMinutesPlayed() ?? 0f,
                topCreatureBond    = GetTopCreatureBond(),
                insights           = new List<WellnessInsight>(_recentInsights),
                parentMessage      = BuildParentMessage(),
            };
        }

        public IReadOnlyList<WellnessInsight> GetRecentInsights() => _recentInsights;

        // ─── Private Analysis ─────────────────────────────────────────────────────

        private void EvaluateAndEmit()
        {
            var insight = AnalyseCurrentState();
            if (insight == null) return;

            _recentInsights.Add(insight);
            if (_recentInsights.Count > 20) _recentInsights.RemoveAt(0);
            OnInsightGenerated?.Invoke(insight);
        }

        private WellnessInsight AnalyseCurrentState()
        {
            // Check for frustration signal — too many retries
            float errorRate = _analytics?.GetErrorRate() ?? 0f;
            if (errorRate > 0.6f)
                return new WellnessInsight { type = InsightType.FrustrationSignal, message = "Your child is working through a challenging puzzle — this builds resilience!", timestamp = DateTime.Now };

            // Check for flow state — consistent success
            float successRate = _analytics?.GetSuccessRate() ?? 0f;
            if (successRate > 0.85f)
                return new WellnessInsight { type = InsightType.FlowState, message = "Your child is in a learning flow state — great cognitive engagement!", timestamp = DateTime.Now };

            // Check for session length
            float minutes = _retention?.GetSessionMinutesPlayed() ?? 0f;
            if (minutes > 25f)
                return new WellnessInsight { type = InsightType.SessionLengthAlert, message = "25 minutes of focused play — a natural break time is approaching.", timestamp = DateTime.Now };

            return null;
        }

        private float GetEngagementLevel()
        {
            float success = _analytics?.GetSuccessRate() ?? 0.5f;
            float bond    = Mathf.Min(1f, GetTotalBondPoints() / 50f);
            return (success + bond) * 0.5f;
        }

        private string GetEmotionalStateLabel()
        {
            float err = _analytics?.GetErrorRate() ?? 0f;
            return err switch { > 0.7f => "Persevering", < 0.2f => "Thriving", _ => "Engaged" };
        }

        private float GetLearningMomentum()
        {
            // Rising success rate implies momentum
            return _analytics?.GetSuccessRate() ?? 0.5f;
        }

        private string GetSessionHealthLabel()
        {
            float min = _retention?.GetSessionMinutesPlayed() ?? 0f;
            return min switch { < 10f => "Just starting", < 20f => "Healthy", < 25f => "Good", _ => "Break recommended" };
        }

        private (string name, int level) GetTopCreatureBond()
        {
            string top = "pip"; int topLevel = 0;
            foreach (var id in new[] { "pip", "mimi", "tomo", "luma", "nori", "sol" })
            {
                int lvl = _bonding?.GetBondLevel(id) ?? 0;
                if (lvl > topLevel) { top = id; topLevel = lvl; }
            }
            return (top, topLevel);
        }

        private int GetTotalBondPoints()
        {
            int total = 0;
            foreach (var id in new[] { "pip", "mimi", "tomo", "luma", "nori", "sol" })
                total += _bonding?.GetBondLevel(id) ?? 0;
            return total;
        }

        private string BuildParentMessage()
        {
            var state = GetEmotionalStateLabel();
            var bond  = GetTopCreatureBond();
            return $"Your child is {state.ToLower()} in the forest. They have the strongest bond with {bond.name} (level {bond.level}). " +
                   "All play is educational, ad-free, and parent-approved.";
        }
    }

    // ─── Data Types ───────────────────────────────────────────────────────────────

    public enum InsightType { FlowState, FrustrationSignal, SessionLengthAlert, MilestoneCelebration, BondMilestone }

    [Serializable]
    public class WellnessInsight
    {
        public InsightType type;
        public string      message;
        public DateTime    timestamp;
    }

    [Serializable]
    public class WellnessSnapshot
    {
        public float                  engagementLevel;
        public string                 emotionalState;
        public float                  learningMomentum;
        public string                 sessionHealthLabel;
        public float                  sessionMinutes;
        public (string name, int level) topCreatureBond;
        public List<WellnessInsight>  insights;
        public string                 parentMessage;
    }
}
