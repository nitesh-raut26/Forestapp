using System;
using System.Collections.Generic;
using UnityEngine;

namespace ForestFriendsQuest
{
    /// <summary>
    /// Generic object pool that eliminates runtime GC allocations for frequently
    /// spawned/despawned objects (particles, UI cards, projectiles, etc.).
    /// Pools are pre-warmed at startup and never shrink below their initial capacity.
    /// </summary>
    public class ObjectPoolManager : MonoBehaviour
    {
        private static ObjectPoolManager _instance;
        public static ObjectPoolManager Instance => _instance;

        private readonly Dictionary<string, Queue<GameObject>> _pools =
            new Dictionary<string, Queue<GameObject>>();

        private readonly Dictionary<string, GameObject> _prefabRegistry =
            new Dictionary<string, GameObject>();

        private Transform _poolRoot;

        private void Awake()
        {
            if (_instance != null && _instance != this)
            {
                Destroy(gameObject);
                return;
            }

            _instance = this;
            DontDestroyOnLoad(gameObject);

            _poolRoot = new GameObject("PoolRoot").transform;
            _poolRoot.SetParent(transform);
        }

        // ─── Registration ────────────────────────────────────────────────────────

        /// <summary>Register a prefab and pre-warm the pool with <paramref name="initialCount"/> instances.</summary>
        public void RegisterPool(string poolKey, GameObject prefab, int initialCount = 8)
        {
            if (_prefabRegistry.ContainsKey(poolKey)) return;

            _prefabRegistry[poolKey] = prefab;
            _pools[poolKey] = new Queue<GameObject>(initialCount);

            for (var i = 0; i < initialCount; i++)
            {
                var instance = CreateInstance(poolKey, prefab);
                _pools[poolKey].Enqueue(instance);
            }
        }

        // ─── Acquire / Release ───────────────────────────────────────────────────

        /// <summary>Retrieve an object from the pool. Creates a new instance if pool is empty.</summary>
        public GameObject Acquire(string poolKey)
        {
            if (!_pools.TryGetValue(poolKey, out var queue))
            {
                Debug.LogWarning($"[ObjectPool] Pool '{poolKey}' not registered.");
                return null;
            }

            GameObject obj;
            if (queue.Count > 0)
            {
                obj = queue.Dequeue();
            }
            else
            {
                // Grow pool on demand
                obj = CreateInstance(poolKey, _prefabRegistry[poolKey]);
            }

            obj.SetActive(true);
            return obj;
        }

        /// <summary>Return an object to its pool. Automatically resets its state.</summary>
        public void Release(string poolKey, GameObject obj)
        {
            if (!_pools.ContainsKey(poolKey))
            {
                Destroy(obj);
                return;
            }

            obj.SetActive(false);
            obj.transform.SetParent(_poolRoot, false);
            _pools[poolKey].Enqueue(obj);
        }

        /// <summary>Schedule an automatic release after a delay (useful for particle bursts).</summary>
        public void ReleaseAfter(string poolKey, GameObject obj, float delay)
        {
            StartCoroutine(ReleaseRoutine(poolKey, obj, delay));
        }

        // ─── Lightweight GameObject Pool (no prefab needed) ───────────────────────

        /// <summary>Acquire a bare GameObject from a lightweight string-keyed pool.</summary>
        public GameObject AcquireRaw(string poolKey, Action<GameObject> onCreate = null)
        {
            if (!_pools.TryGetValue(poolKey, out var queue))
            {
                _pools[poolKey] = new Queue<GameObject>(4);
                queue = _pools[poolKey];
            }

            GameObject obj;
            if (queue.Count > 0)
            {
                obj = queue.Dequeue();
                obj.SetActive(true);
            }
            else
            {
                obj = new GameObject(poolKey);
                obj.transform.SetParent(_poolRoot, false);
                onCreate?.Invoke(obj);
            }

            return obj;
        }

        // ─── Queries ─────────────────────────────────────────────────────────────

        public int GetPoolSize(string poolKey)
        {
            return _pools.TryGetValue(poolKey, out var q) ? q.Count : 0;
        }

        public bool IsRegistered(string poolKey) => _pools.ContainsKey(poolKey);

        // ─── Private Helpers ─────────────────────────────────────────────────────

        private GameObject CreateInstance(string poolKey, GameObject prefab)
        {
            var obj = Instantiate(prefab, _poolRoot);
            obj.name = $"{poolKey}_Pooled";
            obj.SetActive(false);
            return obj;
        }

        private System.Collections.IEnumerator ReleaseRoutine(
            string poolKey, GameObject obj, float delay)
        {
            yield return new WaitForSeconds(delay);
            Release(poolKey, obj);
        }
    }
}
