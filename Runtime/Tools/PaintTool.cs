using UnityEngine;
using VoxelMarchingCubes.Runtime;

namespace VoxelMarchingCubes.Tools
{
    /// <summary>
    /// Paints density towards a target value within a radius.
    /// Uses intensity as blend factor (0..1) for intuitive painting.
    /// </summary>
    [System.Serializable]
    public sealed class PaintTool : TerrainToolBase
    {
        [SerializeField, Tooltip("Target density to paint towards (0..1)")] 
        private float targetDensity = 0.5f;

        public PaintTool() : this(2f, 0.5f, 0.5f) { }

        public PaintTool(float radius, float intensity, float targetDensity = 0.5f) : base(radius, intensity)
        {
            this.targetDensity = Mathf.Clamp01(targetDensity);
        }

        public void SetTargetDensity(float value)
        {
            targetDensity = Mathf.Clamp01(value);
        }

        public override void Apply(IModificableVoxel terrain, Vector3 worldPosition)
        {
            if (terrain == null) return;

            // Prefer VoxelTerrain for proper propagation/events; fallback to interface.
            if (TryGetVoxelTerrain(terrain, out var voxelTerrain))
            {
                float blend = Mathf.Clamp01(intensity);
                voxelTerrain.SetDensityAtPosition(worldPosition, radius, targetDensity, blend);
            }
            else
            {
                // If not a VoxelTerrain, approximate by computing delta towards target at the center
                // Note: Interface doesn't expose get/set density; so we apply as an additive delta heuristic.
                float centerDelta = (targetDensity - 0.5f) * Mathf.Clamp01(intensity);
                terrain.ModifyTerrain(worldPosition, radius, centerDelta);
            }
        }
    }
}
