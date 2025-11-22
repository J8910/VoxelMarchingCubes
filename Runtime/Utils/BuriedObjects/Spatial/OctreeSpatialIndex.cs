using System.Collections.Generic;
using UnityEngine;

namespace VoxelMarchingCubes.Utils.BuriedObjects.Spatial
{
    /// <summary>
    /// Octree-based spatial index for items identified by world-space bounds.
    /// Optimized for intersection queries with axis-aligned bounds.
    /// </summary>
    /// <typeparam name="T">Item type to store.</typeparam>
    public class OctreeSpatialIndex<T> : ISpatialIndex<T>
    {
        private class Node
        {
            public Bounds Bounds;
            public readonly List<(T item, Bounds b)> Items = new List<(T, Bounds)>();
            public Node[] Children; // length 8 when split
            public readonly int Depth;

            public Node(Bounds bounds, int depth)
            {
                Bounds = bounds;
                Depth = depth;
            }
        }

        private Node _root;
        private readonly int _maxDepth;
        private readonly int _capacity;
        private readonly Dictionary<T, Node> _itemNode = new Dictionary<T, Node>();
        private readonly Dictionary<T, Bounds> _itemBounds = new Dictionary<T, Bounds>();

        public int Count => _itemBounds.Count;

        public OctreeSpatialIndex(Bounds worldBounds, int maxDepth = 6, int capacity = 8)
        {
            if (maxDepth < 1) maxDepth = 1;
            if (capacity < 1) capacity = 1;
            _maxDepth = maxDepth;
            _capacity = capacity;
            _root = new Node(worldBounds, 0);
        }

        public void Add(T item, Bounds bounds)
        {
            if (_itemBounds.ContainsKey(item))
            {
                Update(item, bounds);
                return;
            }

            Insert(_root, item, bounds);
            _itemBounds[item] = bounds;
        }

        public bool Remove(T item)
        {
            if (!_itemBounds.TryGetValue(item, out var b))
                return false;

            if (_itemNode.TryGetValue(item, out var node))
            {
                for (int i = 0; i < node.Items.Count; i++)
                {
                    if (EqualityComparer<T>.Default.Equals(node.Items[i].item, item))
                    {
                        node.Items.RemoveAt(i);
                        break;
                    }
                }
                _itemNode.Remove(item);
            }
            else
            {
                // Fallback: search remove
                RemoveFromNode(_root, item);
            }

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
            var result = new HashSet<T>();
            QueryNode(_root, queryBounds, result);
            return result;
        }

        public void Clear()
        {
            _root.Items.Clear();
            _root.Children = null;
            _itemBounds.Clear();
            _itemNode.Clear();
        }

        private void Insert(Node node, T item, Bounds bounds)
        {
            if (node.Children != null)
            {
                int childIndex = GetChildIndexContaining(node, bounds);
                if (childIndex >= 0)
                {
                    Insert(node.Children[childIndex], item, bounds);
                    return;
                }
            }

            node.Items.Add((item, bounds));
            _itemNode[item] = node;

            if (node.Items.Count > _capacity && node.Depth < _maxDepth)
            {
                Split(node);
                // Re-distribute items that fully fit into a child
                for (int i = node.Items.Count - 1; i >= 0; i--)
                {
                    var kv = node.Items[i];
                    int idx = GetChildIndexContaining(node, kv.b);
                    if (idx >= 0)
                    {
                        node.Items.RemoveAt(i);
                        Insert(node.Children[idx], kv.item, kv.b);
                    }
                }
            }
        }

        private void Split(Node node)
        {
            if (node.Children != null) return;

            node.Children = new Node[8];
            Vector3 size = node.Bounds.size * 0.5f;
            Vector3 min = node.Bounds.min;
            // Create 8 children
            int d = node.Depth + 1;
            for (int xi = 0; xi < 2; xi++)
            for (int yi = 0; yi < 2; yi++)
            for (int zi = 0; zi < 2; zi++)
            {
                Vector3 childMin = new Vector3(
                    min.x + xi * size.x,
                    min.y + yi * size.y,
                    min.z + zi * size.z);
                var b = new Bounds(childMin + size * 0.5f, size);
                int index = (xi << 2) | (yi << 1) | zi;
                node.Children[index] = new Node(b, d);
            }
        }

        private int GetChildIndexContaining(Node node, Bounds b)
        {
            if (node.Children == null) return -1;
            for (int i = 0; i < 8; i++)
            {
                if (ContainsBounds(node.Children[i].Bounds, b))
                    return i;
            }
            return -1;
        }

        private static bool ContainsBounds(Bounds container, Bounds containee)
        {
            Vector3 min = containee.min;
            Vector3 max = containee.max;
            return container.min.x <= min.x && container.max.x >= max.x &&
                   container.min.y <= min.y && container.max.y >= max.y &&
                   container.min.z <= min.z && container.max.z >= max.z;
        }

        private void QueryNode(Node node, Bounds query, HashSet<T> results)
        {
            if (!node.Bounds.Intersects(query)) return;

            for (int i = 0; i < node.Items.Count; i++)
            {
                var kv = node.Items[i];
                if (kv.b.Intersects(query))
                {
                    results.Add(kv.item);
                }
            }

            if (node.Children == null) return;
            for (int i = 0; i < 8; i++)
            {
                QueryNode(node.Children[i], query, results);
            }
        }

        private bool RemoveFromNode(Node node, T item)
        {
            for (int i = 0; i < node.Items.Count; i++)
            {
                if (EqualityComparer<T>.Default.Equals(node.Items[i].item, item))
                {
                    node.Items.RemoveAt(i);
                    return true;
                }
            }
            if (node.Children != null)
            {
                for (int i = 0; i < 8; i++)
                {
                    if (RemoveFromNode(node.Children[i], item)) return true;
                }
            }
            return false;
        }
    }
}
