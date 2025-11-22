using UnityEngine;
using VoxelMarchingCubes.Core;

namespace VoxelMarchingCubes.Core.Generators
{
    /// <summary>
    /// Simple density generator that produces a heightmap using Unity's Perlin noise.
    /// Density is calculated as (heightAtXZ - y), after applying scale provided via <see cref="SetScale"/>.
    /// </summary>
    public sealed class PerlinHeightGenerator : IVoxelDensityGenerator
    {
        private readonly float _noiseScale;
        private readonly float _terrainHeight;
        private Vector3 _scale = Vector3.one;

        public PerlinHeightGenerator(float noiseScale, float terrainHeight)
        {
            _noiseScale = noiseScale;
            _terrainHeight = terrainHeight;
        }

        /// <inheritdoc />
        public void SetScale(Vector3 scale)
        {
            _scale = scale;
        }

        /// <inheritdoc />
        public float GetDensity(int x, int y, int z)
        {
            float scaledX = x / _scale.x;
            float scaledZ = z / _scale.z;
            float scaledY = y / _scale.y;

            float noise = Mathf.PerlinNoise(scaledX * _noiseScale, scaledZ * _noiseScale);
            float height = noise * _terrainHeight;

            return height - scaledY;
        }
    }
}
