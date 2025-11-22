using UnityEngine;
using VoxelMarchingCubes.Runtime;

namespace VoxelMarchingCubes.Tools
{
    public interface ITerrainTools
    {
        /// <summary>
        /// Apply the tool effect to the terrain at the specified world position
        /// </summary>
        void Apply(IModificableVoxel terrain, Vector3 worldPosition);
        
        /// <summary>
        /// Get the current radius of the tool's effect
        /// </summary>
        float GetRadius();
        
        /// <summary>
        /// Set the radius of the tool's effect
        /// </summary>
        void SetRadius(float radius);
        
        /// <summary>
        /// Set the intensity/strength of the tool's effect
        /// </summary>
        void SetIntensity(float intensity);
    }
}
