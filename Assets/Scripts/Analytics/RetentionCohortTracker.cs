using System;
using UnityEngine;

namespace ForestFriendsQuest
{
    /// <summary>
    /// RetentionCohortTracker — tracks D1/D3/D7/D30 retention without PII.
    ///
    /// Uses a first-install epoch stored in PlayerPrefs. On each session start
    /// it computes the day number and fires a cohort event to Firebase. The day
    /// number is a bucketed integer — no exact timestamp is ever transmitted.
    ///
    /// COPPA note: day number alone is not PII. No install timestamp is logged.
    /// </summary>
    public class RetentionCohortTracker : MonoBehaviour
    {
        private const string InstallEpochKey  = "FFQ.Analytics.InstallEpoch";
        private const string TotalSessionsKey = "FFQ.Analytics.Sessions";

        private FirebaseAnalyticsConnector _firebase;

        public void Initialize(FirebaseAnalyticsConnector firebase)
        {
            _firebase = firebase;
            EnsureInstallEpoch();
            RecordSession();
        }

        // ─── Public ──────────────────────────────────────────────────────────────

        /// <summary>Days since first install (0 = install day).</summary>
        public int DaysSinceInstall
        {
            get
            {
                var epoch = PlayerPrefs.GetInt(InstallEpochKey, 0);
                if (epoch == 0) return 0;
                var installDate = new DateTime(1970, 1, 1).AddDays(epoch);
                return (DateTime.UtcNow.Date - installDate.Date).Days;
            }
        }

        public int TotalSessions => PlayerPrefs.GetInt(TotalSessionsKey, 0);

        // ─── Private ─────────────────────────────────────────────────────────────

        private void EnsureInstallEpoch()
        {
            if (PlayerPrefs.HasKey(InstallEpochKey)) return;
            var epoch = (int)(DateTime.UtcNow.Date - new DateTime(1970, 1, 1)).TotalDays;
            PlayerPrefs.SetInt(InstallEpochKey, epoch);
            PlayerPrefs.Save();
        }

        private void RecordSession()
        {
            var sessions = TotalSessions + 1;
            PlayerPrefs.SetInt(TotalSessionsKey, sessions);
            PlayerPrefs.Save();

            var day = DaysSinceInstall;

            // Fire standard retention cohort milestones.
            if (day == 1)  LogCohort("d1");
            if (day == 3)  LogCohort("d3");
            if (day == 7)  LogCohort("d7");
            if (day == 14) LogCohort("d14");
            if (day == 30) LogCohort("d30");

            // Log a general session count event for funnel analysis.
            _firebase?.LogEvent("session_cohort", new()
            {
                { "day_number",     day.ToString() },
                { "session_number", sessions.ToString() }
            });
        }

        private void LogCohort(string cohortLabel)
        {
            _firebase?.LogEvent("retention_cohort", new()
            {
                { "cohort", cohortLabel }
            });
            Debug.Log($"[RetentionCohortTracker] Cohort: {cohortLabel}");
        }
    }
}
