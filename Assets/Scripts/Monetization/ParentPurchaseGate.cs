using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace ForestFriendsQuest
{
    /// <summary>
    /// ParentPurchaseGate — COPPA-compliant parental confirmation layer.
    ///
    /// Every purchase attempt must pass through this gate before the store is
    /// contacted. Implements two confirmation methods:
    ///
    ///   1. PIN gate — parent sets a 4-digit PIN in the parent dashboard.
    ///   2. Math gate — parent solves a simple arithmetic challenge (no PIN required,
    ///      keeps children from accidentally tapping through prompts).
    ///
    /// The gate also enforces a 30-second cooldown between failed attempts to
    /// prevent brute-force PIN entry by children.
    /// </summary>
    public class ParentPurchaseGate : MonoBehaviour
    {
        // ─── Config ──────────────────────────────────────────────────────────────

        private const string PinPrefsKey   = "FFQ.Parent.PIN";
        private const float  FailCooldown  = 30f;   // seconds between failed attempts

        // ─── State ───────────────────────────────────────────────────────────────

        private Action _onApproved;
        private Action _onDenied;

        private float  _failCooldownRemaining;
        private Canvas _gateCanvas;
        private Text   _promptText;
        private InputField _pinInput;
        private Text   _errorText;
        private int    _mathA, _mathB;

        // ─── Lifecycle ───────────────────────────────────────────────────────────

        public void Initialize(Canvas overlayCanvas)
        {
            _gateCanvas = overlayCanvas;
            BuildUI();
        }

        private void Update()
        {
            if (_failCooldownRemaining > 0f)
                _failCooldownRemaining -= Time.unscaledDeltaTime;
        }

        // ─── Public API ──────────────────────────────────────────────────────────

        /// <summary>Show the parental gate. Calls onApproved or onDenied when resolved.</summary>
        public void RequestAuthorization(Action onApproved, Action onDenied)
        {
            if (_failCooldownRemaining > 0f)
            {
                var secs = Mathf.CeilToInt(_failCooldownRemaining);
                Debug.Log($"[ParentPurchaseGate] Cooldown: {secs}s remaining.");
                onDenied?.Invoke();
                return;
            }

            _onApproved = onApproved;
            _onDenied   = onDenied;

            ShowGate();
        }

        /// <summary>Set or update the parental PIN (called from parent dashboard).</summary>
        public void SetPin(string pin)
        {
            if (pin?.Length != 4 || !int.TryParse(pin, out _)) return;
            PlayerPrefs.SetString(PinPrefsKey, pin);
            PlayerPrefs.Save();
        }

        public bool HasPin() => !string.IsNullOrEmpty(PlayerPrefs.GetString(PinPrefsKey, null));

        // ─── Gate UI ─────────────────────────────────────────────────────────────

        private void ShowGate()
        {
            if (_gateCanvas != null) _gateCanvas.gameObject.SetActive(true);
            if (_errorText  != null) _errorText.text = string.Empty;
            if (_pinInput   != null) _pinInput.text  = string.Empty;

            // Use math gate if no PIN is set
            if (!HasPin())
                ShowMathGate();
            else
                ShowPinGate();
        }

        private void ShowPinGate()
        {
            if (_promptText != null)
                _promptText.text = "Parent check: enter your 4-digit PIN to continue.";
        }

        private void ShowMathGate()
        {
            _mathA = UnityEngine.Random.Range(10, 30);
            _mathB = UnityEngine.Random.Range(10, 30);
            if (_promptText != null)
                _promptText.text = $"Parent check: what is {_mathA} + {_mathB}?";
        }

        private void OnConfirmPressed()
        {
            var input = _pinInput?.text ?? string.Empty;

            bool approved;
            if (HasPin())
                approved = input == PlayerPrefs.GetString(PinPrefsKey);
            else
                approved = int.TryParse(input, out int answer) && answer == _mathA + _mathB;

            if (approved)
            {
                HideGate();
                _failCooldownRemaining = 0f;
                _onApproved?.Invoke();
            }
            else
            {
                _failCooldownRemaining = FailCooldown;
                if (_errorText != null)
                    _errorText.text = "Incorrect — please try again in 30 seconds.";
            }
        }

        private void OnCancelPressed()
        {
            HideGate();
            _onDenied?.Invoke();
        }

        private void HideGate()
        {
            if (_gateCanvas != null) _gateCanvas.gameObject.SetActive(false);
        }

        // ─── UI Construction ─────────────────────────────────────────────────────

        private void BuildUI()
        {
            if (_gateCanvas == null) return;

            var font = ForestUiFactory.GetDefaultFont();
            var bg   = ForestUiFactory.CreateImage(_gateCanvas.transform, "GateBG",
                           new Color(0f, 0f, 0f, 0.82f));
            ForestUiFactory.Stretch(bg.rectTransform);

            var panel = ForestUiFactory.CreateUiObject("GatePanel", _gateCanvas.transform);
            panel.anchorMin        = new Vector2(0.1f, 0.3f);
            panel.anchorMax        = new Vector2(0.9f, 0.7f);
            panel.offsetMin        = Vector2.zero;
            panel.offsetMax        = Vector2.zero;

            var panelBg = panel.gameObject.AddComponent<Image>();
            panelBg.color = new Color(0.12f, 0.22f, 0.16f, 0.97f);

            _promptText = ForestUiFactory.CreateText(panel, "Prompt", "Parent check...",
                font, 22, Color.white, TextAnchor.UpperCenter);
            var pt = _promptText.rectTransform;
            pt.anchorMin        = new Vector2(0.05f, 0.65f);
            pt.anchorMax        = new Vector2(0.95f, 0.95f);
            pt.offsetMin        = Vector2.zero;
            pt.offsetMax        = Vector2.zero;

            var inputGo = new GameObject("PinInput");
            inputGo.transform.SetParent(panel, false);
            var inputRt = inputGo.AddComponent<RectTransform>();
            inputRt.anchorMin = new Vector2(0.15f, 0.45f);
            inputRt.anchorMax = new Vector2(0.85f, 0.62f);
            inputRt.offsetMin = Vector2.zero;
            inputRt.offsetMax = Vector2.zero;
            var inputBg = inputGo.AddComponent<Image>();
            inputBg.color = Color.white;

            _pinInput               = inputGo.AddComponent<InputField>();
            _pinInput.contentType   = InputField.ContentType.Pin;
            _pinInput.characterLimit = 4;
            var inputTextGo = new GameObject("Text");
            inputTextGo.transform.SetParent(inputGo.transform, false);
            var inputText          = inputTextGo.AddComponent<Text>();
            inputText.font         = font;
            inputText.fontSize     = 28;
            inputText.color        = Color.black;
            inputText.alignment    = TextAnchor.MiddleCenter;
            _pinInput.textComponent = inputText;

            _errorText = ForestUiFactory.CreateText(panel, "Error", string.Empty,
                font, 16, new Color(1f, 0.4f, 0.4f), TextAnchor.MiddleCenter);
            var et = _errorText.rectTransform;
            et.anchorMin = new Vector2(0.05f, 0.3f);
            et.anchorMax = new Vector2(0.95f, 0.44f);
            et.offsetMin = Vector2.zero;
            et.offsetMax = Vector2.zero;

            ForestUiFactory.CreateButton(panel, "ConfirmBtn", "Confirm", font,
                new Color(0.2f, 0.7f, 0.4f), Color.white, OnConfirmPressed);
            var cb = panel.Find("ConfirmBtn").GetComponent<RectTransform>();
            cb.anchorMin = new Vector2(0.08f, 0.05f);
            cb.anchorMax = new Vector2(0.48f, 0.27f);
            cb.offsetMin = Vector2.zero;
            cb.offsetMax = Vector2.zero;

            ForestUiFactory.CreateButton(panel, "CancelBtn", "Cancel", font,
                new Color(0.7f, 0.25f, 0.25f), Color.white, OnCancelPressed);
            var ccb = panel.Find("CancelBtn").GetComponent<RectTransform>();
            ccb.anchorMin = new Vector2(0.52f, 0.05f);
            ccb.anchorMax = new Vector2(0.92f, 0.27f);
            ccb.offsetMin = Vector2.zero;
            ccb.offsetMax = Vector2.zero;

            _gateCanvas.gameObject.SetActive(false);
        }
    }
}
