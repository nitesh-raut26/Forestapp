using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace ForestFriendsQuest
{
    /// <summary>
    /// Rotating Path Puzzle — connect the forest entrance to the sanctuary exit
    /// by rotating path tiles until a continuous route is formed.
    ///
    /// Each tile has a connection bitmask (N=1, E=2, S=4, W=8).
    /// Tapping a tile rotates it 90 degrees clockwise, cycling its exits.
    ///
    /// Tile shapes:
    ///   Straight  = N|S  (or E|W after rotation)
    ///   Corner    = N|E  (rotatable)
    ///   T-junction= N|E|S
    ///   Cross     = N|E|S|W (all-open, no rotation needed)
    ///   Dead-end  = N only
    ///
    /// Sprout (4-6):  3x3, mostly straights + 1 corner
    /// Scout (7-11):  4x4, mix of shapes
    /// Druid (12-16): 5x5, complex mesh with dead-ends
    ///
    /// Visual feedback:
    ///   - Tile rotated: DiscoveryRuneGlow pulse
    ///   - Continuous path forms: HappyPollenBurst per connected tile
    ///   - Solved (entrance to exit connected): JoyBurst + SolvePuzzle
    /// </summary>
    public class RotatingPathPuzzle : MonoBehaviour
    {
        // ─── Direction Bitmask Constants ──────────────────────────────────────────

        private const int N = 1, E = 2, S = 4, W = 8;

        private struct PathTile
        {
            public int   connections;  // bitmask of N/E/S/W
            public bool  isFixed;      // start/end tiles don't rotate
            public bool  isConnected;  // on the solved path
        }

        // ─── State ───────────────────────────────────────────────────────────────

        private PathTile[,] _tiles;
        private int         _cols, _rows;
        private Vector2Int  _entrance;
        private Vector2Int  _exit;

        // ─── UI ──────────────────────────────────────────────────────────────────

        private Image[,]         _tileImages;
        private RectTransform[,] _tileRects;
        private Image[,]         _glowOverlay;
        private const float      TileSize = 84f;
        private const float      TileGap  = 4f;

        // ─── Colors ───────────────────────────────────────────────────────────────

        private static readonly Color ColorIdle      = new Color(0.20f, 0.35f, 0.25f, 0.80f);
        private static readonly Color ColorFixed     = new Color(0.40f, 0.70f, 0.45f, 0.95f);
        private static readonly Color ColorConnected = new Color(0.55f, 0.95f, 0.65f, 1.00f);
        private static readonly Color ColorPath      = new Color(0.35f, 0.85f, 0.50f, 0.70f);

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

            _tiles       = new PathTile[_cols, _rows];
            _tileImages  = new Image[_cols, _rows];
            _tileRects   = new RectTransform[_cols, _rows];
            _glowOverlay = new Image[_cols, _rows];

            BuildLayout(tier);
            BuildGridUI(parent);
            CheckPathConnectivity();

            _manager.StartPuzzle(PuzzleType.RotatingPath, tier);
        }

        // ─── Player Input ─────────────────────────────────────────────────────────

        public void OnTileTapped(int col, int row)
        {
            if (!InBounds(col, row)) return;
            if (_tiles[col, row].isFixed) return;

            // Rotate 90° clockwise: N->E->S->W->N for each connection bit
            _tiles[col, row].connections = RotateMask90(_tiles[col, row].connections);

            _particles?.Spawn(EmotionalParticleType.DiscoveryRuneGlow,
                TileCenter(new Vector2Int(col, row)), 2);

            CheckPathConnectivity();

            if (IsFullyConnected())
                Solved();
        }

        // ─── Path Connectivity ────────────────────────────────────────────────────

        private void CheckPathConnectivity()
        {
            // Reset
            for (var c = 0; c < _cols; c++)
                for (var r = 0; r < _rows; r++)
                    _tiles[c, r].isConnected = false;

            // BFS from entrance following matching connections
            var queue   = new Queue<Vector2Int>();
            var visited = new HashSet<int>();

            queue.Enqueue(_entrance);
            visited.Add(_entrance.x * _rows + _entrance.y);
            _tiles[_entrance.x, _entrance.y].isConnected = true;

            while (queue.Count > 0)
            {
                var cur = queue.Dequeue();
                var connections = _tiles[cur.x, cur.y].connections;

                TryExpand(cur, Vector2Int.up,    S, N, queue, visited);
                TryExpand(cur, Vector2Int.right, W, E, queue, visited);
                TryExpand(cur, Vector2Int.down,  N, S, queue, visited);
                TryExpand(cur, Vector2Int.left,  E, W, queue, visited);
            }

            RefreshAllTileVisuals();
        }

        private void TryExpand(
            Vector2Int cur,
            Vector2Int dir,
            int exitBitFromNeighbour,
            int entryBitFromCurrent,
            Queue<Vector2Int> queue,
            HashSet<int> visited)
        {
            // Does current tile exit in this direction?
            if ((_tiles[cur.x, cur.y].connections & entryBitFromCurrent) == 0) return;

            var next = cur + dir;
            if (!InBounds(next.x, next.y)) return;

            var key = next.x * _rows + next.y;
            if (visited.Contains(key)) return;

            // Does neighbour tile enter from the matching side?
            if ((_tiles[next.x, next.y].connections & exitBitFromNeighbour) == 0) return;

            visited.Add(key);
            _tiles[next.x, next.y].isConnected = true;
            queue.Enqueue(next);
        }

        private bool IsFullyConnected()
        {
            return _tiles[_exit.x, _exit.y].isConnected;
        }

        // ─── Victory ─────────────────────────────────────────────────────────────

        private void Solved()
        {
            _particles?.SpawnJoyBurst(TileCenter(_exit));
            _manager.SolvePuzzle(TileCenter(_exit));
            OnPuzzleEnd?.Invoke(true);
        }

        // ─── Bitmask Rotation ─────────────────────────────────────────────────────

        private static int RotateMask90(int mask)
        {
            // Rotate each direction flag 90° clockwise: N(1)->E(2)->S(4)->W(8)->N(1)
            var newMask = 0;
            if ((mask & N) != 0) newMask |= E;
            if ((mask & E) != 0) newMask |= S;
            if ((mask & S) != 0) newMask |= W;
            if ((mask & W) != 0) newMask |= N;
            return newMask;
        }

        // ─── Layout Builder ───────────────────────────────────────────────────────

        private void BuildLayout(string tier)
        {
            // Entrance: left-center, Exit: right-center
            _entrance = new Vector2Int(0, _rows / 2);
            _exit     = new Vector2Int(_cols - 1, _rows / 2);

            // Fill all with disconnected stubs first
            var rng = new System.Random(77);
            for (var c = 0; c < _cols; c++)
            {
                for (var r = 0; r < _rows; r++)
                {
                    _tiles[c, r] = new PathTile
                    {
                        connections = RandomShape(rng, tier),
                        isFixed     = false,
                        isConnected = false
                    };
                }
            }

            // Carve a guaranteed-solvable straight path, then rotate tiles to scramble
            CarveCorridorAndScramble(rng);

            // Fix entrance and exit tiles so they always face inward
            _tiles[_entrance.x, _entrance.y].connections = E;          // exits East
            _tiles[_entrance.x, _entrance.y].isFixed     = true;
            _tiles[_exit.x,     _exit.y    ].connections = W;          // enters from West
            _tiles[_exit.x,     _exit.y    ].isFixed     = true;
        }

        private void CarveCorridorAndScramble(System.Random rng)
        {
            // Build a simple L-shaped solved path
            var path = new List<Vector2Int>();
            for (var c = 0; c < _cols; c++)
                path.Add(new Vector2Int(c, _rows / 2));

            // Assign correct connection masks for the corridor
            for (var i = 0; i < path.Count; i++)
            {
                var p = path[i];
                var mask = 0;
                if (i > 0)             mask |= W;  // connects west
                if (i < path.Count - 1) mask |= E;  // connects east
                _tiles[p.x, p.y].connections = mask;
            }

            // Scramble non-fixed tiles by random rotations
            for (var c = 1; c < _cols - 1; c++)
            {
                for (var r = 0; r < _rows; r++)
                {
                    if (r == _rows / 2) continue;   // don't scramble the solved corridor row
                    int rotations = rng.Next(0, 4);
                    for (var rot = 0; rot < rotations; rot++)
                        _tiles[c, r].connections = RotateMask90(_tiles[c, r].connections);
                }
            }
        }

        private static int RandomShape(System.Random rng, string tier)
        {
            // Choose from available tile shapes by tier
            var shapes = tier == "druid"
                ? new[] { N | S, E | W, N | E, S | W, N | E | S, N | E | W, N | E | S | W, N }
                : tier == "scout"
                ? new[] { N | S, E | W, N | E, S | W, N | E | S }
                : new[] { N | S, E | W, N | E };

            return shapes[rng.Next(0, shapes.Length)];
        }

        // ─── UI Builder ───────────────────────────────────────────────────────────

        private void BuildGridUI(RectTransform parent)
        {
            var totalW = _cols * (TileSize + TileGap) - TileGap;
            var totalH = _rows * (TileSize + TileGap) - TileGap;

            for (var c = 0; c < _cols; c++)
            {
                for (var r = 0; r < _rows; r++)
                {
                    var xPos = c * (TileSize + TileGap) - totalW / 2f + TileSize / 2f;
                    var yPos = r * (TileSize + TileGap) - totalH / 2f + TileSize / 2f;

                    var go = new GameObject($"PT_{c}_{r}");
                    go.transform.SetParent(parent, false);

                    var rt = go.AddComponent<RectTransform>();
                    rt.anchoredPosition = new Vector2(xPos, yPos);
                    rt.sizeDelta        = new Vector2(TileSize, TileSize);

                    var img = go.AddComponent<Image>();
                    img.sprite = CreateTileSprite();
                    img.color  = _tiles[c, r].isFixed ? ColorFixed : ColorIdle;

                    var btn = go.AddComponent<Button>();
                    var col = c; var row = r;
                    btn.onClick.AddListener(() => OnTileTapped(col, row));

                    _tileRects[c, r]  = rt;
                    _tileImages[c, r] = img;

                    // Glow overlay
                    var ov = new GameObject($"PTOv_{c}_{r}");
                    ov.transform.SetParent(go.transform, false);
                    var ovRt = ov.AddComponent<RectTransform>();
                    ovRt.anchorMin = Vector2.zero;
                    ovRt.anchorMax = Vector2.one;
                    ovRt.offsetMin = ovRt.offsetMax = Vector2.zero;
                    var ovImg = ov.AddComponent<Image>();
                    ovImg.sprite = img.sprite;
                    ovImg.color  = new Color(0, 0, 0, 0);
                    _glowOverlay[c, r] = ovImg;
                }
            }
        }

        // ─── Visual Refresh ───────────────────────────────────────────────────────

        private void RefreshAllTileVisuals()
        {
            for (var c = 0; c < _cols; c++)
            {
                for (var r = 0; r < _rows; r++)
                {
                    if (_tileImages[c, r] == null) continue;

                    _tileImages[c, r].color =
                        _tiles[c, r].isFixed      ? ColorFixed :
                        _tiles[c, r].isConnected  ? ColorConnected :
                        ColorIdle;

                    if (_glowOverlay[c, r] != null)
                        _glowOverlay[c, r].color = _tiles[c, r].isConnected
                            ? ColorPath : new Color(0, 0, 0, 0);
                }
            }
        }

        // ─── Helpers ─────────────────────────────────────────────────────────────

        private bool InBounds(int c, int r) =>
            c >= 0 && c < _cols && r >= 0 && r < _rows;

        private Vector2 TileCenter(Vector2Int pos)
        {
            if (_tileRects == null || !InBounds(pos.x, pos.y)) return Vector2.zero;
            return _tileRects[pos.x, pos.y].anchoredPosition;
        }

        // ─── Sprite Factory ───────────────────────────────────────────────────────

        private static Sprite CreateTileSprite()
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
