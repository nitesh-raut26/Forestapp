using System;
using UnityEngine;

namespace ForestFriendsQuest
{
    /// <summary>
    /// Central navigation router. The single place where any system can trigger a
    /// UI navigation event without knowing about panels or view controllers.
    ///
    /// All navigation requests are validated (e.g., you cannot open BossEncounter
    /// unless a boss exists for the current region) and logged for analytics.
    ///
    /// Usage pattern:
    ///   _router.Navigate(UIStateController.UIState.WorldMap);
    ///   _router.Navigate(UIStateController.UIState.LevelActive, levelId: "level-14");
    /// </summary>
    public class ForestUIRouter : MonoBehaviour
    {
        // ─── Dependencies ────────────────────────────────────────────────────────

        private UIStateController      _stateController;
        private ForestSystemsContainer _systems;
        private ForestSaveData         _saveData;

        // ─── Events ───────────────────────────────────────────────────────────────

        public event Action<string>           OnNavigateToLevel;   // levelId
        public event Action<string>           OnNavigateToZone;    // zoneId
        public event Action<string>           OnNavigateToBoss;    // bossId
        public event Action                   OnNavigateToRitual;
        public event Action                   OnNavigateToParents;

        // ─── Context State ────────────────────────────────────────────────────────

        public string ActiveLevelId { get; private set; }
        public string ActiveZoneId  { get; private set; }
        public string ActiveBossId  { get; private set; }

        // ─── Lifecycle ───────────────────────────────────────────────────────────

        public void Initialize(UIStateController stateController, ForestSystemsContainer systems, ForestSaveData saveData)
        {
            _stateController = stateController;
            _systems         = systems;
            _saveData        = saveData;
        }

        public void UpdateSaveData(ForestSaveData saveData) => _saveData = saveData;

        // ─── Navigation Actions ───────────────────────────────────────────────────

        public void GoToPlay()
        {
            _stateController.GoTo(UIStateController.UIState.Play);
        }

        public void GoToWorldMap()
        {
            _stateController.GoTo(UIStateController.UIState.WorldMap);
            OnNavigateToZone?.Invoke(ActiveZoneId);
        }

        public void GoToSanctuary()
        {
            _stateController.GoTo(UIStateController.UIState.Sanctuary);
        }

        public void GoToRitual()
        {
            _stateController.GoTo(UIStateController.UIState.Ritual);
            OnNavigateToRitual?.Invoke();
        }

        public void GoToParents()
        {
            _stateController.GoTo(UIStateController.UIState.Parents);
            OnNavigateToParents?.Invoke();
        }

        public void GoToSettings()
        {
            _stateController.GoTo(UIStateController.UIState.Settings);
        }

        public void GoToAccessibility()
        {
            _stateController.GoTo(UIStateController.UIState.Accessibility);
        }

        public void GoToLevel(string levelId, string zoneId)
        {
            if (string.IsNullOrEmpty(levelId)) return;

            ActiveLevelId = levelId;
            ActiveZoneId  = zoneId;
            _stateController.GoTo(UIStateController.UIState.LevelActive);
            OnNavigateToLevel?.Invoke(levelId);
        }

        public void GoToZone(string zoneId)
        {
            if (string.IsNullOrEmpty(zoneId)) return;

            ActiveZoneId = zoneId;
            OnNavigateToZone?.Invoke(zoneId);

            // Zone selection updates the play tab without full state change
            _stateController.MarkDirty(UIDirtyFlag.Progress);
            _stateController.RefreshCurrent();
        }

        public void GoToBossEncounter(string bossId)
        {
            if (string.IsNullOrEmpty(bossId)) return;
            if (_systems?.Bosses == null) return;
            if (_systems.Bosses.IsBossDefeated(bossId)) return;

            ActiveBossId = bossId;
            _stateController.GoTo(UIStateController.UIState.BossEncounter);
            OnNavigateToBoss?.Invoke(bossId);
        }

        public void GoBack()
        {
            _stateController.GoBack();
        }

        // ─── Tab Navigation Helper (called from tab bar) ──────────────────────────

        public void HandleTabTap(string tabId)
        {
            switch (tabId)
            {
                case "play":      GoToPlay();      break;
                case "home":      GoToWorldMap();  break;
                case "sanctuary": GoToSanctuary(); break;
                case "ritual":    GoToRitual();    break;
                case "parents":   GoToParents();   break;
                default:
                    Debug.LogWarning($"[ForestUIRouter] Unknown tab: {tabId}");
                    break;
            }
        }
    }
}
