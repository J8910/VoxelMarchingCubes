using System.Collections.Generic;
using UnityEngine;
using VoxelMarchingCubes.Runtime;
using VoxelMarchingCubes.Utils.BuriedObjects.Core;
using VoxelMarchingCubes.Utils.BuriedObjects.Spatial;

namespace VoxelMarchingCubes.Utils.BuriedObjects.Managers
{
    [DefaultExecutionOrder(-10)]
    public class BuriedObjectManager : MonoBehaviour
    {
        public static BuriedObjectManager Instance { get; private set; }
        public static bool HasInstance => Instance != null;
        
        [Header("Settings")]
        [SerializeField] private VoxelTerrain voxelTerrain;
        [SerializeField] private bool updateAllOnStart = true;
        [Header("Spatial Indexing")]
        [Tooltip("Spatial index implementation to use for locating buried objects affected by terrain changes.")]
        [SerializeField] private SpatialIndexType spatialIndexType = SpatialIndexType.Octree;
        [Tooltip("Cell size for the grid index (used only when SpatialIndexType = Grid).")]
        [SerializeField] private float spatialCellSize = 32f; 
        [Tooltip("Max depth for the octree (used only when SpatialIndexType = Octree). Higher values allow finer partitioning.")]
        [SerializeField] private int octreeMaxDepth = 6;
        [Tooltip("Max items per node before subdivision (used only when SpatialIndexType = Octree).")]
        [SerializeField] private int octreeNodeCapacity = 8;
        [Tooltip("Enable verbose runtime logging for buried object manager.")]
        [SerializeField] private bool enableLogging = false;
        [Tooltip("When true, the spatial index refreshes object bounds before processing a terrain change event.")]
        [SerializeField] private bool refreshIndexOnTerrainChange = true;

        private ISpatialIndex<BuriedObject> _spatialIndex;
        private List<BuriedObject> _allObjects = new List<BuriedObject>();
        private bool _isSubscribed = false;
        private Bounds _terrainWorldBounds;

        public enum SpatialIndexType
        {
            Grid,
            Octree
        }
        
        #region Singleton
        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;

            if (voxelTerrain == null)
            {
                voxelTerrain = FindFirstObjectByType<VoxelTerrain>();
                if (voxelTerrain == null)
                {
                    Debug.LogError("[BuriedObjectManager] No VoxelTerrain found in scene.");
                }
            }
                
        }
        
        private void OnDestroy()
        {
            if (Instance == this)
            {
                Instance = null;
            }
        }
        #endregion
        
        #region Unity Lifecycle
        
        private void OnEnable()
        {
            SubscribeToTerrain();
            InitializeSpatialIndex();
            RebuildSpatialIndex();
        }
        
        private void OnDisable()
        {
            UnsubscribeFromTerrain();
        }
        private void Start()
        {
            if (enableLogging)
                Debug.Log($"<color=cyan>[BuriedObjectManager]</color> Start called. Objects registered: {_allObjects.Count}");
            
            if (updateAllOnStart)
            {
                if (enableLogging)
                    Debug.Log($"<color=cyan>[BuriedObjectManager]</color> Performing initial update for {_allObjects.Count} objects");
                ForceUpdateAll();
            }
        }
        
        #endregion
        
        #region Subsription Management
        
        private void SubscribeToTerrain()
        {
            if (_isSubscribed)
            {
                if (enableLogging)
                    Debug.LogWarning("[BuriedObjectManager] Already subscribed!");
                return;
            }
            
            if (voxelTerrain == null)
            {
                Debug.LogError("<color=red>[BuriedObjectManager]</color> Cannot subscribe: VoxelTerrain is NULL!");
                return;
            }
        
            voxelTerrain.OnTerrainChanged += HandleTerrainModification;
            _isSubscribed = true;
            if (enableLogging)
                Debug.Log($"<color=green>[BuriedObjectManager]</color> ✓ Subscribed to terrain events from '{voxelTerrain.name}'");
        }
        
        private void UnsubscribeFromTerrain()
        {
            if (!_isSubscribed || voxelTerrain == null) return;
            
            voxelTerrain.OnTerrainChanged -= HandleTerrainModification;
            _isSubscribed = false;
            
            if (enableLogging)
                Debug.Log($"Unsubscribed from terrain events");
        }
        
        #endregion
        
        #region Object Registration
        
        public void RegisterObject(BuriedObject obj)
        {
            if (obj == null) return;
        
            if (!_allObjects.Contains(obj))
            {
                _allObjects.Add(obj);
                AddToIndex(obj);
            
                if (enableLogging)
                    Debug.Log($"<color=cyan>[BuriedObjectManager]</color> Registered: {obj.name} at {obj.Position}");
            
                if (voxelTerrain != null)
                {
                    obj.UpdateExposure(voxelTerrain);
                }
                else
                {
                    Debug.LogWarning($"<color=orange>[BuriedObjectManager]</color> {obj.name} registered but terrain is null");
                }
            }
        }

        public void UnregisterObject(BuriedObject obj)
        {
            if (obj == null) return;
            
            if (_allObjects.Remove(obj))
            {
                RemoveFromIndex(obj);
                if (enableLogging)
                    Debug.Log($"Unregistered: {obj.name}");
            }
        }
        #endregion
        
        #region Spatial Index Logic

        private void InitializeSpatialIndex()
        {
            // Compute world bounds of the terrain for octree initialization
            _terrainWorldBounds = CalculateTerrainWorldBounds();

            switch (spatialIndexType)
            {
                case SpatialIndexType.Grid:
                    _spatialIndex = new GridSpatialIndex<BuriedObject>(spatialCellSize);
                    break;
                case SpatialIndexType.Octree:
                default:
                    var bounds = _terrainWorldBounds.size.sqrMagnitude > 0.0f
                        ? _terrainWorldBounds
                        : new Bounds(transform.position, Vector3.one * 128f);
                    _spatialIndex = new OctreeSpatialIndex<BuriedObject>(bounds, Mathf.Max(1, octreeMaxDepth), Mathf.Max(1, octreeNodeCapacity));
                    break;
            }

            if (enableLogging)
            {
                Debug.Log($"[BuriedObjectManager] Spatial index initialized: {spatialIndexType} | Bounds={_terrainWorldBounds}");
            }
        }

        private void RebuildSpatialIndex()
        {
            if (_spatialIndex == null)
                InitializeSpatialIndex();

            _spatialIndex.Clear();
            foreach (var obj in _allObjects)
            {
                if (obj != null)
                    AddToIndex(obj);
            }
        }

        private void AddToIndex(BuriedObject obj)
        {
            if (_spatialIndex == null)
                InitializeSpatialIndex();

            var b = GetObjectBounds(obj);
            _spatialIndex.Add(obj, b);
        }

        private void RemoveFromIndex(BuriedObject obj)
        {
            _spatialIndex?.Remove(obj);
        }

        private Bounds CalculateTerrainWorldBounds()
        {
            if (voxelTerrain == null)
                return new Bounds(transform.position, Vector3.zero);

            // Terrain overall size in local space
            Vector3 localSize = new Vector3(
                voxelTerrain.ChunkCount.x * voxelTerrain.ChunkSize.x,
                voxelTerrain.ChunkSize.y,
                voxelTerrain.ChunkCount.z * voxelTerrain.ChunkSize.z);

            Vector3 scale = voxelTerrain.transform.lossyScale;
            Vector3 worldSize = Vector3.Scale(localSize, scale);
            Vector3 worldCenter = voxelTerrain.transform.position + worldSize * 0.5f;
            return new Bounds(worldCenter, worldSize);
        }

        #endregion
        
        #region Event Handling
        
        private void HandleTerrainModification(Bounds modifiedBounds)
        {
            if (enableLogging)
                Debug.Log($"<color=magenta>[BuriedObjectManager]</color> ★ HandleTerrainModification called! Bounds: {modifiedBounds}");
            
            if (voxelTerrain == null)
            {
                Debug.LogWarning("[BuriedObjectManager] Terrain is null during modification event");
                return;
            }

            HashSet<BuriedObject> objectsToUpdate = new();
            if (_spatialIndex == null)
            {
                InitializeSpatialIndex();
                RebuildSpatialIndex();
            }

            if (refreshIndexOnTerrainChange)
            {
                RefreshIndexBounds();
            }

            foreach (var obj in _spatialIndex.Query(modifiedBounds))
            {
                if (obj != null && obj.isActiveAndEnabled)
                {
                    objectsToUpdate.Add(obj);
                }
            }
            
            if (enableLogging)
                Debug.Log($"<color=green>[BuriedObjectManager]</color> Updating {objectsToUpdate.Count} objects in affected area");
            
            foreach (var obj in objectsToUpdate)
            {
                if (enableLogging)
                    Debug.Log($"<color=cyan>[BuriedObjectManager]</color> Updating: {obj.name}");
                obj.UpdateExposure(voxelTerrain);
            }
        }

        private void RefreshIndexBounds()
        {
            if (_spatialIndex == null) return;
            for (int i = 0; i < _allObjects.Count; i++)
            {
                var obj = _allObjects[i];
                if (obj == null) continue;
                var b = GetObjectBounds(obj);
                _spatialIndex.Update(obj, b);
            }
        }
        
        private Bounds GetObjectBounds(BuriedObject obj)
        {
            var coll = obj.GetComponent<Collider>();
            return coll != null ? coll.bounds : new Bounds(obj.transform.position, Vector3.one);
        }
        
        #endregion
        
        #region Public API
        
        [ContextMenu("Force Update All")]
        public void ForceUpdateAll()
        {
            if (voxelTerrain == null)
            {
                Debug.LogError("[BuriedObjectManager] Cannot update: terrain is null");
                return;
            }
        
            foreach (var obj in _allObjects)
            {
                if (obj != null && obj.isActiveAndEnabled)
                {
                    obj.UpdateExposure(voxelTerrain);
                }
            }
            
            if (enableLogging)
                Debug.Log($"Force updated all {_allObjects.Count} objects");
        }
        
        public void SetTerrain(VoxelTerrain terrain)
        {
            UnsubscribeFromTerrain();
            voxelTerrain = terrain;
            SubscribeToTerrain();
            InitializeSpatialIndex();
            RebuildSpatialIndex();
        }
        
        #endregion
        
        #region Debug
        
        [ContextMenu("Debug: Print Spatial Index Info")]
        private void DebugPrintSpatialIndex()
        {
            if (_spatialIndex == null)
            {
                Debug.Log("Spatial index not initialized");
                return;
            }
            Debug.Log($"=== Spatial Index ===\nType: {spatialIndexType}\nCount: {_spatialIndex.Count}\nTerrainBounds: {_terrainWorldBounds}");
        }
        
        #endregion
    }
}