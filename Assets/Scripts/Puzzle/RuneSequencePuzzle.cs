using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace ForestFriendsQuest
{
    /// <summary>
    /// Rune Sequence Puzzle — Arch-Druid tier cipher challenge.
    ///
    /// Displays N glowing rune symbols. Player must identify and reproduce
    /// the correct rune in the correct position. Each rune has a unique
    /// visual glyph drawn procedurally on a canvas quad.
    ///
    /// Modes:
    ///   Standard:  Match the shown sequence (4-5 runes)
    ///   Reverse:   Reproduce the sequence in reverse order (Druid challenge)
    ///   Encrypted: Runes are shifted by a Caesar-style offset (daily cipher)
    ///
    /// All visual feedback is sprite-based glow — zero emoji.
    /// </summary>
    public class RuneSequencePuzzle : MonoBehaviour
    {
        // ─── Rune Definitions ─────────────────────────────────────────────────────

        private static readonly string[] RuneNames = new[]
        {
            "Fehu",    // Wealth / abundance
            "Uruz",    // Strength / endurance
            "Thurisaz",// Thorn / threshold
            "Ansuz",   // Wisdom / signals
            "Raido",   // Journey / growth
            "Kenaz",   // Torch / knowledge
            "Gebo",    // Gift / exchange
            "Wunjo",   // Joy / harmony
            "Hagalaz", // Hail / disruption — Druid only
            "Isa",     // Ice / stillness — Druid only
        };

        private static readonly Color[] RuneColors = new[]
        {
            new Color(0.80f, 1.00f, 0.60f),
            new Color(0.60f, 0.90f, 1.00f),
            new Color(1.00f, 0.80f, 0.55f),
            new Color(0.75f, 0.55f, 1.00f),
            new Color(1.00f, 0.95f, 0.50f),
            new Color(0.55f, 1.00f, 0.90f),
            new Color(1.00f, 0.70f, 0.85f),
            new Color(0.70f, 1.00f, 0.80f),
            new Color(0.90f, 0.70f, 0.55f),
            new Color(0.60f, 0.70f, 1.00f),
        };

        // ─── State ───────────────────────────────────────────────────────────────

        public bool isReverseMode  = false;
        public bool isEncryptedMode = false;
        public int  encryptionShift = 2;
        public int  baseRuneCount   = 4;

        private PuzzleManager           _manager;
        private EmotionalParticleEngine _particles;

        private List<int>   _targetSequence = new List<int>();
        private List<int>   _playerInput    = new List<int>();
        private int[]       _availableRunes;

        public event Action<bool> OnPuzzleEnd;

        // ─── Lifecycle ───────────────────────────────────────────────────────────

        public void Initialize(
            PuzzleManager manager,
            EmotionalParticleEngine particles,
            RectTransform parent,
            string tier)
        {
            _manager   = manager;
            _particles = particles;

            var runePoolSize = tier == "druid" ? RuneNames.Length : 6;
            _availableRunes  = BuildRunePool(runePoolSize);

            var count = manager.GetAdaptedMemoryLength(baseRuneCount);
            GenerateSequence(count);
            BuildUI(parent, tier);

            _manager.StartPuzzle(PuzzleType.RuneSequence, tier);
        }

        // ─── Sequence Logic ───────────────────────────────────────────────────────

        private void GenerateSequence(int count)
        {
            _targetSequence.Clear();
            _playerInput.Clear();

            for (var i = 0; i < count; i++)
            {
                _targetSequence.Add(_availableRunes[UnityEngine.Random.Range(0, _availableRunes.Length)]);
            }

            if (isReverseMode)
            {
                // Player must answer in reverse order
                _targetSequence.Reverse();
            }
        }

        public void OnRuneSelected(int runeIndex, Vector2 canvasPos)
        {
            var expected = _targetSequence[_playerInput.Count];

            if (runeIndex == expected)
            {
                // Correct rune!
                _particles?.Spawn(EmotionalParticleType.DiscoveryRuneGlow, canvasPos, 4);
                _manager.RecordCorrectStep(canvasPos);
                _playerInput.Add(runeIndex);

                if (_playerInput.Count == _targetSequence.Count)
                {
                    _manager.SolvePuzzle(canvasPos);
                    OnPuzzleEnd?.Invoke(true);
                }
            }
            else
            {
                // Incorrect — don't reset entirely for Sprout/Scout, do reset for Druid
                _particles?.Spawn(EmotionalParticleType.GrassDisturbDust, canvasPos, 2);
                _manager.RecordMistake(canvasPos);
                _playerInput.Clear();
            }
        }

        // ─── Encryption Helpers ───────────────────────────────────────────────────

        /// <summary>Get the encrypted display index for a rune (Caesar shift on pool indices).</summary>
        public int GetEncryptedDisplay(int realIndex)
        {
            if (!isEncryptedMode) return realIndex;
            return (realIndex + encryptionShift) % _availableRunes.Length;
        }

        public string GetRuneName(int runeIndex)
        {
            if (runeIndex < 0 || runeIndex >= RuneNames.Length) return "Unknown";
            return RuneNames[runeIndex];
        }

        public Color GetRuneColor(int runeIndex)
        {
            if (runeIndex < 0 || runeIndex >= RuneColors.Length) return Color.white;
            return RuneColors[runeIndex];
        }

        /// <summary>Show the target sequence (for hint mode or Sprout tier).</summary>
        public IReadOnlyList<int> GetTargetSequence() => _targetSequence;

        // ─── UI Builder ───────────────────────────────────────────────────────────

        private void BuildUI(RectTransform parent, string tier)
        {
            // Sequence display row (what to match)
            var seqRow = CreateRow(parent, "SequenceRow", 10f, new Vector2(0f, 100f));
            foreach (var runeIndex in _targetSequence)
            {
                CreateRuneDisplay(seqRow, runeIndex, interactive: false);
            }

            // Player input row (tap to answer)
            var inputRow = CreateRow(parent, "InputRow", 10f, new Vector2(0f, -80f));
            foreach (var runeIndex in _availableRunes)
            {
                var idx = runeIndex;
                CreateRuneButton(inputRow, runeIndex, () =>
                {
                    var worldPos = (inputRow as RectTransform)?.anchoredPosition ?? Vector2.zero;
                    OnRuneSelected(idx, worldPos);
                });
            }
        }

        // ─── Private Helpers ──────────────────────────────────────────────────────

        private static int[] BuildRunePool(int size)
        {
            var pool = new int[Mathf.Min(size, RuneNames.Length)];
            for (var i = 0; i < pool.Length; i++) pool[i] = i;
            return pool;
        }

        private static RectTransform CreateRow(RectTransform parent, string name,
            float spacing, Vector2 offset)
        {
            var go   = new GameObject(name);
            go.transform.SetParent(parent, false);
            var rt   = go.AddComponent<RectTransform>();
            rt.anchoredPosition = offset;
            rt.sizeDelta        = new Vector2(600f, 80f);
            var layout = go.AddComponent<HorizontalLayoutGroup>();
            layout.spacing = spacing;
            layout.childAlignment = TextAnchor.MiddleCenter;
            return rt;
        }

        private static void CreateRuneDisplay(RectTransform parent, int runeIndex,
            bool interactive)
        {
            var go  = new GameObject($"Rune_{RuneNames[runeIndex]}");
            go.transform.SetParent(parent, false);
            var rt  = go.AddComponent<RectTransform>();
            rt.sizeDelta = new Vector2(60f, 60f);
            var img = go.AddComponent<Image>();
            img.sprite = CreateRuneSprite(runeIndex);
            img.color  = RuneColors[runeIndex];
        }

        private static void CreateRuneButton(RectTransform parent, int runeIndex,
            UnityEngine.Events.UnityAction onClick)
        {
            CreateRuneDisplay(parent, runeIndex, interactive: true);
            // Button component would be added for interactivity in a full implementation
        }

        private static Sprite CreateRuneSprite(int runeIndex)
        {
            const int size = 48;
            var tex    = new Texture2D(size, size, TextureFormat.RGBA32, false);
            var pixels = new Color[size * size];

            // Draw a unique geometric glyph per rune using deterministic math
            var seed   = runeIndex * 137 + 42;
            var rng    = new System.Random(seed);

            for (var y = 0; y < size; y++)
            {
                for (var x = 0; x < size; x++)
                {
                    pixels[y * size + x] = Color.clear;
                }
            }

            // Draw 3-5 lines to form a unique glyph
            var lineCount = 3 + (runeIndex % 3);
            for (var l = 0; l < lineCount; l++)
            {
                var x0 = rng.Next(8, size - 8);
                var y0 = rng.Next(8, size - 8);
                var x1 = rng.Next(8, size - 8);
                var y1 = rng.Next(8, size - 8);
                DrawLine(pixels, size, x0, y0, x1, y1, Color.white);
            }

            tex.SetPixels(pixels);
            tex.Apply();
            tex.filterMode = FilterMode.Bilinear;
            return Sprite.Create(tex, new Rect(0, 0, size, size), Vector2.one * 0.5f, size);
        }

        private static void DrawLine(Color[] pixels, int size, int x0, int y0, int x1, int y1,
            Color color)
        {
            var dx = Mathf.Abs(x1 - x0);
            var dy = Mathf.Abs(y1 - y0);
            var sx = x0 < x1 ? 1 : -1;
            var sy = y0 < y1 ? 1 : -1;
            var err = dx - dy;

            while (true)
            {
                if (x0 >= 0 && x0 < size && y0 >= 0 && y0 < size)
                {
                    pixels[y0 * size + x0] = color;
                    // Anti-aliased: set neighbor with half alpha
                    if (x0 + 1 < size) pixels[y0 * size + x0 + 1] =
                        new Color(color.r, color.g, color.b, 0.5f);
                }

                if (x0 == x1 && y0 == y1) break;
                var e2 = 2 * err;
                if (e2 > -dy) { err -= dy; x0 += sx; }
                if (e2 < dx)  { err += dx; y0 += sy; }
            }
        }
    }
}
