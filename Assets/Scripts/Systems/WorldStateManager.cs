using System;
using System.Collections.Generic;
using UnityEngine;

namespace ForestFriendsQuest
{
    public enum RegionUnlockState { Locked, Unlocked, Discovered, Mastered }

    [Serializable]
    public class RegionState
    {
        public string            regionId;
        public string            displayName;
        public RegionUnlockState unlockState;
        public int               clearsRequired;        // levels cleared to unlock
        public bool              secretPathFound;
        public bool              loreCollected;
        public bool              bossDefeated;
        public List<string>      discoveredLoreIds = new List<string>();
    }

    [Serializable]
    public class WorldPath
    {
        public string fromRegionId;
        public string toRegionId;
        public bool   isOpen;
        public string unlockCondition;   // free-text description, e.g. "Complete Scout Quest 2"
    }

    /// <summary>
    /// Single source of truth for the 10-region world map state.
    ///
    /// Tracks:
    ///   - Which regions are locked / unlocked / discovered / mastered
    ///   - Which inter-region paths are open
    ///   - Active world events (e.g. meteor shower, boss appears)
    ///   - Lore page discovery per region
    ///
    /// Persists through SaveSystem. Emits events for map UI re-renders.
    /// </summary>
    public class WorldStateManager : MonoBehaviour
    {
        private SaveSystem    _saveSystem;
        private QuestEngine   _quests;

        public event Action<RegionState>  OnRegionUnlocked;
        public event Action<RegionState>  OnRegionMastered;
        public event Action<WorldPath>    OnPathOpened;
        public event Action<string>       OnLoreDiscovered;   // lore page id

        // ─── State ───────────────────────────────────────────────────────────────

        private readonly Dictionary<string, RegionState> _regions = new Dictionary<string, RegionState>();
        private readonly List<WorldPath>                  _paths   = new List<WorldPath>();

        // ─── Lifecycle ───────────────────────────────────────────────────────────

        public void Initialize(SaveSystem saveSystem, QuestEngine quests)
        {
            _saveSystem = saveSystem;
            _quests     = quests;

            BuildRegions();
            BuildPaths();
            LoadPersistedState();
        }

        // ─── Public API ───────────────────────────────────────────────────────────

        public RegionState GetRegion(string regionId)
        {
            _regions.TryGetValue(regionId, out var s);
            return s;
        }

        public IEnumerable<RegionState> GetAllRegions() => _regions.Values;

        public bool IsRegionUnlocked(string regionId)
        {
            return _regions.TryGetValue(regionId, out var s) &&
                   s.unlockState != RegionUnlockState.Locked;
        }

        /// <summary>Called after completing a level to possibly unlock new regions.</summary>
        public void OnLevelCleared(int totalClears)
        {
            foreach (var region in _regions.Values)
            {
                if (region.unlockState == RegionUnlockState.Locked &&
                    totalClears >= region.clearsRequired)
                {
                    UnlockRegion(region.regionId);
                }
            }
        }

        public void DiscoverLore(string regionId, string loreId, ForestSaveData saveData = null)
        {
            if (!_regions.TryGetValue(regionId, out var region)) return;
            if (region.discoveredLoreIds.Contains(loreId)) return;

            region.discoveredLoreIds.Add(loreId);
            OnLoreDiscovered?.Invoke(loreId);

            if (_saveSystem != null)
                _saveSystem.SetAchievementUnlocked($"Lore.{loreId}", true);

            _quests?.ProgressObjective("lore_page_collected");

            Debug.Log($"[WorldStateManager] Lore discovered: {loreId} in {regionId}");
        }

        public void MarkSecretPathFound(string regionId)
        {
            if (!_regions.TryGetValue(regionId, out var region)) return;
            if (region.secretPathFound) return;
            region.secretPathFound = true;
            _quests?.ProgressObjective("river_trail_complete");
        }

        public void MarkBossDefeated(string regionId, ForestSaveData saveData = null)
        {
            if (!_regions.TryGetValue(regionId, out var region)) return;
            region.bossDefeated = true;
            PersistRegion(regionId);
            CheckMastery(region, saveData);
        }

        public void OpenPath(string fromId, string toId)
        {
            foreach (var path in _paths)
            {
                if (path.fromRegionId == fromId && path.toRegionId == toId && !path.isOpen)
                {
                    path.isOpen = true;
                    OnPathOpened?.Invoke(path);
                    return;
                }
            }
        }

        public IReadOnlyList<WorldPath> GetOpenPaths()
        {
            var open = new List<WorldPath>();
            foreach (var p in _paths) { if (p.isOpen) open.Add(p); }
            return open;
        }

        // ─── Private Helpers ──────────────────────────────────────────────────────

        private void UnlockRegion(string regionId)
        {
            if (!_regions.TryGetValue(regionId, out var region)) return;
            if (region.unlockState != RegionUnlockState.Locked) return;

            region.unlockState = RegionUnlockState.Unlocked;
            PersistRegion(regionId);
            OnRegionUnlocked?.Invoke(region);

            // Auto-open the path leading into this region
            foreach (var path in _paths)
            {
                if (path.toRegionId == regionId)
                    path.isOpen = true;
            }

            Debug.Log($"[WorldStateManager] Region unlocked: {region.displayName}");
        }

        private void CheckMastery(RegionState region, ForestSaveData saveData)
        {
            if (region.secretPathFound && region.loreCollected && region.bossDefeated)
            {
                region.unlockState = RegionUnlockState.Mastered;
                PersistRegion(region.regionId);
                OnRegionMastered?.Invoke(region);
            }
        }

        private void PersistRegion(string regionId)
        {
            if (_saveSystem != null)
                _saveSystem.SetAchievementUnlocked($"Region.{regionId}", true);
        }

        private void LoadPersistedState()
        {
            if (_saveSystem == null) return;
            foreach (var region in _regions.Values)
            {
                if (_saveSystem.IsAchievementUnlocked($"Region.{region.regionId}"))
                {
                    if (region.unlockState == RegionUnlockState.Locked)
                        region.unlockState = RegionUnlockState.Unlocked;
                }
            }
        }

        // ─── World Definition ─────────────────────────────────────────────────────

        private void BuildRegions()
        {
            Add("fern-trail",          "Whispering Meadow",     0);
            Add("firefly-hollow",      "Firefly Hollow",        4);
            Add("river-bend",          "River Bend",            8);
            Add("moonlit-creek",       "Moonlit Creek",        10);
            Add("elderwood-grove",     "Elderwood Grove",      15);
            Add("crystal-caverns",     "Crystal Caverns",      20);
            Add("forgotten-ruins",     "Forgotten Ruins",      26);
            Add("firefly-marsh",       "Firefly Marsh",        22);
            Add("ancient-observatory", "Ancient Observatory",  32);
            Add("skyroot-canopy",      "Skyroot Canopy",       38);

            // Starter zone is always unlocked
            _regions["fern-trail"].unlockState = RegionUnlockState.Unlocked;
        }

        private void Add(string id, string name, int clearsRequired)
        {
            _regions[id] = new RegionState
            {
                regionId       = id,
                displayName    = name,
                unlockState    = clearsRequired == 0
                    ? RegionUnlockState.Unlocked : RegionUnlockState.Locked,
                clearsRequired = clearsRequired
            };
        }

        private void BuildPaths()
        {
            Path("fern-trail",      "firefly-hollow",      true,  "Always open");
            Path("firefly-hollow",  "river-bend",          true,  "Always open");
            Path("river-bend",      "moonlit-creek",       false, "Clear 10 levels");
            Path("moonlit-creek",   "elderwood-grove",     false, "Clear 15 levels");
            Path("elderwood-grove", "crystal-caverns",     false, "Clear 20 levels");
            Path("elderwood-grove", "firefly-marsh",       false, "Clear 22 levels");
            Path("crystal-caverns", "forgotten-ruins",     false, "Clear 26 levels");
            Path("firefly-marsh",   "ancient-observatory", false, "Clear 32 levels");
            Path("forgotten-ruins", "ancient-observatory", false, "Clear 32 levels");
            Path("ancient-observatory", "skyroot-canopy",  false, "Clear 38 levels");
        }

        private void Path(string from, string to, bool open, string condition)
        {
            _paths.Add(new WorldPath
            {
                fromRegionId    = from,
                toRegionId      = to,
                isOpen          = open,
                unlockCondition = condition
            });
        }
    }
}
