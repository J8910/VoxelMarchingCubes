using System.Collections.Generic;
using UnityEngine;

namespace VoxelMarchingCubes.Utils.BuriedObjects.Spatial
{
    /// <summary>
    /// Simple 3D uniform grid spatial index. Stores items in all cells overlapped by their bounds.
    /// </summary>
    /// <typeparam name="T">Item type.</typeparam>
    public class GridSpatialIndex<T> : ISpatialIndex<T>
    {
        private readonly float _cellSize;
        private readonly Dictionary<Vector3Int, HashSet<T>> _cells = new Dictionary<Vector3Int, HashSet<T>>();
        private readonly Dictionary<T, List<Vector3Int>> _itemCells = new Dictionary<T, List<Vector3Int>>();
        private readonly Dictionary<T, Bounds> _itemBounds = new Dictionary<T, Bounds>();

        public int Count => _itemBounds.Count;

        public GridSpatialIndex(float cellSize)
        {
            _cellSize = Mathf.Max(0.01f, cellSize);
        }

        public void Add(T item, Bounds bounds)
        {
            if (_itemBounds.ContainsKey(item))
            {
                Update(item, bounds);
                return;
            }

            var keys = GetKeysForBounds(bounds);
            foreach (var key in keys)
            {
                if (!_cells.TryGetValue(key, out var set))
                {
                    set = new HashSet<T>();
                    _cells[key] = set;
                }
                set.Add(item);
            }
            _itemCells[item] = keys;
            _itemBounds[item] = bounds;
        }

        public bool Remove(T item)
        {
            if (!_itemCells.TryGetValue(item, out var keys))
                return false;

            foreach (var key in keys)
            {
                if (_cells.TryGetValue(key, out var set))
                {
                    set.Remove(item);
                    if (set.Count == 0)
                        _cells.Remove(key);
                }
            }

            _itemCells.Remove(item);
            _itemBounds.Remove(item);
            return true;
        }

        public void Update(T item, Bounds newBounds)
        {
            Remove(item);
            Add(item, newBounds);
        }

        public IEnumerable<T> Query(Bounds queryBounds)
        {
            var results = new HashSet<T>();
            var keys = GetKeysForBounds(queryBounds);
            foreach (var key in keys)
            {
                if (_cells.TryGetValue(key, out var set))
                {
                    foreach (var item in set)
                    {
                        // Filter false positives by exact intersection
                        if (_itemBounds.TryGetValue(item, out var b) && b.Intersects(queryBounds))
                        {
                            results.Add(item);
                        }
                    }
                }
            }
            return results;
        }

        public void Clear()
        {
            _cells.Clear();
            _itemCells.Clear();
            _itemBounds.Clear();
        }

        private List<Vector3Int> GetKeysForBounds(Bounds b)
        {
            var keys = new List<Vector3Int>(8);
            Vector3 min = b.min;
            Vector3 max = b.max;

            Vector3Int minKey = WorldToCell(min);
            Vector3Int maxKey = WorldToCell(max);

            for (int x = minKey.x; x <= maxKey.x; x++)
            for (int y = minKey.y; y <= maxKey.y; y++)
            for (int z = minKey.z; z <= maxKey.z; z++)
            {
                keys.Add(new Vector3Int(x, y, z));
            }
            return keys;
        }

        private Vector3Int WorldToCell(Vector3 world)
        {
            return new Vector3Int(
                Mathf.FloorToInt(world.x / _cellSize),
                Mathf.FloorToInt(world.y / _cellSize),
                Mathf.FloorToInt(world.z / _cellSize)
            );
        }
    }
}
