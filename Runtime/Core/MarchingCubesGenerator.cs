using System.Collections.Generic;
using UnityEngine;
#if UNITY_BURST || UNITY_COLLECTIONS || UNITY_JOBS
using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
#endif

namespace VoxelMarchingCubes.Core
{
    public class MarchingCubesGenerator : IMeshGenerator
    {
        private static readonly int[,] TriTable = Tables.TriTable;
        private static readonly int[] EdgeTable = Tables.EdgeTable;
        private readonly float _isoLevel;
        
        /// <summary>
        /// When true, attempts to use a Burst-compiled job to compute smooth normals after mesh generation.
        /// Falls back to the standard managed implementation when Jobs/Burst are unavailable or if job path fails.
        /// Defaults to false to preserve behavior and avoid scheduling overhead in small meshes.
        /// </summary>
        public static bool UseBurstJobs { get; set; }
        
        public MarchingCubesGenerator(float isoLevel)
        {
            _isoLevel = isoLevel;
        }
        
        public MeshData Generate(VoxelGrid voxelGrid)
        {
            if (voxelGrid == null) throw new System.ArgumentNullException(nameof(voxelGrid));

            Vector3Int size = voxelGrid.Size;
            // Early out if there are no cells to process
            if (size.x < 2 || size.y < 2 || size.z < 2)
            {
                return new MeshData
                {
                    Vertices = System.Array.Empty<Vector3>(),
                    Triangles = System.Array.Empty<int>(),
                    Normals = System.Array.Empty<Vector3>()
                };
            }

            int cellCount = (size.x - 1) * (size.y - 1) * (size.z - 1);
            var vertices = new List<Vector3>(cellCount * 12); // heuristic pre-size
            var triangles = new List<int>(cellCount * 15);     // up to 5 tris per cell => 15 indices

            // Reusable buffers to avoid per-cell allocations
            float[] cubeCorners = new float[8];
            Vector3[] vertList = new Vector3[12];
            var vertexMap = new Dictionary<Vector3, int>(32);

            for (int x = 0; x < size.x - 1; x++)
            for (int y = 0; y < size.y - 1; y++)
            for (int z = 0; z < size.z - 1; z++)
            {
                SampleCube(voxelGrid, x, y, z, cubeCorners);
                MarchCube(new Vector3(x, y, z), cubeCorners, vertices, triangles, vertList, vertexMap);
            }
            
            Vector3[] smoothNormals;

#if UNITY_BURST || UNITY_COLLECTIONS || UNITY_JOBS
            if (UseBurstJobs)
            {
                if (!TryComputeSmoothNormalsJob(vertices, triangles, out smoothNormals))
                {
                    smoothNormals = CalculateSmoothNormals(vertices, triangles);
                }
            }
            else
            {
                smoothNormals = CalculateSmoothNormals(vertices, triangles);
            }
#else
            smoothNormals = CalculateSmoothNormals(vertices, triangles);
#endif

            return new MeshData
            {
                Vertices = vertices.ToArray(),
                Triangles = triangles.ToArray(),
                Normals = smoothNormals
            };
        }

        private Vector3[] CalculateSmoothNormals(List<Vector3> vertices, List<int> triangles)
        {
            var smoothNormals = new Vector3[vertices.Count];
            var normalMap = new Dictionary<Vector3, Vector3>();

            for (int i = 0; i < triangles.Count; i += 3)
            {
                int i0 = triangles[i];
                int i1 = triangles[i + 1];
                int i2 = triangles[i + 2];
                
                Vector3 v0 = vertices[i0];
                Vector3 v1 = vertices[i1];
                Vector3 v2 = vertices[i2];
                
                Vector3 normal = Vector3.Cross(v1 - v0, v2 - v0).normalized;
                
                normalMap[v0] = normalMap.TryGetValue(v0, out var n0) ? n0 + normal : normal;
                normalMap[v1] = normalMap.TryGetValue(v1, out var n1) ? n1 + normal : normal;
                normalMap[v2] = normalMap.TryGetValue(v2, out var n2) ? n2 + normal : normal;
            }
            for (int i = 0; i < vertices.Count; i++)
            {
                if (normalMap.TryGetValue(vertices[i], out var smooth))
                    smoothNormals[i] = smooth.normalized;
                else
                    smoothNormals[i] = Vector3.up;
            }

            return smoothNormals;
        }

#if UNITY_BURST || UNITY_COLLECTIONS || UNITY_JOBS
        /// <summary>
        /// Attempts to compute smooth normals using a Burst-compiled job. The algorithm mirrors the managed version:
        /// it accumulates face normals into a position-keyed map to smooth across duplicate vertex indices that share
        /// the same coordinates. Returns false if any exception occurs, in which case the caller should fall back.
        /// </summary>
        private bool TryComputeSmoothNormalsJob(List<Vector3> vertices, List<int> triangles, out Vector3[] result)
        {
            result = null;
            if (vertices.Count == 0)
            {
                result = System.Array.Empty<Vector3>();
                return true;
            }

            NativeArray<float3> v = default;
            NativeArray<int> t = default;
            NativeArray<float3> n = default;
            NativeParallelHashMap<float3, float3> normalMap = default;

            try
            {
                v = new NativeArray<float3>(vertices.Count, Allocator.TempJob);
                for (int i = 0; i < vertices.Count; i++) v[i] = (float3)vertices[i];

                t = new NativeArray<int>(triangles.Count, Allocator.TempJob);
                for (int i = 0; i < triangles.Count; i++) t[i] = triangles[i];

                n = new NativeArray<float3>(vertices.Count, Allocator.TempJob);
                normalMap = new NativeParallelHashMap<float3, float3>(math.max(8, vertices.Count * 2), Allocator.TempJob);

                var job = new ComputeSmoothNormalsJob
                {
                    Vertices = v,
                    Triangles = t,
                    OutNormals = n,
                    NormalMap = normalMap
                };

                // Single job with Burst brings solid speedups while avoiding race conditions of parallel accumulation.
                var handle = job.Schedule();
                handle.Complete();

                var arr = new Vector3[n.Length];
                for (int i = 0; i < n.Length; i++)
                {
                    float3 nn = n[i];
                    // If zero, fallback to up to avoid degenerate normals
                    arr[i] = math.lengthsq(nn) > 1e-10f ? (Vector3)math.normalize(nn) : Vector3.up;
                }

                result = arr;
                v.Dispose();
                t.Dispose();
                n.Dispose();
                normalMap.Dispose();
                return true;
            }
            catch
            {
                // Ensure disposal if any allocation succeeded
                if (n.IsCreated) n.Dispose();
                if (t.IsCreated) t.Dispose();
                if (v.IsCreated) v.Dispose();
                if (normalMap.IsCreated) normalMap.Dispose();
                return false;
            }
        }

        [BurstCompile]
        private struct ComputeSmoothNormalsJob : IJob
        {
            [ReadOnly]
            public NativeArray<float3> Vertices;
            [ReadOnly]
            public NativeArray<int> Triangles;

            // Output per-index normal sums. Final normalization occurs on the managed side.
            public NativeArray<float3> OutNormals;

            // Position keyed accumulation to smooth across duplicate indices at the same position.
            public NativeParallelHashMap<float3, float3> NormalMap;

            public void Execute()
            {
                // Accumulate face normals into a position-keyed map
                for (int i = 0; i < Triangles.Length; i += 3)
                {
                    int i0 = Triangles[i];
                    int i1 = Triangles[i + 1];
                    int i2 = Triangles[i + 2];

                    float3 v0 = Vertices[i0];
                    float3 v1 = Vertices[i1];
                    float3 v2 = Vertices[i2];

                    float3 normal = math.normalize(math.cross(v1 - v0, v2 - v0));

                    Accumulate(v0, normal);
                    Accumulate(v1, normal);
                    Accumulate(v2, normal);
                }

                // Map position sums back to each vertex index
                for (int i = 0; i < Vertices.Length; i++)
                {
                    float3 pos = Vertices[i];
                    if (NormalMap.TryGetValue(pos, out var sum))
                    {
                        OutNormals[i] = sum;
                    }
                    else
                    {
                        OutNormals[i] = new float3(0, 0, 0);
                    }
                }
            }

            private void Accumulate(float3 key, float3 value)
            {
                if (NormalMap.TryGetValue(key, out var sum))
                {
                    // NativeParallelHashMap doesn't support in-place add; remove + add pattern
                    NormalMap.Remove(key);
                    NormalMap.TryAdd(key, sum + value);
                }
                else
                {
                    NormalMap.TryAdd(key, value);
                }
            }
        }
#endif

        private void SampleCube(VoxelGrid grid, int x, int y, int z, float[] cube)
        {
            cube[0] = grid.GetVoxel(new Vector3Int(x, y, z)).Density;
            cube[1] = grid.GetVoxel(new Vector3Int(x + 1, y, z)).Density;
            cube[2] = grid.GetVoxel(new Vector3Int(x + 1, y, z + 1)).Density;
            cube[3] = grid.GetVoxel(new Vector3Int(x, y, z + 1)).Density;
            cube[4] = grid.GetVoxel(new Vector3Int(x, y + 1, z)).Density;
            cube[5] = grid.GetVoxel(new Vector3Int(x + 1, y + 1, z)).Density;
            cube[6] = grid.GetVoxel(new Vector3Int(x + 1, y + 1, z + 1)).Density;
            cube[7] = grid.GetVoxel(new Vector3Int(x, y + 1, z + 1)).Density;
        }

        /// <summary>
        /// Generates polygons for a single cube in the marching cubes algorithm. Each corner of the cube is sampled
        /// against the isosurface level to determine the edges to be intersected, enabling the creation of vertices,
        /// triangles, and normals to represent the surface.
        /// </summary>
        /// <param name="pos">The position of the cube in world space.</param>
        /// <param name="cube">An array of scalar values representing the sampled density values at the cube's corners.</param>
        /// <param name="verts">A list of vertices where new vertices for the polygon will be added.</param>
        /// <param name="tris">A list of triangle indices where new indices for the polygon will be added.</param>
        private void MarchCube(
            Vector3 pos,
            float[] cube,
            List<Vector3> verts,
            List<int> tris,
            Vector3[] vertList,
            Dictionary<Vector3, int> vertexMap)
        {
            int cubeIndex = 0;
            if (cube[0] < _isoLevel) cubeIndex |= 1;
            if (cube[1] < _isoLevel) cubeIndex |= 2;
            if (cube[2] < _isoLevel) cubeIndex |= 4;
            if (cube[3] < _isoLevel) cubeIndex |= 8;
            if (cube[4] < _isoLevel) cubeIndex |= 16;
            if (cube[5] < _isoLevel) cubeIndex |= 32;
            if (cube[6] < _isoLevel) cubeIndex |= 64;
            if (cube[7] < _isoLevel) cubeIndex |= 128;
            
            if (EdgeTable[cubeIndex] == 0)
                return;

            if ((EdgeTable[cubeIndex] & 1) != 0)
                vertList[0] = VertexInterp(_isoLevel, pos + new Vector3(0, 0, 0), pos + new Vector3(1, 0, 0), cube[0], cube[1]);
            if ((EdgeTable[cubeIndex] & 2) != 0)
                vertList[1] = VertexInterp(_isoLevel, pos + new Vector3(1, 0, 0), pos + new Vector3(1, 0, 1), cube[1], cube[2]);
            if ((EdgeTable[cubeIndex] & 4) != 0)
                vertList[2] = VertexInterp(_isoLevel, pos + new Vector3(1, 0, 1), pos + new Vector3(0, 0, 1), cube[2], cube[3]);
            if ((EdgeTable[cubeIndex] & 8) != 0)
                vertList[3] = VertexInterp(_isoLevel, pos + new Vector3(0, 0, 1), pos + new Vector3(0, 0, 0), cube[3], cube[0]);
            if ((EdgeTable[cubeIndex] & 16) != 0)
                vertList[4] = VertexInterp(_isoLevel, pos + new Vector3(0, 1, 0), pos + new Vector3(1, 1, 0), cube[4], cube[5]);
            if ((EdgeTable[cubeIndex] & 32) != 0)
                vertList[5] = VertexInterp(_isoLevel, pos + new Vector3(1, 1, 0), pos + new Vector3(1, 1, 1), cube[5], cube[6]);
            if ((EdgeTable[cubeIndex] & 64) != 0)
                vertList[6] = VertexInterp(_isoLevel, pos + new Vector3(1, 1, 1), pos + new Vector3(0, 1, 1), cube[6], cube[7]);
            if ((EdgeTable[cubeIndex] & 128) != 0)
                vertList[7] = VertexInterp(_isoLevel, pos + new Vector3(0, 1, 1), pos + new Vector3(0, 1, 0), cube[7], cube[4]);
            if ((EdgeTable[cubeIndex] & 256) != 0)
                vertList[8] = VertexInterp(_isoLevel, pos + new Vector3(0, 0, 0), pos + new Vector3(0, 1, 0), cube[0], cube[4]);
            if ((EdgeTable[cubeIndex] & 512) != 0)
                vertList[9] = VertexInterp(_isoLevel, pos + new Vector3(1, 0, 0), pos + new Vector3(1, 1, 0), cube[1], cube[5]);
            if ((EdgeTable[cubeIndex] & 1024) != 0)
                vertList[10] = VertexInterp(_isoLevel, pos + new Vector3(1, 0, 1), pos + new Vector3(1, 1, 1), cube[2], cube[6]);
            if ((EdgeTable[cubeIndex] & 2048) != 0)
                vertList[11] = VertexInterp(_isoLevel, pos + new Vector3(0, 0, 1), pos + new Vector3(0, 1, 1), cube[3], cube[7]);
            
            vertexMap.Clear();
            
            for (int i = 0; TriTable[cubeIndex, i] != -1; i += 3)
            {
                int a, b, c;

                // j = 0
                Vector3 v0 = vertList[TriTable[cubeIndex, i]];
                if (!vertexMap.TryGetValue(v0, out a))
                {
                    a = verts.Count;
                    vertexMap[v0] = a;
                    verts.Add(v0);
                }

                // j = 1
                Vector3 v1 = vertList[TriTable[cubeIndex, i + 1]];
                if (!vertexMap.TryGetValue(v1, out b))
                {
                    b = verts.Count;
                    vertexMap[v1] = b;
                    verts.Add(v1);
                }

                // j = 2
                Vector3 v2 = vertList[TriTable[cubeIndex, i + 2]];
                if (!vertexMap.TryGetValue(v2, out c))
                {
                    c = verts.Count;
                    vertexMap[v2] = c;
                    verts.Add(v2);
                }

                tris.Add(a);
                tris.Add(c);
                tris.Add(b);
            }
        }

        private Vector3 VertexInterp(float iso, Vector3 p1, Vector3 p2, float valp1, float valp2)
        {
            if (Mathf.Abs(iso - valp1) < 0.00001f)
                return p1;
            if (Mathf.Abs(iso - valp2) < 0.00001f)
                return p2;
            if (Mathf.Abs(valp1 - valp2) < 0.00001f)
                return p1;
            float mu = (iso - valp1) / (valp2 - valp1);
            return p1 + mu * (p2 - p1);
        }
    }
}
