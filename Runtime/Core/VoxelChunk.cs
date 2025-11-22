using System.Collections.Generic;
using UnityEngine;
using VoxelMarchingCubes.Profiling;
using VoxelMarchingCubes.Runtime;

namespace VoxelMarchingCubes.Core
{
    /// <summary>
    /// Represents a voxel-based chunk in a 3D space, providing functionality for voxel manipulation
    /// and mesh generation based on densities within the chunk. This class implements terrain modification
    /// and mesh rebuilding using the marching cubes algorithm.
    /// </summary>
    [RequireComponent(typeof(MeshFilter),typeof(MeshRenderer), typeof(MeshCollider))]
    public class VoxelChunk : MonoBehaviour, IModificableVoxel
    {
        private VoxelGrid _voxelGrid;
        private IMeshGenerator _meshGenerator;
        private MeshFilter _meshFilter;
        private MeshRenderer _meshRenderer;
        private MeshCollider _meshCollider;
        private bool _initialized;
        
        private Mesh _mesh;

        private bool _meshDirty = false;
        
        #region Profiling Markers
        private const string PROFILE_REBUILD_MESH = "VoxelChunk.RebuildMesh";
        private const string PROFILE_MESH_GENERATION = "VoxelChunk.MeshGeneration";
        private const string PROFILE_MESH_APPLY = "VoxelChunk.MeshApply";
        private const string PROFILE_COLLIDER_UPDATE = "VoxelChunk.ColliderUpdate";
        #endregion

        /// <summary>
        /// An event that is triggered whenever the voxel chunk is modified.
        /// This occurs during terrain modification processes, such as density changes or terrain shaping.
        /// </summary>
        /// <remarks>
        /// The event provides the following parameters:
        /// <list type="bullet">
        /// <item><description><c>VoxelChunk</c>: The chunk that was modified.</description></item>
        /// <item><description><c>Vector3</c>: The position (local to the chunk) where the modification occurred.</description></item>
        /// <item><description><c>float</c>: The radius of the modification applied.</description></item>
        /// <item><description><c>float</c>: The delta value of the modification.</description></item>
        /// </list>
        /// Listeners can subscribe to this event to handle or respond to voxel modifications,
        /// such as updating dependent systems or synchronizing changes.
        /// </remarks>
        public event System.Action<VoxelChunk, Vector3, float, float> OnModified;
    
        public Vector3Int Size { get; private set; }
        public VoxelGrid VoxelGrid => _voxelGrid;

        /// <summary>
        /// Initializes the VoxelChunk with the specified size and isosurface level.
        /// </summary>
        /// <param name="size">The size of the voxel grid in the chunk, represented as a 3D vector.</param>
        /// <param name="isoLevel">The isosurface level used for the marching cubes algorithm.</param>
        public void Initialize(Vector3Int size, float isoLevel)
        {
            if (_mesh != null)
            {
                Destroy(_mesh);
            }
            Size = size;
            _voxelGrid = new VoxelGrid(size + Vector3Int.one);
            _meshGenerator = new MarchingCubesGenerator(isoLevel);
        
            _meshFilter = GetComponent<MeshFilter>();
            _meshRenderer = GetComponent<MeshRenderer>();
            _meshCollider = GetComponent<MeshCollider>();

            if (_meshFilter == null)
            {
                Debug.LogError($"{name} does not have a MeshFilter component");
            }
            if (_meshRenderer == null)
            {
                Debug.LogError($"{name} does not have a MeshRenderer component");
            }
            if (_meshCollider == null)
            {
                Debug.LogError($"{name} does not have a MeshCollider component");
            }
            
            _mesh = new Mesh { name = $"{name}_mesh" };
            _mesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;
            _mesh.MarkDynamic();
            _meshFilter.sharedMesh = _mesh;
            
            _meshCollider.sharedMesh = null;
            _initialized = true;
        }
        
        #region Density Access Methods

        /// <summary>
        /// Sets the density value for a specific voxel at the given position within the voxel grid.
        /// </summary>
        /// <param name="position">The 3D local position of the voxel in the chunk, represented as a Vector3Int.</param>
        /// <param name="density">The density value to be set for the voxel. Higher values increase voxel solidity.</param>
        public void SetDensity(Vector3Int position, float density)
        {
            _voxelGrid.SetVoxel(position, density);
        }

        /// <summary>
        /// Sets the density of a voxel at the specified position within the voxel grid.
        /// </summary>
        /// <param name="x">The x-coordinate of the voxel position.</param>
        /// <param name="y">The y-coordinate of the voxel position.</param>
        /// <param name="z">The z-coordinate of the voxel position.</param>
        /// <param name="density">The density value to set for the voxel.</param>
        public void SetDensity(int x, int y, int z, float density)
        {
            _voxelGrid.SetVoxel(new Vector3Int(x, y, z), density);
        }

        /// <summary>
        /// Retrieves the density value of the voxel at the specified position within the voxel grid.
        /// </summary>
        /// <param name="position">The 3D position of the voxel whose density is to be retrieved, represented as a Vector3Int.</param>
        /// <returns>The density value of the voxel at the specified position. Returns 0 if the position is outside the bounds of the grid.</returns>
        public float GetDensity(Vector3Int position)
        {
            return _voxelGrid.GetDensity(position);
        }

        /// <summary>
        /// Retrieves the density value at the specified voxel grid coordinates.
        /// </summary>
        /// <param name="x">The x-coordinate of the voxel within the grid.</param>
        /// <param name="y">The y-coordinate of the voxel within the grid.</param>
        /// <param name="z">The z-coordinate of the voxel within the grid.</param>
        /// <returns>The density value at the specified coordinates.</returns>
        public float GetDensity(int x, int y, int z)
        {
            return _voxelGrid.GetDensity(new Vector3Int(x, y, z));
        }

        #endregion

        #region Terrain Modification Methods

        /// <summary>
        /// Modifies the terrain within the chunk by altering the density values of voxels
        /// in a specified spherical area based on the given parameters.
        /// </summary>
        /// <param name="localPosition">The local position within the chunk where the modification is centered.</param>
        /// <param name="radius">The radius of the spherical region to be modified.</param>
        /// <param name="delta">The amount by which to adjust the density values.</param>
        /// <param name="notifyNeighbors">Indicates whether neighboring chunks should be notified of the modification.</param>
        public void ModifyTerrain(Vector3 localPosition, float radius, float delta, bool notifyNeighbors = true)
        {
            // Expect radius in local (grid) space. Do not apply additional scaling here.
            _voxelGrid.ModifyDensity(localPosition, radius, delta);
            _meshDirty = true;

            if (notifyNeighbors)
            {
                OnModified?.Invoke(this, localPosition, radius, delta);
            }
        }

        /// <summary>
        /// Modifies the terrain of the voxel chunk by applying the specified delta to the densities of voxels
        /// within the given radius around the provided world position.
        /// </summary>
        /// <param name="worldPosition">The world-space position where the terrain modification should be applied.</param>
        /// <param name="radius">The radius around the world position within which voxels will be modified.</param>
        /// <param name="delta">The change in density to be applied to the voxels within the specified radius.</param>
        public void ModifyTerrain(Vector3 worldPosition, float radius, float delta)
        {
            Vector3 localPos = transform.InverseTransformPoint(worldPosition);
            // Convert world-space radius to local radius using this chunk's average scale
            Vector3 scale = transform.lossyScale;
            float avgScale = (scale.x + scale.y + scale.z) / 3f;
            float localRadius = avgScale > 0f ? radius / avgScale : radius;
            ModifyTerrain(localPos, localRadius, delta, notifyNeighbors: true);
        }

        /// <summary>
        /// Sets the density of voxels within a specified radius around a local position, blending the current density
        /// with the target density based on a blend factor.
        /// </summary>
        /// <param name="localPosition">The center position in local chunk space where the density modification will be applied.</param>
        /// <param name="radius">The radius around the local position within which voxel densities will be modified.</param>
        /// <param name="targetDensity">The target density value to set within the specified radius.</param>
        /// <param name="blendFactor">The factor that controls the blending between the current and target densities; a value of 1 fully applies the target density.</param>
        public void SetDensityInRadius(Vector3 localPosition, float radius, float targetDensity, float blendFactor = 1f)
        {
            // Expect radius in local (grid) space. Do not apply additional scaling here.
            _voxelGrid.SetDensityInRadius(localPosition, radius, targetDensity, blendFactor);
            _meshDirty = true;
            
            OnModified?.Invoke(this, localPosition, radius, 0f);
        }

        /// <summary>
        /// Modifies the terrain at the specified local position within the chunk.
        /// This method adjusts the voxel densities in a spherical region without notifying neighboring chunks.
        /// </summary>
        /// <param name="localPosition">The local position in the chunk where the modification starts.</param>
        /// <param name="radius">The radius of the spherical region to be modified.</param>
        /// <param name="delta">The change in density to apply within the specified radius.</param>
        internal void ModifyTerrainInternal(Vector3 localPosition, float radius, float delta)
        {
            ModifyTerrain(localPosition, radius, delta, notifyNeighbors: false);
        }
        #endregion

        #region Mesh Generation Methods

        /// <summary>
        /// Rebuilds the mesh for the voxel chunk based on the current voxel grid data and updates the corresponding mesh collider.
        /// </summary>
        public void RebuildMesh()
        {
            if (!_initialized || _mesh == null) return;

            var profiler = PerformanceManager.Instance.Profiler;
            profiler.BeginSample(PROFILE_REBUILD_MESH);
            {
                MeshData meshData;
                
                profiler.BeginSample(PROFILE_MESH_GENERATION);
                meshData = _meshGenerator.Generate(_voxelGrid);
                profiler.EndSample();
            
                profiler.BeginSample(PROFILE_MESH_APPLY);
                {
                    _mesh.Clear();

                    if (meshData.Vertices != null && meshData.Vertices.Length > 0)
                    {
                        _mesh.SetVertices(meshData.Vertices);
                        if (meshData.Normals != null)
                            _mesh.SetNormals(meshData.Normals);
                        if (meshData.Triangles != null)
                            _mesh.SetTriangles(meshData.Triangles, 0);
                        _mesh.RecalculateTangents();
                    }
                }
                profiler.EndSample();
                

                if (_meshCollider != null)
                {
                    profiler.BeginSample(PROFILE_COLLIDER_UPDATE);
                    {
                        if (_mesh.vertexCount > 0)
                        {
                            if (!_meshCollider.enabled) _meshCollider.enabled = true;
                            _meshCollider.sharedMesh = null;
                            _meshCollider.sharedMesh = _mesh;
                        }
                        else
                        {
                            _meshCollider.sharedMesh = null;
                            _meshCollider.enabled = false;
                        }
                    }
                    profiler.EndSample();
                }
                
                profiler.RecordMetric("VoxelChunk.VertexCount", meshData.Vertices?.Length ?? 0);
                profiler.RecordMetric("VoxelChunk.TriangleCount", (meshData.Triangles?.Length ?? 0) / 3);
            }
            profiler.EndSample();
        }

        /// <summary>
        /// Marks the mesh as dirty, indicating that it needs to be rebuilt or updated.
        /// </summary>
        public void MarkMeshDirty()
        {
            _meshDirty = true;
        }
        #endregion
        private void LateUpdate()
        {
            if (!_initialized) return;
            
            if (_meshDirty)
            {
                RebuildMesh();
                _meshDirty = false;
            }
        }

        [System.Obsolete("Use ModifyTerrain(worldPosition, radius, delta) on VoxelChunk (world-space overload) or prefer VoxelTerrain.ModifyTerrain instead.")]
        public void ModifyTerrainWorld(Vector3 worldPosition, float radius, float delta)
        {
            Vector3 localPos = transform.InverseTransformPoint(worldPosition);
            // Convert world-space radius to local radius using this chunk's average scale
            Vector3 scale = transform.lossyScale;
            float avgScale = (scale.x + scale.y + scale.z) / 3f;
            float localRadius = avgScale > 0f ? radius / avgScale : radius;
            ModifyTerrain(localPos, localRadius, delta);
        }

        private void OnDestroy()
        {
            if (_mesh != null)
            {
                Destroy(_mesh);
                _mesh = null;
            }

            OnModified = null;
        }
        
        #region Helper Methods

        public Vector3 WorldToLocal(Vector3 worldPosition)
        {
            return transform.InverseTransformPoint(worldPosition);
        }

        public Vector3 LocalToWorld(Vector3 localPosition)
        {
            return transform.TransformPoint(localPosition);
        }

        public Bounds GetWorldBounds()
        {
            Vector3 center = transform.TransformPoint(new Vector3(Size.x, Size.y, Size.z) * 0.5f);
            Vector3 size = Vector3.Scale(new Vector3(Size.x, Size.y, Size.z), transform.lossyScale);
            return new Bounds(center, size);
        }
        #endregion
    }
}
