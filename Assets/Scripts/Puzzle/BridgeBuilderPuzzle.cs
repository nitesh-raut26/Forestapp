using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace ForestFriendsQuest
{
    /// <summary>
    /// Bridge Builder Puzzle — place planks into river gaps in the correct numbered sequence
    /// to build a crossing from the left bank to the right bank.
    ///
    /// How it works:
    ///   • A horizontal river crossing is displayed: left bank → stones → right bank.
    ///   • Some stones are solid (safe, already placed). Gap tiles show a number (1…M).
    ///   • Player must tap gap tiles in ascending order: 1 first, then 2, then 3 …
    ///   • Tapping the correct gap places a plank (tile turns to stone colour + HappyPollenBurst).
    ///   • Tapping the wrong gap → GrassDisturbDust + RecordMistake (no reset, retry allowed).
    ///   • All gaps filled in order → JoyBurst + SolvePuzzle.
    ///
    /// Sprout  (4-6):  7 stones total, 3 gaps
    /// Scout  (7-11):  9 stones total, 4 gaps
    /// Druid (12-16): 11 stones total, 5 gaps
    ///
    /// Visual layout:
    ///   [Left Bank] ── [stone|gap|stone|gap|stone …] ── [Right Bank]
    ///   Gap tiles display their sequence number. Solid tiles show a mossy stone look.
    /// </summary>
    public class BridgeBuilderPuzzle : MonoBehaviour
    {
        // ─── Tile Types ───────────────────────────────────────────────────────────

        private enum TileKind { Bank, Stone, Gap }

        private struct BridgeTile
        {
            public TileKind kind;
            public int      gapSequence;  // 1-based order to fill; 0 for non-gaps
            public bool     filled;
            public Image    image;
            public Text     label;
        }

        // ─── State ───────────────────────────────────────────────────────────────

        private BridgeTile[] _tiles;
        private int          _tileCount;
        private int          _gapCount;
        private int          _nextSequence; // next expected gap number (1-based)

        // ─── Colors ───────────────────────────────────────────────────────────────

        private static readonly Color ColorBank    = new Color(0.40f, 0.65f, 0.30f, 1.00f); // grassy bank
        private static readonly Color ColorStone   = new Color(0.55f, 0.50f, 0.45f, 1.00f); // mossy stone
        private static readonly Color ColorGap     = new Color(0.28f, 0.50f, 0.85f, 0.85f); // river water
        private static readonly Color ColorFilled  = new Color(0.62f, 0.55f, 0.40f, 1.00f); // freshly placed plank
        private static readonly Color ColorNumber  = new Color(1.00f, 0.98f, 0.85f, 1.00f);

        private const float TileW = 76f;
        private const float TileH = 76f;
        private const float TileGap = 6f;

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
            _manager  = manager;
            _particles = particles;

            _gapCount   = tier == "druid" ? 5 : tier == "scout" ? 4 : 3;
            int interior = _gapCount == 5 ? 11 : _gapCount == 4 ? 9 : 7;
            // layout: bank + interior stones/gaps + bank = interior + 2 total
            _tileCount  = interior + 2;
            _nextSequence = 1;

            _tiles = new BridgeTile[_tileCount];

            BuildLayout(interior);
            BuildUI(parent);
            _manager.StartPuzzle(PuzzleType.BridgeBuilder, tier);
        }

        // ─── Input ───────────────────────────────────────────────────────────────

        private void HandleTileTapped(int tileIndex)
        {
            var t = _tiles[tileIndex];
            if (t.kind != TileKind.Gap || t.filled) return;

            var pos = _tiles[tileIndex].image != null
                ? _tiles[tileIndex].image.rectTransform.anchoredPosition
                : Vector2.zero;

            if (t.gapSequence == _nextSequence)
            {
                // ── Correct plank placement ───────────────────────────────────────
                _tiles[tileIndex].filled = true;
                _tiles[tileIndex].image.color  = ColorFilled;
                _tiles[tileIndex].image.sprite = MakeStoneSprite();
                if (_tiles[tileIndex].label != null)
                    _tiles[tileIndex].label.text = "";

                _manager.RecordCorrectStep(pos);
                _particles?.Spawn(EmotionalParticleType.HappyPollenBurst, pos, 4);

                _nextSequence++;

                if (_nextSequence > _gapCount)
                {
                    // All planks placed — solved!
                    _particles?.SpawnJoyBurst(pos);
                    _manager.SolvePuzzle(pos);
                    OnPuzzleEnd?.Invoke(true);
                }
            }
            else
            {
                // ── Wrong sequence ────────────────────────────────────────────────
                _particles?.Spawn(EmotionalParticleType.GrassDisturbDust, pos, 2);
                _manager.RecordMistake(pos);
            }
        }

        // ─── Layout Builder ───────────────────────────────────────────────────────

        private void BuildLayout(int interior)
        {
            // Left bank
            _tiles[0] = new BridgeTile { kind = TileKind.Bank, gapSequence = 0 };

            // Interior: spread gaps evenly among stones
            var rng        = new System.Random(19);
            var gapSlots   = new HashSet<int>(PickGapSlots(interior, _gapCount, rng));

            // Assign gap sequence randomly distributed across the gap slots
            var gapSeqOrder = new List<int>();
            for (int i = 1; i <= _gapCount; i++) gapSeqOrder.Add(i);
            FisherYates(gapSeqOrder, rng);
            int seqIdx = 0;

            for (int i = 0; i < interior; i++)
            {
                int tileIdx = i + 1; // offset by 1 for left bank
                if (gapSlots.Contains(i))
                {
                    _tiles[tileIdx] = new BridgeTile
                    {
                        kind         = TileKind.Gap,
                        gapSequence  = gapSeqOrder[seqIdx++],
                        filled       = false,
                    };
                }
                else
                {
                    _tiles[tileIdx] = new BridgeTile { kind = TileKind.Stone, gapSequence = 0 };
                }
            }

            // Right bank
            _tiles[_tileCount - 1] = new BridgeTile { kind = TileKind.Bank, gapSequence = 0 };
        }

        /// <summary>Pick <paramref name="count"/> distinct slot indices in [0, total),
        /// ensuring no two are adjacent and both ends are not gaps.</summary>
        private static List<int> PickGapSlots(int total, int count, System.Random rng)
        {
            var candidates = new List<int>();
            for (int i = 1; i < total - 1; i++) candidates.Add(i); // exclude index 0 and last
            FisherYates(candidates, rng);

            var chosen = new List<int>();
            foreach (var c in candidates)
            {
                bool adj = false;
                foreach (var ch in chosen)
                    if (Mathf.Abs(ch - c) <= 1) { adj = true; break; }
                if (!adj)
                {
                    chosen.Add(c);
                    if (chosen.Count == count) break;
                }
            }

            // Fallback if adjacency constraint can't be fully satisfied
            if (chosen.Count < count)
            {
                foreach (var c in candidates)
                {
                    if (!chosen.Contains(c)) chosen.Add(c);
                    if (chosen.Count == count) break;
                }
            }

            return chosen;
        }

        // ─── UI Builder ───────────────────────────────────────────────────────────

        private void BuildUI(RectTransform parent)
        {
            // River background strip
            var river = MakeRect("River", parent, Vector2.zero,
                new Vector2(_tileCount * (TileW + TileGap), TileH + 28f));
            var riverImg = river.gameObject.AddComponent<Image>();
            riverImg.color = new Color(0.20f, 0.40f, 0.75f, 0.45f);

            float totalW  = _tileCount * TileW + (_tileCount - 1) * TileGap;
            float startX  = -totalW / 2f + TileW / 2f;

            for (int i = 0; i < _tileCount; i++)
            {
                float x = startX + i * (TileW + TileGap);

                var rt  = MakeRect($"Tile_{i}", parent, new Vector2(x, 0f),
                    new Vector2(TileW, TileH));
                var img = rt.gameObject.AddComponent<Image>();

                switch (_tiles[i].kind)
                {
                    case TileKind.Bank:
                        img.sprite = MakeBankSprite();
                        img.color  = ColorBank;
                        break;
                    case TileKind.Stone:
                        img.sprite = MakeStoneSprite();
                        img.color  = ColorStone;
                        break;
                    case TileKind.Gap:
                        img.sprite = MakeWaterSprite();
                        img.color  = ColorGap;
                        break;
                }

                _tiles[i].image = img;

                // Button only on gap tiles
                if (_tiles[i].kind == TileKind.Gap)
                {
                    var btn = rt.gameObject.AddComponent<Button>();
                    int ti  = i;
                    btn.onClick.AddListener(() => HandleTileTapped(ti));

                    // Number label
                    var labelRt  = MakeRect("Num", rt, new Vector2(0, 2f),
                        new Vector2(TileW, TileH));
                    var txt      = labelRt.gameObject.AddComponent<Text>();
                    txt.text      = _tiles[i].gapSequence.ToString();
                    txt.font      = ForestUiFactory.GetDefaultFont();
                    txt.fontSize  = 24;
                    txt.fontStyle = FontStyle.Bold;
                    txt.color     = ColorNumber;
                    txt.alignment = TextAnchor.MiddleCenter;
                    _tiles[i].label = txt;
                }
            }

            // Hint label below bridge
            var hint    = MakeRect("Hint", parent, new Vector2(0, -TileH / 2f - 24f),
                new Vector2(400f, 26f));
            var hintTxt = hint.gameObject.AddComponent<Text>();
            hintTxt.text      = "Tap the gaps in order: 1 → 2 → 3 …";
            hintTxt.font      = ForestUiFactory.GetDefaultFont();
            hintTxt.fontSize  = 15;
            hintTxt.color     = new Color(0.9f, 0.95f, 0.85f, 0.80f);
            hintTxt.alignment = TextAnchor.MiddleCenter;
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

        // ─── Sprite Factories ─────────────────────────────────────────────────────

        private static Sprite MakeRoundedSprite(int sz, float corner)
        {
            var tex    = new Texture2D(sz, sz, TextureFormat.RGBA32, false);
            var pixels = new Color[sz * sz];
            for (int y = 0; y < sz; y++)
                for (int x = 0; x < sz; x++)
                {
                    float dx = Mathf.Min(x, sz - 1 - x);
                    float dy = Mathf.Min(y, sz - 1 - y);
                    pixels[y * sz + x] = new Color(1, 1, 1,
                        Mathf.Clamp01(Mathf.Min(dx, dy) / corner));
                }
            tex.SetPixels(pixels);
            tex.Apply();
            return Sprite.Create(tex, new Rect(0, 0, sz, sz), Vector2.one * 0.5f, sz);
        }

        private static Sprite MakeBankSprite()  => MakeRoundedSprite(48, 6f);
        private static Sprite MakeStoneSprite() => MakeRoundedSprite(48, 9f);

        private static Sprite MakeWaterSprite()
        {
            // Wavy horizontal lines to suggest water
            const int sz = 48;
            var tex    = new Texture2D(sz, sz, TextureFormat.RGBA32, false);
            var pixels = new Color[sz * sz];
            for (int y = 0; y < sz; y++)
                for (int x = 0; x < sz; x++)
                {
                    float wave = Mathf.Sin((x + y * 0.5f) * 0.55f) * 0.5f + 0.5f;
                    pixels[y * sz + x] = new Color(1, 1, 1, 0.55f + wave * 0.30f);
                }
            tex.SetPixels(pixels);
            tex.Apply();
            return Sprite.Create(tex, new Rect(0, 0, sz, sz), Vector2.one * 0.5f, sz);
        }
    }
}
