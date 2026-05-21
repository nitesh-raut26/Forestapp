using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace ForestFriendsQuest
{
    /// <summary>
    /// Star Constellation Puzzle — tap numbered stars in the correct order to trace
    /// a forest constellation across the night sky.
    ///
    /// How it works:
    ///   • N stars are scattered on a dark sky panel, each labelled with a number.
    ///   • Player must tap star 1, then 2, then 3 … up to N in strict sequence.
    ///   • Tapping the correct next star draws a glowing line to the previous star
    ///     and lights the star gold (DiscoveryRuneGlow).
    ///   • Tapping the wrong star gives GrassDisturbDust + RecordMistake (no reset).
    ///   • All stars connected in order → JoyBurst + SolvePuzzle.
    ///
    /// Sprout  (4-6):  5 stars
    /// Scout  (7-11):  7 stars
    /// Druid (12-16):  9 stars
    ///
    /// Lines between stars are drawn as thin rotated RectTransforms (no LineRenderer needed).
    /// </summary>
    public class StarConstellationPuzzle : MonoBehaviour
    {
        // ─── State ───────────────────────────────────────────────────────────────

        private int           _starCount;
        private Vector2[]     _starPositions;   // anchored positions in the panel
        private int[]         _tapOrder;         // which logical star index is star 1, 2, etc.
        private int           _nextExpected;     // which sequence number to tap next (0-based)

        private Image[]       _starImages;
        private RectTransform[] _starRects;
        private Text[]        _starLabels;
        private List<GameObject> _lineSegments = new List<GameObject>();

        private RectTransform _panelRect;        // parent used to anchor lines

        // ─── Colors ───────────────────────────────────────────────────────────────

        private static readonly Color ColorSky      = new Color(0.05f, 0.08f, 0.20f, 1f);
        private static readonly Color ColorStarDim  = new Color(0.85f, 0.88f, 1.00f, 0.80f);
        private static readonly Color ColorStarLit  = new Color(1.00f, 0.92f, 0.35f, 1.00f);
        private static readonly Color ColorLine     = new Color(0.75f, 0.88f, 1.00f, 0.65f);

        private const float StarSize  = 52f;
        private const float LineThick = 4f;

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
            _manager    = manager;
            _particles  = particles;
            _panelRect  = parent;

            _starCount = tier == "druid" ? 9 : tier == "scout" ? 7 : 5;

            _starPositions = new Vector2[_starCount];
            _tapOrder      = new int[_starCount];
            _starImages    = new Image[_starCount];
            _starRects     = new RectTransform[_starCount];
            _starLabels    = new Text[_starCount];
            _nextExpected  = 0;

            BuildSkyBackground(parent);
            PlaceStars(parent);
            _manager.StartPuzzle(PuzzleType.StarConstellation, tier);
        }

        // ─── Input ───────────────────────────────────────────────────────────────

        private void HandleStarTapped(int starIndex)
        {
            // _tapOrder[i] == sequenceNumber assigned to that star (0-based)
            int sequenceNum = _tapOrder[starIndex];

            if (sequenceNum == _nextExpected)
            {
                // ── Correct ───────────────────────────────────────────────────────
                var pos = _starPositions[starIndex];

                // Light up star
                _starImages[starIndex].color = ColorStarLit;
                _starImages[starIndex].sprite = MakeStarSprite(true);
                _manager.RecordCorrectStep(pos);
                _particles?.Spawn(EmotionalParticleType.DiscoveryRuneGlow, pos, 4);

                // Draw a line from the previous star
                if (_nextExpected > 0)
                {
                    int prevStarIdx = IndexOfSequence(_nextExpected - 1);
                    if (prevStarIdx >= 0)
                        DrawLine(_starPositions[prevStarIdx], pos);
                }

                _nextExpected++;

                if (_nextExpected >= _starCount)
                {
                    _particles?.SpawnJoyBurst(pos);
                    _manager.SolvePuzzle(pos);
                    OnPuzzleEnd?.Invoke(true);
                }
            }
            else
            {
                // ── Wrong order ───────────────────────────────────────────────────
                var pos = _starPositions[starIndex];
                _particles?.Spawn(EmotionalParticleType.GrassDisturbDust, pos, 2);
                _manager.RecordMistake(pos);
            }
        }

        /// <summary>Returns the star array index whose sequence number equals <paramref name="seq"/>.</summary>
        private int IndexOfSequence(int seq)
        {
            for (int i = 0; i < _tapOrder.Length; i++)
                if (_tapOrder[i] == seq) return i;
            return -1;
        }

        // ─── UI Builder ───────────────────────────────────────────────────────────

        private void BuildSkyBackground(RectTransform parent)
        {
            var go  = new GameObject("SkyBg");
            go.transform.SetParent(parent, false);
            var rt  = go.AddComponent<RectTransform>();
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.sizeDelta = Vector2.zero;
            var img = go.AddComponent<Image>();
            img.color = ColorSky;

            // A handful of tiny background twinkle dots for atmosphere
            var rng = new System.Random(77);
            for (int i = 0; i < 28; i++)
            {
                float px = rng.Next(-200, 200);
                float py = rng.Next(-160, 160);
                SpawnTwinkle(parent, new Vector2(px, py), rng.Next(2, 5));
            }
        }

        private void PlaceStars(RectTransform parent)
        {
            var rng   = new System.Random(31 + _starCount);
            var taken = new List<Vector2>();

            // Build a shuffled sequence assignment (star i gets sequence _tapOrder[i])
            var seqList = new List<int>();
            for (int i = 0; i < _starCount; i++) seqList.Add(i);
            FisherYates(seqList, rng);
            for (int i = 0; i < _starCount; i++) _tapOrder[i] = seqList[i];

            // Panel usable area (leave margin from edges)
            const float marginX = 55f, marginY = 55f;
            const float panelW = 360f, panelH = 320f;   // approximate inner safe area

            for (int i = 0; i < _starCount; i++)
            {
                Vector2 pos = FindOpenPosition(rng, taken, marginX, marginY, panelW, panelH);
                taken.Add(pos);
                _starPositions[i] = pos;

                var rt  = MakeRect($"Star_{i}", parent, pos, new Vector2(StarSize, StarSize));
                var img = rt.gameObject.AddComponent<Image>();
                img.sprite = MakeStarSprite(false);
                img.color  = ColorStarDim;

                var btn = rt.gameObject.AddComponent<Button>();
                int si  = i;
                btn.onClick.AddListener(() => HandleStarTapped(si));

                // Number label (1-based display)
                var labelRt = MakeRect("Num", rt, new Vector2(0, -StarSize * 0.55f),
                    new Vector2(StarSize, 22f));
                var txt = labelRt.gameObject.AddComponent<Text>();
                txt.text      = (seqList.IndexOf(_tapOrder[i]) + 1).ToString();
                // Actually simpler: the display number = sequence+1
                txt.text      = (_tapOrder[i] + 1).ToString();
                txt.font      = ForestUiFactory.GetDefaultFont();
                txt.fontSize  = 16;
                txt.fontStyle = FontStyle.Bold;
                txt.color     = new Color(0.85f, 0.92f, 1f, 0.90f);
                txt.alignment = TextAnchor.MiddleCenter;

                _starImages[i] = img;
                _starRects[i]  = rt;
                _starLabels[i] = txt;
            }
        }

        private Vector2 FindOpenPosition(System.Random rng,
            List<Vector2> taken, float mx, float my, float w, float h)
        {
            const float minDist = 80f;
            for (int attempt = 0; attempt < 300; attempt++)
            {
                float px = rng.Next((int)-w / 2 + (int)mx, (int)w / 2 - (int)mx);
                float py = rng.Next((int)-h / 2 + (int)my, (int)h / 2 - (int)my);
                var   p  = new Vector2(px, py);
                bool  ok = true;
                foreach (var t in taken)
                    if (Vector2.Distance(p, t) < minDist) { ok = false; break; }
                if (ok) return p;
            }
            // Fallback grid position
            int n = taken.Count;
            return new Vector2(n * 70f - w / 2f + mx, 0f);
        }

        // ─── Line Drawing ─────────────────────────────────────────────────────────

        private void DrawLine(Vector2 a, Vector2 b)
        {
            var go = new GameObject("Line");
            go.transform.SetParent(_panelRect, false);
            _lineSegments.Add(go);

            var rt  = go.AddComponent<RectTransform>();
            var mid = (a + b) * 0.5f;
            var len = Vector2.Distance(a, b);
            var ang = Mathf.Atan2(b.y - a.y, b.x - a.x) * Mathf.Rad2Deg;

            rt.anchoredPosition = mid;
            rt.sizeDelta        = new Vector2(len, LineThick);
            rt.localRotation    = Quaternion.Euler(0, 0, ang);

            var img = go.AddComponent<Image>();
            img.color  = ColorLine;
            img.sprite = MakeLineSprite();

            // Push behind stars in sibling order
            go.transform.SetSiblingIndex(1);
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

        private void SpawnTwinkle(RectTransform parent, Vector2 pos, int radius)
        {
            var rt  = MakeRect("Twinkle", parent, pos, Vector2.one * radius * 2f);
            var img = rt.gameObject.AddComponent<Image>();
            img.sprite = MakeCircleSprite(radius);
            img.color  = new Color(0.90f, 0.93f, 1.00f, UnityEngine.Random.Range(0.25f, 0.55f));
        }

        // ─── Sprite Factories ─────────────────────────────────────────────────────

        private static Sprite MakeStarSprite(bool lit)
        {
            const int sz = 48;
            var tex    = new Texture2D(sz, sz, TextureFormat.RGBA32, false);
            var pixels = new Color[sz * sz];
            float cx = sz / 2f - 0.5f, cy = sz / 2f - 0.5f;
            float outerR = sz / 2f - 2f, innerR = outerR * 0.42f;
            const int points = 5;

            for (int y = 0; y < sz; y++)
                for (int x = 0; x < sz; x++)
                {
                    float dx = x - cx, dy = y - cy;
                    float dist = Mathf.Sqrt(dx * dx + dy * dy);
                    float angle = Mathf.Atan2(dy, dx);
                    // Compute star radius at this angle
                    float sector   = (2f * Mathf.PI) / points;
                    float halfSect = sector * 0.5f;
                    float norm     = ((angle % sector) + sector) % sector;
                    float t        = norm < halfSect ? norm / halfSect : (sector - norm) / halfSect;
                    float starR    = Mathf.Lerp(innerR, outerR, t);
                    float a        = Mathf.Clamp01(starR - dist + 1.5f);
                    pixels[y * sz + x] = new Color(1, 1, 1, a);
                }

            tex.SetPixels(pixels);
            tex.Apply();
            return Sprite.Create(tex, new Rect(0, 0, sz, sz), Vector2.one * 0.5f, sz);
        }

        private static Sprite MakeCircleSprite(int radius)
        {
            int sz  = radius * 2;
            if (sz < 2) sz = 2;
            float cx = sz * 0.5f - 0.5f, cy = sz * 0.5f - 0.5f, r = sz * 0.5f - 0.5f;
            var tex    = new Texture2D(sz, sz, TextureFormat.RGBA32, false);
            var pixels = new Color[sz * sz];
            for (int y = 0; y < sz; y++)
                for (int x = 0; x < sz; x++)
                {
                    float d = Mathf.Sqrt((x - cx) * (x - cx) + (y - cy) * (y - cy));
                    pixels[y * sz + x] = new Color(1, 1, 1, Mathf.Clamp01(r - d + 1f));
                }
            tex.SetPixels(pixels);
            tex.Apply();
            return Sprite.Create(tex, new Rect(0, 0, sz, sz), Vector2.one * 0.5f, sz);
        }

        private static Sprite MakeLineSprite()
        {
            var tex = new Texture2D(4, 4, TextureFormat.RGBA32, false);
            var px  = new Color[16];
            for (int i = 0; i < 16; i++) px[i] = Color.white;
            tex.SetPixels(px);
            tex.Apply();
            return Sprite.Create(tex, new Rect(0, 0, 4, 4), Vector2.one * 0.5f, 4);
        }
    }
}
