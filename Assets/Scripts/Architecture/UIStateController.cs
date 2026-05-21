using System;
using System.Collections.Generic;
using UnityEngine;

namespace ForestFriendsQuest
{
    /// <summary>
    /// Central UI state machine. Replaces the Rebuild() antipattern completely.
    ///
    /// Instead of destroying and recreating every GameObject on every tap,
    /// UIStateController tracks which UI state is active and tells only the
    /// panels affected by a state change to update themselves. Panels are
    /// never destroyed — they are shown, hidden, or refreshed in-place.
    ///
    /// State change flow:
    ///   1. System calls RequestStateChange(newState)
    ///   2. UIStateController validates the transition
    ///   3. Active panel receives OnDeactivate()
    ///   4. Transition animation plays
    ///   5. New panel receives OnActivate()
    ///   6. Only dirty components within the new panel call Refresh()
    /// </summary>
    public class UIStateController : MonoBehaviour
    {
        // ─── State Definitions ────────────────────────────────────────────────────

        public enum UIState
        {
            None,
            Play,
            WorldMap,
            Sanctuary,
            Ritual,
            Parents,
            CreatureDetail,
            BossEncounter,
            LevelActive,
            Settings,
            Accessibility
        }

        // ─── Events ───────────────────────────────────────────────────────────────

        public event Action<UIState, UIState> OnStateWillChange;   // (from, to)
        public event Action<UIState, UIState> OnStateDidChange;    // (from, to)
        public event Action<string>           OnBreadcrumbChanged; // navigation label

        // ─── Dependencies ────────────────────────────────────────────────────────

        private AnimatedTransitionController _transition;
        private ForestUIRouter               _router;

        // ─── State ───────────────────────────────────────────────────────────────

        private UIState _currentState  = UIState.None;
        private UIState _previousState = UIState.None;
        private bool    _isTransitioning;

        private readonly Dictionary<UIState, PanelViewController> _panels =
            new Dictionary<UIState, PanelViewController>();

        private readonly Stack<UIState> _history = new Stack<UIState>();

        public UIState Current  => _currentState;
        public UIState Previous => _previousState;
        public bool    CanGoBack => _history.Count > 1;

        // ─── Lifecycle ───────────────────────────────────────────────────────────

        public void Initialize(AnimatedTransitionController transition, ForestUIRouter router)
        {
            _transition = transition;
            _router     = router;
        }

        // ─── Panel Registration ───────────────────────────────────────────────────

        public void RegisterPanel(UIState state, PanelViewController panel)
        {
            _panels[state] = panel;

            // All panels start hidden except the initial state
            if (state != UIState.Play)
                panel.SetVisible(false, instant: true);
        }

        // ─── State Navigation ─────────────────────────────────────────────────────

        /// <summary>Navigate to a new state. Respects transition lock.</summary>
        public void GoTo(UIState newState, bool addToHistory = true)
        {
            if (_isTransitioning)
            {
                Debug.LogWarning($"[UIStateController] Transition in progress, queuing {newState}");
                return;
            }

            if (newState == _currentState) return;

            var fromState = _currentState;
            OnStateWillChange?.Invoke(fromState, newState);

            _isTransitioning = true;

            // Notify leaving panel
            if (_panels.TryGetValue(fromState, out var leavingPanel))
                leavingPanel.OnDeactivate();

            // History management
            if (addToHistory && fromState != UIState.None)
                _history.Push(fromState);

            _previousState = fromState;
            _currentState  = newState;

            // Run transition, then activate new panel
            if (_transition != null)
            {
                _transition.CrossFade(
                    leavingPanel?.RootTransform,
                    GetPanel(newState)?.RootTransform,
                    onMidpoint: () =>
                    {
                        ActivatePanel(newState);
                    },
                    onComplete: () =>
                    {
                        _isTransitioning = false;
                        OnStateDidChange?.Invoke(fromState, newState);
                        UpdateBreadcrumb(newState);
                    }
                );
            }
            else
            {
                // No transition system — instant swap
                ActivatePanel(newState);
                _isTransitioning = false;
                OnStateDidChange?.Invoke(fromState, newState);
                UpdateBreadcrumb(newState);
            }
        }

        /// <summary>Navigate back in history stack.</summary>
        public void GoBack()
        {
            if (!CanGoBack) return;
            var target = _history.Pop();
            GoTo(target, addToHistory: false);
        }

        /// <summary>Refresh the current panel's dirty components without a state change.</summary>
        public void RefreshCurrent()
        {
            if (_panels.TryGetValue(_currentState, out var panel))
                panel.Refresh();
        }

        /// <summary>Mark a specific data domain dirty so panels update it on next show.</summary>
        public void MarkDirty(UIDirtyFlag flag)
        {
            foreach (var panel in _panels.Values)
                panel.MarkDirty(flag);
        }

        // ─── Panel Accessors ──────────────────────────────────────────────────────

        public PanelViewController GetPanel(UIState state)
        {
            _panels.TryGetValue(state, out var p);
            return p;
        }

        public T GetPanel<T>(UIState state) where T : PanelViewController
        {
            return GetPanel(state) as T;
        }

        // ─── Private Helpers ─────────────────────────────────────────────────────

        private void ActivatePanel(UIState state)
        {
            if (!_panels.TryGetValue(state, out var panel)) return;
            panel.SetVisible(true, instant: false);
            panel.OnActivate();
        }

        private void UpdateBreadcrumb(UIState state)
        {
            var label = state switch
            {
                UIState.Play         => "Quest",
                UIState.WorldMap     => "World",
                UIState.Sanctuary    => "Sanctuary",
                UIState.Ritual       => "Daily Ritual",
                UIState.Parents      => "Parents",
                UIState.BossEncounter => "Boss Encounter",
                UIState.LevelActive  => "Puzzle",
                UIState.Settings     => "Settings",
                UIState.Accessibility => "Accessibility",
                _                    => "Forest"
            };
            OnBreadcrumbChanged?.Invoke(label);
        }
    }

    // ─── Dirty Flag Bitmask ───────────────────────────────────────────────────────

    [Flags]
    public enum UIDirtyFlag
    {
        None         = 0,
        Progress     = 1 << 0,   // Level clears, zone unlocks
        BondLevels   = 1 << 1,   // Creature bond changes
        Treats       = 1 << 2,   // Currency changes
        Crafting     = 1 << 3,   // Inventory / crafting changes
        Rituals      = 1 << 4,   // Daily ritual state
        Achievements = 1 << 5,   // Achievement unlocks
        WorldState   = 1 << 6,   // Region unlock state
        Season       = 1 << 7,   // Season / weather change
        Evolution    = 1 << 8,   // Creature evolution
        All          = ~0
    }
}
