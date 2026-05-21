using System;
using UnityEngine;
using UnityEngine.UI;

namespace ForestFriendsQuest
{
    /// <summary>
    /// Abstract base for all panel view controllers.
    ///
    /// A panel owns exactly one root RectTransform and manages the UI elements
    /// within it. Instead of being destroyed and recreated, panels update only
    /// the elements whose data has changed (dirty-flag pattern).
    ///
    /// Subclass contract:
    ///   - Override OnBuild()    — called once on first activation to create child elements
    ///   - Override OnRefresh()  — called when dirty flags require a data update
    ///   - Override OnShow()     — called when the panel becomes visible
    ///   - Override OnHide()     — called when the panel is hidden
    /// </summary>
    public abstract class PanelViewController : MonoBehaviour
    {
        // ─── Dependencies ────────────────────────────────────────────────────────

        protected ForestSystemsContainer Systems { get; private set; }
        protected ForestGameContent      Content  { get; private set; }
        protected ForestSaveData         SaveData { get; private set; }

        // ─── UI Root ─────────────────────────────────────────────────────────────

        public RectTransform RootTransform { get; private set; }
        protected CanvasGroup CanvasGroup  { get; private set; }

        // ─── State ───────────────────────────────────────────────────────────────

        private bool        _isBuilt;
        private UIDirtyFlag _dirtyFlags = UIDirtyFlag.All;

        public bool IsVisible   { get; private set; }

        // ─── Initialization ───────────────────────────────────────────────────────

        public virtual void Initialize(
            RectTransform          root,
            ForestSystemsContainer systems,
            ForestGameContent      content,
            ForestSaveData         saveData)
        {
            RootTransform = root;
            Systems       = systems;
            Content       = content;
            SaveData      = saveData;

            CanvasGroup = root.gameObject.GetComponent<CanvasGroup>()
                ?? root.gameObject.AddComponent<CanvasGroup>();

            CanvasGroup.alpha          = 0f;
            CanvasGroup.blocksRaycasts = false;
        }

        // ─── Lifecycle (called by UIStateController) ──────────────────────────────

        /// <summary>Panel is about to become the active state.</summary>
        public void OnActivate()
        {
            if (!_isBuilt)
            {
                OnBuild();
                _isBuilt = true;
            }

            if (_dirtyFlags != UIDirtyFlag.None)
            {
                OnRefresh(_dirtyFlags);
                _dirtyFlags = UIDirtyFlag.None;
            }

            OnShow();
        }

        /// <summary>Panel is leaving the active state.</summary>
        public void OnDeactivate()
        {
            OnHide();
        }

        /// <summary>Force-refresh with current dirty flags.</summary>
        public void Refresh()
        {
            if (!_isBuilt) return;
            if (_dirtyFlags == UIDirtyFlag.None) return;

            OnRefresh(_dirtyFlags);
            _dirtyFlags = UIDirtyFlag.None;
        }

        /// <summary>Mark data domains dirty — panel refreshes on next activation.</summary>
        public void MarkDirty(UIDirtyFlag flags)
        {
            _dirtyFlags |= flags;

            // If already visible, refresh immediately
            if (IsVisible && _isBuilt)
            {
                OnRefresh(_dirtyFlags);
                _dirtyFlags = UIDirtyFlag.None;
            }
        }

        /// <summary>Show or hide panel. Instant = skip fade animation.</summary>
        public void SetVisible(bool visible, bool instant)
        {
            IsVisible = visible;
            if (instant)
            {
                CanvasGroup.alpha          = visible ? 1f : 0f;
                CanvasGroup.blocksRaycasts = visible;
                RootTransform.gameObject.SetActive(visible);
            }
            else
            {
                RootTransform.gameObject.SetActive(visible);
                CanvasGroup.blocksRaycasts = visible;
            }
        }

        /// <summary>Update the SaveData reference (called after save/load events).</summary>
        public void UpdateSaveData(ForestSaveData saveData)
        {
            SaveData = saveData;
            MarkDirty(UIDirtyFlag.All);
        }

        // ─── Subclass Overrides ───────────────────────────────────────────────────

        /// <summary>Build all child GameObjects once. Only called on first activation.</summary>
        protected abstract void OnBuild();

        /// <summary>Update only the elements affected by the dirty flags.</summary>
        protected abstract void OnRefresh(UIDirtyFlag dirtyFlags);

        /// <summary>Called each time the panel becomes the active view.</summary>
        protected virtual void OnShow()  { }

        /// <summary>Called each time the panel stops being the active view.</summary>
        protected virtual void OnHide() { }

        // ─── Shared UI Helpers ────────────────────────────────────────────────────

        protected Text CreateLabel(Transform parent, string name, string text,
            int fontSize, Color color, TextAnchor anchor = TextAnchor.MiddleLeft)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            var rt = go.AddComponent<RectTransform>();
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.sizeDelta = Vector2.zero;

            var t = go.AddComponent<Text>();
            t.text      = text;
            t.fontSize  = fontSize;
            t.color     = color;
            t.alignment = anchor;
            t.font      = ForestUiFactory.GetDefaultFont();
            return t;
        }

        protected Button CreateButton(Transform parent, string name, string label,
            Color bgColor, Color textColor, Action onClick, int fontSize = 24)
        {
            return ForestUiFactory.CreateButton(
                parent as RectTransform,
                name, label,
                ForestUiFactory.GetDefaultFont(),
                bgColor, textColor,
                onClick, fontSize);
        }

        protected Image CreatePanel(Transform parent, string name, Color color, float radius = 8f)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            var img = go.AddComponent<Image>();
            img.color = color;
            return img;
        }
    }
}
