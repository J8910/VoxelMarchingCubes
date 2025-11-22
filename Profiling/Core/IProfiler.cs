using System;
using System.Collections.Generic;

namespace VoxelMarchingCubes.Profiling
{
    /// <summary>
    /// Interface for performance profiling operations.
    /// Follows Strategy Pattern for different profiling implementations.
    /// </summary>
    public interface IProfiler
    {
        /// <summary>
        /// Begin profiling a specific operation
        /// </summary>
        void BeginSample(string sampleName);
        
        /// <summary>
        /// End profiling the current operation
        /// </summary>
        void EndSample();
        
        /// <summary>
        /// Record a metric value
        /// </summary>
        void RecordMetric(string metricName, float value);
        
        /// <summary>
        /// Get a disposable scope for automatic Begin/End (RAII pattern)
        /// </summary>
        IDisposable Sample(string sampleName);
        
        /// <summary>
        /// Get a specific metric by name
        /// </summary>
        PerformanceMetrics GetMetric(string metricName);
        
        /// <summary>
        /// Get all recorded metrics
        /// </summary>
        IReadOnlyDictionary<string, PerformanceMetrics> Metrics { get; }

        /// <summary>
        /// Reset all recorded metrics to initial state.
        /// </summary>
        void Reset();
    }
}