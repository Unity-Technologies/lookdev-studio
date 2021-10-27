using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEditor.Graphs;
using UnityEditor.VersionControl;
using UnityEngine;
using UnityEngine.Rendering;

namespace LookDev.Editor
{
    public class RenderSettingsBundler : UnityEditor.Editor
    {
        public static event Action<LookDevSession> OnDirectivesCreate;
        public static event Action<LookDevSession> OnDirectivesLoad;
        
        [MenuItem("LookDev Studio/Create RenderSettings Configuration")]
        public static void CreateBundle()
        {
            //CreateRenderSettingsPackage();
            CopyProjectExport();
        }

        [MenuItem("LookDev Studio/Load RenderSettings Configuration")]
        public static void LoadBundle()
        {
            //LoadRenderSettingsPackage();
        }

        static void CreateRenderSettingsBundle()
        {
            var destination = EditorUtility.OpenFolderPanel("Select Destination", "Assets", "");

            var renderPipelineAssetGuids = AssetDatabase.FindAssets("t:RenderPipelineAsset");
            string[] renderPipelineAssetPaths = new string[renderPipelineAssetGuids.Length];

            int i = 0;
            foreach (var renderPipelineAssetGuid in renderPipelineAssetGuids)
            {
                renderPipelineAssetPaths[i] = AssetDatabase.GUIDToAssetPath(renderPipelineAssetGuid);
                ++i;
            }

            AssetBundleBuild[] buildMap = new AssetBundleBuild[renderPipelineAssetGuids.Length];
            buildMap[0].assetBundleName = "RenderSettingsBundle";
            buildMap[0].assetNames = renderPipelineAssetPaths;

            BuildPipeline.BuildAssetBundles(destination, buildMap, BuildAssetBundleOptions.None,
                BuildTarget.StandaloneWindows64);
        }

        static void CreateRenderSettingsPackage()
        {
            // THINGS WE NEED:
            //- Render Settings
            //  - Current Render Settings
            //  - Global Render Settings
            //  - Other (non-current) Render Settings?
            //- Scene
            //  - Lights
            //  - Post Processing
            //  - Skybox
            //  - Models / Prefabs / Assets
            //  - Camera?
            var destination = EditorUtility.OpenFolderPanel("Select Destination", "Assets", "");

            if (destination == String.Empty)
                return;

            var renderPipelineAssetGuids = AssetDatabase.FindAssets("t:RenderPipelineAsset");
            var renderPipelineDataGuids = AssetDatabase.FindAssets("t:ScriptableRendererData");
            string[] assetPaths = new string[renderPipelineAssetGuids.Length + renderPipelineDataGuids.Length + 1];
            
            int i = 0;
            foreach (var guid in renderPipelineAssetGuids)
            {
                assetPaths[i] = AssetDatabase.GUIDToAssetPath(guid);
                // if (Provider.hasLockingSupport)
                // {
                //     var asset = Provider.GetAssetByPath(assetPaths[i]);
                //     Provider.Lock(asset, true);
                // }
                
                ++i;
            }

            foreach (var guid in renderPipelineDataGuids)
            {
                assetPaths[i] = AssetDatabase.GUIDToAssetPath(guid);
                ++i;
            }

            LookDevSession session = CreateInstance<LookDevSession>();
            AssetDatabase.CreateAsset(session, "Assets/LookDevSession.asset");
            session.Guid = new Guid(AssetDatabase.GUIDFromAssetPath("Assets/LookDevSession.asset"));
            EditorUtility.SetDirty(session);
            AssetDatabase.SaveAssetIfDirty(session);

            AssetDatabase.RenameAsset("Assets/LookDevSession.asset", $"{session.Guid.ToHexString()}.asset");
            assetPaths[i] = $"Assets/{session.Guid.ToHexString()}.asset";
            session.Assets = assetPaths;

            // Call upon subscribers to populate directives.
            OnDirectivesCreate?.Invoke(session);
            
            EditorUtility.SetDirty(session);
            AssetDatabase.SaveAssetIfDirty(session);

            AssetDatabase.ExportPackage(assetPaths, $"{destination}/RenderSettingsBundle.lookdevsettings",
                ExportPackageOptions.Default);
        }

        static void LoadRenderSettingsPackage()
        {
            void LoadItemsCallback(string[] strings)
            {
                foreach (var item in strings)
                {
                    var asset = AssetDatabase.LoadMainAssetAtPath(item);
                    Debug.Log($"Loaded {item} of type {asset.GetType()} from LookDev Settings Bundle");
                        
                    switch (asset)
                    {
                        case RenderPipelineAsset:
                            Debug.Log($"Type is RenderPipelineAsset");
                            break;
                        case LookDevSession session:
                            Debug.Log($"Type is LookDevSession");
                            OnDirectivesLoad?.Invoke(session);
                            break;
                    }
                }

                AssetDatabase.onImportPackageItemsCompleted = null;
            }

            var destination = EditorUtility.OpenFilePanelWithFilters("Select Render Settings Bundle", "Assets",
                new[] {"LookDev Settings", "lookdevsettings"});
            
            if (destination == String.Empty)
                return;

            AssetDatabase.onImportPackageItemsCompleted = LoadItemsCallback;
            AssetDatabase.ImportPackage(destination, false);
        }

        static void CopyProjectExport()
        {
            
            // Step 1: 
            // Validate no assets live outside LookDev directory.
            {
                string diPath = Application.dataPath + @"\LookDev";
                diPath = diPath.Replace('\\', '/');

                DirectoryInfo di = new DirectoryInfo(diPath);

                var allFiles = new List<string>();
                var allDeps = new List<string>();

                foreach (var fi in di.EnumerateFiles("*.*", SearchOption.AllDirectories))
                {
                    if (fi.Extension == ".meta")
                        continue;

                    string trunkName = fi.FullName.Replace('\\', '/').Remove(0, Application.dataPath.Length - "Assets".Length);

                    var asset = AssetDatabase.LoadAssetAtPath(trunkName, AssetDatabase.GetMainAssetTypeAtPath(trunkName));
                    var path = AssetDatabase.GetAssetPath(asset);

                    allFiles.Add(trunkName);
                    string[] deps = AssetDatabase.GetDependencies(path, true);
                    allDeps.AddRange(deps);

                    //Debug.Log($"{fi.Name}:\n{string.Join("\n", deps)}");
                    for (int i = 0; i < deps.Length; i++) // Avoid foreach allocs.
                    {
                        if (deps[i].StartsWith("Assets/LookDev"))
                            continue;

                        if (deps[i].StartsWith("Packages/"))
                            continue;

                        Debug.LogError($"Dependent file found outside LookDev directory. Aborting.\n{deps[i]}");
                        return;
                    }
                }

                Debug.Log($"FILES:\n{string.Join("\n", allFiles)}");
                Debug.Log($"DEPS:\n{string.Join("\n", allDeps)}");
            }

            // Step 2:
            // Copy LookDev Folder wholesale into other project.
            {
                var destination = EditorUtility.OpenFolderPanel("Select Destination", "Assets", "");

                if (destination == String.Empty)
                    return;

                DirectoryInfo di = new DirectoryInfo(destination);
                if (di.GetFiles().Length != 0 || di.GetDirectories().Length != 0)
                {
                    Debug.LogError("Not an empty directory");
                    return;
                }

                {//Assets
                    string LookDevSrc = Application.dataPath + @"\LookDev";
                    string LookDevDst = Path.Combine(destination, "Assets", "LookDev");
                    DirectoryCopy(LookDevSrc, LookDevDst, true);
                }

                {//Packages
                    DirectoryCopy(Application.dataPath + @"\..\Packages", Path.Combine(destination, "Packages"), true);
                }
                {//Packages
                    DirectoryCopy(Application.dataPath + @"\..\ProjectSettings", Path.Combine(destination, "ProjectSettings"), true);
                }
            }
        }
        
        private static void DirectoryCopy(string sourceDirName, string destDirName, bool copySubDirs)
        {
            // Get the subdirectories for the specified directory.
            DirectoryInfo dir = new DirectoryInfo(sourceDirName);

            if (!dir.Exists)
            {
                throw new DirectoryNotFoundException(
                    "Source directory does not exist or could not be found: "
                    + sourceDirName);
            }

            DirectoryInfo[] dirs = dir.GetDirectories();
        
            // If the destination directory doesn't exist, create it.       
            Directory.CreateDirectory(destDirName);        

            // Get the files in the directory and copy them to the new location.
            FileInfo[] files = dir.GetFiles();
            foreach (FileInfo file in files)
            {
                string tempPath = Path.Combine(destDirName, file.Name);
                file.CopyTo(tempPath, false);
            }

            // If copying subdirectories, copy them and their contents to new location.
            if (copySubDirs)
            {
                foreach (DirectoryInfo subdir in dirs)
                {
                    string tempPath = Path.Combine(destDirName, subdir.Name);
                    DirectoryCopy(subdir.FullName, tempPath, copySubDirs);
                }
            }
        }


        enum SyncMode
        {
            Push,
            Pull
        }

        static void Synchronize(SyncMode mode)
        {
            var externalPath = EditorUtility.OpenFolderPanel("Select Destination", "Assets", "");

            if (externalPath == String.Empty)
                return;
            
            
            const string LDS_Subpath = "LookDev";
            string internalPath = Path.Combine(Application.dataPath,LDS_Subpath);
            externalPath = Path.Combine(externalPath, LDS_Subpath);

            switch (mode)
            {
                case SyncMode.Push:
                    DirectoryCopy(internalPath, externalPath, true);
                    break;
                case SyncMode.Pull:
                    DirectoryCopy(externalPath, internalPath, true);
                    break;
            }
        }

        [MenuItem("LookDev Studio/Sync/Push Configuration")]
        static void Push()
        {
            Synchronize(SyncMode.Push);
        }
        [MenuItem("LookDev Studio/Sync/Pull Configuration")]
        static void Pull()
        {
            Synchronize(SyncMode.Pull);
        }
    }
}