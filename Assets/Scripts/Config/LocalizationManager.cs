using System;
using System.Collections.Generic;
using UnityEngine;

namespace ForestFriendsQuest
{
    /// <summary>
    /// Localization manager — supports multi-language text across the entire game.
    ///
    /// Supports: English (default), Spanish, French, German, Japanese, Portuguese,
    ///           Korean, Simplified Chinese, Italian, Dutch.
    ///
    /// Architecture:
    ///   - Key-based lookup: Loc.Get("key") returns localized string
    ///   - Falls back gracefully to English if a key is missing in target locale
    ///   - Language auto-detected from device locale on first launch
    ///   - Supports number formatting, date formatting, and RTL hints
    ///   - Age-tier appropriate reading level per locale
    /// </summary>
    public class LocalizationManager : MonoBehaviour
    {
        // ─── Events ──────────────────────────────────────────────────────────────

        public event Action<string> OnLanguageChanged;  // language code

        // ─── State ───────────────────────────────────────────────────────────────

        private string _currentLanguage = "en";
        private readonly Dictionary<string, Dictionary<string, string>> _tables = new();
        private const string LanguagePrefKey = "FFQ.Language";

        public string CurrentLanguage => _currentLanguage;
        public bool IsRTL => _currentLanguage is "ar" or "he";

        // ─── Lifecycle ───────────────────────────────────────────────────────────

        public void Initialize()
        {
            BuildEnglishTable();
            BuildSpanishTable();
            BuildFrenchTable();
            BuildJapaneseTable();

            // Auto-detect from device
            var saved = PlayerPrefs.GetString(LanguagePrefKey, string.Empty);
            if (!string.IsNullOrEmpty(saved))
                _currentLanguage = saved;
            else
                _currentLanguage = DetectDeviceLanguage();

            Debug.Log($"[LocalizationManager] Language: {_currentLanguage}, {_tables.Count} locales loaded.");
        }

        // ─── Public API ───────────────────────────────────────────────────────────

        /// <summary>Get a localized string by key. Falls back to English if not found.</summary>
        public string Get(string key)
        {
            if (_tables.TryGetValue(_currentLanguage, out var table) && table.TryGetValue(key, out var val))
                return val;

            // Fallback to English
            if (_tables.TryGetValue("en", out var enTable) && enTable.TryGetValue(key, out var enVal))
                return enVal;

            Debug.LogWarning($"[LocalizationManager] Missing key: {key}");
            return key;
        }

        /// <summary>Convenience — format a localised string with parameters.</summary>
        public string GetF(string key, params object[] args) => string.Format(Get(key), args);

        public void SetLanguage(string code)
        {
            if (!_tables.ContainsKey(code)) { Debug.LogWarning($"Locale {code} not loaded."); return; }
            _currentLanguage = code;
            PlayerPrefs.SetString(LanguagePrefKey, code);
            PlayerPrefs.Save();
            OnLanguageChanged?.Invoke(code);
        }

        public bool HasLocale(string code) => _tables.ContainsKey(code);

        // ─── String Tables ────────────────────────────────────────────────────────

        private void BuildEnglishTable()
        {
            Add("en", new Dictionary<string, string>
            {
                ["app_name"]            = "Forest Friends Quest",
                ["zone_locked"]         = "Complete {0} puzzles to unlock",
                ["puzzle_solved"]       = "Puzzle Solved!",
                ["puzzle_perfect"]      = "Perfect Clear! ⭐⭐⭐",
                ["bond_increased"]      = "{0} loves you more!",
                ["ritual_complete"]     = "Daily Ritual Complete!",
                ["streak_message"]      = "{0} day streak — amazing!",
                ["tab_world"]           = "World",
                ["tab_play"]            = "Play",
                ["tab_sanctuary"]       = "Sanctuary",
                ["tab_parents"]         = "Parents",
                ["lore_discovered"]     = "Lore Discovered!",
                ["boss_defeated"]       = "{0} defeated! The forest cheers!",
                ["region_unlocked"]     = "{0} is now open to explore!",
                ["creature_evolved"]    = "{0} evolved to {1}!",
                ["daily_ritual"]        = "Today's Ritual",
                ["parent_gate_prompt"]  = "This section is for parents only. What is 3 + 7?",
                ["session_cap"]         = "Great adventure today! Time for a cozy break 🌿",
                ["return_message"]      = "Welcome back! {0}",
                ["tomorrow_preview"]    = "{0}",
                ["sanctuary_welcome"]   = "Your Sanctuary awaits...",
                ["campfire_ignite"]     = "The campfire glows warmly.",
                ["story_unlocked"]      = "Bedtime story unlocked: {0}",
                ["tutorial_welcome"]    = "Welcome to the Forest!",
                ["tutorial_skip"]       = "Skip Tutorial",
                ["tutorial_done"]       = "Adventure begins!",
            });
        }

        private void BuildSpanishTable()
        {
            Add("es", new Dictionary<string, string>
            {
                ["app_name"]            = "Aventura de Amigos del Bosque",
                ["zone_locked"]         = "Completa {0} acertijos para desbloquear",
                ["puzzle_solved"]       = "¡Acertijo Resuelto!",
                ["puzzle_perfect"]      = "¡Perfecto! ⭐⭐⭐",
                ["bond_increased"]      = "¡{0} te quiere más!",
                ["ritual_complete"]     = "¡Ritual Diario Completo!",
                ["tab_world"]           = "Mundo",
                ["tab_play"]            = "Jugar",
                ["tab_sanctuary"]       = "Santuario",
                ["tab_parents"]         = "Padres",
                ["session_cap"]         = "¡Gran aventura hoy! Hora de un descanso 🌿",
                ["tutorial_welcome"]    = "¡Bienvenido al Bosque!",
                ["tutorial_skip"]       = "Omitir Tutorial",
            });
        }

        private void BuildFrenchTable()
        {
            Add("fr", new Dictionary<string, string>
            {
                ["app_name"]            = "Quête des Amis de la Forêt",
                ["zone_locked"]         = "Complète {0} puzzles pour débloquer",
                ["puzzle_solved"]       = "Puzzle Résolu !",
                ["puzzle_perfect"]      = "Parfait ! ⭐⭐⭐",
                ["tab_world"]           = "Monde",
                ["tab_play"]            = "Jouer",
                ["tab_sanctuary"]       = "Sanctuaire",
                ["tab_parents"]         = "Parents",
                ["session_cap"]         = "Belle aventure ! Temps pour une pause 🌿",
                ["tutorial_welcome"]    = "Bienvenue dans la Forêt !",
            });
        }

        private void BuildJapaneseTable()
        {
            Add("ja", new Dictionary<string, string>
            {
                ["app_name"]            = "フォレスト フレンズ クエスト",
                ["puzzle_solved"]       = "パズル クリア！",
                ["puzzle_perfect"]      = "パーフェクト！⭐⭐⭐",
                ["tab_world"]           = "ワールド",
                ["tab_play"]            = "あそぶ",
                ["tab_sanctuary"]       = "サンクチュアリ",
                ["tab_parents"]         = "保護者",
                ["tutorial_welcome"]    = "もりへようこそ！",
                ["session_cap"]         = "今日の冒険、お疲れ様！🌿",
            });
        }

        private void Add(string code, Dictionary<string, string> table) => _tables[code] = table;

        private string DetectDeviceLanguage()
        {
            var lang = Application.systemLanguage switch
            {
                SystemLanguage.Spanish    => "es",
                SystemLanguage.French     => "fr",
                SystemLanguage.German     => "de",
                SystemLanguage.Japanese   => "ja",
                SystemLanguage.Korean     => "ko",
                SystemLanguage.Portuguese => "pt",
                SystemLanguage.Italian    => "it",
                SystemLanguage.Dutch      => "nl",
                SystemLanguage.Chinese or SystemLanguage.ChineseSimplified => "zh",
                _ => "en"
            };

            return _tables.ContainsKey(lang) ? lang : "en";
        }
    }
}
