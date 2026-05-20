using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace ForestFriendsQuest
{
    /// <summary>
    /// Time Memory Challenge — a memory sequence puzzle with a twist:
    ///
    ///   Sprout (4-6):  Show 2-3 symbols, recall in FORWARD order. Timer: none.
    ///   Scout (7-11):  Show 4-5 symbols with timestamps, recall in FORWARD order.
    ///                  A short blank delay (1.5s) between encode and recall.
    ///   Druid (12-16): Show 5-7 symbols with timestamps, recall in REVERSE order.
    ///                  Longer blank delay (2.5s). Every symbol tagged with a
    ///                  "time stone" number so child can reason about reversal.
    ///
    /// Phases:
    ///   Encoding  — nodes flash one by one with time-stamp labels
    ///   Holding   — all nodes go dark (blank delay phase)
    ///   Recall    — player taps nodes in correct order
    ///   Solved / Failed
    ///
    /// Visual feedback:
    ///   - Correct tap: DiscoveryRuneGlow particles + node flash green
    ///   - Wrong tap:   GrassDisturbDust + node flash red (input resets, try again)
    ///   - Solved:      SpawnJoyBurst + SolvePuzzle
    ///
    /// Differences from MemoryTrailPuzzle:
    ///   - Adds Holding phase (blank delay) to stress working memory
    ///   - Druid recall order is reversed (reverse-time ritual theme)
    ///   - Timestamp labels on nodes during encoding
    ///   - Phase label UI shows "Remember...", "Wait...", "Now!"
    /// </summary>
    public class TimeMemoryChallenge : MonoBehaviour
    {
        // ─── Config ──────────────────────────────────────────────────────────────

        [Header("Timing")]
        public float encodeShowDuration = 0.55f;
        public float encodeGap          = 0.18f;
        public float holdDuration       = 1.5f;   // blank phase; overridden per tier

        [Header("Node Visuals")]
        public float nodeSize           = 88f;
        public Color nodeIdleColor      = new Color(0.28f, 0.50f, 0.38f, 0.55f);
        public Color nodeActiveColor    = new Color(0.85f, 1.00f, 0.55f, 1.00f);
        public Color nodeCorrectColor   = new Color(0.45f, 1.00f, 0.75f, 0.90f);
        public Color nodeErrorColor     = new Color(1.00f, 0.50f, 0.42f, 0.85f);
        public Color nodeStampColor     = new Color(1.00f, 0.95f, 0.60f, 1.00f);   // timestamp highlight

        // ─── Phase ───────────────────────────────────────────────────────────────

        private enum Phase { Idle, Encoding, Holding, Recall, Solved, Failed }

        private Phase _phase = Phase.Idle;

        // ─── Sequence State ───────────────────────────────────────────────────────

        private List<int>  _sequence       = new List<int>();
        private List<int>  _recallTarget   = new List<int>();   // may be reversed
        private List<int>  _playerInput    = new List<int>();
        private bool       _reverseRecall  = false;

        private int   _encodeIndex;
        private float _encodeTimer;
        private bool  _nodeShowing;
        private float _holdTimer;

        // ─── UI ──────────────────────────────────────────────────────────────────

        private RectTransform[] _nodeRects;
        private Image[]          _nodeImages;
        private Text[]           _nodeStampLabels;  // shows "1", "2", … during encoding
        private Text             _phaseLabel;        // "Remember...", "Wait...", "Now!"

        // ─── Systems ─────────────────────────────────────────────────────────────

        private PuzzleManager           _manager;
        private EmotionalParticleEngine _particles;

        public event Action<bool> OnPuzzleEnd;

        // ─── Lifecycle ───────────────────────────────────────────────────────────

        public void Initialize(
            PuzzleManager manager,
            EmotionalParticleEngine particles,
            RectTransform parent,
            int nodeCount,
            string tier)
        {
            _manager   = manager;
            _particles = particles;

            _reverseRecall = (tier == "druid");
            holdDuration   = tier == "druid" ? 2.5f : (tier == "scout" ? 1.5f : 0f);

            var length = manager.GetAdaptedMemoryLength(3);

            BuildNodes(parent, nodeCount);
            BuildPhaseLabel(parent);
            GenerateSequence(length);

            _manager.StartPuzzle(PuzzleType.TimeMemory, tier);
        }

        private void Update()
        {
            switch (_phase)
            {
                case Phase.Encoding: UpdateEncoding(); break;
                case Phase.Holding:  UpdateHolding();  break;
            }
        }

        // ─── Public API ───────────────────────────────────────────────────────────

        /// <summary>Begin the encoding phase.</summary>
        public void StartChallenge()
        {
            _phase        = Phase.Encoding;
            _encodeIndex  = 0;
            _encodeTimer  = 0f;
            _nodeShowing  = false;
            ResetNodeColors();
            SetPhaseLabel("Remember...");
        }

        /// <summary>Called when player taps a node (0-indexed).</summary>
        public void OnNodeTapped(int nodeIndex)
        {
            if (_phase != Phase.Recall) return;
            if (nodeIndex < 0 || nodeIndex >= _nodeRects.Length) return;

            var expected = _recallTarget[_playerInput.Count];

            if (nodeIndex == expected)
            {
                FlashNode(nodeIndex, nodeCorrectColor);
                _particles?.Spawn(EmotionalParticleType.DiscoveryRuneGlow,
                    _nodeRects[nodeIndex].anchoredPosition, 3);
                _manager.RecordCorrectStep(_nodeRects[nodeIndex].anchoredPosition);
                _playerInput.Add(nodeIndex);

                if (_playerInput.Count == _recallTarget.Count)
                {
                    _phase = Phase.Solved;
                    SetPhaseLabel("Forest magic unlocked!");
                    _manager.SolvePuzzle(_nodeRects[nodeIndex].anchoredPosition);
                    OnPuzzleEnd?.Invoke(true);
                }
            }
            else
            {
                FlashNode(nodeIndex, nodeErrorColor);
                _particles?.Spawn(EmotionalParticleType.GrassDisturbDust,
                    _nodeRects[nodeIndex].anchoredPosition, 2);
                _manager.RecordMistake(_nodeRects[nodeIndex].anchoredPosition);

                // Soft reset: clear input, let player try again from the start
                _playerInput.Clear();
                HideAllStampLabels();
            }
        }

        // ─── Encoding Phase ───────────────────────────────────────────────────────

        private void UpdateEncoding()
        {
            _encodeTimer += Time.deltaTime;
            var nodeShowTime = encodeShowDuration + encodeGap;

            if (_encodeIndex >= _sequence.Count)
            {
                // All encoded — enter hold
                HideAllNodes();
                HideAllStampLabels();
                _phase = Phase.Holding;
                _holdTimer = 0f;
                SetPhaseLabel("Wait...");
                return;
            }

            if (!_nodeShowing && _encodeTimer >= encodeGap)
            {
                var idx = _sequence[_encodeIndex];
                SetNodeColor(idx, nodeActiveColor);
                ShowStampLabel(idx, _encodeIndex + 1);   // "1", "2", …
                _particles?.Spawn(EmotionalParticleType.HappyGoldenWisp,
                    _nodeRects[idx].anchoredPosition, 2);
                _nodeShowing = true;
            }

            if (_nodeShowing && _encodeTimer >= nodeShowTime)
            {
                var idx = _sequence[_encodeIndex];
                SetNodeColor(idx, nodeIdleColor);
                _encodeIndex++;
                _encodeTimer = 0f;
                _nodeShowing = false;
            }
        }

        // ─── Holding Phase ────────────────────────────────────────────────────────

        private void UpdateHolding()
        {
            _holdTimer += Time.deltaTime;
            if (_holdTimer >= holdDuration)
            {
                _phase = Phase.Recall;
                ShowRecallNodes();
                SetPhaseLabel(_reverseRecall ? "Now! (backwards)" : "Now!");
            }
        }

        // ─── Recall Start ─────────────────────────────────────────────────────────

        private void ShowRecallNodes()
        {
            // Restore idle-visible nodes for tapping
            for (var i = 0; i < _nodeImages.Length; i++)
                SetNodeColor(i, nodeIdleColor);

            // Show reversed stamp hint if Druid (numbers remain as memory aid)
            if (_reverseRecall && _manager.ShouldShowGuides())
            {
                for (var i = 0; i < _recallTarget.Count; i++)
                {
                    var idx = _recallTarget[i];
                    ShowStampLabel(idx, i + 1);
                }
            }
        }

        // ─── Sequence Generation ──────────────────────────────────────────────────

        private void GenerateSequence(int length)
        {
            _sequence.Clear();
            _recallTarget.Clear();
            _playerInput.Clear();

            for (var i = 0; i < length; i++)
                _sequence.Add(UnityEngine.Random.Range(0, _nodeRects.Length));

            // Recall order: forward or reversed depending on tier
            _recallTarget.AddRange(_sequence);
            if (_reverseRecall)
                _recallTarget.Reverse();
        }

        // ─── Node Builder ─────────────────────────────────────────────────────────

        private void BuildNodes(RectTransform parent, int nodeCount)
        {
            _nodeRects       = new RectTransform[nodeCount];
            _nodeImages      = new Image[nodeCount];
            _nodeStampLabels = new Text[nodeCount];

            var angleStep = 360f / nodeCount;
            var radius    = nodeCount <= 4 ? 130f : 190f;

            for (var i = 0; i < nodeCount; i++)
            {
                var angle = (i * angleStep - 90f) * Mathf.Deg2Rad;
                var pos   = new Vector2(Mathf.Cos(angle) * radius, Mathf.Sin(angle) * radius);

                var go = new GameObject($"TMNode_{i}");
                go.transform.SetParent(parent, false);

                var rt = go.AddComponent<RectTransform>();
                rt.anchoredPosition = pos;
                rt.sizeDelta        = new Vector2(nodeSize, nodeSize);

                var img = go.AddComponent<Image>();
                img.sprite = CreateNodeSprite();
                img.color  = nodeIdleColor;

                var btn = go.AddComponent<Button>();
                var idx = i;
                btn.onClick.AddListener(() => OnNodeTapped(idx));

                _nodeRects[i]  = rt;
                _nodeImages[i] = img;

                // Stamp label (shows "1", "2", etc. during encoding)
                var labelGo = new GameObject($"TMStamp_{i}");
                labelGo.transform.SetParent(go.transform, false);
                var labelRt = labelGo.AddComponent<RectTransform>();
                labelRt.anchorMin = Vector2.zero;
                labelRt.anchorMax = Vector2.one;
                labelRt.offsetMin = labelRt.offsetMax = Vector2.zero;

                var lbl = labelGo.AddComponent<Text>();
                lbl.text      = "";
                lbl.fontSize  = 26;
                lbl.fontStyle = FontStyle.Bold;
                lbl.alignment = TextAnchor.MiddleCenter;
                lbl.color     = nodeStampColor;
                _nodeStampLabels[i] = lbl;
            }
        }

        private void BuildPhaseLabel(RectTransform parent)
        {
            var go = new GameObject("TMPhaseLabel");
            go.transform.SetParent(parent, false);

            var rt = go.AddComponent<RectTransform>();
            rt.anchoredPosition = new Vector2(0, 240f);
            rt.sizeDelta        = new Vector2(400f, 60f);

            var lbl = go.AddComponent<Text>();
            lbl.text      = "";
            lbl.fontSize  = 28;
            lbl.fontStyle = FontStyle.Bold;
            lbl.alignment = TextAnchor.MiddleCenter;
            lbl.color     = new Color(0.85f, 1.00f, 0.70f, 1.00f);
            _phaseLabel = lbl;
        }

        // ─── Node Color / Stamp Helpers ───────────────────────────────────────────

        private void SetNodeColor(int index, Color color)
        {
            if (_nodeImages != null && _nodeImages[index] != null)
                _nodeImages[index].color = color;
        }

        private void ResetNodeColors()
        {
            for (var i = 0; i < _nodeImages.Length; i++)
                SetNodeColor(i, nodeIdleColor);
        }

        private void HideAllNodes()
        {
            if (_manager.ShouldShowGuides()) return;
            for (var i = 0; i < _nodeImages.Length; i++)
                SetNodeColor(i, new Color(nodeIdleColor.r, nodeIdleColor.g, nodeIdleColor.b, 0.20f));
        }

        private void FlashNode(int index, Color color)
        {
            SetNodeColor(index, color);
        }

        private void ShowStampLabel(int nodeIndex, int stamp)
        {
            if (_nodeStampLabels == null || _nodeStampLabels[nodeIndex] == null) return;
            _nodeStampLabels[nodeIndex].text = stamp.ToString();
        }

        private void HideAllStampLabels()
        {
            if (_nodeStampLabels == null) return;
            foreach (var lbl in _nodeStampLabels)
                if (lbl != null) lbl.text = "";
        }

        private void SetPhaseLabel(string text)
        {
            if (_phaseLabel != null) _phaseLabel.text = text;
        }

        // ─── Sprite Factory ───────────────────────────────────────────────────────

        private static Sprite CreateNodeSprite()
        {
            const int size = 64;
            var tex    = new Texture2D(size, size, TextureFormat.RGBA32, false);
            var center = new Vector2(size / 2f, size / 2f);
            var pixels = new Color[size * size];

            for (var y = 0; y < size; y++)
            {
                for (var x = 0; x < size; x++)
                {
                    var dist  = Vector2.Distance(new Vector2(x + 0.5f, y + 0.5f), center);
                    var alpha = 1f - Mathf.Clamp01((dist - size * 0.38f) / (size * 0.10f));
                    pixels[y * size + x] = new Color(1f, 1f, 1f, alpha);
                }
            }

            tex.SetPixels(pixels);
            tex.Apply();
            return Sprite.Create(tex, new Rect(0, 0, size, size), Vector2.one * 0.5f, size);
        }
    }
}
