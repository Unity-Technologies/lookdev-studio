using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;

[InitializeOnLoad]
public static class PackageImportSetup
{
    static Dictionary<string, HashSet<Type>> directoryStructure = new Dictionary<string, HashSet<Type>>()
    {
        {"Materials", new HashSet<Type>(){typeof(Material)}},
        {"Models", new HashSet<Type>(){typeof(GameObject)}},
        {"Textures", new HashSet<Type>(){typeof(Texture2D),typeof(Texture3D)}},
        {"Scenes", new HashSet<Type>(){typeof(Scene)}},
    };
    
    static PackageImportSetup()
    {
        EnsureUserDirectoriesExist(false);
    }

    static private void EnsureUserDirectoriesExist(bool destructiveCorrection)
    {
        const string rootDirName = "Assets";
        const string keyDirName = "LookDev";
        string assetPathBase = Path.Combine(rootDirName, keyDirName);

        if (!AssetDatabase.IsValidFolder(assetPathBase))
        {
            AssetDatabase.CreateFolder(rootDirName, keyDirName);
        }
        // TODO: Check for non-compliant subdirectories. 

        foreach (var entry in directoryStructure)
        {
            string subDirName = entry.Key;
            string assetPathSubDir = Path.Combine(assetPathBase, subDirName);

            // If asset directory is missing, create an empty one and move on.
            if (!AssetDatabase.IsValidFolder(assetPathSubDir))
            {
                AssetDatabase.CreateFolder(assetPathBase, subDirName);
                continue;
            }

            // We need the filesystem path here.
            string filepathSubDir = Path.Combine(Application.dataPath, keyDirName, subDirName);
            if (EnsureExclusiveType(assetPathSubDir, entry.Value, destructiveCorrection, out var nonCompliantFiles))
                continue;
            
            
            // Micro-optimizations: inverting branch & loop nesting to closer resemble a single-operation loop.
            if(destructiveCorrection)
                foreach (var f in nonCompliantFiles)
                    Debug.LogWarning($"[LookDev] Deleted non-compliant asset for [{subDirName}]: {f}");
            else
                foreach (var f in nonCompliantFiles)
                    Debug.LogWarning($"[LookDev] Non-compliant asset found for [{subDirName}]: {f}");
        }
    }
    
    /// <summary>
    /// Checks a directory only contains the specified types.
    /// </summary>
    /// <param name="directory">Filesystem directory</param>
    /// <param name="supportedTypes"></param>
    /// <param name="destructiveCorrection">If true, non-compliant assets will be deleted.</param>
    /// <param name="nonCompliantFiles"></param>
    /// <returns>True, if directory is fully compliant with supported extensions.</returns>
    static private bool EnsureExclusiveType(string directory, HashSet<Type> supportedTypes, bool destructiveCorrection, out List<string> nonCompliantFiles)
    {
        bool outcome = true;
        nonCompliantFiles = null;

        var assets = new List<UnityEngine.Object>();
        if (TryGetUnityObjectsOfTypeFromPath(directory, assets) == 0)
        {
            return true;
        }
        
        nonCompliantFiles = new List<string>();

        foreach (var curAsset in assets)
        {
            if (supportedTypes.Contains(curAsset.GetType()))
                continue;

            outcome = false;
            nonCompliantFiles.Add(AssetDatabase.GetAssetPath(curAsset));
        }

        if (destructiveCorrection)
        {
            var failures = new List<string>();
            if (!AssetDatabase.DeleteAssets(nonCompliantFiles.ToArray(), failures))
            {
                Debug.LogError($"Failed to delete: {failures.ToString()}");
            }
        }

        return outcome;
    }
    
    /// <summary>
    /// Adds newly (if not already in the list) found assets.
    /// Returns how many found (not how many added)
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="path"></param>
    /// <param name="assetsFound">Adds to this list if it is not already there</param>
    /// <returns></returns>
    public static int TryGetUnityObjectsOfTypeFromPath<T>(string path, List<T> assetsFound) where T : UnityEngine.Object
    {
        // TODO: Make recursive.
        string[] filePaths = System.IO.Directory.GetFiles(path);
 
        int countFound = 0;
        
        if (filePaths.Length > 0)
        {
            for (int i = 0; i < filePaths.Length; i++)
            {
                string normalizedPath = filePaths[i].Replace("\\", "/");
                UnityEngine.Object obj = UnityEditor.AssetDatabase.LoadAssetAtPath(normalizedPath, typeof(T));
                if (obj is T asset)
                {
                    countFound++;
                    if (!assetsFound.Contains(asset))
                    {
                        assetsFound.Add(asset);
                    }
                }
            }
        }
 
        return countFound;
    }
}
