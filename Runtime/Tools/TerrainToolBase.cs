using UnityEngine;
using VoxelMarchingCubes.Runtime;

namespace VoxelMarchingCubes.Tools
{
    /// <summary>
    /// Abstract base class for terrain tools.
    /// Provides common functionality and utilities for all tools.
    /// </summary>
    [System.Serializable]
    public abstract class TerrainToolBase : ITerrainTools
    {
        [SerializeField] protected float radius = 2f;
        [SerializeField] protected float intensity = 1f;

        protected TerrainToolBase(float radius, float intensity)
        {
            this.radius = radius;
            this.intensity = intensity;
        }

        public virtual float GetRadius() => radius;

        public virtual void SetRadius(float newRadius)
        {
            radius = Mathf.Max(0.1f, newRadius);
        }

        public virtual void SetIntensity(float newIntensity)
        {
            intensity = Mathf.Max(0.01f, newIntensity);
        }

        public abstract void Apply(IModificableVoxel terrain, Vector3 worldPosition);

        /// <summary>
        /// Try to resolve the concrete VoxelTerrain component from an arbitrary IModificableVoxel.
        /// This is helpful because many colliders in the scene belong to VoxelChunk which implements the interface,
        /// but higher-level systems (like BuriedObjectManager) rely on VoxelTerrain events.
        /// </summary>
        /// <param name="source">The object provided by triggers or callers, implementing <see cref="IModificableVoxel"/>.</param>
        /// <param name="voxelTerrain">Resolved VoxelTerrain if found.</param>
        /// <returns>True if a VoxelTerrain could be resolved, false otherwise.</returns>
        protected static bool TryGetVoxelTerrain(IModificableVoxel source, out VoxelTerrain voxelTerrain)
        {
            voxelTerrain = null;
            if (source is MonoBehaviour mb)
            {
                voxelTerrain = mb.GetComponentInParent<VoxelTerrain>();
                if (voxelTerrain == null)
                {
                    voxelTerrain = mb as VoxelTerrain;
                }
            }
            return voxelTerrain != null;
        }
    }
}