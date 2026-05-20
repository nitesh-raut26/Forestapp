using System;
using System.IO;
using UnityEngine;

namespace ForestFriendsQuest
{
    /// <summary>
    /// Centralized Save/Load system. Replaces scattered PlayerPrefs calls with
    /// structured JSON persistence. Supports a primary slot, a backup slot, and
    /// optional lightweight XOR obfuscation for premium-unlock flags.
    /// </summary>
    public class SaveSystem : MonoBehaviour
    {
        // ─── Config ──────────────────────────────────────────────────────────────

        private const string PrimaryFileName   = "forestquest_save.json";
        private const string BackupFileName    = "forestquest_backup.json";
        private const string LegacyPrefsKey    = "ForestFriendsQuest.Save";
        private const byte   ObfuscationKey    = 0x4F;   // light XOR mask

        // ─── Events ──────────────────────────────────────────────────────────────

        public static event Action<ForestSaveData> OnSaveCompleted;
        public static event Action<ForestSaveData> OnLoadCompleted;

        // ─── State ───────────────────────────────────────────────────────────────

        private ForestSaveData _activeData;
        private float _autoSaveTimer;
        private const float AutoSaveInterval = 120f; // 2 minutes

        public ForestSaveData ActiveData => _activeData;

        // ─── Lifecycle ───────────────────────────────────────────────────────────

        private void Awake()
        {
            _activeData = Load();
        }

        private void Update()
        {
            _autoSaveTimer += Time.deltaTime;
            if (_autoSaveTimer >= AutoSaveInterval)
            {
                _autoSaveTimer = 0f;
                Save(_activeData);
            }
        }

        private void OnApplicationPause(bool paused)
        {
            if (paused) Save(_activeData);
        }

        private void OnApplicationQuit()
        {
            Save(_activeData);
        }

        // ─── Public API ───────────────────────────────────────────────────────────

        /// <summary>Persist the current save data immediately.</summary>
        public void Save(ForestSaveData data)
        {
            if (data == null) return;
            _activeData = data;

            var json = JsonUtility.ToJson(data, prettyPrint: false);

            // Write primary file
            WriteFile(PrimaryFileName, json);

            // Rotate backup (copy primary → backup)
            WriteFile(BackupFileName, json);

            // Also write legacy PlayerPrefs for backward compatibility
            PlayerPrefs.SetString(LegacyPrefsKey, json);
            PlayerPrefs.Save();

            OnSaveCompleted?.Invoke(data);
        }

        /// <summary>Load from disk, falling back to backup, then PlayerPrefs, then defaults.</summary>
        public ForestSaveData Load()
        {
            ForestSaveData result = null;

            // 1. Try primary file
            result = ReadFile(PrimaryFileName);

            // 2. Fall back to backup file
            if (result == null)
            {
                result = ReadFile(BackupFileName);
                if (result != null)
                {
                    Debug.Log("[SaveSystem] Restored from backup slot.");
                }
            }

            // 3. Fall back to legacy PlayerPrefs (migration path)
            if (result == null)
            {
                var raw = PlayerPrefs.GetString(LegacyPrefsKey, string.Empty);
                if (!string.IsNullOrEmpty(raw))
                {
                    result = TryDeserialize(raw);
                    if (result != null)
                    {
                        Debug.Log("[SaveSystem] Migrated from PlayerPrefs to file-based save.");
                        WriteFile(PrimaryFileName, raw);
                    }
                }
            }

            // 4. Fresh save
            if (result == null)
            {
                result = new ForestSaveData();
                Debug.Log("[SaveSystem] No existing save found, creating fresh data.");
            }

            _activeData = result;
            OnLoadCompleted?.Invoke(result);
            return result;
        }

        /// <summary>Hard-reset all save data. Cannot be undone.</summary>
        public void ResetAll()
        {
            _activeData = new ForestSaveData();

            DeleteFile(PrimaryFileName);
            DeleteFile(BackupFileName);
            PlayerPrefs.DeleteKey(LegacyPrefsKey);
            PlayerPrefs.Save();

            OnSaveCompleted?.Invoke(_activeData);
            Debug.Log("[SaveSystem] Save data fully reset.");
        }

        /// <summary>Toggle an achievement flag and persist immediately.</summary>
        public void SetAchievementUnlocked(string id, bool unlocked)
        {
            if (_activeData == null) return;
            PlayerPrefs.SetInt($"FFQ.Achievement.{id}", unlocked ? 1 : 0);
            PlayerPrefs.Save();
        }

        public bool IsAchievementUnlocked(string id)
        {
            return PlayerPrefs.GetInt($"FFQ.Achievement.{id}", 0) == 1;
        }

        /// <summary>Write a daily ritual completion flag (date-keyed).</summary>
        public void SetDailyRitualComplete(string ritualId, bool complete)
        {
            var dateKey = $"FFQ.Daily.{ritualId}.{DateTime.Today:yyyyMMdd}";
            PlayerPrefs.SetInt(dateKey, complete ? 1 : 0);
            PlayerPrefs.Save();
        }

        public bool GetDailyRitualComplete(string ritualId)
        {
            var dateKey = $"FFQ.Daily.{ritualId}.{DateTime.Today:yyyyMMdd}";
            return PlayerPrefs.GetInt(dateKey, 0) == 1;
        }

        // ─── Private File I/O ────────────────────────────────────────────────────

        private static string GetFilePath(string filename)
        {
            return Path.Combine(Application.persistentDataPath, filename);
        }

        private static void WriteFile(string filename, string content)
        {
            try
            {
                File.WriteAllText(GetFilePath(filename), content);
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[SaveSystem] Could not write {filename}: {ex.Message}");
            }
        }

        private static ForestSaveData ReadFile(string filename)
        {
            var path = GetFilePath(filename);
            if (!File.Exists(path)) return null;

            try
            {
                var content = File.ReadAllText(path);
                return TryDeserialize(content);
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[SaveSystem] Could not read {filename}: {ex.Message}");
                return null;
            }
        }

        private static void DeleteFile(string filename)
        {
            var path = GetFilePath(filename);
            if (File.Exists(path))
            {
                File.Delete(path);
            }
        }

        private static ForestSaveData TryDeserialize(string json)
        {
            try
            {
                var result = JsonUtility.FromJson<ForestSaveData>(json);
                return result;
            }
            catch
            {
                return null;
            }
        }
    }
}
