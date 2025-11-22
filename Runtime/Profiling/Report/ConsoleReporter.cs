// This reporter is currently unused and excluded from compilation to keep the
// package lean for distribution via Git. If you need console reporting for
// performance metrics, you can restore this file by removing the #if/#endif.
#if false
using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace VoxelMarchingCubes.Profiling
{
    /// <summary>
    /// Reports performance metrics to Unity console.
    /// </summary>
    public class ConsoleReporter : IPerformanceReporter
    {
        private readonly StringBuilder _sb = new StringBuilder();

        public void Report(IReadOnlyDictionary<string, PerformanceMetrics> metrics)
        {
            if (metrics.Count == 0) return;

            _sb.Clear();
            _sb.AppendLine("=== Performance Report ===");

            foreach (var metric in metrics.Values)
            {
                _sb.AppendLine(metric.ToString());
                
                // Warning for high values
                if (metric.Current > 16f)
                {
                    _sb.AppendLine($"WARNING: {metric.Name} exceeded 16ms budget!");
                }
            }

            Debug.Log(_sb.ToString());
        }
    }
}
#endif