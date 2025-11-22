using UnityEngine;

namespace VoxelMarchingCubes.Core.Density
{
    public class ImprovedPerlinGenerator : ScaleAwareDensityGenerator
    {
        private readonly float _noiseScale;
        private readonly float _terrainHeight;
        private readonly float _baseHeight;
        private readonly float _falloffDistance;
        private readonly Vector2 _noiseOffset;
        
        private float _cachedNoiseScale;
        private float _cachedTerrainHeight;
        private float _cachedBaseHeight;
        private bool _parametersNeedUpdate = true;
        
        public ImprovedPerlinGenerator(
            float noiseScale, 
            float terrainHeight, 
            float baseHeight, 
            float falloffDistance = 2f,
            Vector2 noiseOffset = default)
        {
            _noiseScale = noiseScale;
            _terrainHeight = terrainHeight;
            _baseHeight = baseHeight;
            _falloffDistance = falloffDistance;
            _noiseOffset = noiseOffset;
        }
        
        protected override void OnScaleChanged(Vector3 newScale)
        {
            _parametersNeedUpdate = true;
        }
        
        private void UpdateCachedParameters()
        {
            if (!_parametersNeedUpdate) return;
            
            _cachedNoiseScale = _noiseScale;
            _cachedTerrainHeight = _terrainHeight;
            _cachedBaseHeight = _baseHeight;
            
            _parametersNeedUpdate = false;
        }
        
        public override float GetDensity(int x, int y, int z)
        {
            UpdateCachedParameters();
            
            float sampleX = x * _cachedNoiseScale + _noiseOffset.x;
            float sampleZ = z * _cachedNoiseScale + _noiseOffset.y;
            
            float noise = Mathf.PerlinNoise(sampleX, sampleZ);
            noise = Mathf.Clamp01(noise);
            
            float currentHeight = y; 
            
            if (currentHeight <= _cachedBaseHeight)
            {
                return _cachedBaseHeight - currentHeight + 1f;
            }
            float surfaceHeight = _cachedBaseHeight + (noise * (_cachedTerrainHeight - _cachedBaseHeight));
            
            float distanceFromBase = currentHeight - _cachedBaseHeight;
            
            if (distanceFromBase <= _falloffDistance)
            {
                float t = Mathf.Clamp01(distanceFromBase / _falloffDistance);
                float baseDensity = _cachedBaseHeight - currentHeight + 1f;
                float surfaceDensity = surfaceHeight - currentHeight;
                return Mathf.Lerp(baseDensity, surfaceDensity, t);
            }
        
            return surfaceHeight - currentHeight;
        }
    }
}