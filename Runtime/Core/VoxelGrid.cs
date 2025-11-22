using UnityEngine;

namespace VoxelMarchingCubes.Core
{
    /// <summary>
    /// 3D grid of voxels storing density values. Provides utility methods to query and modify densities
    /// within spherical regions and rectangular regions with bounds checking.
    /// </summary>
    public class VoxelGrid
    {
        private readonly Voxel[,,] _voxels;
        public Vector3Int Size { get; }
        
        public VoxelGrid(Vector3Int size)
        {
            Size = size;
            _voxels = new Voxel[size.x, size.y, size.z];
        }
        
        /// <summary>
        /// Returns a reference to the voxel at the given position. Throws if out of bounds.
        /// </summary>
        public ref Voxel GetVoxel(Vector3Int position)
        {
            if (!IsInsideBounds(position))
                throw new System.IndexOutOfRangeException($"Position {position} is out of bounds (Size: {Size})");
            
            return ref _voxels[position.x, position.y, position.z];
        }

        /// <summary>
        /// Sets the density for the voxel at position if inside bounds.
        /// </summary>
        public void SetVoxel(Vector3Int position, float density)
        {
            if (!IsInsideBounds(position)) return;
            
            _voxels[position.x, position.y, position.z].Density = density;
        }

        /// <summary>
        /// Returns density at position, or 0 if out of bounds.
        /// </summary>
        public float GetDensity(Vector3Int position)
        {
            if (!IsInsideBounds(position))
                return 0f;
            
            return _voxels[position.x, position.y, position.z].Density;
        }
        
        /// <summary>
        /// Sets density clamped to [min,max] if inside bounds.
        /// </summary>
        public void SetDensityClamped(Vector3Int position, float density, float min = 0f, float max = 1f)
        {
            if (!IsInsideBounds(position))
                return;
            
            _voxels[position.x, position.y, position.z].Density = Mathf.Clamp(density, min, max);
        }
        
        
        /// <summary>
        /// Computes the average density inside a sphere defined by center and radius.
        /// </summary>
        public float GetAverageDensity(Vector3 localPos, float radius, float sampleStep = 0.5f)
        {
            if (radius <= 0f)
                return 0f;

            int samples = 0;
            float total = 0f;
            float radiusSquared = radius * radius;
            int step = Mathf.Max(1, Mathf.RoundToInt(sampleStep));
            
            int minX = Mathf.Max(0, Mathf.FloorToInt(localPos.x - radius));
            int maxX = Mathf.Min(Size.x - 1, Mathf.CeilToInt(localPos.x + radius));
            int minY = Mathf.Max(0, Mathf.FloorToInt(localPos.y - radius));
            int maxY = Mathf.Min(Size.y - 1, Mathf.CeilToInt(localPos.y + radius));
            int minZ = Mathf.Max(0, Mathf.FloorToInt(localPos.z - radius));
            int maxZ = Mathf.Min(Size.z - 1, Mathf.CeilToInt(localPos.z + radius));

            for (int x = minX; x <= maxX; x += step)
            for (int y = minY; y <= maxY; y += step)
            for (int z = minZ; z <= maxZ; z += step)
            {
                Vector3 pos = new Vector3(x, y, z);
                float distSquared = (pos - localPos).sqrMagnitude;

                if (distSquared <= radiusSquared)
                {
                    total += _voxels[x, y, z].Density;
                    samples++;
                }
            }

            return samples > 0 ? total / samples : 0f;
        }
        
        /// <summary>
        /// True if the position is inside grid bounds.
        /// </summary>
        public bool IsInsideBounds(Vector3Int position)
        {
            return position.x >= 0 && position.x < Size.x &&
                   position.y >= 0 && position.y < Size.y &&
                   position.z >= 0 && position.z < Size.z;
        }

        /// <summary>
        /// Computes clamped iteration bounds for a sphere region in grid coordinates.
        /// </summary>
        private void GetIterationBounds(Vector3 center, float radius,
            out int minX, out int maxX, out int minY, out int maxY, out int minZ, out int maxZ)
        {
            minX = Mathf.Max(0, Mathf.FloorToInt(center.x - radius));
            maxX = Mathf.Min(Size.x - 1, Mathf.CeilToInt(center.x + radius));
            minY = Mathf.Max(0, Mathf.FloorToInt(center.y - radius));
            maxY = Mathf.Min(Size.y - 1, Mathf.CeilToInt(center.y + radius));
            minZ = Mathf.Max(0, Mathf.FloorToInt(center.z - radius));
            maxZ = Mathf.Min(Size.z - 1, Mathf.CeilToInt(center.z + radius));
        }

        public void ModifyDensity(Vector3 center, float radius, float delta)
        {
            if (radius <= 0f)
                return;

            GetIterationBounds(center, radius, out int minX, out int maxX, out int minY, out int maxY, out int minZ, out int maxZ);
            
            float radiusSquared = radius * radius;
            float invRadius = 1f / radius;

            for (int x = minX; x <= maxX; x++)
            for (int y = minY; y <= maxY; y++)
            for (int z = minZ; z <= maxZ; z++)
            {
                Vector3 pos = new Vector3(x, y, z);
                float distSquared = (pos - center).sqrMagnitude;
        
                if (distSquared <= radiusSquared)
                {
                    float dist = Mathf.Sqrt(distSquared);
                    float falloff = 1f - dist * invRadius;
                    _voxels[x, y, z].Density += delta * falloff;
                }
            }
        }
        
        public void SetDensityInRadius(Vector3 center, float radius, float targetDensity, float blendFactor = 1f)
        {
            if (radius <= 0f)
                return;

            GetIterationBounds(center, radius, out int minX, out int maxX, out int minY, out int maxY, out int minZ, out int maxZ);
            
            float radiusSquared = radius * radius;
            float invRadius = 1f / radius;

            for (int x = minX; x <= maxX; x++)
            for (int y = minY; y <= maxY; y++)
            for (int z = minZ; z <= maxZ; z++)
            {
                Vector3 pos = new Vector3(x, y, z);
                float distSquared = (pos - center).sqrMagnitude;
        
                if (distSquared <= radiusSquared)
                {
                    float dist = Mathf.Sqrt(distSquared);
                    float falloff = 1f - dist * invRadius;
                    float currentDensity = _voxels[x, y, z].Density;
                    _voxels[x, y, z].Density = Mathf.Lerp(currentDensity, targetDensity, falloff * blendFactor);
                }
            }
        }
        
        /// <summary>
        /// Fills a rectangular region (inclusive) with the given density.
        /// </summary>
        public void FillRegion(Vector3Int min, Vector3Int max, float density)
        {
            min = Vector3Int.Max(min, Vector3Int.zero);
            max = Vector3Int.Min(max, Size - Vector3Int.one);

            for (int x = min.x; x <= max.x; x++)
            for (int y = min.y; y <= max.y; y++)
            for (int z = min.z; z <= max.z; z++)
            {
                _voxels[x, y, z].Density = density;
            }
        }
        
        /// <summary>
        /// Resets all densities to zero.
        /// </summary>
        public void Clear()
        {
            for (int x = 0; x < Size.x; x++)
            for (int y = 0; y < Size.y; y++)
            for (int z = 0; z < Size.z; z++)
            {
                _voxels[x, y, z].Density = 0f;
            }
        }
    }
}
