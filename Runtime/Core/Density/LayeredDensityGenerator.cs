using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace VoxelMarchingCubes.Core.Density
{
    [System.Serializable]
    public class LayeredDensityGenerator : IVoxelDensityGenerator
    {
        [SerializeField] private List<IDensityLayer> _layers = new List<IDensityLayer>();
        [SerializeField] private DensityCombineMode _combineMode = DensityCombineMode.Additive;
        
        private Vector3 _scale = Vector3.one;

        public enum DensityCombineMode
        {
            Additive,
            Maximum,
            Minimum,
            Override
        }

        public LayeredDensityGenerator()
        {
            _layers = new List<IDensityLayer>();
        }

        public void AddLayer(IDensityLayer layer)
        {
            _layers.Add(layer);
            _layers = _layers.OrderBy(l => l.Priority).ToList();
        }

        public void RemoveLayer(IDensityLayer layer)
        {
            _layers.Remove(layer);
        }

        public void ClearLayers()
        {
            _layers.Clear();
        }
        
        public float GetDensity(int x, int y, int z)
        {
            if (_layers.Count == 0) return 0f;
            
            switch (_combineMode)
            {
                case DensityCombineMode.Additive:
                    return GetAdditiveDensity(x, y, z);
                    
                case DensityCombineMode.Maximum:
                    return GetMaximumDensity(x, y, z);
                    
                case DensityCombineMode.Minimum:
                    return GetMinimumDensity(x, y, z);
                    
                case DensityCombineMode.Override:
                    return GetOverrideDensity(x, y, z);
                    
                default:
                    return GetAdditiveDensity(x, y, z);
            }
        }
        
        private float GetAdditiveDensity(int x, int y, int z)
        {
            float totalDensity = 0f;
            foreach (var layer in _layers)
            {
                totalDensity += layer.GetDensity(x, y, z);
            }
            return totalDensity;
        }
        
        private float GetMaximumDensity(int x, int y, int z)
        {
            float maxDensity = float.MinValue;
            foreach (var layer in _layers)
            {
                float density = layer.GetDensity(x, y, z);
                if (density > maxDensity)
                    maxDensity = density;
            }
            return maxDensity == float.MinValue ? 0f : maxDensity;
        }
        
        private float GetMinimumDensity(int x, int y, int z)
        {
            float minDensity = float.MaxValue;
            foreach (var layer in _layers)
            {
                float density = layer.GetDensity(x, y, z);
                if (density < minDensity)
                    minDensity = density;
            }
            return minDensity == float.MaxValue ? 0f : minDensity;
        }
        
        private float GetOverrideDensity(int x, int y, int z)
        {
            foreach (var layer in _layers)
            {
                float density = layer.GetDensity(x, y, z);
                if (Mathf.Abs(density) > 0.001f)
                    return density;
            }
            return 0f;
        }
        
        public void SetScale(Vector3 scale)
        {
            _scale = scale;
            foreach (var layer in _layers)
            {
                layer.SetScale(scale);
            }
        }
        
        public void SetCombineMode(DensityCombineMode mode)
        {
            _combineMode = mode;
        }
        
        public LayeredDensityGenerator WithBaseSolid(float height, float falloff = 2f)
        {
            AddLayer(new BaseSolidLayer(height, falloff));
            return this;
        }
        
        public LayeredDensityGenerator WithNoiseSurface(float noiseScale, float surfaceHeight, float amplitude, float startHeight = 0f)
        {
            AddLayer(new NoiseSurfaceLayer(noiseScale, surfaceHeight, amplitude, startHeight));
            return this;
        }
    }
}