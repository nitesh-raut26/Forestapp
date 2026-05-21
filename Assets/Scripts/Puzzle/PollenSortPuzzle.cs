using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace ForestFriendsQuest
{
    /// <summary>
    /// Pollen Sort Puzzle — sort colored pollen orbs into matching flower baskets.
    ///
    /// How it works:
    ///   • Flower baskets (each a unique forest color) line the top of the panel.
    ///   • Pollen orbs (colored circles) are scattered in a pool below.
    ///   • Tap a pollen orb to select it (turns gold), then tap the basket of the same color.
    ///   • Wrong basket → GrassDisturbDust + mistake; orb stays selected for retry.
    ///   • Tap a different orb while one is selected → switches selection.
    ///   • All pollen correctly sorted → SOLVED.
    ///
    /// Sprout  (4-6):  3 colors, 2 pollen each = 6 total
    /// Scout  (7-11):  4 colors, 2 pollen each = 8 total
    /// Druid (12-16):  5 colors, 2 pollen each = 10 total
    ///
    /// Visual feedback:
    ///   Selected     → gold tint + HappyGoldenWisp particles
    ///   Correct sort → DiscoveryRuneGlow; basket brightens as it fills
    ///   Wrong basket → GrassDisturbDust + RecordMistake
    ///   All sorted   → JoyBurst + SolvePuzzle
    /// </summary>
    public class PollenSortPuzzle : MonoBehaviour
    {
        // ─── Data ─────────────────────────────────────────────────────────────────

        private struct PollenOrb
        {
            public int    colorIndex;
            public bool   sorted;
            public Image  image;
        }

        private struct FlowerBasket
        {
            public int    colorIndex;
            public int    filled;
            public int    capacity;
            public Image  image;
        }

        // ─── State ───────────────────────────────────────────────────────────────

        private PollenOrb[]   _orbs;
        private FlowerBasket[] _baskets;
        private RectTransform[] _orbRects;
        private RectTransform[] _basketRects;

        private int _selectedOrb = -1;
        private int _sortedCount;
        private int _totalOrbs;
        private int _colorCount;

        // ─── Palette ─────────────────────────────────────────────────────────────
        // Five forest-themed hues used for pollen and baskets

        private static readonly Color[] Palette = {
            new Color(0.38f, 0.85f, 0.42f, 1f),   // 0 Leaf Green
            new Color(0.35f, 0.70f, 1.00f, 1f),   // 1 Sky Blue
            new Color(1.00f, 0.88f, 0.28f, 1f),   // 2 Sun Yellow
            new Color(1.00f, 0.38f, 0.42f, 1f),   // 3 Berry Red
            new Color(0.82f, 0.55f, 1.00f, 1f),   // 4 Lavender
        };

        private static readonly string[] PaletteNames = { "Leaf", "Sky", "Sun", "Berry", "Luna" };

        private static readonly Color ColorSelected   = new Color(1.00f, 1.00f, 0.55f, 1f);
        private static readonly Color ColorSorted     = new Color(0.60f, 0.65f, 0.60f, 0.45f);
        private static readonly Color ColorBasketDark = new Color(0.20f, 0.15f, 0.08f, 0.90f);

        // ─── Systems ─────────────────────────────────────────────────────────────

        private PuzzleManager           _manager;
        private EmotionalParticleEngine _particles;

        public event Action<bool> OnPuzzleEnd;

        // ─── Lifecycle ───────────────────────────────────────────────────────────

        public void Initialize(
            PuzzleManager           manager,
            EmotionalParticleEngine particles,
            RectTransform           parent,
            string                  tier)
        {
            _manager   = manager;
            _particles = particles;

            _colorCount = tier == "druid" ? 5 : tier == "scout" ? 4 : 3;
            const int orbsPerColor = 2;
            _totalOrbs = _colorCount * orbsPerColor;

            _orbs        = new PollenOrb[_totalOrbs];
            _baskets     = new FlowerBasket[_colorCount];
            _orbRects    = new RectTransform[_totalOrbs];
            _basketRects = new RectTransform[_colorCount];

            BuildUI(parent, orbsPerColor);
            _manager.StartPuzzle(PuzzleType.PollenSort, tier);
        }

        // ─── Input ───────────────────────────────────────────────────────────────

        private void HandleOrbTapped(int idx)
        {
            if (_orbs[idx].sorted) return;

            var pos = _orbRects[idx] != null ? _orbRects[idx].anchoredPosition : Vector2.zero;

            if (_selectedOrb == idx)
            {
                // Deselect same orb
                _selectedOrb = -1;
                RefreshOrbColors();
                return;
            }

            _selectedOrb = idx;
            _particles?.Spawn(EmotionalParticleType.HappyGoldenWisp, pos, 2);
            RefreshOrbColors();
        }

        private void HandleBasketTapped(int bIdx)
        {
            if (_selectedOrb < 0) return;

            var bPos = _basketRects[bIdx] != null ? _basketRects[bIdx].anchoredPosition : Vector2.zero;

            if (_orbs[_selectedOrb].colorIndex == _baskets[bIdx].colorIndex)
            {
                // ── Correct ───────────────────────────────────────────────────────
                _orbs[_selectedOrb].sorted = true;
                _baskets[bIdx].filled++;
                _sortedCount++;
                _manager.RecordCorrectStep(bPos);
                _particles?.Spawn(EmotionalParticleType.DiscoveryRuneGlow, bPos, 4);

                _selectedOrb = -1;
                RefreshOrbColors();
                RefreshBasketColors();

                if (_sortedCount >= _totalOrbs)
                {
                    _particles?.SpawnJoyBurst(bPos);
                    _manager.SolvePuzzle(bPos);
                    OnPuzzleEnd?.Invoke(true);
                }
            }
            else
            {
                // ── Wrong basket ──────────────────────────────────────────────────
                _particles?.Spawn(EmotionalParticleType.GrassDisturbDust, bPos, 2);
                _manager.RecordMistake(bPos);
                // Orb stays selected — player retries
            }
        }

        // ─── UI Builder ───────────────────────────────────────────────────────────

        private void BuildUI(RectTransform parent, int orbsPerColor)
        {
            var rng = new System.Random(13);

            // ── Baskets ───────────────────────────────────────────────────────────
            const float BW    = 86f;
            const float BGap  = 12f;
            float bRowW = _colorCount * BW + (_colorCount - 1) * BGap;

            for (int i = 0; i < _colorCount; i++)
            {
                float bx = -bRowW / 2f + i * (BW + BGap) + BW / 2f;

                var rt  = MakeRect($"Basket_{i}", parent, new Vector2(bx, 140f), new Vector2(BW, BW));
                var img = rt.gameObject.AddComponent<Image>();
                img.sprite = MakeBasketSprite();
                img.color  = ColorBasketDark;

                // Color ring overlay inside basket
                var ring = MakeRect("Ring", rt, Vector2.zero, Vector2.zero);
                ring.anchorMin = new Vector2(0.12f, 0.12f);
                ring.anchorMax = new Vector2(0.88f, 0.88f);
                ring.sizeDelta = Vector2.zero;
                var rImg = ring.gameObject.AddComponent<Image>();
                rImg.sprite = MakeCircleSprite(false);
                rImg.color  = Palette[i] * new Color(1, 1, 1, 0.45f);

                var btn = rt.gameObject.AddComponent<Button>();
                int bi  = i;
                btn.onClick.AddListener(() => HandleBasketTapped(bi));

                AddCenteredLabel(rt, PaletteNames[i], 13, new Vector2(0f, -BW * 0.62f));

                _baskets[i] = new FlowerBasket
                {
                    colorIndex = i,
                    filled     = 0,
                    capacity   = orbsPerColor,
                    image      = img,
                };
                _basketRects[i] = rt;
            }

            // ── Pollen orbs (shuffled grid below) ─────────────────────────────────
            const float OW   = 62f;
            const float OGap = 10f;
            int orbCols = Mathf.Max(3, Mathf.CeilToInt(Mathf.Sqrt(_totalOrbs)));
            float orbRowW = orbCols * OW + (orbCols - 1) * OGap;

            // Build shuffled color list
            var colorList = new List<int>();
            for (int c = 0; c < _colorCount; c++)
                for (int p = 0; p < orbsPerColor; p++)
                    colorList.Add(c);
            FisherYates(colorList, rng);

            for (int i = 0; i < _totalOrbs; i++)
            {
                int col = i % orbCols;
                int row = i / orbCols;
                float ox = -orbRowW / 2f + col * (OW + OGap) + OW / 2f;
                float oy = -16f - row * (OW + OGap);

                var rt  = MakeRect($"Orb_{i}", parent, new Vector2(ox, oy), new Vector2(OW, OW));
                var img = rt.gameObject.AddComponent<Image>();
                img.sprite = MakeCircleSprite(true);
                img.color  = Palette[colorList[i]];

                var btn = rt.gameObject.AddComponent<Button>();
                int oi  = i;
                btn.onClick.AddListener(() => HandleOrbTapped(oi));

                _orbs[i] = new PollenOrb { colorIndex = colorList[i], sorted = false, image = img };
                _orbRects[i] = rt;
            }
        }

        // ─── Visual Refresh ───────────────────────────────────────────────────────

        private void RefreshOrbColors()
        {
            for (int i = 0; i < _orbs.Length; i++)
            {
                if (_orbs[i].image == null) continue;
                _orbs[i].image.color = _orbs[i].sorted
                    ? ColorSorted
                    : i == _selectedOrb
                        ? ColorSelected
                        : Palette[_orbs[i].colorIndex];
            }
        }

        private void RefreshBasketColors()
        {
            for (int i = 0; i < _baskets.Length; i++)
            {
                if (_baskets[i].image == null) continue;
                float t = (float)_baskets[i].filled / Mathf.Max(1, _baskets[i].capacity);
                _baskets[i].image.color = Color.Lerp(ColorBasketDark, Palette[i], t * 0.85f);
            }
        }

        // ─── Helpers ─────────────────────────────────────────────────────────────

        private static RectTransform MakeRect(string name, RectTransform parent,
            Vector2 pos, Vector2 size)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            var rt = go.AddComponent<RectTransform>();
            rt.anchoredPosition = pos;
            rt.sizeDelta        = size;
            return rt;
        }

        private static void FisherYates(List<int> list, System.Random rng)
        {
            for (int k = list.Count - 1; k > 0; k--)
            {
                int j = rng.Next(0, k + 1);
                (list[k], list[j]) = (list[j], list[k]);
            }
        }

        private void AddCenteredLabel(RectTransform parent, string text, int size, Vector2 offset)
        {
            var rt  = MakeRect("Label", parent, offset, new Vector2(parent.sizeDelta.x, 22f));
            var txt = rt.gameObject.AddComponent<Text>();
            txt.text      = text;
            txt.font      = ForestUiFactory.GetDefaultFont();
            txt.fontSize  = size;
            txt.fontStyle = FontStyle.Bold;
            txt.color     = Color.white;
            txt.alignment = TextAnchor.MiddleCenter;
        }

        // ─── Sprite Factories ─────────────────────────────────────────────────────

        /// <summary>Solid filled circle (filled=true) or thin ring (filled=false).</summary>
        private static Sprite MakeCircleSprite(bool filled)
        {
            const int sz = 64;
            float cx = sz * 0.5f - 0.5f, cy = sz * 0.5f - 0.5f;
            float r  = sz * 0.5f - 2f;
            var tex    = new Texture2D(sz, sz, TextureFormat.RGBA32, false);
            var pixels = new Color[sz * sz];

            for (int y = 0; y < sz; y++)
                for (int x = 0; x < sz; x++)
                {
                    float d = Mathf.Sqrt((x - cx) * (x - cx) + (y - cy) * (y - cy));
                    float a = filled
                        ? Mathf.Clamp01(r - d + 1f)
                        : Mathf.Clamp01(1f - Mathf.Abs(d - (r - 3f)));
                    pixels[y * sz + x] = new Color(1, 1, 1, a);
                }

            tex.SetPixels(pixels);
            tex.Apply();
            return Sprite.Create(tex, new Rect(0, 0, sz, sz), Vector2.one * 0.5f, sz);
        }

        /// <summary>Simple U-shaped basket silhouette.</summary>
        private static Sprite MakeBasketSprite()
        {
            const int sz    = 64;
            const int thick = 5;
            var tex    = new Texture2D(sz, sz, TextureFormat.RGBA32, false);
            var pixels = new Color[sz * sz];

            for (int y = 0; y < sz; y++)
                for (int x = 0; x < sz; x++)
                {
                    bool left   = x >= 6   && x < 6   + thick && y >= 8 && y <= 50;
                    bool right  = x >= sz - 6 - thick && x < sz - 6 && y >= 8 && y <= 50;
                    bool bottom = y >= 8   && y < 8   + thick && x >= 6 && x <= sz - 7;
                    bool rim    = y >= 48  && y < 48  + thick && x >= 4 && x <= sz - 5;
                    pixels[y * sz + x] = (left || right || bottom || rim)
                        ? Color.white : Color.clear;
                }

            tex.SetPixels(pixels);
            tex.Apply();
            return Sprite.Create(tex, new Rect(0, 0, sz, sz), Vector2.one * 0.5f, sz);
        }
    }
}
