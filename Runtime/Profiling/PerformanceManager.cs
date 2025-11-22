using UnityEngine;
using VoxelMarchingCubes.Profiling.Core;

namespace VoxelMarchingCubes.Profiling
{
    /// <summary>
    /// Central manager for all performance profiling operations.
    /// Singleton pattern for global access.
    /// </summary>
    [DisallowMultipleComponent]
    public class PerformanceManager : MonoBehaviour
    {
        private static PerformanceManager _instance;
        public static PerformanceManager Instance
        {
            get
            {
                // Return if we already have a cached instance
                if (_instance != null)
                    return _instance;

                // Try to find an existing instance in the scene first (prevents duplicates)
                _instance = FindExistingInstance();
                if (_instance != null)
                    return _instance;

                // Create a new instance. Avoid DontDestroyOnLoad here to keep editor safe.
                var go = new GameObject("[PerformanceManager]");
#if UNITY_EDITOR
                if (!Application.isPlaying)
                {
                    // Do not save editor-spawned instance to the scene and keep it hidden
                    go.hideFlags = HideFlags.DontSaveInEditor | HideFlags.HideInHierarchy;
                }
#endif
                _instance = go.AddComponent<PerformanceManager>();
                return _instance;
            }
        }

        [Header("Settings")]
        [SerializeField] private bool enableProfiling = true;
        [SerializeField] private bool logToConsole = false;
        [SerializeField] private float consoleLogInterval = 1f;

        [Header("Performance Thresholds")]
        [SerializeField] private float meshGenerationWarningThreshold = 16f; // ms
        [SerializeField] private float frameTimeWarningThreshold = 33f; // ms (30 FPS)

        private IProfiler _profiler;
        private static readonly IProfiler _nullProfiler = new NullProfiler();
        private float _lastLogTime;

        public IProfiler Profiler => (enableProfiling && _profiler != null) ? _profiler : _nullProfiler;
        public bool IsEnabled => enableProfiling;

        private void Awake()
        {
#if !DEVELOPMENT_BUILD && !UNITY_EDITOR
            enableProfiling = false;
#endif
            if (_instance != null && _instance != this)
            {
                // Enforce singleton
                Destroy(gameObject);
                return;
            }

            _instance = this;

            // Only mark as persistent during play mode to avoid editor exceptions
            if (Application.isPlaying)
            {
                DontDestroyOnLoad(gameObject);
            }
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            _profiler = new UnityProfilerAdapter();
            Debug.Log("Performance profiling ENABLED");
#else
            _profiler = _nullProfiler;
#endif
        }

        private static PerformanceManager FindExistingInstance()
        {
            // First try direct search
            var existing = FindFirstObjectByType<PerformanceManager>();
            if (existing != null)
                return existing;

            // Optionally, try to locate by the known name
            var go = GameObject.Find("[PerformanceManager]");
            if (go != null)
            {
                existing = go.GetComponent<PerformanceManager>();
                if (existing != null) return existing;
            }
            return null;
        }

        private void Update()
        {
            if (!enableProfiling) return;

            // Periodic logging
            if (logToConsole && Time.time - _lastLogTime >= consoleLogInterval)
            {
                PrintReport();
                _lastLogTime = Time.time;
            }

            // Threshold checks (use the configured thresholds to avoid unused-field warnings)
            CheckThresholds();
        }

        public void ResetMetrics()
        {
            Profiler.Reset();
        }

        /// <summary>
        /// Check if a metric exceeds warning threshold
        /// </summary>
        public bool IsMetricOverThreshold(string metricName, float threshold)
        {
            var metric = _profiler.GetMetric(metricName);
            return metric != null && metric.Current > threshold;
        }

        [ContextMenu("Print Performance Report")]
        private void PrintReport()
        {
            Debug.Log("=== Performance Report ===");
            foreach (var metric in _profiler.Metrics.Values)
            {
                Debug.Log(metric.ToString());
            }
        }

        [ContextMenu("Reset All Metrics")]
        private void ResetAll()
        {
            ResetMetrics();
            Debug.Log("All performance metrics reset");
        }

        // Runs lightweight threshold checks to surface potential performance issues.
        // Uses consoleLogInterval as a natural throttle to avoid spamming logs.
        private float _lastThresholdCheckTime;
        private void CheckThresholds()
        {
            // Throttle checks
            if (Time.time - _lastThresholdCheckTime < consoleLogInterval)
                return;
            _lastThresholdCheckTime = Time.time;

            // Frame time check
            float frameMs = Time.deltaTime * 1000f;
            if (frameMs > frameTimeWarningThreshold)
            {
                Debug.LogWarning($"[Performance] High frame time detected: {frameMs:F1} ms > {frameTimeWarningThreshold} ms");
            }

            // Mesh generation time check (aggregated metric from chunks)
            var metric = Profiler.GetMetric("VoxelChunk.MeshGeneration");
            if (metric != null && metric.Current > meshGenerationWarningThreshold)
            {
                Debug.LogWarning($"[Performance] Mesh generation slow: {metric.Current:F1} ms > {meshGenerationWarningThreshold} ms");
            }
        }
    }
}