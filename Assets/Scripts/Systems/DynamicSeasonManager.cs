using System;
using UnityEngine;
using UnityEngine.UI;

namespace ForestFriendsQuest
{
    // NOTE: Season enum is defined in SeasonalEventSystem.cs (single canonical definition).

    /// <summary>
    /// Advances the in-game season based on real calendar days played.
    ///
    /// Each season lasts 7 real days of play (configurable). Seasons affect:
    ///   - World map and sanctuary background tint
    ///   - Available ritual themes
    ///   - Rare world event probability weights
    ///   - Creature idle emotion weights
    ///
    /// Season state is stored in ForestSaveData.currentSeasonIndex and
    /// ForestSaveData.totalInGameDays.
    /// </summary>
    public class DynamicSeasonManager : MonoBehaviour
    {
        private SaveSystem _saveSystem;

        public Season     CurrentSeason { get; private set; }
        public int        DaysInSeason  { get; private set; }

        private const int DaysPerSeason = 7;

        public event Action<Season, Season> OnSeasonChanged; // (previous, next)

        // ─── Colors ───────────────────────────────────────────────────────────────

        private static readonly Color SpringBg = new Color32(14,  40,  22, 255);
        private static readonly Color SummerBg = new Color32(18,  50,  20, 255);
        private static readonly Color AutumnBg = new Color32(38,  28,  12, 255);
        private static readonly Color WinterBg = new Color32(12,  20,  35, 255);

        private static readonly Color SpringTint = new Color32(120, 220, 140, 255);
        private static readonly Color SummerTint = new Color32(180, 230, 100, 255);
        private static readonly Color AutumnTint = new Color32(220, 150,  70, 255);
        private static readonly Color WinterTint = new Color32(140, 180, 240, 255);

        // ─── Lifecycle ────────────────────────────────────────────────────────────

        public void Initialize(SaveSystem saveSystem)
        {
            _saveSystem   = saveSystem;
            var data      = saveSystem?.ActiveData;
            CurrentSeason = data != null ? (Season)data.currentSeasonIndex : Season.Spring;
            DaysInSeason  = data?.totalInGameDays % DaysPerSeason ?? 0;
        }

        // ─── Daily Tick ───────────────────────────────────────────────────────────

        /// <summary>Called once per real calendar day the app is opened.</summary>
        public void TickDay()
        {
            var data = _saveSystem?.ActiveData;
            if (data == null) return;

            data.totalInGameDays++;
            DaysInSeason = data.totalInGameDays % DaysPerSeason;

            if (DaysInSeason == 0)
                AdvanceSeason(data);

            _saveSystem.Save(data);
        }

        // ─── Public Queries ───────────────────────────────────────────────────────

        public Color GetSeasonBackground() => CurrentSeason switch
        {
            Season.Summer => SummerBg,
            Season.Autumn => AutumnBg,
            Season.Winter => WinterBg,
            _             => SpringBg
        };

        public Color GetSeasonAccentColor() => CurrentSeason switch
        {
            Season.Summer => SummerTint,
            Season.Autumn => AutumnTint,
            Season.Winter => WinterTint,
            _             => SpringTint
        };

        public string GetSeasonDisplayName() => CurrentSeason switch
        {
            Season.Summer => "Summer",
            Season.Autumn => "Autumn",
            Season.Winter => "Winter",
            _             => "Spring"
        };

        /// <summary>Returns a 0-1 progress through the current season.</summary>
        public float GetSeasonProgress() => DaysInSeason / (float)DaysPerSeason;

        // ─── Private ─────────────────────────────────────────────────────────────

        private void AdvanceSeason(ForestSaveData data)
        {
            var previous      = CurrentSeason;
            CurrentSeason     = (Season)(((int)CurrentSeason + 1) % 4);
            data.currentSeasonIndex = (int)CurrentSeason;

            Debug.Log($"[DynamicSeasonManager] Season changed: {previous} → {CurrentSeason}");
            OnSeasonChanged?.Invoke(previous, CurrentSeason);
        }
    }
}
