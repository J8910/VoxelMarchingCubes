namespace VoxelMarchingCubes.Core
{
    /// <summary>
    /// Generates mesh data from a voxel grid using a chosen algorithm (e.g., Marching Cubes).
    /// Implementations should be allocation-conscious and avoid unnecessary GC where possible.
    /// </summary>
    public interface IMeshGenerator
    {
        /// <summary>
        /// Generates a mesh representation for the given voxel grid.
        /// </summary>
        /// <param name="voxelGrid">Source voxel grid. Must not be null.</param>
        /// <returns>Mesh data (vertices/triangles/normals). Arrays may be empty but not null.</returns>
        MeshData Generate(VoxelGrid voxelGrid);
    }
}
