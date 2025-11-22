using UnityEngine;

namespace VoxelMarchingCubes.Runtime
{
    public interface IModificableVoxel
    {
        void ModifyTerrain(Vector3 worldPos, float radius, float delta);
    }
}
