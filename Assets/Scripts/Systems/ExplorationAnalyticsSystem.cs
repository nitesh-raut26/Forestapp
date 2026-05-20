using System;
using System.Collections.Generic;
using UnityEngine;

namespace ForestFriendsQuest
{
    [Serializable]
    public class ZoneExplorationData
    {
        public string zoneId;
        public bool   firstVisited;
        public int    secretsFound;
        public int    loreCollected;
        public bool   allSecretsFound;
    }

    [Serializable]
    public class LoreEntry
    {
        public string id;
        public string title;
        public string content;
        public string zoneId;
        public bool   collected;
    }

    /// <summary>
    /// Tracks per-zone discovery progress, lore collection, secret finds,
    /// and rare sightings. Feeds the Quest engine with exploration objectives
    /// and gives parents a meaningful progress dashboard.
    /// </summary>
    public class ExplorationAnalyticsSystem : MonoBehaviour
    {
        // ─── Events ───────────────────────────────────────────────────────────────

        public event Action<string>    OnZoneFirstVisited;
        public event Action<LoreEntry> OnLoreCollected;
        public event Action<string>    OnSecretFound;
        public event Action<string>    OnRareCreatureSighted;

        // ─── State ───────────────────────────────────────────────────────────────

        private readonly Dictionary<string, ZoneExplorationData> _zoneData =
            new Dictionary<string, ZoneExplorationData>();

        private readonly List<LoreEntry>  _loreEntries  = new List<LoreEntry>();
        private readonly HashSet<string>  _raresighted  = new HashSet<string>();
        private int _totalSecretsFound;
        private int _totalLoreCollected;

        // ─── Zone Totals (known maximums for completion %) ───────────────────────

        private static readonly Dictionary<string, int> ZoneSecretCounts =
            new Dictionary<string, int>
            {
                { "whispering_meadow",   3 },
                { "moonlit_creek",       4 },
                { "elderwood_grove",     5 },
                { "crystal_caverns",     4 },
                { "forgotten_ruins",     6 },
                { "firefly_marsh",       3 },
                { "ancient_observatory", 5 },
                { "skyroot_canopy",      4 },
                { "druid_sanctuary",     7 },
                { "endless_dream",       8 }
            };

        // ─── Lifecycle ───────────────────────────────────────────────────────────

        public void Initialize()
        {
            BuildLoreCatalog();

            // Pre-create zone data for all known zones
            foreach (var zoneId in ZoneSecretCounts.Keys)
            {
                _zoneData[zoneId] = new ZoneExplorationData { zoneId = zoneId };
            }
        }

        // ─── Public Events ────────────────────────────────────────────────────────

        public void RecordZoneVisit(string zoneId)
        {
            var data = GetOrCreate(zoneId);
            if (data.firstVisited) return;

            data.firstVisited = true;
            OnZoneFirstVisited?.Invoke(zoneId);
            Debug.Log($"[Exploration] First visit: {zoneId}");
        }

        public void RecordSecretFound(string zoneId, string secretId)
        {
            var data = GetOrCreate(zoneId);
            data.secretsFound++;
            _totalSecretsFound++;

            var maxSecrets = ZoneSecretCounts.TryGetValue(zoneId, out var max) ? max : 1;
            data.allSecretsFound = data.secretsFound >= maxSecrets;

            OnSecretFound?.Invoke(secretId);
        }

        public void RecordLoreCollected(string loreId)
        {
            foreach (var entry in _loreEntries)
            {
                if (entry.id == loreId && !entry.collected)
                {
                    entry.collected = true;
                    _totalLoreCollected++;
                    OnLoreCollected?.Invoke(entry);

                    // Track in zone data
                    if (!string.IsNullOrEmpty(entry.zoneId))
                    {
                        GetOrCreate(entry.zoneId).loreCollected++;
                    }
                    break;
                }
            }
        }

        public void RecordRareCreatureSighting(string creatureId)
        {
            if (_raresighted.Add(creatureId))
            {
                OnRareCreatureSighted?.Invoke(creatureId);
                Debug.Log($"[Exploration] Rare sighting: {creatureId}");
            }
        }

        // ─── Queries ─────────────────────────────────────────────────────────────

        public int GetVisitedZoneCount()
        {
            var count = 0;
            foreach (var d in _zoneData.Values) { if (d.firstVisited) count++; }
            return count;
        }

        public float GetZoneCompletionPercent(string zoneId)
        {
            if (!_zoneData.TryGetValue(zoneId, out var data)) return 0f;
            var max = ZoneSecretCounts.TryGetValue(zoneId, out var m) ? m : 1;
            return (float)data.secretsFound / max;
        }

        public float GetOverallExplorationPercent()
        {
            var totalMax = 0;
            foreach (var v in ZoneSecretCounts.Values) totalMax += v;
            return totalMax > 0 ? (float)_totalSecretsFound / totalMax : 0f;
        }

        public int TotalLoreCollected   => _totalLoreCollected;
        public int TotalLoreAvailable   => _loreEntries.Count;
        public int TotalSecretsFound    => _totalSecretsFound;
        public int RareCreaturesSighted => _raresighted.Count;

        public bool HasVisitedZone(string zoneId)
        {
            return _zoneData.TryGetValue(zoneId, out var d) && d.firstVisited;
        }

        public IReadOnlyList<LoreEntry> GetCollectedLore()
        {
            var result = new List<LoreEntry>();
            foreach (var e in _loreEntries) { if (e.collected) result.Add(e); }
            return result;
        }

        // ─── Private Helpers ──────────────────────────────────────────────────────

        private ZoneExplorationData GetOrCreate(string zoneId)
        {
            if (!_zoneData.TryGetValue(zoneId, out var data))
            {
                data = new ZoneExplorationData { zoneId = zoneId };
                _zoneData[zoneId] = data;
            }
            return data;
        }

        private void BuildLoreCatalog()
        {
            _loreEntries.AddRange(new[]
            {
                new LoreEntry { id = "lore_meadow_01",  title = "The First Seed",           zoneId = "whispering_meadow",   content = "In the beginning, one seed fell from the Dream Sky. Where it landed, the meadow remembered everything." },
                new LoreEntry { id = "lore_meadow_02",  title = "Pip's Origin",             zoneId = "whispering_meadow",   content = "Pip the fox was born on the night of the autumn comet. Her fur holds a streak of starlight still." },
                new LoreEntry { id = "lore_creek_01",   title = "Moonlit Water Memory",     zoneId = "moonlit_creek",       content = "The creek flows from the Moon Blossom Springs. It carries memories of ancient explorers who crossed it." },
                new LoreEntry { id = "lore_creek_02",   title = "Tomo's Shell Inscription", zoneId = "moonlit_creek",       content = "Tomo's shell carries 200 growth rings. Each ring tells of a different forest season he witnessed." },
                new LoreEntry { id = "lore_grove_01",   title = "The Elder Conversation",   zoneId = "elderwood_grove",     content = "The Elder Oak and the Great Willow have spoken for 1000 years through root-whispers. No creature has heard the full conversation." },
                new LoreEntry { id = "lore_caverns_01", title = "Crystal Origin",           zoneId = "crystal_caverns",     content = "The crystals grew from tears of joy wept by the first forest explorers who discovered how beautiful the underground was." },
                new LoreEntry { id = "lore_ruins_01",   title = "The Druid Circle",         zoneId = "forgotten_ruins",     content = "The Ancient Druids left encoded messages in the stone pillars. Only those who learn their cipher can read the final blessing." },
                new LoreEntry { id = "lore_ruins_02",   title = "The Lost Portal",          zoneId = "forgotten_ruins",     content = "A portal once connected the forest to the Dream World. It was sealed when the last eclipse ended. Some say it can be reopened." },
                new LoreEntry { id = "lore_marsh_01",   title = "Luma's First Glow",        zoneId = "firefly_marsh",       content = "Luma ignited on the night of the Firefly Migration. She became the guide for all lights that follow." },
                new LoreEntry { id = "lore_obs_01",     title = "Observatory Founding",     zoneId = "ancient_observatory", content = "Sol's ancestors built the observatory to track star cycles. The telescope still remembers where to look for rare events." },
                new LoreEntry { id = "lore_canopy_01",  title = "The Sky Root Network",     zoneId = "skyroot_canopy",      content = "The roots of the Skyroot trees extend underground for miles, forming a living network of silent forest communication." },
                new LoreEntry { id = "lore_dream_01",   title = "The Dream Forest Secret",  zoneId = "endless_dream",       content = "The Endless Dream Forest exists beyond the edge of sleep. Those who find it never fully leave." }
            });
        }
    }
}
