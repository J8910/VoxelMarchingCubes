using UnityEngine;
using VoxelMarchingCubes.Runtime;
using VoxelMarchingCubes.Core;

namespace VoxelMarchingCubes.Tools
{
    /// <summary>
    /// Tool for smoothing terrain by averaging density.
    /// Uses strength parameter instead of intensity for more intuitive control.
    /// </summary>
    [System.Serializable]
    public sealed class SmoothTool : ITerrainTools
    {
        [SerializeField] private float radius = 2f;
        [SerializeField] private float strength = 0.5f;

        public SmoothTool() : this(2f, 0.5f) { }

        public SmoothTool(float radius, float strength)
        {
            this.radius = radius;
            this.strength = strength;
        }
        
        /// <summary>
        /// Current smoothing radius.
        /// </summary>
        public float GetRadius() => radius;

        public void Apply(IModificableVoxel terrain, Vector3 worldPosition)
        {
            if (terrain is VoxelTerrain voxelTerrain)
            {
                // Get current density
                float currentDensity = voxelTerrain.GetDensityAtWorldPosition(worldPosition);
                
                // Get average density in area
                VoxelChunk chunk = voxelTerrain.GetChunkAtWorldPosition(worldPosition);
                if (chunk != null && chunk.VoxelGrid != null)
                {
                    Vector3 localPos = chunk.WorldToLocal(worldPosition);
                    float avgDensity = chunk.VoxelGrid.GetAverageDensity(localPos, radius);
                    
                    // Blend towards average
                    float targetDensity = Mathf.Lerp(currentDensity, avgDensity, strength);
                    voxelTerrain.SetDensityAtPosition(worldPosition, radius, targetDensity, strength);
                }
            }
        }

        /// <summary>
        /// Sets the smoothing radius (clamped to a small positive value).
        /// </summary>
        public void SetRadius(float newRadius)
        {
            radius = Mathf.Max(0.1f, newRadius);
        }
        
        /// <summary>
        /// SetIntensity maps to strength for interface compatibility
        /// </summary>
        public void SetIntensity(float newIntensity)
        {
            strength = Mathf.Clamp01(newIntensity);
        }

        /// <summary>
        /// More descriptive method for smooth tool (same as SetIntensity)
        /// </summary>
        public void SetStrength(float newStrength)
        {
            strength = Mathf.Clamp01(newStrength);
        }
    }
}