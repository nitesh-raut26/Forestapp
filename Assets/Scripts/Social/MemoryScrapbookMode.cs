using System;
using System.Collections.Generic;
using UnityEngine;

namespace ForestFriendsQuest
{
    /// <summary>
    /// Memory scrapbook — a magical gallery of a player's journey.
    ///
    /// Collects and displays:
    ///   - Creature bonding moments (with date and bond level)
    ///   - First puzzle solves per zone
    ///   - Seasonal event participation
    ///   - Lore pages discovered
    ///   - Boss defeats
    ///   - Evolution moments
    ///   - Sanctuary milestones
    ///
    /// Each memory is a "polaroid-style" card with creature illustration,
    /// timestamp, and a warm descriptive message.
    ///
    /// Philosophy: The scrapbook should make children want to
    /// show parents what they've discovered.
    /// </summary>
    public class MemoryScrapbookMode : MonoBehaviour
    {
        // ─── Events ──────────────────────────────────────────────────────────────

        public event Action<MemoryCard>       OnMemoryAdded;
        public event Action<List<MemoryCard>> OnScrapbookOpened;

        // ─── State ───────────────────────────────────────────────────────────────

        private readonly List<MemoryCard> _memories = new();
        private SaveSystem                _save;
        private UIAnimationSystem         _uiAnim;

        private const string SaveKey = "FFQ.Scrapbook";
        private const int    MaxMemories = 100;

        // ─── Lifecycle ───────────────────────────────────────────────────────────

        public void Initialize(SaveSystem save, UIAnimationSystem uiAnim)
        {
            _save   = save;
            _uiAnim = uiAnim;

            LoadMemories();
            Debug.Log($"[MemoryScrapbookMode] Loaded {_memories.Count} memories.");
        }

        // ─── Public API — Add Memories ────────────────────────────────────────────

        public void RecordBondMoment(string creatureId, int bondLevel)
        {
            AddMemory(new MemoryCard
            {
                type        = MemoryType.BondMoment,
                creatureId  = creatureId,
                title       = $"A moment with {CapFirst(creatureId)}",
                description = $"Your bond with {CapFirst(creatureId)} reached level {bondLevel}!",
                timestamp   = DateTime.Now,
                emoji       = "💚"
            });
        }

        public void RecordZonePuzzleSolve(string zoneId, string zoneName)
        {
            AddMemory(new MemoryCard
            {
                type        = MemoryType.PuzzleSolve,
                zoneId      = zoneId,
                title       = $"First solve in {zoneName}",
                description = $"You cracked your first puzzle in {zoneName}!",
                timestamp   = DateTime.Now,
                emoji       = "⭐"
            });
        }

        public void RecordEvolution(string creatureId, string stageName)
        {
            AddMemory(new MemoryCard
            {
                type        = MemoryType.Evolution,
                creatureId  = creatureId,
                title       = $"{CapFirst(creatureId)} Evolved!",
                description = $"{CapFirst(creatureId)} became a {stageName}!",
                timestamp   = DateTime.Now,
                emoji       = "✨"
            });
        }

        public void RecordBossDefeat(string bossName, string regionId)
        {
            AddMemory(new MemoryCard
            {
                type        = MemoryType.BossDefeat,
                zoneId      = regionId,
                title       = $"Defeated {bossName}!",
                description = $"You bravely defeated {bossName} and the forest rejoiced!",
                timestamp   = DateTime.Now,
                emoji       = "🏆"
            });
        }

        public void RecordLoreDiscovery(string loreTitle, string regionId)
        {
            AddMemory(new MemoryCard
            {
                type        = MemoryType.LoreDiscovery,
                zoneId      = regionId,
                title       = "Ancient Lore Found!",
                description = $"You discovered: {loreTitle}",
                timestamp   = DateTime.Now,
                emoji       = "📜"
            });
        }

        public void RecordSeasonalEvent(string eventTitle, string season)
        {
            AddMemory(new MemoryCard
            {
                type        = MemoryType.SeasonalEvent,
                title       = "Seasonal Memory",
                description = $"You attended {eventTitle} in the forest!",
                timestamp   = DateTime.Now,
                emoji       = SeasonEmoji(season)
            });
        }

        // ─── Public API — Read ────────────────────────────────────────────────────

        public IReadOnlyList<MemoryCard> GetAllMemories() => _memories;

        public List<MemoryCard> GetMemoriesForCreature(string creatureId)
            => _memories.FindAll(m => m.creatureId == creatureId);

        public List<MemoryCard> GetMemoriesOfType(MemoryType type)
            => _memories.FindAll(m => m.type == type);

        public void OpenScrapbook()
        {
            OnScrapbookOpened?.Invoke(_memories);
        }

        // ─── Private Helpers ──────────────────────────────────────────────────────

        private void AddMemory(MemoryCard card)
        {
            _memories.Add(card);
            if (_memories.Count > MaxMemories)
                _memories.RemoveAt(0);

            PersistMemories();
            OnMemoryAdded?.Invoke(card);
            Debug.Log($"[MemoryScrapbookMode] Memory added: {card.title}");
        }

        private void PersistMemories()
        {
            var json = JsonUtility.ToJson(new MemoryCardList { cards = _memories });
            PlayerPrefs.SetString(SaveKey, json);
            PlayerPrefs.Save();
        }

        private void LoadMemories()
        {
            var json = PlayerPrefs.GetString(SaveKey, string.Empty);
            if (!string.IsNullOrEmpty(json))
            {
                try
                {
                    var list = JsonUtility.FromJson<MemoryCardList>(json);
                    if (list?.cards != null) _memories.AddRange(list.cards);
                }
                catch { _memories.Clear(); }
            }
        }

        private static string CapFirst(string s) => string.IsNullOrEmpty(s) ? s : char.ToUpper(s[0]) + s[1..];

        private static string SeasonEmoji(string season) => season?.ToLower() switch
        {
            "spring" => "🌸", "summer" => "☀️", "autumn" => "🍂", "winter" => "❄️", _ => "🌿"
        };

        [Serializable] private class MemoryCardList { public List<MemoryCard> cards; }
    }

    // ─── Data Types ───────────────────────────────────────────────────────────────

    public enum MemoryType { BondMoment, PuzzleSolve, Evolution, BossDefeat, LoreDiscovery, SeasonalEvent, SanctuaryMilestone }

    [Serializable]
    public class MemoryCard
    {
        public MemoryType type;
        public string     creatureId;
        public string     zoneId;
        public string     title;
        public string     description;
        public DateTime   timestamp;
        public string     emoji;
    }
}
