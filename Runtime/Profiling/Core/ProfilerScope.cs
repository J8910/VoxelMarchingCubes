using System;
using Unity.Profiling;

namespace VoxelMarchingCubes.Profiling.Core
{
    /// <summary>
    /// Disposable scope for automatic profiling using RAII pattern.
    /// Ensures EndSample is always called even if exceptions occur.
    /// </summary>
    public struct ProfilerScope : IDisposable
    {
        private readonly ProfilerMarker _marker;
        private readonly bool _isValid;

        public ProfilerScope(ProfilerMarker marker)
        {
            _marker = marker;
            _isValid = true;
            _marker.Begin();
        }

        public void Dispose()
        {
            if (_isValid)
            {
                _marker.End();
            }
        }
    }
}