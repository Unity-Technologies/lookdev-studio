using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEditor.Presets;

using Object = UnityEngine.Object;

using SFB;

namespace LookDev.Editor
{
    public class AssetManageHelpers
    {
        // Fuctions that have a dependency with the type of RenderPipeline
        public static Func<Shader> GetDefaultLitShader;

        public static Func<string> GetDefaultPathOfHdri;
        public static Func<string[]> GetPathsOfHdri;

        public static Func<string> GetDefaultPathOfSkybox;
        public static Func<string[]> GetPathsOfSkybox;


        static string previousOpendFolder;


        static public void CreateDefaultMaterial()
        {
            string defaultMaterialName = "Default";

            string newAssetPath = string.Format("Assets/{0}/{1}.mat", LookDevHelpers.LookDevSubdirectoryForMaterial, defaultMaterialName);
            newAssetPath = AssetDatabase.GenerateUniqueAssetPath(newAssetPath);

            Material newMaterial = new Material(GetDefaultLitShader?.Invoke());

            AssetDatabase.CreateAsset(newMaterial, newAssetPath);

            // Apply preset
            Preset opaquePreset = AssetDatabase.LoadAssetAtPath<Preset>("Assets/LookDev/Setup/Settings/MaterialPreset/Opaque.preset");

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


        static public void OpenAsset(Object targetObj)
        {
            if (targetObj == null)
                return;

            string assetPath = AssetDatabase.GetAssetPath(targetObj);

            if (string.IsNullOrEmpty(assetPath) == false)
            {
                AssetDatabase.OpenAsset(targetObj);
            }
        }

        static public Object CreateMaterialByPresetName(string shaderName, string outputMaterialName)
        {
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
                targetMaterial.shader = GetDefaultLitShader?.Invoke();
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

        static public void LoadTextureOnDCC(string targetFilePath)
        {
            targetFilePath = Path.GetFullPath(targetFilePath);

            if (ProjectSettingWindow.projectSetting != null)
            {
                string dccPath = ProjectSettingWindow.projectSetting.paintingTexDccPath;

                if (File.Exists(dccPath) == false || File.Exists(targetFilePath) == false)
                    return;

                System.Diagnostics.Process process = new System.Diagnostics.Process();
                process.StartInfo.FileName = dccPath;
                process.StartInfo.Arguments = $"{targetFilePath}";

                process.Start();
                process.Close();
            }
        }


        static public void LoadModelOnDCC(string path, DCCType dccType)
        {
            Object target = AssetDatabase.LoadAssetAtPath<Object>(path);

            string assetFullPath = string.Empty;

            if (target != null)
            {
                if (PrefabUtility.GetPrefabAssetType(target) == PrefabAssetType.Model) // if it's Model
                {
                    // Model
                    assetFullPath = Path.GetFullPath(path);
                    assetFullPath = assetFullPath.Replace("\\", "/");
                }
                else if (PrefabUtility.GetPrefabAssetType(target) == PrefabAssetType.Regular || PrefabUtility.GetPrefabAssetType(target) == PrefabAssetType.Variant) // if it's Prefab
                {
                    // Prefab

                    List<string> meshList = new List<string>();

                    MeshRenderer[] meshRends = (target as GameObject).GetComponentsInChildren<MeshRenderer>();
                    SkinnedMeshRenderer[] skinRends = (target as GameObject).GetComponentsInChildren<SkinnedMeshRenderer>();

                    if (meshRends.Length == 0 && skinRends.Length == 0)
                        return;

                    foreach(MeshRenderer render in meshRends)
                    {
                        MeshFilter mFilter = render.gameObject.GetComponent<MeshFilter>();
                        if (mFilter == null)
                            continue;
                        if (mFilter.sharedMesh == null)
                            continue;

                        string meshPath = AssetDatabase.GetAssetPath(mFilter.sharedMesh);

                        if (string.IsNullOrEmpty(meshPath) == false)
                        {

                            if (!meshList.Contains(meshPath))
                                meshList.Add(meshPath);
                        }
                    }

                    foreach(SkinnedMeshRenderer sRender in skinRends)
                    {
                        if (sRender.sharedMesh != null)
                        {
                            string meshPath = AssetDatabase.GetAssetPath(sRender.sharedMesh);

                            if (string.IsNullOrEmpty(meshPath) == false)
                            {
                                if (!meshList.Contains(meshPath))
                                    meshList.Add(meshPath);
                            }
                        }
                    }

                    if (meshList.Count == 0)
                        return;

                    if (meshList.Count > 1)
                    {
                        LoadMeshWindow.InitLoadMeshWindow(meshList, true);
                        assetFullPath = LoadMeshWindow.GetTarget();
                    }
                    else
                        assetFullPath = meshList[0];

                    if (string.IsNullOrEmpty(assetFullPath) == false)
                    {
                        assetFullPath = Path.GetFullPath(assetFullPath);
                        assetFullPath = assetFullPath.Replace("\\", "/");
                    }

                }
                else
                {
                    return;
                }

                if (File.Exists(assetFullPath))
                    DCCLauncher.Load(dccType, assetFullPath);
            }
        }


        static public void LoadModelOnPaintingDCC(string path, PaintingMeshDCCs paintingMeshDCC)
        {
            Object target = AssetDatabase.LoadAssetAtPath<Object>(path);

            string assetFullPath = string.Empty;

            if (target != null)
            {
                if (PrefabUtility.GetPrefabAssetType(target) == PrefabAssetType.Model)
                {
                    // Model
                    assetFullPath = Path.GetFullPath(path);
                    assetFullPath = assetFullPath.Replace("\\", "/");
                }
                else
                {
                    // Prefab
                }

                if (File.Exists(ProjectSettingWindow.projectSetting.paintingMeshDccPath) && File.Exists(assetFullPath))
                {
                    System.Diagnostics.Process process = new System.Diagnostics.Process();
                    process.StartInfo.FileName = ProjectSettingWindow.projectSetting.paintingMeshDccPath;
                    process.StartInfo.Arguments = $"--mesh \"{assetFullPath}\"";

                    process.Start();
                    process.Close();
                }
                else
                    Debug.LogWarning("Could not find Painting DCC tool");

            }
        }


        static public void ImportAsset()
        {

            if (previousOpendFolder == string.Empty)
                previousOpendFolder = Application.dataPath;

            List<string> supportedFormats = LookDevHelpers.GetSupportedFormatExtension();

            ExtensionFilter filter = new ExtensionFilter();
            filter.Name = "LookDev Files";
            filter.Extensions = new string[supportedFormats.Count];

            for (int i = 0; i < supportedFormats.Count; i++)
                filter.Extensions[i] = supportedFormats[i].Replace(".", string.Empty);


            StandaloneFileBrowser.OpenFilePanelAsync("Select the target files to be imported", previousOpendFolder, new ExtensionFilter[] { filter }, true, (string[] targetFiles) => 
            {
                if (targetFiles.Length == 0)
                    return;

                previousOpendFolder = targetFiles[0].Replace(Path.GetFileName(targetFiles[0]), string.Empty);
                LookDevHelpers.Import(targetFiles);
            });

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
                        // Duplicate preview image as well.
                        string sourcePreview = sourcePath.Replace(Path.GetFileName(sourcePath), Path.GetFileNameWithoutExtension(sourcePath) + ".png");
                        string targetPreview = targetPath.Replace(Path.GetFileName(targetPath), Path.GetFileNameWithoutExtension(targetPath) + ".png");

                        if (AssetDatabase.LoadAssetAtPath<Texture>(sourcePreview) != null)
                            AssetDatabase.CopyAsset(sourcePreview, targetPreview);


                        if (SceneView.lastActiveSceneView != null)
                            SceneView.lastActiveSceneView.ShowNotification(new GUIContent($"Asset Duplicated :\n {targetPath}"), 4f);

                        generatedGos.Add(AssetDatabase.LoadAssetAtPath<Object>(targetPath));
                    }
                }
            }

            Selection.objects = generatedGos.ToArray();

        }


        static public void ApplyPresetToObject(string presetPath, Object target)
        {
            if (target == null)
                return;

            Preset inPreset = AssetDatabase.LoadAssetAtPath<Preset>(presetPath);

            if (inPreset != null)
            {
                if (inPreset.CanBeAppliedTo(target))
                {
                    inPreset.ApplyTo(target);
                    if (target.GetType() == typeof(TextureImporter))
                        (target as TextureImporter).SaveAndReimport();
                }
            }
            else
                Debug.LogError($"Could not fine the Preset : {presetPath}");
        }


        static public void ApplyHdriPresetOnSelectedTextures(string presetPath)
        {
            if (Selection.objects.Length == 0)
            {
                Debug.LogWarning($"No Selected Object");
                return;
            }

            Preset inPreset = AssetDatabase.LoadAssetAtPath<Preset>(presetPath);

            if (inPreset != null)
            {
                foreach (Object currentGo in Selection.objects)
                {
                    if (currentGo == null)
                    {
                        Debug.LogError($"Found Null Object in the selection");
                        continue;
                    }

                    string assetPath = AssetDatabase.GetAssetPath(currentGo);

                    TextureImporter texImporter = (TextureImporter)AssetImporter.GetAtPath(assetPath);

                    if (inPreset.CanBeAppliedTo(texImporter) && string.IsNullOrEmpty(assetPath) == false && texImporter != null)
                    {
                        inPreset.ApplyTo(texImporter);
                        texImporter.SaveAndReimport();

                        string hdriDefaultPath = GetDefaultPathOfHdri?.Invoke();
                        string[] hdriDefault = GetPathsOfHdri?.Invoke();

                        if (hdriDefault.Length == 0)
                            AssetDatabase.MoveAsset(assetPath, $"{hdriDefaultPath}/{Path.GetFileName(assetPath)}");
                        else
                            AssetDatabase.MoveAsset(assetPath, $"{hdriDefault[0]}/{Path.GetFileName(assetPath)}");

                        LookDevSearchHelpers.SwitchCurrentProvider(5);

                        if (SceneView.lastActiveSceneView != null)
                            SceneView.lastActiveSceneView.ShowNotification(new GUIContent($"Asset Switched :\n {Path.GetFileNameWithoutExtension(assetPath)}"), 4f);
                    }
                }
            }
            else
            {
                Debug.LogWarning($"Could not fin the preset : {presetPath}");
                return;
            }
        }


        static public void ApplySkyboxPresetOnSelectedTextures(string presetPath)
        {
            if (Selection.objects.Length == 0)
            {
                Debug.LogWarning($"No Selected Object");
                return;
            }

            Preset inPreset = AssetDatabase.LoadAssetAtPath<Preset>(presetPath);

            if (inPreset != null)
            {
                foreach (Object currentGo in Selection.objects)
                {
                    if (currentGo == null)
                    {
                        Debug.LogError($"Found Null Object in the selection");
                        continue;
                    }

                    string assetPath = AssetDatabase.GetAssetPath(currentGo);

                    TextureImporter texImporter = (TextureImporter)AssetImporter.GetAtPath(assetPath);

                    if (inPreset.CanBeAppliedTo(texImporter) && string.IsNullOrEmpty(assetPath) == false && texImporter != null)
                    {
                        inPreset.ApplyTo(texImporter);
                        texImporter.SaveAndReimport();

                        string skyboxDefaultPath = GetDefaultPathOfSkybox?.Invoke();
                        string[] skyboxDefault = GetPathsOfSkybox?.Invoke();

                        // Move textures
                        string newTexturePath = string.Empty;

                        if (skyboxDefault.Length == 0)
                            newTexturePath = $"{skyboxDefaultPath}/{Path.GetFileName(assetPath)}";
                        else
                            newTexturePath = $"{skyboxDefault[0]}/{Path.GetFileName(assetPath)}";

                        newTexturePath = AssetDatabase.GenerateUniqueAssetPath(newTexturePath);
                        AssetDatabase.MoveAsset(assetPath, newTexturePath);

                        // Generate Material
                        Material newSkyboxMaterial = new Material(Shader.Find("Skybox/Cubemap"));

                        if (newSkyboxMaterial != null)
                        {
                            string newMaterialPath = string.Empty;

                            if (skyboxDefault.Length == 0)
                                newMaterialPath = $"{skyboxDefaultPath}/{Path.GetFileNameWithoutExtension(assetPath)}.mat";
                            else
                                newMaterialPath = $"{skyboxDefault[0]}/{Path.GetFileNameWithoutExtension(assetPath)}.mat";

                            newMaterialPath = AssetDatabase.GenerateUniqueAssetPath(newMaterialPath);

                            AssetDatabase.CreateAsset(newSkyboxMaterial, newMaterialPath);

                            newSkyboxMaterial = AssetDatabase.LoadAssetAtPath<Material>(newMaterialPath);
                            newSkyboxMaterial.SetTexture("_Tex", AssetDatabase.LoadAssetAtPath<Texture>(newTexturePath));

                        }
                        else
                            Debug.LogError($"Could not find the shader named \"Skybox/Cubemap\"");

                        LookDevSearchHelpers.SwitchCurrentProvider(5);

                        if (SceneView.lastActiveSceneView != null)
                            SceneView.lastActiveSceneView.ShowNotification(new GUIContent($"Asset Switched :\n {Path.GetFileNameWithoutExtension(assetPath)}"), 4f);
                    }
                }
            }
            else
            {
                Debug.LogWarning($"Could not find the preset : {presetPath}");
                return;
            }
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
                string previewImage = selectedAssetPath.Replace(Path.GetExtension(selectedAssetPath), ".png");

                if (string.IsNullOrEmpty(selectedAssetPath))
                {
                    Debug.LogError($"Could not find the asset Path : {currentGo.name}");
                    continue;
                }

                Object targetObj = AssetDatabase.LoadAssetAtPath<Object>(selectedAssetPath);

                if (targetObj != null)
                {
                    // Delete Preview Image
                    if (AssetDatabase.LoadAssetAtPath<Texture>(previewImage) != null)
                        AssetDatabase.DeleteAsset(previewImage);

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


        static public void OpenToAsset(params string[] assetPaths)
        {
            SearchProviderForAssociatedAssets.DisposeAssetPaths();

            foreach(string assetPath in assetPaths)
            {
                Object obj = AssetDatabase.LoadAssetAtPath<Object>(assetPath);

                if (obj != null)
                    SearchProviderForAssociatedAssets.AddAssetPath(assetPath);
            }

            LookDevSearchHelpers.SwitchCurrentProvider(-1);

        }


        static public void GoToAsset(string assetPath)
        {
            SearchProviderForAssociatedAssets.DisposeAssetPaths();

            Object selectedObj = AssetDatabase.LoadAssetAtPath<Object>(assetPath);


            if (selectedObj.GetType() == typeof(Material))
            {
                Material selectedMat = selectedObj as Material;
                string[] textureNames = selectedMat.GetTexturePropertyNames();

                foreach (string textureName in textureNames)
                {
                    Texture associatedTexture = selectedMat.GetTexture(textureName);
                    if (associatedTexture != null)
                    {
                        string texturePath = AssetDatabase.GetAssetPath(associatedTexture);
                        if (string.IsNullOrEmpty(texturePath) == false)
                        {
                            SearchProviderForAssociatedAssets.AddAssetPath(texturePath);
                        }
                    }
                }
            }
            else if (selectedObj.GetType() == typeof(Texture) || selectedObj.GetType() == typeof(Texture2D))
            {
                Texture selectedTex = selectedObj as Texture;

                string[] materialPaths = SearchProviderForMaterials.GetAllMaterialPaths();

                foreach(string materialPath in materialPaths)
                {
                    Material material = AssetDatabase.LoadAssetAtPath<Material>(materialPath);
                    string[] textureNames = material.GetTexturePropertyNames();

                    bool isCorrespondingMat = false;

                    foreach(string textureName in textureNames)
                    {
                        Texture assignedTex = material.GetTexture(textureName);

                        if (selectedTex == assignedTex)
                        {
                            isCorrespondingMat = true;
                            break;
                        }
                    }

                    if (isCorrespondingMat)
                        SearchProviderForAssociatedAssets.AddAssetPath(materialPath);
                }

            }
            else if (PrefabUtility.GetPrefabAssetType(selectedObj) != PrefabAssetType.MissingAsset && PrefabUtility.GetPrefabAssetType(selectedObj) != PrefabAssetType.NotAPrefab)
            {
                GameObject selectedGo = selectedObj as GameObject;
                Renderer[] renderers = selectedGo.GetComponentsInChildren<Renderer>();

                foreach(Renderer renderer in renderers)
                {
                    foreach(Material mat in renderer.sharedMaterials)
                    {
                        if (mat != null)
                        {
                            string targetMatPath = AssetDatabase.GetAssetPath(mat);
                            if (string.IsNullOrEmpty(targetMatPath) == false)
                                SearchProviderForAssociatedAssets.AddAssetPath(targetMatPath);
                        }
                    }
                }
            }

            LookDevSearchHelpers.SwitchCurrentProvider(-1);
        }

    }
}