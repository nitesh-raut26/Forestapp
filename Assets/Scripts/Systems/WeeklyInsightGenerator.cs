using System;
using System.Collections.Generic;
using UnityEngine;

namespace ForestFriendsQuest
{
    /// <summary>
    /// Generates a weekly plain-English insight report for parents based on
    /// CognitiveAnalyticsSystem data and play patterns.
    ///
    /// Reports are cached for the current week and regenerated Monday.
    /// The language is warm, non-technical, and age-appropriate for parents.
    /// </summary>
    public class WeeklyInsightGenerator : MonoBehaviour
    {
        private CognitiveAnalyticsSystem _analytics;
        private AchievementSystem        _achievements;
        private EmotionalBondingEngine   _bonding;

        // ─── Lifecycle ────────────────────────────────────────────────────────────

        public void Initialize(CognitiveAnalyticsSystem analytics,
            AchievementSystem achievements, EmotionalBondingEngine bonding)
        {
            _analytics    = analytics;
            _achievements = achievements;
            _bonding      = bonding;
        }

        // ─── Public API ───────────────────────────────────────────────────────────

        /// <summary>Returns a full insight report for the parent dashboard.</summary>
        public WeeklyInsightReport GenerateReport()
        {
            var report  = _analytics?.GetReport();
            var insight = new WeeklyInsightReport();

            if (report == null)
            {
                insight.Summary  = "Play a few puzzles to see your child's learning insights here!";
                insight.IsEmpty  = true;
                return insight;
            }

            insight.WeekOf       = GetWeekStart();
            insight.SkillSummary = GenerateSkillSummary(report);
            insight.StrengthNote = GenerateStrengthNote(report);
            insight.GrowthNote   = GenerateGrowthNote(report);
            insight.PlayHabit    = GeneratePlayHabitNote(report);
            insight.Summary      = GenerateSummary(report);
            insight.Milestones   = GetRecentMilestones();

            return insight;
        }

        // ─── Generators ───────────────────────────────────────────────────────────

        private static string GenerateSkillSummary(CognitiveReport report)
        {
            var strongest = "logical reasoning";
            var score     = report.logicScore;

            if (report.spatialScore > score)  { strongest = "spatial intelligence"; score = report.spatialScore; }
            if (report.patternScore > score)  { strongest = "pattern recognition";  score = report.patternScore; }

            var level = score > 75f ? "exceptional"
                      : score > 55f ? "strong"
                      : score > 35f ? "growing"
                      : "early";

            return $"Showing {level} {strongest} this week.";
        }

        private static string GenerateStrengthNote(CognitiveReport report)
        {
            if (report.spatialScore >= report.patternScore && report.spatialScore >= report.logicScore)
                return "Your child excels at visualising shapes and spatial relationships — great for maths and science!";

            if (report.patternScore >= report.logicScore)
                return "Pattern recognition is a standout — this supports reading, music, and early coding skills.";

            return "Logical step-by-step reasoning is the strongest skill — excellent for problem-solving!";
        }

        private static string GenerateGrowthNote(CognitiveReport report)
        {
            var comfort = 1f - report.frustrationScore;

            if (comfort > 0.7f && report.totalClears > 5)
                return "Solving puzzles comfortably — consider exploring harder zones next session.";

            if (comfort < 0.4f && report.totalAttempts > report.totalClears * 2)
                return "Some puzzles felt challenging this week. That's great! Struggling builds resilience.";

            if (report.totalClears > 10)
                return "Impressive persistence — completed many puzzles this week!";

            return "Building momentum — every session adds new neural pathways.";
        }

        private string GeneratePlayHabitNote(CognitiveReport report)
        {
            var avgTime = report.avgCompletionTime;

            if (avgTime > 0 && avgTime < 30f)
                return "Quick puzzle solver — takes decisive action.";

            if (avgTime > 90f)
                return "Thoughtful and methodical — takes time to consider options carefully.";

            var bondCount = GetBondedCount();
            if (bondCount >= 4)
                return "Deeply engaged with creature bonding — shows nurturing and empathy.";

            return "Balanced play style across puzzles and exploration.";
        }

        private static string GenerateSummary(CognitiveReport report)
        {
            var overall = (report.spatialScore + report.patternScore + report.logicScore) / 3f;
            var tone    = overall > 65f ? "fantastic" : overall > 40f ? "good" : "steady";

            return $"Overall, a {tone} week in the forest. " +
                   $"{report.totalClears} puzzles completed across {report.totalAttempts} attempts.";
        }

        private List<string> GetRecentMilestones()
        {
            var milestones = new List<string>();

            if (_achievements == null) return milestones;

            // Check recently unlocked achievements (last 7 days via PlayerPrefs date tracking)
            var checkIds = new[] { "ach_first_clear", "ach_streak_7", "sea_daily_7",
                                   "ach_bond_2", "ach_bond_5", "ach_100_clears" };

            foreach (var id in checkIds)
            {
                if (_achievements.IsUnlocked(id))
                {
                    var label = GetAchievementLabel(id);
                    if (label != null) milestones.Add(label);
                }
            }

            return milestones;
        }

        private int GetBondedCount()
        {
            if (_bonding == null) return 0;
            var ids   = new[] { "pip", "mimi", "tomo", "luma", "nori", "sol" };
            var count = 0;
            foreach (var id in ids)
            {
                var bond = _bonding.GetBondState(id);
                if (bond != null && bond.bondLevel >= 2) count++;
            }
            return count;
        }

        private static string GetWeekStart()
        {
            var today = DateTime.Today;
            var diff  = (7 + (int)today.DayOfWeek - (int)DayOfWeek.Monday) % 7;
            return today.AddDays(-diff).ToString("MMM d");
        }

        private static string GetAchievementLabel(string id) => id switch
        {
            "ach_first_clear" => "First puzzle completed!",
            "ach_streak_7"    => "7-day play streak!",
            "sea_daily_7"     => "7 daily rituals in a row!",
            "ach_bond_2"      => "First creature friendship formed.",
            "ach_bond_5"      => "Creature became a companion.",
            "ach_100_clears"  => "100 puzzles solved!",
            _                 => null
        };
    }

    // ─── Report Data ──────────────────────────────────────────────────────────────

    public class WeeklyInsightReport
    {
        public string       WeekOf;
        public string       SkillSummary;
        public string       StrengthNote;
        public string       GrowthNote;
        public string       PlayHabit;
        public string       Summary;
        public List<string> Milestones = new List<string>();
        public bool         IsEmpty;
    }
}
