using System;
using System.Collections.Generic;
using UnityEngine;
using VoxelMarchingCubes.Core;
using VoxelMarchingCubes.Profiling;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace VoxelMarchingCubes.Runtime
{
    [ExecuteAlways]
    public class VoxelTerrain : MonoBehaviour, IModificableVoxel
    {
        [Header("Terrain Settings")]
        [SerializeField] private Material terrainMaterial;
        [Tooltip("Enable runtime debug logging for terrain events and actions.")]
        [SerializeField] private bool enableLogging = false;
        
        [Header("Chunk Settings")]
        [SerializeField] private Vector3Int chunkCount = new(4, 1, 4);
        [SerializeField] private Vector3Int chunkSize = new(32, 16, 32);
        [SerializeField] private float isoLevel = 0.5f;
        
        [Header("Generation Settings")]
        [SerializeField] private float noiseScale = 0.1f;
        [SerializeField] private float terrainHeight = 0.7f;
        [SerializeField] private float baseHeight = 0.3f;
        [SerializeField] private float noiseFalloff = 2f;
        [SerializeField] private Vector2 noiseOffset = Vector2.zero;
        [SerializeField] private VoxelMarchingCubes.Core.Density.DensityGeneratorFactory.GeneratorType generatorType = 
            VoxelMarchingCubes.Core.Density.DensityGeneratorFactory.GeneratorType.ImprovedPerlin;

        [Header("Adaptive Resolution")]
        [SerializeField] private bool useAdaptiveResolution = true;
        [SerializeField] private VoxelMarchingCubes.Core.Scaling.AdaptiveVoxelResolution adaptiveResolution = new();
        [SerializeField] private Vector3 targetWorldSize = new Vector3(10f, 5f, 10f);
        [SerializeField] private float voxelsPerUnit = 4f;
        
        [Header("Debug")]
        [SerializeField] private bool showGizmos = true;

        [SerializeField] private Color terrainBoundsColor = new Color(0f, 1f, 1f, 0.2f);
        [SerializeField] private Color chunkBoundsColor = new Color(1f, 0.5f, 0f, 0.2f);
        
        public event Action<Bounds> OnTerrainChanged;
        public event Action OnTerrainInitialized;
        private IVoxelDensityGenerator _densityGenerator;
        private List<VoxelChunk> _chunks = new List<VoxelChunk>();
        private Dictionary<Vector2Int, VoxelChunk> _chunkMap = new Dictionary<Vector2Int, VoxelChunk>();
        private bool _isInitialized = false;
        private Material _sharedFallbackMaterial; // created once if needed
        
        #region Public Properties
        
        public bool UseAdaptiveResolution => useAdaptiveResolution;
        public Vector3 TargetWorldSize => targetWorldSize;
        public Core.Scaling.AdaptiveVoxelResolution AdaptiveResolution => adaptiveResolution; 
        public float VoxelsPerUnit => voxelsPerUnit;
        public Vector3Int ChunkCount => chunkCount;
        public Vector3Int ChunkSize => chunkSize;
        public float IsoLevel => isoLevel;
        public bool IsInitialized => _isInitialized;
        
        #endregion
        
        #region Unity Lifecycle

        private void Start()
        {
            if (!_isInitialized)
            {
                InitializeTerrain();
            }
        }

        private void OnValidate()
        {
            if (useAdaptiveResolution)
            {
                UpdateAdaptiveResolution();
            }
            else
            {
                chunkCount = Vector3Int.Max(chunkCount, Vector3Int.one);
                chunkSize = Vector3Int.Max(chunkSize, new Vector3Int(4, 4, 4));
            }
            
            isoLevel = Mathf.Clamp01(isoLevel);
            
            if (useAdaptiveResolution && !Application.isPlaying) 
            {
                var data = adaptiveResolution.CalculateOptimalResolution(transform.localScale);
                if (data.IsValid)
                {
                    chunkSize = data.ChunkSize; // Uncomment to force chunk size
                }
            }
        }
        
        private void UpdateAdaptiveResolution()
        {
            if (adaptiveResolution == null) return;
            
            adaptiveResolution.SetTargetSize(targetWorldSize);
            adaptiveResolution.SetVoxelDensity(voxelsPerUnit);
            
            var resolutionData = adaptiveResolution.CalculateOptimalResolution(transform.localScale);
            
            if (resolutionData.IsValid)
            {
                if (transform.localScale.magnitude < 0.5f)
                {
                    chunkCount = Vector3Int.one;
                }
                
                if (_densityGenerator != null && _isInitialized)
                {
                    _densityGenerator = CreateDensityGenerator();
                }
                
                if (Application.isEditor && !Application.isPlaying)
                {
                    resolutionData.LogInfo($"OnValidate - Scale: {transform.localScale}");
                }
            }
        }
        
        #endregion
        
        #region Initialization

        public void InitializeTerrain()
        {
            _densityGenerator = CreateDensityGenerator();
            GenerateChunks();
            _isInitialized = true;
            try
            {
                OnTerrainInitialized?.Invoke();
            }
            catch (Exception ex)
            {
                if (enableLogging)
                {
                    Debug.LogError($"[VoxelTerrain] Exception during OnTerrainInitialized invocation: {ex}");
                }
            }
        }

        protected virtual IVoxelDensityGenerator CreateDensityGenerator()
        {
            float adjustedTerrainHeight = chunkSize.y * terrainHeight;
            float adjustedBaseHeight = chunkSize.y * baseHeight;
            
            var settings = new VoxelMarchingCubes.Core.Density.TerrainGenerationSettings(
                noiseScale,
                adjustedTerrainHeight,
                adjustedBaseHeight,
                noiseFalloff
            );
            settings.noiseOffset = noiseOffset;
            
            return VoxelMarchingCubes.Core.Density.DensityGeneratorFactory.Create(generatorType, settings);

        }

        [ContextMenu("Regenerate Terrain")]
        public void RegenerateTerrain()
        {
            ClearChunks();
            InitializeTerrain();
        }
        
        
        #endregion
        
        #region Chunk Management

        private void GenerateChunks()
        {
            var profiler = PerformanceManager.Instance.Profiler;
            profiler.BeginSample("VoxelTerrain.GenerateChunks");
            ClearChunks();
            // Pre-size collections to avoid reallocations
            int totalChunks = Mathf.Max(1, chunkCount.x * chunkCount.z);
            if (_chunks.Capacity < totalChunks) _chunks.Capacity = totalChunks;
            _chunkMap = new Dictionary<Vector2Int, VoxelChunk>(totalChunks);
            profiler.RecordMetric("VoxelTerrain.ChunkCount", totalChunks);
            
            for (int cx = 0; cx < chunkCount.x; cx++)
            for (int cz = 0; cz < chunkCount.z; cz++)
            {
                CreateChunk(cx, cz);
            }
            profiler.EndSample();
                
        }

        private void CreateChunk(int cx, int cz)
        {
            GameObject chunkObject = new GameObject($"Chunk_{cx}_{cz}");
            chunkObject.transform.SetParent(transform, false);
            
            chunkObject.transform.localPosition = new Vector3(
                cx * chunkSize.x,
                0f,
                cz * chunkSize.z);
            
            var chunk = chunkObject.AddComponent<VoxelChunk>();
            chunk.Initialize(chunkSize, isoLevel);
            
            var meshRenderer = chunkObject.GetComponent<MeshRenderer>();
            if (meshRenderer != null && terrainMaterial != null)
            {
                meshRenderer.sharedMaterial = terrainMaterial;
            }
            else if (meshRenderer != null)
            {
                // Reuse a single fallback material to avoid per-chunk allocations
                if (_sharedFallbackMaterial == null)
                {
                    var shader = Shader.Find("Standard");
                    _sharedFallbackMaterial = shader != null ? new Material(shader) { name = "VoxelTerrain_Fallback" } : null;
                }
                if (_sharedFallbackMaterial != null)
                {
                    meshRenderer.sharedMaterial = _sharedFallbackMaterial;
                }
            }
            
            var profiler = PerformanceManager.Instance.Profiler;
            profiler.BeginSample("VoxelTerrain.CreateChunk");
            FillChunk(chunk, cx, cz);
            chunk.RebuildMesh();
            profiler.EndSample();
            
            chunk.OnModified += HandleChunkModified;
            _chunks.Add(chunk);
            _chunkMap[new Vector2Int(cx, cz)] = chunk;
        }

        private void ClearChunks()
        {
            foreach (var chunk in _chunks)
            {
                if (chunk != null)
                {
                    chunk.OnModified -= HandleChunkModified;
                }
            }
            
            VoxelChunk[] existingChunks = GetComponentsInChildren<VoxelChunk>();
            foreach (var chunk in existingChunks)
            {
                if (chunk != null)
                {
#if UNITY_EDITOR
                    if (Application.isPlaying) 
                        Destroy(chunk.gameObject);
                    else
                        DestroyImmediate(chunk.gameObject);
#else
                    Destroy(chunk.gameObject);
#endif               
                }
            }
            
            _chunks.Clear();
            _chunkMap.Clear();
        }

        private void FillChunk(VoxelChunk chunk, int cx, int cz)
        {
            var profiler = PerformanceManager.Instance.Profiler;
            profiler.BeginSample("VoxelTerrain.FillChunk");
            Vector3 scale = transform.lossyScale;

            if (_densityGenerator != null)
            {
                _densityGenerator.SetScale(scale);
            }
            
            // Iterate with z as the innermost loop for better cache locality on [x,y,z] arrays
            for (int x = 0; x <= chunkSize.x; x++)
            for (int y = 0; y <= chunkSize.y; y++)
            for (int z = 0; z <= chunkSize.z; z++)
            {
                int wx = cx * chunkSize.x + x;
                int wy = y;
                int wz = cz * chunkSize.z + z;
                
                float density = _densityGenerator.GetDensity(wx, wy, wz);
                chunk.SetDensity(x, y, z, density);
            }
            profiler.EndSample();
        }
        
        #endregion
        
        #region Terrain Modification (IModificableVoxel)

        public void ModifyTerrain(Vector3 worldPosition, float radius, float delta)
        {
            Vector3 localPos = transform.InverseTransformPoint(worldPosition);
        
            Vector3 scale = transform.lossyScale;
            float avgScale = (scale.x + scale.y + scale.z) / 3f;
            float scaledRadius = radius / avgScale;
            
            foreach (var chunk in _chunks)
            {
                if (chunk == null) continue;

                if (IsChunkInRange(chunk, localPos, scaledRadius))
                {
                    Vector3 chunkLocal = localPos - chunk.transform.localPosition;
                    chunk.ModifyTerrain(chunkLocal, scaledRadius, delta, notifyNeighbors: true);
                }
            }
            
            NotifyTerrainChange(worldPosition, radius);
        }
        
        private void NotifyTerrainChange(Vector3 center, float radius)
        {
            if (enableLogging)
                Debug.Log($"<color=yellow>[VoxelTerrain]</color> Notifying terrain change at {center}, radius: {radius}");
            
            Bounds bounds = new Bounds(center, Vector3.one * (radius * 2f));
            OnTerrainChanged?.Invoke(bounds);
        }
        
        public void SetDensityAtPosition(Vector3 worldPosition, float radius, float targetDensity, float blendFactor = 1f)
        {
            Vector3 localPos = transform.InverseTransformPoint(worldPosition);
            
            Vector3 scale = transform.lossyScale;
            float avgScale = (scale.x + scale.y + scale.z) / 3f;
            float scaledRadius = radius / avgScale;

            foreach (var chunk in _chunks)
            {
                if (chunk == null) continue;

                if (IsChunkInRange(chunk, localPos, scaledRadius))
                {
                    Vector3 chunkLocal = localPos - chunk.transform.localPosition;
                    chunk.SetDensityInRadius(chunkLocal, scaledRadius, targetDensity, blendFactor);
                }
            }
            
            NotifyTerrainChange(worldPosition, radius);
        }
        
        public float GetDensityAtWorldPosition(Vector3 worldPosition)
        {
            VoxelChunk chunk = GetChunkAtWorldPosition(worldPosition);
            if (chunk == null) return 0f;

            Vector3 localPos = chunk.WorldToLocal(worldPosition);
            Vector3Int gridPos = new Vector3Int(
                Mathf.RoundToInt(localPos.x),
                Mathf.RoundToInt(localPos.y),
                Mathf.RoundToInt(localPos.z)
            );

            return chunk.GetDensity(gridPos);
        }
        
        #endregion
        
        #region Neighbor Propagation

        private void HandleChunkModified(VoxelChunk sourceChunk, Vector3 localPos, float radius, float delta)
        {
            foreach (var chunk in _chunks)
            {
                if (chunk == sourceChunk) continue;
                
                Vector3 sourceWorld = sourceChunk.transform.TransformPoint(localPos);
                Vector3 targetLocal = chunk.WorldToLocal(sourceWorld);

                if (IsPositionInChunkRange(targetLocal, chunk.Size, radius))
                {
                    chunk.ModifyTerrainInternal(targetLocal, radius, delta);
                }
            }
        }

        private bool IsChunkInRange(VoxelChunk chunk, Vector3 localPosition, float radius)
        {
            Vector3 chunkLocal = localPosition - chunk.transform.localPosition;
            Vector3 chunkCenter = new Vector3(chunkSize.x / 2f, chunkSize.y / 2f, chunkSize.z / 2f);
            
            float maxDistance = radius + Mathf.Sqrt(
                chunkSize.x * chunkSize.x + 
                chunkSize.y * chunkSize.y + 
                chunkSize.z * chunkSize.z
            ) / 2f;
            
            return (chunkLocal - chunkCenter).sqrMagnitude <= (maxDistance + 4f) * (maxDistance + 4f);
        }
        
        private bool IsPositionInChunkRange(Vector3 position, Vector3Int chunkSize, float radius)
        {
            float propagationPadding = 3.0f; 
            float effectiveRadius = radius + propagationPadding;
            
            Vector3 min = new Vector3(-effectiveRadius, -effectiveRadius, -effectiveRadius);
            Vector3 max = new Vector3(
                chunkSize.x + effectiveRadius, 
                chunkSize.y + effectiveRadius, 
                chunkSize.z + effectiveRadius
            );
            return position.x >= min.x && position.x <= max.x &&
                   position.y >= min.y && position.y <= max.y &&
                   position.z >= min.z && position.z <= max.z;
        }
        
        #endregion
        
        public VoxelChunk GetChunkAtWorldPosition(Vector3 worldPosition)
        {
            Vector3 localPos = transform.InverseTransformPoint(worldPosition);
            int cx = Mathf.FloorToInt(localPos.x / chunkSize.x);
            int cz = Mathf.FloorToInt(localPos.z / chunkSize.z);
            
            Vector2Int key = new Vector2Int(cx, cz);
            return _chunkMap.TryGetValue(key, out VoxelChunk chunk) ? chunk : null;
        }
        
        
        #region Gizmos

        private void OnDrawGizmos()
        {
            if (!showGizmos) return;
            
            Vector3 scale = Application.isPlaying ? transform.lossyScale : transform.localScale;
            
            // Draw total terrain bounds
            Gizmos.color = terrainBoundsColor;
            Vector3 totalSize = Vector3.Scale(
                new Vector3(chunkCount.x * chunkSize.x, chunkSize.y, chunkCount.z * chunkSize.z),
                scale
            );
            Vector3 center = transform.position + Vector3.Scale(totalSize / 2f, Vector3.one);
            Gizmos.DrawWireCube(center, totalSize);
            
            // Draw individual chunk bounds
            Gizmos.color = chunkBoundsColor;
            for (int cx = 0; cx < chunkCount.x; cx++)
            for (int cz = 0; cz < chunkCount.z; cz++)
            {
                Vector3 chunkLocalPos = new Vector3(cx * chunkSize.x, 0f, cz * chunkSize.z);
                Vector3 chunkWorldPos = transform.TransformPoint(chunkLocalPos);
                Vector3 chunkWorldSize = Vector3.Scale(
                    new Vector3(chunkSize.x, chunkSize.y, chunkSize.z),
                    scale
                );
                Vector3 chunkCenter = chunkWorldPos + Vector3.Scale(chunkWorldSize / 2f, Vector3.one);
                Gizmos.DrawWireCube(chunkCenter, chunkWorldSize);
            }
        }

        #endregion
        
        #region Debug Utilities

#if UNITY_EDITOR
        [ContextMenu("Debug: Print Scale Info")]
        private void DebugScaleInfo()
        {
            Vector3 scale = transform.lossyScale;
            if (enableLogging)
            {
                Debug.Log($"=== VoxelTerrain Scale Debug ===");
                Debug.Log($"Transform Scale: {scale}");
                Debug.Log($"Chunk Count: {chunkCount}");
                Debug.Log($"Chunk Size (voxels): {chunkSize}");
                Debug.Log($"Total Voxels per Chunk: {chunkSize.x * chunkSize.y * chunkSize.z}");
                Debug.Log($"Visual Size (world units): {Vector3.Scale(new Vector3(chunkSize.x, chunkSize.y, chunkSize.z), scale)}");
                Debug.Log($"Noise Scale: {noiseScale}");
            }
            
            if (_chunks != null && _chunks.Count > 0)
            {
                var firstChunk = _chunks[0];
                if (firstChunk != null && firstChunk.VoxelGrid != null)
                {
                    if (enableLogging)
                    {
                        Debug.Log($"First Chunk Grid Size: {firstChunk.VoxelGrid.Size}");
                        Debug.Log($"First Chunk Voxel Count: {firstChunk.VoxelGrid.Size.x * firstChunk.VoxelGrid.Size.y * firstChunk.VoxelGrid.Size.z}");
                    }
                }
            }
        }

        [ContextMenu("Debug: Test Density at Origin")]
        private void DebugDensityAtOrigin()
        {
            if (_densityGenerator == null)
            {
                Debug.LogWarning("Density generator not initialized");
                return;
            }
            
            _densityGenerator.SetScale(transform.lossyScale);
            
            if (enableLogging)
            {
                Debug.Log($"=== Density Sampling Test (Scale: {transform.lossyScale}) ===");
                for (int i = 0; i < 5; i++)
                {
                    float density = _densityGenerator.GetDensity(i, 0, 0);
                    Debug.Log($"Density at ({i}, 0, 0): {density:F3}");
                }
            }
        }
#endif

        #endregion
    
    }
}
