using UnityEngine;
using UnityEngine.UI;
using System.Collections;

namespace ForestFriendsQuest
{
    /// <summary>
    /// Horizontal skill bar with animated fill, label, and percentage text.
    /// Used by ParentDashboardController for spatial/pattern/logic scores.
    /// </summary>
    public class SkillGrowthGraphRenderer : MonoBehaviour
    {
        private Text        _labelText;
        private Text        _percentText;
        private RectTransform _fillRect;
        private Image       _fillImage;
        private float       _targetValue;
        private bool        _initialized;

        public void Initialize(string label, Color barColor, Font font)
        {
            if (_initialized) return;
            _initialized = true;

            var rt = GetComponent<RectTransform>();
            if (rt == null) rt = gameObject.AddComponent<RectTransform>();

            // Track background
            var trackGo  = new GameObject("Track");
            trackGo.transform.SetParent(transform, false);
            var trackRt  = trackGo.AddComponent<RectTransform>();
            trackRt.anchorMin = new Vector2(0f, 0.15f);
            trackRt.anchorMax = new Vector2(1f, 0.85f);
            trackRt.sizeDelta = Vector2.zero;
            var trackImg = trackGo.AddComponent<Image>();
            trackImg.color = new Color(0.1f, 0.18f, 0.14f, 0.9f);

            // Fill bar
            var fillGo  = new GameObject("Fill");
            fillGo.transform.SetParent(trackGo.transform, false);
            _fillRect   = fillGo.AddComponent<RectTransform>();
            _fillRect.anchorMin = Vector2.zero;
            _fillRect.anchorMax = new Vector2(0f, 1f);
            _fillRect.sizeDelta = Vector2.zero;
            _fillRect.pivot     = new Vector2(0f, 0.5f);
            _fillImage  = fillGo.AddComponent<Image>();
            _fillImage.color = barColor;

            // Label (left side)
            var labelGo = new GameObject("Label");
            labelGo.transform.SetParent(transform, false);
            var labelRt = labelGo.AddComponent<RectTransform>();
            labelRt.anchorMin = new Vector2(0f, 0f);
            labelRt.anchorMax = new Vector2(0.55f, 0.25f);
            labelRt.sizeDelta = Vector2.zero;
            _labelText = labelGo.AddComponent<Text>();
            _labelText.font      = font;
            _labelText.fontSize  = 16;
            _labelText.color     = new Color32(180, 210, 180, 255);
            _labelText.alignment = TextAnchor.MiddleLeft;
            _labelText.text      = label;

            // Percent (right side)
            var pctGo = new GameObject("Percent");
            pctGo.transform.SetParent(transform, false);
            var pctRt = pctGo.AddComponent<RectTransform>();
            pctRt.anchorMin = new Vector2(0.55f, 0f);
            pctRt.anchorMax = new Vector2(1f, 0.25f);
            pctRt.sizeDelta = Vector2.zero;
            _percentText = pctGo.AddComponent<Text>();
            _percentText.font      = font;
            _percentText.fontSize  = 16;
            _percentText.color     = barColor;
            _percentText.alignment = TextAnchor.MiddleRight;
            _percentText.text      = "—";
        }

        /// <summary>Set the fill value (0-1) and animate to it.</summary>
        public void SetValue(float normalizedValue)
        {
            _targetValue = Mathf.Clamp01(normalizedValue);
            if (gameObject.activeInHierarchy)
                StartCoroutine(AnimateFill(_targetValue));
            else
                ApplyFillImmediate(_targetValue);
        }

        private IEnumerator AnimateFill(float target)
        {
            var startX   = _fillRect != null ? _fillRect.anchorMax.x : 0f;
            var elapsed  = 0f;
            var duration = 0.6f;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                var t  = 1f - Mathf.Pow(1f - Mathf.Clamp01(elapsed / duration), 3f);
                var cx = Mathf.Lerp(startX, target, t);
                ApplyFillImmediate(cx);
                yield return null;
            }

            ApplyFillImmediate(target);
        }

        private void ApplyFillImmediate(float value)
        {
            if (_fillRect != null)
                _fillRect.anchorMax = new Vector2(value, 1f);

            if (_percentText != null)
                _percentText.text = value > 0f ? $"{Mathf.RoundToInt(value * 100)}%" : "—";
        }
    }
}
