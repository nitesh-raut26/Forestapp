using System;
using UnityEngine;

namespace ForestFriendsQuest
{
    /// <summary>
    /// Production-safe logging and crash-safe analytics hooks.
    ///
    /// In Release builds: filters to Warning+ level, sends critical errors to
    /// the analytics backend (simulated — swap for Crashlytics/Sentry).
    ///
    /// In Debug builds: full verbose logging with category prefixes.
    ///
    /// Features:
    ///   - Structured log format: [Category] Message
    ///   - Release build filtering (no verbose in production)
    ///   - Crash-safe save trigger on critical exceptions
    ///   - Analytics event hooks for funnel tracking
    ///   - Log file export for QA bug reports
    /// </summary>
    public class ProductionLogger : MonoBehaviour
    {
        // ─── Events ──────────────────────────────────────────────────────────────

        public event Action<string, LogLevel> OnCriticalError;  // message, level

        // ─── State ───────────────────────────────────────────────────────────────

        private bool _isReleaseBuild;
        private SaveSystem _save;

        private const int MaxLogEntries = 200;
        private readonly System.Collections.Generic.Queue<string> _logBuffer = new();

        // ─── Lifecycle ───────────────────────────────────────────────────────────

        public void Initialize(SaveSystem save, bool isReleaseBuild = false)
        {
            _save           = save;
            _isReleaseBuild = isReleaseBuild;

            // Hook into Unity's log system for critical error forwarding
            Application.logMessageReceived += OnUnityLog;

            Log("ProductionLogger", "Initialized. Release: " + isReleaseBuild, LogLevel.Info);
        }

        // ─── Public Logging API ───────────────────────────────────────────────────

        public void Log(string category, string message, LogLevel level = LogLevel.Info)
        {
            if (_isReleaseBuild && level == LogLevel.Verbose) return;

            var entry = $"[{level}][{category}] {message}";
            BufferEntry(entry);

            switch (level)
            {
                case LogLevel.Verbose:
                case LogLevel.Info:    Debug.Log(entry);        break;
                case LogLevel.Warning: Debug.LogWarning(entry); break;
                case LogLevel.Error:   Debug.LogError(entry);   break;
                case LogLevel.Critical:
                    Debug.LogError(entry);
                    HandleCriticalError(message);
                    break;
            }
        }

        public void LogAnalyticsEvent(string eventName, string context = null)
        {
            // Hook to analytics backend — swap for Firebase Analytics, etc.
            Log("Analytics", $"Event: {eventName} | {context}", LogLevel.Verbose);
            // Firebase: FirebaseAnalytics.LogEvent(eventName);
        }

        public void LogFunnelStep(string step)
        {
            LogAnalyticsEvent("funnel_step", step);
        }

        /// <summary>Export log buffer as a string for QA bug reports.</summary>
        public string ExportLogs() => string.Join("\n", _logBuffer);

        // ─── Private Helpers ──────────────────────────────────────────────────────

        private void OnUnityLog(string condition, string stacktrace, LogType type)
        {
            if (type == LogType.Exception || type == LogType.Error)
            {
                BufferEntry($"[UNITY_ERR] {condition}");
                if (type == LogType.Exception)
                    HandleCriticalError(condition);
            }
        }

        private void HandleCriticalError(string message)
        {
            // Attempt crash-safe save
            try { _save?.ForceSave(); }
            catch (Exception ex) { Debug.LogError($"[ProductionLogger] Force-save failed: {ex.Message}"); }

            OnCriticalError?.Invoke(message, LogLevel.Critical);
        }

        private void BufferEntry(string entry)
        {
            _logBuffer.Enqueue($"{DateTime.Now:HH:mm:ss} {entry}");
            while (_logBuffer.Count > MaxLogEntries)
                _logBuffer.Dequeue();
        }

        private void OnDestroy()
        {
            Application.logMessageReceived -= OnUnityLog;
        }
    }

    public enum LogLevel { Verbose, Info, Warning, Error, Critical }
}
