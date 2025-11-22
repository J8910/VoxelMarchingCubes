using UnityEngine;
using VoxelMarchingCubes.Core;

#if UNITY_EDITOR
[ExecuteAlways]
public class VoxelGridDebugger : MonoBehaviour
{
    [Header("Debug Settings")]
    [SerializeField] private bool drawVoxels = true;
    [SerializeField, Range(0.01f, 1f)] private float voxelSize = 0.1f;
    [SerializeField] private Color solidColor = new(0f, 1f, 0f, 0.3f);
    [SerializeField] private Color emptyColor = new(1f, 0f, 0f, 0.1f);
    [SerializeField] private float isoLevel = 0.5f;

    [Header("Grid Reference")]
    [SerializeField] private VoxelGrid voxelGrid;

    private void OnDrawGizmos()
    {
        if (!drawVoxels || voxelGrid == null)
            return;

        Gizmos.matrix = transform.localToWorldMatrix;

        Vector3Int size = voxelGrid.Size;
        
        for (int x = 0; x < size.x; x++)
        for (int y = 0; y < size.y; y++)
        for (int z = 0; z < size.z; z++)
        {
            var voxel = voxelGrid.GetVoxel(new Vector3Int(x, y, z));
            Gizmos.color = voxel.Density >= isoLevel ? solidColor : emptyColor;

            Vector3 pos = new Vector3(x, y, z);
            Gizmos.DrawCube(pos, Vector3.one * voxelSize);
        }
    }
}
#endif