using System;
using System.Collections.Generic;
using UnityEngine;

namespace ForestFriendsQuest
{
    // Type-safe wrapper around ReusableCardPool that provides individual
    // Acquire / Release semantics instead of batch Bind.
    //
    // Each Acquire or Release call re-invokes Bind on the underlying card pool
    // so card visibility always matches the active item list.
    public class UIPool<T>
    {
        private readonly ReusableCardPool _cardPool;
        private readonly List<T> _activeItems = new List<T>();
        private Action<ForestCard, T, int> _configure;

        public IReadOnlyList<T> ActiveItems => _activeItems;
        public int ActiveCount             => _activeItems.Count;

        public UIPool(RectTransform parent, int capacity, Action<ForestCard, T, int> configure = null)
        {
            _cardPool  = new ReusableCardPool(parent, capacity);
            _configure = configure;
        }

        public void SetConfigure(Action<ForestCard, T, int> configure) => _configure = configure;

        // Adds item to the visible set and re-binds all cards.
        public void Acquire(T item)
        {
            _activeItems.Add(item);
            Rebind();
        }

        // Removes item from the visible set and re-binds the remainder.
        // Returns true when the item was present.
        public bool Release(T item)
        {
            var removed = _activeItems.Remove(item);
            if (removed) Rebind();
            return removed;
        }

        // Removes the item at the given index and re-binds.
        public void ReleaseAt(int index)
        {
            _activeItems.RemoveAt(index);
            Rebind();
        }

        // Replaces the entire visible set in one call — avoids repeated Rebind().
        public void BindAll(IReadOnlyList<T> items, Action<ForestCard, T, int> configure = null)
        {
            _activeItems.Clear();
            foreach (var item in items) _activeItems.Add(item);
            if (configure != null) _configure = configure;
            Rebind();
        }

        // Hides all cards and clears the active list.
        public void ReleaseAll()
        {
            _activeItems.Clear();
            _cardPool.Clear();
        }

        private void Rebind()
        {
            if (_configure == null)
            {
                Debug.LogWarning("[UIPool] No configure action set — cards will not be updated.");
                return;
            }
            _cardPool.Bind<T>(_activeItems, _configure);
        }
    }

    // Registry / factory for named UIPool<T> instances.
    // Plain class — no MonoBehaviour lifecycle required.
    public class UIPoolManager
    {
        private readonly Dictionary<string, PoolEntry> _pools =
            new Dictionary<string, PoolEntry>();

        // Returns an existing pool for key, or creates one if absent.
        public UIPool<T> GetOrCreate<T>(string key, RectTransform parent, int capacity = 40)
        {
            if (_pools.TryGetValue(key, out var entry))
                return entry.Pool as UIPool<T>;

            var pool = new UIPool<T>(parent, capacity);
            _pools[key] = new PoolEntry { Pool = pool, ReleaseAll = pool.ReleaseAll };
            return pool;
        }

        // Returns an existing pool, or null if the key has not been registered.
        public UIPool<T> Get<T>(string key)
        {
            return _pools.TryGetValue(key, out var entry) ? entry.Pool as UIPool<T> : null;
        }

        // Calls ReleaseAll on every pool and removes them from the registry.
        public void DisposeAll()
        {
            foreach (var entry in _pools.Values)
                entry.ReleaseAll?.Invoke();
            _pools.Clear();
        }

        private class PoolEntry
        {
            public object Pool;
            public Action ReleaseAll;
        }
    }
}
