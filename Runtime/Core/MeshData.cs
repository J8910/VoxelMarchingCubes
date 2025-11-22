using System;
using UnityEngine;

namespace VoxelMarchingCubes.Core
{
    /// <summary>
    /// Simple container for mesh buffers produced by voxel mesh generators.
    /// Arrays should be non-null; use <see cref="Empty"/> when there is no geometry.
    /// </summary>
    public class MeshData
    {
        /// <summary>Vertex positions.</summary>
        public Vector3[] Vertices;
        /// <summary>Triangle indices (multiple of 3).</summary>
        public int[] Triangles;
        /// <summary>Per-vertex normals aligned with <see cref="Vertices"/>.</summary>
        public Vector3[] Normals;

        /// <summary>
        /// Reusable empty instance with zero-length arrays. Prefer this over allocating new empty arrays.
        /// </summary>
        public static readonly MeshData Empty = new MeshData
        {
            Vertices = Array.Empty<Vector3>(),
            Triangles = Array.Empty<int>(),
            Normals = Array.Empty<Vector3>()
        };
    }
}
