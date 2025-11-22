using UnityEngine;
using VoxelMarchingCubes.Runtime;

namespace VoxelMarchingCubes.Tools
{
    /// <summary>
    /// Tool for digging/removing terrain material
    /// </summary>
    [System.Serializable]
    public sealed class DigTool : TerrainToolBase
    {
        public DigTool() : this(2f, 1f) { }

        public DigTool(float radius, float intensity) : base(radius, intensity) { }

        public override void Apply(IModificableVoxel terrain, Vector3 worldPosition)
        {
            if (terrain == null)
                return;

            float removeDelta = -Mathf.Abs(intensity);

            if (TryGetVoxelTerrain(terrain, out var voxelTerrain))
            {
                voxelTerrain.ModifyTerrain(worldPosition, radius, removeDelta);
            }
            else
            {
                terrain.ModifyTerrain(worldPosition, radius, removeDelta);
            }
        }
    }
}
