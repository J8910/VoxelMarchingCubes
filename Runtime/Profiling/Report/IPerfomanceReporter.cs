// This interface is currently unused and excluded from compilation to keep the
// package minimal and free of dead code. Remove the #if/#endif to restore.
#if false
using System.Collections.Generic;

namespace VoxelMarchingCubes.Profiling
{
    /// <summary>
    /// Interface for performance reporting implementations.
    /// Follows Strategy Pattern for different output formats.
    /// </summary>
    public interface IPerformanceReporter
    {
        void Report(IReadOnlyDictionary<string, PerformanceMetrics> metrics);
    }
}
#endif