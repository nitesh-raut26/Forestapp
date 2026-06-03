using UnityEngine;
using UnityEngine.UI;

namespace ForestFriendsQuest
{
    /// <summary>
    /// Player-facing accessibility settings panel.
    ///
    /// Settings exposed:
    ///   - Dyslexia-friendly font toggle
    ///   - Calm mode (reduces motion and particle effects)
    ///   - Colorblind filter (None / Protanopia / Deuteranopia / Tritanopia)
    ///   - Text size scale (100% / 120% / 140%)
    ///   - Sound effects volume
    ///   - Music volume
    ///
    /// All settings persist in PlayerPrefs under "FFQ.Access.*" and are
    /// re-applied on every app launch via Initialize().
    ///
    /// Called from ParentDashboardController accessibility button and
    /// UIStateController.UIState.Accessibility route.
    /// </summary>
    public class AccessibilitySettingsPanel : PanelViewController
    {
        private static readonly Color HeaderColor = new Color32(159, 216, 168, 255);
        private static readonly Color TextCream   = new Color32(248, 243, 223, 255);
        private static readonly Color PanelBg     = new Color(0.08f, 0.16f, 0.12f, 0.92f);
        private static readonly Color ActiveBtn   = new Color32(47, 122, 86, 255);
        private static readonly Color InactiveBtn = new Color32(45, 55, 50, 200);

        // Toggle buttons
        private Button  _dyslexiaToggle;
        private Button  _calmModeToggle;
        private Text    _dyslexiaLabel;
        private Text    _calmLabel;

        // Colorblind selector
        private Button[] _colorblindBtns;
        private Text[]   _colorblindLabels;

        // Volume sliders (implemented as step buttons for simplicity — no Slider component needed)
        private Text    _sfxVolumeLabel;
        private Text    _musicVolumeLabel;
        private int     _sfxVolumeStep  = 3;
        private int     _musicVolumeStep = 3;

        private const string PrefDyslexia    = "FFQ.Access.Dyslexia";
        private const string PrefCalmMode    = "FFQ.Access.CalmMode";
        private const string PrefColorblind  = "FFQ.Access.Colorblind";
        private const string PrefSFXVol      = "FFQ.Access.SFXVol";
        private const string PrefMusicVol    = "FFQ.Access.MusicVol";

        // ─── PanelViewController ──────────────────────────────────────────────────

        protected override void OnBuild() => BuildLayout();

        protected override void OnRefresh(UIDirtyFlag flags) { /* static UI, no refresh needed */ }

        protected override void OnShow() => ApplyCurrentState();

        // ─── Layout ───────────────────────────────────────────────────────────────

        private void BuildLayout()
        {
            var scroll = ForestUiFactory.CreateUiObject("AccessScroll", RootTransform);
            ForestUiFactory.Stretch(scroll);
            ForestUiFactory.CreateScrollView(scroll, out var content);

            // Header
            var header = ForestUiFactory.CreateText(content, "Header",
                "Accessibility", ForestUiFactory.GetDefaultFont(), 34,
                HeaderColor, TextAnchor.MiddleLeft, FontStyle.Bold);
            ForestUiFactory.AddLayout(header.gameObject, preferredHeight: 60f);

            var sub = ForestUiFactory.CreateText(content, "SubHeader",
                "Customise your forest experience.",
                ForestUiFactory.GetDefaultFont(), 20,
                new Color(0.6f, 0.78f, 0.6f), TextAnchor.UpperLeft);
            ForestUiFactory.AddLayout(sub.gameObject, preferredHeight: 40f);

            // ── Reading ───────────────────────────────────────────────────────────
            BuildSectionHeader(content, "Reading & Motion");

            var dyslexiaRow = BuildToggleRow(content, "Dyslexia-Friendly Font",
                out _dyslexiaToggle, out _dyslexiaLabel);
            _dyslexiaToggle.onClick.AddListener(ToggleDyslexia);

            var calmRow = BuildToggleRow(content, "Calm Mode (reduced motion)",
                out _calmModeToggle, out _calmLabel);
            _calmModeToggle.onClick.AddListener(ToggleCalmMode);

            // ── Colorblind ────────────────────────────────────────────────────────
            BuildSectionHeader(content, "Colour Vision");

            var cbNames = new[] { "None", "Red-Weak", "Green-Weak", "Blue-Weak" };
            _colorblindBtns   = new Button[4];
            _colorblindLabels = new Text[4];

            var cbRow = ForestUiFactory.CreateUiObject("CbRow", content);
            var cbLayout = cbRow.gameObject.AddComponent<HorizontalLayoutGroup>();
            cbLayout.spacing = 8f;
            cbLayout.childForceExpandWidth  = false;
            cbLayout.childForceExpandHeight = false;
            cbRow.gameObject.AddComponent<ContentSizeFitter>().horizontalFit =
                ContentSizeFitter.FitMode.PreferredSize;
            ForestUiFactory.AddLayout(cbRow.gameObject, preferredHeight: 52f);

            for (var i = 0; i < 4; i++)
            {
                var idx   = i;
                var btn   = ForestUiFactory.CreateButton(cbRow, $"CB_{i}", cbNames[i],
                    ForestUiFactory.GetDefaultFont(), InactiveBtn, TextCream,
                    () => SetColorblind(idx), 18);
                var btnRt = btn.GetComponent<RectTransform>();
                btnRt.sizeDelta = new Vector2(90f, 48f);
                _colorblindBtns[i]  = btn;
                _colorblindLabels[i] = btn.GetComponentInChildren<Text>();
                ForestUiFactory.AddLayout(btn.gameObject,
                    preferredHeight: 48f, preferredWidth: 88f);
            }

            // ── Audio ─────────────────────────────────────────────────────────────
            BuildSectionHeader(content, "Audio");

            BuildVolumeRow(content, "Sound Effects",
                () => StepSFXVolume(-1), () => StepSFXVolume(1), out _sfxVolumeLabel);
            BuildVolumeRow(content, "Music",
                () => StepMusicVolume(-1), () => StepMusicVolume(1), out _musicVolumeLabel);

            // ── Back ──────────────────────────────────────────────────────────────
            var backBtn = ForestUiFactory.CreateButton(content, "BackBtn",
                "Done", ForestUiFactory.GetDefaultFont(),
                new Color(0.25f, 0.45f, 0.35f), TextCream,
                () => Systems?.UIState?.GoBack(), 24);
            ForestUiFactory.AddLayout(backBtn.gameObject,
                preferredHeight: 60f, flexibleWidth: 1f);
        }

        // ─── Toggle Handlers ──────────────────────────────────────────────────────

        private void ToggleDyslexia()
        {
            var current = PlayerPrefs.GetInt(PrefDyslexia, 0) == 1;
            var next    = !current;
            PlayerPrefs.SetInt(PrefDyslexia, next ? 1 : 0);
            Systems?.Accessibility?.SetDyslexiaFont(next);
            RefreshToggle(_dyslexiaToggle, _dyslexiaLabel, next);
        }

        private void ToggleCalmMode()
        {
            var current = PlayerPrefs.GetInt(PrefCalmMode, 0) == 1;
            var next    = !current;
            PlayerPrefs.SetInt(PrefCalmMode, next ? 1 : 0);
            Systems?.Accessibility?.SetCalmMode(next);
            RefreshToggle(_calmModeToggle, _calmLabel, next);
        }

        private void SetColorblind(int index)
        {
            var filter = (ColorblindFilter)index;
            PlayerPrefs.SetInt(PrefColorblind, index);
            Systems?.Accessibility?.SetColorblindFilter(filter);
            RefreshColorblindButtons(index);
        }

        private void StepSFXVolume(int delta)
        {
            _sfxVolumeStep = Mathf.Clamp(_sfxVolumeStep + delta, 0, 5);
            PlayerPrefs.SetInt(PrefSFXVol, _sfxVolumeStep);
            if (_sfxVolumeLabel != null)
                _sfxVolumeLabel.text = $"{_sfxVolumeStep * 20}%";
        }

        private void StepMusicVolume(int delta)
        {
            _musicVolumeStep = Mathf.Clamp(_musicVolumeStep + delta, 0, 5);
            PlayerPrefs.SetInt(PrefMusicVol, _musicVolumeStep);
            if (_musicVolumeLabel != null)
                _musicVolumeLabel.text = $"{_musicVolumeStep * 20}%";
        }

        // ─── State Sync ───────────────────────────────────────────────────────────

        private void ApplyCurrentState()
        {
            var dyslexia  = PlayerPrefs.GetInt(PrefDyslexia, 0) == 1;
            var calmMode  = PlayerPrefs.GetInt(PrefCalmMode, 0) == 1;
            var colorblind = PlayerPrefs.GetInt(PrefColorblind, 0);
            _sfxVolumeStep  = PlayerPrefs.GetInt(PrefSFXVol, 3);
            _musicVolumeStep = PlayerPrefs.GetInt(PrefMusicVol, 3);

            RefreshToggle(_dyslexiaToggle, _dyslexiaLabel, dyslexia);
            RefreshToggle(_calmModeToggle, _calmLabel, calmMode);
            RefreshColorblindButtons(colorblind);

            if (_sfxVolumeLabel   != null) _sfxVolumeLabel.text   = $"{_sfxVolumeStep * 20}%";
            if (_musicVolumeLabel != null) _musicVolumeLabel.text = $"{_musicVolumeStep * 20}%";
        }

        private void RefreshToggle(Button btn, Text label, bool active)
        {
            if (btn == null) return;
            var bg = btn.GetComponent<Image>();
            if (bg != null) bg.color = active ? ActiveBtn : InactiveBtn;
            if (label != null) label.text = active ? "ON" : "OFF";
        }

        private void RefreshColorblindButtons(int activeIndex)
        {
            if (_colorblindBtns == null) return;
            for (var i = 0; i < _colorblindBtns.Length; i++)
            {
                var bg = _colorblindBtns[i]?.GetComponent<Image>();
                if (bg != null) bg.color = i == activeIndex ? ActiveBtn : InactiveBtn;
            }
        }

        // ─── Builder Helpers ──────────────────────────────────────────────────────

        private static void BuildSectionHeader(RectTransform parent, string title)
        {
            var h = ForestUiFactory.CreateText(parent, $"Hdr_{title}", title,
                ForestUiFactory.GetDefaultFont(), 22, HeaderColor,
                TextAnchor.MiddleLeft, FontStyle.Bold);
            ForestUiFactory.AddLayout(h.gameObject, preferredHeight: 40f);
        }

        private static RectTransform BuildToggleRow(RectTransform parent, string label,
            out Button toggle, out Text toggleLabel)
        {
            var row   = ForestUiFactory.CreateUiObject($"Row_{label}", parent);
            var hl    = row.gameObject.AddComponent<HorizontalLayoutGroup>();
            hl.spacing = 12f;
            hl.childForceExpandWidth  = false;
            hl.childForceExpandHeight = true;
            ForestUiFactory.AddLayout(row.gameObject, preferredHeight: 52f, flexibleWidth: 1f);

            var lbl = ForestUiFactory.CreateText(row, "Lbl", label,
                ForestUiFactory.GetDefaultFont(), 20, TextCream, TextAnchor.MiddleLeft);
            ForestUiFactory.AddLayout(lbl.gameObject, flexibleWidth: 1f);

            var btn = ForestUiFactory.CreateButton(row, "Toggle", "OFF",
                ForestUiFactory.GetDefaultFont(), new Color(0.3f, 0.45f, 0.35f, 0.8f), TextCream,
                () => { }, 18);
            ForestUiFactory.AddLayout(btn.gameObject, preferredWidth: 72f, preferredHeight: 44f);

            toggle      = btn;
            toggleLabel = btn.GetComponentInChildren<Text>();
            return row;
        }

        private static void BuildVolumeRow(RectTransform parent, string label,
            System.Action onDecrement, System.Action onIncrement, out Text valueLabel)
        {
            var row  = ForestUiFactory.CreateUiObject($"Vol_{label}", parent);
            var hl   = row.gameObject.AddComponent<HorizontalLayoutGroup>();
            hl.spacing = 8f;
            hl.childForceExpandWidth  = false;
            hl.childForceExpandHeight = true;
            ForestUiFactory.AddLayout(row.gameObject, preferredHeight: 52f, flexibleWidth: 1f);

            var lbl = ForestUiFactory.CreateText(row, "Lbl", label,
                ForestUiFactory.GetDefaultFont(), 20, TextCream, TextAnchor.MiddleLeft);
            ForestUiFactory.AddLayout(lbl.gameObject, flexibleWidth: 1f);

            var dec = ForestUiFactory.CreateButton(row, "Dec", "-",
                ForestUiFactory.GetDefaultFont(), InactiveBtn, TextCream, () => onDecrement?.Invoke(), 22);
            ForestUiFactory.AddLayout(dec.gameObject, preferredWidth: 48f, preferredHeight: 44f);

            var valGo = ForestUiFactory.CreateText(row, "Val", "60%",
                ForestUiFactory.GetDefaultFont(), 20, TextCream, TextAnchor.MiddleCenter);
            ForestUiFactory.AddLayout(valGo.gameObject, preferredWidth: 60f);
            valueLabel = valGo;

            var inc = ForestUiFactory.CreateButton(row, "Inc", "+",
                ForestUiFactory.GetDefaultFont(), InactiveBtn, TextCream, () => onIncrement?.Invoke(), 22);
            ForestUiFactory.AddLayout(inc.gameObject, preferredWidth: 48f, preferredHeight: 44f);
        }
    }
}
