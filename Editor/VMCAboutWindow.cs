#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using VoxelMarchingCubes;

namespace VoxelMarchingCubes.Editor
{
    /// <summary>
    /// Minimal, non-intrusive About window to credit the author.
    /// This follows a common Unity convention: an About entry under the Tools menu.
    /// </summary>
    public sealed class VMCAboutWindow : EditorWindow
    {
        private const string TitleFallback = "Voxel Marching Cubes • About";

        [MenuItem("Tools/Voxel Marching Cubes/About", priority = 1000)]
        public static void ShowWindow()
        {
            var dynamicTitle = string.IsNullOrEmpty(VMCInfo.DisplayName)
                ? TitleFallback
                : ($"{VMCInfo.DisplayName} • About");
            var wnd = GetWindow<VMCAboutWindow>(utility: true, title: dynamicTitle, focus: true);
            wnd.minSize = new Vector2(420, 220);
            wnd.maxSize = new Vector2(600, 320);
            wnd.Show();
        }

        private void OnGUI()
        {
            GUILayout.Space(8);
            using (new GUILayout.VerticalScope("box"))
            {
                GUILayout.Label(string.IsNullOrEmpty(VMCInfo.DisplayName) ? "Voxel Marching Cubes" : VMCInfo.DisplayName, EditorStyles.boldLabel);
                EditorGUILayout.LabelField("Version", VMCInfo.Version);
                EditorGUILayout.LabelField("Author", VMCInfo.Author);
                EditorGUILayout.LabelField("Year", VMCInfo.Year);
                EditorGUILayout.LabelField("Description", VMCInfo.Description);
            }

            GUILayout.Space(6);
            EditorGUILayout.HelpBox(
                $"This package ({VMCInfo.DisplayName}) was authored by {VMCInfo.Author} in {VMCInfo.Year}.\n" +
                $"Version {VMCInfo.Version}. Thank you for using {VMCInfo.DisplayName}!",
                MessageType.Info);

            GUILayout.FlexibleSpace();

            using (new GUILayout.HorizontalScope())
            {
                GUILayout.FlexibleSpace();
                if (GUILayout.Button("Close", GUILayout.Width(80)))
                {
                    Close();
                }
            }
            GUILayout.Space(8);
        }
    }
}
#endif
