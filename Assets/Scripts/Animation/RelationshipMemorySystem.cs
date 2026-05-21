using System;
using System.Collections.Generic;
using UnityEngine;

namespace ForestFriendsQuest
{
    /// <summary>
    /// Tracks per-creature interaction history so that dialogue and behavior
    /// adapt based on *how* the player has engaged, not just bond level.
    ///
    /// Memory types:
    ///   - LastPetTime / LastFeedTime         → freshness of care
    ///   - FavoriteInteraction                → most-used action
    ///   - SharedLevelClears                  → levels completed with creature present
    ///   - FirstMeetDate                      → displayed in creature info card
    ///   - RecentEmotionHistory               → last 5 triggered emotions
    ///
    /// Persisted in PlayerPrefs under "FFQ.Rel.{creatureId}.{key}".
    /// Not in save JSON to keep save small — PlayerPrefs is fine for this soft data.
    /// </summary>
    public class RelationshipMemorySystem : MonoBehaviour
    {
        private readonly Dictionary<string, CreatureMemory> _memories =
            new Dictionary<string, CreatureMemory>();

        private static readonly string[] CreatureIds =
            { "pip", "mimi", "tomo", "luma", "nori", "sol" };

        // ─── Lifecycle ────────────────────────────────────────────────────────────

        public void Initialize()
        {
            foreach (var id in CreatureIds)
                _memories[id] = LoadMemory(id);
        }

        // ─── Record Events ────────────────────────────────────────────────────────

        public void RecordPet(string creatureId)
        {
            var m = Get(creatureId);
            m.LastPetTime        = DateTime.UtcNow;
            m.TotalPets++;
            m.FavoriteInteraction = GetFavorite(m);
            Save(creatureId, m);
        }

        public void RecordFeed(string creatureId)
        {
            var m = Get(creatureId);
            m.LastFeedTime = DateTime.UtcNow;
            m.TotalFeeds++;
            m.FavoriteInteraction = GetFavorite(m);
            Save(creatureId, m);
        }

        public void RecordLevelClear(string creatureId)
        {
            var m = Get(creatureId);
            m.SharedLevelClears++;
            Save(creatureId, m);
        }

        public void RecordEmotion(string creatureId, CreatureEmotion emotion)
        {
            var m = Get(creatureId);
            m.RecentEmotions.Add(emotion);
            if (m.RecentEmotions.Count > 5)
                m.RecentEmotions.RemoveAt(0);
            Save(creatureId, m);
        }

        // ─── Query ────────────────────────────────────────────────────────────────

        public CreatureMemory GetMemory(string creatureId) => Get(creatureId);

        public bool WasRecentlyInteracted(string creatureId)
        {
            var m = Get(creatureId);
            var lastAny = m.LastPetTime > m.LastFeedTime ? m.LastPetTime : m.LastFeedTime;
            return (DateTime.UtcNow - lastAny).TotalHours < 8;
        }

        public string GetPersonalizedGreeting(string creatureId)
        {
            var m = Get(creatureId);
            if (m.TotalPets == 0 && m.TotalFeeds == 0)
                return "This is your first meeting!";
            if (WasRecentlyInteracted(creatureId))
                return $"You two have been close lately.";
            if (m.SharedLevelClears > 10)
                return $"Cleared {m.SharedLevelClears} levels together!";
            return $"Bonded since {m.FirstMeetDate:MMM d}.";
        }

        // ─── Persistence ─────────────────────────────────────────────────────────

        private static CreatureMemory LoadMemory(string id)
        {
            var key     = $"FFQ.Rel.{id}";
            var m       = new CreatureMemory { CreatureId = id };
            var raw     = PlayerPrefs.GetString(key, "");
            if (string.IsNullOrEmpty(raw)) return m;

            try
            {
                var parts = raw.Split('|');
                if (parts.Length >= 6)
                {
                    m.TotalPets         = int.Parse(parts[0]);
                    m.TotalFeeds        = int.Parse(parts[1]);
                    m.SharedLevelClears = int.Parse(parts[2]);
                    m.FavoriteInteraction = parts[3];
                    m.FirstMeetDate     = DateTime.Parse(parts[4]);
                    m.LastPetTime       = DateTime.Parse(parts[5]);
                }
            }
            catch { /* corrupt data — start fresh */ }

            return m;
        }

        private static void Save(string id, CreatureMemory m)
        {
            var key = $"FFQ.Rel.{id}";
            var raw = $"{m.TotalPets}|{m.TotalFeeds}|{m.SharedLevelClears}|" +
                      $"{m.FavoriteInteraction}|{m.FirstMeetDate:O}|{m.LastPetTime:O}";
            PlayerPrefs.SetString(key, raw);
        }

        private CreatureMemory Get(string id)
        {
            if (!_memories.TryGetValue(id, out var m))
            {
                m = LoadMemory(id);
                _memories[id] = m;
            }
            return m;
        }

        private static string GetFavorite(CreatureMemory m)
        {
            if (m.TotalPets > m.TotalFeeds) return "petting";
            if (m.TotalFeeds > m.TotalPets) return "feeding";
            return "both";
        }

        // ─── Data ─────────────────────────────────────────────────────────────────

        public class CreatureMemory
        {
            public string              CreatureId;
            public int                 TotalPets;
            public int                 TotalFeeds;
            public int                 SharedLevelClears;
            public string              FavoriteInteraction = "none";
            public DateTime            FirstMeetDate       = DateTime.UtcNow;
            public DateTime            LastPetTime         = DateTime.MinValue;
            public DateTime            LastFeedTime        = DateTime.MinValue;
            public List<CreatureEmotion> RecentEmotions    = new List<CreatureEmotion>();
        }
    }
}
