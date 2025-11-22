using UnityEngine;
using VoxelMarchingCubes.Runtime;
using VoxelMarchingCubes.Utils.BuriedObjects.Core;

namespace VoxelMarchingCubes.Utils.BuriedObjects.Detection
{
    public interface IExposureDetector
    {
        ExposureData CalculateExposure(Bounds objectBounds, VoxelTerrain terrain);
    }
}