using System;
using System.Collections.Generic;
using Unity.Profiling;

namespace VoxelMarchingCubes.Profiling.Core
{
    /// <summary>
    /// Adapter for Unity's built-in Profiler.
    /// Provides clean interface while using Unity's profiling infrastructure.
    /// </summary>
    public class UnityProfilerAdapter : IProfiler
    {
        private readonly Dictionary<string, ProfilerMarker> _markers = new();
        private readonly Dictionary<string, PerformanceMetrics> _metrics = new();
        private readonly System.Diagnostics.Stopwatch _stopwatch = new();
        private string _currentSample;

        public IReadOnlyDictionary<string, PerformanceMetrics> Metrics => _metrics;

        public void BeginSample(string sampleName)
        {
            if (!_markers.TryGetValue(sampleName, out var marker))
            {
                marker = new ProfilerMarker(sampleName);
                _markers[sampleName] = marker;
            }

            marker.Begin();
            _currentSample = sampleName;
            _stopwatch.Restart();
        }

        public void EndSample()
        {
            _stopwatch.Stop();
            
            if (!string.IsNullOrEmpty(_currentSample))
            {
                if (_markers.TryGetValue(_currentSample, out var marker))
                {
                    marker.End();
                }

                // Record timing
                float ms = (float)_stopwatch.Elapsed.TotalMilliseconds;
                RecordMetric(_currentSample, ms);
                
                _currentSample = null;
            }
        }

        public void RecordMetric(string metricName, float value)
        {
            if (!_metrics.TryGetValue(metricName, out var metric))
            {
                metric = new PerformanceMetrics(metricName);
                _metrics[metricName] = metric;
            }

            metric.RecordSample(value);
        }

        public IDisposable Sample(string sampleName)
        {
            if (!_markers.TryGetValue(sampleName, out var marker))
            {
                marker = new ProfilerMarker(sampleName);
                _markers[sampleName] = marker;
            }

            return new ProfilerScope(marker);
        }

        public void Reset()
        {
            foreach (var metric in _metrics.Values)
            {
                metric.Reset();
            }
        }

        public PerformanceMetrics GetMetric(string metricName)
        {
            return _metrics.TryGetValue(metricName, out var metric) ? metric : null;
        }
    }
}