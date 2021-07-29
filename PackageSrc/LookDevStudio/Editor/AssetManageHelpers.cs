using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEditor.Presets;

using Object = UnityEngine.Object;

namespace LookDev.Editor
{
    public class AssetManageHelpers
    {


        static public void CreateDefaultMaterial()
        {
            string defaultMaterialName = "Default";

            string newAssetPath = string.Format("Assets/{0}/{1}.mat", LookDevHelpers.LookDevSubdirectoryForMaterial, defaultMaterialName);
            newAssetPath = AssetDatabase.GenerateUniqueAssetPath(newAssetPath);

            Material newMaterial = new Material(Shader.Find("HDRP/Lit"));

            AssetDatabase.CreateAsset(newMaterial, newAssetPath);

            // Apply preset
            // Packages/com.unity.lookdevstudio/Setup/Settings/MaterialPreset/Opaque.preset
            Preset opaquePreset = AssetDatabase.LoadAssetAtPath<Preset>("Packages/com.unity.lookdevstudio/Setup/Settings/MaterialPreset/Opaque.preset");

            if (opaquePreset != null)
            {
                Object target = AssetDatabase.LoadAssetAtPath<Object>(newAssetPath);

                if (target != null)
                {
                    opaquePreset.ApplyTo(target);
                    Selection.activeObject = target;

                    if (SceneView.lastActiveSceneView != null)
                        SceneView.lastActiveSceneView.ShowNotification(new GUIContent($"New Material Generated : {target.name}"), 4f);
                }
            }

        }


        static public Object CreateMaterialByPresetName(string shaderName, string outputMaterialName)
        {
            string[] tokens = shaderName.Split('/');

            string defaultMaterialName = outputMaterialName;

            string newAssetPath = string.Format("Assets/{0}/{1}.mat", LookDevHelpers.LookDevSubdirectoryForMaterial, defaultMaterialName);
            newAssetPath = AssetDatabase.GenerateUniqueAssetPath(newAssetPath);

            Shader targetShader = Shader.Find(shaderName);

            if (targetShader != null)
            {
                Material newMaterial = new Material(targetShader);
                AssetDatabase.CreateAsset(newMaterial, newAssetPath);

                Selection.activeObject = AssetDatabase.LoadAssetAtPath<Object>(newAssetPath);

                if (SceneView.lastActiveSceneView != null)
                    SceneView.lastActiveSceneView.ShowNotification(new GUIContent($"New material generated from the preset :\n {newAssetPath}"), 4f);

                return Selection.activeObject;
            }
            else
            {
                Debug.LogError($"Cound not find the target Shader : {shaderName}");
                return null;
            }
        }

        static public Object CreateMaterialByPreset(string shaderName)
        {
            string[] tokens = shaderName.Split('/');

            string defaultMaterialName = tokens[tokens.Length - 1];

            string newAssetPath = string.Format("Assets/{0}/{1}.mat", LookDevHelpers.LookDevSubdirectoryForMaterial, defaultMaterialName);
            newAssetPath = AssetDatabase.GenerateUniqueAssetPath(newAssetPath);

            Shader targetShader = Shader.Find(shaderName);

            if (targetShader != null)
            {
                Material newMaterial = new Material(targetShader);
                AssetDatabase.CreateAsset(newMaterial, newAssetPath);

                Selection.activeObject = AssetDatabase.LoadAssetAtPath<Object>(newAssetPath);

                return Selection.activeObject;
            }
            else
            {
                Debug.LogError($"Cound not find the target Shader : {shaderName}");
                return null;
            }
        }

        static public void AssignDefaultShaderOnTargetMaterial(Material targetMaterial)
        {
            if (targetMaterial != null)
            {
                targetMaterial.shader = Shader.Find("HDRP/Lit");
            }
        }

        static public void AssignDefaultShaderOnTargetMaterial(string targetMaterialPath)
        {
            if (string.IsNullOrEmpty(targetMaterialPath))
            {
                Debug.LogError("Null or Empty Material's Path");
                return;
            }

            Material extractedMap = AssetDatabase.LoadAssetAtPath<Material>(targetMaterialPath);
            AssignDefaultShaderOnTargetMaterial(extractedMap);
        }


        static public void ShowinExplorer(string path)
        {
            string fullPath = Path.GetFullPath(path);

            if (!File.Exists(fullPath))
            {
                Debug.LogError($"Could not find the path : {path}");
                return;
            }

            if (SceneView.lastActiveSceneView != null)
            {
                SceneView.lastActiveSceneView.ShowNotification(new GUIContent($"Open the file location :\n {path}"), 4f);
            }

            EditorUtility.RevealInFinder(path);
        }

        static string previousOpendFolder;

        static public void ImportAsset()
        {
            if (previousOpendFolder == string.Empty)
                previousOpendFolder = Application.dataPath;

            string targetFile = EditorUtility.OpenFilePanel("Select the target file to be imported", previousOpendFolder, "*");

            if (!File.Exists(targetFile))
                return;

            previousOpendFolder = targetFile.Replace(Path.GetFileName(targetFile), string.Empty);
            LookDevHelpers.Import(targetFile);

            
        }


        static public void DuplicateSelectedAssets()
        {
            if (Selection.objects.Length == 0)
            {
                Debug.LogWarning($"No Selected Object");
                return;
            }

            List<Object> generatedGos = new List<Object>();

            foreach (Object currentGo in Selection.objects)
            {
                if (currentGo == null)
                {
                    Debug.LogError($"Found Null Object in the selection");
                    continue;
                }

                string selectedAssetPath = AssetDatabase.GetAssetPath(currentGo.GetInstanceID());

                if (string.IsNullOrEmpty(selectedAssetPath))
                {
                    Debug.LogError($"Could not find the asset Path : {currentGo.name}");
                    continue;
                }

                Object targetObj = AssetDatabase.LoadAssetAtPath<Object>(selectedAssetPath);

                string sourcePath = selectedAssetPath;

                if (targetObj != null)
                {
                    string targetPath = AssetDatabase.GenerateUniqueAssetPath(selectedAssetPath);

                    if (AssetDatabase.CopyAsset(sourcePath, targetPath))
                    {
                        if (SceneView.lastActiveSceneView != null)
                            SceneView.lastActiveSceneView.ShowNotification(new GUIContent($"Asset Duplicated :\n {targetPath}"), 4f);

                        generatedGos.Add(AssetDatabase.LoadAssetAtPath<Object>(targetPath));
                    }
                }
            }

            Selection.objects = generatedGos.ToArray();

            // To do : SearchWindow should be updated as well.
        }


        static public void DeleteSelectedAssets()
        {
            if (Selection.objects.Length == 0)
            {
                Debug.LogWarning($"No Selected Object");
                return;
            }

            string assetPath = AssetDatabase.GetAssetPath(Selection.objects[0]);

            if (!EditorUtility.DisplayDialog("Delete selected asset?", $"{assetPath}\n\nYou cannot undo the delete assets action.", "Delete", "Cancel"))
                return;

            foreach (Object currentGo in Selection.objects)
            {
                if (currentGo == null)
                {
                    Debug.LogError($"Found Null Object in the selection");
                    continue;
                }

                string selectedAssetPath = AssetDatabase.GetAssetPath(currentGo.GetInstanceID());

                if (string.IsNullOrEmpty(selectedAssetPath))
                {
                    Debug.LogError($"Could not find the asset Path : {currentGo.name}");
                    continue;
                }

                Object targetObj = AssetDatabase.LoadAssetAtPath<Object>(selectedAssetPath);

                if (targetObj != null)
                {
                    if (!AssetDatabase.DeleteAsset(selectedAssetPath))
                    {
                        Debug.LogError($"Failed to delete {selectedAssetPath}");
                    }
                    else
                    {
                        if (SceneView.lastActiveSceneView != null)
                            SceneView.lastActiveSceneView.ShowNotification(new GUIContent($"Asset Deleted :\n {selectedAssetPath}"), 4f);
                    }
                }
            }

            Selection.objects.Initialize();

        }


        static public void RenameSelectedAssets()
        {
            if (Selection.objects.Length == 0)
            {
                Debug.LogWarning($"No Selected Object");
                return;
            }

            List<Object> renamedGos = new List<Object>();

            foreach (Object currentGo in Selection.objects)
            {
                if (currentGo == null)
                {
                    Debug.LogError($"Found Null Object in the selection");
                    continue;
                }

                string selectedAssetPath = AssetDatabase.GetAssetPath(currentGo.GetInstanceID());

                if (string.IsNullOrEmpty(selectedAssetPath))
                {
                    Debug.LogError($"Could not find the asset Path : {currentGo.name}");
                    continue;
                }

                Object targetObj = AssetDatabase.LoadAssetAtPath<Object>(selectedAssetPath);

                if (targetObj != null)
                {
                    RenameWindow.InitRenameWindow(selectedAssetPath, true);
                }
            }

        }

    }
}