using System;
using System.Collections.Generic;
using UnityEngine;

namespace ForestFriendsQuest
{
    /// <summary>
    /// Ethical long-term retention system — keeps players returning
    /// without manipulative mechanics. Based on positive psychology principles.
    ///
    /// Philosophy: "The forest misses you" — not "your streak is broken."
    ///
    /// Features:
    ///   - Daily ritual streak tracking with cozy warmth rewards (not punishment)
    ///   - Emotional return moments ("Pip found something while you were away!")
    ///   - Seasonal anticipation previews (tomorrow's forest magic)
    ///   - Non-addictive hard session cap (configurable, default 25 min)
    ///   - Cozy "Come back tomorrow" messages from creatures
    /// </summary>
    public class RetentionPacingSystem : MonoBehaviour
    {
        // ─── Events ──────────────────────────────────────────────────────────────

        public event Action<int>    OnStreakUpdated;         // new streak count
        public event Action<string> OnReturnMomentReady;     // message
        public event Action<string> OnTomorrowPreviewReady;  // preview text
        public event Action         OnSessionCapReached;

        // ─── State ───────────────────────────────────────────────────────────────

        private SaveSystem _save;

        private float _sessionStartTime;
        private bool  _sessionCapFired;
        private const float SessionCapMinutes   = 25f;
        private const string LastPlayDateKey    = "FFQ.LastPlayDate";
        private const string StreakKey          = "FFQ.DailyStreak";

        public int   DailyStreak    { get; private set; }
        public bool  PlayedToday    { get; private set; }

        // ─── Lifecycle ───────────────────────────────────────────────────────────

        public void Initialize(SaveSystem save)
        {
            _save = save;
            _sessionStartTime = Time.time;

            LoadStreak();
            CheckDailyReturn();

            Debug.Log($"[RetentionPacingSystem] Streak: {DailyStreak}, PlayedToday: {PlayedToday}");
        }

        private void Update()
        {
            // Gentle session cap — celebrate the play, not punish
            if (!_sessionCapFired)
            {
                float minutesPlayed = (Time.time - _sessionStartTime) / 60f;
                if (minutesPlayed >= SessionCapMinutes)
                {
                    _sessionCapFired = true;
                    OnSessionCapReached?.Invoke();
                    Debug.Log("[RetentionPacingSystem] Session cap reached — time for a cozy break!");
                }
            }
        }

        // ─── Public API ───────────────────────────────────────────────────────────

        /// <summary>Call when player completes any meaningful action (ritual, puzzle, etc.).</summary>
        public void RecordDailyPlay()
        {
            if (PlayedToday) return;

            PlayedToday = true;
            DailyStreak++;
            PlayerPrefs.SetString(LastPlayDateKey, DateTime.Today.ToString("yyyy-MM-dd"));
            PlayerPrefs.SetInt(StreakKey, DailyStreak);
            PlayerPrefs.Save();

            OnStreakUpdated?.Invoke(DailyStreak);
            Debug.Log($"[RetentionPacingSystem] Streak: {DailyStreak}");
        }

        public string GetReturnMessage()
        {
            var messages = new[]
            {
                "Pip found a shiny pebble while you were away!",
                "Tomo has been thinking about your next path puzzle.",
                "Mimi learned a new tune — she can't wait to sing it for you!",
                "Luma's glow grew brighter while you were resting.",
                "The forest held a firefly gathering in your honour last night.",
                "Nori discovered a new secret trail she wants to show you.",
                "Sol found an ancient rune fragment under the observatory.",
            };
            return messages[DailyStreak % messages.Length];
        }

        public string GetTomorrowPreview()
        {
            var previews = new[]
            {
                "Tomorrow: A rare firefly shower may light up the hollow!",
                "Tomorrow: Tomo has a new path puzzle waiting.",
                "Tomorrow: Mimi's morning song unlocks a secret memory trail.",
                "Tomorrow: The Elder Oak whispers a new lore chapter at dusk.",
                "Tomorrow: Luma spotted something glowing near the crystal caverns.",
                "Tomorrow: A rare world event may visit the ancient observatory.",
                "Tomorrow: The campfire will be extra warm for a bedtime story.",
            };
            return previews[(DailyStreak + 1) % previews.Length];
        }

        public string GetStreakMessage()
        {
            return DailyStreak switch
            {
                1  => "Your first day — the forest welcomes you!",
                3  => "3 days in a row — Pip is impressed!",
                7  => "A whole week! Tomo bows with respect.",
                14 => "Two weeks! The forest remembers you now.",
                30 => "A full month — you are a true Forest Guardian.",
                _  => $"{DailyStreak} days — the forest grows with your care."
            };
        }

        public float GetSessionMinutesPlayed() => (Time.time - _sessionStartTime) / 60f;

        // ─── Private Helpers ──────────────────────────────────────────────────────

        private void LoadStreak()
        {
            DailyStreak = PlayerPrefs.GetInt(StreakKey, 0);
            var lastDate = PlayerPrefs.GetString(LastPlayDateKey, string.Empty);
            PlayedToday = lastDate == DateTime.Today.ToString("yyyy-MM-dd");

            // Break streak if more than 2 days missed (gentle — 1 miss is forgiven)
            if (!string.IsNullOrEmpty(lastDate) && DateTime.TryParse(lastDate, out var last))
            {
                int daysMissed = (DateTime.Today - last).Days;
                if (daysMissed > 2)
                {
                    DailyStreak = 0;
                    PlayerPrefs.SetInt(StreakKey, 0);
                }
            }
        }

        private void CheckDailyReturn()
        {
            var lastDate = PlayerPrefs.GetString(LastPlayDateKey, string.Empty);
            if (!string.IsNullOrEmpty(lastDate) && lastDate != DateTime.Today.ToString("yyyy-MM-dd"))
            {
                // Player is returning — prepare emotional moment
                var msg = GetReturnMessage();
                OnReturnMomentReady?.Invoke(msg);

                var preview = GetTomorrowPreview();
                OnTomorrowPreviewReady?.Invoke(preview);
            }
        }
    }
}
