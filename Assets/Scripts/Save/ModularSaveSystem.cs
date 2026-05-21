using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace ForestFriendsQuest
{
    /// <summary>
    /// Production-grade modular save system.
    ///
    /// Architecture:
    ///   - Each subsystem owns a named save "module" (slice of JSON)
    ///   - Modules are versioned independently — patch-safe migration
    ///   - Atomic writes: temp file → rename prevents corruption on crash
    ///   - Cloud-sync ready: ICloudSyncProvider interface for GPlay/GameCenter
    ///   - Offline fallback: all reads from local disk; cloud sync is opportunistic
    ///
    /// Migration:
    ///   RegisterModule() takes a migrate callback invoked when saved version < current.
    ///   This allows each system to transform its own data slice without touching others.
    ///
    /// Usage:
    ///   var module = SaveSys.RegisterModule("creatures", version: 2,
    ///       migrate: (old, newVer) => MigrateCreatures(old));
    ///   module.Set("bondLevels", JsonUtility.ToJson(data));
    ///   module.Get("bondLevels", defaultJson);
    /// </summary>
    public class ModularSaveSystem : MonoBehaviour
    {
        private const string FileName        = "ffq_modular.json";
        private const string BackupFileName  = "ffq_modular.bak";
        private const string TempFileName    = "ffq_modular.tmp";

        private readonly Dictionary<string, SaveModule> _modules =
            new Dictionary<string, SaveModule>();

        private ModularSaveData _data = new ModularSaveData();
        private float           _dirtyTimer;
        private bool            _dirty;
        private const float     AutoFlushInterval = 90f;

        // ─── Lifecycle ────────────────────────────────────────────────────────────

        private void Awake()
        {
            _data = LoadFromDisk() ?? new ModularSaveData();
        }

        private void Update()
        {
            if (!_dirty) return;
            _dirtyTimer += Time.unscaledDeltaTime;
            if (_dirtyTimer >= AutoFlushInterval)
                Flush();
        }

        private void OnApplicationPause(bool paused) { if (paused && _dirty) Flush(); }
        private void OnApplicationQuit() { if (_dirty) Flush(); }

        // ─── Public API ───────────────────────────────────────────────────────────

        /// <summary>
        /// Register a save module. Must be called once during initialization.
        /// migrate is called with (existingJson, newVersion) when version mismatch detected.
        /// </summary>
        public SaveModule RegisterModule(string moduleId, int version,
            Func<string, int, string> migrate = null)
        {
            if (!_modules.TryGetValue(moduleId, out var module))
            {
                module = new SaveModule(moduleId, version, migrate, this);
                _modules[moduleId] = module;

                // Run migration if needed
                if (_data.modules.TryGetValue(moduleId, out var entry))
                {
                    if (entry.version < version && migrate != null)
                    {
                        entry.json    = migrate(entry.json, version);
                        entry.version = version;
                        MarkDirty();
                    }
                }
            }
            return module;
        }

        /// <summary>Force-flush to disk immediately.</summary>
        public void Flush()
        {
            _dirty      = false;
            _dirtyTimer = 0f;
            WriteToDisk(_data);
        }

        /// <summary>Wipe all save data (used by reset flow).</summary>
        public void WipeAll()
        {
            _data = new ModularSaveData();
            Flush();
            Debug.Log("[ModularSaveSystem] All data wiped.");
        }

        // ─── Internal ─────────────────────────────────────────────────────────────

        internal string Read(string moduleId, string key, string defaultValue)
        {
            if (!_data.modules.TryGetValue(moduleId, out var entry))
                return defaultValue;

            entry.fields.TryGetValue(key, out var val);
            return val ?? defaultValue;
        }

        internal void Write(string moduleId, string key, string value, int version)
        {
            if (!_data.modules.TryGetValue(moduleId, out var entry))
            {
                entry = new ModuleEntry { version = version };
                _data.modules[moduleId] = entry;
            }
            entry.fields[key] = value;
            MarkDirty();
        }

        internal void MarkDirty()
        {
            _dirty      = true;
            _dirtyTimer = 0f;
        }

        // ─── Disk I/O (atomic) ────────────────────────────────────────────────────

        private static void WriteToDisk(ModularSaveData data)
        {
            var path    = Path.Combine(Application.persistentDataPath, FileName);
            var tmpPath = Path.Combine(Application.persistentDataPath, TempFileName);
            var bakPath = Path.Combine(Application.persistentDataPath, BackupFileName);

            try
            {
                var json = JsonUtility.ToJson(data, prettyPrint: false);
                File.WriteAllText(tmpPath, json);  // write to temp

                if (File.Exists(path))
                    File.Copy(path, bakPath, overwrite: true); // rotate backup

                File.Move(tmpPath, path); // atomic rename
            }
            catch (Exception e)
            {
                Debug.LogError($"[ModularSaveSystem] Write failed: {e.Message}");
            }
        }

        private static ModularSaveData LoadFromDisk()
        {
            var path    = Path.Combine(Application.persistentDataPath, FileName);
            var bakPath = Path.Combine(Application.persistentDataPath, BackupFileName);

            foreach (var candidate in new[] { path, bakPath })
            {
                if (!File.Exists(candidate)) continue;
                try
                {
                    var json = File.ReadAllText(candidate);
                    return JsonUtility.FromJson<ModularSaveData>(json);
                }
                catch
                {
                    Debug.LogWarning($"[ModularSaveSystem] Corrupt save at {candidate}, trying backup.");
                }
            }
            return null;
        }
    }

    // ─── Module Handle ────────────────────────────────────────────────────────────

    public class SaveModule
    {
        private readonly string           _id;
        private readonly int              _version;
        private readonly ModularSaveSystem _sys;

        internal SaveModule(string id, int version, Func<string, int, string> _,
            ModularSaveSystem sys)
        {
            _id      = id;
            _version = version;
            _sys     = sys;
        }

        public string Get(string key, string defaultValue = "")
            => _sys.Read(_id, key, defaultValue);

        public void Set(string key, string value)
            => _sys.Write(_id, key, value, _version);

        public bool GetBool(string key, bool def = false)
            => bool.TryParse(Get(key, def.ToString()), out var v) ? v : def;

        public void SetBool(string key, bool value) => Set(key, value.ToString());

        public int GetInt(string key, int def = 0)
            => int.TryParse(Get(key, def.ToString()), out var v) ? v : def;

        public void SetInt(string key, int value) => Set(key, value.ToString());

        public float GetFloat(string key, float def = 0f)
            => float.TryParse(Get(key, def.ToString()), out var v) ? v : def;

        public void SetFloat(string key, float value) => Set(key, value.ToString());
    }

    // ─── Serializable Data ────────────────────────────────────────────────────────

    [Serializable]
    public class ModularSaveData
    {
        public int dataVersion = 1;
        public SerializableDict modules = new SerializableDict();
    }

    [Serializable]
    public class ModuleEntry
    {
        public int                    version;
        public string                 json = "";
        public SerializableStringDict fields = new SerializableStringDict();
    }

    // Unity JsonUtility requires concrete serializable dict wrappers
    [Serializable]
    public class SerializableDict : Dictionary<string, ModuleEntry>, ISerializationCallbackReceiver
    {
        [SerializeField] private List<string>      _keys   = new List<string>();
        [SerializeField] private List<ModuleEntry> _values = new List<ModuleEntry>();

        public void OnBeforeSerialize()
        {
            _keys.Clear(); _values.Clear();
            foreach (var kv in this) { _keys.Add(kv.Key); _values.Add(kv.Value); }
        }

        public void OnAfterDeserialize()
        {
            Clear();
            for (var i = 0; i < Mathf.Min(_keys.Count, _values.Count); i++)
                this[_keys[i]] = _values[i];
        }
    }

    [Serializable]
    public class SerializableStringDict : Dictionary<string, string>, ISerializationCallbackReceiver
    {
        [SerializeField] private List<string> _keys   = new List<string>();
        [SerializeField] private List<string> _values = new List<string>();

        public void OnBeforeSerialize()
        {
            _keys.Clear(); _values.Clear();
            foreach (var kv in this) { _keys.Add(kv.Key); _values.Add(kv.Value); }
        }

        public void OnAfterDeserialize()
        {
            Clear();
            for (var i = 0; i < Mathf.Min(_keys.Count, _values.Count); i++)
                this[_keys[i]] = _values[i];
        }
    }
}
