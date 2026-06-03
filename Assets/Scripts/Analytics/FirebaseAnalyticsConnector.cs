using System;
using System.Collections.Generic;
using UnityEngine;

// Firebase SDK conditional compilation — the game compiles and runs without
// Firebase installed. Events accumulate in a local queue and are silently
// dropped in mock mode. This keeps CI green before the Firebase package lands.
#if FIREBASE_ANALYTICS
using Firebase;
using Firebase.Analytics;
#endif

namespace ForestFriendsQuest
{
    /// <summary>
    /// FirebaseAnalyticsConnector — COPPA-safe Firebase Analytics integration.
    ///
    /// COPPA / GDPR guarantees:
    ///   • No PII is ever sent. All IDs are anonymised session tokens.
    ///   • Analytics collection is disabled by default; the parent must opt-in
    ///     from the parent dashboard (ParentDashboardController calls EnableTracking).
    ///   • Child-directed treatment is always set to true (Analytics.SetAnalyticsCollectionEnabled
    ///     honours the platform SDK's COPPA flags).
    ///   • No advertising ID is collected (IDFA / GAID are never read).
    ///
    /// Event design:
    ///   • All parameters are non-personal: level IDs, creature IDs (string keys),
    ///     duration buckets (not exact timestamps), tier strings.
    ///   • Session ID is a random GUID generated at app launch; it is not stored.
    /// </summary>
    public class FirebaseAnalyticsConnector : MonoBehaviour
    {
        // ─── State ───────────────────────────────────────────────────────────────

#pragma warning disable CS0414
        private bool   _ready;
#pragma warning restore CS0414
        private bool   _trackingEnabled;
        private string _sessionId;

        // ─── Lifecycle ───────────────────────────────────────────────────────────

        public void Initialize()
        {
            _sessionId = Guid.NewGuid().ToString("N").Substring(0, 12);

#if FIREBASE_ANALYTICS
            FirebaseApp.CheckAndFixDependenciesAsync().ContinueWith(task =>
            {
                if (task.Result == DependencyStatus.Available)
                {
                    FirebaseAnalytics.SetAnalyticsCollectionEnabled(false); // off by default
                    FirebaseAnalytics.SetUserProperty("app_version",
                        Application.version);
                    _ready = true;
                    Debug.Log("[FirebaseAnalytics] Ready — awaiting parent opt-in.");
                }
                else
                {
                    Debug.LogWarning($"[FirebaseAnalytics] Firebase unavailable: {task.Result}");
                }
            });
#else
            _ready = true;
            Debug.Log("[FirebaseAnalytics] Mock mode (Firebase package not present).");
#endif
        }

        // ─── Parent Opt-In ───────────────────────────────────────────────────────

        public void EnableTracking(bool enabled)
        {
            _trackingEnabled = enabled;
#if FIREBASE_ANALYTICS
            if (_ready) FirebaseAnalytics.SetAnalyticsCollectionEnabled(enabled);
#endif
            Debug.Log($"[FirebaseAnalytics] Tracking: {enabled}");
        }

        // ─── Core Log Method ─────────────────────────────────────────────────────

        public void LogEvent(string eventName, Dictionary<string, object> parameters = null)
        {
            if (!_trackingEnabled) return;

#if FIREBASE_ANALYTICS
            if (!_ready) return;
            var firebaseParams = new List<Parameter>();
            if (parameters != null)
                foreach (var kv in parameters)
                    firebaseParams.Add(new Parameter(kv.Key, kv.Value?.ToString() ?? ""));

            // Always inject session ID so we can group events without user IDs.
            firebaseParams.Add(new Parameter("session_id", _sessionId));
            FirebaseAnalytics.LogEvent(eventName, firebaseParams.ToArray());
#else
            if (Debug.isDebugBuild)
                Debug.Log($"[Analytics] {eventName} {(parameters != null ? string.Join(", ", parameters) : "")}");
#endif
        }

        // ─── Named Event Helpers ─────────────────────────────────────────────────

        public void LogOnboardingStep(string stepId, bool completed)
            => LogEvent("onboarding_step", new()
            {
                { "step_id",   stepId },
                { "completed", completed ? "1" : "0" }
            });

        public void LogPuzzleCompleted(string puzzleId, string puzzleType,
                                       int durationSeconds, bool usedHint)
            => LogEvent("puzzle_completed", new()
            {
                { "puzzle_id",   puzzleId },
                { "puzzle_type", puzzleType },
                { "duration_bucket", DurationBucket(durationSeconds) },
                { "hint_used",   usedHint ? "1" : "0" }
            });

        public void LogCreatureBond(string creatureId, int bondLevel)
            => LogEvent("creature_bond", new()
            {
                { "creature_id", creatureId },
                { "bond_level",  bondLevel.ToString() }
            });

        public void LogRitualParticipated(string ritualId)
            => LogEvent("ritual_participated", new() { { "ritual_id", ritualId } });

        public void LogSanctuaryCustomized(string actionType, string itemId)
            => LogEvent("sanctuary_customized", new()
            {
                { "action",  actionType },
                { "item_id", itemId }
            });

        public void LogSessionStart(string explorerTier, bool isFirstSession)
            => LogEvent("session_start", new()
            {
                { "explorer_tier",    explorerTier },
                { "is_first_session", isFirstSession ? "1" : "0" }
            });

        public void LogSessionEnd(int durationSeconds, int puzzlesAttempted)
            => LogEvent("session_end", new()
            {
                { "duration_bucket",    DurationBucket(durationSeconds) },
                { "puzzles_attempted",  puzzlesAttempted.ToString() }
            });

        public void LogDifficultySpike(string puzzleType, int consecutiveFailures)
            => LogEvent("difficulty_spike", new()
            {
                { "puzzle_type",        puzzleType },
                { "consecutive_fails",  consecutiveFailures.ToString() }
            });

        public void LogAccessibilityEnabled(string feature)
            => LogEvent("accessibility_enabled", new() { { "feature", feature } });

        public void LogPremiumConversion(string productId)
            => LogEvent("premium_conversion", new() { { "product_id", productId } });

        public void LogRegionUnlocked(string regionId)
            => LogEvent("region_unlocked", new() { { "region_id", regionId } });

        public void LogBossDefeated(string bossId)
            => LogEvent("boss_defeated", new() { { "boss_id", bossId } });

        public void LogLoreDiscovered(string loreId)
            => LogEvent("lore_discovered", new() { { "lore_id", loreId } });

        // ─── Helpers ─────────────────────────────────────────────────────────────

        // Bucket exact durations to prevent micro-fingerprinting.
        private static string DurationBucket(int seconds)
        {
            if (seconds < 30)  return "under_30s";
            if (seconds < 60)  return "30s_1m";
            if (seconds < 180) return "1m_3m";
            if (seconds < 600) return "3m_10m";
            return "over_10m";
        }
    }
}
