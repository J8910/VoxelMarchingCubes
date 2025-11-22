using UnityEngine;

namespace VoxelMarchingCubes.Core.Density
{
    [System.Serializable]
    public class BaseSolidLayer : IDensityLayer
    {
        [SerializeField] private float _baseHeight;
        [SerializeField] private float _falloffDistance = 2f;
        
        private Vector3 _scale = Vector3.one;

        public int Priority => 0; // Lower priority means higher priority

        public BaseSolidLayer(float baseHeight, float falloffDistance = 2f)
        {
            _baseHeight = baseHeight;
            _falloffDistance = falloffDistance;
        }

        public float GetDensity(int x, int y, int z)
        {
            float scaledY = y / _scale.y;
            float scaledBaseHeight = _baseHeight / _scale.y;
            
            if (scaledY <= scaledBaseHeight)
            {
                return scaledBaseHeight - scaledY + 1f; // +1 to be > isoLevel
            }
            
            float distanceFromBase = scaledY - scaledBaseHeight;
            
            if (distanceFromBase <= _falloffDistance)
            {
                float t = distanceFromBase / _falloffDistance;
                float solidDensity = scaledBaseHeight - scaledY + 1f;
                return Mathf.Lerp(solidDensity, 0f, t);
            }
            
            return 0f;
        }
        
        public void SetScale(Vector3 scale)
        {
            _scale = scale;
        }
    }
}