using System;
using System.Collections.Generic;
using UnityEngine;

namespace ForestFriendsQuest
{
    /// <summary>
    /// Placement result returned after attempting to place an item.
    /// </summary>
    public enum PlacementResult
    {
        Success,
        OutOfBounds,
        Occupied,
        InvalidItem
    }

    /// <summary>
    /// A single occupied cell in the sanctuary grid.
    /// </summary>
    [Serializable]
    public class PlacedItem
    {
        public string     itemId;
        public int        col;
        public int        row;
        public int        width;   // in grid cells
        public int        height;  // in grid cells
        public float      rotation; // 0, 90, 180, 270
    }

    /// <summary>
    /// Grid-based sanctuary placement system.
    ///
    /// Manages a 2D grid of cells, enforces overlap detection,
    /// supports 1x1 to 2x2 footprint items, and allows 90-degree rotation.
    /// Emits placement events for VFX integration.
    ///
    /// Grid coordinates: (0,0) = bottom-left corner.
    /// </summary>
    public class SanctuaryPlacementGrid : MonoBehaviour
    {
        // ─── Config ──────────────────────────────────────────────────────────────

        [Header("Grid Dimensions")]
        public int cols = 8;
        public int rows = 6;

        // ─── Events ───────────────────────────────────────────────────────────────

        public event Action<PlacedItem>  OnItemPlaced;
        public event Action<string>      OnItemRemoved;   // itemId
        public event Action<PlacedItem>  OnItemRotated;

        // ─── State ───────────────────────────────────────────────────────────────

        private bool[,]              _occupied;
        private PlacedItem[,]        _itemMap;   // which placed item owns each cell
        private readonly List<PlacedItem> _placedItems = new List<PlacedItem>();

        // ─── Lifecycle ───────────────────────────────────────────────────────────

        private void Awake()
        {
            ResetGrid();
        }

        public void ResetGrid()
        {
            _occupied  = new bool[cols, rows];
            _itemMap   = new PlacedItem[cols, rows];
            _placedItems.Clear();
        }

        // ─── Placement API ────────────────────────────────────────────────────────

        /// <summary>Try to place an item at a grid position. Returns the result status.</summary>
        public PlacementResult TryPlace(string itemId, int col, int row,
            int width = 1, int height = 1, float rotation = 0f)
        {
            if (string.IsNullOrEmpty(itemId))  return PlacementResult.InvalidItem;
            if (!IsInBounds(col, row, width, height)) return PlacementResult.OutOfBounds;
            if (!IsFree(col, row, width, height))     return PlacementResult.Occupied;

            var item = new PlacedItem
            {
                itemId   = itemId,
                col      = col,
                row      = row,
                width    = width,
                height   = height,
                rotation = rotation
            };

            OccupyCells(item);
            _placedItems.Add(item);
            OnItemPlaced?.Invoke(item);
            return PlacementResult.Success;
        }

        /// <summary>Remove a placed item by its item ID. Returns false if not found.</summary>
        public bool TryRemove(string itemId)
        {
            PlacedItem found = null;
            foreach (var item in _placedItems)
            {
                if (item.itemId == itemId) { found = item; break; }
            }

            if (found == null) return false;

            FreeCells(found);
            _placedItems.Remove(found);
            OnItemRemoved?.Invoke(itemId);
            return true;
        }

        /// <summary>Remove the item occupying a specific cell.</summary>
        public bool TryRemoveAt(int col, int row)
        {
            if (!IsInBounds(col, row, 1, 1)) return false;
            var target = _itemMap[col, row];
            if (target == null) return false;
            return TryRemove(target.itemId);
        }

        /// <summary>Rotate an existing placed item by 90 degrees (moves it to new footprint).</summary>
        public bool TryRotate(string itemId)
        {
            PlacedItem target = null;
            foreach (var item in _placedItems)
            {
                if (item.itemId == itemId) { target = item; break; }
            }

            if (target == null) return false;

            // Swap width/height for 90-degree rotation
            var newW = target.height;
            var newH = target.width;

            if (!IsFreeExcluding(target.col, target.row, newW, newH, target))
                return false;

            FreeCells(target);
            target.width    = newW;
            target.height   = newH;
            target.rotation = (target.rotation + 90f) % 360f;
            OccupyCells(target);
            OnItemRotated?.Invoke(target);
            return true;
        }

        // ─── Query API ────────────────────────────────────────────────────────────

        public bool IsCellOccupied(int col, int row)
        {
            if (!IsInBounds(col, row, 1, 1)) return true;
            return _occupied[col, row];
        }

        public PlacedItem GetItemAt(int col, int row)
        {
            if (!IsInBounds(col, row, 1, 1)) return null;
            return _itemMap[col, row];
        }

        public IReadOnlyList<PlacedItem> GetAllPlaced() => _placedItems;

        public int GetPlacedCount()   => _placedItems.Count;
        public int GetFreeCount()
        {
            var free = 0;
            for (var c = 0; c < cols; c++)
            {
                for (var r = 0; r < rows; r++)
                {
                    if (!_occupied[c, r]) free++;
                }
            }
            return free;
        }

        /// <summary>Convert a world/canvas position to grid coordinates.</summary>
        public Vector2Int WorldToGrid(Vector2 localPos, float cellSize)
        {
            var col = Mathf.FloorToInt((localPos.x + cols * cellSize / 2f) / cellSize);
            var row = Mathf.FloorToInt((localPos.y + rows * cellSize / 2f) / cellSize);
            return new Vector2Int(col, row);
        }

        /// <summary>Convert grid coordinates to a local canvas position (cell center).</summary>
        public Vector2 GridToWorld(int col, int row, float cellSize)
        {
            var x = (col - cols / 2f + 0.5f) * cellSize;
            var y = (row - rows / 2f + 0.5f) * cellSize;
            return new Vector2(x, y);
        }

        // ─── Private Helpers ──────────────────────────────────────────────────────

        private bool IsInBounds(int col, int row, int width, int height)
        {
            return col >= 0 && row >= 0
                && col + width  <= cols
                && row + height <= rows;
        }

        private bool IsFree(int col, int row, int width, int height)
        {
            for (var c = col; c < col + width; c++)
            {
                for (var r = row; r < row + height; r++)
                {
                    if (_occupied[c, r]) return false;
                }
            }
            return true;
        }

        private bool IsFreeExcluding(int col, int row, int width, int height,
            PlacedItem exclude)
        {
            for (var c = col; c < col + width; c++)
            {
                for (var r = row; r < row + height; r++)
                {
                    if (_occupied[c, r] && _itemMap[c, r] != exclude) return false;
                }
            }
            return true;
        }

        private void OccupyCells(PlacedItem item)
        {
            for (var c = item.col; c < item.col + item.width; c++)
            {
                for (var r = item.row; r < item.row + item.height; r++)
                {
                    _occupied[c, r] = true;
                    _itemMap[c, r]  = item;
                }
            }
        }

        private void FreeCells(PlacedItem item)
        {
            for (var c = item.col; c < item.col + item.width; c++)
            {
                for (var r = item.row; r < item.row + item.height; r++)
                {
                    _occupied[c, r] = false;
                    _itemMap[c, r]  = null;
                }
            }
        }

        // ─── Save/Load Helpers ────────────────────────────────────────────────────

        public PlacedItem[] GetSaveState()  => _placedItems.ToArray();

        public void LoadSaveState(PlacedItem[] savedItems)
        {
            ResetGrid();
            if (savedItems == null) return;

            foreach (var item in savedItems)
            {
                TryPlace(item.itemId, item.col, item.row,
                    item.width, item.height, item.rotation);
            }
        }
    }
}
