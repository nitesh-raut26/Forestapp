using System;
using System.Collections.Generic;
using UnityEngine;

namespace ForestFriendsQuest
{
    /// <summary>
    /// Patch-safe save migration and content version management.
    ///
    /// Ensures that saves written with older game versions can always be
    /// loaded in newer versions by applying migration steps in order.
    ///
    /// Responsibilities:
    ///   - Track current game content version
    ///   - Validate bundle compatibility against loaded version
    ///   - Apply migration steps to ForestSaveData when version changes
    ///   - Detect and handle corrupt saves gracefully
    /// </summary>
    public class ContentVersionManager : MonoBehaviour
    {
        // ─── Constants ───────────────────────────────────────────────────────────

        public const string CurrentGameVersion   = "3.0.0";
        public const int    CurrentSaveVersion   = 3;
        private const string VersionPrefKey      = "FFQ.ContentVersion";

        // ─── Events ──────────────────────────────────────────────────────────────

        public event Action<string, string> OnVersionMigrated;  // fromVersion, toVersion
        public event Action<string>         OnSaveCorrupted;    // reason

        // ─── Migration Registry ───────────────────────────────────────────────────

        private readonly List<SaveMigrationStep> _migrations = new();

        // ─── Lifecycle ───────────────────────────────────────────────────────────

        public void Initialize()
        {
            RegisterMigrations();
            Debug.Log($"[ContentVersionManager] Game version: {CurrentGameVersion}");
        }

        // ─── Public API ───────────────────────────────────────────────────────────

        /// <summary>Returns true if a content bundle's version is compatible.</summary>
        public bool IsBundleCompatible(string bundleKey, string bundleVersion)
        {
            if (string.IsNullOrEmpty(bundleVersion)) return true;
            return Version.TryParse(bundleVersion, out var bv)
                && Version.TryParse(CurrentGameVersion, out var gv)
                && bv.Major <= gv.Major;
        }

        /// <summary>
        /// Migrate save data from its stored version to the current version.
        /// Returns true if migration succeeded (or was not needed).
        /// </summary>
        public bool MigrateSave(ForestSaveData save)
        {
            if (save == null)
            {
                OnSaveCorrupted?.Invoke("Null save data");
                return false;
            }

            int from = save.version;
            int to   = CurrentSaveVersion;

            if (from == to) return true;
            if (from > to)
            {
                Debug.LogWarning($"[ContentVersionManager] Save version {from} > current {to}. Possible downgrade.");
                return true;
            }

            foreach (var step in _migrations)
            {
                if (step.fromVersion >= from && step.fromVersion < to)
                {
                    try
                    {
                        step.migrate(save);
                        Debug.Log($"[ContentVersionManager] Applied migration v{step.fromVersion}→v{step.toVersion}");
                    }
                    catch (Exception ex)
                    {
                        OnSaveCorrupted?.Invoke($"Migration v{step.fromVersion}→v{step.toVersion} failed: {ex.Message}");
                        return false;
                    }
                }
            }

            string fromVer = $"v{from}";
            save.version = to;
            OnVersionMigrated?.Invoke(fromVer, $"v{to}");
            return true;
        }

        /// <summary>Store the current game version in PlayerPrefs for next-launch checks.</summary>
        public void PersistCurrentVersion()
        {
            PlayerPrefs.SetString(VersionPrefKey, CurrentGameVersion);
            PlayerPrefs.Save();
        }

        /// <summary>Check if this is the first launch after an update.</summary>
        public bool IsPostUpdateLaunch()
        {
            var stored = PlayerPrefs.GetString(VersionPrefKey, "0.0.0");
            return stored != CurrentGameVersion;
        }

        // ─── Migration Definitions ────────────────────────────────────────────────

        private void RegisterMigrations()
        {
            // v1 → v2: introduce explorerTier default
            _migrations.Add(new SaveMigrationStep
            {
                fromVersion = 1,
                toVersion   = 2,
                migrate     = save =>
                {
                    if (string.IsNullOrEmpty(save.explorerTier))
                        save.explorerTier = "scout";
                    save.forestTreats = Mathf.Max(save.forestTreats, 4);
                }
            });

            // v2 → v3: introduce AAA expansion fields with safe defaults
            _migrations.Add(new SaveMigrationStep
            {
                fromVersion = 2,
                toVersion   = 3,
                migrate     = save =>
                {
                    save.attendedSeasonalEventIds ??= Array.Empty<string>();
                    save.discoveredLoreIds        ??= Array.Empty<string>();
                    save.defeatedBossIds          ??= Array.Empty<string>();
                    save.unlockedRegionIds        ??= Array.Empty<string>();
                    save.creatureEvolutionState   ??= Array.Empty<string>();
                    save.craftedItemIds           ??= Array.Empty<string>();
                    if (save.totalInGameDays == 0) save.totalInGameDays = 1;
                }
            });
        }

        // ─── Data Types ───────────────────────────────────────────────────────────

        private class SaveMigrationStep
        {
            public int              fromVersion;
            public int              toVersion;
            public Action<ForestSaveData> migrate;
        }
    }
}
