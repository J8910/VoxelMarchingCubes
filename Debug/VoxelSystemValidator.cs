#if UNITY_EDITOR
using UnityEngine;
using VoxelMarchingCubes.Runtime;

namespace VoxelMarchingCubes
{
    public static class VoxelSystemValidator
    {
        public static ValidationResult ValidateVoxelTerrain(VoxelTerrain terrain)
        {
            var result = new ValidationResult();
            
            Vector3 scale = terrain.transform.lossyScale;
            if (IsExtremeScale(scale))
            {
                result.AddWarning($"Extreme scale detected: {scale}. May cause precision issues.");
            }
            
            if (terrain.transform.GetComponent<VoxelTerrain>() == null)
            {
                result.AddError("VoxelTerrain component missing.");
            }
            
            
            return result;
        }
        
        private static bool IsExtremeScale(Vector3 scale)
        {
            return scale.x < 0.01f || scale.y < 0.01f || scale.z < 0.01f ||
                   scale.x > 10f || scale.y > 10f || scale.z > 10f;
        }
    }
    
    public class ValidationResult
    {
        public System.Collections.Generic.List<string> Errors { get; } = new();
        public System.Collections.Generic.List<string> Warnings { get; } = new();
        
        public bool IsValid => Errors.Count == 0;
        
        public void AddError(string error) => Errors.Add(error);
        public void AddWarning(string warning) => Warnings.Add(warning);
        
        public void LogToConsole()
        {
            foreach (var error in Errors)
                Debug.LogError($"[VoxelSystem] {error}");
                
            foreach (var warning in Warnings)
                Debug.LogWarning($"[VoxelSystem] {warning}");
        }
    }
}
#endif