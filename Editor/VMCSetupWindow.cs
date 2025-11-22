#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.PackageManager;
using UnityEditor.PackageManager.Requests;
using UnityEngine;

namespace VoxelMarchingCubes.Editor
{
    /// <summary>
    /// Setup window to help users enable Burst/Jobs/Collections for the Voxel Marching Cubes package.
    /// Provides one-click package installation and scripting define configuration that matches the
    /// optional compilation flags used in the codebase.
    /// </summary>
    public sealed class VMCSetupWindow : EditorWindow
    {
        private const string Title = "Voxel Marching Cubes • Setup";
        private const string Pref_DontShowAgain = "VoxelMC.Setup.DontShowAgain";
        private const string Session_ShownThisSession = "VoxelMC.Setup.ShownSession";

        // These are the packages that unlock optimal performance for this repo
        private static readonly PackageInfoData[] RecommendedPackages =
        {
            new("com.unity.burst", "Burst", "High-performance math compiler"),
            new("com.unity.jobs", "Jobs", "C# Job System"),
            new("com.unity.collections", "Collections", "Native containers for jobs/Burst"),
            new("com.unity.mathematics", "Mathematics", "float3/math types used by Burst")
        };

        // These defines are used by the project to conditionally compile job/burst paths
        private static readonly string[] OptionalDefines =
        {
            "UNITY_BURST", "UNITY_JOBS", "UNITY_COLLECTIONS"
        };

        private ListRequest _listRequest;
        private readonly Dictionary<string, bool> _installed = new();
        private readonly Dictionary<string, AddRequest> _installing = new();
        private string _status;
        private Vector2 _scroll;

        [MenuItem("Tools/Voxel Marching Cubes/Setup", priority = 0)]
        public static void ShowWindow()
        {
            var wnd = GetWindow<VMCSetupWindow>();
            wnd.titleContent = new GUIContent(Title);
            wnd.minSize = new Vector2(520, 420);
            wnd.RefreshPackageList();
            wnd.Focus();
        }

        /// <summary>
        /// Show the window only if there's something to act upon and the user didn't opt out.
        /// </summary>
        public static void ShowWindowIfNeeded()
        {
            if (EditorPrefs.GetBool(Pref_DontShowAgain, false)) return;
            if (SessionState.GetBool(Session_ShownThisSession, false)) return;

            // Heuristic: open if optional defines are not present. Package checks are handled inside the window
            // asynchronously to avoid blocking the Editor initialization with PackageManager.
            bool missingDefines = OptionalDefines.Any(d => !HasDefine(d));

            if (missingDefines)
            {
                SessionState.SetBool(Session_ShownThisSession, true);
                ShowWindow();
            }
        }

        private void OnEnable()
        {
            RefreshPackageList();
        }

        private void RefreshPackageList()
        {
            try
            {
                _status = "Querying installed packages...";
                _listRequest = Client.List(true);
            }
            catch (Exception e)
            {
                _status = "Failed to query packages: " + e.Message;
            }
            Repaint();
        }

        private void Update()
        {
            // Process listing
            if (_listRequest != null)
            {
                if (_listRequest.IsCompleted)
                {
                    if (_listRequest.Status == StatusCode.Success)
                    {
                        _installed.Clear();
                        foreach (var p in _listRequest.Result)
                        {
                            _installed[p.name] = true;
                        }
                        _status = "";
                    }
                    else if (_listRequest.Status >= StatusCode.Failure)
                    {
                        _status = "Packages list error: " + _listRequest.Error?.message;
                    }
                    _listRequest = null;
                    Repaint();
                }
            }

            // Process ongoing installs
            if (_installing.Count > 0)
            {
                var completed = new List<string>();
                foreach (var kv in _installing)
                {
                    var req = kv.Value;
                    if (!req.IsCompleted) continue;

                    if (req.Status == StatusCode.Success)
                    {
                        _installed[kv.Key] = true;
                        _status = $"Installed {kv.Key}";
                    }
                    else if (req.Status >= StatusCode.Failure)
                    {
                        _status = $"Failed to install {kv.Key}: {req.Error?.message}";
                    }
                    completed.Add(kv.Key);
                }
                foreach (var k in completed)
                {
                    _installing.Remove(k);
                }
                if (completed.Count > 0) Repaint();
            }
        }

        private void OnGUI()
        {
            using (new GUILayout.VerticalScope())
            {
                DrawHeader();
                EditorGUILayout.Space();
                _scroll = EditorGUILayout.BeginScrollView(_scroll);
                DrawPackagesSection();
                EditorGUILayout.Space();
                DrawDefinesSection();
                EditorGUILayout.Space();
                DrawFooter();
                EditorGUILayout.EndScrollView();
            }

            if (!string.IsNullOrEmpty(_status))
            {
                EditorGUILayout.HelpBox(_status, MessageType.Info);
            }
        }

        private void DrawHeader()
        {
            GUILayout.Label("Optimize Voxel Marching Cubes", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox(
                "This project can use Unity Burst, Jobs and Collections for faster mesh generation.\n" +
                "Install recommended packages and optionally enable scripting define symbols to compile job/burst paths.",
                MessageType.None);
        }

        private void DrawPackagesSection()
        {
            GUILayout.Label("Recommended Packages", EditorStyles.boldLabel);
            using (new GUILayout.VerticalScope("box"))
            {
                foreach (var p in RecommendedPackages)
                {
                    bool installed = IsInstalledCached(p.Name);
                    // Jobs are included by default in many Unity versions. If UNITY_JOBS is already defined
                    // (or the type exists in CoreModule), consider it satisfied to avoid prompting a preview install.
                    bool jobsProvided = p.Name == "com.unity.jobs" && IsJobsProvided();
                    if (jobsProvided) installed = true;
                    using (new GUILayout.HorizontalScope())
                    {
                        GUILayout.Label(new GUIContent(p.FriendlyName, p.Description), GUILayout.Width(180));
                        GUILayout.FlexibleSpace();
                        var status = installed 
                            ? (jobsProvided ? "Provided" : "Installed") 
                            : (_installing.ContainsKey(p.Name) ? "Installing..." : "Missing");
                        Color old = GUI.color;
                        GUI.color = installed ? Color.green : Color.yellow;
                        GUILayout.Label(status, GUILayout.Width(100));
                        GUI.color = old;

                        using (new EditorGUI.DisabledScope(installed || _installing.ContainsKey(p.Name)))
                        {
                            if (GUILayout.Button("Install", GUILayout.Width(80)))
                            {
                                TryInstall(p.Name);
                            }
                        }
                    }
                }
                EditorGUILayout.Space();
                using (new EditorGUI.DisabledScope(RecommendedPackages.All(x => IsPackageSatisfied(x.Name))))
                {
                    if (GUILayout.Button("Install All Recommended"))
                    {
                        foreach (var p in RecommendedPackages)
                        {
                            if (!IsPackageSatisfied(p.Name)) TryInstall(p.Name);
                        }
                    }
                }
            }
        }

        private void DrawDefinesSection()
        {
            GUILayout.Label("Optional Scripting Define Symbols", EditorStyles.boldLabel);
            using (new GUILayout.VerticalScope("box"))
            {
                var group = EditorUserBuildSettings.selectedBuildTargetGroup;
                var nbt = NamedBuildTarget.FromBuildTargetGroup(group);
                string defines = PlayerSettings.GetScriptingDefineSymbols(nbt);
                var defineSet = new HashSet<string>((defines ?? string.Empty).Split(';'));

                EditorGUILayout.HelpBox(
                    "The code uses UNITY_BURST/UNITY_JOBS/UNITY_COLLECTIONS to enable accelerated paths. " +
                    "These are optional defines; add them if you installed the matching packages.",
                    MessageType.Info);

                bool changed = false;
                foreach (var d in OptionalDefines)
                {
                    bool has = defineSet.Contains(d);
                    bool next = EditorGUILayout.ToggleLeft($"{d}", has);
                    if (next != has)
                    {
                        changed = true;
                        if (next) defineSet.Add(d); else defineSet.Remove(d);
                    }
                }

                if (changed)
                {
                    string nextDefines = string.Join(";", defineSet.Where(s => !string.IsNullOrWhiteSpace(s)));
                    PlayerSettings.SetScriptingDefineSymbols(nbt, nextDefines);
                    _status = "Updated scripting define symbols for " + group + ".";
                }

                EditorGUILayout.Space();
                using (new GUILayout.HorizontalScope())
                {
                    if (GUILayout.Button("Enable All Defines"))
                    {
                        foreach (var d in OptionalDefines) defineSet.Add(d);
                        PlayerSettings.SetScriptingDefineSymbols(nbt, string.Join(";", defineSet));
                        _status = "Enabled all optional defines.";
                    }
                    if (GUILayout.Button("Disable All Defines"))
                    {
                        foreach (var d in OptionalDefines) defineSet.Remove(d);
                        PlayerSettings.SetScriptingDefineSymbols(nbt, string.Join(";", defineSet));
                        _status = "Disabled all optional defines.";
                    }
                }
            }
        }

        private void DrawFooter()
        {
            using (new GUILayout.HorizontalScope())
            {
                GUILayout.FlexibleSpace();
                bool dontShow = EditorPrefs.GetBool(Pref_DontShowAgain, false);
                bool next = GUILayout.Toggle(dontShow, "Don't show automatically again");
                if (next != dontShow)
                {
                    EditorPrefs.SetBool(Pref_DontShowAgain, next);
                }
            }
        }

        // Removed synchronous package check to prevent blocking the Editor on domain reload.

        private bool IsInstalledCached(string packageName)
        {
            if (_installed.TryGetValue(packageName, out var val)) return val;
            return false;
        }

        /// <summary>
        /// Determines whether the given package requirement is already satisfied.
        /// Special handling for com.unity.jobs which can be provided by Unity without the package.
        /// </summary>
        private bool IsPackageSatisfied(string packageName)
        {
            if (packageName == "com.unity.jobs")
                return IsInstalledCached(packageName) || IsJobsProvided();
            return IsInstalledCached(packageName);
        }

        private void TryInstall(string packageName)
        {
            if (_installing.ContainsKey(packageName)) return;
            try
            {
                var req = Client.Add(packageName);
                _installing[packageName] = req;
                _status = "Installing " + packageName + "...";
            }
            catch (Exception e)
            {
                _status = $"Failed to start install for {packageName}: {e.Message}";
            }
        }

        private static bool HasDefine(string define)
        {
            var group = EditorUserBuildSettings.selectedBuildTargetGroup;
            var nbt = NamedBuildTarget.FromBuildTargetGroup(group);
            string defines = PlayerSettings.GetScriptingDefineSymbols(nbt) ?? string.Empty;
            var parts = defines.Split(';');
            return parts.Any(p => string.Equals(p, define, StringComparison.Ordinal));
        }

        /// <summary>
        /// Returns true if Jobs support is already available without needing to add the com.unity.jobs package.
        /// This is true when UNITY_JOBS define is present or when the Jobs types are present in CoreModule.
        /// </summary>
        private static bool IsJobsProvided()
        {
            if (HasDefine("UNITY_JOBS")) return true;
            // Check for the type presence without assuming a specific assembly, to cover variations across Unity versions.
            var t = Type.GetType("Unity.Jobs.JobHandle");
            if (t != null) return true;
            try
            {
                var assemblies = AppDomain.CurrentDomain.GetAssemblies();
                for (int i = 0; i < assemblies.Length; i++)
                {
                    try
                    {
                        var found = assemblies[i].GetType("Unity.Jobs.JobHandle", throwOnError: false, ignoreCase: false);
                        if (found != null) return true;
                    }
                    catch { /* ignore per-assembly errors */ }
                }
            }
            catch { /* ignore domain reflection issues */ }
            return false;
        }

        private readonly struct PackageInfoData
        {
            public readonly string Name;
            public readonly string FriendlyName;
            public readonly string Description;
            public PackageInfoData(string name, string friendlyName, string description)
            {
                Name = name; FriendlyName = friendlyName; Description = description;
            }
        }
    }

    /// <summary>
    /// Bootstrap that shows the setup window once after import when assistance is likely needed.
    /// </summary>
    [InitializeOnLoad]
    internal static class VMCSetupBootstrap
    {
        static VMCSetupBootstrap()
        {
            // Defer a tick so the editor is fully loaded
            EditorApplication.update += DeferredOpen;
        }

        private static void DeferredOpen()
        {
            EditorApplication.update -= DeferredOpen;
            // Don't spam while compiling
            if (EditorApplication.isCompiling) return;
            VMCSetupWindow.ShowWindowIfNeeded();
        }
    }
}
#endif
