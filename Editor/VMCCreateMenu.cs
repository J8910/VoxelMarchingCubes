#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using VoxelMarchingCubes.Runtime;
using VoxelMarchingCubes.Tools;

namespace VoxelMarchingCubes.Editor
{
    /// <summary>
    /// Adds convenient GameObject context menu items to create Voxel Marching Cubes objects.
    /// Right-click in the Hierarchy (or use the GameObject menu) to add components with sane defaults.
    /// </summary>
    internal static class VMCCreateMenu
    {
        private const int BasePriority = 10;

        [MenuItem("GameObject/Voxel Marching Cubes/Voxel Terrain", false, BasePriority)]
        private static void CreateVoxelTerrain(MenuCommand menuCommand)
        {
            var go = new GameObject("Voxel Terrain");
            go.AddComponent<VoxelTerrain>();

            PlaceInHierarchy(go, menuCommand);
            Undo.RegisterCreatedObjectUndo(go, "Create Voxel Terrain");
            Selection.activeObject = go;
        }

        [MenuItem("GameObject/Voxel Marching Cubes/Terrain Tool Controller", false, BasePriority + 1)]
        private static void CreateTerrainToolController(MenuCommand menuCommand)
        {
            var go = new GameObject("Terrain Tool Controller");
            go.AddComponent<TerrainToolController>(); // RequireComponent will add SphereCollider

            // Ensure the auto-added SphereCollider is configured as a trigger for interaction
            var sphere = go.GetComponent<SphereCollider>();
            if (sphere != null)
            {
                sphere.isTrigger = true;
            }

            PlaceInHierarchy(go, menuCommand);
            Undo.RegisterCreatedObjectUndo(go, "Create Terrain Tool Controller");
            Selection.activeObject = go;
        }

        private static void PlaceInHierarchy(GameObject go, MenuCommand menuCommand)
        {
            var parent = menuCommand?.context as GameObject;
            if (parent != null)
            {
                GameObjectUtility.SetParentAndAlign(go, parent);
            }
            // Ensure the object is created in the current stage (scene or prefab editing stage)
            StageUtility.PlaceGameObjectInCurrentStage(go);
        }
    }
}
#endif
