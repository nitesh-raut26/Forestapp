using System;
using System.Text;
using UnityEngine;

namespace ForestFriendsQuest
{
    /// <summary>
    /// Generates weekly family insight reports for the Parent Dashboard.
    ///
    /// Every 7 days, compiles a warm, readable summary of:
    ///   - Puzzles solved and zones explored
    ///   - Cognitive skill areas exercised (memory, logic, spatial, music)
    ///   - Creature bonds formed
    ///   - Total engaged play time
    ///   - Recommended next learning area
    ///
    /// Reports are written in parent-friendly language — no jargon.
    /// </summary>
    public class WeeklyReportGenerator : MonoBehaviour
    {
        // ─── Events ──────────────────────────────────────────────────────────────

        public event Action<WeeklyReport> OnReportGenerated;

        // ─── Dependencies ─────────────────────────────────────────────────────────

        private CognitiveAnalyticsSystem _analytics;
        private EmotionalBondingEngine   _bonding;
        private AchievementSystem        _achievements;
        private RetentionPacingSystem    _retention;

        private const string LastReportKey = "FFQ.LastReportDate";

        // ─── Lifecycle ───────────────────────────────────────────────────────────

        public void Initialize(
            CognitiveAnalyticsSystem analytics,
            EmotionalBondingEngine   bonding,
            AchievementSystem        achievements,
            RetentionPacingSystem    retention)
        {
            _analytics    = analytics;
            _bonding      = bonding;
            _achievements = achievements;
            _retention    = retention;

            CheckAndGenerateIfDue();
        }

        // ─── Public API ───────────────────────────────────────────────────────────

        /// <summary>Generate a fresh weekly report right now.</summary>
        public WeeklyReport GenerateReport()
        {
            var report = new WeeklyReport
            {
                generatedDate       = DateTime.Today.ToString("yyyy-MM-dd"),
                weekNumber          = GetISOWeekNumber(DateTime.Today),
                daysActive          = _retention?.DailyStreak ?? 0,
                totalPuzzlesSolved  = _analytics?.GetSessionCount() ?? 0,
                memoryChallenges    = _analytics?.GetSkillScore("memory") ?? 0f,
                logicChallenges     = _analytics?.GetSkillScore("logic") ?? 0f,
                spatialChallenges   = _analytics?.GetSkillScore("spatial") ?? 0f,
                musicChallenges     = _analytics?.GetSkillScore("music") ?? 0f,
                creatureBonds       = GetTotalBonds(),
                achievementsEarned  = _achievements?.GetUnlockedCount() ?? 0,
                sessionMinutes      = _retention?.GetSessionMinutesPlayed() ?? 0f,
                parentSummary       = BuildParentSummary(),
                recommendedFocus    = GetRecommendedFocus(),
            };

            PlayerPrefs.SetString(LastReportKey, DateTime.Today.ToString("yyyy-MM-dd"));
            PlayerPrefs.Save();

            OnReportGenerated?.Invoke(report);
            Debug.Log($"[WeeklyReportGenerator] Report generated: Week {report.weekNumber}");
            return report;
        }

        public bool IsReportDue()
        {
            var last = PlayerPrefs.GetString(LastReportKey, string.Empty);
            if (string.IsNullOrEmpty(last)) return true;
            if (DateTime.TryParse(last, out var lastDate))
                return (DateTime.Today - lastDate).TotalDays >= 7;
            return true;
        }

        // ─── Private Helpers ──────────────────────────────────────────────────────

        private void CheckAndGenerateIfDue()
        {
            if (IsReportDue())
                GenerateReport();
        }

        private int GetTotalBonds()
        {
            if (_bonding == null) return 0;
            int total = 0;
            foreach (var id in new[] { "pip", "mimi", "tomo", "luma", "nori", "sol" })
                total += _bonding.GetBondLevel(id);
            return total;
        }

        private string BuildParentSummary()
        {
            var sb = new StringBuilder();
            var days = _retention?.DailyStreak ?? 0;
            var puzzles = _analytics?.GetSessionCount() ?? 0;

            sb.AppendLine($"Your child played for {days} days this week and solved {puzzles} puzzles.");

            float topSkill = 0; string topSkillName = "memory";
            var skills = new[] { ("memory", "Memory"), ("logic", "Logic"), ("spatial", "Spatial thinking"), ("music", "Music recognition") };
            foreach (var (key, label) in skills)
            {
                float score = _analytics?.GetSkillScore(key) ?? 0f;
                if (score > topSkill) { topSkill = score; topSkillName = label; }
            }

            sb.AppendLine($"Their strongest skill this week was {topSkillName}.");
            sb.AppendLine($"They formed {GetTotalBonds()} total creature bonds — great for emotional engagement!");
            sb.AppendLine("All content is age-appropriate, ad-free, and educational.");
            return sb.ToString();
        }

        private string GetRecommendedFocus()
        {
            // Suggest the weakest skill area
            float lowest = float.MaxValue; string name = "spatial";
            var map = new[] { ("spatial", "Spatial puzzles"), ("music", "Music patterns"), ("logic", "Logic trails"), ("memory", "Memory sequences") };
            foreach (var (key, label) in map)
            {
                float s = _analytics?.GetSkillScore(key) ?? 0f;
                if (s < lowest) { lowest = s; name = label; }
            }
            return $"Try more {name} this week to build balanced skills!";
        }

        private static int GetISOWeekNumber(DateTime date)
        {
            System.Globalization.CultureInfo ci = System.Globalization.CultureInfo.InvariantCulture;
            return ci.Calendar.GetWeekOfYear(date, System.Globalization.CalendarWeekRule.FirstFourDayWeek, DayOfWeek.Monday);
        }
    }

    // ─── Data Types ───────────────────────────────────────────────────────────────

    [Serializable]
    public class WeeklyReport
    {
        public string generatedDate;
        public int    weekNumber;
        public int    daysActive;
        public int    totalPuzzlesSolved;
        public float  memoryChallenges;
        public float  logicChallenges;
        public float  spatialChallenges;
        public float  musicChallenges;
        public int    creatureBonds;
        public int    achievementsEarned;
        public float  sessionMinutes;
        public string parentSummary;
        public string recommendedFocus;
    }
}
