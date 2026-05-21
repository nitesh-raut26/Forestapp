using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace ForestFriendsQuest
{
    /// <summary>
    /// Forest Routing Puzzle — draw a path from the forest entrance to the
    /// sanctuary gate while collecting all required waypoints (glowing crystals).
    ///
    /// How it works:
    ///   • A grid is shown with a Start cell (green) and a Goal cell (gold).
    ///   • Crystal waypoints are scattered on the grid — ALL must be collected.
    ///   • Thorn cells (red) are obstacles — tapping one counts as a mistake
    ///     and clears the current path back to the last waypoint.
    ///   • Player taps adjacent cells in sequence to draw the path.
    ///   • Reaching the goal with all crystals collected → SOLVED.
    ///
    /// Sprout  (4-6):  3×4 grid, 2 crystals, no thorns
    /// Scout  (7-11):  4×5 grid, 3 crystals, 2 thorns
    /// Druid (12-16):  5×6 grid, 4 crystals, 4 thorns, must find optimal route
    ///
    /// Visual feedback:
    ///   Path drawn     → tile turns green + HappyPollenBurst
    ///   Crystal picked → DiscoveryRuneGlow burst
    ///   Thorn hit      → GrassDisturbDust + path resets to last safe waypoint
    ///   Goal reached   → JoyBurst + SolvePuzzle
    /// </summary>
    public class ForestRoutingPuzzle : MonoBehaviour
    {
        // ─── Cell Types ───────────────────────────────────────────────────────────

        private enum CellType { Empty, Start, Goal, Crystal, Thorn, Path }

        private struct GridCell
        {
            public CellType type;
            public bool     collected;    // for crystals
            public bool     onPath;
        }

        // ─── State ───────────────────────────────────────────────────────────────

        private GridCell[,]        _grid;
        private int                _cols, _rows;
        private Vector2Int         _start, _goal;
        private List<Vector2Int>   _crystals     = new List<Vector2Int>();
        private List<Vector2Int>   _playerPath   = new List<Vector2Int>();
        private int                _collectedCount;
        private int                _totalCrystals;

        // ─── UI ──────────────────────────────────────────────────────────────────

        private Image[,]         _cellImages;
        private RectTransform[,] _cellRects;
        private const float      CellSize = 78f;
        private const float      CellGap  = 4f;

        private static readonly Color ColorEmpty    = new Color(0.18f, 0.32f, 0.22f, 0.75f);
        private static readonly Color ColorStart    = new Color(0.35f, 0.88f, 0.50f, 1.00f);
        private static readonly Color ColorGoal     = new Color(1.00f, 0.85f, 0.30f, 1.00f);
        private static readonly Color ColorCrystal  = new Color(0.50f, 0.80f, 1.00f, 1.00f);
        private static readonly Color ColorThorn    = new Color(0.85f, 0.25f, 0.25f, 0.90f);
        private static readonly Color ColorPath     = new Color(0.40f, 0.90f, 0.55f, 0.85f);
        private static readonly Color ColorCollected = new Color(0.45f, 0.55f, 0.45f, 0.70f);

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

            var sizeV  = manager.GetAdaptedGridSize(3, 4);
            _cols      = sizeV.x;
            _rows      = sizeV.y;

            _grid       = new GridCell[_cols, _rows];
            _cellImages = new Image[_cols, _rows];
            _cellRects  = new RectTransform[_cols, _rows];

            BuildLayout(tier);
            BuildGridUI(parent);
            PlacePlayer();

            _manager.StartPuzzle(PuzzleType.ForestRouting, tier);
        }

        // ─── Player Input ─────────────────────────────────────────────────────────

        public void OnCellTapped(int col, int row)
        {
            if (!InBounds(col, row)) return;

            var tappedPos  = new Vector2Int(col, row);
            var playerPos  = _playerPath.Count > 0
                ? _playerPath[_playerPath.Count - 1]
                : _start;

            // Must be adjacent (no diagonal)
            if (!IsAdjacent(playerPos, tappedPos)) return;
            if (tappedPos == playerPos) return;

            var cell = _grid[col, row];
            var canvasPos = CellCenter(tappedPos);

            // ── Thorn hit ────────────────────────────────────────────────────────
            if (cell.type == CellType.Thorn)
            {
                _particles?.Spawn(EmotionalParticleType.GrassDisturbDust, canvasPos, 3);
                _manager.RecordMistake(canvasPos);
                // Roll back path to the last collected waypoint (or start)
                RollbackToLastWaypoint();
                RefreshAllVisuals();
                return;
            }

            // ── Backtrack (tap the previous cell to undo) ────────────────────────
            if (_playerPath.Count >= 2 && tappedPos == _playerPath[_playerPath.Count - 2])
            {
                var last = _playerPath[_playerPath.Count - 1];
                _grid[last.x, last.y].onPath = false;
                if (_grid[last.x, last.y].type == CellType.Crystal && _grid[last.x, last.y].collected)
                {
                    _grid[last.x, last.y].collected = false;
                    _collectedCount--;
                }
                _playerPath.RemoveAt(_playerPath.Count - 1);
                RefreshAllVisuals();
                return;
            }

            // ── Normal move ───────────────────────────────────────────────────────
            _grid[col, row].onPath = true;
            _playerPath.Add(tappedPos);

            // Crystal collected
            if (cell.type == CellType.Crystal && !_grid[col, row].collected)
            {
                _grid[col, row].collected = true;
                _collectedCount++;
                _particles?.Spawn(EmotionalParticleType.DiscoveryRuneGlow, canvasPos, 5);
            }

            _manager.RecordCorrectStep(canvasPos);
            RefreshAllVisuals();

            // ── Reached Goal ─────────────────────────────────────────────────────
            if (cell.type == CellType.Goal && _collectedCount >= _totalCrystals)
            {
                _particles?.SpawnJoyBurst(canvasPos);
                _manager.SolvePuzzle(canvasPos);
                OnPuzzleEnd?.Invoke(true);
            }
            else if (cell.type == CellType.Goal && _collectedCount < _totalCrystals)
            {
                // Reached goal but missed crystals — gentle nudge, don't fail
                _particles?.Spawn(EmotionalParticleType.GrassDisturbDust, canvasPos, 2);
                _manager.RecordMistake(canvasPos);
            }
        }

        // ─── Path Logic ───────────────────────────────────────────────────────────

        private void PlacePlayer()
        {
            _playerPath.Clear();
            _playerPath.Add(_start);
            _grid[_start.x, _start.y].onPath = true;
        }

        private void RollbackToLastWaypoint()
        {
            // Remove cells from path until we find a crystal or reach start
            while (_playerPath.Count > 1)
            {
                var last = _playerPath[_playerPath.Count - 1];
                _grid[last.x, last.y].onPath = false;
                _playerPath.RemoveAt(_playerPath.Count - 1);

                if (_grid[last.x, last.y].type == CellType.Crystal
                    && _grid[last.x, last.y].collected)
                    break; // stop at last collected crystal
            }
        }

        private static bool IsAdjacent(Vector2Int a, Vector2Int b)
        {
            var d = a - b;
            return (Mathf.Abs(d.x) + Mathf.Abs(d.y)) == 1;
        }

        // ─── Layout Builder ───────────────────────────────────────────────────────

        private void BuildLayout(string tier)
        {
            int crystalCount = tier == "druid" ? 4 : tier == "scout" ? 3 : 2;
            int thornCount   = tier == "druid" ? 4 : tier == "scout" ? 2 : 0;
            _totalCrystals   = crystalCount;

            // Start at top-left, goal at bottom-right
            _start = new Vector2Int(0, _rows - 1);
            _goal  = new Vector2Int(_cols - 1, 0);

            _grid[_start.x, _start.y].type = CellType.Start;
            _grid[_goal.x,  _goal.y ].type = CellType.Goal;

            var rng  = new System.Random(42);
            var used = new HashSet<int>
            {
                _start.x * _rows + _start.y,
                _goal.x  * _rows + _goal.y
            };

            // Scatter crystals
            for (var i = 0; i < crystalCount; i++)
            {
                var pos = RandomEmpty(rng, used);
                _grid[pos.x, pos.y].type = CellType.Crystal;
                _crystals.Add(pos);
                used.Add(pos.x * _rows + pos.y);
            }

            // Scatter thorns (not on guaranteed path)
            for (var i = 0; i < thornCount; i++)
            {
                var pos = RandomEmpty(rng, used);
                _grid[pos.x, pos.y].type = CellType.Thorn;
                used.Add(pos.x * _rows + pos.y);
            }
        }

        private Vector2Int RandomEmpty(System.Random rng, HashSet<int> used)
        {
            for (var attempt = 0; attempt < 200; attempt++)
            {
                var c   = rng.Next(0, _cols);
                var r   = rng.Next(0, _rows);
                var key = c * _rows + r;
                if (!used.Contains(key) && _grid[c, r].type == CellType.Empty)
                    return new Vector2Int(c, r);
            }
            return new Vector2Int(_cols / 2, _rows / 2);
        }

        // ─── UI Builder ───────────────────────────────────────────────────────────

        private void BuildGridUI(RectTransform parent)
        {
            var totalW = _cols * (CellSize + CellGap) - CellGap;
            var totalH = _rows * (CellSize + CellGap) - CellGap;

            for (var c = 0; c < _cols; c++)
            {
                for (var r = 0; r < _rows; r++)
                {
                    var xPos = c * (CellSize + CellGap) - totalW / 2f + CellSize / 2f;
                    var yPos = r * (CellSize + CellGap) - totalH / 2f + CellSize / 2f;

                    var go  = new GameObject($"FR_{c}_{r}");
                    go.transform.SetParent(parent, false);

                    var rt  = go.AddComponent<RectTransform>();
                    rt.anchoredPosition = new Vector2(xPos, yPos);
                    rt.sizeDelta        = new Vector2(CellSize, CellSize);

                    var img = go.AddComponent<Image>();
                    img.sprite = CreateCellSprite();
                    img.color  = CellColor(c, r);

                    var btn = go.AddComponent<Button>();
                    var col = c; var row = r;
                    btn.onClick.AddListener(() => OnCellTapped(col, row));

                    _cellRects[c, r]  = rt;
                    _cellImages[c, r] = img;

                    // Icon label for special cells
                    AddCellLabel(go.transform as RectTransform, c, r);
                }
            }
        }

        private void AddCellLabel(RectTransform parent, int c, int r)
        {
            var type = _grid[c, r].type;
            string label = type switch
            {
                CellType.Start   => "S",
                CellType.Goal    => "G",
                CellType.Crystal => "*",
                CellType.Thorn   => "X",
                _                => ""
            };
            if (string.IsNullOrEmpty(label)) return;

            var go  = new GameObject("Label");
            go.transform.SetParent(parent, false);
            var rt  = go.AddComponent<RectTransform>();
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.sizeDelta = Vector2.zero;
            var txt = go.AddComponent<Text>();
            txt.text      = label;
            txt.font      = ForestUiFactory.GetDefaultFont();
            txt.fontSize  = 26;
            txt.fontStyle = FontStyle.Bold;
            txt.color     = Color.white;
            txt.alignment = TextAnchor.MiddleCenter;
        }

        private Color CellColor(int c, int r)
        {
            return _grid[c, r].type switch
            {
                CellType.Start   => ColorStart,
                CellType.Goal    => ColorGoal,
                CellType.Crystal => ColorCrystal,
                CellType.Thorn   => ColorThorn,
                _                => ColorEmpty
            };
        }

        private void RefreshAllVisuals()
        {
            for (var c = 0; c < _cols; c++)
                for (var r = 0; r < _rows; r++)
                {
                    if (_cellImages[c, r] == null) continue;
                    var cell = _grid[c, r];

                    if (cell.type == CellType.Crystal && cell.collected)
                        _cellImages[c, r].color = ColorCollected;
                    else if (cell.onPath && cell.type == CellType.Empty)
                        _cellImages[c, r].color = ColorPath;
                    else
                        _cellImages[c, r].color = CellColor(c, r);
                }
        }

        // ─── Helpers ─────────────────────────────────────────────────────────────

        private bool InBounds(int c, int r) => c >= 0 && c < _cols && r >= 0 && r < _rows;

        private Vector2 CellCenter(Vector2Int pos)
        {
            if (_cellRects == null || !InBounds(pos.x, pos.y)) return Vector2.zero;
            return _cellRects[pos.x, pos.y].anchoredPosition;
        }

        private static Sprite CreateCellSprite()
        {
            const int size = 48;
            const float corner = 8f;
            var tex    = new Texture2D(size, size, TextureFormat.RGBA32, false);
            var pixels = new Color[size * size];
            for (var y = 0; y < size; y++)
                for (var x = 0; x < size; x++)
                {
                    var dx = Mathf.Min(x, size - 1 - x);
                    var dy = Mathf.Min(y, size - 1 - y);
                    pixels[y * size + x] = new Color(1f, 1f, 1f,
                        Mathf.Clamp01(Mathf.Min(dx, dy) / corner));
                }
            tex.SetPixels(pixels);
            tex.Apply();
            return Sprite.Create(tex, new Rect(0, 0, size, size), Vector2.one * 0.5f, size);
        }
    }
}
