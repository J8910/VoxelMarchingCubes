#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using VoxelMarchingCubes.Runtime;

namespace VoxelMarchingCubes.Editor
{
    [CustomEditor(typeof(VoxelTerrain))]
    public class VoxelTerrainInspector : UnityEditor.Editor
    {
        private SerializedProperty _targetWorldSizeProp;
        private SerializedProperty _useAdaptiveResolutionProp;
        private SerializedProperty _adaptiveResolutionProp;
        private SerializedProperty _voxelsPerUnitProp;

        private void OnEnable()
        {
            _targetWorldSizeProp = serializedObject.FindProperty("targetWorldSize");
            _useAdaptiveResolutionProp = serializedObject.FindProperty("useAdaptiveResolution");
            _adaptiveResolutionProp = serializedObject.FindProperty("adaptiveResolution");
            _voxelsPerUnitProp = serializedObject.FindProperty("voxelsPerUnit");
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();
            DrawDefaultInspector();
            
            // About button (non-intrusive authoring credit)
            using (new GUILayout.HorizontalScope())
            {
                GUILayout.FlexibleSpace();
                if (GUILayout.Button("About VMC", GUILayout.Width(100)))
                {
                    VMCAboutWindow.ShowWindow();
                }
            }

            VoxelTerrain terrain = (VoxelTerrain)target;
            
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Resolution Preview", EditorStyles.boldLabel);
            
            Vector3 scale = terrain.transform.localScale;
            Vector3 targetSize = _targetWorldSizeProp.vector3Value;
            Vector3 actualSize = Vector3.Scale(targetSize, scale);
            
            EditorGUILayout.LabelField($"Target Size: {targetSize}");
            EditorGUILayout.LabelField($"Actual Size: {actualSize}");
            
            if (_useAdaptiveResolutionProp.boolValue)
            {
                
                var resolutionData = terrain.AdaptiveResolution.CalculateOptimalResolution(scale);
                EditorGUILayout.LabelField($"Optimal Chunk Size: {resolutionData.ChunkSize}");
                EditorGUILayout.LabelField($"Voxel Size: {resolutionData.VoxelWorldSize}");
                
                if (scale.magnitude < 0.01f)
                {
                    EditorGUILayout.HelpBox(
                        "Very small scale detected. Consider increasing target size or voxel density.",
                        MessageType.Warning
                    );
                }
            }
            
            EditorGUILayout.Space();
            
            if (GUILayout.Button("Test Resolution"))
            {
                terrain.InitializeTerrain();
            }
            
            if (GUILayout.Button("Regenerate with New Resolution"))
            {
                terrain.RegenerateTerrain();
            }
            
            serializedObject.ApplyModifiedProperties();
        }
    }
}
#endif