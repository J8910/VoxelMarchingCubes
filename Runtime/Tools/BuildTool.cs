using UnityEngine;
using VoxelMarchingCubes.Runtime;

namespace VoxelMarchingCubes.Tools
{
    /// <summary>
    /// Tool for adding/building terrain material
    /// </summary>
    [System.Serializable]
    public sealed class BuildTool : TerrainToolBase
    {
        public BuildTool() : this(2f, 1f) { }

        public BuildTool(float radius, float intensity) : base(radius, intensity) { }

        public override void Apply(IModificableVoxel terrain, Vector3 worldPosition)
        {
            if (terrain == null)
                return;

            // Prefer operating through VoxelTerrain to ensure global terrain events are raised
            if (TryGetVoxelTerrain(terrain, out var voxelTerrain))
            {
                voxelTerrain.ModifyTerrain(worldPosition, radius, intensity);
            }
            else
            {
                // Fallback to the provided interface (e.g., a VoxelChunk)
                terrain.ModifyTerrain(worldPosition, radius, intensity);
            }
        }
    }
}