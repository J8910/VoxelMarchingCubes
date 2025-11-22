using UnityEngine;

namespace VoxelMarchingCubes.Core.Scaling
{
    [System.Serializable]
    public class AdaptiveVoxelResolution
    {
        [Header("Target Dimensions")]
        [SerializeField] private Vector3 _targetWorldSize = new Vector3(10f, 5f, 10f); 
        
        [Header("Voxel Density")]
        [SerializeField] private float _voxelsPerUnit = 4f; 
        [SerializeField] private Vector3Int _minChunkSize = new Vector3Int(8, 4, 8);
        [SerializeField] private Vector3Int _maxChunkSize = new Vector3Int(128, 64, 128);
        
        public VoxelResolutionData CalculateOptimalResolution(Vector3 worldScale)
        {
            float scaleX = Mathf.Max(0.001f, worldScale.x);
            float scaleY = Mathf.Max(0.001f, worldScale.y);
            float scaleZ = Mathf.Max(0.001f, worldScale.z);
            
            Vector3 safeScale = new Vector3(scaleX, scaleY, scaleZ);
            
            Vector3 actualWorldSize = Vector3.Scale(_targetWorldSize, worldScale);
            
           
            Vector3 requiredVoxelsTotal = actualWorldSize * _voxelsPerUnit;
            
            if (safeScale.x < 1.0f) 
            {
                requiredVoxelsTotal *= (1.0f / safeScale.x); // Optional: Scale down by 1/x to avoid floating point errors
            }
            
            Vector3Int optimalChunkSize = new Vector3Int(
                Mathf.Clamp(Mathf.RoundToInt(requiredVoxelsTotal.x), _minChunkSize.x, _maxChunkSize.x),
                Mathf.Clamp(Mathf.RoundToInt(requiredVoxelsTotal.y), _minChunkSize.y, _maxChunkSize.y),
                Mathf.Clamp(Mathf.RoundToInt(requiredVoxelsTotal.z), _minChunkSize.z, _maxChunkSize.z)
            );
            
            Vector3 voxelWorldSize = new Vector3(
                actualWorldSize.x / Mathf.Max(1, optimalChunkSize.x),
                actualWorldSize.y / Mathf.Max(1, optimalChunkSize.y),
                actualWorldSize.z / Mathf.Max(1, optimalChunkSize.z)
            );
            
            return new VoxelResolutionData
            {
                ChunkSize = optimalChunkSize,
                VoxelWorldSize = voxelWorldSize,
                ActualWorldSize = actualWorldSize,
                TargetWorldSize = _targetWorldSize,
                ResolutionScale = worldScale,
                VoxelsPerUnit = _voxelsPerUnit
            };
        }
        
        public void SetTargetSize(Vector3 targetSize)
        {
            _targetWorldSize = targetSize;
        }
        
        public void SetVoxelDensity(float voxelsPerUnit)
        {
            _voxelsPerUnit = Mathf.Max(0.1f, voxelsPerUnit);
        }
    }
    
    [System.Serializable]
    public struct VoxelResolutionData
    {
        public Vector3Int ChunkSize;
        public Vector3 VoxelWorldSize;
        public Vector3 ActualWorldSize;
        public Vector3 TargetWorldSize;
        public Vector3 ResolutionScale;
        public float VoxelsPerUnit;
        
        public bool IsValid => ChunkSize.x > 0 && ChunkSize.y > 0 && ChunkSize.z > 0;
        
        public void LogInfo(string context = "")
        {
            UnityEngine.Debug.Log($"=== Voxel Resolution {context} ===");
            UnityEngine.Debug.Log($"Target World Size: {TargetWorldSize}");
            UnityEngine.Debug.Log($"Actual World Size: {ActualWorldSize}");
            UnityEngine.Debug.Log($"Optimal Chunk Size: {ChunkSize}");
            UnityEngine.Debug.Log($"Voxel World Size: {VoxelWorldSize}");
            UnityEngine.Debug.Log($"Resolution Scale: {ResolutionScale}");
            UnityEngine.Debug.Log($"Voxels Per Unit: {VoxelsPerUnit}");
        }
    }
}