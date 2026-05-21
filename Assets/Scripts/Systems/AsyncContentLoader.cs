using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace ForestFriendsQuest
{
    // ScriptableObject stub that holds all LevelData assets for the project.
    // Create via: Assets > Create > ForestFriendsQuest > Level Data Registry
    [CreateAssetMenu(menuName = "ForestFriendsQuest/Level Data Registry",
                     fileName = "LevelDataRegistry")]
    public class LevelDataRegistry : ScriptableObject
    {
        [SerializeField] private LevelData[] _levels = Array.Empty<LevelData>();

        public LevelData[] Levels => _levels;

        public LevelData Find(string levelId)
        {
            foreach (var level in _levels)
                if (level.id == levelId) return level;
            return null;
        }
    }

    // Coroutine-based content loading coordinator.
    //
    // Loads LevelData from a LevelDataRegistry ScriptableObject and caches results
    // so repeated requests for the same level skip the lookup entirely.
    //
    // Assign the registry in the Inspector or call Initialize() at runtime before
    // issuing any LoadLevel calls.
    public class AsyncContentLoader : MonoBehaviour
    {
        [SerializeField] private LevelDataRegistry _registry;

        private readonly Dictionary<string, LevelData> _cache =
            new Dictionary<string, LevelData>();

        public void Initialize(LevelDataRegistry registry)
        {
            _registry = registry;
        }

        // Loads level data for levelId and invokes callback when ready.
        // Served from cache on subsequent calls for the same id.
        // Callback receives null when the id is not found in the registry.
        public void LoadLevel(string levelId, Action<LevelData> callback)
        {
            if (_cache.TryGetValue(levelId, out var cached))
            {
                callback?.Invoke(cached);
                return;
            }
            StartCoroutine(LoadCoroutine(levelId, callback));
        }

        // Removes a single entry from the in-memory cache.
        public void Evict(string levelId) => _cache.Remove(levelId);

        // Clears all cached data.
        public void EvictAll() => _cache.Clear();

        private IEnumerator LoadCoroutine(string levelId, Action<LevelData> callback)
        {
            // Yield one frame so callers can treat every LoadLevel as always-async,
            // even when the result would otherwise be available synchronously.
            yield return null;

            if (_registry == null)
            {
                Debug.LogWarning("[AsyncContentLoader] No LevelDataRegistry assigned.");
                callback?.Invoke(null);
                yield break;
            }

            var data = _registry.Find(levelId);
            if (data != null)
                _cache[levelId] = data;
            else
                Debug.LogWarning($"[AsyncContentLoader] Level '{levelId}' not found in registry.");

            callback?.Invoke(data);
        }
    }
}
