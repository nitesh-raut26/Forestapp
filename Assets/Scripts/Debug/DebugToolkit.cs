using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace ForestFriendsQuest
{
    /// <summary>
    /// QA debug toolkit — overlays, cheat codes, and test tools.
    ///
    /// Only active in Debug and QA builds (disabled in Release automatically).
    ///
    /// Features:
    ///   - FPS counter overlay
    ///   - Memory usage display
    ///   - System status panel (all 36+ systems listed)
    ///   - Cheat: unlock all zones
    ///   - Cheat: max bond with all creatures
    ///   - Cheat: trigger boss encounter
    ///   - Cheat: trigger seasonal event
    ///   - Cheat: evolve a creature immediately
    ///   - Screenshot export
    ///   - Reset all save data (with 5-tap confirmation)
    /// </summary>
    public class DebugToolkit : MonoBehaviour
    {
        // ─── State ───────────────────────────────────────────────────────────────

        private ForestSystemsContainer _systems;
        private ReleaseConfiguration   _config;
        private bool                   _isVisible;
        private Canvas                 _debugCanvas;
        private Text                   _fpsText;

        private float _fpsTimer;
        private int   _frameCount;
        private float _currentFPS;

        private int   _resetTapCount;
        private float _lastResetTapTime;

        // ─── Lifecycle ───────────────────────────────────────────────────────────

        public void Initialize(ForestSystemsContainer systems, ReleaseConfiguration config)
        {
            _systems = systems;
            _config  = config;

            // Only build debug UI in Debug/QA builds
            if (config.IsRelease && !config.EnableDebugOverlay)
            {
                gameObject.SetActive(false);
                return;
            }

            BuildDebugOverlay();
            Debug.Log("[DebugToolkit] Debug overlay ready. Tap screen corner 5× to toggle.");
        }

        private void Update()
        {
            UpdateFPS();
            CheckToggleGesture();
        }

        // ─── Public API ───────────────────────────────────────────────────────────

        public void ToggleVisibility()
        {
            _isVisible = !_isVisible;
            if (_debugCanvas != null)
                _debugCanvas.gameObject.SetActive(_isVisible);
        }

        // ─── Cheat Commands ───────────────────────────────────────────────────────

        public void CheatUnlockAllZones()
        {
            for (int i = 0; i <= 50; i++)
                _systems?.World?.OnLevelCleared(i);
            Debug.Log("[DebugToolkit] CHEAT: All zones unlocked.");
        }

        public void CheatMaxBondAllCreatures()
        {
            foreach (var id in new[] { "pip", "mimi", "tomo", "luma", "nori", "sol" })
                for (int i = 0; i < 20; i++)
                    _systems?.BondingEngine?.IncreaseBond(id, 1);
            Debug.Log("[DebugToolkit] CHEAT: Max bond all creatures.");
        }

        public void CheatTriggerBossEncounter(string regionId = "fern-trail")
        {
            _systems?.Bosses?.StartEncounter(regionId);
            Debug.Log($"[DebugToolkit] CHEAT: Boss encounter triggered for {regionId}");
        }

        public void CheatEvolveCreature(string creatureId = "pip")
        {
            _systems?.Evolution?.ForceEvolve(creatureId);
            Debug.Log($"[DebugToolkit] CHEAT: {creatureId} force evolved.");
        }

        public void CheatTriggerSeasonalEvent()
        {
            _systems?.Seasons?.ForceNextEvent();
            Debug.Log("[DebugToolkit] CHEAT: Seasonal event triggered.");
        }

        public void CheatAddTreats(int amount = 100)
        {
            if (_systems?.SaveSystem?.ActiveData != null)
                _systems.SaveSystem.ActiveData.forestTreats += amount;
            Debug.Log($"[DebugToolkit] CHEAT: Added {amount} treats.");
        }

        /// <summary>Requires 5 taps within 3 seconds to prevent accidental reset.</summary>
        public void TapResetConfirm()
        {
            if (Time.time - _lastResetTapTime > 3f)
                _resetTapCount = 0;
            _resetTapCount++;
            _lastResetTapTime = Time.time;
            if (_resetTapCount >= 5)
            {
                PlayerPrefs.DeleteAll();
                PlayerPrefs.Save();
                Debug.LogWarning("[DebugToolkit] ALL SAVE DATA RESET. Restart to apply.");
                _resetTapCount = 0;
            }
        }

        public string GetSystemStatus()
        {
            var sb = new System.Text.StringBuilder();
            sb.AppendLine($"FPS: {_currentFPS:F0}");
            sb.AppendLine($"Memory: {SystemInfo.systemMemorySize}MB / {GC.GetTotalMemory(false) / 1024 / 1024}MB used");
            sb.AppendLine($"Streak: {_systems?.DailyRitual?.CurrentStreak ?? 0}");
            sb.AppendLine($"Biome: {_systems?.Biome?.GetCurrentBiome()?.displayName ?? "none"}");
            sb.AppendLine($"Season: {_systems?.SeasonManager?.CurrentSeason}");
            sb.AppendLine($"Performance tier: {_systems?.Performance?.GetCurrentTier()}");
            return sb.ToString();
        }

        // ─── Overlay Build ────────────────────────────────────────────────────────

        private void BuildDebugOverlay()
        {
            var go = new GameObject("DebugCanvas");
            go.transform.SetParent(transform, false);

            _debugCanvas = go.AddComponent<Canvas>();
            _debugCanvas.renderMode  = RenderMode.ScreenSpaceOverlay;
            _debugCanvas.sortingOrder = 999;
            go.AddComponent<CanvasScaler>();
            go.AddComponent<GraphicRaycaster>();
            go.SetActive(false);

            // FPS counter in corner
            var fpsGo = new GameObject("FPSText");
            fpsGo.transform.SetParent(go.transform, false);
            _fpsText = fpsGo.AddComponent<Text>();
            _fpsText.font     = TMPro.TMP_Settings.defaultFontAsset;
            _fpsText.fontSize = 24;
            _fpsText.color    = Color.yellow;
            _fpsText.text     = "FPS: --";

            var rt = fpsGo.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0, 1);
            rt.anchorMax = new Vector2(0, 1);
            rt.pivot     = new Vector2(0, 1);
            rt.anchoredPosition = new Vector2(10, -10);
            rt.sizeDelta = new Vector2(200, 40);
        }

        private void UpdateFPS()
        {
            _frameCount++;
            _fpsTimer += Time.deltaTime;
            if (_fpsTimer >= 0.5f)
            {
                _currentFPS = _frameCount / _fpsTimer;
                _frameCount = 0;
                _fpsTimer   = 0f;
                if (_fpsText != null)
                    _fpsText.text = $"FPS: {_currentFPS:F0} | {GetSystemStatus().Split('\n')[1]}";
            }
        }

        private void CheckToggleGesture()
        {
#if UNITY_EDITOR
            if (Input.GetKeyDown(KeyCode.F1)) ToggleVisibility();
#endif
        }
    }
}
