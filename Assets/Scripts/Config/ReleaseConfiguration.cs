using System;
using UnityEngine;

namespace ForestFriendsQuest
{
    /// <summary>
    /// Release configuration and feature flag management.
    ///
    /// Single source of truth for:
    ///   - Build type (Debug / Release / QA)
    ///   - Feature flags (IAP, Analytics, Cloud Save, Social)
    ///   - Store targets (iOS App Store / Google Play / Steam / Nintendo)
    ///   - Live ops configuration (event scheduling, CDN endpoints)
    ///   - Analytics endpoints
    ///   - Maximum session length
    ///
    /// In production, these values would be fetched from Remote Config.
    /// This class provides sane developer defaults and a consistent API.
    /// </summary>
    public class ReleaseConfiguration : MonoBehaviour
    {
        // ─── Build Type ───────────────────────────────────────────────────────────

        public BuildType CurrentBuild { get; private set; } = BuildType.Debug;

        // ─── Feature Flags ─────────────────────────────────────────────────────────

        public bool EnableIAP           { get; private set; } = false;
        public bool EnableCloudSave      { get; private set; } = false;
        public bool EnableAnalytics     { get; private set; } = true;
        public bool EnableSocialSharing  { get; private set; } = true;
        public bool EnablePushNotifications { get; private set; } = false;
        public bool EnableLiveContent    { get; private set; } = false;
        public bool EnableDebugOverlay   { get; private set; } = true;

        // ─── Store Target ──────────────────────────────────────────────────────────

        public StoreTarget CurrentStore { get; private set; } = StoreTarget.None;

        // ─── Session Config ────────────────────────────────────────────────────────

        public float MaxSessionMinutes  { get; private set; } = 25f;
        public bool  EnforceSessionCap  { get; private set; } = true;

        // ─── CDN / API ─────────────────────────────────────────────────────────────

        public string ContentCDNBase     { get; private set; } = "https://cdn.forestfriendsquest.com/v3";
        public string AnalyticsEndpoint  { get; private set; } = "https://analytics.forestfriendsquest.com/events";
        public string LiveOpsEndpoint    { get; private set; } = "https://liveops.forestfriendsquest.com/schedule";

        // ─── Version ───────────────────────────────────────────────────────────────

        public string GameVersion => ContentVersionManager.CurrentGameVersion;
        public string BuildId     { get; private set; } = "dev-local";

        // ─── Lifecycle ───────────────────────────────────────────────────────────

        public void Initialize()
        {
            ApplyBuildType();
            ApplyStoreTarget();
            Debug.Log($"[ReleaseConfiguration] Build: {CurrentBuild} | Store: {CurrentStore} | v{GameVersion}");
        }

        // ─── Public API ───────────────────────────────────────────────────────────

        /// <summary>Override a feature flag at runtime (e.g., from Remote Config).</summary>
        public void SetFlag(string flag, bool value)
        {
            switch (flag)
            {
                case "iap":           EnableIAP           = value; break;
                case "cloud_save":    EnableCloudSave     = value; break;
                case "analytics":     EnableAnalytics     = value; break;
                case "social":        EnableSocialSharing  = value; break;
                case "push":          EnablePushNotifications = value; break;
                case "live_content":  EnableLiveContent    = value; break;
                default: Debug.LogWarning($"[ReleaseConfig] Unknown flag: {flag}"); break;
            }
            Debug.Log($"[ReleaseConfiguration] Flag '{flag}' → {value}");
        }

        public bool IsDebug   => CurrentBuild == BuildType.Debug;
        public bool IsRelease => CurrentBuild == BuildType.Release;
        public bool IsQA      => CurrentBuild == BuildType.QA;

        // ─── Private Setup ────────────────────────────────────────────────────────

        private void ApplyBuildType()
        {
#if UNITY_EDITOR
            CurrentBuild = BuildType.Debug;
            EnableDebugOverlay = true;
#elif DEVELOPMENT_BUILD
            CurrentBuild = BuildType.QA;
            EnableDebugOverlay = true;
#else
            CurrentBuild = BuildType.Release;
            EnableDebugOverlay = false;
            EnableIAP = true;
            EnableAnalytics = true;
            EnableLiveContent = true;
#endif
            BuildId = $"{GameVersion}-{CurrentBuild.ToString().ToLower()}-{DateTime.Now:yyyyMMdd}";
        }

        private void ApplyStoreTarget()
        {
#if UNITY_IOS
            CurrentStore = StoreTarget.AppleAppStore;
            EnablePushNotifications = true;
#elif UNITY_ANDROID
            CurrentStore = StoreTarget.GooglePlay;
            EnablePushNotifications = true;
#elif UNITY_STANDALONE
            CurrentStore = StoreTarget.Steam;
            EnableSocialSharing = false;
#elif UNITY_SWITCH
            CurrentStore = StoreTarget.NintendoSwitch;
            MaxSessionMinutes = 30f;
#else
            CurrentStore = StoreTarget.None;
#endif
        }
    }

    // ─── Enums ────────────────────────────────────────────────────────────────────

    public enum BuildType   { Debug, QA, Release }
    public enum StoreTarget { None, AppleAppStore, GooglePlay, Steam, NintendoSwitch }
}
