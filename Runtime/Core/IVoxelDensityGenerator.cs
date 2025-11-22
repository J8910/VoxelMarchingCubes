using UnityEngine;

namespace VoxelMarchingCubes.Core
{
    /// <summary>
    /// Strategy interface for computing voxel density values at integer grid coordinates.
    /// Implementations can represent different procedural terrain generation techniques.
    /// </summary>
    public interface IVoxelDensityGenerator
    {
        /// <summary>
        /// Compute the density value at the specified grid coordinate.
        /// Positive values are considered solid, negative values empty, and zero isosurface.
        /// </summary>
        float GetDensity(int x, int y, int z);

        /// <summary>
        /// Provide the world scale of the voxel grid so generators can be scale-aware if needed.
        /// </summary>
        void SetScale(Vector3 scale);
    }
}
