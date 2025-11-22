using UnityEngine;

namespace VoxelMarchingCubes.Core.Density
{
    public class SolidBaseNoiseGenerator : IVoxelDensityGenerator
    {
        private readonly LayeredDensityGenerator _layeredGenerator;
        
        public SolidBaseNoiseGenerator(float noiseScale, float terrainHeight, float baseHeight, float noiseFalloff = 2f)
        {
            _layeredGenerator = new LayeredDensityGenerator();
            
            float noiseAmplitude = terrainHeight - baseHeight;
            
            _layeredGenerator
                .WithBaseSolid(baseHeight, noiseFalloff)
                .WithNoiseSurface(noiseScale, terrainHeight, noiseAmplitude, baseHeight)
                .SetCombineMode(LayeredDensityGenerator.DensityCombineMode.Maximum);
        }
        
        public float GetDensity(int x, int y, int z)
        {
            return _layeredGenerator.GetDensity(x, y, z);
        }
        
        public void SetScale(Vector3 scale)
        {
            _layeredGenerator.SetScale(scale);
        }
    }
}