using UnityEngine;

namespace VoxelMarchingCubes.Utils.BuriedObjects.Core
{
    public class ExposureData
    {
        public float Exposure { get; }
        public int TotalSamples { get; }
        public int ExposedSamples { get; }
        public Vector3 CenterPosition { get; }
        public Bounds SampleBounds { get; }
        
        public ExposureData(float exposure, int totalSamples, int exposedSamples, Vector3 centerPosition, Bounds sampleBounds)
        {
            Exposure = Mathf.Clamp01(exposure);
            TotalSamples = totalSamples;
            ExposedSamples = exposedSamples;
            CenterPosition = centerPosition;
            SampleBounds = sampleBounds;
        }
        
        public bool IsFullyExposed(float threshold = 0.8f) => Exposure >= threshold;
        public bool IsPartiallyExposed(float threshold = 0.2f) => Exposure >= threshold && Exposure < 0.8f;
        public bool IsBuried(float threshold = 0.2f) => Exposure < threshold;
        
        public override string ToString()
        {
            return $"Exposure: {Exposure:P0} ({ExposedSamples}/{TotalSamples} samples)";
        }
    }
}