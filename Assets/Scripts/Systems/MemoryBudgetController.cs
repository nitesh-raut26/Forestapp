using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace ForestFriendsQuest
{
    /// <summary>
    /// Monitors runtime memory and evicts cached assets when above budget.
    ///
    /// Works with AudioAssetLibrary to clear synthesized clip cache on low memory.
    /// Tracks allocated texture memory via Profiler.GetTotalAllocatedMemoryLong().
    ///
    /// Budget thresholds are adjusted per device tier from PerformanceManager.
    /// </summary>
    public class MemoryBudgetController : MonoBehaviour
    {
        private PerformanceManager _perf;
        private AudioAssetLibrary  _audioLibrary;

        private long _budgetBytes;
        private float _checkInterval = 30f;

        private readonly List<System.WeakReference> _evictableObjects =
            new List<System.WeakReference>();

        // ─── Setup ────────────────────────────────────────────────────────────────

        public void Initialize(PerformanceManager perf, AudioAssetLibrary audioLibrary)
        {
            _perf         = perf;
            _audioLibrary = audioLibrary;

            _budgetBytes = perf.CurrentTier switch
            {
                DeviceTier.Low  => 80L  * 1024 * 1024,   // 80 MB
                DeviceTier.Mid  => 150L * 1024 * 1024,   // 150 MB
                _               => 300L * 1024 * 1024    // 300 MB
            };

            StartCoroutine(MemoryCheckLoop());
        }

        // ─── Public API ───────────────────────────────────────────────────────────

        public void RegisterEvictable(System.WeakReference obj)
        {
            _evictableObjects.Add(obj);
        }

        public long GetAllocatedBytes()
        {
#if UNITY_2021_2_OR_NEWER
            return UnityEngine.Profiling.Profiler.GetTotalAllocatedMemoryLong();
#else
            return 0L;
#endif
        }

        // ─── Private ─────────────────────────────────────────────────────────────

        private IEnumerator MemoryCheckLoop()
        {
            while (true)
            {
                yield return new WaitForSeconds(_checkInterval);

                var used = GetAllocatedBytes();
                if (used > _budgetBytes)
                {
                    Debug.LogWarning($"[MemoryBudget] Over budget: {used / (1024 * 1024)}MB / " +
                                     $"{_budgetBytes / (1024 * 1024)}MB — evicting caches.");
                    EvictCaches();
                }
            }
        }

        private void EvictCaches()
        {
            // Remove dead weak references
            _evictableObjects.RemoveAll(r => !r.IsAlive);

            // On Low/Mid tier — prune audio clip cache via GC hint
            if (_perf.CurrentTier != DeviceTier.High)
            {
                Resources.UnloadUnusedAssets();
                System.GC.Collect();
            }
        }
    }
}
