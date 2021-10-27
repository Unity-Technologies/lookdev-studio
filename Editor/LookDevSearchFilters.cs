using UnityEditor;
using UnityEditor.Search;
using UnityEngine;
using UnityEngine.UIElements;
using System.Collections.Generic;
using System.Runtime.Serialization.Formatters.Binary;
using System;
using System.IO;

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


        static void AddFilter(LookDevFilter lookDevFilter)
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

            BinaryFormatter bf = new BinaryFormatter();

            string outputPath = Path.Combine(Path.GetFullPath(filterPath), $"{lookDevFilter.filterName}.dat");
            string outputRelPath = $"{filterPath}/{lookDevFilter.filterName}.dat";

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
                paths = new List<string>() { "Assets/LookDev" }
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

                    globalFilter.showPrefab = globalFilter.showPrefab | keyValuePair.Value.showPrefab;
                    globalFilter.showModel = globalFilter.showModel | keyValuePair.Value.showModel;
                    globalFilter.showLightingPresetScene = globalFilter.showLightingPresetScene | keyValuePair.Value.showLightingPresetScene;
                    globalFilter.showLightingGroup = globalFilter.showLightingGroup | keyValuePair.Value.showLightingGroup;

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

            SearchProviderForMaterials.folders = gfilter.paths;
            SearchProviderForTextures.folders = gfilter.paths;
            SearchProviderForModels.folders = gfilter.paths;
            SearchProviderForShader.folders = gfilter.paths;
            SearchProviderForLight.folders = gfilter.paths;
            SearchProviderForAnimation.folders = gfilter.paths;

            SearchProviderForModels.showModel = gfilter.showModel;
            SearchProviderForModels.showPrefab = gfilter.showPrefab;

            SearchProviderForLight.showLightingPresetScene = gfilter.showLightingPresetScene;
            SearchProviderForLight.showLightingGroup = gfilter.showLightingGroup;

            // Skybox(HDRi)
            SetAssetsFolders?.Invoke(gfilter.paths.ToArray());
        }


        void OnChangedFilters()
        {
            // Make Global-filter by enabled filters
            globalFilter = UpdateGlobalFilter();
            ApplyGlobalFilter(globalFilter);

            LookDevSearchHelpers.RefreshWindow();
        }


        void OnRemoveAllFilters()
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
                    createNewFilterWindow.ShowModalUtility();

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
                createNewFilterWindow.ShowModalUtility();

                GUIUtility.ExitGUI();
            }

            GUILayout.EndVertical();

        }
    }
}
