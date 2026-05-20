using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace ForestFriendsQuest
{
    /// <summary>
    /// Light Reflection Puzzle — rotate fixed-position mirrors so beams from
    /// one or more forest lanterns hit all crystal targets.
    ///
    /// Differences from LogicMirrorPuzzle:
    ///   - Multiple simultaneous light sources (1–3 by tier)
    ///   - Multiple crystal targets that ALL must be lit at once
    ///   - Mirrors occupy fixed cells; player only rotates them (4 orientations)
    ///   - Beam splitting: a cross mirror lets light pass through in two directions
    ///   - Beams rendered as a sprite tint overlay, not UI lines
    ///
    /// Sprout (4-6):  3x3, 1 lantern, 1 crystal, 2 mirrors
    /// Scout (7-11):  4x4, 2 lanterns, 2 crystals, 3 mirrors
    /// Druid (12-16): 5x5, 3 lanterns, 3 crystals, 5 mirrors, cross-mirrors available
    ///
    /// Visual feedback:
    ///   - Mirror rotated: DiscoveryRuneGlow particles
    ///   - Crystal lit: HappyGoldenWisp burst
    ///   - All crystals lit → solved: JoyBurst + SolvePuzzle
    /// </summary>
    public class LightReflectionPuzzle : MonoBehaviour
    {
        // ─── Mirror & Cell Data ───────────────────────────────────────────────────

        public enum MirrorAngle { Deg45, Deg135, Deg225, Deg315 }
        public enum Direction    { Right = 0, Up = 1, Left = 2, Down = 3 }

        private enum CellRole { Empty, Mirror, Lantern, Crystal, Wall }

        private struct BeamCell
        {
            public CellRole    role;
            public MirrorAngle mirrorAngle;
            public bool        isCross;      // cross-mirror: pass-through in both axes
            public bool        isLit;        // currently illuminated
            public int         litCount;     // beams passing through (for cross)
        }

        // ─── State ───────────────────────────────────────────────────────────────

        private BeamCell[,]    _grid;
        private int            _cols, _rows;
        private int            _totalCrystals;
        private int            _litCrystals;

        private List<Vector2Int>  _lanterns = new List<Vector2Int>();
        private List<Vector2Int>  _crystals = new List<Vector2Int>();
        private List<Vector2Int>  _mirrors  = new List<Vector2Int>();

        // ─── UI ──────────────────────────────────────────────────────────────────

        private Image[,]         _cellImages;
        private RectTransform[,] _cellRects;
        private Image[,]         _beamOverlay;   // dim glow overlay per cell
        private const float      CellSize = 82f;
        private const float      CellGap  = 6f;

        // ─── Colors ───────────────────────────────────────────────────────────────

        private static readonly Color ColorEmpty       = new Color(0.18f, 0.30f, 0.22f, 0.70f);
        private static readonly Color ColorWall        = new Color(0.12f, 0.18f, 0.14f, 0.95f);
        private static readonly Color ColorLantern     = new Color(1.00f, 0.92f, 0.40f, 1.00f);
        private static readonly Color ColorCrystalOff  = new Color(0.40f, 0.55f, 0.80f, 0.70f);
        private static readonly Color ColorCrystalOn   = new Color(0.55f, 1.00f, 0.95f, 1.00f);
        private static readonly Color ColorMirror      = new Color(0.70f, 0.92f, 1.00f, 0.88f);
        private static readonly Color ColorBeamOverlay = new Color(1.00f, 0.98f, 0.55f, 0.22f);
        private static readonly Color ColorCross       = new Color(0.80f, 1.00f, 0.80f, 0.88f);

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

            var size = manager.GetAdaptedGridSize(3, 3);
            _cols = size.x;
            _rows = size.y;

            _grid        = new BeamCell[_cols, _rows];
            _cellImages  = new Image[_cols, _rows];
            _cellRects   = new RectTransform[_cols, _rows];
            _beamOverlay = new Image[_cols, _rows];

            BuildLayout(tier);
            BuildGridUI(parent);
            TraceAllBeams();

            _manager.StartPuzzle(PuzzleType.LightReflection, tier);
        }

        // ─── Player Input ─────────────────────────────────────────────────────────

        public void OnCellTapped(int col, int row)
        {
            if (!InBounds(col, row)) return;
            if (_grid[col, row].role != CellRole.Mirror) return;

            // Rotate mirror clockwise through 4 orientations
            var cur = (int)_grid[col, row].mirrorAngle;
            _grid[col, row].mirrorAngle = (MirrorAngle)((cur + 1) % 4);

            _particles?.Spawn(EmotionalParticleType.DiscoveryRuneGlow,
                CellCenter(new Vector2Int(col, row)), 2);

            TraceAllBeams();

            if (_litCrystals == _totalCrystals)
                OnAllCrystalsLit();
        }

        // ─── Beam Tracing ─────────────────────────────────────────────────────────

        private void TraceAllBeams()
        {
            // Reset lit state
            _litCrystals = 0;
            for (var c = 0; c < _cols; c++)
                for (var r = 0; r < _rows; r++)
                {
                    _grid[c, r].isLit   = false;
                    _grid[c, r].litCount = 0;
                }

            // Trace one beam per lantern
            foreach (var lantern in _lanterns)
                TraceBeam(lantern, Direction.Right);

            // Count lit crystals
            foreach (var crystal in _crystals)
            {
                if (_grid[crystal.x, crystal.y].isLit)
                    _litCrystals++;
            }

            RefreshAllVisuals();
        }

        private void TraceBeam(Vector2Int start, Direction dir)
        {
            var pos      = start;
            var maxSteps = _cols * _rows * 2;

            for (var step = 0; step < maxSteps; step++)
            {
                pos = Advance(pos, dir);
                if (!InBounds(pos.x, pos.y)) break;

                ref var cell = ref _grid[pos.x, pos.y];

                if (cell.role == CellRole.Wall) break;

                cell.isLit = true;
                cell.litCount++;

                if (cell.role == CellRole.Crystal) break;

                if (cell.role == CellRole.Mirror)
                {
                    if (cell.isCross)
                    {
                        // Pass-through in original axis, also reflect
                        var reflected = ReflectDir(dir, cell.mirrorAngle);
                        if (reflected != dir && reflected != OppositeDir(dir))
                            TraceBeam(pos, reflected);   // spawn reflected sub-beam
                        // continue straight (don't break)
                    }
                    else
                    {
                        dir = ReflectDir(dir, cell.mirrorAngle);
                    }
                }
            }
        }

        private static Direction ReflectDir(Direction dir, MirrorAngle angle)
        {
            // Mirror at 45°  (/): R->U, U->R, L->D, D->L
            // Mirror at 135° (\): R->D, D->R, L->U, U->L
            // 225/315 are the same physical mirrors rotated 180° — identical reflection
            bool isFwd = (angle == MirrorAngle.Deg45 || angle == MirrorAngle.Deg225);

            if (isFwd)
            {
                switch (dir)
                {
                    case Direction.Right: return Direction.Up;
                    case Direction.Up:    return Direction.Right;
                    case Direction.Left:  return Direction.Down;
                    case Direction.Down:  return Direction.Left;
                }
            }
            else
            {
                switch (dir)
                {
                    case Direction.Right: return Direction.Down;
                    case Direction.Down:  return Direction.Right;
                    case Direction.Left:  return Direction.Up;
                    case Direction.Up:    return Direction.Left;
                }
            }
            return dir;
        }

        private static Direction OppositeDir(Direction d)
            => (Direction)(((int)d + 2) % 4);

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

        // ─── Victory ─────────────────────────────────────────────────────────────

        private void OnAllCrystalsLit()
        {
            foreach (var crystal in _crystals)
                _particles?.Spawn(EmotionalParticleType.HappyGoldenWisp, CellCenter(crystal), 5);

            _manager.SolvePuzzle(CellCenter(_crystals.Count > 0 ? _crystals[0] : Vector2Int.zero));
            OnPuzzleEnd?.Invoke(true);
        }

        // ─── Layout ───────────────────────────────────────────────────────────────

        private void BuildLayout(string tier)
        {
            int lanternCount = tier == "druid" ? 3 : (tier == "scout" ? 2 : 1);
            int crystalCount = lanternCount;
            int mirrorCount  = tier == "druid" ? 5 : (tier == "scout" ? 3 : 2);
            bool allowCross  = tier == "druid";

            _totalCrystals = crystalCount;

            var rng   = new System.Random(137);
            var used  = new HashSet<int>();

            // Place lanterns on left column
            for (var i = 0; i < lanternCount; i++)
            {
                int r = Mathf.Clamp(i * (_rows / Mathf.Max(lanternCount, 1)), 0, _rows - 1);
                var pos = new Vector2Int(0, r);
                _grid[0, r].role = CellRole.Lantern;
                _lanterns.Add(pos);
                used.Add(r);
            }

            // Place crystals on right column
            for (var i = 0; i < crystalCount; i++)
            {
                int r = Mathf.Clamp(i * (_rows / Mathf.Max(crystalCount, 1)), 0, _rows - 1);
                var pos = new Vector2Int(_cols - 1, r);
                _grid[_cols - 1, r].role = CellRole.Crystal;
                _crystals.Add(pos);
                used.Add(_cols * _rows - 1 - i);
            }

            // Place mirrors in random interior positions
            var interiorUsed = new HashSet<int>(used);
            for (var m = 0; m < mirrorCount; m++)
            {
                var pos = RandomInteriorCell(rng, interiorUsed);
                _grid[pos.x, pos.y].role        = CellRole.Mirror;
                _grid[pos.x, pos.y].mirrorAngle = (MirrorAngle)(m % 4);
                _grid[pos.x, pos.y].isCross     = allowCross && m == mirrorCount - 1;
                _mirrors.Add(pos);
                interiorUsed.Add(pos.x * _rows + pos.y);
            }
        }

        private Vector2Int RandomInteriorCell(System.Random rng, HashSet<int> used)
        {
            for (var attempt = 0; attempt < 100; attempt++)
            {
                var c = rng.Next(1, _cols - 1);
                var r = rng.Next(0, _rows);
                var key = c * _rows + r;
                if (!used.Contains(key) && _grid[c, r].role == CellRole.Empty)
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

                    // Base tile
                    var go = new GameObject($"LR_{c}_{r}");
                    go.transform.SetParent(parent, false);
                    var rt = go.AddComponent<RectTransform>();
                    rt.anchoredPosition = new Vector2(xPos, yPos);
                    rt.sizeDelta        = new Vector2(CellSize, CellSize);

                    var img = go.AddComponent<Image>();
                    img.sprite = CreateCellSprite();
                    img.color  = RoleTileColor(_grid[c, r].role);

                    var btn = go.AddComponent<Button>();
                    var col = c; var row = r;
                    btn.onClick.AddListener(() => OnCellTapped(col, row));

                    _cellRects[c, r]  = rt;
                    _cellImages[c, r] = img;

                    // Beam overlay child
                    var ov = new GameObject($"LROv_{c}_{r}");
                    ov.transform.SetParent(go.transform, false);
                    var ovRt = ov.AddComponent<RectTransform>();
                    ovRt.anchorMin = Vector2.zero;
                    ovRt.anchorMax = Vector2.one;
                    ovRt.offsetMin = Vector2.zero;
                    ovRt.offsetMax = Vector2.zero;

                    var ovImg = ov.AddComponent<Image>();
                    ovImg.sprite = img.sprite;
                    ovImg.color  = new Color(0, 0, 0, 0);
                    _beamOverlay[c, r] = ovImg;
                }
            }
        }

        private static Color RoleTileColor(CellRole role)
        {
            switch (role)
            {
                case CellRole.Lantern: return ColorLantern;
                case CellRole.Crystal: return ColorCrystalOff;
                case CellRole.Mirror:  return ColorMirror;
                case CellRole.Wall:    return ColorWall;
                default:               return ColorEmpty;
            }
        }

        // ─── Visual Refresh ───────────────────────────────────────────────────────

        private void RefreshAllVisuals()
        {
            for (var c = 0; c < _cols; c++)
            {
                for (var r = 0; r < _rows; r++)
                {
                    var cell = _grid[c, r];

                    // Update beam overlay
                    if (_beamOverlay[c, r] != null)
                        _beamOverlay[c, r].color = cell.isLit ? ColorBeamOverlay
                            : new Color(0, 0, 0, 0);

                    // Crystal color swap
                    if (cell.role == CellRole.Crystal && _cellImages[c, r] != null)
                    {
                        _cellImages[c, r].color = cell.isLit ? ColorCrystalOn : ColorCrystalOff;
                        if (cell.isLit)
                            _particles?.Spawn(EmotionalParticleType.HappyGoldenWisp,
                                CellCenter(new Vector2Int(c, r)), 1);
                    }
                }
            }
        }

        private Vector2 CellCenter(Vector2Int pos)
        {
            if (_cellRects == null || !InBounds(pos.x, pos.y)) return Vector2.zero;
            return _cellRects[pos.x, pos.y].anchoredPosition;
        }

        // ─── Sprite Factory ───────────────────────────────────────────────────────

        private static Sprite CreateCellSprite()
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
    }
}
