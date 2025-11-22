using UnityEngine;
using VoxelMarchingCubes.Core.Generators;

namespace VoxelMarchingCubes.Core.Density
{
    /// <summary>
    /// Factory for creating different implementations of <see cref="IVoxelDensityGenerator"/>.
    /// </summary>
    public static class DensityGeneratorFactory
    {
        public enum GeneratorType
        {
            Simple,
            Layered,
            Perlin,
            ImprovedPerlin
        }
        
        /// <summary>
        /// Create a density generator instance for the given type and settings.
        /// </summary>
        /// <param name="type">Generator type.</param>
        /// <param name="settings">Settings used by generators that require them. Can be null when type is <see cref="GeneratorType.Simple"/>.</param>
        /// <returns>An instance of <see cref="IVoxelDensityGenerator"/>.</returns>
        /// <exception cref="System.ArgumentNullException">Thrown when settings is null for generator types that require settings.</exception>
        /// <exception cref="System.ArgumentException">Thrown when an unknown generator type is supplied.</exception>
        public static IVoxelDensityGenerator Create(
            GeneratorType type,
            TerrainGenerationSettings settings)
        {
            // Only Simple does not require settings
            if (type != GeneratorType.Simple && settings == null)
                throw new System.ArgumentNullException(nameof(settings), "Settings must be provided for this generator type.");

            return type switch
            {
                GeneratorType.Simple => new SimpleDensityGenerator(),
                GeneratorType.Layered => CreateLayeredGenerator(settings),
                GeneratorType.Perlin => new PerlinHeightGenerator(settings.noiseScale, settings.terrainHeight),
                GeneratorType.ImprovedPerlin => CreateImprovedPerlinGenerator(settings),
                _ => throw new System.ArgumentException($"Unknown generator type: {type}")
            };
        }
        
        private static LayeredDensityGenerator CreateLayeredGenerator(TerrainGenerationSettings settings)
        {
            var generator = new LayeredDensityGenerator();
            
            generator.WithBaseSolid(settings.baseHeight, settings.falloffDistance);
            generator.WithNoiseSurface(
                settings.noiseScale,
                settings.terrainHeight,
                settings.terrainHeight - settings.baseHeight,
                settings.baseHeight
            );
            generator.SetCombineMode(LayeredDensityGenerator.DensityCombineMode.Maximum);
            
            return generator;
        }
        
        private static ImprovedPerlinGenerator CreateImprovedPerlinGenerator(TerrainGenerationSettings settings)
        {
            return new ImprovedPerlinGenerator(
                settings.noiseScale,
                settings.terrainHeight,
                settings.baseHeight,
                settings.falloffDistance,
                settings.noiseOffset
            );
        }
    }
    
    [System.Serializable]
    public class TerrainGenerationSettings
    {
        public float noiseScale = 0.1f;
        public float terrainHeight = 8f;
        public float baseHeight = 4f;
        public float falloffDistance = 2f;
        public Vector2 noiseOffset = Vector2.zero;
        
        public TerrainGenerationSettings() { }
        
        public TerrainGenerationSettings(float noiseScale, float terrainHeight, float baseHeight, float falloffDistance = 2f)
        {
            this.noiseScale = noiseScale;
            this.terrainHeight = terrainHeight;
            this.baseHeight = baseHeight;
            this.falloffDistance = falloffDistance;
        }
    }
}