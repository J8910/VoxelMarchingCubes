using UnityEngine;

namespace VoxelMarchingCubes.Core.Density
{
    public interface IDensityLayer
    {
        float GetDensity(int x, int y, int z);
        void SetScale(Vector3 scale);
        int Priority { get; }
    }
}