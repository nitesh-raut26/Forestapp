using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace ForestFriendsQuest
{
    /// <summary>
    /// Pressure Gate Puzzle — navigate a forest creature through a grid maze,
    /// stepping on all pressure plates to open gates and reach the exit.
    ///
    /// Tile types: Open, Wall, Plate (unactivated), PlateOn, Gate (closed),
    ///             GateOpen, Player, Goal.
    ///
    /// Sprout (4-6):  3x3 grid, 1 plate, path highlighted
    /// Scout (7-11):  4x4 grid, 2 plates
    /// Druid (12-16): 5x5 grid, 3 plates, maze walls
    ///
    /// Visual feedback:
    ///   - Plate activated: HappyPollenBurst particles
    ///   - Gate opens: DiscoveryRuneGlow burst
    ///   - Puzzle solved: JoyBurst + SolvePuzzle event
    /// </summary>
    public class PressureGatePuzzle : MonoBehaviour
    {
        // ─── Tile Types ───────────────────────────────────────────────────────────

        private enum TileType { Open, Wall, PlateOff, PlateOn, GateClosed, GateOpen, Start, Goal }

        private struct GridCell
        {
            public TileType type;
            public int      gateGroupId;   // plates with same groupId open gates with same groupId
        }

        // ─── Grid State ───────────────────────────────────────────────────────────

        private GridCell[,] _grid;
        private int         _cols, _rows;
        private Vector2Int  _playerPos;
        private int         _platesRemaining;

        // ─── UI ──────────────────────────────────────────────────────────────────

        private Image[,]         _cellImages;
        private RectTransform[,] _cellRects;
        private Image            _playerImage;
        private const float      CellSize = 80f;
        private const float      CellGap  = 6f;

        // ─── Colors ───────────────────────────────────────────────────────────────

        private static readonly Color ColorOpen       = new Color(0.22f, 0.38f, 0.28f, 0.70f);
        private static readonly Color ColorWall       = new Color(0.15f, 0.22f, 0.18f, 0.95f);
        private static readonly Color ColorPlateOff   = new Color(0.75f, 0.60f, 0.30f, 0.85f);
        private static readonly Color ColorPlateOn    = new Color(0.95f, 0.90f, 0.40f, 1.00f);
        private static readonly Color ColorGateClosed = new Color(0.60f, 0.25f, 0.20f, 0.90f);
        private static readonly Color ColorGateOpen   = new Color(0.30f, 0.70f, 0.50f, 0.60f);
        private static readonly Color ColorGoal       = new Color(0.40f, 1.00f, 0.65f, 1.00f);
        private static readonly Color ColorPlayer     = new Color(0.90f, 0.75f, 0.35f, 1.00f);

        // ─── Systems ─────────────────────────────────────────────────────────────

        private PuzzleManager           _manager;
        private EmotionalParticleEngine _particles;

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

            var gridSize = manager.GetAdaptedGridSize(3, 3);
            _cols = gridSize.x;
            _rows = gridSize.y;

            _grid       = new GridCell[_cols, _rows];
            _cellImages = new Image[_cols, _rows];
            _cellRects  = new RectTransform[_cols, _rows];

            BuildLayout(tier);
            BuildGridUI(parent);
            SpawnPlayerToken(parent);
            RefreshAllCells();

            _manager.StartPuzzle(PuzzleType.PressureGate, tier);
        }

        // ─── Player Movement ──────────────────────────────────────────────────────

        /// <summary>Attempt to move the player to a neighbouring cell.</summary>
        public void TryMove(Vector2Int delta)
        {
            var next = _playerPos + delta;
            if (!InBounds(next.x, next.y))         return;
            if (_grid[next.x, next.y].type == TileType.Wall) return;
            if (_grid[next.x, next.y].type == TileType.GateClosed) return;

            _playerPos = next;
            PlacePlayerToken();

            var cell = _grid[next.x, next.y];

            if (cell.type == TileType.PlateOff)
            {
                ActivatePlate(next);
            }
            else if (cell.type == TileType.Goal)
            {
                if (_platesRemaining == 0)
                    Solved();
                else
                    _manager.RecordMistake(CellCenter(next));  // missed plates — soft nudge
            }
        }

        // ─── DPAD Helpers ─────────────────────────────────────────────────────────

        public void MoveUp()    => TryMove(Vector2Int.up);
        public void MoveDown()  => TryMove(Vector2Int.down);
        public void MoveLeft()  => TryMove(Vector2Int.left);
        public void MoveRight() => TryMove(Vector2Int.right);

        // ─── Plate Activation ─────────────────────────────────────────────────────

        private void ActivatePlate(Vector2Int pos)
        {
            var groupId = _grid[pos.x, pos.y].gateGroupId;
            _grid[pos.x, pos.y].type = TileType.PlateOn;
            _platesRemaining--;

            _particles?.Spawn(EmotionalParticleType.HappyPollenBurst, CellCenter(pos), 6);
            _manager.RecordCorrectStep(CellCenter(pos));
            UpdateCellVisual(pos.x, pos.y);

            // Open linked gates
            bool anyGateOpened = false;
            for (var c = 0; c < _cols; c++)
            {
                for (var r = 0; r < _rows; r++)
                {
                    if (_grid[c, r].type == TileType.GateClosed &&
                        _grid[c, r].gateGroupId == groupId)
                    {
                        _grid[c, r].type = TileType.GateOpen;
                        _particles?.Spawn(EmotionalParticleType.DiscoveryRuneGlow,
                            CellCenter(new Vector2Int(c, r)), 4);
                        UpdateCellVisual(c, r);
                        anyGateOpened = true;
                    }
                }
            }

            if (anyGateOpened)
                _manager.RecordCorrectStep(CellCenter(pos));
        }

        // ─── Victory ─────────────────────────────────────────────────────────────

        private void Solved()
        {
            _particles?.SpawnJoyBurst(CellCenter(_playerPos));
            _manager.SolvePuzzle(CellCenter(_playerPos));
            OnPuzzleEnd?.Invoke(true);
        }

        // ─── Layout Builder ───────────────────────────────────────────────────────

        private void BuildLayout(string tier)
        {
            // Fill open
            for (var c = 0; c < _cols; c++)
                for (var r = 0; r < _rows; r++)
                    _grid[c, r] = new GridCell { type = TileType.Open, gateGroupId = -1 };

            // Start bottom-left, Goal top-right
            _playerPos = new Vector2Int(0, 0);
            _grid[0, 0].type = TileType.Start;
            _grid[_cols - 1, _rows - 1].type = TileType.Goal;

            int plateCount = tier == "druid" ? 3 : (tier == "scout" ? 2 : 1);
            _platesRemaining = plateCount;

            // Scatter plates and matching gates in a solvable arrangement
            int groupId = 0;
            var usedCells = new HashSet<int>();
            usedCells.Add(0);
            usedCells.Add((_cols - 1) * _rows + (_rows - 1));

            var rng = new System.Random(42);  // deterministic layout

            for (var p = 0; p < plateCount; p++, groupId++)
            {
                // Place plate
                var platePos = RandomFreeCell(rng, usedCells);
                _grid[platePos.x, platePos.y] = new GridCell
                    { type = TileType.PlateOff, gateGroupId = groupId };
                usedCells.Add(platePos.x * _rows + platePos.y);

                // Place gate that blocks path until plate activated
                var gatePos = RandomFreeCell(rng, usedCells);
                _grid[gatePos.x, gatePos.y] = new GridCell
                    { type = TileType.GateClosed, gateGroupId = groupId };
                usedCells.Add(gatePos.x * _rows + gatePos.y);
            }

            // Add walls only for larger grids to create maze feel
            if (_cols >= 4)
            {
                for (var w = 0; w < _cols; w++)
                {
                    var wallPos = RandomFreeCell(rng, usedCells);
                    _grid[wallPos.x, wallPos.y] = new GridCell
                        { type = TileType.Wall, gateGroupId = -1 };
                    usedCells.Add(wallPos.x * _rows + wallPos.y);
                }
            }
        }

        private Vector2Int RandomFreeCell(System.Random rng, HashSet<int> used)
        {
            for (var attempt = 0; attempt < 100; attempt++)
            {
                var c = rng.Next(0, _cols);
                var r = rng.Next(0, _rows);
                var key = c * _rows + r;
                if (!used.Contains(key))
                    return new Vector2Int(c, r);
            }
            return new Vector2Int(1, 1);
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

                    var go = new GameObject($"PGCell_{c}_{r}");
                    go.transform.SetParent(parent, false);

                    var rt = go.AddComponent<RectTransform>();
                    rt.anchoredPosition = new Vector2(xPos, yPos);
                    rt.sizeDelta        = new Vector2(CellSize, CellSize);

                    var img = go.AddComponent<Image>();
                    img.sprite = CreateRoundedSquareSprite();
                    img.color  = ColorOpen;

                    // Non-wall cells get tap buttons for movement hint in Sprout
                    var btn = go.AddComponent<Button>();
                    var col = c; var row = r;
                    btn.onClick.AddListener(() => TapNavigate(col, row));

                    _cellRects[c, r]  = rt;
                    _cellImages[c, r] = img;
                }
            }
        }

        private void SpawnPlayerToken(RectTransform parent)
        {
            var go = new GameObject("PlayerToken");
            go.transform.SetParent(parent, false);

            var rt = go.AddComponent<RectTransform>();
            rt.sizeDelta = new Vector2(CellSize * 0.65f, CellSize * 0.65f);

            var img = go.AddComponent<Image>();
            img.sprite = CreateCircleSprite();
            img.color  = ColorPlayer;

            _playerImage = img;
            PlacePlayerToken();
        }

        private void PlacePlayerToken()
        {
            if (_playerImage == null) return;
            var rt  = _playerImage.rectTransform;
            rt.anchoredPosition = CellCenter(_playerPos);
        }

        private void TapNavigate(int col, int row)
        {
            var delta = new Vector2Int(col, row) - _playerPos;
            // Only accept adjacent moves
            if (Mathf.Abs(delta.x) + Mathf.Abs(delta.y) == 1)
                TryMove(delta);
        }

        // ─── Visual Refresh ───────────────────────────────────────────────────────

        private void RefreshAllCells()
        {
            for (var c = 0; c < _cols; c++)
                for (var r = 0; r < _rows; r++)
                    UpdateCellVisual(c, r);
        }

        private void UpdateCellVisual(int c, int r)
        {
            if (_cellImages[c, r] == null) return;
            _cellImages[c, r].color = TileColor(_grid[c, r].type);
        }

        private static Color TileColor(TileType t)
        {
            switch (t)
            {
                case TileType.Wall:        return ColorWall;
                case TileType.PlateOff:    return ColorPlateOff;
                case TileType.PlateOn:     return ColorPlateOn;
                case TileType.GateClosed:  return ColorGateClosed;
                case TileType.GateOpen:    return ColorGateOpen;
                case TileType.Goal:        return ColorGoal;
                default:                   return ColorOpen;
            }
        }

        // ─── Helpers ─────────────────────────────────────────────────────────────

        private bool InBounds(int c, int r) =>
            c >= 0 && c < _cols && r >= 0 && r < _rows;

        private Vector2 CellCenter(Vector2Int pos)
        {
            if (_cellRects == null || !InBounds(pos.x, pos.y)) return Vector2.zero;
            return _cellRects[pos.x, pos.y].anchoredPosition;
        }

        // ─── Sprite Factories ─────────────────────────────────────────────────────

        private static Sprite CreateRoundedSquareSprite()
        {
            const int size = 64;
            const float corner = 10f;
            var tex    = new Texture2D(size, size, TextureFormat.RGBA32, false);
            var pixels = new Color[size * size];

            for (var y = 0; y < size; y++)
            {
                for (var x = 0; x < size; x++)
                {
                    var dx    = Mathf.Min(x, size - 1 - x);
                    var dy    = Mathf.Min(y, size - 1 - y);
                    var edge  = Mathf.Min(dx, dy);
                    var alpha = Mathf.Clamp01(edge / corner);
                    pixels[y * size + x] = new Color(1f, 1f, 1f, alpha);
                }
            }

            tex.SetPixels(pixels);
            tex.Apply();
            return Sprite.Create(tex, new Rect(0, 0, size, size), Vector2.one * 0.5f, size);
        }

        private static Sprite CreateCircleSprite()
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
