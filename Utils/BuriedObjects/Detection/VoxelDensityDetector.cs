using UnityEngine;
using VoxelMarchingCubes.Runtime;
using VoxelMarchingCubes.Utils.BuriedObjects.Core;

namespace VoxelMarchingCubes.Utils.BuriedObjects.Detection
{
    public class VoxelDensityDetector : IExposureDetector
    {
        private readonly float _densityThreshold;
        private readonly int _samplesPerAxis;
        private readonly float _sampleExpansion;

        public VoxelDensityDetector(float densityThreshold = 0.5f, int samplesPerAxis = 3, float sampleExpansion = 0.1f)
        {
            _densityThreshold = densityThreshold;
            _samplesPerAxis = Mathf.Max(2, samplesPerAxis);
            _sampleExpansion = sampleExpansion;
        }

        public ExposureData CalculateExposure(Bounds objectBounds, VoxelTerrain terrain)
        {
            if (terrain == null)
            {
                return new ExposureData(0f, 0, 0, objectBounds.center, objectBounds);
            }
            
            Bounds sampleBounds = new Bounds(objectBounds.center, objectBounds.size * (1f + _sampleExpansion));

            int totalSamples = _samplesPerAxis * _samplesPerAxis * _samplesPerAxis;
            int exposedSamples = 0;
            int sampledPoints = 0;
            
            Vector3 step = new Vector3(
                sampleBounds.size.x / (_samplesPerAxis - 1),
                sampleBounds.size.y / (_samplesPerAxis - 1),
                sampleBounds.size.z / (_samplesPerAxis - 1)
            );
            
            Vector3 startPos = sampleBounds.min;

            for (int x = 0; x < _samplesPerAxis; x++)
            {
                for (int y = 0; y < _samplesPerAxis; y++)
                {
                    for (int z = 0; z < _samplesPerAxis; z++)
                    {
                        Vector3 samplePos = startPos + new Vector3(x * step.x, y * step.y, z * step.z);
                        float density = terrain.GetDensityAtWorldPosition(samplePos);

                        sampledPoints++;
                        
                        if (density < _densityThreshold)
                        {
                            exposedSamples++;
                        }
                    }
                }
            }

            float exposureRatio = sampledPoints > 0 ? (float)exposedSamples / sampledPoints : 0f;

            return new ExposureData(
                exposureRatio,
                exposedSamples,
                sampledPoints,
                objectBounds.center,
                sampleBounds
            );
        }
    }
}