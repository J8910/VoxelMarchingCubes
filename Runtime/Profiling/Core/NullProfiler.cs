using System;
using System.Collections.Generic;

namespace VoxelMarchingCubes.Profiling.Core
{
    public class NullProfiler : IProfiler
    {
        private static readonly NullDisposable _nullDisposable = new NullDisposable();
        
        public void BeginSample(string sampleName) { }
        public void EndSample() { }
        public void RecordMetric(string metricName, float value) { }
        public IDisposable Sample(string sampleName) => _nullDisposable;
        public PerformanceMetrics GetMetric(string metricName) => null;
        public IReadOnlyDictionary<string, PerformanceMetrics> Metrics => 
            new Dictionary<string, PerformanceMetrics>();
        public void Reset() { }

        private struct NullDisposable : IDisposable
        {
            public void Dispose() { }
        }
    }
}