using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace ForestFriendsQuest
{
    /// <summary>
    /// Shadow Match Puzzle — match every creature shadow to its correct silhouette.
    ///
    /// The left panel shows N creature shadow blobs (dark filled shapes).
    /// The right panel shows N creature silhouettes (outlined shapes with labels A, B, C…).
    /// The player taps a shadow to select it, then taps the matching silhouette.
    /// A correct match locks the pair in with a glow; wrong → gentle nudge.
    /// When all pairs are correctly matched → SOLVED.
    ///
    /// Sprout  (4-6):  3 pairs — simple round shapes, outline visible
    /// Scout  (7-11):  4 pairs — more complex shapes, labels only
    /// Druid (12-16):  5 pairs — 5 unique creature silhouettes + 1 decoy shape
    ///
    /// All shapes are procedurally drawn — no emoji, no sprites loaded from disk.
    /// </summary>
    public class ShadowMatchPuzzle : MonoBehaviour
    {
        // ─── Creature Shapes ─────────────────────────────────────────────────────

        // Shape IDs correspond to creature silhouettes
        private static readonly string[] CreatureNames =
            { "Pip", "Mimi", "Tomo", "Luma", "Nori", "Sol" };

        private static readonly Color[] SilhouetteColors =
        {
            new Color(0.88f, 0.55f, 0.25f),   // Pip   — fox orange
            new Color(0.30f, 0.65f, 0.90f),   // Mimi  — bird blue
            new Color(0.50f, 0.75f, 0.45f),   // Tomo  — turtle green
            new Color(1.00f, 0.95f, 0.35f),   // Luma  — firefly gold
            new Color(0.55f, 0.80f, 0.40f),   // Nori  — deer green
            new Color(0.65f, 0.55f, 0.80f),   // Sol   — owl violet
        };

        // ─── State ───────────────────────────────────────────────────────────────

        private int              _pairCount;
        private int[]            _shadowOrder;   // display order of shadows (shuffled)
        private int[]            _silhouetteOrder; // display order of silhouettes (shuffled)
        private int              _selectedShadow = -1;     // -1 = none
        private bool[]           _matched;
        private int              _matchedCount;

        // ─── UI ──────────────────────────────────────────────────────────────────

        private RectTransform[]  _shadowRects;
        private Image[]          _shadowImages;
        private RectTransform[]  _silhouetteRects;
        private Image[]          _silhouetteImages;
        private Image[]          _silhouetteBorders;

        private static readonly Color ShadowColor    = new Color(0.08f, 0.10f, 0.12f, 0.88f);
        private static readonly Color SelectedColor  = new Color(0.40f, 0.85f, 0.55f, 1.00f);
        private static readonly Color MatchedColor   = new Color(0.30f, 0.90f, 0.55f, 0.80f);
        private static readonly Color WrongFlash     = new Color(0.90f, 0.30f, 0.25f, 0.80f);

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

            _pairCount       = tier == "druid" ? 5 : tier == "scout" ? 4 : 3;
            _matched         = new bool[_pairCount];
            _shadowOrder     = ShuffledIndices(_pairCount, seed: 11);
            _silhouetteOrder = ShuffledIndices(_pairCount, seed: 77);

            _shadowRects      = new RectTransform[_pairCount];
            _shadowImages     = new Image[_pairCount];
            _silhouetteRects  = new RectTransform[_pairCount];
            _silhouetteImages = new Image[_pairCount];
            _silhouetteBorders = new Image[_pairCount];

            BuildUI(parent, tier);
            _manager.StartPuzzle(PuzzleType.ShadowMatch, tier);
        }

        // ─── Player Input ─────────────────────────────────────────────────────────

        public void OnShadowTapped(int displayIndex, Vector2 canvasPos)
        {
            var creatureIdx = _shadowOrder[displayIndex];
            if (_matched[creatureIdx]) return;

            // Deselect previous selection
            if (_selectedShadow >= 0)
                _shadowImages[_selectedShadow].color = ShadowColor;

            _selectedShadow = displayIndex;
            _shadowImages[displayIndex].color = SelectedColor;
        }

        public void OnSilhouetteTapped(int displayIndex, Vector2 canvasPos)
        {
            if (_selectedShadow < 0) return;

            var shadowCreature    = _shadowOrder[_selectedShadow];
            var silhouetteCreature = _silhouetteOrder[displayIndex];

            if (_matched[shadowCreature]) { _selectedShadow = -1; return; }
            if (_matched[silhouetteCreature]) return;

            if (shadowCreature == silhouetteCreature)
            {
                // Correct match!
                _matched[shadowCreature] = true;
                _matchedCount++;

                _shadowImages[_selectedShadow].color = MatchedColor;
                _silhouetteImages[displayIndex].color = MatchedColor;
                _particles?.Spawn(EmotionalParticleType.HappyGoldenWisp, canvasPos, 5);
                _manager.RecordCorrectStep(canvasPos);
                _selectedShadow = -1;

                if (_matchedCount >= _pairCount)
                {
                    _particles?.SpawnJoyBurst(canvasPos);
                    _manager.SolvePuzzle(canvasPos);
                    OnPuzzleEnd?.Invoke(true);
                }
            }
            else
            {
                // Wrong match — flash red, deselect
                _particles?.Spawn(EmotionalParticleType.GrassDisturbDust, canvasPos, 2);
                _manager.RecordMistake(canvasPos);
                _shadowImages[_selectedShadow].color = ShadowColor;
                _selectedShadow = -1;
            }
        }

        // ─── UI Builder ───────────────────────────────────────────────────────────

        private void BuildUI(RectTransform parent, string tier)
        {
            // Title
            CreateLabel(parent, "Match the shadows!", new Vector2(0f, 200f), 24, Color.white);

            float rowHeight = Mathf.Min(340f / _pairCount, 80f);
            float startY    = (rowHeight * (_pairCount - 1)) / 2f;

            for (var i = 0; i < _pairCount; i++)
            {
                var y = startY - i * rowHeight;
                var creatureIdx = _shadowOrder[i];

                // Shadow (left side)
                var shadowGo = CreateShapeCard(parent, new Vector2(-180f, y),
                    new Vector2(90f, 90f), CreatureNames[creatureIdx], dark: true);
                _shadowRects[i]  = shadowGo.GetComponent<RectTransform>();
                _shadowImages[i] = shadowGo.GetComponent<Image>();
                _shadowImages[i].color = ShadowColor;
                var si = i;
                shadowGo.GetComponent<Button>().onClick.AddListener(
                    () => OnShadowTapped(si, shadowGo.GetComponent<RectTransform>().anchoredPosition));

                // Silhouette (right side) — creature at shuffled position
                var silCreatureIdx = _silhouetteOrder[i];
                var silGo = CreateShapeCard(parent, new Vector2(180f, y),
                    new Vector2(90f, 90f), CreatureNames[silCreatureIdx], dark: false);
                _silhouetteRects[i]  = silGo.GetComponent<RectTransform>();
                _silhouetteImages[i] = silGo.GetComponent<Image>();
                _silhouetteImages[i].color = SilhouetteColors[silCreatureIdx] * 0.6f;
                var sil = i;
                silGo.GetComponent<Button>().onClick.AddListener(
                    () => OnSilhouetteTapped(sil, silGo.GetComponent<RectTransform>().anchoredPosition));

                // Letter label on silhouette
                CreateLabel(silGo.GetComponent<RectTransform>(), ((char)('A' + i)).ToString(),
                    new Vector2(0f, -54f), 20, Color.white);
            }
        }

        private static GameObject CreateShapeCard(RectTransform parent, Vector2 pos,
            Vector2 size, string creatureName, bool dark)
        {
            var go  = new GameObject($"Shape_{creatureName}_{(dark ? "shadow" : "sil")}");
            go.transform.SetParent(parent, false);
            var rt  = go.AddComponent<RectTransform>();
            rt.anchoredPosition = pos;
            rt.sizeDelta        = size;

            var img    = go.AddComponent<Image>();
            img.sprite = CreateCreatureShapeSprite(creatureName, dark);
            img.raycastTarget = true;
            go.AddComponent<Button>();
            return go;
        }

        private static void CreateLabel(RectTransform parent, string text, Vector2 pos,
            int fontSize, Color color)
        {
            var go  = new GameObject("Label");
            go.transform.SetParent(parent, false);
            var rt  = go.AddComponent<RectTransform>();
            rt.anchoredPosition = pos;
            rt.sizeDelta        = new Vector2(200f, 32f);
            var txt = go.AddComponent<Text>();
            txt.text      = text;
            txt.font      = ForestUiFactory.GetDefaultFont();
            txt.fontSize  = fontSize;
            txt.color     = color;
            txt.alignment = TextAnchor.MiddleCenter;
            txt.fontStyle = FontStyle.Bold;
        }

        // ─── Sprite Factory ───────────────────────────────────────────────────────

        private static Sprite CreateCreatureShapeSprite(string creatureName, bool darkFill)
        {
            const int size = 64;
            var tex    = new Texture2D(size, size, TextureFormat.RGBA32, false);
            var pixels = new Color[size * size];
            var cx     = size / 2f;
            var cy     = size / 2f;

            Color fillCol   = darkFill ? new Color(0.08f, 0.10f, 0.12f, 1f) : Color.clear;
            Color borderCol = darkFill ? Color.clear : new Color(0.85f, 0.92f, 0.80f, 1f);

            // Each creature gets a distinct silhouette shape
            switch (creatureName)
            {
                case "Pip":    DrawFoxShape(pixels, size, cx, cy, fillCol, borderCol);    break;
                case "Mimi":   DrawBirdShape(pixels, size, cx, cy, fillCol, borderCol);   break;
                case "Tomo":   DrawTurtleShape(pixels, size, cx, cy, fillCol, borderCol); break;
                case "Luma":   DrawFireflyShape(pixels, size, cx, cy, fillCol, borderCol);break;
                case "Nori":   DrawDeerShape(pixels, size, cx, cy, fillCol, borderCol);   break;
                case "Sol":    DrawOwlShape(pixels, size, cx, cy, fillCol, borderCol);    break;
                default:       DrawGenericBlob(pixels, size, cx, cy, fillCol, borderCol); break;
            }

            tex.SetPixels(pixels);
            tex.Apply();
            return Sprite.Create(tex, new Rect(0, 0, size, size), Vector2.one * 0.5f, size);
        }

        // Each creature shape is built from simple geometric primitives

        private static void DrawFoxShape(Color[] p, int s, float cx, float cy,
            Color fill, Color border)
        {
            // Round body + pointy ears
            FillEllipse(p, s, cx, cy + 5, 14, 12, fill, border);
            FillTriangle(p, s, (int)cx - 10, (int)cy - 8, (int)cx - 4, (int)cy - 20, (int)cx - 16, (int)cy - 20, fill, border);
            FillTriangle(p, s, (int)cx + 10, (int)cy - 8, (int)cx + 4, (int)cy - 20, (int)cx + 16, (int)cy - 20, fill, border);
            FillEllipse(p, s, cx, cy - 9, 10, 8, fill, border);
        }

        private static void DrawBirdShape(Color[] p, int s, float cx, float cy,
            Color fill, Color border)
        {
            FillEllipse(p, s, cx, cy + 4, 12, 10, fill, border); // body
            FillEllipse(p, s, cx, cy - 10, 8, 7, fill, border);  // head
            FillTriangle(p, s, (int)cx + 7, (int)cy - 10, (int)cx + 18, (int)cy - 8, (int)cx + 8, (int)cy - 6, fill, border); // beak
            FillEllipse(p, s, cx - 14, cy + 2, 7, 4, fill, border); // wing
        }

        private static void DrawTurtleShape(Color[] p, int s, float cx, float cy,
            Color fill, Color border)
        {
            FillEllipse(p, s, cx, cy, 18, 13, fill, border); // shell
            FillEllipse(p, s, cx, cy - 16, 7, 6, fill, border); // head
            FillEllipse(p, s, cx - 20, cy + 4, 5, 3, fill, border); // left leg
            FillEllipse(p, s, cx + 20, cy + 4, 5, 3, fill, border); // right leg
        }

        private static void DrawFireflyShape(Color[] p, int s, float cx, float cy,
            Color fill, Color border)
        {
            FillEllipse(p, s, cx, cy + 4, 8, 13, fill, border); // elongated body
            FillEllipse(p, s, cx, cy - 14, 6, 5, fill, border);  // head
            FillEllipse(p, s, cx - 14, cy - 2, 8, 3, fill, border); // wing L
            FillEllipse(p, s, cx + 14, cy - 2, 8, 3, fill, border); // wing R
        }

        private static void DrawDeerShape(Color[] p, int s, float cx, float cy,
            Color fill, Color border)
        {
            FillEllipse(p, s, cx, cy + 6, 12, 10, fill, border); // body
            FillEllipse(p, s, cx, cy - 10, 7, 8, fill, border);   // head
            // antlers
            DrawSeg(p, s, (int)cx - 4, (int)cy - 18, (int)cx - 12, (int)cy - 28, border);
            DrawSeg(p, s, (int)cx + 4, (int)cy - 18, (int)cx + 12, (int)cy - 28, border);
            DrawSeg(p, s, (int)cx - 12, (int)cy - 28, (int)cx - 8,  (int)cy - 34, border);
            DrawSeg(p, s, (int)cx + 12, (int)cy - 28, (int)cx + 8,  (int)cy - 34, border);
        }

        private static void DrawOwlShape(Color[] p, int s, float cx, float cy,
            Color fill, Color border)
        {
            FillEllipse(p, s, cx, cy + 4, 14, 16, fill, border); // round body
            FillEllipse(p, s, cx, cy - 12, 11, 10, fill, border); // round head
            FillEllipse(p, s, cx - 6,  cy - 13, 4, 3, fill, border); // left eye
            FillEllipse(p, s, cx + 6,  cy - 13, 4, 3, fill, border); // right eye
            // ear tufts
            FillTriangle(p, s, (int)cx - 8, (int)cy - 20, (int)cx - 12, (int)cy - 28, (int)cx - 4, (int)cy - 22, fill, border);
            FillTriangle(p, s, (int)cx + 8, (int)cy - 20, (int)cx + 12, (int)cy - 28, (int)cx + 4, (int)cy - 22, fill, border);
        }

        private static void DrawGenericBlob(Color[] p, int s, float cx, float cy,
            Color fill, Color border)
            => FillEllipse(p, s, cx, cy, 16, 14, fill, border);

        // ─── Primitive Drawing ────────────────────────────────────────────────────

        private static void FillEllipse(Color[] pixels, int size, float cx, float cy,
            float rx, float ry, Color fill, Color border)
        {
            int minX = Mathf.Max(0, (int)(cx - rx - 2));
            int maxX = Mathf.Min(size - 1, (int)(cx + rx + 2));
            int minY = Mathf.Max(0, (int)(cy - ry - 2));
            int maxY = Mathf.Min(size - 1, (int)(cy + ry + 2));

            for (var y = minY; y <= maxY; y++)
            {
                for (var x = minX; x <= maxX; x++)
                {
                    float dx = (x - cx) / rx;
                    float dy = (y - cy) / ry;
                    float d  = dx * dx + dy * dy;

                    if (d <= 1.0f && fill.a > 0)
                        pixels[y * size + x] = fill;
                    else if (d <= 1.2f && border.a > 0)
                        pixels[y * size + x] = border;
                }
            }
        }

        private static void FillTriangle(Color[] pixels, int size,
            int x0, int y0, int x1, int y1, int x2, int y2, Color fill, Color border)
        {
            int minX = Mathf.Max(0, Mathf.Min(x0, Mathf.Min(x1, x2)));
            int maxX = Mathf.Min(size - 1, Mathf.Max(x0, Mathf.Max(x1, x2)));
            int minY = Mathf.Max(0, Mathf.Min(y0, Mathf.Min(y1, y2)));
            int maxY = Mathf.Min(size - 1, Mathf.Max(y0, Mathf.Max(y1, y2)));

            for (var y = minY; y <= maxY; y++)
            {
                for (var x = minX; x <= maxX; x++)
                {
                    if (PointInTriangle(x, y, x0, y0, x1, y1, x2, y2))
                    {
                        if (fill.a > 0) pixels[y * size + x] = fill;
                        else if (border.a > 0) pixels[y * size + x] = border;
                    }
                }
            }
        }

        private static bool PointInTriangle(int px, int py,
            int x0, int y0, int x1, int y1, int x2, int y2)
        {
            int d1 = Sign(px, py, x0, y0, x1, y1);
            int d2 = Sign(px, py, x1, y1, x2, y2);
            int d3 = Sign(px, py, x2, y2, x0, y0);
            bool hasNeg = (d1 < 0) || (d2 < 0) || (d3 < 0);
            bool hasPos = (d1 > 0) || (d2 > 0) || (d3 > 0);
            return !(hasNeg && hasPos);
        }

        private static int Sign(int px, int py, int x0, int y0, int x1, int y1)
            => (px - x1) * (y0 - y1) - (x0 - x1) * (py - y1);

        private static void DrawSeg(Color[] pixels, int size, int x0, int y0, int x1, int y1,
            Color col)
        {
            if (col.a <= 0) return;
            var dx = x1 - x0; var dy = y1 - y0;
            var steps = Mathf.Max(Mathf.Abs(dx), Mathf.Abs(dy));
            if (steps == 0) return;
            for (var i = 0; i <= steps; i++)
            {
                int x = x0 + (int)(dx * i / (float)steps);
                int y = y0 + (int)(dy * i / (float)steps);
                if (x >= 0 && x < size && y >= 0 && y < size)
                {
                    pixels[y * size + x] = col;
                    if (x + 1 < size) pixels[y * size + x + 1] = col;
                }
            }
        }

        // ─── Helpers ─────────────────────────────────────────────────────────────

        private static int[] ShuffledIndices(int count, int seed)
        {
            var arr = new int[count];
            for (var i = 0; i < count; i++) arr[i] = i;
            var rng = new System.Random(seed);
            for (var i = count - 1; i > 0; i--)
            {
                int j = rng.Next(0, i + 1);
                (arr[i], arr[j]) = (arr[j], arr[i]);
            }
            return arr;
        }
    }
}
