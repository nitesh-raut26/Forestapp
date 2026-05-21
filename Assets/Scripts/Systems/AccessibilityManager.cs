using System;
using UnityEngine;
using UnityEngine.UI;

namespace ForestFriendsQuest
{
    public enum ColorblindFilter
    {
        None,
        Protanopia, // Red-weak
        Deuteranopia, // Green-weak
        Tritanopia // Blue-weak
    }

    public class AccessibilityManager : MonoBehaviour
    {
        // ─── Events ──────────────────────────────────────────────────────────────

        /// <summary>Fired whenever calm mode is toggled. Passes the new enabled state.</summary>
        public event Action<bool>              OnCalmModeChanged;
        /// <summary>Fired whenever the colorblind filter changes. Passes the new filter.</summary>
        public event Action<ColorblindFilter>  OnColorblindModeChanged;

        // ─── State ───────────────────────────────────────────────────────────────

        private bool _dyslexiaFontEnabled = false;
        private bool _calmModeEnabled = false;
        private ColorblindFilter _activeFilter = ColorblindFilter.None;

        private Font _standardFont;
        private Font _dyslexicFont;

        public bool DyslexiaFontEnabled => _dyslexiaFontEnabled;
        public bool CalmModeEnabled => _calmModeEnabled;
        public ColorblindFilter ActiveFilter => _activeFilter;

        public void Initialize(Font standard, Font dyslexic)
        {
            _standardFont = standard;
            _dyslexicFont = dyslexic;
        }

        public void SetDyslexiaFont(bool enabled)
        {
            _dyslexiaFontEnabled = enabled;
            ApplyAccessibilitySettings();
        }

        public void SetCalmMode(bool enabled)
        {
            _calmModeEnabled = enabled;
            ApplyAccessibilitySettings();
            OnCalmModeChanged?.Invoke(enabled);
        }

        public void SetColorblindFilter(ColorblindFilter filter)
        {
            _activeFilter = filter;
            ApplyAccessibilitySettings();
            OnColorblindModeChanged?.Invoke(filter);
        }

        public void ApplyAccessibilitySettings()
        {
            // Traverse all Text elements in active Canvas and apply correct font settings
            var texts = FindObjectsByType<Text>(FindObjectsSortMode.None);
            var targetFont = _dyslexiaFontEnabled && _dyslexicFont != null ? _dyslexicFont : _standardFont;

            if (targetFont != null)
            {
                foreach (var txt in texts)
                {
                    txt.font = targetFont;
                }
            }
        }

        public Color AdaptColor(Color originalColor)
        {
            if (_activeFilter == ColorblindFilter.None) return originalColor;

            // Simple algorithmic color adjustment to assist colorblindness
            var r = originalColor.r;
            var g = originalColor.g;
            var b = originalColor.b;

            switch (_activeFilter)
            {
                case ColorblindFilter.Protanopia:
                    // Enhance red visibility or shift red tones
                    return new Color(r * 0.9f + g * 0.1f, g * 0.8f + r * 0.2f, b, originalColor.a);
                case ColorblindFilter.Deuteranopia:
                    // Shift green tones away from red overlap
                    return new Color(r * 0.8f + g * 0.2f, g * 0.9f + r * 0.1f, b, originalColor.a);
                case ColorblindFilter.Tritanopia:
                    // Assist blue/yellow differentiation
                    return new Color(r, g * 0.7f + b * 0.3f, b * 0.7f + g * 0.3f, originalColor.a);
                default:
                    return originalColor;
            }
        }
    }
}
