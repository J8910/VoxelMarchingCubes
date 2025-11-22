using UnityEngine;

namespace VoxelMarchingCubes.Core.Density
{
    [System.Serializable]
    public class NoiseSurfaceLayer : IDensityLayer
    {
        [SerializeField] private float _noiseScale = 0.1f;
        [SerializeField] private float _surfaceHeight = 8f;
        [SerializeField] private float _noiseAmplitude = 4f;
        [SerializeField] private float _startHeight = 0f;
        [SerializeField] private Vector2 _noiseOffset = Vector2.zero;
        
        private Vector3 _scale = Vector3.one;
        
        public int Priority => 10;
        
        public NoiseSurfaceLayer(float noiseScale, float surfaceHeight, float noiseAmplitude, float startHeight = 0f)
        {
            _noiseScale = noiseScale;
            _surfaceHeight = surfaceHeight;
            _noiseAmplitude = noiseAmplitude;
            _startHeight = startHeight;
        }
        
        public float GetDensity(int x, int y, int z)
        {
            float scaledX = x / _scale.x;
            float scaledY = y / _scale.y;
            float scaledZ = z / _scale.z;
            float scaledStartHeight = _startHeight / _scale.y;
            
            if (scaledY < scaledStartHeight)
            {
                return 0f;
            }
            
            float noise = Mathf.PerlinNoise(
                (scaledX + _noiseOffset.x) * _noiseScale, 
                (scaledZ + _noiseOffset.y) * _noiseScale
            );
            
            float terrainHeight = scaledStartHeight + (noise * _noiseAmplitude);
            
            terrainHeight = Mathf.Min(terrainHeight, _surfaceHeight / _scale.y);
            
            return terrainHeight - scaledY;
        }
        
        public void SetScale(Vector3 scale)
        {
            _scale = scale;
        }
    }
}