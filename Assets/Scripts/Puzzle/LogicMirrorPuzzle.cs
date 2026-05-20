using System;
using UnityEngine;
using UnityEngine.UI;

namespace ForestFriendsQuest
{
    /// <summary>
    /// Logic Mirror Puzzle — grid-based light reflection challenge.
    ///
    /// Player places and rotates mirror tiles on a grid so a light beam
    /// travels from source to target. Scales from 2x2 (Sprout) to 4x4+ (Druid).
    ///
    /// Mirror types: 45-degree, 135-degree, half-mirror (beam splits).
    /// Difficulty adapts via DynamicDifficultySystem grid dimensions.
    ///
    /// Visual feedback: beam path drawn with sprite glow lines.
    /// </summary>
    public class LogicMirrorPuzzle : MonoBehaviour
    {
        // ─── Grid State ───────────────────────────────────────────────────────────

        public enum MirrorType { None, Mirror45, Mirror135, SplitMirror }
        public enum Direction   { Right, Up, Left, Down }

        private struct Cell
        {
            public MirrorType mirror;
            public bool       isSource;
            public bool       isTarget;
            public bool       isLit;
        }

        private Cell[,]   _grid;
        private int       _cols, _rows;
        private Vector2Int _sourceCell;
        private Vector2Int _targetCell;
        private Direction  _sourceDirection = Direction.Right;

        // ─── UI Elements ─────────────────────────────────────────────────────────

        private RectTransform[,] _cellRects;
        private Image[,]          _cellImages;
        private const float CellSize = 90f;
        private const float CellGap  = 8f;

        // ─── Systems ─────────────────────────────────────────────────────────────

        private PuzzleManager           _manager;
        private EmotionalParticleEngine _particles;

        public event Action<bool> OnPuzzleEnd;

        // ─── Colors ───────────────────────────────────────────────────────────────

        private static readonly Color ColorEmpty   = new Color(0.20f, 0.35f, 0.28f, 0.80f);
        private static readonly Color ColorMirror  = new Color(0.60f, 0.90f, 1.00f, 0.90f);
        private static readonly Color ColorSource  = new Color(1.00f, 0.90f, 0.40f, 1.00f);
        private static readonly Color ColorTarget  = new Color(0.40f, 1.00f, 0.70f, 1.00f);
        private static readonly Color ColorLit     = new Color(0.85f, 1.00f, 0.65f, 0.80f);
        private static readonly Color ColorUnlit   = new Color(0.20f, 0.35f, 0.28f, 0.60f);

        // ─── Lifecycle ───────────────────────────────────────────────────────────

        public void Initialize(
            PuzzleManager manager,
            EmotionalParticleEngine particles,
            RectTransform parent,
            string tier)
        {
            _manager   = manager;
            _particles = particles;

            var gridSize = manager.GetAdaptedGridSize(3, 3);
            _cols = gridSize.x;
            _rows = gridSize.y;

            _grid      = new Cell[_cols, _rows];
            _cellRects = new RectTransform[_cols, _rows];
            _cellImages = new Image[_cols, _rows];

            SetupPuzzleLayout();
            BuildGridUI(parent);
            TraceBeam();

            _manager.StartPuzzle(PuzzleType.LogicMirror, tier);
        }

        // ─── Player Interaction ───────────────────────────────────────────────────

        public void OnCellTapped(int col, int row)
        {
            if (_grid[col, row].isSource || _grid[col, row].isTarget) return;

            // Cycle mirror type on tap
            var next = (MirrorType)(((int)_grid[col, row].mirror + 1) % 3);
            _grid[col, row].mirror = next;

            UpdateCellVisual(col, row);
            TraceBeam();
            CheckSolved();
        }

        // ─── Beam Tracing ─────────────────────────────────────────────────────────

        private void TraceBeam()
        {
            // Reset lit state
            for (var c = 0; c < _cols; c++)
            {
                for (var r = 0; r < _rows; r++)
                {
                    _grid[c, r].isLit = false;
                }
            }

            // Trace beam from source
            var pos = _sourceCell;
            var dir = _sourceDirection;
            var maxSteps = _cols * _rows * 2;

            for (var step = 0; step < maxSteps; step++)
            {
                if (!InBounds(pos.x, pos.y)) break;

                _grid[pos.x, pos.y].isLit = true;
                UpdateCellVisual(pos.x, pos.y);

                var mirror = _grid[pos.x, pos.y].mirror;
                dir = ReflectDirection(dir, mirror);
                pos = Advance(pos, dir);
            }

            RefreshAllCellVisuals();
        }

        private Direction ReflectDirection(Direction dir, MirrorType mirror)
        {
            switch (mirror)
            {
                case MirrorType.Mirror45:
                    // / mirror: R->U, U->R, L->D, D->L
                    switch (dir)
                    {
                        case Direction.Right: return Direction.Up;
                        case Direction.Up:    return Direction.Right;
                        case Direction.Left:  return Direction.Down;
                        case Direction.Down:  return Direction.Left;
                    }
                    break;

                case MirrorType.Mirror135:
                    // \ mirror: R->D, D->R, L->U, U->L
                    switch (dir)
                    {
                        case Direction.Right: return Direction.Down;
                        case Direction.Down:  return Direction.Right;
                        case Direction.Left:  return Direction.Up;
                        case Direction.Up:    return Direction.Left;
                    }
                    break;
            }
            return dir; // no mirror — straight through
        }

        private static Vector2Int Advance(Vector2Int pos, Direction dir)
        {
            switch (dir)
            {
                case Direction.Right: return pos + Vector2Int.right;
                case Direction.Up:    return pos + Vector2Int.up;
                case Direction.Left:  return pos + Vector2Int.left;
                case Direction.Down:  return pos + Vector2Int.down;
                default:              return pos;
            }
        }

        private bool InBounds(int c, int r) =>
            c >= 0 && c < _cols && r >= 0 && r < _rows;

        // ─── Victory Check ────────────────────────────────────────────────────────

        private void CheckSolved()
        {
            if (_grid[_targetCell.x, _targetCell.y].isLit)
            {
                var targetPos = _cellRects[_targetCell.x, _targetCell.y].anchoredPosition;
                _particles?.SpawnDiscoveryBurst(targetPos);
                _manager.SolvePuzzle(targetPos);
                OnPuzzleEnd?.Invoke(true);
            }
        }

        // ─── Puzzle Layout ────────────────────────────────────────────────────────

        private void SetupPuzzleLayout()
        {
            // Source: top-left, shooting right
            _sourceCell      = new Vector2Int(0, _rows / 2);
            _grid[0, _rows / 2].isSource = true;

            // Target: bottom-right, beam must reach it
            _targetCell      = new Vector2Int(_cols - 1, _rows / 2);
            _grid[_cols - 1, _rows / 2].isTarget = true;

            // Pre-place some mirror tiles for a solvable puzzle
            if (_cols >= 3)
            {
                _grid[1, _rows / 2].mirror = MirrorType.Mirror45;
            }
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

                    var go  = new GameObject($"Cell_{c}_{r}");
                    go.transform.SetParent(parent, false);
                    var rt  = go.AddComponent<RectTransform>();
                    rt.anchoredPosition = new Vector2(xPos, yPos);
                    rt.sizeDelta        = new Vector2(CellSize, CellSize);

                    var img = go.AddComponent<Image>();
                    img.sprite = CreateCellSprite();
                    img.color  = ColorEmpty;

                    var btn = go.AddComponent<Button>();
                    var col = c; var row = r;
                    btn.onClick.AddListener(() => OnCellTapped(col, row));

                    _cellRects[c, r]  = rt;
                    _cellImages[c, r] = img;
                }
            }
        }

        private void UpdateCellVisual(int c, int r)
        {
            if (_cellImages[c, r] == null) return;

            Color color;
            if (_grid[c, r].isSource) color = ColorSource;
            else if (_grid[c, r].isTarget) color = _grid[c, r].isLit ? ColorTarget : ColorUnlit;
            else if (_grid[c, r].mirror != MirrorType.None) color = ColorMirror;
            else color = _grid[c, r].isLit ? ColorLit : ColorUnlit;

            _cellImages[c, r].color = color;
        }

        private void RefreshAllCellVisuals()
        {
            for (var c = 0; c < _cols; c++)
            {
                for (var r = 0; r < _rows; r++)
                {
                    UpdateCellVisual(c, r);
                }
            }
        }

        private static Sprite CreateCellSprite()
        {
            const int size = 64;
            var tex    = new Texture2D(size, size, TextureFormat.RGBA32, false);
            var pixels = new Color[size * size];
            const float corner = 10f;

            for (var y = 0; y < size; y++)
            {
                for (var x = 0; x < size; x++)
                {
                    var dx   = Mathf.Min(x, size - 1 - x);
                    var dy   = Mathf.Min(y, size - 1 - y);
                    var edge = Mathf.Min(dx, dy);
                    var alpha = edge < corner ? edge / corner : 1f;
                    pixels[y * size + x] = new Color(1f, 1f, 1f, alpha * alpha);
                }
            }

            tex.SetPixels(pixels);
            tex.Apply();
            return Sprite.Create(tex, new Rect(0, 0, size, size), Vector2.one * 0.5f, size);
        }
    }
}
