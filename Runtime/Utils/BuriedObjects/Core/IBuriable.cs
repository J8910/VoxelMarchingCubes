using UnityEngine;
using VoxelMarchingCubes.Runtime;

namespace VoxelMarchingCubes.Utils
{
    public interface IBuriable
    {
        float Exposure { get; }
        
        void UpdateExposure(VoxelTerrain terrain);
        
        Vector3 Position { get; }

        bool IsFullyExposed();
    }
}
