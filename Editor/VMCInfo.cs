using System;
using System.IO;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.PackageManager;
#endif

namespace VoxelMarchingCubes
{
    /// <summary>
    /// Centralized info about the Voxel Marching Cubes package.
    /// Keep authoring metadata and semantic version here to avoid duplication across editor windows.
    /// </summary>
    public static class VMCInfo
    {
        private static bool _loaded;
        private static string _version;
        private static string _author;
        private static string _description;
        private static string _displayName;
        private static string _packageName;

        [Serializable]
        private class AuthorInfo
        {
            public string name;
            public string url;
        }

        [Serializable]
        private class PackageJson
        {
            public string name;
            public string version;
            public string displayName;
            public string description;
            public AuthorInfo author;
        }

        private static void EnsureLoaded()
        {
            if (_loaded) return;
            _loaded = true;

            try
            {
                // Try to resolve package.json path for both development (Assets/) and UPM-installed (Packages/) cases.
                var pkgPath = ResolvePackageJsonPath();
                if (File.Exists(pkgPath))
                {
                    var json = File.ReadAllText(pkgPath);
                    var data = JsonUtility.FromJson<PackageJson>(json);
                    if (data != null)
                    {
                        _version = string.IsNullOrWhiteSpace(data.version) ? null : data.version;
                        _displayName = string.IsNullOrWhiteSpace(data.displayName) ? null : data.displayName;
                        _description = string.IsNullOrWhiteSpace(data.description) ? null : data.description;
                        _packageName = string.IsNullOrWhiteSpace(data.name) ? null : data.name;
                        if (data.author != null)
                        {
                            _author = string.IsNullOrWhiteSpace(data.author.name) ? null : data.author.name;
                        }
                    }
                }
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[VMCInfo] Failed to load package.json: {e.Message}");
            }

            // Fallbacks if something wasn't loaded.
            _displayName ??= "Voxel Marching Cubes";
            _description ??= "Core systems and tools for voxel terrain with marching cubes for Unity";
            _version ??= "0.0.0";
            _author ??= "Unknown";
            _packageName ??= "com.j8910.voxelmarchingcubes";
        }

        private static string ResolvePackageJsonPath()
        {
            // Default fallback for dev environment under Assets
            var defaultAssetsPath = Path.Combine(Application.dataPath, "VoxelMarchingCubes", "package.json");

#if UNITY_EDITOR
            try
            {
                // 1) If installed as a UPM package, try via PackageManager using known package name
                const string kPackageName = "com.j8910.voxelmarchingcubes";
                var packagesAssetPath = $"Packages/{kPackageName}";
                var pInfo = UnityEditor.PackageManager.PackageInfo.FindForAssetPath(packagesAssetPath);
                if (pInfo != null && !string.IsNullOrEmpty(pInfo.resolvedPath))
                {
                    var candidate = Path.Combine(pInfo.resolvedPath, "package.json");
                    if (File.Exists(candidate)) return candidate;
                }

                // 2) Try to infer root from the location of this script asset (works in both Assets and Packages)
                var guids = AssetDatabase.FindAssets("VMCInfo t:Script");
                foreach (var guid in guids)
                {
                    var assetPath = AssetDatabase.GUIDToAssetPath(guid); // e.g., Assets/..../VMCInfo.cs or Packages/com.../Editor/VMCInfo.cs
                    if (string.IsNullOrEmpty(assetPath)) continue;

                    // Get directory and walk up until we find "Editor" folder, then take its parent as package root
                    var dir = Path.GetDirectoryName(assetPath)?.Replace('\\', '/');
                    if (string.IsNullOrEmpty(dir)) continue;

                    // Normalize to project absolute path
                    string absDir;
                    if (dir.StartsWith("Assets/", StringComparison.OrdinalIgnoreCase))
                    {
                        absDir = Path.Combine(Application.dataPath, dir.Substring("Assets/".Length));
                    }
                    else if (dir.StartsWith("Packages/", StringComparison.OrdinalIgnoreCase))
                    {
                        // Resolve Packages absolute root
                        var projectRoot = Path.GetFullPath(Path.Combine(Application.dataPath, ".."));
                        absDir = Path.Combine(projectRoot, dir);
                    }
                    else
                    {
                        continue;
                    }

                    // Walk up to the package root (parent of Editor folder if present)
                    var current = new DirectoryInfo(absDir);
                    while (current != null && !string.Equals(current.Name, "Editor", StringComparison.OrdinalIgnoreCase))
                    {
                        // If we already are at the package root (has package.json), use it
                        var pj = Path.Combine(current.FullName, "package.json");
                        if (File.Exists(pj)) return pj;

                        // Stop at Assets or Packages boundary to avoid scanning entire project
                        if (string.Equals(current.Name, "Assets", StringComparison.OrdinalIgnoreCase) ||
                            string.Equals(current.Name, "Packages", StringComparison.OrdinalIgnoreCase))
                        {
                            break;
                        }

                        current = current.Parent;
                    }

                    // If we ended at Editor folder, try its parent
                    if (current != null && string.Equals(current.Name, "Editor", StringComparison.OrdinalIgnoreCase))
                    {
                        var root = current.Parent;
                        if (root != null)
                        {
                            var pj = Path.Combine(root.FullName, "package.json");
                            if (File.Exists(pj)) return pj;
                        }
                    }
                }
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[VMCInfo] ResolvePackageJsonPath editor resolution failed: {e.Message}");
            }
#endif
            // 3) Fallback to dev path under Assets
            return defaultAssetsPath;
        }

        /// <summary>Semantic version of the package (from package.json).</summary>
        public static string Version { get { EnsureLoaded(); return _version; } }

        /// <summary>Primary author credit (from package.json author.name).</summary>
        public static string Author { get { EnsureLoaded(); return _author; } }

        /// <summary>Display name of the package (from package.json displayName).</summary>
        public static string DisplayName { get { EnsureLoaded(); return _displayName; } }

        /// <summary>Package name (from package.json name).</summary>
        public static string PackageName { get { EnsureLoaded(); return _packageName; } }

        /// <summary>Short description used in About dialogs and headers (from package.json).</summary>
        public static string Description { get { EnsureLoaded(); return _description; } }

        /// <summary>
        /// Year string shown in About dialogs. Not in package.json; defaults to current year.
        /// </summary>
        public static string Year => DateTime.Now.Year.ToString();
    }
}
