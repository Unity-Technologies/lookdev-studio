using UnityEditor;
using UnityEditor.Search;
using UnityEngine;
using UnityEngine.UIElements;
using System.Collections.Generic;
using System.Runtime.Serialization.Formatters.Binary;
using System;
using System.IO;
using System.Linq;

using Object = UnityEngine.Object;

namespace LookDev.Editor
{

    [Serializable]
    public class LookDevFilter
    {
        public bool enabled = false;
        public string filterName;

        public List<string> pathForMaterial;
        public List<string> pathForTexture;
        public List<string> pathForModel;
        public List<string> pathForShader;
        public List<string> pathForLight;
        public List<string> pathForSkybox;
        public List<string> pathForAnimation;

        public List<string> paths;

        public List<string> objectGuid;

        public bool showPrefab = true;
        public bool showModel = true;
        public bool showLightingPresetScene = true;
        public bool showLightingGroup = true;

        public LookDevFilter()
        {
            filterName = "NewFilter";
            pathForMaterial = new List<string>();
            pathForTexture = new List<string>();
            pathForModel = new List<string>();
            pathForShader = new List<string>();
            pathForLight = new List<string>();
            pathForSkybox = new List<string>();
            pathForAnimation = new List<string>();
            paths = new List<string>();

            objectGuid = new List<string>();

            showPrefab = false;
            showModel = false;
            showLightingPresetScene = false;
            showLightingGroup = false;
        }
    }



    public class LookDevSearchFilters : EditorWindow
    { 

        static readonly string filterPath = "Assets/LookDev/Setup/Filter";

        public LookDevFilter globalFilter;
        public static Dictionary<string, LookDevFilter> filters = new Dictionary<string, LookDevFilter>();

        Vector2 scrollPosition;


        public static Action<string[]> SetAssetsFolders;

        CreateNewFilterWindow createNewFilterWindow;


        public static void AddFilter(LookDevFilter lookDevFilter)
        {
            AddFilter(lookDevFilter.filterName, lookDevFilter);
        }


        static void AddFilter(string filterName, LookDevFilter lookDevFilter)
        {
            if (lookDevFilter == null || string.IsNullOrEmpty(filterName))
                return;


            if (filters.ContainsKey(filterName) == false)
                filters.Add(filterName, lookDevFilter);
            else
            {
                filters[filterName] = lookDevFilter;
            }
        }


        public static void SaveFilter(LookDevFilter lookDevFilter)
        {
            if (lookDevFilter == null || string.IsNullOrEmpty(lookDevFilter.filterName))
                return;

            string outputPath = Path.Combine(Path.GetFullPath(filterPath), $"{lookDevFilter.filterName}.dat");
            string outputRelPath = $"{filterPath}/{lookDevFilter.filterName}.dat";

            string previousFilterPath = $"{filterPath}/{lookDevFilter.filterName}.dat";

            BinaryFormatter bf = new BinaryFormatter();

            FileStream fileStream = File.Create(outputPath);

            bf.Serialize(fileStream, lookDevFilter);
            fileStream.Close();

            AssetDatabase.ImportAsset(outputRelPath);

            if (SceneView.lastActiveSceneView != null)
                SceneView.lastActiveSceneView.ShowNotification(new GUIContent($"Saved Filter : {outputPath}"), 4f);

        }


        public static LookDevFilter LoadFilter(string lookDevFilterName)
        {
            BinaryFormatter bf = new BinaryFormatter();

            string inputPath = Path.Combine(Path.GetFullPath(filterPath), $"{lookDevFilterName}.dat");
            string inputRelPath = $"{filterPath}/{lookDevFilterName}.dat";

            if (File.Exists(inputPath) == false)
                return null;

            FileStream fileStream = File.Open(inputPath, FileMode.Open);

            if (fileStream == null || fileStream.Length == 0)
                return null;

            LookDevFilter outFilter = (LookDevFilter)bf.Deserialize(fileStream);

            fileStream.Close();

            return outFilter;
        }

        public static void RemovePreviousFilter(string filterName)
        {
            if (filterName == string.Empty)
                return;

            string outputPath = Path.Combine(Path.GetFullPath(filterPath), $"{filterName}.dat");
            string outputRelPath = $"{filterPath}/{filterName}.dat";

            if (File.Exists(outputPath))
            {
                AssetDatabase.DeleteAsset(outputRelPath);
                filters.Remove(filterName);
            }

        }


        static void RemoveFilter(string filterName)
        {
            string outputPath = Path.Combine(Path.GetFullPath(filterPath), $"{filterName}.dat");
            string outputRelPath = $"{filterPath}/{filterName}.dat";

            if (File.Exists(outputPath))
            {
                if (EditorUtility.DisplayDialog("Delete Filter", $"\"{outputRelPath}\" is going to be deleted.", "Yes", "No"))
                {
                    AssetDatabase.DeleteAsset(outputRelPath);
                    filters.Remove(filterName);
                }
            }
        }


        public static void RefreshFilters()
        {
            if (AssetDatabase.IsValidFolder(filterPath))
            {
                string[] guids = AssetDatabase.FindAssets("*", new string[] { filterPath });

                foreach(string guid in guids)
                {
                    string assetPath = AssetDatabase.GUIDToAssetPath(guid);
                    string assetName = Path.GetFileNameWithoutExtension(assetPath);

                    AddFilter(LoadFilter(assetName));
                }
            }
        }


        void GenerateDummyFliter()
        {
            // Set LDS Default Filters
            
            filters = new Dictionary<string, LookDevFilter>();

            LookDevFilter defaultFilter = new LookDevFilter()
            {
                filterName = "LDS Default",
                pathForMaterial = new List<string>() { "Assets/SampleSceneAssets/Materials/General" },
                pathForTexture = new List<string>() { "Assets/SampleSceneAssets/Textures/VFX" },
                pathForModel = new List<string>() { "Assets/LookDev/Models" },
                pathForShader = new List<string>() { "Assets/LookDev/Shaders" },
                pathForLight = new List<string>() { "Assets/LookDev/Lights" },
                pathForSkybox = new List<string>() { "Assets/LookDev/Setup/Skybox/UnityHDRI" },
                pathForAnimation = new List<string>() { "Assets/LookDev/Animations" },
                paths = new List<string>() { "Assets/LookDev" },
                objectGuid = new List<string>()
            };

            SaveFilter(defaultFilter);
            defaultFilter.filterName = "LDS Default";
            SaveFilter(defaultFilter);
            
        }


        private void OnEnable()
        {
            // Init Global Filter
            globalFilter = new LookDevFilter();

            // Check the depot of Filters
            if (AssetDatabase.IsValidFolder(filterPath) == false)
                AssetDatabase.CreateFolder("Assets/LookDev/Setup", "Filter");

            RefreshFilters();

            // Reset
            foreach (KeyValuePair<string, LookDevFilter> keyValuePair in filters)
            {
                keyValuePair.Value.enabled = false;
            }

            OnChangedFilters();

        }


        LookDevFilter UpdateGlobalFilter()
        {
            globalFilter = new LookDevFilter();

            foreach (KeyValuePair<string, LookDevFilter> keyValuePair in filters)
            {
                if (keyValuePair.Value.enabled)
                {
                    // Material
                    foreach (string path in keyValuePair.Value.pathForMaterial)
                    {
                        if (AssetDatabase.IsValidFolder(path) == false)
                            continue;

                        if (globalFilter.pathForMaterial.Contains(path) == false)
                            globalFilter.pathForMaterial.Add(path);
                    }

                    // Texture
                    foreach (string path in keyValuePair.Value.pathForTexture)
                    {
                        if (AssetDatabase.IsValidFolder(path) == false)
                            continue;

                        if (globalFilter.pathForTexture.Contains(path) == false)
                            globalFilter.pathForTexture.Add(path);
                    }

                    // Model
                    foreach (string path in keyValuePair.Value.pathForModel)
                    {
                        if (AssetDatabase.IsValidFolder(path) == false)
                            continue;

                        if (globalFilter.pathForModel.Contains(path) == false)
                            globalFilter.pathForModel.Add(path);
                    }

                    // Shader
                    foreach (string path in keyValuePair.Value.pathForShader)
                    {
                        if (AssetDatabase.IsValidFolder(path) == false)
                            continue;

                        if (globalFilter.pathForShader.Contains(path) == false)
                            globalFilter.pathForShader.Add(path);
                    }

                    // Light
                    foreach (string path in keyValuePair.Value.pathForLight)
                    {
                        if (AssetDatabase.IsValidFolder(path) == false)
                            continue;

                        if (globalFilter.pathForLight.Contains(path) == false)
                            globalFilter.pathForLight.Add(path);
                    }

                    // Skybox
                    foreach (string path in keyValuePair.Value.pathForSkybox)
                    {
                        if (AssetDatabase.IsValidFolder(path) == false)
                            continue;

                        if (globalFilter.pathForSkybox.Contains(path) == false)
                            globalFilter.pathForSkybox.Add(path);
                    }

                    // Animation
                    foreach (string path in keyValuePair.Value.pathForAnimation)
                    {
                        if (AssetDatabase.IsValidFolder(path) == false)
                            continue;

                        if (globalFilter.pathForAnimation.Contains(path) == false)
                            globalFilter.pathForAnimation.Add(path);
                    }

                    // Paths
                    foreach (string path in keyValuePair.Value.paths)
                    {
                        if (AssetDatabase.IsValidFolder(path) == false)
                            continue;

                        if (globalFilter.paths.Contains(path) == false)
                            globalFilter.paths.Add(path);
                    }

                    // Objects
                    foreach (string obj in keyValuePair.Value.objectGuid)
                    {
                        if (obj == null)
                            continue;

                        if (globalFilter.objectGuid.Contains(obj) == false)
                            globalFilter.objectGuid.Add(obj);
                    }

                    globalFilter.showPrefab = globalFilter.showPrefab | keyValuePair.Value.showPrefab;
                    globalFilter.showModel = globalFilter.showModel | keyValuePair.Value.showModel;
                    globalFilter.showLightingPresetScene = globalFilter.showLightingPresetScene | keyValuePair.Value.showLightingPresetScene;
                    globalFilter.showLightingGroup = globalFilter.showLightingGroup | keyValuePair.Value.showLightingGroup;

                }
                else
                {

                }
            }

            return globalFilter;
        }


        void ApplyGlobalFilter(LookDevFilter gfilter)
        {
            if (gfilter == null)
                return;

            /*
            SearchProviderForMaterials.folders = gfilter.pathForMaterial;
            SearchProviderForTextures.folders = gfilter.pathForTexture;
            SearchProviderForModels.folders = gfilter.pathForModel;
            SearchProviderForShader.folders = gfilter.pathForShader;
            SearchProviderForLight.folders = gfilter.pathForLight;
            SearchProviderForAnimation.folders = gfilter.pathForAnimation;

            SetAssetsFolders?.Invoke(gfilter.pathForSkybox.ToArray());
            */

            // Apply folder
            SearchProviderForMaterials.folders = gfilter.paths;
            SearchProviderForTextures.folders = gfilter.paths;
            SearchProviderForModels.folders = gfilter.paths;
            SearchProviderForShader.folders = gfilter.paths;
            SearchProviderForLight.folders = gfilter.paths;
            SearchProviderForAnimation.folders = gfilter.paths;


            List<string> modelGUIDs = new List<string>();
            List<string> materialGUIDs = new List<string>();
            List<string> textureGUIDs = new List<string>();
            List<string> ShaderGUIDs = new List<string>();

            // Apply object
            for (int i=0;i<gfilter.objectGuid.Count;i++)
            {
                string currentGUID = gfilter.objectGuid[i];

                if (string.IsNullOrEmpty(currentGUID))
                    continue;

                string assetPath = AssetDatabase.GUIDToAssetPath(currentGUID);
                Object targetObj = AssetDatabase.LoadAssetAtPath<Object>(assetPath);

                if (targetObj == null)
                    continue;

                Type currnetObjType = targetObj.GetType();
                PrefabAssetType prefabAssetType = PrefabUtility.GetPrefabAssetType(targetObj);

                if (currnetObjType == typeof(Texture) || currnetObjType == typeof(Texture2D))
                {
                    
                    // Add Texture
                    if (textureGUIDs.Contains(currentGUID) == false)
                        textureGUIDs.Add(currentGUID);

                    string textureAssetPath = AssetDatabase.GUIDToAssetPath(currentGUID);

                    // Add Materials
                    string[] allMatGUIDs = AssetDatabase.FindAssets("t:Material", new string[] { "Assets" });

                    foreach(string matGUID in allMatGUIDs)
                    {
                        string matPath = AssetDatabase.GUIDToAssetPath(matGUID);

                        string[] dependentAssets = AssetDatabase.GetDependencies(matPath);

                        List<string> dependentAssetList = dependentAssets.ToList<string>();

                        if (dependentAssetList.Contains(textureAssetPath))
                        {
                            if (materialGUIDs.Contains(matGUID) == false)
                                materialGUIDs.Add(matGUID);
                        }
                    }

                    // Add Models
                    string[] allModelGUIDs = AssetDatabase.FindAssets("t:Model t:Prefab", new string[] { "Assets" });

                    foreach(string modGUID in allModelGUIDs)
                    {
                        string modPath = AssetDatabase.GUIDToAssetPath(modGUID);

                        string[] dependentAssets = AssetDatabase.GetDependencies(modPath);

                        List<string> dependentAssetList = dependentAssets.ToList<string>();

                        if (dependentAssetList.Contains(textureAssetPath))
                        {
                            if (modelGUIDs.Contains(modGUID) == false)
                                modelGUIDs.Add(modGUID);
                        }
                    }

                }
                else if (currnetObjType == typeof(Material))
                {
                    string materialAssetPath = AssetDatabase.GUIDToAssetPath(currentGUID);

                    // Add Material
                    if (materialGUIDs.Contains(currentGUID) == false)
                        materialGUIDs.Add(currentGUID);

                    // Add Texture and Shader
                    string[] dependentAssetsFromMat = AssetDatabase.GetDependencies(materialAssetPath);

                    foreach(string dependentAsset in dependentAssetsFromMat)
                    {
                        Type curType = AssetDatabase.GetMainAssetTypeAtPath(dependentAsset);

                        if (curType == typeof(Texture) || curType == typeof(Texture2D))
                        {
                            string assetGUID = AssetDatabase.AssetPathToGUID(dependentAsset);
                            if (textureGUIDs.Contains(assetGUID) == false)
                            {
                                textureGUIDs.Add(assetGUID);
                            }
                        }
                        else if (curType == typeof(Shader))
                        {
                            string assetGUID = AssetDatabase.AssetPathToGUID(dependentAsset);
                            if (ShaderGUIDs.Contains(assetGUID) == false)
                            {
                                ShaderGUIDs.Add(assetGUID);
                            }
                        }
                    }

                    // Add Model
                    string[] allModelGUIDs = AssetDatabase.FindAssets("t:Model t:Prefab", new string[] { "Assets" });

                    foreach (string modGUID in allModelGUIDs)
                    {
                        string modPath = AssetDatabase.GUIDToAssetPath(modGUID);

                        string[] dependentAssets = AssetDatabase.GetDependencies(modPath);

                        List<string> dependentAssetList = dependentAssets.ToList<string>();

                        if (dependentAssetList.Contains(materialAssetPath))
                        {
                            if (modelGUIDs.Contains(modGUID) == false)
                                modelGUIDs.Add(modGUID);
                        }
                    }

                }
                else if (currnetObjType == typeof(SceneAsset))
                {
                    string sceneAssetPath = AssetDatabase.GUIDToAssetPath(currentGUID);

                    string[] dependentAssets = AssetDatabase.GetDependencies(sceneAssetPath);

                    foreach(string dependentAsset in dependentAssets)
                    {
                        Object obj = AssetDatabase.LoadMainAssetAtPath(dependentAsset);
                        string objGUID = AssetDatabase.AssetPathToGUID(dependentAsset);

                        if (obj.GetType() == typeof(Material))
                        {
                            if (materialGUIDs.Contains(objGUID) == false)
                                materialGUIDs.Add(objGUID);
                        }
                        else if (obj.GetType() == typeof(Texture) || obj.GetType() == typeof(Texture2D))
                        {
                            if (textureGUIDs.Contains(objGUID) == false)
                                textureGUIDs.Add(objGUID);
                        }
                        else if (obj.GetType() == typeof(Shader))
                        {
                            if (ShaderGUIDs.Contains(objGUID) == false)
                                ShaderGUIDs.Add(objGUID);
                        }
                        else if (PrefabUtility.GetPrefabAssetType(obj) != PrefabAssetType.MissingAsset && PrefabUtility.GetPrefabAssetType(obj) != PrefabAssetType.NotAPrefab)
                        {
                            if (modelGUIDs.Contains(objGUID) == false)
                                modelGUIDs.Add(objGUID);
                        }
                    }

                }
                else if (currnetObjType == typeof(Shader))
                {
                    string shaderAssetPath = AssetDatabase.GUIDToAssetPath(currentGUID);

                    // Add Shader
                    if (ShaderGUIDs.Contains(currentGUID) == false)
                        ShaderGUIDs.Add(currentGUID);

                    // Add Material
                    string[] allMatGUIDs = AssetDatabase.FindAssets("t:Material", new string[] { "Assets" });

                    foreach (string matGUID in allMatGUIDs)
                    {
                        string matPath = AssetDatabase.GUIDToAssetPath(matGUID);

                        string[] dependentAssets = AssetDatabase.GetDependencies(matPath);

                        List<string> dependentAssetList = dependentAssets.ToList<string>();

                        if (dependentAssetList.Contains(shaderAssetPath))
                        {
                            if (materialGUIDs.Contains(matGUID) == false)
                                materialGUIDs.Add(matGUID);

                            // Add Texture
                            foreach(string dependentAsset in dependentAssets)
                            {
                                Type assetType = AssetDatabase.GetMainAssetTypeAtPath(dependentAsset);
                                string assetGuid = AssetDatabase.AssetPathToGUID(dependentAsset);

                                if (assetType == typeof(Texture) || assetType == typeof(Texture2D))
                                {
                                    if (textureGUIDs.Contains(assetGuid) == false)
                                        textureGUIDs.Add(assetGuid);
                                }
                            }

                        }

                    }

                    // Add Model
                    string[] allModelGUIDs = AssetDatabase.FindAssets("t:Model t:Prefab", new string[] { "Assets" });

                    foreach (string modGUID in allModelGUIDs)
                    {
                        string modPath = AssetDatabase.GUIDToAssetPath(modGUID);

                        string[] dependentAssets = AssetDatabase.GetDependencies(modPath);

                        List<string> dependentAssetList = dependentAssets.ToList<string>();

                        if (dependentAssetList.Contains(shaderAssetPath))
                        {
                            if (modelGUIDs.Contains(modGUID) == false)
                                modelGUIDs.Add(modGUID);
                        }
                    }

                }
                else if (prefabAssetType != PrefabAssetType.NotAPrefab && prefabAssetType != PrefabAssetType.MissingAsset)
                {
                    // Add Model
                    if (modelGUIDs.Contains(currentGUID) == false)
                        modelGUIDs.Add(currentGUID);

                    // Add Materials, Textures and Shaders
                    Renderer[] rends = (targetObj as GameObject).GetComponentsInChildren<Renderer>();

                    foreach(Renderer rend in rends)
                    {

                        foreach(Material mat in rend.sharedMaterials)
                        {
                            if (mat == null)
                                continue;

                            string matPath = AssetDatabase.GetAssetPath(mat);
                            string matGUID = AssetDatabase.AssetPathToGUID(matPath);

                            if (materialGUIDs.Contains(matGUID) == false)
                                materialGUIDs.Add(matGUID);

                            if (mat.shader != null)
                            {
                                string shaderPath = AssetDatabase.GetAssetPath(mat.shader);
                                string shaderGUID = AssetDatabase.AssetPathToGUID(shaderPath);

                                if (ShaderGUIDs.Contains(shaderGUID) == false)
                                {
                                    ShaderGUIDs.Add(shaderGUID);
                                }
                            }

                            string[] texPropertyNames = mat.GetTexturePropertyNames();

                            foreach(string propertyName in texPropertyNames)
                            {
                                Texture texLinked = mat.GetTexture(propertyName);

                                if (texLinked != null)
                                {
                                    string texAssetPath = AssetDatabase.GetAssetPath(texLinked);

                                    if (string.IsNullOrEmpty(texAssetPath) == false)
                                    {
                                        string texGUID = AssetDatabase.AssetPathToGUID(texAssetPath);
                                        if (textureGUIDs.Contains(texGUID) == false)
                                            textureGUIDs.Add(texGUID);
                                    }
                                }
                            }

                        }
                    }

                    //Debug.LogWarning($"{targetObj} is Prefab");
                }
                else if (currnetObjType == typeof(GameObject))
                {
                    Debug.LogWarning($"{targetObj} is GameObject");
                }
                
            }

            // Apply all
            SearchProviderForModels.objectsGUID = modelGUIDs;
            SearchProviderForMaterials.objectsGUID = materialGUIDs;
            SearchProviderForTextures.objectsGUID = textureGUIDs;
            SearchProviderForShader.objectsGUID = ShaderGUIDs;




            SearchProviderForModels.showModel = gfilter.showModel;
            SearchProviderForModels.showPrefab = gfilter.showPrefab;

            SearchProviderForLight.showLightingPresetScene = gfilter.showLightingPresetScene;
            SearchProviderForLight.showLightingGroup = gfilter.showLightingGroup;

            // Skybox(HDRi)
            SetAssetsFolders?.Invoke(gfilter.paths.ToArray());
        }


        public void OnChangedFilters()
        {
            // Make Global-filter by enabled filters
            globalFilter = UpdateGlobalFilter();
            ApplyGlobalFilter(globalFilter);

            LookDevSearchHelpers.RefreshWindow();
        }


        public void OnRemoveAllFilters()
        {
            List<string> keys = new List<string>(filters.Keys);

            for (int i = 0; i < filters.Keys.Count; i++)
                filters[(keys[i])].enabled = false;

            OnChangedFilters();
        }


        private void OnGUI()
        {
            scrollPosition = GUILayout.BeginScrollView(scrollPosition,"Box", GUILayout.Width(position.width-5), GUILayout.Height(position.height - 35));
            GUILayout.BeginVertical();

            GUILayout.BeginHorizontal();
            GUILayout.Label("Filter Assets from", EditorStyles.boldLabel);
            GUILayout.FlexibleSpace();
            if (GUILayout.Button("Remove All Filters"))
            {
                OnRemoveAllFilters();
            }
            GUILayout.EndHorizontal();

            GUILayout.Space(3);


            for (int i=0;i< filters.Keys.Count;i++)
            {
                List<string> keys = new List<string>(filters.Keys);

                GUILayout.BeginHorizontal("Box");
                EditorGUI.BeginChangeCheck();
                {
                    filters[(keys[i])].enabled = GUILayout.Toggle(filters[(keys[i])].enabled, $"{filters[(keys[i])].filterName}", GUILayout.Width((int)((float)position.width * 0.78f)));
                }
                if (EditorGUI.EndChangeCheck())
                {
                    OnChangedFilters();
                }

                GUILayout.FlexibleSpace();

                if (GUILayout.Button("E", GUILayout.Width(20)))
                {
                    createNewFilterWindow = EditorWindow.GetWindow<CreateNewFilterWindow>("Edit Filter", true);

                    createNewFilterWindow.SetFilter(filters[(keys[i])]);
                    createNewFilterWindow.ResetWindowPosition();

                    createNewFilterWindow.SetPreviousFilterName(createNewFilterWindow.GetFilter().filterName);

                    //createNewFilterWindow.ShowModalUtility();
                    createNewFilterWindow.ShowAuxWindow();

                    OnChangedFilters();
                    GUIUtility.ExitGUI();

                }
                if (GUILayout.Button("X", GUILayout.Width(20)))
                {
                    RemoveFilter(filters[(keys[i])].filterName);
                    OnChangedFilters();
                }
                GUILayout.FlexibleSpace();

                GUILayout.EndHorizontal();

            }

            GUILayout.EndVertical();
            GUILayout.EndScrollView();

            GUILayout.BeginVertical();

            if (GUILayout.Button("+ Create New Filter", GUILayout.Width(170)))
            {
                createNewFilterWindow = EditorWindow.GetWindow<CreateNewFilterWindow>("Create New Filter", true);
                createNewFilterWindow.ResetWindowPosition();

                createNewFilterWindow.GetFilter().showModel = true;
                createNewFilterWindow.GetFilter().showPrefab = true;
                createNewFilterWindow.GetFilter().showLightingGroup = true;
                createNewFilterWindow.GetFilter().showLightingPresetScene = true;

                //createNewFilterWindow.ShowModalUtility();
                createNewFilterWindow.ShowAuxWindow();

                GUIUtility.ExitGUI();
            }

            GUILayout.EndVertical();

        }



    }
}
