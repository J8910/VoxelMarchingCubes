using System.Collections.Generic;
using UnityEngine;

namespace VoxelMarchingCubes.Utils.BuriedObjects.Spatial
{
    /// <summary>
    /// Abstraction for a spatial index capable of storing items with world-space bounds
    /// and querying them by an axis-aligned bounding box.
    /// </summary>
    /// <typeparam name="T">The item type stored in the index.</typeparam>
    public interface ISpatialIndex<T>
    {
        /// <summary>
        /// Add an item with its current world bounds.
        /// </summary>
        void Add(T item, Bounds bounds);

        /// <summary>
        /// Remove an item from the index.
        /// </summary>
        /// <returns>True if the item was found and removed.</returns>
        bool Remove(T item);

        /// <summary>
        /// Update an item's bounds in the index.
        /// </summary>
        void Update(T item, Bounds newBounds);

        /// <summary>
        /// Return all items whose bounds intersect the query bounds.
        /// </summary>
        IEnumerable<T> Query(Bounds queryBounds);

        /// <summary>
        /// Remove all items.
        /// </summary>
        void Clear();

        /// <summary>
        /// Total number of items currently stored in the index.
        /// </summary>
        int Count { get; }
    }
}
