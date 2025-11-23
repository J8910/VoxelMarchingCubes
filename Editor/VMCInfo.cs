using System;
using System.IO;
using UnityEngine;

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
                // Build absolute path to package.json within this package under Assets.
                var pkgPath = Path.Combine(Application.dataPath, "VoxelMarchingCubes", "package.json");
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
