using UnityEngine;

namespace VoxelMarchingCubes.Runtime
{
    public interface IVoxelTerrainInspectorData
    {
        bool UseAdaptiveResolution { get; }
        Vector3 TargetWorldSize { get; }
        float VoxelsPerUnit { get; }
        Vector3Int CurrentChunkSize { get; }
        Vector3Int CurrentChunkCount { get; }
        
        Vector3 ActualWorldSize { get; }
        bool IsExtremeScale { get; }
        float EstimatedMemoryUsage { get; }
        
        void UpdateAdaptiveResolution();
        void RegenerateTerrain();
        
        VoxelMarchingCubes.Core.Scaling.VoxelResolutionData GetCurrentResolutionData();
    }
    
    public interface IVoxelTerrainDebugData
    {
        string GetScaleDebugInfo();
        string GetVoxelDebugInfo();
        bool HasDensityGenerator { get; }
    }
}