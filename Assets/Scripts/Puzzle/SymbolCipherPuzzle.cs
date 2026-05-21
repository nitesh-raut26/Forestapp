using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace ForestFriendsQuest
{
    /// <summary>
    /// Symbol Cipher Puzzle — crack a simple forest cipher to solve an equation.
    ///
    /// The puzzle presents a "cipher key": each forest symbol (Leaf, Mushroom, Stone,
    /// Acorn, Crystal, Berry) is secretly assigned a numeric value (1–9).
    /// A subset of symbol=value pairs are revealed as clues. The player must read
    /// the clues, then answer a hidden equation by tapping the correct answer card.
    ///
    /// Sprout  (4-6):  3 symbols, 1-step sum, 3 answer choices
    /// Scout  (7-11):  4 symbols, 2-step expression, 3 answer choices
    /// Druid (12-16):  5 symbols, 3-step chained expression, 4 answer choices
    ///
    /// Visual feedback:
    ///   Correct answer → HappyGoldenWisp burst
    ///   Wrong answer   → GrassDisturbDust + RecordMistake (doesn't end puzzle)
    ///   Solved         → JoyBurst + SolvePuzzle
    /// </summary>
    public class SymbolCipherPuzzle : MonoBehaviour
    {
        // ─── Symbol Definitions ───────────────────────────────────────────────────

        private static readonly string[] SymbolNames = { "Leaf", "Mushroom", "Stone", "Acorn", "Crystal", "Berry" };
        private static readonly Color[]  SymbolColors =
        {
            new Color(0.45f, 0.88f, 0.35f),  // Leaf     — green
            new Color(0.90f, 0.50f, 0.30f),  // Mushroom — orange
            new Color(0.65f, 0.65f, 0.70f),  // Stone    — grey
            new Color(0.92f, 0.78f, 0.30f),  // Acorn    — amber
            new Color(0.55f, 0.80f, 1.00f),  // Crystal  — sky blue
            new Color(0.90f, 0.30f, 0.55f),  // Berry    — rose
        };

        // ─── State ───────────────────────────────────────────────────────────────

        private PuzzleManager           _manager;
        private EmotionalParticleEngine _particles;
        private string                  _tier;

        // symbol index → cipher value (1–9)
        private int[]       _cipherValues;
        // which symbols are used in this round
        private int[]       _activeSymbols;
        // equation operands (symbol indices) and operators
        private int[]       _equationSymbols;
        private int[]       _operators;      // 0 = add, 1 = subtract
        private int         _correctAnswer;
        // answer cards shown to player
        private int[]       _answerChoices;

        private bool        _solved;

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
            _tier      = tier;

            var rng = new System.Random(DateTime.Today.DayOfYear + tier.GetHashCode());
            SetupCipher(rng, tier);
            BuildEquation(rng, tier);
            BuildUI(parent);

            _manager.StartPuzzle(PuzzleType.SymbolCipher, tier);
        }

        // ─── Cipher Setup ─────────────────────────────────────────────────────────

        private void SetupCipher(System.Random rng, string tier)
        {
            int symbolCount = tier == "druid" ? 5 : tier == "scout" ? 4 : 3;
            _cipherValues   = new int[SymbolNames.Length];
            _activeSymbols  = new int[symbolCount];

            // Assign values 1–9, pick unique symbols
            var used = new HashSet<int>();
            for (var i = 0; i < symbolCount; i++)
            {
                int sym;
                do { sym = rng.Next(0, SymbolNames.Length); } while (used.Contains(sym));
                used.Add(sym);
                _activeSymbols[i] = sym;
                _cipherValues[sym] = rng.Next(1, 10);  // 1–9
            }
        }

        private void BuildEquation(System.Random rng, string tier)
        {
            // Number of terms in the equation
            int terms = tier == "druid" ? 3 : tier == "scout" ? 2 : 1;

            _equationSymbols = new int[terms];
            _operators       = new int[Mathf.Max(terms - 1, 0)];

            for (var i = 0; i < terms; i++)
                _equationSymbols[i] = _activeSymbols[rng.Next(0, _activeSymbols.Length)];

            for (var i = 0; i < _operators.Length; i++)
                _operators[i] = tier == "sprout" ? 0 : rng.Next(0, 2);  // sprout: add only

            // Compute correct answer
            _correctAnswer = _cipherValues[_equationSymbols[0]];
            for (var i = 0; i < _operators.Length; i++)
            {
                int val = _cipherValues[_equationSymbols[i + 1]];
                _correctAnswer = _operators[i] == 0 ? _correctAnswer + val : _correctAnswer - val;
            }

            // Ensure answer is positive (clamp) — kid-friendly
            _correctAnswer = Mathf.Max(1, _correctAnswer);

            // Build answer choices (1 correct + 2 or 3 distractors)
            int numChoices    = tier == "druid" ? 4 : 3;
            _answerChoices    = new int[numChoices];
            _answerChoices[0] = _correctAnswer;
            var usedAnswers   = new HashSet<int> { _correctAnswer };

            for (var i = 1; i < numChoices; i++)
            {
                int distractor;
                do
                {
                    distractor = _correctAnswer + rng.Next(-4, 5);
                    if (distractor <= 0) distractor = rng.Next(1, 5);
                } while (usedAnswers.Contains(distractor));
                usedAnswers.Add(distractor);
                _answerChoices[i] = distractor;
            }

            // Shuffle choices
            for (var i = numChoices - 1; i > 0; i--)
            {
                int j = rng.Next(0, i + 1);
                (_answerChoices[i], _answerChoices[j]) = (_answerChoices[j], _answerChoices[i]);
            }
        }

        // ─── Player Input ─────────────────────────────────────────────────────────

        public void OnAnswerSelected(int choiceIndex, Vector2 canvasPos)
        {
            if (_solved) return;

            if (_answerChoices[choiceIndex] == _correctAnswer)
            {
                _solved = true;
                _particles?.SpawnJoyBurst(canvasPos);
                _manager.SolvePuzzle(canvasPos);
                OnPuzzleEnd?.Invoke(true);
            }
            else
            {
                _particles?.Spawn(EmotionalParticleType.GrassDisturbDust, canvasPos, 2);
                _manager.RecordMistake(canvasPos);
            }
        }

        // ─── UI Builder ───────────────────────────────────────────────────────────

        private void BuildUI(RectTransform parent)
        {
            // ── Cipher Key Panel ─────────────────────────────────────────────────
            var keyPanel = CreatePanel(parent, "CipherKey",
                new Vector2(0f, 130f), new Vector2(560f, 100f),
                new Color(0.10f, 0.22f, 0.15f, 0.85f));

            float symbolStep = 560f / (_activeSymbols.Length + 1);
            for (var i = 0; i < _activeSymbols.Length; i++)
            {
                var sym = _activeSymbols[i];
                var xPos = symbolStep * (i + 1) - 280f;
                CreateSymbolClue(keyPanel, sym, _cipherValues[sym], new Vector2(xPos, 0f));
            }

            // ── Equation Display ──────────────────────────────────────────────────
            var eqPanel = CreatePanel(parent, "Equation",
                new Vector2(0f, 10f), new Vector2(560f, 80f),
                new Color(0.08f, 0.18f, 0.12f, 0.90f));

            BuildEquationLabel(eqPanel);

            // ── Answer Choices ────────────────────────────────────────────────────
            float cardW    = 520f / _answerChoices.Length - 8f;
            float startX   = -(520f / 2f) + cardW / 2f;

            for (var i = 0; i < _answerChoices.Length; i++)
            {
                var idx    = i;
                var xPos   = startX + i * (cardW + 8f);
                var card   = CreateAnswerCard(parent, new Vector2(xPos, -120f),
                    new Vector2(cardW, 72f), _answerChoices[i]);
                var btn    = card.GetComponent<Button>() ?? card.gameObject.AddComponent<Button>();
                btn.onClick.AddListener(() => OnAnswerSelected(idx, card.anchoredPosition));
            }
        }

        private void BuildEquationLabel(RectTransform parent)
        {
            // Build equation string: "Leaf + Stone = ?"
            var sb = new System.Text.StringBuilder();
            sb.Append(SymbolNames[_equationSymbols[0]]);
            for (var i = 0; i < _operators.Length; i++)
            {
                sb.Append(_operators[i] == 0 ? " + " : " - ");
                sb.Append(SymbolNames[_equationSymbols[i + 1]]);
            }
            sb.Append(" = ?");

            var go  = new GameObject("EqText");
            go.transform.SetParent(parent, false);
            var rt  = go.AddComponent<RectTransform>();
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.sizeDelta = Vector2.zero;
            var txt = go.AddComponent<Text>();
            txt.text      = sb.ToString();
            txt.font      = ForestUiFactory.GetDefaultFont();
            txt.fontSize  = 26;
            txt.color     = new Color(0.90f, 1.00f, 0.80f);
            txt.alignment = TextAnchor.MiddleCenter;
            txt.fontStyle = FontStyle.Bold;
        }

        private static void CreateSymbolClue(RectTransform parent, int symIdx, int value, Vector2 pos)
        {
            var go  = new GameObject($"Clue_{SymbolNames[symIdx]}");
            go.transform.SetParent(parent, false);
            var rt  = go.AddComponent<RectTransform>();
            rt.anchoredPosition = pos;
            rt.sizeDelta        = new Vector2(80f, 80f);

            // Coloured symbol circle
            var img = go.AddComponent<Image>();
            img.sprite = CreateSymbolSprite(symIdx);
            img.color  = SymbolColors[symIdx];

            // "= N" label below
            var labelGo = new GameObject("Val");
            labelGo.transform.SetParent(go.transform, false);
            var labelRt = labelGo.AddComponent<RectTransform>();
            labelRt.anchoredPosition = new Vector2(0f, -46f);
            labelRt.sizeDelta        = new Vector2(60f, 28f);
            var txt = labelGo.AddComponent<Text>();
            txt.text      = $"= {value}";
            txt.font      = ForestUiFactory.GetDefaultFont();
            txt.fontSize  = 18;
            txt.color     = Color.white;
            txt.alignment = TextAnchor.MiddleCenter;
            txt.fontStyle = FontStyle.Bold;
        }

        private static RectTransform CreateAnswerCard(RectTransform parent, Vector2 pos,
            Vector2 size, int value)
        {
            var go  = new GameObject($"Answer_{value}");
            go.transform.SetParent(parent, false);
            var rt  = go.AddComponent<RectTransform>();
            rt.anchoredPosition = pos;
            rt.sizeDelta        = size;

            var img = go.AddComponent<Image>();
            img.color  = new Color(0.22f, 0.50f, 0.35f, 0.90f);
            img.sprite = CreateRoundRectSprite();

            var txt = go.AddComponent<Text>();
            txt.text      = value.ToString();
            txt.font      = ForestUiFactory.GetDefaultFont();
            txt.fontSize  = 32;
            txt.color     = new Color(0.90f, 1.00f, 0.78f);
            txt.alignment = TextAnchor.MiddleCenter;
            txt.fontStyle = FontStyle.Bold;

            go.AddComponent<Button>();
            return rt;
        }

        private static RectTransform CreatePanel(RectTransform parent, string name,
            Vector2 pos, Vector2 size, Color bg)
        {
            var go  = new GameObject(name);
            go.transform.SetParent(parent, false);
            var rt  = go.AddComponent<RectTransform>();
            rt.anchoredPosition = pos;
            rt.sizeDelta        = size;
            var img = go.AddComponent<Image>();
            img.color  = bg;
            img.sprite = CreateRoundRectSprite();
            return rt;
        }

        // ─── Sprite Factories ─────────────────────────────────────────────────────

        private static Sprite CreateSymbolSprite(int symIdx)
        {
            const int size = 48;
            var tex    = new Texture2D(size, size, TextureFormat.RGBA32, false);
            var pixels = new Color[size * size];
            var cx     = size / 2f;
            var cy     = size / 2f;
            var rng    = new System.Random(symIdx * 31 + 7);

            // Soft circle background
            for (var y = 0; y < size; y++)
                for (var x = 0; x < size; x++)
                {
                    var d = Vector2.Distance(new Vector2(x + 0.5f, y + 0.5f), new Vector2(cx, cy));
                    var a = 1f - Mathf.Clamp01((d - size * 0.38f) / (size * 0.1f));
                    pixels[y * size + x] = new Color(1f, 1f, 1f, a);
                }

            // Unique inner mark (cross, star, diamond, ring, dot, leaf-shape)
            switch (symIdx)
            {
                case 0: DrawThickLine(pixels, size, 12, 24, 36, 24, Color.white, 2); // horizontal = Leaf
                        DrawThickLine(pixels, size, 24, 12, 24, 36, Color.white, 2); break;
                case 1: DrawCircle(pixels, size, 24, 24, 12, Color.white, 2);        break; // Mushroom
                case 2: DrawDiamond(pixels, size, Color.white);                      break; // Stone
                case 3: DrawStar(pixels, size, Color.white, 5);                      break; // Acorn
                case 4: DrawHexagon(pixels, size, Color.white);                      break; // Crystal
                case 5: DrawCircle(pixels, size, 24, 24, 8, Color.white, 3);
                        DrawCircle(pixels, size, 24, 24, 4, Color.white, 3);         break; // Berry
            }

            tex.SetPixels(pixels);
            tex.Apply();
            return Sprite.Create(tex, new Rect(0, 0, size, size), Vector2.one * 0.5f, size);
        }

        private static Sprite CreateRoundRectSprite()
        {
            const int size   = 32;
            const int corner = 6;
            var tex    = new Texture2D(size, size, TextureFormat.RGBA32, false);
            var pixels = new Color[size * size];

            for (var y = 0; y < size; y++)
                for (var x = 0; x < size; x++)
                {
                    var dx    = Mathf.Min(x, size - 1 - x);
                    var dy    = Mathf.Min(y, size - 1 - y);
                    var alpha = Mathf.Clamp01(Mathf.Min(dx, dy) / (float)corner);
                    pixels[y * size + x] = new Color(1f, 1f, 1f, alpha);
                }

            tex.SetPixels(pixels);
            tex.Apply();
            return Sprite.Create(tex, new Rect(0, 0, size, size), Vector2.one * 0.5f, size);
        }

        // ─── Drawing Helpers ──────────────────────────────────────────────────────

        private static void DrawThickLine(Color[] pixels, int size, int x0, int y0, int x1,
            int y1, Color col, int thickness)
        {
            var dx = x1 - x0; var dy = y1 - y0;
            var steps = Mathf.Max(Mathf.Abs(dx), Mathf.Abs(dy));
            if (steps == 0) return;
            for (var i = 0; i <= steps; i++)
            {
                int px = x0 + (int)(dx * i / (float)steps);
                int py = y0 + (int)(dy * i / (float)steps);
                for (var ty = -thickness; ty <= thickness; ty++)
                    for (var tx = -thickness; tx <= thickness; tx++)
                    {
                        int nx = px + tx; int ny = py + ty;
                        if (nx >= 0 && nx < size && ny >= 0 && ny < size)
                            pixels[ny * size + nx] = col;
                    }
            }
        }

        private static void DrawCircle(Color[] pixels, int size, int cx, int cy, int r,
            Color col, int thickness)
        {
            for (var y = cy - r - thickness; y <= cy + r + thickness; y++)
                for (var x = cx - r - thickness; x <= cx + r + thickness; x++)
                {
                    var d = Mathf.Sqrt((x - cx) * (x - cx) + (y - cy) * (y - cy));
                    if (Mathf.Abs(d - r) <= thickness && x >= 0 && x < size && y >= 0 && y < size)
                        pixels[y * size + x] = col;
                }
        }

        private static void DrawDiamond(Color[] pixels, int size, Color col)
        {
            var cx = size / 2; var cy = size / 2; var r = size / 4;
            DrawThickLine(pixels, size, cx,     cy - r, cx + r, cy,     col, 2);
            DrawThickLine(pixels, size, cx + r, cy,     cx,     cy + r, col, 2);
            DrawThickLine(pixels, size, cx,     cy + r, cx - r, cy,     col, 2);
            DrawThickLine(pixels, size, cx - r, cy,     cx,     cy - r, col, 2);
        }

        private static void DrawStar(Color[] pixels, int size, Color col, int points)
        {
            var cx = size / 2f; var cy = size / 2f;
            var outerR = size * 0.40f; var innerR = size * 0.18f;
            for (var i = 0; i < points * 2; i++)
            {
                var angleA = Mathf.PI * i       / points - Mathf.PI / 2f;
                var angleB = Mathf.PI * (i + 1) / points - Mathf.PI / 2f;
                var rA = (i % 2 == 0) ? outerR : innerR;
                var rB = (i % 2 == 0) ? innerR : outerR;
                var x0 = (int)(cx + Mathf.Cos(angleA) * rA);
                var y0 = (int)(cy + Mathf.Sin(angleA) * rA);
                var x1 = (int)(cx + Mathf.Cos(angleB) * rB);
                var y1 = (int)(cy + Mathf.Sin(angleB) * rB);
                DrawThickLine(pixels, size, x0, y0, x1, y1, col, 1);
            }
        }

        private static void DrawHexagon(Color[] pixels, int size, Color col)
        {
            var cx = size / 2f; var cy = size / 2f; var r = size * 0.36f;
            for (var i = 0; i < 6; i++)
            {
                var a0 = Mathf.PI / 3f * i;
                var a1 = Mathf.PI / 3f * (i + 1);
                DrawThickLine(pixels, size,
                    (int)(cx + Mathf.Cos(a0) * r), (int)(cy + Mathf.Sin(a0) * r),
                    (int)(cx + Mathf.Cos(a1) * r), (int)(cy + Mathf.Sin(a1) * r),
                    col, 2);
            }
        }
    }
}
